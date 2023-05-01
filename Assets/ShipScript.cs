using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipScript : MonoBehaviour
{
    public enum ShipClass {Screen, Laser, Missile}

    public enum ManeuverMode {Idle, Rendezvous, Hold, Intercept, Formation}

    public ShipClass shipClass;

    public ManeuverMode maneuverMode;

    public Sprite smallSprite;

    public Sprite mediumSprite;

    public Sprite largeSprite;

    public GameObject markerPrefab;

    public GameObject marker;

    public GameObject rangeMarkerPrefab;

    public GameObject rangeMarker;

    public GameObject gameMaster;

    public bool detected;

    public bool tracked;

    public bool hasRadar;

    public bool hasActiveRadar;

    public bool radarIsOn;

    public bool hasIR;

    public float thrust;

    public float radarCrossSection;

    public float radarPower;

    public float baseIRSignature;

    public float iRSignature;

    public float iRSensitivity;

    public float maxRange; // Rule of Engagement, only engage targets below max range

    public float minRCS; // Rule of Engagement, only engage targets above minimum perceived size (estimated by radarCrossSection).

    public float targetRadius; // For Formation mode only

    public int missilesTargeting = 0; // The number of missiles targeting this ship

    public Vector3 gradient; // For Formation mode only

    public Vector3 targetPosition; // For Rendezvous mode only

    public Rigidbody rb; // This rigidbody

    public Rigidbody referenceBody; // Another ship's rigidbody

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame, uses the thrust to perform some maneuver program and updates the radar marker
    void Update()
    {

        // isPaused shall be deprecated, ending the game shall be achieved by deactivating the GameMaster
        //if (gameMaster.GetComponent<GameMaster>().isPaused)
        //return;
        iRSignature = baseIRSignature; // Assume no thrust is used to start
        if (thrust == 0) // Can't change velocity if there is no thrust
            return;
        float sqrDist = Vector3.SqrMagnitude(transform.position);
        float xscale = Mathf.Max(5 / (sqrDist / 1000 + 1), 0.2f) * 2;
        float yscale = Mathf.Min((sqrDist / 1000 + 1) / 5, 5) * 2;
        float deltaV = Time.deltaTime * thrust / rb.mass;
        Vector3 relativeVelocity;
        Vector3 relativePosition;
        Vector3 targetVelocity;
        Vector3 velocityChange;
        Vector3 radialVelocity;
        Vector3 lateralVelocity;
        marker.transform.position = new Vector3(MarkerBearing() * 7 / 180, MarkerElevation() * 7 / 180 + 1); // Update the position of the radar marker on screen
        rangeMarker.transform.localScale = new Vector3(xscale, yscale, 1); // Update the range marker for the new range
        if (referenceBody == null || referenceBody == rb)
            return;
        switch (maneuverMode) {
            case ManeuverMode.Rendezvous: // Deprecate, will no longer be used in favor of formation mode
                relativeVelocity = rb.velocity - referenceBody.velocity;
                relativePosition = rb.position - referenceBody.position - targetPosition;
                if (relativePosition.Equals(Vector3.zero))
                    targetVelocity = Vector3.zero;
                else
                    targetVelocity = -2 * relativePosition / Mathf.Sqrt(2 * Vector3.Magnitude(relativePosition) / (0.95f * thrust / rb.mass));
                if (Vector3.SqrMagnitude(targetVelocity) < 0.0001)
                    targetVelocity = Vector3.zero;
                velocityChange = targetVelocity - relativeVelocity;
                if (Vector3.SqrMagnitude(velocityChange) < 0.00000001)
                    velocityChange = Vector3.zero;
                if (deltaV * deltaV < Vector3.SqrMagnitude(velocityChange))
                {
                    rb.AddForce(Vector3.Normalize(velocityChange) * deltaV, ForceMode.VelocityChange);
                    iRSignature = baseIRSignature + thrust;
                }
                else
                {
                    rb.AddForce(velocityChange, ForceMode.VelocityChange);
                    iRSignature = baseIRSignature + (thrust * Vector3.Magnitude(velocityChange) / deltaV);
                }
                break;
            case ManeuverMode.Intercept: // Sets the behavior of this ship to intercept the target (referencBody) at maximum speed
                if (referenceBody.GetComponent<ShipScript>().detected == false) // Cannot intercept an undetected target
                    break;
                relativeVelocity = rb.velocity - referenceBody.velocity;
                relativePosition = rb.position - referenceBody.position; // Target position will always be the center of the target body (Vector3.zero), so is excluded from this calculation
                if (relativePosition.Equals(Vector3.zero)) // Special case, if we're at the target, do nothing
                    break;
                radialVelocity = Vector3.Dot(relativeVelocity, relativePosition) * relativePosition / Vector3.SqrMagnitude(relativePosition);
                lateralVelocity = relativeVelocity - radialVelocity;
                if (deltaV * deltaV < Vector3.SqrMagnitude(lateralVelocity))
                {
                    rb.AddForce(-Vector3.Normalize(lateralVelocity) * deltaV, ForceMode.VelocityChange);
                    iRSignature = baseIRSignature + thrust;
                }
                else
                {
                    velocityChange = (-Vector3.Normalize(relativePosition) * Mathf.Sqrt(deltaV * deltaV - Vector3.SqrMagnitude(lateralVelocity))) - lateralVelocity;
                    rb.AddForce(velocityChange, ForceMode.VelocityChange);
                    iRSignature = baseIRSignature + (thrust * Vector3.Magnitude(velocityChange) / deltaV);
                }
                break;
            case ManeuverMode.Formation: // Sets the behavior of this ship to enter a spherical formation with other ships with a common formation radius and common center of the formation
                relativeVelocity = rb.velocity - referenceBody.velocity;
                relativePosition = rb.position - referenceBody.position - targetPosition;
                if (!relativePosition.Equals(Vector3.zero)) // We can only find our destination if we aren't exactly at the center of the formation
                {
                    if (!(relativePosition + gradient).Equals(Vector3.zero)) // We can only find our destination if our gradient doesn't take us to the center of the formation
                        relativePosition = relativePosition - Vector3.Normalize(relativePosition + gradient) * targetRadius;
                    else
                        relativePosition = relativePosition - Vector3.Normalize(relativePosition) * targetRadius; // Use a default position as the destination in the alternative case
                }
                else
                {
                    relativePosition = targetRadius * Vector3.left; // Use a default position as the destination in the alternative case
                }
                if (relativePosition.Equals(Vector3.zero)) // Special case, to avoid division by zero we set the targetVelocity to zero if we are at our destination already
                    targetVelocity = Vector3.zero;
                else
                    targetVelocity = -2 * relativePosition / Mathf.Sqrt(2 * Vector3.Magnitude(relativePosition) / (0.95f * thrust / rb.mass));
                if (Vector3.SqrMagnitude(targetVelocity) < 0.0001) // If the target velocity is close enough to zero, just make it zero
                    targetVelocity = Vector3.zero;
                velocityChange = targetVelocity - relativeVelocity;
                if (Vector3.SqrMagnitude(velocityChange) < 0.00000001) // If the velocity difference between current and target velocity is close enough to zero, just make it zero
                    velocityChange = Vector3.zero;
                if (deltaV * deltaV < Vector3.SqrMagnitude(velocityChange)) // Branch if we can't reach the targetVelocity in this frame
                {
                    rb.AddForce(Vector3.Normalize(velocityChange) * deltaV, ForceMode.VelocityChange);
                    iRSignature = baseIRSignature + thrust;
                }
                else // Branch if we can reach the targetVelocity in this frame
                {
                    rb.AddForce(velocityChange, ForceMode.VelocityChange);
                    iRSignature = baseIRSignature + (thrust * Vector3.Magnitude(velocityChange) / deltaV);
                }
                break;
            case ManeuverMode.Hold: // Tries to simply match velocity with the reference body
                velocityChange = referenceBody.velocity - rb.velocity;
                if (Vector3.SqrMagnitude(velocityChange) < 0.00000001) // If the target velocity is close enough to zero, just make it zero
                    velocityChange = Vector3.zero;
                if (deltaV * deltaV < Vector3.SqrMagnitude(velocityChange)) // Branch if we can't reach the targetVelocity in this frame
                {
                    rb.AddForce(Vector3.Normalize(velocityChange) * deltaV, ForceMode.VelocityChange);
                    iRSignature = baseIRSignature + thrust;
                }
                else // Branch if we can reach the targetVelocity in this frame
                {
                    rb.AddForce(velocityChange, ForceMode.VelocityChange);
                    iRSignature = baseIRSignature + (thrust * Vector3.Magnitude(velocityChange) / deltaV);
                }
                break;
            case ManeuverMode.Idle: // Does nothing
                break;
        }
    }

    public (bool, bool) Detect(ShipScript other) // Returns whether detection and tracking were achieved, respectively
    {
        bool d = false; // Assume no detection yet
        if (hasRadar) // If we have a radar, attempt both active and passive radar detection (only active radar detection can achieve a track)
        {
            float radarReturn = 0; // The radar return is used to determine whether tracking is achieved
            float rcs = other.radarCrossSection;
            if (radarIsOn) // The radar must be on to attempt active detection
                radarReturn += radarPower * rcs / Mathf.Pow(Vector3.SqrMagnitude(transform.position - other.transform.position), 2); // The radar return depends on the rcs, radarPower, and distance
            float radarSignal = radarReturn; // The radar signal is used to determine whether detection is achieved
            if (other.radarIsOn) // The other ship's radar must be on for us to listen for its signal
                radarSignal += other.radarPower / Vector3.SqrMagnitude(transform.position - other.transform.position); // The other ship's radar contributes to detection, but not tracking
            if (radarReturn * radarPower > 16) // If the radar gets a strong return with its own waves, tracking is achieved
            {
                return (true, true);
            }
            if (radarSignal * radarPower > 1) // If the radar gets a weak combined radar signal, only detection is achieved
                d = true;
        }
        if (hasIR)
        {
            float iRSignal = other.iRSignature / Vector3.SqrMagnitude(transform.position - other.transform.position); // All IR detection and tracking is passive only
            if (iRSignal * iRSensitivity > 1) // Determine if tracking has been achieved
            {
                return (true, true);
            }
        }
        return (d, false); // If tracking was not achieved, return the value of d and false for the tracking attempt
    }

    // Attempts to select a target from the given list of options using the current rules of engagement
    public void TrySelectTarget(List<List<GameObject>> taskForces)
    {
        ShipScript bestCandidate = null;
        float sqrBestRange = maxRange * maxRange;
        if (maxRange == 0)
            return; // Rule of Engagement max range of 0 indicates Do Not Enage order, so return immediately
        switch (shipClass)
        {
            case ShipClass.Screen:
                return; // Screen ships only detect enemies and can't select targets
            case ShipClass.Missile:
                if (maneuverMode == ManeuverMode.Intercept) // Don't change targets
                    return;
                for (int i = 0; i < taskForces.Count; i++) // Loop over all task forces that might contain a valid target
                {
                    for (int j = 0; j < taskForces[i].Count; j++) // Loop over all ships in each task force
                    {
                        ShipScript cur = taskForces[i][j].GetComponent<ShipScript>();
                        if (!cur.detected || cur.radarCrossSection < minRCS)
                        {
                            continue; // If the current ship is not detected or is outside the rules of engagement, we must ignore it
                        }
                        float sqrRange = Vector3.SqrMagnitude(transform.position - cur.transform.position);
                        sqrRange += 0.5f * cur.missilesTargeting * sqrRange; // Potential targets that already have missiles attacking them are deprioritized
                        if (sqrRange < sqrBestRange)
                        {
                            bestCandidate = cur;
                            sqrBestRange = sqrRange;
                        }
                    }
                }
                if (bestCandidate)
                {
                    referenceBody = bestCandidate.GetComponent<Rigidbody>();
                    bestCandidate.missilesTargeting++;
                    maneuverMode = ManeuverMode.Intercept;
                }
                return;
            case ShipClass.Laser:
                return;
        }
    }

    // Uses a design to set the initial stats of this ship
    public void UseDesign(ShipDesign shipDesign)
    {
        shipClass = shipDesign.GetShipClass();

        hasRadar = shipDesign.HasRadar();

        hasActiveRadar = shipDesign.HasActiveRadar();

        hasIR = shipDesign.HasIR();

        thrust = shipDesign.GetThrust();

        radarCrossSection = shipDesign.GetRadarCrossSection();

        radarPower = shipDesign.GetRadarPower();

        baseIRSignature = shipDesign.GetBaseIRSignature();

        iRSensitivity = shipDesign.GetIRSensitivity();

        rb.mass = shipDesign.GetMass();
    }

    // Initializes the marker object that will represent this ship on the radar screen, initializes fields
    public void Startup()
    {
        detected = false; // Start undetected and untracked
        tracked = false;
        if (hasActiveRadar) // Start with radar on only if the radar has active capabilities
            radarIsOn = true;
        else
            radarIsOn = false;
        iRSignature = baseIRSignature; // The IR signature should start as the base IR signature
        missilesTargeting = 0; // Nothing is targeting this ship at the start
        maneuverMode = ManeuverMode.Idle; // This ship is idle at the start
        maxRange = 0; // This ship won't engage at the start
        minRCS = 0; // This ship has no preferences for targets at the start
        float sqrDist = Vector3.SqrMagnitude(transform.position);
        float xscale = Mathf.Max(5 / (sqrDist / 1000 + 1), 0.2f) * 2;
        float yscale = Mathf.Min((sqrDist / 1000 + 1) / 5, 5) * 2;
        marker = Instantiate(markerPrefab, new Vector3(MarkerBearing() * 7 / 180, MarkerElevation() * 7 / 180 + 1), Quaternion.identity);
        if (radarCrossSection < 10)
            marker.GetComponent<SpriteRenderer>().sprite = smallSprite;
        else if (radarCrossSection < 1000)
            marker.GetComponent<SpriteRenderer>().sprite = mediumSprite;
        else
            marker.GetComponent<SpriteRenderer>().sprite = largeSprite;
        if (marker.TryGetComponent<DesignationScript>(out DesignationScript temp))
        {
            temp.creator = gameObject;
            temp.gameMaster = gameMaster;
        }
        rangeMarker = Instantiate(rangeMarkerPrefab, marker.transform);
        rangeMarker.transform.localScale = new Vector3(xscale, yscale, 1);
    }

    // Sets the ship to be tracked and fully activates the radar marker, activates collision checks
    public void setTracked()
    {
        GetComponent<SphereCollider>().enabled = true;
        detected = true;
        tracked = true;
        marker.SetActive(true);
        rangeMarker.SetActive(true);
    }

    // Sets the ship to be detected and only shows the direction of the ship on radar without range, deactivates collision checks
    public void setDetected()
    {
        GetComponent<SphereCollider>().enabled = false;
        detected = true;
        tracked = false;
        marker.SetActive(true);
        rangeMarker.SetActive(false);
    }

    // Sets the ship to be undetected and hides the radar marker, deactivates collision checks
    public void setUndetected()
    {
        GetComponent<SphereCollider>().enabled = false;
        detected = false;
        tracked = false;
        marker.SetActive(false);
        rangeMarker.SetActive(false);
    }

    void SetTargetPosition(Vector3 targetPosition)
    {
        this.targetPosition = targetPosition;
    }

    // Calculates the bearing angle (not to be confused with heading angle) of this ship from the origin
    float MarkerBearing()
    {
        float x = this.transform.position.x;
        float y = this.transform.position.y;
        if (x == 0 && y == 0)
            return 0;
        Vector3 flattenedPosition = new Vector3(x, y, 0);
        float bearing = Vector3.Angle(flattenedPosition, Vector3.right);
        
        if (y < 0)
            bearing = -bearing;

        return bearing;
    }

    // Calculates the elevation angle of this ship from the origin
    float MarkerElevation()
    {
        float x = this.transform.position.x;
        float y = this.transform.position.y;
        float z = this.transform.position.z;
        if (x == 0 && y == 0)
        {
            if (z > 0)
                return 90;
            if (z == 0)
                return 0;
            return -90;
        }
        Vector3 flattenedPosition = new Vector3(x, y, 0);
        float elevation = Vector3.Angle(flattenedPosition, this.transform.position);

        if (z < 0)
            elevation = -elevation;

        return elevation;
    }

    // Destroys the colliding ships
    private void OnTriggerEnter(Collider other)
    {
        ShipScript otherShip = other.GetComponent<ShipScript>();
        if (otherShip.referenceBody != null && otherShip.maneuverMode == ManeuverMode.Intercept)
            otherShip.referenceBody.GetComponent<ShipScript>().missilesTargeting--; // If the other ship was on interception course, decrement the missilesTargeting for the target
        if (referenceBody != null && maneuverMode == ManeuverMode.Intercept)
            referenceBody.GetComponent<ShipScript>().missilesTargeting--; // If this ship was on interception course, decrement the missilesTargeting for the target
        Destroy(other.GetComponent<ShipScript>().marker);
        Destroy(marker);
        Destroy(other);
        Destroy(this);
    }
}

