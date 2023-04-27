using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class AudioVolumeOptions : MonoBehaviour
{
    public AudioMixer audioMixer;

    public Slider masterVolume;

    public Slider musicVolume;

    public Slider sfxVolume;

    private void Start()
    {
        if (PlayerPrefs.GetInt("First Play") == 0)
        {
            PlayerPrefs.SetInt("First Play", 1);
            PlayerPrefs.SetFloat("MasterVolume", 0.5f);
            PlayerPrefs.SetFloat("MusicVolume", 0.5f);
            PlayerPrefs.SetFloat("SFXVolume", 0.5f);
        }

        masterVolume.value = PlayerPrefs.GetFloat("MasterVolume");
        musicVolume.value = PlayerPrefs.GetFloat("MusicVolume");
        sfxVolume.value = PlayerPrefs.GetFloat("SFXVolume");
        SetMasterVolume();
        SetMusicVolume();
        SetSFXVolume();
    }

    public void SetMasterVolume()
    {
        SetVolume("MasterVolume", masterVolume.value);
    }

    public void SetMusicVolume()
    {
        SetVolume("MusicVolume", musicVolume.value);
    }

    public void SetSFXVolume()
    {
        SetVolume("SFXVolume", sfxVolume.value);
    }

    void SetVolume(string name, float value)
    {
        float volume;
        if (value == 0.0f)
            volume = -80;
        else
            volume = Mathf.Log10(value) * 20;
        audioMixer.SetFloat(name, volume);
        PlayerPrefs.SetFloat(name, value);
    }
}
