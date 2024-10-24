using UnityEngine;

public class TerrainChunk {
    public Vector2 coord;   // Coordinates of the chunk

    public GameObject meshObject;   // GameObject representing the terrain chunk
    Vector2 sampleCenter;   // Center point for sampling height map data

    MeshRenderer meshRenderer;   // Mesh renderer for the chunk
    public MeshFilter meshFilter;   // Mesh filter to hold the mesh
    public MeshCollider meshCollider;   // Mesh collider for physics interactions

    HeightMap heightMap;   // The height map data for the terrain chunk
    bool heightMapReceived;   // Flag to indicate if the height map has been received

    HeightMapSettings heightMapSettings;   // Settings for the height map generation
    MeshSettings meshSettings;   // Settings for the mesh generation

    // Constructor for TerrainChunk class
    public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, Transform parent, Material material) {
        this.coord = coord;
        this.heightMapSettings = heightMapSettings;
        this.meshSettings = meshSettings;

        // Calculate the sample center and set the chunk's position and bounds
        sampleCenter = coord * meshSettings.meshWorldSize / meshSettings.meshScale;
        Vector2 position = coord * meshSettings.meshWorldSize;

        // Create the GameObject and assign components for mesh rendering and collision
        meshObject = new GameObject("Terrain Chunk");
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>(); // Immediately add MeshCollider
        meshRenderer.material = material;

        // Set the position of the mesh object in the scene
        meshObject.transform.position = new Vector3(position.x, 0, position.y);
        meshObject.transform.parent = parent;

        // Assign the "Terrain" tag to the mesh object
        meshObject.tag = "Terrain";
    }

    // Load the height map data for this chunk
    public void Load() {
        ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, sampleCenter), OnHeightMapReceived);
    }

    // Callback function when the height map is received
    void OnHeightMapReceived(object heightMapObject) {
        heightMap = (HeightMap) heightMapObject;
        heightMapReceived = true;

        GenerateMesh();   // Generate the mesh once the height map is received
    }

    // Generate the mesh for the terrain chunk
    void GenerateMesh() {
        if (heightMapReceived) {
            RequestMesh();   // Request the mesh immediately after the height map is received
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
        meshCollider.sharedMesh = meshFilter.mesh; // Immediately assign the generated mesh to the collider
    }
}
