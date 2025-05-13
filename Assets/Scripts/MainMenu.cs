using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject controlsPanel;
    public GameObject pausePanel;
   

    private bool isPaused = false;

    private Rigidbody playerRb;

    void Start()
    {
        // Initialize the player's Rigidbody
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("ForestScene");
    }

    public void OpenPanel()
    {
        controlsPanel.SetActive(true);
    }

    public void ClosePanel()
    {
        controlsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }

    public void PauseGame()
    {
        if (!isPaused)
        {
            Time.timeScale = 0f; // Freeze game time
            pausePanel.SetActive(true); // Show the pause panel
            isPaused = true;

           
        }
    }

    public void ResumeGame()
    {
        if (isPaused)
        {
            Time.timeScale = 1f; // Resume game time
            pausePanel.SetActive(false); // Hide the pause panel
            isPaused = false;

          
        }
    }

    void Update()
    {
        // Add any other specific checks here if needed (e.g., checking input for pause/unpause)
    }
}
