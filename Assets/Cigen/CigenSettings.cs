using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cigen.MetricConstraint;

[CreateAssetMenu(fileName = "New City Settings", menuName = "Cigen/CitySettings", order = 1)]
public class CitySettings : ScriptableObject {
    [Header("City settings")]
    public Vector3 cityDimensions = new Vector3(100, 0, 100); //total city bounds

    [Space(10)]
    [Header("Road settings")]
    public Texture2D roadTexture;
    public UnityEngine.Vector2 roadDimensions = new UnityEngine.Vector2(2, .75f); //dimensions of the lines that are drawn, eventually to be replaced by a mesh
    public int maxNumberOfRoads = 100;
    public float minimumRoadLength = 5f;

    [Space(10)]
    [Header("Intersection settings")]
    public int maxNumberOfIntersections = 150;
    public int intersectionPlacementAttempts = 8; //amount of intersection placement attempts per individual intersection
    public float maxIntersectionMergeRadius = 2; //how close a proposed intersection must to another intersection before they are merged
    [HideInInspector]
    public float maxIntersectionVerticesMergeRadius = 0.001f; //how close do the vertices have to be to be combined in the mesh

    [Space(10)]
    [Header("Plot settings")]
    public float plotPadding = 5f;
    public float plotWidth = 20f; //how deep perpendicular to the road should we build the plot
    public float minPlotWidth = 6f;
    [Range(2, 10)]
    public int plotResolution = 2;

    [Space(10)]
    [Header("Building settings")]
    public int numBuildings = 10;
    public Vector3 minBuildingSize = new Vector3(1, 3, 1);
    public Vector3 maxBuildingSize = new Vector3(5, 10, 5);

    [Space(10)]
    [Header("Metric settings")]
    public MetricSpace metric = MetricSpace.EUCLIDEAN;

    //dd
}
