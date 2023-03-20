using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

public class TerrainGenerator : MonoBehaviour
{
    public static TerrainGenerator instance { get; private set; }

    [Header("Terrain Setting"), Space(10)]
    [SerializeField] private Terrain MainTerrain = null;
    [SerializeField] private float StartTerrainHeight = 0f;
    [SerializeField] private Vector3Int TerrainScale = Vector3Int.zero;
    [SerializeField] private int TerrainDirtIndex = 0;

    private int alphaMapCount = 0;
    private float[,] heightMapArray = null;
    private float[,,] alphaMapArray = null;

    private readonly Vector3Int[] direction =
    {
        new Vector3Int(1, 0, 1),
        new Vector3Int(0, 0, 1),
        new Vector3Int(-1, 0, 1),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(-1, 0, -1),
        new Vector3Int(0, 0, -1),
        new Vector3Int(1, 0, -1),
        new Vector3Int(1, 0, 0),
    };

    private void Start()
    {
        instance = this;
        InitializeTerrain();
    }

    private void InitializeTerrain()
    {
        alphaMapCount = MainTerrain.terrainData.terrainLayers.Length;

        // X : WORLD SPACE Z, Z : WORLD SPACE X,
        // Because, terrain depth refers Terrain Texture
        heightMapArray = new float[TerrainScale.z, TerrainScale.x];
        alphaMapArray = new float[TerrainScale.z, TerrainScale.x, alphaMapCount];

        float depth = StartTerrainHeight / TerrainScale.y;
        for (int z = 0; z < TerrainScale.z; ++z)
        {
            for (int x = 0; x < TerrainScale.x; ++x)
            {
                heightMapArray[z, x] = depth;
                alphaMapArray[z, x, 0] = 1;
                for (int i = 1; i < alphaMapCount; ++i)
                    alphaMapArray[z, x, i] = 0;
            }
        }

        TerrainData data = MainTerrain.terrainData;
        data.baseMapResolution = TerrainScale.z / 2;
        data.heightmapResolution = TerrainScale.z + 1;
        data.alphamapResolution = TerrainScale.z;
        data.size = TerrainScale;
        MainTerrain.terrainData = data;

        MainTerrain.terrainData.SetHeights(0, 0, heightMapArray);
        MainTerrain.terrainData.SetAlphamaps(0, 0, alphaMapArray);
    }

    public void PaintTerrainDirt(int sx, int sz, int scaleX, int scaleZ, int layer)
    {
        scaleX += 2;
        scaleZ += 2;
        float[,,] preAlphaMapArray = new float[scaleZ, scaleX, alphaMapCount];

        for (int z = 0; z < scaleZ; ++z)
        {
            for (int x = 0; x < scaleX; ++x)
            {
                Vector3Int position = new Vector3Int(x + sx, 100, z + sz);
                if (Physics.Raycast(position, Vector3.down, 200, layer))
                {
                    PaintDirt(x, z, position.x, position.z);
                    Vector3Int randomDirection = direction[Random.Range(0, direction.Length)];
                    Vector3Int randomPosition = position + randomDirection;
                    PaintDirt(x + randomDirection.x, z + randomDirection.z, randomPosition.x, randomPosition.z);
                }
                else
                {
                    for (int i = 0; i < alphaMapCount; ++i)
                        preAlphaMapArray[z, x, i] = alphaMapArray[position.z, position.x, i];
                }
            }
        }

        MainTerrain.terrainData.SetAlphamaps(sx, sz, preAlphaMapArray);


        void PaintDirt(int x, int z, int realX, int realZ)
        {
            if (x < 0 || z < 0 || x >= scaleX || z >= scaleZ) return;

            for (int i = 0; i < alphaMapCount; ++i)
            {
                alphaMapArray[realZ, realX, i] = 0;
                preAlphaMapArray[z, x, i] = 0;
            }

            alphaMapArray[realZ, realX, TerrainDirtIndex] = 1;
            preAlphaMapArray[z, x, TerrainDirtIndex] = 1;
        }
    }
}
