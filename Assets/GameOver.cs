using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    public void Restart()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void RestartTutorial()
    {
        SceneManager.LoadScene("TutorialScene");
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
