using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Clothoid {

    /// <summary>
    /// This class converts an input polyline to a ClothoidCurve using the method described by Singh McCrae in the Eurographics Association 2008.
    /// </summary>
    public class ClothoidSolutionSinghMcCrae : ClothoidSolution<ClothoidSegmentSinghMcCrae>
    {

        /// <summary>
        /// The ordered translation offsets. Translate the segment i by the amount at segmentTranslation[i] to get the right offset.
        /// </summary>
        private List<Vector3> segmentTranslation = new List<Vector3>();
        /// <summary>
        /// The ordered y rotation offsets. Rotate the segment i about the y axis by segmentYRotation[i] to get the correct rotation.
        /// </summary>
        private List<float> segmentYRotation = new List<float>();
        /// <summary>
        /// The approximated segments from the segmented regression algorithm.
        /// </summary>
        private List<Vector3> segmentedLKNodes = new List<Vector3>();
        /// <summary>
        /// This dictionary maps node positions to their coordinates in LK Space (arc length/curvature space).
        /// The value of the dictionary takes the form of a pair of floats, the first float is the arc length,
        /// and the second float is the curvature at the given Vector3.
        /// Note, you will not find curvature values for the first and last node here.
        /// </summary>
        public Dictionary<Vector3, (float, float)> LKNodeMap { get; private set; }

        public List<Vector3> LKNodeMap_orderedList { get {
            List<Vector3> v = new List<Vector3>();
            for(int i = 0; i < this.clippedPolyline.Count; i++) {
                try {
                    Vector3 a = new Vector3(LKNodeMap[this.clippedPolyline[i]].Item1, 0, LKNodeMap[this.clippedPolyline[i]].Item2);
                    v.Add(a);
                } catch (Exception) {
                    continue;
                }
            }
            return v;
        }}

        public Dictionary<Vector3, (float, float)> LKNodeMap_norm { get; private set; }

        /// <summary>
        /// This is an ordered list of Vector3s built upon LKNodeMap_norm, the X value is the arclength and the Z value is the normalized Curvature
        /// useful for visualization since normally the curvature is very small.
        /// </summary>
        public List<Vector3> LKNodeMap_norm_orderedList { get {
            List<Vector3> v = new List<Vector3>();
            for(int i = 0; i < this.clippedPolyline.Count; i++) {
                try {
                    Vector3 a = new Vector3(LKNodeMap_norm[this.clippedPolyline[i]].Item1, 0, LKNodeMap_norm[this.clippedPolyline[i]].Item2);
                    v.Add(a);
                } catch (Exception) {
                    continue;
                }
                //Debug.Log($"(arcLength, 0, curvature) -> {a}");
            }
            return v;
        }}
        /// <summary>
        /// This is a copy of the polyline list without the first or last vectors.
        /// Useful if we want to iterate over the polyline nodes (in order) that have an associated
        /// mapping in LK space.
        /// </summary>
        public List<Vector3> clippedPolyline { get; private set; }
        public float maxValue = 10;

        void Start()
        {
            this.LKNodeMap = new Dictionary<Vector3, (float, float)>();
            this.LKNodeMap_norm = new Dictionary<Vector3, (float, float)>();
        }

        /// <summary>
        /// Calculate the Arc length and curvature at a polyline node index. Or return the cached one.
        /// </summary>
        /// <param name="polylineNodeIndex"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private (float, float) GetLKPositionAtPolylineIndex(int polylineNodeIndex) {
            if (polylineNodeIndex <= 0 || polylineNodeIndex >= this.polyline.Count) {
                throw new ArgumentOutOfRangeException();
            }            
            if (LKNodeMap.TryGetValue(this.polyline[polylineNodeIndex], out (float, float) lkNode)) {
                return lkNode;
            }
            Vector3 v1 = this.polyline[polylineNodeIndex]-this.polyline[polylineNodeIndex-1];
            Vector3 v2 = this.polyline[polylineNodeIndex+1]-this.polyline[polylineNodeIndex];
            float theta = (float)System.Math.Acos((double)Vector3.Dot(v1/v1.magnitude, v2/v2.magnitude));
            float numerator = 2f * (float)System.Math.Sin(theta/2);
            float denominator = (float)System.Math.Sqrt(v1.magnitude*v2.magnitude);
            if (float.IsNaN(numerator) || float.IsInfinity(numerator) || float.IsNaN(denominator) || float.IsInfinity(denominator)) {
                Debug.Log($"Index: {polylineNodeIndex} | Count: {this.polyline.Count}");
                Debug.Log($"Numerator: {numerator} | Denominator: {denominator}");
                Debug.Log($"v1: ({v1.x}, {v1.y}, {v1.z})\t v1Magnitude: {v1.magnitude}");
                Debug.Log($"v2: ({v2.x}, {v2.y}, {v2.z})\t v2Magnitude: {v2.magnitude}");
                Debug.Log($"theta: {theta}");
                Debug.Log($"index: {polylineNodeIndex-1}: {this.polyline[polylineNodeIndex-1]}");
                Debug.Log($"index: {polylineNodeIndex}: {this.polyline[polylineNodeIndex]}");
                Debug.Log($"index: {polylineNodeIndex+1}: {this.polyline[polylineNodeIndex+1]}");
            }
            (float, float) value = (EstimateArcLength(this.polyline[polylineNodeIndex]), numerator/denominator);
            return value;
        }

        /// <summary>
        /// Segmented regression for the third time. This time I found source code from either Singh or McCrae so I'm just
        /// gonna copy it instead of trying to literally reinvent segmented linear regression with 10 lines of code. What 
        /// a massive hubris moment this was. If anyone reads this, no, it's not as easy as you might think it is based on
        /// first assumptions. Just copy this code. It will approximate a data set with linear segments through some matrix
        /// magic.
        /// </summary>
        /// <param name="dataSet">The data set to perform segmented regression on.</param>
        /// <param name="allowableError">Set this to control how accurate the linear segmentation is. If zero this will basically form a line between each sequential point in the dataSet.</param>
        /// <returns></returns>
        private List<Vector3> SegmentedRegression3(List<Vector3> dataSet, float allowableError) {
            //The list of points in LK space that determine our clothoid segments
            List<Vector3> segmentedPointSet = new List<Vector3>();
            float[,] errorMatrix = new float[dataSet.Count, dataSet.Count];
            int[,] walkMatrix = new int[dataSet.Count, dataSet.Count];
            float[,] slopeMatrix = new float[dataSet.Count, dataSet.Count];
            float[,] zIntMatrix = new float[dataSet.Count, dataSet.Count];
            
            for (int i = 0; i < dataSet.Count; i++) {
                for (int j = 0; j < dataSet.Count; j++) {
                    errorMatrix[i,j] = 0f;
                    walkMatrix[i,j] = -1;
                }
            }

            float segmentCost = allowableError;
            for (int i = 0; i+1 < dataSet.Count; i++) {
                errorMatrix[i,i+1] = segmentCost;
                if (HeightLineFit(dataSet[i], dataSet[i+1], dataSet, out float slope, out float zIntercept)) {
                    slopeMatrix[i,i+1] = slope;
                    zIntMatrix[i,i+1] = zIntercept;
                }
            }

            //populate the first diagonal which is the cost of line segment between neighbouring points.
            for (int j = 2; j < dataSet.Count; j++) {
                for (int i = 0; i+j < dataSet.Count; i++) {
                    // do linear regression on segments between i and i+j inclusive
                    // if that error + penalty for segment is less than 
                    // sum of errors [i][i+j-1] and [i+1][i+j], then use
                    // that
                    // otherwise, use sum of [i][i+j-1] and [i+1][i+j]
                    Vector3 start = dataSet[i];
                    Vector3 end = dataSet[i+j];
                    if (HeightLineFit(start, end, dataSet, out float m, out float b)) {
                        slopeMatrix[i,i+j] = m;
                        zIntMatrix[i,i+j] = b;

                        float fitError = GetFitErrors(start, end, dataSet, m, b);
                        float minError = fitError + segmentCost;                        
                        int minIndex = -1;

                        for (int k = i+1; k < i+j; k++) {
                            if (errorMatrix[i,k]+errorMatrix[k,i+j] < minError) {
                                minIndex = k;
                                minError = errorMatrix[i,k] + errorMatrix[k,i+j];
                            }
                        }

                        walkMatrix[i,i+j] = minIndex;
                        errorMatrix[i,i+j] = minError;
                    }
                }
            }

            bool[] segmentEnd = new bool[dataSet.Count];
            Array.Fill(segmentEnd, false);

            RecurseThroughMatrix(walkMatrix, 0, dataSet.Count-1, segmentEnd);

            int endIndex = -1;
            int endNextIndex;

            for (int i = 1; i < dataSet.Count; i++) {
                if (segmentEnd[i]) {
                    endIndex = i;
                    break;
                }
            }

            segmentedPointSet.Add(new Vector3(0, 0, zIntMatrix[0,endIndex]));
            for (int i = 0; i < dataSet.Count-1; i++) {
                if (segmentEnd[i]) {
                    for (int j = i+1; j < dataSet.Count; j++) {
                        if (segmentEnd[j]) {
                            endIndex = j;
                            break;
                        }
                    }

                    float s1 = slopeMatrix[i,endIndex];
                    float b1 = zIntMatrix[i,endIndex];

                    endNextIndex = endIndex;
                    for (int j = endIndex+1; j < dataSet.Count; j++) {
                        if (segmentEnd[j]) {
                            endNextIndex = j;
                            break;
                        }
                    }

                    if (endNextIndex > endIndex) {
                        float s2 = slopeMatrix[endIndex, endNextIndex];
                        float b2 = zIntMatrix[endIndex, endNextIndex];

                        float xIntersect = (b2 - b1) / (s1 - s2);
                        float zIntersect = (s1 * xIntersect) + b1;

                        segmentedPointSet.Add(new Vector3(xIntersect, 0, zIntersect));
                    } else {
                        segmentedPointSet.Add(new Vector3(dataSet[^1].x, 0, (s1 * dataSet[^1].x) + b1));
                        break;
                    }
                }
            }

            bool foundOvershoot;
            int indexToRemove;
            do {
                foundOvershoot = false;
                indexToRemove = -1;
                for (int i = 1; i < segmentedPointSet.Count-1; i++) {
                    if ((segmentedPointSet[i].x > segmentedPointSet[i+1].x && segmentedPointSet[i].x > segmentedPointSet[i-1].x) ||
                        (segmentedPointSet[i].x < segmentedPointSet[i-1].x && segmentedPointSet[i].x < segmentedPointSet[i+1].x)) {
                            indexToRemove = i;
                            foundOvershoot = true;
                            break;
                    }
                }
                if (foundOvershoot && indexToRemove >= 0) {
                    segmentedPointSet.RemoveAt(indexToRemove);
                }
            } while (foundOvershoot);

            return segmentedPointSet;
        }

        private void RecurseThroughMatrix(int[,] walkMatrix, int startIndex, int endIndex, bool[] segmentEnd)
        {
            if (startIndex+1 > endIndex) {
                segmentEnd[startIndex] = true;
                segmentEnd[endIndex] = true;
            }

            if (walkMatrix[startIndex,endIndex] == -1) {
                segmentEnd[startIndex] = true;
                segmentEnd[endIndex] = true;
            } else {
                RecurseThroughMatrix(walkMatrix, startIndex, walkMatrix[startIndex,endIndex], segmentEnd);
                RecurseThroughMatrix(walkMatrix, walkMatrix[startIndex,endIndex], endIndex, segmentEnd);
            }
        }

        /// <summary>
        /// This is the error of the linear regression approximating straight line segments in the LK node map (x -> l -> arc length, y -> k -> curvature) space.
        /// It is given in section 4.2 of the SinghMcCrae paper. A and B should be points in LK space. This function will approximate the error of a linear
        /// regression from A to B by summing the vertical error between the discrete points between A and B and the straight line connecting A and B. 
        /// If A is the point immediately preceding B, then the error is zero by definition.
        /// </summary>
        /// <returns></returns>
        private float GetFitErrors(Vector3 start, Vector3 end, List<Vector3> dataSet, float slopeMatrixValue, float zIntMatrixValue) {
            float totalError = 0f;
            int a = dataSet.IndexOf(start);
            int b = dataSet.IndexOf(end);
            for (int i = a; i <= b; i++) {
                totalError += Mathf.Abs(zIntMatrixValue + (slopeMatrixValue * dataSet[i].x) - dataSet[i].y);
            }
            return totalError;
        }

        /// <summary>
        /// This function will perform linear regression on all the points between a and b in the dataList. The function returns 
        /// </summary>
        private bool HeightLineFit(Vector3 a, Vector3 b, List<Vector3> dataList, out float aMatrixValue, out float bMatrixValue) {
            int indA = dataList.IndexOf(a);
            int indB = dataList.IndexOf(b);
            if (indA < 0 || indB < 0) throw new ArgumentException("Index of A or index of B is less than zero.");
            if (indA > indB) throw new ArgumentException("Index of a is greater than index of b.");

            float sumX = 0;
            float sumZ = 0;
            float sumXX = 0;
            float sumXZ = 0;
            int n = indB - indA + 1;

            for (int i = indA; i <= indB; i++) {
                sumX += dataList[i].x;
                sumZ += dataList[i].z;
                sumXX += dataList[i].x*dataList[i].x;
                sumXZ += dataList[i].x*dataList[i].z;
            }

            float m = ((n * sumXZ) - (sumX * sumZ))/((n * sumXX) - (sumX * sumX));
            float bValue = (sumZ - (m * sumX)) / n;
            aMatrixValue = m;
            bMatrixValue = bValue;
            return true;
        }

        /// <summary>
        /// Set the polyline nodes and recalculates the LK node map.
        /// </summary>
        /// <param name="polyline">The new polyline</param>
        protected override void SetPolyline(List<Vector3> polyline)
        {
            base.SetPolyline(polyline);
            Vector3[] vecs = new Vector3[polyline.Count-2];
            //copy the polyline minus the two endpoints to a new array
            Array.Copy(polyline.ToArray(), 1, vecs, 0, polyline.Count-2);
            this.clippedPolyline = vecs.ToList();
            this.LKNodeMap.Clear();
            if (polyline.Count < 3) return;
            //PrintDict();
            //Debug.Log("=========================");
            for (int i = 1; i < polyline.Count-1; i++) {
                //Debug.Log(this.polyline[i]);
                (float, float) lkNode = GetLKPositionAtPolylineIndex(i);
                if (float.IsNaN(lkNode.Item1) || float.IsNaN(lkNode.Item2)) continue;
                this.LKNodeMap.TryAdd(this.polyline[i], lkNode);
            }
            NormalizeCurvatureSpace();
            //PrintDict();
        }

        /// <summary>
        /// Map the curvature range from its min and max value to
        /// [-maxValue, maxValue]
        /// </summary>
        void NormalizeCurvatureSpace() {
            this.LKNodeMap_norm.Clear();
            float minValue = this.LKNodeMap.Values.Min(val => val.Item2);
            float maxValue = this.LKNodeMap.Values.Max(val => val.Item2);
            float diff = maxValue - minValue;
            float slope = 2*this.maxValue / diff;
            Dictionary<Vector3, (float, float)> normalizedCurvatureMap = new Dictionary<Vector3, (float, float)>();
            foreach (KeyValuePair<Vector3, (float, float)> kvp in this.LKNodeMap) {
                normalizedCurvatureMap.Add(kvp.Key, (kvp.Value.Item1, ((kvp.Value.Item2-minValue) * slope)-this.maxValue));
            }
            this.LKNodeMap_norm = normalizedCurvatureMap;
            //PrintPolylineDict(this.LKNodeMap_norm);
        }

        /// <summary>
        /// Print a dictionary defined by a vector and two floats
        /// </summary>
        /// <param name="d"></param>
        void PrintPolylineDict(Dictionary<Vector3, (float, float)> d) {
            for (int i = 0; i < polyline.Count; i++) {
                if (d.TryGetValue(polyline[i], out (float, float) LKNode)) {
                    Debug.Log($"Key: ({polyline[i].x}, {polyline[i].y}, {polyline[i].z}) | (arcLength, curvature) -> ({LKNode.Item1}, {LKNode.Item2})");
                }
            }
        }

        public override ClothoidCurve CalculateClothoidCurve(List<Vector3> inputPolyline)
        {
            SetPolyline(inputPolyline);
            this.segmentedLKNodes = SegmentedRegression3(this.LKNodeMap_orderedList, 0.02f);
            SetupTranslationRotationOffsets();
            for(int i = 0; i < this.LKNodeMap_orderedList.Count; i++) {
                Debug.Log(this.LKNodeMap_orderedList[i]);
            } 
            for(int i = 0; i < segmentedLKNodes.Count; i++) {
                Debug.Log(segmentedLKNodes[i]);
            }
            this.clothoidCurve = new ClothoidCurve(ClothoidSegment.GenerateSegmentsFromLKGraph(segmentedLKNodes), inputPolyline);

            return this.clothoidCurve;
        }

        /// <summary>
        /// Create the translation and rotation vectors and store them for each segment
        /// </summary>
        private void SetupTranslationRotationOffsets() {
            segmentTranslation.Clear();
            segmentYRotation.Clear();

            segmentTranslation.Add(Vector3.zero);
            segmentYRotation.Add(0);

            for (int i = 1; i < segmentedLKNodes.Count; i++) {
                Vector3 prevOffset = segmentTranslation[^1];
                float prevRotation = segmentYRotation[^1];
                float curveS = segmentedLKNodes[i-1].y;
                float curveE = segmentedLKNodes[i].y;
                float curveDiff = curveE - curveS;
                float eachLength = segmentedLKNodes[i].x - segmentedLKNodes[i-1].x;
                Vector3 transVec;

                switch (ClothoidSegment.GetLineTypeFromCurvatureDiff(curveS, curveE)) {
                    case LineType.LINE:
                        transVec = new Vector3(eachLength, 0, 0);
                        segmentTranslation.Add(prevOffset + ClothoidSegment.RotateAboutAxis(transVec, Vector3.up, prevRotation));
                        segmentYRotation.Add(prevRotation);
                    break;
                    case LineType.CLOTHOID:                        
                        float B = Mathf.Sqrt(eachLength/(Mathf.PI * Mathf.Abs(curveDiff)));
                        float t1 = curveS * B * ClothoidSegment.CURVE_FACTOR;
                        float t2 = curveE * B * ClothoidSegment.CURVE_FACTOR;
                        transVec = ClothoidSegment.GetClothoidPiecePoint(t1, t2, t2) * Mathf.PI * B;
                        float rotAmount = t2 > t1 ? ((t2 * t2) - (t1 * t1)) * 180f / Mathf.PI : ((t1 * t1) - (t2 * t2)) * 180f / Mathf.PI;
                        segmentTranslation.Add(prevOffset + ClothoidSegment.RotateAboutAxis(transVec, Vector3.up, prevRotation));
                        segmentYRotation.Add(prevRotation-rotAmount);
                    break;
                    case LineType.CIRCLE:
                        float radius = 2f / (curveS + curveE);
                        bool negativeCurvature = radius < 0 ? true : false;
                        radius = Mathf.Abs(radius);
                        float circumference = 2 * Mathf.PI * radius;
                        float anglesweep_rad = (2 * Mathf.PI * eachLength) / circumference;
                        float rot;
                        if (negativeCurvature) {
                            transVec = new Vector3(0, 0, radius);
                            transVec = ClothoidSegment.RotateAboutAxis(transVec, Vector3.up, anglesweep_rad * 180f / Mathf.PI);
                            transVec = new Vector3(transVec.x, transVec.y, transVec.z - radius);
                            rot = -anglesweep_rad * 180f / Mathf.PI;
                        } else {
                            transVec = new Vector3(0, 0, -radius);
                            transVec = ClothoidSegment.RotateAboutAxis(transVec, Vector3.up, -anglesweep_rad * 180f / Mathf.PI);
                            transVec = new Vector3(transVec.x, transVec.y, transVec.z + radius);
                            rot = anglesweep_rad * 180f / Mathf.PI;
                        }

                        segmentTranslation.Add(prevOffset + ClothoidSegment.RotateAboutAxis(transVec, Vector3.up, prevRotation));
                        segmentYRotation.Add(prevRotation - rot);

                    break;
                }
            }

        }       

        /// <summary>
        /// This will setup the translation/rotation matrix that aligns each segment with the input polyline.
        /// This also has a variable input weighting to configure how much the endpoints need to be aligned.
        /// </summary>
        protected void SetupFitTransform(float endpointWeight) {
            //assign weights to all points in the input polyline
            int arcLengthSampleStart = 0;
            int arcLengthSampleEnd = polyline.Count;
            float[] weighting = new float[arcLengthSampleEnd - arcLengthSampleStart - 1];
            Array.Fill(weighting, 1f);
            weighting[arcLengthSampleStart] = endpointWeight;
            weighting[arcLengthSampleEnd-1] = endpointWeight;
            float totalWeight = weighting.Sum(); //total weight of all pieces for normalization i would imagine.

            //find a translation matrix by aligning the centers of mass of the input polyline and the resulting curve.
            Vector3 cmPolyline = Vector3.zero;
            Vector3 cmCurve = Vector3.zero;

            for (int i = arcLengthSampleStart; i < arcLengthSampleEnd; i++) {
                cmPolyline += this.polyline[i] * weighting[i-arcLengthSampleStart];
            }

            cmPolyline /= totalWeight;

            for (int i = arcLengthSampleStart; i < arcLengthSampleEnd; i++) {
                
            }

        }
    }
}