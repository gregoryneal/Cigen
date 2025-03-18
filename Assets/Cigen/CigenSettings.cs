using UnityEngine;
using Cigen.MetricConstraint;
using OpenCvSharp;
using UnityEditor;
using Cigen;
using System.Linq;

[CreateAssetMenu(fileName = "New City Settings", menuName = "Cigen/CitySettings", order = 1)]
public class CitySettings : ScriptableSingleton<CitySettings> {

    [Space(10)]
    [Header("Cost settings")]
    //Control cost values for different road priorities    
    public float[] sacrifice = new float[]{10f};
    public float[] maxCurvature = new float[]{10f};
    public float[] maxSlope = new float[]{10f};
    public float[] tunnelCost = new float[]{10f};
    public float[] bridgeCost = new float[]{10f};
/*
    [Space(10)]
    [Header("Highway settings")]
    //public float minHighwayBridgeLength = 50f;
    //public float maxHighwayBridgeLength = 500f;
    public float minHighwayTunnelLength = 5f;
    public float maxHighwayTunnelLength = 25f;
    public float highwayIdealSegmentLength = 5;
    //how many neighbors are allowed on a start or end position in a segment?
    public int maxHighwayNeighbors = 4;
    /// <summary>
    /// The maximum distance a highway branch can go when searching for a population center across water
    /// </summary>
    public float highwayMaxWaterCrossing = 100f;
    /// <summary>
    /// How close do generated highway segments need to be before they are merged?
    /// </summary>
    public float highwayConnectionThreshold = 10f;
    /// <summary>
    /// The maximum angle between road segments, in degrees!
    /// This angle is also used for branching highway segments within local constraints.
    /// </summary>
    [Range(0, 90)]
    public int maxAngleBetweenHighwayBranchSegments = 2;
    public float minAngleBetweenHighwayMergeSegments = 30;
    /// <summary>
    /// Every time the highway branches, we will create this many potential branches
    /// at the highway segment length. This will also control branching during local constraints.
    /// </summary>
    [Range(3, 30)]
    public int maxHighwayBranches = 10;
    /// <summary>
    /// Texture override for the highways, if not set they will be generated somehow.
    /// </summary>
    //public Texture2D highwayTexture;
    //public UnityEngine.Vector2 highwayRoadDimensions = new UnityEngine.Vector2(10, .75f);
    //rgb texture that allows the highway generation algorithm to choose between different types of highway patterns.
    //R -> Ring road
    //G -> Throughpass (the highway passes through the centroid of the population center)
    //B -> Bypass (the highways avoid the centroid of the population center)
    //the functions work with 4 channels, so we could add another layer to this setup at some point.
    //public Texture2D highwayMap;

    /// <summary>
    /// This setting affects the construction of ring roads placed around a PoplationCenter in conjunction with ringRoadPopulationDensityCutoff.
    /// Initially the ring road will be constructed as an ellipse, but this value will control how much each
    /// intersection lerps towards the centroid.
    /// </summary>
    [Range(0, 1)]
    public float ringRoadFollowContourAmount = 0.5f;
    public float highwayHeightOffset = 1f;


    [Space(10)]
    [Header("Street settings")]
    public float minStreetTunnelLength = 2;
    public float maxStreetTunnelLength = 10;
    public float streetIdealSegmentLength = 2;
    public int maxStreetNeighbors = 4;
    public float streetConnectionThreshold = 4;
    public Texture2D roadTexture;
    public UnityEngine.Vector2 roadDimensions = new UnityEngine.Vector2(2, .75f); //dimensions of the lines that are drawn, eventually to be replaced by a mesh
    //public int maxNumberOfRoads = 100;
    public float minimumRoadLength = 5f;
    public float maxAngleBetweenStreetBranchSegments = 5;
    public float minAngleBetweenStreetMergeSegments = 30;
    /// <summary>
    /// How many branches to create during any global goal branching. 
    /// </summary>
    public int maxStreetBranches = 10;

    [Space(10)]
    [Header("Intersection settings")]
    public float maxIntersectionMergeRadius = 2; //how close a proposed intersection must to another intersection before they are merged
    [HideInInspector]
    public float maxIntersectionVerticesMergeRadius = 0.001f; //how close do the vertices have to be to be combined in the mesh

    [Space(10)]
    [Header("Plot settings")]
    public float plotPadding = 5f;
    public float plotWidth = 20f; //how deep perpendicular to the road should we build the plot
    public float minPlotWidth = 6f;
    [Range(2, 10)]
    //public int plotResolution = 2;

    [Space(10)]
    [Header("Building settings")]
    //public int numBuildings = 10;
    public Vector3 minBuildingSize = new Vector3(1, 3, 1);
    public Vector3 maxBuildingSize = new Vector3(5, 10, 5);

    [Space(10)]
    [Header("Metric settings")]
    public MetricSpace metric = MetricSpace.EUCLIDEAN;
    //public int gridSpacing = 10;
*/
    
    [Space(10)]
    [Header("Terrain settings")]
    public bool generateTerrain = true;
    public float terrainMaxHeight = 250;
    //denotes the height of the terrain
    public Texture2D terrainHeightMap;
    public Mat terrainHeightMapMat;
    //denotes the population density


