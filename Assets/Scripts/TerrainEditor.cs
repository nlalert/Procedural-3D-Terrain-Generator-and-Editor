using UnityEngine;
using System.Collections.Generic;

public class TerrainDeformer : MonoBehaviour
{
    public enum TerrainTool { None, IncreaseHeight, DecreaseHeight, Smooth }
    public TerrainTool currentTool = TerrainTool.None;

    public float deformRadius = 10f;   // Radius within which the terrain will be deformed or smoothed
    public float deformSpeed = 5f;     // Speed for deformation (positive for increase, negative for decrease)
    public float smoothingSpeed = 2f;  // Speed at which terrain vertices are smoothed

    public TerrainGenerator terrainGenerator;  // Reference to the terrain generator that holds the chunks

    // Tool selection methods
    public void SetIncreaseHeightTool() {
        currentTool = TerrainTool.IncreaseHeight;
        deformSpeed = Mathf.Abs(deformSpeed);  // Ensure the speed is positive
    }

    public void SetDecreaseHeightTool() {
        currentTool = TerrainTool.DecreaseHeight;
        deformSpeed = -Mathf.Abs(deformSpeed);  // Set deform speed to negative
    }

    public void SetSmoothTool() {
        currentTool = TerrainTool.Smooth;
    }

    void Update()
    {
        if ((currentTool == TerrainTool.IncreaseHeight || currentTool == TerrainTool.DecreaseHeight) && Input.GetMouseButton(0))
        {
            PerformTerrainDeformation();  // Deform terrain
        }
        else if (currentTool == TerrainTool.Smooth && Input.GetMouseButton(0))
        {
            PerformTerrainSmoothing();  // Smooth terrain
        }
    }

