using System;
using System.Collections.Generic;
using UnityEngine;

namespace Clothoid {
    public class ClothoidSegment {
        public static float CURVE_FACTOR = (float)System.Math.Sqrt(System.Math.PI/2);
        //if the curvature is less than this we will approximate it as a line segment.
        public static float MIN_CURVATURE_DIFF = 0.0015f;
        /// <summary>
        /// Arc length at the start, not necessarily 0. We could shift the represented segment along the curve.
        /// </summary>
        public float ArcLengthStart { get; protected set; }
        /// <summary>
        /// Arc length at the end of the segment.
        /// </summary>
        public float ArcLengthEnd { get; protected set; }

        public float TotalArcLength => ArcLengthEnd - ArcLengthStart;
        /// <summary>
        /// Curvature in radians at the start of the segment. Positive -> right hand turn, negative -> left hand turn.
        /// </summary>
        public float StartCurvature { get; protected set; }
        /// <summary>
        /// Curvature in radians at the end of the segment. Positive -> right hand turn, negative -> left hand turn.
        /// </summary>
        public float EndCurvature { get; protected set; }
        /// <summary>
        /// Scale factor for the entire curve, can be negative.
        /// </summary>
        public float B { get; protected set; }

        public float Sharpness => (EndCurvature - StartCurvature) / TotalArcLength;

        public Vector3 Offset { get; private set; }

        public float Rotation { get; private set; }

        public LineType LineType { get; private set; }


        /// <summary>
        /// This determines the clothoid segment parameters. All clothoids are calculated first in "standard" form. 
        /// That is, from the origin with the initial tangent positioned along the positive x axis. 
        /// To connect them we must apply a rotation, a mirror, and then a translation.
        /// </summary>
        /// <param name="arcLengthStart"></param>
        /// <param name="arcLengthEnd"></param>
        /// <param name="startCurvature"></param>
        /// <param name="endCurvature"></param>
        /// <param name="B"></param>
        public ClothoidSegment(float arcLengthStart, float arcLengthEnd, float startCurvature, float endCurvature, float B) {
            this.ArcLengthStart = arcLengthStart;
            this.ArcLengthEnd = arcLengthEnd;
            this.StartCurvature = startCurvature;
            this.EndCurvature = endCurvature;
            this.B = B;
            this.LineType = GetLineTypeFromCurvatureDiff(StartCurvature, EndCurvature);
            CalculateOffsetAndRotation();
        }

        public ClothoidSegment(float arcLengthStart, float arcLengthEnd, float startCurvature, float endCurvature) : this(arcLengthStart, arcLengthEnd, startCurvature, endCurvature, CalculateB(arcLengthStart, arcLengthEnd, startCurvature, endCurvature)) {}

        public ClothoidSegment(float totalArcLength, float startCurvature , float sharpness) : this(0, totalArcLength, startCurvature, (sharpness * totalArcLength) + startCurvature, CalculateB(0, totalArcLength, startCurvature, (sharpness * totalArcLength) + startCurvature)) {}

        /// <summary>
        /// Calcluate the scaling factor for the constrained SinghMcCrae clothoid segment.
        /// </summary>
        /// <returns></returns>
        public static float CalculateB(float arcLengthStart, float arcLengthEnd, float startCurvature, float endCurvature) {
            float arcLength = arcLengthEnd - arcLengthStart;
            float B = (float)System.Math.Sqrt(arcLength/(System.Math.PI * System.Math.Abs(endCurvature - startCurvature)));
            return B;
        }

