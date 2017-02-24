using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cigen.MetricConstraint { 
    [System.Serializable]
    public abstract class MetricConstraint : ScriptableObject {
        public abstract float Distance(Vector3 start, Vector3 end);
        public abstract Vector3[] ExtraVerticesBetween(Vector3 start, Vector3 end);
        public abstract Vector3[] ProcessPoints(params Vector3[] points);
    }
}