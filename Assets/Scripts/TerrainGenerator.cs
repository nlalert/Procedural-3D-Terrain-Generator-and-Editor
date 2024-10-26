using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureSettings textureSettings;

    public Transform viewer; // Reference to the player's position (viewer)
    public Material mapMaterial; // Material used for the terrain

    public GameObject waterPlanePrefab; // Assign a water plane prefab in the inspector
    private GameObject waterPlane; // The water plane instance

    // Dictionary to store generated terrain chunks
    public Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();

    // Initialization of terrain generation
    void Start() {
        // Apply texture settings to the material
        textureSettings.ApplyToMaterial(mapMaterial);
        textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

        // Create the water plane on top of the terrain
        CreateWaterPlane();

        // Generate all terrain chunks at the start
        GenerateAllChunks();

        // SaveTerrain("Test");
        // LoadTerrain("Test");
    }

    // Generate all terrain chunks within the map radius at the start
    void GenerateAllChunks() {
        // Loop through all coordinates within the mapRadius
        for (int y = -meshSettings.mapRadius; y <= meshSettings.mapRadius; y++) {
            for (int x = -meshSettings.mapRadius; x <= meshSettings.mapRadius; x++) {
                Vector2 chunkCoord = new Vector2(x, y);

                // Create a new terrain chunk at the specified coordinates
                TerrainChunk newChunk = new TerrainChunk(chunkCoord, heightMapSettings, meshSettings, transform, mapMaterial);
                terrainChunkDictionary.Add(chunkCoord, newChunk);
                newChunk.Load(); // Load the terrain chunk data
            }
        }
    }

    // Function to create the water plane
    void CreateWaterPlane() {
        // Instantiate the water plane prefab
        waterPlane = Instantiate(waterPlanePrefab, Vector3.zero, Quaternion.identity);

        // Calculate the total size of the terrain based on the map area level
        float terrainSize = (meshSettings.mapRadius + 0.5f) * meshSettings.meshWorldSize * 2;

        // Set the water plane's scale to cover the entire terrain
        waterPlane.transform.localScale = new Vector3(terrainSize / 10f, 1, terrainSize / 10f); // Assuming the default plane is 10x10 units

        // Position the water plane at y=0 and center it based on the height map's water level
        waterPlane.transform.position = new Vector3(0, textureSettings.layers[1].startHeight * heightMapSettings.heightMultiplier, 0);
    }

    public void SaveTerrain(string saveFileName)
    {
        TerrainSaveData saveData = new TerrainSaveData();

        // Save settings
        saveData.meshSettings = meshSettings;
        saveData.heightMapSettings = heightMapSettings;

        // Save each chunk's data
        saveData.terrainChunks = new List<TerrainChunkData>();
        foreach (var chunkEntry in terrainChunkDictionary)
        {
            TerrainChunk chunk = chunkEntry.Value;
            Mesh mesh = chunk.meshFilter.mesh;

            TerrainChunkData chunkData = new TerrainChunkData(mesh.vertices, mesh.triangles, mesh.uv, mesh.normals);

            saveData.terrainChunks.Add(chunkData);
        }

        string jsonData = JsonUtility.ToJson(saveData);
        File.WriteAllText(Application.persistentDataPath + "/" + saveFileName + ".json", jsonData);
        Debug.Log("Terrain saved to " + saveFileName);
    }

    public void LoadTerrain(string saveFileName)
    {
        string filePath = Application.persistentDataPath + "/" + saveFileName + ".json";

        if (File.Exists(filePath))
        {
            string jsonData = File.ReadAllText(filePath);
            TerrainSaveData saveData = JsonUtility.FromJson<TerrainSaveData>(jsonData);

            // Restore settings
            meshSettings = saveData.meshSettings;
            heightMapSettings = saveData.heightMapSettings;
            textureSettings.ApplyToMaterial(mapMaterial);
            textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

            // Remove existing chunks before loading new ones
            RemoveExistingChunks();

            // Generate terrain chunks based on the saved settings
            GenerateAllChunks();

            // Load each chunk's mesh data
            for (int i = 0; i < saveData.terrainChunks.Count; i++)
            {
                // Calculate chunk coordinates as before
                int x = i % (2 * meshSettings.mapRadius + 1) - meshSettings.mapRadius;
                int y = i / (2 * meshSettings.mapRadius + 1) - meshSettings.mapRadius;
                Vector2 chunkCoord = new Vector2(x, y);

                if (terrainChunkDictionary.TryGetValue(chunkCoord, out TerrainChunk chunk))
                {
                    Mesh mesh = chunk.meshFilter.mesh;

                    // Restore the saved mesh data
                    mesh.vertices = saveData.terrainChunks[i].vertices;
                    mesh.triangles = saveData.terrainChunks[i].triangles;
                    mesh.uv = saveData.terrainChunks[i].uvs;
                    mesh.normals = saveData.terrainChunks[i].normals;

                    mesh.RecalculateBounds();
                    chunk.meshCollider.sharedMesh = mesh;

                    Debug.Log($"Chunk {chunkCoord} loaded successfully.");
                }
            }

            Debug.Log("Terrain loaded from " + filePath);
        }
        else
        {
            Debug.LogWarning("Save file not found at " + filePath);
        }
    }

    // New method to remove existing terrain chunks
    private void RemoveExistingChunks()
    {
        foreach (var chunk in terrainChunkDictionary.Values)
        {
            Destroy(chunk.meshObject); // Destroy the chunk GameObject
        }

        terrainChunkDictionary.Clear(); // Clear the dictionary
    }
}

[System.Serializable]
public class TerrainSaveData
{
    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public List<TerrainChunkData> terrainChunks;
}
