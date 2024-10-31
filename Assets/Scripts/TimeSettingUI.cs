using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimeSettingsUI : MonoBehaviour
{
    public GameObject timePanel;
    public Slider timeSlider;
    public TextMeshProUGUI sizeText;
    public Slider timeSpeedSlider;
    public TextMeshProUGUI timeSpeedText;

    public Toggle autoTimeToggle; // Add a Toggle for auto time

    public LightingManager lightingManager; 

    void Start()
    {
        timeSlider.minValue = 0f;
        timeSlider.maxValue = 24.0f;

        timeSpeedSlider.minValue = 5f;
        timeSpeedSlider.maxValue = 100.0f;

        timePanel.SetActive(false);

        timeSlider.value = lightingManager.timeOfDay;
        timeSpeedSlider.value = lightingManager.timeSpeed;

        // Set the toggle's initial state based on isAutoTime
        autoTimeToggle.isOn = lightingManager.isAutoTime;

        timeSlider.onValueChanged.AddListener(OnTimeSliderChanged);
        timeSpeedSlider.onValueChanged.AddListener(OnTimeSpeedSliderChanged);
        autoTimeToggle.onValueChanged.AddListener(OnAutoTimeToggleChanged);

        UpdateTimeText();
        UpdateTimeSpeedText();
    }

    public void ToggleTimePanel()
    {
        timePanel.SetActive(!timePanel.activeSelf);
    }

    private void OnTimeSliderChanged(float value)
    {
        float newTime = Mathf.Clamp(value, timeSlider.minValue, timeSlider.maxValue);
        lightingManager.timeOfDay = newTime;
        UpdateTimeText();
    }

    private void OnTimeSpeedSliderChanged(float value)
    {
        int newTimeSpeed = (int) Mathf.Clamp(value, timeSpeedSlider.minValue, timeSpeedSlider.maxValue);
        lightingManager.timeSpeed = newTimeSpeed;
        UpdateTimeSpeedText();
    }

    private void OnAutoTimeToggleChanged(bool isOn)
    {
        lightingManager.isAutoTime = isOn; // Update isAutoTime based on the toggle's value
    }

    public void UpdateTimeText()
    {
        int hours = (int)lightingManager.timeOfDay;
        int minutes = (int)((lightingManager.timeOfDay - hours) * 60); // Calculate minutes from the decimal part
        sizeText.text = $"{hours:00}:{minutes:00}"; // Format as HH:mm
    }
    private void UpdateTimeSpeedText() => timeSpeedText.text = $"{lightingManager.timeSpeed:F2}";
}
