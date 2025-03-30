using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Hierarchy;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using UnityEngine.Events;

//TODO: Remove reliance on UnityEngine and user System.Numerics.Vector3 instead

namespace GeneralPathfinder {

    /// <summary>
    /// The base pathfinding class, implementations are expected to use some flavor of A* pathfinding but maybe theres some other usages who knows. 
    /// </summary>
    public abstract class GeneralPathfinder : MonoBehaviour {
        //protected float costCoeff = 1;
        //protected float distCoeff = 1;
        protected Vector3Int startPosition;
        protected Vector3Int destination;
        protected PriorityQueue<Node, Cost> startOpenList = new PriorityQueue<Node, Cost>();
        protected Dictionary<Vector3Int, Node> startClosedList = new Dictionary<Vector3Int, Node>();
        protected PriorityQueue<Node, Cost> endOpenList = new PriorityQueue<Node, Cost>();
        protected Dictionary<Vector3Int, Node> endClosedList = new Dictionary<Vector3Int, Node>();
        protected int segmentMaskValue;
        protected int segmentMaskResolution;
        protected int segmentSolveDist;
        
        protected bool shouldKeepGraphing = true;
        public bool isSolved { get; protected set; }
        public List<Node> solution = new List<Node>();

        public UnityEvent SolutionEvent;


        //private int segmentMaskValue;

        //private int segmentMaskResolution;
        //private int segmentSolveDist;
        public virtual PathfinderSettings settings {get; private set;}

        void Start()
        {
            //"compound assignment" if SolutionEvent is null assign it the value of new UnityEvent()
            SolutionEvent ??= new UnityEvent();
        }

        /// <summary>
        /// Calculate the height of a specific node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected abstract float GetNodeY(Node node);
        /// <summary>
        /// Start the pathfinding algorithm.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="pathPriority"></param>
        /// <returns></returns>
        public abstract IEnumerator Graph(Vector3 start, Vector3 end, int pathPriority=0);
        /// <summary>
        /// Function that finds neighbors to add to the open list
        /// </summary>
        /// <param name="node"></param>
        /// <param name="openList"></param>
        /// <param name="closedList"></param>
        /// <param name="goal"></param>
        protected abstract void AddNeighbours(Node node, PriorityQueue<Node, Cost> openList, Dictionary<Vector3Int, Node> closedList, Vector3Int goal);
        /// <summary>
        /// Dequeue a new node from the open list and process it
        /// </summary>
        /// <param name="openList"></param>
        /// <param name="closedList"></param>
        /// <param name="oppositeClosedList"></param>
        /// <param name="goal"></param>
        protected abstract void ProcessNode(PriorityQueue<Node, Cost> openList, Dictionary<Vector3Int, Node> closedList, Dictionary<Vector3Int, Node> oppositeClosedList, Vector3Int goal);

        /// <summary>
        /// Process endpoints by creating an associated node and cost object and placing them into the open list queue.
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="node"></param>
        /// <param name="openList"></param>
        /// <param name="closedList"></param>
        /// <param name="goal"></param>
        /// <param name="isTunnel"></param>
        /// <param name="isBridge"></param>
        protected abstract void ProcessEndpoints(List<Tuple<float, Vector3Int>> endpoints, Node node, PriorityQueue<Node, Cost> openList, Dictionary<Vector3Int, Node> closedList, Vector3 goal, bool isTunnel=false, bool isBridge=false);

        public void Reset() {
            this.isSolved = false;
            solution.Clear();
            this.shouldKeepGraphing = true;
            this.startOpenList.Clear();
            this.endOpenList.Clear();
            this.startClosedList.Clear();
            this.endClosedList.Clear();
        }
        
        public void StopGraph() {
            this.shouldKeepGraphing = false;
        }
    }
}