using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System;
using OpenCvSharp;
using Cigen.Factories;
using Cigen.Structs;
using Cigen.Conversions;
using Cigen.ImageAnalyzing;
using System.Data;
using UnityEditor.Media;
using System.Linq;
using Cigen.Maths;
using System.IO;
using UnityEditor.Experimental.GraphView;

namespace Cigen
{
    /// <summary>
    /// This class contains the main algorithm for texture, road and building generation.
    /// </summary>
    public class CityGenerator : MonoBehaviour {


        #region Variables

        #region Public
        public CitySettings settings;
        public CityGenerator waitUntilBuilt;
        public bool isBuilt = false;
        public PopulationCenter[] populationCenters;
        #endregion

        #region Private
        private City city;
        private DateTime startTime;
        public Dictionary<Texture2D, Mat> CVMaterials { get; private set; }
        private GameObject terrain;
        private int terrainWidthPixels = 0;
        #endregion

        #region Cigen2 Simplified Method Variables
        
        //every iteration dequeue the oldest item and try to add another road to it.
        private Queue<RoadSegment> highwayEvaluationQueue = new Queue<RoadSegment>();
        private  Queue<RoadSegment> streetEvaluationQueue = new Queue<RoadSegment>();
        //road segments that have been passed through the local constraints function and modified to fit.
        public List<RoadSegment> highwayAcceptedSegments = new List<RoadSegment>();
        public List<RoadSegment> streetAcceptedSegments = new List<RoadSegment>();
        private int highwaySegmentLimit = 1000;
        private int streetSegmentLimit = 0;
        private Texture2D contourTex;
        private Vector3 point1;
        private Vector3 point2;
        private PathfinderV1 pathfinder;
        private bool pathfinderIsRunning = false;
        private Coroutine pathfinderCoroutine;
        private Maths.Math.PriorityQueue<Maths.Node> openList1 = new Maths.Math.PriorityQueue<Maths.Node>();
        private Maths.Math.PriorityQueue<Maths.Node> openList2 = new Maths.Math.PriorityQueue<Maths.Node>();
        private Dictionary<Vector3Int, Cost> closedList1 = new Dictionary<Vector3Int, Cost>();
        private Dictionary<Vector3Int, Cost> closedList2 = new Dictionary<Vector3Int, Cost>();
        public GameObject gameObjectPoint1 { get; private set; }
        public GameObject gameObjectPoint2 { get; private set; }

        //private LineRenderer lineRenderer;

        //These are the local constraints that we should apply in this order.
        private List<LocalConstraint> localConstraints = new List<LocalConstraint>(){
            //new OverWaterConstraint(),
            //new NearSegmentConstraint(),
            //new OutOfBoundsConstraint(),
        };

        #endregion

        #region Unity Methods


        public void SetContourTexture(Texture2D contours) {
            this.contourTex = contours;
        }

        void Start() {
            startTime = DateTime.Now;
            CitySettings.instance.cigen = this;
            this.pathfinder = new PathfinderV1();
            //lineRenderer = gameObject.AddComponent<LineRenderer>();
            //lineRenderer.positionCount = 2;
            transform.position = Vector3.zero;

            this.gameObjectPoint1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            this.gameObjectPoint1.transform.localScale = Vector3.one * 15;
            this.gameObjectPoint1.transform.position = this.point1;
            this.gameObjectPoint1.GetComponent<Renderer>().material.color = Color.green;
            this.gameObjectPoint2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            this.gameObjectPoint2.transform.localScale = Vector3.one * 15;
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

            //test all textures to ensure they imported properly and are the same size.
            if (ImageAnalysis.CheckAllTexturesArePowerOfTwo(bwTextures, true) == false) {
                Debug.LogError("Textures are not a power of two or not the same size. Check your input images!");
                return;
            }

            if (ImageAnalysis.CheckAllTexturesArePowerOfTwo(rgbTextures, true) == false) {            
                Debug.LogError("Textures are not a power of two or not the same size. Check your input images!");
                return;
            }
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
                StartCoroutine(BuildTerrain());
            }  

