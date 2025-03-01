using System.Runtime.CompilerServices;
using Cigen.ImageAnalyzing;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework.Constraints;
using OpenCvSharp;
using UnityEditor;
using System;
using System.Linq;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
using Cigen.Structs;
using static Cigen.Maths.Math;
using UnityEditor.Experimental.GraphView;
using System.Data.Common;

namespace Cigen {
    /// <summary>
    /// A local constraint is applied to a road segment to apply rules to that segment based on it's parameters in relation to the input image maps.
    /// This might alter the road segment parameters themselves. If any in a chain of local constraints fail, we must delete the segment from the final road network.
    /// ideas for constraints are things like:
    /// OverWaterConstraint -> check if endposition is over water and build a bridge or alter the endpoint to fit 
    /// NearOtherIntersectionsConstraint -> check if we are near another endposition of a highway or street network and attach to it 
    /// OutsidePopulationCenterConstraint -> check if we are outside of a population center, if so do different thing depending on if we are a highway or street (street terminate, highway create large exploratory branches)
    /// OverNatureConstraint -> check if we are over nature and either flag roads or something
    /// TunnelingConstraint -> check if we are straddling a mountain range or something, flag the road as a tunnel or alter pathing for trails or something (how neat would that be)
    /// OverlapSegmentConstraint -> check if we are overlapping a segment, if it's a highway overlapping a road we can set a height value for both endpoints on the highway, don't let streets overlap highways though, they must terminate.
    /// GradeConstraint -> check if our segment is too steep, if so try to smooth it out or something.
    /// OutOfBoundsConstraint -> check if we are out of bounds
    /// ClippingTerrainConstraint -> this may be better off hard coded into the weighted branching functions because we never want to clip into terrain (UNLESS WE ARE A TUNNEL?)
    /// </summary>
    public class LocalConstraint {
        /// <summary>
        /// Attempt to apply the constraint to the RoadSegment.
        /// </summary>
        /// <param name="dirtySegment">The road segment in question. This is passed by reference so expect it to change during chaining.</param>
        /// <returns>True if the constraint could be satisfied, false if the segment cannot be constructed.</returns>
        public virtual bool ApplyConstraint(ref RoadSegment segment) {
            return segment;
        }

        /// <summary>
        /// Attempt to rotate the segment to fit in bounds
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns> 
        public static bool RotateSegment(ref RoadSegment segment) {
            //attempt to rotate the segment to fit on the land.
            //find candidate endpoints 
            List<Vector3> rotatedEndpoints = GlobalGoals.BranchLocationsFromPosition(segment.StartPosition, segment.EndPosition - segment.StartPosition, segment.SegmentLength, segment);
            foreach (Vector3 endpoint in rotatedEndpoints) {
                if (ImageAnalysis.PointInBounds(endpoint)) {
                    //we found a suitable end position
                    segment.EndPosition = endpoint;
                    return true;
                }
            }
            return false;
        }

        public static bool PruneSegment(ref RoadSegment segment) {
            //make sure our segment is long enough to be pruning in the first place
            if ((segment.StartPosition - segment.EndPosition).magnitude > CitySettings.instance.maxPruneLength * segment.IdealSegmentLength) {
                //attempt to prune back the segment by a max amount to fit within the bounds.
                //sample positions while lerping back from the endposition to the max prune position as a percentage of the ideal segment length
                //in the direction from end to start at a length of the maxPruneLength * IdealSegmentLength
                Vector3 maxPruneVector = (segment.StartPosition-segment.EndPosition).normalized * CitySettings.instance.maxPruneLength * segment.IdealSegmentLength;
                for (int i = 0; i <= CitySettings.instance.maxHighwayBranches; i++) {
                    Vector3 position = segment.EndPosition + (i * maxPruneVector / CitySettings.instance.maxHighwayBranches);
                    if (ImageAnalysis.PointInBounds(position)) {
                        //we found a suitable end position!
                        segment.EndPosition = position;
                        return true;
                    }
                }
                
                //no suitable position found.
                return false;
            } else {
                //we cannot prune this road
                return false;
            }
        }
    }

