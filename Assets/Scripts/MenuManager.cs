using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void LoadTrainingMode()
    {
        SceneManager.LoadScene("MLTrainingScene");
    }

    public void LoadVSMode()
    {
        SceneManager.LoadScene("TrainingArena");
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}