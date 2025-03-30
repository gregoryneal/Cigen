using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeneralPathfinder {
    public class AgentPathfinder : GeneralPathfinder
    {
        public override IEnumerator Graph(Vector3 start, Vector3 end, int pathPriority = 0)
        {
            throw new NotImplementedException();
        }

        protected override void AddNeighbours(Node node, PriorityQueue<Node, Cost> openList, Dictionary<Vector3Int, Node> closedList, Vector3Int goal)
        {
            throw new NotImplementedException();
        }

        protected override float GetNodeY(Node node)
        {
            throw new NotImplementedException();
        }

        protected override void ProcessEndpoints(List<Tuple<float, Vector3Int>> endpoints, Node node, PriorityQueue<Node, Cost> openList, Dictionary<Vector3Int, Node> closedList, Vector3 goal, bool isTunnel = false, bool isBridge = false)
        {
            throw new NotImplementedException();
        }

        protected override void ProcessNode(PriorityQueue<Node, Cost> openList, Dictionary<Vector3Int, Node> closedList, Dictionary<Vector3Int, Node> oppositeClosedList, Vector3Int goal)
        {
            throw new NotImplementedException();
        }
    }
}