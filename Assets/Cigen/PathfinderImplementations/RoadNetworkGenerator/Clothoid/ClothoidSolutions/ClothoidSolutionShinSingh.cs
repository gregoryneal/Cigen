using System;
using System.Collections.Generic;
using UnityEngine;
using System.Numerics;

namespace Clothoid {

    /// <summary>
    /// A solution to solve a general clothoid curve with the constraint that every point in the input polyline must be passed through by the resulting curve.
    /// 
    /// It works by creating a Posture object for every sequence of three input nodes, and then drawing a curve between each Posture. We will take left turns to correspond
    /// to negative radii/curvature, and right turns to correspond to positive radii/curvature.
    /// 
    /// This method was described by Dong Hun Shin and Sajiv Singh in their December 1990 paper titled "Path Generation for Robot Vehicles Using Composite Clothoid Segments"
    /// Copyright Carnegie-Mellon University.
    /// </summary>
    public class ClothoidSolutionShinSingh : ClothoidSolution
    {
        /// <summary>
        /// This controls how many degrees each successive tangent vectors of the input polyline.
        /// </summary>
        private float minAngleDeviationDegrees = 1f;

        /// <summary>
        /// A posture at index i is a circle that passes through the indexed polyline points i-1, i, i+1.
        /// There is not a posture associated for the first and last polyline node.
        /// If the three successive points fall in a line, we approximate the segment with a line segment.
        /// This is represented with a posture that has 0 curvature.
        /// </summary>
        public List<Posture> Postures { get; private set; }

        public override ClothoidCurve CalculateClothoidCurve(List<UnityEngine.Vector3> inputPolyline, float allowableError = 0.1F, float endpointWeight = 1)
        {
            this.clothoidCurve = new ClothoidCurve();
            Postures = new List<Posture>();
            this.SetPolyline(inputPolyline);
            if (inputPolyline.Count >= 3) {
                SetupPostures();
            }

            SolveClothoidParameters();

            return this.clothoidCurve;
            //throw new System.NotImplementedException();
        }

