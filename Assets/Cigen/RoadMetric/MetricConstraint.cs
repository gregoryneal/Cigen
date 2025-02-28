using Cigen.Factories;
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
        public virtual Vector3 ProcessPoint(Vector3 point) {
            return point;
        }
        public virtual Quaternion ProcessRotation(Quaternion rotation) {
            return rotation;
        }

        //Given the start and ending positions, generate a set of intersection
        //positions that starts at start, ends at end, and each road follows
        //the metric.
        public virtual List<Vector3> ProcessPath(Vector3 start, Vector3 end) {
            return new List<Vector3> { start, end };
        }

        //Same as ProcessPath except no endpoints are returned
        public List<Vector3> ProcessPathNoEndpoints(Vector3 start, Vector3 end) {
            List<Vector3> pathWithEndpoints = ProcessPath(start, end);
            pathWithEndpoints.Remove(start);
            pathWithEndpoints.Remove(end);
            return pathWithEndpoints;
        }
    }
}