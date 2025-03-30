using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Constraints;
using UnityEngine;

namespace Clothoid {

    /// <summary>
    /// A class that holds a sequence of ClothoidSegments and can calculate any position given an arc length (optionally parameterized to the range [0f,1f])
    /// </summary>
    public class ClothoidCurve {

        public int Count { get { return segments.Count; }}

        public ClothoidSegment this[int index] {
            get => this.segments[index];
        }

        public int PolylineCount { get { return this.inputPolyline.Count; }}

        private List<Vector3> drawingNodes = new List<Vector3>();

        /// <summary>
        /// The cached offset of the end of each segment.
        /// </summary>
        private List<Vector3> xzOffsets = new List<Vector3>();

        public float StartCurvature { get {
            return segments[0].StartCurvature;
        }}

        public float EndCurvature { get {
            return segments[^1].EndCurvature;
        }}
        protected List<ClothoidSegment> segments = new List<ClothoidSegment>();
        protected List<Vector3> inputPolyline = new List<Vector3>();
        public ClothoidCurve(List<ClothoidSegment> orderedSegments, List<Vector3> inputPolyline) {
            this.segments = orderedSegments;
            this.inputPolyline = inputPolyline;
            
            this.xzOffsets.Add(Vector3.zero);
            for (int i = 1; i < segments.Count; i++) {
                this.xzOffsets.Add(this.xzOffsets[^1] + segments[i].SamplePointByArcLength(segments[i].ArcLengthEnd));
            }
        }

        /// <summary>
        /// Get the position based on the total arc length of the point along the curve.
        /// This is useful for comparing to GetPositionFromPolyline to see if we are getting the right points.
        /// </summary>
        /// <param name="arcLength"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Vector3 SamplePointByArcLength(float arcLength) {
            if (arcLength > segments[^1].ArcLengthEnd) throw new ArgumentOutOfRangeException();

            for (int i = 0; i < segments.Count; i++) {
                if (arcLength >= segments[i].ArcLengthStart && arcLength <= segments[i].ArcLengthEnd) {
                    //If this doesn't work replace the input parameter with just the arclengthstart or just the arclength idk im so tired rn just try stuff.
                    //return segments[i].CalculatePosition(arcLength - segments[i].ArcLengthStart);
                    //return segments[i].CalculatePosition(arcLength - segments[0].ArcLengthStart);
                    //return this.xzOffsets[i] + segments[i].SamplePointByArcLength(arcLength);
                    return segments[i].SamplePointByArcLength(arcLength);
                }
            }
            return Vector3.zero;
        }

        /// <summary>
        /// Get the position of a polyline node by its index.
        /// </summary>
        /// <param name="polylineIndex"></param>
        /// <returns></returns>
        public Vector3 GetPositionFromPolyline(int polylineIndex) {
            if (polylineIndex >= this.inputPolyline.Count) polylineIndex = this.inputPolyline.Count - 1;
            return this.inputPolyline[polylineIndex];
        }

        /// <summary>
        /// Get the arc length of a polyline node based on its index.
        /// </summary>
        /// <param name="polylineIndex"></param>
        /// <returns></returns>
        public float GetStartingArcLengthFromPolylineIndex (int polylineIndex) {
            return EstimateArcLength(polylineIndex);
        }

        /// <summary>
        /// Sample the clothoid curve
        /// </summary>
        /// <returns></returns>
        public List<Vector3> CalculateDrawingNodes(int subdivisions) {
            if (this.drawingNodes == null || this.drawingNodes.Count != subdivisions) {
                List<Vector3> positions = new List<Vector3>();
                float arcLength = this[^1].ArcLengthEnd - this[0].ArcLengthStart;
                for (int i = 0; i < subdivisions; i++) {
                    float currArcLength = this[0].ArcLengthStart + (i * arcLength / (subdivisions-1));
                    positions.Add(SamplePointByArcLength(currArcLength));
                }
                this.drawingNodes = positions;
                return positions;
            } else {
                return this.drawingNodes;
            }
        }

        /// <summary>
        /// Calculate drawing nodes using the input polyline arc length values at each node.
        /// </summary>
        /// <returns></returns>
        public List<Vector3> CalculateDrawingNodesAtInputPolylineNodes() {
            List<Vector3> positions = new List<Vector3>();
            for (int i = 0; i < this.inputPolyline.Count; i++) {
                float arcLength = EstimateArcLength(i);
                positions.Add(SamplePointByArcLength(arcLength));
            }
            return positions;
        }

        protected float EstimateArcLength(Vector3 node) {
            if (this.inputPolyline.Contains(node) == false) throw new ArgumentException("Node not contained in the input polyline.");
            return EstimateArcLength(this.inputPolyline.IndexOf(node));
        }

        protected float EstimateArcLength(int polylineIndex) {
            if (polylineIndex > this.inputPolyline.Count - 2) polylineIndex = this.inputPolyline.Count - 2;
            if (this.inputPolyline == null) throw new ArgumentNullException();
            if (this.inputPolyline.Count == 0) throw new ArgumentOutOfRangeException();
            float sum = 0;
            for (int i = 0; i+1 <= polylineIndex; i++) {
                sum += Vector3.Distance(this.inputPolyline[i], this.inputPolyline[i+1]);
            }
            return sum;
        }
    }
}