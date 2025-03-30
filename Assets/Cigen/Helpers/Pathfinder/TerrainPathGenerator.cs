using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cigen;
using Cigen.ImageAnalyzing;
using Unity.Mathematics;
using UnityEditor.MPE;
using Cigen.Structs;

namespace GeneralPathfinder {
    public class TerrainPathGenerator : GeneralPathfinder {
        private int tunnelSegmentMaskValue;
        private int tunnelSegmentMaskResolution;
        private int tunnelSegmentSolveDist;
        private int nodeArraySubtractor = 1;
        private bool allowBothSidesConnection = false;
        //for logging the current min distance to the goal.
        private float min = float.MaxValue;

        //use instanced settings so we can configure different pathfinding presets
        //public override PathfinderSettings settings => base.settings;
        public new AnisotropicLeastCostPathSettings settings;

        protected override float GetNodeY(Node node) {
            if (node.head) return ImageAnalysis.TerrainHeightAt(node.position, settings);
            if (node.cost.isBridge || node.cost.isTunnel) {
                //float diff = node.goal.y - node.cost.parentNode.yValue;
                return node.cost.parentNode.yValue;// + (diff/4f);
            }
            return ImageAnalysis.TerrainHeightAt(node.position, settings);
        }
        
        /// <summary>
        /// Attempt to alter node1 Y value to match the new node Y value. Check to see if the three nodes composed of this node,
        /// the input node and the parent of this or the input node satisfies the slope and curvature requirements. 
        /// </summary>
        /// <param name="node2"></param>
        /// <returns></returns>
        public bool TryAlignNodes(Node node1, Node node2) {
            if (node1.head && node2.head) return false;
            //the new position for this node, if all the checks pass
            Vector3 prev;
            Vector3 curr;
            Vector3 end;
            //the previous node from which to
            if (node1.head && node2.cost.parentNode.head == false) {
                //align node2 to node1                
                prev = node2.cost.parentNode.cost.parentNode.worldPosition;
                curr = node2.cost.parentNode.worldPosition;
                end = node1.worldPosition;
                //Debug.Log("Checking segment is legal attempting to connect node2 to node1");
                //Debug.Log($"prev: {prev} | curr: {curr} | end: {end}");
                if (GlobalGoals.SegmentIsLegal(prev, curr, end, this.settings, node1.priority)) {
                    node2.SetHeight(node1.yValue);
                    return true;
                }
            }
            
            //Debug.Log("Checking segment is legal attempting to connect node1 to node2");
            if (node2.head && node1.cost.parentNode.head == false) {
                //Debug.Log("Node2 is head and node1 parent is not head");
                
                //use this node parent as the previous node for curvature calculations\
                //prev = _nodes[_costs[_nodes[_costs[node1.costIndex].parentNodeIndex].costIndex].parentNodeIndex].worldPosition;
                prev = node1.cost.parentNode.cost.parentNode.worldPosition;
                curr = node1.cost.parentNode.worldPosition;
                end = node2.worldPosition;
                //Debug.Log($"prev: {prev} | curr: {curr} | end: {end}");
                
            } else {
                //Debug.Log("Node2 is not head or node1 parent is head");
                //the slope is measured with curr and end so make sure that this is reflected in the assignments
                end = node1.cost.parentNode.worldPosition;
                curr = node2.worldPosition;
                prev = node2.cost.parentNode.worldPosition;
                //Debug.Log($"prev: {prev} | curr: {curr} | end: {end}");
            }

            if (GlobalGoals.SegmentIsLegal(prev, curr, end, this.settings, node1.priority)) {
                node1.SetHeight(node2.yValue);
                return true;
            } else {
                //Debug.Log("Segment not legal, cannot connect nodes");
                return false;
            }
        }

        private void DrawNode(Node n, float time = 5f) {
            if (n.head) return;
            //float h = Vector3.Dot(n.worldPosition, Vector3.one)/(3*settings.terrainHeightMap.width);
            Vector3 from = n.worldPosition;
            Vector3 to = n.cost.parentNode.worldPosition;
            Debug.DrawLine(from, to, UnityEngine.Random.ColorHSV(0, 1, 1, 1, 1, 1), time);
            //Debug.Log($"Drawing line from {from} to {to}");
            //Debug.DrawLine(n.worldPosition, _costs[n.costIndex].parentNode.worldPosition, Color.HSVToRGB(h, 0.8f, 0.8f), time);
        }

