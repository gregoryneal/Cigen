using System;
using System.Collections.Generic;
using OpenCvSharp;
using UnityEngine;

namespace Clothoid {

    [RequireComponent(typeof(ClothoidSolutionSinghMcCrae))]
    public class ClothoidGenerator : MonoBehaviour {
        public int numSamples = 100;

        public bool markNodes = true;
        public float lineWidth = 1;

        [Range(0.001f, 0.2f)]
        public float maxError = 0.1f;
        public float endpointWeight = 1f;
        public LineRenderer lrNodes;
        public LineRenderer unTranslatedSamplesLR;
        public LineRenderer translatedSampleLR;
        public LineRenderer lkNodeLR;
        public LineRenderer segmentedLKLR;
        public LineRenderer circleLR;
        public int polylineIndex = 2; 

        [Space(10)]
        [Header("Curve Settings")]
        public int a = 0;
        public int b = 5;
        public int c = 10;
        public int d = 15;
        public int e = 20;
        public int f = 30;
        bool awake = false;

        private List<GameObject> spawnedGameObjects = new List<GameObject>();

        private ClothoidCurve clothoidCurve1;

        private IEnumerator<List<Vector3>> getNextPointList;

        public ClothoidSolutionShinSingh solutionshinsingh;
        public ClothoidSolutionSinghMcCrae solutionSinghMcCrae;
        void Start() {
            //solution = GetComponent<ClothoidSolutionSinghMcCrae>();
            awake = true;
            getNextPointList = ClothoidCurve.TestAllConnectionTypes(a, b, c, d, e, f);
            clothoidCurve1 = new ClothoidCurve();

            renderers = new LineRenderer[] {unTranslatedSamplesLR, translatedSampleLR, lkNodeLR, segmentedLKLR};
            //CreateRandomClothoidCurve();
        }
    
        bool shouldDraw = false;
        public float sampleTime = 0.5f;//how long between taking samples of the mouse position
        float t = 0;
        public float sampleArcLength = 1f; //how far should the mouse move before each another sample is taken?
        Vector3 lastSample;
        List<Vector3> drawingPoints = new List<Vector3>(); //
        List<Vector3> cachedDrawingPoints = new List<Vector3>();

        [Space(10)]
        [Header("Clothoid Curve Properties")]
        public float ArcLengthStart = 0;
        public float ArcLengthEnd = 10;
        public float CurvatureStart = 0;
        public float CurvatureEnd = 1;
        void Update()
        {
            if (Input.GetMouseButton(0)) {
                shouldDraw = true;
            } else {
                if (shouldDraw) {
                    drawingPoints.Clear();
                    shouldDraw = false;
                }
            }

            if (shouldDraw) {
                Vector2 mousePixels = Input.mousePosition; //bottom left is 0,0
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePixels.x, mousePixels.y, Camera.main.transform.position.y));
                if (lastSample == null) lastSample = worldPos;
                else if (Vector3.Distance(worldPos, lastSample) >= sampleArcLength) {
                    drawingPoints.Add(worldPos);
                    this.DrawOrderedVector3s(drawingPoints, this.lrNodes);
                    lastSample = worldPos;
                }

