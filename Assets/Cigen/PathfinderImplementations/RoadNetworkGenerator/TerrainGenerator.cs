using System.Collections;
using GeneralPathfinder;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {
    public GameObject BuildTerrain(Texture2D heightmap, Texture2D displayTexture, float terrainMaxHeight) {
        TerrainData td = new TerrainData();
        td.heightmapResolution = heightmap.width;
        td.size = new Vector3((int)heightmap.width, terrainMaxHeight, (int)heightmap.height);
        float[,] heights = new float[heightmap.width, heightmap.height];
        for (int i = 0; i < heightmap.width; i++) {
            for (int j = 0; j < heightmap.height; j++) {
                heights[j,i] = heightmap.GetPixel(i,j).grayscale;
            }
        }
        td.SetHeights(0, 0, heights);

        TerrainLayer tl = new TerrainLayer();
        tl.diffuseTexture = displayTexture;

        td.terrainLayers = new TerrainLayer[]{tl};


        return Terrain.CreateTerrainGameObject(td);
    }

}