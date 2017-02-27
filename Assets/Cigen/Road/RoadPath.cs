using Cigen.Factories;
using Cigen.MetricConstraint;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Essentially a list of directly connected intersections that form a path.
/// Uses the chosen MetricConstraint to create roads and intersections conforming
/// to it.
/// </summary>
public class RoadPath {

	public Intersection exposedHead;
    public Intersection exposedChild;

    public Vector3 startPosition;
    public Vector3 endPosition;

    private List<Intersection> hiddenIntersections = new List<Intersection>();
    private City city;

    public RoadPath(Intersection parent, Intersection child) {
        exposedHead = parent;
        exposedChild = child;
        startPosition = parent.Position;
        endPosition = child.Position;
        city = parent.city;
    }

    public RoadPath(Vector3 start, Vector3 end, City city) {
        startPosition = start;
        endPosition = end;
        this.city = city;
    }

    public void BuildPath() {
        List<Vector3> path = city.metricConstraint.ProcessPathNoEndpoints(startPosition, endPosition);

        Intersection start = exposedHead;
        Intersection end = exposedChild;
        
        if (exposedHead == null) //initialized with Init(Intersection, Intersection);
        {
            start = CigenFactory.CreateOrMergeIntersection(startPosition, city);
        }
        if (exposedChild == null) {
            end = CigenFactory.CreateOrMergeIntersection(endPosition, city);
        }

        Intersection curr = start;
        foreach (Vector3 v in path) {
            Intersection newIntersection = CigenFactory.CreateOrMergeIntersection(v, city);
            CigenFactory.CreateRoad(curr, newIntersection);
            curr = newIntersection;
        }
        CigenFactory.CreateRoad(curr, end);
    }

    public List<Vector3> Path {
        get {
            return city.metricConstraint.ProcessPath(startPosition, endPosition);
        }
    }
}
