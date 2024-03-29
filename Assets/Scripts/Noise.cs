using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class Noise
{

    public enum NormaliseMode { Local, Global };
    public static float[,] GenerateNoiseMap(
        int mapWidth,
        int mapHeight,
        string seed,
        float scale,
        int octaves,
        float persistance,
        float lacunarity,
        Vector2 offset,
        NormaliseMode normaliseMode
    )
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random prng = new System.Random(convertStringSeedToInt(seed));
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        // since glo. norm. is based off the highest possible values, majority of noise will never be those values
        // so we use this value to divide the 'highest possible value'. Increasing too much will cause plateus where heights exceed 1
        float globalNormaliseCorrectionEstimate = 1.1f; // in tut it is 1.75 - someting gone successfully wrong? 
        

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }


        if (scale <= 0) {
            scale = 0.0001f;
        }

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2;
        float halfHeight = mapHeight / 2;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                // each octave we will layer another level of noise on to the noiseMap,
                // subsequent octaves will impact the overall map less based on persistance
                // and lacunarity
                for( int i = 0; i < octaves; i++)
                {
                    // sample points subtract halfWidth / halfHeight so when we change noise scale,
                    // it scales from the center of the noise map instead of the top-righht corner
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                    perlinValue += perlinValue * 2 - 1; // allows value to be in the range +-1 so noiseHeight may sometimes decrease

                    noiseHeight += perlinValue * amplitude;
                     
                    amplitude *= persistance; // decreases each octave
                    frequency *= lacunarity; // increases each octave
                }

                if(noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                } 
                else if ( noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }
                noiseMap[x,y] = noiseHeight;
            }
        }

        // go through all noise map values and normalise them between max and min values
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                switch (normaliseMode)
                {
                    case NormaliseMode.Local:
                        noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                        break;
                    case NormaliseMode.Global:
                        float normalisedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight / globalNormaliseCorrectionEstimate);
                        noiseMap[x, y] = Mathf.Clamp(normalisedHeight,0, int.MaxValue);
                        break;
                    default:
                        break;
                }

            }
        }

        return noiseMap;
    }
    
    public static int convertStringSeedToInt(string seed)
    {
        int seedValue = 0;
        byte[] bytes = Encoding.ASCII.GetBytes(seed);

        foreach (byte b in bytes)
        {
            seedValue += b;
        }

        return seedValue;
    }
}
