using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class TerrainData : UpdateableData
{
    public float terrainScale = 1f;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public bool useFlatShading;

    public bool useFalloff;
    [Range(1f, 10f)]
    public float falloffTransition = 3;
    [Range(1f, 10f)]
    public float falloffDeadzone = 2.2f;

}
