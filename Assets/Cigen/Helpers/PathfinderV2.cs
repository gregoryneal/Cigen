using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

namespace Cigen.Helpers {
    public class PathfinderV2 {
        PriorityQueue<Node, Cost> startOpenList = new PriorityQueue<Node, Cost>();
        Dictionary<Vector3Int, Node> startClosedList = new Dictionary<Vector3Int, Node>();
        PriorityQueue<Node, Cost> endOpenList = new PriorityQueue<Node, Cost>();
        Dictionary<Vector3Int, Node> endClosedList = new Dictionary<Vector3Int, Node>();
        
        protected bool shouldKeepGraphing = true;
        public bool isSolved { get; private set; }
        public List<Node> solution = new List<Node>();

        private int segmentMaskValue;

        private int segmentMaskResolution;
        private int tunnelSegmentMaskValue;
        private int tunnelSegmentMaskResolution;
        private Vector3Int startPosition;
        private Vector3Int destination;
        private bool allowSurfaceRoads = false;
        private bool allowBridge = false;
        private bool allowTunnel = false;
        private bool graphBothEnds = false;
        private bool allowBothSidesConnection = false;

        private float min = float.MaxValue;

        public void Reset() {
            this.isSolved = false;
            solution.Clear();
            this.shouldKeepGraphing = true;
            this.startOpenList.Clear();
            this.endOpenList.Clear();
            this.startClosedList.Clear();
            this.endClosedList.Clear();
        }

        public void StopGraph() {
            this.shouldKeepGraphing = false;
        }

        private void DrawNode(Node n, float time = 5f) {
            if (n.head) return;
            float h = Vector3.Dot(n.worldPosition, Vector3.one)/(3*CitySettings.instance.terrainHeightMap.width);
            Debug.DrawLine(n.worldPosition, n.cost.parentNode.worldPosition, Color.HSVToRGB(h, 0.8f, 0.8f), time);
        }

        public IEnumerator GraphBothEnds(Vector3 start, Vector3 destination, int roadPriority = 0)
        {
            this.min = float.PositiveInfinity;

            this.graphBothEnds = CitySettings.instance.searchFromBothDirections;
            this.allowSurfaceRoads = CitySettings.instance.generateSurfaceRoads;
            this.allowBridge = CitySettings.instance.generateBridges;
            this.allowTunnel = CitySettings.instance.generateTunnels;
            this.startPosition = new Vector3Int(Mathf.RoundToInt(start.x), 0, Mathf.RoundToInt(start.z));
            //start = this.startPosition + (Vector3.up * start.y);
            this.destination = new Vector3Int(Mathf.RoundToInt(destination.x), 0, Mathf.RoundToInt(destination.z));
            //destination = this.destination + (Vector3.up * destination.y);
            this.segmentMaskValue = CitySettings.GetSegmentMaskValue(roadPriority);
            this.segmentMaskResolution = CitySettings.GetSegmentMaskResolution(roadPriority);
            this.tunnelSegmentMaskValue = CitySettings.GetTunnelSegmentMaskValue(roadPriority);
            this.tunnelSegmentMaskResolution = CitySettings.GetTunnelSegmentMaskResolution(roadPriority);
            int minSegmentsToDest = 200;
            float distanceBetweenGoals = GlobalGoals.DistanceOverTerrain(this.startPosition, this.destination, minSegmentsToDest);
            this.allowBothSidesConnection = CitySettings.GetAllowBothSidesConnection(roadPriority);

            Node startNode = new Node(this.startPosition, new Cost(this.startPosition, 0, distanceBetweenGoals), roadPriority, true);
            Node endNode = new Node(this.destination, new Cost(this.destination, 0, distanceBetweenGoals), roadPriority, true);
            startOpenList.Enqueue(startNode, startNode.cost);
            endOpenList.Enqueue(endNode, endNode.cost);

//            Debug.Log(startOpenList.Count + endOpenList.Count);

            int maxBatchNum = 50;
            int batchNum = 0;
            while (this.shouldKeepGraphing && startOpenList.Count + endOpenList.Count > 0) {
                if (startOpenList.Count > 0) {
                    ProcessNode(startOpenList, startClosedList, endClosedList, this.destination);
                    if (this.isSolved) yield break;
                }
                if (this.graphBothEnds && endOpenList.Count > 0) {
                    ProcessNode(endOpenList, endClosedList, startClosedList, this.startPosition);
                    if (this.isSolved) yield break;
                }
                if (batchNum > maxBatchNum) {
                    batchNum = 0;
                    yield return new WaitForEndOfFrame();
                }
                else batchNum++;
            }
        }

        protected void AddNeighbours(Node node, PriorityQueue<Node, Cost> openList, Dictionary<Vector3Int, Node> closedList, Vector3Int goal)
        {
            if (this.allowSurfaceRoads) ProcessEndpoints(GlobalGoals.WeightedEndpointsFromNode(node, this.segmentMaskValue, this.segmentMaskResolution, node.priority, goal), node, openList, closedList, goal, false, false);
            if (this.allowTunnel) ProcessEndpoints(GlobalGoals.WeightedTunnelEndpoints(node, this.tunnelSegmentMaskResolution, this.tunnelSegmentMaskValue * this.tunnelSegmentMaskResolution), node, openList, closedList, goal, true, false);
            if (this.allowBridge) ProcessEndpoints(GlobalGoals.WeightedBridgeEndpoints(node, this.tunnelSegmentMaskResolution, this.tunnelSegmentMaskValue * this.tunnelSegmentMaskResolution), node, openList, closedList, goal, false, true);
        }

