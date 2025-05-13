using System.Collections.Generic;
using UnityEngine;

namespace Clothoid {
    
    public class ThreeSegmentGuesser : MonoBehaviour {

        [Header("Unknown values")]
        [Min(0)]
        public float arcLength1;
        [Min(0)]
        public float arcLength2;
        [Min(0)]
        public float arcLength3;
        [Range(-.01f, .01f)]
        public float sharpness;

        [Header("Known values")]
        public Vector2 start = Vector2.zero;
        public Vector2 end = Vector2.zero;
        [Range(-2, 2)]
        public float startCurvature = 0;
        [Range(-2, 2)]
        public float endCurvature = 1;
        [Range(0, 180)]
        public float startAngle = 0;
        
        [Range(0, 180f)]
        public float endAngle;

        [Header("Polyline")]
        private ClothoidCurve curve;
        private ClothoidCurve testCurve;
        private bool awake = false;
        private GameObject startGO;
        public LineRenderer startLR;
        private GameObject endGO;
        public LineRenderer endLR;
        public LineRenderer curveLR;
        private GUIStyle g;
        void Start()
        {

            g = new GUIStyle();
            g.fontSize = 20;
            g.fontStyle = FontStyle.Bold;
            g.font = Font.CreateDynamicFontFromOSFont("Times New Roman", 40);

            startLR.startWidth = .5f;
            startLR.endWidth = startLR.startWidth;
            endLR.startWidth = startLR.startWidth;
            endLR.endWidth = startLR.startWidth;
            //startLR.startColor = Color.green;
            //startLR.endColor = Color.green;
            //endLR.startColor = Color.red;
            //endLR.endColor = Color.red;

            awake = true;
            curve = new ClothoidCurve();
            testCurve = new ClothoidCurve();

            startGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);            
            startGO.transform.localScale = Vector3.one * 2;
            startGO.GetComponent<Renderer>().material.color = Color.green;
            endGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            endGO.transform.localScale = Vector3.one * 2;
            endGO.GetComponent<Renderer>().material.color = Color.red;
            SetupVisuals();
        }

        void SetupVisuals() {
            //Draw start and end point, label curvature and tangent
            startGO.transform.position = v(start);
            DrawOrderedVector3s(new List<Vector3>(){v(start), v(start) + (GetTangent(startAngle) * 3)}, this.startLR);

            endGO.transform.position = new Vector3(end.x, 0, end.y);
            DrawOrderedVector3s(new List<Vector3>(){v(end), v(end) + (GetTangent(endAngle) * 3)}, this.endLR);
        }

        ClothoidCurve trackCurve = new ClothoidCurve();
        [ContextMenu("Draw Curve")]
        public void DrawCurve() {
            /* //Check the end tangent of a clothoid segment
            ClothoidCurve c = new ClothoidCurve();
            c += new ClothoidSegment(startCurvature, sharpness, arcLength1);
            Vector3 endpoint = c.Endpoint;
            DrawOrderedVector3s(c.GetSamples(100), this.curveLR);
            DrawOrderedVector3s(new List<Vector3>(){endpoint, endpoint + (GetTangent(c[0].Rotation) * 5)}, this.endLR);*/
            
            //DrawOrderedVector3s(e.GetSamples(50), endLR);
            /*LineRenderer[] l = new LineRenderer[3]{this.startLR, this.endLR, this.curveLR};
            for (int i = 0; i < c.Count; i++) {
                DrawOrderedVector3s(c[i].GetSamples(50), l[i], 10 * i);
            }*/
            //ClothoidCurve c = new ClothoidCurve();
            //c += new ClothoidSegment(0, 5, .1f, .1f);
            //c += new ClothoidSegment(0, 10, -.1f, -.1f);
            ClothoidCurve c = ClothoidCurve.ThreeSegmentsLocal(startCurvature, sharpness, arcLength1, arcLength2, arcLength3);
            c.Offset = new Vector3(start.x, 0, start.y);
            c.AngleOffset = startAngle;
            DrawOrderedVector3s(c.GetSamples(c.Count * 50), curveLR);

            //trackCurve.AddRandomSegment3();
            //DrawOrderedVector3s(trackCurve.GetSamples(trackCurve.Count * 50), curveLR);
        }

        void OnGUI() {            
            Rect pos = new Rect(new Vector2(start.x, start.y - 3), Vector2.one * 10);
            Rect pos2 = new Rect(new Vector2(end.x, end.y - 3), Vector2.one * 10);
            GUI.Label(pos, $"Ci = {startCurvature}", g);
            GUI.Label(pos2, $"Cf = {endCurvature}", g);
        }

        Vector3 GetTangent(float angle) {
            return ClothoidSegment.RotateAboutAxis(new Vector3(1, 0, 0), Vector3.up, -angle);
        }

        Vector3 v(Vector2 v) {
            return new Vector3(v.x, 0, v.y);
        }

        [ContextMenu("Reset Curve")] 
        public void ResetCurve() {
            curve.Reset();
        }

        [ContextMenu("Reset Test Curve")] 
        public void ResetTestCurve() {
            testCurve.Reset();
        }

        [ContextMenu("Accept Current Test Curve")] 
        public void AcceptTestCurve() {
            curve += testCurve;
        }

        void OnValidate()
        {
            if (!awake) return;

            curve.Reset();
            SetupVisuals();
            DrawCurve();
        }

        void DrawOrderedVector3s(List<Vector3> positions, LineRenderer lr, float zOffset = 0, float yOffset = 0) {            
            float sumOfLength = 0;
            List<Vector3> newPositions = new List<Vector3>();
            for (int i = 0; i < positions.Count; i++) {
                Vector3 p = positions[i] + (Vector3.forward * zOffset) + (Vector3.up * yOffset);
                if (zOffset != 0 || yOffset != 0) newPositions.Add(p);
                if (i < positions.Count-1) sumOfLength += Vector3.Distance(positions[i], positions[i+1]);
            }
            if (zOffset != 0 || yOffset != 0) positions = newPositions;
            lr.positionCount = positions.Count;
            lr.SetPositions(positions.ToArray());
        }
    }
}