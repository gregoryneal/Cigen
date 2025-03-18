using Cigen.ImageAnalyzing;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using System;
using System.Linq;
using Unity.Collections;
using System.Net;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEditor.SpeedTree.Importer;
using NUnit.Framework.Constraints;
using System.Data.Common;

namespace Cigen {
    /// <summary>
    /// This will create branches from segments and vectors to find new places to build a road on.
    /// This is how we grow the network at any stage. 
    /// </summary>
    public class GlobalGoals {/*
        /// <summary>
        /// Given a road segment, branch from its location out to a number of proposed nearby locations on the map.
        /// Returns a list of potential new EndPositions of the provided RoadSegment.
        /// </summary>
        /// <param name="dirtySegment">The segment to branch out from.</param>
        /// <param name="distance">The distance to search</param>
        /// <param name="reverse">Look in reverse instead, AKA look out from start in the direction of (start-end).normalized</param>
        /// <returns>A number of proposed new EndPosition vectors that can be set as the new EndPosition of dirtySegment. Or we can create a new RoadSegment entirely from the new data.</returns>
        public static List<Vector3> BranchLocationsFromSegment(RoadSegment dirtySegment, float distance, bool reverse = false) {
            if (reverse) {
                return BranchLocationsFromPosition(dirtySegment.StartPosition, dirtySegment.StartPosition - dirtySegment.EndPosition, distance, dirtySegment);
            } else {
                return BranchLocationsFromPosition(dirtySegment.EndPosition, dirtySegment.EndPosition - dirtySegment.StartPosition, distance, dirtySegment);
            }
        }*/

        /// <summary>
        /// Given a start position (pivot) and a direction. Create a number of branches at a regular interval going from -maxAngle to +maxAngle in the direction and at the distance specified.
        /// This is a path segment mask.
        /// </summary>
        /// <param name="pivot">The start position of the vector.</param>
        /// <param name="direction">The initial direction of the vector.</param>
        /// <param name="distance">The distance our proposed locations should be from the pivot point.</param>
        /// <param name="maxAngle">The max deviation from the direction. The min angle will be T-maxAngle and the max will be T+maxAngle, where T is the angle direction makes with the x axis.</param>
        /// <param name="numBranches">The number of branches to create.</param>
        /// <returns>A list of vectors in world position with terrain height data</returns>
        private static List<Vector3> BranchLocationsFromPosition(Vector3 pivot, Vector3 direction, float distance, float maxAngle, int numBranches, bool random = false) {
            List<Vector3> proposedLocations = new List<Vector3>();
            Vector3 straightRay = pivot + (direction.normalized * distance);
            //uniform sampling
            /*
            for (int i = 0; i <= numBranches; i++) {
                float angle = (2*maxAngle*i/numBranches)-maxAngle;
                Vector3 rotatedVector = Maths.Math.RotateAroundPivot(straightRay, pivot, Vector3.up * angle, true);                
                if (ImageAnalysis.PointInBounds(rotatedVector) == false) continue; //don't consider any rays that are out of bounds
                float yValue = ImageAnalysis.TerrainHeightAt(rotatedVector.x, rotatedVector.z);
                Vector3 worldPointVector = new Vector3(rotatedVector.x, yValue, rotatedVector.z);
                //if (ImageAnalysis.IsClippingTerrain(pivot, worldPointVector) == true) continue; //don't consider rays that are clipping the terrain either
                proposedLocations.Add(worldPointVector);
                //Debug.DrawLine(pivot, worldPointVector, Color.white);
            }*/
            
            //random sampling
            for (int i = 0; i <= numBranches; i++) {
                float angle = random? UnityEngine.Random.Range(-1*maxAngle, maxAngle) : (2*maxAngle*i/numBranches)-maxAngle;
                Vector3 rotatedVector = Maths.RotateAroundPivot(straightRay, pivot, Vector3.up * angle, true);                
                if (ImageAnalysis.PointInBounds(rotatedVector) == false) continue; //don't consider any rays that are out of bounds
                float yValue = ImageAnalysis.TerrainHeightAt(rotatedVector.x, rotatedVector.z);
                Vector3 worldPointVector = new Vector3(rotatedVector.x, yValue, rotatedVector.z);
                //if (ImageAnalysis.IsClippingTerrain(pivot, worldPointVector) == true) continue; //don't consider rays that are clipping the terrain either
                proposedLocations.Add(worldPointVector);
                //Debug.DrawLine(pivot, worldPointVector, Color.white);
            }
            
            return proposedLocations;
        }
/*
        public static List<Vector3> BranchLocationsFromPosition(Vector3 pivot, Vector3 direction, float distance, RoadSegment reference) {
            float maxAngle = reference.IsHighway ? CitySettings.instance.maxAngleBetweenHighwayBranchSegments : CitySettings.instance.maxAngleBetweenStreetBranchSegments;
            int numBranches = reference.IsHighway ? CitySettings.instance.maxHighwayBranches : CitySettings.instance.maxStreetBranches;
            return BranchLocationsFromPosition(pivot, direction, distance, maxAngle, numBranches);
        }*/

