using System.Collections.Generic;
using Clothoid;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ClothoidSegmentExplorer : MonoBehaviour
{
    public bool markNodes = true;
    public Color nodeColor = Color.red;
    [Min(.001f)]
    public float nodeSize = 0.5f;
    [Min(.001f)]
    public float lineWidth = 1f;
    public Color lineColor = Color.blue;
    [Min(0)]
    public float startArcLength = 0;
    public float endArcLength = 1;
    public float startCurvature = 0;
    public float endCurvature = 0.5f;
    [Tooltip("this is the scaling paramater")]
    public float B = 1;

    [Min(2)]
    public int numSamples = 10;
    private ClothoidSegment segment;
    private List<GameObject> spawnedGameObjects = new List<GameObject>();
    private LineRenderer lr;

    private bool awake = false;
    
    void Start()
    {
        this.awake = true;
        this.lr = GetComponent<LineRenderer>();
        Redraw();
    }

    private void Redraw() {
        this.segment = new ClothoidSegment(this.startArcLength, this.endArcLength, this.startCurvature, this.endCurvature, this.B);
        //DrawOrderedVector3s(this.segment.CalculateDrawingNodes(this.numSamples));
    }

    void OnValidate()
    {
        if (awake) {
            Redraw();     
        }
    }

    void DrawOrderedVector3s(List<Vector3> positions, float zOffset = 0) {
        foreach (GameObject go in this.spawnedGameObjects) {
            Destroy(go);
        }
        
        float sumOfLength = 0;
        List<Vector3> newPositions = new List<Vector3>();
        for (int i = 0; i < positions.Count; i++) {
            Vector3 p = positions[i] + (Vector3.forward * zOffset);
            if (zOffset != 0) newPositions.Add(p);
            if (i < positions.Count-1) sumOfLength += Vector3.Distance(positions[i], positions[i+1]);
            
            if (this.markNodes) {
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                g.transform.localScale = Vector3.one * this.nodeSize;
                if (zOffset != 0) g.transform.position = p;
                else g.transform.position = positions[i];
                g.GetComponent<Renderer>().material.color = nodeColor;
                Destroy(g.GetComponent<Collider>());
                this.spawnedGameObjects.Add(g);
            }
        }

        //sumOfLength += Vector3.Distance(positions[^2], positions[^1]);
        if (zOffset != 0) positions = newPositions;

        lr.startWidth = this.lineWidth;
        lr.endWidth = this.lineWidth;
        lr.startColor = this.lineColor;
        lr.endColor = lr.startColor;
        lr.positionCount = positions.Count;
        lr.SetPositions(positions.ToArray());
    }
}
