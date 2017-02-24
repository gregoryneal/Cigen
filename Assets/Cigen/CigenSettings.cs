using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cigen.MetricConstraint;

[CreateAssetMenu(fileName = "CiSettings", menuName = "Cigen/CiSettings", order = 1)]
public class CiSettings : ScriptableObject {
    public Vector3 cityDimensions = new Vector3(3000, 0, 3000); //total city bounds
    public Vector3 initialCityPosition = Vector3.zero; //starting position of the city
    public Vector2 roadDimensions = new Vector2(20, 1); //dimensions of the lines that are drawn, eventually to be replaced by a mesh
    public bool animateRoadBuilding = false;
    public float roadBuildSpeed = 20f;
    public float minimumRoadLength = 40f;
    public int numberOfIntersections = 500;
    public int intersectionPlacementAttempts = 20;
    public float maxIntersectionMergeRadius = 30f; //how close a proposed intersection must to another intersection before they are merged
    [MinMaxRange(0, 60)]
    public MinMaxRange randomIntersectionSearchRadius;
    public MetricSpace metric = MetricSpace.MANHATTAN;
    public MetricConstraint MetricConstraint { get; private set; }
}
