using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cigen.MetricConstraint;
using System;

public class EuclideanConstraint : MetricConstraint {
    public override float Distance(Vector3 start, Vector3 end)
    {
        return Vector3.Distance(start, end);
    }

    public override Vector3[] ExtraVerticesBetween(Vector3 start, Vector3 end)
    {
        return null;
    }

    public override Vector3[] ProcessPoints(params Vector3[] points)
    {
        return points;
    }
}
