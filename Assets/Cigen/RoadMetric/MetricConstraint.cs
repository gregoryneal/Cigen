using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cigen.MetricConstraint { 
    public abstract class MetricConstraint {
        public abstract float Distance(Vector3 start, Vector3 end);
        public abstract Vector3[] ExtraVertices(Vector3 start, Vector3 end);
    }
}