using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System;
using OpenCvSharp;
using Cigen.Factories;
using Cigen.Structs;
using Cigen.Conversions;
using Cigen.ImageAnalyzing;
using GeneralPathfinder;
using UnityEditor.ShaderGraph.Serialization;
using System.Linq;
using UnityEngine.Events;
using System.ComponentModel.Design.Serialization;

namespace Cigen
{
    /// <summary>
    /// This class contains an algorithm to create a constrained road network based on user input.
    /// </summary>
    [RequireComponent(typeof(TerrainGenerator))]
    [RequireComponent(typeof(TerrainPathGenerator))]
    public class RoadNetworkGenerator : MonoBehaviour {

        /// <summary>
        /// Event is called when the solution nodes have all been identified and added to this.currentSolutionNodes
        /// </summary>
        public UnityEvent SolutionConstructedEvent;

        public AnisotropicLeastCostPathSettings settings;
        [HideInInspector]
        public TerrainPathGenerator pathfinder;

        [HideInInspector]
        public GameObject gameObjectPoint1 { get; private set; }
        [HideInInspector]
        public GameObject gameObjectPoint2 { get; private set; }
        [HideInInspector]
        public Dictionary<Texture2D, Mat> CVMaterials { get; private set; }

        [HideInInspector]
        public RoadRecord currentSerializedRoadNetwork;
        private City city;
        private GameObject terrain;
        private int terrainWidthPixels = 0;

        private Texture2D contourTex;
        private Vector3 point1;
        private Vector3 point2;
        //private PathfinderV1 pathfinder;
        private bool pathfinderIsRunning = false;
        private Coroutine pathfinderCoroutine;

        public List<Vector3> currentOrderedSolutionNodes = new List<Vector3>();


        public string saveFileName = "test";
        [ContextMenu("Save Current Road Network")]
        public void SaveCurrentRoadNetwork() {
            string jsonData = JsonUtility.ToJson(this.currentSerializedRoadNetwork);
            Debug.Log(jsonData);
            string filePath = Application.persistentDataPath + $"/{this.saveFileName.Trim()}.json";
            System.IO.File.WriteAllText(filePath, jsonData);
            Debug.Log($"Wroted file to {filePath}");
        }

        public void SetContourTexture(Texture2D contours) {
            this.contourTex = contours;
        }

