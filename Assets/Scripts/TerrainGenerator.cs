using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    // Threshold for updating chunks when the viewer moves
    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate; // Square of the threshold for faster comparisons

    // Public fields to set LOD (Level of Detail), settings, and references
    public int colliderLODIndex;
    public LODInfo[] detailLevels; // Array of LOD levels

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureSettings textureSettings;

    public Transform viewer; // Reference to the player's position (viewer)
    public Material mapMaterial; // Material used for the terrain

    Vector2 viewerPosition; // Current position of the viewer
    Vector2 viewerPositionOld; // Previous position of the viewer to detect movement

    float meshWorldSize; // Size of each terrain chunk in world units
    int chunksVisibleInViewDst; // Number of chunks visible from the viewer's perspective

    public GameObject waterPlanePrefab; // Assign a water plane prefab in the inspector

    private GameObject waterPlane; // The water plane instance

    // Dictionary to store generated terrain chunks
    public Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    // List to keep track of visible terrain chunks
    List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

    // Initialization of terrain generation
    void Start() {
        // Apply texture settings to the material
        textureSettings.ApplyToMaterial(mapMaterial);
        textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

        // Calculate the maximum view distance based on the last detail level's visible distance threshold
        float maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        meshWorldSize = meshSettings.meshWorldSize; // Get the size of the mesh in world units
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / meshWorldSize); // Calculate the number of visible chunks

        // Create the water plane on top of the terrain
        CreateWaterPlane();

        // Initialize visible chunks
        UpdateVisibleChunks();
    }

    // Called every frame to update visible chunks based on viewer movement
    void Update() {
        // Update the viewer's current position in 2D space (x and z coordinates)
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        // If the viewer has moved, update the terrain chunks' collision meshes
        if (viewerPosition != viewerPositionOld) {
            foreach (TerrainChunk terrainChunk in visibleTerrainChunks) {
                terrainChunk.UpdateCollisionMesh();
            }
        }

        // Update terrain chunks if the viewer has moved beyond the update threshold
        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    // Update the visible terrain chunks
    void UpdateVisibleChunks() {
        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();

        // Loop through previously visible chunks and mark them for update
        for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--) {
            alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
            visibleTerrainChunks[i].UpdateTerrainChunk();
        }

        // Calculate the current chunk coordinates where the viewer is located
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

        // Loop through the surrounding chunks within the visible distance range
        for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
                Vector2 viewChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                // Ensure chunk coordinates are within the map bounds specified by meshSettings
                if (viewChunkCoord.x >= -meshSettings.mapAreaLevel && viewChunkCoord.x <= meshSettings.mapAreaLevel && viewChunkCoord.y >= -meshSettings.mapAreaLevel && viewChunkCoord.y <= meshSettings.mapAreaLevel) {
                    
                    // If this chunk hasn't been updated yet in the current frame
                    if (!alreadyUpdatedChunkCoords.Contains(viewChunkCoord)) {
                        if (terrainChunkDictionary.ContainsKey(viewChunkCoord)) {
                            // Update the existing terrain chunk
                            terrainChunkDictionary[viewChunkCoord].UpdateTerrainChunk();
                        } else {
                            // Create a new terrain chunk if it doesn't exist yet
                            TerrainChunk newChunk = new TerrainChunk(viewChunkCoord, heightMapSettings, meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial);
                            terrainChunkDictionary.Add(viewChunkCoord, newChunk);
                            newChunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged; // Subscribe to visibility change events
                            newChunk.Load(); // Load the terrain chunk data
                        }
                    }
                }
            }
        }
    }

    // Callback for when a terrain chunk's visibility changes
    void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible) {
        if (isVisible) {
            // Add chunk to visible list if it's now visible
            visibleTerrainChunks.Add(chunk);
        } else {
            // Remove chunk from visible list if it's no longer visible
            visibleTerrainChunks.Remove(chunk);
        }
    }

    // Function to create the water plane
    void CreateWaterPlane() {
        // Instantiate the water plane prefab
        waterPlane = Instantiate(waterPlanePrefab, Vector3.zero, Quaternion.identity);

        // Calculate the total size of the terrain based on the map area level
        float terrainSize = (meshSettings.mapAreaLevel + 0.5f) * meshSettings.meshWorldSize * 2;

        // Set the water plane's scale to cover the entire terrain
        waterPlane.transform.localScale = new Vector3(terrainSize / 10f, 1, terrainSize / 10f); // Assuming the default plane is 10x10 units

        // Position the water plane at y=0 and center it based on the height map's water level
        waterPlane.transform.position = new Vector3(0, textureSettings.layers[1].startHeight * heightMapSettings.heightMultiplier, 0);
    }
}
