using System.Collections;
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
}
