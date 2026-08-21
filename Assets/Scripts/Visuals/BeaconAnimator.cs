using UnityEngine;

/// <summary>
/// Animates 3D Beacons and Dungeon Core Crystals with smooth levitation bobbing,
/// continuous 3D rotation, and gentle light pulsing.
/// </summary>
public class BeaconAnimator : MonoBehaviour
{
    public Vector3 rotationAxis = new Vector3(30f, 60f, 20f);
    public float hoverSpeed = 2.4f;
    public float hoverAmplitude = 0.08f;
    public bool pulseLight = true;

    private Vector3 initialLocalPos;
    private Light beaconLight;
    private float baseIntensity;

    void Start()
    {
        initialLocalPos = transform.localPosition;
        beaconLight = GetComponentInChildren<Light>();
        if (beaconLight != null)
        {
            baseIntensity = beaconLight.intensity;
        }
    }

    void Update()
    {
        // 1. Continuous 3D Rotation
        transform.Rotate(rotationAxis * Time.deltaTime, Space.Self);

        // 2. Sinusoidal Hover Floating
        float hoverOffset = Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
        transform.localPosition = initialLocalPos + new Vector3(0f, hoverOffset, 0f);

        // 3. Gentle Light Pulse
        if (pulseLight && beaconLight != null)
        {
            float pulse = Mathf.Sin(Time.time * (hoverSpeed * 1.5f)) * 0.35f;
            beaconLight.intensity = baseIntensity + pulse;
        }
    }
}
