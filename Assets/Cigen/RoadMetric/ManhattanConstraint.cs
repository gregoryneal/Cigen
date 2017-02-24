using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cigen.MetricConstraint;
using System;

public class ManhattanConstraint : MetricConstraint {
    public override float Distance(Vector3 a, Vector3 b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);
    }

    public override Vector3[] ExtraVertices(Vector3 start, Vector3 end)
    {
        List<Vector3> newPos = new List<Vector3>();
        Vector3 dPos = end - start;
        float toBeat = 0;

        if (Mathf.Abs(dPos.x) >= toBeat) { 
            start += new Vector3(dPos.x, 0, 0);
            newPos.Add(start);
        }
        if (Mathf.Abs(dPos.y) >= toBeat) { 
            start += new Vector3(0, dPos.y, 0);
            newPos.Add(start);
        }
        if (Mathf.Abs(dPos.z) >= toBeat) { 
            start += new Vector3(0, 0, dPos.z);
            newPos.Add(start);
        }
        return newPos.ToArray();
    }
}
