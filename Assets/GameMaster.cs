using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMaster : MonoBehaviour
{
    public GameObject friendlyPrefab;

    public GameObject friendlyMissilePrefab;

    public GameObject enemyPrefab;

    public GameObject enemyMissilePrefab;

    public GameObject target;

    public GameObject targetSprite;

    public Rigidbody fleetCenter;

    public AudioSource alarmSource;

    bool isPaused = false;

    List<List<GameObject>> taskForces;

    List<List<GameObject>> friendlyTaskForces;

    List<List<GameObject>> enemyTaskForces;

    bool gameOver = false;
    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(1776);
        taskForces = new List<List<GameObject>>();
        friendlyTaskForces = new List<List<GameObject>>();
        enemyTaskForces = new List<List<GameObject>>();
        StartCoroutine(SpawnEnemies());
        StartCoroutine(TargetSelection());
        StartCoroutine(DetectionRoutine());
        StartCoroutine(GradientRoutine());
        SpawnFriendlies();
    }

    // Update is called once per frame
    void Update()
    {
        // Deprecte isPaused, it's a dumb field to have anyways
        if (isPaused)
        {
            alarmSource.enabled = false;
            return;
        }
        // XXX Change GameOver condition -- STOP REFERRING TO FRIENDLYSHIPS
        //if (friendlyShips.Count == 0 && !gameOver)
        //gameOver = true;
        // Setting the game master inactive will stop all children of the gamemaster, including their coroutines, preferable to changing the timescale
        // Deprecate, using timeScale to stop the game is inferior to simply deactivating the GameMaster and all its children
        if (gameOver)
        {
            Time.timeScale = 0;
            return;
        }
        if (target && (!target.TryGetComponent<ShipScript>(out ShipScript ship) || !ship.detected)) // This is good for now
            target = null;
        CleanShips();
        // Start Graphical update to target marker position
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
        // End Graphical update to target marker position
        // Start checks to activate or deactivate the missile warning (alarm)
        bool setAlarm = false;
        for (int i = 0; i < enemyTaskForces.Count; i++)
        {
            for (int j = 0; j < enemyTaskForces[i].Count; j++)
            {
                ShipScript enemyShip = enemyTaskForces[i][j].GetComponent<ShipScript>();
                if (enemyShip.detected && enemyShip.shipClass == ShipScript.ShipClass.Missile && Vector3.SqrMagnitude(enemyShip.transform.position) < 2500)
                {
                    setAlarm = true;
                    break;
                }
            }
            if (setAlarm)
                break;
        }
        alarmSource.enabled = setAlarm;
        // End checks to activate or deactivate the missile warning (alarm)
    }
    /*
    public void DispatchMissile() // Deprecate, ships will use their rules of engagement to select targets
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
    }*/

    // Removes destroyed ships from task forces, necessary so that ships don't try to interact with destroyed ships
    void CleanShips()
    {
        for (int i = 0; i < taskForces.Count; i++) // Loop through all task forces
        {
            for (int j = 0; j < taskForces[i].Count; j++) // Loop through all ships in the task force
            {
                if (!taskForces[i][j].TryGetComponent<ShipScript>(out ShipScript ship)) // If the ship no longer has a ShipScript (meaning it was destroyed), remove from the task force
                {
                    taskForces[i].RemoveAt(j);
                    j--; // Since removal moves the tail of the list up, decrement the index for the next ship in the task force
                }
            }
        }
    }

    // Fix
    List<GameObject> RandomSpawn(GameObject spawnPrefab, ShipDesign shipDesign, Vector3 location, Vector3 velocity, float radius, int count)
    {
        List<GameObject> taskForce = new List<GameObject>();
        for (int i = 0; i < count; i++)
        {
            GameObject temp = Instantiate(
                spawnPrefab,
                location + new Vector3(Random.Range(-radius, radius), Random.Range(-radius, radius), Random.Range(-radius, radius)),
                Quaternion.identity,
                transform
                );
            temp.GetComponent<ShipScript>().UseDesign(shipDesign);
            temp.GetComponent<Rigidbody>().velocity = velocity + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
            temp.GetComponent<ShipScript>().gameMaster = gameObject;
            temp.GetComponent<ShipScript>().Startup();
            taskForce.Add(temp);
        }
        return taskForce;
    }

    // Sets the task force to form up on the given rigidbody with the given radius of the formation
    void SetFormation(List<GameObject> taskForce, float targetRadius, Rigidbody referenceBody)
    {
        for (int i = 0; i < taskForce.Count; i++)
        {
            ShipScript ship = taskForce[i].GetComponent<ShipScript>();
            if (ship.maneuverMode == ShipScript.ManeuverMode.Intercept)
                continue;
            if (taskForce[i].GetComponent<Rigidbody>() != referenceBody)
            {
                ship.maneuverMode = ShipScript.ManeuverMode.Formation;
                ship.targetRadius = targetRadius;
                ship.referenceBody = referenceBody;
                ship.targetPosition = Vector3.zero; // Not necessary if targetposition is deprecated
            }
            else
                ship.maneuverMode = ShipScript.ManeuverMode.Idle;
        }
    }

    // Sets the rules of engagment of the task force, which will only engage within a certain range and only ships of a minimum size estimated by RCS
    void SetROE(List<GameObject> taskForce, float maxRange, float minRCS)
    {
        for (int i = 0; i < taskForce.Count; i++)
        {
            ShipScript ship = taskForce[i].GetComponent<ShipScript>();
            ship.maxRange = maxRange;
            ship.minRCS = minRCS;
        }
    }

    // Sets the radar sets of the task force to val, could be false to achieve stealth
    void SetRadarActive(List<GameObject> taskForce, bool val)
    {
        for (int i = 0; i < taskForce.Count; i++)
        {
            if (taskForce[i].GetComponent<ShipScript>().hasActiveRadar)
                taskForce[i].GetComponent<ShipScript>().radarIsOn = val;
        }
    }

    void SpawnFriendlies()
    {
        ShipDesign shipDesign = new ShipDesign(true, true, true, 10, 100, 10000, 0.1f, 2500, 1000);
        List<GameObject> taskForce = RandomSpawn(friendlyPrefab, shipDesign, Vector3.zero, Vector3.zero, 50, 20);
        SetFormation(taskForce, 50, fleetCenter);
        taskForces.Add(taskForce);
        friendlyTaskForces.Add(taskForce);
    }

    IEnumerator SpawnEnemies() // XXX Complete
    {
        // Spawn loose formation of enemy scouts, Idle, they should try to loiter on the outskirts of the friendly fleet once they spot it (set formation on one of the friendly ships)
        Vector3 direction = Random.onUnitSphere;
        Vector3 spawnLocation = direction * Random.Range(450f, 500f);
        Vector3 spawnVelocity = -direction * Random.Range(1f, 2f);
        ShipDesign shipDesign = new ShipDesign(true, true, true, 1, 100, 1000, 0.1f, 2500, 100);
        List<GameObject> taskForce = RandomSpawn(enemyPrefab, shipDesign, spawnLocation, spawnVelocity, 100, Random.Range(6, 10));
        taskForces.Add(taskForce);
        enemyTaskForces.Add(taskForce);
        while (true)
            yield return new WaitForSeconds(60);

        // Wait for 20 seconds
        // Spawn loose formation of enemy missiles, they should have aggressive ROE with Idle maneuvering
        // Simultaneous spawn of more scouts, they should immediately set formation on the world origin
        // Simultaneous spawn of stealth missiles, they should be Idle with only IR sensors and small RCS, they should have cautious ROE so they can close the distance and penetrate the screen
        // Wait for 30 seconds
        // Spawn large fleet consisting of a screen in formation around the center of the fleet, laser capital ships in the middle, missile interceptors, and offensive missiles
        // Win condition is the player destroying all enemy offensive capabilities or all enemy offensive ships drifting over 500 km away
        // Loss condition is the player losing all offensive capabilities
    }
    /*
    IEnumerator MissileSpawn() // XXX Deprecate
    {
        while (true)
        {
            RandomSpawn(friendlyMissilePrefab, friendlyMissileOrganizer, friendlyMissiles, friendlies, Vector3.zero, Vector3.zero, 10, 2);
            yield return new WaitForSeconds(10);
        }
    }

    IEnumerator MissileDispatch() // XXX Deprecate
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
    }*/

    IEnumerator DetectionRoutine() // Computes the detection status for one ship per task force per frame to improve performance
    {
        int i = 0; // With modulation, i will select the ship for this frame for each task force
        while (true)
        {
            for (int j = 0; j < friendlyTaskForces.Count; j++) // Loop through each friendly task force, enemies will attempt detection on one friendly ship per friendly task force per frame
            {
                if (friendlyTaskForces[j].Count == 0) // Can't detect or track empty task forces
                    continue;
                ShipScript friendlyShip = friendlyTaskForces[j][i % friendlyTaskForces[j].Count].GetComponent<ShipScript>(); // The ship to attempt detection and tracking on

                //Initially assume the ship is neither tracked nor detected
                bool detected = false;
                bool tracked = false;
                for (int k = 0; k < enemyTaskForces.Count; k++) // Loop through each enemy task force so each task force can attempt to detect the friendly ship
                {
                    for (int n = 0; n < enemyTaskForces[k].Count; n++) // Loop through each ship in the enemy task force
                    {
                        ShipScript enemyShip = enemyTaskForces[k][n].GetComponent<ShipScript>(); // Select the enemy ship
                        (detected, tracked) = enemyShip.Detect(friendlyShip); // Attempt to detect the friendly ship with the enemy ship
                        if (tracked)
                            break;
                    }
                    if (tracked)
                        break;
                }
                // Set the tracked and detected fields of the friendly ship
                friendlyShip.tracked = tracked;
                friendlyShip.detected = detected;
                if (tracked)
                    friendlyShip.GetComponent<SphereCollider>().enabled = true;
                else
                    friendlyShip.GetComponent<SphereCollider>().enabled = false;
            }
            for (int j = 0; j < enemyTaskForces.Count; j++) // Loop through each enemy task force, friendlies will attempt detection on one enemy ship per enemy task force per frame
            {
                if (enemyTaskForces[j].Count == 0) // Can't detect or track empty task forces
                    continue;
                ShipScript enemyShip = enemyTaskForces[j][i % enemyTaskForces[j].Count].GetComponent<ShipScript>(); // The ship to attempt detection and tracking on

                //Initially assume the ship is neither tracked nor detected
                bool detected = false;
                bool tracked = false;
                for (int k = 0; k < friendlyTaskForces.Count; k++) // Loop through each friendly task force so each task force can attempt to detect the enemy ship
                {
                    for (int n = 0; n < friendlyTaskForces[k].Count; n++) // Loop through each ship in the friendly task force
                    {
                        ShipScript friendlyShip = friendlyTaskForces[k][n].GetComponent<ShipScript>(); // Select the friendly ship
                        (detected, tracked) = friendlyShip.Detect(enemyShip); // Attempt to detect the enemy ship with the friendly ship
                        if (tracked)
                            break;
                    }
                    if (tracked)
                        break;
                }
                // Using the special methods of the ShipScript also affects how they are displayed on the player's radar screen, hence the difference here
                if (tracked)
                    enemyShip.setTracked();
                else if (detected)
                    enemyShip.setDetected();
                else
                    enemyShip.setUndetected();
            }
            if (i == int.MaxValue) // Don't let i overflow
                i = 0;
            else
                i++;  // Increment the selector for the next frame
            yield return null;
        }
    }

    IEnumerator GradientRoutine() // Computes the formation gradient for one ship per task force per frame to improve performance
    {
        int i = 0; // With modulation, i will select the ship for this frame for each task force
        while (true)
        {
            for (int j = 0; j < taskForces.Count; j++) // Loop through each task force
            {
                if (taskForces[j].Count == 0) // Can't compute gradients for empty task forces
                    continue;
                ShipScript ship = taskForces[j][i % taskForces[j].Count].GetComponent<ShipScript>(); // The ship to compute the gradient for
                ship.gradient = Vector3.zero; // Zero out the old gradient calculation
                if (ship.maneuverMode == ShipScript.ManeuverMode.Formation) // Only compute a non zero gradient if the ship is in formation
                {
                    for (int k = 0; k < taskForces[j].Count; k++) // Compute the gradient from every other ship in the task force
                    {
                        ShipScript otherShip = taskForces[j][k].GetComponent<ShipScript>();
                        if (i % taskForces[j].Count == k || otherShip.maneuverMode != ShipScript.ManeuverMode.Formation) // The other ship must not be this ship and must be in formation
                            continue;
                        Vector3 radius = ship.transform.position - otherShip.transform.position;
                        if (radius.Equals(Vector3.zero)) // If the ships are in the exact same position, we can't compute the gradient, so just leave it
                            continue;
                        ship.gradient += Mathf.Pow(ship.targetRadius, 3) * Vector3.Normalize(radius) / Vector3.SqrMagnitude(radius); //Use the direction and distance between the ships to compute gradient
                    }
                }
            }
            if (i == int.MaxValue) // Don't let i overflow
                i = 0;
            else
                i++; // Increment the selector for the next frame
            yield return null;
        }
    }

    IEnumerator TargetSelection() // Sets one ship per task force per frame to attempt to select a target
    {
        int i = 0; // With modulation, i will select the ship for this frame for each task force
        while (true)
        {
            for (int j = 0; j < friendlyTaskForces.Count; j++) // Loop through each friendly task force, one friendly ship per friendly task force will attempt to find a target each frame
            {
                if (friendlyTaskForces[j].Count == 0) // Task force must have at least one ship to assign a target
                    continue;
                ShipScript friendlyShip = friendlyTaskForces[j][i % friendlyTaskForces[j].Count].GetComponent<ShipScript>(); // The ship that will attempt target selection
                friendlyShip.TrySelectTarget(enemyTaskForces); // Try to select a target
            }
            for (int j = 0; j < enemyTaskForces.Count; j++) // Loop through each enemy task force, one enemy ship per enemy task force will attempt to find a target each frame
            {
                if (enemyTaskForces[j].Count == 0) // Task force must have at least one ship to assign a target
                    continue;
                ShipScript enemyShip = enemyTaskForces[j][i % enemyTaskForces[j].Count].GetComponent<ShipScript>(); // The ship that will attempt target selection
                enemyShip.TrySelectTarget(friendlyTaskForces); // Try to select a target
            }
            if (i == int.MaxValue) // Don't let i overflow
                i = 0;
            else
                i++; // Increment the selector for the next frame
            yield return null;
        }
    }
}
