using System;
using System.Collections.Generic;
using UnityEngine;
using Cigen.MetricConstraint;
using Cigen.Factories;
using System.Collections;
using System.Linq;

public class City : MonoBehaviour {
    
    public List<Intersection> intersections = new List<Intersection>();
    public List<Road> roads = new List<Road>();
    public List<Plot> plots = new List<Plot>();
    public List<Building> buildings = new List<Building>();
    public Intersection origin;
    public CitySettings settings { get; private set; }

    public MetricConstraint metricConstraint;

    public void Init(Vector3 position, CitySettings settings) {
        transform.position = position;
        this.settings = settings;
        metricConstraint = MetricFactory.Process(this.settings.metric, settings);
        origin = CigenFactory.CreateOrMergeIntersection(position, this);
    }
    
    //Creates an intersection at the position, returns an intersection if one already exists there (or close enough)
    public Intersection CreateOrMergeNear(Vector3 position) {
        position = metricConstraint.ProcessPoint(position);
        Intersection nearest = NearestIntersection(position);
        float dist = nearest == null ? 0 : metricConstraint.Distance(position, nearest.Position);
        if(nearest != null && dist <= settings.maxIntersectionMergeRadius) {
            return nearest;
        } else { 
            Intersection temp = GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<Intersection>();
            temp.Init(position, this);
            intersections.Add(temp);
            return temp;
        }
    }

    public bool IsValidRoad(Vector3 proposedStart, Vector3 proposedEnd) {
        Vector3 center = Vector3.Lerp(proposedStart, proposedEnd, 0.5f);
        float length = metricConstraint.Distance(proposedStart, proposedEnd);
        Vector3 direction = (proposedEnd - proposedStart).normalized;
        List<Vector3> path = new RoadPath(proposedStart, proposedEnd, this).Path;

        if (length < settings.minimumRoadLength) {
            return false;
        }

        for (int i = 0; i < path.Count-1; i++) {
            center = Vector3.Lerp(path[i], path[i+1], 0.5f);
            length = metricConstraint.Distance(path[i], path[i+1]);
            if (Physics.CheckBox(center, new Vector3(settings.roadDimensions.x, settings.roadDimensions.y, length*0.95f) / 2f)) {
                return false;
            }
        }
        return true;
    }

    public Intersection CreateIntersectionAtPositionOnRoad(Vector3 position, Road road) {
        Intersection newIntersection = CigenFactory.CreateOrMergeIntersection(position, this);
        Intersection parent = road.parentNode;
        Intersection child = road.childNode;
        road.Remove();
        CigenFactory.CreateRoad(parent, newIntersection);
        CigenFactory.CreateRoad(newIntersection, child);
        return newIntersection;
    }

    private Vector3 RandomPosition() {
        Func<float, float> r = f => UnityEngine.Random.Range(-f, f);
        Vector3 pos = metricConstraint.ProcessPoint(new Vector3(r(settings.cityDimensions.x), r(settings.cityDimensions.y), r(settings.cityDimensions.z)));
        return pos;
    }

    //Need to test every possible road.
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
        Vector3 p1 = RandomPosition();

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
                Intersection q1 = CigenFactory.CreateOrMergeIntersection(p1, this);

                if (cmpr is Intersection) {
                    intersectionToConnectTo = (Intersection)cmpr;
                }

                if (cmpr is Road) {
                    roadToConnectTo = (Road)cmpr;
                    intersectionToConnectTo = CreateIntersectionAtPositionOnRoad(bestPositionForIntersection, roadToConnectTo);
                }

                if (intersectionToConnectTo != null) {
                    CigenFactory.CreatePath(q1, intersectionToConnectTo);
                }
            }
        }
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
}