        /// <summary>
        /// Get a list of new positions based on the segment masks described in section 5 of "E. Galin, A. Peytavie, N. Maréchal, E. Guérin / Procedural Generation of Roads"
        /// For all grid points p(i,j) around the pivot point P where i != 0 and j != 0 and i,j within [-distance*resoulution,distance*resolution], choose those points where
        /// the greatest common divisor of i and j is 1.
        /// </summary>
        /// <param name="pivot"></param>
        /// <param name="distance"></param>
        /// <param name="resolution"></param>
        /// <returns>A list of offset vectors, note the Y value of the terrain is not returned. It is always 0.</returns>
        public static List<Vector3Int> BranchLocationsBySegmentMask(Vector3Int pivot, int distance, int resolution = 1) {
            List<Vector3Int> points = new List<Vector3Int>();
            int v = distance;// * resolution;
            for (int i = -v; i <= v; i++) {
                for (int j = -v; j <= v; j++) {
                    if (i == 0 && j == 0) continue;
                    if (Maths.GreatestCommonDivisor((uint)System.Math.Abs(i), (uint)System.Math.Abs(j)) != 1) continue;

                    points.Add(new Vector3Int(pivot.x+(i*resolution), 0, pivot.z+(j*resolution)));
                }
            }
            return points;
        }

        /// <summary>
        /// Randomly sample positions for potential tunnel passages. 
        /// </summary>
        /// <param name="pivot"></param>
        /// <param name="minRadius"></param>
        /// <param name="maxRadius"></param>
        /// <param name="numBranches"></param>
        /// <returns></returns>
//        fix all of this, it isn't generating valid points 
        private static List<Vector3Int> BranchTunnelFromPosition(Vector3Int pivot, float minRadius, float maxRadius, int roadPriority, int numBranches = 20) {
            int maskResolution = CitySettings.GetTunnelSegmentMaskResolution(roadPriority);
            List<Vector3Int> list = new List<Vector3Int>();
            for (int i = 0; i < numBranches; i++) {
                float r;
                float t;
                int x = 0;
                int z = 0;
                int j = 0;
                //stochastically sample endpoints within a minimum and maximum range, ensure they are on our grid to prevent
                //extraneous searching of the same path
                do {
                    if (j > 100) break;
                    j++;
                    r = UnityEngine.Random.Range(minRadius, maxRadius);
                    t = UnityEngine.Random.Range(0, 2*Mathf.PI);
                    x = Mathf.RoundToInt(r * Mathf.Cos(t));
                    z = Mathf.RoundToInt(r * Mathf.Sin(t));
                } while (Maths.GreatestCommonDivisor((uint)Mathf.Abs(x), (uint)Mathf.Abs(z)) != maskResolution);
                if (j > 100) continue;
                Vector3Int point = new Vector3Int(x + pivot.x, 0, z + pivot.z);
                if (ImageAnalysis.PointInBounds(point) == false) continue;
                list.Add(point);
            }
            return list;
        }

