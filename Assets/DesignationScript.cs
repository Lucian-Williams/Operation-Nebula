using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DesignationScript : MonoBehaviour, IPointerClickHandler
{
    public GameObject gameMaster;

    public GameObject creator;

    public TaskForce taskForce;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!gameMaster.activeInHierarchy)
            return;
        gameMaster.GetComponent<GameMaster>().SetTarget(creator, taskForce);
    }
}
