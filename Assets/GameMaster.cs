using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMaster : MonoBehaviour
{
    public GameObject friendlyPrefab;

    public GameObject friendlyOrganizer;

    public GameObject friendlyMissilePrefab;

    public GameObject friendlyMissileOrganizer;

    public GameObject enemyPrefab;

    public GameObject enemyOrganizer;

    public GameObject target;

    public GameObject targetSprite;

    public List<GameObject> friendlies;

    public List<GameObject> friendlyMissiles;

    public List<GameObject> enemies;
    // Start is called before the first frame update
    void Start()
    {
        enemies.Add(Instantiate(enemyPrefab, new Vector3(90, 0, 10), Quaternion.identity, enemyOrganizer.GetComponent<Transform>()));
        enemies[0].GetComponent<Rigidbody>().velocity = new Vector3(-2, -0.1f, 0.1f);
        enemies[0].GetComponent<ShipScript>().gameMaster = gameObject;
        enemies[0].GetComponent<ShipScript>().Startup();
        enemies.Add(Instantiate(enemyPrefab, new Vector3(120, 10, 0), Quaternion.identity, enemyOrganizer.GetComponent<Transform>()));
        enemies[1].GetComponent<Rigidbody>().velocity = new Vector3(-2.2f, -0.1f, 0);
        enemies[1].GetComponent<ShipScript>().gameMaster = gameObject;
        enemies[1].GetComponent<ShipScript>().Startup();
        friendlies.Add(Instantiate(friendlyPrefab, new Vector3(1, 0, -1), Quaternion.identity, friendlyOrganizer.GetComponent<Transform>()));
        friendlies[0].GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        friendlies[0].GetComponent<ShipScript>().gameMaster = gameObject;
        friendlies[0].GetComponent<ShipScript>().Startup();
        friendlies.Add(Instantiate(friendlyPrefab, new Vector3(1, 0, 1), Quaternion.identity, friendlyOrganizer.GetComponent<Transform>()));
        friendlies[1].GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        friendlies[1].GetComponent<ShipScript>().gameMaster = gameObject;
        friendlies[1].GetComponent<ShipScript>().Startup();
        friendlyMissiles.Add(Instantiate(friendlyMissilePrefab, new Vector3(1, -1, 0), Quaternion.identity, friendlyMissileOrganizer.GetComponent<Transform>()));
        friendlyMissiles[0].GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        friendlyMissiles[0].GetComponent<ShipScript>().gameMaster = gameObject;
        friendlyMissiles[0].GetComponent<ShipScript>().Startup();
        friendlyMissiles.Add(Instantiate(friendlyMissilePrefab, new Vector3(1, 1, 0), Quaternion.identity, friendlyMissileOrganizer.GetComponent<Transform>()));
        friendlyMissiles[1].GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        friendlyMissiles[1].GetComponent<ShipScript>().gameMaster = gameObject;
        friendlyMissiles[1].GetComponent<ShipScript>().Startup();
    }

    // Update is called once per frame
    void Update()
    {
        if (target && !target.TryGetComponent<ShipScript>(out ShipScript non))
            target = null;
        CleanShips();
        if (target)
        {
            targetSprite.transform.position = target.GetComponent<ShipScript>().marker.transform.position;
            if (!targetSprite.activeSelf)
            {
                targetSprite.SetActive(true);
            }
        }
        if (!target && targetSprite.activeSelf)
        {
            targetSprite.SetActive(false);
        }
    }

    public void DispatchMissile()
    {
        if (!target)
            return;
        for (int i = 0; i < friendlyMissiles.Count; i++)
        {
            if (!friendlyMissiles[i].GetComponent<ShipScript>().referenceBody)
            {
                friendlyMissiles[i].GetComponent<ShipScript>().referenceBody = target.GetComponent<Rigidbody>();
                friendlyMissiles[i].GetComponent<ShipScript>().maneuverMode = ShipScript.ManeuverMode.Intercept;
                return;
            }
        }
    }

    void CleanShips()
    {
        for (int i = 0; i < friendlies.Count; i++)
        {
            if (!friendlies[i].TryGetComponent<ShipScript>(out ShipScript non))
            {
                friendlies.RemoveAt(i);
                i--;
            }
        }
        for (int i = 0; i < friendlyMissiles.Count; i++)
        {
            if (!friendlyMissiles[i].TryGetComponent<ShipScript>(out ShipScript non))
            {
                friendlyMissiles.RemoveAt(i);
                i--;
            }
        }
        for (int i = 0; i < enemies.Count; i++)
        {
            if (!enemies[i].TryGetComponent<ShipScript>(out ShipScript non))
            {
                enemies.RemoveAt(i);
                i--;
            }
        }
    }

    void RandomSpawn(GameObject spawnPrefab, GameObject spawnOrganizer, List<GameObject> spawnList, float x, float y, float z, float size)
    {

    }
}
