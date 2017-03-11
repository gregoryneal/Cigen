using System;
using System.Collections.Generic;

using UnityEngine;

using Cigen.Factories;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Road : MonoBehaviour {

	public City city { get; private set; }
	public Intersection parentNode;
	public Intersection childNode;
    public bool built { get; private set; }
    public float length { get; private set; }
    public Vector3 direction { get; private set; }    
    public Vector3 Midpoint {
        get {
           return Vector3.Lerp(parentNode.Position, childNode.Position, 0.5f); 
        }
    }
    public Plot leftPlot { get; private set; }
    public Plot rightPlot { get; private set; }

    public Vector3[] worldSpaceVertices { get; private set; } //all vertices
    private Dictionary<Intersection, List<Vector3>> intersectionConnectedVertices = new Dictionary<Intersection, List<Vector3>>(); //these are vertices of the road in world space. They are meant to be consumed by each connecting intersection, which converts them into it's own local space to use as vertices for it's own mesh, so everything matches up nicely ;)

	public void Init(Intersection parent, Intersection child, City city) {
        if (parent.Position == child.Position) { //sanity check
            Destroy(gameObject);
            return;
        }

		this.parentNode = parent;
		this.childNode = child;
		this.city = city;
        this.built = false;
        this.length = Vector3.Distance(parentNode.Position, childNode.Position);
        this.direction = (childNode.Position - parentNode.Position).normalized;
        
        intersectionConnectedVertices[parentNode] = new List<Vector3>();
        intersectionConnectedVertices[childNode] = new List<Vector3>();
        BuildMesh();

        parent.AddRoad(this);
        child.AddRoad(this);
        parent.ConnectToIntersection(child);
        city.roads.Add(this);
        transform.parent = city.transform;
	}

    public void Rebuild() {
        built = false;
        BuildMesh();
    }

    public void BuildMesh() {
        float roadWidth = city.settings.roadDimensions.x;
        float roadHeight = city.settings.roadDimensions.y;
        Texture texture = city.settings.roadTexture;

        Vector3 from = city.transform.TransformPoint(parentNode.Position);
        Vector3 to = city.transform.TransformPoint(childNode.Position);
        Vector3 direction = (from - to).normalized;
        Vector3 offset = direction * roadWidth / 2f;
        from -= offset; //so the endings don't go all the way to the point, they line up neatly with intersections
        to += offset;
        transform.position = from;
        float length = Vector3.Distance(from, to);

        Vector3[] vertices = new Vector3[4];
        vertices[0] = -transform.right * roadWidth / 2f;
        vertices[1] = (transform.forward * length) - (transform.right * roadWidth / 2f);
        vertices[2] = (transform.forward * length) + (transform.right * roadWidth / 2f);
        vertices[3] = transform.right * roadWidth / 2f;

        Vector2 mainTexScale;
        GetComponent<MeshFilter>().mesh = HullMesher2D.BuildPolygon(vertices, out mainTexScale);        
        Material mat = GetComponent<MeshRenderer>().material;
        if (texture != null) {        
            mat.mainTexture = texture;
            mat.mainTextureScale = mainTexScale;
        } else {
            mat.color = Color.gray;
        }

        Mesh m = GetComponent<MeshFilter>().mesh;

        Quaternion rotation = Quaternion.identity;
        Vector3 z = Vector3.zero;
        Matrix4x4 op0 = Matrix4x4.TRS(z, rotation, Vector3.one);
        Matrix4x4 op1 = Matrix4x4.TRS(transform.up * roadHeight, rotation, Vector3.one);
        Matrix4x4[] ops = new Matrix4x4[] { op0, op1, };

        MeshExtrusion.ExtrudeMesh(m, GetComponent<MeshFilter>().mesh, ops, true);

        m.RecalculateNormals();
        m.RecalculateBounds();

        transform.position = from;
        transform.LookAt(to);

        {
            Func<Vector3, Transform, Vector3> a = (v,t) => {
                Vector3 b;
                //converts the vector from this local space into world space, then converts it again into the transforms local space
                b = t.InverseTransformPoint(transform.TransformPoint(v)); //store the vertices in world coordinates
                GizmosToDraw.Add(b);
                return b;
            };

            //maybe add these vertices to the intersections vertex list?
            parentNode.AddVerts(a(vertices[0], parentNode.transform), a(vertices[3], parentNode.transform));
            childNode.AddVerts(a(vertices[1], childNode.transform), a(vertices[2], childNode.transform));
        }
    }

    public void ZonePlots() {
        if (length < city.settings.minimumRoadLength)
            return;

        if (this.leftPlot != null)
            Destroy(this.leftPlot.gameObject);
        if (this.rightPlot != null)
            Destroy(this.rightPlot.gameObject);

        Plot[] plots = CigenFactory.CreatePlots(this);
        this.leftPlot = plots[0];
        this.rightPlot = plots[1];
    }

    //TODO: Add removal of vertices from intersection dictionary
    public void Remove() {
        parentNode.RemoveConnection(childNode);
        city.roads.Remove(this);
        if (leftPlot != null) { 
            city.plots.Remove(leftPlot);
            Destroy(leftPlot.gameObject);
        }
        if (rightPlot != null) { 
            city.plots.Remove(rightPlot);
            Destroy(rightPlot.gameObject);
        }
        Destroy(gameObject);
    }

    //Converts local space vertices into global space ones and sets them in the class variable.
    private void SetVerts(Vector3[] localSpaceVertices) {
        for (int i = 0; i < localSpaceVertices.Length; i++) {
            localSpaceVertices[i] = transform.TransformPoint(localSpaceVertices[i]);
        }

        worldSpaceVertices = localSpaceVertices;
    }    

    public List<Vector3> GetVerticesForIntersection(Intersection intersection) {
        List<Vector3> v;

        try {
            v = intersectionConnectedVertices[intersection];
        } catch {
            v = new List<Vector3>();
        }

        return v;
    }

    private List<Vector3> GizmosToDraw = new List<Vector3>(); 
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red; //red head
        if (intersectionConnectedVertices[parentNode].Count > 0) {
            foreach (Vector3 v in intersectionConnectedVertices[parentNode]) {
                Gizmos.DrawSphere(v, 1f);
            }
        }

        Gizmos.color = Color.blue; //blue child
        if (intersectionConnectedVertices[childNode].Count > 0) {
            foreach (Vector3 v in intersectionConnectedVertices[childNode]) {                
                Gizmos.DrawSphere(v, 1f);
            }
        }
    }
}
