using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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
            this.clothoidCurve1 = new ClothoidCurve();
            //solution = GetComponent<ClothoidSolutionSinghMcCrae>();
            awake = true;
            getNextPointList = ClothoidCurve.TestAllConnectionTypes(a, b, c, d, e, f);

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

        private GameObject Q;
        void Update()
        {
            /*
             //Drawing points
            if (Input.GetMouseButton(0)) {
                shouldDraw = true;
            } else {
                if (shouldDraw) {
                    Vector3[] points = new Vector3[drawingPoints.Count];
                    drawingPoints.CopyTo(points, 0);
                    cachedDrawingPoints = points.ToList();
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
            }*/


            if (Input.GetMouseButtonDown(0)) {
                Vector2 mousePixels = Input.mousePosition; //bottom left is 0,0
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePixels.x, mousePixels.y, Camera.main.transform.position.y));

                if (Q) GameObject.Destroy(Q);
                Q = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Q.transform.position = worldPos;
                Q.transform.localScale = Vector3.one * 5;
                Q.GetComponent<Renderer>().material.color = Color.red;

                Debug.Log($"Q in Gamma == {ClothoidSolutionWaltonMeek.IsInGamma(worldPos, startCurvature, endCurvature, endTangent)}");
            }

            if (Input.GetKeyDown(KeyCode.Space)) {
                //TestAllConnectionTypes();
                ShowTangents();
            }
        }

        [Header("Gamma Region testing")]
        [Range(0, MathF.PI)]
        public float endTangent = Mathf.PI / 4;
        [Range(0, 1)]
        public float startCurvature = 0;
        [Range(0, 1)]
        public float endCurvature = .2f;

        [ContextMenu("Test Gamma Region")]
        public void TestGammaRegion() {
            if (startCurvature > endCurvature) return;
            (float, float, Vector3) gammaParameters = ClothoidSolutionWaltonMeek.GetGammaRegionParameters(startCurvature, endCurvature, endTangent);
            float slope = gammaParameters.Item1;

            string item2 = startCurvature > 0 ? "radius" : "R.y";
            string item3 = startCurvature > 0 ? "center" : "unused";
            Debug.Log($"Gamma parameters: slope: {gammaParameters.Item1} | {item2}: {gammaParameters.Item2} | {item3}: {gammaParameters.Item3}");

            if (startCurvature > 0) {
                //draw line and a circle
                List<Vector3> line = new List<Vector3>() { Vector3.zero, new Vector3(300, 0, 300 * gammaParameters.Item1)};
                List<Vector3> circle = new List<Vector3>();
                for (float i = 0; i <= Mathf.PI * 2; i+=Mathf.PI/50) {
                    circle.Add(gammaParameters.Item3 + (new Vector3(Mathf.Cos(i), 0, Mathf.Sin(i)) * gammaParameters.Item2));
                }

                DrawOrderedVector3s(line, this.segmentedLKLR, 0, false);
                DrawOrderedVector3s(circle, this.translatedSampleLR, 0, false);
            } else {
                //draw two lines
                float Rx = gammaParameters.Item2 / slope; // y = mx => x = y/m => item2 is R.y
                Vector3 R = new Vector3(Rx, 0, gammaParameters.Item2);
                List<Vector3> line = new List<Vector3>() { Vector3.zero, new Vector3(300, 0, 300 * gammaParameters.Item1)};
                List<Vector3> line2 = new List<Vector3>() { R, R + (Vector3.right * 300) };
                DrawOrderedVector3s(line, this.segmentedLKLR, 0, false);
                DrawOrderedVector3s(line2, this.translatedSampleLR, 0, false);
            }

            DrawOrderedVector3s(ClothoidSolutionWaltonMeek.CalculateDCurve(endTangent * 2 / Mathf.PI, startCurvature, endCurvature), this.lkNodeLR, 0, false);
            DrawOrderedVector3s(ClothoidSolutionWaltonMeek.CalculateECurve(endTangent * 2 / Mathf.PI, startCurvature, endCurvature), this.circleLR, 0, false);

            if (objectR) GameObject.Destroy(objectR);
            objectR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            objectR.transform.position = ClothoidSolutionWaltonMeek.CalculateR(endTangent, endCurvature);
            objectR.transform.localScale = Vector3.one * 2;
        }

        private GameObject objectR;

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

        public void ShowTangents() {
            Debug.Log($"Num cached points: {cachedDrawingPoints.Count}");
            List<Posture> postures = Posture.CalculatePostures(cachedDrawingPoints);
            Posture p1;
            Posture p2;
            Vector3 diff;
            float angleDiff;
            for (int i = 1; i < postures.Count; i++) {
                p1 = postures[i-1];
                p2 = postures[i];
                diff = p2.Position - p1.Position;
                diff = ClothoidSegment.RotateAboutAxis(diff, Vector3.up, -p1.Angle);
                angleDiff = p2.Angle - p1.Angle;
                Debug.DrawRay(p1.Position, p1.Tangent.normalized * 15, Color.red, 10f);
                Debug.Log($"{i} | angle diff {p2.Angle} - {p1.Angle} = {angleDiff}");
            }
        }

        [ContextMenu("Redraw curve")]
        public void RedrawCurve() {
            ShowTangents();

            //clothoidCurve1.AddRandomCurve2();
            //DrawOrderedVector3s(clothoidCurve1.GetSamples(clothoidCurve1.Count * 10), this.circleLR);

            //this.solutionshinsingh.CalculateClothoidCurve(drawingPoints);
            //Debug.Log($"Drawing points count: {drawingPoints.Count}");
            //Debug.Log($"Curve segment count: {this.solutionshinsingh.clothoidCurve.Count}");
            //DrawOrderedVector3s(solutionshinsingh.GetFitSamples(150), this.circleLR);

            //the singh mccrae solution
            //its not very good after all, maybe just my implementation but even the demo from mccrae wasn't that great. 
            //though its way better than mine
            /*
            this.clothoidCurve2 = this.solutionSinghMcCrae.CalculateClothoidCurve(drawingPoints);
            DrawOrderedVector3s(solutionSinghMcCrae.GetFitSamples(150), this.circleLR, 0, false, 1);*/
            

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

        void TestAllConnectionTypes() {
            if (getNextPointList.MoveNext()) DrawOrderedVector3s(getNextPointList.Current, this.circleLR);
        }

        void DrawOrderedVector3s(List<Vector3> positions, LineRenderer lr, float zOffset = 0, bool markNodes = true, float yOffset = 0) {
            foreach (GameObject go in this.spawnedGameObjects) {
                Destroy(go);
            }
            
            float sumOfLength = 0;
            List<Vector3> newPositions = new List<Vector3>();
            for (int i = 0; i < positions.Count; i++) {
                Vector3 p = positions[i] + (Vector3.forward * zOffset) + (Vector3.up * yOffset);
                if (zOffset != 0 || yOffset != 0) newPositions.Add(p);
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
            if (zOffset != 0 || yOffset != 0) positions = newPositions;

            lr.startWidth = this.lineWidth;
            lr.endWidth = this.lineWidth;
            //lr.startColor = UnityEngine.Random.ColorHSV(0f, 1f, 1, 1, 1, 1);
            //lr.endColor = lr.startColor;

            lr.positionCount = positions.Count;
            lr.SetPositions(positions.ToArray());
        }
        void OnValidate() {
            if (awake) {
                TestGammaRegion();
            }
        }
    }
}
