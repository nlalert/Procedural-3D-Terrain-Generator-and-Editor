using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float panSpeed = 20f;        // Speed of panning with middle mouse
    public float rotationSpeed = 100f;  // Speed of rotation with right mouse
    public float zoomSpeed = 10f;       // Speed of zooming (scroll wheel)
    public float moveSpeed = 20f;       // Speed of WASD movement
    public float minPitch = 10f;        // Minimum pitch angle (looking down)
    public float maxPitch = 80f;        // Maximum pitch angle (eye level)

    private Vector3 lastMousePosition;  // To track mouse movement
    private bool rotatingAroundPoint = false;
    private Vector3 rotationPoint;
    private float fixedY;               // Fixed Y position for WASD movement
    public GameObject cursorVisualCue;  // Visual cue (e.g., ring or highlight) to show on terrain

    // Custom cursors
    public Texture2D rotateCursor;
    public Texture2D terrainCursorTexture;
    private Vector2 terrainBoundsX;     // X-axis boundaries (minX, maxX)
    private Vector2 terrainBoundsZ;     // Z-axis boundaries (minZ, maxZ)
    private Vector2 cursorHotspot = Vector2.zero; // Hotspot for the cursor
    public MeshSettings meshSettings;

    private bool terrainFound = false;  // To track if the cursor is on the terrain
    public float minY = 0f;             // Minimum height (ground level)
    public float maxY = 220f;           // Maximum height

    // To track right-click position
    private Vector3 rightClickScreenPos;
    private bool isRightMouseHeld = false;

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

        if (Physics.Raycast(ray, out hit, 2000.0f))
        {
            if (hit.collider.CompareTag("Terrain")) // Make sure the hit object is terrain
            {
                rotationPoint = hit.point;
                rotatingAroundPoint = true;
                isRightMouseHeld = true;

                // Capture the screen position where the user right-clicked
                rightClickScreenPos = Input.mousePosition;
                Cursor.visible = false; // Hide the system cursor
            }
        }
    }

    if (Input.GetMouseButton(1) && isRightMouseHeld) // Holding right mouse button
    {
        // Continuously update the rotation point under the cursor
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 2000.0f) && hit.collider.CompareTag("Terrain"))
        {
            rotationPoint = hit.point;  // Update the rotation point
        }

        Vector3 delta = Input.mousePosition - lastMousePosition;
        float rotX = delta.y * rotationSpeed * Time.deltaTime;
        float rotY = delta.x * rotationSpeed * Time.deltaTime;

        if (rotatingAroundPoint)
        {
            // Apply Yaw (horizontal rotation)
            transform.RotateAround(rotationPoint, Vector3.up, rotY);

            // Apply Pitch (vertical rotation)
            Vector3 originalRotation = transform.eulerAngles;
            transform.RotateAround(rotationPoint, transform.right, rotX);

            // Clamp the pitch (rotation around X-axis)
            float currentPitch = transform.eulerAngles.x;
            if (currentPitch > 180) currentPitch -= 360;  // Ensure the angle is in range [-180, 180]
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
            transform.eulerAngles = new Vector3(currentPitch, transform.eulerAngles.y, 0);

            // After rotation, clamp the position
            ClampCameraPosition();
        }
    }

    if (Input.GetMouseButtonUp(1)) // Right mouse button released
    {
        isRightMouseHeld = false;
        Cursor.visible = true; // Show the system cursor again
    }
}
    void OnGUI()
    {
        if (isRightMouseHeld)
        {
            // Draw the custom "grabbing" cursor at the right-click position
            GUI.DrawTexture(new Rect(rightClickScreenPos.x - rotateCursor.width / 2, 
                                    Screen.height - rightClickScreenPos.y - rotateCursor.height / 2, 
                                    rotateCursor.width, rotateCursor.height), rotateCursor);
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

        // Clamp Y to not go below the minimum height (ground level)
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        // Apply the clamped position back to the camera
        transform.position = pos;
    }

    void LateUpdate()
    {
        lastMousePosition = Input.mousePosition;
    }
}
