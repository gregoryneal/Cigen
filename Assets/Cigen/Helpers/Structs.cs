using System.Collections.Generic;
using Cigen.ImageAnalyzing;
using OpenCvSharp;
using UnityEngine;

namespace Cigen.Structs {
    /// <summary>
    /// Encapsulates information about a region of population
    /// </summary>
    //[System.Serializable]
    public class PopulationCenter {
        //central position in the world
        public Vector3 worldPosition;
        //central position on the texture map
        public Point pixelPosition;
        //bounding points in the world
        public Vector3[] worldBoundingPoints;
        //bounding points in the texture map
        public Point[] pixelBoundingPoints;
        //size in the world
        public Vector3 size;
        public float density;
        public HighwayType highwayType;
        public List<PopulationCenter> connectedPCs;
        
        public override bool Equals(object obj)
        {
            //
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //
            
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            
            // TODO: write your implementation of Equals() here
            if (obj.GetHashCode() == this.GetHashCode()) return true;
            return base.Equals (obj);
        }
        
        // override object.GetHashCode
        public override int GetHashCode()
        {
            return this.worldPosition.GetHashCode() * 14 + this.size.GetHashCode();
        }
    }

    public struct Goal {
        public Vector3 from;
        public Vector3 to;
        public readonly Vector3 direction { get { return to - from; }}
        float priority;

        public Goal(Vector3 from, Vector3 to, float priority = 1f) {
            this.from = from;
            this.to = to;
            this.priority = priority;
        }

        public Goal(Goal goal) {
            this.from = goal.from;
            this.to = goal.to;
            this.priority = goal.priority;
        }

        public static Goal NONE = new Goal(Vector3.zero, Vector3.zero, 0);

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(Goal)) {
                return ((Goal)obj).GetHashCode() == this.GetHashCode();
            }
            return false;
        }

        public override int GetHashCode() {
            return from.GetHashCode() * 9 + to.GetHashCode() * 17 - priority.GetHashCode();
        }
    }
}