        void Start() {
            SolutionConstructedEvent ??= new UnityEvent();
            transform.position = Vector3.zero;
            this.pathfinder = GetComponent<TerrainPathGenerator>();
            this.pathfinder.settings = settings;
            settings.cigen = this;

            this.gameObjectPoint1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            this.gameObjectPoint1.transform.localScale = Vector3.one * settings.GetSegmentMaskResolution(0);
            this.gameObjectPoint1.transform.position = this.point1;
            this.gameObjectPoint1.GetComponent<Renderer>().material.color = Color.green;
            this.gameObjectPoint2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            this.gameObjectPoint2.transform.localScale = this.gameObjectPoint1.transform.localScale;
            this.gameObjectPoint2.transform.position = this.point2;
            this.gameObjectPoint2.GetComponent<Renderer>().material.color = Color.red;

            //build a copy of all textures to store here
            //bwTextures is the grayscale textures
            //rgbTextures are the color textures
            List<Texture2D> bwTextures = new List<Texture2D>
            {
                settings.terrainHeightMap,
                settings.populationDensityMap,
                settings.waterMap,
                //settings.natureMap
            };

            List<Texture2D> rgbTextures = new List<Texture2D> {
                //settings.highwayMap,
            };
            //end texture array building
            /*
            //test all textures to ensure they imported properly and are the same size.
            if (ImageAnalysis.CheckAllTexturesArePowerOfTwo(bwTextures, true) == false) {
                Debug.LogError("Textures are not a power of two or not the same size. Check your input images!");
                return;
            }

            if (ImageAnalysis.CheckAllTexturesArePowerOfTwo(rgbTextures, true) == false) {            
                Debug.LogError("Textures are not a power of two or not the same size. Check your input images!");
                return;
            }*/
            //end texture testing

            //Convert all textures to OpenCvSharp Mat objects to speed up pixel operations.
            //Also build a dictionary that maps input textures to Mat objects so we don't need to save two references
            //when we wanna add another input image.
            CVMaterials = Conversion.ConvertTexturesToMats(bwTextures);
            foreach(KeyValuePair<Texture2D, Mat> kvp in Conversion.ConvertTexturesToMats(rgbTextures, false)) {
                CVMaterials.Add(kvp.Key, kvp.Value);
            }
            //end conversion to Mat objects

            //check that every Texture2D object has an associated Mat object.
            //if this passes we can assume they will have references for the rest of the lifecycle of the algo.
            List<Texture2D> allTextures = new List<Texture2D>(bwTextures);
            allTextures.AddRange(rgbTextures);
            foreach (Texture2D texture in allTextures) {
                if (CVMaterials.TryGetValue(texture, out _) == false) {
                    Debug.LogError($"{texture.name} conversion to OpenCvSharp Mat object failed.");
                    return;
                }
            }
            /*
            if (CVMaterials.TryGetValue(settings.populationDensityMap, out testMat) == false) {
                Debug.LogError("Population density map conversion failed.");
                return;
            }

            if (CVMaterials.TryGetValue(settings.terrainHeightMap, out testMat) == false) {            
                Debug.LogError("Terrain heightmap conversion failed.");
                return;
            }

            if (CVMaterials.TryGetValue(settings.waterMap, out testMat) == false) {            
                Debug.LogError("Water map conversion failed.");
                return;
            }

            if (CVMaterials.TryGetValue(settings.natureMap, out testMat) == false) {            
                Debug.LogError("Nature map conversion failed.");
                return;
            }
            */
            //end testing Mat object conversions

            //all the input images are the same size (and a power of two) so just set the size here for reference 
            //in our terrain generation method (we need it for resolution)        
            terrainWidthPixels = settings.terrainHeightMap.width;

            //some quick references
            settings.populationDensityMapMat = CVMaterials[settings.populationDensityMap];
            settings.terrainHeightMapMat = CVMaterials[settings.terrainHeightMap];
            settings.waterMapMat = CVMaterials[settings.waterMap];
            //settings.natureMapMat = CVMaterials[settings.natureMap];          
            //WE CAN START BUILDING NOW THAT THE BORING STUFF IS OUT OF THE WAY ヽ(^o^)丿

            //build terrain from heightmap
            if (settings.generateTerrain) {
                this.terrain = GetComponent<TerrainGenerator>().BuildTerrain(settings.terrainHeightMap, settings.waterMap, settings.terrainMaxHeight);
                this.terrain.AddComponent<TerrainHit>();
                //this.terrain.AddComponent<TerrainTester>();
                //StartCoroutine(BuildTerrain());
            }  

            //found a new city
            StartCoroutine(BuildCity(transform.position));

            //ProcessQueues();
        }

        void Update() {
            if (Input.GetKeyDown(KeyCode.KeypadEnter)) {
                Debug.Log("Starting over!");
                float y1 = ImageAnalysis.TerrainHeightAt(point1, settings);
                float y2 = ImageAnalysis.TerrainHeightAt(point2, settings);
                this.gameObjectPoint1.transform.position = new Vector3(this.gameObjectPoint1.transform.position.x, y1, this.gameObjectPoint1.transform.position.z);
                this.gameObjectPoint2.transform.position = new Vector3(this.gameObjectPoint2.transform.position.x, y2, this.gameObjectPoint2.transform.position.z);
                this.point1 = this.gameObjectPoint1.transform.position;
                this.point2 = this.gameObjectPoint2.transform.position;
                //start the generator over again
                _RunPathfinder(this.point1, this.point2);
            }

            if (Input.GetKeyDown(KeyCode.Backspace)) {
                StartCoroutine(DestroyRoadSegments());
            }
        }

        void OnDisable() {
            StopAllCoroutines();
        }

        private IEnumerator DestroyRoadSegments() {
            foreach (RoadSegment r in GameObject.FindObjectsByType<RoadSegment>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                GameObject.Destroy(r.gameObject);
                yield return new WaitForEndOfFrame();
            }
            yield break;
        }

        public IEnumerator BuildCity(Vector3 initState) {            
            if (settings == null) {
                Debug.LogError("No CigenSettings detected, drag and drop one into the inspector.");
                yield break;
            }

            //print("building city");
            city = CigenFactory.CreateCity(initState, settings);
        }


        

