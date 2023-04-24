using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipScript : MonoBehaviour
{
    public enum ShipClass {Screen, Capital, Missile}

    public enum ManeuverMode {Idle, Rendezvous, Hold, Intercept, Formation}

    public ShipClass shipClass;

    public ManeuverMode maneuverMode;

    public GameObject markerPrefab;

    public GameObject marker;

    public GameObject rangeMarkerPrefab;

    public GameObject rangeMarker;

    public GameObject gameMaster;

    public bool detected;

    public bool tracked;

    public bool hasRadar;

    public bool radarIsOn;

    public bool hasIR;

    public float thrust;

    public float radarCrossSection;

    public float radarPower;

    public float baseIRSignature;

    public float iRSignature;

    public float iRSensitivity;

    public float targetRadius; // For Formation mode only

    public Vector3 gradient; // For Formation mode only

    public Vector3 targetPosition; // For Rendezvous mode only

    public Rigidbody rb; // This rigidbody

    public Rigidbody referenceBody; // Another ship's rigidbody

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (gameMaster.GetComponent<GameMaster>().isPaused)
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
        marker.transform.position = new Vector3(MarkerBearing() * 7 / 180, MarkerElevation() * 7 / 180);
        rangeMarker.transform.localScale = new Vector3(xscale, yscale, 1);
        if (referenceBody == null || referenceBody == rb)
        {
            iRSignature = baseIRSignature;
            return;
        }
        switch (maneuverMode) {
            case ManeuverMode.Rendezvous:
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
            case ManeuverMode.Intercept:
                relativeVelocity = rb.velocity - referenceBody.velocity;
                relativePosition = rb.position - referenceBody.position; // Target position will always be the center of the target body, so is excluded from this calculation
                if (relativePosition.Equals(Vector3.zero))
                {
                    iRSignature = baseIRSignature;
                    break;
                }
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
            case ManeuverMode.Formation:
                relativeVelocity = rb.velocity - referenceBody.velocity;
                relativePosition = rb.position - referenceBody.position - targetPosition;
                if (!relativePosition.Equals(Vector3.zero))
                {
                    if (!(relativePosition + gradient).Equals(Vector3.zero))
                        relativePosition = relativePosition - Vector3.Normalize(relativePosition + gradient) * targetRadius;
                    else
                        relativePosition = relativePosition - Vector3.Normalize(relativePosition) * targetRadius;
                }
                else
                {
                    relativePosition = targetRadius * Vector3.left;
                }
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
            case ManeuverMode.Hold:
                velocityChange = referenceBody.velocity - rb.velocity;
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
            case ManeuverMode.Idle:
                iRSignature = baseIRSignature;
                break;
        }
    }

    public void Startup()
    {
        float sqrDist = Vector3.SqrMagnitude(transform.position);
        float xscale = Mathf.Max(5 / (sqrDist / 1000 + 1), 0.2f) * 2;
        float yscale = Mathf.Min((sqrDist / 1000 + 1) / 5, 5) * 2;
        marker = Instantiate(markerPrefab, new Vector3(MarkerBearing() * 7 / 180, MarkerElevation() * 7 / 180), Quaternion.identity);
        if (marker.TryGetComponent<DesignationScript>(out DesignationScript temp))
        {
            temp.creator = gameObject;
            temp.gameMaster = gameMaster;
        }
        rangeMarker = Instantiate(rangeMarkerPrefab, marker.transform);
        rangeMarker.transform.localScale = new Vector3(xscale, yscale, 1);
    }

    public void setTracked()
    {
        detected = true;
        tracked = true;
        marker.SetActive(true);
        rangeMarker.SetActive(true);
    }

    public void setDetected()
    {
        detected = true;
        tracked = false;
        marker.SetActive(true);
        rangeMarker.SetActive(false);
    }

    public void setUndetected()
    {
        detected = false;
        tracked = false;
        marker.SetActive(false);
        rangeMarker.SetActive(false);
    }

    void SetTargetPosition(Vector3 targetPosition)
    {
        this.targetPosition = targetPosition;
    }

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

    private void OnTriggerEnter(Collider other)
    {
        Destroy(other.GetComponent<ShipScript>().marker);
        Destroy(marker);
        Destroy(other);
        Destroy(this);
    }
}
