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

    public bool detected;

    public float thrust;

    public float radarCrossSection;

    public float radarPower;

    public float radarSensitivity;

    public float targetRadius; // For Formation mode only

    public Vector3 targetPosition; // For Rendezvous mode only

    public Rigidbody rb;

    public Rigidbody referenceBody;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float deltaV = Time.deltaTime * thrust / rb.mass;
        Vector3 relativeVelocity;
        Vector3 relativePosition;
        Vector3 targetVelocity;
        Vector3 velocityChange;
        Vector3 radialVelocity;
        Vector3 lateralVelocity;
        marker.transform.position = new Vector3(MarkerBearing() * 7 / 180, MarkerElevation() * 7 / 180);
        Debug.Log(MarkerBearing() + " " + MarkerElevation());
        if (referenceBody == null || referenceBody == rb)
            return;
        switch (maneuverMode) {
            case ManeuverMode.Rendezvous:
                relativeVelocity = rb.velocity - referenceBody.velocity;
                relativePosition = rb.position - referenceBody.position - targetPosition;
                targetVelocity = -2 * relativePosition / Mathf.Sqrt(2 * Vector3.Magnitude(relativePosition) / (0.95f * thrust / rb.mass));
                velocityChange = targetVelocity - relativeVelocity;
                if (deltaV * deltaV < Vector3.SqrMagnitude(velocityChange))
                {
                    rb.AddForce(Vector3.Normalize(velocityChange) * deltaV, ForceMode.VelocityChange);
                }
                else
                {
                    rb.AddForce(velocityChange, ForceMode.VelocityChange);
                }
                break;
            case ManeuverMode.Intercept:
                relativeVelocity = rb.velocity - referenceBody.velocity;
                relativePosition = rb.position - referenceBody.position; // Target position will always be the center of the target body, so is excluded from this calculation
                radialVelocity = Vector3.Dot(relativeVelocity, relativePosition) * relativePosition / Vector3.SqrMagnitude(relativePosition);
                lateralVelocity = relativeVelocity - radialVelocity;
                if (deltaV * deltaV < Vector3.SqrMagnitude(lateralVelocity))
                {
                    rb.AddForce(-Vector3.Normalize(lateralVelocity) * deltaV, ForceMode.VelocityChange);
                }
                else
                {
                    rb.AddForce((-Vector3.Normalize(relativePosition) * Mathf.Sqrt(deltaV * deltaV - Vector3.SqrMagnitude(lateralVelocity))) - lateralVelocity, ForceMode.VelocityChange);
                }
                break;
            case ManeuverMode.Formation:
                break;
            case ManeuverMode.Hold:
                break;
        }
    }

    public void Startup()
    {
        marker = Instantiate(markerPrefab, new Vector3(MarkerBearing() * 7 / 180, MarkerElevation() * 7 / 180), Quaternion.identity);
    }

    void SetTargetPosition(Vector3 targetPosition)
    {
        this.targetPosition = targetPosition;
    }

    float MarkerBearing()
    {
        float x = this.transform.position.x;
        float y = this.transform.position.y;
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
        Vector3 flattenedPosition = new Vector3(x, y, 0);
        float elevation = Vector3.Angle(flattenedPosition, this.transform.position);

        if (z < 0)
            elevation = -elevation;

        return elevation;
    }
}
