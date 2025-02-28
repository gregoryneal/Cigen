using OpenCvSharp;
using UnityEngine;
using Cigen.ImageAnalyzing;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace Cigen.Conversions {

    public static class Conversion {

        /// <summary>
        /// Converts all of our lookup textures into a consistently formatted OpenCvSharp Mat. 
        /// Try not to use textures at all, sent Mat objects between function to prevent conversion every time. 
        /// </summary>
        /// <param name="textures">The textures we want to convert.</param>
        /// <returns>A dictionary that maps input textures to their generated OpenCvSharp materials.</returns>
        public static Dictionary<Texture2D, Mat> ConvertTexturesToBWMats(List<Texture2D> textures) {
            Dictionary<Texture2D, Mat> mats = new Dictionary<Texture2D, Mat>();
            foreach(Texture2D texture in textures) {
                Mat image = OpenCvSharp.Unity.TextureToMat(texture);
                //ensures our images are grayscale and uniform.
                Mat gray = new Mat();
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
                mats.Add(texture, gray);
            }

            return mats;
        }

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
        public static Vector3 TextureToWorldSpace(Point texturePoint) {
            return TextureToWorldSpace(PointToVector3(texturePoint));
        }

        /// <summary>
        /// Convert a point in the generator textures to a point in the world.
        /// </summary>
        /// <param name="texturePoint">The Vector3 pointing to a spot on the texture. The Y value is ignored and read from the heightmap here.</param>
        /// <param name="settings">The city settings.</param>
        /// <returns>A Vector3 pointing to a spot in world space.</returns>
        public static Vector3 TextureToWorldSpace(Vector3 texturePoint) {
            Vector3 pos = TextureToWorldSpaceNoY(texturePoint);
            //read pixel value at Point(x,z) in the heightmap texture.
            float y = 0;
            if (CitySettings.instance.roadsFollowTerrain) {
                y = CitySettings.instance.terrainMaxHeight * ImageAnalysis.TerrainHeightAt(pos.x, pos.z);
            }

            return new Vector3(pos.x, y, pos.z);
        }

        /// <summary>
        /// Convert a point in the generator textures to a point in the world, ignoring the Y value, subsequently skipping a read call to the heightmap texture.
        /// </summary>
        /// <param name="texturePoint">The Vector3 pointing to a spot on the texture.</param>
        /// <param name="settings">The city settings.</param>
        /// <returns>A Vector3 pointing to a spot in world space.</returns>
        public static Vector3 TextureToWorldSpaceNoY(Vector3 texturePoint) {
            float x = texturePoint.x * CitySettings.instance.textureToWorldSpace.x;
            float z = texturePoint.z * CitySettings.instance.textureToWorldSpace.z;
            return new Vector3(x, 0, z);
        }

        /// <summary>
        /// Converts a world position into a texture space position where Vector.x => Point.x etc.
        /// </summary>
        /// <param name="x">The world position x value.</param>
        /// <param name="z">The world position z value.</param>
        /// <returns></returns>
        public static Point WorldToTextureSpace(float x , float z) {
            //round to nearest integer of the scale
            int newx = (int)Math.Round(x / CitySettings.instance.textureToWorldSpace.x);
            int newz = (int)Math.Round(z / CitySettings.instance.textureToWorldSpace.z);
            //z needs to be mirrored, use texture size in z axis
            return new Point(CitySettings.instance.terrainHeightMap.height - 1 - newz, newx);
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