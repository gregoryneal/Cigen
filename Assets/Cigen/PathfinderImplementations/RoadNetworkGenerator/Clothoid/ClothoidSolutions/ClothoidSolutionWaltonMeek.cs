using System;
using System.Collections.Generic;
using System.Text;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;

namespace Clothoid {
    public class ClothoidSolutionWaltonMeek : ClothoidSolution
    {
        private float W { get; set; }

        private List<Posture> postures;

        public override ClothoidCurve CalculateClothoidCurve(List<Vector3> inputPolyline, float allowableError = 0.1f, float endpointWeight = 1)
        {
            this.postures = Posture.CalculatePostures(inputPolyline);
            Vector3 R;
            Vector3 startPosition;
            Vector3 endPosition;
            Vector3 endTangent;
            float W;
            bool isMirrored;
            Posture sp;
            Posture ep;
            for (int i = 0; i + 1 < postures.Count; i++) {
                //postures only record the total offsets, we need each pair to be shifted to where the position of the first one is at the origin,
                //and then we need to rotate it so that the initial tangent is aligned with the positive x axis.
                //this solution is only valid when the final tangent angle is less than 180 degrees. Therefore we also need to possibly mirror the
                //solution along the x axis if after shifting and rotating the final point has a negative z value. Once the calculation is finished
                //we can mirror -> rotate -> translate to realign it with the rest of the curve. But the clothoid segment object handles that for us, 
                //so we just need to add clothoid segments in standard position.
                sp = postures[i];
                ep = postures[i+1];
                //shift the end position by start position
                startPosition = sp.Position - sp.Position;
                endPosition = ep.Position - sp.Position;
                //rotate the end tangent by the start tangent angle
                endTangent = ClothoidSegment.RotateAboutAxis(ep.Tangent, Vector3.up, -sp.Angle);
                float endAngle;

                //check if the points can be defined with circular segments or line segments. The clothoid segment object can handle these
                //with just the start and end curvature values so we just check here.
                switch (ClothoidSegment.GetLineTypeFromCurvatureDiff(sp.Curvature, ep.Curvature)) {
                    case LineType.LINE:
                    break;
                    case LineType.CIRCLE:
                    break;
                    case LineType.CLOTHOID:                
                        //check if the postures in standard form are going into the the negative z subplane. if so flag it and mirror them.
                        if (startPosition.z > endPosition.z) {
                            endPosition = new Vector3(endPosition.x, endPosition.y, -endPosition.z);
                            endTangent = new Vector3(endTangent.x, endTangent.y, -endTangent.z);
                            isMirrored = true;
                        } else {
                            isMirrored = false;
                        }

                        endAngle = Mathf.Atan2(endTangent.z, endTangent.x);
                        R = CalculateR(endAngle, ep.Curvature);

                        if (IsInGamma(endPosition, R, sp.Curvature, ep.Curvature)) {

                        }
                    break;
                }
            }
            throw new NotImplementedException();
        }

        public override List<Vector3> GetFitSamples(int numSaples)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This returns parameters for the gamma region. 
        /// If startCurvature = 0: 
        /// Item1: slope from origin to point R
        /// Item2: y value of point R
        /// Item3: Vector3.zero
        /// 
        /// If startCurvature > 0:
        /// Item1: slope from origin to point R
        /// Item2: radius of bounding circle
        /// Item3: center of bounding circle
        /// </summary>
        /// <param name="startCurvature"></param>
        /// <param name="endCurvature"></param>
        /// <param name="endTangent">the tangent in radians</param>
        /// <returns></returns>
        public static (float, float, Vector3) GetGammaRegionParameters(float startCurvature, float endCurvature, float endTangent) {
            Vector3 R = CalculateR(endTangent, endCurvature);
            float slope = R.z / R.x;
            float radius;
            Vector3 center;
            if (startCurvature > 0) {
                radius = (1 / startCurvature) - (1 / endCurvature);
                center = new Vector3(R.x, 0, R.z + (1 / startCurvature) - (1 / endCurvature));
            } else {
                radius = R.z;
                center = Vector3.zero;
            }

            return (slope, radius, center);
        }

