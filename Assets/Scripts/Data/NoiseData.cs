using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class NoiseData : UpdateableData
{
    public string seed;
    public Vector2 offset;

    public float noiseScale;
    [Range(1, 30)]
    public int octaves;
    [Range(0f, 1f)]
    public float persistence;
    public float lacunarity;

    public Noise.NormaliseMode normaliseMode;

    protected override void OnValidate()
    {
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
        base.OnValidate();
    }
}
