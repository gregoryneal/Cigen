using System.Collections.Generic;
using UnityEngine;

namespace Clothoid {

    /// <summary>
    /// A Posture is a method to generate target tangents and curvatures for a set of input nodes.
    /// For every subsequent three points a, b, c the posture position P = point b (except for the first
    /// and last posture, which use P = a and P = c respectively). The curvature is the radius of the 
    /// circle C that passes through all three points (it handles collinear points as well), the sign of which
    /// denotes the direction of curvature. The angle is defined by the angle made with the positive x-axis of
    /// P on C.
    /// </summary>
    public class Posture {

        public Vector3 Position => new Vector3(X, 0, Z);
        //public Vector3 Tangent => new Vector3(Mathf.Cos(Angle * Mathf.PI / 180), 0, Mathf.Sin(Angle * Mathf.PI / 180));
        public Vector3 Tangent { get; private set; }

        /// <summary>
        /// The point on the circumference of the circle represented by this posture.
        /// </summary>
        public float X { get; private set; }
        public float Z { get; private set; }

        private float _angle = float.NaN;

        /// <summary>
        /// The the tangent angle in degrees of the circle at point (x, z)
        /// </summary>
        public float Angle { get {
                if (float.IsNaN(_angle)) {
                    _angle = Mathf.Atan2(Z, X) * 180 / Mathf.PI;
                    while (_angle < 0) _angle += 360;
                }
                return _angle;
            }
        }
        public float Curvature { get; private set; }
        public Vector3 CircleCenter { get; private set; }
/*
        public Posture(float x, float z, float angle, float curvature, Vector3 circleCenter) {
            this.X = x;
            this.Z = z;
            this.Angle = angle;
            this.Curvature = curvature;
            this.CircleCenter = circleCenter;
        }
*/
        public Posture(float x, float z, float curvature, Vector3 tangent, Vector3 circleCenter) {
            this.X = x;
            this.Z = z;
            this.Curvature = curvature;
            this.Tangent = tangent;
            this.CircleCenter = circleCenter;
        }

        public override string ToString()
        {
            return $"position: ({X}, {Z}) | angle: {Angle} | curvature: {Curvature}";
        }

        public static List<Posture> CalculatePostures(List<Vector3> polyline) {
            List<Posture> postures = new List<Posture>();
            if (polyline.Count < 3) return postures;

            for (int i = 0; i < polyline.Count; i++) {
                if (i == 0) {
                    postures.Add(CalculatePosture(polyline[0], polyline[1], polyline[2], 1));
                    continue;
                }
                if (i == polyline.Count-1) {
                    postures.Add(CalculatePosture(polyline[^3], polyline[^2], polyline[^1], 3));
                    continue;
                }
                postures.Add(CalculatePosture(polyline[i-1], polyline[i], polyline[i+1]));
            }

            return postures;
        }

        /// <summary>
        /// Calulates a posture on point{I}, where point1 is the first point and point3 is the last point of the subsequence in the input polyline.
        /// The Posture is simply a list of useful properties of each point on the polyline, position, heading, curvature, etc. 
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="point3"></param>
        /// <param name="I">The index that should be considered the position of the posture, for tangent calculations.</param>
        /// <returns></returns>
        private static Posture CalculatePosture(Vector3 point1, Vector3 point2, Vector3 point3, int I = 2) {
            float x;
            float z;
            if (I == 1) {
                x = point1.x;
                z = point1.z;
            } else if (I == 2) {
                x = point2.x;
                z = point2.z;
            } else {
                x = point3.x;
                z = point3.z;
            }

            float curvature;
            Vector3 tangent;
            Vector3 circleCenter;
            bool areCollinear = Mathc.AreCollinearPoints(point1, point2, point3);
            bool foundCenter = false;
            if (areCollinear) {
                curvature = 0;
                tangent = point3 - point1;
                circleCenter = new Vector3(x, 0, z);
            } else {
                curvature = Mathc.MoretonSequinCurvature(point1, point2, point3);
                if (Mathc.CenterOfCircleOfThreePoints2(out Vector3 center, point1, point2, point3)) {
                    circleCenter = center;
                    //tangent slope is negative reciprocal of normal slope since they are orthogonal.
                    tangent = new Vector3(z - circleCenter.z, 0, -(x - circleCenter.x));
                    if (curvature > 0) tangent *= -1;

                } else {
                    //we shouldn't get here so we can use this as debug.
                    Debug.LogError("Warning, something is wrong with the center of the circle calculations!");
                    circleCenter = point1;
                    tangent = Vector3.zero;
                }
            }

            //angle = Mathf.Abs(angle);

            if (foundCenter) {
                //Debug.Log($"We found the center! point1: {point1} | point2: {point2} | point3: {point3} | areCollinear: {areCollinear} | curvature: {curvature} | circleCenter: {circleCenter} | angle: {angle}");
            }

            return new Posture(x, z, curvature, tangent, circleCenter);
        }

    }
}