// Represents a ship design that can either contain all of the stats of a ship or calculate the stats from a set of components
public class ShipDesign
{
    private bool useComponentBasedDesign;

    private ShipScript.ShipClass shipClass;

    private bool hasRadar;

    private bool hasActiveRadar;

    private bool hasIR;

    private float thrust;

    private float radarCrossSection;

    private float radarPower;

    private float baseIRSignature;

    private float iRSensitivity;

    private float mass;

    public ShipDesign(bool hR, bool hAR, bool hI, float t, float rcs, float pow, float iSig, float iSens, float m)
    {
        useComponentBasedDesign = false;
        hasRadar = hR;
        hasActiveRadar = hAR;
        hasIR = hI;
        thrust = t;
        radarCrossSection = rcs;
        radarPower = pow;
        baseIRSignature = iSig;
        iRSensitivity = iSens;
        mass = m;
    }

    public ShipScript.ShipClass GetShipClass()
    {
        if (!useComponentBasedDesign)
            return shipClass;
        else
            return shipClass; // Placeholder
    }

    public bool HasRadar()
    {
        if (!useComponentBasedDesign)
            return hasRadar;
        else
            return hasRadar; // Placeholder
    }

    public bool HasActiveRadar()
    {
        if (!useComponentBasedDesign)
            return hasActiveRadar;
        else
            return hasActiveRadar; // Placeholder
    }

    public bool HasIR()
    {
        if (!useComponentBasedDesign)
            return hasIR;
        else
            return hasIR; // Placeholder
    }

    public float GetThrust()
    {
        if (!useComponentBasedDesign)
            return thrust;
        else
            return thrust; // Placeholder
    }

    public float GetRadarCrossSection()
    {
        if (!useComponentBasedDesign)
            return radarCrossSection;
        else
            return radarCrossSection; // Placeholder
    }

    public float GetRadarPower()
    {
        if (!useComponentBasedDesign)
            return radarPower;
        else
            return radarPower; // Placeholder
    }

    public float GetBaseIRSignature()
    {
        if (!useComponentBasedDesign)
            return baseIRSignature;
        else
            return baseIRSignature; // Placeholder
    }

    public float GetIRSensitivity()
    {
        if (!useComponentBasedDesign)
            return iRSensitivity;
        else
            return iRSensitivity; // Placeholder
    }

    public float GetMass()
    {
        if (!useComponentBasedDesign)
            return mass;
        else
            return mass; // Placeholder
    }
}