        /// <summary>
        /// The same as the other WeightedEndpointsByTerrainHeight, except this one generates the list of path extensions via segment mask.
        /// Assumes the Y value for the startPosition is 0, it will also return the Y value of the endpoints as 0.
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        /*
        public static List<Tuple<float, Vector3Int>> WeightedEndpointsByTerrainHeight(Vector3Int startPosition, int distance, int resolution, int roadPriority = 0) {
            List<Tuple<float, Vector3Int>> sortedEndpoints = new List<Tuple<float, Vector3Int>>();
            List<Vector3Int> endpoints = BranchLocationsBySegmentMask(startPosition, distance, resolution);
            float yStart = ImageAnalysis.TerrainHeightAt(startPosition);
            Vector3 startPos = new Vector3(startPosition.x, yStart, startPosition.z);
            foreach (Vector3Int endpoint in endpoints) {
                if (ImageAnalysis.PointInBounds(endpoint) == false) continue;
                float yEndpoint = ImageAnalysis.TerrainHeightAt(endpoint);
                //Debug.DrawLine(new Vector3(startPosition.x, yStart, startPosition.z), new Vector3(endpoint.x, yEndpoint, endpoint.z), new Color(255, 165, 0));
                Vector3 e = endpoint + (Vector3.up * yEndpoint);
                float slopeCost = SlopeCost(startPos, e, roadPriority);
                if (slopeCost == float.PositiveInfinity) continue;
                if (SegmentGoesOverWater(startPos, endpoint)) continue;
                float weight = DistanceOverTerrain(startPos, e) + slopeCost;
                //Debug.Log($"Vector: {startPosition} to {endpoint} -> {sampleSum}");
                sortedEndpoints.Add(new Tuple<float, Vector3Int>(weight, endpoint));
            }
            //Debug.Break();
            return sortedEndpoints;
        }*/

        ///<summary>
        ///Searches for potential road endpoints based on the current node.
        ///</summary>
        public static List<Tuple<float, Vector3Int>> WeightedEndpointsFromNode(Node node, int distance, int resolution, int roadPriority, Vector3Int goal) {
            List<Tuple<float, Vector3Int>> sortedEndpoints = new List<Tuple<float, Vector3Int>>();
            List<Vector3Int> endpoints = BranchLocationsBySegmentMask(node.position, distance, resolution);
            Vector3Int prev = node.head ? node.position : node.cost.parentPosition;
            foreach (Vector3Int endpoint in endpoints) {
                if (ImageAnalysis.PointInBounds(endpoint) == false) continue;
                //since we are a road endpoint we will always be on top of the terrain.
                if (OverallTerrainCost(prev, node.worldPosition, new Vector3(endpoint.x, ImageAnalysis.TerrainHeightAt(endpoint), endpoint.z), roadPriority, out float weight)) {
                    sortedEndpoints.Add(new Tuple<float, Vector3Int>(weight, endpoint));
                }
            }
            return sortedEndpoints;
        }

        /// <summary>
        /// Get a set of weighted tunnel endpoints at a world position. Tunnels are parametrized by its slope and depth of the tunnel under the terrain (to prevent tunnels being formed where they don't need to be.)
        /// Be sure to use the Y value of tunnels and bridges returned by this function as the actual Y value of the node. This allows them to travel inside terrain.
        /// </summary>
        /// <param name="startPosition">Start position</param>
        /// <param name="minRadius">The minimum allowed length of the tunnel.</param>
        /// <param name="maxRadius">The maximum allowed length of the tunnel.</param>
        /// <param name="numBranches">Samples taken</param>
        /// <returns>A list of weighted tunnel endpoints</returns>
        public static List<Tuple<float, Vector3Int>> WeightedTunnelEndpoints(Node node, float minRadius, float maxRadius, int numBranches = 10) {
            //Debug.Log($"Start: {startPosition}\tDirection: {direction}\tDistance: {distance}");
            List<Vector3Int> endpoints = BranchTunnelFromPosition(node.position, minRadius, maxRadius, node.priority, numBranches);
            List<Tuple<float, Vector3Int>> preSortedEndpoints = new List<Tuple<float, Vector3Int>>();
            //Vector3 highestSumVector = Vector3.zero;
            //float maxValue = float.MinValue;
            Vector3Int prev = node.head ? node.position : node.cost.parentPosition;
            foreach (Vector3Int endpoint in endpoints) {
                if (endpoint == node.position) continue;
                /*if (diff.y < 0) diff += Vector3.up * diff.y/2f;
                else diff -= Vector3.up * diff.y/2f;*/
                //Vector3 newEndpoint = node.worldPosition + (diff.normalized * Vector3.Distance(node.worldPosition, new Vector3(endpoint.x, node.worldPosition.y, endpoint.z)));
                //newEndpoint = new Vector3(Mathf.RoundToInt(newEndpoint.x), newEndpoint.y, Mathf.RoundToInt(newEndpoint.z));
                if (OverallTunnelCost(prev, node.worldPosition, new Vector3(endpoint.x, node.yValue, endpoint.z), node.priority, out float weight)) {
                    //Debug.Log($"start: {node.worldPosition} endpoint: {endpoint} cost: {weight}");
                    //Debug.Log($"Vector: {startPosition} to {endpoint} -> {sampleSum}");
                    preSortedEndpoints.Add(new Tuple<float, Vector3Int>(weight, endpoint));
                }
            }
            //Debug.Log($"found {preSortedEndpoints.Count} potential endpoints");
            return preSortedEndpoints;
        }

