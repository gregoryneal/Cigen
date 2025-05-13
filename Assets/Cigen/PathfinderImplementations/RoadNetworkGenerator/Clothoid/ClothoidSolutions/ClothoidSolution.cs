using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEditor.Rendering;
using UnityEngine;

namespace Clothoid {
    /// <summary>
    /// This is a generic class that implements specific clothoid segmentation algorithms. Given an input polyline (sequential list of points in space) this will return a
    /// ClothoidCurve object made up of an ordered list of derived ClothoidSegment.
    /// </summary>
    public abstract class ClothoidSolution : MonoBehaviour {
        protected List<ClothoidSegment> segments = new List<ClothoidSegment>();
        public List<Vector3> polyline { get; protected set; }
        public ClothoidCurve clothoidCurve { get; protected set; }
        /// <summary>
        /// The number of polyline nodes in this solution
        /// </summary>
        public virtual int Count { get {
                try {
                    return polyline.Count; 
                } catch (NullReferenceException) {
                    return 0;
                }
            }   
        }
        /// <summary>
        /// Number of segments in the solution curve.
        /// </summary>
        public virtual int SegmentCount { get { return clothoidCurve.Count; }}

        /// <summary>
        /// Given an ordered list of Vector3s that make up the polyline control nodes, generate a list of ClothoidSegments that use those nodes.
        /// </summary>
        /// <param name="polylineNodes"></param>
        /// <returns></returns>
        public abstract ClothoidCurve CalculateClothoidCurve(List<Vector3> inputPolyline, float allowableError = 0.1f, float endpointWeight = 1);

        public virtual List<Vector3> GetFitSamples(int numSamples) {
            return clothoidCurve.GetSamples(numSamples);
        }
        
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

        protected float EstimateArcLength(int polylineNodeIndex) {
            float sum = 0;
            Vector3 prev = this.polyline[0];
            for (int i = 1; i <= polylineNodeIndex; i++) {
                sum += Vector3.Distance(prev, this.polyline[i]);
                prev = this.polyline[i];
            }
            return sum;
        }

        protected virtual void SetPolyline(List<Vector3> polyline) {
            this.polyline = polyline;
        }
    }
}