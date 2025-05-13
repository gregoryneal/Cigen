using System.Collections.Generic;
using UnityEngine;

namespace Clothoid {
    public class ClothoidSolutionThomasWalton : ClothoidSolution
    {
        public override ClothoidCurve CalculateClothoidCurve(List<Vector3> inputPolyline, float allowableError = 0.1F, float endpointWeight = 1)
        {
            ClothoidCurve c = new ClothoidCurve();
            if (inputPolyline.Count < 3) return c;

            //Consider only the posture of the first node, calculate the postures based on that for each subsequent node.
            for (int i = 0; i+1 < inputPolyline.Count; i++) {

            }

        
            throw new System.NotImplementedException();
        }
    }
}