        public static List<Tuple<float, Vector3Int>> WeightedBridgeEndpoints(Node node, float minRadius, float maxRadius, int numBranches=10) {
            Vector3 startPosition = node.worldPosition;
            List<Tuple<float,Vector3Int>> preSortedEndpoints = new List<Tuple<float, Vector3Int>>();
            List<Vector3Int> endpoints = BranchTunnelFromPosition(node.position, minRadius, maxRadius, numBranches);
            //Debug.Log(endpoints.Count);
            foreach (Vector3Int endpoint in endpoints) {
                if (endpoint == node.position) continue;
                if (OverallBridgeCost(node.cost.parentPosition, node.worldPosition, new Vector3(endpoint.x, node.yValue, endpoint.z), node.priority, out float weight)) {
                    preSortedEndpoints.Add(new Tuple<float, Vector3Int>(weight, endpoint));
                }
            }
            //Debug.Log($"Added {preSortedEndpoints.Count} new endpoints");
            return preSortedEndpoints;
        }

        /// <summary>
        /// Approximate the distance traversed in a straight line over the terrain from start to end.
        /// TODO: add estimates for bridges
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="maxSamples"></param>
        /// <returns></returns>
        public static float DistanceOverTerrain(Vector3 start, Vector3 end, int maxSamples = 40) {
            float totalCost = 0;
            float yStart = ImageAnalysis.TerrainHeightAt(start);
            float yEnd = ImageAnalysis.TerrainHeightAt(end);
            Vector3 s = new Vector3(start.x, yStart, start.z);            
            Vector3 e = new Vector3(end.x, yEnd, end.z);
            Vector3 prevEnd = s;
            for (int i = 1; i <= maxSamples; i++) {
                Vector3 samplePoint = s + ((e-s)*i/maxSamples);
                float thSample = ImageAnalysis.TerrainHeightAt(samplePoint);
                samplePoint = new Vector3(samplePoint.x, thSample, samplePoint.z);
                totalCost += Vector3.Distance(prevEnd, samplePoint);
                prevEnd = samplePoint;
            }
            return totalCost;
        }

        /// <summary>
        /// How much does it cost to move from start to end assuming this path is a tunnel. The tunnel needs a Vector3 with a y value set at the tunnel Y value.
        /// This is so tunnel segments can travel through terrain. When we use Tunnel endpoints we must remember to round the x and z coordinates to the nearest integer.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="maxSamples"></param>
        /// <returns></returns>
        private static float TunnelCost(Vector3 start, Vector3 end, int roadPriority, int maxSamples = 20, int minCost = 25) {
            if (Vector3.Distance(start, end) < CitySettings.GetTunnelSegmentMaskResolution(roadPriority)) return float.PositiveInfinity;
            if (SegmentGoesThroughTerrain(start, end, out float percentUnderTerrain, out float sumOfDepth) && percentUnderTerrain > .8f) {
                //Debug.Log(sumOfDepth);
                return sumOfDepth;
            }
            return float.PositiveInfinity;
        }

