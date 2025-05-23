using OpenCvSharp;
using UnityEngine;
using Cigen.Structs;
using Cigen.Conversions;
using System.Collections.Generic;
using System;

namespace Cigen.ImageAnalyzing {

    public static class ImageAnalysis {
        /// <summary>
        /// Given a population density map, this will return a list of population centers in world space
        /// </summary>
        /// <param name="inputData">The population density map.</param>
        /// <returns>An array of PopulationCenter objects.</returns>
        public static PopulationCenter[] FindPopulationCenters(AnisotropicLeastCostPathSettings settings) {
                List<PopulationCenter> populationCenters = new List<PopulationCenter>();

                Mat data = settings.populationDensityMapMat;
                //Mat highwayData = GameObject.FindFirstObjectByType<CityGenerator>().CVMaterials[CitySettings.instance.highwayMap];

                //apply a threshold to this image, if pixel(x,y) > 127 then it is set to 255, otherwise it is set to 0
                //in opencv the objects we search for should be white and everything else should be black.
                Mat thresh = new Mat ();
                Cv2.Threshold(data, thresh, 100, 255, ThresholdTypes.Binary);

                // find the contours
                Cv2.FindContours (thresh, out Point[][] contours, out HierarchyIndex[] _, RetrievalModes.Tree, ContourApproximationModes.ApproxNone, null);

                Debug.Log($"Found {contours.Length} population centers!");

                foreach (Point[] contour in contours) {
                    List<Point> boundingPoints = new List<Point>();
                    List<Vector3> worldBoundingPoints = new List<Vector3>();
                    //arc length of the contour, true indicates it is a closed loop (count the distance between the last two vertices)                    
                    double length = Cv2.ArcLength(contour, true);
                    //this uses the Ramer Douglas Peucker algorithm to approximate the contour with fewer points. 
                    //https://en.wikipedia.org/wiki/Ramer%E2%80%93Douglas%E2%80%93Peucker_algorithm
                    Point[] approx = Cv2.ApproxPolyDP(contour, length * 0.01, true);
                    //Point[] approx = contour;
                    foreach(Point p in approx) {
                        boundingPoints.Add(p);
                        worldBoundingPoints.Add(Conversion.TextureToWorldSpace(p, settings));
                    }

                    //find the center of each contour
                    Moments m = Cv2.Moments(contour);
                    int centerX = (int)(m.M10 / m.M00);
					int centerY = (int)(m.M01 / m.M00);

                    PopulationCenter pc = new PopulationCenter();

                    //add world position
                    pc.pixelPosition = new OpenCvSharp.Point(centerX, data.Width - 1 - centerY);
                    pc.worldPosition = Conversion.TextureToWorldSpace(pc.pixelPosition, settings);
                    pc.worldBoundingPoints = worldBoundingPoints.ToArray();
                    pc.pixelBoundingPoints = boundingPoints.ToArray();
                    
                    //get the bounding rectangle of the contour, for size information
					OpenCvSharp.Rect rect = Cv2.BoundingRect(contour);
                    pc.size = new Vector3(rect.Size.Width, 0, rect.Size.Height);
                    pc.density = (float)AvgOfBoundingRect(data, rect).Val0/255;

                    //find the dominant highway type
                    //pc.highwayType = //Highway.FindDominantHighwayTypeInRect(highwayData, rect);
                    //Debug.Log($"[{avgs.Val0}, {avgs.Val1}, {avgs.Val2}, {avgs.Val3}]");
                    //initialize a list of population centers that are connected to this one via highways
                    pc.connectedPCs = new List<PopulationCenter>();

                    populationCenters.Add(pc);                   
                }

                //draw all the found contours to the original image
                //first convert grayscale back to color just for display purposes
                Mat colorMat = new Mat();             
                Cv2.CvtColor(data, colorMat, ColorConversionCodes.GRAY2BGR);
                Scalar color = new Scalar(0, 0, 255); //bgr
                Cv2.DrawContours(colorMat, contours, -1, color, 5);
                //create a gameobject and apply the texture to it
                Texture2D texture = OpenCvSharp.Unity.MatToTexture(colorMat);
                settings.cigen.SetContourTexture(texture);
                /*GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                go.transform.position = new Vector3(0, 0, 0);
                go.GetComponent<Renderer>().material.mainTexture = texture;*/

                return populationCenters.ToArray();
        }