                if (drawingPoints.Count > 3) {
                        //MakeCoolGraph();
                        RedrawCurve();
                }
            }
        }

        [ContextMenu("Calculate Curve")]
        public void MakeCoolGraph() {
            if (this.drawingPoints.Count == 0) return;
            //DrawOrderedVector3s(CreateLinearDecreasingCurvatureCurve(maxArcLength, curvature, numSamples));
            //List<Vector3> verts = CrazyCurve(this.numSamples);//OrderedVertices(UnityEngine.Random.Range(20, 30));
            //this.clothoidCurve = this.solution.CalculateClothoidCurve(this.drawingPoints, this.maxError, this.endpointWeight);

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
            //DrawOrderedVector3s(verts, this.lrNodes, 30);
            //RedrawCurve();
        }

        GameObject cmPolyline;
        GameObject cmCurve;
        private bool shouldOverwrite;

        private LineRenderer[] renderers;
        private ClothoidCurve clothoidCurve2;

        [ContextMenu("Redraw curve")]
        public void RedrawCurve() {
            this.clothoidCurve1 = this.solutionshinsingh.CalculateClothoidCurve(drawingPoints);
            Debug.Log($"Drawing points count: {drawingPoints.Count}");
            Debug.Log($"Posture count: {this.solutionshinsingh.Postures.Count}");
            for (int i = 0; i < solutionshinsingh.Postures.Count; i++) {
                int lrindex = i % renderers.Length;
                DrawOrderedVector3s(solutionshinsingh.GetPostureSamples(i, 150), renderers[lrindex]);
            }

            //the singh mccrae solution
            //its not very good after all, maybe just my implementation but even the demo from mccrae wasn't that great. 
            //though its way better than mine
            /*
            this.clothoidCurve2 = this.solutionSinghMcCrae.CalculateClothoidCurve(drawingPoints);
            DrawOrderedVector3s(solutionSinghMcCrae.GetFitSamples(150), this.circleLR);
            */

/*
            if (drawingPoints.Count > 3) {
                for (int i = 0; i+2 < drawingPoints.Count; i++) {
                    int lrindex = i % renderers.Length;
                    // Draw a trail of osculating circles on the input polyline
                    if (Clothoid.Math.AreCollinearPoints(drawingPoints[i], drawingPoints[i+1], drawingPoints[i+2])) {
                        Debug.Log("Points form a line");
                    } else {
                        float radius = 1f / Clothoid.Math.MoretonSequinCurvature(drawingPoints[i], drawingPoints[i+1], drawingPoints[i+2]);
                        Vector3 centerCircle = Clothoid.Math.CenterOfCircleOfThreePoints(drawingPoints[i], drawingPoints[i+1], drawingPoints[i+2]);

                        Vector3 sampleCircle = new Vector3(0, 0, radius);
                        List<Vector3> samples = new List<Vector3>();
                        float sampleAngleDeg = 360f / 100f;
                        for (float angleDeg = 0; angleDeg <= 360f; angleDeg += sampleAngleDeg) {
                            samples.Add(ClothoidSegment.RotateAboutAxis(sampleCircle, Vector3.up, angleDeg) + centerCircle);
                        }
                        DrawOrderedVector3s(samples, renderers[lrindex]);
                    }
                }
            }*/
            //DrawOrderedVector3s(this.solution.TranslatedArcLengthSamples, this.unTranslatedSamplesLR);
            /*
            //LK Nodes
            //vector where the x value is the x value of the input polyline node and z is the curvature
            List<Vector3> xKNodes = new List<Vector3>();
            //List<Vector3> lknodes = this.solution.LKNodeMap_scaled_orderedList;
            for (int i = 0; i < lknodes.Count; i++) {
                xKNodes.Add(new Vector3(this.solution.polyline[i].x, 0, lknodes[i].z));
            }
            
            DrawOrderedVector3s(lknodes, this.lkNodeLR);
            DrawOrderedVector3s(this.solution.SegmentedLKNodes_scaled, this.segmentedLKLR, 5);

            if (cmPolyline != null) GameObject.Destroy(cmPolyline);
            if (cmCurve != null) GameObject.Destroy(cmCurve);

            cmPolyline = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cmPolyline.transform.localScale = Vector3.one * 3; 
            cmPolyline.transform.position = this.solution.cmPolyline;
            cmPolyline.GetComponent<Renderer>().material.color = new Color(128, 0, 128); //purple

            cmCurve = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cmCurve.transform.localScale = Vector3.one * 3; 
            cmCurve.transform.position = this.solution.cmCurve;
            cmCurve.GetComponent<Renderer>().material.color = new Color(255, 136, 0); //orange
            */
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

        void DrawOrderedVector3s(List<Vector3> positions, LineRenderer lr, float zOffset = 0, bool markNodes = true) {
            foreach (GameObject go in this.spawnedGameObjects) {
                Destroy(go);
            }
            
            float sumOfLength = 0;
            List<Vector3> newPositions = new List<Vector3>();
            for (int i = 0; i < positions.Count; i++) {
                Vector3 p = positions[i] + (Vector3.forward * zOffset);
                if (zOffset != 0) newPositions.Add(p);
                if (i < positions.Count-1) sumOfLength += Vector3.Distance(positions[i], positions[i+1]);

                if (markNodes && !float.IsNaN(positions[i].x) && !float.IsNaN(positions[i].y) && !float.IsNaN(positions[i].z)) {
                    GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    g.transform.localScale = Vector3.one * .3f;
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
                //TestCurve();
            }
        }
    }
}
