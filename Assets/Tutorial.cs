using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tutorial : MonoBehaviour
{
    public GameObject gameMaster;

    public GameObject panel1;

    public GameObject panel2;

    public GameObject panel3;

    public GameObject panel4;

    public GameObject panel5;

    int phase;

    // Start is called before the first frame update
    void Start()
    {
        phase = 0;
    }

    public void Continue()
    {
        switch (phase)
        {
            case 0:
                panel1.SetActive(false);
                panel2.SetActive(true);
                phase++;
                break;
            case 1:
                panel2.SetActive(false);
                panel3.SetActive(true);
                phase++;
                break;
            case 2:
                panel3.SetActive(false);
                panel4.SetActive(true);
                phase++;
                break;
            case 3:
                panel4.SetActive(false);
                panel5.SetActive(true);
                phase++;
                break;
            default:
                panel5.SetActive(false);
                Time.timeScale = 1;
                gameObject.SetActive(false);
                break;
        }
    }
}
