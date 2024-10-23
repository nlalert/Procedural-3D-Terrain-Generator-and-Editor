using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class LightingManager : MonoBehaviour
{
    [SerializeField] private Light directionalLight;
    [SerializeField] private LightingSettings lightingSettings;
    [SerializeField] private float timeSpeed = 5;
    [SerializeField, Range (0, 24)] private float timeOfDay;

    private void Update(){
        if(lightingSettings == null)
            return;
        
        if(Application.isPlaying){
            timeOfDay += Time.deltaTime / 100 * timeSpeed;
            timeOfDay %= 24; // clamp 0-24
        }

        UpdateLighting(timeOfDay / 24f);
    }

    private void UpdateLighting(float timePercent){
        RenderSettings.ambientLight = lightingSettings.ambientColor.Evaluate(timePercent);
        RenderSettings.fogColor = lightingSettings.fogColor.Evaluate(timePercent);

        if(directionalLight != null){
            directionalLight.color = lightingSettings.directionalColor.Evaluate(timePercent);
            directionalLight.transform.localRotation = Quaternion.Euler(new Vector3((timePercent * 360f) - 90f, -170, 0));
        }
    }

    private void OnValidate(){
        if(directionalLight != null)
            return;

        if(RenderSettings.sun != null){
            directionalLight = RenderSettings.sun;
        }
        else{
            Light[] lights = GameObject.FindObjectsOfType<Light>();
            foreach(Light light in lights){
                if(light.type == LightType.Directional){
                    directionalLight = light;
                    return;
                }
            }
        }
    }
}
