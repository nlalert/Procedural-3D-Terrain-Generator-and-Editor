using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator {

    //return MeshData instead of Mesh to able to create new Mesh outside thread
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail)
    {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);//each thread have their own AnimationCurve now
        
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        float halfWidth = (width - 1) / 2f; 
        float halfHeight = (height - 1) / 2f;

        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;//step 1 2 4 6 8 10 12
        int verticesPerLine = (width - 1)/meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        for (int x = 0; x < width; x += meshSimplificationIncrement) {
            for (int y = 0; y < height; y += meshSimplificationIncrement) {
                // meshData.vertices[vertexIndex] = new Vector3(x - halfWidth, heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier, y - halfHeight);
                // meshData.uvs[vertexIndex] = new Vector2(x/(float)width, y/(float)height);

                //start of negative side so mesh is center
                meshData.vertices[vertexIndex] = new Vector3(x - halfWidth, heightCurve.Evaluate(heightMap[width-1-x, height-1-y]) * heightMultiplier, y - halfHeight);
                 
                meshData.uvs[vertexIndex] = new Vector2(1f - x/(float)width, 1f - y/(float)height);//range 0-1

                //clockwise index
                if(x < width - 1 && y < height - 1) {//ignore right and bottom vertices
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);//v,v+w+1,v+w
                    meshData.AddTriangle(vertexIndex, vertexIndex + 1, vertexIndex + verticesPerLine + 1);//v, v+1, v+w+1
                }

                vertexIndex++;
            } 
        }
        
        return meshData;
    }
}

public class MeshData {
    public Vector3[] vertices;
    public int[] triangles;//all vertices for drawing triangle
    public Vector2[] uvs;
    int triangleIndex;

    public MeshData(int meshWidth, int meshHeight) {
        vertices    = new Vector3[meshWidth * meshHeight];
        uvs          = new Vector2[meshWidth * meshHeight];
        triangles   = new int[(meshWidth - 1) * (meshHeight - 1) * 6]; //2 triangle * 3 vertices
    }

    public void AddTriangle(int a, int b, int c) {
        triangles[triangleIndex]     = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        triangleIndex += 3;
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = this.vertices;
        mesh.triangles = this.triangles;
        mesh.uv = this.uvs;
        mesh.RecalculateNormals();

        return mesh;
    }
}
