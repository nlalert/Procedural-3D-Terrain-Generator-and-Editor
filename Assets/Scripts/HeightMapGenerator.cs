using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightMapGenerator
{
    // Generates a height map based on the provided width, height, height map settings, and sample center
    public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCenter){
        // Generate a noise map using the provided noise settings and sample center
        float[,] values = Noise.GenerateNoiseMap(width, height, settings.noiseSettings, sampleCenter);

        // Variables to track the minimum and maximum height values
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        // Loop through all points in the generated noise map to apply height curve and multiplier
        for (int i = 0; i < width; i++){
            for (int j = 0; j < height; j++){
                // Apply height curve and multiplier to each noise value
                values[i, j] *= values[i, j] * settings.heightMultiplier;

                // Update the min and max values to track height extremes
                if(values[i, j] > maxValue){
                    maxValue = values[i, j];
                }
                if(values[i, j] < minValue){
                    minValue = values[i, j];
                }
            }
        }

        // Return a HeightMap struct containing the height values, and min/max heights
        return new HeightMap(values, minValue, maxValue);
    } 
}

// Struct to store the height map data, including the 2D height values and the min/max height values
public readonly struct HeightMap {
    public readonly float[,] values; // 2D array storing the height values
    public readonly float minValue; // Minimum height value in the map
    public readonly float maxValue; // Maximum height value in the map

    // Constructor to initialize the height map values and its min/max
    public HeightMap(float[,] values, float minValue, float maxValue) {
        this.values = values;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }
}
