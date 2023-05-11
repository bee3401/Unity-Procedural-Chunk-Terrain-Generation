using System;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public const float maxViewDistance = 700f;
    public Transform player;
    public Material material;

    public static Vector2 playerPosition;

    static MapGenerator mapGenerator;
    int chunkSize;
    int visibleChunks;
    public static Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> previousVisibleTerrainChunks = new List<TerrainChunk>();
    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        chunkSize = MapGenerator.chunkSize;
        visibleChunks = Mathf.RoundToInt(maxViewDistance / chunkSize);
    }

    void Update()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        playerPosition = new Vector2(player.position.x, player.position.z);
        
        UpdateVisibleChunks();
        
    }

    void UpdateVisibleChunks()
    {

        for (int i = 0; i < previousVisibleTerrainChunks.Count; i++)
        {
            previousVisibleTerrainChunks[i].SetVisible(false);
        }
        previousVisibleTerrainChunks.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(playerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(playerPosition.y / chunkSize);

        for (int yOffset = -visibleChunks; yOffset <= visibleChunks; yOffset++)
        {
            for (int xOffset = -visibleChunks; xOffset <= visibleChunks; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    if (terrainChunkDictionary[viewedChunkCoord].IsVisible())
                    {
                        previousVisibleTerrainChunks.Add(terrainChunkDictionary[viewedChunkCoord]);
                    }
                }
                else
                {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, mapGenerator, transform, material));
                }
            }
        }
    }

    public class TerrainChunk
    {
        GameObject terrainChunk;
        Terrain terrain;
        Transform parent;
        MapData mapData;
        Bounds bounds;
        Vector2 position;
        float[,] noiseMap;
        MapGenerator mapGenerator;

        public TerrainChunk(Vector2 coord, int size, MapGenerator mapGenerator, Transform parent, Material material)
        {
            this.mapGenerator = mapGenerator;
            this.parent = parent;
            int vertexCount = size + 1;


            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
 
            mapGenerator.RequestMapData(new Vector2(position.y, -1*position.x), OnMapDataReceived);   
        }

        void OnMapDataReceived(MapData mapData)
        {
            mapGenerator.RequestTerrainData(mapData, (terrainData) => StartCoroutine(onTerrainDataReceived(terrainData)));

            this.mapData = mapData;
        }

        
        IEnumerator onTerrainDataReceived(TerrainData terrainData)
        {
            yield return null;

            int groundLayer = LayerMask.NameToLayer("Ground");
            float subpixelOffset = 0.0f;

            Vector3 positionV3 = new Vector3(position.x, UnityEngine.Random.Range(-subpixelOffset, subpixelOffset), position.y);

            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, new Vector2(MapGenerator.chunkSize, MapGenerator.chunkSize));

            TerrainLayer terrainLayer = new TerrainLayer();
            terrainLayer.diffuseTexture = texture;

            float tileX = texture.width;
            float tileZ = texture.height;
            terrainLayer.tileSize = new Vector2(tileX, tileZ);

            terrainData.terrainLayers = new TerrainLayer[] {terrainLayer};
            terrainChunk = Terrain.CreateTerrainGameObject(terrainData);
            terrainChunk.layer = groundLayer;
            terrainChunk.transform.position = positionV3;
            terrain = terrainChunk.GetComponent<Terrain>();
            terrain.allowAutoConnect = true;
            terrain.transform.parent = parent;
            SetVisible(false);
        }

        IEnumerator UpdateEdgeHeights(TerrainData terrainData)
        {
            TerrainData leftNeighbor, rightNeighbor, bottomNeighbor, topNeighbor;
            
            // Waits until all neighbors are generated
            do
            {
                yield return new WaitForSeconds(0.1f);

                leftNeighbor = GetNeighborTerrainData(new Vector2(position.x / chunkSize - 1, position.y / chunkSize));
                rightNeighbor = GetNeighborTerrainData(new Vector2(position.x / chunkSize + 1, position.y / chunkSize));
                bottomNeighbor = GetNeighborTerrainData(new Vector2(position.x / chunkSize, position.y / chunkSize - 1));
                topNeighbor = GetNeighborTerrainData(new Vector2(position.x / chunkSize, position.y / chunkSize + 1));

            } while (leftNeighbor == null || rightNeighbor == null || bottomNeighbor == null || topNeighbor == null);

            // Modifies terrain heights based on neighbors
            for (int x = 0; x < terrainData.heightmapWidth; x++)
            {
                for (int y = 0; y < terrainData.heightmapHeight; y++)
                {
                    if (x == 0 && leftNeighbor != null)
                    {
                        terrainData.SetHeights(x, y, leftNeighbor.GetHeights(terrainData.heightmapWidth - 1, y, 1, 1));
                    }
                    else if (x == terrainData.heightmapWidth - 1 && rightNeighbor != null)
                    {
                        terrainData.SetHeights(x, y, rightNeighbor.GetHeights(0, y, 1, 1));
                    }

                    if (y == 0 && bottomNeighbor != null)
                    {
                        terrainData.SetHeights(x, y, bottomNeighbor.GetHeights(x, terrainData.heightmapHeight - 1, 1, 1));
                    }
                    else if (y == terrainData.heightmapHeight - 1 && topNeighbor != null)
                    {
                        terrainData.SetHeights(x, y, topNeighbor.GetHeights(x, 0, 1, 1));
                    }
                }
            }
        }


        private void GenerateTerrainTexture(TerrainData terrainData, float[,] heights)
        {
            int width = heights.GetLength(0);
            int height = heights.GetLength(1);

            Color[] colorMap = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float currentHeight = heights[x, y];
                    for (int i = 0; i < mapGenerator.terrainTypes.Length; i++)
                    {
                        if (currentHeight <= mapGenerator.terrainTypes[i].height)
                        {
                            colorMap[y * width + x] = mapGenerator.terrainTypes[i].color;
                            break;
                        }
                    }
                }
            }

            Texture2D texture = TextureGenerator.TextureFromColorMap(colorMap,new Vector2(width, height));

            // New terrain layer
            TerrainLayer terrainLayer = new TerrainLayer();
            terrainLayer.diffuseTexture = texture;

            // Computes tiling settings based on the terrain size and texture size
            float tileX = texture.width;
            float tileZ = texture.height;
            terrainLayer.tileSize = new Vector2(tileX, tileZ);

            // Assigns the terrain layer to the terrain object
            terrainData.terrainLayers = new TerrainLayer[] { terrainLayer };
        }

        public void UpdateTerrainChunk()
        {
            float chunkDistanceToPlayer = Mathf.Sqrt(bounds.SqrDistance(playerPosition));//Vector2.Distance(playerPosition, position);
            bool visible = chunkDistanceToPlayer <= maxViewDistance;
            SetVisible(visible);
        }

        public void SetVisible(bool visible)
        {
            if (terrain != null)
            {
                terrain.gameObject.SetActive(visible);
            }
        }

        public bool IsVisible ()
        {
            if (terrain != null)
            {
                return terrain.gameObject.activeSelf;
            }
            return false;
        }

        public Terrain GetTerrainChunk()
        {
            return this.terrain;
        }
        public Vector2 GetPosition()
        {
            return this.position;
        }

        public Terrain GetAdjacentTerrain(int dx, int dz)
        {
            //Debug.Log("Looking for terrain's at position " + position + ", [" + dx + ',' + dz +"] neighbor");
            Vector2 adjacentChunkCoord = new Vector2(position.x / MapGenerator.chunkSize + dx, position.y / MapGenerator.chunkSize + dz);

            TerrainChunk adjacentTerrainChunk;
            if (MapManager.terrainChunkDictionary.TryGetValue(adjacentChunkCoord, out adjacentTerrainChunk) && adjacentTerrainChunk != null)
            {
                //Debug.Log("    " + adjacentTerrainChunk.terrain.transform.position);

                return adjacentTerrainChunk.terrain;
            }

            return null;
        }

        TerrainData GetNeighborTerrainData(Vector2 neighborCoord)
        {
            TerrainChunk neighborTerrainChunk;
            if (terrainChunkDictionary.TryGetValue(neighborCoord, out neighborTerrainChunk) && neighborTerrainChunk != null)
            {
                return neighborTerrainChunk.GetTerrainData();
            }
            return null;
        }

        public TerrainData GetTerrainData()
        {
            return terrain.terrainData;
        }


    }
}
