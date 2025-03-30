using System;
using UnityEngine;


public class PathfinderSettings : ScriptableObject {
    [Space(10)]
    [Header("Basic settings")]
    public bool generateSurfacePaths = true;
    public float[] sacrifice = new float[]{10f};
    /// <summary>
    /// The maximum curvature of the path based on its priority index
    /// </summary>
    [Tooltip("The maximum curvature of the path based on its priority index")]
    public float[] maxCurvature = new float[]{10f};
    /// <summary>
    /// The maximum slope of the path based on its priority index
    /// </summary>
    [Tooltip("The maximum slope of the path based on its priority index")]
    public float[] maxSlope = new float[]{10f};
    /// <summary>
    /// The coefficient of the cost value when summing with the distance cost.
    /// The cost is the made up of the slope and curvature costs, and more 
    /// depending on which settings object is being used.
    /// The larger this value is in comparison to the distance coefficient, the more
    /// accurate the search becomes, at the cost of speed.
    /// </summary>
    [Tooltip("The coefficient of the cost value when summing with the distance cost. The cost is the made up of the slope and curvature costs, and more  depending on which settings object is being used. The larger this value is in comparison to the distance coefficient, the more accurate the search becomes, at the cost of speed.")]
    public float heuristicCostCoefficient = 1f;
    /// <summary>
    /// The coefficient of the distance coefficient when summing with the cost.
    /// The larger this value is in comparison to the cost coefficient, the more
    /// greedy the algorithm becomes, at the cost of accuracy.
    /// </summary>
    [Tooltip("The coefficient of the distance coefficient when summing with the cost. The larger this value is in comparison to the cost coefficient, the more greedy the algorithm becomes, at the cost of accuracy.")]
    public float heuristicDistanceCoefficient = 1f;
    /// <summary>
    /// Should we search from the start to the end as well as the end to the start?
    /// This will sometimes be faster.
    /// </summary>
    [Tooltip("Should we search from the start to the end as well as the end to the start? This will sometimes be faster.")]
    public bool searchFromBothDirections = true;
    /// <summary>
    /// If we are searching from both directions at once, should we connect the paths if they overlap at some point that satisfies
    /// out constraints (curvature and slope)?
    /// </summary>
    [Tooltip("If we are searching from both directions at once, should we connect the paths if they overlap at some point that satisfies out constraints (curvature and slope)?")]
    public bool[] allowBothSidesConnection = new bool[]{true};
    /// <summary>
    /// This is the mask for which points to consider candidate endpoints at some exploration node centered on the origin.
    /// Consider the grid of integer points around our node:
    /// x, y in [-maskValue, maskValue] where (x,y) != (0,0)
    /// we consider only points where the greatest common factor of x and y is 1. So if the mask value
    /// is 1, we will get all 8 points immediately surrounding the node etc. 
    /// </summary>
    [Tooltip("This is the mask for which points to consider candidate endpoints at some exploration node centered on the origin. Consider the grid of integer points around our node: x, y in [-maskValue, maskValue] where (x,y) != (0,0) we consider only points where the greatest common factor of x and y is 1. So if the mask value is 1, we will get all 8 points immediately surrounding the node etc. ")]
    public int[] segmentMaskValue = new int[]{5};
    /// <summary>
    /// This is a scale factor for the segment mask value. Our actual candidate endpoint will be scaled out by this factor away from the exploration node.
    /// For example if the mask value is 1 and the segment mask resolution is 1 we will get the expected 8 nodes immediately surrouding our search node.
    /// But if the mask resolution is 2, we will scale those points out by twice their length, still returning 8 points but each of them is at twice the distance
    /// as before.
    /// </summary>
    [Tooltip("This is a scale factor for the segment mask value. Our actual candidate endpoint will be scaled out by this factor away from the exploration node. For example if the mask value is 1 and the segment mask resolution is 1 we will get the expected 8 nodes immediately surrouding our search node. But if the mask resolution is 2, we will scale those points out by twice their length, still returning 8 points but each of them is at twice the distance as before.")]
    public int[] segmentMaskResolution = new int[]{4};

    [HideInInspector]
    public float GetMaxCurvature(int pathPriority) {
        if (pathPriority >= maxCurvature.Length) {
            throw new ArgumentOutOfRangeException();
        }

        return maxCurvature[pathPriority];
    }
    [HideInInspector]
    public float GetMaxSlope(int pathPriority) {
        if (pathPriority >= maxSlope.Length) {
            throw new ArgumentOutOfRangeException();
        }

        return maxSlope[pathPriority];
    }    

    [HideInInspector]
    public int GetSegmentMaskValue(int pathPriority) {
        if (pathPriority >= segmentMaskValue.Length) {
            throw new ArgumentOutOfRangeException();
        }
        return segmentMaskValue[pathPriority];
    }

    [HideInInspector]
    public int GetSegmentMaskResolution(int pathPriority) {
        if (pathPriority >= segmentMaskResolution.Length) {
            throw new ArgumentOutOfRangeException();
        }
        return segmentMaskResolution[pathPriority];
    }


}