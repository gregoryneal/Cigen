using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cigen.ImageAnalyzing;

namespace Cigen
{
    public static class Maths {
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
        public static bool LineLineIntersection(out Vector3 intersection, Vector3 startPoint1, Vector3 startDir1, Vector3 startPoint2, Vector3 startDir2, AnisotropicLeastCostPathSettings settings, bool ignoreY = true){
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
                    intersection = new Vector3(intersection.x, ImageAnalysis.TerrainHeightAt(intersection.x, intersection.z, settings), intersection.z);
                }
                //Debug.Log("Line intersection");
                return true;
            }
            //Debug.Log("No line intersection");
            intersection = Vector3.zero;
            return false;
        }

        public static bool LineRectangleIntersection(out Vector3 intersection, Vector3 lineStart, Vector3 lineEnd, Vector3 boxPosition, Vector3 boxSize, AnisotropicLeastCostPathSettings settings) {
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
            if (LineLineIntersection(out intersection, lineStart, direction, corner1, corner2-corner1, settings) ||
                LineLineIntersection(out intersection, lineStart, direction, corner1, corner3-corner1, settings) ||
                LineLineIntersection(out intersection, lineStart, direction, corner3, corner4-corner3, settings) ||
                LineLineIntersection(out intersection, lineStart, direction, corner4, corner1-corner4, settings)) {
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
    }
}