        public static float TerrainHeightAt(float x, float z, AnisotropicLeastCostPathSettings settings) {
            if (PointInBounds(new Vector3(x,0,z), settings) == false) return float.PositiveInfinity;
            if (settings.roadsFollowTerrain) {
                //Point texturePoint = Conversion.WorldToTextureSpace(x, z, settings);
                //Debug.Log($"worldToTexture OUTPUT XZ: {(Mathf.Round(x * 100)) / 100.0} -> {texturePoint.X}, { (Mathf.Round(z * 100)) / 100.0} -> {texturePoint.Y}");
                //return (float)NormalizedPointOnMat(texturePoint, settings.terrainHeightMapMat, settings) * settings.terrainMaxHeight;
                return NormalizedPointOnTextureInWorldSpace(x, z, settings.terrainHeightMap, settings) * settings.terrainMaxHeight;
            }
            return 0;
        }

        public static float TerrainHeightAt(Vector3 worldPosition, AnisotropicLeastCostPathSettings settings) {
            return TerrainHeightAt(worldPosition.x, worldPosition.z, settings);
        }



        /// <summary>
        /// Given an x, y position in world space, find the population density at that point.
        /// </summary>
        /// <param name="x">The X value in world space.</param>
        /// <param name="y">The Y value in world space.</param>
        /// <returns>The population density, a value between 0 and 1.</returns>
        public static float PopulationDensityAt(float x, float z, AnisotropicLeastCostPathSettings settings) {
            Point texturePoint = Conversion.WorldToTextureSpace(x, z, settings);
            if (PointInBounds(new Vector3(x, 0, z),settings)) {
                //read the texture at that point
                return (float)NormalizedPointOnMat(texturePoint.X, texturePoint.Y, settings.populationDensityMapMat, settings);
            } else {
                return 0;
            }
        }

        public static float PopulationDensityAt(Vector3 worldPosition, AnisotropicLeastCostPathSettings settings) {
            return PopulationDensityAt(worldPosition.x, worldPosition.z, settings);
        }

        /// <summary>
        /// Check if a given world position is in any population center defined by the population center cutoff amount in the settings.
        /// </summary>
        /// <param name="x">The world position x value.</param>
        /// <param name="z">The world position z value.</param>
        /// <returns>Whether or not the given position is in a population center.</returns>
        /*public static bool PointInAPopulationCenter(Vector3 worldPosition) {
            float pd = ImageAnalysis.PopulationDensityAt(worldPosition.x, worldPosition.z);
            return pd >= CitySettings.instance.populationDensityCutoff;
        }*/

        public static bool PointInPopulationCenter(Vector3 worldPosition, PopulationCenter pc) {
            return Maths.IsPointInRectangle(worldPosition, pc.worldPosition, pc.size);
        }

        /// <summary>
        /// Return the closest population center
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <param name="isInside">Require the method to only return the closest population center that it is inside of.</param>
        /// <returns></returns>
        public static PopulationCenter ClosestPopulationCenter(Vector3 worldPosition, AnisotropicLeastCostPathSettings settings, bool isInside = false) {
            PopulationCenter closest = settings.city.PopulationCenters[0];
            float minDistance = float.MaxValue;
            foreach (PopulationCenter pc in settings.city.PopulationCenters) {
                if (isInside && PointInPopulationCenter(worldPosition, pc) == false) continue;
                float d = Vector3.Distance(worldPosition, pc.worldPosition);
                if (d < minDistance) {
                    minDistance = d;
                    closest = pc;
                }
            }
            return closest;
        }

