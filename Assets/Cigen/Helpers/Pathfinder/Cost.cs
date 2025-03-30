using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace GeneralPathfinder {
    /// <summary>
    /// Attempt to pathfind between two points in world space using weighted endpoints by goal function
    /// </summary>
        [Serializable]
        public class Cost : IComparable<Cost> {
        /// <summary>
        /// The parent position, for tracing back the path to the start.
        /// </summary>
        public Vector3Int parentPosition { get; private set; }
        [MaybeNull] public Node parentNode { get; private set; }
        /// <summary>
        /// The total distance travelled up to this point.
        /// </summary>
        public float totalHeuristicCost { get; private set; }
        /// <summary>
        /// The cost value to the final destination from this point.
        /// </summary>
        public readonly float distanceToGoal;
        public float WeightedCost { get { return (this.distCoeff*this.distanceToGoal)+(this.costCoeff*this.totalHeuristicCost); }}

        public bool isTunnel = false;
        public bool isBridge = false;
        float costCoeff;
        float distCoeff;
        /// <summary>
        /// Create a new cost object
        /// </summary>
        /// <param name="parentPosition">The parent position, for tracing back the path to the start.</param>
        /// <param name="initialCost">The total distance travelled up to this point.</param>
        /// <param name="distanceToGoal">The cost value to the final destination from this point.</param>
        public Cost(Vector3Int parentPosition, float initialCost, float distanceToGoal, float costCoeff, float distCoeff) {
            this.parentPosition = parentPosition;
            this.totalHeuristicCost = initialCost;
            this.distanceToGoal = distanceToGoal;
            this.costCoeff = costCoeff;
            this.distCoeff = distCoeff;
        }

        public Cost(Node parentNode, Vector3Int parentPosition, float initialCost, float distanceToGoal, float costCoeff, float distCoeff) : this(parentPosition, initialCost, distanceToGoal, costCoeff, distCoeff) {
            this.parentNode = parentNode;
        }

        public int CompareTo(Cost other) {
            return WeightedCost.CompareTo(other.WeightedCost);
        }

        public static bool operator <(Cost first, Cost second) {
            return first.CompareTo(second) < 0;
        }

        public static bool operator >(Cost first, Cost second) {
            return first.CompareTo(second) > 0;
        }

        public static bool operator <=(Cost first, Cost second) {
            return first.CompareTo(second) <= 0;
        }

        public static bool operator >=(Cost first, Cost second) {
            return first.CompareTo(second) >= 0;
        }

        public override string ToString() {
            return WeightedCost.ToString();
        }
    }
}