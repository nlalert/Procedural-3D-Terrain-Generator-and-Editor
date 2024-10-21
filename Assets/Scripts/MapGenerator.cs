using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MapGenerator : MonoBehaviour {
    public enum DrawMode {NoiseMap, Mesh, FalloffMap};
    public DrawMode drawMode;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public textureData textureData;

    public Material terrainMaterial;

    [Range(0, MeshSettings.numSupportedLODS-1)]
    public int editorPreviewLOD;

    public bool autoUpdate;

    float[,] falloffMap;

    void Start(){

    }

    void OnValuesUpdated(){
        if (!Application.isPlaying) { // In Editor Mode
            EditorApplication.delayCall += () => DrawMapInEditor(); // Postpone execution
        }
    }

    void OnTextureValuesUpdated(){
        textureData.ApplyToMaterial(terrainMaterial);
    }

    public void DrawMapInEditor(){
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if(drawMode == DrawMode.NoiseMap){
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap.values));
        }
        else if(drawMode == DrawMode.Mesh){
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorPreviewLOD));
        }
        else if(drawMode == DrawMode.FalloffMap){
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numVertsPerLine)));
        }
    }

    void OnValidate(){
        //subscribe
        if(meshSettings != null){
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if(heightMapSettings != null){
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if(textureData != null){
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }
}
