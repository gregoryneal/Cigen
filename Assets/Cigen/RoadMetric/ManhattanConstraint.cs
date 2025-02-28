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
        List<Vector3> ret = new List<Vector3> { start, };
        Vector3 positionToAdd;

        if (UnityEngine.Random.value >= 0.5f) {
            positionToAdd = new Vector3(start.x, start.y, end.z);
        } else {
            positionToAdd = new Vector3(end.x, end.y, start.z);
        }
        ret.Add(positionToAdd);
        ret.Add(end);
        return ret;
        
    }
}