        /// <summary>
        /// Checks if all provided textures are a power of two, optionally check if they are the same size as each other.
        /// Note this will always pass if the image import settings have Non-power of two setting set to anything but None.
        /// </summary>
        /// <param name="textures">A list of textures to check against each other.</param>
        /// <returns>True if all textures are a power of two and optionally the same size. False if any condition fails.</returns>
        public static bool CheckAllTexturesArePowerOfTwo(List<Texture2D> textures, bool requireSameSize = true) {
            int size = -1;

            //Debug.Log($"Checking {textures.Count} textures!");

            foreach(Texture2D texture in textures) {
                if (size == -1) {
                    size = texture.height;
                }
                
                if (requireSameSize) {
                    if (texture.height != size || texture.width != size) {
                        return false;
                    }
                }

                if (Cigen.Maths.IsPowerOfTwo(texture.height) == false || Cigen.Maths.IsPowerOfTwo(texture.width) == false) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns a normalized value of the given Mat object at the given point.
        /// </summary>
        /// <param name="row">The row number of the pixel grid.</param>
        /// <param name="col">The column number of the pixel grid.</param>
        /// <param name="texture">The texture to read the value of.</param>
        /// <returns>A value between 0 and 1, 0 being black and 1 being white.</returns>
        public static float NormalizedPointOnMat(int row, int col, Mat texture, AnisotropicLeastCostPathSettings settings) {
            try {
                return (float)texture.At<ushort>(row, col)/ushort.MaxValue;
            } catch (NullReferenceException) {
                //Debug.Log("ERROR");
                string texName = "Unknown texture!";
                foreach (KeyValuePair<Texture2D, Mat> kvp in settings.cigen.CVMaterials) {
                    if (kvp.Value == texture) texName = kvp.Key.name;
                }
                Debug.LogError($"ERROR ON MAT {texName} at ROW {row} and COL {col}");
                Debug.Break();
                return 0;
            }
        }

        public static float NormalizedPointOnMat(Point point, Mat texture, AnisotropicLeastCostPathSettings settings) {
            return NormalizedPointOnMat(point.X, point.Y, texture, settings);
            //return (float)texture.At<ushort>(point.X, point.Y)/ushort.MaxValue;
        }


        public static float NormalizedPointOnTextureInWorldSpace2(float x, float z, Mat mat, AnisotropicLeastCostPathSettings settings) {
            Point texturePoint = Conversion.WorldToTextureSpace(x, z, settings);
            return NormalizedPointOnMat(texturePoint, mat, settings);
        }

        public static float NormalizedPointOnTextureInWorldSpace(float x, float z, Texture2D texture, AnisotropicLeastCostPathSettings settings) {
            (int, int) point = Conversion.WorldToTextureSpace2(x, z, settings);
            return NormalizedPointOnTexture(point.Item1, point.Item2, texture);

        }

        public static float NormalizedPointOnTexture(int row, int col, Texture2D texture) {
            return texture.GetPixel(row, col).grayscale;
        }

        /// <summary>
        /// Compute the average pixel value of a region of interest of a Mat.
        /// </summary>
        /// <param name="mat">The Mat to sum</param>
        /// <param name="rowMin">The starting row number for the region of interest.</param>
        /// <param name="colMin">The starting column number for the region of interest.</param>
        /// <param name="rowMax">The maximum row number for the region of interest.</param>
        /// <param name="colMax">The maximum column number for the region of interest.</param>
        /// <returns>The average pixel value in the region, in Scalar form.</returns>
        public static Scalar AvgOfBoundingRect(Mat mat, int rowMin, int colMin, int rowMax, int colMax) {
            return AvgOfBoundingRect(mat, new OpenCvSharp.Rect(colMin, rowMax, colMax-colMin, rowMax-rowMin));
        }

        /// <summary>
        /// Compute the average pixel value of a region of interest on a Mat.
        /// </summary>
        /// <param name="mat">The Mat in question.</param>
        /// <param name="boundingRect">The bounding rect to average the pixels inside of.</param>
        /// <returns>An OpenCvSharp Scalar of pixel values between 0 and 255. Val0 is the value of the R channel, Val1 is the value of the G channel, Val2 is the value of the B channel, and Val3 is the value of the A channel.</returns>
        public static Scalar AvgOfBoundingRect(Mat mat, OpenCvSharp.Rect boundingRect) {
            Mat croppedImage = mat.Clone(boundingRect);
            return Cv2.Mean(croppedImage);
        }

        public static bool RandomPointWithinPopulationCenter(out Vector3 randomPosition, PopulationCenter pc, AnisotropicLeastCostPathSettings settings) {
            Vector3 point = Maths.RandomPointInRectangle(pc.worldPosition, pc.size);
            int i = 0;
            int maxTries = 1000;
            while (PointInBounds(point, settings) == false) {
                if (i > maxTries) {
                    randomPosition = Vector3.zero;
                    return false;
                }
                point = Maths.RandomPointInRectangle(pc.worldPosition, pc.size);
                i++;
            }
            randomPosition = new Vector3(point.x, TerrainHeightAt(point.x, point.z, settings), point.z);
            return true;
        }

        /// <summary>
        /// Analyze all of the maps to find a single random point within bounds, ostensibly for road generation.
        /// </summary>
        /// <param name="withinPopualtionCenter">Should the point also be within a population center?</param>
        /// <param name="randomPosition">The out vector3 to store the point in.</param>
        /// <returns>A random point within bounds, given in world coordinates.</returns>
        public static bool RandomPointWithinBounds(out Vector3 randomPosition, AnisotropicLeastCostPathSettings settings, bool withinPopulationCenter = false) {
            //analyze water, and nature maps (and optionally population density maps) to find a single location within bounds
            if (withinPopulationCenter) {
                PopulationCenter[] pcs = FindPopulationCenters(settings);
                PopulationCenter pc = pcs[UnityEngine.Random.Range(0, pcs.Length)];
                if (RandomPointWithinPopulationCenter(out randomPosition, pc, settings)) {
                    return true;
                }
                randomPosition = Vector3.zero;
                return false;
            } else {
                //just pick a random point on the map and sample it
                int i = 0; 
                int maxTries = 10000;
                Vector3 worldPos = new Vector3(UnityEngine.Random.Range(0, settings.populationDensityMap.width), 0, UnityEngine.Random.Range(0, settings.populationDensityMap.height));
                while (PointInBounds(worldPos, settings) == false) {
                    if (i >= maxTries) {
                        randomPosition = Vector3.zero;
                        return false;
                    }
                    worldPos = new Vector3(UnityEngine.Random.Range(0, settings.populationDensityMap.width), 0, UnityEngine.Random.Range(0, settings.populationDensityMap.height));
                    i++;
                }
                randomPosition = new Vector3(worldPos.x, TerrainHeightAt(worldPos.x, worldPos.z, settings), worldPos.z);
                return true;                
            }
        }
        /*
        /// <summary>
        /// This function will filter a point through
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Vector3 PointOn(CitySettings settings, Vector3 point) {

        }*/

        /// <summary>
        /// Look for a point at a distance away from another point on the terrain.
        /// </summary>
        /// <param name="settings">City settings</param>
        /// <param name="point">Point to search from</param>
        /// <param name="distance">Distance to search from point</param>
        /// <returns></returns>
        public static Vector3 PointNear(Vector3 point, float distance, AnisotropicLeastCostPathSettings settings) {
            //get a random point on the circle of radius distance around our point.
            Vector2 uc = new Vector2(point.x, point.z) + (UnityEngine.Random.insideUnitCircle.normalized * distance);
            //convert it to a vector3 with terrain height on the y axis.
            Vector3 rdmPos = new Vector3(uc.x, TerrainHeightAt(uc.x, uc.y,settings), uc.y);
            return rdmPos;
        }

        /// <summary>
        /// Check if we are in the bounds of our world/texture space.
        /// </summary>
        /// <param name="worldPoint"></param>
        /// <returns></returns>
        public static bool PointInBounds(Vector3 worldPoint, AnisotropicLeastCostPathSettings settings) {
            OpenCvSharp.Point texCoords = Conversion.WorldToTextureSpace(worldPoint.x, worldPoint.z, settings);
            int width = settings.terrainHeightMap.width;
            if (texCoords.X < 0 || texCoords.X > width || texCoords.Y < 0 || texCoords.Y > width) {
                //Debug.Log($"OUTOF BOUNDS COORDS: world: {worldPoint.x}, {worldPoint.z} -> texture: {texCoords.X}, {texCoords.Y}" );
                //Debug.Break();
                return false;
            }
            //if (PointOverWater(worldPoint)) return false;
            //if (CitySettings.instance.natureMapPreventsRoadGeneration && PointOverNature(worldPoint)) return false;
            return true;
        }

        public static bool PointOverWater(Vector3 worldPoint, AnisotropicLeastCostPathSettings settings) {
            if (PointInBounds(worldPoint, settings)) {
                float epsilon = .05f;
                float val = (float)NormalizedPointOnTextureInWorldSpace2(worldPoint.x, worldPoint.z, settings.waterMapMat, settings);
                //water map is a hard line so we check if the value is close enough to 1
                if (1 - val < epsilon) {
                    //we are in water
                    return true;
                }
            }
            return false;
        }
        /*
        public static bool PointOverNature(Vector3 worldPoint) {
            float val = NormalizedPointOnTextureInWorldSpace(worldPoint.x, worldPoint.z, CitySettings.instance.natureMapMat);
            //nature map has a fuzzy boundary so we check if val is greater than the nature map boundary
            if (val > CitySettings.instance.natureMapCutoff) return true;
            return false;
        }*/

        /// <summary>
        /// Sample the terrain at discrete intervals and if any point is below the terrain height, return true. Return false otherwise.
        /// </summary>
        /// <param name="start">The start position in world space.</param>
        /// <param name="end">The end position in world space.</param>
        /// <returns>Is the line connecting the two vectors in world space clipping the terrain?</returns>
        public static bool IsClippingTerrain(Vector3 start, Vector3 end, AnisotropicLeastCostPathSettings settings) {
            float forgiveness = 1f; //how forgiving are we? ideally this might be the thickness of the road mesh or something close to but less than it.
            int numSamples = 10;
            for (int i = 0; i <= numSamples; i++) {
                Vector3 testPosition = start + ((end - start)*(i/numSamples));
                float terrainHeight = TerrainHeightAt(testPosition.x, testPosition.z, settings);
                if (testPosition.y < terrainHeight && Mathf.Abs(terrainHeight-testPosition.y) > forgiveness) {
                    Debug.Log($"Clipping terrain at {testPosition} with y value: {terrainHeight}");
                    //Debug.Break();
                    return true;
                }
            }
            return false;
        }
    }
}