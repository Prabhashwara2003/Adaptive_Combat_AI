using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health playerHealth;
    [SerializeField] private Health agentHealth;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI winnerText;

    void Update()
    {
        CheckGameOver();
    }

    void CheckGameOver()
    {
        if (!playerHealth.IsAlive)
        {
            ShowGameOver("AI WINS!");
        }
        else if (!agentHealth.IsAlive)
        {
            ShowGameOver("PLAYER WINS!");
        }
    }

    void ShowGameOver(string message)
    {
        gameOverPanel.SetActive(true);
        winnerText.text = message;
        Time.timeScale = 0f; // Pause game
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}