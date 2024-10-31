using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BrushSettingsUI : MonoBehaviour
{
    public GameObject brushPanel;
    public Slider sizeSlider;
    public TextMeshProUGUI sizeText;
    public Slider speedSlider;
    public TextMeshProUGUI speedText;
    public TerrainDeformer terrainDeformer;  // Reference to the TerrainDeformer

    void Start()
    {
        sizeSlider.minValue = 5f;
        sizeSlider.maxValue = 100.0f;

        speedSlider.minValue = 1f;
        speedSlider.maxValue = 20.0f;

        brushPanel.SetActive(false);

        sizeSlider.value = terrainDeformer.deformRadius;
        speedSlider.value = terrainDeformer.deformSpeed;

        sizeSlider.onValueChanged.AddListener(OnSizeSliderChanged);
        speedSlider.onValueChanged.AddListener(OnSpeedSliderChanged);

        UpdateSizeText();
        UpdateSpeedText();
    }

    public void ToggleBrushPanel()
    {
        brushPanel.SetActive(!brushPanel.activeSelf);
    }

    private void OnSizeSliderChanged(float value)
    {
        float newSize = Mathf.Clamp(value, sizeSlider.minValue, sizeSlider.maxValue);
        terrainDeformer.SetBrushRadius(newSize);
        UpdateSizeText();
    }

    private void OnSpeedSliderChanged(float value)
    {
        float newSpeed = Mathf.Clamp(value, speedSlider.minValue, speedSlider.maxValue);
        terrainDeformer.SetBrushSpeed(newSpeed);
        UpdateSpeedText();
    }

    private void UpdateSizeText() => sizeText.text = $"{terrainDeformer.deformRadius:F2}";
    private void UpdateSpeedText() => speedText.text = $"{terrainDeformer.deformSpeed:F2}";
}
