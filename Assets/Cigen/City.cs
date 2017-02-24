using System;
using System.Collections.Generic;
using UnityEngine;
using Cigen.MetricConstraint;
using Cigen.Factories;

public class City : MonoBehaviour {
    
    public List<Intersection> intersections = new List<Intersection>();
    public List<Road> roads = new List<Road>();
    public Intersection origin;
    public CiSettings settings { get; private set; }

    private MetricConstraint m;

    public void Init(Vector3 position, CiSettings settings) {
        this.settings = settings;
        m = MetricFactory.Process(this.settings.metric);
        origin = CreateIntersection(position);
    }
    
    public Intersection CreateIntersection(Vector3 position) {
        Intersection temp = new GameObject("intersection").AddComponent<Intersection>();
        temp.Init(m.ProcessPoints(position)[0], this);
        intersections.Add(temp);
        return temp;
    }

    public bool IsValidRoad(Vector3 proposedStart, Vector3 proposedEnd) {
        Vector3 center = Vector3.Lerp(proposedStart, proposedEnd, 0.5f);
        float length = Vector3.Distance(proposedStart, proposedEnd);
        if (length < settings.minimumRoadLength) {
            return false;
        }
        if (Physics.CheckBox(center, new Vector3(settings.roadDimensions.x, settings.roadDimensions.y, length) / 2f)) {
            return false;
        }
        return true;
    }

    private Vector3 RandomPosition() {
        Func<float, float> r = f => UnityEngine.Random.Range(-f, f);
        Vector3 pos = new Vector3(r(settings.cityDimensions.x), r(settings.cityDimensions.y), r(settings.cityDimensions.z));
        return m.ProcessPoints(pos)[0];
    }

    //Nearest node in graphNodes to position q
    private Intersection NearestIntersection(Vector3 q) {
        //print("Nearest neighbor called!");
        List<Intersection> emptyIntersections = new List<Intersection>();
        Intersection closest = origin;
        float smallestDist = float.MaxValue;
        foreach(Intersection v in intersections) {
            if (intersections.Count > 1 && v.AllConnections.Count == 0) { //if we have at least 2 intersections but this one isn't connected to any of them
                emptyIntersections.Add(v); // mark this intersection for deletion and don't consider it as the nearest node
                continue;
            }

            float dist = m.Distance(v.Position, q);
            if (dist < smallestDist) {
                smallestDist = dist;
                closest = v;
            }
        }

        if (emptyIntersections.Count > 0) { 
            foreach(Intersection v in emptyIntersections) {
                intersections.Remove(v);
            }
        }

        if (closest == null) 
            print("closest null");
        return closest;
    }

    public void AddRandomIntersectionToRoadNetwork() {
        Intersection qRand = CigenFactory.CreateIntersection(RandomPosition(), this);  //get a random position
        Intersection qNear = NearestIntersection(qRand.Position); //find the nearest intersection to the random position
        List<Intersection> connections = qNear.AllConnections;
        bool failed = true;
        if (connections.Count <= 0) {                              //if it has no connections...
            for (int i = 0; i < settings.intersectionPlacementAttempts; i++) { 
                if (IsValidRoad(qNear.Position, qRand.Position)) {
                    failed = false;
                    break;
                } else {
                    print(qRand.GetInstanceID() + ": Invalid road, moving to random nearby location.");
                    qRand.MoveIntersection(RandomPosition());
                    qNear = NearestIntersection(qRand.Position); 
                }
            }
            if (failed) {
                print(qRand.GetInstanceID() + ": failed to find place for intersection, destroying.");      
                Destroy(qRand.gameObject);
                return;
            }
            CigenFactory.CreateRoad(qNear, qRand);
        } else {                                                    //otherwise
            Intersection otherNode = connections[0];                //try to find a closer 
            Vector3 bestPos = otherNode.Position;
            for (int i = 0; i < settings.intersectionPlacementAttempts; i++) { 
                connections = qNear.AllConnections;
                float smallestDist = Vector3.Distance(qRand.Position, bestPos);
                foreach (Intersection intersection in connections) {
                    Vector3 closestPointOnLineBetweenNodes = GetClosestPointOnLineSegment(qNear.Position, intersection.Position, qRand.Position);
                    float dist = Vector3.Distance(qRand.Position, closestPointOnLineBetweenNodes);
                    if (dist < smallestDist) {
                        smallestDist = dist;
                        bestPos = closestPointOnLineBetweenNodes;
                        otherNode = intersection;
                    }
                }

                if (IsValidRoad(bestPos, qRand.Position)) {
                    failed = false;
                    break;
                } else {
                    print(qRand.GetInstanceID() + ": Invalid road, moving to random nearby location.");
                    qRand.MoveIntersection(RandomPosition());
                    qNear = NearestIntersection(qRand.Position);
                } 
            }

            if (failed) {            
                print(qRand.GetInstanceID() + ": failed to find place for intersection, destroying.");      
                Destroy(qRand.gameObject);
                return;
            }

            Intersection newNode = qNear;
            if (Vector3.Distance(bestPos, qNear.Position) > settings.maxIntersectionMergeRadius) {
                newNode = Intersection.CreateIntersectionBetweenIntersections(qNear, otherNode, bestPos);
                intersections.Add(newNode);
            }
                
            Intersection[] extraNodes = TransformToMetric(newNode.Position, qRand.Position); //add any extra nodes (intersections) due to constraints
            if (extraNodes.Length > 0) {
                //connect all extra nodes downstream from newNode to qRand
                foreach (Intersection extraNode in extraNodes) {
                    intersections.Add(extraNode);
                    CigenFactory.CreateRoad(newNode, extraNode);
                    newNode = extraNode;
                }
            }
            CigenFactory.CreateRoad(newNode, qRand);
        }
        intersections.Add(qRand);
    }

    private Intersection[] TransformToMetric(Vector3 nearestPoint, Vector3 randomPoint) {
        //print("transforming to metric!");
        Vector3[] positionsConformingToMetric = m.ExtraVerticesBetween(nearestPoint, randomPoint);
        
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

        if (distance < 0) {
            return lineStart;
        }
        else if (distance > 1) {
            return lineEnd;
        }
        return lineStart + AB * distance;
    }
}
