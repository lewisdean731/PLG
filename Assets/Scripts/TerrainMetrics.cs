using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainMetrics
{
    public const int mapChunkSize = 239; // actual mesh size 240
    public const int totalMapChunkSize = mapChunkSize + 2; // to account for border vertices/tris that are discarded
}