        /// <summary>
        /// Bridges are weighted by how much water they go over.
        /// If they go inside any land they are refused.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="maxSamples"></param>
        /// <returns></returns>
        private static float BridgeCost(Vector3 start, Vector3 end, int roadPriority, int maxSamples = 20) {
            if (Vector3.Distance(start, end) < CitySettings.GetTunnelSegmentMaskResolution(roadPriority)) return float.PositiveInfinity;
            if (SegmentGoesOverWater(start, end, out float percentAboveWater)) {
                return 1 + (10  * percentAboveWater);
            }
            return float.PositiveInfinity;
            /*
            start += Vector3Int.down * start.y;
            end += Vector3Int.down * end.y;
            if (start == end) return float.PositiveInfinity;       
            Vector3 s = new Vector3(start.x, ImageAnalysis.TerrainHeightAt(start), start.z);
            Vector3 e = new Vector3(end.x, ImageAnalysis.TerrainHeightAt(end), end.z);
            float cost = 0;
            for (int i = 0; i <= maxSamples; i++) {
                Vector3 sp = Vector3.Lerp(s, e, 1f*i/maxSamples);
                float height = sp.y - ImageAnalysis.TerrainHeightAt(sp);
                if (height > 0) {
                    cost += height;
                } else {
                    //bridges cannot go underground
                    return float.PositiveInfinity;
                }
            }

            return cost;*/
            /*
            terminatedEndpoint = end;
            float scale = CitySettings.GetBridgeCostScaler(roadPriority);
            float minLength = CitySettings.GetSegmentMaskResolution(roadPriority);
            float maxHeight = 30;
            //float biggestDiff = 0;
            float bridgeCost;
            float waterBonus = 1f;
            Vector3 previousSample = start;
            for (int i = 1; i <= maxSamples; i++) {
                Vector3 samplePoint = Vector3.Lerp(start, end, 1f*i/maxSamples);
                Vector3Int spi = new Vector3Int(Mathf.RoundToInt(samplePoint.x), 0, Mathf.RoundToInt(samplePoint.z));
                if (spi.x == start.x && spi.z == start.z) continue;
                float terrainHeight = ImageAnalysis.TerrainHeightAt(spi);
                if (ImageAnalysis.PointOverWater(spi)) waterBonus -= 0.01f;
                if (samplePoint.y > terrainHeight && samplePoint.y - terrainHeight < 1 && SlopeCost(previousSample, spi, roadPriority) != float.PositiveInfinity) {
                    bridgeCost = Vector3.Distance(start, spi);
                    if (bridgeCost < minLength) continue;
                    terminatedEndpoint = spi;
                    return bridgeCost * waterBonus * scale;
                }

                float bridgeHeight = samplePoint.y;
                float diff = bridgeHeight - terrainHeight;
                if (diff < 0 && previousSample != start) {
                    //todo: make this turn into a tunnel somehow?
                    //or terminate the node here and let the algo
                    //process tunnel points organically
                    //Debug.Log($"Bridge hit terrain!");
                    bridgeCost = Vector3.Distance(start, previousSample);
                    terminatedEndpoint = new Vector3Int(Mathf.RoundToInt(previousSample.x), 0, Mathf.RoundToInt(previousSample.z));
                    return bridgeCost * waterBonus * scale;
                }
                if (diff > maxHeight) {
                    //Debug.Log($"Max height of bridge exceeded by {diff}!");
                    return float.PositiveInfinity;
                }
                //if (diff > biggestDiff) biggestDiff = diff;
                previousSample = spi;
            }

            bridgeCost = Vector3.Distance(start, end);
            //Debug.Log($"Terminated Endpoint: {terminatedEndpoint}");

            return bridgeCost * waterBonus * scale;
            */
        }

        /// <summary>
        /// Calculate the gradient of the road, the "rise over run". The vertical distance covered divided by the flat ground distance covered. 
        /// Take that value and multiply it by 100 for a percentage grade. 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="maxGradePercentage">The maximum allowed grade of the roadway, expressed as a percentage. 100 * dy/dx.</param>
        /// <returns>The slope cost which is the grade divided by 100. The Delta y/delta x.</returns>
        private static float SlopeCost(Vector3Int start, Vector3Int end, int roadPriority = 0, float maxGradePercentage=-1) {
            //only consider side to side curvature.
            Vector3 s = new Vector3(start.x, ImageAnalysis.TerrainHeightAt(start), start.z);
            Vector3 e = new Vector3(end.x, ImageAnalysis.TerrainHeightAt(end), end.z);
            return SlopeCost(s, e, roadPriority, maxGradePercentage);
            /*
            start += Vector3Int.down * start.y;
            end += Vector3Int.down * end.y;
            if (start == end) return 0;
            float e = .001f;
            if (maxGradePercentage == -1) maxGradePercentage = CitySettings.GetMaxSlope(roadPriority);
            //calculate the slope of the bridge
            float flatGroundDist = Vector3.Distance(new Vector3(start.x, 0, start.z), new Vector3(end.x, 0, end.z));
            float heightDiff = ImageAnalysis.TerrainHeightAt(end) - ImageAnalysis.TerrainHeightAt(start); //+ means going uphill - means going downhill
            float grade = 100*Math.Abs(heightDiff/flatGroundDist);
            if (grade >= maxGradePercentage) return float.PositiveInfinity;
            //Debug.Log($"Grade: {100*grade}");
            //return maxGradePercentage / (maxGradePercentage - grade);
            return 100*grade;
            */
        }

