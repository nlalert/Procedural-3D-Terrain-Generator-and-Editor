using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways] // This attribute ensures the script runs in both edit mode and play mode
public class LightingManager : MonoBehaviour
{
    // References to the main directional light and the lighting settings
    [SerializeField] private Light directionalLight; // The directional light in the scene (e.g., sun)
    [SerializeField] private LightingSettings lightingSettings; // ScriptableObject containing lighting data (colors for different times of day)
    
    // Speed at which time progresses (when playing) and the current time of day
    [SerializeField] public int timeSpeed = 5; // Speed modifier for simulating time progression
    [SerializeField, Range(0, 24)] public float timeOfDay; // Time of day, clamped between 0 and 24

    public TimeSettingsUI timeSettingsUI;
    public bool isAutoTime = true;

    // Update is called once per frame
    private void Update(){
        // If lighting settings are missing, don't proceed
        if(lightingSettings == null)
            return;
        if(isAutoTime){
            // In play mode, advance the time of day based on the time speed and deltaTime
            if(Application.isPlaying){
                // Increase time of day with deltaTime and scale by timeSpeed
                timeOfDay += Time.deltaTime / 100 * timeSpeed;
                if(timeOfDay > 24) 
                    timeSettingsUI.timeSlider.value = 0;
                timeOfDay %= 24; // Wrap timeOfDay to stay within 0-24 range (simulating 24-hour cycle)
            }
        }

        // Update lighting based on the current time of day percentage (normalized to 0-1)
        UpdateLighting(timeOfDay / 24f);
        timeSettingsUI.UpdateTimeText();
    }

    // Updates lighting settings based on time of day percentage
    private void UpdateLighting(float timePercent){
        // Evaluate and set ambient light and fog color using gradient data from the lighting settings
        RenderSettings.ambientLight = lightingSettings.ambientColor.Evaluate(timePercent); // Set ambient light based on time
        RenderSettings.fogColor = lightingSettings.fogColor.Evaluate(timePercent); // Set fog color based on time
        directionalLight.intensity = Mathf.Lerp(1.0f, 0.1f, Mathf.Clamp01((timePercent - 0.5f) * 2));
        // If there is a directional light (e.g., the sun), update its color and rotation
        if(directionalLight != null){
            directionalLight.color = lightingSettings.directionalColor.Evaluate(timePercent); // Change sun color over time
            // Rotate the directional light based on the time of day, simulating sun movement
            directionalLight.transform.localRotation = Quaternion.Euler(new Vector3((timePercent * 360f) - 90f, -170, 0));
        }
    }

    // OnValidate is called when the script is loaded or a value is changed in the inspector
    private void OnValidate(){
        // If a directional light is already assigned, no need to search for one
        if(directionalLight != null)
            return;

        // If the RenderSettings' sun is set, assign it as the directional light
        if(RenderSettings.sun != null){
            directionalLight = RenderSettings.sun;
            Debug.Log("SUN");
        }
    }
}