        public static float CalculateW(float endTangent) {
            return endTangent * 2 / Mathf.PI;
        }

        public static Vector3 CalculateR(float endTangent, float endCurvature) {
            float x = (float)Math.Sin(endTangent);
            float z = 1 - (float)Math.Cos(endTangent);
            //Debug.Log($"X: {x} | Z: {z}");
            return new Vector3(x, 0, z) / endCurvature;
        }

        
        /// <summary>
        /// Determines which side a point falls in relation to a curve.
        /// </summary>
        /// <returns>True: point lies on the convex side of the curve.
        /// False: point lies on the concave side of the curve.</returns>
        private bool WhichSide() {
            return true;
        }

        public static bool IsInGamma(Vector3 point, float startCurvature, float endCurvature, float endTangent) {
            Vector3 R = CalculateR(endTangent, endCurvature);
            return IsInGamma(point, R, startCurvature, endCurvature);
        }

        /// <summary>
        /// Checks if the point is in an acceptable region where we can find a solution.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static bool IsInGamma(Vector3 point, Vector3 R, float startCurvature, float endCurvature) {
            // check if point.z > R.z, the x value is a bit more complicated.
            // Draw a ray from the origin that passes through R (R is in quadrant 1)
            // if our point.x > ray.x at point.z then we are in bounds.
            if (point.z < R.z) {
                /*Debug.Log("point.z < R.z");
                Debug.Log($"R: {R}");
                Debug.Log($"point: {point}");*/
                return false;
            }
            // y = mx + b where b = 0, m = R.y/R.x => x = y / m
            //check if slope of Q is greater than slope of R
            if (point.z / point.x >= R.z / R.x) {                
                /*Debug.Log("point.x <= point.z * R.x / R.y");
                Debug.Log($"R: {R}");
                Debug.Log($"point: {point}");*/
                return false;
            }

            if (startCurvature > 0) {
                // Additionally check if we are in the defined circle
                float radius = (1 / startCurvature) - (1 / endCurvature);
                Vector3 center = new Vector3(R.x, 0, (1 / startCurvature) - R.z);
                float a = point.x - center.x;
                float b = point.z - center.z;
                if ((a * a) + (b * b) >= radius * radius) {
                    /*Debug.Log("(a * a) + (b * b) >= radius * radius");
                    Debug.Log($"R: {R}");
                    Debug.Log($"point: {point}");
                    Debug.Log($"center: {center}");
                    Debug.Log($"radius: {radius}");
                    Debug.Log($"a: {a}");
                    Debug.Log($"b: {b}");*/
                    return false;
                }
            }

            return true;
        }

        public static List<Vector3> CalculateDCurve(float W, float Kp, float Kq) {
            
            List<Vector3> points = new List<Vector3>();
            int numSamples = 250;
            float Kq2 = Kq * Kq;
            float Kp2 = Kp * Kp;
            float B;
            float KqB;
            float KpB;
            float omega;
            if (Kp > 0) {
                for (float t = 0; t < W; t += W / numSamples) {
                    B = CalculateB(W, t, Kq2, Kp2);
                    KqB = Kq * B;
                    KpB = Kp * B;
                    omega = - (KpB * KpB);
                    points.Add(SampleD(t, B, W, omega, Kq, KqB, KpB));
                }
                //add last point manually
                B = CalculateB(W, W, Kq2, Kp2);
                KqB = Kq * B;
                KpB = Kp * B;
                omega = - (KpB * KpB);
                points.Add(SampleD(W, B, W, omega, Kq, KqB, KpB));
            } else {
                Vector3 R = CalculateR(W * Mathf.PI / 2, Kq);
                for (float t = 0; t < W; t += W / numSamples) {
                    points.Add(SampleD2(t, W, Kq, R));
                }                
                //add last point manually
                points.Add(SampleD2(W, W, Kq, R));
            }

            return points;
        }

