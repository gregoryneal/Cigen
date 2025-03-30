using System;
using Cigen.ImageAnalyzing;
using UnityEngine;
using Cigen;
using System.Runtime.CompilerServices;

namespace GeneralPathfinder {
    [Serializable]
    public class Node {//: IComparable<Node> {
        public int priority = 0;
        public Vector3Int position;
        public float yValue { get; private set; }
        public Cost cost;
        public Vector3 worldPosition { get {return new Vector3(position.x, yValue, position.z);} }
        public bool head = false;
        public Vector3 goal;

        //function that 
        /*
        public Node(Vector3Int position, Cost cost, int priority = 0, bool head = false) {
            this.priority = priority;
            this.position = position;
            this.cost = cost;
            this.head = head;
            this.worldPosition = new Vector3(position.x, ImageAnalysis.TerrainHeightAt(this.position), position.z);
            //Debug.Log($"New Int node created at {worldPosition}");
        }*/

        public Node(Vector3Int position, Cost cost, Vector3 goal, int priority = 0, bool head = false) {
            this.position = new Vector3Int(Mathf.RoundToInt(position.x), 0, Mathf.RoundToInt(position.z));
            this.priority = priority;
            this.cost = cost;
            this.head = head;
            this.goal = goal;
            //Debug.Log($"New node with priority {priority}");
        }

        /// <summary>
        /// Change the head of the current node
        /// </summary>
        /// <param name="newHead"></param>
        /*public void ChangeParent(Node newHead, float newDistanceTravelled) {
            this.head = false;
            this.cost.ChangeParent(newHead, newDistanceTravelled);
        }*/

        /*public int CompareTo(Node other) { 
            return cost.CompareTo(other.cost); 
        }*/

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

        public void SetHeight(float height) {
            this.yValue = height;
        }
        
        /*
        private void SetYValue() {
            if (yValueDelegate == null) yValueDelegate = ImageAnalysis.TerrainHeightAt;
            if (this.head) this.yValue = yValueDelegate(this.position, settings);
            if (this.cost.isBridge || this.cost.isTunnel) {
                this.yValue = cost.parentNode.yValue;
            } else {
                this.yValue = yValueDelegate(this.position, settings);
            }
        }*/

    }
}