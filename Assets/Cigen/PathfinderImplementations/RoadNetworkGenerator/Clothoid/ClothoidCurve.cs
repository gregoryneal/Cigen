using System;
using System.Collections.Generic;
using UnityEngine;

namespace Clothoid {

    /// <summary>
    /// A class that holds a sequence of ClothoidSegments and can calculate any position given an arc length (optionally parameterized to the range [0f,1f])
    /// </summary>
    public class ClothoidCurve {
        public int Count { get { return segments.Count; }}

        public ClothoidSegment this[int index] {
            get => this.segments[index];
        }

        public int PolylineCount { get { return this.inputPolyline.Count; }}

        private List<Vector3> drawingNodes = new List<Vector3>();

        public float StartCurvature { get {
            return segments[0].StartCurvature;
        }}

        public float EndCurvature { get {
            return segments[^1].EndCurvature;
        }}

        public float TotalArcLength { get {
            return segments[^1].ArcLengthEnd - segments[0].ArcLengthStart;
        }}
        protected List<ClothoidSegment> segments = new List<ClothoidSegment>();
        protected List<Vector3> inputPolyline = new List<Vector3>();

        protected Vector3 curveCM = Vector3.zero;
        protected Vector3 polylineCM = Vector3.zero;
        protected float polylineRotation = 0;
        private double[][] rotationMatrix;

        public ClothoidCurve() {
            this.segments = new List<ClothoidSegment>();
            this.inputPolyline = new List<Vector3>();
        }
        public ClothoidCurve(List<ClothoidSegment> orderedSegments, List<Vector3> inputPolyline) {
            this.segments = orderedSegments;
            this.inputPolyline = inputPolyline;
        }

        public void AddBestFitTranslationRotation(Vector3 curveCM, Vector3 polylineCM, float bestRotate) {
            this.curveCM = curveCM;
            this.polylineCM = polylineCM;
            this.polylineRotation = bestRotate;
            Debug.Log($"Best rotation angle (degrees): {bestRotate}");
        }

        public void AddBestFitTranslationRotation(Vector3 curveCM, Vector3 polylineCM, double[][] bestRotate) {
            this.curveCM = curveCM;
            this.polylineCM = polylineCM;
            this.rotationMatrix = bestRotate;

            Debug.Log("best rotation matrix");
            Clothoid.Math.SVDJacobiProgram.MatShow(bestRotate, 2, 4);
        }

        public ClothoidCurve AddSegment(ClothoidSegment newSegment) {
            if (segments.Count > 0) newSegment.ShiftStartArcLength(segments[^1].ArcLengthEnd);
            else newSegment.ShiftStartArcLength(0);
            this.segments.Add(newSegment);
            float arclength = newSegment.TotalArcLength;
            return this;
        }

        public void Reset() {
            this.segments.Clear();
            this.inputPolyline.Clear();   
        }

        /// <summary>
        /// Sample positions from this curve, without applying the input polyline transformations.
        /// In local space of the curve.
        /// </summary>
        /// <param name="arcLength"></param>
        /// <returns></returns>
        public Vector3 SampleCurveFromArcLength(float arcLength) {
            Vector3 value = Vector3.zero;
            Vector3 offset = Vector3.zero;
            float rotation = 0;
            for (int i = 0; i < segments.Count; i++) {
                ClothoidSegment segment = segments[i];
                if (arcLength >= segment.ArcLengthStart && arcLength <= segment.ArcLengthEnd) {
                    //Debug.Log($"Now sampling curve type: {segment.LineType}");
                    value = segment.SampleSegmentByTotalArcLength(arcLength);
                    value = ClothoidSegment.RotateAboutAxis(value, Vector3.up, rotation);
                    value += offset;
                    break;
                }
                offset += ClothoidSegment.RotateAboutAxis(segment.Offset, Vector3.up, rotation);
                rotation += segment.Rotation; //apply rotation first
                //Debug.Log($"New offset and rotation along curve: {offset}, {rotation}");
            }
            return value;
        }

        /// <summary>
        /// Get samples along the curve, if it was generated from a polyline this will return points that approximate the input polyline as well. If not the points will start from the origin.
        /// </summary>
        /// <param name="numSamples"></param>
        /// <returns></returns>
        public List<Vector3> GetSamples(int numSamples) {
            List<Vector3> points = new List<Vector3>();
            float increment = TotalArcLength / numSamples;
            for (float arcLength = 0; arcLength < TotalArcLength; arcLength += increment) {
                Vector3 point = SampleCurveFromArcLength(arcLength); //untranslated, unrotated
                if (point == Vector3.zero && arcLength != 0) continue;
                point -= curveCM;
                point = GetRotatedPoint(point);
                point += polylineCM;                
                points.Add(point);
            }
            return points;
        }

