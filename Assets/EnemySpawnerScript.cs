using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnerScript : MonoBehaviour
{
    public GameObject scoutPrefab;

    public GameObject parent;

    public List<GameObject> enemyShips;

    // Start is called before the first frame update
    void Start()
    {
        enemyShips.Add(Instantiate(scoutPrefab, new Vector3(1000, 0, 0), Quaternion.identity, parent.GetComponent<Transform>()));
        enemyShips[0].GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        enemyShips[0].GetComponent<ShipScript>().Startup();
        enemyShips.Add(Instantiate(scoutPrefab, new Vector3(0, 500, 0), Quaternion.identity, parent.GetComponent<Transform>()));
        enemyShips[1].GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        enemyShips[1].GetComponent<ShipScript>().Startup();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
