using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorMap
{
    public static Color[] generateColorMap(MapGenerator mapGenerator, float[,] noiseMap, int lod)
    {
        // We need the LOD so we can make the size of the colour map smaller as the size of the mesh decreases with each LOD
        int simplificationIncrement = TerrainMetrics.lods[lod];
        int lodDifference = TerrainMetrics.highestLod - simplificationIncrement;
        int colourMapSize = TerrainMetrics.mapChunkSize + (2 * TerrainMetrics.highestLod) - (2 * lodDifference);
        Debug.Log(lod);
        Debug.Log(simplificationIncrement);
        Debug.Log(lodDifference);
        Debug.Log(colourMapSize);

        Color[] colourMap = new Color[colourMapSize * colourMapSize];

        for (int y = 0; y < colourMapSize; y++)
        {
            for (int x = 0; x < colourMapSize; x++)
            {
                float currentHeight = noiseMap[x + lodDifference, y + lodDifference];
                for (int i = mapGenerator.regions.Length - 1; i >= 0; i--)
                {
                    if (currentHeight >= mapGenerator.regions[i].height)
                    {
                        colourMap[y * colourMapSize + x] = mapGenerator.regions[i].colour;
                        break;
                    }
                }
            }
        }

        return colourMap;
    } 
}
