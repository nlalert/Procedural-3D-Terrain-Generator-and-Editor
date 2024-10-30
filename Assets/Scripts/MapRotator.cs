using UnityEngine;

public class MapRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 5f; // Speed of rotation in degrees per second

    // Update is called once per frame
    void Update()
    {
        // Rotate the map around the Y axis (upwards) at the specified speed
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}
