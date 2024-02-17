using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{

    public enum DrawMode { NoiseMap, FalloffMap, Mesh };
    public DrawMode drawMode;

    public NoiseData noiseData;
    public TerrainData terrainData;
    public TextureData textureData;

    public Material terrainMaterial;

    [Range(0, 5)]
    public int editorPreviewLevelOfDetail;

    public bool autoUpdate;
    bool isPlaying = false;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMap();
        }
    }

    void onTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    float[,] falloffMap = new float[TerrainMetrics.mapChunkSize, TerrainMetrics.mapChunkSize];

    void Awake()
    {
        isPlaying = Application.isPlaying;
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
    }

    public void DrawMap()
    {
        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
        MapData mapData = generateMapData(Vector2.zero, editorPreviewLevelOfDetail);

        MapDisplay display = FindObjectOfType<MapDisplay>();
        switch (drawMode)
        {
            case DrawMode.NoiseMap:
                display.DrawTexture(TextureGenerator.textureFromHeightMap(mapData.heightMap));
                break;
            case DrawMode.FalloffMap:
                display.DrawTexture(TextureGenerator.textureFromHeightMap(Falloff.generateFalloffMap(TerrainMetrics.totalMapChunkSize, terrainData.falloffTransition, terrainData.falloffDeadzone)));
                break;
            case DrawMode.Mesh:
                display.DrawMesh(
                    MeshGenerator.generateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, editorPreviewLevelOfDetail, terrainData.useFlatShading)
                );
                break;
            default:
                break;
        }
    }

    public void RequestMapData(Vector2 center, int lod, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(center, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center, int lod, Action<MapData> callback)
    {
        MapData mapData = generateMapData(center, lod);
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
        MeshData meshData = MeshGenerator.generateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, lod, terrainData.useFlatShading);
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

    MapData generateMapData(Vector2 center, int lod)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(TerrainMetrics.totalMapChunkSize, TerrainMetrics.totalMapChunkSize, noiseData.seed, noiseData.noiseScale, noiseData.octaves, noiseData.persistence, noiseData.lacunarity, center + noiseData.offset, noiseData.normaliseMode);
        if (terrainData.useFalloff)
        {
            if (falloffMap == null || !isPlaying)
            {
                falloffMap = Falloff.generateFalloffMap(TerrainMetrics.totalMapChunkSize, terrainData.falloffTransition, terrainData.falloffDeadzone);
            }
            int hlod = TerrainMetrics.highestLod;
            for (int y = 0; y < TerrainMetrics.totalMapChunkSize; y++)
            {
                for(int x = 0; x < TerrainMetrics.totalMapChunkSize; x++)
                {
                    if (x < hlod || x > TerrainMetrics.mapChunkSize + hlod || y < hlod || y > TerrainMetrics.mapChunkSize + hlod)
                    {
                        noiseMap[x, y] = 0;
                    }
                    else
                    {
                        noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                    }
                }
            }
        }

        return new MapData(noiseMap); 
    }
    void OnValidate()
    {
        if (terrainData != null)
        {
            // unsub and resub so we don't start adding multiple subscriptions every update
            // camera controller probably does this better?
            terrainData.onValuesUpdated -= OnValuesUpdated;
            terrainData.onValuesUpdated += OnValuesUpdated;
        }
        if (noiseData != null)
        {
            noiseData.onValuesUpdated -= OnValuesUpdated;
            noiseData.onValuesUpdated += OnValuesUpdated;
        }
        if (textureData != null)
        {
            textureData.onValuesUpdated -= onTextureValuesUpdated;
            textureData.onValuesUpdated += onTextureValuesUpdated;
        }
        if (terrainData.useFalloff)
        {
            // we do this in the awake function, but it does not run in the editor
            falloffMap = Falloff.generateFalloffMap(TerrainMetrics.totalMapChunkSize, terrainData.falloffTransition, terrainData.falloffDeadzone);
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

public struct MapData
{
    public readonly float[,] heightMap;

    public MapData(float[,] heightMap)
    {
        this.heightMap = heightMap;
    }
}