        /// <summary>
        /// Check if a segment is legal according to the rules.
        /// </summary>
        /// <param name="previous"></param>
        /// <param name="current"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static bool SegmentIsLegal(Vector3 previous, Vector3 current, Vector3 endpoint) {
            float s = SlopeCost(current, endpoint);
            float cc = CurvatureCost(previous, current, endpoint);
            Debug.Log($"slope: {s} | curve: {cc}");
            return s + cc != float.PositiveInfinity;
            //return SlopeCost(current, endpoint) != float.PositiveInfinity && CurvatureCost(previous, current, endpoint) != float.PositiveInfinity;
        }

        private static float SlopeCost(Vector3 start, Vector3 end, int roadPriority=0, float maxGradePercentage=-1) {
            float d = Vector3.Distance(start, end);
            if (d <= .001f) return 0;
            if (maxGradePercentage == -1) maxGradePercentage = CitySettings.GetMaxSlope(roadPriority);
            //calculate the slope of the bridge
            float grade = 100*Math.Abs((end.y - start.y)/Vector3.Distance(new Vector3(start.x, 0, start.z), new Vector3(end.x, 0, end.z)));
            if (grade >= maxGradePercentage) return float.PositiveInfinity;
            //Debug.Log($"Grade: {100*grade}");
            //return maxGradePercentage / (maxGradePercentage - grade);
            if (float.IsNaN(grade)) return float.PositiveInfinity;
            return 100*grade;
        }

        /// <summary>
        /// Given a current node, one of its potential endpoints and its predecessor, weight the cost of the curve of the road.
        /// The higher value of beta, the more cost is given to curvier roads. 
        /// </summary>
        /// <param name="previous">The parent node of current.</param>
        /// <param name="current">The parent node of the endpoint.</param>
        /// <param name="endpoint">The new endpoint.</param>
        /// <param name="beta">Cost scaling value.</param>
        /// <returns>The weight of the segment</returns>
        private static float CurvatureCost(Vector3 previous, Vector3 current, Vector3 endpoint, int roadPriority = 0) {
            //only consider side to side curvature.
            previous = new Vector3(previous.x, 0, previous.z);
            current = new Vector3(current.x, 0, current.z);
            endpoint = new Vector3(endpoint.x, 0, endpoint.z);
            if (Vector3.Distance(previous, current) < .0001f || Vector3.Distance(current, endpoint) < .00001f || Vector3.Distance(previous, endpoint) < .00001f) return 0;
            float beta = CitySettings.GetMaxCurvature(roadPriority);
            //vector3.angle returns a value between 0 and 180
            float theta = Vector3.Angle(current-previous,endpoint-current);//Mathf.Atan2(endpoint.position.z-current.position.z, endpoint.position.x-current.position.x) - Mathf.Atan2(current.position.z-current.cost.parentPosition.z, current.position.x-current.cost.parentPosition.x);
            if (theta > beta) return float.PositiveInfinity;
            //Debug.Log($"Angle: {theta} | Prev: {previous} | Curr: {current} | End: {endpoint}");
            float s = theta*beta/180;
            //Debug.Log($"Curvature cost: {s}");
            if (float.IsNaN(s)) return float.PositiveInfinity;
            return s;
        }

        /// <summary>
        /// An overload that considers a current and endpoint node at any Y value, this is so we can reject overland roads that are underground. Or accept them if they had just come out of the terrain.
        /// </summary>
        /// <param name="previous"></param>
        /// <param name="current"></param>
        /// <param name="endpoint"></param>
        /// <param name="roadPriority"></param>
        /// <returns></returns>
        public static bool OverallTerrainCost(Vector3Int previous, Vector3 current, Vector3 endpoint, int roadPriority, out float weight) {
            weight = float.PositiveInfinity;
            //overland roads cannot be underground
            if (current.y < ImageAnalysis.TerrainHeightAt(current) || SegmentGoesOverWater(current, endpoint, out _)) return false;
            float d = DistanceOverTerrain(current, endpoint) + SlopeCost(current, endpoint, roadPriority) + CurvatureCost(previous, current, endpoint, roadPriority);
            if (float.IsNaN(d) || d == float.PositiveInfinity) return false;
            weight = d;
            return true;/*
            float d = DistanceOverTerrain(current, endpoint);
            float s = SlopeCost(current, endpoint, roadPriority);
            float c = CurvatureCost(previous, current, endpoint);
            return d + s + c;*/
        }

