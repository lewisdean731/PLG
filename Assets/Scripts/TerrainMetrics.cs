using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainMetrics
{
    public static int[] lods = { 1, 2, 4, 6, 8, 10, 12 }; // LODS 0-6, maps to factors of 240
    public const int highestLod = 12;

    public const int mapChunkSize = 241; // actual mesh size 240
    public const int totalMapChunkSize = mapChunkSize + (2* highestLod); // to account for border vertices/tris that are discarded

    public const float terrainScale = 1f;
}
