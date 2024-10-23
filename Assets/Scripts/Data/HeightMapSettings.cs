using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Allows creation of an instance of HeightMapSettings from Unity's asset menu
[CreateAssetMenu()]
public class HeightMapSettings : UpdatableData // Inherits from UpdatableData class
{
    // Reference to NoiseSettings, used to generate noise for the height map
    public NoiseSettings noiseSettings;

    // Toggle to determine whether to use a falloff map (which can be used to flatten terrain near the edges)
    public bool useFalloff;
    
    // Multiplier to scale the height of the terrain on the Y-axis
    public float heightMultiplier;

    // AnimationCurve to control how terrain height changes (based on noise input)
    public AnimationCurve heightCurve;

    // Property to get the minimum height of the terrain by evaluating the heightCurve at 0 (the lowest point)
    public float minHeight {
        get{
            return heightMultiplier * heightCurve.Evaluate(0);
        }
    }

    // Property to get the maximum height of the terrain by evaluating the heightCurve at 1 (the highest point)
    public float maxHeight {
        get{
            return heightMultiplier * heightCurve.Evaluate(1);
        }
    }

    // Code that only runs in the Unity Editor
    #if UNITY_EDITOR

    // This method is called when changes are made in the Inspector
    protected override void OnValidate(){
        // Validate values in the noise settings to ensure they are within valid ranges
        noiseSettings.ValidateValues();
        // Calls the base class's OnValidate() to handle other validation tasks
        base.OnValidate();
    }

    #endif
}
