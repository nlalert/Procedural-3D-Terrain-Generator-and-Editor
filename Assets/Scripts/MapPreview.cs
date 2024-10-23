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
    public enum DrawMode {NoiseMap, Mesh, FalloffMap};
    public DrawMode drawMode; // Current mode of the map preview (Noise, Mesh, or Falloff)

    // Settings for the mesh, height map, and texture generation
    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureSettings textureSettings;

    public Material terrainMaterial; // Material for applying texture and height changes to the terrain

    // Editor-specific settings
    [Range(0, MeshSettings.numSupportedLODS-1)] // Select LOD level for preview
    public int editorPreviewLOD; // Level of detail to be used in editor preview

    public bool autoUpdate; // Automatically update when values are changed

    // Method to draw the map preview in the Unity editor
    public void DrawMapInEditor(){
        // Apply texture settings to the terrain material
        textureSettings.ApplyToMaterial(terrainMaterial);
        textureSettings.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

        // Generate the height map using the specified mesh and height map settings
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, Vector2.zero);

        // Check the selected draw mode (NoiseMap, Mesh, or FalloffMap) and call the appropriate method
        if(drawMode == DrawMode.NoiseMap){
            DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap)); // Draw noise map as a texture
        }
        else if(drawMode == DrawMode.Mesh){
            DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorPreviewLOD)); // Draw terrain mesh
        }
        else if(drawMode == DrawMode.FalloffMap){
            // Generate a falloff map and display it as a texture
            DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numVertsPerLine), 0, 1)));
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

    // Draw the generated mesh on the meshFilter
    public void DrawMesh(MeshData meshData) {
        // Assign the generated mesh to the mesh filter
        meshFilter.sharedMesh = meshData.CreateMesh();

        // Enable the mesh renderer and disable the texture renderer
        textureRenderer.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
    }

    // Callback for when values are updated in the settings (e.g., in the editor)
    void OnValuesUpdated(){
        if (!Application.isPlaying) { // Only perform updates in the editor, not during runtime
            #if UNITY_EDITOR
            // Use Unity Editor's delay call to postpone execution, ensuring the update happens in the editor
            EditorApplication.delayCall += () => DrawMapInEditor();
            #endif
        }
    }

    // Callback when the texture settings are updated
    void OnTextureValuesUpdated(){
        // Apply any changes to the texture settings to the material
        textureSettings.ApplyToMaterial(terrainMaterial);
    }

    // Method called by Unity when the script is validated (when script changes or values change)
    void OnValidate(){
        // Ensure event subscriptions are handled properly to avoid multiple subscriptions

        // Unsubscribe and resubscribe to mesh settings update events
        if(meshSettings != null){
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }

        // Unsubscribe and resubscribe to height map settings update events
        if(heightMapSettings != null){
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }

        // Unsubscribe and resubscribe to texture settings update events
        if(textureSettings != null){
            textureSettings.OnValuesUpdated -= OnTextureValuesUpdated;
            textureSettings.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }
}
