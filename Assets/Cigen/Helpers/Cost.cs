using System;
using UnityEngine;

namespace Cigen {
    /// <summary>
    /// Attempt to pathfind between two points in world space using weighted endpoints by goal function
    /// </summary>
       public class Cost : IComparable<Cost> {
        /// <summary>
        /// The parent position, for tracing back the path to the start.
        /// </summary>
        public Vector3Int parentPosition { get; private set; }
        public Node parentNode { get; private set; }
        /// <summary>
        /// The total distance travelled up to this point.
        /// </summary>
        public float totalCostOfPath { get; private set; }
        /// <summary>
        /// The cost value to the final destination from this point.
        /// </summary>
        public readonly float distanceToGoal;
        public float WeightedCost { get; private set; }

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
        public Cost(Vector3Int parentPosition, float initialCost, float distanceToGoal) {
            this.parentPosition = parentPosition;
            this.totalCostOfPath = initialCost;
            this.distanceToGoal = distanceToGoal;
            this.costCoeff = CitySettings.instance.heuristicCostCoefficient;
            this.distCoeff = CitySettings.instance.heuristicDistanceCoefficient;
            this.WeightedCost = (this.distCoeff*this.distanceToGoal)+(this.costCoeff*this.totalCostOfPath);
        }

        public Cost(Node parentNode, float initialCost, float distanceToGoal) : this(parentNode.position, initialCost, distanceToGoal) {
            this.parentNode = parentNode;
        }

        public int CompareTo(Cost other) {
            return WeightedCost.CompareTo(other.WeightedCost);
        }

        public void ChangeParent(Node newParent, float newDistanceTravelled) {
            this.parentNode = newParent;
            this.parentPosition = newParent.position;
            this.totalCostOfPath = newDistanceTravelled;
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