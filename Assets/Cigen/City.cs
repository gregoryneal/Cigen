using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using Cigen.MetricConstraint;
using Cigen.Factories;
using Cigen.Structs;
using Cigen.ImageAnalyzing;
using Cigen;
using System.Runtime.CompilerServices;

public class City : MonoBehaviour {
    
    public List<Intersection> intersections = new List<Intersection>();
    public List<Road> roads = new List<Road>();
    public List<Plot> plots = new List<Plot>();
    public List<Building> buildings = new List<Building>();
    public Intersection origin;
    public CitySettings Settings { get; private set; }
    public PopulationCenter[] PopulationCenters { get; private set; }

    public MetricConstraint metricConstraint;

    //initializing a city sets the metric constraints and then creates the first intersection
    public void Init(Vector3 position, CitySettings settings) {
        transform.position = position;
        this.Settings = settings;
        this.Settings.city = this;
        //metricConstraint = MetricFactory.Process(this.Settings.metric, settings);

        if (Settings.populationDensityMapMat == null) {
            Debug.LogError("pop density map is null");
        }
        PopulationCenters = ImageAnalysis.FindPopulationCenters();
        /*Debug.Log($"Found {PopulationCenters.Length} population centers!");
        int i = 0;
        foreach (PopulationCenter pc in PopulationCenters) {
            Debug.Log($"Center {i}:\nPopDens: {pc.density}\nSize: {pc.size}\nHgwyType: {pc.highwayType}");
            i++;
        }*/
        //CreateFirstIntersection();
    }
/*
    /// <summary>
    /// Is there a path between every population center?
    /// Traverse a node with a BFS and once we hit every node return true. 
    /// If we get to the end without hitting every node at least once return false.
    /// </summary>
    /// <returns></returns>
    public bool IsFullyConnected() {
        if (this.PopulationCenters.Length < 2) return true;
        Queue<PopulationCenter> nodesToVisit = new Queue<PopulationCenter>();      
        nodesToVisit.Enqueue(this.PopulationCenters[0]);
        HashSet<PopulationCenter> populationCenters = this.PopulationCenters.ToHashSet<PopulationCenter>();
        while (nodesToVisit.Count > 0) {
            PopulationCenter pc = nodesToVisit.Dequeue();
            foreach (PopulationCenter p in pc.connectedPCs) {
                //only queue unvisited nodes
                if (populationCenters.Contains(p)) nodesToVisit.Enqueue(p);
            }
            populationCenters.Remove(pc);
        }

        //if any pc remains in the set at the end of the operation we are not fully connected.
        return populationCenters.Count == 0;
    }
    
    public void CreateFirstIntersection() {
        origin = CreateOrMergeNear(RandomLocalPosition());
    }   

    //see if there is a highway connecting each city
    public bool AllHighwaysConnected() {
        return false;
    }
    /// <summary>
    /// Searches for an Intersection near a position, the search radius is
    /// given by CigenSettings.maxIntersectionMergeRadius, if none are found
    /// it creates a new Intersection at the position.
    /// </summary>
    /// <param name="position">The world coordinate of the new position to add.</param>
    /// <param name="name">The name of the intersection</param>
    /// <param name="processMetricYAxis">Should we process the y point with the metric constraint, or leave the value up to the heightmap? Defaults to the heightmap lookup.</param>
    /// <returns>The created intersection.</returns>
    public Intersection CreateOrMergeNear(Vector3 position, string name = "Intersection", bool processMetricYAxis = false) {
        position = metricConstraint.ProcessPoint(position);
        Intersection nearest = NearestIntersection(position);
        float dist = nearest == null ? 0 : metricConstraint.Distance(position, nearest.Position);
        if(nearest != null && dist <= Settings.maxIntersectionMergeRadius) {
            return nearest;
        } else { 
            Intersection temp = new GameObject().AddComponent<Intersection>();
            float newY = ImageAnalysis.TerrainHeightAt(position.x, position.z);
            float newYY = TerrainHeightAt(position.x, position.z);
            Debug.Log($"Heightmap analysis: {newY}");            
            position = new Vector3(position.x, newY + Settings.highwayHeightOffset, position.z);
            temp.Init(position, this);
            intersections.Add(temp);
            temp.name = name;
            return temp;
        }
    }

    public bool IsValidRoad(Vector3 proposedStart, Vector3 proposedEnd) {        
        //raycast from start to end and see if we hit something
        Vector3 center = Vector3.Lerp(proposedStart, proposedEnd, 0.5f);
        float length = metricConstraint.Distance(proposedStart, proposedEnd);
        Vector3 direction = (proposedEnd - proposedStart).normalized;
        //List<Vector3> path = new RoadPath(proposedStart, proposedEnd, this).Path;

        if (length < Settings.minimumRoadLength) {
            return false;
        }

        *//*

        if (intersections.Where(i => (proposedStart - i.Position <= settings.maxIntersectionMergeRadius || proposedEnd - i.Position <= settings.maxIntersectionMergeRadius)).Count() > 0) {
            return false;
        }


        /*
        for (int i = 0; i < path.Count-1; i++) {
            center = Vector3.Lerp(path[i], path[i+1], 0.5f);
            length = metricConstraint.Distance(path[i], path[i+1]);
            if (Physics.CheckBox(center, new Vector3(settings.roadDimensions.x, settings.roadDimensions.y, length*0.95f) / 2f)) {
                Debug.Log("BAD ROAD");
                return false;
            }
        }*//*
        return true;
    }

    public Intersection CreateIntersectionAtPositionOnRoad(Vector3 position, Road road) {
        Intersection newIntersection = CreateOrMergeNear(position);
        Intersection parent = road.parentNode;
        Intersection child = road.childNode;
        road.Remove();
        CigenFactory.CreateRoad(parent, newIntersection);
        CigenFactory.CreateRoad(newIntersection, child);
        return newIntersection;
    }

    private Vector3 RandomLocalPosition() {
        Func<float, float> r = f => UnityEngine.Random.Range(-f, f);
        Vector3 pos = metricConstraint.ProcessPoint(new Vector3(r(Settings.cityDimensions.x), r(Settings.cityDimensions.y), r(Settings.cityDimensions.z)));
        return pos;
    }*/

