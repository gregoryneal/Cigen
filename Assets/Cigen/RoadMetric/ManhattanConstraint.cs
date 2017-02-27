using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cigen.MetricConstraint;
using System;

/// <summary>
/// Allows Intersections to be placed anywhere but roads can only flow horizontal or vertical (with some exceptions)
/// </summary>
public class ManhattanConstraint : MetricConstraint {
    public ManhattanConstraint(CitySettings settings) : base(settings) { }

    public override float Distance(Vector3 a, Vector3 b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);
    }

    public override List<Vector3> ProcessPath(Vector3 start, Vector3 end)
    {
        start = ProcessPoint(start);
        end = ProcessPoint(end);

        Vector3 positionToAdd = start;
        Vector3 delta = end - start;

        List<Vector3> ret = new List<Vector3> { start, };
        positionToAdd = ProcessPoint(positionToAdd + delta.x * Vector3.right);
        if (delta.x >= settings.minimumRoadLength && !ret.Contains(positionToAdd)) {
            ret.Add(positionToAdd);
        }

        positionToAdd = ProcessPoint(positionToAdd + delta.y * Vector3.up);
        if (delta.y >= settings.minimumRoadLength && !ret.Contains(positionToAdd)) {
            ret.Add(positionToAdd);
        }
        
        positionToAdd = ProcessPoint(positionToAdd + delta.z * Vector3.forward);
        if (delta.z >= settings.minimumRoadLength && !ret.Contains(positionToAdd)) {
            ret.Add(positionToAdd);
        }

        ret.Add(end);

        return ret;
    }
}