        /// <summary>
        /// Create 3 clothoid segments for each successive pair of postures depending on the orientation of the two postures.
        /// Let Pi, Ci denote the first posture and circle, and let Pf, Cf denote the second. The two circles will overlap in two places,
        /// marking the start and end nodes of the three clothoid segments. 
        /// 
        /// First, figure out which sign the curvature takes at the start and end node. Visually this involves drawing the Ci and Cf, with 
        /// the tangents visible. If the tangent at Pf points outside of Ci, set the sign of the sharpness of the first clothoid segment so
        /// that it stays within Ci. If the tangent at Pf points inside of Ci, set the sign of the sharpness of the first clothoid segment 
        /// so that it goes outside of Ci. Otherwise set the sharpness to 0 so that the curve follows the circle Ci (Ci = Cf).
        /// 
        /// Now to find the sign of the final (third) clothoid segment we do something very similar. If the tangent of Pi lies outside of Cf,
        /// set the sign of the sharpness of the last clothoid segment so that the curve lies outside Cf. If the tangent at Pi lies inside Cf,
        /// set the sign of the sharpness of the last clothoid segment so that the curve lies inside Cf. Otherwise set the sharpness to 0.
        /// 
        /// Here is a helpful table, Xi and Xf represent the initial and final sharpness sign
        /// and in this solution positive values denote left turns and negative values denote right turns:
        /// 
        /// ki > kf > 0 -> Xi, Xf > 0 
        /// ki > 0 > kf -> Xi, Xf > 0 
        /// 0 > ki > kf -> Xi, Xf < 0 
        /// 0 > kf > ki -> Xi, Xf > 0
        /// kf > 0 > ki -> Xi, Xf < 0
        /// kf > ki > 0 -> Xi, Xf < 0
        /// ki > kf = 0 -> Xi, Xf > 0
        /// 0 = ki > kf -> Xi, Xf > 0
        /// 0 = kf > ki -> Xi, Xf < 0
        /// kf > ki = 0 -> Xi, Xf < 0
        /// kf = ki = 0 -> Xi, Xf = 0
        /// 
        /// Once we figure this out we then need to build three clothoid segments between each posture pair.
        /// They will have the (sharpness, arcLength) values of (x, l1), (-x, l2), (x, l3) where the sign of
        /// x depends on the postures being compared using the table above.
        /// 
        /// We would need to solve fresnel integrals with arc length l1, l2, l3 to find values for x, l1, l2,
        /// and l3. So instead we use numerical methods to compute them.
        /// 
        /// Let l1 and l2 be 1/3 the average length of the (small) circular arcs that connect the initial and final posture.
        /// Those values are:
        /// 
        /// si = ri * theta1 => ri is radius of posture Pi.
        /// theta1 = angle between line CiPi and CiPf (this time Pi and Pf represent the points (Pi.X, Pi.Z) and (Pf.X, Pf.Z))
        /// 
        /// you can derive theta2 similarly as
        /// theta2 = angle between line CfPi and CfPf
        /// 
        /// Therefore set l1 = l2 = (si + sf) / (2 * 3) = (si + sf) / 6
        /// 
        /// Then follow a numerical method iteratively guess the real values of x, l1, l2 and l3:
        /// 
        /// Using l1 and l2, solve for l3, x with the equations:
        /// 
        /// kf = ki + xl1 - xl2 + xl3
        /// -> x = (kf - ki) / (l1 - l2 + l3)
        /// -> l3 = (kf - ki - xl1 + xl2) / x                                                              (1)
        /// a denotes the angle of the posture.
        /// 
        /// af = ai + ki(l1 + l2 + l3) + x(l1l2 - l2l3 + l1l3) + (x/2)(l1^2 - l2^2 + l3^2) 
        /// -> 
        /// x = (af - ai - ki(l1 + l2 + l3)) / ((l1l2 - l2l3 + l1l3) + ((l1^2 - l2^2 + l3^2) / 2))         (2)
        /// 
        /// substitution eq (1) into eq (2) and simplifying yields the following quadratic on x:
        /// 
        /// x^2(l2^2 - l1l2) + x(af - ai - 2l2ki) - (kf^2 - ki^2) / 2 = 0
        /// 
        /// choose the solution with the sign according to the above convention, then substitute back into
        /// eq (1) to solve for l3. Now we have estimated the quadruple (k, l1, l2, l3) which defines the 
        /// 3 clothoid segments connecting the two postures. To check if this solution is valid calculate
        /// the endpoint of the three clothoid segments and see if it nearby the second posture position.
        /// If it is then we have our parameters, if not then we must adjust l1 and l2. To make this numerical
        /// procedure a little faster, we can follow this algorithm:
        /// 
        /// guess l1, l2
        /// -> fix l2 and randomly alter l1 5 times
        ///     -> calculate final position for each pair
        ///     -> choose new l1 that minimizes distance to final posture
        /// -> fix l1 and randomly alter l2 5 times
        ///     -> calculate final position for each pair
        ///     -> choose new l2 that minimizes distance to final posture
        /// -> if distance is within bound exit, otherwise repeat loop.
        /// 
        /// </summary>
        private void SolveClothoidParameters() {
            List<ClothoidSegment> segments = new List<ClothoidSegment>();
            Posture posture1;
            Posture posture2;
            int sign;
            float ki;
            float kf;
            //angles of small circular arc segment of the posture circles
            float theta1;
            float theta2;
            //circular arc lengths for current estimation values of l1 and l2.
            float s1;
            float s2;
            float tempS1; //all generated l1 values
            float tempS2; //all l2 values
            float pa = .1f; //the determines how much to perturb s1 and s2 by every iteration
            //perturb the arc lengths based on the difference between the guess and the goal positions (maybe i havent tried it yet it might suck)
            //s1[i] = s1 * i * pa * dist(guess, goal)
            float s3; //final arc length that is calculated from l1 and l2
            //vectors pointing from the circle center to the posture points
            UnityEngine.Vector3 v1;
            UnityEngine.Vector3 v2;
            UnityEngine.Vector3 v3;
            UnityEngine.Vector3 v4;

            //Quadratic equation parameters
            float a;
            float b;
            float c;
            //sharpness guesses
            float xplus;
            float xminus;
            float x;

            //curvature estimations
            float k1f;
            float k2f;
            float k3f;

            //guess curve
            ClothoidCurve curve = new ClothoidCurve();

            //final position guess
            UnityEngine.Vector3 guess;
            //temp variable to find min distance from guess to goal
            float minDist = Mathf.Infinity;
            float goalDist = 0.01f;

            for (int i = 0; i+1 < Postures.Count; i++) {
                posture1 = Postures[i];
                posture2 = Postures[i+1];
                sign = 0; //sign of the sharpness (curvature derivative)
                ki = posture1.Curvature;
                kf = posture2.Curvature;
                v1 = posture1.Position - posture1.CircleCenter;
                v2 = posture2.Position - posture1.CircleCenter;
                v3 = posture1.Position - posture2.CircleCenter;
                v4 = posture2.Position - posture2.CircleCenter;

                // Figure out the sharpness sign
                if (ki > kf && kf > 0) sign = 1;
                if (ki > 0 && 0 > kf) sign = 1;
                if (0 > ki && ki > kf) sign = -1;
                if (0 > kf && kf > ki) sign = 1;
                if (kf > 0 && 0 > ki) sign = -1;
                if (kf > ki && ki > 0) sign = -1;
                if (ki > kf && kf > 0) sign = 1;
                if (0 > ki && ki > kf) sign = 1;
                if (0 > kf && kf > ki) sign = -1;
                if (kf > ki && ki > 0) sign = -1;

                //first l1 and l2 guess
                // Guess the initial total arc lengths of the first and second clothoid segments
                //radians
                theta1 = Mathf.Acos(UnityEngine.Vector3.Dot(v1, v2) / (v1.magnitude * v2.magnitude));
                theta2 = Mathf.Acos(UnityEngine.Vector3.Dot(v3, v4) / (v3.magnitude * v4.magnitude));
                s1 = theta1 / ki; //first clothoid
                s2 = theta2 / kf; //second clothoid

                //set these two values so my debugger stops complaining about unset values (they definitely get set in the below loop)
                x = 0;
                s3 = 0;

                int u = 0;

                while (minDist > goalDist && u < 5000) {
                    // Fix s2 and perturb s1 5 times
                    for (int j = -2; j < 3; j++) {
                        tempS1 = j * pa * s1;

                        // Calculate the sharpness with a quadratic equation that took me 3 pages to derive
                        a = (s2 * s2) - (tempS1 * s2);
                        b = posture2.Angle - posture1.Angle - (2f * s2 * ki);
                        c = - ((kf * kf) - (ki * ki)) / 2f;

                        float z = ((b * b) - (4 * a * c)) / ( 2 * a);

                        xplus = - b + Mathf.Sqrt(z);
                        xminus = - b - Mathf.Sqrt(z);

                        // Pick the value which matches the sign of sign
                        if (sign > 0) {
                            if (xplus > 0) x = xplus;
                            else x = xminus;
                        } else if (sign < 0) {
                            if (xplus < 0) x = xplus;
                            else x = xminus;
                        } else x = 0;

                        // Calculate the arc length of the third clothoid
                        s3 = ((kf - ki) / x) + s2 - tempS1;

                        // Calculate the final position of the clothoids
                        // final curvature is:
                        // ki + xs1 -> first segment
                        // ki + xs1 - xs2 -> second segment
                        // ki + xs1 - xs2 + xs3 -> third segment (this should also approximate kf)
                        k1f = ki + (x * tempS1);
                        k2f = k1f - (x * s2);
                        k3f = k2f + (x * s3);

                        // Build segments so we can make a curve and have it do all the calculations for us
                        curve.Reset();
                        curve.AddSegment(new ClothoidSegment(0, tempS1, ki, k1f))
                            .AddSegment(new ClothoidSegment(0, s2, k1f, k2f))
                            .AddSegment(new ClothoidSegment(0, s3, k2f, k3f));

                        // This is the value in local clothoid space, which extends from the x axis
                        guess = posture1.Position + ClothoidSegment.RotateAboutAxis(curve.SampleCurveFromArcLength(tempS1 + s2 + s3), UnityEngine.Vector3.up, posture1.Angle);
                        float dist = UnityEngine.Vector3.Distance(guess, posture2.Position);
                        if (dist < minDist) {
                            minDist = dist;
                            //fix new value of s1 that minimizes the distance
                            s1 = tempS1;
                        }
                    }

                    if (minDist <= goalDist) break;

                    // Fix s1 and perturb s2 5 times
                    for (int j = -2; j < 3; j++) {
                        tempS2 = j * pa * s2;

                        // Calculate the sharpness with a quadratic equation that took me 3 pages to derive
                        a = (tempS2 * tempS2) - (s1 * tempS2);
                        b = posture2.Angle - posture1.Angle - (2f * tempS2 * ki);
                        c = - ((kf * kf) - (ki * ki)) / 2f;

                        float z = ((b * b) - (4 * a * c)) / ( 2 * a);

                        xplus = - b + Mathf.Sqrt(z);
                        xminus = - b - Mathf.Sqrt(z);

                        // Pick the value which matches the sign of sign
                        if (sign > 0) {
                            if (xplus > 0) x = xplus;
                            else x = xminus;
                        } else if (sign < 0) {
                            if (xplus < 0) x = xplus;
                            else x = xminus;
                        } else x = 0;

                        // Calculate the arc length of the third clothoid
                        s3 = ((kf - ki) / x) + tempS2 - s1;

                        // Calculate the final position of the clothoids
                        // final curvature is:
                        // ki + xs1 -> first segment
                        // ki + xs1 - xs2 -> second segment
                        // ki + xs1 - xs2 + xs3 -> third segment (this should also approximate kf)
                        k1f = ki + (x * s1);
                        k2f = k1f - (x * tempS2);
                        k3f = k2f + (x * s3);

                        // Build segments so we can make a curve and have it do all the calculations for us
                        curve.Reset();
                        curve.AddSegment(new ClothoidSegment(0, s1, ki, k1f))
                            .AddSegment(new ClothoidSegment(0, tempS2, k1f, k2f))
                            .AddSegment(new ClothoidSegment(0, s3, k2f, k3f));

                        // This is the value in local clothoid space, which extends from the x axis
                        guess = posture1.Position + ClothoidSegment.RotateAboutAxis(curve.SampleCurveFromArcLength(s1 + tempS2 + s3), UnityEngine.Vector3.up, posture1.Angle);
                        float dist = UnityEngine.Vector3.Distance(guess, posture2.Position);
                        if (dist < minDist) {
                            minDist = dist;
                            //fix new value of s1 that minimizes the distance
                            s2 = tempS2;
                        }
                    }

                    u++;
                }

                if (u >= 5000) Debug.Log("Attempt count exceeded!");
                Debug.Log($"final params: x: {x}, s1: {s1}, s2: {s2}, s3: {s3}");

                // The curve object should have our clothoid curve now. This is just a subset of the entire curve, the three segments that make up the curve connecting the two postures.
                // Lets add the curve to our current built curve
                this.clothoidCurve += curve;
            }

            for (int i = 0; i < segments.Count; i++) {
                Debug.Log(segments[i].Description());
            }
        }

