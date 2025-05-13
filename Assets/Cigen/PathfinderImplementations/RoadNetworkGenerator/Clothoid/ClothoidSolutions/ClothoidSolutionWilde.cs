using System.Collections.Generic;
using UnityEngine;

namespace Clothoid {

    public class ClothoidSolutionWilde : ClothoidSolution
    {

        protected List<Posture> postures;

        protected float maxDeflectionAngle = 10f;

        public override ClothoidCurve CalculateClothoidCurve(List<Vector3> inputPolyline, float allowableError = 0.1F, float endpointWeight = 1)
        {
            SetPolyline(polyline);
            if (polyline.Count >= 3) this.postures = Posture.CalculatePostures(polyline);
            throw new System.NotImplementedException();
        }
        private void CalculateSegments() {

        }

        private List<ClothoidSegment> GenericTurn() {
            return new List<ClothoidSegment>();
        }
    }
}