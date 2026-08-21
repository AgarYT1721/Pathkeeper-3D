using UnityEngine;
using TMPro;

/// <summary>
/// Individual floating combat text instance that pops, drifts upward, and fades smoothly (Arknights style).
/// </summary>
public class FloatingText : MonoBehaviour
{
    private TextMeshPro tmp;
    private float lifetime = 0.85f;
    private float timer = 0f;
    private Vector3 moveVelocity;
    private Vector3 initialScale;

    public void Initialize(string text, Color textColor, float scaleMultiplier = 1f, bool isCritical = false)
    {
        tmp = gameObject.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = isCritical ? 4.2f : 3.2f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = textColor;
        tmp.sortingOrder = 50; // Always render in front of sprites and health bars

        // Attach Billboard so text always faces the camera
        gameObject.AddComponent<Billboard>();

        // Random horizontal drift
        float randomX = Random.Range(-0.35f, 0.35f);
        moveVelocity = new Vector3(randomX, isCritical ? 1.4f : 1.1f, 0f);

        initialScale = Vector3.one * scaleMultiplier;
        transform.localScale = Vector3.zero; // Start small for pop effect
        timer = 0f;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = timer / lifetime;

        // 1. Upward float
        transform.position += moveVelocity * Time.deltaTime;
        moveVelocity.y = Mathf.Lerp(moveVelocity.y, 0.4f, Time.deltaTime * 3f);

        // 2. Snappy pop-in scale (0 to 1 with slight overshoot)
        if (t < 0.2f)
        {
            float popT = t / 0.2f;
            float scale = Mathf.Sin(popT * Mathf.PI * 0.5f) * 1.15f;
            transform.localScale = initialScale * scale;
        }
        else
        {
            transform.localScale = initialScale;
        }

        // 3. Smooth fade out
        if (t > 0.45f)
        {
            float fadeT = (t - 0.45f) / 0.55f;
            Color c = tmp.color;
            c.a = Mathf.Lerp(1f, 0f, fadeT);
            tmp.color = c;
        }

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
