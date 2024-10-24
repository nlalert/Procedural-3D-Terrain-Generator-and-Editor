using UnityEngine;

// Create an instance of this scriptable object from Unity's asset menu
[CreateAssetMenu()]
public class MeshSettings : UpdatableData // Inherits from UpdatableData class
{
    // Constants for defining supported chunk sizes
    // public const int numSupportedChunkSizes = 9;

    // // Dropdown for selecting the chunk size index for the mesh 
    // [Range(0, numSupportedChunkSizes - 1)]
    // public int chunkSizeIndex;
    // public static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };

    [Range(48, 144)]
    public int chunkSize = 48;

    [Range(0, 2)]
    public int mapRadius = 1;

    // Controls the scale of the mesh (affects world size)
    public float meshScale = 2f; // Map size scale (x, y, z)

    // Property to calculate the number of vertices per line in the mesh
    // Includes two extra vertices used for normal calculation but excluded from the final mesh
    public int numVertsPerLine
    {
        get
        {
            // get the chunk size plus 1 for the extra vertices
            // return supportedChunkSizes[chunkSizeIndex] + 1;
            return chunkSize;
        }
    }

    // Property to calculate the world size of the mesh
    public float meshWorldSize
    {
        get
        {
            // Subtract 3 vertices (1 for the extra vertex at each end and 2 from the border) and scale the mesh size
            return (numVertsPerLine - 3) * meshScale;
        }
    }
}
