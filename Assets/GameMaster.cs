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

    public List<GameObject> friendlyShips;

    public List<GameObject> friendlyMissiles;

    public List<GameObject> enemies;

    public List<GameObject> enemyShips;

    public List<GameObject> enemyMissiles;

    public bool isPaused = false;

    private bool gameOver = false;
    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(1776);
        //StartCoroutine(SpawnEnemies());
        //StartCoroutine(MissileSpawn());
        //StartCoroutine(MissileDispatch());
        //StartCoroutine(DetectionRoutine());
        friendlyShips.Add(Instantiate(friendlyPrefab, new Vector3(1, 0, 1), Quaternion.identity, friendlyOrganizer.GetComponent<Transform>()));
        friendlyShips[0].GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        friendlyShips[0].GetComponent<ShipScript>().gameMaster = gameObject;
        friendlyShips[0].GetComponent<ShipScript>().Startup();
        friendlyShips.Add(Instantiate(friendlyPrefab, new Vector3(1, 0, -1), Quaternion.identity, friendlyOrganizer.GetComponent<Transform>()));
        friendlyShips[1].GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        friendlyShips[1].GetComponent<ShipScript>().gameMaster = gameObject;
        friendlyShips[1].GetComponent<ShipScript>().Startup();
        friendlyShips.Add(Instantiate(friendlyPrefab, new Vector3(-1, 1, 0), Quaternion.identity, friendlyOrganizer.GetComponent<Transform>()));
        friendlyShips[2].GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        friendlyShips[2].GetComponent<ShipScript>().gameMaster = gameObject;
        friendlyShips[2].GetComponent<ShipScript>().Startup();
        friendlyShips.Add(Instantiate(friendlyPrefab, new Vector3(-1, -1, 0), Quaternion.identity, friendlyOrganizer.GetComponent<Transform>()));
        friendlyShips[3].GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        friendlyShips[3].GetComponent<ShipScript>().gameMaster = gameObject;
        friendlyShips[3].GetComponent<ShipScript>().Startup();
        friendlies.Add(friendlyShips[0]);
        friendlies.Add(friendlyShips[1]);
    }

    // Update is called once per frame
    void Update()
    {
        if (isPaused)
            return;
        if (friendlyShips.Count == 0 && !gameOver)
            gameOver = true;
        if (gameOver)
        {
            Time.timeScale = 0;
            return;
        }
        if (target && (!target.TryGetComponent<ShipScript>(out ShipScript non) || !non.detected))
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
        for (int i = 0; i < friendlyShips.Count; i++)
        {
            if (!friendlyShips[i].TryGetComponent<ShipScript>(out ShipScript non))
            {
                friendlyShips.RemoveAt(i);
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
        for (int i = 0; i < enemyShips.Count; i++)
        {
            if (!enemyShips[i].TryGetComponent<ShipScript>(out ShipScript non))
            {
                enemyShips.RemoveAt(i);
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

    void RandomSpawn(GameObject spawnPrefab, GameObject spawnOrganizer, List<GameObject> spawnList, List<GameObject> collectiveList, Vector3 location, Vector3 velocity, float radius, int count)
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
            collectiveList.Add(temp);
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
            RandomSpawn(enemyPrefab, enemyOrganizer, enemyShips, enemies, spawnLocation, spawnVelocity, 10, Random.Range(2, 11));
            // Spawn between 1-4 missiles
            RandomSpawn(enemyMissilePrefab, enemyMissileOrganizer, enemyMissiles, enemies, spawnLocation, spawnVelocity, 10, Random.Range(1, 5));
            // Yield return new waitforseconds 20-35 seconds
            yield return new WaitForSeconds(Random.Range(20, 35));
        }
    }

    IEnumerator MissileSpawn()
    {
        while (true)
        {
            RandomSpawn(friendlyMissilePrefab, friendlyMissileOrganizer, friendlyMissiles, friendlies, Vector3.zero, Vector3.zero, 10, 2);
            yield return new WaitForSeconds(10);
        }
    }

    IEnumerator MissileDispatch()
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

            if (friendlyShips.Count > 0)
            {
                enemyMissiles[i].GetComponent<ShipScript>().referenceBody = friendlyShips[Random.Range(0, friendlyShips.Count)].GetComponent<Rigidbody>();
                enemyMissiles[i].GetComponent<ShipScript>().maneuverMode = ShipScript.ManeuverMode.Intercept;
            }
            yield return new WaitForSeconds(2);
            i++;
        }
    }

    IEnumerator DetectionRoutine()
    {
        int i = 0;
        int j = 0;
        while (true)
        {
            if (i >= enemies.Count)
                i = 0;
            if (j >= friendlies.Count)
                j = 0;
            if (i < enemies.Count) // attempt to detect/track one enemy per loop
            {
                int num;
                bool detected = false;
                bool tracked = false;
                for (num = 0; num < friendlies.Count; num++)
                {
                    if (friendlies[num].GetComponent<ShipScript>().hasRadar)
                    {
                        float radarPower = friendlies[num].GetComponent<ShipScript>().radarPower;
                        float radarReturn = 0;
                        float rcs = enemies[i].GetComponent<ShipScript>().radarCrossSection;
                        if (friendlies[num].GetComponent<ShipScript>().radarIsOn)
                            radarReturn += radarPower * rcs / Mathf.Pow(Vector3.SqrMagnitude(friendlies[num].transform.position - enemies[i].transform.position), 2);
                        float radarSignal = radarReturn;
                        if (enemies[i].GetComponent<ShipScript>().radarIsOn)
                            radarSignal += enemies[i].GetComponent<ShipScript>().radarPower / Vector3.SqrMagnitude(friendlies[num].transform.position - enemies[i].transform.position);
                        if (radarReturn * radarPower > 16) // If the radar gets a strong return with its own waves, tracking is achieved
                        {
                            detected = true;
                            tracked = true;
                            break;
                        }
                        if (radarSignal * radarPower > 1) // IF the radar gets a weak combined radar signal, only detection is achieved
                            detected = true;
                    }
                    if (friendlies[num].GetComponent<ShipScript>().hasIR)
                    {
                        float iRSignal = enemies[i].GetComponent<ShipScript>().iRSignature / Vector3.SqrMagnitude(friendlies[num].transform.position - enemies[i].transform.position);
                        if (iRSignal * friendlies[num].GetComponent<ShipScript>().iRSensitivity > 1) // Determine if detection/tracking has been achieved
                        {
                            detected = true;
                            tracked = true;
                            break;
                        }
                    }
                }
                if (tracked)
                {
                    enemies[i].GetComponent<ShipScript>().setTracked();
                }
                else if (detected)
                {
                    enemies[i].GetComponent<ShipScript>().setDetected();
                }
                else
                {
                    enemies[i].GetComponent<ShipScript>().setUndetected();
                }
            }
            if (j < friendlies.Count) // attempt to detect/track one friendly per loop
            {
                int num;
                bool detected = false;
                bool tracked = false;
                for (num = 0; num < enemies.Count; num++)
                {
                    if (enemies[num].GetComponent<ShipScript>().hasRadar)
                    {
                        float radarPower = enemies[num].GetComponent<ShipScript>().radarPower;
                        float radarReturn = 0;
                        float rcs = friendlies[j].GetComponent<ShipScript>().radarCrossSection;
                        if (enemies[num].GetComponent<ShipScript>().radarIsOn)
                            radarReturn += radarPower * rcs / Mathf.Pow(Vector3.SqrMagnitude(enemies[num].transform.position - friendlies[j].transform.position), 2);
                        float radarSignal = radarReturn;
                        if (friendlies[j].GetComponent<ShipScript>().radarIsOn)
                            radarSignal += friendlies[j].GetComponent<ShipScript>().radarPower / Vector3.SqrMagnitude(enemies[num].transform.position - friendlies[j].transform.position);
                        if (radarReturn * radarPower > 16) // If the radar gets a strong return with its own waves, tracking is achieved
                        {
                            detected = true;
                            tracked = true;
                            break;
                        }
                        if (radarSignal * radarPower > 1) // IF the radar gets a weak combined radar signal, only detection is achieved
                            detected = true;
                    }
                    if (enemies[num].GetComponent<ShipScript>().hasIR)
                    {
                        float iRSignal = friendlies[j].GetComponent<ShipScript>().iRSignature / Vector3.SqrMagnitude(enemies[num].transform.position - friendlies[j].transform.position);
                        if (iRSignal * enemies[num].GetComponent<ShipScript>().iRSensitivity > 1) // Determine if detection/tracking has been achieved
                        {
                            detected = true;
                            tracked = true;
                            break;
                        }
                    }
                }
                if (tracked)
                {
                    friendlies[j].GetComponent<ShipScript>().tracked = true;
                    friendlies[j].GetComponent<ShipScript>().detected = true;
                }
                else if (detected)
                {
                    friendlies[j].GetComponent<ShipScript>().tracked = false;
                    friendlies[j].GetComponent<ShipScript>().detected = true;
                }
                else
                {
                    friendlies[j].GetComponent<ShipScript>().tracked = false;
                    friendlies[j].GetComponent<ShipScript>().detected = false;
                }
            }
            i++;
            j++;
            yield return null;
        }
    }
}