    //Need to test every possible road.
    /*
    private object[] ClosestPointOnRoadNetwork(Vector3 position) {
        object[] ret = new object[2];
        if (roads == null || roads.Count == 0) {
            if (intersections.Count > 0) {
                Intersection r = NearestIntersection(position);
                ret[0] = r.Position;
                ret[1] = r;
                return ret;
            }
        }
        
        Vector3 minPosition = position;
        float minDistance = float.MaxValue;
        Road roadz = null;
        foreach (Road road in roads) {
            Vector3 closestPointOnLine = metricConstraint.ProcessPoint(GetClosestPointOnLineSegment(road.parentNode.Position, road.childNode.Position, position));
            float dist = metricConstraint.Distance(closestPointOnLine, position);
            if (dist < minDistance) {
                minPosition = closestPointOnLine;
                minDistance = dist;
                roadz = road;
            }
        }
        ret[0] = minPosition;
        ret[1] = roadz;
        return ret;
    }

    //Nearest node in graphNodes to position q
    private Intersection NearestIntersection(Vector3 q) {
        if (intersections.Count > 0) {
            return intersections.OrderBy(i=>metricConstraint.Distance(q, i.Position)).First();
        }

        return null;
    }

    public void AddRandomIntersectionToRoadNetwork() {
        Vector3 p1 = RandomLocalPosition();

        Vector3 bestPositionForIntersection = Vector3.one * float.MaxValue;
        Road roadToConnectTo = null;
        Intersection intersectionToConnectTo = null;
        object cmpr = null;
        {
            object[] data = ClosestPointOnRoadNetwork(p1);
            bestPositionForIntersection = (Vector3)data[0];
            cmpr = data[1];
        }
        if (bestPositionForIntersection != Vector3.one * float.MaxValue) {
            if (IsValidRoad(bestPositionForIntersection, p1)) {
                Intersection q1 = CreateOrMergeNear(p1);

                if (cmpr is Intersection) {
                    intersectionToConnectTo = (Intersection)cmpr;
                }

                if (cmpr is Road) {
                    roadToConnectTo = (Road)cmpr;
                    intersectionToConnectTo = CreateIntersectionAtPositionOnRoad(bestPositionForIntersection, roadToConnectTo);
                }

                if (intersectionToConnectTo != null) {
                    CreatePath(q1, intersectionToConnectTo);
                }
            }
        }
    }

    //creates a series of intersections starting at head and ending at tail following the metric constraint, with a road between each of them
    public void CreatePath(Intersection head, Intersection tail) {
        List<Vector3> path = metricConstraint.ProcessPath(head.Position, tail.Position);

        Debug.Log(string.Format("Creating path with {0} intersections!", path.Count()));

        Intersection curr = head;
        foreach (Vector3 v in path) {
            Intersection newIntersection = CreateOrMergeNear(v);
            CigenFactory.CreateRoad(curr, newIntersection);
            curr = newIntersection;
        }
        CigenFactory.CreateRoad(curr, tail);
    }

    public static Vector3 GetClosestPointOnLineSegment(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
    {
        Vector3 AB = lineEnd - lineStart;
        float distance = Vector3.Dot(point - lineStart, AB) / Vector3.Dot(AB, AB);

        if (distance <= 0) {
            return lineStart;
        }
        else if (distance >= 1) {
            return lineEnd;
        }
        return lineStart + AB * distance;
    }

    public Plot RandomPlot() {
        return plots[UnityEngine.Random.Range(0, plots.Count)];
    }

    //create a highway network based on regions of high population
    //set in the city settings

    /// <summary>
    /// Use the PopulationCenters property to create a highway network around each PC.
    /// </summary>
    /// <returns></returns>
    public IEnumerator CreateHighways() {
    *//*    List<PopulationCenter> centers = settings.populationCenters;
        List<GameObject> places = new List<GameObject>();
        foreach(PopulationCenter pc in centers) {
            GameObject testSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            testSphere.transform.position = pc.position;
            testSphere.transform.localScale = pc.size;
            testSphere.name = "PopulationDensity: " + pc.density.ToString();
        }*//*

        if (Settings == null) {
            Debug.Log("settings is null");
        }

        foreach (PopulationCenter pc in PopulationCenters) {
            switch (pc.highwayType){
                case HighwayType.RING:
                    Highway.CreateRingRoad(pc, this);
                    break;
                case HighwayType.THROUGHPASS:
                    Highway.CreateThroughpass(pc, this);
                    break;
                case HighwayType.BYPASS:
                    Highway.CreateBypass(pc, this);
                    break;
                default:
                    break;
            }

            Debug.Log(pc.highwayType);
        }

        yield break;
    }

    public float TerrainHeightAt(float x, float z) {
        Terrain t = GetComponent<Terrain>();
        if (t == null)
            return 0;
        
        return t.SampleHeight(new Vector3(x, 0, z));
    }*/
}
