using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using NUnit.Framework.Constraints;
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

        private List<Vector3> pointList = new List<Vector3>() {new Vector3(1, 0, 3), new Vector3(5, 0, 8), new Vector3(13, 0, 2), new Vector3(15, 0, -6), new Vector3(17, 0, -12)};

        private IEnumerator<List<Vector3>> getNextPointList;

        public ClothoidSolutionShinSingh solutionshinsingh;
        public ClothoidSolutionSinghMcCrae solutionSinghMcCrae;

        private IEnumerator<(Posture, Posture)> standardPostures;
        private List<Posture> pointListPostures; 
        void Start() {
            this.clothoidCurve1 = new ClothoidCurve();
            clothoidCurve1.Reset();
            //solution = GetComponent<ClothoidSolutionSinghMcCrae>();
            awake = true;
            getNextPointList = ClothoidCurve.TestAllConnectionTypes(a, b, c, d, e, f);

            DrawOrderedVector3s(pointList, this.lrNodes);
            solutionshinsingh.CalculateClothoidCurve(pointList);
            nextCurve = solutionshinsingh.SolveClothoidParameters();

            renderers = new LineRenderer[] {unTranslatedSamplesLR, translatedSampleLR, lkNodeLR, segmentedLKLR};

            /*endpointObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            endpointObject.GetComponent<Renderer>().material.color = Color.red;
            endpointObject.transform.localScale = Vector3.one * 2;*/

            pointListPostures = Posture.CalculatePostures(pointList);
            standardPostures = Posture.GetStandardPostures(pointListPostures);
            //CreateRandomClothoidCurve();
        }

        [ContextMenu("Show Next Posture")]
        public void NextPosture() {
            if (standardPostures.MoveNext()) {
                pointlistindex++;
                ShowPosture();
            }
        }
        private int pointlistindex = -1;

        public void ShowPosture() {
            if (pointlistindex >= 0 && pointlistindex < pointList.Count) {
                (Posture, Posture) p = standardPostures.Current;
                DrawOrderedVector3s(new List<Vector3>() {p.Item1.Position, p.Item2.Position}, this.segmentedLKLR);
                DrawOrderedVector3s(p.Item1.GetSamples(50), this.circleLR);
                DrawOrderedVector3s(p.Item2.GetSamples(50), this.lkNodeLR);
                DrawOrderedVector3s(pointListPostures[pointlistindex].GetSamples(50), this.translatedSampleLR);
            }
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
            }


            //Test if click point is in Gamma Region
            /*
            if (Input.GetMouseButtonDown(0)) {
                Vector2 mousePixels = Input.mousePosition; //bottom left is 0,0
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePixels.x, mousePixels.y, Camera.main.transform.position.y));

                if (Q) GameObject.Destroy(Q);
                Q = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Q.transform.position = worldPos;
                Q.transform.localScale = Vector3.one;
                Q.GetComponent<Renderer>().material.color = Color.red;

                Debug.Log($"Q in Gamma == {ClothoidSolutionWaltonMeek.IsInGamma(worldPos, startCurvature, endCurvature, endTangent)}");
            }

            if (Input.GetKeyDown(KeyCode.Space)) {
                //TestAllConnectionTypes();
                ShowTangents();
            }*/
        }

        [Header("Gamma Region testing")]
        [Range(0, MathF.PI)]
        public float endTangent = Mathf.PI / 4;
        [Range(0, 1)]
        public float startCurvature = 0;
        [Range(0, 1)]
        public float endCurvature = .2f;
        [Range(0, 1)]
        public float sharpness = .01f;

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
            //ClothoidCurve c = this.solutionshinsingh.CalculateClothoidCurve(this.drawingPoints);
            //DrawOrderedVector3s(this.solutionshinsingh.GetFitSamples(this.solutionshinsingh.SegmentCount * 20), this.circleLR);
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

        public float maxAngle = 360;
        public void DrawLastPosture() {
            Debug.Log("RUNNING");
            if (drawingPoints.Count == 0 && cachedDrawingPoints.Count >= 3) {
                solutionshinsingh.CalculateClothoidCurve(cachedDrawingPoints);
                Posture p = solutionshinsingh.Postures[^1];
                DrawOrderedVector3s(p.GetSamples(100, 0, maxAngle), this.circleLR, 0, false, 3);
            } else {
                if (solutionshinsingh.Count >= 3) {
                    Posture p = solutionshinsingh.Postures[^1];
                    DrawOrderedVector3s(p.GetSamples(100, 0, maxAngle), this.circleLR, 0, false, 3);
                } else {
                    Debug.Log("NOT ENOUGH POINTS");
                }
            }
        }

        IEnumerator<ClothoidCurve> nextCurve;
        [ContextMenu("Setup Point List")]
        public void SetupPointList() {
            this.solutionshinsingh.CalculateClothoidCurve(pointList);
        }
        [ContextMenu("Get Next Curve Approximation")]
        public void TestPointList() {
            if (nextCurve.MoveNext()) {
                ClothoidCurve c = nextCurve.Current;
                //Debug.Log(c.ToString());
                List<Vector3> samples = c.GetSamples(100 * c.Count);
                //Debug.Log(samples.Count);
                //segmentedLKLR.startWidth = .1f;
                //segmentedLKLR.endWidth = .1f;
                DrawOrderedVector3s(samples, this.segmentedLKLR, 0, false);
            } else {
                Debug.Log("Can't move next!");
            }
        }

        private double F1(double x) {
            return Math.Sin(x);
        }

        private double F2(double x) {
            return (x * x * x) / (1 - (4 * Math.Log(x, Math.E)));
        }

        private GameObject endpointObject;

        [ContextMenu("Redraw curve")]
        public void RedrawCurve() {
            //Debug.Log($"Adding segment number {clothoidCurve1.Count + 1}");
            DrawOrderedVector3s(ClothoidCurve.GetRandomCurve().GetSamples(50), this.circleLR, 0, false);

            //this.solutionshinsingh.CalculateClothoidCurve(drawingPoints);

            /*ClothoidSegment s = new ClothoidSegment(startCurvature, sharpness, ArcLengthEnd); //final curvature is k + xs (initial curvature + (sharpness * arclength))
            ClothoidSegment s2 = new ClothoidSegment(s.EndCurvature, s.Sharpness, s.TotalArcLength);
            ClothoidCurve c = new ClothoidCurve().AddSegments(s, s2);
            DrawOrderedVector3s(c.GetSamples(100), this.circleLR, 0, false);

            endpointObject.transform.position = c.Endpoint;


            Debug.Log(s.Description());*/

            /*
            if (solutionshinsingh.Count >= 3) {
                //draw the last two posture arcs (excluding the final point since the posture is the same shape as the second to last posture 
                
                Posture p1 = solutionshinsingh.Postures[^3];
                Posture p2 = solutionshinsingh.Postures[^2];
                List<Vector3>[] arcs = ClothoidSolutionShinSingh.GetSmallArcsThatConnectPostures(p1, p2);
                //DrawLastPosture();
                DrawOrderedVector3s(p1.GetSamples(500), this.lkNodeLR);
                DrawOrderedVector3s(p2.GetSamples(500), this.segmentedLKLR);
                DrawOrderedVector3s(arcs[0], this.translatedSampleLR, 0, false, 1);
                DrawOrderedVector3s(arcs[1], this.unTranslatedSamplesLR, 0, false, 2);
            }*/
            //Debug.Log($"Drawing points count: {drawingPoints.Count}");
            //Debug.Log($"Curve segment count: {this.solutionshinsingh.clothoidCurve.Count}");
            //DrawOrderedVector3s(solutionshinsingh.GetFitSamples(20 * solutionshinsingh.SegmentCount), this.circleLR);

            //the singh mccrae solution
            //its not very good after all, maybe just my implementation but even the demo from mccrae wasn't that great. 
            //though its way better than mine            
            //this.clothoidCurve2 = this.solutionSinghMcCrae.CalculateClothoidCurve(drawingPoints);
            //DrawOrderedVector3s(solutionSinghMcCrae.GetFitSamples(150), this.circleLR, 0, false, 1);
            

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

            //lr.startWidth = this.lineWidth;
            //lr.endWidth = this.lineWidth;
            //lr.startColor = UnityEngine.Random.ColorHSV(0f, 1f, 1, 1, 1, 1);
            //lr.endColor = lr.startColor;

            lr.positionCount = positions.Count;
            lr.SetPositions(positions.ToArray());
        }

        void DrawOrderedVector3s(List<System.Numerics.Vector3> positions, LineRenderer lr, float zOffset = 0, bool markNodes = true, float yOffset = 0) {
            List<Vector3> v = new List<Vector3>();
            for (int i = 0; i < positions.Count; i++) {
                v.Add(positions[i].ToUnityVector3());
            }
            DrawOrderedVector3s(v, lr, zOffset, markNodes, yOffset);
        }

        void OnValidate() {
            if (awake) {
                //TestGammaRegion();
                //DrawLastPosture();
                RedrawCurve();
            }
        }
    }
}