            //found a new city
            StartCoroutine(BuildCity(transform.position));

            //ProcessQueues();
        }

        void Update() {
            if (Input.GetKeyDown(KeyCode.KeypadEnter)) {
                Debug.Log("Starting over!");
                //start the generator over again
                _RunPathfinder(this.point1, this.point2);
            }
        }

        void OnDisable() {
            StopAllCoroutines();
        }

        #endregion

        public IEnumerator BuildTerrain() {
            Mat heightmap = settings.terrainHeightMapMat;

            //Create Terrain
            TerrainData _terraindata = new TerrainData();

            //Assign terrain settings
            _terraindata.heightmapResolution = heightmap.Width + 1;
            _terraindata.size = new Vector3(settings.textureToWorldSpace.x * terrainWidthPixels, settings.textureToWorldSpace.y * settings.terrainMaxHeight, settings.textureToWorldSpace.z * terrainWidthPixels);

            //double max = 0;
            //double min = 100;
            int maxBatch = 300;
            int batch = 0;
            float h;
            float [,] heights = new float[(int)_terraindata.size.x, (int)_terraindata.size.z];
            for (int i = 0; i < heightmap.Rows; i++) {
                for (int j = 0; j < heightmap.Cols; j++) {
                    //this is a value between 0 and 1
                    h = ImageAnalysis.NormalizedPointOnMat(i, j, heightmap);
                    //flip the rows on the terrain data to get the textures to line up when they are applied to the terrain.
                    //this means we also need to flip the rows when we convert world coordinates to texture coordinates.
                    heights[heightmap.Rows - 1 - i, j] = h;
                    /*Debug.Log(h);
                    if (h > max) {
                        max = h;
                    } else if (h < min) {
                        min = h;
                    }*/

                    if (batch >= maxBatch) {
                        batch = 0;
                        //yield return new WaitForEndOfFrame();
                    } else {
                        batch++;
                    }          
                }
            }
            _terraindata.SetHeights(0, 0, heights);
            TerrainLayer layer = new TerrainLayer();
            //layer2 contains the population density map to overlay onto the terrain for visual reference
            TerrainLayer layer2 = new TerrainLayer();
            TerrainLayer layer3 = new TerrainLayer();
            TerrainLayer layer4 = new TerrainLayer();
            layer2.diffuseTexture = settings.populationDensityMap;
            //apply the heightmap texture to the terrain for visual clarification
            layer.diffuseTexture = settings.terrainHeightMap;
            layer.tileSize = new Vector2(_terraindata.size.x, _terraindata.size.z);
            layer2.tileSize = layer.tileSize;
            layer3.diffuseTexture = settings.waterMap;
            layer3.tileSize = layer.tileSize;
            layer4.diffuseTexture = this.contourTex;
            layer4.tileSize = layer.tileSize;
            _terraindata.terrainLayers = new TerrainLayer[]{layer2};

            _terraindata.baseMapResolution = 4098;
        
            //Debug.Log($"Stats: Min: {min}, Max: {max}");
            //Apply settings to terrain
            terrain = Terrain.CreateTerrainGameObject(_terraindata);
            
            //attach a TerrainHit script object to test the pixel value at the clicked coordinates for visual reference
            //attach different textures to the terrain above to check that the values match where you click the terrain at.
            TerrainHit th = terrain.AddComponent<TerrainHit>();
            
            //shift it down so we can see our road generator a little easier
            //terrain.transform.position = terrain.transform.position - (UnityEngine.Vector3.up * 4);
            yield break;
        }

