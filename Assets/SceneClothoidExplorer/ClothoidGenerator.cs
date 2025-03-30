using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Clothoid {

    [RequireComponent(typeof(ClothoidSolutionSinghMcCrae))]
    public class ClothoidGenerator : MonoBehaviour {
        public int numSamples = 45;

        public bool markNodes = true;
        public float lineWidth = 1;

        [Range(0, 14f)]
        public float maxError = 0.1f;
        public LineRenderer lrNodes;
        public LineRenderer lrLKNodes;
        public LineRenderer lrSegmentedRegressionNodes;
        public int polylineIndex = 2; 
        bool awake = false;

        private List<GameObject> spawnedGameObjects = new List<GameObject>();

        private ClothoidCurve clothoidCurve;

        ClothoidSolutionSinghMcCrae solution;
        void Start() {
            solution = GetComponent<ClothoidSolutionSinghMcCrae>();
            awake = true;
            //CreateRandomClothoidCurve();
        }        

        [ContextMenu("Make a cool graphs")]
        public void MakeCoolGraph() {
            //DrawOrderedVector3s(CreateLinearDecreasingCurvatureCurve(maxArcLength, curvature, numSamples));
            List<Vector3> verts = CrazyCurve(this.numSamples);//OrderedVertices(UnityEngine.Random.Range(20, 30));
            this.clothoidCurve = this.solution.CalculateClothoidCurve(verts);

            /*Debug.Log("Positions from polyline index vs resultant curve sampling: (dindex, dsampling) ");
            for (int i = 0; i+1 < this.clothoidCurve.PolylineCount; i++) {
                Vector3 firstPos = this.clothoidCurve.GetPositionFromPolyline(i);
                Vector3 secondPos = this.clothoidCurve.GetPositionFromPolyline(i+1);
                Vector3 firstClothPos = this.clothoidCurve.GetPositionFromArcLength(this.clothoidCurve.GetStartingArcLengthFromPolylineIndex(i));
                Vector3 secondClothPos = this.clothoidCurve.GetPositionFromArcLength(this.clothoidCurve.GetStartingArcLengthFromPolylineIndex(i+1));
                //Debug.Log($"({secondPos.x/firstPos.x}, {secondPos.z/firstPos.z}) => ({secondClothPos.x/firstClothPos.x}, {secondClothPos.z/firstClothPos.z})");
                //Debug.Log($"({firstClothPos.x/firstPos.x}, {firstClothPos.z/firstPos.z}) => ({secondClothPos.x/secondPos.x}, {secondClothPos.z/secondPos.z})");
            }*/

            //random clothoid segment to make sure im not crazy
            DrawOrderedVector3s(verts, this.lrNodes, 30);
            RedrawCurve();
        }

        [ContextMenu("Redraw curve")]
        public void RedrawCurve() {
            DrawOrderedVector3s(this.clothoidCurve.CalculateDrawingNodes(2*this.numSamples), this.lrSegmentedRegressionNodes);
        }

        void PlotLKGraph() {
            List<Vector3> positions = new List<Vector3>();
            for (int i = 0; i < this.solution.Count; i++) {
                if (this.solution.LKNodeMap_norm.TryGetValue(this.solution.polyline[i], out (float, float) LKNode)) {
                    positions.Add(new Vector3(LKNode.Item1, 0, LKNode.Item2));
                }
            }
            DrawOrderedVector3s(positions, this.lrLKNodes);
        }

        List<Vector3> CrazyCurve(int num) {
            List<Vector3> positions = new List<Vector3>(){Vector3.zero};
            while (positions.Count < num) {
                float val = UnityEngine.Random.value;
                int numNewSegments = UnityEngine.Random.Range(10, 20);
                switch (val < 0.5f) {
                    case true:
                        //add straight line segments
                        float xOffset;  
                        float zOffset;
                        Vector3 newOffset;
                        for (int i = 0; i < numNewSegments; i++) {
                            xOffset = UnityEngine.Random.Range(4, 6f);  
                            zOffset = UnityEngine.Random.Range(-1f, 1f);
                            newOffset = positions[^1] + new Vector3(xOffset, 0, zOffset);
                            positions.Add(newOffset);
                        }
                        break;
                    case false:
                        //add curvy segments
                        float deflectionAngle = 0;
                        float maxDeflection = UnityEngine.Random.Range(-.1f, .1f);
                        float length = UnityEngine.Random.Range(4, 6f);
                        for (int i = 0; i < numNewSegments; i++) {
                            deflectionAngle += maxDeflection * i / numNewSegments;
                            Vector3 newPos = positions[^1] + (new Vector3(Mathf.Cos(deflectionAngle), 0, Mathf.Sin(deflectionAngle))*length);
                            positions.Add(newPos);
                        }
                        break;
                }
            }
            return positions;
        }

        void DrawOrderedVector3s(List<Vector3> positions, LineRenderer lr, float zOffset = 0) {
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
                    g.transform.localScale = Vector3.one;
                    if (zOffset != 0) g.transform.position = p;
                    else g.transform.position = positions[i];
                    g.GetComponent<Renderer>().material.color = Color.red;
                    Destroy(g.GetComponent<Collider>());
                    this.spawnedGameObjects.Add(g);
                }
            }

            //sumOfLength += Vector3.Distance(positions[^2], positions[^1]);
            if (zOffset != 0) positions = newPositions;

            lr.startWidth = this.lineWidth;
            lr.endWidth = this.lineWidth;
            //lr.startColor = UnityEngine.Random.ColorHSV(0f, 1f, 1, 1, 1, 1);
            //lr.endColor = lr.startColor;

            lr.positionCount = positions.Count;
            lr.SetPositions(positions.ToArray());
        }
        void OnValidate() {
            if (awake) {
            }
        }

        [ContextMenu("Estimate Curvature at index")]
        public void PrintLKPointAtIndex() {
            Debug.Log(this.solution.LKNodeMap[this.solution.polyline[this.polylineIndex]]);
        }
    }
}