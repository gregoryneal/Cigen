using UnityEngine;
using OpenCvSharp;
using Cigen;
using System;

[CreateAssetMenu(fileName = "New Pathfinder Settings", menuName = "Pathfinding/Anisotropic Least Cost Path Finder Settings", order = 1)]
public class AnisotropicLeastCostPathSettings : PathfinderSettings
{
    [Header("Texture maps", order=0)]
    public Texture2D terrainHeightMap;
    [HideInInspector]
    public Mat terrainHeightMapMat;
    public Texture2D waterMap;
    [HideInInspector]
    public Mat waterMapMat;
    public Texture2D populationDensityMap;
    [HideInInspector]
    public Mat populationDensityMapMat;
    public Texture2D natureMap;
    [HideInInspector]
    public Mat natureMapMat;


    [Space(10)]
    [Header("Cost settings")]
    //Control cost values for different path priorities    
    public float[] tunnelCost = new float[]{10f};
    public float[] bridgeCost = new float[]{10f};
    
    [Space(10)]
    [Header("Terrain settings")]
    public bool generateTerrain = true;
    public float terrainMaxHeight = 250;
    //denotes the height of the terrain
    //denotes the population density


    [Space(10)]
    [Header("Terrain road path settings")]
    public bool generateBridgePaths = true;
    public bool generateTunnelPaths = true;
    public int[] tunnelSegmentMaskValue = new int []{10};
    public int[] tunnelSegmentMaskResolution = new int []{4};

    /// <summary>
    /// This setting defines the boundary of what is considered a populated area. Anything less than this is considered unpopulated.
    /// </summary>
    [Range(0, 1)]
    public bool roadsFollowTerrain = true;

    /// <summary>
    /// this affects how the texture coordinates are transformed into world space.
    /// X_world = textureToWorldSpace_x * X_texture
    /// Z_world = textureToWorldSpace_z * Y_texture
    /// the Y coordinate is different because it is affected by the terrain height map pixel value at texture coordinate (x, y) and the max height setting.
    /// Y_world = textureToWorldSpace_y * (P(x,y)_heightmap / 255) * terrainMaxHeight.
    /// </summary>
    public Vector3 textureToWorldSpace = new Vector3(1, 1, 1);

    [HideInInspector]
    public RoadNetworkGenerator cigen;
    [HideInInspector]
    public City city;

    [HideInInspector]
    public float GetTunnelCostScaler(int pathPriority) {
        if (pathPriority >= tunnelCost.Length) {
            throw new ArgumentOutOfRangeException();
        }

        return tunnelCost[pathPriority];
    }
    [HideInInspector]
    public float GetBridgeCostScaler(int pathPriority) {
        if (pathPriority >= bridgeCost.Length) {
            throw new ArgumentOutOfRangeException();
        }

        return bridgeCost[pathPriority];
    }
    
    [HideInInspector]
    public int GetTunnelSegmentMaskValue(int pathPriority) {
        if (pathPriority >= tunnelSegmentMaskValue.Length) {
            throw new ArgumentOutOfRangeException();
        }
        return tunnelSegmentMaskValue[pathPriority];
    }

    [HideInInspector]
    public int GetTunnelSegmentMaskResolution(int pathPriority) {
        //Debug.Log($"priority: {pathPriority}");
        //Debug.Log($"resolution: {String.Join(',', tunnelSegmentMaskResolution)}");
        if (pathPriority >= tunnelSegmentMaskResolution.Length) {
            throw new ArgumentOutOfRangeException();
        }
        return tunnelSegmentMaskResolution[pathPriority];
    }

    [HideInInspector]
    public bool GetAllowBothSidesConnection(int pathPriority) {
        if (pathPriority >= allowBothSidesConnection.Length) {
            throw new ArgumentOutOfRangeException();
        }
        return allowBothSidesConnection[pathPriority];
    }
}
