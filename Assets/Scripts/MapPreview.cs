using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class MapPreview : MonoBehaviour
{
    // References to renderers and filters for displaying the texture and mesh
    public Renderer textureRenderer; // Renderer for displaying the noise/falloff map as a texture
    public MeshFilter meshFilter; // Mesh filter for applying the terrain mesh
    public MeshRenderer meshRenderer; // Renderer for the terrain mesh

    // Enumeration for selecting the type of preview
    public enum DrawMode { NoiseMap, Mesh };
    public DrawMode drawMode; // Current mode of the map preview (Noise or Mesh)

    // Settings for the mesh, height map, and texture generation
    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureSettings textureSettings;

    public Material terrainMaterial; // Material for applying texture and height changes to the terrain

    // Method to draw the map preview in the Unity editor
    public void DrawMapInEditor() {
        // Clear previously generated chunks before generating new ones
        ClearGeneratedChunks();

        // Apply texture settings to the terrain material
        textureSettings.ApplyToMaterial(terrainMaterial);
        textureSettings.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

        // Loop through and generate each terrain chunk in a radius
        for (int y = -meshSettings.mapRadius; y <= meshSettings.mapRadius; y++) {
            for (int x = -meshSettings.mapRadius; x <= meshSettings.mapRadius; x++) {
                Vector2 chunkCoord = new Vector2(x, y);
                DrawChunk(chunkCoord); // Draw each chunk
            }
        }
    }

    // Method to clear previously generated chunks before creating new ones
    void ClearGeneratedChunks() {
        // Loop through all child GameObjects of the MapPreview GameObject
        for (int i = transform.childCount - 1; i >= 0; i--) {
            // Destroy any child GameObjects with the name "Chunk Preview" (which were previously generated)
            if (transform.GetChild(i).name == "Chunk Preview") {
                DestroyImmediate(transform.GetChild(i).gameObject); // Destroy chunk preview immediately in editor mode
            }
        }
    }

    // Draw an individual chunk for the map preview
    void DrawChunk(Vector2 chunkCoord) {
        // Calculate the sample center for the current chunk
        Vector2 sampleCenter = chunkCoord * meshSettings.meshWorldSize / meshSettings.meshScale;

        // Generate the height map using the specified settings
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, sampleCenter);

        // Check the selected draw mode and call the appropriate method
        if (drawMode == DrawMode.NoiseMap) {
            DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap)); // Draw noise map as a texture
        } 
        else if (drawMode == DrawMode.Mesh) {
            DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings), chunkCoord); // Draw terrain mesh
        }
    }

    // Draw the texture on the textureRenderer
    public void DrawTexture(Texture2D texture) {
        // Apply the generated texture to the shared material (used in Editor mode)
        textureRenderer.sharedMaterial.mainTexture = texture;

        // Adjust the scale of the renderer to match the texture size
        textureRenderer.transform.localScale = new Vector3(texture.width / 10f, 1, texture.height / 10f);

        // Enable the texture renderer and disable the mesh filter
        textureRenderer.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
    }

    // Draw the generated mesh on the meshFilter, offsetting it according to chunk coordinates
    public void DrawMesh(MeshData meshData, Vector2 chunkCoord) {
        // Create a new GameObject for each chunk to preview multiple chunks
        GameObject chunkObject = new GameObject("Chunk Preview");
        chunkObject.transform.parent = transform;

        // Add MeshFilter and MeshRenderer to the chunk object
        MeshFilter chunkMeshFilter = chunkObject.AddComponent<MeshFilter>();
        MeshRenderer chunkMeshRenderer = chunkObject.AddComponent<MeshRenderer>();

        // Assign the generated mesh to the chunk's mesh filter
        chunkMeshFilter.sharedMesh = meshData.CreateMesh();
        chunkMeshRenderer.sharedMaterial = terrainMaterial;

        // Offset the chunk to its correct position
        chunkObject.transform.position = new Vector3(chunkCoord.x * meshSettings.meshWorldSize, 0, chunkCoord.y * meshSettings.meshWorldSize);

        // Enable the mesh renderer and disable the texture renderer
        textureRenderer.gameObject.SetActive(false);
    }

    // Callback for when values are updated in the settings (e.g., in the editor)
    void OnValuesUpdated() {
        if (!Application.isPlaying) { // Only perform updates in the editor, not during runtime
            #if UNITY_EDITOR
            // Use Unity Editor's delay call to postpone execution, ensuring the update happens in the editor
            EditorApplication.delayCall += () => DrawMapInEditor();
            #endif
        }
    }

    // Callback when the texture settings are updated
    void OnTextureValuesUpdated() {
        // Apply any changes to the texture settings to the material
        textureSettings.ApplyToMaterial(terrainMaterial);
    }

    // Method called by Unity when the script is validated (when script changes or values change)
    void OnValidate() {
        // Ensure event subscriptions are handled properly to avoid multiple subscriptions

        // Unsubscribe and resubscribe to mesh settings update events
        if (meshSettings != null) {
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }

        // Unsubscribe and resubscribe to height map settings update events
        if (heightMapSettings != null) {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }

        // Unsubscribe and resubscribe to texture settings update events
        if (textureSettings != null) {
            textureSettings.OnValuesUpdated -= OnTextureValuesUpdated;
            textureSettings.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }
}
