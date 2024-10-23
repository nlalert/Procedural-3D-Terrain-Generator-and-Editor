using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk {
    const float colliderGenerationDistanceThreshold = 1000;   // Threshold distance for generating colliders
    public event System.Action<TerrainChunk, bool> onVisibilityChanged;  // Event triggered when visibility changes
    public Vector2 coord;   // Coordinates of the chunk
    
    public GameObject meshObject;   // GameObject representing the terrain chunk
    Vector2 sampleCenter;   // Center point for sampling height map data
    Bounds bounds;   // Bounding box for the chunk

    MeshRenderer meshRenderer;   // Mesh renderer for the chunk
    public MeshFilter meshFilter;   // Mesh filter to hold the mesh
    public MeshCollider meshCollider;   // Mesh collider for physics interactions

    LODInfo[] detailLevels;   // Array of level of detail (LOD) information
    LODMesh[] lodMeshes;   // Array of LOD meshes for the terrain chunk
    int colliderLODIndex;   // Index for the LOD to be used for collision mesh

    HeightMap heightMap;   // The height map data for the terrain chunk
    bool heightMapReceived;   // Flag to indicate if the height map has been received
    int previousLODIndex = -1;   // Index of the previous LOD used for rendering
    bool hasSetCollider;   // Flag to indicate if the collider has been set
    float maxViewDst;   // Maximum view distance for the chunk to be visible

    HeightMapSettings heightMapSettings;   // Settings for the height map generation
    MeshSettings meshSettings;   // Settings for the mesh generation
    Transform viewer;   // Reference to the viewer (usually the player/camera)

    // Constructor for TerrainChunk class
    public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material) {
        this.coord = coord;
        this.detailLevels = detailLevels;
        this.colliderLODIndex = colliderLODIndex;
        this.heightMapSettings = heightMapSettings;
        this.meshSettings = meshSettings;
        this.viewer = viewer;

        // Calculate the sample center and set the chunk's position and bounds
        sampleCenter = coord * meshSettings.meshWorldSize / meshSettings.meshScale;
        Vector2 position = coord * meshSettings.meshWorldSize;
        bounds = new Bounds(position, Vector2.one * meshSettings.meshWorldSize);

        // Create the GameObject and assign components for mesh rendering and collision
        meshObject = new GameObject("Terrain Chunk");
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();
        meshRenderer.material = material;

        // Set the position of the mesh object in the scene
        meshObject.transform.position = new Vector3(position.x, 0, position.y);
        meshObject.transform.parent = parent;

        // Assign the "Terrain" tag to the mesh object
        meshObject.tag = "Terrain";

        // Initially set the terrain chunk as not visible
        SetVisible(false);
        
        // Initialize the LOD meshes and set up update callbacks
        lodMeshes = new LODMesh[detailLevels.Length];
        for (int i = 0; i < detailLevels.Length; i++) {
            lodMeshes[i] = new LODMesh(detailLevels[i].lod);
            lodMeshes[i].updateCallback += UpdateTerrainChunk;
            if (i == colliderLODIndex) {
                lodMeshes[i].updateCallback += UpdateCollisionMesh;   // Update collision mesh for the LOD used for collisions
            }
        }

        // Set the maximum view distance to the last LOD's visible distance threshold
        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
    }

    // Load the height map data for this chunk
    public void Load() {
        ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, sampleCenter), OnHeightMapReceived);
    }
    
    // Callback function when the height map is received
    void OnHeightMapReceived(object heightMapObject) {
        this.heightMap = (HeightMap)heightMapObject;
        heightMapReceived = true;

        UpdateTerrainChunk();   // Update the terrain chunk after height map is received
    }

    // Property to get the viewer's position in 2D space
    Vector2 viewerPosition {
        get {
            return new Vector2(viewer.position.x, viewer.position.z);
        }
    }

    // Update the terrain chunk's visibility and LOD based on the viewer's distance
    public void UpdateTerrainChunk() {
        if (heightMapReceived) {
            // Calculate distance from the viewer to the nearest edge of the chunk
            float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));   // Smallest distance

            bool wasVisible = IsVisible();   // Check if the chunk was previously visible
            bool visible = viewerDstFromNearestEdge <= maxViewDst;   // Determine if the chunk should be visible

            if (visible) {
                int lodIndex = 0;
                // Determine the appropriate LOD based on the viewer's distance
                for (int i = 0; i < detailLevels.Length - 1; i++) {
                    if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold) {
                        lodIndex = i + 1;
                    } else {
                        break;
                    }
                }

                // Update the mesh if the LOD has changed
                if (lodIndex != previousLODIndex) {
                    LODMesh lodMesh = lodMeshes[lodIndex];
                    if (lodMesh.hasMesh) {
                        previousLODIndex = lodIndex;
                        meshFilter.mesh = lodMesh.mesh;   // Assign the mesh for the current LOD
                    } else if (!lodMesh.hasRequestedMesh) {
                        lodMesh.RequestMesh(heightMap, meshSettings);   // Request mesh generation if not already done
                    }
                }
            }
            if (wasVisible != visible) {
                SetVisible(visible);   // Set the visibility of the chunk
                if (onVisibilityChanged != null) {
                    onVisibilityChanged(this, visible);   // Trigger visibility change event
                }
            }
        }
    }

    // Update the collision mesh for the chunk
    public void UpdateCollisionMesh() {
        if (!hasSetCollider) {
            float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

            // Request the LOD mesh for collisions if the viewer is close enough
            if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDstThreshold) {
                if (!lodMeshes[colliderLODIndex].hasRequestedMesh) {
                    lodMeshes[colliderLODIndex].RequestMesh(heightMap, meshSettings);
                }
            }

            // Set the collider mesh if the viewer is within the generation threshold
            if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold) {
                if (lodMeshes[colliderLODIndex].hasMesh) {
                    meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;   // Set the collision mesh
                    hasSetCollider = true;
                }
            }
        }
    }

    // Set the visibility of the terrain chunk
    public void SetVisible(bool visible) {
        meshObject.SetActive(visible);
    }

    // Check if the terrain chunk is currently visible
    public bool IsVisible() {
        return meshObject.activeSelf;
    }
}

// Class to manage the LOD meshes for the terrain chunk
class LODMesh {
    public Mesh mesh;   // The mesh for this LOD
    public bool hasRequestedMesh;   // Flag to indicate if the mesh has been requested
    public bool hasMesh;   // Flag to indicate if the mesh has been generated
    int lod;   // Level of detail (LOD) index
    public event System.Action updateCallback;   // Callback to update the terrain chunk when the mesh is ready

    public LODMesh(int lod) {
        this.lod = lod;
    }

    // Callback function when mesh data is received
    void OnMeshDataReceived(object meshDataObject) {
        mesh = ((MeshData)meshDataObject).CreateMesh();   // Create the mesh from the received data
        hasMesh = true;

        updateCallback();   // Trigger the update callback to update the terrain chunk
    }

    // Request mesh generation for this LOD
    public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings) {
        hasRequestedMesh = true;
        ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, lod), OnMeshDataReceived);
    }
}

// Struct to store LOD information
[System.Serializable]
public struct LODInfo {
    [Range(0, MeshSettings.numSupportedLODS - 1)]
    public int lod;   // LOD index
    public float visibleDstThreshold;   // Visible distance threshold for this LOD

    // Square of the visible distance threshold (used for efficiency)
    public float sqrVisibleDstThreshold {
        get {
            return visibleDstThreshold * visibleDstThreshold;
        }
    }
}
