using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class MapPreview : MonoBehaviour
{
    public Renderer textureRenderer;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public enum DrawMode { NoiseMap, Mesh };
    public DrawMode drawMode;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureSettings textureSettings;

    public Material terrainMaterial;

    public void DrawMapInEditor() {
        if (this == null) return;

        // Store the current rotation
        Quaternion currentRotation = transform.rotation;

        ClearGeneratedChunks();

        textureSettings.ApplyToMaterial(terrainMaterial);
        textureSettings.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

        for (int y = -meshSettings.mapRadius; y <= meshSettings.mapRadius; y++) {
            for (int x = -meshSettings.mapRadius; x <= meshSettings.mapRadius; x++) {
                Vector2 chunkCoord = new Vector2(x, y);
                DrawChunk(chunkCoord);
            }
        }

        // Reapply the rotation after drawing chunks
        transform.rotation = currentRotation;
    }

    void ClearGeneratedChunks() {
        if (this == null) return;  // Check if the object exists

        for (int i = transform.childCount - 1; i >= 0; i--) {
            if (transform.GetChild(i).name == "Chunk Preview") {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }
    }

    public void DrawChunk(Vector2 chunkCoord) {
        Vector2 sampleCenter = chunkCoord * meshSettings.meshWorldSize / meshSettings.meshScale;
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, sampleCenter);

        if (drawMode == DrawMode.NoiseMap) {
            DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
        } else if (drawMode == DrawMode.Mesh) {
            GameObject chunkObject = new GameObject("Chunk Preview");
            chunkObject.transform.SetParent(transform, false); // Set as a child with local transformations

            MeshFilter chunkMeshFilter = chunkObject.AddComponent<MeshFilter>();
            MeshRenderer chunkMeshRenderer = chunkObject.AddComponent<MeshRenderer>();

            chunkMeshFilter.sharedMesh = MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings).CreateMesh();
            chunkMeshRenderer.sharedMaterial = terrainMaterial;

            // Set the chunk's position relative to the parent, ensuring they align seamlessly
            chunkObject.transform.localPosition = new Vector3(chunkCoord.x * meshSettings.meshWorldSize, 0, chunkCoord.y * meshSettings.meshWorldSize);

            if (textureRenderer != null) {
                textureRenderer.gameObject.SetActive(false);
            }
        }
    }

    public void DrawTexture(Texture2D texture) {
        if (textureRenderer == null || meshFilter == null) return;  // Check if the object still exists
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width / 10f, 1, texture.height / 10f);
        textureRenderer.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
    }

    void OnValuesUpdated() {
        if (this == null) return;  // Check if the object exists before proceeding
        if (!Application.isPlaying) {
            #if UNITY_EDITOR
            EditorApplication.delayCall += () => {
                if (this != null) DrawMapInEditor();  // Check again before calling
            };
            #endif
        }
    }

    void OnTextureValuesUpdated() {
        if (terrainMaterial == null) return;
        textureSettings.ApplyToMaterial(terrainMaterial);
    }

    void OnValidate() {
        if (meshSettings != null) {
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }

        if (heightMapSettings != null) {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }

        if (textureSettings != null) {
            textureSettings.OnValuesUpdated -= OnTextureValuesUpdated;
            textureSettings.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }
}
