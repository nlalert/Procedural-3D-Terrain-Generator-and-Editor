using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class EndlessTerrain : MonoBehaviour
{
    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
    const float colliderGenerationDistanceThreshold = 5;

    public int colliderLODIndex;
    public LODInfo[] detailLevels;
    public static float maxViewDst;

    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;
    float meshWorldSize;
    int chunksVisibleInViewDst;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();//keep track of chunk that visible last update
    void Start() {
        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDst = detailLevels[detailLevels.Length-1].visibleDstThreshold;
        meshWorldSize = mapGenerator.meshSettings.meshWorldSize;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst/meshWorldSize);

        UpdateVisibleChunks();
    }

    void Update() {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        if(viewerPosition != viewerPositionOld){
            foreach (TerrainChunk terrainChunk in visibleTerrainChunks)
            {
                terrainChunk.UpdateCollisionMesh();
            }
        }

        if((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate){
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks() {
        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
        //set all terrain chunk that visible in last update to be false
        for (int i = visibleTerrainChunks.Count-1; i >= 0; i--){
            alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
            visibleTerrainChunks[i].UpdateTerrainChunk();
        }

        //coordinate relative to 0,0 -> -1,0 or 1,1 NOT real world position like 234,212
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x/meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y/meshWorldSize);

        //all chunk visible around viewer
        for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
                Vector2 viewChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if(!alreadyUpdatedChunkCoords.Contains(viewChunkCoord)){
                    if(terrainChunkDictionary.ContainsKey(viewChunkCoord)) {
                        terrainChunkDictionary[viewChunkCoord].UpdateTerrainChunk();
                    }
                    else{
                        //create new terrain chunk
                        terrainChunkDictionary.Add(viewChunkCoord, new TerrainChunk(viewChunkCoord, meshWorldSize, detailLevels, colliderLODIndex, transform, mapMaterial));
                    }
                }
            }
        }
    }

    public class TerrainChunk {

        public Vector2 coord;
        
        GameObject meshObject;
        Vector2 sampleCenter;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        int colliderLODIndex;

        HeightMap mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;
        bool hasSetCollider;

        public TerrainChunk(Vector2 coord, float meshWorldSize, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Material material) {
            this.coord = coord;
            this.detailLevels = detailLevels;
            this.colliderLODIndex = colliderLODIndex;

            sampleCenter = coord * meshWorldSize / mapGenerator.meshSettings.meshScale;
            Vector2 position = coord * meshWorldSize;
            bounds = new Bounds(position, Vector2.one * meshWorldSize);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;

            meshObject.transform.position = new Vector3(position.x, 0, position.y);
            meshObject.transform.parent = parent;
            SetVisible(false);
            
            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++) {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod);
                lodMeshes[i].updateCallback += UpdateTerrainChunk;
                if(i == colliderLODIndex){
                    lodMeshes[i].updateCallback += UpdateCollisionMesh;
                }
            }

            //main thread
            mapGenerator.RequestHeightMap(sampleCenter, OnMapDataReceived);//request the generatedChunk "once it's done". 
        }
        
        void OnMapDataReceived(HeightMap mapData) {
            this.mapData = mapData;
            mapDataReceived = true;

            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk() {
            if(mapDataReceived){
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));//smallest sqrt distance

                bool wasVisible = IsVisible();
                bool visible = viewerDstFromNearestEdge <= maxViewDst;

                if (visible) {
                    int lodIndex = 0;
                    for (int i = 0; i < detailLevels.Length-1; i++) {
                        if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold) {
                            lodIndex = i + 1;
                        }
                        else{
                            break;
                        }
                    }

                    if (lodIndex != previousLODIndex){
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh){
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh){
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                }
                if(wasVisible != visible){
                    if(visible){
                        visibleTerrainChunks.Add(this);
                    }
                    else{
                        visibleTerrainChunks.Remove(this);
                    }
                    SetVisible(visible);
                }
            }
        }

        public void UpdateCollisionMesh(){
            if(!hasSetCollider){
                float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

                if(sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDstThreshold){
                    if(!lodMeshes[colliderLODIndex].hasRequestedMesh){
                        lodMeshes[colliderLODIndex].RequestMesh(mapData);
                    }
                }

                if(sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold){
                    if(lodMeshes[colliderLODIndex].hasMesh){
                        meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                        hasSetCollider = true;
                    }
                }
            }

        }

        public void SetVisible(bool visible) {
            meshObject.SetActive(visible);
        }

        public bool IsVisible() {
            return meshObject.activeSelf;
        }
    }

    //for fetching it own mesh from mapGenerator
    class LODMesh {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        public event System.Action updateCallback;

        public LODMesh(int lod){
            this.lod = lod;
        }

        void OnMeshDataReceived(MeshData meshData){
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(HeightMap mapData){
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo {
        [Range (0, MeshSettings.numSupportedLODS-1)]
        public int lod;
        public float visibleDstThreshold;

        public float sqrVisibleDstThreshold{
            get{
                return visibleDstThreshold * visibleDstThreshold;
            }
        }
    }
}