        /// <summary>
        /// Rotates a point by the rotation matrix. Be sure to treat the unrotatedPoint and rotatedPoint as a column vector.
        /// Be sure to rotate only points that are centered on the origin, and then translate them by the polyline center of mass afterwards.
        /// </summary>
        /// <param name="unrotatedPoint"></param>
        /// <returns></returns>
        public Vector3 GetRotatedPoint(Vector3 unrotatedPoint, float degrees = 0) {
            //return ClothoidSegment.RotateAboutAxis(unrotatedPoint, Vector3.up, degrees);
            return GetRotatedPoint(unrotatedPoint, this.rotationMatrix);
        }

        public static Vector3 GetRotatedPoint(Vector3 unrotatedPoint, double[][] rotationMatrix) {      
            if (rotationMatrix == null) return unrotatedPoint;      
            double[][] point = new double[3][];
            point[0] = new double[] {unrotatedPoint.x};
            point[1] = new double[] {unrotatedPoint.y};
            point[2] = new double[] {unrotatedPoint.z};
            double[][] rotatedPoint = Clothoid.Math.SVDJacobiProgram.MatProduct(rotationMatrix, point);
            return new Vector3((float)rotatedPoint[0][0], (float)rotatedPoint[1][0], (float)rotatedPoint[2][0]);
        }


        /// <summary>
        /// Create some test segments and display them.
        /// </summary>
        /// <returns></returns>
        public static IEnumerator<List<Vector3>> TestAllConnectionTypes(int a=0, int b=10, int c=15, int d=20, int e=35, int f=49) {
            /*
            float minchange = 0;
            float maxchange = 1f;
            float cs = 0;
            float ce = cs + UnityEngine.Random.Range(minchange, maxchange);
            ClothoidSegmentSinghMcCrae segment1 = new ClothoidSegmentSinghMcCrae(a, b, cs, ce);
            cs = ce;
            ce = cs - UnityEngine.Random.Range(minchange, maxchange);
            ClothoidSegmentSinghMcCrae segment2 = new ClothoidSegmentSinghMcCrae(b, c, cs, ce);
            cs = ce;
            ce = cs + UnityEngine.Random.Range(minchange, maxchange);
            ClothoidSegmentSinghMcCrae segment3 = new ClothoidSegmentSinghMcCrae(c, d, cs, ce);
            cs = ce;
            ce = cs - UnityEngine.Random.Range(minchange, maxchange);
            ClothoidSegmentSinghMcCrae segment4 = new ClothoidSegmentSinghMcCrae(d, e, cs, ce);
            cs = ce;
            ce = cs + UnityEngine.Random.Range(minchange, maxchange);
            ClothoidSegmentSinghMcCrae segment5 = new ClothoidSegmentSinghMcCrae(e, f, cs, ce);
            */

            ClothoidSegment lineSeg = new ClothoidSegment(0, b, 0, 0);
            ClothoidSegment lineSeg2 = new ClothoidSegment(b, c, 0, 0);
            ClothoidSegment cplus = new ClothoidSegment(0, b, 1, 1);
            ClothoidSegment cplus2 = new ClothoidSegment(b, c, 1, 1);
            ClothoidSegment cminus = new ClothoidSegment(0, b, -1, -1);
            ClothoidSegment cminus2 = new ClothoidSegment(b, c, -1, -1);
            ClothoidSegment clplus = new ClothoidSegment(0, b, 0, 1);
            ClothoidSegment clplus2 = new ClothoidSegment(b, c, 0, 1);
            ClothoidSegment clminus = new ClothoidSegment(0, b, 0, -1);
            ClothoidSegment clminus2 = new ClothoidSegment(b, c, 0, -1);
            ClothoidSegment clplusminus = new ClothoidSegment(0, b, 1, -1);
            ClothoidSegment clplusminus2 = new ClothoidSegment(b, c, 1, -1);
            ClothoidSegment clminusplus = new ClothoidSegment(0, b, -1, 1);
            ClothoidSegment clminusplus2 = new ClothoidSegment(b, c, -1, 1);

            ClothoidSegment[] segments = new ClothoidSegment[]{lineSeg, cplus, cminus, clplus, clminus, clplusminus, clminusplus};
            ClothoidSegment[] segments2 = new ClothoidSegment[]{lineSeg2, cplus2, cminus2, clplus2, clminus2, clplusminus2, clminusplus2};

            for (int i = 0; i < segments.Length; i++) {
                for (int j = 0; j < segments2.Length; j++) {
                    ClothoidCurve curve = new ClothoidCurve(new List<ClothoidSegment>() {segments[i], segments2[j]}, new List<Vector3>());

                    for (int k = 0; k < curve.segments.Count; k++) {
                        Debug.Log($"({i},{j}) Segment{k+1}: {curve.segments[k]} -> {curve.segments[k].Description()}");
                    }

                    yield return curve.GetSamples(100);
                }
            }
            yield break;
        }

