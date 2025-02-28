using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cigen;
using Cigen.ImageAnalyzing;
using Cigen.Structs;
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
            if (!verts.Any(q=>Vector3.Distance(v,q)<=city.Settings.maxIntersectionVerticesMergeRadius))
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

    /// <summary>
    /// Extend the intersection with a new Road segment terminated by a new Intersection
    /// </summary>
    /// <param name="newPosition">The position to build the new intersection at.</param>
    /// <returns>The created intersection.</returns>
    public Intersection Extend(Vector3 newPosition) {
        Intersection newIntersection = city.CreateOrMergeNear(newPosition);
        city.CreatePath(this, newIntersection);
        return newIntersection;
    }

    public void AddRoad(Road road) {
        if (roads == null)
            roads = new List<Road>();

        if (!roads.Contains(road))
            roads.Add(road);
        
        //StartCoroutine(BuildMesh());
    }

    //set the transform, parent and references
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
        float roadWidth = city.Settings.roadDimensions.x;
        v.Add((roadWidth / 2f) * (-transform.forward - transform.right));
        v.Add((roadWidth / 2f) * (transform.forward - transform.right));
        v.Add((roadWidth / 2f) * (transform.forward + transform.right));
        v.Add((roadWidth / 2f) * (-transform.forward + transform.right));

        return v;
    }

    public IEnumerator BuildMesh() {

        if (roads.Count < 2) yield break;

        float roadHeight = city.Settings.roadDimensions.y;
        Texture texture = city.Settings.roadTexture;

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

public class RoadSegment : MonoBehaviour {
    //which segments are connected to this start position
    public List<RoadSegment> StartNeighbors = new List<RoadSegment>();
    //which segments are connected to this end position
    public List<RoadSegment> EndNeighbors = new List<RoadSegment>();
    public Vector3 StartPosition { get; set; }
    public Vector3 EndPosition { get; set; }
    //these control the offset on the y axis of the start and end nodes of the segment. this will allow us to overlap segments with other segments and build cool shit (hopefully)
    public float StartOffsetY = 0;
    public float EndOffsetY = 0;
    public City City { get; private set; }

    /// <summary>
    /// Highway flag, any branches from this will automatically be highways themselves. Only roads can connect to highways using their own heuristics.
    /// </summary>
    public bool IsHighway { get; private set; }
    /// <summary>
    /// Does this segment go over water?
    /// </summary>
    public bool IsBridge { get; set; }
    public bool IsTunnel { get; set; }
    //make sure this is less than segmentLength.
    public float SegmentLength { get { return this.SegmentDirection.magnitude; } }
    public Vector3 SegmentDirection { get { return this.EndPosition - this.StartPosition; }}
    public float MaxAngle { get; private set; }
    //how long should our segments be when generating them?
    public float IdealSegmentLength { get; private set; }
    /// <summary>
    /// Is this segment the axiom?
    /// </summary>
    public bool IsAxiom = false;
    /// <summary>
    /// If set to true this segment will not have its endpoints considered for growth.
    /// </summary>
    public bool StopGrowing = false;

    public bool ISNONE = false;
    public int MaxBranches { get; private set; }
    public float MergeDistance { get; private set; }

    public static RoadSegment NONE() {
        RoadSegment ret = new RoadSegment();
        ret.ISNONE = true;
        return ret;
    }

    public RoadSegment() {}

    private void InitSettingsValues() {        
        this.IdealSegmentLength = this.IsHighway? CitySettings.instance.highwayIdealSegmentLength : CitySettings.instance.streetIdealSegmentLength;
        this.MaxAngle = this.IsHighway? CitySettings.instance.maxAngleBetweenHighwayBranchSegments : CitySettings.instance.maxAngleBetweenStreetBranchSegments;
        this.MaxBranches = this.IsHighway? CitySettings.instance.maxHighwayBranches : CitySettings.instance.maxStreetBranches;
        this.MergeDistance = this.IsHighway? CitySettings.instance.highwayConnectionThreshold : CitySettings.instance.streetConnectionThreshold;
    }

    /// <summary>
    /// Naivelly build a segment at two positions
    /// </summary>
    /// <param name="startPosition"></param>
    /// <param name="endPosition"></param>
    /// <param name="isHighway"></param>
    public void Init(Vector3 startPosition, Vector3 endPosition, bool isHighway, bool isBridge, bool isTunnel) {
        this.StartPosition = startPosition;
        this.EndPosition = endPosition;
        this.IsHighway = isHighway;
        this.IsBridge = isBridge;
        this.IsTunnel = isTunnel;
        InitSettingsValues();
        Draw();
    }

    public void Init(Vector3 startPosition, Vector3 endPosition, RoadSegment reference) {
        this.StartPosition = startPosition;
        this.EndPosition = endPosition;
        this.IsHighway = reference.IsHighway;
        InitSettingsValues();

        this.City = reference.City;
        transform.SetPositionAndRotation(this.City.transform.position + startPosition, this.City.transform.rotation);
        transform.parent = this.City.transform;
        this.name = "Intersection";
        Draw();

        //figure out the neighbor situation
        if (startPosition == reference.StartPosition) {
            //start aligned with start
            this.StartNeighbors.Add(reference);
            reference.StartNeighbors.Add(this);
        } else if (startPosition == reference.EndPosition) {
            //start aligned with end
            this.StartNeighbors.Add(reference);
            reference.EndNeighbors.Add(this);
        } else if (endPosition == reference.StartPosition) {
            //end aligned with start
            this.EndNeighbors.Add(reference);
            reference.StartNeighbors.Add(this);
        } else if (endPosition == reference.EndPosition) {
            //end aligned with end
            this.EndNeighbors.Add(reference);
            reference.EndNeighbors.Add(this);
        } else {
            //not neighbors
            Debug.Log("Not neighbors");
        }
    }

    //Create a new intersection in a legal location on the map
    //we will pass this through LocalConstraints 
    public void InitAxiom(City city) {
        this.IsAxiom = true;
        this.City = city;        
        this.name = "Intersection";
        InitSettingsValues();

        if (this.IsHighway) {
            //see if we can find a random point within a population center
            if (ImageAnalysis.RandomPointWithinBounds(out Vector3 randomOffset, true)) {
                transform.SetPositionAndRotation(city.transform.position + randomOffset, city.transform.rotation);
                transform.parent = city.transform;

                //find a random population center and grow towards it

                //use this as start position
                StartPosition = randomOffset;
                EndPosition = ImageAnalysis.PointNear(StartPosition, this.IdealSegmentLength);

                //Debug.Log($"StartPosition: {StartPosition}, TerrainHeight: {ImageAnalysis.TerrainHeightAt(StartPosition.x, StartPosition.z, this.Settings)}");
                //Debug.Log($"EndPosition: {EndPosition}, TerrainHeight: {ImageAnalysis.TerrainHeightAt(EndPosition.x, EndPosition.z, this.Settings)}");
                //Debug.Log($"Distance between start and end: {(EndPosition-StartPosition).magnitude}");
                int i = 0;
                int maxTries = 1000;
                //look for end position on cirle of radius segmentLength around startposition.
                while (ImageAnalysis.PointInBounds(EndPosition) == false) {
                    if (i > maxTries) {
                        Debug.LogError("Bro we couldn't even generate an end point for your start point. That blows, maybe try again or increase the max tries value near this error message if you can find me?");
                        return;
                    }
                    EndPosition = ImageAnalysis.PointNear(StartPosition, this.IdealSegmentLength);
                }
            } else {
                Debug.LogError("Bro we couldn't even generate a single intersection. Try again or check your maps and stuff!");
                return;
            }
        } else {
            //branch off a highway segment inside a random population center
        }

        Draw();
    }

    public void InitAxiom(City city, bool isHighway) {
        this.IsHighway = isHighway;
        InitAxiom(city);
    }

    /// <summary>
    /// Attempt to merge a road segment endpoint with another road segment endpoint. It can fail if criteria are not met:
    /// 1. The connection point has too many neighbors
    /// 2. The proposed connection point would leave the roads close to parallel.
    /// 3. to be determined...
    /// </summary>
    /// <param name="to">The segment we are attempting to merge to.</param>
    /// <param name="fromStart">Are we trying to merge the start of the input segment? If false we assume we are merging the end of the input segment.</param>
    /// <param name="toStart">Are we merging to the start of the to Segment? If false we assume we are merging to the end of the to segment.</param>
    /// <returns>Whether the merge was succesful. We will have altered the value of the input segment.</returns>
    public bool Merge(RoadSegment to, bool fromStart, bool toStart) {
        int maxNeighbors =  this.IsHighway ? CitySettings.instance.maxHighwayNeighbors : CitySettings.instance.maxStreetNeighbors;
        float minAngle = this.IsHighway ? CitySettings.instance.minAngleBetweenHighwayMergeSegments : CitySettings.instance.minAngleBetweenStreetMergeSegments;
        //if we are at capacity just short circuit the logic and return.
        if (toStart && to.StartNeighbors.Count > maxNeighbors) return false;
        if (toStart == false && to.EndNeighbors.Count > maxNeighbors) return false;

        //also check the angle using the proposed connection point as the pivot point or "base" of the two vectors for measurement
        //vec direction -> final - initial points in the direction of final from initial
        if (fromStart && toStart) {
            if (UnityEngine.Vector3.Angle(this.EndPosition-this.StartPosition, to.EndPosition-to.StartPosition) > minAngle) {
                this.StartPosition = to.StartPosition;
                this.StartNeighbors.Add(to);
                to.StartNeighbors.Add(this);
                Debug.Log("Merged segments!", this);
            } else {
                return false;
            }
        }
        if (fromStart == false && toStart) {
            if (UnityEngine.Vector3.Angle(this.StartPosition-this.EndPosition, to.EndPosition-to.StartPosition) > minAngle) {
                this.EndPosition = to.StartPosition; 
                this.EndNeighbors.Add(to);
                to.StartNeighbors.Add(this);
                Debug.Log("Merged segments!", this);
            } else {
                return false;
            }
        }
        if (fromStart && toStart == false) {
            if (UnityEngine.Vector3.Angle(this.EndPosition-this.StartPosition, to.StartPosition-to.EndPosition) > minAngle) {
                this.StartPosition = to.EndPosition;
                this.StartNeighbors.Add(to);
                to.EndNeighbors.Add(this);
                Debug.Log("Merged segments!", this);
            } else {
                return false;
            }
        }
        if (fromStart == false && toStart == false) {
            if (UnityEngine.Vector3.Angle(this.StartPosition-this.EndPosition, to.StartPosition-to.EndPosition) > minAngle) {
                this.EndPosition = to.EndPosition;
                this.EndNeighbors.Add(to);
                to.EndNeighbors.Add(this);
                Debug.Log("Merged segments!", this);
            } else {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Attempt to merge two segments that intersect on the x and z plane
    /// </summary>
    /// <param name="to"></param>
    /// <param name="intersectionPoint"></param>
    /// <param name="fromStart"></param>
    /// <returns></returns>
    public bool MergeCrossing(RoadSegment to, Vector3 intersectionPoint, bool fromStart) {
        return false;
    }

    /// <summary>
    /// Delete the segment, remove itself from its neighbors.
    /// </summary>
    void OnDestroy() {
        foreach (RoadSegment neighbor in this.StartNeighbors) {
            Debug.Log("REMOVING FROM NEIGHBORS");
            neighbor.StartNeighbors.Remove(this);
            neighbor.EndNeighbors.Remove(this);
        }
        foreach (RoadSegment neighbor in this.EndNeighbors) {
            Debug.Log("REMOVING FROM NEIGHBORS");
            neighbor.StartNeighbors.Remove(this);
            neighbor.EndNeighbors.Remove(this);
        }
    }

    public void Draw() {
        LineRenderer lr = gameObject.AddComponent<LineRenderer>();
        Vector3 offset = Vector3.up * 2f;
        lr.positionCount = 2;
        lr.SetPositions(new Vector3[]{StartPosition + offset, EndPosition + offset});
        lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
        lr.startWidth = .5f;
        lr.endWidth = .5f;
        if (this.IsBridge) {
            //Debug.Log("I AM A BRIDGE");
            lr.startColor = Color.blue;
            lr.endColor = Color.blue;
        } else if (this.IsTunnel) {
            //Debug.Log("I AM A TUNNEL");
            lr.startColor = Color.yellow;
            lr.endColor = Color.yellow;
        } else {
            //Debug.Log("I AM SIMPLY A ROAD");
            lr.startColor = Color.red;
            lr.endColor = Color.red;
        }
        
        /*GameObject gos = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        GameObject goe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        LineRenderer line = segment.gameObject.AddComponent<LineRenderer>();
        //line.SetPositions()
        gos.transform.localScale = Vector3.one * 3;
        gos.transform.position = segment.StartPosition;
        gos.transform.parent = segment.transform;
        gos.name = "Start";
        gos.GetComponent<Renderer>().material.color = Color.red;
        goe.transform.localScale = Vector3.one * 3;
        goe.transform.position = segment.EndPosition;
        goe.transform.parent = segment.transform;
        goe.GetComponent<Renderer>().material.color = Color.blue;
        goe.name = "End";*/
    }
}