using OpenCvSharp;
using UnityEngine;
using Cigen.ImageAnalyzing;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace Cigen.Conversions {

    public static class Conversion {

        public static Dictionary<Texture2D, Mat> ConvertTexturesToMats(List<Texture2D> textures, bool convertBW = true) {            
            Dictionary<Texture2D, Mat> mats = new Dictionary<Texture2D, Mat>();
            foreach(Texture2D texture in textures) {
                Mat image = OpenCvSharp.Unity.TextureToMat(texture);
                Mat newMat = new Mat();
                if (convertBW) {
                    //ensures our images are grayscale and uniform.
                    Cv2.CvtColor(image, newMat, ColorConversionCodes.BGR2GRAY);
                } else {
                    Cv2.CvtColor(image, newMat, ColorConversionCodes.BGR2RGB);
                }
                mats.Add(texture, newMat);

            }

            return mats;
        }
        /// <summary>
        /// Convert an OpenCvSharp Point to a UnityEngine Vector3. Note: the Y value is set to 0.
        /// </summary>
        /// <param name="point">And OpenCvSharp Point object.</param>
        /// <returns>A UnityEngine Vector3 object.</returns>
        public static Vector3 PointToVector3(Point point) {
            return new Vector3(point.X, 0, point.Y);
        }

        /// <summary>
        /// Convert an OpenCvSharp Point to a spot in world space.
        /// </summary>
        /// <param name="texturePoint">The OpenCvSharp Point.</param>
        /// <param name="settings">The city settings.</param>
        /// <returns>A Vector3 pointing to a spot in world space.</returns>
        public static Vector3 TextureToWorldSpace(Point texturePoint, AnisotropicLeastCostPathSettings settings) {
            return TextureToWorldSpace(PointToVector3(texturePoint), settings);
        }

        /// <summary>
        /// Convert a point in the generator textures to a point in the world.
        /// </summary>
        /// <param name="texturePoint">The Vector3 pointing to a spot on the texture. The Y value is ignored and read from the heightmap here.</param>
        /// <param name="settings">The city settings.</param>
        /// <returns>A Vector3 pointing to a spot in world space.</returns>
        public static Vector3 TextureToWorldSpace(Vector3 texturePoint, AnisotropicLeastCostPathSettings settings) {
            Vector3 pos = TextureToWorldSpaceNoY(texturePoint, settings);
            //read pixel value at Point(x,z) in the heightmap texture.
            float y = 0;
            if (settings.roadsFollowTerrain) {
                y = settings.terrainMaxHeight * ImageAnalysis.TerrainHeightAt(pos.x, pos.z, settings);
            }

            return new Vector3(pos.x, y, pos.z);
        }

        /// <summary>
        /// Convert a point in the generator textures to a point in the world, ignoring the Y value, subsequently skipping a read call to the heightmap texture.
        /// </summary>
        /// <param name="texturePoint">The Vector3 pointing to a spot on the texture.</param>
        /// <param name="settings">The city settings.</param>
        /// <returns>A Vector3 pointing to a spot in world space.</returns>
        public static Vector3 TextureToWorldSpaceNoY(Vector3 texturePoint, AnisotropicLeastCostPathSettings settings) {
            float x = texturePoint.x * settings.textureToWorldSpace.x;
            float z = texturePoint.z * settings.textureToWorldSpace.z;
            return new Vector3(x, 0, z);
        }

        /// <summary>
        /// Converts a world position into a texture space position where Vector.x => Point.x etc.
        /// </summary>
        /// <param name="x">The world position x value.</param>
        /// <param name="z">The world position z value.</param>
        /// <returns></returns>
        public static Point WorldToTextureSpace(float x , float z, AnisotropicLeastCostPathSettings settings) {
            //round to nearest integer of the scale
            int newx = (int)Math.Round(x / settings.textureToWorldSpace.x);
            int newz = (int)Math.Round(z / settings.textureToWorldSpace.z);
            //z needs to be mirrored, use texture size in z axis
            return new Point(settings.terrainHeightMap.height - 1 - newz, newx);
        }

        public static (int, int) WorldToTextureSpace2(float x, float z, AnisotropicLeastCostPathSettings settings) {
            int newx = (int)Math.Round(x / settings.textureToWorldSpace.x);
            int newz = (int)Math.Round(z / settings.textureToWorldSpace.z);
            return (newx, newz);
        }

        public static Texture2D MatToTexture(Mat sourceMat) {
            //Get the height and width of the Mat 
            int imgHeight = sourceMat.Height;
            int imgWidth = sourceMat.Width;

            byte[] matData = new byte[imgHeight * imgWidth];

            //Get the byte array and store in matData
            sourceMat.GetArray(0, 0, matData);
            //Create the Color array that will hold the pixels 
            Color32[] c = new Color32[imgHeight * imgWidth];

            //Get the pixel data from parallel loop
            Parallel.For(0, imgHeight, i => {
                for (var j = 0; j < imgWidth; j++) {
                    byte vec = matData[j + i * imgWidth];
                    var color32 = new Color32 {
                        r = vec,
                        g = vec,
                        b = vec,
                        a = 0
                    };
                    c[j + i * imgWidth] = color32;
                }
            });

            //Create Texture from the result
            Texture2D tex = new Texture2D(imgWidth, imgHeight, TextureFormat.RGBA32, true, true);
            tex.SetPixels32(c);
            tex.Apply();
            return tex;
        }
    }
}