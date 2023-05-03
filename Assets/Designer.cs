using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Designer : MonoBehaviour
{
    public int numDesigns;

    public Module[] modules;

    List<(int design, int count)> taskForceList;
    // Start is called before the first frame update
    void Start()
    {
        /*
        numDesigns = PlayerPrefs.GetInt("numDesigns");
        modules = new Module[4];
        bool hasRadar = false;

        // Sum up stats of all modules
        for (int i = 0; i < modules.Count; i++)
        {
            if (modules[i].hasRadar)
                hasRadar = true;
        }
        // Apply all stat multipliers
        for (int i = 0; i < modules.Count i++)
        {

        }
        ShipDesign temp;*/
    }

    void SaveFleet()
    {

    }

    void StartGame()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

public class Fleet
{
    List<(int design, int count)> taskForceList;

    public Fleet()
    {
        taskForceList = new List<(int, int)>();
    }

    public void AddTaskForce(int d, int c)
    {
        taskForceList.Add((d, c));
    }

    public void RemoveTaskForce(int i)
    {
        taskForceList.RemoveAt(i);
    }
}

public class Module
{
    public string moduleName;

    public float scale;

    public float power;

    public int isMissile;

    public int hasRadar;

    public float thrust;

    public float radarCrossSection;

    public float radarPower;

    public float laserPower;

    public float hitPoints;

    public float maxShieldPoints;

    public float baseIRSignature;

    public float iRSensitivity;

    public float mass;

    public Module(string moduleName, float scale, float power, int isMissile, int hasRadar, float thrust,
        float radarCrossSection, float radarPower, float laserPower, float hitPoints,
        float maxShieldPoints, float baseIRSignature, float iRSensitivity, float mass)
    {
        this.moduleName = moduleName;
        this.scale = scale;
        this.power = power;
        this.isMissile = 1;
        this.hasRadar = 1;
        this.thrust = thrust;
        this.radarCrossSection = radarCrossSection;
        this.radarPower = radarPower;
        this.laserPower = laserPower;
        this.hitPoints = hitPoints;
        this.maxShieldPoints = maxShieldPoints;
        this.baseIRSignature = baseIRSignature;
        this.iRSensitivity = iRSensitivity;
        this.mass = mass;
    }

    public Module(string moduleName, float scale, float power, bool isMissile, bool hasRadar, float thrust,
        float radarCrossSection, float radarPower, float laserPower, float hitPoints,
        float maxShieldPoints, float baseIRSignature, float iRSensitivity, float mass)
    {
        this.moduleName = moduleName;
        this.scale = scale;
        this.power = power;
        if (isMissile)
            this.isMissile = 1;
        else
            this.isMissile = 0;
        if (hasRadar)
            this.hasRadar = 1;
        else
            this.hasRadar = 0;
        this.thrust = thrust;
        this.radarCrossSection = radarCrossSection;
        this.radarPower = radarPower;
        this.laserPower = laserPower;
        this.hitPoints = hitPoints;
        this.maxShieldPoints = maxShieldPoints;
        this.baseIRSignature = baseIRSignature;
        this.iRSensitivity = iRSensitivity;
        this.mass = mass;
    }

    public static Module ActiveRadar(float sc)
    {
        return new Module("Active Radar Set", sc, -sc * sc, false, true, 0, sc * sc, sc * sc, 0, 1, 1, sc * sc / 10, 0, sc * sc * sc);
    }

    public static Module PassiveRadar(float sc)
    {
        return new Module("Passive Radar Set", sc, -sc * sc / 16, false, false, 0, sc * sc, sc * sc, 0, 1, 1, sc * sc / 20, 0, sc * sc * sc / 4);
    }

    public static Module Engine(float sc)
    {
        return new Module("Engine", sc, -sc * sc, false, false, sc * sc, sc * sc / 4, 0, 0, 1, 1, sc * sc / 2, 0, sc * sc * sc / 4);
    }

    public static Module IRSensor(float sc)
    {
        return new Module("IR Sensor", sc, -sc * sc / 16, false, false, 0, sc * sc / 4, 0, 0, 1, 1, sc * sc / 20, sc * sc, sc * sc * sc / 16);
    }

    public static Module Laser(float sc)
    {
        return new Module("Laser", sc, -sc * sc, false, false, 0, sc * sc, 0, sc * sc, 1, 1, sc * sc, 0, sc * sc * sc);
    }

    public static Module PowerSupply(float sc)
    {
        return new Module("Power Supply", sc, sc * sc, false, false, 0, sc * sc / 4, 0, 0, 1, 1, sc * sc / 10, 0, sc * sc * sc);
    }

    public static Module WarHead()
    {
        return new Module("WarHead", 1, 0, true, false, 0, 1, 0, 0, 1, 1, 0.01f, 0, 1);
    }

    public void SaveModule(string key)
    {
        PlayerPrefs.SetString(key + ".moduleName", moduleName);
        PlayerPrefs.SetFloat(key + ".scale", scale);
        PlayerPrefs.SetFloat(key + ".power", power);
        PlayerPrefs.SetInt(key + ".isMissile", isMissile);
        PlayerPrefs.SetInt(key + ".hasRadar", hasRadar);
        PlayerPrefs.SetFloat(key + ".thrust", thrust);
        PlayerPrefs.SetFloat(key + ".radarCrossSection", radarCrossSection);
        PlayerPrefs.SetFloat(key + ".radarPower", radarPower);
        PlayerPrefs.SetFloat(key + ".laserPower", laserPower);
        PlayerPrefs.SetFloat(key + ".hitPoints", hitPoints);
        PlayerPrefs.SetFloat(key + ".maxShieldPoints", maxShieldPoints);
        PlayerPrefs.SetFloat(key + ".baseIRSignature", baseIRSignature);
        PlayerPrefs.SetFloat(key + ".iRSensitivity", iRSensitivity);
        PlayerPrefs.SetFloat(key + ".mass", mass);
    }

    public Module LoadModule(string key)
    {
        return new Module(PlayerPrefs.GetString(key + ".moduleName"),
            PlayerPrefs.GetFloat(key + ".scale"),
            PlayerPrefs.GetFloat(key + ".power"),
            PlayerPrefs.GetInt(key + ".isMissile"),
            PlayerPrefs.GetInt(key + ".hasRadar"),
            PlayerPrefs.GetFloat(key + ".thrust"),
            PlayerPrefs.GetFloat(key + ".radarCrossSection"),
            PlayerPrefs.GetFloat(key + ".radarPower"),
            PlayerPrefs.GetFloat(key + ".laserPower"),
            PlayerPrefs.GetFloat(key + ".hitPoints"),
            PlayerPrefs.GetFloat(key + ".maxShieldPoints"),
            PlayerPrefs.GetFloat(key + ".baseIRSignature"),
            PlayerPrefs.GetFloat(key + ".iRSensitivity"),
            PlayerPrefs.GetFloat(key + ".mass"));
    }
}