        /// <summary>
        /// Sample this segment instance in local space (without offsets and rotations added).
        /// </summary>
        /// <param name="arcLength"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Vector3 SampleSegmentByTotalArcLength(float arcLength) {
            if (arcLength > ArcLengthEnd || arcLength < ArcLengthStart) throw new ArgumentOutOfRangeException();
            float interp = (arcLength - ArcLengthStart) / TotalArcLength;
            switch (this.LineType) {
                case LineType.LINE:
                    return new Vector3(arcLength - ArcLengthStart, 0, 0);
                case LineType.CIRCLE:
                    //we build the circle constructively to find the point and rotation angle.
                    //start with a positive or negative radius, and build a vector centered on the origin with the z value as the radius.
                    //rotate the point by the desired theta, positive theta rotates in the clockwise direction, negative values are ccw.
                    //now subtract the final z value by the radius (point.z -= radius) to get the final offset. 
                    float radius = -2f / (StartCurvature + EndCurvature); //this might be positive or negative
                    float circumference = 2 * Mathf.PI * radius;
                    float fullSweepAngle_deg = 360f * TotalArcLength / circumference;
                    float rotationAngle = interp * fullSweepAngle_deg; //same here, value in degrees
                    Vector3 vector = RotateAboutAxis(new Vector3(0, 0, radius), Vector3.up, rotationAngle);
                    return new Vector3(vector.x, vector.y, vector.z - radius);
                default: 
                    //clothoid
                    return this.SampleClothoid(interp);
            }
        }

        /// <summary>
        /// Shifts the start arc length to newStartArcLength, shifts the end arc length value to newStartArcLength + TotalArcLength of the segment at creation.
        /// </summary>
        /// <param name="newStartArcLength"></param>
        public void ShiftStartArcLength(float newStartArcLength) {
            float arcLength = TotalArcLength;
            this.ArcLengthStart = newStartArcLength;
            this.ArcLengthEnd = newStartArcLength + arcLength;
        }

        /// <summary>
        /// Sample this segment by a value constrained between 0 and 1.
        /// </summary>  
        /// <param name="value"></param>
        /// <returns></returns>
        public Vector3 SampleSegmentByRelativeLength(float value) {
            if (value < 0 || value > 1) throw new ArgumentOutOfRangeException();
            float totalArcLength = ArcLengthStart + (value * (ArcLengthEnd - ArcLengthStart));
            return SampleSegmentByTotalArcLength(totalArcLength);
        }

        /// <summary>
        /// Here we calculate the offset and rotation for the segment. All of the shapes can be thought of as being generated from the origin.
        /// Lines are generated in the positive X direction, circles with negative radius (Left turning) are generate in the positive z planar subsection,
        /// centered on the x axis. Clothoids are generated in the positive X direction, but can turn into the positive or negative z direction.
        /// 
        /// Rotations are calculated as follows: for lines there is no rotation, so it is 0. For circles it is calculated by the ratio 360deg * (arcLength / circumference).
        /// Clothoids are calculated with (EndCurvature - StartCurvature) * ArcLengthEnd * ArcLengthEnd / (2 * (ArcLengthEnd - ArcLengthStart)) 
        /// </summary>
        protected void CalculateOffsetAndRotation() {
            switch (this.LineType) {
                case LineType.LINE:
                    this.Offset = new Vector3(ArcLengthEnd - ArcLengthStart, 0, 0);
                    this.Rotation = 0;
                    break;
                case LineType.CIRCLE:
                    //we build the circle constructively to find the point and rotation angle.
                    //start with a positive or negative radius, and build a vector centered on the origin with the z value as the radius.
                    //rotate the point by the desired theta, positive theta rotates in the clockwise direction, negative values are ccw.
                    //now subtract the final z value by the radius (point.z -= radius) to get the final offset. 
                    float radius = 2f / (StartCurvature + EndCurvature); //this might be positive or negative

                    bool negativeCurvature = radius < 0;
                    float circumference = 2f * Mathf.PI * Mathf.Abs(radius);
                    float rotationAngle = TotalArcLength * 360f / circumference; //same here, value in degrees
                    Vector3 vector;

                    if (!negativeCurvature) {
                        vector = RotateAboutAxis(new Vector3(0, 0, radius), Vector3.up, rotationAngle);                        
                        //Debug.Log($"Circle offset before radius subtraction: {vector}");
                        vector = new Vector3(vector.x, vector.y, vector.z - radius);
                        this.Rotation = rotationAngle;
                        this.Offset = vector;
                    } else {
                        radius = Mathf.Abs(radius);
                        vector = RotateAboutAxis(new Vector3(0, 0, -radius), Vector3.up, -rotationAngle);
                        //Debug.Log($"Circle offset before radius addition: {vector}");
                        vector = new Vector3(vector.x, vector.y, vector.z + radius);
                        this.Rotation = -rotationAngle;
                        this.Offset = vector;
                    }

                    //Debug.Log($"Circle stats: radius: {radius}, negativeCurvature: {negativeCurvature}, circumference: {circumference}...");
                    //Debug.Log($"Circle stats cont: rotationAngle: {rotationAngle}, offset: {vector}");
                    break;
                default: 
                    //clothoid
                    //the angle is represented by the difference of squares of the scaled curvature parameters.
                    this.Offset = SampleClothoid(1);
                    float t1 = StartCurvature * B * CURVE_FACTOR;
                    float t2 = EndCurvature * B * CURVE_FACTOR;
                    if (t2 > t1) this.Rotation = - ((t2 * t2) - (t1 * t1)) * 180f / Mathf.PI;
                    else this.Rotation = - (((t1 * t1) - (t2 * t2)) * 180f / Mathf.PI);
                    break;
            }
        }

