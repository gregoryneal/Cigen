using System;
using System.Collections.Generic;
using UnityEngine;

namespace Clothoid {

    /// <summary>
    /// This class converts an input polyline to a ClothoidCurve using the method described by Singh McCrae in the Eurographics Association 2008.
    /// The generated curve will almost certainly not pass through the input polyline nodes, however they will usually be very close. This solution
    /// would be good for approximating curves for drawing applications, where exact node values don't necessarily need to be followed.
    /// 
    /// Some planned features that didn't make it over from the Singh McCrae paper:
    /// - G1 (tangent) discontinuities: The LK graph can be used to detect G1 discontinuities as large spikes. We can filter those out and generate
    ///   curves with sharp corners, given some additional processing.
    /// - G2 & G3 fitting: The paper describes a method to smooth the LK graph in between segments to generate G2 and G3 segment continuity.
    /// 
    /// Some problems that need addressed: 
    /// - Some curves generate anomalous curvature values, this is visible when viewing LK node graph and its segmented counterpart. No work has been
    ///   done to address this yet.
    /// </summary>
    public class ClothoidSolutionSinghMcCrae : ClothoidSolution
    {
        public static float CURVE_FACTOR = (float)System.Math.Sqrt(System.Math.PI/2);
        /// <summary>
        /// The approximated segments from the segmented regression algorithm.
        /// </summary>
        [HideInInspector]
        public List<Vector3> segmentedLKNodes = new List<Vector3>();


        public List<Vector3> SegmentedLKNodes_scaled { get; private set; }
        /// <summary>
        /// This dictionary maps node positions to their coordinates in LK Space (arc length/curvature space).
        /// The value of the dictionary takes the form of a pair of floats, the first float is the arc length,
        /// and the second float is the curvature at the given Vector3.
        /// Note, you will not find curvature values for the first and last node here.
        /// </summary>
        public Dictionary<Vector3, (float, float)> LKNodeMap { get; private set; }

        /// <summary>
        /// This is a generated ordered list of node values in the LK graph. x values are the arc length, and z
        /// values represent the curvature. Y values are not used.
        /// </summary>
        public List<Vector3> LKNodeMap_orderedList { get {
            List<Vector3> v = new List<Vector3>();
            for(int i = 0; i < this.polyline.Count; i++) {
                try {
                    Vector3 a = new Vector3(LKNodeMap[this.polyline[i]].Item1, 0, LKNodeMap[this.polyline[i]].Item2);
                    v.Add(a);
                } catch (Exception) {
                    Debug.LogError($"Couldn't add node from LKNodeMap with index: {i}");
                    continue;
                }
            }
            return v;
        }}

        /// <summary>
        /// Normalized lknode map values, for viewing in the plot, as curvature values tend to be very small.
        /// </summary>
        public Dictionary<Vector3, (float, float)> LKNodeMap_norm { get; private set; }

        /// <summary>
        /// This is an ordered list of Vector3s built upon LKNodeMap_norm, the X value is the arclength and the Z value is the normalized Curvature
        /// useful for visualization since normally the curvature is very small.
        /// </summary>
        public List<Vector3> LKNodeMap_scaled_orderedList { get {
            List<Vector3> v = new List<Vector3>();
            for(int i = 0; i < this.polyline.Count; i++) {
                try {
                    Vector3 a = new Vector3(LKNodeMap_norm[this.polyline[i]].Item1, 0, LKNodeMap_norm[this.polyline[i]].Item2);
                    v.Add(a);
                } catch (Exception) {
                    continue;
                }
                //Debug.Log($"(arcLength, 0, curvature) -> {a}");
            }
            return v;
        }}
        
        /// <summary>
        /// Max value for curvature normalization.
        /// </summary>
        public float maxNormalizedCurvature = 10;

