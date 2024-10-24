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

    HeightMap heightMap;   // The height map data for the terrain chunk
    bool heightMapReceived;   // Flag to indicate if the height map has been received
    bool hasSetCollider;   // Flag to indicate if the collider has been set
    float maxViewDst = 1500;   // Maximum view distance for the chunk to be visible

    HeightMapSettings heightMapSettings;   // Settings for the height map generation
    MeshSettings meshSettings;   // Settings for the mesh generation
    Transform viewer;   // Reference to the viewer (usually the player/camera)

    // Constructor for TerrainChunk class
    public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, Transform parent, Transform viewer, Material material) {
        this.coord = coord;
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

    // Update the terrain chunk's visibility based on the viewer's distance
    public void UpdateTerrainChunk() {
        if (heightMapReceived) {
            // Calculate distance from the viewer to the nearest edge of the chunk
            float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));   // Smallest distance

            bool wasVisible = IsVisible();   // Check if the chunk was previously visible
            bool visible = viewerDstFromNearestEdge <= maxViewDst;   // Determine if the chunk should be visible

            if (visible && !meshFilter.sharedMesh) {
                RequestMesh();   // Request the mesh if not already generated
            }

            if (wasVisible != visible) {
                SetVisible(visible);   // Set the visibility of the chunk
                onVisibilityChanged?.Invoke(this, visible);   // Trigger visibility change event
            }
        }
    }

    // Request mesh generation for the terrain chunk
    void RequestMesh() {
        ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings), OnMeshDataReceived);
    }

    // Callback function when mesh data is received
    void OnMeshDataReceived(object meshDataObject) {
        MeshData meshData = (MeshData)meshDataObject;
        meshFilter.mesh = meshData.CreateMesh();   // Assign the generated mesh
        UpdateCollisionMesh();   // Update the collision mesh
    }

    // Update the collision mesh for the chunk
    public void UpdateCollisionMesh() {
        if (!hasSetCollider) {
            float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

            // Set the collider mesh if the viewer is within the generation threshold
            if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold) {
                meshCollider.sharedMesh = meshFilter.mesh;   // Set the collision mesh
                hasSetCollider = true;
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
