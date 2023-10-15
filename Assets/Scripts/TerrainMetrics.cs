using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainMetrics
{
    public static int[] lods = { 1, 2, 4, 6, 8, 12 }; // LODS 0-5, maps to factors of 96
    public const int highestLod = 12;

    public const int mapChunkSize = 97; // actual mesh size 96
    public const int totalMapChunkSize = mapChunkSize + (2* highestLod); // to account for border vertices/tris that are discarded

    public const float terrainScale = 1f;
}
