using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public static TerrainData GenerateFlatTerrain(Vector2 dimensions) {
        TerrainData terrainData = new TerrainData();
        terrainData.size = new Vector3(dimensions.x, 1, dimensions.y);
        
        return terrainData;
    }

    public static TerrainData GenerateTerrain(float[,] heightMap, float depth, AnimationCurve heightCurve) 
    {
        //I don't think it's necessary in my case, but it eases parallelization
        AnimationCurve threadHeightCurve = new AnimationCurve(heightCurve.keys);

        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1); 
        TerrainData terrainData = new TerrainData();
        terrainData.size = new Vector3(width/8, depth, height/8);
        float[,] heights = new float[height, width];
        for (int y = 0; y < height; y++)
        {
            for(int x = 0; x < width; x++)
            {
                  
                heights[x, y] = threadHeightCurve.Evaluate(heightMap[x, y]);                           
            }
        }
        int resolution = Mathf.NextPowerOfTwo(Mathf.Max(width, height)) + 1;
        terrainData.heightmapResolution = resolution;
        terrainData.SetHeights(0, 0, heights);
        
        return terrainData;
    }
}
