using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class TerrainSettingsUI : MonoBehaviour
{
    [Header("Map Previewer Reference")]
    public MapPreview mapPreview;

    [Header("ScriptableObject Reference")]
    public HeightMapSettings heightMapSettings;
    public MeshSettings meshSettings;

    [Header("UI Elements")]
    public Slider chunkSizeSlider;
    public Slider noiseScaleSlider; 
    public TextMeshProUGUI noiseScaleText;
    public Slider octavesSlider;
    public TextMeshProUGUI octavesText;
    public Slider persistanceSlider;
    public TextMeshProUGUI persistanceText;
    public Slider lacunaritySlider;
    public TextMeshProUGUI lacunarityText;
    public Slider heightMultiplierSlider;
    public TextMeshProUGUI heightMultiplierText;
    public TMP_Dropdown mapRadiusDropdown;
    public Slider meshScaleSlider;
    public TextMeshProUGUI meshScaleText;
    public TextMeshProUGUI chunkSizeText;
    public Button changeSceneButton;

    // New UI Elements for noiseSeed and offset
    public TMP_InputField noiseSeedInputField;
    public TMP_InputField offsetXInputField;
    public TMP_InputField offsetYInputField;

    private void Start()
    {
        // Set slider ranges and initial values
        chunkSizeSlider.minValue = 48;
        chunkSizeSlider.maxValue = 144;
        chunkSizeSlider.wholeNumbers = true;

        meshScaleSlider.minValue = 1.0f;
        meshScaleSlider.maxValue = 5.0f;

        noiseScaleSlider.minValue = 1.0f;
        noiseScaleSlider.maxValue = 100.0f;

        persistanceSlider.minValue = 0.1f;
        persistanceSlider.maxValue = 1.0f;

        lacunaritySlider.minValue = 1.0f;
        lacunaritySlider.maxValue = 8.0f;

        octavesSlider.minValue = 1;
        octavesSlider.maxValue = 6;
        octavesSlider.wholeNumbers = true;

        heightMultiplierSlider.minValue = 0.1f;
        heightMultiplierSlider.maxValue = 100.0f;

        // Initialize dropdown options for map radius
        mapRadiusDropdown.ClearOptions();
        mapRadiusDropdown.AddOptions(new List<string> { "0", "1", "2" });
        mapRadiusDropdown.value = (int)meshSettings.mapRadius;
        mapRadiusDropdown.onValueChanged.AddListener(OnMapRadiusChanged);

        // Initialize UI with current ScriptableObject values
        chunkSizeSlider.value = meshSettings.chunkSize;
        noiseScaleSlider.value = heightMapSettings.noiseSettings.scale;
        octavesSlider.value = heightMapSettings.noiseSettings.octaves;
        persistanceSlider.value = heightMapSettings.noiseSettings.persistance;
        lacunaritySlider.value = heightMapSettings.noiseSettings.lacunarity;
        heightMultiplierSlider.value = heightMapSettings.heightMultiplier;
        meshScaleSlider.value = meshSettings.meshScale;

        // Set initial values for noiseSeed and offset
        noiseSeedInputField.text = heightMapSettings.noiseSettings.seed.ToString();
        offsetXInputField.text = heightMapSettings.noiseSettings.offset.x.ToString();
        offsetYInputField.text = heightMapSettings.noiseSettings.offset.y.ToString();

        // Add listeners to update ScriptableObject when UI changes
        chunkSizeSlider.onValueChanged.AddListener(OnChunkSizeChanged);
        noiseScaleSlider.onValueChanged.AddListener(OnNoiseScaleChanged);
        octavesSlider.onValueChanged.AddListener(OnOctavesChanged);
        persistanceSlider.onValueChanged.AddListener(OnPersistanceChanged);
        lacunaritySlider.onValueChanged.AddListener(OnLacunarityChanged);
        heightMultiplierSlider.onValueChanged.AddListener(OnHeightMultiplierChanged);
        meshScaleSlider.onValueChanged.AddListener(OnMeshScaleChanged);

        noiseSeedInputField.onEndEdit.AddListener(OnNoiseSeedChanged);
        offsetXInputField.onEndEdit.AddListener(OnOffsetXChanged);
        offsetYInputField.onEndEdit.AddListener(OnOffsetYChanged);

        changeSceneButton.onClick.AddListener(ChangeToTerrainEditorScene);

        // Update text fields initially
        UpdateNoiseScaleText();
        UpdateChunkSizeText();
        UpdateMeshScaleText();
        UpdatePersistanceText();
        UpdateLacunarityText();
        UpdateHeightMultiplierText();
        UpdateOctavesText();

        mapPreview.DrawMapInEditor();
    }
    // Method to update ScriptableObject values
    private void OnChunkSizeChanged(float value)
    {
        meshSettings.chunkSize = Mathf.Clamp((int)value, (int)chunkSizeSlider.minValue, (int)chunkSizeSlider.maxValue);
        UpdateChunkSizeText();
        mapPreview.DrawMapInEditor();
    }

    private void OnMapRadiusChanged(int value)
    {
        meshSettings.mapRadius = value; // Update mapRadius based on dropdown selection
        mapPreview.DrawMapInEditor();
    }

    private void OnMeshScaleChanged(float value)
    {
        meshSettings.meshScale = Mathf.Clamp(value, meshScaleSlider.minValue, meshScaleSlider.maxValue);
        UpdateMeshScaleText();
        mapPreview.DrawMapInEditor();
    }

    private void OnNoiseScaleChanged(float value)
    {
        heightMapSettings.noiseSettings.scale = Mathf.Clamp(value, noiseScaleSlider.minValue, noiseScaleSlider.maxValue);
        UpdateNoiseScaleText();
        mapPreview.DrawMapInEditor();
    }

    private void OnPersistanceChanged(float value)
    {
        heightMapSettings.noiseSettings.persistance = Mathf.Clamp(value, persistanceSlider.minValue, persistanceSlider.maxValue);
        UpdatePersistanceText();
        mapPreview.DrawMapInEditor();
    }

    private void OnLacunarityChanged(float value)
    {
        heightMapSettings.noiseSettings.lacunarity = Mathf.Clamp(value, lacunaritySlider.minValue, lacunaritySlider.maxValue);
        UpdateLacunarityText();
        mapPreview.DrawMapInEditor();
    }

    private void OnHeightMultiplierChanged(float value)
    {
        heightMapSettings.heightMultiplier = Mathf.Clamp(value, heightMultiplierSlider.minValue, heightMultiplierSlider.maxValue);
        UpdateHeightMultiplierText();
        mapPreview.DrawMapInEditor();
    }

    private void OnOctavesChanged(float value)
    {
        heightMapSettings.noiseSettings.octaves = Mathf.Clamp((int)value, (int)octavesSlider.minValue, (int)octavesSlider.maxValue);
        UpdateOctavesText();
        mapPreview.DrawMapInEditor();
    }

    private void OnNoiseSeedChanged(string value)
    {
        if (int.TryParse(value, out int seed))
        {
            heightMapSettings.noiseSettings.seed = seed;
            mapPreview.DrawMapInEditor();
        }
    }

    private void OnOffsetXChanged(string value)
    {
        if (float.TryParse(value, out float offsetX))
        {
            heightMapSettings.noiseSettings.offset.x = offsetX;
            mapPreview.DrawMapInEditor();
        }
    }

    private void OnOffsetYChanged(string value)
    {
        if (float.TryParse(value, out float offsetY))
        {
            heightMapSettings.noiseSettings.offset.y = offsetY;
            mapPreview.DrawMapInEditor();
        }
    }

    // Methods to update text fields
    private void UpdateChunkSizeText() => chunkSizeText.text = $"{meshSettings.chunkSize}";
    private void UpdateMeshScaleText() => meshScaleText.text = $"{meshSettings.meshScale:F2}";
    private void UpdatePersistanceText() => persistanceText.text = $"{heightMapSettings.noiseSettings.persistance:F2}";
    private void UpdateLacunarityText() => lacunarityText.text = $"{heightMapSettings.noiseSettings.lacunarity:F2}";
    private void UpdateHeightMultiplierText() => heightMultiplierText.text = $"{heightMapSettings.heightMultiplier:F2}";
    private void UpdateOctavesText() => octavesText.text = $"{heightMapSettings.noiseSettings.octaves}"; // Update text for octaves
    private void UpdateNoiseScaleText() => noiseScaleText.text = $"{heightMapSettings.noiseSettings.scale}"; // Update text for octaves

    // Method to change to the TerrainEditor scene
    private void ChangeToTerrainEditorScene() => SceneManager.LoadScene("TerrainEditor");
}
