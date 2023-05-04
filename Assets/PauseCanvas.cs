using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseCanvas : MonoBehaviour
{
    public GameObject gameMaster;

    public AudioSource musicSource;

    public void Play()
    {
        musicSource.Play();
        gameObject.SetActive(false);
        Time.timeScale = 1;
        return;
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
