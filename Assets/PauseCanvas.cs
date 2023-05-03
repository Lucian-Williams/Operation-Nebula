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
        gameMaster.SetActive(true);
        musicSource.Play();
        gameObject.SetActive(false);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
