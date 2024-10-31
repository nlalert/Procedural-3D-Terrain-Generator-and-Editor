using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class TerrainDeformer : MonoBehaviour
{
    public enum TerrainTool { None, IncreaseHeight, IncreaseHeightGaussian, DecreaseHeight, DecreaseHeightGaussian, Smooth }
    public TerrainTool currentTool = TerrainTool.None;

    public float deformRadius = 10f;   // Radius within which the terrain will be deformed or smoothed
    public float deformSpeed = 5f;     // Speed for deformation (positive for increase, negative for decrease)
    public float smoothingSpeed = 2f;  // Speed at which terrain vertices are smoothed

    public TerrainGenerator terrainGenerator;  // Reference to the terrain generator that holds the chunks

    private Stack<List<TerrainState>> undoStack = new Stack<List<TerrainState>>();
    private Stack<List<TerrainState>> redoStack = new Stack<List<TerrainState>>();

    private float undoRedoDelay = 0.2f; // Adjust the delay as needed
    private float nextUndoRedoTime = 0f;
    private bool isDeforming = false;
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
    
    public void SetBrushRadius(float radius) {
        deformRadius = radius;
    }

    public void SetBrushSpeed(float speed) {
        deformSpeed = speed;
    }

    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        
        // Undo/Redo logic
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if (Time.time >= nextUndoRedoTime)
            {
                if (Input.GetKey(KeyCode.Z)) { Undo(); nextUndoRedoTime = Time.time + undoRedoDelay; }
                else if (Input.GetKey(KeyCode.Y)) { Redo(); nextUndoRedoTime = Time.time + undoRedoDelay; }
            }
        }

        if (Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl))
            nextUndoRedoTime = 0f;

        // Check if the player starts deforming
        if (Input.GetMouseButtonDown(0))
        {
            // **Save initial state before deformation**
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.CompareTag("Terrain"))
            {
                List<TerrainChunk> chunksToModify = GetChunksAroundHitPoint(hit.point);
                SaveState(chunksToModify);
                redoStack.Clear(); // Clear redo stack after starting a new action
            }
            isDeforming = true;
        }

        // Perform deformation or smoothing based on the current tool
        if (isDeforming && Input.GetMouseButton(0))
        {
            if (currentTool == TerrainTool.IncreaseHeight || currentTool == TerrainTool.DecreaseHeight)
                PerformTerrainDeformation();
            else if (currentTool == TerrainTool.IncreaseHeightGaussian || currentTool == TerrainTool.DecreaseHeightGaussian)
                PerformGaussianTerrainDeformation();
            else if (currentTool == TerrainTool.Smooth)
                PerformTerrainSmoothing();
        }

        // Save the state on mouse release if any deformation occurred
        if (Input.GetMouseButtonUp(0) && isDeforming)
        {
            isDeforming = false;  // Reset the flag
        }
    }


    void PerformTerrainDeformation()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) && hit.collider.CompareTag("Terrain"))
        {
            ModifyTerrainInChunks(hit.point, isSmoothing: false, isGaussian: false);
        }
    }

    void PerformGaussianTerrainDeformation()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) && hit.collider.CompareTag("Terrain"))
        {
            ModifyTerrainInChunks(hit.point, isSmoothing: false, isGaussian: true);
        }
    }

    void PerformTerrainSmoothing()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) && hit.collider.CompareTag("Terrain"))
        {
            ModifyTerrainInChunks(hit.point, isSmoothing: true);
        }
    }

    List<TerrainChunk> GetChunksAroundHitPoint(Vector3 hitPoint)
    {
        Vector2 chunkCoord = new Vector2(Mathf.Floor(hitPoint.x / terrainGenerator.meshSettings.meshWorldSize),
                                         Mathf.Floor(hitPoint.z / terrainGenerator.meshSettings.meshWorldSize));

        List<TerrainChunk> chunksToModify = new List<TerrainChunk>();

        if (terrainGenerator.terrainChunkDictionary.TryGetValue(chunkCoord, out TerrainChunk mainChunk))
        {
            chunksToModify.Add(mainChunk);
        }

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

        return chunksToModify;
    }

    void ModifyTerrainInChunks(Vector3 hitPoint, bool isSmoothing = false, bool isGaussian = false)
    {
        List<TerrainChunk> chunksToModify = GetChunksAroundHitPoint(hitPoint);
        foreach (TerrainChunk chunk in chunksToModify)
        {
            if (isSmoothing)
            {
                SmoothChunkVertices(chunksToModify, hitPoint);
            }
            else if (isGaussian)
            {
                DeformChunkVerticesGaussian(chunk, hitPoint);
            }
            else
            {
                DeformChunkVertices(chunk, hitPoint);
            }
        }
    }

    void SaveState(List<TerrainChunk> chunks)
    {
        List<TerrainState> stateSnapshot = new List<TerrainState>();
        foreach (var chunk in chunks)
        {
            Vector3[] vertices = (Vector3[])chunk.meshFilter.mesh.vertices.Clone();
            stateSnapshot.Add(new TerrainState(vertices, chunk));
        }
        undoStack.Push(stateSnapshot);
        redoStack.Clear();
    }

    public void Undo()
    {
        if (undoStack.Count > 0)
        {
            List<TerrainState> currentState = CaptureCurrentState();  // Capture current state
            redoStack.Push(currentState);  // Push current state to redo stack

            List<TerrainState> previousState = undoStack.Pop();  // Pop the state to undo to
            ApplyTerrainState(previousState);  // Apply that state
        }
    }

    public void Redo()
    {
        if (redoStack.Count > 0)
        {
            List<TerrainState> currentState = CaptureCurrentState();  // Capture current state
            undoStack.Push(currentState);  // Push current state to undo stack

            List<TerrainState> nextState = redoStack.Pop();  // Pop the state to redo to
            ApplyTerrainState(nextState);  // Apply that state
        }
    }

    // Helper method to capture the current state of the chunks
    List<TerrainState> CaptureCurrentState()
    {
        List<TerrainState> stateSnapshot = new List<TerrainState>();
        foreach (var chunk in terrainGenerator.terrainChunkDictionary.Values)
        {
            Vector3[] vertices = (Vector3[])chunk.meshFilter.mesh.vertices.Clone();
            stateSnapshot.Add(new TerrainState(vertices, chunk));
        }
        return stateSnapshot;
    }

    void ApplyTerrainState(List<TerrainState> stateSnapshot)
    {
        foreach (var state in stateSnapshot)
        {
            Mesh mesh = state.chunk.meshFilter.mesh;
            mesh.vertices = state.vertices;
            mesh.RecalculateNormals();
            state.chunk.meshCollider.sharedMesh = mesh;
        }
    }

    void DeformChunkVertices(TerrainChunk terrainChunk, Vector3 hitPoint) 
    {
        Mesh mesh = terrainChunk.meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertexWorldPos = terrainChunk.meshObject.transform.TransformPoint(vertices[i]);
            float distance = Vector3.Distance(vertexWorldPos, hitPoint);

            if (distance < deformRadius)
            {
                vertices[i].y += deformSpeed * Time.deltaTime;
            }
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        terrainChunk.meshCollider.sharedMesh = mesh;
    }

    void DeformChunkVerticesGaussian(TerrainChunk terrainChunk, Vector3 hitPoint)
    {
        Mesh mesh = terrainChunk.meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;

        float sigma = deformRadius / 2f;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertexWorldPos = terrainChunk.meshObject.transform.TransformPoint(vertices[i]);
            float distance = Vector3.Distance(vertexWorldPos, hitPoint);

            if (distance < deformRadius)
            {
                float gaussianMultiplier = Mathf.Exp(-Mathf.Pow(distance, 2) / (2 * Mathf.Pow(sigma, 2)));
                vertices[i].y += deformSpeed * gaussianMultiplier * Time.deltaTime;
            }
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        terrainChunk.meshCollider.sharedMesh = mesh;
    }

    void SmoothChunkVertices(List<TerrainChunk> terrainChunks, Vector3 hitPoint)
    {
        List<Vector3> verticesInRadius = new List<Vector3>();

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
                }
            }
        }

        float averageHeight = 0;
        foreach (Vector3 vertex in verticesInRadius)
        {
            averageHeight += vertex.y;
        }
        averageHeight /= verticesInRadius.Count;

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
                    vertices[i].y = Mathf.Lerp(vertices[i].y, averageHeight, smoothingSpeed * Time.deltaTime);
                }
            }

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            chunk.meshCollider.sharedMesh = mesh;
        }
    }
}

// Helper class for storing terrain state
public class TerrainState
{
    public Vector3[] vertices;
    public TerrainChunk chunk;

    public TerrainState(Vector3[] vertices, TerrainChunk chunk)
    {
        this.vertices = (Vector3[])vertices.Clone();  // Clone the vertices array for safe storage
        this.chunk = chunk;
    }
}
