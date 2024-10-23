using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public int colliderLODIndex;
    public LODInfo[] detailLevels;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureSettings textureSettings;

    public Transform viewer;
    public Material mapMaterial;

    Vector2 viewerPosition;
    Vector2 viewerPositionOld;

    float meshWorldSize;
    int chunksVisibleInViewDst;

    public GameObject waterPlanePrefab; // Assign a water plane prefab in the inspector

    private GameObject waterPlane; // The water plane instance

    public Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();
    void Start() {
        textureSettings.ApplyToMaterial(mapMaterial);
        textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

        float maxViewDst = detailLevels[detailLevels.Length-1].visibleDstThreshold;
        meshWorldSize = meshSettings.meshWorldSize;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst/meshWorldSize);
        
        // Create the water plane
        CreateWaterPlane();

        UpdateVisibleChunks();
    }

    void Update() {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        if(viewerPosition != viewerPositionOld){
            foreach (TerrainChunk terrainChunk in visibleTerrainChunks)
            {
                terrainChunk.UpdateCollisionMesh();
            }
        }

        if((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate){
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks() {
        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();

        // Set all previously visible chunks to be updated
        for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--) {
            alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
            visibleTerrainChunks[i].UpdateTerrainChunk();
        }

        // Calculate the current chunk coordinate of the viewer
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

        // Generate terrain chunks within the bounds of (-mapSize, -mapSize) to (mapSize, mapSize)
        for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
                Vector2 viewChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                // Ensure chunk coordinates are within the specified map bounds
                if (viewChunkCoord.x >= -meshSettings.mapAreaLevel && viewChunkCoord.x <= meshSettings.mapAreaLevel && viewChunkCoord.y >= -meshSettings.mapAreaLevel && viewChunkCoord.y <= meshSettings.mapAreaLevel) {
                    
                    // If this chunk hasn't been updated in this frame yet
                    if (!alreadyUpdatedChunkCoords.Contains(viewChunkCoord)) {
                        if (terrainChunkDictionary.ContainsKey(viewChunkCoord)) {
                            terrainChunkDictionary[viewChunkCoord].UpdateTerrainChunk();
                        } else {
                            // Create a new terrain chunk if it doesn't exist yet
                            TerrainChunk newChunk = new TerrainChunk(viewChunkCoord, heightMapSettings, meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial);
                            terrainChunkDictionary.Add(viewChunkCoord, newChunk);
                            newChunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;
                            newChunk.Load();
                        }
                    }
                }
            }
        }
    }

    void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible){
        if(isVisible){
            visibleTerrainChunks.Add(chunk);
        }
        else{
            visibleTerrainChunks.Remove(chunk);
        }
    }

     // Function to create the water plane
    void CreateWaterPlane() {
        // Instantiate water plane prefab
        waterPlane = Instantiate(waterPlanePrefab, Vector3.zero, Quaternion.identity);

        // Calculate the total size of the terrain based on mapAreaLevel
        float terrainSize = (meshSettings.mapAreaLevel + 0.5f) * meshSettings.meshWorldSize * 2;

        // Set the water plane size to cover the whole terrain
        waterPlane.transform.localScale = new Vector3(terrainSize / 10f, 1, terrainSize / 10f); // Assuming the default plane is 10x10 units

        // Position the water plane at y=0 and center it
        waterPlane.transform.position = new Vector3(0, textureSettings.layers[1].startHeight * heightMapSettings.heightMultiplier, 0);
    }
}
