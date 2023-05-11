using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode {NoiseMap, ColorMap, Terrain}
    public DrawMode drawMode;
    public Noise.NormalizeMode normalizeMode;
    public const int chunkSize = 255;
    public int mapWidth;
    public int mapHeight;
    public int depth;
    public int seed;
    public float noiseScale;
    public int octaves;
    [Range (0,1)]
    public float persistence;
    public float lacunarity;
    public Vector2 offset;
    public AnimationCurve curve;
    public bool autoUpdate;
    public TerrainType[] terrainTypes;

    Queue < MapThreadInfo<MapData> > mapThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue < MapThreadInfo<TerrainData> > terrainThreadInfoQueue = new Queue<MapThreadInfo<TerrainData>>();
    
    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);
        Vector2 dimensions = new Vector2(mapWidth, mapHeight);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap), dimensions);
        }
        else if (drawMode == DrawMode.ColorMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, dimensions), dimensions);
        }
        else if (drawMode == DrawMode.Terrain){
            display.DrawTerrain(TextureGenerator.TextureFromColorMap(mapData.colorMap, dimensions), mapData.heightMap, depth, curve);
        }
    }
    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(center, callback);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);
        lock (mapThreadInfoQueue) {
            mapThreadInfoQueue.Enqueue(new MapThreadInfo<MapData> (callback, mapData));
        }
    }

    public void RequestTerrainData(MapData mapData, Action<TerrainData> callback)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            TerrainData terrainData = TerrainGenerator.GenerateTerrain(mapData.heightMap, depth, curve);
            callback(terrainData);
        });
    }

    void TerrainDataThread(MapData mapData, Action<TerrainData> callback)
    {
        TerrainData terrainData = TerrainGenerator.GenerateTerrain(mapData.heightMap, depth, curve);
        lock (terrainThreadInfoQueue) 
        {
            terrainThreadInfoQueue.Enqueue(new MapThreadInfo<TerrainData> (callback, terrainData));
        }
    }

    void Update(){
        if (mapThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapThreadInfoQueue.Count; i++) 
            {
                MapThreadInfo<MapData> threadInfo = mapThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if (terrainThreadInfoQueue.Count > 0) 
        {
            for (int i = 0; i < terrainThreadInfoQueue.Count; i++) 
            {
                MapThreadInfo<TerrainData> threadInfo = terrainThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    MapData GenerateMapData(Vector2 center) 
    {
        float [,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, noiseScale, octaves, persistence, lacunarity, center, normalizeMode);
        Color[] colorMap = new Color[mapWidth * mapHeight];
        for (int y = 0; y < mapHeight; y++) 
        {
            for (int x = 0; x < mapWidth; ++x) 
            {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < terrainTypes.Length; i++) 
                {
                    if (currentHeight >= terrainTypes[i].height)
                    {
                        colorMap[y * mapWidth + x] = terrainTypes[i].color;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colorMap);
    }

    void OnValidate() {
        if (mapWidth < 1) mapWidth = 1;
        if (mapHeight < 1) mapHeight = 1;
        if (lacunarity < 1) lacunarity = 1;
        if (octaves < 0) octaves = 0;    
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo (Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;

        }
    }
}


[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;

}

public struct MapData 
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap) {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}
