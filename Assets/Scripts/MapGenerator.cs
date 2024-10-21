using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MapGenerator : MonoBehaviour {
    public enum DrawMode {NoiseMap, Mesh, FalloffMap};
    public DrawMode drawMode;

    public TerrainMeshData terrainMeshData;
    public NoiseData noiseData;
    public textureData textureData;

    public Material terrainMaterial;

    [Range(0, 6)]
    public int editorPreviewLOD;

    public bool autoUpdate;

    float[,] falloffMap;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    void Awake(){
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, terrainMeshData.minHeight, terrainMeshData.maxHeight);
    }

    void OnValuesUpdated(){
        if (!Application.isPlaying) { // In Editor Mode
            EditorApplication.delayCall += () => DrawMapInEditor(); // Postpone execution
        }
    }

    void OnTextureValuesUpdated(){
        textureData.ApplyToMaterial(terrainMaterial);
    }

    public  int mapChunkSize{
        get{
            if(terrainMeshData.useFlatShading){
                return 95;
            }
            else{
                return 239;
            }
        }
    }

    public void DrawMapInEditor(){
        textureData.UpdateMeshHeights(terrainMaterial, terrainMeshData.minHeight, terrainMeshData.maxHeight);
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if(drawMode == DrawMode.NoiseMap){
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if(drawMode == DrawMode.Mesh){
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainMeshData.meshHeightMultiplier, terrainMeshData.meshHeightCurve, editorPreviewLOD, terrainMeshData.useFlatShading));
        }
        else if(drawMode == DrawMode.FalloffMap){
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
        }
    }

    //Thread
    public void RequestMapData(Vector2 center, Action<MapData> callback) {// receive void callback(Mapdata)
        ThreadStart threadStart = delegate {//method 
            MapDataThread(center, callback);
        };
        //new thread exectutes the MapDataThread method. 
        new Thread(threadStart).Start();
    }

    //this method run from different thread (create from RequestMapData() ->MapDataThread(callback);)
    void MapDataThread(Vector2 center, Action<MapData> callback) {
        MapData mapData = GenerateMapData(center);//execute is this same thread
        //lock queue
        lock (mapDataThreadInfoQueue) {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
        ThreadStart threadStart = delegate {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainMeshData.meshHeightMultiplier, terrainMeshData.meshHeightCurve, lod, terrainMeshData.useFlatShading);
        lock (meshDataThreadInfoQueue) {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    //Update Thread
    void Update() {
        if(mapDataThreadInfoQueue.Count > 0) {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++){
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
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

    MapData GenerateMapData(Vector2 center){
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, noiseData.seed, noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, center + noiseData.offset, noiseData.normalizeMode);// Generate 1 extra noise value for around it 

        if(terrainMeshData.useFalloff){

            if(falloffMap == null){
                falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2);
            }

            Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
            for (int x = 0; x < mapChunkSize + 2; x++){
                for (int y = 0; y < mapChunkSize + 2; y++){
                    if(terrainMeshData.useFalloff){
                        noiseMap[x,y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                    }
                }
            }
        }

        return new MapData(noiseMap);
    }

    void OnValidate(){
        //subscribe
        if(terrainMeshData != null){
            terrainMeshData.OnValuesUpdated -= OnValuesUpdated;
            terrainMeshData.OnValuesUpdated += OnValuesUpdated;
        }
        if(noiseData != null){
            noiseData.OnValuesUpdated -= OnValuesUpdated;
            noiseData.OnValuesUpdated += OnValuesUpdated;
        }
        if(textureData != null){
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }

    //struct for handle both mapData and meshData
    readonly struct MapThreadInfo<T> {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

public readonly struct MapData {
    public readonly float[,] heightMap;

    public MapData(float[,] heightMap) {
        this.heightMap = heightMap;
    }
}
