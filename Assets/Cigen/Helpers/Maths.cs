using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using OpenCvSharp;
using UnityEngine.Assertions.Comparers;
using Cigen.ImageAnalyzing;
using System;
using UnityEngine.UIElements;
using System.Collections;
using JetBrains.Annotations;

namespace Cigen.Maths
{

    public static class Math {
        public static void SortByXThenYFromReference(this IEnumerable<Vector2> points, Vector2 reference) {            
            points.OrderBy(v=>v.x-reference.x).ThenBy(v=>v.y-reference.y);
        }

        public static void Atan2RadialSortFromPositiveX(this IEnumerable<Vector2> points) {
            points.OrderBy(x=>Vector2.right.PositiveAngleTo(x)); 
        }

        public static void SortFromPositiveAngleToXAxis(this IEnumerable<VertexWithIndex> points, VertexWithIndex reference) {
            points.OrderBy(x=>reference.vertex.PositiveAngleTo(x.vertex)); 
        }

        public static void SortByXThenY(this IEnumerable<Vector2> points) {
        }

        /// <summary>
        /// Given two vectors, rotate the point about the pivot by the angle amount.
        /// </summary>
        /// <param name="point">The endpoint</param>
        /// <param name="pivot">The start/pivot point</param>
        /// <param name="angles">The angle to rotate by</param>
        /// <param name="ignoreY">Ignore the y axis? If true this will use the y value at pivot.</param>
        /// <returns></returns>
        public static Vector3 RotateAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles, bool ignoreY = false) {
            if (ignoreY) {
                point = new Vector3(point.x, pivot.y, point.z);
            }
            Vector3 dir = point - pivot; // get point direction relative to pivot
            dir = Quaternion.Euler(angles) * dir; // rotate it
            return dir + pivot; // return it
        }

        public static bool IsInCircle(Vector2 testPoint, Vector2 a, Vector2 b, Vector2 c)
        {
            float A = Vector2.Distance(b, c);
            float B = Vector2.Distance(a, c);
            float C = Vector2.Distance(a, b);
            float s = 0.5f * (A + B + C);
            float area = Mathf.Sqrt(s * (s - A) * (s - B) * (s - C)); // Heron's formula
            float radius = (A * B * C) / (4 * area);

            if (Vector2.Distance(testPoint, a) > radius)
                return false;
            if (Vector2.Distance(testPoint, b) > radius)
                return false;
            if (Vector2.Distance(testPoint, c) > radius)
                return false;

            return true;
        }

