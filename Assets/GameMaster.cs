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

    public GameObject enemyMissilePrefab;

    public GameObject enemyMissileOrganizer;

    public GameObject target;

    public GameObject targetSprite;

    public List<GameObject> friendlies;

    public List<GameObject> friendlyMissiles;

    public List<GameObject> enemies;

    public List<GameObject> enemyMissiles;

    public bool isPaused = false;

    private bool gameOver = false;
    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(1776);
        StartCoroutine(SpawnEnemies());
        StartCoroutine(SpawnMissiles());
        StartCoroutine(DispatchMissiles());
        friendlies.Add(Instantiate(friendlyPrefab, new Vector3(1, 0, -1), Quaternion.identity, friendlyOrganizer.GetComponent<Transform>()));
        friendlies[0].GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        friendlies[0].GetComponent<ShipScript>().gameMaster = gameObject;
        friendlies[0].GetComponent<ShipScript>().Startup();
        friendlies.Add(Instantiate(friendlyPrefab, new Vector3(1, 0, 1), Quaternion.identity, friendlyOrganizer.GetComponent<Transform>()));
        friendlies[1].GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        friendlies[1].GetComponent<ShipScript>().gameMaster = gameObject;
        friendlies[1].GetComponent<ShipScript>().Startup();
    }

    // Update is called once per frame
    void Update()
    {
        if (isPaused)
            return;
        if (friendlies.Count == 0 && !gameOver)
            gameOver = true;
        if (gameOver)
        {
            Time.timeScale = 0;
            return;
        }
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
        for (int i = 0; i < enemyMissiles.Count; i++)
        {
            if (!enemyMissiles[i].TryGetComponent<ShipScript>(out ShipScript non))
            {
                enemyMissiles.RemoveAt(i);
                i--;
            }
        }
    }

    void RandomSpawn(GameObject spawnPrefab, GameObject spawnOrganizer, List<GameObject> spawnList, Vector3 location, Vector3 velocity, float radius, int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject temp = Instantiate(
                spawnPrefab,
                location + new Vector3(Random.Range(-radius, radius), Random.Range(-radius, radius), Random.Range(-radius, radius)),
                Quaternion.identity,
                spawnOrganizer.GetComponent<Transform>()
                );
            temp.GetComponent<Rigidbody>().velocity = velocity + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
            temp.GetComponent<ShipScript>().gameMaster = gameObject;
            temp.GetComponent<ShipScript>().Startup();
            spawnList.Add(temp);
        }
    }

    IEnumerator SpawnEnemies()
    {
        while (true)
        {
            Vector3 direction = Random.onUnitSphere;
            Vector3 spawnLocation = direction * Random.Range(100, 200);
            Vector3 spawnVelocity = -direction * Random.Range(1, 2);
            // Spawm between 2-10 targets
            RandomSpawn(enemyPrefab, enemyOrganizer, enemies, spawnLocation, spawnVelocity, 10, Random.Range(2, 11));
            // Spawn between 1-4 missiles
            RandomSpawn(enemyMissilePrefab, enemyMissileOrganizer, enemyMissiles, spawnLocation, spawnVelocity, 10, Random.Range(1, 5));
            // Yield return new waitforseconds 20-35 seconds
            yield return new WaitForSeconds(Random.Range(20, 35));
        }
    }

    IEnumerator SpawnMissiles()
    {
        while (true)
        {
            RandomSpawn(friendlyMissilePrefab, friendlyMissileOrganizer, friendlyMissiles, Vector3.zero, Vector3.zero, 10, 2);
            yield return new WaitForSeconds(10);
        }
    }

    IEnumerator DispatchMissiles()
    {
        int i = 0;
        while (true)
        {
            if (enemyMissiles.Count < 1)
            {
                yield return new WaitForSeconds(2);
                continue;
            }
            if (i >= enemyMissiles.Count)
            {
                i = i % enemyMissiles.Count;
            }

            if (friendlies.Count > 0)
            {
                enemyMissiles[i].GetComponent<ShipScript>().referenceBody = friendlies[Random.Range(0, friendlies.Count)].GetComponent<Rigidbody>();
                enemyMissiles[i].GetComponent<ShipScript>().maneuverMode = ShipScript.ManeuverMode.Intercept;
            }
            yield return new WaitForSeconds(2);
            i++;
        }
    }
}
