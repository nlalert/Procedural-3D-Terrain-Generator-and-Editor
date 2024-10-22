using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float panSpeed = 20f;        // Speed of panning with middle mouse
    public float rotationSpeed = 100f;  // Speed of rotation with right mouse
    public float zoomSpeed = 10f;       // Speed of zooming (scroll wheel)
    public float moveSpeed = 20f;       // Speed of WASD movement

    private Vector3 lastMousePosition;  // To track mouse movement
    private bool rotatingAroundPoint = false;
    private Vector3 rotationPoint;
    private float fixedY;  // Fixed Y position for WASD movement
     public GameObject cursorVisualCue;  // Visual cue (e.g., ring or highlight) to show on terrain

    // Custom cursors
    public Texture2D rotateCursor;
    public Texture2D terrainCursorTexture;
    private Vector2 terrainBoundsX; // X-axis boundaries (minX, maxX)
    private Vector2 terrainBoundsZ; // Z-axis boundaries (minZ, maxZ)
    private Vector2 cursorHotspot = Vector2.zero; // Hotspot for the cursor
    public MeshSettings meshSettings;

    private bool terrainFound = false;  // To track if the cursor is on the terrain

    void Start()
    {
        float boundary = (meshSettings.mapAreaLevel + 0.5f) * meshSettings.meshWorldSize;
        terrainBoundsX = new Vector2(-boundary, boundary);
        terrainBoundsZ = new Vector2(-boundary, boundary);
        // Set the initial Y elevation when the scene starts
        fixedY = transform.position.y;

        // Set the default cursor at the start (optional)
        Cursor.SetCursor(null, cursorHotspot, CursorMode.Auto);
    }

    void Update()
    {
        HandlePanning();
        HandleRotation();
        HandleZooming();
        HandleWASDMovement();
        HandleTerrainDetection();  // New function to detect terrain and show visual cue
    }

    void HandleTerrainDetection()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            // Check if we hit the terrain (you can check the tag of the terrain mesh)
            if (hit.collider.CompareTag("Terrain"))
            {
                terrainFound = true;

                // Show visual cue and position it on the hit point
                // cursorVisualCue.SetActive(true);
                // cursorVisualCue.transform.position = hit.point;
                Cursor.SetCursor(terrainCursorTexture, Vector2.zero, CursorMode.Auto);
            }
            else
            {
                terrainFound = false;
                Cursor.SetCursor(null, cursorHotspot, CursorMode.Auto);
            }
        }
        else
        {
            terrainFound = false;
        }

        // Hide visual cue if not hovering over terrain
        if (!terrainFound)
        {
            // cursorVisualCue.SetActive(false);
        }
    }
    void HandlePanning()
    {
        if (Input.GetMouseButton(2)) // Middle mouse button
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            Vector3 move = new Vector3(-delta.x, 0, -delta.y) * panSpeed * Time.deltaTime;
            transform.Translate(move, Space.Self);

            Vector3 newPosition = transform.position;
            newPosition.y = fixedY;
            transform.position = newPosition;

            // Clamp position within terrain bounds
            ClampCameraPosition();
        }
    }

    void HandleRotation()
    {
        if (Input.GetMouseButtonDown(1)) // Right mouse button clicked
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 1500.0f))
            {
                if (hit.collider.CompareTag("Terrain")) // Make sure the hit object is terrain
                {
                    rotationPoint = hit.point;
                    rotatingAroundPoint = true;
                    Debug.Log("Hit point on terrain: " + hit.point);
                }
                else
                {
                    rotatingAroundPoint = false;
                }
            }
            else
            {
                rotatingAroundPoint = false;
            }
        }

        if (Input.GetMouseButton(1)) // Holding right mouse button
        {
            Cursor.SetCursor(rotateCursor, cursorHotspot, CursorMode.Auto);

            Vector3 delta = Input.mousePosition - lastMousePosition;
            float rotX = delta.y * rotationSpeed * Time.deltaTime;
            float rotY = delta.x * rotationSpeed * Time.deltaTime;

            if (rotatingAroundPoint)
            {
                transform.RotateAround(rotationPoint, Vector3.up, rotY);
                transform.RotateAround(rotationPoint, transform.right, rotX);

                // After rotation, clamp the position
                ClampCameraPosition();
            }
        }
        else
        {
            Cursor.SetCursor(null, cursorHotspot, CursorMode.Auto);
        }
    }


    void HandleZooming()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        Vector3 move = transform.forward * scroll * zoomSpeed;
        transform.Translate(move, Space.World);

        fixedY = transform.position.y;

        // Clamp position within terrain bounds
        ClampCameraPosition();
    }

    void HandleWASDMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(moveX, 0, moveZ) * moveSpeed * Time.deltaTime;
        transform.Translate(move, Space.Self);

        Vector3 newPosition = transform.position;
        newPosition.y = fixedY;
        transform.position = newPosition;

        // Clamp position within terrain bounds
        ClampCameraPosition();
    }


    void ClampCameraPosition()
    {
        Vector3 pos = transform.position;

        // Clamp X and Z within terrain bounds
        pos.x = Mathf.Clamp(pos.x, terrainBoundsX.x, terrainBoundsX.y);
        pos.z = Mathf.Clamp(pos.z, terrainBoundsZ.x, terrainBoundsZ.y);

        // Optionally, if you want to prevent zooming below the terrain level
        // pos.y = Mathf.Clamp(pos.y, minY, maxY);

        // Apply the clamped position back to the camera
        transform.position = pos;
    }

    void LateUpdate()
    {
        lastMousePosition = Input.mousePosition;
    }
}