        public static float PositiveAngleTo(this Vector2 this_, Vector2 to) {
            Vector2 direction = to - this_;
            float angle = Mathf.Atan2(direction.y,  direction.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;

            /* Thought this was needed at one point, it may still be useful just not yet...
            float closeEnough = 0.001f;
            if (Mathf.Abs(angle) <= closeEnough) angle = 0f;
            if (Mathf.Abs(angle - 90f) <= closeEnough) angle = 90f;
            if (Mathf.Abs(angle - 180f) <= closeEnough) angle = 180f;
            if (Mathf.Abs(angle - 270f) <= closeEnough) angle = 270f;
            if (Mathf.Abs(angle - 360f) <= closeEnough) angle = 0f;
            */
            return angle;
        }

        public static bool IsPowerOfTwo(int value) {
            bool b = (value != 0) && ((value & (value - 1)) == 0);
            //Debug.Log($"Is {value} a power of two -> {b}");
            return b;
        }

        /// <summary>
        /// Given an array of float values, this will select one of them based on
        /// a random weighted selection. It will find the maximum values within some 
        /// epsilon then randomly choose an index that contains one of the max values.
        /// </summary>
        /// <param name="values">The values to choose between.</param>
        /// <returns>A weighted random selection of a value from the values list.</returns>
        public static int WeightedRandomSelection(float[] values) {
            float epsilon = 0.001f;
            //String s = String.Join<float>(", ", vals);
            //Debug.Log($"Values List: [{s}]");

            float maxValue = Mathf.Max(values);

            //Debug.Log($"Max Value {maxValue}");
            //this will store indexes of max values from the vals array
            //we use them to randomly pick one of the indexes that has a max
            //value, then use that index as a reference to which highwaytype 
            //is dominant.
            List<int> indexList = new List<int>();

            for (int i = 0; i < 4; i++) {
                double val = values[i];
                //if this value is close enough to the max value it becomes a candidate for selection
                if (maxValue - val < epsilon) {
                    indexList.Add(i);
                }
            }
            //s = String.Join<int>(", ", indexList);
            //Debug.Log($"Index List: [{s}]");

            //now we need to select from the index list randomly, but each value is weighted by 
            //its proportion to the sum of all candidate values

            //create an array of max values
            List<float> maxes = new List<float>();
            List<float> weights = new List<float>();
            foreach (int j in indexList) {
                maxes.Add(values[j]);
            }

            //s = String.Join<float>(", ", maxes);
            //Debug.Log($"Maxes List: [{s}]");

            float sum = maxes.Sum();
            //Debug.Log($"Sum of maxes: {sum}");

            //Yes, Lists guarantee ordering so indexes are fine to use!
            //I had no idea until 02/15/2025 at 10:42AM here in Fort Collins, Colorado!
            foreach (int k in indexList) {
                //create the weighted CDF of the max value list.
                //since they are normalized all the values should add up to 1.
                weights.Add(values[k]/sum);
            }

            //s = String.Join<float>(", ", weights);
            //Debug.Log($"Weights List: [{s}]");

            //choose a random number between 0 and 1.
            float r = UnityEngine.Random.value;
            int chosenIndex = -1;
            foreach (float f in weights) {
                r -= f;
                if (r <= 0) 
                    chosenIndex = indexList[weights.IndexOf(f)];
            }

            //Debug.Log($"Chosen Index: [{chosenIndex}]");

            if (chosenIndex == -1) {
                //we couldn't get indices, maybe that part of the map wasn't colored?
                //Either way we just pick a random value.
                if (indexList.Count == 0) {
                    //random value between 0 and 3 inclusive.
                    chosenIndex = UnityEngine.Random.Range(0, 4);
                } else {
                    //choose a random index from the list.
                    chosenIndex = UnityEngine.Random.Range(0, indexList.Count);
                }
            }

            //Debug.Log($"Chosen Index: [{chosenIndex}]");

            return chosenIndex;
        }

        public static Vector3 ProjectedPointOnEllipse(Vector3 point, float majorAxis, float minorAxis) {
            float theta = UnityEngine.Mathf.Atan2(point.z, point.x);
            float a = majorAxis;
            float b = minorAxis;
            float c = a * b;
            float d = Mathf.Cos(theta);
            float e = Mathf.Sin(theta);
            float f = (b * b * d * d) + (a * a * e * e);
            float g = Mathf.Sqrt(f);
            float k = c / g;

            return new Vector3(k * d, point.y, k * e);
        }

        /// <summary>
        /// Get a random vector3 contained within a rectangle centered on center, with x width given as size.x and z width given as size.z.
        /// Ignores the y axis entirely.
        /// </summary>
        /// <param name="center">The center point of the rectangle</param>
        /// <param name="size">The extents of the rectangle.</param>
        /// <returns>A point within the rectangle.</returns>
        public static Vector3 RandomPointInRectangle(Vector3 center, Vector3 size) {
            float x = UnityEngine.Random.Range(-1f * (size.x/2), size.x/2) + center.x;
            float z = UnityEngine.Random.Range(-1f * (size.z/2), size.z/2) + center.z;
            return new Vector3(x, center.y, z);
        }

        /// <summary>
        /// Find out if the line between two pairs of vector3s intersect. 
        /// </summary>
        /// <param name="intersection">The intersection point if one exists. If not it will be set to Vector3.zero</param>
        /// <param name="startPoint1">The start position of the first line.</param>
        /// <param name="startDir1">The direction of the first line, not normalized.</param>
        /// <param name="startPoint2">The start position of the second line.</param>
        /// <param name="startDir2">The direction of the second line, not normalized.</param>
        /// <param name="ignoreY">Should we ignore the Y value when finding intersections? If set to true then the intersection point will have its Y value set to the sampled terrain height instead of the intersection point.</param>
        /// <returns></returns>
        public static bool LineLineIntersection(out Vector3 intersection, Vector3 startPoint1, Vector3 startDir1, Vector3 startPoint2, Vector3 startDir2, bool ignoreY = true){
            if (ignoreY) {
                startPoint1 = new Vector3(startPoint1.x, 0, startPoint1.z);
                startDir1 = new Vector3(startDir1.x, 0, startDir1.z);
                startPoint2 = new Vector3(startPoint2.x, 0, startPoint2.z);
                startDir2 = new Vector3(startDir2.x, 0, startDir2.z);
            }
            Vector3 lineVec3 = startPoint2 - startPoint1;
            Vector3 crossVec1and2 = Vector3.Cross(startDir1, startDir2);
            Vector3 crossVec3and2 = Vector3.Cross(lineVec3, startDir2);

            float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

            //is coplanar, and not parallel
            if(Mathf.Abs(planarFactor) < 0.0001f && crossVec1and2.sqrMagnitude > 0.0001f) {
                float s = Vector3.Dot(crossVec3and2, crossVec1and2) 
                        / crossVec1and2.sqrMagnitude;
                intersection = startPoint1 + (startDir1 * s);

                if (ignoreY) {
                    intersection = new Vector3(intersection.x, ImageAnalysis.TerrainHeightAt(intersection.x, intersection.z), intersection.z);
                }
                //Debug.Log("Line intersection");
                return true;
            }
            //Debug.Log("No line intersection");
            intersection = Vector3.zero;
            return false;
        }

        public static bool LineRectangleIntersection(out Vector3 intersection, Vector3 lineStart, Vector3 lineEnd, Vector3 boxPosition, Vector3 boxSize) {
            //do 4 line intersections
            //top right
            Vector3 direction = lineEnd - lineStart;
            Vector3 corner1 = boxPosition + new Vector3(boxSize.x/2, 0, boxSize.z/2);
            //top left
            Vector3 corner2 = boxPosition + new Vector3(-boxSize.x/2, 0, boxSize.z/2);
            //bottom right
            Vector3 corner3 = boxPosition + new Vector3(boxSize.x/2, 0, -boxSize.z/2);
            //bottom left
            Vector3 corner4 = boxPosition + new Vector3(-boxSize.x/2, 0, -boxSize.z/2);
            if (LineLineIntersection(out intersection, lineStart, direction, corner1, corner2-corner1) ||
                LineLineIntersection(out intersection, lineStart, direction, corner1, corner3-corner1) ||
                LineLineIntersection(out intersection, lineStart, direction, corner3, corner4-corner3) ||
                LineLineIntersection(out intersection, lineStart, direction, corner4, corner1-corner4)) {
                Debug.Log($"Intersection! {intersection}");
                return true;
                
            }            
            Debug.Log($"No intersection!");
            intersection = Vector3.zero;
            return false;
        }

        public static bool IsPointInRectangle(Vector3 point, Vector3 rectPosition, Vector3 rectSize) {
            if (point.x < rectPosition.x - (rectSize.x/2) ||
                point.x > rectPosition.x + (rectSize.x/2) ||
                point.z < rectPosition.z - (rectSize.z/2) ||
                point.z > rectPosition.z + (rectSize.z/2)) {
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Get the greatest common divisor, the largest number that evenly divides both a and b. 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>The greatest common divisor.</returns>
        public static uint GreatestCommonDivisor(uint a, uint b) {
            while (a != 0 && b != 0) {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }
            return a | b;
        }

        /// <summary>
        /// https://en.wikipedia.org/wiki/Degree_of_curvature
        /// In our case the degreeOfCurvature is the max road angle, the cord length is the segment length. 
        /// We use it to determine the curvature of the road to weight highly curved roads way higher. 
        /// </summary>
        /// <param name="cordLength">The length of the circular cord.</param>
        /// <param name="degreeOfCurvature">The degree of curvature of the circle.</param>
        /// <returns>The angle of the circle made by the cord length and degree of curvature.</returns>
        public static float RadiusFromCordAndDegreeOfCurvature(float cordLength, float degreeOfCurvature) {
            return 2*Mathf.Sin(degreeOfCurvature/2)/cordLength;
        }

        

        public class PriorityQueue<T> where T : Node {
            private List<T> items = new List<T>();
            public int Count { get { return items.Count; } }
            public void Clear() { items.Clear(); }
            public void Insert(T item) {
                int i = items.Count;
                items.Add(item);
                while (i > 0 && items[(i - 1) / 2].CompareTo(item) > 0) {
                    items[i] = items[(i - 1) / 2];
                    i = (i - 1) / 2;
                }
                items[i] = item;
            }
            public T Peek() { return items[0]; }
            public T RemoveRoot() {
                T firstItem = items[0];
                T tempItem = items[items.Count - 1];
                items.RemoveAt(items.Count - 1);
                if (items.Count > 0) {
                    int i = 0;
                    while (i < items.Count / 2) {
                        int j = (2 * i) + 1;
                        if ((j < items.Count - 1) && (items[j].CompareTo(items[j + 1]) > 0)) ++j;
                        if (items[j].CompareTo(tempItem) >= 0) break;
                        items[i] = items[j];
                        i = j;
                    }
                    items[i] = tempItem;
                }
                return firstItem;
            }

            public override string ToString() {
                return ToString(items.Count);
            }
            
            public string ToString(int maxIndex) {
                if (maxIndex > items.Count) maxIndex = items.Count;
                string s = "";
                for (int i = 0; i < maxIndex; i++) {
                    s += items[i].ToString() + ", ";
                }
                return s;
            }

            public bool GetValue(Vector3Int position, out Node node) {
                foreach (T item in items) {
                    if (item.position == position) {
                        node = item;
                        return true;
                    }
                }
                node = this.Peek();
                return false;
            }
        }
    }

    public abstract class AStar<TKey, TValue> where TValue : Cost {
        protected IEnumerator Graph(Node start, Math.PriorityQueue<Node> openList, Dictionary<Vector3Int, Cost> closedList) {
            openList.Insert(start);
            while (openList.Count > 0) {
                Debug.Log($"Removing root from list: {openList.ToString(10)}");
                Node node = openList.RemoveRoot();                
                Debug.Log($"Removed item: {node.position}");
                if (closedList.ContainsKey(node.position)) {
                    Debug.Log($"Already searched node: {node.position}");
                    continue;
                }
                closedList.Add(node.position, node.cost);
                if (IsDestination(node.position)) {
                    Debug.Log($"Found destination node! {node.position}");
                    yield break;
                }
                Debug.Log($"Adding neighbors: {node.position}");
                AddNeighbours(node, openList);
                yield return new WaitForEndOfFrame();
            }
        }
        protected abstract void AddNeighbours(Node node, Math.PriorityQueue<Node> openList, bool toDestination = true);
        protected abstract bool IsDestination(Vector3Int position);
        protected abstract bool IsStartPosition(Vector3Int position);
    }

    /// <summary>
    /// Attempt to pathfind between two points in world space using weighted endpoints by goal function
    /// </summary>
    public class PathfinderV1 : AStar<Vector3Int, Cost> {
        private Vector3Int destination;
        private Vector3Int startPosition;
        private float idealSegmentLength;
        private bool shouldKeepGraphing = true;
        public bool isSolved { get; private set; }
        public List<Node> solution = new List<Node>();

        public void Reset() {
            this.isSolved = false;
            solution.Clear();
            this.shouldKeepGraphing = true;
        }

        public void StopGraph() {
            this.shouldKeepGraphing = false;
        }

        //try to solve the path from both ends simultaneously
        public IEnumerator GraphBothEnds(Vector3 start, Vector3 destination, Math.PriorityQueue<Node> openList1, Math.PriorityQueue<Node> openList2, Dictionary<Vector3Int, Cost> closedList1, Dictionary<Vector3Int, Cost> closedList2, float segmentLength) {
            this.startPosition = Vector3Int.RoundToInt(start);
            this.destination = Vector3Int.RoundToInt(destination);
            this.idealSegmentLength = segmentLength;
            int minSegmentsToDest = Mathf.FloorToInt(Vector3Int.Distance(this.startPosition, this.destination)/this.idealSegmentLength);
            float initCost = GlobalGoals.DistanceCost(this.startPosition, this.destination, minSegmentsToDest);
            //list 1 starts at the start position and works its way towards the end position
            openList1.Insert(new Node(this.startPosition, new Cost(this.startPosition, 0, initCost), segmentLength, true));
            //list 2 starts at the end position and works its way towards the start position
            openList2.Insert(new Node(this.destination, new Cost(this.destination, 0, initCost), segmentLength, true));
            float t = 0;
            float maxT = 1f;
            int maxBatchNum = 10;
            int batchNum = 1;
            while (openList1.Count + openList2.Count > 0 && this.shouldKeepGraphing) {
                //draw the segments again
                if (t > maxT) {
                    foreach (KeyValuePair<Vector3Int, Cost> kvp in closedList1) {
                        float h = 1f*(kvp.Key.x+kvp.Key.y+kvp.Key.z)/(3*CitySettings.instance.terrainHeightMap.width);
                        Debug.DrawLine(kvp.Key, kvp.Value.parentPosition, Color.HSVToRGB(h, 0.8f, 0.8f), maxT);
                    }
                    foreach (KeyValuePair<Vector3Int, Cost> kvp in closedList2) {
                        float h = 1f*(kvp.Key.x+kvp.Key.y+kvp.Key.z)/(3*CitySettings.instance.terrainHeightMap.width);
                        Debug.DrawLine(kvp.Key, kvp.Value.parentPosition, Color.HSVToRGB(h, 0.8f, 0.8f), maxT);
                    }
                    t = 0;
                }

                if (openList1.Count > 0) {
                    Node node1 = openList1.RemoveRoot();
                    if (closedList1.ContainsKey(node1.position) == false) {
                        closedList1.Add(node1.position, node1.cost);
                        
                        if (IsDestination(node1.position)) {
                            this.isSolved = true;
                            //get the final distance to the destination
                            float finalDist = GlobalGoals.DistanceCost(node1.position, this.destination);
                            Cost finalCost = new Cost(node1, node1.cost.distanceTravelled+finalDist, finalDist);
                            Node finalNode = new Node(this.destination, finalCost, node1.length);
                            this.solution.Add(finalNode);
                            closedList1.TryAdd(this.destination, finalCost);

                            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            g.transform.position = node1.position;
                            g.transform.localScale = Vector3.one * 15;
                            g.GetComponent<Renderer>().material.color = Color.cyan;
                            yield break;
                        }

                        if (closedList2.ContainsKey(node1.position)) {
                            this.isSolved = true;
                            //we search the openList2 for the node at node1.position and set that as a solution node
                            //as well as node1
                            this.solution.Add(node1);
                            Node n;
                            if (openList2.GetValue(node1.position, out n)) {
                                this.solution.Add(n);
                            } else {
                                Debug.Log("Node1 not found in OpenList2!? Why??");
                            }
                            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            g.transform.position = node1.position;
                            g.transform.localScale = Vector3.one * 15;
                            g.GetComponent<Renderer>().material.color = Color.blue;
                            yield break;
                        }
                        AddNeighbours(node1, openList1);
                    }
                }

                if (openList2.Count > 0) {
                    Node node2 = openList2.RemoveRoot();
                    if (closedList2.ContainsKey(node2.position) == false) {
                        closedList2.Add(node2.position, node2.cost);
                        if (IsStartPosition(node2.position)) {
                            this.isSolved = true;
                            //Debug.Log($"Solved! {node.position}");
                            float finalDist = GlobalGoals.DistanceCost(node2.position, this.startPosition);
                            Cost finalCost = new Cost(node2, node2.cost.distanceTravelled+finalDist, finalDist);
                            Node finalNode = new Node(this.startPosition, finalCost, node2.length);
                            this.solution.Add(finalNode);
                            closedList2.TryAdd(this.startPosition, finalCost);
                            //closedList.Add(this.destination, finalCost);
                            //Debug.Log($"Found destination node! {node.position}");
                            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            g.transform.position = node2.position;
                            g.transform.localScale = Vector3.one * 15;
                            g.GetComponent<Renderer>().material.color = Color.black;
                            yield break;
                        }

                        if (closedList1.ContainsKey(node2.position)) {
                            this.isSolved = true;
                            this.solution.Add(node2);
                            Node n;
                            if (openList1.GetValue(node2.position, out n)) {
                                this.solution.Add(n);
                            } else {
                                Debug.Log("Node2 not found in OpenList1!? Why??");
                            }
                            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            g.transform.position = node2.position;
                            g.transform.localScale = Vector3.one * 15;
                            g.GetComponent<Renderer>().material.color = Color.white;
                            yield break;
                        }                        
                        AddNeighbours(node2, openList2, false);
                    }
                }

                t += Time.deltaTime;
                if (batchNum >= maxBatchNum) {
                    batchNum = 1;
                    yield return new WaitForEndOfFrame();
                }
                else batchNum++;
            }
        }

        public IEnumerator Graph(Vector3 startPosition, Vector3 destination, Math.PriorityQueue<Node> openList, Dictionary<Vector3Int, Cost> closedList, float segmentLength) {
            this.startPosition = Vector3Int.RoundToInt(startPosition);
            this.destination = Vector3Int.RoundToInt(destination);
            this.idealSegmentLength = segmentLength;
            int minSegmentsToDest = Mathf.FloorToInt(Vector3Int.Distance(this.startPosition, this.destination)/segmentLength);
            openList.Insert(new Node(this.startPosition, new Cost(this.startPosition, 0, GlobalGoals.DistanceCost(this.startPosition, this.destination, minSegmentsToDest)), segmentLength, true));
            float t = 0;
            float maxT = 1f;
            int maxBatchNum = 10;
            int batchNum = 1;
            while (openList.Count > 0) {
                if (this.shouldKeepGraphing == false) yield break;
                if (t > maxT) {
                    foreach (KeyValuePair<Vector3Int, Cost> kvp in closedList) {
                        float h = 1f*(kvp.Key.x+kvp.Key.y+kvp.Key.z)/(3*CitySettings.instance.terrainHeightMap.width);
                        Debug.DrawLine(kvp.Key, kvp.Value.parentPosition, Color.HSVToRGB(h, 0.8f, 0.8f), maxT);
                    }
                    t = 0;
                }

                //Debug.Log($"Removing root from list: {openList.ToString(10)}");
                Node node = openList.RemoveRoot();                
                //Debug.Log($"Removed item: {node.position}");
                if (closedList.ContainsKey(node.position)) {
                    //Debug.Log($"Already searched node: {node.position}");
                    continue;
                }
                closedList.Add(node.position, node.cost);
                if (IsDestination(node.position)) {
                    //Debug.Log($"Solved! {node.position}");
                    this.isSolved = true;
                    float finalDist = GlobalGoals.DistanceCost(node.position, destination);
                    Cost finalCost = new Cost(node, node.cost.distanceTravelled+finalDist, finalDist);
                    this.solution.Add(new Maths.Node(this.destination, finalCost, node.length));
                    closedList.TryAdd(this.destination, finalCost);
                    //closedList.Add(this.destination, finalCost);
                    //Debug.Log($"Found destination node! {node.position}");
                    yield break;
                }
                //Debug.Log($"Adding neighbors: {node.position}");
                AddNeighbours(node, openList);

                t += Time.deltaTime;
                if (batchNum >= maxBatchNum) {
                    batchNum = 1;
                    yield return new WaitForEndOfFrame();
                }
                else batchNum++;
            }

            //yield return Graph(new Node(startPosition, new Cost(startPosition, 0, Vector3.Distance(startPosition, destination)), segmentLength), openList, closedList);
            
        }

        /// <summary>
        /// Look around the current node for neighbors, we can use our branchposition functions
        /// </summary>
        /// <param name="node"></param>
        /// <param name="openList"></param>
        protected override void AddNeighbours(Node node, Math.PriorityQueue<Node> openList, bool toDestination = true) {
            //use a goal so we can change the cost weight direction
            Vector3 goal = toDestination ? this.destination : this.startPosition;
            Vector3 direction = Vector3.forward;
            //Don't change this we can restrict the path angle through constraints later on, we wanna be able to branch back in any direction
            //to find a lower cost route if need be. 
            float maxAngle = 180;
            //Debug.Log(node.length);
            
            /*List<Tuple<float, Vector3>> endpoints = GlobalGoals.WeightedEndpointsByTerrainHeight(node.position, direction, node.length, maxAngle, 25);
            foreach (Tuple<float, Vector3> endpoint in endpoints) {
                Vector3Int vi = Vector3Int.RoundToInt(endpoint.Item2);
                //cost of the total distance travelled up to this point
                float distanceCost = node.cost.distanceTravelled + Vector3.Distance(node.position, endpoint.Item2) + endpoint.Item1;
                //int minSegmentsToDestination = Mathf.FloorToInt(Vector3.Distance(endpoint.Item2, destination) / node.length);
                Cost newCost = new Cost(node, distanceCost, distanceCost+Vector3.Distance(endpoint.Item2, goal));
                Node newNode = new Node(vi, newCost, node.length);
                //insert new node into priority queue
                openList.Insert(newNode);
            }*/
            List<Tuple<float, Vector3Int>> endpoints = GlobalGoals.WeightedEndpointsByTerrainHeight(node.position, 5);
            foreach (Tuple<float, Vector3Int> endpoint in endpoints) {
                //cost of the total distance travelled up to this point
                float distanceCost = node.cost.distanceTravelled + Vector3.Distance(node.position, endpoint.Item2) + endpoint.Item1;
                //int minSegmentsToDestination = Mathf.FloorToInt(Vector3.Distance(endpoint.Item2, destination) / node.length);
                Cost newCost = new Cost(node, distanceCost, distanceCost+Vector3.Distance(endpoint.Item2, goal));
                Node newNode = new Node(endpoint.Item2, newCost, node.length);
                //insert new node into priority queue
                openList.Insert(newNode);
            }
            /*
            List<Tuple<float, Vector3>> tunnelEndpoints = GlobalGoals.WeightedTunnelEndpoints(node.position, CitySettings.instance.minHighwayTunnelLength, CitySettings.instance.maxHighwayTunnelLength);
            foreach (Tuple<float, Vector3> endpoint in tunnelEndpoints) {
                Vector3Int vi = Vector3Int.RoundToInt(endpoint.Item2);
                //cost of the total distance travelled up to this point
                float distanceCost = node.cost.distanceTravelled + Vector3.Distance(node.position, endpoint.Item2);
                //int minSegmentsToDestination = Mathf.FloorToInt(Vector3.Distance(endpoint.Item2, destination) / node.length);
                Cost newCost = new Cost(node.position, distanceCost, distanceCost+Vector3.Distance(endpoint.Item2, goal));
                newCost.isTunnel = true;
                Node newNode = new Node(vi, newCost, node.length);
                //insert new node into priority queue
                openList.Insert(newNode);
            }
                
            List<Tuple<float, Vector3>> bridgeEndpoints = GlobalGoals.WeightedBridgeEndpoints(node.position, CitySettings.instance.minHighwayBridgeLength, CitySettings.instance.maxHighwayBridgeLength);
            foreach (Tuple<float, Vector3> endpoint in bridgeEndpoints) {
                Vector3Int vi = Vector3Int.RoundToInt(endpoint.Item2);
                //cost of the total distance travelled up to this point
                float distanceCost = node.cost.distanceTravelled + Vector3.Distance(node.position, endpoint.Item2);
                //int minSegmentsToDestination = Mathf.FloorToInt(Vector3.Distance(endpoint.Item2, destination) / node.length);
                Cost newCost = new Cost(node.position, distanceCost, distanceCost+Vector3.Distance(endpoint.Item2, goal));
                newCost.isBridge = true;
                Node newNode = new Node(vi, newCost, node.length);
                //insert new node into priority queue
                openList.Insert(newNode);
            }*/
            //Debug.Log($"added {endpoints.Count} nodes");
        }

        protected override bool IsDestination(Vector3Int position) {            
            return Vector3Int.Distance(position, this.destination) < this.idealSegmentLength;
        }

        protected override bool IsStartPosition(Vector3Int position) {
            return Vector3Int.Distance(position, this.startPosition) < this.idealSegmentLength;
        }
    }    

    public class Cost : IComparable<Cost> {
        /// <summary>
        /// The parent position, for tracing back the path to the start.
        /// </summary>
        public readonly Vector3Int parentPosition;
        public readonly Node parentNode;
        /// <summary>
        /// The total distance travelled up to this point.
        /// </summary>
        public readonly float distanceTravelled;
        /// <summary>
        /// The cost value to the final destination from this point.
        /// </summary>
        public readonly float totalCost;

        public bool isTunnel = false;
        public bool isBridge = false;
        /// <summary>
        /// Create a new cost object
        /// </summary>
        /// <param name="parentPosition">The parent position, for tracing back the path to the start.</param>
        /// <param name="distanceTravelled">The total distance travelled up to this point.</param>
        /// <param name="totalCost">The cost value to the final destination from this point.</param>
        public Cost(Vector3Int parentPosition, float distanceTravelled, float totalCost) {
            this.parentPosition = parentPosition;
            this.distanceTravelled = distanceTravelled;
            this.totalCost = totalCost;
        }

        public Cost(Node parentNode, float distanceTravelled, float totalCost) {
            this.parentNode = parentNode;
            this.parentPosition = parentNode.position;
            this.distanceTravelled = distanceTravelled;
            this.totalCost = totalCost;
        }
        public int CompareTo(Cost other) {
            return this.totalCost.CompareTo(other.totalCost);
        }
    }

    public class Node : IComparable<Node> {
            public Vector3Int position;
            public Cost cost;
            public float length;
            public bool head = false;
            public Node(Vector3Int position, Cost cost, float length, bool head = false) {
                this.position = position;
                this.cost = cost;
                this.length = length;
                this.head = head;
            }
            public int CompareTo(Node other) { 
                return cost.CompareTo(other.cost); 
            }

            public override string ToString()
            {
                return position.ToString();
            }

            public override bool Equals(object obj)
            {
                return this.GetHashCode() == obj.GetHashCode();
            }

            public override int GetHashCode()
            {
                return position.GetHashCode() * 41;
            }
        }
}
