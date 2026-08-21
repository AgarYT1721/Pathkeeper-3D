using UnityEngine;

/// <summary>
/// Spawns Arknights-style floating damage numbers, status effect popups, and healing text in 3D world space.
/// </summary>
public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }

    public enum TextType
    {
        Physical,
        Burn,
        Poison,
        Bleed,
        Static,
        Freeze,
        Curse,
        Heal,
        Cleanse,
        Buff
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void SpawnDamage(Vector3 worldPos, float amount, TextType type = TextType.Physical)
    {
        if (amount < 0.5f) return;

        string displayStr = $"-{Mathf.RoundToInt(amount)}";
        Color color = GetColorForType(type);
        bool isCrit = amount >= 60f;

        if (type == TextType.Burn) displayStr += " BURN";
        else if (type == TextType.Poison) displayStr += " POISON";
        else if (type == TextType.Bleed) displayStr += " BLEED";
        else if (type == TextType.Static) displayStr += " SHOCK";

        CreateFloatingText(worldPos, displayStr, color, isCrit ? 1.3f : 1.0f, isCrit);
    }

    public static void SpawnStatus(Vector3 worldPos, string text, TextType type)
    {
        Color color = GetColorForType(type);
        CreateFloatingText(worldPos, text, color, 1.15f, true);
    }

    public static void SpawnHeal(Vector3 worldPos, float amount, string customLabel = null)
    {
        string displayStr = customLabel != null ? customLabel : $"+{Mathf.RoundToInt(amount)} HEAL";
        Color color = new Color(0.35f, 0.95f, 0.45f, 1f); // Vibrant Holy Green
        CreateFloatingText(worldPos, displayStr, color, 1.2f, true);
    }

    private static void CreateFloatingText(Vector3 worldPos, string text, Color color, float scaleMultiplier, bool isCrit)
    {
        GameObject textObj = new GameObject("FloatingText");
        textObj.transform.position = worldPos + new Vector3(0f, 1.4f, 0f);

        FloatingText ft = textObj.AddComponent<FloatingText>();
        ft.Initialize(text, color, scaleMultiplier, isCrit);
    }

    private static Color GetColorForType(TextType type)
    {
        switch (type)
        {
            case TextType.Physical: return new Color(1f, 0.25f, 0.25f, 1f);   // Crimson Red
            case TextType.Burn:     return new Color(1f, 0.50f, 0.12f, 1f);   // Fire Orange
            case TextType.Poison:   return new Color(0.45f, 0.88f, 0.15f, 1f); // Toxic Acid Lime
            case TextType.Bleed:    return new Color(0.85f, 0.10f, 0.20f, 1f); // Blood Red
            case TextType.Static:   return new Color(1f, 0.88f, 0.20f, 1f);   // Shock Yellow
            case TextType.Freeze:   return new Color(0.30f, 0.85f, 1f, 1f);   // Frost Cyan
            case TextType.Curse:    return new Color(0.70f, 0.25f, 0.95f, 1f); // Void Violet
            case TextType.Heal:     return new Color(0.35f, 0.95f, 0.45f, 1f); // Emerald Green
            case TextType.Cleanse:  return new Color(1f, 0.92f, 0.40f, 1f);   // Holy Gold
            case TextType.Buff:     return new Color(0.95f, 0.40f, 0.90f, 1f); // Bard Magenta
            default:                return Color.white;
        }
    }
}
