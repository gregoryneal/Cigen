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
    public Intersection origin;
    public CitySettings settings { get; private set; }

    private MetricConstraint m;

    public void Init(Vector3 position, CitySettings settings) {
        transform.position = position;
        this.settings = settings;
        m = MetricFactory.Process(this.settings.metric, settings);
        origin = CigenFactory.CreateIntersection(position, this);
    }
    
    //Creates an intersection at the position, returns an intersection if one already exists there (or close enough)
    public Intersection CreateIntersection(Vector3 position) {
        position = m.ProcessPoints(position)[0];
        Intersection nearest = NearestIntersection(position);
        float dist = nearest == null ? 0 : m.Distance(position, nearest.Position);
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
        float length = m.Distance(proposedStart, proposedEnd);
        Vector3 direction = (proposedEnd - proposedStart).normalized;

        if (length < settings.minimumRoadLength) {
            return false;
        }
        if (Physics.CheckBox(center, new Vector3(settings.roadDimensions.x, settings.roadDimensions.y, length) / 2f)) {
            return false;
        }

        /*
        foreach (Road road in roads) {
            float a = Vector3.Distance(proposedStart, road.parentNode.Position);
            float b = Vector3.Distance(proposedEnd, road.childNode.Position);
            float c = Vector3.Distance(proposedStart, road.childNode.Position);
            float e = Vector3.Distance(proposedEnd, road.parentNode.Position);
            Vector3 d = road.direction;
            if (Mathf.Max(a,b,c,e) < settings.minimumRoadLength && Mathf.Acos(Vector3.Dot(direction, d) / (direction.magnitude * d.magnitude)) <= settings.minimumNearbyRoadAngleSimilarity) {
                return false;
            }
        }*/
        return true;
    }

    public Intersection CreateIntersectionAtPositionOnRoad(Vector3 position, Road road) {
        Intersection newIntersection = CigenFactory.CreateIntersection(position, this);
        Intersection parent = road.parentNode;
        Intersection child = road.childNode;
        road.Remove();
        CigenFactory.CreateRoad(parent, newIntersection);
        CigenFactory.CreateRoad(newIntersection, child);
        return newIntersection;
    }

    private Vector3 RandomPosition() {
        Func<float, float> r = f => UnityEngine.Random.Range(-f, f);
        Vector3 pos = m.ProcessPoints(new Vector3(r(settings.cityDimensions.x), r(settings.cityDimensions.y), r(settings.cityDimensions.z)))[0];
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
            Vector3 closestPointOnLine = GetClosestPointOnLineSegment(road.parentNode.Position, road.childNode.Position, position);
            float dist = m.Distance(closestPointOnLine, position);
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
            return intersections.OrderBy(i=>m.Distance(q, i.Position)).First();
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
                Intersection q1 = CigenFactory.CreateIntersection(p1, this);

                if (cmpr is Intersection) {
                    intersectionToConnectTo = (Intersection)cmpr;
                    CigenFactory.CreateRoad(q1, intersectionToConnectTo);
                }

                if (cmpr is Road) {
                    roadToConnectTo = (Road)cmpr;
                    CigenFactory.CreateRoad(q1, CreateIntersectionAtPositionOnRoad(bestPositionForIntersection, roadToConnectTo));
                }
            }
        }
    }

    private Intersection[] TransformToMetric(Vector3 nearestPoint, Vector3 randomPoint) {
        //print("transforming to metric!");
        Vector3[] positionsConformingToMetric = m.ProcessPoints(nearestPoint, randomPoint);
        
        if (positionsConformingToMetric != null) {
            Intersection[] nodes = new Intersection[positionsConformingToMetric.Length];
            for (int i = 0; i < nodes.Length; i++) {
                nodes[i] = CigenFactory.CreateIntersection(positionsConformingToMetric[i], this);
            }
            return nodes;
        }
        return new Intersection[] { };
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
}
