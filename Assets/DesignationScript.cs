using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DesignationScript : MonoBehaviour, IPointerClickHandler
{
    public GameObject gameMaster;

    public GameObject creator;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameMaster.GetComponent<GameMaster>().isPaused)
            return;
        gameMaster.GetComponent<GameMaster>().target = creator;
    }
}