        /// <summary>
        /// This list is populated with samples of the curve at arc lengths equivalent to the node arc lengths of the input polyline.
        /// </summary>
        public List<Vector3> ArcLengthSamples { get; protected set; }
        /// <summary>
        /// arc length samples translated by FitTranslate
        /// </summary>
        public List<Vector3> TranslatedArcLengthSamples { get { 
            List<Vector3> ret = new List<Vector3>();
            for (int i = 0; i < ArcLengthSamples.Count; i++) {
                ret.Add(ArcLengthSamples[i] + FitTranslate);
            }
            return ret;
        }}
        /// <summary>
        /// Arc length samples minimum index, this is always 0 but I used it this way to be consistent with the paper.
        /// </summary>
        private int arcLengthSamplesMinIndex;
        /// <summary>
        /// This value should be equal to ArcLengthSamples.Count
        /// </summary>
        private int arcLengthSamplesMaxIndex;
        /// <summary>
        /// This tells us how to shift the solution curve on the XZ plane to best fit the input polyline.
        /// Add this value to a sampled point to align the curve with the input (minus a rotation on the y axis)
        /// </summary>
        private Vector3 FitTranslate => cmPolyline - cmCurve;
        /// <summary>
        /// This tells us how much we need to rotate the solution about the Y axis (in degrees) to fit the input polyline
        /// </summary>
        private float fitRotate = 0;
        /// <summary>
        /// A rotation transform vector that when applied to a point on the curve in local space, approximates the position of the curve centered on the input polyline, minus a translation.
        /// </summary>
        private double[][] rotationMatrix;
        public Vector3 cmPolyline = Vector3.zero;
        public Vector3 cmCurve = Vector3.zero;
        [Range(0f, 0.1f)]
        public float maxError = 0.01f;
        
        void Start()
        {
            this.LKNodeMap = new Dictionary<Vector3, (float, float)>();
            this.LKNodeMap_norm = new Dictionary<Vector3, (float, float)>();
            this.rotationMatrix = new double[][]{new double[]{0}, new double[]{0}, new double[]{0}};
        }

        /// <summary>
        /// Calculate the Arc length and curvature at a polyline node index. Or return the cached one.
        /// </summary>
        /// <param name="polylineNodeIndex"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private (float, float) GetLKPositionAtPolylineIndex(int polylineNodeIndex) {
            if (polylineNodeIndex == 0) {
                //approximate what the start curvature should be with a linear approximation from the next two node segment curvatures.
                if (this.polyline.Count >= 3) {
                    (float, float) pointa = GetLKPositionAtPolylineIndex(1);
                    (float, float) pointb = GetLKPositionAtPolylineIndex(2);
                    float newArcLength = 0;
                    float newCurvature = ((pointb.Item2 - pointa.Item2) * (newArcLength - pointb.Item1) / (pointb.Item1 - pointa.Item1)) + pointb.Item2;
                    return (newArcLength, newCurvature);
                } else {
                    return (0, GetLKPositionAtPolylineIndex(1).Item2);
                }
            }
            if (polylineNodeIndex == this.polyline.Count-1) {
                if (this.polyline.Count >= 3) {
                    (float, float) pointa = GetLKPositionAtPolylineIndex(this.polyline.Count-3);
                    (float, float) pointb = GetLKPositionAtPolylineIndex(this.polyline.Count-2);
                    float newArcLength = EstimateArcLength(polylineNodeIndex);
                    float newCurvature = ((pointb.Item2 - pointa.Item2) * (newArcLength - pointb.Item1) / (pointb.Item1 - pointa.Item1)) + pointb.Item2;
                    return (newArcLength, newCurvature);
                } else {
                    return (EstimateArcLength(polylineNodeIndex), GetLKPositionAtPolylineIndex(this.polyline.Count-2).Item2);
                }
            }
            if (LKNodeMap.TryGetValue(this.polyline[polylineNodeIndex], out (float, float) lkNode)) {
                return lkNode;
            }

            Vector3 point1 = this.polyline[polylineNodeIndex-1];
            Vector3 point2 = this.polyline[polylineNodeIndex];
            Vector3 point3 = this.polyline[polylineNodeIndex+1];
            //negatize the curvature because in this solution the convention is that positive curvature is a right turn, opposite to most conventions
            return (EstimateArcLength(this.polyline[polylineNodeIndex]), -Clothoid.Mathc.MoretonSequinCurvature(point1, point2, point3));
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

