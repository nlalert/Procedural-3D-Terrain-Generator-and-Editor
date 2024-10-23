using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// This class defines texture settings for terrain layers, inheriting from the UpdatableData class
[CreateAssetMenu()]
public class TextureSettings : UpdatableData
{
    // Constants for texture settings
    const int textureSize = 512;  // Fixed size for textures
    const TextureFormat textureFormat = TextureFormat.RGB565;  // Texture format used for terrain layers

    // Array to store all the layers used in the terrain
    public Layer[] layers;

    // Saved minimum and maximum heights for mesh adjustments
    float savedMinHeight;
    float savedMaxHeight;

    // Method to apply texture and material settings to the terrain material
    public void ApplyToMaterial(Material material)
    {
        // Set various properties related to the layers of the terrain in the material
        material.SetInt("layerCount", layers.Length);
        material.SetColorArray("baseColors", layers.Select(x => x.tint).ToArray());
        material.SetFloatArray("baseStartHeights", layers.Select(x => x.startHeight).ToArray());
        material.SetFloatArray("baseBlends", layers.Select(x => x.blendStrength).ToArray());
        material.SetFloatArray("baseColorStrength", layers.Select(x => x.tintStrength).ToArray());
        material.SetFloatArray("baseTextureScales", layers.Select(x => x.textureScale).ToArray());

        // Create a texture array from the textures and assign it to the material
        Texture2DArray texturesArray = GenerateTextureArray(layers.Select(x => x.texture).ToArray());
        material.SetTexture("baseTextures", texturesArray);

        // Update mesh heights based on the min and max height of the terrain
        UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
    }

    // Method to update the material's min and max height values
    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
        // Save the min and max height for later use
        savedMinHeight = minHeight;
        savedMaxHeight = maxHeight;

        // Update the material properties for height control
        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
    }

    // Method to generate a Texture2DArray from an array of Texture2D objects
    Texture2DArray GenerateTextureArray(Texture2D[] textures)
    {
        // Create a new texture array with the defined size and format
        Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);

        // Loop through the textures and assign their pixels to the texture array
        for (int i = 0; i < textures.Length; i++)
        {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }
        textureArray.Apply();  // Apply the changes to the texture array

        return textureArray;  // Return the generated texture array
    }

    // Serializable class to define each layer's properties (used for terrain textures)
    [System.Serializable]
    public class Layer
    {
        public Texture2D texture;  // Texture for the layer
        public Color tint;         // Tint color for the layer
        [Range(0, 1)]
        public float tintStrength; // Strength of the tint applied to the layer
        [Range(0, 1)]
        public float startHeight;  // The height at which the layer starts on the terrain
        [Range(0, 1)]
        public float blendStrength; // How smoothly the layer blends with the next layer
        public float textureScale;  // Scale of the texture for the layer
    }
}