        public override IEnumerator Graph(Vector3 start, Vector3 destination, int pathPriority = 0)
        {
            this.min = float.PositiveInfinity;
            this.startPosition = new Vector3Int(Mathf.RoundToInt(start.x), 0, Mathf.RoundToInt(start.z));
            //start = this.startPosition + (Vector3.up * start.y);
            this.destination = new Vector3Int(Mathf.RoundToInt(destination.x), 0, Mathf.RoundToInt(destination.z));
            //destination = this.destination + (Vector3.up * destination.y);
            this.segmentMaskValue = settings.GetSegmentMaskValue(pathPriority);
            //Debug.Log($"Graph priority after finding segment mask: {pathPriority}");
            this.segmentMaskResolution = settings.GetSegmentMaskResolution(pathPriority);
            //Debug.Log($"Graph priority after finding segment resolution: {pathPriority}");
            this.tunnelSegmentMaskValue = settings.GetTunnelSegmentMaskValue(pathPriority);
            //Debug.Log($"Graph priority after finding tunnel mask: {pathPriority}");
            this.tunnelSegmentMaskResolution = settings.GetTunnelSegmentMaskResolution(pathPriority);
            this.segmentSolveDist = this.segmentMaskResolution * this.segmentMaskValue;
            this.tunnelSegmentSolveDist = this.tunnelSegmentMaskResolution * this.tunnelSegmentMaskValue;
            int minSegmentsToDest = 200;
            float distanceBetweenGoals = GlobalGoals.DistanceOverTerrain(this.startPosition, this.destination, settings, minSegmentsToDest);
            this.allowBothSidesConnection = settings.GetAllowBothSidesConnection(pathPriority);
            this.nodeArraySubtractor = settings.searchFromBothDirections ? 2 : 1;

            //Debug.Log($"Segment Solve: {this.segmentSolveDist} | Tunnel Solve: {this.tunnelSegmentSolveDist}");
            Cost startCost =  new Cost(this.startPosition, 0, distanceBetweenGoals, settings.heuristicCostCoefficient, settings.heuristicDistanceCoefficient);
            Node startNode = new Node(this.startPosition, startCost, this.destination + (Vector3.up*destination.y), pathPriority, true);
            startNode.SetHeight(GetNodeY(startNode));
            Cost endCost = new Cost(this.destination, 0, distanceBetweenGoals, settings.heuristicCostCoefficient, settings.heuristicDistanceCoefficient);
            Node endNode = new Node(this.destination, endCost, this.startPosition+(Vector3.up*start.y), pathPriority, true);
            endNode.SetHeight(GetNodeY(endNode));
            startOpenList.Enqueue(startNode, startCost);
            endOpenList.Enqueue(endNode, endCost);

//            Debug.Log(startOpenList.Count + endOpenList.Count);

            int maxBatchNum = 50;
            int batchNum = 0;
            while (this.shouldKeepGraphing && startOpenList.Count + endOpenList.Count > 0) {
                if (startOpenList.Count > 0) {
                    ProcessNode(startOpenList, startClosedList, endClosedList, this.destination);
                    if (this.isSolved) {
                        this.SolutionEvent.Invoke();
                        yield break;
                    }
                }
                if (settings.searchFromBothDirections && endOpenList.Count > 0) {
                    ProcessNode(endOpenList, endClosedList, startClosedList, this.startPosition);
                    if (this.isSolved) {
                        this.SolutionEvent.Invoke();
                        yield break;
                    }
                }
                if (batchNum > maxBatchNum) {
                    batchNum = 0;
                    yield return new WaitForEndOfFrame();
                }
                else batchNum++;
            }
        }

