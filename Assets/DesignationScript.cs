using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DesignationScript : MonoBehaviour, IPointerClickHandler
{
    public GameObject gameMaster;

    public GameObject creator;

    public TaskForce taskForce;

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
        if (!gameMaster.activeInHierarchy)
            return;
        gameMaster.GetComponent<GameMaster>().SetTarget(creator, taskForce);
    }
}
