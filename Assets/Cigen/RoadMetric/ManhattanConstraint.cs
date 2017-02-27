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

    public override Vector3[] ProcessPoints(params Vector3[] points)
    {
        return points;
    }
}