    /// <summary>
    /// This will create branches from segments and vectors to find new places to build a road on.
    /// This is how we grow the network at any stage. 
    /// </summary>
    public class GlobalGoals {
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
        }

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
                Vector3 rotatedVector = Maths.Math.RotateAroundPivot(straightRay, pivot, Vector3.up * angle, true);                
                if (ImageAnalysis.PointInBounds(rotatedVector) == false) continue; //don't consider any rays that are out of bounds
                float yValue = ImageAnalysis.TerrainHeightAt(rotatedVector.x, rotatedVector.z);
                Vector3 worldPointVector = new Vector3(rotatedVector.x, yValue, rotatedVector.z);
                //if (ImageAnalysis.IsClippingTerrain(pivot, worldPointVector) == true) continue; //don't consider rays that are clipping the terrain either
                proposedLocations.Add(worldPointVector);
                //Debug.DrawLine(pivot, worldPointVector, Color.white);
            }
            
            return proposedLocations;
        }

        public static List<Vector3> BranchLocationsFromPosition(Vector3 pivot, Vector3 direction, float distance, RoadSegment reference) {
            float maxAngle = reference.IsHighway ? CitySettings.instance.maxAngleBetweenHighwayBranchSegments : CitySettings.instance.maxAngleBetweenStreetBranchSegments;
            int numBranches = reference.IsHighway ? CitySettings.instance.maxHighwayBranches : CitySettings.instance.maxStreetBranches;
            return BranchLocationsFromPosition(pivot, direction, distance, maxAngle, numBranches);
        }

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
                    if (Maths.Math.GreatestCommonDivisor((uint)Math.Abs(i), (uint)Math.Abs(j)) != 1) continue;

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
        public static List<Vector3> BranchTunnelFromPosition(Vector3 pivot, float minRadius, float maxRadius, int numBranches = 20) {
            List<Vector3> list = new List<Vector3>();
            for (int i = 0; i < numBranches; i++) {
                float r = UnityEngine.Random.Range(minRadius, maxRadius);
                float t = UnityEngine.Random.Range(0, 2*Mathf.PI);
                float x = r * Mathf.Cos(t) + pivot.x;
                float z = r * Mathf.Sin(t) + pivot.z;
                Vector3 point = new Vector3(x, ImageAnalysis.TerrainHeightAt(x, z), z);
                if (ImageAnalysis.PointInBounds(point) == false) continue;
                list.Add(point);
            }
            return list;
        }

        /// <summary>
        /// Weight a list of branches by population density and distance to the start position
        /// </summary>
        /// <param name="startPosition">The start position</param>
        /// <param name="endPositions">The potential branches</param>
        /// <returns>A list of branch endpoints with their weights, sorted in descending order.</returns>
        public static List<Tuple<float, Vector3>> BranchesWeightedByDistAndPopDens(Vector3 startPosition, List<Vector3> branchEndpoints) {
            List<Tuple<float, Vector3>> preSortedEndpoints = new List<Tuple<float, Vector3>>();
            //Vector3 highestSumVector = Vector3.zero;
            //float maxValue = float.MinValue;
            foreach (Vector3 endpoint in branchEndpoints) {
                if (ImageAnalysis.PointInBounds(endpoint) == false) continue;
                //sum the sampled population density at specific intervals weighted by the inverse of the distance to the segment.EndPosition
                //the branch with the highest sum is chosen and we build our new segment in that direction, but only at IdealSegmentLength, not 10x it or whatever we use for branch length
                //this allows for more varied roads instead of just straight shots from one place to the other "my eyes can see longer than my arms can reach"
                //we can also weight the terrain height map here as well, but lets save that for experimentation later.
                int numSamples = 10;
                float sampleSum = 0;
                for (int i = 0; i <= numSamples; i++) {
                    //sample the endpoint at discrete points along the vector
                    Vector3 samplePos = startPosition + ((endpoint-startPosition)*(i/numSamples));
                    //population density
                    float pd = ImageAnalysis.PopulationDensityAt(samplePos);
                    //the terrain height at our sampled position
                    float th = ImageAnalysis.TerrainHeightAt(samplePos);
                    //distance to segment end position
                    float dst = Vector3.Distance(startPosition, new Vector3(samplePos.x, th, samplePos.z));
                    sampleSum += (1+pd) / (1+dst);
                }
                Debug.Log($"Weight: {sampleSum}");
                preSortedEndpoints.Add(new Tuple<float, Vector3>(sampleSum, endpoint));
            }
            
            return preSortedEndpoints.OrderByDescending(e => e.Item1).ToList<Tuple<float, Vector3>>();
        }

        /// <summary>
        /// Weight proposed branches that cross a body of water. Find the first point on the other side of the water, up to the max crossing amount and sample that point.
        /// The points on land are weighted by population density and distance. 
        /// </summary>
        /// <param name="startPosition">Start position in world space.</param>
        /// <param name="branchEndpoints">The potential endpoints of the bridge.</param>
        /// <returns></returns>
        public static List<Tuple<float, Vector3>> BranchesWeightedByWaterCrossing(Vector3 startPosition, List<Vector3> branchEndpoints) {
            List<Tuple<float, Vector3>> preSortedEndpoints = new List<Tuple<float, Vector3>>();
            int numSamples = 10;
            Mat waterMat = CitySettings.instance.waterMapMat;
            foreach (Vector3 endpoint in branchEndpoints) {
                //sum the sampled population density at specific intervals weighted by the inverse of the distance to the segment.EndPosition
                //the branch with the highest sum is chosen and we build our new segment in that direction, but only at IdealSegmentLength, not 10x it or whatever we use for branch length
                //this allows for more varied roads instead of just straight shots from one place to the other "my eyes can see longer than my arms can reach"
                //we can also weight the terrain height map here as well, but lets save that for experimentation later.
                for (int i = 0; i <= numSamples; i++) {
                    //sample the endpoint at discrete points along the vector
                    Vector3 samplePos = startPosition + ((endpoint-startPosition)*(1f*i/numSamples));
                    float val = ImageAnalysis.NormalizedPointOnTextureInWorldSpace(samplePos.x, samplePos.z, waterMat);
                    if (val > 0.5f) {
                        //Debug.DrawLine(startPosition, samplePos, Color.red);
                        Debug.Break();
                        Debug.Log($"SAMPLED LAND LAND with value {val}");
                        //we are not on water anymore                        
                        //population density
                        float pd = ImageAnalysis.PopulationDensityAt(samplePos);
                        //the terrain height at our sampled position
                        float th = ImageAnalysis.TerrainHeightAt(samplePos);
                        //distance to segment end position
                        float dst = Vector3.Distance(startPosition, new Vector3(samplePos.x, th, samplePos.z));
                        float weight = (1+pd)/(1+dst); //i wanna weight distance to nearby land as well, implement a check or something
                        preSortedEndpoints.Add(new Tuple<float, Vector3>(weight, samplePos));
                        Debug.Log($"WEIGHTS: pd {pd}, th {th}, dst {dst}, weight {weight}");

                        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        g.transform.position = samplePos;
                        g.transform.localScale = Vector3.one * 2;
                        g.GetComponent<Renderer>().material.color = new Color(50+(i/numSamples), 50, 50);
                        Debug.Break();
                        break;
                    } else {
                        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        g.transform.position = samplePos;
                        g.transform.localScale = Vector3.one * 20;
                        g.GetComponent<Renderer>().material.color = Color.red;
                    }
                }
            }
            
            return preSortedEndpoints.OrderByDescending(e => e.Item1).ToList();
        }

        /// <summary>
        /// Create an exploratory RoadSegment with the highest weight based on the weightedbypopdens function after branching.
        /// </summary>
        public static bool CreateWeightedSegment(Vector3 startPosition, Vector3 direction, float distance, RoadSegment reference, out RoadSegment potentialSegment, float lowWeightDistanceScale = 2) {
            List<Vector3> branchEndpoints;
            //create a new segment in that direction and add it to the list
            RoadSegment newSegment = new GameObject().AddComponent<RoadSegment>();
            potentialSegment = newSegment;

            //check if we are outside a population zone at the endposition
            if (ImageAnalysis.PopulationDensityAt(startPosition) < CitySettings.instance.populationDensityCutoff) {
                //branch out from the endposition to find new population centers
                branchEndpoints = BranchLocationsFromPosition(startPosition, direction, lowWeightDistanceScale * distance, reference);
            } else {
                branchEndpoints = BranchLocationsFromPosition(startPosition, direction, distance, reference);
            }

            if (branchEndpoints.Count == 0) {
                GameObject.Destroy(newSegment.gameObject);
                return false;
            }

            List<Tuple<float, Vector3>> ehh;
            ehh = BranchesWeightedByDistAndPopDens(startPosition, branchEndpoints);
            //string s = String.Join(',', ehh.Select(a => a.Item1.ToString()));
            //Debug.Log($"BEST WEIGHT: {ehh[0].Item1} out of {s}");
            Vector3 bestDirection = (ehh[0].Item2 - startPosition).normalized;
            /*
            foreach (Vector3 endpoint in branchEndpoints) {
                if (Math.Abs(endpoint.x - bestDirection.x) <= 0.01f && Math.Abs(endpoint.z - bestDirection.z) <= 0.01f) {
                    Debug.DrawLine(startPosition, endpoint, Color.green);
                } else {
                    Debug.DrawLine(startPosition, endpoint, Color.red);
                }
            }*/
            //create a new segment in that direction and add it to the list
            newSegment.Init(startPosition, startPosition + (bestDirection*distance), reference);
            return true;
        }

        /// <summary>
        /// Get a list of endpoints sorted by their distance to the provided start position.
        /// </summary>
        /// <param name="startPosition">The starting position.</param>
        /// <param name="distance">The sampling distance.</param>
        /// <returns>A list of endpoints sorted by their distance to the start position.</returns>
        public static List<Tuple<float, Vector3>> WeightedEndpointByDistance(Vector3 startPosition, Vector3 direction, float distance, float maxAngle = 179, int numBranches = 10, SegmentType type = SegmentType.Highway) {
            float minR = 0;
            float maxR = 0;
            switch (type) {
                case SegmentType.Highway:
                    minR = CitySettings.instance.minHighwayTunnelLength;
                    maxR = CitySettings.instance.maxHighwayTunnelLength;
                    break;
                case SegmentType.Street:
                    minR = CitySettings.instance.minStreetTunnelLength;
                    maxR = CitySettings.instance.maxStreetTunnelLength;
                    break;
                default:
                    break;
            }
            //Debug.Log($"Start: {startPosition}\tDirection: {direction}\tDistance: {distance}");
            List<Vector3> endpoints = BranchLocationsFromPosition(startPosition, direction, distance, maxAngle, numBranches);
            if (minR != maxR && minR != 0) endpoints.AddRange(BranchTunnelFromPosition(startPosition, minR, maxR));
            List<Tuple<float, Vector3>> preSortedEndpoints = new List<Tuple<float, Vector3>>();
            //Vector3 highestSumVector = Vector3.zero;
            //float maxValue = float.MinValue;
            float thStart = ImageAnalysis.TerrainHeightAt(startPosition);
            foreach (Vector3 endpoint in endpoints) {
                if (ImageAnalysis.PointInBounds(endpoint) == false){
                    //Debug.Log($"Point out of bounds!: {endpoint}");
                    continue;
                }
                //Debug.Log($"Vector: {startPosition} to {endpoint} -> {sampleSum}");
                preSortedEndpoints.Add(new Tuple<float, Vector3>(Vector3.Distance(endpoint, new Vector3(startPosition.x, thStart, startPosition.z)), endpoint));
            }
            
            return preSortedEndpoints.OrderByDescending(e => e.Item1).ToList();
        }

        /// <summary>
        /// Eva
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="direction"></param>
        /// <param name="distance"></param>
        /// <param name="maxAngle"></param>
        /// <param name="numBranches"></param>
        /// <returns></returns>
        public static List<Tuple<float, Vector3>> WeightedEndpointsByTerrainHeight(Vector3 startPosition, Vector3 direction, float distance, float maxAngle, int numBranches = 10, SegmentType type = SegmentType.Highway) {
            //Debug.Log($"Start: {startPosition}\tDirection: {direction}\tDistance: {distance}");
            List<Vector3> endpoints = BranchLocationsFromPosition(startPosition, direction, distance, maxAngle, numBranches);
            List<Tuple<float, Vector3>> preSortedEndpoints = new List<Tuple<float, Vector3>>();
            //Vector3 highestSumVector = Vector3.zero;
            //float maxValue = float.MinValue;
            float thStart = ImageAnalysis.TerrainHeightAt(startPosition);
            foreach (Vector3 endpoint in endpoints) {
                if (ImageAnalysis.PointInBounds(endpoint) == false){
                    //Debug.Log($"Point out of bounds!: {endpoint}");
                    continue;
                }
                /*
                //if diffTH > 1 we are going uphill
                //if 0 < diffTH < 1 we are going downhill
                float diffTH = Mathf.Abs(endpoint.y - thStart);
                float scale = 2;
                float maxGrade = .1f;
                //Debug.Log(diffTH);
                if (diffTH < maxGrade) continue; //scale = float.PositiveInfinity;
                */
                float slopeCost = SlopeCost(startPosition, endpoint);
                if (slopeCost == float.PositiveInfinity) continue;
                bool isOverWater = false;
                int maxSamples = 10;
                for (int i = 0; i <= maxSamples; i++) {
                    Vector3 samplePoint = startPosition + ((endpoint - startPosition)*i/maxSamples);
                    if (ImageAnalysis.PointOverWater(samplePoint)) {
                        isOverWater = true;
                        break;
                    }
                }
                if (isOverWater) continue;
                float weight = DistanceCost(startPosition, endpoint) + slopeCost;
                //Debug.Log($"Vector: {startPosition} to {endpoint} -> {sampleSum}");
                preSortedEndpoints.Add(new Tuple<float, Vector3>(weight, endpoint));
            }            
            if (numBranches < 10) {
                return preSortedEndpoints.OrderByDescending(e => e.Item1).ToList();
            }
            //return the best weighted half of the group
            return preSortedEndpoints.OrderByDescending(e => e.Item1).ToList();//.Skip(Math.Max(0, preSortedEndpoints.Count() - Mathf.RoundToInt(numBranches/2f))).ToList();
        }

        /// <summary>
        /// The same as the other WeightedEndpointsByTerrainHeight, except this one generates the list of path extensions via segment mask.
        /// Assumes the Y value for the startPosition is 0, it will also return the Y value of the endpoints as 0.
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static List<Tuple<float, Vector3Int>> WeightedEndpointsByTerrainHeight(Vector3Int startPosition, int distance, int resolution = 4) {
            List<Tuple<float, Vector3Int>> sortedEndpoints = new List<Tuple<float, Vector3Int>>();
            List<Vector3Int> endpoints = BranchLocationsBySegmentMask(startPosition, distance, 15);
            float yStart = ImageAnalysis.TerrainHeightAt(startPosition);
            Vector3 startPos = new Vector3(startPosition.x, yStart, startPosition.z);
            foreach (Vector3Int endpoint in endpoints) {
                if (ImageAnalysis.PointInBounds(endpoint) == false) continue;
                float yEndpoint = ImageAnalysis.TerrainHeightAt(endpoint);
                //Debug.DrawLine(new Vector3(startPosition.x, yStart, startPosition.z), new Vector3(endpoint.x, yEndpoint, endpoint.z), new Color(255, 165, 0));
                Vector3 e = endpoint + (Vector3.up * yEndpoint);
                float slopeCost = SlopeCost(startPos, e);
                if (slopeCost == float.PositiveInfinity) continue;
                bool isOverWater = false;
                int maxSamples = 10;
                for (int i = 0; i <= maxSamples; i++) {
                    Vector3 samplePoint = startPos + ((endpoint - startPos)*i/maxSamples);
                    if (ImageAnalysis.PointOverWater(samplePoint)) {
                        isOverWater = true;
                        break;
                    }
                }
                if (isOverWater) continue;
                float weight = DistanceCost(startPos, e) + slopeCost;
                //Debug.Log($"Vector: {startPosition} to {endpoint} -> {sampleSum}");
                sortedEndpoints.Add(new Tuple<float, Vector3Int>(weight, endpoint));
            }
            //Debug.Break();
            return sortedEndpoints;
        }

        /// <summary>
        /// Get a set of weighted tunnel endpoints at a world position. Tunnels are parametrized by its slope and depth of the tunnel under the terrain (to prevent tunnels being formed where they don't need to be.)
        /// </summary>
        /// <param name="startPosition">Start position</param>
        /// <param name="minRadius">The minimum allowed length of the tunnel.</param>
        /// <param name="maxRadius">The maximum allowed length of the tunnel.</param>
        /// <param name="numBranches">Samples taken</param>
        /// <returns>A list of weighted tunnel endpoints</returns>
        public static List<Tuple<float, Vector3>> WeightedTunnelEndpoints(Vector3 startPosition, float minRadius, float maxRadius, int numBranches = 50) {
            //Debug.Log($"Start: {startPosition}\tDirection: {direction}\tDistance: {distance}");
            List<Vector3> endpoints = BranchTunnelFromPosition(startPosition, minRadius, maxRadius, numBranches);
            List<Tuple<float, Vector3>> preSortedEndpoints = new List<Tuple<float, Vector3>>();
            //Vector3 highestSumVector = Vector3.zero;
            //float maxValue = float.MinValue;
            float thStart = ImageAnalysis.TerrainHeightAt(startPosition);
            foreach (Vector3 endpoint in endpoints) {
                if (ImageAnalysis.PointInBounds(endpoint) == false){
                    //Debug.Log($"Point out of bounds!: {endpoint}");
                    continue;
                }
                startPosition = new Vector3(startPosition.x, thStart, startPosition.z);
                float weight = TunnelCost(startPosition, endpoint, 15);
                if (weight == float.PositiveInfinity) continue;

                //Debug.Log($"Vector: {startPosition} to {endpoint} -> {sampleSum}");
                preSortedEndpoints.Add(new Tuple<float, Vector3>(weight, endpoint));
            }
            if (numBranches < 10) {
                return preSortedEndpoints.OrderByDescending(e => e.Item1).ToList();
            }
            //return the best weighted half of the group
            return preSortedEndpoints.OrderByDescending(e => e.Item1).Skip(Math.Max(0, preSortedEndpoints.Count() - Mathf.RoundToInt(numBranches/2f))).ToList();
        }

        public static List<Tuple<float, Vector3>> WeightedBridgeEndpoints(Vector3 startPosition, float minRadius, float maxRadius, int numBranches=20) {
            List<Tuple<float,Vector3>> preSortedEndpoints = new List<Tuple<float, Vector3>>();
            List<Vector3> endpoints = BranchBridgeFromPosition(startPosition, minRadius, maxRadius, numBranches);

            float thStart = ImageAnalysis.TerrainHeightAt(startPosition);
            foreach (Vector3 endpoint in endpoints) {
                if (ImageAnalysis.PointInBounds(endpoint) == false){
                    //Debug.Log($"Point out of bounds!: {endpoint}");
                    continue;
                }
                startPosition = new Vector3(startPosition.x, thStart, startPosition.z);
                float weight = BridgeCost(startPosition, endpoint, 15);
                if (weight == float.PositiveInfinity) continue;

                //Debug.Log($"Vector: {startPosition} to {endpoint} -> {sampleSum}");
                preSortedEndpoints.Add(new Tuple<float, Vector3>(weight, endpoint));
            }

            if (numBranches < 10) {
                return preSortedEndpoints.OrderByDescending(e => e.Item1).ToList();
            }
            //return the best weighted half of the group
            return preSortedEndpoints.OrderByDescending(e => e.Item1).Skip(Math.Max(0, preSortedEndpoints.Count() - Mathf.RoundToInt(numBranches/2f))).ToList();
        }

        private static List<Vector3> BranchBridgeFromPosition(Vector3 pivot, float minRadius, float maxRadius, int numBranches) {
            List<Vector3> list = new List<Vector3>();
                for (int i = 0; i < numBranches; i++) {
                    float r = UnityEngine.Random.Range(minRadius, maxRadius);
                    float t = UnityEngine.Random.Range(0, 2*Mathf.PI);
                    float x = r * Mathf.Cos(t) + pivot.x;
                    float z = r * Mathf.Sin(t) + pivot.z;
                    Vector3 point = new Vector3(x, ImageAnalysis.TerrainHeightAt(x, z), z);
                    if (ImageAnalysis.PointInBounds(point) == false) continue;
                    list.Add(point);
                }
                return list;    
        }

        /// <summary>
        /// How much does it cost to move from point A to point B based solely on distance travelled over terrain.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="maxSamples"></param>
        /// <returns></returns>
        public static float DistanceCost(Vector3 start, Vector3 end, int maxSamples = 1) {
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
        /// How much does it cost to move from start to end assuming this path is a tunnel.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="maxSamples"></param>
        /// <returns></returns>
        public static float TunnelCost(Vector3 start, Vector3 end, int maxSamples = 1, int minCost = 250) {
            //calculate the slope of the bridge
            float slopeCost = SlopeCost(start, end);
            if (slopeCost == float.PositiveInfinity) return slopeCost;
            //approximate the sum of the ground above the proposed tunnel segment
            //if groundSum is positive after the loop then the tunnel goes through more ground than it goes over
            //if groundSum is negative we go over more ground than we go through.
            //reject all groundSum < N where N is configurable.
            float groundSum = 0;
            for (int i = 1; i <= maxSamples; i++) {
                Vector3 samplePoint = start+((end-start)*i/maxSamples);
                float tunnelHeight = samplePoint.y;
                float terrainHeight = ImageAnalysis.TerrainHeightAt(samplePoint);
                groundSum += terrainHeight - tunnelHeight;
            }

            if (groundSum < minCost) return float.PositiveInfinity;

            //float diffTH = Mathf.Abs(end.y - start.y);
            //float maxDiff = 1;
            //if (diffTH > maxDiff) return float.PositiveInfinity;
            //float dc = DistanceCost(start, end, maxSamples);
            //reject roads that don't go through terrain somehow, read the paper?
            return Vector3.Distance(start,end)/Math.Max(0.0001f,groundSum);
        }

        /// <summary>
        /// Bridges are weighted by how much water they go over.
        /// If they go inside any land they are refused.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="maxSamples"></param>
        /// <returns></returns>
        public static float BridgeCost(Vector3 start, Vector3 end, int maxSamples = 1) {
            //approximate the sum of the height above the water
            float slopeCost = SlopeCost(start, end);
            if (slopeCost == float.PositiveInfinity) return slopeCost;
            float maxHeight = 30;
            float biggestDiff = 0;
            float bridgeHeightSum = 0;
            float landCost = 1000;
            for (int i = 0; i <= maxSamples; i++) {
                Vector3 samplePoint = start+((end-start)*i/maxSamples);
                if (ImageAnalysis.PointOverWater(samplePoint) == false) {
                    bridgeHeightSum += landCost;
                    continue;
                }
                float bridgeHeight = samplePoint.y;
                float terrainHeight = ImageAnalysis.TerrainHeightAt(samplePoint);
                float diff = bridgeHeight - terrainHeight;
                if (diff < 0) return float.PositiveInfinity;
                if (diff > maxHeight) return float.PositiveInfinity;
                if (diff > biggestDiff) biggestDiff = diff;
                bridgeHeightSum += 1+diff;
            }

            return bridgeHeightSum;
        }

        /// <summary>
        /// Calculate the gradient of the road, the "rise over run". The vertical distance covered divided by the flat ground distance covered. 
        /// Take that value and multiply it by 100 for a percentage grade. 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="maxGradePercentage">The maximum allowed grade of the roadway, expressed as a percentage. 100 * dy/dx.</param>
        /// <returns>The slope cost which is the grade divided by 100. The Delta y/delta x.</returns>
        public static float SlopeCost(Vector3 start, Vector3 end, float maxGradePercentage = 5f) {
            //calculate the slope of the bridge
            float flatGroundDist = Vector3.Distance(new Vector3(start.x, 0, start.z), new Vector3(end.x, 0, end.z));
            float heightDiff = ImageAnalysis.TerrainHeightAt(end) - ImageAnalysis.TerrainHeightAt(start); //+ means going uphill - means going downhill
            float grade = 100*Math.Abs(heightDiff/flatGroundDist);
            if (grade > maxGradePercentage) return float.PositiveInfinity;
            //Debug.Log($"GradePerc: {grade}");
            return grade/100;
        }
    }


    /// <summary>
    /// A constraint used to check if a segment ends over water. If so try to resolve the problem.
    /// If the segment is a highway, try to make a bridge using long branches to feel for population centers.
    /// If no bridge can be made, attempt to rotate or scale the segment to fit within bounds.
    /// </summary>
    public class OverWaterConstraint : LocalConstraint {
        public override bool ApplyConstraint(ref RoadSegment segment) {
            CitySettings settings = CitySettings.instance;
            if (ImageAnalysis.PointOverWater(segment.EndPosition)) {
                Debug.Log("Applying OverWaterConstraint", segment);
                if (segment.IsHighway) {
                    //search the radius for land
                    //we can create a branch up to a certain amount over water, as long as it hits a population center
                    //create possible branches across the water
                    //the direction we are currently going
                    //add all proposed endpoints to a list
                    List<Vector3> proposedSegments = GlobalGoals.BranchLocationsFromSegment(segment, settings.highwayMaxWaterCrossing);
                    List<Tuple<float, Vector3>> weightedSegments = GlobalGoals.BranchesWeightedByWaterCrossing(segment.StartPosition, proposedSegments);
                    //check a random proposed segment for signs of a population center
                    //DEBUG CHECK ALL PROPOSED SEGMENTS
                    /*foreach(Tuple<float, Vector3> weights in weightedSegments) {
                        //Debug.Log(weights.Item1);
                        float v = 50 + (100 * weights.Item1);
                        Debug.DrawLine(segment.StartPosition, weights.Item2, new Color(v, 70, 70));
                    }
                    Debug.Break();
                    return false;*/

                    //get top weighted segment
                    //try to assign as bridge point.
                    if (weightedSegments.Count > 0) {
                        Vector3 bestPlace = weightedSegments[0].Item2;
                        if (ImageAnalysis.PointInBounds(bestPlace)) {
                            segment.EndPosition = bestPlace;
                            segment.IsBridge = true;
                            return true;
                        }
                    }

                    //we couldn't find a spot to make the bridge
                    if (segment.IsBridge == false) {
                        Debug.Log("Couldn't build a bridge, attempting to rotate and scale to fit.", segment);
                        bool didRotate = LocalConstraint.RotateSegment(ref segment);
                        bool didPrune = LocalConstraint.PruneSegment(ref segment);
                        if (didRotate == false && didPrune == false) {
                            //we couldn't rotate or prune the road to fit
                            return false;
                        } else {
                            return true;
                        }
                    } else {
                        //the segment is a bridge, sanity check if we are in bounds. we shouldn't reach this part of the code
                        //since if we are a bridge, we returned true right after setting that value.
                        return ImageAnalysis.PointInBounds(segment.EndPosition);
                    }
                } else {
                    //just try to rotate and prune street segments
                    bool didRotate = LocalConstraint.RotateSegment(ref segment);
                    bool didPrune = LocalConstraint.PruneSegment(ref segment);
                    if (didRotate == false && didPrune == false) {
                        //we couldn't rotate or prune the road to fit
                        return false;
                    } else {
                        return true;
                    }
                }
            } else {
                //we don't have to do nothing, our point isn't over water
                return true;
            }
        }
    }

    /// <summary>
    /// Apply the near segment constraint to a reference segment. If there is a nearby segment end point we will attempt to connect to it.
    /// If we are overlapping a road segment at any point we will try to build a new crossing point
    /// </summary>
    public class NearSegmentConstraint : LocalConstraint {
        public override bool ApplyConstraint(ref RoadSegment segment) {
            float closeEnough = segment.IsHighway ? CitySettings.instance.highwayConnectionThreshold : CitySettings.instance.streetConnectionThreshold;
            List<RoadSegment> segs = segment.IsHighway ? CitySettings.instance.cigen.highwayAcceptedSegments : CitySettings.instance.cigen.streetAcceptedSegments;
            //search segments for nearby ends to connect to
            for (int i = segs.Count-1; i >= 0; i--) {
                //the segment we are looking at
                RoadSegment seg = segs[i];
                //don't consider neighbor segments
                if (seg.EndNeighbors.Contains(segment) || seg.StartNeighbors.Contains(segment)) continue;
                if (Vector3.Distance(segment.StartPosition, seg.StartPosition) <= closeEnough) {
                    //merge start to start
                    return segment.Merge(seg, true, true);
                }                    
                if (Vector3.Distance(segment.EndPosition, seg.StartPosition) <= closeEnough) {
                    //merge end to start
                    return segment.Merge(seg, false, true);
                }
                if (Vector3.Distance(segment.StartPosition, seg.EndPosition) <= closeEnough) {
                    //merge start to end
                    return segment.Merge(seg, true, false);
                }
                if (Vector3.Distance(segment.EndPosition, seg.EndPosition) <= closeEnough) {
                    //merge end to end
                    return segment.Merge(seg, false, false);
                }
                
                
                Vector3 intersectionPoint;
                //check if the segments intersect at any point
                //extend the search point by the min merge amount to check for close intersections as well
                if (Maths.Math.LineLineIntersection(out intersectionPoint, seg.StartPosition, seg.SegmentDirection, segment.StartPosition, segment.SegmentDirection + (segment.SegmentDirection.normalized * closeEnough))) {
                    //if the proposed intersection would be at a shallow angle deny it
                    Vector3 vec1 = segment.StartPosition - intersectionPoint;
                    Vector3 vec2 = seg.StartPosition - intersectionPoint;
                    Vector3 vec3 = seg.EndPosition - intersectionPoint;
                    float angle1 = Vector3.Angle(vec1, vec2);
                    float angle2 = Vector3.Angle(vec1, vec3);
                    float minAngle = segment.IsHighway ? CitySettings.instance.minAngleBetweenHighwayMergeSegments : CitySettings.instance.minAngleBetweenStreetMergeSegments;
                    if (angle1 < minAngle || angle2 < minAngle) return false;
                    
                    //create a new intersection on the sampled segment position, at intersectionPoint.
                    segment.EndPosition = intersectionPoint;
                    //also flag the segment to stop growing
                    segment.StopGrowing = true;
                    //create a new segment from intersection going to old seg startposition and one going to old seg endposition
                    RoadSegment segToEnd = new GameObject().AddComponent<RoadSegment>();
                    segToEnd.Init(intersectionPoint, seg.EndPosition, segment);
                    RoadSegment segToStart = new GameObject().AddComponent<RoadSegment>();
                    segToStart.Init(intersectionPoint, seg.StartPosition, segment);
                    //create two new segments that start at the intersection point and go out towards the old intersection points
                    //first remove the crossed segment from its neighbors and add the new segment ass branches with appropriate neighbors
                    foreach (RoadSegment neighbor in seg.StartNeighbors) {
                        //we removed the intersected segment from its 
                        if (neighbor.StartNeighbors.Remove(seg)) {
                            //put neighbor segToStart endneighbors
                            segToStart.EndNeighbors.Add(neighbor);
                            neighbor.StartNeighbors.Add(segToStart);
                        }
                        if (neighbor.EndNeighbors.Remove(seg)) {
                            segToStart.EndNeighbors.Add(neighbor);
                            neighbor.EndNeighbors.Add(segToStart);
                        }
                    }
                    foreach (RoadSegment neighbor in seg.EndNeighbors) {
                        if (neighbor.StartNeighbors.Remove(seg)) {
                            segToEnd.EndNeighbors.Add(neighbor);
                            neighbor.StartNeighbors.Add(segToEnd);
                        }
                        if (neighbor.EndNeighbors.Remove(seg)) {
                            segToEnd.EndNeighbors.Add(neighbor);
                            neighbor.EndNeighbors.Add(segToEnd);
                        }
                    }
                    //Add created segments as start neighbors
                    segToEnd.StartNeighbors.Add(segToStart);
                    segToStart.StartNeighbors.Add(segToEnd);
                    
                    //finally delete the old segment from the accepted segment list and delete it from the game
                    if (seg.IsHighway) {
                        CitySettings.instance.cigen.highwayAcceptedSegments.RemoveAt(i);
                    } else {
                        CitySettings.instance.cigen.streetAcceptedSegments.RemoveAt(i);
                    }
                    GameObject.Destroy(seg.gameObject);

                    Camera.main.transform.position = intersectionPoint + (Vector3.one * 20);
                    Camera.main.transform.LookAt(intersectionPoint);
                    Debug.Break();
                    break;
                    
                }
            }

            return true;
        }
    }

    public class OutOfBoundsConstraint : LocalConstraint {
        public override bool ApplyConstraint(ref RoadSegment segment)
        {
            if (ImageAnalysis.PointInBounds(segment.StartPosition) && ImageAnalysis.PointInBounds(segment.EndPosition)) return true;
            if (LocalConstraint.RotateSegment(ref segment)) return true;
            if (LocalConstraint.PruneSegment(ref segment)) return true;
            return false;
        }
    }
}