        public IEnumerator BuildCity(Vector3 initState) {
            if (waitUntilBuilt != null) {
                yield return new WaitUntil(()=>waitUntilBuilt.isBuilt);
            }
            
            if (settings == null) {
                Debug.LogError("No CigenSettings detected, drag and drop one into the inspector.");
                yield break;
            }

            //print("building city");
            city = CigenFactory.CreateCity(initState, settings);

            //analyze source images, define population, water, and nature regions
            //analyze population centers
            

            //generate arterial roads
            //yield return city.CreateHighways();
            /*
            //generate local roads
            while ((DateTime.Now - startTime).TotalSeconds < settings.maxRoadGenerationSeconds) {
                if (city.roads.Count >= settings.maxNumberOfRoads) {
                    break;
                }
                
                //print("Starting next iteration...");
                city.AddRandomIntersectionToRoadNetwork();
                yield return new WaitForEndOfFrame();
            }

            //Because new roads can make an intersection need rebuilding, lets
            //wait until we finish generating every road before we generate intersection meshes.
            foreach (Intersection i in city.intersections) {
                if ((DateTime.Now - startTime).TotalSeconds >= settings.maxIntersectionGenerationSeconds) {
                    break;
                }

                yield return StartCoroutine(i.BuildMesh());
            }

            foreach (Road r in city.roads) {
                if ((DateTime.Now - startTime).TotalSeconds >= settings.maxPlotGenerationSeconds) {
                    break;
                }

                r.ZonePlots();
                yield return new WaitForEndOfFrame();
            }

            print("Intersections: " + city.intersections.Count);
            print("Roads: " + city.roads.Count);
            print("Plots: " + city.plots.Count);
            this.isBuilt = true;
            yield break;
            */
        }

        private void ConnectNodes(Intersection parent, Intersection child) {
            CigenFactory.CreateRoad(parent, child);
            //DrawLineBetweenPoints(parent.Position, child.Position);

        }

        #region Cigen2 Simplified Methods

        

