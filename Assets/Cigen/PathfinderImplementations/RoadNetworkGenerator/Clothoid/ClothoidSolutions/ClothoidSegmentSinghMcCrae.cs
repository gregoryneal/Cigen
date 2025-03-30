using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Clothoid {
    public class ClothoidSegmentSinghMcCrae : ClothoidSegment {
        private float t1;
        private float t2;
        public ClothoidSegmentSinghMcCrae(float arcLengthStart, float arcLengthEnd, float startCurvature, float endCurvature) : base(arcLengthStart, arcLengthEnd, startCurvature, endCurvature, CalculateB(arcLengthStart, arcLengthEnd, startCurvature, endCurvature)) {
            this.t1 = startCurvature * B * CURVE_FACTOR;
            this.t2 = endCurvature * B * CURVE_FACTOR;
        }
        public ClothoidSegmentSinghMcCrae(float arcLengthStart, float arcLengthEnd, float startCurvature, float endCurvature, float B) : base(arcLengthStart, arcLengthEnd, startCurvature, endCurvature, B) {}

        /// <summary>
        /// Calcluate the scaling factor for the constrained SinghMcCrae clothoid segment.
        /// </summary>
        /// <returns></returns>
        public static float CalculateB(float arcLengthStart, float arcLengthEnd, float startCurvature, float endCurvature) {
            float arcLength = arcLengthEnd - arcLengthStart;
            float B = (float)System.Math.Sqrt(arcLength/(System.Math.PI * System.Math.Abs(endCurvature - startCurvature)));
            return B;
        }
    }    
}