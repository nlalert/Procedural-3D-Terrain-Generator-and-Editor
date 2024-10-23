using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class LightingSettings : UpdatableData
{
    public Gradient ambientColor;
    public Gradient directionalColor;
    public Gradient fogColor;
}