        public static bool OverallBridgeCost(Vector3Int previous, Vector3 current, Vector3 endpoint, int roadPriority, out float weight) {
            weight = float.PositiveInfinity;
            if (current.y < ImageAnalysis.TerrainHeightAt(current) || SegmentGoesThroughTerrain(current, endpoint, out _, out _)) return false;
            float b = (BridgeCost(current, endpoint, roadPriority) * CitySettings.GetBridgeCostScaler(roadPriority)) + SlopeCost(current, endpoint, roadPriority) + CurvatureCost(previous, current, endpoint, roadPriority);
            if (float.IsNaN(b) || b == float.PositiveInfinity) return false;
            weight = b;
            return true;
            /*
            float s = SlopeCost(current, endpoint, roadPriority);
            float c = CurvatureCost(previous, current, endpoint, roadPriority);
            //Debug.Log($"Slope Current TerminatedEndpoint: {s} {current} {terminatedEndpoint}");
            //.Log($"Bridge Slope CurvatureCost: {b} {s} {c}");
            //return b*s*c;
            return b + s + c;*/
        }

        /// <summary>
        /// For tunnel and bridge costs current and endpoint need to be Vector3 with Y value set at the height of the tunnel or bridge.
        /// </summary>
        /// <param name="previous"></param>
        /// <param name="current"></param>
        /// <param name="endpoint"></param>
        /// <param name="roadPriority"></param>
        /// <returns></returns>
        public static bool OverallTunnelCost(Vector3Int previous, Vector3 current, Vector3 endpoint, int roadPriority, out float weight) {
            weight = float.PositiveInfinity;
            //Vector3Int c = new Vector3Int((int)current.x, 0, (int)current.z);
            //Vector3Int e = new Vector3Int((int)endpoint.x, 0, (int)endpoint.z);
            float t = (TunnelCost(current, endpoint, roadPriority) * CitySettings.GetTunnelCostScaler(roadPriority)) + SlopeCost(current, endpoint, roadPriority) + CurvatureCost(previous, current, endpoint, roadPriority);
            if (float.IsNaN(t) || t == float.PositiveInfinity) return false;
            weight = t;
            return true;
            /*
            float s = SlopeCost(current, endpoint, roadPriority);
            float cc = CurvatureCost(previous, current, endpoint, roadPriority);
            Debug.Log($"Tunnel Slope CurvatureCost: {t} {s} {cc}");
            return t + s + cc;*/
        }

        public static bool SegmentGoesOverWater(Vector3 startPos, Vector3 endpoint, out float percentAboveWater) {
            bool isOverWater = false;
            int maxSamples = 10;
            percentAboveWater = 0;
            for (int i = 0; i < maxSamples; i++) {
                Vector3 samplePoint = startPos + ((endpoint - startPos)*i/(maxSamples-1));
                if (ImageAnalysis.PointOverWater(samplePoint)) {
                    isOverWater = true;
                    percentAboveWater += 1f/maxSamples;
                }
            }
            return isOverWater;
        }

        private static bool SegmentGoesThroughTerrain(Vector3 startPos, Vector3 endpoint, out float percentUnderTerrain, out float sumOfDepth) {
            bool isUnderTerrain = false;
            int maxSamples = 50;
            float minDistUndergroundToCount = 2;
            percentUnderTerrain = 0;
            sumOfDepth = 0;
            for (int i = 0; i < maxSamples; i++) {
                Vector3 samplePoint = startPos + ((endpoint - startPos)*i/(maxSamples-1));
                float partialSum = ImageAnalysis.TerrainHeightAt(samplePoint) - samplePoint.y;
                sumOfDepth += partialSum;
                if (partialSum > minDistUndergroundToCount) {
                    //Debug.Log($"Terrain: {partialSum+samplePoint.y} | Sample: {samplePoint.y} | PartialSum: {partialSum}");
                    //Debug.Break();
                    isUnderTerrain = true;
                    percentUnderTerrain += 1f/maxSamples;
                }
            }
            return isUnderTerrain;
        }
    }
}