        IEnumerator RunPathfinder() {
            this.pathfinderIsRunning = true;
            while (!this.pathfinder.isSolved) {
                //Debug.Log($"Searched {closedList.Count} out of {openList.Count} left");
                //yield return this.pathfinder.Graph(this.point1, this.point2, this.openList1, this.closedList1, CitySettings.instance.highwayIdealSegmentLength);
                yield return this.pathfinder.GraphBothEnds(this.point1, this.point2, this.openList1, this.openList2, this.closedList1, this.closedList2, CitySettings.instance.highwayIdealSegmentLength);
            }
            if (this.pathfinder.isSolved) {
                //generate a segment for each node
                //we start generating from the final node so check if our current node is at the start before terminating
                Queue<Maths.Node> solutionNodes = new Queue<Maths.Node>(this.pathfinder.solution);
                Maths.Node node;
                while (solutionNodes.TryDequeue(out node)) {
                    try {
                        Debug.Log($"Node solution: {node.position} | isBridge: {node.cost.isBridge} | isTunnel: {node.cost.isTunnel}");
                        if (node.head) {
                            continue;
                        } else {
                            //create a road segment between the node and its parent, queue the parent   
                            RoadSegment seg = new GameObject().AddComponent<RoadSegment>();
                            Cost cost = node.cost;
                            seg.Init(node.position, cost.parentPosition, true, cost.isBridge, cost.isTunnel);
                            solutionNodes.Enqueue(cost.parentNode);
                            yield return new WaitForEndOfFrame();
                        }
                    } finally {}
                    yield return new WaitForEndOfFrame();
                }
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
                this.openList1.Clear();
                this.closedList1.Clear();
                this.openList2.Clear();
                this.closedList2.Clear();
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

        void ProcessQueues() {
            PopulationCenter pc1 = city.PopulationCenters[UnityEngine.Random.Range(0, city.PopulationCenters.Length)];                
            PopulationCenter pc2 = city.PopulationCenters[UnityEngine.Random.Range(0, city.PopulationCenters.Length)];
            while (pc1 == pc2 || pc1.connectedPCs.Contains(pc2)) {
                pc2 = city.PopulationCenters[UnityEngine.Random.Range(0, city.PopulationCenters.Length)];
            }
            //lets choose two positions within their bounds
            if (ImageAnalysis.RandomPointWithinPopulationCenter(out this.point1, pc1)) {
                if (ImageAnalysis.RandomPointWithinPopulationCenter(out this.point2, pc2)) {
                    //solve the path
                    _RunPathfinder(this.point1, this.point2);
                } else {
                    Debug.Log("Couldn't find an appropriate location within the second population center!");
                }
            } else {
                Debug.Log("Couldn't find an appropriate location within the first population center!");
            }
            /*
            while (streetAcceptedSegments.Count < streetSegmentLimit) {
                //create new axiom
                //the initial road segment, use texture maps to find suitable location,
                //you can even seed multiple starters based on the number of population centers or something.
                RoadSegment intersection = new GameObject().AddComponent<RoadSegment>();
                //this will place the intersection at a random valid location on the map
                intersection.InitAxiom(city, false);
                //draw axiom segment
                //lineRenderer.SetPosition(0, intersection.StartPosition);
                //lineRenderer.SetPosition(1, intersection.EndPosition);
                streetEvaluationQueue.Enqueue(intersection);

                yield return ProcessStreetQueue();
            }*/
        }

        IEnumerator ProcessHighwayQueue() {
            //int i = 0;
            while (highwayAcceptedSegments.Count < highwaySegmentLimit && highwayEvaluationQueue.Count > 0) {
                RoadSegment currentSegment = highwayEvaluationQueue.Dequeue();
                //always accept the first intersection because we will generate it on legal ground
                //we can rely on short circuiting to prevent calling LocalConstraints on the first intersection.
                bool accepted = LocalConstraints(currentSegment);
                if (accepted) {
                    highwayAcceptedSegments.Add(currentSegment);
                    //we may set StopGrowing on the segment in a local constraint so lets check that here, after we accept it as a segment
                    if (currentSegment.StopGrowing == false) {
                        foreach (RoadSegment segment in GeneratePossibleSegments(currentSegment)) {
                            highwayEvaluationQueue.Enqueue(segment);
                        }
                    }
                    //i++;

                    //render it in the linerenderer
                    //lineRenderer.positionCount++;
                    //lineRenderer.SetPosition(lineRenderer.positionCount-1, currentSegment.EndPosition);
                } else {
                    GameObject.Destroy(currentSegment.gameObject);
                }
                yield return new WaitForSeconds(.02f);
            }
            Debug.Log($"Highway queue empty! Number of segments created: {highwayAcceptedSegments.Count}");
            yield break;
        }
/*
        IEnumerator ProcessStreetQueue() {   
            int j = 0;         
            while (j < streetSegmentLimit && streetEvaluationQueue.Count > 0) {
                RoadSegment currentSegment = streetEvaluationQueue.Dequeue();
                bool accepted = LocalConstraints(currentSegment);
                //always accept the first intersection because we will generate it on legal ground
                if (accepted) {
                    streetAcceptedSegments.Add(currentSegment);
                    foreach (RoadSegment segment in GeneratePossibleSegments(currentSegment)) {
                        streetEvaluationQueue.Enqueue(segment);
                    }
                    j++;
                    //render it in the linerenderer
                    //lineRenderer.positionCount++;
                    //lineRenderer.SetPosition(lineRenderer.positionCount-1, currentSegment.EndPosition);
                } else {
                    GameObject.Destroy(currentSegment.gameObject);
                }
                yield return new WaitForEndOfFrame();
            }
            Debug.Log($"Street Queue empty! Number of segments created: {streetAcceptedSegments.Count}");
            yield break;
        }*/

        //This is where we branch off and find possible segments, implementing "global goals" as potential branches
        /// <summary>
        /// Naivelly branch off segments towards favored positions. 
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        public List<RoadSegment> GeneratePossibleSegments(RoadSegment segment) {
            List<RoadSegment> newSegments = new List<RoadSegment>();
            Vector3 direction = (segment.EndPosition - segment.StartPosition).normalized;
            RoadSegment potentialSegment1;
            RoadSegment potentialSegment2;
            return new List<RoadSegment>();

            //if the segment has a goal
            /*
            if (segment.Goal.from != Vector3.zero) {
                if (segment.IsAxiom) {
                    List<Vector3> branchEndpoints;
                    //create a new segment in that direction and add it to the list
                    RoadSegment newSegment = new GameObject().AddComponent<RoadSegment>();
                    float lowWeightDistanceScale = 5f;
                    float distance = segment.IdealSegmentLength;
                    //check if we are outside a population zone at the endposition
                    if (ImageAnalysis.PopulationDensityAt(segment.EndPosition) < CitySettings.instance.populationDensityCutoff) {
                        //branch out from the endposition to find new population centers
                        branchEndpoints = GlobalGoals.BranchLocationsFromPosition(segment.EndPosition, -direction, lowWeightDistanceScale * distance, segment);
                    } else {
                        branchEndpoints = GlobalGoals.BranchLocationsFromPosition(segment.EndPosition, -direction, distance, segment);
                    }

                    List<Tuple<float, Vector3>> ehh = GlobalGoals.BranchesWeightedByDistAndPopDens(segment.EndPosition, branchEndpoints);
                    Vector3 bestDirection = (ehh[ehh.Count-1].Item2 - segment.EndPosition).normalized;
                    //create a new segment in that direction and add it to the list
                    newSegment.Init(segment.EndPosition, segment.EndPosition + (bestDirection*distance), segment, true);
                    newSegments.Add(newSegment);
                }                 
                /*                    
                List<Tuple<float,Vector3>> endpoints = GlobalGoals.WeightedEndpointByGoal(segment.EndPosition, segment.GOAL, segment.SegmentDirection, segment.IdealSegmentLength, segment.MaxAngle);
                if (endpoints.Count > 0) {
                    //create the segment
                    RoadSegment newSeg = new GameObject().AddComponent<RoadSegment>();
                    newSeg.Init(segment.EndPosition, endpoints[0].Item2, segment);
                    newSegments.Add(newSeg);
                }*//*
                bool couldGenerateSegments = GlobalGoals.CreateWeightedSegment(segment.EndPosition, segment.SegmentDirection, segment.IdealSegmentLength, segment, out potentialSegment1);
                //generate segments outwards from the end position
                if (couldGenerateSegments) newSegments.Add(potentialSegment1);
                else GameObject.Destroy(potentialSegment1.gameObject);
            } else {
                bool couldGenerateSegments = GlobalGoals.CreateWeightedSegment(segment.EndPosition, direction, segment.IdealSegmentLength, segment, out potentialSegment1);
                //generate segments outwards from the end position
                if (couldGenerateSegments) newSegments.Add(potentialSegment1);
                else GameObject.Destroy(potentialSegment1.gameObject);

                //generate segments outwards from the start position (only really on the axiom segment)
                if (segment.IsAxiom) {
                    couldGenerateSegments = GlobalGoals.CreateWeightedSegment(segment.StartPosition, -1 * direction, segment.IdealSegmentLength, segment, out potentialSegment2);
                    if (couldGenerateSegments) newSegments.Add(potentialSegment2);
                    else GameObject.Destroy(potentialSegment2.gameObject);
                }
            }
            return newSegments.ToArray();*/
        }

        /// <summary>
        /// Check and modify parameters of the segment until it can find an acceptable location to generate the segment
        /// </summary>
        /// <param name="segment">The segment to modify.</param>
        /// <returns>True if the segment could be made to fit, false otherwise.</returns>
        public bool LocalConstraints(RoadSegment dirtySegment) {
            foreach (LocalConstraint constraint in this.localConstraints) {
                if (constraint.ApplyConstraint(ref dirtySegment) == false) {
                    //we failed to apply the constraint, terminate the segment
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Math methods

        #region Metric constraint methods

        #endregion

        #endregion
    }
}

#endregion