using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;
using static UnityEditor.Experimental.GraphView.GraphView;

public class TerrainGenerator : MonoBehaviour
{
    public static TerrainGenerator instance { get; private set; }

    [Header("Terrain Setting"), Space(10)]
    [SerializeField] private Terrain MainTerrain = null;
    [SerializeField] private float StartTerrainHeight = 0f;
    [SerializeField] private Vector3Int TerrainScale = Vector3Int.zero;
    [SerializeField] private int DirtIndex_1 = 0;
    [SerializeField] private int DirtIndex_2 = 0;

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
        float[,,] preAlphaMapArray = new float[scaleZ, scaleX, alphaMapCount];

        for (int z = 0; z < scaleZ; ++z)
        {
            for (int x = 0; x < scaleX; ++x)
            {
                Vector3Int position = new Vector3Int(x + sx, 100, z + sz);
                if (Physics.Raycast(position, Vector3.down, 200, layer))
                {
                    for (int i = 0; i < alphaMapCount; ++i)
                    {
                        alphaMapArray[position.z, position.x, i] = 0;
                        preAlphaMapArray[z, x, i] = 0;
                    }

                    alphaMapArray[position.z, position.x, DirtIndex_1] = 1;
                    preAlphaMapArray[z, x, DirtIndex_1] = 1;
                }
                else
                {
                    for (int i = 0; i < alphaMapCount; ++i)
                        preAlphaMapArray[z, x, i] = alphaMapArray[position.z, position.x, i];
                }
            }
        }
        MainTerrain.terrainData.SetAlphamaps(sx, sz, preAlphaMapArray);
    }

    public void PaintTerrainDirt(int sx, int sz, int scaleX, int scaleZ, Vector3[] positions)
    {
        float[,,] preAlphaMapArray = new float[scaleZ, scaleX, alphaMapCount];

        for (int z = 0; z < scaleZ; ++z)
        {
            for (int x = 0; x < scaleX; ++x)
            {
                Vector3Int position = new Vector3Int(x + sx, 0, z + sz);
                for (int i = 0; i < alphaMapCount; ++i)
                    preAlphaMapArray[z, x, i] = alphaMapArray[position.z, position.x, i];
            }
        }

        for (int i = 0; i < positions.Length; ++i)
        {
            Vector3Int realPosition = Vector3Int.RoundToInt(positions[i] - new Vector3(.5f, 0, .5f));
            Vector3Int prePosition = realPosition - new Vector3Int(sx, 0, sz);
            for (int layer = 0; layer < alphaMapCount; ++layer)
            {
                alphaMapArray[realPosition.z, realPosition.x, layer] = 0;
                preAlphaMapArray[prePosition.z, prePosition.x, layer] = 0;
            }

            alphaMapArray[realPosition.z, realPosition.x, DirtIndex_1] = 1;
            preAlphaMapArray[prePosition.z, prePosition.x, DirtIndex_1] = 1;
        }
        MainTerrain.terrainData.SetAlphamaps(sx, sz, preAlphaMapArray);
    }

    public void PaintTerrainDefault(int sx, int sz, int scaleX, int scaleZ, int layer, int hashCode)
    {
        scaleX += 2;
        scaleZ += 2;
        float[,,] preAlphaMapArray = new float[scaleZ, scaleX, alphaMapCount];

        for (int z = 0; z < scaleZ; ++z)
        {
            for (int x = 0; x < scaleX; ++x)
            {
                Vector3Int position = new Vector3Int(x + sx, 100, z + sz);
                if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 200, layer) && hit.transform.GetHashCode().Equals(hashCode))
                {
                    alphaMapArray[position.z, position.x, DirtIndex_1] = 0;
                    preAlphaMapArray[z, x, DirtIndex_1] = 0;
                    alphaMapArray[position.z, position.x, 0] = 1;
                    preAlphaMapArray[z, x, 0] = 1;
                }
                else
                {
                    for (int i = 0; i < alphaMapCount; ++i)
                    {
                        if (alphaMapArray[position.z, position.x, DirtIndex_2].Equals(1))
                        {
                            alphaMapArray[position.z, position.x, DirtIndex_2] = 0;
                            alphaMapArray[position.z, position.x, 0] = 1;
                            preAlphaMapArray[z, x, 0] = 1;
                        }
                        else
                            preAlphaMapArray[z, x, i] = alphaMapArray[position.z, position.x, i];
                    }
                }
            }
        }
        MainTerrain.terrainData.SetAlphamaps(sx, sz, preAlphaMapArray);
    }
}
