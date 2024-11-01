using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Pause : MonoBehaviour
{    
    public Button resumeButton;
    public Button changeSceneButton;
    public Button exitButton;

    private void Start()
    {
        resumeButton.onClick.AddListener(Resume);
        changeSceneButton.onClick.AddListener(ChangeToTerrainEditorScene);
        exitButton.onClick.AddListener(ExitProgram);
    }
    private void Resume()
    {

    }
    private void ExitProgram()
    {
        // Exits the application
        Application.Quit();

    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #endif
    }
    private void ChangeToTerrainEditorScene() => SceneManager.LoadScene("Main Menu");
}
