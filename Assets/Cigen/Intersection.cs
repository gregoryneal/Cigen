using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Intersection : MonoBehaviour {
    
	public Vector3 Position { get { return _localPosition; } }
    public List<Intersection> AllConnections {
        get {
            List<Intersection> ret = new List<Intersection>();
            if (parent != null)
                ret.Add(parent);
            if (children.Count > 0)
                ret.AddRange(children);
            return ret;
        }
    }
    private Vector3 _localPosition;
    private Intersection parent = null;
    private List<Intersection> children = new List<Intersection>();
    public City city { get; private set; }
    public List<Road> roads { get; private set; }

    public List<Vector3> verts = new List<Vector3>();
    public void AddVerts(params Vector3[] vertices) {
        foreach (Vector3 v in vertices) {
            if (!verts.Any(q=>Vector3.Distance(v,q)<=city.settings.maxIntersectionVerticesMergeRadius))
                verts.Add(v);
        }
    }

    public void MoveIntersection(Vector3 newLocalPosition) {
        transform.localPosition = newLocalPosition;
        _localPosition = newLocalPosition;

        foreach (Road road in roads) {
            road.Rebuild();
        }

        //StartCoroutine(BuildMesh());
    }

    public void AddRoad(Road road) {
        if (roads == null)
            roads = new List<Road>();

        if (!roads.Contains(road))
            roads.Add(road);
        
        //StartCoroutine(BuildMesh());
    }

    public void Init(Vector3 position, City city) {
        transform.position = city.transform.position + position;
        _localPosition = position;
        transform.rotation = city.transform.rotation;
        this.city = city;
        transform.parent = city.transform;
    }

    private List<Vector3> LocalSpaceVertices() {

        if (verts.Count > 0) {
            //print("Found " + verts.Count + " vertices to draw!");
            return verts;
        }
        
        List<Vector3> v = new List<Vector3>();
        //print("Drawing default square!");
        float roadWidth = city.settings.roadDimensions.x;
        v.Add((roadWidth / 2f) * (-transform.forward - transform.right));
        v.Add((roadWidth / 2f) * (transform.forward - transform.right));
        v.Add((roadWidth / 2f) * (transform.forward + transform.right));
        v.Add((roadWidth / 2f) * (-transform.forward + transform.right));

        return v;
    }

    public IEnumerator BuildMesh() {

        if (roads.Count < 2) yield break;

        float roadHeight = city.settings.roadDimensions.y;
        Texture texture = city.settings.roadTexture;

        Vector3[] vertices = LocalSpaceVertices().ToArray();
        

        if (vertices.Length < 3) { 
            //print("Not enough vertices to make a shape! Vertex count: " + vertices.Length);
            yield break;
        }

        Vector2 mainTexScale;
        Mesh m = HullMesher2D.BuildPolygon(vertices, out mainTexScale);

        Material mat = GetComponent<MeshRenderer>().material;
        if (texture != null) {        
            mat.mainTexture = texture;
            mat.mainTextureScale = mainTexScale;
        } else {
            mat.color = Color.gray;
        }


        GizmosToDraw = new List<Vector3>();
        GizmosToDraw.AddRange(vertices);

        Quaternion rotation = Quaternion.identity;
        Vector3 z = Vector3.zero;
        Matrix4x4 op0 = Matrix4x4.TRS(z, rotation, Vector3.one);
        Matrix4x4 op1 = Matrix4x4.TRS(transform.up * roadHeight, rotation, Vector3.one);
        Matrix4x4[] ops = new Matrix4x4[] { op0, op1, };

        MeshExtrusion.ExtrudeMesh(m, GetComponent<MeshFilter>().mesh, ops, true);

        m.RecalculateNormals();
        m.RecalculateBounds();

        //print("Finished building intersection.");
        yield break;
    }

    public void ConnectToIntersection(Intersection child) {
        if (this == child)
            return;

        children.Add(child);
        child.parent = this;
        //Vector3 look = child.Position - transform.position;
    }

    public void RemoveConnection(Intersection child) {
        child.parent = null;
        children.Remove(child);
    }

    private List<Vector3> GizmosToDraw = new List<Vector3>(); 
    private void OnDrawGizmosSelected()
    {
        if (GizmosToDraw.Count > 0) {
            foreach (Vector3 v in GizmosToDraw) {
                Gizmos.DrawSphere(transform.TransformPoint(v), 0.4f);
            }
        }
    }

    private IEnumerator FocusOn() {
        Transform t = Camera.main.transform;
        t.position = transform.position + new Vector3(UnityEngine.Random.Range(3, 4f), UnityEngine.Random.Range(5, 8f), UnityEngine.Random.Range(3, 4f));
        t.LookAt(transform.position);
        yield return new WaitUntil(()=>Input.GetKeyDown(KeyCode.Space));
    }
}