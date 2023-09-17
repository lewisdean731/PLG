using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class EndlessTerrain : MonoBehaviour
{
    public const float maxViewDistance = 300;
    public Transform viewer;

    public static Vector2 viewerPosition;
    int chunkSize;
    int chunksVisibleInViewDistance;

    Dictionary<Vector2, TerrainChunk> terrainChunks = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> chunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start()
    {
        chunkSize = MapGenerator.mapChunkSize - 1; // is 240 (mesh size); mapChunkSize val 241
        chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        updateVisibleChunks();
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
                    if (terrainChunks[viewedChunkCoord].isVisible())
                    {
                        chunksVisibleLastUpdate.Add(terrainChunks[viewedChunkCoord]);
                    }
                } else
                {
                    terrainChunks.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, transform));
                }
            }
        }
    }
    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;
        public TerrainChunk(Vector2 coord, int size, Transform parent)
        {
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObject.transform.position = positionV3;
            meshObject.transform.localScale = Vector3.one * size / 10f; // 10 because of the scale of the plane we are using - TODO set this somewhere
            meshObject.transform.parent = parent;
            setVisible(false);
        }

        public void updateTerrainChunk()
        {
            float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;
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
    }
}