        /// <summary>
        /// This function uses the walk matrix to figure out where the segments should start and end on the given polyline node. These values are marked in the segment end array.
        /// </summary>
        /// <param name="walkMatrix"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <param name="segmentEnd"></param>
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
            this.LKNodeMap.Clear();
            if (polyline.Count < 3) return;
            //PrintDict();
            //Debug.Log("=========================");
            for (int i = 0; i < polyline.Count; i++) {
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
            Dictionary<Vector3, (float, float)> normalizedCurvatureMap = new Dictionary<Vector3, (float, float)>();
            foreach (KeyValuePair<Vector3, (float, float)> kvp in this.LKNodeMap) {
                normalizedCurvatureMap.Add(kvp.Key, (kvp.Value.Item1, kvp.Value.Item2 * maxNormalizedCurvature));
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

        /// <summary>
        /// This will build the clothoid curve from the input polyline and an endpoint weight (for alignment).
        /// Once this has run you can use the solution to calculate points from the clothoid curve by arc length.
        /// 
        /// TODO: watch this video and redo the 
        /// </summary>
        /// <param name="inputPolyline"></param>
        /// <param name="endpointWeight"></param>
        /// <returns></returns>
        public override ClothoidCurve CalculateClothoidCurve(List<Vector3> inputPolyline, float regressionError = 0.1f, float endpointWeight = 1f)
        {
            SetPolyline(inputPolyline);
            this.segmentedLKNodes = SegmentedRegression3(this.LKNodeMap_orderedList, this.maxError);
            //normalize the segmented nodes for viewing on a plot
            SegmentedLKNodes_scaled = new List<Vector3>();
            for (int i = 0; i < segmentedLKNodes.Count; i++) {
                SegmentedLKNodes_scaled.Add(new Vector3(segmentedLKNodes[i].x, segmentedLKNodes[i].y, segmentedLKNodes[i].z * maxNormalizedCurvature));
            }
            /*for (int i = 0; i < this.segmentedLKNodes.Count; i++) {
                Debug.Log($"Segmented LK Node {i}: {segmentedLKNodes[i]}");
            }*/
            //SetupCanonicalSegments(); //build the segmentTranslation and segmentYRotation lists
            this.clothoidCurve = new ClothoidCurve(ClothoidSegment.GenerateSegmentsFromLKGraph(segmentedLKNodes), inputPolyline, (float)System.Math.Sqrt(System.Math.PI/2));
            SetupArcLengthSamples(); //collect samples on the curve based on the arc lengths of the polyline
            SetupFitTranslation(endpointWeight);
            SetupFitRotation3(); //use the previous sampled points to calculate the optimal translation and rotation offset for the curve
            //SetupFitRotation(endpointWeight);

            //CalculateTangentsAndNormals();
            /*Debug.Log("====== Segment Offset Results ======");
            for(int i = 0; i < this.segmentTranslation.Count; i++) {
                Debug.Log($"trans: {segmentTranslation[i]}");
                Debug.Log($"rot: {segmentYRotation[i]}");
            } 
            Debug.Log("=================================");
            Debug.Log("====== Curve Fit Results ======");
            Debug.Log($"trans: {this.fitTranslate}");
            Debug.Log($"rot: {this.fitRotate}");
            Debug.Log("=================================");*/
            //this.clothoidCurve.AddBestFitTranslationRotation(this.cmCurve, this.FitTranslate, this.fitRotate);
            this.clothoidCurve.AddBestFitTranslationRotation(this.cmCurve, this.cmPolyline, this.rotationMatrix);
            return this.clothoidCurve;
        }

        /// <summary>
        /// Sample a point on a generalized clothoid curve, one made up of line segments, circle segments and clothoid segments.
        /// </summary>
        /// <param name="startArcLength"></param>
        /// <param name="endArcLength"></param>
        /// <param name="startCurvature"></param>
        /// <param name="endCurvature"></param>
        /// <param name="interpolation"></param>
        /// <returns></returns>
        public Vector3 SampleGeneralizedPoint(float startArcLength, float endArcLength, float startCurvature, float endCurvature, float interpolation) {
            Vector3 transVec;
            float totalArcLength = endArcLength - startArcLength;
            float currArcLength = startArcLength + (interpolation * totalArcLength);
            switch (ClothoidSegment.GetLineTypeFromCurvatureDiff(startCurvature, endCurvature)) {
                case LineType.CLOTHOID:
                    Debug.Log("Sampling clothoid");
                    transVec = ClothoidSegment.SampleClothoidSegment(startArcLength, endArcLength, startCurvature, endCurvature, interpolation);
                break;
                case LineType.LINE:
                    Debug.Log("Sampling line");
                    transVec = new Vector3(currArcLength - startArcLength, 0, 0);
                break;
                default:
                //LineType.CIRCLE
                    Debug.Log("Sampling circle");
                    float radius = 2f / (endCurvature + startCurvature);
                    bool negativeCurvature = radius < 0;
                    if (negativeCurvature) {
                        radius = Mathf.Abs(radius);
                    }

                    float circumference = Mathf.PI * 2 * radius;
                    float anglesweep_rad = totalArcLength * Mathf.PI * 2 / circumference;

                    if (negativeCurvature) {
                        //Debug.Log("curvature negative");
                        transVec = new Vector3(0, 0, radius);
                        transVec = ClothoidSegment.RotateAboutAxis(transVec, Vector3.up, interpolation * anglesweep_rad * 180f / Mathf.PI);
                        transVec = new Vector3(transVec.x, transVec.y, transVec.z - radius);
                    } else {
                        //Debug.Log("curvature positive");
                        transVec = new Vector3(0, 0, -radius);
                        transVec = ClothoidSegment.RotateAboutAxis(transVec, Vector3.up, -interpolation * anglesweep_rad * 180f / Mathf.PI);
                        transVec = new Vector3(transVec.x, transVec.y, transVec.z + radius);
                    }
                break;
            }

            return transVec;
        }

        /// <summary>
        /// Get a sample of the curve for each node in the input polyline.
        /// </summary>
        protected void SetupArcLengthSamples() {
            this.ArcLengthSamples = new List<Vector3>();
            this.arcLengthSamplesMinIndex = int.MaxValue;
            this.arcLengthSamplesMaxIndex = 0;
            for (int i = 0; i < this.LKNodeMap_orderedList.Count; i++) {
                Vector3 node = this.LKNodeMap_orderedList[i];
                ArcLengthSamples.Add(clothoidCurve.SampleCurveFromArcLength(node.x));/*
                for (int j = 0; j < this.segmentedLKNodes.Count-1; j++) {
                    Vector3 segmentedNode = this.segmentedLKNodes[j];
                    Vector3 nextSegmentedNode = this.segmentedLKNodes[j+1];
                    if (node.x >= segmentedNode.x && node.x <= nextSegmentedNode.x) {
                        //float curveS = segmentedNode.z;
                        //float curveE = nextSegmentedNode.z;
                        float interp = (node.x - segmentedNode.x) / (nextSegmentedNode.x-segmentedNode.x);
                        Vector3 transVec = SampleGeneralizedPoint(segmentedNode.x, nextSegmentedNode.x, segmentedNode.z, nextSegmentedNode.z, interp);
                        Vector3 newVec = this.segmentTranslation[j] + ClothoidSegment.RotateAboutAxis(transVec, Vector3.up, this.segmentYRotation[j]);
                        arcLengthSamples.Add(newVec);
                        if (i < arcLengthSamplesMinIndex)
                            arcLengthSamplesMinIndex = i;
                        if (i > arcLengthSamplesMaxIndex)
                            arcLengthSamplesMaxIndex = i;
                        
                        break;
                    }
                }*/
            }
            arcLengthSamplesMinIndex = 0;
            arcLengthSamplesMaxIndex = ArcLengthSamples.Count;
            /*
            Debug.Log($"arcLengthSampleStart: {arcLengthSamplesMinIndex}");
            Debug.Log($"arcLengthSamplesEnd: {arcLengthSamplesMaxIndex}");
            Debug.Log($"Max i: {this.LKNodeMap_orderedList.Count-1}");

            Debug.Log("============== Arc Length Samples ===============");
            for (int i = 0; i < this.arcLengthSamples.Count; i++) {
                Debug.Log(this.arcLengthSamples[i]);
            }
            Debug.Log("=============================================");*/
        }

        /// <summary>
        /// Uses singular value decomposition to factorize the product of the centered input polyline and the centered curve as a product of a rotation, scaling, and
        /// another rotation. Since the two curves are very close in size due to the generation algorithm, we only need the rotation portion of the factorization.
        /// </summary>
        protected void SetupFitRotation3() {
            //1. Translate both sets of points so they are centered around the origin (point - cmPolyline)
            //2. Create a matrix for each set of points. A is the points on the arc length samples and B is the points on the input polyline.
            //2b. The matrices A and B are organized with row vectors, where each row is a point on the line. here is an example with 3 points
            // | ax ay az |
            // | bx by bz |
            // | cx cy cz |
            //2c. Find the transpose of A (A') and multiply it with B => A'B = M
            //3. Find the SVD of M => SVD(M) = USV' where U and V' are orthogonal (real square) matrices and S is a diagonal. V' is the transpose of V.
            //4. The optimal rotation is given by R = UV'.
            //5. To rotate a point by this amount we must multiply the *column* vector by the rotation vector P' = RP where P => [x, y, z]^T, we must read the result as a column vector as well.

            //1. & 2.


            /*double[][] cip = new double[clippedPolyline.Count][];
            double[][] ccp = new double[clippedPolyline.Count][];*/
            double[][] cip = new double[polyline.Count][];
            double[][] ccp = new double[polyline.Count][];
            for (int i = 0; i < ArcLengthSamples.Count; i++) {
                //Debug.Log("Accessing " + i);
                //Vector3 centeredInputPoint = clippedPolyline[i] - this.cmPolyline;
                Vector3 centeredInputPoint = polyline[i] - this.cmPolyline;
                Vector3 centeredCurvePoint = ArcLengthSamples[i] - this.cmCurve;
                cip[i] = new double[] {centeredInputPoint.x, centeredInputPoint.y, centeredInputPoint.z};
                ccp[i] = new double[] {centeredCurvePoint.x, centeredCurvePoint.y, centeredCurvePoint.z};
            }

            //3. 
            double[][] M = Clothoid.Mathc.SVDJacobiProgram.MatProduct(Clothoid.Mathc.SVDJacobiProgram.MatTranspose(ccp), cip);

            //4. Vh is V transpose.
            if (Clothoid.Mathc.SVDJacobiProgram.SVD_Jacobi(M, out double[][] U, out double[][] Vh, out double[] S)) {
            
                //5.
                this.rotationMatrix = Clothoid.Mathc.SVDJacobiProgram.MatProduct(U, Vh);
            } else {
                Debug.LogError("Error: No rotation matrix found for spline");
            }
            //Clothoid.Math.SVDJacobiProgram.MatShow(this.rotationMatrix, 2, 4);
        }
        
        /// <summary>
        /// This is my attempt at copying the rotation solving algorithm from the paper. It was not successful but I leave it here just in case I ever want to work on it again.
        /// I ended up using singlular value decomposition.
        /// </summary>
        protected void SetupFitRotation(float endpointWeight) {

            float[] A_pq = new float[4];
            Array.Fill(A_pq, 0);

            for (int i = arcLengthSamplesMinIndex; i < arcLengthSamplesMaxIndex; i++) {
                //difference in the polyline point to the center of mass
                //Vector3 p_i = clippedPolyline[i] - cmPolyline;
                Vector3 p_i = polyline[i] - cmPolyline;
                //diff in the curve point to the center of mass of the curve
                Vector3 q_i = ArcLengthSamples[i-arcLengthSamplesMinIndex] - cmCurve;

                float weight = i == arcLengthSamplesMinIndex || i == arcLengthSamplesMaxIndex ? endpointWeight : 1f;

                //weighting vector of some sort
                A_pq[0] += p_i.x * q_i.x * weight;
                A_pq[1] += p_i.x * q_i.z * weight;
                A_pq[2] += p_i.z * q_i.x * weight;
                A_pq[3] += p_i.z * q_i.z * weight;
            }

            float[] A_pqTA_pq = new float[4];
            A_pqTA_pq[0] = (A_pq[0] * A_pq[0]) + (A_pq[2] * A_pq[2]);
            A_pqTA_pq[1] = (A_pq[0] * A_pq[1]) + (A_pq[2] * A_pq[3]);
            A_pqTA_pq[2] = (A_pq[0] * A_pq[1]) + (A_pq[2] * A_pq[3]);
            A_pqTA_pq[3] = (A_pq[1] * A_pq[1]) + (A_pq[3] * A_pq[3]);

            float a, b, c, d;
            a = A_pqTA_pq[0];
            b = A_pqTA_pq[1];
            c = A_pqTA_pq[2];
            d = A_pqTA_pq[3];

            //matrix ranks? column and row numbers I think.
            float r_1 = ((a + d) / 2f) + Mathf.Sqrt(((a + d) * (a + d) / 4f) + (b * c) - (a * d));
            float r_2 = ((a + d) / 2f) - Mathf.Sqrt(((a + d) * (a + d) / 4f) + (b * c) - (a * d));

            float[] sqrtA = new float[4];
            float[] Sinv = new float[4];
            float[] R = new float[4];

            if (r_1 != 0f && r_2 != 0f) {
                //if matrix is not rank deficient
                float m;
                float p;
                
                if (r_1 != r_2) {
                    /*m = (Mathf.Sqrt(r_2) - Mathf.Sqrt(r_1)) / (r_2 - r_1);
                    p = ((r_2 * Mathf.Sqrt(r_1)) - (r_1 * Mathf.Sqrt(r_2))) / (r_2 - r_1);*/
                    m = (Mathf.Sqrt(Mathf.Abs(r_2)) - Mathf.Sqrt(Mathf.Abs(r_1))) / (r_2 - r_1);
                    p = ((r_2 * Mathf.Sqrt(Mathf.Abs(r_1))) - (r_1 * Mathf.Sqrt(Mathf.Abs(r_2)))) / (r_2 - r_1);
                } else {
                    /*m = 1f / (4 * r_1);
                    p = Mathf.Sqrt(r_1) / 2f;*/
                    m = 1f / (4 * r_1);
                    p = Mathf.Sqrt(Mathf.Abs(r_1)) / 2f;
                }

                //Debug.Log($"m: {m}");
                //Debug.Log($"p: {p}");

                sqrtA[0] = (m * A_pqTA_pq[0]) + p;
                sqrtA[1] = (m * A_pqTA_pq[1]);
                sqrtA[2] = (m * A_pqTA_pq[2]);
                sqrtA[3] = (m * A_pqTA_pq[3]) + p;

                float determinant = 1f / ((sqrtA[0] * sqrtA[3]) - (sqrtA[1] * sqrtA[2]));
                Sinv[0] = determinant * sqrtA[3];
                Sinv[1] = determinant * -sqrtA[1];
                Sinv[2] = determinant * -sqrtA[2];
                Sinv[3] = determinant * sqrtA[0];

                R[0] = (A_pq[0] * Sinv[0]) + (A_pq[1] * Sinv[2]);
                R[1] = (A_pq[0] * Sinv[1]) + (A_pq[1] * Sinv[3]);
                R[2] = (A_pq[2] * Sinv[0]) + (A_pq[3] * Sinv[2]);
                R[3] = (A_pq[2] * Sinv[1]) + (A_pq[3] * Sinv[3]);

                if (Mathf.Abs(R[0] - R[3]) < .001f && Mathf.Abs(R[1] - R[2]) > .001f) {
                    if (R[1] < 0f) {
                        this.fitRotate = -Mathf.Acos(R[0]) * 180f / Mathf.PI;
                    } else {
                        this.fitRotate = Mathf.Acos(R[0]) * 180f / Mathf.PI;
                    }
                } else {
                    if (R[1] < 0f) {
                        this.fitRotate = Mathf.Acos(R[0]) * 180f / Mathf.PI;
                    } else {
                        this.fitRotate = -Mathf.Acos(R[0]) * 180f / Mathf.PI;
                    }
                }
            } else {
                //matrix is rank deficient
                //use arc tangent of first tangent to approximate
                float y = this.polyline[^1].z - this.polyline[0].z;
                float x = this.polyline[^1].x - this.polyline[0].x;
                this.fitRotate = -Mathf.Atan2(y, x) * 180f / Mathf.PI;
            }

            if (float.IsNaN(this.fitRotate)) {
                Debug.Log("FITROTATE DEBUG SECTION");
                Debug.Log($"A_pq (final value after loop): {String.Join(',', A_pq)}");
                Debug.Log($"A_pqTA_pq: {String.Join(',', A_pqTA_pq)}");
                Debug.Log($"r_1: {r_1}");
                Debug.Log($"r_2: {r_2}");
                Debug.Log($"sqrtA: {String.Join(',', sqrtA)}");
                Debug.Log($"R: {String.Join(',', A_pqTA_pq)}");
            } else {
                //Debug.Log($"FitRotate degrees: {fitRotate}");
            }
        }
    
        /// <summary>
        /// This calculates the center of mass of the curve by sampling points equivalent in arc length to the input polyline.
        /// The endpoint weight is configurable.
        /// </summary>
        /// <param name="endpointWeight"></param>
        protected void SetupFitTranslation(float endpointWeight) {
            //assign weights to all points in the input polyline
            float weight = 1f;
            Vector3 polylineCM = Vector3.zero;
            Vector3 curveCM = Vector3.zero;

            float totalPolylineWeight = (weight * (polyline.Count - 2)) + (endpointWeight * 2);
            float totalCurveWeight = (weight * (ArcLengthSamples.Count - 2)) + (endpointWeight * 2);

            float w;
            for (int i = 0; i < polyline.Count; i++) { 
                w = i == 0 || i == polyline.Count - 1 ? endpointWeight : weight;
                polylineCM += polyline[i] * w;
            }

            polylineCM /= totalPolylineWeight;

            for (int i = 0; i < ArcLengthSamples.Count; i++) {
                w = i == 0 || i == ArcLengthSamples.Count - 1 ? endpointWeight : weight;
                curveCM += ArcLengthSamples[i] * w;
            }

            curveCM /= totalCurveWeight;
            
            this.cmCurve = curveCM;
            this.cmPolyline = polylineCM;
        }
    }
}