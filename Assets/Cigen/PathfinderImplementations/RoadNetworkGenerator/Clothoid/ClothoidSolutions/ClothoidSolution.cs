using System;
using System.Collections.Generic;
using UnityEngine;

namespace Clothoid {
    /// <summary>
    /// This is a generic class that implements specific clothoid segmentation algorithms. Given an input polyline (sequential list of points in space) this will return a
    /// ClothoidCurve object made up of an ordered list of derived ClothoidSegment objects of type T. I admit this might by a bit overkill for this type of problem but I wanted
    /// to implement different types of clothoid curves using different implementation algorithms and this did it for me. 
    /// </summary>
    /// <typeparam name="T">The specific class derived from ClothoidSegment that holds information about the type of ClothoidSegment for that solution.</typeparam>
    public abstract class ClothoidSolution<T> : MonoBehaviour where T : ClothoidSegment {
        protected List<T> segments = new List<T>();
        public List<Vector3> polyline { get; protected set; }
        public ClothoidCurve clothoidCurve { get; protected set; }
        public virtual int Count { get { return polyline.Count; }}
        /// <summary>
        /// Given an ordered list of Vector3s that make up the polyline control nodes, generate a list of ClothoidSegments that use those nodes.
        /// </summary>
        /// <param name="polylineNodes"></param>
        /// <returns></returns>
        public abstract ClothoidCurve CalculateClothoidCurve(List<Vector3> inputPolyline);
        
        /// <summary>
        /// Estimate the arc length of a given node on the polyline.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        protected float EstimateArcLength(Vector3 node) {
            if (this.polyline.Contains(node) == false) throw new ArgumentException("Node not in the polyline");
            if (this.polyline == null) throw new ArgumentNullException();
            if (this.polyline.Count == 0) throw new ArgumentOutOfRangeException();
            if (node == this.polyline[0]) return 0;
            float sum = 0;
            Vector3 prev = this.polyline[0];
            for (int i = 1; i < this.polyline.Count; i++) {
                sum += Vector3.Distance(prev, this.polyline[i]);
                prev = this.polyline[i];
                if (this.polyline[i] == node) break;
            }
            return sum;
        }

        protected virtual void SetPolyline(List<Vector3> polyline) {
            this.polyline = polyline;
        }
    }
}