        public ClothoidCurve AddRandomCurve() {
            bool doLine = UnityEngine.Random.value <= .33f;

            if (segments.Count > 0) {
                ClothoidSegment lastSegment = segments[^1];

                float newArcLength = UnityEngine.Random.Range(5, 9);
                float curvatureChange = doLine ? 0 : UnityEngine.Random.Range(-.3f, .3f);
                float startCurvature = doLine ? 0 : lastSegment.EndCurvature + curvatureChange;
                ClothoidSegment newSegment = new ClothoidSegment(lastSegment.ArcLengthEnd, lastSegment.ArcLengthEnd+newArcLength, startCurvature, startCurvature+curvatureChange);
                return AddSegment(newSegment);
            } else {
                float newArcLength = UnityEngine.Random.Range(5, 9);
                float curvatureChange = doLine ? 0 : UnityEngine.Random.Range(-.3f, .3f);
                float startCurvature = doLine ? 0 : curvatureChange;
                ClothoidSegment newSegment = new ClothoidSegment(0, newArcLength, startCurvature, startCurvature+curvatureChange);
                return AddSegment(newSegment);
            }
        }

        /// <summary>
        /// Get the position of a polyline node by its index.
        /// </summary>
        /// <param name="polylineIndex"></param>
        /// <returns></returns>
        public Vector3 GetPositionFromPolyline(int polylineIndex) {
            if (polylineIndex >= this.inputPolyline.Count) polylineIndex = this.inputPolyline.Count - 1;
            return this.inputPolyline[polylineIndex];
        }

        /// <summary>
        /// Get the arc length of a polyline node based on its index.
        /// </summary>
        /// <param name="polylineIndex"></param>
        /// <returns></returns>
        public float GetStartingArcLengthFromPolylineIndex (int polylineIndex) {
            return EstimateArcLength(polylineIndex);
        }

        /// <summary>
        /// Sample the clothoid curve
        /// </summary>
        /// <returns></returns>
        /*public List<Vector3> CalculateDrawingNodes(int subdivisions) {
            if (this.drawingNodes == null || this.drawingNodes.Count != subdivisions) {
                List<Vector3> positions = new List<Vector3>();
                float arcLength = this[^1].ArcLengthEnd - this[0].ArcLengthStart;
                for (int i = 0; i < subdivisions; i++) {
                    float currArcLength = this[0].ArcLengthStart + (i * arcLength / (subdivisions-1));
                    positions.Add(SamplePointByArcLength(currArcLength));
                }
                this.drawingNodes = positions;
                return positions;
            } else {
                return this.drawingNodes;
            }
        }*/

        /// <summary>
        /// Calculate drawing nodes using the input polyline arc length values at each node.
        /// </summary>
        /// <returns></returns>
        /*public List<Vector3> CalculateDrawingNodesAtInputPolylineNodes() {
            List<Vector3> positions = new List<Vector3>();
            for (int i = 0; i < this.inputPolyline.Count; i++) {
                float arcLength = EstimateArcLength(i);
                positions.Add(SamplePointByArcLength(arcLength));
            }
            return positions;
        }*/

        protected float EstimateArcLength(Vector3 node) {
            if (this.inputPolyline.Contains(node) == false) throw new ArgumentException("Node not contained in the input polyline.");
            return EstimateArcLength(this.inputPolyline.IndexOf(node));
        }

        protected float EstimateArcLength(int polylineIndex) {
            if (polylineIndex > this.inputPolyline.Count - 2) polylineIndex = this.inputPolyline.Count - 2;
            if (this.inputPolyline == null) throw new ArgumentNullException();
            if (this.inputPolyline.Count == 0) throw new ArgumentOutOfRangeException();
            float sum = 0;
            for (int i = 0; i+1 <= polylineIndex; i++) {
                sum += Vector3.Distance(this.inputPolyline[i], this.inputPolyline[i+1]);
            }
            return sum;
        }
    }
}