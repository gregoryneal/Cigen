using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Cigen.Maths
{

    public static class Maths {
        public static void SortByXThenYFromReference(this IEnumerable<Vector2> points, Vector2 reference) {            
            points.OrderBy(v=>v.x-reference.x).ThenBy(v=>v.y-reference.y);
        }

        public static void Atan2RadialSortFromPositiveX(this IEnumerable<Vector2> points) {
            points.OrderBy(x=>Vector2.right.PositiveAngleTo(x)); 
        }

        public static void SortFromPositiveAngleToXAxis(this IEnumerable<VertexWithIndex> points, VertexWithIndex reference) {
            points.OrderBy(x=>reference.vertex.PositiveAngleTo(x.vertex)); 
        }

        public static void SortByXThenY(this IEnumerable<Vector2> points) {
        }

        public static Vector3 RotateAroundPivot(this Vector3 point, Vector3 pivot, Vector3 angles) {
            Vector3 dir = point - pivot; // get point direction relative to pivot
            dir = Quaternion.Euler(angles) * dir; // rotate it
            point = dir + pivot; // calculate rotated point
            return point; // return it
        }

        public static bool IsInCircle(Vector2 testPoint, Vector2 a, Vector2 b, Vector2 c)
        {
            float A = Vector2.Distance(b, c);
            float B = Vector2.Distance(a, c);
            float C = Vector2.Distance(a, b);
            float s = 0.5f * (A + B + C);
            float area = Mathf.Sqrt(s * (s - A) * (s - B) * (s - C)); // Heron's formula
            float radius = (A * B * C) / (4 * area);

            if (Vector2.Distance(testPoint, a) > radius)
                return false;
            if (Vector2.Distance(testPoint, b) > radius)
                return false;
            if (Vector2.Distance(testPoint, c) > radius)
                return false;

            return true;
        }

        public static float PositiveAngleTo(this Vector2 this_, Vector2 to) {
            Vector2 direction = to - this_;
            float angle = Mathf.Atan2(direction.y,  direction.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;

            /* Thought this was needed at one point, it may still be useful just not yet...
            float closeEnough = 0.001f;
            if (Mathf.Abs(angle) <= closeEnough) angle = 0f;
            if (Mathf.Abs(angle - 90f) <= closeEnough) angle = 90f;
            if (Mathf.Abs(angle - 180f) <= closeEnough) angle = 180f;
            if (Mathf.Abs(angle - 270f) <= closeEnough) angle = 270f;
            if (Mathf.Abs(angle - 360f) <= closeEnough) angle = 0f;
            */
            return angle;
        }

    }
}
