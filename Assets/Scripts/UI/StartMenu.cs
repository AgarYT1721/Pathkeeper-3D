using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple UI menu controller for Start and Quit buttons.
/// </summary>
public class StartMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
