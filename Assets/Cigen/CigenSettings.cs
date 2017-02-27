using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cigen.MetricConstraint;

[CreateAssetMenu(fileName = "CitySettings", menuName = "Cigen/CitySettings", order = 1)]
public class CitySettings : ScriptableObject {
    [Header("City settings")]
    public Vector3 cityDimensions = new Vector3(3000, 0, 3000); //total city bounds
    public Vector3 initialCityPosition = Vector3.zero; //starting position of the city

    [Space(10)]
    [Header("Road settings")]
    public Texture2D roadTexture;
    public Vector2 roadDimensions = new Vector2(20, 1); //dimensions of the lines that are drawn, eventually to be replaced by a mesh
    public bool animateRoadBuilding = false;
    public float roadBuildSpeed = 20f;
    public float minimumRoadLength = 40f;
    public float minimumNearbyRoadAngleSimilarity = 10f;

    [Space(10)]
    [Header("Intersection settings")]
    public int maxNumberOfIntersections = 150;
    public int intersectionPlacementAttempts = 20; //amount of intersection placement attempts per individual intersection
    public float maxIntersectionMergeRadius = 30f; //how close a proposed intersection must to another intersection before they are merged
    [MinMaxRange(0, 60)]
    public MinMaxRange randomIntersectionSearchRadius;

    [Space(10)]
    [Header("Plot settings")]
    public float plotPadding = 5f;
    public float maxPlotWidth = 20f;
    [Range(2, 10)]
    public int plotResolution = 2;

    [Space(10)]
    [Header("Building settings")]
    public int numBuildings = 10;
    public Vector3 minBuildingSize = new Vector3(1, 3, 1);
    public Vector3 maxBuildingSize = new Vector3(5, 10, 5);

    [Space(10)]
    [Header("Metric settings")]
    public MetricSpace metric = MetricSpace.MANHATTAN;
}
