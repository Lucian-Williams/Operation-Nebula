using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenOptions : MonoBehaviour
{
    public Dropdown dropdown;

    public Toggle isFullScreen;

    Resolution[] resolutions;

    // Start is called before the first frame update
    void Start()
    {
        resolutions = Screen.resolutions;
        isFullScreen.isOn = Screen.fullScreen;
        for (int i = 0; i < resolutions.Length; i++)
        {
            string resolutionString = resolutions[i].width.ToString() + "x" + resolutions[i].height.ToString();
            dropdown.options.Add(new Dropdown.OptionData(resolutionString));
            if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
            {
                dropdown.value = i;
            }
        }
    }

    public void SetResolution()
    {
        Screen.SetResolution(resolutions[dropdown.value].width, resolutions[dropdown.value].height, true);
        Screen.fullScreen = isFullScreen.isOn;
    }
}
