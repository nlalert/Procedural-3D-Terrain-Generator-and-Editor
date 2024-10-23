using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Allows creation of an instance of LightingSettings from Unity's asset menu
[CreateAssetMenu()]
public class LightingSettings : UpdatableData // Inherits from UpdatableData class
{
    // A Gradient for controlling the ambient lighting color over time or other variables (e.g., based on day/night cycle)
    public Gradient ambientColor;

    // A Gradient for controlling the color of the directional light (e.g., sunlight or main light source)
    public Gradient directionalColor;

    // A Gradient for controlling the fog color over time or based on conditions
    public Gradient fogColor;
}
