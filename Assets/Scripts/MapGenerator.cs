using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{

    public enum DrawMode { NoiseMap, FalloffMap, ColourMap, NoiseMesh, ColourMesh};
    public DrawMode drawMode;

    [Range(0, 6)]
    public int editorPreviewlevelOfDetail;

    public string seed;
    public Vector2 offset;
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
    
    public float noiseScale;
    [Range(1, 30)]
    public int octaves;
    [Range(0f, 1f)]
    public float persistence;
    public float lacunarity;

    public bool useFalloff;
    [Range (0f, 1f)]
    public float falloffValueTODO;
    public Noise.NormaliseMode normaliseMode;

    public bool autoUpdate;

    public TerrainType[] regions;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    float[,] falloffMap = new float[TerrainMetrics.totalMapChunkSize, TerrainMetrics.totalMapChunkSize];

    void Awake()
    {
        falloffMap = Falloff.generateFalloffMap(TerrainMetrics.totalMapChunkSize);
    }

    public void DrawMap()
    {
        MapData mapData = generateMapData(Vector2.zero);

        MapDisplay display = FindObjectOfType<MapDisplay>();
        switch (drawMode)
        {
            case DrawMode.NoiseMap:
                display.DrawTexture(TextureGenerator.textureFromHeightMap(mapData.heightMap));
                break;
            case DrawMode.FalloffMap:
                display.DrawTexture(TextureGenerator.textureFromHeightMap(Falloff.generateFalloffMap(TerrainMetrics.mapChunkSize)));
                break;
            case DrawMode.ColourMap:
                display.DrawTexture(TextureGenerator.textureFromColourMap(mapData.colourMap, TerrainMetrics.mapChunkSize, TerrainMetrics.mapChunkSize));
                break;
            case DrawMode.NoiseMesh:
                display.DrawMesh(
                    MeshGenerator.generateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewlevelOfDetail),
                    TextureGenerator.textureFromHeightMap(mapData.heightMap)
                );
                break;
            case DrawMode.ColourMesh:
                display.DrawMesh(
                    MeshGenerator.generateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewlevelOfDetail),
                    TextureGenerator.textureFromColourMap(mapData.colourMap, TerrainMetrics.mapChunkSize, TerrainMetrics.mapChunkSize)
                );
                break;
            default:
                break;
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
        MapData mapData = generateMapData(center);
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.generateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }
    void Update()
    {
        if(mapDataThreadInfoQueue.Count > 0)
        {
            for(int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    MapData generateMapData(Vector2 center)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(TerrainMetrics.totalMapChunkSize, TerrainMetrics.totalMapChunkSize, seed, noiseScale, octaves, persistence, lacunarity, center + offset, normaliseMode);
        Color[] colourMap = new Color[TerrainMetrics.mapChunkSize * TerrainMetrics.mapChunkSize];

        for (int y = 0; y < TerrainMetrics.totalMapChunkSize; y++)
        {
            for(int x = 0; x < TerrainMetrics.totalMapChunkSize; x++)
            {
                if (useFalloff)
                {
                    noiseMap[x,y] = Mathf.Clamp01(noiseMap[x,y] - falloffMap[x,y]);
                }
            }
        }
        for (int y = 0; y < TerrainMetrics.mapChunkSize; y++)
        {
            for (int x = 0; x < TerrainMetrics.mapChunkSize; x++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i = regions.Length - 1; i >= 0; i--)
                {
                    if (currentHeight >= regions[i].height)
                    {
                        colourMap[y * TerrainMetrics.mapChunkSize + x] = regions[i].colour;
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colourMap);

        
        
    }
    void OnValidate()
    {
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
        if (useFalloff)
        {
            // we do this in the awake function, but it does not run in the editor
            falloffMap = Falloff.generateFalloffMap(TerrainMetrics.totalMapChunkSize);
        }
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
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
    public Color colour;

}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colourMap;

    public MapData(float[,] heightMap, Color[] colourMap)
    {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
    }
}
