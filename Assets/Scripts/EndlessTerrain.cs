using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class EndlessTerrain : MonoBehaviour
{

    const float scale = 5;

    public LODInfo[] detailLevels;
    public static float maxViewDistance;
    public Transform viewer;

    public Material mapMaterial;

    public static Vector2 viewerPosition;
    public static Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDistance;

    float viewerMoveThresholdForChunkUpdate;
    float sqrViewerMoveThresholdForChunkUpdate;

    Dictionary<Vector2, TerrainChunk> terrainChunks = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> chunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreashold;

        chunkSize = MapGenerator.mapChunkSize - 1; // is 240 (mesh size); mapChunkSize val 241
        chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);
        viewerMoveThresholdForChunkUpdate = chunkSize / 4;
        sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

        updateVisibleChunks();
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / scale;
        
        // only update when viewer position has changed a bit
        if((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            updateVisibleChunks();
        }

    }

    // sets all chunks to invisible, then recalculates visible chunks based on viewer position
    void updateVisibleChunks()
    {

        for(int i = 0; i < chunksVisibleLastUpdate.Count; i++)
        {
            chunksVisibleLastUpdate[i].setVisible(false);
        }
        chunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for(int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (terrainChunks.ContainsKey(viewedChunkCoord))
                {
                    terrainChunks[viewedChunkCoord].updateTerrainChunk();
                } 
                else
                {
                    terrainChunks.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }
    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;

        MapData mapData;
        bool mapDataRecieved;
        int previousLodIndex = -1;
        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;

            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            meshObject.transform.position = positionV3 * scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * scale;
            setVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, updateTerrainChunk);
            }

            mapGenerator.RequestMapData(position, OnMapDataRecieved);
        }

        void OnMapDataRecieved(MapData mapData)
        {
            this.mapData = mapData;
            mapDataRecieved = true;

            Texture2D texture = TextureGenerator.textureFromColourMap(mapData.colourMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            updateTerrainChunk();
        }

        void onMeshDataRecieved(MeshData meshData) // ???
        {
            print("Am I being called?");
            meshFilter.mesh = meshData.createMesh();
        }

        public void updateTerrainChunk()
        {
            if (!mapDataRecieved) return;

            float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;

            if (visible)
            {
                int lodIndex = 0;
                for(int i = 0; i < detailLevels.Length - 1; i++) // -1 because visible will always be false if we were to get to that number 
                {
                    if (viewerDistanceFromNearestEdge > detailLevels[i].visibleDistanceThreashold)
                    {
                        lodIndex = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }
                if(lodIndex != previousLodIndex)
                {
                    LODMesh lodMesh = lodMeshes[lodIndex];
                    if(lodMesh.hasBeenRecieved)
                    {
                        previousLodIndex = lodIndex;
                        meshFilter.mesh = lodMesh.mesh;
                    }
                    else if (!lodMesh.hasBeenRequested)
                    {
                        lodMesh.requestMesh(mapData);
                    }
                }

                chunksVisibleLastUpdate.Add(this);

            }

            setVisible(visible);
        }

        public void setVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool isVisible()
        {
            return meshObject.activeSelf;
        }

        class LODMesh
        {
            public Mesh mesh;
            public bool hasBeenRequested;
            public bool hasBeenRecieved;
            int lod;
            Action updateCallback;

            public LODMesh(int lod, Action updateCallback)
            {
                this.lod = lod;
                this.updateCallback = updateCallback;
            }

            void onMeshDataRecieved(MeshData meshData)
            {
                mesh = meshData.createMesh();
                hasBeenRecieved = true;
                updateCallback();
            }

            public void requestMesh(MapData mapData)
            {
                hasBeenRequested = true;
                mapGenerator.RequestMeshData(mapData, lod, onMeshDataRecieved);
            }
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDistanceThreashold;
    }
}

