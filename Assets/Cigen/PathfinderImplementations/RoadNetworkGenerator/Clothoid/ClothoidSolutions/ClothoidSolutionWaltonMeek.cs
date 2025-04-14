using System;
using System.Collections.Generic;
using UnityEngine;

namespace Clothoid {
    public class ClothoidSolutionWaltonMeek : ClothoidSolution
    {
        public override ClothoidCurve CalculateClothoidCurve(List<Vector3> inputPolyline, float allowableError = 0.1f, float endpointWeight = 1)
        {
            throw new NotImplementedException();
        }

        public override List<Vector3> GetFitSamples(int numSaples)
        {
            throw new NotImplementedException();
        }
    }
}