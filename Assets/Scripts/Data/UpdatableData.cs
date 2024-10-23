using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class is a ScriptableObject that can automatically update values in the Unity Editor
public class UpdatableData : ScriptableObject
{
    // Event that triggers when values are updated
    public event System.Action OnValuesUpdated;

    // Boolean to control whether the values auto-update when modified
    public bool autoUpdate;

    // The following code will only be compiled inside the Unity Editor
    #if UNITY_EDITOR

    // This method is called whenever the ScriptableObject is modified in the editor
    // It checks if autoUpdate is enabled, and if so, subscribes to Unity's editor update event
    protected virtual void OnValidate(){
        if(autoUpdate){
            // Subscribe to the editor update loop
            UnityEditor.EditorApplication.update += NotifyOfUpdatedValues;
        }
    }

    // This method notifies the system that the values have been updated
    public void NotifyOfUpdatedValues(){
        // Unsubscribe from the editor update loop after updating
        UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;

        // If there are any subscribers to the OnValuesUpdated event, trigger the event
        if(OnValuesUpdated != null){
            OnValuesUpdated();
        }
    }

    #endif
}
