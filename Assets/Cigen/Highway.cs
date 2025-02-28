
using System;
using System.Collections.Generic;
using System.Linq;
using Cigen.Factories;
using Cigen.ImageAnalyzing;
using Cigen.Structs;
using OpenCvSharp;
using UnityEngine;

namespace Cigen {
    public class Highway : Road {

        
        public CitySettings settings { get; private set; }

        public void Init(Vector3 position, CitySettings settings) {
            this.settings = settings;
            transform.position = position;
        }        

        /// <summary>
        /// Given a PopulationCenter, build a ring road around it. Uses ringRoadFollowContourAmount setting.
        /// The road will start as an ellipse with major and minor radii being the size of the PC bounding box.
        /// But the intersections will lerp in the direction of the contour centroid until the pop density reads
        /// about 0.5 (make this value configurable!) by the amount of RRFCA setting.
        /// For example if the ringRoadPopulationDensityCutoff value is 0.5, and ringRoadFollowContourAmount is 0.8,
        /// then when we generate an intersection on the ring road ellipse, it will lerp towards the centroid until
        /// it has traversed 80% of the distance between the starting location and the first pixel that has a normalized
        /// value of 0.5 (127/255, etc).
        /// </summary>
        /// <param name="populationCenter">The population center to generate a ring road around.</param>
        /// <param name="settings">The city settings.</param>
        /// <returns>An array of intersections that define the vertices of the ring road.</returns>
        public static Intersection[] CreateRingRoad(PopulationCenter populationCenter, City city) {
            CitySettings settings = city.Settings;
            //generate an intersection at each bounding point, and a road between each subsequent intersection
            List<Intersection> ints = new List<Intersection>();
            //our starting point is the projected ellipse point
            float[] compare = new float[]{populationCenter.size.x, populationCenter.size.z};
            float b = Mathf.Max(compare);
            float a = Mathf.Min(compare);
            //project our first contour point on the ellipse the covers our PC.
            //subtract the position because the math function assumes the ellipse is centered on 0,0
            //just add the position back after
            //Vector3 startPos = Cigen.Maths.Math.ProjectedPointOnEllipse(populationCenter.worldBoundingPoints[0]-populationCenter.worldPosition, a, b);
            //lerp the value towards the centroid
            //Vector3 axiomPos = ApplyRingRoadSettings(startPos+populationCenter.worldPosition, populationCenter, settings);
            Vector3 axiomPos = RingRoadContourPosition(populationCenter.worldBoundingPoints[0], populationCenter);
            Intersection axiom = city.CreateOrMergeNear(axiomPos);
            Intersection node = axiom;
            ints.Add(axiom);
            //Extend each intersection
            for (int i = 1; i < populationCenter.worldBoundingPoints.Length; i++) {
                //project the next point onto the ellipse
                Vector3 newPos = RingRoadContourPosition(populationCenter.worldBoundingPoints[i], populationCenter);
                //Vector3 projP = Cigen.Maths.Math.ProjectedPointOnEllipse(populationCenter.worldBoundingPoints[i]-populationCenter.worldPosition, a, b);
                //Vector3 newPos = ApplyRingRoadSettings(projP+populationCenter.worldPosition, populationCenter, settings);
                node = node.Extend(newPos);
                ints.Add(node);
            }
            //close the loop
            city.CreatePath(node, axiom);


            return ints.ToArray();
        }

        /// <summary>
        /// Apply settings to proposed ring road intersection position.
        /// </summary>
        /// <param name="pos">The proposed intersection position in world space.</param>
        /// <param name="pc">The population center.</param>
        /// <param name="city">The city.</param>
        /// <returns>A finalized intersection location.</returns>
        public static Vector3 RingRoadContourPosition(Vector3 pos, PopulationCenter pc) {
            //our starting point is the projected ellipse point
            float[] compare = new float[]{pc.size.x, pc.size.z};
            float b = Mathf.Max(compare);
            float a = Mathf.Min(compare);
            Debug.Log($"{String.Join(", ", compare)} -> Major and minor axes");
            //subtract worldposition because projected point function assumes the ellipse is centered on 0,0
            Vector3 startPos = Maths.Math.ProjectedPointOnEllipse(pos-pc.worldPosition, a, b);
            //return startPos + pc.worldPosition;
            //add back the world position to apply PD functions.
            return ApplyRingRoadSettings(startPos+pc.worldPosition, pc);
        }

