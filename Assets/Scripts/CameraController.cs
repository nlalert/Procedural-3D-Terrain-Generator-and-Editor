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

    private Vector2 cursorHotspot = Vector2.zero; // Hotspot for the cursor

    private bool terrainFound = false;  // To track if the cursor is on the terrain

    void Start()
    {
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
                    Debug.Log("Hit object but not terrain: " + hit.collider.name);
                    rotatingAroundPoint = false;
                }
            }
            else
            {
                Debug.Log("Raycast did not hit any object.");
                rotatingAroundPoint = false;
            }
        }

        if (Input.GetMouseButton(1)) // Holding right mouse button
        {
            // Change to rotating cursor
            Cursor.SetCursor(rotateCursor, cursorHotspot, CursorMode.Auto);

            Vector3 delta = Input.mousePosition - lastMousePosition;
            float rotX = delta.y * rotationSpeed * Time.deltaTime;
            float rotY = delta.x * rotationSpeed * Time.deltaTime;

            rotX = Mathf.Lerp(0, rotX, 0.1f); // Smoothing
            rotY = Mathf.Lerp(0, rotY, 0.1f);

            if (rotatingAroundPoint)
            {
                transform.RotateAround(rotationPoint, Vector3.up, rotY);
                transform.RotateAround(rotationPoint, transform.right, rotX);
            }
            else
            {
                Debug.Log("No point found");
                // Vector3 currentEulerAngles = transform.eulerAngles;
                // currentEulerAngles += new Vector3(rotX, rotY, 0);
                // transform.eulerAngles = currentEulerAngles;
            }
        }
        else
        {
            // Reset cursor when not rotating
            Cursor.SetCursor(null, cursorHotspot, CursorMode.Auto);
        }
    }

    void HandleZooming()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        Vector3 move = transform.forward * scroll * zoomSpeed;
        transform.Translate(move, Space.World);

        fixedY = transform.position.y;
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
    }

    void LateUpdate()
    {
        lastMousePosition = Input.mousePosition;
    }
}