        protected override void AddNeighbours(Node node, PriorityQueue<Node, Cost> openList, Dictionary<Vector3Int, Node> closedList, Vector3Int goal)
        {

            if (settings.generateSurfacePaths) ProcessEndpoints(GlobalGoals.WeightedEndpointsFromNode(node, settings, node.priority), node, openList, closedList, goal, false, false);
            if (settings.generateTunnelPaths) ProcessEndpoints(GlobalGoals.WeightedTunnelEndpoints(node, this.settings, this.tunnelSegmentMaskResolution, this.tunnelSegmentMaskValue * this.tunnelSegmentMaskResolution), node, openList, closedList, goal, true, false);
            if (settings.generateBridgePaths) ProcessEndpoints(GlobalGoals.WeightedBridgeEndpoints(node, this.settings, this.tunnelSegmentMaskResolution, this.tunnelSegmentMaskValue * this.tunnelSegmentMaskResolution), node, openList, closedList, goal, false, true);
        }

        protected override void ProcessNode(PriorityQueue<Node, Cost> openList, Dictionary<Vector3Int, Node> closedList, Dictionary<Vector3Int, Node> oppositeClosedList, Vector3Int goal) {
            string s = "";
            foreach ((Node, Cost) e in openList.Peek(10)) {
               s += e.Item2.WeightedCost + ", ";
            }
            //Debug.Log(s);
            Node node = openList.Dequeue();
            if (closedList.ContainsKey(node.position) == false) {
                closedList.Add(node.position, node);
                DrawNode(node);
                float minDist =  (node.cost.isBridge || node.cost.isTunnel) ? this.tunnelSegmentSolveDist : this.segmentSolveDist;
                float dist = Vector3.Distance(node.worldPosition, goal);
                if (dist < this.min) {
                    this.min = dist;
                    //Debug.Log($"Min Dist: {minDist} | Min Val: {dist}");
                }

                if (dist <= 2*minDist) {
                    if (GlobalGoals.SegmentIsLegal(node.cost.parentNode.worldPosition, node.worldPosition, goal, this.settings, node.priority)) {
                        this.isSolved = true;
                        //get the final distance to the destination
                        float finalDist = GlobalGoals.DistanceOverTerrain(node.worldPosition, goal, settings);
                        Cost finalCost = new Cost(node, node.position, node.cost.totalHeuristicCost+finalDist, 0, settings.heuristicCostCoefficient, settings.heuristicDistanceCoefficient);
                        Node finalNode = new Node(goal, finalCost, node.goal, node.priority);
                        finalNode.SetHeight(GetNodeY(finalNode));
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
                        if (TryAlignNodes(node, node2)) {
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

        protected override void ProcessEndpoints(List<Tuple<float, Vector3Int>> endpoints, Node node, PriorityQueue<Node, Cost> openList, Dictionary<Vector3Int, Node> closedList, Vector3 goal, bool isTunnel = false, bool isBridge = false)  {
            foreach (Tuple<float, Vector3Int> endpoint in endpoints) {
                /*Debug.Log($"item 1: {endpoint.Item1}");
                Debug.Log($"item 2: {endpoint.Item2}");
                Debug.Log($"node: {node}");
                Debug.Log($"cost: {node.cost}");
                Debug.Log($"cc: {settings.heuristicCostCoefficient}");
                Debug.Log($"dc: {settings.heuristicDistanceCoefficient}");*/
                float newWeight = endpoint.Item1;
                //cost of the total distance travelled up to this point plus the evaluated cost of the new endpoint
                float heuristicCost = node.cost.totalHeuristicCost + newWeight;// + Vector3.Distance(node.position, goal); //use flat distance to value paths that are closer to a straight line
                //Debug.Log($"New weight: {newWeight} | Total Cost of Path: {heuristicCost}");
                if (heuristicCost == float.PositiveInfinity) continue;
                Cost newCost = new Cost(node, node.position, heuristicCost, Vector3.Distance(node.worldPosition, goal), settings.heuristicCostCoefficient, settings.heuristicDistanceCoefficient);
                newCost.isBridge = isBridge;
                newCost.isTunnel = isTunnel;
                //8if (newCost.parentNodeIndex < 0)
                Node newNode = new Node(endpoint.Item2, newCost, node.goal, node.priority);
                newNode.SetHeight(GetNodeY(newNode));
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