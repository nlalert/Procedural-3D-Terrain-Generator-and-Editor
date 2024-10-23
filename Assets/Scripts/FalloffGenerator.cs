using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FalloffGenerator
{
    // Generates a falloff map based on the specified size
    public static float[,] GenerateFalloffMap(int size){
        // Create a 2D array to store falloff values
        float[,] map = new float[size, size];

        // Loop through the map, calculating falloff values for each coordinate
        for (int i = 0; i < size; i++){
            for (int j = 0; j < size; j++){
                // Normalize i and j values from -1 to 1, with 0 at the center of the map
                float x = i / (float)size * 2 - 1;
                float y = j / (float)size * 2 - 1;

                // Calculate the value based on the distance to the edge, using the maximum of x or y
                // Closer to the edge results in higher values (closer to 1)
                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));

                // Apply the falloff evaluation function to determine the final falloff value
                map[i, j] = Evaluate(value);
            }
        }

        // Return the generated falloff map
        return map;
    }

    // Evaluates a falloff value using a power function to control the steepness of the falloff
    static float Evaluate(float value) {
        // a controls the steepness of the falloff curve
        float a = 3;
        // b controls how much of the map is affected by the falloff
        float b = 2.2f;

        // Apply a mathematical function to create the falloff effect
        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}
