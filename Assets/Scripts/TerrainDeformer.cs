using UnityEngine;
using System.Collections.Generic;

public class TerrainDeformer : MonoBehaviour
{
    public float deformRadius = 10f;   // Radius of deformation
    public float deformSpeed = 5f;     // Speed of height increase
    public float smoothingSpeed = 2f;  // Speed of terrain smoothing

    public TerrainGenerator terrainGenerator;  // Reference to the terrain generator

    void Update()
    {
        // Check if the right mouse button is held for smoothing
        if (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftControl))
        {
            HandleTerrainSmoothing();
        }
        // Check if the left mouse button is held for deformation
        else if (Input.GetMouseButton(0))
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
                ModifySurroundingChunks(hit.point, isSmoothing: false);
            }
        }
    }

    void HandleTerrainSmoothing()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Perform a raycast to detect the terrain
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.CompareTag("Terrain"))
            {
                // Smooth terrain chunks in the radius
                ModifySurroundingChunks(hit.point, isSmoothing: true);
            }
        }
    }

    // Find all chunks in the deformation or smoothing radius
    void ModifySurroundingChunks(Vector3 hitPoint, bool isSmoothing)
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
            if (isSmoothing)
            {
                SmoothVertices(chunksToModify, hitPoint);  // Send all chunks for cross-chunk smoothing
            }
            else
            {
                ModifyVertices(chunk, hitPoint);
            }
        }
    }

    void ModifyVertices(TerrainChunk terrainChunk, Vector3 hitPoint)
    {
        // Get the mesh and vertex data
        Mesh mesh = terrainChunk.meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;

        // List to store affected vertex indices
        HashSet<int> affectedVertexIndices = new HashSet<int>();

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
                affectedVertexIndices.Add(i);  // Track affected vertex
            }
        }

        // Update the mesh with the new vertices
        mesh.vertices = vertices;

        // Recalculate normals only for affected vertices
        RecalculateNormalsForAffectedVertices(mesh, affectedVertexIndices);

        // If using a mesh collider, update it as well
        terrainChunk.meshCollider.sharedMesh = mesh;
    }

    void RecalculateNormalsForAffectedVertices(Mesh mesh, HashSet<int> affectedVertexIndices)
    {
        Vector3[] normals = mesh.normals;
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        // Iterate over triangles and recalculate normals only for affected vertices
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v0 = triangles[i];
            int v1 = triangles[i + 1];
            int v2 = triangles[i + 2];

            // Only recalculate the normal if at least one vertex in the triangle is affected
            if (affectedVertexIndices.Contains(v0) || affectedVertexIndices.Contains(v1) || affectedVertexIndices.Contains(v2))
            {
                Vector3 normal = CalculateNormal(vertices[v0], vertices[v1], vertices[v2]);

                normals[v0] = normal;
                normals[v1] = normal;
                normals[v2] = normal;
            }
        }

        // Update mesh normals
        mesh.normals = normals;
    }

    Vector3 CalculateNormal(Vector3 v0, Vector3 v1, Vector3 v2)
    {
        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;
        return Vector3.Cross(edge1, edge2).normalized;
    }

    void SmoothVertices(List<TerrainChunk> terrainChunks, Vector3 hitPoint)
    {
        // A list to store all vertices and their world positions within the radius
        List<Vector3> verticesInRadius = new List<Vector3>();
        List<Vector3> worldPositionsInRadius = new List<Vector3>();

        // Collect vertices from all chunks within the smoothing radius
        foreach (TerrainChunk chunk in terrainChunks)
        {
            Mesh mesh = chunk.meshFilter.mesh;
            Vector3[] vertices = mesh.vertices;

            // Convert hit point to local space of the chunk
            Vector3 localHitPoint = hitPoint - chunk.meshObject.transform.position;

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

        // Calculate the average height of all vertices within the radius
        float averageHeight = 0f;
        foreach (Vector3 vertex in verticesInRadius)
        {
            averageHeight += vertex.y;
        }
        averageHeight /= verticesInRadius.Count;  // Get the average height

        // Smooth each chunk's vertices based on the average height
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
                    // Smooth the vertex height by moving it towards the average height
                    vertices[i].y = Mathf.Lerp(vertices[i].y, averageHeight, smoothingSpeed * Time.deltaTime);
                    affectedVertexIndices.Add(i);  // Track affected vertex
                }
            }

            // Update the mesh with the new vertices
            mesh.vertices = vertices;

            // Recalculate normals only for affected vertices
            RecalculateNormalsForAffectedVertices(mesh, affectedVertexIndices);

            // If using a mesh collider, update it as well
            chunk.meshCollider.sharedMesh = mesh;
        }
    }
}
