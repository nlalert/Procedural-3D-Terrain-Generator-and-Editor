using UnityEngine;

[System.Serializable]
public class TerrainChunkData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;
    public Vector3[] normals;

    public TerrainChunkData(Vector3[] vertices, int[] triangles, Vector2[] uvs, Vector3[] normals)
    {
        this.vertices = vertices;
        this.triangles = triangles;
        this.uvs = uvs;
        this.normals = normals;
    }
}
