using System.Collections;
using UnityEngine;

public static class MeshGenerator {
    // Generate a terrain mesh using a height map and mesh settings.
    // Returns MeshData instead of Mesh so the mesh can be created outside of the thread.
    public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail) {
        // Determine the mesh simplification step based on the level of detail (LOD).
        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2; // step sizes: 1, 2, 4, 6, 8, 10, 12
        
        int borderedSize = heightMap.GetLength(0); // Size of the height map with borders
        int meshSize = borderedSize - 2 * meshSimplificationIncrement; // Size of the mesh after border removal
        int meshSizeUnsimplified = borderedSize - 2; // Size of the unsimplified mesh

        float halfWidth = (meshSizeUnsimplified - 1) / 2f; // Half width of the mesh
        float halfHeight = (meshSizeUnsimplified - 1) / 2f; // Half height of the mesh

        int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1; // Number of vertices per line after simplification

        // Initialize MeshData with the number of vertices and flat shading setting
        MeshData meshData = new MeshData(verticesPerLine, meshSettings.useFlatShading);

        // Keep track of vertex indices
        // Positive indices for mesh vertices, negative for border vertices
        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;

        // Assign indices to vertices, distinguishing between border and mesh vertices
        for (int x = 0; x < borderedSize; x += meshSimplificationIncrement) {
            for (int y = 0; y < borderedSize; y += meshSimplificationIncrement) {
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;

                if (isBorderVertex) {
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--; // Border vertices get negative indices
                } else {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++; // Mesh vertices get positive indices
                }
            }
        }

        // Loop over each vertex and set its position
        for (int x = 0; x < borderedSize; x += meshSimplificationIncrement) {
            for (int y = 0; y < borderedSize; y += meshSimplificationIncrement) {
                int vertexIndex = vertexIndicesMap[x, y];

                // Calculate UV coordinates based on vertex position
                Vector2 percent = new Vector2((x - meshSimplificationIncrement) / (float)meshSize, (y - meshSimplificationIncrement) / (float)meshSize);
                
                // Get the height from the height map
                float height = heightMap[x, y];
                
                // Position the vertex in 3D space, centered on the mesh
                Vector3 vertexPosition = new Vector3((percent.x * meshSizeUnsimplified - halfWidth) * meshSettings.meshScale, height, (percent.y * meshSizeUnsimplified - halfHeight) * meshSettings.meshScale);

                // Add the vertex to MeshData
                meshData.AddVertex(vertexPosition, percent, vertexIndex);

                // Add triangles (two per square)
                if (x < borderedSize - 1 && y < borderedSize - 1) { // Ignore right and bottom edge vertices
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + meshSimplificationIncrement, y];
                    int c = vertexIndicesMap[x, y + meshSimplificationIncrement];
                    int d = vertexIndicesMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];
                    meshData.AddTriangle(a, c, d); // First triangle
                    meshData.AddTriangle(a, d, b); // Second triangle
                }
            } 
        }

        // Perform flat shading or baked normals depending on the mesh settings
        if (meshData.useFlatShading) {
            meshData.FlatShading(); // Apply flat shading
        } else {
            meshData.BakedNormals(); // Use pre-calculated normals
        }

        return meshData; // Return the mesh data
    }
}

public class MeshData {
    Vector3[] vertices; // Vertex positions
    int[] triangles; // Triangle indices
    Vector2[] uvs; // UV coordinates for texture mapping
    Vector3[] bakedNormals; // Pre-calculated normals for smooth shading
    
    Vector3[] borderVertices; // Vertices along the border (not part of the main mesh)
    int[] borderTriangles; // Triangle indices for the border

    int triangleIndex; // Index for mesh triangles
    int borderTriangleIndex; // Index for border triangles

    public bool useFlatShading; // Flag to indicate whether flat shading is used

    // Constructor to initialize mesh data arrays
    public MeshData(int verticesPerLine, bool useFlatShading) {
        this.useFlatShading = useFlatShading;

        vertices = new Vector3[verticesPerLine * verticesPerLine]; // Main mesh vertices
        uvs = new Vector2[verticesPerLine * verticesPerLine]; // UVs for texture mapping
        triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6]; // 2 triangles per square (6 indices per square)

        borderVertices = new Vector3[verticesPerLine * 4 + 4]; // Border vertices (4 sides + 4 corners)
        borderTriangles = new int[24 * verticesPerLine]; // Border triangles (6 indices * 4 sides * verticesPerLine)
    }

    // Add a vertex to the mesh or border
    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex) {
        if (vertexIndex < 0) { // Border vertex
            borderVertices[-vertexIndex - 1] = vertexPosition; // Store in borderVertices
        } else { // Mesh vertex
            vertices[vertexIndex] = vertexPosition; // Store in mesh vertices
            uvs[vertexIndex] = uv; // Store UV coordinates
        }
    }

    // Add a triangle to the mesh or border
    public void AddTriangle(int a, int b, int c) {
        if (a < 0 || b < 0 || c < 0) { // Border triangle
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex += 3; // Move to next triangle
        } else { // Mesh triangle
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3; // Move to next triangle
        }
    }

    // Calculate normals for smooth shading
    Vector3[] CalculateNormals() {
        Vector3[] vertexNormals = new Vector3[vertices.Length]; // Array to store normals for each vertex
        int triangleCount = triangles.Length / 3;

        // Loop over each triangle and calculate its surface normal
        for (int i = 0; i < triangleCount; i++) {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            // Calculate the surface normal of the triangle
            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal; // Add the normal to each vertex of the triangle
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        // Repeat for border triangles
        int borderTriangleCount = borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++) {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriangleIndex];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            if (vertexIndexA >= 0) vertexNormals[vertexIndexA] += triangleNormal;
            if (vertexIndexB >= 0) vertexNormals[vertexIndexB] += triangleNormal;
            if (vertexIndexC >= 0) vertexNormals[vertexIndexC] += triangleNormal;
        }

        // Normalize the normals to ensure they are unit vectors
        for (int i = 0; i < vertexNormals.Length; i++) {
            vertexNormals[i].Normalize();
        }

        return vertexNormals; // Return the calculated normals
    }

    // Calculate surface normal for a triangle given its three vertex indices
    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
        Vector3 pointA = (indexA < 0) ? borderVertices[-indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];

        // Calculate the vectors for two edges of the triangle
        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;

        // Return the cross product of the two edges, which is perpendicular to the triangle's surface
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    // Apply flat shading by recalculating normals for each face (not shared between vertices)
    public void FlatShading() {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUVs = new Vector2[triangles.Length];

        for (int i = 0; i < triangles.Length; i++) {
            flatShadedVertices[i] = vertices[triangles[i]]; // Copy the vertex position
            flatShadedUVs[i] = uvs[triangles[i]]; // Copy the UV coordinates
            triangles[i] = i; // Reset the triangle indices
        }

        vertices = flatShadedVertices; // Replace original vertices with flat shaded ones
        uvs = flatShadedUVs; // Replace original UVs with flat shaded UVs
    }

    // Perform smoothing using pre-calculated normals for baked shading
    public void BakedNormals() {
        bakedNormals = CalculateNormals();
    }

    // Create the final mesh object with all necessary data
    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices; // Set the vertices of the mesh
        mesh.triangles = triangles; // Set the triangles of the mesh
        mesh.uv = uvs; // Set the UV coordinates

        if (useFlatShading) {
            mesh.RecalculateNormals(); // Calculate normals for flat shading
        } else {
            mesh.normals = bakedNormals; // Use pre-calculated normals for smooth shading
        }

        return mesh; // Return the generated mesh
    }
}
