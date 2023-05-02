using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameMaster : MonoBehaviour
{
    public GameObject friendlyPrefab; // The prefab for friendly ships

    public GameObject friendlyMissilePrefab; // The prefab for friendly missiles

    public GameObject enemyPrefab; // The prefab for enemy ships

    public GameObject enemyMissilePrefab; // The prefab for enemy missiles

    public GameObject targetSprite; // The sprite for the currently selected ship

    public GameObject fleetCenter; // The center of the player fleet, typically (0, 0, 0,), is the default target when the target variable is null

    public AudioSource alarmSource; // The GameMaster can control when to play the alarm sound effect

    public Slider radiusSlider;

    public Slider roESlider;

    public Text taskForceText;

    public Text roEText;

    public Text weaponsClassText;

    public Text sizeClassText;

    public Text strengthText;

    public Text engagementRateText;

    public Text maneuverModeText;

    public Text engagementRangeText;

    public Text radarStatusText;

    bool isPaused = false; // Deprecate

    int curTaskForce; // The index of the currently selected friendly task force

    List<TaskForce> taskForces; // The list of all TaskForces

    List<TaskForce> friendlyTaskForces; // The list of only friendly TaskForces

    List<TaskForce> enemyTaskForces; // The list of only enemy TaskForces

    bool gameOver = false; // Deprecate
    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(1776);
        taskForces = new List<TaskForce>();
        friendlyTaskForces = new List<TaskForce>();
        enemyTaskForces = new List<TaskForce>();
        curTaskForce = 0;
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
        CleanShips();
        for (int i = 0; i < taskForces.Count; i++)
        {
            // Update the task force centers
            taskForces[i].UpdateCenter();
            // Reintegrate missiles that have lost their target into the task force
            for (int j = 0; j < taskForces[i].ships.Count; j++)
            {
                ShipScript tempShip = taskForces[i].ships[j].GetComponent<ShipScript>();
                if (tempShip.maneuverMode == ShipScript.ManeuverMode.Intercept)
                    continue;
                tempShip.maneuverMode = taskForces[i].maneuverMode;
                if (taskForces[i].targetCenter)
                    tempShip.referenceBody = taskForces[i].targetCenter.GetComponent<Rigidbody>();
            }
        }
        UpdateOverview();
        if (friendlyTaskForces[curTaskForce].target && (!friendlyTaskForces[curTaskForce].target.TryGetComponent<ShipScript>(out ShipScript ship) || !ship.detected))
            friendlyTaskForces[curTaskForce].target = null;
        // Start Graphical update to target marker position
        if (friendlyTaskForces[curTaskForce].target)
        {
            targetSprite.transform.position = friendlyTaskForces[curTaskForce].target.GetComponent<ShipScript>().marker.transform.position;
            targetSprite.SetActive(true);
        }
        else
            targetSprite.SetActive(false);
        // End Graphical update to target marker position
        // Start checks to activate or deactivate the missile warning (alarm)
        bool setAlarm = false;
        for (int i = 0; i < enemyTaskForces.Count; i++)
        {
            for (int j = 0; j < enemyTaskForces[i].ships.Count; j++)
            {
                ShipScript enemyShip = enemyTaskForces[i].ships[j].GetComponent<ShipScript>();
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
            for (int j = 0; j < taskForces[i].ships.Count; j++) // Loop through all ships in the task force
            {
                if (!taskForces[i].ships[j].TryGetComponent<ShipScript>(out ShipScript ship)) // If the ship no longer has a ShipScript (meaning it was destroyed), remove from the task force
                {
                    taskForces[i].ships.RemoveAt(j);
                    j--; // Since removal moves the tail of the list up, decrement the index for the next ship in the task force
                }
            }
        }
    }

    // Updates the task force overview that the player sees on their screen
    void UpdateOverview()
    {
        string shipClassText;
        string sizeText;
        string maneuverText;
        string radarText;
        switch (friendlyTaskForces[curTaskForce].shipClass)
        {
            case ShipScript.ShipClass.Laser:
                shipClassText = "Laser";
                break;
            case ShipScript.ShipClass.Missile:
                shipClassText = "Missile";
                break;
            case ShipScript.ShipClass.Screen:
                shipClassText = "Screen";
                break;
            default:
                shipClassText = "";
                break;
        }
        if (friendlyTaskForces[curTaskForce].radarCrossSection < 10)
            sizeText = "Drone";
        else if (friendlyTaskForces[curTaskForce].radarCrossSection < 1000)
            sizeText = "Escort";
        else
            sizeText = "Capital";
        switch (friendlyTaskForces[curTaskForce].maneuverMode)
        {
            case ShipScript.ManeuverMode.Idle:
                maneuverText = "Idle";
                break;
            case ShipScript.ManeuverMode.Hold:
                maneuverText = "Hold";
                break;
            case ShipScript.ManeuverMode.Formation:
                maneuverText = "Formation on ";
                if (friendlyTaskForces[curTaskForce].targetCenter == fleetCenter)
                    maneuverText += "Fleet Center - Radius ";
                else
                    maneuverText += "Enemy Forces - Radius ";
                maneuverText += friendlyTaskForces[curTaskForce].targetRadius;
                break;
            default:
                maneuverText = "";
                break;
        }
        if (friendlyTaskForces[curTaskForce].radarIsOn)
            radarText = "On";
        else
            radarText = "Off";
        taskForceText.text = "Task Force " + curTaskForce;
        if (friendlyTaskForces[curTaskForce].minRCS == 0)
            roEText.text = "Engage All";
        else if (friendlyTaskForces[curTaskForce].minRCS == 10)
            roEText.text = "Engage Ships";
        else
            roEText.text = "Engage Capitals";
        weaponsClassText.text = "Weapons Class: " + shipClassText;
        sizeClassText.text = "Size Class: " + sizeText;
        strengthText.text = "Strength: " + friendlyTaskForces[curTaskForce].ships.Count;
        engagementRateText.text = "Engagement Rate: " + friendlyTaskForces[curTaskForce].EngagementRate();
        maneuverModeText.text = "Maneuver: " + maneuverText;
        engagementRangeText.text = "Engagement Range: " + friendlyTaskForces[curTaskForce].maxRange;
        radarStatusText.text = "Radar: " + radarText;
    }

    List<GameObject> RandomSpawn(GameObject spawnPrefab, ShipDesign shipDesign, Vector3 location, Vector3 velocity, float radius, int count)
    {
        List<GameObject> ships = new List<GameObject>();
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
            ships.Add(temp);
        }
        return ships;
    }

    // Selects the next TaskForce
    public void NextTaskForce()
    {
        curTaskForce++;
        if (curTaskForce >= friendlyTaskForces.Count)
            curTaskForce = 0;
        radiusSlider.value = (friendlyTaskForces[curTaskForce].targetRadius - 5) / 295;
        roESlider.value = friendlyTaskForces[curTaskForce].maxRange / 1000;
    }

    // Sets the formation target of the task force
    public void SetTarget(GameObject target, TaskForce taskForce)
    {
        for (int i = 0; i < friendlyTaskForces[curTaskForce].ships.Count; i++)
        {
            ShipScript ship = friendlyTaskForces[curTaskForce].ships[i].GetComponent<ShipScript>();
            if (ship.maneuverMode == ShipScript.ManeuverMode.Intercept)
                continue;
            ship.referenceBody = taskForce.taskForceCenter.GetComponent<Rigidbody>();
        }
        friendlyTaskForces[curTaskForce].target = target;
        friendlyTaskForces[curTaskForce].targetCenter = taskForce.taskForceCenter;
    }

    // Sets the task force to hold positions relative to the reference position
    public void HoldPositions()
    {
        for (int i = 0; i < friendlyTaskForces[curTaskForce].ships.Count; i++)
        {
            ShipScript ship = friendlyTaskForces[curTaskForce].ships[i].GetComponent<ShipScript>();
            if (ship.maneuverMode == ShipScript.ManeuverMode.Intercept)
                continue;
            ship.maneuverMode = ShipScript.ManeuverMode.Hold;
        }
        friendlyTaskForces[curTaskForce].maneuverMode = ShipScript.ManeuverMode.Hold;
    }

    // Sets the task force to idle engines
    public void IdleEngines()
    {
        for (int i = 0; i < friendlyTaskForces[curTaskForce].ships.Count; i++)
        {
            ShipScript ship = friendlyTaskForces[curTaskForce].ships[i].GetComponent<ShipScript>();
            if (ship.maneuverMode == ShipScript.ManeuverMode.Intercept)
                continue;
            ship.maneuverMode = ShipScript.ManeuverMode.Idle;
        }
        friendlyTaskForces[curTaskForce].maneuverMode = ShipScript.ManeuverMode.Idle;
    }

    // Unsets the reference target for the formation
    public void UnSetTarget()
    {
        for (int i = 0; i < friendlyTaskForces[curTaskForce].ships.Count; i++)
        {
            ShipScript ship = friendlyTaskForces[curTaskForce].ships[i].GetComponent<ShipScript>();
            if (ship.maneuverMode == ShipScript.ManeuverMode.Intercept)
                continue;
            ship.referenceBody = fleetCenter.GetComponent<Rigidbody>();
        }
        friendlyTaskForces[curTaskForce].target = null;
        friendlyTaskForces[curTaskForce].targetCenter = fleetCenter;
    }

    // Sets the radius of the formation for the task force
    public void SetRadius()
    {
        for (int i = 0; i < friendlyTaskForces[curTaskForce].ships.Count; i++)
        {
            ShipScript ship = friendlyTaskForces[curTaskForce].ships[i].GetComponent<ShipScript>();
            ship.targetRadius = radiusSlider.value * 295 + 5;
        }
        friendlyTaskForces[curTaskForce].targetRadius = radiusSlider.value * 295 + 5;
    }

    // Sets the task force to form up on the reference position
    public void SetFormation()
    {
        for (int i = 0; i < friendlyTaskForces[curTaskForce].ships.Count; i++)
        {
            ShipScript ship = friendlyTaskForces[curTaskForce].ships[i].GetComponent<ShipScript>();
            if (ship.maneuverMode == ShipScript.ManeuverMode.Intercept)
                continue;
            ship.maneuverMode = ShipScript.ManeuverMode.Formation;
        }
        friendlyTaskForces[curTaskForce].maneuverMode = ShipScript.ManeuverMode.Formation;
    }

    // Toggles the active radar sets in the task force on or off
    public void ToggleRadarStatus()
    {
        for (int i = 0; i < friendlyTaskForces[curTaskForce].ships.Count; i++)
        {
            ShipScript ship = friendlyTaskForces[curTaskForce].ships[i].GetComponent<ShipScript>();
            if (ship.hasRadar)
                ship.radarIsOn = !friendlyTaskForces[curTaskForce].radarIsOn;
        }
        friendlyTaskForces[curTaskForce].radarIsOn = !friendlyTaskForces[curTaskForce].radarIsOn;
    }

    // Sets the maximum range of engagement of the task force
    public void SetMaxRange()
    {
        for (int i = 0; i < friendlyTaskForces[curTaskForce].ships.Count; i++)
        {
            ShipScript ship = friendlyTaskForces[curTaskForce].ships[i].GetComponent<ShipScript>();
            ship.maxRange = roESlider.value * 1000;
        }
        friendlyTaskForces[curTaskForce].maxRange = roESlider.value * 1000;
    }

    // Toggles the task force between having everything as a valid target, only ships, or only capital ships
    public void ToggleRoE()
    {
        if (friendlyTaskForces[curTaskForce].minRCS == 10)
            friendlyTaskForces[curTaskForce].minRCS = 1000;
        else if (friendlyTaskForces[curTaskForce].minRCS == 1000)
            friendlyTaskForces[curTaskForce].minRCS = 0;
        else
            friendlyTaskForces[curTaskForce].minRCS = 10;
        for (int i = 0; i < friendlyTaskForces[curTaskForce].ships.Count; i++)
        {
            ShipScript ship = friendlyTaskForces[curTaskForce].ships[i].GetComponent<ShipScript>();
            ship.minRCS = friendlyTaskForces[curTaskForce].minRCS;
        }
    }

    // Sets the rules of engagment of the task force, which will only engage within a certain range and only ships of a minimum size estimated by RCS
    void SetROE(TaskForce taskForce, float maxRange, float minRCS)
    {
        for (int i = 0; i < taskForce.ships.Count; i++)
        {
            ShipScript ship = taskForce.ships[i].GetComponent<ShipScript>();
            ship.maxRange = maxRange;
            ship.minRCS = minRCS;
        }
        taskForce.maxRange = maxRange;
        taskForce.minRCS = minRCS;
    }

    // Sets the radar sets of the task force to val, could be false to achieve stealth
    void SetRadarActive(TaskForce taskForce, bool val)
    {
        for (int i = 0; i < taskForce.ships.Count; i++)
        {
            if (taskForce.ships[i].GetComponent<ShipScript>().hasRadar)
                taskForce.ships[i].GetComponent<ShipScript>().radarIsOn = val;
        }
        taskForce.radarIsOn = val;
    }

    void SpawnFriendlies()
    {
        ShipDesign shipDesign = new ShipDesign(ShipScript.ShipClass.Screen, true, true, 10, 100, 10000, 0.1f, 2500, 1000);
        List<GameObject> ships = RandomSpawn(friendlyPrefab, shipDesign, Vector3.zero, Vector3.zero, 10, 10);
        TaskForce taskForce = new TaskForce(ships, ShipScript.ShipClass.Missile, shipDesign.GetRadarCrossSection(), Instantiate(fleetCenter, transform), true);
        for (int i = 0; i < ships.Count; i++) // Set the friendly task force to automatically have the fleet center as the formation target
        {
            ships[i].GetComponent<ShipScript>().referenceBody = fleetCenter.GetComponent<Rigidbody>();
        }
        taskForce.targetCenter = fleetCenter;
        taskForces.Add(taskForce);
        friendlyTaskForces.Add(taskForce);
        shipDesign = new ShipDesign(ShipScript.ShipClass.Missile, false, true, 10, 1, 10, 0.1f, 50, 1);
        ships = RandomSpawn(friendlyMissilePrefab, shipDesign, Vector3.zero, Vector3.zero, 50, 10);
        taskForce = new TaskForce(ships, ShipScript.ShipClass.Missile, shipDesign.GetRadarCrossSection(), Instantiate(fleetCenter, transform), true);
        for (int i = 0; i < ships.Count; i++) // Set the friendly task force to automatically have the fleet center as the formation target
        {
            ships[i].GetComponent<ShipScript>().referenceBody = fleetCenter.GetComponent<Rigidbody>();
        }
        taskForce.targetCenter = fleetCenter;
        taskForces.Add(taskForce);
        friendlyTaskForces.Add(taskForce);
    }

    IEnumerator SpawnEnemies() // XXX Complete
    {
        // Spawn loose formation of enemy scouts, Idle, they should try to loiter on the outskirts of the friendly fleet once they spot it (set formation on one of the friendly ships)
        Vector3 direction = Random.onUnitSphere;
        Vector3 spawnLocation = direction * Random.Range(450f, 500f);
        Vector3 spawnVelocity = -direction * Random.Range(1f, 2f);
        ShipDesign shipDesign = new ShipDesign(ShipScript.ShipClass.Laser, true, true, 1, 100, 1000, 0.1f, 2500, 100);
        List<GameObject> ships = RandomSpawn(enemyPrefab, shipDesign, spawnLocation, spawnVelocity, 100, Random.Range(6, 10));
        TaskForce taskForce = new TaskForce(ships, ShipScript.ShipClass.Screen, shipDesign.GetRadarCrossSection(), Instantiate(fleetCenter, transform), true);
        for(int i = 0; i < ships.Count; i++) // Designation needs to know the task force it is designating, how annoying
        {
            ships[i].GetComponent<ShipScript>().marker.GetComponent<DesignationScript>().taskForce = taskForce;
        }
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
                if (friendlyTaskForces[j].ships.Count == 0) // Can't detect or track empty task forces
                    continue;
                ShipScript friendlyShip = friendlyTaskForces[j].ships[i % friendlyTaskForces[j].ships.Count].GetComponent<ShipScript>(); // The ship to attempt detection and tracking on

                //Initially assume the ship is neither tracked nor detected
                bool detected = false;
                bool tracked = false;
                for (int k = 0; k < enemyTaskForces.Count; k++) // Loop through each enemy task force so each task force can attempt to detect the friendly ship
                {
                    for (int n = 0; n < enemyTaskForces[k].ships.Count; n++) // Loop through each ship in the enemy task force
                    {
                        ShipScript enemyShip = enemyTaskForces[k].ships[n].GetComponent<ShipScript>(); // Select the enemy ship
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
                if (enemyTaskForces[j].ships.Count == 0) // Can't detect or track empty task forces
                    continue;
                ShipScript enemyShip = enemyTaskForces[j].ships[i % enemyTaskForces[j].ships.Count].GetComponent<ShipScript>(); // The ship to attempt detection and tracking on

                //Initially assume the ship is neither tracked nor detected
                bool detected = false;
                bool tracked = false;
                for (int k = 0; k < friendlyTaskForces.Count; k++) // Loop through each friendly task force so each task force can attempt to detect the enemy ship
                {
                    for (int n = 0; n < friendlyTaskForces[k].ships.Count; n++) // Loop through each ship in the friendly task force
                    {
                        ShipScript friendlyShip = friendlyTaskForces[k].ships[n].GetComponent<ShipScript>(); // Select the friendly ship
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
                if (taskForces[j].ships.Count == 0) // Can't compute gradients for empty task forces
                    continue;
                ShipScript ship = taskForces[j].ships[i % taskForces[j].ships.Count].GetComponent<ShipScript>(); // The ship to compute the gradient for
                ship.gradient = Vector3.zero; // Zero out the old gradient calculation
                if (ship.maneuverMode == ShipScript.ManeuverMode.Formation) // Only compute a non zero gradient if the ship is in formation
                {
                    for (int k = 0; k < taskForces[j].ships.Count; k++) // Compute the gradient from every other ship in the task force
                    {
                        ShipScript otherShip = taskForces[j].ships[k].GetComponent<ShipScript>();
                        if (i % taskForces[j].ships.Count == k || otherShip.maneuverMode != ShipScript.ManeuverMode.Formation) // The other ship must not be this ship and must be in formation
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
                if (friendlyTaskForces[j].ships.Count == 0) // Task force must have at least one ship to assign a target
                    continue;
                ShipScript friendlyShip = friendlyTaskForces[j].ships[i % friendlyTaskForces[j].ships.Count].GetComponent<ShipScript>(); // The ship that will attempt target selection
                friendlyShip.TrySelectTarget(enemyTaskForces); // Try to select a target
            }
            for (int j = 0; j < enemyTaskForces.Count; j++) // Loop through each enemy task force, one enemy ship per enemy task force will attempt to find a target each frame
            {
                if (enemyTaskForces[j].ships.Count == 0) // Task force must have at least one ship to assign a target
                    continue;
                ShipScript enemyShip = enemyTaskForces[j].ships[i % enemyTaskForces[j].ships.Count].GetComponent<ShipScript>(); // The ship that will attempt target selection
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
