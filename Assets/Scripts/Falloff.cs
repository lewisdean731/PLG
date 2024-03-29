using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Falloff
{
    public static float[,] generateFalloffMap(int size, float transition, float deadzone)
    {
        float[,] map = new float[size, size];

        for(int i = 0; i < size; i++)
        {
            for(int j = 0; j < size; j++)
            {
                float x = i / (float)size * 2 - 1; // give us value in the range +-1
                float y = j / (float)size * 2 - 1;

                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                map[i,j] = evaluate(value, transition, deadzone);
            }
        }

        return map;
    }

    // curve for generating the value
    static float evaluate(float value, float transition = 3, float deadzone = 2.2f)
    {
        float a = transition;
        float b = deadzone;

        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}
