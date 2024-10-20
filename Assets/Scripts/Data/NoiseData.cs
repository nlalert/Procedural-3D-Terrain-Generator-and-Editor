using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdatableData
{
    public Noise.NormalizeMode normalizeMode;    
    public float noiseScale;

    [Range(1, 10)]
    public int octaves;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    protected override void OnValidate(){
        if(noiseScale < 0) noiseScale = 0.0001f;;
        if(lacunarity < 1) lacunarity = 1;
        if(octaves < 0) octaves = 0;

        base.OnValidate();
    }
}