    // Handle terrain deformation based on raycasting from the mouse position
    void PerformTerrainDeformation()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Perform a raycast to detect the terrain where the mouse is pointing
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.CompareTag("Terrain"))
            {
                // Modify terrain chunks in the deformation radius around the hit point
                ModifyTerrainInChunks(hit.point, isSmoothing: false);
            }
        }
    }

    // Handle terrain smoothing based on raycasting from the mouse position
    void PerformTerrainSmoothing()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Perform a raycast to detect the terrain where the mouse is pointing
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.CompareTag("Terrain"))
            {
                // Smooth terrain chunks in the smoothing radius around the hit point
                ModifyTerrainInChunks(hit.point, isSmoothing: true);
            }
        }
    }

    // Modify the vertices of chunks in the surrounding area based on the hit point
    void ModifyTerrainInChunks(Vector3 hitPoint, bool isSmoothing)
    {
        // Determine the chunk the user clicked on based on the hit point
        Vector2 chunkCoord = new Vector2(Mathf.Floor(hitPoint.x / terrainGenerator.meshSettings.meshWorldSize),
                                         Mathf.Floor(hitPoint.z / terrainGenerator.meshSettings.meshWorldSize));

        List<TerrainChunk> chunksToModify = new List<TerrainChunk>();

        // Add the main chunk where the hit point is located
        if (terrainGenerator.terrainChunkDictionary.TryGetValue(chunkCoord, out TerrainChunk mainChunk))
        {
            chunksToModify.Add(mainChunk);
        }

        // Check neighboring chunks in all 8 directions (left, right, back, front, and diagonals)
        Vector2[] neighborOffsets = {
            new Vector2(-1, 0), new Vector2(1, 0),  // Left and Right
            new Vector2(0, -1), new Vector2(0, 1),  // Back and Front
            new Vector2(-1, -1), new Vector2(1, 1), // Diagonal chunks
            new Vector2(-1, 1), new Vector2(1, -1)
        };

        // Add neighboring chunks to the list of chunks to modify
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
            if (isSmoothing)
            {
                SmoothChunkVertices(chunksToModify, hitPoint);  // Smooth vertices across all chunks
            }
            else
            {
                DeformChunkVertices(chunk, hitPoint);  // Deform vertices in the chunk
            }
        }
    }

    // Modify the vertices of a specific chunk for terrain deformation
    void DeformChunkVertices(TerrainChunk terrainChunk, Vector3 hitPoint)
    {
        // Get the mesh and vertex data of the chunk
        Mesh mesh = terrainChunk.meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;

        // List to store the indices of affected vertices
        HashSet<int> affectedVertexIndices = new HashSet<int>();

        // Convert the hit point to the local space of the chunk
        Vector3 localHitPoint = hitPoint - terrainChunk.meshObject.transform.position;

        // Loop through the vertices and modify those within the deform radius
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertexWorldPos = terrainChunk.meshObject.transform.TransformPoint(vertices[i]);
            float distance = Vector3.Distance(vertexWorldPos, hitPoint);

            if (distance < deformRadius)
            {
                // Modify the vertex height based on the deform speed (positive for increase, negative for decrease)
                vertices[i].y += deformSpeed * Time.deltaTime;
                affectedVertexIndices.Add(i);  // Track the affected vertex
            }
        }

        // Update the mesh with the new vertex positions
        mesh.vertices = vertices;

        // Recalculate normals only for the affected vertices
        RecalculateNormalsForAffectedVertices(mesh, affectedVertexIndices);

        // If using a mesh collider, update it with the modified mesh
        terrainChunk.meshCollider.sharedMesh = mesh;
    }

    // Recalculate normals for affected vertices to ensure smooth shading
    void RecalculateNormalsForAffectedVertices(Mesh mesh, HashSet<int> affectedVertexIndices)
    {
        Vector3[] normals = mesh.normals;
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        // Iterate over triangles and recalculate normals for affected vertices
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v0 = triangles[i];
            int v1 = triangles[i + 1];
            int v2 = triangles[i + 2];

            // Recalculate normals if at least one vertex in the triangle is affected
            if (affectedVertexIndices.Contains(v0) || affectedVertexIndices.Contains(v1) || affectedVertexIndices.Contains(v2))
            {
                Vector3 normal = CalculateTriangleNormal(vertices[v0], vertices[v1], vertices[v2]);

                // Update normals for the affected vertices
                normals[v0] = normal;
                normals[v1] = normal;
                normals[v2] = normal;
            }
        }

        // Update the mesh normals
        mesh.normals = normals;
    }

    // Calculate the normal for a triangle formed by three vertices
    Vector3 CalculateTriangleNormal(Vector3 v0, Vector3 v1, Vector3 v2)
    {
        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;
        return Vector3.Cross(edge1, edge2).normalized;  // Return the normalized normal vector
    }

    // Smooth the vertices across all affected chunks
    void SmoothChunkVertices(List<TerrainChunk> terrainChunks, Vector3 hitPoint)
    {
        // List to store vertices and their world positions within the smoothing radius
        List<Vector3> verticesInRadius = new List<Vector3>();
        List<Vector3> worldPositionsInRadius = new List<Vector3>();

        // Collect vertices from all chunks within the smoothing radius
        foreach (TerrainChunk chunk in terrainChunks)
        {
            Mesh mesh = chunk.meshFilter.mesh;
            Vector3[] vertices = mesh.vertices;

            // Convert hit point to local space of the chunk
            Vector3 localHitPoint = hitPoint - chunk.meshObject.transform.position;

            // Loop through vertices and add those within the smoothing radius
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertexWorldPos = chunk.meshObject.transform.TransformPoint(vertices[i]);
                float distance = Vector3.Distance(vertexWorldPos, hitPoint);

                if (distance < deformRadius)
                {
                    verticesInRadius.Add(vertices[i]);
                    worldPositionsInRadius.Add(vertexWorldPos);
                }
            }
        }

        // Calculate the average height of all vertices within the smoothing radius
        float averageHeight = 0f;
        foreach (Vector3 vertex in verticesInRadius)
        {
            averageHeight += vertex.y;
        }
        averageHeight /= verticesInRadius.Count;  // Get the average height

        // Smooth each chunk's vertices by adjusting them towards the average height
        foreach (TerrainChunk chunk in terrainChunks)
        {
            Mesh mesh = chunk.meshFilter.mesh;
            Vector3[] vertices = mesh.vertices;

            HashSet<int> affectedVertexIndices = new HashSet<int>();

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertexWorldPos = chunk.meshObject.transform.TransformPoint(vertices[i]);
                float distance = Vector3.Distance(vertexWorldPos, hitPoint);

                if (distance < deformRadius)
                {
                    // Smooth the vertex height by interpolating towards the average height
                    vertices[i].y = Mathf.Lerp(vertices[i].y, averageHeight, smoothingSpeed * Time.deltaTime);
                    affectedVertexIndices.Add(i);  // Track the affected vertex
                }
            }

            // Update the mesh with the smoothed vertex positions
            mesh.vertices = vertices;

            // Recalculate normals for affected vertices
            RecalculateNormalsForAffectedVertices(mesh, affectedVertexIndices);

            // If using a mesh collider, update it with the modified mesh
            chunk.meshCollider.sharedMesh = mesh;
        }
    }
}
