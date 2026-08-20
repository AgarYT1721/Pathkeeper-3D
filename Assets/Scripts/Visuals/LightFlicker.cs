using UnityEngine;

/// <summary>
/// Organic Perlin-noise light flicker for fire, magma, and torches.
/// </summary>
public class LightFlicker : MonoBehaviour
{
    public float minIntensity = 0.8f;
    public float maxIntensity = 1.3f;
    public float flickerSpeed = 3.5f;

    private Light myLight;
    private float randomOffset;

    void Start()
    {
        myLight = GetComponent<Light>();
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (myLight != null)
        {
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + randomOffset, 0f);
            myLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
        }
    }
}
