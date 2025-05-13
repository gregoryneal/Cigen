using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Clothoid {

    public static class Mathc {
        
        /// <summary>
        /// This is the Fresnel Cosine Integral approximation, as given by:
        /// 
        /// HEALD M. A.: Rational approximations for the
        /// fresnel integrals. Mathematics of Computation 44, 170 (1985), 459–461.
        ///
        /// </summary>
        /// <param name="arcLength"></param>
        /// <returns></returns>
        public static float C(float arcLength) {
            arcLength /= ClothoidSegment.CURVE_FACTOR;
            int scalar = 1;
            if (arcLength < 0) {
                scalar = -1;
                arcLength *= -1;
            }
            float val = .5f - (R(arcLength)*(float)System.Math.Sin(System.Math.PI * 0.5f * (A(arcLength) - (arcLength * arcLength))));
            return scalar * val * ClothoidSegment.CURVE_FACTOR;
        }

        /// <summary>
        /// The Fresnel Sine Integral approximation, given by HEALD M. A.
        /// </summary>
        /// <param name="arcLength"></param>
        /// <returns></returns>
        public static float S(float arcLength) {
            arcLength /= ClothoidSegment.CURVE_FACTOR;
            int scalar = 1;
            if (arcLength < 0) {
                scalar = -1;
                arcLength *= -1;
            }
            float val = 0.5f - (R(arcLength)*(float)System.Math.Cos(System.Math.PI * 0.5f * (A(arcLength) - (arcLength * arcLength))));
            return scalar * val * ClothoidSegment.CURVE_FACTOR;
        }

        /// <summary>
        /// Helper function used by the Fresnel Integral approximations
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static float R(float t) {
            float num = (.506f*t) + 1;
            float denom = (1.79f*t*t) + (2.054f*t) + (float)System.Math.Sqrt(2);
            return num / denom;
        }

        /// <summary>
        /// Helper function used by the Fresnel Integral approximations.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static float A(float t) {
            return 1f / ((.803f*t*t*t) + (1.886f*t*t) + (2.524f*t) + 2);
        }

        /// <summary>
        /// Approximate the curvature of the circum-circle that passes through all three points.
        /// If they are collinear, return 0, if any of the points are coincident, also return 0.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="point3"></param>
        /// <returns>A positive only value curvature.</returns>
        public static float MengerCurvature(Vector3 point1, Vector3 point2, Vector3 point3) {
            if (point1 == point2 || point2 == point3 || point1 == point3) return 0;
            float a = Vector3.Distance(point1, point2);
            float b = Vector3.Distance(point2, point3);
            float c = Vector3.Distance(point1, point3);
            float s = (a + b + c)/2; //half perimeter
            float area = Mathf.Sqrt(s * (s - a) * (s - b) * (s - c)); //herons forumula
            if (area == 0) return 0; //collinear points
            float numerator = 4 * area;
            float denominator = a * b * c;
            return numerator / denominator;
        }

        /// <summary>
        /// Discrete curvature estimation given by Moreton and Sequin in 1992.
        /// This one is the most useful since is distinguishes between positive and 
        /// negative curvature. Positive curvature corresponds to a left turn, and
        /// negative curvature corresponds to a right turn.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="point3"></param>
        /// <returns></returns>
        public static float MoretonSequinCurvature(Vector3 point1, Vector3 point2, Vector3 point3) {
            return (float)MoretonSequinCurvature(point1.ToCSVector3(), point2.ToCSVector3(), point3.ToCSVector3());
            /*
            Vector3 v1 = point2 - point1;
            Vector3 v2 = point3 - point2;
            float det = (v1.x*v2.z) - (v2.x*v1.z);
            return 2 * det / (v1.magnitude * v2.magnitude * (v1 + v2).magnitude);*/
        }

        public static double MoretonSequinCurvature(System.Numerics.Vector3 point1, System.Numerics.Vector3 point2, System.Numerics.Vector3 point3) {
            System.Numerics.Vector3 v1 = point2 - point1;
            System.Numerics.Vector3 v2 = point3 - point2;
            double det = (v1.X*v2.Z) - (v2.X*v1.Z);
            return 2 * det / (v1.Length() * v2.Length() * (v1 + v2).Length());
        }

        /// <summary>
        /// Basically the same as the absolute value of the MoretonSequinCurvature()
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="point3"></param>
        /// <returns></returns>
        public static float FrenetSerretCurvature(Vector3 point1, Vector3 point2, Vector3 point3) {
            Vector3 v1 = point2 - point1;
            Vector3 v2 = point3 - point2;
            float mag1 = v1.magnitude;
            float mag2 = v2.magnitude;
            if (mag1 == 0 || mag2 == 0) return 0;
            float theta = Mathf.Acos(Vector3.Dot(v1.normalized, v2.normalized));
            return 2*Mathf.Sin(theta/2) / Mathf.Sqrt(mag1 * mag2);
        }

        /// <summary>
        /// Some curvature calculation that I found on arxiv in the middle of the night, it doesn't work.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="point3"></param>
        /// <returns></returns>
        public static float RandomArxivCurvature(Vector3 point1, Vector3 point2, Vector3 point3) {
            Vector3 v1 = point2 - point1;
            Vector3 v2 = point3 - point2;
            return Mathf.PI - (Vector3.Angle(v1, v2) * Mathf.PI / 180f);
        }

        /// <summary>
        /// Approximate any single valued integral using the box method and some number of boxes.
        /// This is here because I tried to approximate the actual Fresnel Integrals at first since they are
        /// single valued and simple, but the above approximation is way more accurate and computationally less
        /// expensive. This is here to remind myself how simple some things really are. 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="function"></param>
        /// <param name="resolution">The number of boxes. The higher this number is the more accurate the approximation is.</param>
        /// <returns></returns>
        public static float IntegralApproximation(float from, float to, Func<float, float> function, int resolution = 20) {
            float integral = 0;
            float dx = (from-to)/resolution;
            for (int i = 0; i < resolution; i++) {
                float x = i * dx;
                integral += function(x) * dx;
            }
            return integral;
        }

        /// <summary>
        /// Checks if the three provided points are collinear in the XZ plane.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="point3"></param>
        /// <returns></returns>
        public static bool AreCollinearPoints(Vector3 point1, Vector3 point2, Vector3 point3, float minError = 0.01f) {
            return Mathf.Abs(((point2.z - point1.z) / (point2.x - point1.x)) - ((point3.z - point2.z) / (point3.x - point2.x))) < minError;
        }

/*
        /// <summary>
        /// Given three points in the XZ plane, find the center of the circle that passes through all of them.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="point3"></param>
        /// <returns></returns>
        public static Vector3 CenterOfCircleOfThreePoints(Vector3 point1, Vector3 point2, Vector3 point3) {
            float zDelta_a = point2.z - point1.z;
            float xDelta_a = point2.x - point1.x;
            float zDelta_b = point3.z - point2.z;
            float xDelta_b = point3.x - point2.x;

            //if any of the above values are zero that means two of our points lie on the x or z plane, which means 
            //the center lies on the z or x plane respectively. So lets just check manually whether to process the 
            //slopes or not.

            float centerX = 0;
            float centerZ = 0;

            if (xDelta_a != 0 && xDelta_b != 0) {}
            if (zDelta_a != 0 && zDelta_b != 0) {}

            float aSlope = zDelta_a / xDelta_a;
            float bSlope = zDelta_b / xDelta_b;
            float centerX = ((aSlope * bSlope * (point1.z - point3.z)) + (bSlope * (point1.x + point2.x)) - (aSlope * (point2.x + point3.x))) / (2 * (bSlope - aSlope));
            float centerZ = (-1f * (centerX - ((point1.x + point2.x) / 2f)) / aSlope) + ((point1.z + point2.z) / 2f);
            return new Vector3(centerX, 0, centerZ); 
        }*/

        /// <summary>
        /// Find a circle that goes through three points by finding the intersection of the perpendicular bisectors of two cords.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="point3"></param>
        /// <returns></returns>
        public static bool CenterOfCircleOfThreePoints2(out Vector3 center, Vector3 point1, Vector3 point2, Vector3 point3) {
            center = point1;

            //chord of point1 and 2
            (float, float) line1 = EquationOfLineFromTwoPoints(point1, point2);
            //chord of point2 and 3
            (float, float) line2 = EquationOfLineFromTwoPoints(point2, point3);
            //get perpendicular slope
            float slopePerpLine1 = float.IsFinite(line1.Item1) ? -1f / line1.Item1 : 0;
            float slopePerpLine2 = float.IsFinite(line2.Item1) ? -1f / line2.Item1 : 0;
            //get the midpoint of the chord, the line formed by this point and the perpendicular slope passes through the center of the circle
            Vector3 midway1And2 = Vector3.Lerp(point1, point2, 0.5f);
            Vector3 midway2And3 = Vector3.Lerp(point2, point3, 0.5f); 
            //find the point where the chord perpendicular bisectors intersect, that is our circle center.
            if (LineLineIntersection(out Vector3 intersection, midway1And2, slopePerpLine1, midway2And3, slopePerpLine2)) {
                center = intersection;
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Calculate a line intersection based on two points and their respective slopes.
        /// </summary>
        /// <param name="intersection"></param>
        /// <param name="point1"></param>
        /// <param name="slope1"></param>
        /// <param name="point2"></param>
        /// <param name="slope2"></param>
        /// <returns></returns>
        public static bool LineLineIntersection(out Vector3 intersection, Vector3 point1, float slope1, Vector3 point2, float slope2) {
            intersection = Vector3.zero;
            if (Mathf.Abs(slope1) == Mathf.Abs(slope2)) return false;
            
            float int1 = InterceptFromPointAndSlope(point1, slope1);
            float int2 = InterceptFromPointAndSlope(point2, slope2); //y = mx + b => m1x + b1 = m2x + b2 at intersection so x = (b2 - b1) / (m1 - m2)

            float xValue;
            float yValue;
            
            // the slopes can be infinite and if thats the case the x intercept will be returned instead of the y, we need to check for that.
            if (!float.IsFinite(slope1)) {
                //slope2 is not infinity, we need the intersection of line two with a vertical line instead
                xValue = point1.x;
                yValue = (slope2 * xValue) + int2;
            } else if (!float.IsFinite(slope2)) {
                xValue = point2.x;
                yValue = (slope1 * xValue) + int1;
            } else if (slope1 == 0 && slope2 != 0) {
                //if a slope is 0 we need to check for intersection between horizontal line and line with slope
                yValue = point1.z;
                xValue = (yValue - int2) / slope2;
            } else if (slope2 == 0 && slope1 != 0) {
                yValue = point2.z;
                xValue = (yValue - int1) / slope1;
            } else if (slope1 == 0 && slope2 == 0) {
                if (int1 == int2) {
                    xValue = point1.x;
                    yValue = point1.z;
                } else return false; //different horizontal lines
            } else {
                xValue = (int2 - int1) / (slope1 - slope2);
                yValue = (slope1 * xValue) + int1; //y = mx + b
            }

            if (float.IsNaN(xValue) || float.IsNaN(yValue)) {
                Debug.Log($"Error nan value: intercept 1: {int1} | intercept 2: {int2} | slope 1: {slope1} | slope 2: {slope2} | point 1: {point1} | point 2: {point2}");
            }

            intersection = new Vector3(xValue, 0, yValue);

            return true;
        }

        public static bool LineCircleIntersection(out List<Vector3> intersections, Vector3 point, float slope, float circleRadius, Vector3 circleCenter) {
            intersections = new List<Vector3>();
            float zIntercept = InterceptFromPointAndSlope(point, slope);
            float discriminant;
            float a;
            float b;
            float c;
            float x;
            float y;
            if (!float.IsFinite(slope)) {
                //handle case where line is vertical line x = k
                a = 1;
                b = - 2 * circleCenter.z;
                c = (circleCenter.x * circleCenter.x) + (circleCenter.z * circleCenter.z) - (circleRadius * circleRadius) - (2 * zIntercept * circleCenter.x) + (zIntercept * zIntercept);

                discriminant = (b * b) - (4 * a * c);

                if (discriminant < 0) {
                    return false;
                } else if (discriminant > 0) {
                    y = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
                    intersections.Add(new Vector3(zIntercept, 0, y));
                    y = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
                    intersections.Add(new Vector3(zIntercept, 0, y));
                    return true;
                } else {
                    y = -b / (2 * a);
                    intersections.Add(new Vector3(zIntercept, 0, y));
                    return true;
                }

            } else {
                a = (slope * slope) + 1;
                b = 2 * ((slope * zIntercept) - (slope * circleCenter.z) - circleCenter.x);
                c = (circleCenter.z * circleCenter.z) - (circleRadius * circleRadius) + (circleCenter.x * circleCenter.x) - (2 * zIntercept * circleCenter.z) + (zIntercept * zIntercept);

                discriminant = (b * b) - (4 * a * c);

                if (discriminant < 0) {
                    //imaginary solutions 
                    return false;
                } else if (discriminant > 0) {
                    //2 solutions
                    x = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
                    intersections.Add(new Vector3(x, 0, (slope * x) + zIntercept));
                    x = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
                    intersections.Add(new Vector3(x, 0, (slope * x) + zIntercept));
                    return true;
                } else {
                    //1 solution
                    x = -b / (2 * a);
                    intersections.Add(new Vector3(x, 0, (slope * x) + zIntercept));
                    return true;
                }
            }
        }

        /// <summary>
        /// Returns a pair of floats (x, z) where the x value is the slope and the z value is the z intercept of the line passing through the two points given.
        /// If the slope is infinite, the second value will be the x intercept.
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public static (float, float) EquationOfLineFromTwoPoints(Vector3 point1, Vector3 point2) {
            float diffZ = point2.z - point1.z;
            float diffX = point2.x - point1.x;
            float slope;

            if (diffX == 0) {
                slope = float.PositiveInfinity;
            }
            else {
                slope = diffZ / diffX;
            }

            return (slope, InterceptFromPointAndSlope(point1, slope));
        }

        /// <summary>
        /// Returns the intercept from a point and a slope. If the slope is infinity, 
        /// the x intercept will be returned, otherwise the z intercept will be returned.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="slope"></param>
        /// <returns></returns>
        public static float InterceptFromPointAndSlope(Vector3 point, float slope) {
            if (!float.IsFinite(slope)) return point.x;
            return point.z - (slope * point.x); // b = y - mx
        }

        public static Vector3 ToUnityVector3(this System.Numerics.Vector3 vector) {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        public static System.Numerics.Vector3 ToCSVector3(this Vector3 vector) {
            return new System.Numerics.Vector3(vector.x, vector.y, vector.z);
        }

        internal class SVDJacobiProgram {
            public static void Test() {
                Debug.Log("\nBegin SVD decomp via Jacobi algorithm using C#");

                double[][] A = new double[4][];
                A[0] = new double[] { 1, 2, 3 };
                A[1] = new double[] { 5, 0, 2 };
                A[2] = new double[] { 8, 5, 4 };
                A[3] = new double[] { 6, 9, 7 };

                Debug.Log("\nSource matrix: ");
                MatShow(A, 1, 5);

                double[][] U;
                double[][] Vh;
                double[] s;

                Debug.Log("\nPerforming SVD decomposition ");
                SVD_Jacobi(A, out U, out Vh, out s);

                Debug.Log("\nResult U = ");
                MatShow(U, 4, 9);
                Debug.Log("\nResult s = ");
                VecShow(s, 4, 9);
                Debug.Log("\nResult Vh = ");
                MatShow(Vh, 4, 9);

                double[][] S = MatDiag(s);
                double[][] US = MatProduct(U, S);
                double[][] USVh = MatProduct(US, Vh);
                Debug.Log("\nU * S * Vh = ");
                MatShow(USVh, 4, 9);

                Debug.Log("\nEnd demo ");
                Console.ReadLine();
            } // Main

            public static bool SVD_Jacobi(double[][] M, out double[][] U, out double[][] Vh, out double[] s)
            {
                double DBL_EPSILON = 1.0e-15;

                double[][] A = MatCopy(M); // working U
                int m = A.Length;
                int n = A[0].Length;
                double[][] Q = MatIdentity(n); // working V
                double[] t = new double[n];    // working s

                // initialize counters
                int count = 1;
                int sweep = 0;
                //int sweepmax = 5 * n;

                double tolerance = 10 * m * DBL_EPSILON; // heuristic

                // Always do at least 12 sweeps
                int sweepmax = System.Math.Max(5 * n, 12); // heuristic

                // store the column error estimates in St for use
                // during orthogonalization

                for (int j = 0; j < n; ++j)
                {
                    double[] cj = MatGetColumn(A, j);
                    double sj = VecNorm(cj);
                    t[j] = DBL_EPSILON * sj;
                }

                // orthogonalize A by plane rotations
                while (count > 0 && sweep <= sweepmax)
                {
                    // initialize rotation counter
                    count = n * (n - 1) / 2;

                    for (int j = 0; j < n - 1; ++j)
                    {
                        for (int k = j + 1; k < n; ++k)
                        {
                            double cosine, sine;

                            double[] cj = MatGetColumn(A, j);
                            double[] ck = MatGetColumn(A, k);

                            double p = 2.0 * VecDot(cj, ck);
                            double a = VecNorm(cj);
                            double b = VecNorm(ck);

                            double q = a * a - b * b;
                            double v = Hypot(p, q);

                            // test for columns j,k orthogonal,
                            // or dominant errors 
                            double abserr_a = t[j];
                            double abserr_b = t[k];

                            bool sorted = (a >= b);
                            bool orthog = (System.Math.Abs(p) <=
                                tolerance * (a * b));
                            bool noisya = (a < abserr_a);
                            bool noisyb = (b < abserr_b);

                            if (sorted && (orthog ||
                                noisya || noisyb))
                            {
                                --count;
                                continue;
                            }

                            // calculate rotation angles
                            if (v == 0 || !sorted)
                            {
                                cosine = 0.0;
                                sine = 1.0;
                            }
                            else
                            {
                                cosine = System.Math.Sqrt((v + q) / (2.0 * v));
                                sine = p / (2.0 * v * cosine);
                            }

                            // apply rotation to A (U)
                            for (int i = 0; i < m; ++i)
                            {
                                double Aik = A[i][k];
                                double Aij = A[i][j];
                                A[i][j] = Aij * cosine + Aik * sine;
                                A[i][k] = -Aij * sine + Aik * cosine;
                            }

                            // update singular values
                            t[j] = System.Math.Abs(cosine) * abserr_a +
                                System.Math.Abs(sine) * abserr_b;
                            t[k] = System.Math.Abs(sine) * abserr_a +
                                System.Math.Abs(cosine) * abserr_b;

                            // apply rotation to Q (V)
                            for (int i = 0; i < n; ++i)
                            {
                                double Qij = Q[i][j];
                                double Qik = Q[i][k];
                                Q[i][j] = Qij * cosine + Qik * sine;
                                Q[i][k] = -Qij * sine + Qik * cosine;
                            } // i
                        } // k
                    } // j

                    ++sweep;
                } // while

                //    compute singular values
                double prevNorm = -1.0;

                for (int j = 0; j < n; ++j)
                {
                    double[] column = MatGetColumn(A, j);
                    double norm = VecNorm(column);

                    // determine if singular value is zero
                    if (norm == 0.0 || prevNorm == 0.0
                        || (j > 0 &&
                            norm <= tolerance * prevNorm))
                    {
                        t[j] = 0.0;
                        for (int i = 0; i < m; ++i)
                            A[i][j] = 0.0;
                        prevNorm = 0.0;
                    }
                    else
                    {
                        t[j] = norm;
                        for (int i = 0; i < m; ++i)
                            A[i][j] = A[i][j] * 1.0 / norm;
                        prevNorm = norm;
                    }
                }

                U = A;
                Vh = MatTranspose(Q);
                s = t;

                // to sync with default np.linalg.svd() shapes:
                // if m "lt" n, extract 1st m columns of U
                //     extract 1st m values of s
                //     extract 1st m rows of Vh

                if (m < n)
                {
                    U = MatExtractFirstColumns(U, m);
                    s = VecExtractFirst(s, m);
                    Vh = MatExtractFirstRows(Vh, m);
                }

                if (count > 0)
                {
                    Debug.Log("Jacobi iterations did not converge");
                    return false;
                }

                return true;

            } // SVD_Jacobi()

            // === helper functions =================================
            //
            // MatMake, MatCopy, MatIdentity, MatGetColumn,
            // MatExtractFirstColumns, MatExtractFirstRows,
            // MatTranspose, MatDiag, MatProduct, VecNorm, VecDot,
            // Hypot, VecExtractFirst, MatShow, VecShow
            //
            // ======================================================

            static double[][] MatMake(int r, int c)
            {
                double[][] result = new double[r][];
                for (int i = 0; i < r; ++i)
                    result[i] = new double[c];
                return result;
            }

            static double[][] MatCopy(double[][] m)
            {
                int r = m.Length; int c = m[0].Length;
                double[][] result = MatMake(r, c);
                for (int i = 0; i < r; ++i)
                    for (int j = 0; j < c; ++j)
                        result[i][j] = m[i][j];
                return result;
            }

            static double[][] MatIdentity(int n)
            {
                double[][] result = MatMake(n, n);
                for (int i = 0; i < n; ++i)
                    result[i][i] = 1.0;
                return result;
            }

            static double[] MatGetColumn(double[][] m, int j)
            {
                int rows = m.Length;
                double[] result = new double[rows];
                for (int i = 0; i < rows; ++i)
                    result[i] = m[i][j];
                return result;
            }

            static double[][] MatExtractFirstColumns(double[][] src,
                int n)
            {
                int nRows = src.Length;
                // int nCols = src[0].Length;
                double[][] result = MatMake(nRows, n);
                for (int i = 0; i < nRows; ++i)
                    for (int j = 0; j < n; ++j)
                        result[i][j] = src[i][j];
                return result;
            }

            static double[][] MatExtractFirstRows(double[][] src,
                int n)
            {
                // int nRows = src.Length;
                int nCols = src[0].Length;
                double[][] result = MatMake(n, nCols);
                for (int i = 0; i < n; ++i)
                    for (int j = 0; j < nCols; ++j)
                        result[i][j] = src[i][j];
                return result;
            }

            public static double[][] MatTranspose(double[][] m)
            {
                int r = m.Length;
                int c = m[0].Length;
                double[][] result = MatMake(c, r);
                for (int i = 0; i < r; ++i)
                    for (int j = 0; j < c; ++j)
                        result[j][i] = m[i][j];
                return result;
            }

            static double[][] MatDiag(double[] vec)
            {
                int n = vec.Length;
                double[][] result = MatMake(n, n);
                for (int i = 0; i < n; ++i)
                    result[i][i] = vec[i];
                return result;
            }

            public static double[][] MatProduct(double[][] matA,
                double[][] matB)
            {
                int aRows = matA.Length;
                int aCols = matA[0].Length;
                int bRows = matB.Length;
                int bCols = matB[0].Length;
                if (aCols != bRows)
                    throw new Exception("Non-conformable matrices");

                double[][] result = MatMake(aRows, bCols);

                for (int i = 0; i < aRows; ++i)
                    for (int j = 0; j < bCols; ++j)
                        for (int k = 0; k < aCols; ++k)
                            result[i][j] += matA[i][k] * matB[k][j];

                return result;
            }

            static double VecNorm(double[] vec)
            {
                double sum = 0.0;
                int n = vec.Length;
                for (int i = 0; i < n; ++i)
                    sum += vec[i] * vec[i];
                return System.Math.Sqrt(sum);
            }

            static double VecDot(double[] v1, double[] v2)
            {
                int n = v1.Length;
                double sum = 0.0;
                for (int i = 0; i < n; ++i)
                    sum += v1[i] * v2[i];
                return sum;
            }

            static double Hypot(double x, double y)
            {
                // fancy sqrt(x^2 + y^2)
                double xabs = System.Math.Abs(x);
                double yabs = System.Math.Abs(y);
                double min, max;

                if (xabs < yabs)
                {
                    min = xabs; max = yabs;
                }
                else
                {
                    min = yabs; max = xabs;
                }

                if (min == 0)
                    return max;

                double u = min / max;
                return max * System.Math.Sqrt(1 + u * u);
            }

            static double[] VecExtractFirst(double[] vec, int n)
            {
                double[] result = new double[n];
                for (int i = 0; i < n; ++i)
                    result[i] = vec[i];
                return result;
            }
            
            // ------------------------------------------------------

            public static void MatShow(double[][] m, int dec, int wid)
            {
                for (int i = 0; i < m.Length; ++i)
                {
                    string rowString = "";
                    for (int j = 0; j < m[0].Length; ++j)
                    {
                        double v = m[i][j];
                        if (System.Math.Abs(v) < 1.0e-8) v = 0.0;    // hack
                        rowString += v.ToString("F" + dec).PadLeft(wid);
                    }
                    Debug.Log(rowString);
                }
            }

            // ------------------------------------------------------

            public static void VecShow(double[] vec, int dec, int wid)
            {
                string rowString = "";
                for (int i = 0; i < vec.Length; ++i)
                {
                    double x = vec[i];
                    if (System.Math.Abs(x) < 1.0e-8) x = 0.0;
                    rowString += x.ToString("F" + dec).PadLeft(wid);
                }
                Debug.Log(rowString);
            }
        }
    
        /// <summary>
        /// Approximate a function using simpsons method
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static double SimpsonApproximation(double from, double to, Func<double, double> func, int numIntervals) {
            if (numIntervals % 2 != 0) numIntervals++;
            double intervalLength = (to-from)/numIntervals;
            double coeff = intervalLength / 6;
            double sum = 0;
            for (double i = from; i+intervalLength <= to; i+=intervalLength) {
                //new from is i, new to is i+intervalLength
                sum += coeff * (func(i) + (4*func((i+i+intervalLength)/2)) + func(i+intervalLength));
            }
            return sum;
        }
    }
}