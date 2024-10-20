using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainMeshData : UpdatableData
{
    public float uniformScale = 2f;//map size scale (x,y,z)

    public bool useFlatShading;
    public bool useFalloff;
    
    public float meshHeightMultiplier;//scale on Y
    public AnimationCurve meshHeightCurve;

    public float minHeight {
        get{
            return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(0);
        }
    }

    public float maxHeight {
        get{
            return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(1);
        }
    }
}   
