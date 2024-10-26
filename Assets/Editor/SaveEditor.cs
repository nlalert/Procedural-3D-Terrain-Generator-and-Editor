using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(TerrainGenerator))]
public class SaveEditor : Editor
{
    public override void OnInspectorGUI() {
        TerrainGenerator terrainGenerator = (TerrainGenerator)target;

        if(GUILayout.Button("Save")) {
            terrainGenerator.SaveTerrain("Test");
        }
        if(GUILayout.Button("Load")) {
            terrainGenerator.LoadTerrain("Test");
        }
    }
}
