using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cigen.MetricConstraint;
using System;

public class GridConstraint : ManhattanConstraint {
    public Vector3 RoundToScale(Vector3 v) {
        float scale = MetricConstraintSettings.GridSpacing;
        Func<float, float> rts = i => Mathf.Round(i/scale)*scale; //round to scale
        return new Vector3(rts(v.x), rts(v.y), rts(v.z));
    }

    public override Vector3[] ExtraVerticesBetween(Vector3 start, Vector3 end)
    {
        
        List<Vector3> verts = new List<Vector3>();
        Vector3 s = RoundToScale(start);
        Vector3 e = RoundToScale(end);
        if (!verts.Contains(s))
            verts.Add(s);
        s.x = e.x;
        if (!verts.Contains(s))
            verts.Add(s);
        s.y = e.y;
        if (!verts.Contains(s))
            verts.Add(s);
        s.z = e.z;
        if (!verts.Contains(s))
            verts.Add(s);
        return verts.ToArray();
    }

    public override Vector3[] ProcessPoints(params Vector3[] points)
    {
        Vector3[] ret = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++) {
            ret[i] = RoundToScale(points[i]);
        }

        return ret;
    }
}
