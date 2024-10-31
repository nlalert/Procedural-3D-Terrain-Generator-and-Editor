using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways] // This attribute ensures the script runs in both edit mode and play mode
public class LightingManager : MonoBehaviour
{
    [SerializeField] private Light directionalLight; // The directional light in the scene (e.g., sun)
    [SerializeField] private LightingSettings lightingSettings; // ScriptableObject containing lighting data (colors for different times of day)
    
    [SerializeField] public int timeSpeed = 5; // Speed modifier for simulating time progression
    [SerializeField, Range(0, 24)] public float timeOfDay; // Time of day, clamped between 0 and 24

    public TimeSettingsUI timeSettingsUI;
    public bool isAutoTime = true;

    // Skybox settings
    [SerializeField] private Material skyboxMaterial; // Reference to the skybox material
    [SerializeField] private Gradient skyboxTint; // Gradient to control skybox color over time
    [SerializeField] private AnimationCurve skyboxExposureCurve = AnimationCurve.Linear(0, 0.5f, 1, 1f); // Curve for skybox exposure

    private void Update(){
        if(lightingSettings == null || skyboxMaterial == null)
            return;

        if(isAutoTime && Application.isPlaying){
            timeOfDay += Time.deltaTime / 100 * timeSpeed;
            if(timeOfDay > 24) 
                timeSettingsUI.timeSlider.value = 0;
            timeOfDay %= 24; // Wrap timeOfDay to stay within 0-24 range
        }

        UpdateLighting(timeOfDay / 24f);
        timeSettingsUI.UpdateTimeText();
    }

    // Updates lighting settings based on time of day percentage
    private void UpdateLighting(float timePercent){
        if(directionalLight != null){
            directionalLight.color = lightingSettings.directionalColor.Evaluate(timePercent);
            directionalLight.transform.localRotation = Quaternion.Euler(new Vector3((timePercent * 360f) - 90f, -170, 0));

            // Adjust the intensity of the directional light to simulate nighttime darkness
            directionalLight.intensity = Mathf.Lerp(1.0f, 0.0f, Mathf.Clamp01((timePercent - 0.5f) * 2));

        }

        // Set ambient light and fog color based on time of day
        RenderSettings.ambientLight = lightingSettings.ambientColor.Evaluate(timePercent);
        RenderSettings.fogColor = lightingSettings.fogColor.Evaluate(timePercent);

        // Adjust skybox color and exposure based on time of day
        skyboxMaterial.SetColor("_Tint", skyboxTint.Evaluate(timePercent));
        skyboxMaterial.SetFloat("_Exposure", skyboxExposureCurve.Evaluate(timePercent));
    }

    private void OnValidate(){
        if(directionalLight == null && RenderSettings.sun != null){
            directionalLight = RenderSettings.sun;
            Debug.Log("Directional light set from RenderSettings.sun");
        }
    }
}
