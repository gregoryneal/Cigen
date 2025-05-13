using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace Clothoid {

    /// <summary>
    /// A class that holds a sequence of ClothoidSegments and can calculate any position given an arc length (optionally parameterized to the range [0f,1f])
    /// </summary>
    public class ClothoidCurve {

        /// <summary>
        /// Overall Offset vector for the curve. If the curve passes through all the polyline nodes, this should be the position of the first node.
        /// </summary>
        public Vector3 Offset = Vector3.zero;
        /// <summary>
        /// Overall angle offset of the curve. This should be close to if not equal to the first Posture's tangent angle. 
        /// </summary>
        public float AngleOffset = 0;

        /// <summary>
        /// Number of segments in this curve. 
        /// </summary>
        public int Count { get { return segments.Count; }}

        public ClothoidSegment this[int index] {
            get => this.segments[index];
        }

        public Vector3 Endpoint => SampleCurveFromArcLength(TotalArcLength);

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

        /// <summary>
        /// Create a clothoid curve with just a curve factor.
        /// This curve factor is used to scale down the curve before sampling the fresnel integrals. 
        /// Then the point is scaled back up by the same factor.
        /// As far as I know it is only used in the SinghMcCrae solution.
        /// </summary>
        /// <param name="CURVE_FACTOR"></param>
        public ClothoidCurve(float CURVE_FACTOR=1) {
            ClothoidSegment.CURVE_FACTOR = (float)System.Math.Sqrt(System.Math.PI/2);//CURVE_FACTOR;
            this.segments = new List<ClothoidSegment>();
            this.inputPolyline = new List<Vector3>();
        }
        public ClothoidCurve(List<ClothoidSegment> orderedSegments, List<Vector3> inputPolyline, float CURVE_FACTOR=1) {
            ClothoidSegment.CURVE_FACTOR = (float)System.Math.Sqrt(System.Math.PI/2);//CURVE_FACTOR;
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
            Clothoid.Mathc.SVDJacobiProgram.MatShow(bestRotate, 2, 4);
        }

        public ClothoidCurve AddSegment(ClothoidSegment newSegment) {
            if (segments.Count > 0) newSegment.ShiftStartArcLength(segments[^1].ArcLengthEnd);
            else newSegment.ShiftStartArcLength(0);
            this.segments.Add(newSegment);
            return this;
        }

        public ClothoidCurve AddSegments(params ClothoidSegment[] segments) {
            for (int i = 0; i < segments.Length; i++) {
                AddSegment(segments[i]);
            }
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
                    //Debug.Log($"Rotate sampled segment by {rotation}");
                    value += offset;
                    break;
                }
                offset += ClothoidSegment.RotateAboutAxis(segment.Offset, Vector3.up, rotation);
                rotation -= segment.Rotation; //apply rotation last since rotation is applied around the origin
                //Debug.Log($"New offset and rotation along curve: {offset}, {rotation}");
            }
            value = ClothoidSegment.RotateAboutAxis(value, Vector3.up, -AngleOffset);
            //Debug.Log($"Rotate curve by {-AngleOffset}");
            value += Offset;
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
            //Debug.Log($"Increment size: {increment}, totalArcLength: {TotalArcLength}, numSamples: {numSamples}");
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
            double[][] rotatedPoint = Clothoid.Mathc.SVDJacobiProgram.MatProduct(rotationMatrix, point);
            return new Vector3((float)rotatedPoint[0][0], (float)rotatedPoint[1][0], (float)rotatedPoint[2][0]);
        }


        /// <summary>
        /// Create some test segments and display them.
        /// </summary>
        /// <returns></returns>
        public static IEnumerator<List<Vector3>> TestAllConnectionTypes(int a=0, int b=10, int c=15, int d=20, int e=35, int f=49) {
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

        /// <summary>
        /// Add a random curve using the sharpness constructor
        /// </summary>
        /// <returns></returns>
        public ClothoidCurve AddRandomSegment2() {
            float sharpness;
            float startCurvature;
            float newArcLength = UnityEngine.Random.Range(5, 9);
            float shape = 0.5f;// UnityEngine.Random.value;

            if (segments.Count > 0) { 
                ClothoidSegment lastSegment = segments[^1];

                if (shape > .66f) {
                    //line
                    sharpness = 0;
                    startCurvature = 0;
                } else if (shape < .33f) {
                    //circle
                    sharpness = 0;
                    startCurvature = lastSegment.EndCurvature;
                } else {
                    //clothoid
                    sharpness = UnityEngine.Random.Range(-.03f,.03f);
                    startCurvature = lastSegment.EndCurvature;
                }
                ClothoidSegment newSegment = new ClothoidSegment(startCurvature, sharpness, newArcLength);
                return AddSegment(newSegment);
            } else {
                if (shape > .66f) {
                    //line
                    sharpness = 0;
                    startCurvature = 0;
                } else if (shape < .33f) {
                    //circle
                    sharpness = 0;
                    startCurvature = UnityEngine.Random.Range(-.5f, .5f);
                } else {
                    //clothoid
                    sharpness = UnityEngine.Random.Range(-.03f, 03f);
                    startCurvature = UnityEngine.Random.Range(-.3f, .3f);
                }
                ClothoidSegment newSegment = new ClothoidSegment(startCurvature, sharpness, newArcLength);
                Debug.Log($"new parameters: sharpness: {sharpness}, arcLength: {newArcLength}, startcurvature: {startCurvature}");
                return AddSegment(newSegment);
            }
        }

        /// <summary>
        /// Add 3 random segments: a line segment followed by a clothoid transition to a circlar arc segment.
        /// </summary>
        /// <returns></returns>
        public ClothoidCurve AddRandomSegment3() {
            float lineLength = UnityEngine.Random.Range(5, 10);
            float clothoidLength = UnityEngine.Random.Range(5, 10);
            float arcLength = UnityEngine.Random.Range(5, 10);
            float sharpness = UnityEngine.Random.Range(-.008f, .008f);
            float curvature = UnityEngine.Random.Range(-.08f, .08f);

            ClothoidSegment lineSegment = new ClothoidSegment(0, 0, lineLength);
            ClothoidSegment clothoidSegment = new ClothoidSegment(0, sharpness, clothoidLength);
            ClothoidSegment circularSegment = new ClothoidSegment(clothoidSegment.EndCurvature, 0, arcLength);
            return AddSegments(lineSegment, clothoidSegment, circularSegment);
        }

        public ClothoidCurve AddRandomSegment4() {
            float arcLength = UnityEngine.Random.Range(5, 10);
            float curvature = UnityEngine.Random.Range(-.08f, .08f);

            ClothoidSegment circularSegment = new ClothoidSegment(curvature, 0, arcLength);
            return AddSegments(circularSegment);
        }

        public static ClothoidCurve GetRandomCurve() {
            ClothoidCurve c = new ClothoidCurve();
            float sharpness;
            float startCurvature;
            float newArcLength = UnityEngine.Random.Range(5, 9);
            float shape = 0.5f;// UnityEngine.Random.value;
            if (shape > .66f) {
                //line
                sharpness = 0;
                startCurvature = 0;
            } else if (shape < .33f) {
                //circle
                sharpness = 0;
                startCurvature = UnityEngine.Random.Range(-.5f, .5f);
            } else {
                //clothoid
                sharpness = UnityEngine.Random.Range(-.03f,.03f);
                startCurvature = UnityEngine.Random.Range(-.3f, .3f);
            }
            ClothoidSegment newSegment = new ClothoidSegment(startCurvature, sharpness, newArcLength);
            Debug.Log($"new parameters: sharpness: {sharpness}, arcLength: {newArcLength}, startcurvature: {startCurvature}");
            return c.AddSegment(newSegment);
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

        /// <summary>
        /// Add two clothoid curves together. This operation is not commutative, the order matters. The end curve will be added on to the start curve.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static ClothoidCurve operator +(ClothoidCurve start, ClothoidCurve end) {
            ClothoidCurve result = new ClothoidCurve();
            //set the offsets the same as the start offset
            result.Offset = start.Offset;
            result.AngleOffset = start.AngleOffset;
            // Add the segments one at a time
            result.AddSegments(start.segments.ToArray())
            .AddSegments(end.segments.ToArray());/*
            for (int i = 0; i < start.segments.Count; i++) {
                result.AddSegment(start.segments[i]);
            }
            for (int i = 0; i < end.segments.Count; i++) {
                result.AddSegment(end.segments[i]);
            }*/

            return result;
        }

        public static ClothoidCurve operator +(ClothoidCurve start, ClothoidSegment newSegment) {
            ClothoidCurve result = new ClothoidCurve();
            result.Offset = start.Offset;
            result.AngleOffset = start.AngleOffset;
            result.AddSegments(start.segments.ToArray()).AddSegment(newSegment);
            return result;
        }

        public override string ToString()
        {
            string s = "";
            for (int i = 0; i < segments.Count; i++) {
                s += segments[i].Description() + ", ";
            }
            return s;
        }

        /// <summary>
        /// Return a three segment curve in local space. Start point is at the origin and start tangent is along the +X-axis.
        /// </summary>
        /// <param name="sharpness"></param>
        /// <param name="arcLength1"></param>
        /// <param name="arcLength2"></param>
        /// <param name="arcLength3"></param>
        /// <param name="startCurvature"></param>
        /// <param name="endCurvature"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static ClothoidCurve ThreeSegmentsLocal(float startCurvature, float sharpness, float arcLength1, float arcLength2, float arcLength3) {
            ClothoidCurve c = new ClothoidCurve();
            ClothoidSegment s1 = new ClothoidSegment(startCurvature, sharpness, arcLength1);
            ClothoidSegment s2 = new ClothoidSegment(s1.EndCurvature, -sharpness, arcLength2);
            ClothoidSegment s3 = new ClothoidSegment(s2.EndCurvature, sharpness, arcLength3);
            return c.AddSegments(s1, s2, s3);
            //return c.AddSegment(s1);
        }
    }
}