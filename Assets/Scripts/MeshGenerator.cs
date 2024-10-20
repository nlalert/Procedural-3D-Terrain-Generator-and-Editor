using System.Collections;
using UnityEngine;

public static class MeshGenerator {

    //return MeshData instead of Mesh to able to create new Mesh outside thread
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail, bool useFlatShading)
    {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);//each thread have their own AnimationCurve now
        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;//step 1 2 4 6 8 10 12
        
        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * meshSimplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;//use for calculate halfWidth halfHeight

        float halfWidth = (meshSizeUnsimplified - 1) / 2f; 
        float halfHeight = (meshSizeUnsimplified - 1) / 2f;

        int verticesPerLine = (meshSize - 1)/meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, useFlatShading);
        
        //keep vertex index
        //mesh is postive
        //border is negative
        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;
        for (int x = 0; x < borderedSize; x += meshSimplificationIncrement) {
            for (int y = 0; y < borderedSize; y += meshSimplificationIncrement) {
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;

                if(isBorderVertex){
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;//use negative index
                }
                else{
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;//positive index
                }
            }
        }

        for (int x = 0; x < borderedSize; x += meshSimplificationIncrement) {
            for (int y = 0; y < borderedSize; y += meshSimplificationIncrement) {
                int vertexIndex = vertexIndicesMap[x, y];

                //uv - meshSimplificationIncrement to centered
                Vector2 percent = new Vector2((x-meshSimplificationIncrement)/(float)meshSize, ( y-meshSimplificationIncrement)/(float)meshSize);
                
                float height = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
                // //start of negative side so mesh is center 
                Vector3 vertexPosition = new Vector3(percent.x * meshSizeUnsimplified - halfWidth, height, percent.y * meshSizeUnsimplified - halfHeight);

                meshData.AddVertex(vertexPosition, percent, vertexIndex);

                //clockwise index
                if(x < borderedSize - 1 && y < borderedSize - 1) {//ignore right and bottom vertices
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + meshSimplificationIncrement, y];
                    int c = vertexIndicesMap[x, y + meshSimplificationIncrement];
                    int d = vertexIndicesMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];
                    meshData.AddTriangle(a, c, d); // v, v+w, v+w+1
                    meshData.AddTriangle(a, d, b); // v, v+w+1, v+1
                }
            } 
        }

        if(meshData.useFlatShading){
            meshData.FlatShading();//no share anymore
        }
        else{
            meshData.BakedNormals();
        }
        
        return meshData;
    }
}

public class MeshData {
    Vector3[] vertices;
    int[] triangles;//all vertices for drawing triangle
    Vector2[] uvs;
    Vector3[] bakedNormals;
    
    Vector3[] borderVertices;//vertices that won't be include in mesh
    int[] borderTriangles;

    int triangleIndex;
    int borderTriangleIndex;

    public bool useFlatShading;

    public MeshData(int verticesPerLine, bool useFlatShading) {
        this.useFlatShading = useFlatShading;

        vertices    = new Vector3[verticesPerLine * verticesPerLine];
        uvs          = new Vector2[verticesPerLine * verticesPerLine];
        triangles   = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6]; //2 triangle * 3 vertices

        borderVertices = new Vector3[verticesPerLine * 4 + 4];//4 side of mesh vertex + 4 corner
        borderTriangles = new int[24 * verticesPerLine];//6 vertices (for draw 2 triangle) * 4 * verticesPerLine in
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex) {
        if(vertexIndex < 0){//border
            //start from -1 so flip to 1 and minus 1 to start with 0
            borderVertices[-vertexIndex - 1] = vertexPosition;
        }
        else{//mesh
            vertices[vertexIndex] = vertexPosition;
            uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c) {
        if(a < 0 || b < 0 || c < 0){//border 
            borderTriangles[borderTriangleIndex]     = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex += 3;
        }
        else{
            triangles[triangleIndex]     = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }

    }

    Vector3[] CalculateNormals(){
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length /3;
        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriangleIndex];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            if(vertexIndexA >= 0){
                vertexNormals[vertexIndexA] += triangleNormal;
            }
            if(vertexIndexB >= 0){
                vertexNormals[vertexIndexB] += triangleNormal;
            }           
            if(vertexIndexC >= 0){
                vertexNormals[vertexIndexC] += triangleNormal;
            }
        }

        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    //normal of single face with 3 indices
    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC){
        Vector3 pointA = (indexA < 0) ? borderVertices[-indexA-1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertices[-indexB-1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[-indexC-1] : vertices[indexC];

        //cross product to find perpendicular (normal)
        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;

        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void BakedNormals(){
        bakedNormals = CalculateNormals();
    }

    public void FlatShading(){
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUVs = new Vector2[triangles.Length];

        for (int i = 0; i < triangles.Length; i++)
        {
            flatShadedVertices[i] = vertices[triangles[i]];
            flatShadedUVs[i] = uvs[triangles[i]];
            triangles[i] = i;
        }

        vertices = flatShadedVertices;
        uvs = flatShadedUVs;
    }

    //for calling from MAIN GAME Thread
    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = this.vertices;
        mesh.triangles = this.triangles;
        mesh.uv = this.uvs;
        if(useFlatShading){
            mesh.RecalculateNormals();
        }
        else{
            mesh.normals = bakedNormals;
        }

        return mesh;
    }
}
