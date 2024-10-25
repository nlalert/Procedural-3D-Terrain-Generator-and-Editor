using UnityEngine;
using System.Collections.Generic;

public class TerrainDeformer : MonoBehaviour
{
    public enum TerrainTool { None, IncreaseHeight, IncreaseHeightGaussian, DecreaseHeight, DecreaseHeightGaussian, Smooth }
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

    public void SetIncreaseHeightGaussianTool() {
        currentTool = TerrainTool.IncreaseHeightGaussian;
        deformSpeed = Mathf.Abs(deformSpeed);  // Ensure the speed is positive
    }

    public void SetDecreaseHeightGaussianTool() {
        currentTool = TerrainTool.DecreaseHeightGaussian;
        deformSpeed = -Mathf.Abs(deformSpeed);  // Set deform speed to negative
    }

    public void SetSmoothTool() {
        currentTool = TerrainTool.Smooth;
    }

    void Update()
    {
        if ((currentTool == TerrainTool.IncreaseHeight || currentTool == TerrainTool.DecreaseHeight) && Input.GetMouseButton(0))
        {
            PerformTerrainDeformation();
        }
        else if ((currentTool == TerrainTool.IncreaseHeightGaussian || currentTool == TerrainTool.DecreaseHeightGaussian) && Input.GetMouseButton(0))
        {
            PerformGaussianTerrainDeformation();
        }
        else if (currentTool == TerrainTool.Smooth && Input.GetMouseButton(0))
        {
            PerformTerrainSmoothing();
        }
    }

    // Handle regular terrain deformation based on raycasting from the mouse position
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
                ModifyTerrainInChunks(hit.point, isSmoothing: false, isGaussian: false);
            }
        }
    }

    // Handle Gaussian terrain deformation based on raycasting from the mouse position
    void PerformGaussianTerrainDeformation()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Perform a raycast to detect the terrain where the mouse is pointing
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.CompareTag("Terrain"))
            {
                // Modify terrain chunks in the Gaussian deformation radius around the hit point
                ModifyTerrainInChunks(hit.point, isSmoothing: false, isGaussian: true);
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
    void ModifyTerrainInChunks(Vector3 hitPoint, bool isSmoothing = false, bool isGaussian = false)
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
            new Vector2(-1, 0), new Vector2(1, 0),
            new Vector2(0, -1), new Vector2(0, 1),
            new Vector2(-1, -1), new Vector2(1, 1),
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
                SmoothChunkVertices(chunksToModify, hitPoint);  // Smooth vertices across all chunks
            }
            else if (isGaussian)
            {
                DeformChunkVerticesGaussian(chunk, hitPoint);  // Apply Gaussian deformation
            }
            else
            {
                DeformChunkVertices(chunk, hitPoint);  // Apply linear deformation
            }
        }
    }

    // Modify the vertices of a specific chunk for terrain deformation
    void DeformChunkVertices(TerrainChunk terrainChunk, Vector3 hitPoint)
    {
        // Get the mesh and vertex data of the chunk
        Mesh mesh = terrainChunk.meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;

        HashSet<int> affectedVertexIndices = new HashSet<int>();

        Vector3 localHitPoint = hitPoint - terrainChunk.meshObject.transform.position;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertexWorldPos = terrainChunk.meshObject.transform.TransformPoint(vertices[i]);
            float distance = Vector3.Distance(vertexWorldPos, hitPoint);

            if (distance < deformRadius)
            {
                vertices[i].y += deformSpeed * Time.deltaTime;
                affectedVertexIndices.Add(i);
            }
        }

        mesh.vertices = vertices;
        RecalculateNormalsForAffectedVertices(mesh, affectedVertexIndices);
        terrainChunk.meshCollider.sharedMesh = mesh;
    }

    void DeformChunkVerticesGaussian(TerrainChunk terrainChunk, Vector3 hitPoint)
    {
        Mesh mesh = terrainChunk.meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;

        HashSet<int> affectedVertexIndices = new HashSet<int>();

        Vector3 localHitPoint = hitPoint - terrainChunk.meshObject.transform.position;
        float sigma = deformRadius / 2f;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertexWorldPos = terrainChunk.meshObject.transform.TransformPoint(vertices[i]);
            float distance = Vector3.Distance(vertexWorldPos, hitPoint);

            if (distance < deformRadius)
            {
                float gaussianMultiplier = Mathf.Exp(-Mathf.Pow(distance, 2) / (2 * Mathf.Pow(sigma, 2)));
                vertices[i].y += deformSpeed * gaussianMultiplier * Time.deltaTime;
                affectedVertexIndices.Add(i);
            }
        }

        mesh.vertices = vertices;
        RecalculateNormalsForAffectedVertices(mesh, affectedVertexIndices);
        terrainChunk.meshCollider.sharedMesh = mesh;
    }

    // Recalculate normals for affected vertices to ensure smooth shading
    void RecalculateNormalsForAffectedVertices(Mesh mesh, HashSet<int> affectedVertexIndices)
    {
        Vector3[] normals = mesh.normals;
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v0 = triangles[i];
            int v1 = triangles[i + 1];
            int v2 = triangles[i + 2];

            if (affectedVertexIndices.Contains(v0) || affectedVertexIndices.Contains(v1) || affectedVertexIndices.Contains(v2))
            {
                Vector3 normal = CalculateTriangleNormal(vertices[v0], vertices[v1], vertices[v2]);

                normals[v0] = normal;
                normals[v1] = normal;
                normals[v2] = normal;
            }
        }

        mesh.normals = normals;
    }

    Vector3 CalculateTriangleNormal(Vector3 v0, Vector3 v1, Vector3 v2)
    {
        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;
        return Vector3.Cross(edge1, edge2).normalized;
    }

    // Smooth the vertices across all affected chunks
    void SmoothChunkVertices(List<TerrainChunk> terrainChunks, Vector3 hitPoint)
    {
        List<Vector3> verticesInRadius = new List<Vector3>();
        List<Vector3> worldPositionsInRadius = new List<Vector3>();

        foreach (TerrainChunk chunk in terrainChunks)
        {
            Mesh mesh = chunk.meshFilter.mesh;
            Vector3[] vertices = mesh.vertices;

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

        // Smooth the heights by averaging them
        float averageHeight = 0f;
        foreach (Vector3 vertex in verticesInRadius)
        {
            averageHeight += vertex.y;
        }
        averageHeight /= verticesInRadius.Count;

        for (int i = 0; i < terrainChunks.Count; i++)
        {
            Mesh mesh = terrainChunks[i].meshFilter.mesh;
            Vector3[] vertices = mesh.vertices;

            for (int j = 0; j < vertices.Length; j++)
            {
                Vector3 vertexWorldPos = terrainChunks[i].meshObject.transform.TransformPoint(vertices[j]);
                float distance = Vector3.Distance(vertexWorldPos, hitPoint);

                if (distance < deformRadius)
                {
                    vertices[j].y = Mathf.Lerp(vertices[j].y, averageHeight, smoothingSpeed * Time.deltaTime);
                }
            }

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            terrainChunks[i].meshCollider.sharedMesh = mesh;
        }
    }
}
