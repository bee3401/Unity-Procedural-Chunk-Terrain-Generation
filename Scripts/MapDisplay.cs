using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Terrain terrain;

    public void DrawTexture(Texture2D texture, Vector2 dimensions)
    {
        //  Draws texture (noiseMap or colorMap) on a FLAT Terrain object in hierarchy.
        TerrainLayer terrainLayer = GenerateTerrainLayer(texture);
        TerrainData terrainData = TerrainGenerator.GenerateFlatTerrain(dimensions);
        
        terrainData.terrainLayers = new TerrainLayer[] { terrainLayer };
        terrain.terrainData = terrainData;

    }

    public void DrawTerrain(Texture2D texture, float[,] noiseMap, float depth, AnimationCurve heightCurve)
    {
        // Draws texture (colorMap) on a RELIEF Terrain object in hierarchy.
        TerrainLayer terrainLayer = GenerateTerrainLayer(texture);
        TerrainData terrainData = TerrainGenerator.GenerateTerrain(noiseMap, depth, heightCurve);
        
        terrainData.terrainLayers = new TerrainLayer[] {terrainLayer};
        terrain.terrainData = terrainData;
    }

    public TerrainLayer GenerateTerrainLayer(Texture2D texture)
    {
        // Create a new terrain layer
        TerrainLayer terrainLayer = new TerrainLayer();
        terrainLayer.diffuseTexture = texture;

        // Calculate tiling settings based on the terrain size and texture size
        float tileX = texture.width;
        float tileZ = texture.height;
        terrainLayer.tileSize = new Vector2(tileX, tileZ);
        
        return terrainLayer;
    }
}
