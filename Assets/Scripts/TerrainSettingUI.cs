using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Add this line

public class TerrainSettingsUI : MonoBehaviour
{
    [Header("Map Previewer Reference")]
    public MapPreview mapPreview;

    [Header("ScriptableObject Reference")]
    public HeightMapSettings heightMapSettings;
    public MeshSettings meshSettings;

    [Header("UI Elements")]
    public Slider chunkSizeSlider;
    public InputField mapSizeInputField; // Ensure you have this defined
    public Slider noiseScaleSlider; // Ensure you have this defined
    public Dropdown octavesDropdown; // Ensure you have this defined
    public Slider persistenceSlider; // Ensure you have this defined
    public Slider lacunaritySlider; // Ensure you have this defined

    // New UI Elements for mapRadius and meshScale
    public Slider mapRadiusSlider;
    public TextMeshProUGUI mapRadiusText;
    public Slider meshScaleSlider;
    public TextMeshProUGUI meshScaleText;

    public TextMeshProUGUI chunkSizeText; // Text label for chunk size

    // Button to change scene
    public Button changeSceneButton; // Add a reference to the button

    private void Start()
    {
        // Set slider ranges
        chunkSizeSlider.minValue = 48;
        chunkSizeSlider.maxValue = 144;

        mapRadiusSlider.minValue = 0; // Adjust min value as needed
        mapRadiusSlider.maxValue = 2; // Adjust max value as needed

        meshScaleSlider.minValue = 1; // Adjust min value as needed
        meshScaleSlider.maxValue = 10; // Adjust max value as needed

        // Initialize UI with current ScriptableObject values
        chunkSizeSlider.value = meshSettings.chunkSize;
        mapRadiusSlider.value = meshSettings.mapRadius;
        meshScaleSlider.value = meshSettings.meshScale;

        // Add listeners to update ScriptableObject when UI changes
        chunkSizeSlider.onValueChanged.AddListener(OnChunkSizeChanged);
        mapRadiusSlider.onValueChanged.AddListener(OnMapRadiusChanged);
        meshScaleSlider.onValueChanged.AddListener(OnMeshScaleChanged);

        // Add listener for the change scene button
        changeSceneButton.onClick.AddListener(ChangeToTerrainEditorScene);

        // Update the text at the start
        UpdateChunkSizeText();
        UpdateMapRadiusText();
        UpdateMeshScaleText();

        mapPreview.DrawMapInEditor();
    }

    // Method to update ScriptableObject values
    private void OnChunkSizeChanged(float value)
    {
        meshSettings.chunkSize = Mathf.Clamp((int)value, (int)chunkSizeSlider.minValue, (int)chunkSizeSlider.maxValue);
        UpdateChunkSizeText();
        mapPreview.DrawMapInEditor();
    }

    private void OnMapRadiusChanged(float value)
    {
        meshSettings.mapRadius = Mathf.Clamp((int)value, (int)mapRadiusSlider.minValue, (int)mapRadiusSlider.maxValue);
        UpdateMapRadiusText();
        mapPreview.DrawMapInEditor();
    }

    private void OnMeshScaleChanged(float value)
    {
        meshSettings.meshScale = Mathf.Clamp(value, meshScaleSlider.minValue, meshScaleSlider.maxValue);
        UpdateMeshScaleText();
        mapPreview.DrawMapInEditor();
    }

    private void UpdateChunkSizeText()
    {
        chunkSizeText.text = $"{meshSettings.chunkSize}"; // Adjust formatting as needed
    }

    private void UpdateMapRadiusText()
    {
        mapRadiusText.text = $"{meshSettings.mapRadius}"; // Adjust formatting as needed
    }

    private void UpdateMeshScaleText()
    {
        meshScaleText.text = $"{meshSettings.meshScale:F2}"; // Adjust formatting as needed
    }

    // Method to change to the TerrainEditor scene
    private void ChangeToTerrainEditorScene()
    {
        SceneManager.LoadScene("TerrainEditor"); // Load the scene named "TerrainEditor"
    }
}