        /// <summary>
        /// Sample a clothoid segment given its arc length, curvature difference and an interpolations value
        /// </summary>
        /// <param name="arcLengthStart"></param>
        /// <param name="arcLengthEnd"></param>
        /// <param name="curvatureStart"></param>
        /// <param name="curvatureEnd"></param>
        /// <param name="interpolation"></param>
        /// <returns></returns>
        public static Vector3 SampleClothoidSegment(float arcLengthStart, float arcLengthEnd, float curvatureStart, float curvatureEnd, float interpolation) {
            float arcLength = arcLengthEnd - arcLengthStart;
            float curveDiff = curvatureEnd - curvatureStart;
            float B = Mathf.Sqrt(arcLength/(Mathf.PI * Mathf.Abs(curveDiff)));
            float t1 = curvatureStart * B * CURVE_FACTOR;
            float t2 = curvatureEnd * B * CURVE_FACTOR;
            float interpolatedT = t1 + (interpolation * (t2 - t1));
            Vector3 output = B * Mathf.PI * SampleClothoidSegment(t1, t2, interpolatedT);
            if (float.IsNaN(output.x) || float.IsNaN(output.y) || float.IsNaN(output.z)) {
                Debug.Log("NAN OUTPUT");
                Debug.Log($"arclengthstart: {arcLengthStart}");
                Debug.Log($"arcLegnthEnd: {arcLengthEnd}");
                Debug.Log($"curvatureStart: {curvatureStart}");
                Debug.Log($"curvatureEnd: {curvatureEnd}");
                Debug.Log($"interpolation: {interpolation}");
                Debug.Log($"curveDiff: {curveDiff}");
                Debug.Log($"B: {B}");
                Debug.Log($"t1: {t1}");
                Debug.Log($"t2: {t2}");                
                Debug.Log($"interpolatedT: {interpolatedT}");
                Debug.Log($"output: {output}");
            }
            return output;
        }

        /// <summary>
        /// Sample a clothoid between two curvature values
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vector3 SampleClothoidSegment(float t1, float t2, float t) {
            Vector3 point = new Vector3(Mathc.C(t), 0, Mathc.S(t)) - new Vector3(Mathc.C(t1), 0, Mathc.S(t1));
            if (t2 > t1) {
                point = RotateAboutAxis(point, Vector3.up, t1 * t1 * 180f / Mathf.PI);
            } else {
                point = RotateAboutAxis(point, Vector3.up, (t1 * t1 * 180f / Mathf.PI) + 180f);                
                point = new Vector3(point.x, point.y, -point.z);
            }
            return point / CURVE_FACTOR;
        }

