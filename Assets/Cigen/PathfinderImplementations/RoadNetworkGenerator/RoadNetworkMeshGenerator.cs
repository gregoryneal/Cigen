using System.Collections.Generic;
using System.Linq;
using Cigen;
using Clothoid;
using GeneralPathfinder;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
[RequireComponent(typeof(RoadNetworkGenerator))]
public class RoadNetworkMeshGenerator : MonoBehaviour {
    public Vector3 tangentIn = Vector3.one;
    public Vector3 tangentOut = Vector3.one;
    SplineContainer splineContainer;
    Spline spline;
    RoadNetworkGenerator rng;
    List<Vector3> nodes;
    //positions that approximate the clothoid curve of our nodes list
    void Start()
    {
        splineContainer ??= GetComponent<SplineContainer>();
        rng ??= GetComponent<RoadNetworkGenerator>();
        rng.SolutionConstructedEvent.AddListener(OnNetworkSolution);
    }

    void OnNetworkSolution() {
        Debug.Log($"Solution Event callback received. Constructing {rng.currentOrderedSolutionNodes.Count} nodes!");
        this.nodes = rng.currentOrderedSolutionNodes;
    }
}