        IEnumerator RunPathfinder() {
            this.pathfinderIsRunning = true;
            while (!this.pathfinder.isSolved) {
                //Debug.Log($"Searched {closedList.Count} out of {openList.Count} left");
                //yield return this.pathfinder.Graph(this.point1, this.point2, this.openList1, this.closedList1, CitySettings.instance.highwayIdealSegmentLength);
                //yield return this.pathfinder.GraphBothEnds(this.point1, this.point2, this.openList1, this.openList2, this.closedList1, this.closedList2, CitySettings.instance.highwayIdealSegmentLength);
                yield return this.pathfinder.Graph(this.point1, this.point2);
            }
            if (this.pathfinder.isSolved) {
                Queue<Node> solutionNodes = new Queue<Node>(this.pathfinder.solution);
                Color roadColor = UnityEngine.Random.ColorHSV(0, 1, 0.9f, 1, 0.9f, 1);
                List<RoadSegmentRecord> segmentRecords = new List<RoadSegmentRecord>(); //for serialization

                while (solutionNodes.TryDequeue(out Node node)) {
                    try {
                        //Debug.Log($"Node solution: {node.worldPosition} | isBridge: {node.cost.isBridge} | isTunnel: {node.cost.isTunnel}");
                        if (node.head) {
                            continue;
                        } else {
                            //create a road segment between the node and its parent, queue the parent   
                            RoadSegment seg = new GameObject().AddComponent<RoadSegment>();
                            seg.debugColor = roadColor;
                            Cost cost = node.cost;
                            Node parentNode = cost.parentNode;
                            RoadSegmentRecord rsr = new RoadSegmentRecord(node.worldPosition, parentNode.worldPosition, node.priority, node.head, parentNode.head, cost.isBridge, cost.isTunnel);
                            segmentRecords.Add(rsr);
                            // pp = new Vector3(cost.parentPosition.x, ImageAnalysis.TerrainHeightAt(cost.parentPosition), cost.parentPosition.z);
                            seg.Init(node.worldPosition, parentNode.worldPosition, true, cost.isBridge, cost.isTunnel);
                            solutionNodes.Enqueue(parentNode);
                            yield return new WaitForEndOfFrame();
                        }                        
                    } finally {}
                    yield return new WaitForEndOfFrame();
                }
                this.currentSerializedRoadNetwork = new RoadRecord(segmentRecords); //make sure to save the records before creating a new segment
                this.currentOrderedSolutionNodes = OrderedVectorsFromSolution(this.pathfinder.solution);
                this.SolutionConstructedEvent.Invoke();
                //Debug.Log($"Saved {this.currentSerializedRoadNetwork.Count} road segments");
            } else {
                Debug.Log("No solution :(");
            }
            this.pathfinderIsRunning = false;
        }
        
        public void _RunPathfinder(Vector3 point1, Vector3 point2) {
            if (this.pathfinderIsRunning == false) {
                this.pathfinder.Reset();
                this.point1 = point1;
                this.point2 = point2;
                this.gameObjectPoint1.transform.position = point1;
                this.gameObjectPoint2.transform.position = point2;
                this.pathfinderCoroutine = StartCoroutine(RunPathfinder());
            } else {
                StopCoroutine(this.pathfinderCoroutine);
                this.pathfinder.Reset();
                this.pathfinderIsRunning = false;
                _RunPathfinder(point1, point2);
            }
        }

        /// <summary>
        /// Given a list of solution nodes, build a list of nodes that can be traversed in order
        /// to get form the start to the end of the path.
        /// </summary>
        /// <param name="solutionNodes"></param>
        /// <returns></returns>
        List<Vector3> OrderedVectorsFromSolution(List<Node> solutionNodes) {
            if (solutionNodes.Count == 1) return solutionNodes.Select((node, position)=>node.worldPosition).ToList();
            //list should be length two
            if (solutionNodes.Count != 2) return new List<Vector3>();
            Node root;
            List<Vector3> singleRoadList = new List<Vector3>();
            root = solutionNodes[0];
            while (!root.head){
                singleRoadList.Add(root.worldPosition);
                root = root.cost.parentNode;
            }
            singleRoadList.Add(root.worldPosition);
            //we started at the point where both search graphs intersected,
            //so after we traverse from root to head on the first leg, we
            //should flip the array so that the tail is at the end when we
            //start adding from the tail of the other solution node.
            singleRoadList.Reverse();
            //we skip the first node of this side of the solution nodes because
            //it lies in the same position as the final node in the current singleRoadList.
            root = solutionNodes[1].cost.parentNode; //skip the first one in the second solution list
            while (!root.head) {
                singleRoadList.Add(root.worldPosition);
                root = root.cost.parentNode;
            }
            singleRoadList.Add(root.worldPosition);
            return singleRoadList;
        }
    }
}
