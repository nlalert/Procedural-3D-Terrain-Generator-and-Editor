using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Public variables to control camera movement, rotation, and zoom
    public float panSpeed = 20f;        // Speed of panning with middle mouse button
    public float rotationSpeed = 100f;  // Speed of rotation with right mouse button
    public float zoomSpeed = 10f;       // Speed of zooming with the mouse scroll wheel
    public float moveSpeed = 20f;       // Speed of WASD movement
    public float minPitch = 10f;        // Minimum pitch angle (how much you can look down)
    public float maxPitch = 80f;        // Maximum pitch angle (how much you can look up)

    private Vector3 lastMousePosition;  // Stores the mouse position from the previous frame
    private bool rotatingAroundPoint = false;  // Whether the camera is rotating around a specific point
    private Vector3 rotationPoint;      // The point the camera rotates around
    private float fixedY;               // Fixed Y-axis position for maintaining height during movement
    public GameObject cursorVisualCue;  // Visual cue (e.g., a ring or highlight) to indicate where the cursor is on the terrain

    // Custom cursor textures
    public Texture2D rotateCursor;
    public Texture2D terrainCursorTexture;
    private Vector2 terrainBoundsX;     // X-axis boundaries (min and max values)
    private Vector2 terrainBoundsZ;     // Z-axis boundaries (min and max values)
    private Vector2 cursorHotspot = Vector2.zero; // Position of the custom cursor hotspot
    public MeshSettings meshSettings;   // Reference to mesh settings for terrain boundaries

    private bool terrainFound = false;  // Whether the cursor is over the terrain
    public float minY = 0f;             // Minimum camera height (ground level)
    public float maxY = 220f;           // Maximum camera height

    // To track right-click position for custom cursor display
    private Vector3 rightClickScreenPos;
    private bool isRightMouseHeld = false;  // Whether the right mouse button is held down

    void Start()
    {
        // Calculate terrain boundaries based on mesh settings
        float boundary = (meshSettings.mapAreaLevel + 0.5f) * meshSettings.meshWorldSize;
        terrainBoundsX = new Vector2(-boundary, boundary);
        terrainBoundsZ = new Vector2(-boundary, boundary);

        // Set the initial Y position for the camera
        fixedY = transform.position.y;

        // Set the default system cursor (optional)
        Cursor.SetCursor(null, cursorHotspot, CursorMode.Auto);
    }

    void Update()
    {
        // Handle different types of camera controls in each frame
        HandlePanning();
        HandleRotation();
        HandleZooming();
        HandleWASDMovement();
        HandleTerrainDetection();  // Detect terrain and show visual cue
    }

    void HandleTerrainDetection()
    {
        // Create a ray from the camera to the mouse position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            // Check if the ray hit the terrain (identified by the "Terrain" tag)
            if (hit.collider.CompareTag("Terrain"))
            {
                terrainFound = true;

                // Show the custom cursor when hovering over the terrain
                Cursor.SetCursor(terrainCursorTexture, Vector2.zero, CursorMode.Auto);
            }
            else
            {
                // Reset to default cursor when not over the terrain
                terrainFound = false;
                Cursor.SetCursor(null, cursorHotspot, CursorMode.Auto);
            }
        }
        else
        {
            terrainFound = false;
        }
    }

    void HandlePanning()
    {
        if (Input.GetMouseButton(2)) // Middle mouse button
        {
            // Calculate the movement delta based on mouse position change
            Vector3 delta = Input.mousePosition - lastMousePosition;
            Vector3 move = new Vector3(-delta.x, 0, -delta.y) * panSpeed * Time.deltaTime;

            // Move the camera in local space based on delta
            transform.Translate(move, Space.Self);

            // Maintain fixed Y position (don't allow vertical panning)
            Vector3 newPosition = transform.position;
            newPosition.y = fixedY;
            transform.position = newPosition;

            // Clamp the camera's position within the terrain bounds
            ClampCameraPosition();
        }
    }

    void HandleRotation()
    {
        if (Input.GetMouseButtonDown(1)) // Right mouse button pressed
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Check if the ray hits the terrain
            if (Physics.Raycast(ray, out hit, 2000.0f))
            {
                if (hit.collider.CompareTag("Terrain"))  // Only rotate around terrain points
                {
                    rotationPoint = hit.point;  // Set the rotation point
                    rotatingAroundPoint = true;
                    isRightMouseHeld = true;   // Indicate that the right mouse button is held

                    // Capture the mouse position when the right button is clicked
                    rightClickScreenPos = Input.mousePosition;
                    Cursor.visible = false;  // Hide the default system cursor
                }
            }
        }

        if (Input.GetMouseButton(1) && isRightMouseHeld)  // If right mouse button is held
        {
            // Update the rotation point under the cursor
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 2000.0f) && hit.collider.CompareTag("Terrain"))
            {
                rotationPoint = hit.point;  // Update the rotation point
            }

            // Calculate the rotation delta based on mouse movement
            Vector3 delta = Input.mousePosition - lastMousePosition;
            float rotX = delta.y * rotationSpeed * Time.deltaTime;
            float rotY = delta.x * rotationSpeed * Time.deltaTime;

            if (rotatingAroundPoint)
            {
                // Rotate horizontally (yaw) around the Y-axis
                transform.RotateAround(rotationPoint, Vector3.up, rotY);

                // Rotate vertically (pitch) around the camera's local X-axis
                transform.RotateAround(rotationPoint, transform.right, rotX);

                // Clamp the vertical rotation (pitch)
                float currentPitch = transform.eulerAngles.x;
                if (currentPitch > 180) currentPitch -= 360;  // Ensure angle is in range [-180, 180]
                currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);  // Clamp the pitch angle
                transform.eulerAngles = new Vector3(currentPitch, transform.eulerAngles.y, 0);

                // Clamp the camera's position within the terrain bounds
                ClampCameraPosition();
            }
        }

        if (Input.GetMouseButtonUp(1))  // When the right mouse button is released
        {
            isRightMouseHeld = false;
            Cursor.visible = true;  // Restore the default system cursor
        }
    }

    void OnGUI()
    {
        if (isRightMouseHeld)  // Draw custom cursor while right mouse button is held
        {
            GUI.DrawTexture(new Rect(rightClickScreenPos.x - rotateCursor.width / 2, 
                                    Screen.height - rightClickScreenPos.y - rotateCursor.height / 2, 
                                    rotateCursor.width, rotateCursor.height), rotateCursor);
        }
    }

    void HandleZooming()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");  // Get scroll input

        // Move the camera forward or backward based on the scroll input
        Vector3 move = transform.forward * scroll * zoomSpeed;
        transform.Translate(move, Space.World);

        // Update the Y position to keep track of zoom level
        fixedY = transform.position.y;

        // Clamp the camera's position within the terrain bounds
        ClampCameraPosition();
    }

    void HandleWASDMovement()
    {
        // Get input from WASD or arrow keys for movement
        float moveX = Input.GetAxis("Horizontal");  // Left/Right (A/D or Left/Right arrow)
        float moveZ = Input.GetAxis("Vertical");    // Forward/Backward (W/S or Up/Down arrow)

        // Move the camera based on the input
        Vector3 move = new Vector3(moveX, 0, moveZ) * moveSpeed * Time.deltaTime;
        transform.Translate(move, Space.Self);

        // Maintain fixed Y position (don't allow vertical movement)
        Vector3 newPosition = transform.position;
        newPosition.y = fixedY;
        transform.position = newPosition;

        // Clamp the camera's position within the terrain bounds
        ClampCameraPosition();
    }

    // Ensures the camera stays within the defined terrain boundaries
    void ClampCameraPosition()
    {
        Vector3 pos = transform.position;

        // Clamp the X and Z coordinates within the terrain bounds
        pos.x = Mathf.Clamp(pos.x, terrainBoundsX.x, terrainBoundsX.y);
        pos.z = Mathf.Clamp(pos.z, terrainBoundsZ.x, terrainBoundsZ.y);

        // Clamp the Y position to stay within the min and max heights
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        // Apply the clamped position to the camera
        transform.position = pos;
    }

    // Store the mouse position from the current frame to use in the next frame
    void LateUpdate()
    {
        lastMousePosition = Input.mousePosition;
    }
}
