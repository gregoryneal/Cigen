using System;
using Cigen.ImageAnalyzing;
using UnityEngine;

namespace Cigen {
    public class Node : IComparable<Node> {
        public int priority = 0;
        public Vector3Int position;
        public float yValue { get; private set; }
        public Cost cost;
        public Vector3 worldPosition { get {return new Vector3(position.x, yValue, position.z);} }
        public float length { get { return this.head ? 0 : Vector3.Distance(worldPosition, cost.parentNode.worldPosition); }}
        public bool head = false;
        /*
        public Node(Vector3Int position, Cost cost, int priority = 0, bool head = false) {
            this.priority = priority;
            this.position = position;
            this.cost = cost;
            this.head = head;
            this.worldPosition = new Vector3(position.x, ImageAnalysis.TerrainHeightAt(this.position), position.z);
            //Debug.Log($"New Int node created at {worldPosition}");
        }*/

        public Node(Vector3Int worldPosition, Cost cost, int priority = 0, bool head = false) {
            this.position = new Vector3Int(Mathf.RoundToInt(worldPosition.x), 0, Mathf.RoundToInt(worldPosition.z));
            this.priority = priority;
            this.cost = cost;
            this.head = head;
            //Debug.Log($"New node at {worldPosition}");
            SetYValue();
        }

        /// <summary>
        /// Change the head of the current node
        /// </summary>
        /// <param name="newHead"></param>
        public void ChangeParent(Node newHead, float newDistanceTravelled) {
            this.head = false;
            this.cost.ChangeParent(newHead, newDistanceTravelled);
        }

        public int CompareTo(Node other) { 
            return cost.CompareTo(other.cost); 
        }

        public override string ToString()
        {
            return position.ToString();
        }

        public override bool Equals(object obj)
        {
            return this.GetHashCode() == obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            return position.GetHashCode() * 41;
        }

        private void SetYValue() {
            if (this.head) this.yValue = ImageAnalysis.TerrainHeightAt(this.position);
            if (this.cost.isBridge || this.cost.isTunnel) {
                this.yValue = cost.parentNode.yValue;
            } else {
                this.yValue = ImageAnalysis.TerrainHeightAt(this.position);
            }
        }

        /// <summary>
        /// Attempt to alter this nodes Y value to match the new node Y value. Check to see if the three nodes composed of this node,
        /// the input node and the parent of this or the input node satisfies the slope and curvature requirements. 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool TryAlignWithNode(Node node) {
            if (this.head && node.head) return false;
            //the new position for this node, if all the checks pass
            Vector3 newNodePoint = this.position + (Vector3.up * node.yValue);
            Vector3 prev;
            Vector3 curr;
            Vector3 end;
            //the previous node from which to
            if (this.head) {
                //use the other node parent as the previous node 
                prev = newNodePoint;
                curr = node.worldPosition;
                end = node.cost.parentNode.worldPosition;
            } else {
                //use this node parent as the previous node for curvature calculations
                end = this.cost.parentNode.worldPosition;
                curr = newNodePoint;
                prev = node.worldPosition;
            }

            if (GlobalGoals.SegmentIsLegal(prev, curr, end)) {
                this.yValue = node.yValue;
                return true;
            }

            return false;
        }
    }
}