        /// <summary>
        /// Create a posture for each 3 successive input polyline nodes.
        /// </summary>
        private void SetupPostures() {
            this.Postures = Posture.CalculatePostures(polyline);
        }

        /// <summary>
        /// Get samples of the Posture circle for drawing.
        /// </summary>
        /// <param name="postureIndex"></param>
        /// <param name="numSamples"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public List<UnityEngine.Vector3> GetPostureSamples(int postureIndex, int numSamples) {
            if (postureIndex < 0 || postureIndex >= Postures.Count) throw new ArgumentOutOfRangeException();
            Posture posture = Postures[postureIndex];
            //Debug.Log(posture.ToString());
            if (posture.Curvature != 0) {
                float radius = 1 / posture.Curvature;
                UnityEngine.Vector3 centerCircle = posture.CircleCenter;
                UnityEngine.Vector3 sampleCircle = new UnityEngine.Vector3(0, 0, radius);
                List<UnityEngine.Vector3> samples = new List<UnityEngine.Vector3>();
                float sampleAngleDeg = 360f / numSamples;
                for (float angleDeg = 0; angleDeg <= 360f; angleDeg += sampleAngleDeg) {
                    samples.Add(ClothoidSegment.RotateAboutAxis(sampleCircle, UnityEngine.Vector3.up, angleDeg) + centerCircle);
                }
                return samples;
            } 

            return new List<UnityEngine.Vector3>();
        }

        

        /// <summary>
        /// This class represents a quadruple of points as described by shin singh
        /// </summary>
        

        Posture CalculateNewPostureAtArcLength(Posture posture, float sharpness, float arcLength) {
            //float thetaL = posture.
            throw new NotImplementedException();
        }
    }
}