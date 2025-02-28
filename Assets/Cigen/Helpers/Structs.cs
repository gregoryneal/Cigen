using System;
using System.Linq;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Cigen.ImageAnalyzing;
using OpenCvSharp;
using Unity.Profiling;
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

        /// <summary>
        /// Given a goal, project its world line and see if it hits any Population Centers on the way, if it does
        /// this method will split the goals up into Goal object into a consecutive trail of goals that end in the
        /// first PopulationCenter before a new one is created that goes to the next Population Center, all the way
        /// until we reach the original goal location.
        /// </summary>
        /// <param name="testGoal">The original goal to test.</param>
        /// <param name="excludedPopulationCenters">Population centers to ignore for intersection checks, this should be the start and endpoint PCs of the testGoal.</param>
        /// <returns></returns>
        public static List<Goal> SegmentGoalByPCIntersection(Goal testGoal) {
            //dequeue goals to check if they intersect anywhere, if they do then
            Queue<Goal> goalsToCheck = new Queue<Goal>();
            goalsToCheck.Enqueue(testGoal);
            //finalized list of goals
            List<Goal> acceptedGoals = new List<Goal>();
            //list of pc that have an evaluated and accepted goal node in it.
            int maxTries = 500;
            int i = 0;
            while (goalsToCheck.Count > 0) {
                if (i >= maxTries) break;
                else i++;
                Debug.Log($"Queue length: {goalsToCheck.Count}");
                //check if the highway intersects any other population centers and add them as well
                Goal currGoal = goalsToCheck.Dequeue();
                PopulationCenter pcFrom = ImageAnalysis.ClosestPopulationCenter(currGoal.from, true);
                PopulationCenter pcTo = ImageAnalysis.ClosestPopulationCenter(currGoal.to, true);
                bool anyIntersections = false;
                foreach (PopulationCenter pc in CitySettings.instance.city.PopulationCenters) {
                    if (pc == pcFrom || pc == pcTo) continue;
                    if (Maths.Math.LineRectangleIntersection(out _, currGoal.from, currGoal.to, pc.worldPosition, pc.size)) {
                        Vector3 newGoalPoint;
                        if (ImageAnalysis.RandomPointWithinPopulationCenter(out newGoalPoint, pc)) {
                            //Debug.Log("New intersections");
                            goalsToCheck.Enqueue(new Goal(currGoal.from, newGoalPoint));
                            goalsToCheck.Enqueue(new Goal(newGoalPoint, currGoal.to));
                            anyIntersections = true;
                            break;
                        }
                    }
                }

                if (anyIntersections == false) {
                    acceptedGoals.Add(currGoal);
                }
            }

            return acceptedGoals;
        }

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