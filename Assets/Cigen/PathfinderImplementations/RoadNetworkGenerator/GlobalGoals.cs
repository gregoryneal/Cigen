using Cigen.ImageAnalyzing;
using GeneralPathfinder;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Cigen {
    /// <summary>
    /// This will create branches from segments and vectors to find new places to build a road on.
    /// This is how we grow the network at any stage. 
    /// </summary>
    public class GlobalGoals {

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
        private static List<Vector3Int> BranchTunnelFromPosition(AnisotropicLeastCostPathSettings settings, Vector3Int pivot, float minRadius, float maxRadius, int pathPriority, int numBranches = 20) {
            //Debug.Log($"BranchTunnefromPosition priority: {pathPriority}");
            return BranchLocationsBySegmentMask(pivot, settings.GetTunnelSegmentMaskValue(pathPriority), settings.GetTunnelSegmentMaskResolution(pathPriority));
            /*
            int maskResolution = settings.GetTunnelSegmentMaskResolution(pathPriority);
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
                if (ImageAnalysis.PointInBounds(point, settings) == false) continue;
                list.Add(point);
            }
            return list;*/
        }

        ///<summary>
        ///Searches for potential road endpoints based on the current node.
        ///</summary>
        public static List<Tuple<float, Vector3Int>> WeightedEndpointsFromNode(Node node, AnisotropicLeastCostPathSettings settings, int pathPriority) {
            List<Tuple<float, Vector3Int>> sortedEndpoints = new List<Tuple<float, Vector3Int>>();
            List<Vector3Int> endpoints = BranchLocationsBySegmentMask(node.position, settings.GetSegmentMaskValue(pathPriority), settings.GetSegmentMaskResolution(pathPriority));
            Vector3Int prev = node.head ? node.position : node.cost.parentPosition;
            foreach (Vector3Int endpoint in endpoints) {
                if (ImageAnalysis.PointInBounds(endpoint, settings) == false) continue;
                //since we are a road endpoint we will always be on top of the terrain.
                if (OverallTerrainCost(prev, node.worldPosition, new Vector3(endpoint.x, ImageAnalysis.TerrainHeightAt(endpoint, settings), endpoint.z), settings, pathPriority, out float weight)) {
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
        public static List<Tuple<float, Vector3Int>> WeightedTunnelEndpoints(Node node, AnisotropicLeastCostPathSettings settings, float minRadius, float maxRadius, int numBranches = 10) {
            //Debug.Log($"Start: {startPosition}\tDirection: {direction}\tDistance: {distance}");
            List<Vector3Int> endpoints = BranchTunnelFromPosition(settings, node.position, minRadius, maxRadius, node.priority, numBranches);
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
                if (OverallTunnelCost(prev, node.worldPosition, new Vector3(endpoint.x, node.yValue, endpoint.z), settings, node.priority, out float weight)) {
                    //Debug.Log($"start: {node.worldPosition} endpoint: {endpoint} cost: {weight}");
                    //Debug.Log($"Vector: {startPosition} to {endpoint} -> {sampleSum}");
                    preSortedEndpoints.Add(new Tuple<float, Vector3Int>(weight, endpoint));
                }
            }
            //Debug.Log($"found {preSortedEndpoints.Count} potential endpoints");
            return preSortedEndpoints;
        }

        public static List<Tuple<float, Vector3Int>> WeightedBridgeEndpoints(Node node, AnisotropicLeastCostPathSettings settings, float minRadius, float maxRadius, int numBranches=10) {
            Vector3 startPosition = node.worldPosition;
            List<Tuple<float,Vector3Int>> preSortedEndpoints = new List<Tuple<float, Vector3Int>>();
            List<Vector3Int> endpoints = BranchTunnelFromPosition(settings, node.position, minRadius, maxRadius, node.priority, numBranches);
            //Debug.Log(endpoints.Count);
            foreach (Vector3Int endpoint in endpoints) {
                if (endpoint == node.position) continue;
                if (OverallBridgeCost(node.cost.parentPosition, node.worldPosition, new Vector3(endpoint.x, node.yValue, endpoint.z), settings, node.priority, out float weight)) {
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
        public static float DistanceOverTerrain(Vector3 start, Vector3 end, AnisotropicLeastCostPathSettings settings, int maxSamples = 40) {
            float totalCost = 0;
            float yStart = ImageAnalysis.TerrainHeightAt(start, settings);
            float yEnd = ImageAnalysis.TerrainHeightAt(end, settings);
            Vector3 s = new Vector3(start.x, yStart, start.z);            
            Vector3 e = new Vector3(end.x, yEnd, end.z);
            Vector3 prevEnd = s;
            for (int i = 1; i <= maxSamples; i++) {
                Vector3 samplePoint = s + ((e-s)*i/maxSamples);
                float thSample = ImageAnalysis.TerrainHeightAt(samplePoint,settings);
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
        private static float TunnelCost(Vector3 start, Vector3 end, AnisotropicLeastCostPathSettings settings, int pathPriority, int maxSamples = 20, int minCost = 25) {
            //if (Vector3.Distance(start, end) < CitySettings.GetTunnelSegmentMaskResolution(pathPriority)) return float.PositiveInfinity;
            if (SegmentGoesThroughTerrain(start, end, settings, out float percentUnderTerrain, out float sumOfDepth) && percentUnderTerrain > .2f) {
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
        private static float BridgeCost(Vector3 start, Vector3 end, AnisotropicLeastCostPathSettings settings, int pathPriority, int maxSamples = 20) {
            //if (Vector3.Distance(start, end) < CitySettings.GetTunnelSegmentMaskResolution(pathPriority)) return float.PositiveInfinity;
            if (SegmentGoesOverWater(start, end, settings, out float percentAboveWater)) {
                return 1 + (10  * percentAboveWater);
            }
            return float.PositiveInfinity;
        }
        /// <summary>
        /// Check if a segment is legal according to the rules.
        /// </summary>
        /// <param name="previous"></param>
        /// <param name="current"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static bool SegmentIsLegal(Vector3 previous, Vector3 current, Vector3 endpoint, PathfinderSettings settings, int pathPriority = 0) {
            float s = SlopeCost(current, endpoint, settings.GetMaxSlope(pathPriority));
            float cc = CurvatureCost(previous, current, endpoint, settings.GetMaxCurvature(pathPriority));
            //Debug.Log($"slope cost: {s}");
            //Debug.Log($"curve cost: {cc}");
            //Debug.Log($"slope: {s} | curve: {cc}");
            return s + cc != float.PositiveInfinity;
            //return SlopeCost(current, endpoint) != float.PositiveInfinity && CurvatureCost(previous, current, endpoint) != float.PositiveInfinity;
        }

        private static float SlopeCost(Vector3 start, Vector3 end, float maxSlopeDegrees) {
            //float d = Vector3.Distance(start, end);
            //if (d <= .001f) return 0;
            //if (maxGradePercentage == -1) maxGradePercentage = CitySettings.GetMaxSlope(pathPriority);
            //calculate the slope of the bridge
            float y = Math.Abs(end.y - start.y);
            float x = Vector3.Distance(new Vector3(start.x, 0, start.z), new Vector3(end.x, 0, end.z));
            float degs = Mathf.Rad2Deg * Mathf.Atan2(y,x);
            if (degs >= maxSlopeDegrees) return float.PositiveInfinity;
            //Debug.Log($"Degrees: {degs}, Y: {y}, X: {x}");
            float weight = degs/maxSlopeDegrees;
            //Debug.Log($"Grade: {100*grade}");
            //return maxGradePercentage / (maxGradePercentage - grade);
            if (float.IsNaN(weight)) return float.PositiveInfinity;
            //Debug.Log($"Slope Weight: {grade}");
            return weight;
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
        private static float CurvatureCost(Vector3 previous, Vector3 current, Vector3 endpoint, float maxAngleDegrees = 45) {
            //only consider side to side curvature.
            previous = new Vector3(previous.x, 0, previous.z);
            current = new Vector3(current.x, 0, current.z);
            endpoint = new Vector3(endpoint.x, 0, endpoint.z);
            //if (Vector3.Distance(previous, current) < .0001f || Vector3.Distance(current, endpoint) < .00001f || Vector3.Distance(previous, endpoint) < .00001f) return 0;
            //float beta = CitySettings.GetMaxCurvature(pathPriority);
            //vector3.angle returns a value between 0 and 180
            float theta = Vector3.Angle(current-previous,endpoint-current);//Mathf.Atan2(endpoint.position.z-current.position.z, endpoint.position.x-current.position.x) - Mathf.Atan2(current.position.z-current.cost.parentPosition.z, current.position.x-current.cost.parentPosition.x);
            if (theta > maxAngleDegrees) return float.PositiveInfinity;
            //Debug.Log($"Angle: {theta} | Prev: {previous} | Curr: {current} | End: {endpoint}");
            float s = theta/180;
            //Debug.Log($"Curvature cost: {s}");
            if (float.IsNaN(s)) return float.PositiveInfinity;
            //Debug.Log($"Curvature Weight: {s}");
            return s;
        }

        /// <summary>
        /// An overload that considers a current and endpoint node at any Y value, this is so we can reject overland roads that are underground. Or accept them if they had just come out of the terrain.
        /// </summary>
        /// <param name="previous"></param>
        /// <param name="current"></param>
        /// <param name="endpoint"></param>
        /// <param name="pathPriority"></param>
        /// <returns></returns>
        public static bool OverallTerrainCost(Vector3Int previous, Vector3 current, Vector3 endpoint, AnisotropicLeastCostPathSettings settings, int pathPriority, out float weight) {
            weight = float.PositiveInfinity;
            //overland roads cannot be underground
            if (current.y < ImageAnalysis.TerrainHeightAt(current, settings) || SegmentGoesOverWater(current, endpoint, settings, out _)) return false;
            float d = DistanceOverTerrain(current, endpoint, settings) + SlopeCost(current, endpoint, settings.GetMaxSlope(pathPriority)) + CurvatureCost(previous, current, endpoint, settings.GetMaxCurvature(pathPriority));
            if (float.IsNaN(d) || d == float.PositiveInfinity) return false;
            weight = d;
            //Debug.Log($"Terrain Weight: {weight}");
            return true;/*
            float d = DistanceOverTerrain(current, endpoint);
            float s = SlopeCost(current, endpoint, pathPriority);
            float c = CurvatureCost(previous, current, endpoint);
            return d + s + c;*/
        }

        public static bool OverallBridgeCost(Vector3Int previous, Vector3 current, Vector3 endpoint, AnisotropicLeastCostPathSettings settings, int pathPriority, out float weight) {
            weight = float.PositiveInfinity;
            if (current.y < ImageAnalysis.TerrainHeightAt(current, settings) || SegmentGoesThroughTerrain(current, endpoint, settings, out _, out _)) return false;
            float b = (BridgeCost(current, endpoint, settings, pathPriority) * settings.GetBridgeCostScaler(pathPriority)) + SlopeCost(current, endpoint, settings.GetMaxSlope(pathPriority)) + CurvatureCost(previous, current, endpoint, settings.GetMaxCurvature(pathPriority));
            if (float.IsNaN(b) || b == float.PositiveInfinity) return false;
            weight = b;
            //Debug.Log($"Bridge Weight: {weight}");
            return true;
            /*
            float s = SlopeCost(current, endpoint, pathPriority);
            float c = CurvatureCost(previous, current, endpoint, pathPriority);
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
        /// <param name="pathPriority"></param>
        /// <returns></returns>
        public static bool OverallTunnelCost(Vector3Int previous, Vector3 current, Vector3 endpoint, AnisotropicLeastCostPathSettings settings, int pathPriority, out float weight) {
            weight = float.PositiveInfinity;
            //Vector3Int c = new Vector3Int((int)current.x, 0, (int)current.z);
            //Vector3Int e = new Vector3Int((int)endpoint.x, 0, (int)endpoint.z);
            float t = (TunnelCost(current, endpoint, settings, pathPriority) * settings.GetTunnelCostScaler(pathPriority)) + SlopeCost(current, endpoint, settings.GetMaxSlope(pathPriority)) + CurvatureCost(previous, current, endpoint, settings.GetMaxCurvature(pathPriority));
            if (float.IsNaN(t) || t == float.PositiveInfinity) return false;
            weight = t;
            //Debug.Log($"Tunnel Weight: {weight}");
            return true;
            /*
            float s = SlopeCost(current, endpoint, pathPriority);
            float cc = CurvatureCost(previous, current, endpoint, pathPriority);
            Debug.Log($"Tunnel Slope CurvatureCost: {t} {s} {cc}");
            return t + s + cc;*/
        }

        public static bool SegmentGoesOverWater(Vector3 startPos, Vector3 endpoint, AnisotropicLeastCostPathSettings settings, out float percentAboveWater) {
            bool isOverWater = false;
            int maxSamples = 10;
            percentAboveWater = 0;
            for (int i = 0; i < maxSamples; i++) {
                Vector3 samplePoint = startPos + ((endpoint - startPos)*i/(maxSamples-1));
                if (ImageAnalysis.PointOverWater(samplePoint, settings)) {
                    isOverWater = true;
                    percentAboveWater += 1f/maxSamples;
                }
            }
            return isOverWater;
        }

        private static bool SegmentGoesThroughTerrain(Vector3 startPos, Vector3 endpoint, AnisotropicLeastCostPathSettings settings, out float percentUnderTerrain, out float sumOfDepth) {
            bool isUnderTerrain = false;
            int maxSamples = 10;
            float minDistUndergroundToCount = 2;
            percentUnderTerrain = 0;
            sumOfDepth = 0;
            for (int i = 0; i < maxSamples; i++) {
                Vector3 samplePoint = startPos + ((endpoint - startPos)*i/(maxSamples-1));
                float partialSum = ImageAnalysis.TerrainHeightAt(samplePoint, settings) - samplePoint.y;
                sumOfDepth += partialSum;
                if (partialSum > minDistUndergroundToCount) {
                    //Debug.Log($"Terrain: {partialSum+samplePoint.y} | Sample: {samplePoint.y} | PartialSum: {partialSum}");
                    //Debug.Break();
                    isUnderTerrain = true;
                    percentUnderTerrain += 1f/maxSamples;
                }
            }

            //Debug.Log($"Percent Under Terrain: {percentUnderTerrain}");
            return isUnderTerrain;
        }
    }
}