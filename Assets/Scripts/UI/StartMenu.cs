using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Title Screen Menu Controller for Canvas UI Play and Quit buttons.
/// </summary>
public class StartMenu : MonoBehaviour
{
    public void PlayGame()
    {
        if (SceneManager.sceneCountInBuildSettings > 1)
        {
            SceneManager.LoadScene(1);
        }
        else
        {
            SceneManager.LoadScene("SampleScene");
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
