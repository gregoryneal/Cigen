using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cigen.MetricConstraint;
using Cigen.Factories;
using Cigen.Structs;
using OpenCvSharp;
using UnityEditor;
using Cigen;

[CreateAssetMenu(fileName = "New City Settings", menuName = "Cigen/CitySettings", order = 1)]
public class CitySettings : ScriptableSingleton<CitySettings> {
    [Header("City settings")]
    public Vector3 cityDimensions = new Vector3(100, 0, 100); //total city bounds

    [Space(10)]
    [Header("Highway settings")]
    public float minHighwayBridgeLength = 50f;
    public float maxHighwayBridgeLength = 500f;
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
    public Texture2D highwayTexture;
    public UnityEngine.Vector2 highwayRoadDimensions = new UnityEngine.Vector2(10, .75f);
    //rgb texture that allows the highway generation algorithm to choose between different types of highway patterns.
    //R -> Ring road
    //G -> Throughpass (the highway passes through the centroid of the population center)
    //B -> Bypass (the highways avoid the centroid of the population center)
    //the functions work with 4 channels, so we could add another layer to this setup at some point.
    public Texture2D highwayMap;

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
    public int maxNumberOfRoads = 100;
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
    public int plotResolution = 2;

    [Space(10)]
    [Header("Building settings")]
    public int numBuildings = 10;
    public Vector3 minBuildingSize = new Vector3(1, 3, 1);
    public Vector3 maxBuildingSize = new Vector3(5, 10, 5);

    [Space(10)]
    [Header("Metric settings")]
    public MetricSpace metric = MetricSpace.EUCLIDEAN;
    public int gridSpacing = 10;

    
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
    public int segmentMaskValue = 5;
    public int segmentMaskResolution = 4;
    /// <summary>
    /// Slope is defined as the ratio of the heights between two points, h1/h2.
    /// If the slope is greater than 1 then h1 is taller than h2. If the slope is
    /// less than 1 but greater than 0 then h2 is taller than h1. We need to constrain
    /// h1/h2 between the range of (1/maxSlope, maxSlope). If our slope falls outside 
    /// of this range we deem the road too steep to build. If our slope falls within 
    /// this range we weight the road based on how close it is to 1, even flat ground.
    /// </summary>
    public float maxSlope = 1.4f;

    /// <summary>
    /// How much are we allowed to prune off the end of the segment length in order to try and fit into bounds?
    /// Given as a percentage of initial length.
    /// </summary>
    [Range(0f, 1f)]
    public float maxPruneLength = 0.5f;
    /// <summary>
    /// This setting defines the boundary of what is considered a populated area. Anything less than this is considered unpopulated.
    /// </summary>
    [Range(0, 1)]
    public float populationDensityCutoff = 0.1f;
    public bool roadsFollowTerrain = true;
    public int maxRoadGenerationSeconds = 15;
    public int maxIntersectionGenerationSeconds = 15;
    public int maxPlotGenerationSeconds = 15;
    /// <summary>
    /// The value at which the nature map prevents any unflagged nature map roads.
    /// Or the value which controls when the nature map prevents road generation at all, if natureMapPreventsRoadGeneration is true.
    /// </summary>
    public float natureMapCutoff = 0.12f;
    /// <summary>
    /// Should the nature map prevent road generation, or just flag all the roads generated through it. 
    /// We can always set this false, and then create a seperate nature map road generation queue, just like for highways and streets. 
    /// </summary>
    public bool natureMapPreventsRoadGeneration = false;

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
    public Texture2D natureMap;
    public Mat natureMapMat;

    [HideInInspector]
    public CityGenerator cigen;
    [HideInInspector]
    public City city;

    //dd
}