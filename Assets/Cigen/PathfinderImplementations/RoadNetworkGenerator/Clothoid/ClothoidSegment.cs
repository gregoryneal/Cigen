using System;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.IO.Archive;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Splines.Interpolators;
using UnityEngine.UIElements;

namespace Clothoid {
    public abstract class ClothoidSegment {
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

        List<Vector3> drawingNodes;


        public LineType LineType { get; private set; }


        public ClothoidSegment(float arcLengthStart, float arcLengthEnd, float startCurvature, float endCurvature, float B) {
            this.ArcLengthStart = arcLengthStart;
            this.ArcLengthEnd = arcLengthEnd;
            this.StartCurvature = startCurvature;
            this.EndCurvature = endCurvature;
            this.B = B;
            this.LineType = GetLineTypeFromCurvatureDiff(StartCurvature, EndCurvature);
            CalculateDrawingNodes();
        }       

        /// <summary>
        /// Calculate an offset at some arcLength t. T is the total arc length, not parametrized.
        /// </summary>
        /// <param name="arcLength"></param>
        /// <returns></returns>
        public Vector3 SamplePointByArcLength(float arcLength) {
            if (arcLength < ArcLengthStart || arcLength > ArcLengthEnd) throw new ArgumentOutOfRangeException("Arc length does not fall within this clothoid segment!");
            
            //the interpolated value from 0 to 1 of how far along the arc we are for this segment
            //this allows us to find the curvature by interpolating between t1 and t2 by this amount.
            //we have to do this because clothoids vary curvature linearly. 
            float interp = (arcLength-ArcLengthStart) / (ArcLengthEnd-ArcLengthStart);
            Vector3 vec;
            float totalArcLength = ArcLengthEnd - ArcLengthStart;

            switch (this.LineType) {
                case LineType.LINE:
                    Vector3 v1 = new Vector3(Math.C(ArcLengthStart), 0, Math.S(ArcLengthStart));
                    Vector3 v2 = new Vector3(Math.C(ArcLengthEnd), 0, Math.S(ArcLengthEnd));
                    vec = Vector3.Lerp(v1, v2, interp);
                    break;
                case LineType.CLOTHOID:
                    float t1 = StartCurvature * B * CURVE_FACTOR;
                    float t2 = EndCurvature * B * CURVE_FACTOR;
                    float b = Mathf.Sqrt(totalArcLength/(Mathf.PI * Mathf.Abs(EndCurvature - StartCurvature)));
                    float realT = t1 + (interp * (t2 - t1));
                    vec = Mathf.PI * b * GetClothoidPiecePoint(t1, t2, realT);
                    break;
                default:
                    //approximate the segment as a circle segment since the curvature difference is less than the min, but one of the endpoint curvatures is greater than the minimum
                    float radius = 2f/(StartCurvature+EndCurvature);
                    bool negativeCurvature;

                    if (radius < 0) {
                        negativeCurvature = true;
                        radius = Mathf.Abs(radius);
                    } else {
                        negativeCurvature = false;
                    }

                    float circumference = 2 * Mathf.PI * radius;
                    float anglesweep_rad = (totalArcLength * Mathf.PI * 2) / circumference;

                    if (negativeCurvature == false) {
                        vec = new Vector3(0, 0, -radius);
                        vec = RotateAboutAxis(vec, Vector3.up, -interp*anglesweep_rad*180f/Mathf.PI);
                        vec = new Vector3(vec.x, vec.y, vec.z + radius);
                    } else {
                        vec = new Vector3(0, 0, radius);
                        vec = RotateAboutAxis(vec, Vector3.up, interp*anglesweep_rad*180f/Mathf.PI);
                        vec = new Vector3(vec.x, vec.y, vec.z - radius);
                    }
                    break;
            }

            //NOTE: this vec needs to be translated and rotated to fit the segment in the world
            return vec;
        }

        /// <summary>
        /// Sample a clothoid between two curvature values
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vector3 GetClothoidPiecePoint(float t1, float t2, float t) {
            Vector3 point = new Vector3(Math.C(t), 0, Math.S(t));
            point -= new Vector3(Math.C(t1), 0, Math.S(t1));
            if (t2 > t1) {;
                point = RotateAboutAxis(point, Vector3.up, t1 * t1 * 180f / Mathf.PI);
            } else {
                point = RotateAboutAxis(point, Vector3.up, (t1 * t1 * 180f / Mathf.PI) + 180f);
                point = new Vector3(point.x, point.y, -point.z);
            }
            return point / CURVE_FACTOR;            
        }

        public static Vector3 RotateAboutAxis(Vector3 vector, Vector3 axis, float degrees) {
            return Quaternion.AngleAxis(degrees, axis) * vector;
        }

        /// <summary>
        /// Calculate nodes used to draw the segment into the world. Note you need to somehow line these values up with the input polyline if thats how they were generated.
        /// </summary>
        /// <param name="subdivisions"></param>
        /// <returns></returns>
        public List<Vector3> CalculateDrawingNodes(int subdivisions = 40) {
            if (this.drawingNodes == null || this.drawingNodes.Count != subdivisions) {
                List<Vector3> positions = new List<Vector3>();
                float arcLength = this.ArcLengthEnd - this.ArcLengthStart;
                for (int i = 0; i < subdivisions; i++) {
                    float currArcLength = this.ArcLengthStart + (i * arcLength / (subdivisions-1));
                    positions.Add(this.SamplePointByArcLength(currArcLength));
                }
                this.drawingNodes = positions;
                return positions;
            } else {
                return this.drawingNodes;
            }
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
                ClothoidSegmentSinghMcCrae segment = new ClothoidSegmentSinghMcCrae(arcLengthStart, arcLengthEnd, startCurvature, endCurvature);
                segments.Add(segment);            
                //Debug.Log(segment.ToString());
            }
            return segments;
        }

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
            return $"als {ArcLengthStart} | ale {ArcLengthEnd} | cs {StartCurvature} | ec {EndCurvature} | B {B}";
        }
    }
}