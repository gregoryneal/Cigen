using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System;
using OpenCvSharp;
using Cigen.Factories;
using Cigen.Structs;
using Cigen.Conversions;
using Cigen.ImageAnalyzing;
using Cigen.Helpers;

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

        private Texture2D contourTex;
        private Vector3 point1;
        private Vector3 point2;
        //private PathfinderV1 pathfinder;
        private PathfinderV2 pathfinder;
        private bool pathfinderIsRunning = false;
        private Coroutine pathfinderCoroutine;
        public GameObject gameObjectPoint1 { get; private set; }
        public GameObject gameObjectPoint2 { get; private set; }

        #endregion

        #region Unity Methods


        public void SetContourTexture(Texture2D contours) {
            this.contourTex = contours;
        }

        void Start() {
            startTime = DateTime.Now;
            CitySettings.instance.cigen = this;
            //this.pathfinder = new PathfinderV1();
            this.pathfinder = new PathfinderV2();
            //lineRenderer = gameObject.AddComponent<LineRenderer>();
            //lineRenderer.positionCount = 2;
            transform.position = Vector3.zero;

            this.gameObjectPoint1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            this.gameObjectPoint1.transform.localScale = Vector3.one * CitySettings.GetSegmentMaskResolution(0);
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
                float y1 = ImageAnalysis.TerrainHeightAt(point1);
                float y2 = ImageAnalysis.TerrainHeightAt(point2);
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
            int y = 0;
            Texture2D layerTex = settings.waterMap;
            while (this.contourTex == null) {
                if (y > 10) {
                    layerTex = settings.waterMap;
                    break;
                }
                y++;
                yield return new WaitForSeconds(1);
            }
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
            layer4.diffuseTexture = layerTex;
            layer4.tileSize = layer.tileSize;
            _terraindata.terrainLayers = new TerrainLayer[]{layer4};

            _terraindata.baseMapResolution = 4098;
        
            //Debug.Log($"Stats: Min: {min}, Max: {max}");
            //Apply settings to terrain
            terrain = Terrain.CreateTerrainGameObject(_terraindata);
            
            //attach a TerrainHit script object to test the pixel value at the clicked coordinates for visual reference
            //attach different textures to the terrain above to check that the values match where you click the terrain at.
            /*TerrainHit th = */terrain.AddComponent<TerrainHit>();
            
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
                //yield return this.pathfinder.GraphBothEnds(this.point1, this.point2, this.openList1, this.openList2, this.closedList1, this.closedList2, CitySettings.instance.highwayIdealSegmentLength);
                yield return this.pathfinder.GraphBothEnds(this.point1, this.point2);
            }
            if (this.pathfinder.isSolved) {
                //generate a segment for each node
                //we start generating from the final node so check if our current node is at the start before terminating
                Queue<Node> solutionNodes = new Queue<Node>(this.pathfinder.solution);
                Color roadColor = UnityEngine.Random.ColorHSV(0, 1, 0.9f, 1, 0.9f, 1);
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
                            // pp = new Vector3(cost.parentPosition.x, ImageAnalysis.TerrainHeightAt(cost.parentPosition), cost.parentPosition.z);
                            seg.Init(node.worldPosition, cost.parentNode.worldPosition, true, cost.isBridge, cost.isTunnel);
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

        #endregion

        #region Math methods

        #region Metric constraint methods

        #endregion

        #endregion
    }
}

#endregion