        public static List<Vector3> CalculateECurve(float W, float Kp, float Kq) {
            float omega;
            int numPoints = 250;
            List<Vector3> points = new List<Vector3>();
            float B;
            Vector3 a;
            Vector3 b;
            Vector3 S;
            if (Kp > 0) {
                for (float t = 0; t < W; t += W / numPoints) {
                    B = CalculateB(W, t, Kq * Kq, Kp * Kp);
                    omega = t - (Kp * B * Kp * B);
                    a = GammaProduct(omega, Kp * B, Kq * B, B);
                    b = new Vector3(Mathf.Sin(Mathf.PI * t / 2), 0, 1 - Mathf.Cos(Mathf.PI * t / 2)) / Kp;
                    points.Add(a + b);
                }
                B = CalculateB(W, W, Kq * Kq, Kp * Kp);
                omega = W - (Kp * B * Kp * B);
                a = GammaProduct(omega, Kp * B, Kq * B, B);
                b = new Vector3(Mathf.Sin(Mathf.PI * W / 2), 0, 1 - Mathf.Cos(Mathf.PI * W / 2)) / Kp;
                points.Add(a + b);

            } else {
                S = SampleD2(0, W, Kq, CalculateR(W * Mathf.PI / 2, Kq));
                points.Add(S);
                points.Add(S + (Vector3.right * 100));
            }
            return points;
        }

        //TODO: Return D(t) for Kp > 0
        /// <summary>
        /// Sample D(t) when Kp > 0
        /// </summary>
        /// <param name="t"></param>
        /// <param name="B"></param>
        /// <param name="W"></param>
        /// <param name="omega"></param>
        /// <param name="Kq"></param>
        /// <param name="KqB"></param>
        /// <param name="KpB"></param>
        /// <returns></returns>
        private static Vector3 SampleD(float t, float B, float W, float omega, float Kq, float KqB, float KpB) {
            float x = Mathf.PI * W / 2;
            float y = Mathf.PI * (W - t) / 2;
            float vx = -Mathf.Sin(x) + Mathf.Sin(y);
            float vz = Mathf.Cos(x) - Mathf.Cos(y);            
            Vector3 a = GammaProduct(omega, KpB, KqB, B);            
            return new Vector3(a.x - (vx / Kq), 0, a.z - (vz / Kq));
        }

        /// <summary>
        /// Sample D when Kp = 0
        /// </summary>
        /// <param name="t"></param>
        /// <param name="W"></param>
        /// <param name="Kq"></param>
        /// <returns></returns>
        private static Vector3 SampleD2(float t, float W, float Kq, Vector3 R) {
            return R + new Vector3(Mathc.C(Mathf.Sqrt(W - t)), 0, Mathc.S(Mathf.Sqrt(W - t))) * Mathf.PI / Kq;
        }

        private bool IsInGammaA(Vector3 point, Vector3 R, float W, float startCurvature, float endCurvature) {
            return false;
        }

        private bool IsInGammaB(Vector3 point, Vector3 R, float W, float startCurvature, float endCurvature) {
            return false;
        }

        private static double[][] RotationMatrix(float omega) {
            float w = Mathf.PI * omega / 2;
            return new double[2][] {
                new double[] {Mathf.Cos(w), -Mathf.Sin(w)},
                new double[] {Mathf.Sin(w), Mathf.Cos(w)}
            };
        }

        private static double[][] Vector(float KpB, float KqB) {
            return new double[2][] {
                new double[] {Mathc.C(KqB) - Mathc.C(KpB)},
                new double[] {Mathc.S(KqB) - Mathc.S(KpB)}
            };
        }

        public static Vector3 GammaProduct(float omega, float KpB, float KqB, float B) {            
            double[][] a = Clothoid.Mathc.SVDJacobiProgram.MatProduct(RotationMatrix(omega), Vector(KpB, KqB));
            float ax = (float)a[0][0];
            float az = (float)a[1][0];
            return B * Mathf.PI * new Vector3(ax, 0, az);
        }

        private static float CalculateB(float W, float t, float Kq2, float Kp2) {
            return Mathf.Sqrt((W - t) / (Kq2 - Kp2));
        }
    }
}