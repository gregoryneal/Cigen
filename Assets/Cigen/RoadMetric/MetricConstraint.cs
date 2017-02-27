using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cigen.MetricConstraint { 
    [System.Serializable]
    public abstract class MetricConstraint {

        public CitySettings settings;
        public MetricConstraint(CitySettings settings) {
            this.settings = settings;
        }
        public virtual float Distance(Vector3 start, Vector3 end) {
            return Vector3.Distance(start, end);
        }
        public virtual Vector3[] ProcessPoints(params Vector3[] points) {
            return points;
        }
        public virtual Quaternion ProcessRotation(Quaternion rotation) {
            return rotation;
        }
    }
}