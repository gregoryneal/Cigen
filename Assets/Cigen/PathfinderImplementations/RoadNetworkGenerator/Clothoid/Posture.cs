using System.Collections.Generic;
using UnityEngine;

namespace Clothoid {
    public class Posture {

        public Vector3 Position => new Vector3(X, 0, Z);

        /// <summary>
        /// The point on the circumference of the circle represented by this posture.
        /// </summary>
        public float X { get; private set; }
        public float Z { get; private set; }

        /// <summary>
        /// The the tangent angle in degrees of the circle at point (x, z)
        /// </summary>
        public float Angle { get; private set; }
        public float Curvature { get; private set; }
        public Vector3 CircleCenter { get; private set; }

        public Posture(float x, float z, float angle, float curvature, Vector3 circleCenter) {
            this.X = x;
            this.Z = z;
            this.Angle = angle;
            this.Curvature = curvature;
            this.CircleCenter = circleCenter;
        }

        public override string ToString()
        {
            return $"position: ({X}, {Z}) | angle: {Angle} | curvature: {Curvature}";
        }

        public static List<Posture> CalculatePostures(List<Vector3> polyline) {
            List<Posture> postures = new List<Posture>();

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
            float angle;
            Vector3 circleCenter;
            bool areCollinear = Math.AreCollinearPoints(point1, point2, point3);
            bool foundCenter = false;
            if (areCollinear) {
                curvature = 0;
                angle = Mathf.Atan2(point3.z - point1.z, point3.x - point1.x) * 180f / Mathf.PI;
                circleCenter = new Vector3(x, 0, z);
            } else {
                curvature = Math.MoretonSequinCurvature(point1, point2, point3);
                foundCenter = Math.CenterOfCircleOfThreePoints2(out Vector3 center, point1, point2, point3);
                if (foundCenter) {
                    circleCenter = center;
                    angle = Mathf.Atan2(z - circleCenter.z, x - circleCenter.x) * 180f / Mathf.PI;

                } else {
                    //we shouldn't get here so we can use this as debug.
                    Debug.LogError("Warning, something is wrong with the center of the circle calculations!");
                    circleCenter = point1;
                    angle = 0;
                }
            }

            if (foundCenter) {
                Debug.Log($"We found the center! point1: {point1} | point2: {point2} | point3: {point3} | areCollinear: {areCollinear} | curvature: {curvature} | circleCenter: {circleCenter} | angle: {angle}");
            }

            return new Posture(x, z, angle, curvature, circleCenter);
        }

    }
}