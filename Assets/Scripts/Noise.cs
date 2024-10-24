using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    // Function to generate a noise map based on the provided settings
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCenter) {
        float[,] noiseMap = new float[mapWidth, mapHeight];   // 2D array to store noise values

        System.Random prng = new System.Random(settings.seed);   // Pseudo-random number generator using the seed value
        Vector2[] octaveOffsets = new Vector2[settings.octaves];   // Array to store offsets for each octave

        float maxPossibleHeight = 0;   // Maximum possible height for global normalization
        float amplitude = 1;   // Initial amplitude for noise generation
        float frequency = 1;   // Initial frequency for noise generation

        // Generate random offsets for each octave
        for (int i = 0; i < settings.octaves; i++) {
            float offsetX = prng.Next(-100000, 100000) + settings.offset.x + sampleCenter.x;
            float offsetY = prng.Next(-100000, 100000) + settings.offset.y + sampleCenter.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;   // Accumulate the max possible height for normalization (1 * amplitude)
            amplitude *= settings.persistance;   // Decrease amplitude for each octave
        }
        
        // Ensure the scale is not zero or negative, as this would cause errors
        if (settings.scale <= 0) {
            settings.scale = 0.0001f;
        }

        float maxLocalNoiseHeight = float.MinValue;   // Track maximum noise height for local normalization
        float minLocalNoiseHeight = float.MaxValue;   // Track minimum noise height for local normalization

        float halfWidth = mapWidth / 2.0f;   // Half width of the noise map, used to center the sample points
        float halfHeight = mapHeight / 2.0f;   // Half height of the noise map, used to center the sample points

        // Loop through each point in the noise map
        for (int x = 0; x < mapWidth; x++) {
            for (int y = 0; y < mapHeight; y++) {
                
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;   // Initialize the height value for each point

                // Loop through each octave to accumulate the noise value
                for (int i = 0; i < settings.octaves; i++) {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / settings.scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / settings.scale * frequency;

                    // Generate Perlin noise and normalize the value to be between -1 and 1
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;   // Add the weighted noise value to the height

                    amplitude *= settings.persistance;   // Reduce amplitude for the next octave
                    frequency *= settings.lacunarity;   // Increase frequency for the next octave
                }
 
                // Track the maximum and minimum noise height for local normalization
                if (noiseHeight > maxLocalNoiseHeight) {
                    maxLocalNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minLocalNoiseHeight) {
                    minLocalNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;

                float normalizedHeight = (noiseMap[x, y] + 1) / maxPossibleHeight;   // Normalize the height value
                noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);   // Ensure the value stays within a valid range
            }
        }

        // NoiseMapExporter.ExportNoiseMap(noiseMap);
        
        return noiseMap;   // Return the generated noise map
    }
}

[System.Serializable]
public class NoiseSettings {
    public float scale = 50;   // Scale of the noise map

    [Range(1, 10)]
    public int octaves = 6;   // Number of octaves (layers of noise)
    [Range(0, 1)]
    public float persistance = 0.6f;   // Controls amplitude decrease for each octave
    [Range(1, 15)]
    public float lacunarity = 2;   // Controls frequency increase for each octave

    public int seed;   // Seed for random generation of noise
    public Vector2 offset;   // Offset for shifting the noise map

    // Validate the settings to ensure they have proper values
    public void ValidateValues() {
        scale = Mathf.Max(scale, 0.01f);   // Ensure scale is positive and not too small
        octaves = Mathf.Max(octaves, 1);   // Ensure at least one octave
        lacunarity = Mathf.Max(lacunarity, 1);   // Ensure lacunarity is at least 1
        persistance = Mathf.Clamp01(persistance);   // Clamp persistence between 0 and 1
    }
}
