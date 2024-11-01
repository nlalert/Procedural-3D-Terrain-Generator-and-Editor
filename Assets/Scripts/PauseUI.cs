using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseUI : MonoBehaviour
{    
    public Button resumeButton;
    public Button changeSceneButton;
    public Button exitButton;
    public GameObject escapePanel; // Add this for the ESC panel
    public GameObject toolPanel; // Parent GameObject for all other UI elements
    public static bool isPaused = false;

    private void Start()
    {
        resumeButton.onClick.AddListener(Resume);
        changeSceneButton.onClick.AddListener(ChangeToTerrainEditorScene);
        exitButton.onClick.AddListener(ExitProgram);

        escapePanel.SetActive(false); // Make sure the panel is hidden initially
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        isPaused = !isPaused;
        escapePanel.SetActive(isPaused);
        toolPanel.SetActive(!isPaused); // Hide main UI when paused
        if (isPaused)
        {
            Time.timeScale = 0; // Pause game
        }
        else
        {
            Time.timeScale = 1; // Resume game
        }
    }

    private void Resume()
    {
        isPaused = false;
        escapePanel.SetActive(false);
        toolPanel.SetActive(true); // Show main UI when resuming
        Time.timeScale = 1; // Resume game time
    }
    private void ExitProgram()
    {
        // Exits the application
        Application.Quit();

    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #endif
    }
    private void ChangeToTerrainEditorScene(){
        isPaused = false;
        Time.timeScale = 1; // Ensure game time is normal when changing scenes
        SceneManager.LoadScene("Main Menu");
    }
        
}
