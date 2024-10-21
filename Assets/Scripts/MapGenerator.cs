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
    public HeightMapSettings heigtMapSettings;
    public textureData textureData;

    public Material terrainMaterial;

    [Range(0, MeshSettings.numSupportedLODS-1)]
    public int editorPreviewLOD;

    public bool autoUpdate;

    float[,] falloffMap;

    Queue<MapThreadInfo<HeightMap>> heightMapThreadInfoQueue = new Queue<MapThreadInfo<HeightMap>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    void Start(){
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, heigtMapSettings.minHeight, heigtMapSettings.maxHeight);
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
        textureData.UpdateMeshHeights(terrainMaterial, heigtMapSettings.minHeight, heigtMapSettings.maxHeight);
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heigtMapSettings, Vector2.zero);
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

    //Thread
    public void RequestHeightMap(Vector2 center, Action<HeightMap> callback) {// receive void callback(HeightMap)
        ThreadStart threadStart = delegate {//method 
            HeightMapThread(center, callback);
        };
        //new thread exectutes the HeightMapThread method. 
        new Thread(threadStart).Start();
    }

    //this method run from different thread (create from RequestHeightMap() ->HeightMapThread(callback);)
    void HeightMapThread(Vector2 center, Action<HeightMap> callback) {
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heigtMapSettings, center);//execute is this same thread
        //lock queue
        lock (heightMapThreadInfoQueue) {
            heightMapThreadInfoQueue.Enqueue(new MapThreadInfo<HeightMap>(callback, heightMap));
        }
    }

    public void RequestMeshData(HeightMap heightMap, int lod, Action<MeshData> callback) {
        ThreadStart threadStart = delegate {
            MeshDataThread(heightMap, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(HeightMap heightMap, int lod, Action<MeshData> callback) {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, lod);
        lock (meshDataThreadInfoQueue) {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    //Update Thread
    void Update() {
        if(heightMapThreadInfoQueue.Count > 0) {
            for (int i = 0; i < heightMapThreadInfoQueue.Count; i++){
                MapThreadInfo<HeightMap> threadInfo = heightMapThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if(meshDataThreadInfoQueue.Count > 0) {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++){
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    void OnValidate(){
        //subscribe
        if(meshSettings != null){
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if(heigtMapSettings != null){
            heigtMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heigtMapSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if(textureData != null){
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }

    //struct for handle both heightMap and meshData
    readonly struct MapThreadInfo<T> {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}
