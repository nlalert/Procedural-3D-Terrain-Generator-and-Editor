using UnityEngine;
using System.Collections.Generic;

public class TerrainDeformer : MonoBehaviour
{
    public float deformRadius = 10f;   // Radius of deformation
    public float deformSpeed = 5f;     // Speed of height increase

    public TerrainGenerator terrainGenerator;  // Reference to the terrain generator

    void Update()
    {
        // Check if the left mouse button is held
        if (Input.GetMouseButton(0))
        {
            HandleTerrainDeformation();
        }
    }

    void HandleTerrainDeformation()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Perform a raycast to detect the terrain
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.CompareTag("Terrain"))
            {
                // Modify terrain chunks in the deformation radius
                ModifySurroundingChunks(hit.point);
            }
        }
    }

    // Find all chunks in the deformation radius
    void ModifySurroundingChunks(Vector3 hitPoint)
    {
        // Find the chunk the user clicked on
        Vector2 chunkCoord = new Vector2(Mathf.Floor(hitPoint.x / terrainGenerator.meshSettings.meshWorldSize),
                                         Mathf.Floor(hitPoint.z / terrainGenerator.meshSettings.meshWorldSize));

        List<TerrainChunk> chunksToModify = new List<TerrainChunk>();

        // Add the main chunk
        if (terrainGenerator.terrainChunkDictionary.TryGetValue(chunkCoord, out TerrainChunk mainChunk))
        {
            chunksToModify.Add(mainChunk);
        }

        // Check neighboring chunks in all directions
        Vector2[] neighborOffsets = {
            new Vector2(-1, 0), new Vector2(1, 0),  // Left and Right
            new Vector2(0, -1), new Vector2(0, 1),  // Back and Front
            new Vector2(-1, -1), new Vector2(1, 1), // Diagonal chunks
            new Vector2(-1, 1), new Vector2(1, -1)
        };

        foreach (Vector2 offset in neighborOffsets)
        {
            Vector2 neighborCoord = chunkCoord + offset;
            if (terrainGenerator.terrainChunkDictionary.TryGetValue(neighborCoord, out TerrainChunk neighborChunk))
            {
                chunksToModify.Add(neighborChunk);
            }
        }

        // Modify vertices in all the relevant chunks
        foreach (TerrainChunk chunk in chunksToModify)
        {
            ModifyVertices(chunk, hitPoint);
        }
    }

    void ModifyVertices(TerrainChunk terrainChunk, Vector3 hitPoint)
    {
        // Get the mesh and vertex data
        Mesh mesh = terrainChunk.meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;

        // Convert hit point to local space of the chunk
        Vector3 localHitPoint = hitPoint - terrainChunk.meshObject.transform.position;

        // Loop through vertices to find those within the deform radius
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertexWorldPos = terrainChunk.meshObject.transform.TransformPoint(vertices[i]);
            float distance = Vector3.Distance(vertexWorldPos, hitPoint);

            if (distance < deformRadius)
            {
                // Modify vertex height (increase gradually)
                vertices[i].y += deformSpeed * Time.deltaTime;
            }
        }

        // Update the mesh with the new vertices
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        terrainChunk.meshFilter.mesh = mesh;

        // If using a mesh collider, update it as well
        terrainChunk.meshCollider.sharedMesh = mesh;
    }
}
