using UnityEngine;

/// <summary>
/// Arknights-style sleek tactical floating health bar with delayed catch-up damage trail and camera billboarding.
/// </summary>
public class HealthBar : MonoBehaviour
{
    [Header("Positioning & Sizing")]
    public float heightOffset = 1.45f;
    public float barWidth = 0.90f;
    public float barHeight = 0.10f;

    [Header("Colors (Arknights Palette)")]
    public Color borderColor = new Color(0.08f, 0.10f, 0.14f, 0.95f);
    public Color healthFillColor = new Color(0.95f, 0.26f, 0.22f, 1f);      // Crisp Crimson Red
    public Color damageLagColor = new Color(1f, 0.85f, 0.35f, 0.95f);        // Smooth Amber catch-up trail
    public Color armorBorderColor = new Color(0.35f, 0.75f, 1f, 0.9f);      // Shield cyan border for armored units

    private Transform root;
    private Transform fillBar;
    private Transform lagBar;
    private SpriteRenderer fillSr;
    private SpriteRenderer lagSr;

    private float targetFill = 1f;
    private float lagFill = 1f;
    private static Sprite flatWhiteSprite;

    private void Awake()
    {
        EnsureWhiteSprite();
        BuildBarHierarchy();
    }

    private static void EnsureWhiteSprite()
    {
        if (flatWhiteSprite != null) return;
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] cols = new Color[16];
        for (int i = 0; i < 16; i++) cols[i] = Color.white;
        tex.SetPixels(cols);
        tex.Apply();
        flatWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    private void BuildBarHierarchy()
    {
        // 1. Root Container positioned above enemy head
        GameObject rootObj = new GameObject("HealthBarRoot");
        root = rootObj.transform;
        root.SetParent(transform, false);
        root.localPosition = new Vector3(0f, heightOffset, 0f);

        // Ensure Health bar faces the camera
        rootObj.AddComponent<Billboard>();

        // 2. Background Border
        GameObject bgObj = new GameObject("Border");
        bgObj.transform.SetParent(root, false);
        bgObj.transform.localScale = new Vector3(barWidth + 0.05f, barHeight + 0.04f, 1f);
        SpriteRenderer bgSr = bgObj.AddComponent<SpriteRenderer>();
        bgSr.sprite = flatWhiteSprite;
        bgSr.color = borderColor;
        bgSr.sortingOrder = 20;

        // 3. Dark Inner Track
        GameObject trackObj = new GameObject("Track");
        trackObj.transform.SetParent(root, false);
        trackObj.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        SpriteRenderer trackSr = trackObj.AddComponent<SpriteRenderer>();
        trackSr.sprite = flatWhiteSprite;
        trackSr.color = new Color(0.18f, 0.20f, 0.25f, 0.9f);
        trackSr.sortingOrder = 21;

        // 4. Yellow/Amber Damage Catch-up Lag Bar (Pivot on Left)
        GameObject lagObj = new GameObject("LagFill");
        lagBar = lagObj.transform;
        lagBar.SetParent(root, false);
        lagBar.localPosition = new Vector3(-barWidth / 2f, 0f, -0.001f);
        lagBar.localScale = new Vector3(barWidth, barHeight, 1f);
        lagSr = lagObj.AddComponent<SpriteRenderer>();
        lagSr.sprite = flatWhiteSprite;
        lagSr.color = damageLagColor;
        lagSr.sortingOrder = 22;

        // 5. Active Red Health Bar (Pivot on Left)
        GameObject fillObj = new GameObject("HealthFill");
        fillBar = fillObj.transform;
        fillBar.SetParent(root, false);
        fillBar.localPosition = new Vector3(-barWidth / 2f, 0f, -0.002f);
        fillBar.localScale = new Vector3(barWidth, barHeight, 1f);
        fillSr = fillObj.AddComponent<SpriteRenderer>();
        fillSr.sprite = flatWhiteSprite;
        fillSr.color = healthFillColor;
        fillSr.sortingOrder = 23;
    }

    public void UpdateHealth(float currentHP, float maxHP, float armorPercent = 0f)
    {
        if (maxHP <= 0) return;

        targetFill = Mathf.Clamp01(currentHP / maxHP);

        // Instant red bar update
        if (fillBar != null)
        {
            fillBar.localScale = new Vector3(barWidth * targetFill, barHeight, 1f);
            // Re-center pivot from left
            fillBar.localPosition = new Vector3(-barWidth / 2f + (barWidth * targetFill) / 2f, 0f, -0.002f);
        }

        // Change border color to cyan if unit is heavy armored (like Tanker)
        if (armorPercent > 0.2f && root != null)
        {
            SpriteRenderer bg = root.Find("Border")?.GetComponent<SpriteRenderer>();
            if (bg != null) bg.color = Color.Lerp(borderColor, armorBorderColor, 0.5f);
        }
    }

    private void Update()
    {
        // Smoothly lerp the amber lag bar toward current target fill (Arknights damage trail effect)
        if (lagFill > targetFill)
        {
            lagFill = Mathf.MoveTowards(lagFill, targetFill, Time.deltaTime * 0.85f);
            if (lagBar != null)
            {
                lagBar.localScale = new Vector3(barWidth * lagFill, barHeight, 1f);
                lagBar.localPosition = new Vector3(-barWidth / 2f + (barWidth * lagFill) / 2f, 0f, -0.001f);
            }
        }
        else if (lagFill < targetFill)
        {
            lagFill = targetFill;
            if (lagBar != null)
            {
                lagBar.localScale = new Vector3(barWidth * lagFill, barHeight, 1f);
                lagBar.localPosition = new Vector3(-barWidth / 2f + (barWidth * lagFill) / 2f, 0f, -0.001f);
            }
        }
    }
}