        /// <summary>
        /// Sample the clothoid segment in local space, with an interpolation value between 0 and 1. 
        /// </summary>
        /// <param name="interpolation"></param>
        /// <returns></returns>
        public Vector3 SampleClothoid(float interpolation) {
            float t1 = StartCurvature * B * CURVE_FACTOR;
            float t2 = EndCurvature * B * CURVE_FACTOR;
            float t = t1 + (interpolation * (t2 - t1));
            return B * Mathf.PI * SampleClothoidSegment(t1, t2, t);
        }

        public static Vector3 RotateAboutAxis(Vector3 vector, Vector3 axis, float degrees) {
            return Quaternion.AngleAxis(degrees, axis) * vector;
        }
        
        /// <summary>
        /// Helper function to convert a list of N Vector3(arcLength, 0, curvature) into a list of N-1 ClothoidSegments
        /// </summary>
        /// <param name="lkNodes"></param>
        /// <returns></returns>
        public static List<ClothoidSegment> GenerateSegmentsFromLKGraph(List<Vector3> lkNodes) {
            List<ClothoidSegment> segments = new List<ClothoidSegment>();
            for (int segmentIndex = 0; segmentIndex+1 < lkNodes.Count; segmentIndex++) {
                float arcLengthStart = lkNodes[segmentIndex].x;
                float arcLengthEnd = lkNodes[segmentIndex+1].x;
                float startCurvature = lkNodes[segmentIndex].z;
                float endCurvature = lkNodes[segmentIndex+1].z;
                ClothoidSegment segment = new ClothoidSegment(arcLengthStart, arcLengthEnd, startCurvature, endCurvature);
                segments.Add(segment);            
                //Debug.Log(segment.ToString());
            }
            return segments;
        }

        /// <summary>
        /// The start and end curvature values uniquely determine the type of shape to be drawn, these are enumerated in the LineType object as the straight line segment,
        /// the circular arc segment, and the clothoid segment.
        /// </summary>
        /// <param name="startCurvature"></param>
        /// <param name="endCurvature"></param>
        /// <returns></returns>
        public static LineType GetLineTypeFromCurvatureDiff(float startCurvature, float endCurvature) {
            float curveDiff = endCurvature - startCurvature;
            if (Mathf.Abs(curveDiff) >= MIN_CURVATURE_DIFF) {
                return LineType.CLOTHOID;
            } else if (Mathf.Abs(curveDiff) < MIN_CURVATURE_DIFF && (Mathf.Abs(endCurvature) < MIN_CURVATURE_DIFF || Mathf.Abs(startCurvature) < MIN_CURVATURE_DIFF)) {
                return LineType.LINE;
            } else {
                return LineType.CIRCLE;
            }
        }

        public override string ToString()
        {
            return $"LineType {LineType} | ArcLengthStart {ArcLengthStart} | ArcLengthEnd {ArcLengthEnd} | StartCurvature {StartCurvature} | EndCurvature {EndCurvature} | B {B}";
        }

        public string Description() {
            string s = "";

            switch (this.LineType) {
                case LineType.LINE:
                    s += $"Line with length {TotalArcLength}";
                    break;
                case LineType.CIRCLE:
                    float radius = 2f / (StartCurvature + EndCurvature);
                    string b = radius < 0 ? "negative" : "positive";
                    s += $"Circle with a {b} radius of {Mathf.Abs(radius)}";
                    break;
                case LineType.CLOTHOID:
                    string c = StartCurvature >= 0 ? "positive" : "negative";
                    string d = EndCurvature >= 0 ? "positive" : "negative";
                    s += $"Clothoid with {c} start curvature and {d} end curvature";
                    break;
            }
            return s;
        }
    }
}