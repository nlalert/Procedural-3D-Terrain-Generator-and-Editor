using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator {
    // Generates a texture from a color map
    public static Texture2D TextureFromColorMap(Color[] colorMap, int width, int height) {
        // Create a new Texture2D with the given width and height
        Texture2D texture = new Texture2D(width, height);
        
        // Set texture filtering to Point (no blending between pixels)
        texture.filterMode = FilterMode.Point;
        
        // Clamp texture coordinates so there is no wrapping around the edges
        texture.wrapMode = TextureWrapMode.Clamp;
        
        // Apply the color map (an array of colors) to the texture
        texture.SetPixels(colorMap);
        // Apply the changes to the texture
        texture.Apply();
        
        // Return the generated texture
        return texture;
    }

    // Generates a texture from a height map (converts height data to grayscale)
    public static Texture2D TextureFromHeightMap(HeightMap heightMap) {
        // Get the width and height of the height map
        int width = heightMap.values.GetLength(0);
        int height = heightMap.values.GetLength(1);

        // Create a color map array to store colors for each pixel
        Color[] colorMap = new Color[width * height];

        // Loop through each pixel in the height map
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                // Convert the height value to a grayscale color (between black and white)
                // Mathf.InverseLerp normalizes the height value to a 0-1 range based on min and max height values
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, Mathf.InverseLerp(heightMap.minValue, heightMap.maxValue, heightMap.values[x, y])); 
            }
        } 

        // Generate a texture using the color map and return it
        return TextureFromColorMap(colorMap, width, height);
    }
}