        private void ProcessNode(PriorityQueue<Node, Cost> openList, Dictionary<Vector3Int, Node> closedList, Dictionary<Vector3Int, Node> oppositeClosedList, Vector3Int goal) {
            /*string s = "";
            foreach ((Node, Cost) e in openList.Peek(10)) {
               s += e.Item2.WeightedCost + ", ";
            }
            Debug.Log(s);*/
            Node node = openList.Dequeue();
            if (closedList.ContainsKey(node.position) == false) {
                closedList.Add(node.position, node);
                DrawNode(node);
                float minDist =  (node.cost.isBridge || node.cost.isTunnel) ? this.tunnelSegmentMaskResolution : this.segmentMaskResolution;
                float dist = Vector3.Distance(node.worldPosition, goal);
                if (dist < this.min) {
                    this.min = dist;
                    Debug.Log(this.min);
                }

                if (dist <= 2*minDist) {
                    if (GlobalGoals.SegmentIsLegal(node.cost.parentNode.worldPosition, node.worldPosition, goal)) {
                        this.isSolved = true;
                        //get the final distance to the destination
                        float finalDist = GlobalGoals.DistanceOverTerrain(node.worldPosition, goal);
                        Cost finalCost = new Cost(node, node.cost.totalCostOfPath+finalDist, 0);
                        Node finalNode = new Node(goal, finalCost, node.priority);
                        this.solution.Add(finalNode);
                        closedList.TryAdd(finalNode.position, finalNode);
                        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        g.transform.position = node.worldPosition;
                        g.transform.localScale = Vector3.one * 3;
                        g.GetComponent<Renderer>().material.color = Color.white;
                        return;
                    } else {
                        Debug.Log("Solution found but segment not legal!");
                    }
                } else {
                }
                if (this.allowBothSidesConnection && oppositeClosedList.ContainsKey(node.position)) {
                    //we search the openList2 for the node at node1.position and set that as a solution node
                    //as well as node1
                    if (oppositeClosedList.TryGetValue(node.position, out Node node2)) {
                        //align node with node2 and ensure they comply with the curvature and slope requirements
                        if (node.TryAlignWithNode(node2)) {
                            this.isSolved = true;
                            this.solution.Add(node);
                            this.solution.Add(node2);

                            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            g.transform.position = node.worldPosition;
                            g.transform.localScale = Vector3.one * 3;
                            g.GetComponent<Renderer>().material.color = Color.black;
                            return;
                        }
                    } else {
                        Debug.Log("Node1 not found in OpenList2!? Why??");
                    }
                }
                AddNeighbours(node, openList, closedList, goal);
            }
        }

        private void ProcessEndpoints(List<Tuple<float, Vector3Int>> endpoints, Node node, PriorityQueue<Node, Cost> openList, Dictionary<Vector3Int, Node> closedList, Vector3 goal, bool isTunnel, bool isBridge) {
            foreach (Tuple<float, Vector3Int> endpoint in endpoints) {
                //Debug.Log(endpoint.Item2);
                //The endpoint with the terrain height added
                //Vector3 ePoint = new Vector3(endpoint.Item2.x, ImageAnalysis.TerrainHeightAt(endpoint.Item2.x, endpoint.Item2.z), endpoint.Item2.z);
                //cost of the total distance travelled up to this point plus the evaluated cost of the new endpoint
                float newWeight = endpoint.Item1;
                float distanceCost = node.cost.totalCostOfPath + newWeight;// + Vector3.Distance(node.position, goal); //use flat distance to value paths that are closer to a straight line
                if (distanceCost == float.PositiveInfinity) continue;

                //if this is 1 we are perpendicular to the vector pointing towards the goal
                //if this is 0 we are parallel with it, we wanna favor more paths that point towards the goal
                //so if the path is more perpendicular it will have a higher cost, so 
                //int minSegmentsToDestination = Mathf.FloorToInt(Vector3.Distance(endpoint.Item2, destination) / node.length);
                //Cost newCost = new Cost(node, distanceCost, distanceCost+Vector3.Distance(ePoint, goal));
                //Cost newCost = new Cost(node, distanceCost*perpendicularity, GlobalGoals.DistanceOverTerrain(node.worldPosition, goal));
                
                Cost newCost = new Cost(node, distanceCost, Vector3.Distance(node.worldPosition, goal));
                newCost.isBridge = isBridge;
                newCost.isTunnel = isTunnel;
                Node newNode = new Node(endpoint.Item2, newCost, node.priority);
                //insert new node into priority queue
                if (closedList.ContainsKey(newNode.position) == false) {
                    //we don't have this node in the node dictionary, lets add it
                    //Debug.Log("Adding new endpoint to open list");
                    openList.Enqueue(newNode, newCost);
                } else {
                    //Debug.Log("List already contained endpoint, checking if new cost is lower");
                    //we already have this node in the node dictionary, lets evaluate the costs and change the node heads if necessary
                    if (closedList.TryGetValue(newNode.position, out Node otherTail)) {
                        if (newCost < otherTail.cost) {
                            //set the new node as the predecessor of other tail
                            //this causes an infinite loop in the solution queue so lets leave it out for now
                            //otherTail.ChangeParent(node, distanceCost);
                            //Debug.Log($"New path cost changed from {otherTail.cost} to {newCost}");
                        }
                    }
                }
            }
        }
    }
}