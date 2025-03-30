using System;

namespace Clothoid {

    public static class Math {
        
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
            return .5f - (R(arcLength)*(float)System.Math.Sin(System.Math.PI * 0.5f * (A(arcLength) - (arcLength * arcLength))));
        }

        /// <summary>
        /// The Fresnel Sine Integral approximation, given by HEALD M. A.
        /// </summary>
        /// <param name="arclength"></param>
        /// <returns></returns>
        public static float S(float arclength) {
            return 0.5f - (R(arclength)*(float)System.Math.Cos(System.Math.PI * 0.5f * (A(arclength) - (arclength * arclength))));
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
    }
}