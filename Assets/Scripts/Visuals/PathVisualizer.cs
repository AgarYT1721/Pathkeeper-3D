using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders a smooth, buttery animated tactical guiding line with flowing energy waves (Arknights style).
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class PathVisualizer : MonoBehaviour
{
    private LineRenderer line;

    [Header("Flow Animation Settings")]
    public float flowSpeed = 1.2f;          // Speed of the traveling energy pulses
    public float lineThickness = 0.09f;     // Sleek tactical thickness
    public int pulsesAcrossPath = 4;        // Number of traveling pulses visible along the route

    [Header("Colors")]
    public Color baseLineColor = new Color(0.2f, 0.65f, 0.85f, 0.35f);  // Soft background cyan
    public Color activePulseColor = new Color(0.35f, 0.95f, 1f, 0.95f); // Bright traveling glow pulse

    private List<Vector3> rawPathPoints = new List<Vector3>();
    private List<Vector3> smoothPathPoints = new List<Vector3>();
    private Material lineMaterial;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.startWidth = lineThickness;
        line.endWidth = lineThickness;
        line.numCornerVertices = 6;
        line.numCapVertices = 6;
        line.useWorldSpace = true;
        line.alignment = LineAlignment.View;

        // Use clean transparent unlit shader
        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Sprites/Default");
        if (s == null) s = Shader.Find("Unlit/Color");

        lineMaterial = new Material(s);
        if (lineMaterial.HasProperty("_Surface")) lineMaterial.SetFloat("_Surface", 1); // Transparent
        if (lineMaterial.HasProperty("_Blend")) lineMaterial.SetFloat("_Blend", 0);     // Alpha blend

        line.material = lineMaterial;
    }

    public void UpdatePath(List<Vector3> points)
    {
        if (points == null || points.Count == 0)
        {
            line.positionCount = 0;
            rawPathPoints.Clear();
            smoothPathPoints.Clear();
            return;
        }

        rawPathPoints = new List<Vector3>(points);

        // Subdivide waypoints so the vertex-gradient animation is buttery smooth
        smoothPathPoints.Clear();
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 pA = points[i];
            Vector3 pB = points[i + 1];
            int subdivisions = 6; // 6 smooth steps per tile

            for (int s = 0; s < subdivisions; s++)
            {
                float t = (float)s / subdivisions;
                smoothPathPoints.Add(Vector3.Lerp(pA, pB, t));
            }
        }
        smoothPathPoints.Add(points[points.Count - 1]);

        line.positionCount = smoothPathPoints.Count;
        line.SetPositions(smoothPathPoints.ToArray());
    }

    void Update()
    {
        if (line.positionCount == 0 || smoothPathPoints.Count < 2) return;

        // Animate the travelling gradient flow from Start -> Goal
        float phase = (Time.time * flowSpeed) % 1.0f;

        // Build 8-key dynamic gradient traveling forward along the path
        int keyCount = 8;
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[keyCount];
        GradientColorKey[] colorKeys = new GradientColorKey[keyCount];

        for (int i = 0; i < keyCount; i++)
        {
            float normalizedPos = (float)i / (keyCount - 1);
            
            // Calculate traveling sine wave pulse position
            float wavePhase = (normalizedPos * pulsesAcrossPath - phase * pulsesAcrossPath) * Mathf.PI * 2f;
            float pulseIntensity = Mathf.Sin(wavePhase);
            pulseIntensity = Mathf.Clamp01(Mathf.Pow((pulseIntensity + 1f) * 0.5f, 3f)); // Crisp wave peaks

            Color keyColor = Color.Lerp(baseLineColor, activePulseColor, pulseIntensity);
            float keyAlpha = Mathf.Lerp(baseLineColor.a, activePulseColor.a, pulseIntensity);

            alphaKeys[i] = new GradientAlphaKey(keyAlpha, normalizedPos);
            colorKeys[i] = new GradientColorKey(keyColor, normalizedPos);
        }

        Gradient g = new Gradient();
        g.SetKeys(colorKeys, alphaKeys);
        line.colorGradient = g;
    }
}
