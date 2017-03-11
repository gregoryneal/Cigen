using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cigen.Maths;
public struct VertexWithIndex {
    public Vector2 vertex;
    public int index;

    public VertexWithIndex(Vector2 vertex, int index) {
        this.vertex = vertex;
        this.index = index;
    }

    public bool Is(VertexWithIndex other) {
        if (other.vertex == this.vertex && other.index == this.index) {
            return true;
        }

        return false;
    }
}

public static class HullMesher2D {

    /// <summary>
    /// Builds a 2d polygon in the xz plane.
    /// </summary>
    /// <param name="vertices"></param>
    /// <returns></returns>
    public static Mesh BuildPolygon(Vector3[] vertices, out Vector2 mainTextureScale) {
        Vector2[] vs = vertices.Select(v=>new Vector2(v.x, v.z)).ToArray();

        Mesh m = new Mesh();

        m.Clear();
        m.vertices = vertices.Select(v=>new Vector3(v.x, 0, v.z)).ToArray();
        m.triangles = TriangulateHull(vs);

        m.RecalculateNormals();
        m.RecalculateBounds(); //calculate the bounds before calculating UV's

        Vector2[] uvs = new Vector2[vertices.Length];
        Bounds bounds = m.bounds;
        int i = 0;
        while (i < uvs.Length) {
            uvs[i] = new Vector2(vertices[i].x + bounds.extents.x, vertices[i].y / bounds.size.z); // simple planar uv mapping
            i++;
        }
        m.uv = uvs;
        mainTextureScale = new Vector2(1f/bounds.size.x, bounds.size.z);

        return m;   
    }

    public static int[] TriangulateHull(Vector2[] inputVerts)
    {
        if (inputVerts.Length < 3) {
            if (inputVerts.Length == 2) {
                return new int[] { 0, 1, };
            }

            return null;
        }

        List<VertexWithIndex> vertices = new List<VertexWithIndex>();
        for (int i = 0; i < inputVerts.Length; i++)
        {
            vertices.Add(new VertexWithIndex(inputVerts[i], i));
        }        

        List<int> tris = new List<int>();
        
        VertexWithIndex randomSelected = vertices.OrderBy(a=>a.vertex.y+a.vertex.x).First(); // Heuristic to pick the lowest x+y value results in all angles being in [0, 90]
        List<VertexWithIndex> restOfVertices = vertices.Where(v=>!v.Is(randomSelected)).ToList();

        Stack<VertexWithIndex> V = new Stack<VertexWithIndex>(restOfVertices.OrderBy(x=>x.vertex.PositiveAngleTo(randomSelected.vertex))); //The issue is here, it isn't ordering them correctly use the debugger for this method
        VertexWithIndex secondVertexInTriangle = V.Pop();
        VertexWithIndex firstPoppedVertex = secondVertexInTriangle;
        //So what this algorithm does is triangulates by connecting a vertex with all other vertices.
        //Then 
        //It only works for convex hulls.
        while (V.Count > 0) {
            int[] newTriangle = new int[3]; //EnsureCorrectOrdering(randomSelected, secondVertexInTriangle, V.Peek());
            tris.Add(randomSelected.index);
            tris.Add(secondVertexInTriangle.index);

            if (V.Count == 0)
                tris.Add(firstPoppedVertex.index);
            else
                tris.Add(V.Peek().index);

            secondVertexInTriangle = V.Pop();
        }
        

        return tris.ToArray();
    }

    // Given three vertices, return their indices in clockwise order
    static int[] EnsureCorrectOrdering(VertexWithIndex[] triangle) {
        return EnsureCorrectOrdering(triangle[0], triangle[1], triangle[2]);
    }

    static int[] EnsureCorrectOrdering(VertexWithIndex a, VertexWithIndex b, VertexWithIndex c) {
        int[] indices = new int[3];
        Vector3 AC = c.vertex - a.vertex;
        AC = new Vector3(AC.x, AC.z, AC.y);
        Vector3 AB = b.vertex - a.vertex;
        AB = new Vector3(AB.x, AB.z, AB.y);


        if (Vector3.Cross(AC, AB).sqrMagnitude > 0) {
            indices[0] = a.index;
            indices[1] = b.index;
            indices[2] = c.index;
        } else {
            indices[0] = a.index;
            indices[1] = c.index;
            indices[2] = b.index;
        }

        return indices;
    }
}