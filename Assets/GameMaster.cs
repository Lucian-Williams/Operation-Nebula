using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameMaster : MonoBehaviour
{
    public GameObject friendlyPrefab; // The prefab for friendly ships

    public GameObject friendlyMissilePrefab; // The prefab for friendly missiles

    public GameObject enemyPrefab; // The prefab for enemy ships

    public GameObject enemyMissilePrefab; // The prefab for enemy missiles

    public GameObject targetSprite; // The sprite for the currently selected ship

    public GameObject fleetCenter; // The center of the player fleet, typically (0, 0, 0,), is the default target when the target variable is null

    public AudioSource musicSource;

    public AudioSource alarmSource; // The GameMaster can control when to play the alarm sound effect

    public GameObject pauseCanvas;

    public GameObject tutorialCanvas;

    public GameObject gameOverCanvas;

    public Text gameOverText;

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

    int curTaskForce; // The index of the currently selected friendly task force

    bool enemySpawningDone;

    List<TaskForce> taskForces; // The list of all TaskForces

    List<TaskForce> friendlyTaskForces; // The list of only friendly TaskForces

    List<TaskForce> enemyTaskForces; // The list of only enemy TaskForces
    // Start is called before the first frame update
    void Start()
    {
        enemySpawningDone = false;
        taskForces = new List<TaskForce>();
        friendlyTaskForces = new List<TaskForce>();
        enemyTaskForces = new List<TaskForce>();
        curTaskForce = 0;
        StartCoroutine(TargetSelection());
        StartCoroutine(DetectionRoutine());
        StartCoroutine(GradientRoutine());
        if (SceneManager.GetActiveScene().name == "TutorialScene")
        {
            Random.InitState(2023);
            StartCoroutine(SpawnEnemiesTutorial());
            SpawnFriendliesTutorial();
            Time.timeScale = 0;
        }
        else
        {
            StartCoroutine(EnemyCommand());
            SpawnFriendlies();
            Time.timeScale = 1;
        }
    }

    private void Update()
    {
        // No more updates if the game is over
        if (gameOverCanvas.activeInHierarchy)
            return;
        // Please please please get rid of these damn nulls
        CleanShips();
        // Disable pausing and unpausing during the tutorial
        if (tutorialCanvas && tutorialCanvas.activeInHierarchy)
            return;

        // Pause the game if escape key is pressed and game is unpaused
        if (Input.GetKeyDown(KeyCode.Escape) && Time.timeScale == 1)
        {
            musicSource.Pause();
            alarmSource.enabled = false;
            pauseCanvas.SetActive(true);
            Time.timeScale = 0;
            return;
        }
        // Unpause the game if escape key is pressed and game is paused
        else if (Input.GetKeyDown(KeyCode.Escape) && Time.timeScale == 0)
        {
            musicSource.Play();
            pauseCanvas.SetActive(false);
            Time.timeScale = 1;
        }

        int countThreats = 0;
        for (int i = 0; i < enemyTaskForces.Count; i++)
        {
            if (enemyTaskForces[i].shipClass != ShipScript.ShipClass.Screen)
                countThreats += enemyTaskForces[i].ships.Count;
        }
        if (enemySpawningDone && countThreats == 0)
        {
            musicSource.Stop();
            alarmSource.enabled = false;
            gameOverCanvas.SetActive(true);
            gameOverText.text = "Victory!";
            Time.timeScale = 0;
        }

        int countAssets = 0;
        for (int i = 0; i < friendlyTaskForces.Count; i++)
        {
            if (friendlyTaskForces[i].shipClass != ShipScript.ShipClass.Screen)
                countAssets += friendlyTaskForces[i].ships.Count;
        }
        if (countAssets == 0)
        {
            musicSource.Stop();
            alarmSource.enabled = false;
            gameOverCanvas.SetActive(true);
            gameOverText.text = "Defeat";
            Time.timeScale = 0;
        }
    }

    // FixedUpdate is called on a fixed interval
    void FixedUpdate()
    {
        // Check Game is Over
        
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
                tempShip.targetRadius = taskForces[i].targetRadius;
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
                if (!taskForces[i].ships[j]) // If the ship no longer evists, remove from the task force
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
                shipClassText = "None";
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

    // Selects the next task force (for the player)
    public void NextTaskForce()
    {
        curTaskForce++;
        if (curTaskForce >= friendlyTaskForces.Count)
            curTaskForce = 0;
        radiusSlider.value = (friendlyTaskForces[curTaskForce].targetRadius - 5) / 295;
        roESlider.value = friendlyTaskForces[curTaskForce].maxRange / 1000;
    }

    // Sets the formation target of the task force (for the player)
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

    // Sets the task force to hold positions relative to the reference position (for the player)
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

    // Sets the task force to hold positions relative to the reference position (for the AI)
    void HoldPositions(TaskForce taskForce)
    {
        for (int i = 0; i < taskForce.ships.Count; i++)
        {
            ShipScript ship = taskForce.ships[i].GetComponent<ShipScript>();
            if (ship.maneuverMode == ShipScript.ManeuverMode.Intercept)
                continue;
            ship.maneuverMode = ShipScript.ManeuverMode.Hold;
        }
        taskForce.maneuverMode = ShipScript.ManeuverMode.Hold;
    }

    // Sets the task force to idle engines (for the player)
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

    // Sets the task force to idle engines (for the AI)
    void IdleEngines(TaskForce taskForce)
    {
        for (int i = 0; i < taskForce.ships.Count; i++)
        {
            ShipScript ship = taskForce.ships[i].GetComponent<ShipScript>();
            if (ship.maneuverMode == ShipScript.ManeuverMode.Intercept)
                continue;
            ship.maneuverMode = ShipScript.ManeuverMode.Idle;
        }
        taskForce.maneuverMode = ShipScript.ManeuverMode.Idle;
    }

    // Unsets the reference target for the formation (for the player)
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

    // Sets the radius of the formation for the task force (for the player)
    public void SetRadius()
    {
        for (int i = 0; i < friendlyTaskForces[curTaskForce].ships.Count; i++)
        {
            ShipScript ship = friendlyTaskForces[curTaskForce].ships[i].GetComponent<ShipScript>();
            ship.targetRadius = radiusSlider.value * 295 + 5;
        }
        friendlyTaskForces[curTaskForce].targetRadius = radiusSlider.value * 295 + 5;
    }

    // Sets the task force to form up on the reference position (for the player)
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

    // Sets the task force to form up on the reference position (for the AI)
    void SetFormation(TaskForce taskForce, float targetRadius, GameObject formationCenter)
    {
        for (int i = 0; i < taskForce.ships.Count; i++)
        {
            ShipScript ship = taskForce.ships[i].GetComponent<ShipScript>();
            if (ship.maneuverMode == ShipScript.ManeuverMode.Intercept)
                continue;
            ship.maneuverMode = ShipScript.ManeuverMode.Formation;
            ship.targetRadius = targetRadius;
            ship.referenceBody = formationCenter.GetComponent<Rigidbody>();
        }
        taskForce.maneuverMode = ShipScript.ManeuverMode.Formation;
        taskForce.targetRadius = targetRadius;
        taskForce.targetCenter = formationCenter;
    }

    // Toggles the active radar sets in the task force on or off (for the player)
    public void ToggleRadarStatus()
    {
        bool anyHaveRadar = false;
        for (int i = 0; i < friendlyTaskForces[curTaskForce].ships.Count; i++)
        {
            ShipScript ship = friendlyTaskForces[curTaskForce].ships[i].GetComponent<ShipScript>();
            if (ship.hasRadar)
            {
                ship.radarIsOn = !friendlyTaskForces[curTaskForce].radarIsOn;
                anyHaveRadar = true;
            }
        }
        if (anyHaveRadar)
            friendlyTaskForces[curTaskForce].radarIsOn = !friendlyTaskForces[curTaskForce].radarIsOn;
    }

    // Sets the maximum range of engagement of the task force (for the player)
    public void SetMaxRange()
    {
        for (int i = 0; i < friendlyTaskForces[curTaskForce].ships.Count; i++)
        {
            ShipScript ship = friendlyTaskForces[curTaskForce].ships[i].GetComponent<ShipScript>();
            ship.maxRange = roESlider.value * 1000;
        }
        friendlyTaskForces[curTaskForce].maxRange = roESlider.value * 1000;
    }

    // Toggles the task force between having everything as a valid target, only ships, or only capital ships (for the player)
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

    // Sets the rules of engagment of the task force, which will only engage within a certain range and only ships of a minimum size estimated by RCS (for the AI)
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

    // Sets the radar sets of the task force to val, could be false to achieve stealth (for the AI)
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
        ShipDesign shipDesign = new ShipDesign(ShipScript.ShipClass.Laser, true, 1000, 5000, 1000000, 100, 100000, 1000, 5.0f, 500000, 200000);
        List<GameObject> ships = RandomSpawn(friendlyPrefab, shipDesign, Vector3.right * 5, Vector3.zero, 1, 1);
        TaskForce taskForce = new TaskForce(ships, ShipScript.ShipClass.Laser, shipDesign.radarCrossSection, Instantiate(fleetCenter, transform), true);
        for (int i = 0; i < ships.Count; i++) // Set the friendly task force to automatically have the fleet center as the formation target
        {
            ships[i].GetComponent<ShipScript>().referenceBody = fleetCenter.GetComponent<Rigidbody>();
        }
        taskForce.targetCenter = fleetCenter;
        taskForces.Add(taskForce);
        friendlyTaskForces.Add(taskForce);

        shipDesign = new ShipDesign(ShipScript.ShipClass.Screen, true, 5, 10, 10000, 0, 1, 10, 0.1f, 2500, 100);
        ships = RandomSpawn(friendlyPrefab, shipDesign, Vector3.zero, Vector3.zero, 80, 50);
        taskForce = new TaskForce(ships, ShipScript.ShipClass.Screen, shipDesign.radarCrossSection, Instantiate(fleetCenter, transform), true);
        for (int i = 0; i < ships.Count; i++) // Set the friendly task force to automatically have the fleet center as the formation target
        {
            ships[i].GetComponent<ShipScript>().referenceBody = fleetCenter.GetComponent<Rigidbody>();
        }
        taskForce.targetCenter = fleetCenter;
        taskForces.Add(taskForce);
        friendlyTaskForces.Add(taskForce);

        shipDesign = new ShipDesign(ShipScript.ShipClass.Missile, false, 1, 1, 10, 0, 1, 0, 0.1f, 50, 1);
        ships = RandomSpawn(friendlyMissilePrefab, shipDesign, Vector3.zero, Vector3.zero, 50, 30);
        taskForce = new TaskForce(ships, ShipScript.ShipClass.Missile, shipDesign.radarCrossSection, Instantiate(fleetCenter, transform), false);
        for (int i = 0; i < ships.Count; i++) // Set the friendly task force to automatically have the fleet center as the formation target
        {
            ships[i].GetComponent<ShipScript>().referenceBody = fleetCenter.GetComponent<Rigidbody>();
        }
        taskForce.targetCenter = fleetCenter;
        taskForces.Add(taskForce);
        friendlyTaskForces.Add(taskForce);
    }

    // Spawns enemies and commands them
    IEnumerator EnemyCommand()
    {
        bool foundThePlayer = false;
        // Spawn loose formation of enemy scouts, Idle, they should try to loiter on the outskirts of the friendly fleet once they spot it (set formation on one of the friendly ships)
        Vector3 direction = Random.onUnitSphere;
        Vector3 spawnLocation = direction * 800;
        Vector3 spawnVelocity = -direction * Random.Range(3f, 4f);
        ShipDesign shipDesign = new ShipDesign(ShipScript.ShipClass.Screen, true, 1, 10, 10000, 0, 1, 10, 0.1f, 2500, 100);
        List<GameObject> ships = RandomSpawn(enemyPrefab, shipDesign, spawnLocation, spawnVelocity, 500, Random.Range(24, 40));
        TaskForce taskForce = new TaskForce(ships, ShipScript.ShipClass.Screen, shipDesign.radarCrossSection, Instantiate(fleetCenter, transform), true);
        for (int i = 0; i < ships.Count; i++) // DesignationScript needs to know the task force it is designating, how annoying
        {
            ships[i].GetComponent<ShipScript>().marker.GetComponent<DesignationScript>().taskForce = taskForce;
        }
        taskForces.Add(taskForce);
        enemyTaskForces.Add(taskForce);

        int initialStrength = taskForce.ships.Count;


        // The AI now waits to either find the player or be attacked
        while (!foundThePlayer)
        {
            yield return null;
            if (taskForce.ships.Count < initialStrength)
            {
                break;
            }
            else
            {
                for (int i = 0; i < friendlyTaskForces.Count && !foundThePlayer; i++)
                {
                    for (int j = 0; j < friendlyTaskForces[i].ships.Count; j++)
                    {
                        if (friendlyTaskForces[i].ships[j].GetComponent<ShipScript>().detected && Vector3.SqrMagnitude(friendlyTaskForces[i].ships[j].transform.position) < 40000)
                        {
                            foundThePlayer = true;
                            SetFormation(taskForce, 100f, fleetCenter);
                            break;
                        }
                    }
                }
            }
        }

        // The AI now gets its rapid response forces involved
        // Spawn loose formation of enemy missiles, they should have aggressive ROE with Idle maneuvering

        direction = Random.onUnitSphere;
        spawnLocation = direction * 500;
        spawnVelocity = -direction * Random.Range(4f, 7f);
        shipDesign = new ShipDesign(ShipScript.ShipClass.Missile, false, 0.5f, 1, 0, 0, 1, 10, 0.05f, 500, 0.5f);
        ships = RandomSpawn(enemyMissilePrefab, shipDesign, spawnLocation, spawnVelocity, 500, Random.Range(24, 40));
        taskForce = new TaskForce(ships, ShipScript.ShipClass.Missile, shipDesign.radarCrossSection, Instantiate(fleetCenter, transform), false);
        for (int i = 0; i < ships.Count; i++) // Designation needs to know the task force it is designating, how annoying
        {
            ships[i].GetComponent<ShipScript>().marker.GetComponent<DesignationScript>().taskForce = taskForce;
        }
        if (foundThePlayer)
        {
            SetFormation(taskForce, 10, fleetCenter);
            SetROE(taskForce, 100, 10);
        }
        else
        {
            SetROE(taskForce, 200, 10);
        }
        taskForces.Add(taskForce);
        enemyTaskForces.Add(taskForce);

        while (Time.timeSinceLevelLoad < 90)
        {
            yield return null;
            if (!foundThePlayer)
            {
                for (int i = 0; i < friendlyTaskForces.Count && !foundThePlayer; i++)
                {
                    for (int j = 0; j < friendlyTaskForces[i].ships.Count; j++)
                    {
                        if (friendlyTaskForces[i].ships[j].GetComponent<ShipScript>().detected && Vector3.SqrMagnitude(friendlyTaskForces[i].ships[j].transform.position) < 40000)
                        {
                            foundThePlayer = true;
                            SetFormation(taskForce, 10, fleetCenter);
                            SetROE(taskForce, 100, 10);
                            SetFormation(taskForces[0], 100, fleetCenter);
                            break;
                        }
                    }
                }
            }
        }

        direction = Vector3.right;
        spawnLocation = direction * 800;
        spawnVelocity = -direction * Random.Range(4f, 7f);
        shipDesign = new ShipDesign(ShipScript.ShipClass.Laser, true, 200, 5000, 1000000, 0, 100000, 1000, 5.0f, 500000, 200000);
        ships = RandomSpawn(enemyPrefab, shipDesign, spawnLocation, spawnVelocity, 50, 1);
        taskForce = new TaskForce(ships, ShipScript.ShipClass.Laser, shipDesign.radarCrossSection, Instantiate(fleetCenter, transform), true);
        for (int i = 0; i < ships.Count; i++) // Designation needs to know the task force it is designating, how annoying
        {
            ships[i].GetComponent<ShipScript>().marker.GetComponent<DesignationScript>().taskForce = taskForce;
        }
        SetFormation(taskForce, 10, fleetCenter);
        SetROE(taskForce, 100, 0);
        taskForces.Add(taskForce);
        enemyTaskForces.Add(taskForce);

        shipDesign = new ShipDesign(ShipScript.ShipClass.Missile, false, 1f, 1, 0, 0, 1, 10, 0.05f, 500, 0.5f);
        ships = RandomSpawn(enemyMissilePrefab, shipDesign, spawnLocation, spawnVelocity, 500, Random.Range(50, 60));
        taskForce = new TaskForce(ships, ShipScript.ShipClass.Missile, shipDesign.radarCrossSection, Instantiate(fleetCenter, transform), false);
        for (int i = 0; i < ships.Count; i++) // Designation needs to know the task force it is designating, how annoying
        {
            ships[i].GetComponent<ShipScript>().marker.GetComponent<DesignationScript>().taskForce = taskForce;
        }
        SetFormation(taskForce, 100, taskForces[2].taskForceCenter);
        SetROE(taskForce, 80, 0);
        taskForces.Add(taskForce);
        enemyTaskForces.Add(taskForce);

        enemySpawningDone = true;
        yield return null;
    }

    void SpawnFriendliesTutorial()
    {
        ShipDesign shipDesign = new ShipDesign(ShipScript.ShipClass.Laser, true, 10, 100, 10000, 100, 1, 10, 0.1f, 2500, 1000);
        List<GameObject> ships = RandomSpawn(friendlyPrefab, shipDesign, Vector3.zero, Vector3.zero, 10, 10);
        TaskForce taskForce = new TaskForce(ships, ShipScript.ShipClass.Laser, shipDesign.radarCrossSection, Instantiate(fleetCenter, transform), true);
        for (int i = 0; i < ships.Count; i++) // Set the friendly task force to automatically have the fleet center as the formation target
        {
            ships[i].GetComponent<ShipScript>().referenceBody = fleetCenter.GetComponent<Rigidbody>();
        }
        taskForce.targetCenter = fleetCenter;
        taskForces.Add(taskForce);
        friendlyTaskForces.Add(taskForce);
        shipDesign = new ShipDesign(ShipScript.ShipClass.Missile, false, 10, 1, 10, 0, 1, 0, 0.1f, 50, 1);
        ships = RandomSpawn(friendlyMissilePrefab, shipDesign, Vector3.zero, Vector3.zero, 50, 10);
        taskForce = new TaskForce(ships, ShipScript.ShipClass.Missile, shipDesign.radarCrossSection, Instantiate(fleetCenter, transform), false);
        for (int i = 0; i < ships.Count; i++) // Set the friendly task force to automatically have the fleet center as the formation target
        {
            ships[i].GetComponent<ShipScript>().referenceBody = fleetCenter.GetComponent<Rigidbody>();
        }
        taskForce.targetCenter = fleetCenter;
        taskForces.Add(taskForce);
        friendlyTaskForces.Add(taskForce);
    }

    IEnumerator SpawnEnemiesTutorial()
    {
        Vector3 direction = Random.onUnitSphere;
        Vector3 spawnLocation = direction * Random.Range(100f, 150f);
        Vector3 spawnVelocity = -direction * Random.Range(1f, 2f);
        ShipDesign shipDesign = new ShipDesign(ShipScript.ShipClass.Laser, true, 1, 100, 1000, 100, 1, 10, 0.1f, 2500, 100);
        List<GameObject> ships = RandomSpawn(enemyPrefab, shipDesign, spawnLocation, spawnVelocity, 100, Random.Range(6, 10));
        TaskForce taskForce = new TaskForce(ships, ShipScript.ShipClass.Laser, shipDesign.radarCrossSection, Instantiate(fleetCenter, transform), true);
        for (int i = 0; i < ships.Count; i++) // DesignationScript needs to know the task force it is designating, how annoying
        {
            ships[i].GetComponent<ShipScript>().marker.GetComponent<DesignationScript>().taskForce = taskForce;
        }
        taskForces.Add(taskForce);
        enemyTaskForces.Add(taskForce);
        enemySpawningDone = true;
        yield return null;
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
            while (Time.timeScale == 0)
                yield return null;
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
        int i = 0; // With modulo op, i will select the ship for this frame for each task force
        while (true)
        {
            while (Time.timeScale == 0)
                yield return null;
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

    IEnumerator TargetSelection() // Sets all ships to attempt to select a target
    {
        bool activateLaserAlarm = false;
        bool missileLaunch;
        bool laserAttack;
        while (true)
        {
            while (Time.timeScale == 0)
                yield return null;
            activateLaserAlarm = false;
            for (int j = 0; j < friendlyTaskForces.Count; j++) // Loop through each friendly task force, one friendly ship per friendly task force will attempt to find a target each frame
            {
                for (int jj = 0; jj < friendlyTaskForces[j].ships.Count; jj++)
                {
                    ShipScript friendlyShip = friendlyTaskForces[j].ships[jj].GetComponent<ShipScript>(); // The ship that will attempt target selection
                    friendlyShip.TrySelectTarget(enemyTaskForces); // Try to select a target
                }
            }
            for (int j = 0; j < enemyTaskForces.Count; j++) // Loop through each enemy task force, one enemy ship per enemy task force will attempt to find a target each frame
            {
                for (int jj = 0; jj < enemyTaskForces[j].ships.Count; jj++)
                {
                    ShipScript enemyShip = enemyTaskForces[j].ships[jj].GetComponent<ShipScript>(); // The ship that will attempt target selection
                    (missileLaunch, laserAttack) =  enemyShip.TrySelectTarget(friendlyTaskForces); // Try to select a target
                    if (laserAttack)
                        activateLaserAlarm = true;
                }
            }
            //laserAlarm.enabled = activateLaserAlarm;
            yield return null;
        }
    }
}