    [Space(10)]
    [Header("Generator settings")]
    public float heuristicCostCoefficient = 1f;
    public float heuristicDistanceCoefficient = 1f;
    public bool searchFromBothDirections = true;
    public bool generateSurfaceRoads = true;
    public bool generateBridges = true;
    public bool generateTunnels = true;
    public int[] tunnelSegmentMaskValue = new int []{10};
    public int[] tunnelSegmentMaskResolution = new int []{4};
    public int[] segmentMaskValue = new int[]{5};
    public int[] segmentMaskResolution = new int[]{4};
    public bool[] allowBothSidesConnection = new bool[]{true};

    /// <summary>
    /// This setting defines the boundary of what is considered a populated area. Anything less than this is considered unpopulated.
    /// </summary>
    [Range(0, 1)]
//    public float populationDensityCutoff = 0.1f;
    public bool roadsFollowTerrain = true;
    //public int maxRoadGenerationSeconds = 15;
    //public int maxIntersectionGenerationSeconds = 15;
    //public int maxPlotGenerationSeconds = 15;
    /// <summary>
    /// The value at which the nature map prevents any unflagged nature map roads.
    /// Or the value which controls when the nature map prevents road generation at all, if natureMapPreventsRoadGeneration is true.
    /// </summary>
//    public float natureMapCutoff = 0.12f;
    /// <summary>
    /// Should the nature map prevent road generation, or just flag all the roads generated through it. 
    /// We can always set this false, and then create a seperate nature map road generation queue, just like for highways and streets. 
    /// </summary>
    //public bool natureMapPreventsRoadGeneration = false;

    /// <summary>
    /// this affects how the texture coordinates are transformed into world space.
    /// X_world = textureToWorldSpace_x * X_texture
    /// Z_world = textureToWorldSpace_z * Y_texture
    /// the Y coordinate is different because it is affected by the terrain height map pixel value at texture coordinate (x, y) and the max height setting.
    /// Y_world = textureToWorldSpace_y * (P(x,y)_heightmap / 255) * terrainMaxHeight.
    /// </summary>
    public Vector3 textureToWorldSpace = new Vector3(1, 1, 1);
    public Texture2D populationDensityMap;
    //denotes the water
    public Mat populationDensityMapMat;
    public Texture2D waterMap;
    public Mat waterMapMat;
    //denotes natural areas (maybe use custom generation methods nd set a flag on the road itself)
    //public Texture2D natureMap;
    public Mat natureMapMat;

    [HideInInspector]
    public CityGenerator cigen;
    [HideInInspector]
    public City city;

    [HideInInspector]
    public static float GetMaxCurvature(int roadPriority) {
        if (roadPriority >= CitySettings.instance.maxCurvature.Length) {
            return 0;
        }

        return CitySettings.instance.maxCurvature[roadPriority];
    }
    [HideInInspector]
    public static float GetMaxSlope(int roadPriority) {
        if (roadPriority >= CitySettings.instance.maxSlope.Length) {
            return 0;
        }

        return CitySettings.instance.maxSlope[roadPriority];
    }
    [HideInInspector]
    public static float GetTunnelCostScaler(int roadPriority) {
        if (roadPriority >= CitySettings.instance.tunnelCost.Length) {
            return 0;
        }

        return CitySettings.instance.tunnelCost[roadPriority];
    }
    [HideInInspector]
    public static float GetBridgeCostScaler(int roadPriority) {
        if (roadPriority >= CitySettings.instance.bridgeCost.Length) {
            return 0;
        }

        return CitySettings.instance.bridgeCost[roadPriority];
    }

    [HideInInspector]
    public static int GetSegmentMaskValue(int roadPriority) {
        if (roadPriority >= CitySettings.instance.segmentMaskValue.Length) {
            return 50;
        }
        return CitySettings.instance.segmentMaskValue[roadPriority];
    }

    [HideInInspector]
    public static int GetSegmentMaskResolution(int roadPriority) {
        if (roadPriority >= CitySettings.instance.segmentMaskResolution.Length) {
            return 1;
        }
        return CitySettings.instance.segmentMaskResolution[roadPriority];
    }

    [HideInInspector]
    public static int GetTunnelSegmentMaskValue(int roadPriority) {
        if (roadPriority >= CitySettings.instance.tunnelSegmentMaskValue.Length) {
            return 1;
        }
        return CitySettings.instance.tunnelSegmentMaskValue[roadPriority];
    }

    [HideInInspector]
    public static int GetTunnelSegmentMaskResolution(int roadPriority) {
        if (roadPriority >= CitySettings.instance.tunnelSegmentMaskResolution.Length) {
            return 1;
        }
        return CitySettings.instance.tunnelSegmentMaskResolution[roadPriority];
    }

    [HideInInspector]
    public static bool GetAllowBothSidesConnection(int roadPriority) {
        if (roadPriority >= CitySettings.instance.allowBothSidesConnection.Length) {
            return true;
        }
        return CitySettings.instance.allowBothSidesConnection[roadPriority];
    }

    //dd
}