        /// <summary>
        /// Apply the ring road settings to a new ring road intersection position.
        /// </summary>
        /// <param name="intersectionPosition">The proposed intersection position</param>
        /// <returns>Interpolated intersection position, use this value to pass to CreateOrMergeNear or equivalent intersection creation method.</returns>
        public static Vector3 ApplyRingRoadSettings(Vector3 intersectionPosition, PopulationCenter populationCenter) {
            //new approach, start from the intersection and lerp towards the centroid until we find a vector that is close enough,
            //then lerp towards that position using the contour amount settings.
            Vector3 samplePos = intersectionPosition;
            Vector3 direction = (populationCenter.worldPosition - intersectionPosition).normalized;
            float targetPD= CitySettings.instance.populationDensityCutoff;
            float delta = 1f;
            float diff = targetPD - ImageAnalysis.PopulationDensityAt(samplePos.x, samplePos.z);
            while (diff > 0) {
                samplePos += direction * delta;
                diff = targetPD - ImageAnalysis.PopulationDensityAt(samplePos.x, samplePos.z);
            }
            //lerp towards the new value from the place on the circle
            return Vector3.Lerp(intersectionPosition, samplePos, CitySettings.instance.ringRoadFollowContourAmount);
            /*
            //binary search, sample positions in the direction of the centroid until we find a point within epsilon of the target
            float targetValue = settings.ringRoadPopulationDensityCutoff;
            float epsilon = 0.01f;
            int maxTries = 100;
            Vector3 minPos = intersectionPosition;
            Vector3 maxPos = populationCenter.worldPosition;
            //Vector3 samplePos = (minPos + maxPos)/2;
            Vector3 samplePos = minPos;
            //the sampled population density
            float pd = ImageAnalysis.PopulationDensityAt(samplePos.x, samplePos.z, settings);
            float diff = targetValue - pd;
            while (Math.Abs(diff) > epsilon) {
                if (maxTries <= 0) break;
                else maxTries--;
                //sample the sample pos
                if (diff < -1 * epsilon) {
                    //our pd is too large, sample closer to intersection position
                    Debug.Log("==========");
                    Debug.Log($"SamplePos before moving closer to outer edge: {samplePos}");                    
                    Debug.Log($"MinPos before moving closer to outer edge: {minPos}");
                    Debug.Log($"MaxPos before moving closer to outer edge: {maxPos}");
                    samplePos = new Vector3((minPos.x+samplePos.x)/2, (minPos.y+samplePos.y)/2, (minPos.z+samplePos.z)/2);
                    //samplePos = Vector3.Lerp(minPos, samplePos, 0.5f);
                    maxPos = samplePos;
                    Debug.Log($"SamplePos after moving closer to outer edge: {samplePos}");          
                    Debug.Log($"MinPos after moving closer to outer edge: {minPos}");
                    Debug.Log($"MaxPos after moving closer to outer edge: {maxPos}");
                    //samplePos = (samplePos + minPos)/2;
                    //Debug.Log($"{diff} Moving further");
                } else if (diff > epsilon) {
                    //our pd is too small, lets sample closer to the centroid
                    Debug.Log($"SamplePos before moving closer to centroid: {samplePos}");          
                    Debug.Log($"MinPos before moving closer to centroid: {minPos}");
                    Debug.Log($"MaxPos before moving closer to centroid: {maxPos}");
                    samplePos = new Vector3((maxPos.x+samplePos.x)/2, (maxPos.y+samplePos.y)/2, (maxPos.z+samplePos.z)/2);
                    //samplePos = Vector3.Lerp(samplePos, maxPos, 0.5f);
                    minPos = samplePos;
                    Debug.Log($"SamplePos after moving closer to centroid: {samplePos}");
                    Debug.Log($"MinPos after moving closer to centroid: {minPos}");
                    Debug.Log($"MaxPos after moving closer to centroid: {maxPos}");
                    //samplePos = (samplePos + maxPos)/2;

                } else {
                    //we found a good place
                    Debug.Log("Goldilocks");
                    break;
                }

                pd = ImageAnalysis.PopulationDensityAt(samplePos.x, samplePos.z, settings);
                diff = targetValue - pd;
            }
            */            
        }

        /// <summary>
        /// Given a PopulationCenter, build a bypass highway system through it. Generate intersection nodes along
        /// the major axis of the PC bounding box, starting outside towards the outside edge of the map. The roads 
        /// can only pass within a certain distance of the centroid (center of PC). 
        /// </summary>
        /// <param name="populationCenter">The population center to generate a ring road around.</param>
        /// <param name="settings">The city settings.</param>
        /// <returns>An array of intersections that define the vertices of the ring road.</returns>
        public static Intersection[] CreateBypass(PopulationCenter populationCenter, City city) {
            CitySettings settings = city.Settings;
            return new Intersection[]{};
        }

        /// <summary>
        /// Given a PopulationCenter, build a throughpass highway system through it. It is the same as the bypass
        /// system except that the nodes can pass directly through the centroid.
        /// </summary>
        /// <param name="populationCenter">The population center to generate a ring road around.</param>
        /// <param name="settings">The city settings.</param>
        /// <returns>An array of intersections that define the vertices of the ring road.</returns>
        public static Intersection[] CreateThroughpass(PopulationCenter populationCenter, City city) {
            CitySettings settings = city.Settings;
            return new Intersection[]{};
        }

        public static HighwayType FindDominantHighwayTypeInRect(Mat highwayData, OpenCvSharp.Rect boundingRect) {
            Scalar avgs = ImageAnalysis.AvgOfBoundingRect(highwayData, boundingRect);
            return Highway.ScalarToHighwayType(avgs);
        }

        public static HighwayType ScalarToHighwayType(Scalar avgPixelValue) {
            
            float[] vals = new float[] {
                (float)avgPixelValue[0],
                (float)avgPixelValue[1],
                (float)avgPixelValue[2],
                (float)avgPixelValue[3],
            };

            return (HighwayType)Cigen.Maths.Math.WeightedRandomSelection(vals);
        }
    }
}