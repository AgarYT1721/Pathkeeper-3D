using UnityEngine;
using TMPro;

/// <summary>
/// Master hazard and visual property controller for 3D tiles with full Arknights-style hazard themes,
/// exact status effect mechanics, and multi-tier Trap Upgrade System (Tier 1 ➔ Tier 2 ➔ Tier 3).
/// </summary>
public class TileProperty : MonoBehaviour
{
    [System.Serializable]
    public struct HazardData
    {
        public float damage;            // Instant damage (Spikes/Pitfall/Burn burst/Static)
        public float speedMult;         // 1.0 = normal, 0.5 = slow, 0 = freeze
        public float dotDamage;         // Damage per second (Burn/Poison)
        public float duration;          // Duration in seconds (-1 for infinite)
    }

    public HazardData currentData;

    public enum TileType { Normal, Spike, Slow, Burn, Freeze, Pitfall, Poison, Static, Bleed, Curse, Upgrade }
    public TileType type = TileType.Normal;

    [Header("Trap Upgrade Tier (1 to 3)")]
    public int upgradeLevel = 1;

    private Renderer blockRenderer;
    private GameObject currentVFX;
    private GameObject hazardDecalObj;
    private GameObject starBadgeObj;

    void Awake()
    {
        blockRenderer = GetComponent<Renderer>();
        SetType(type);
    }

    public void SetType(TileType newType)
    {
        type = newType;
        upgradeLevel = 1; // Reset to level 1 when morphed
        RecalculateHazardStats();
        ApplyHazardVisuals();
    }

    public int GetUpgradeCost()
    {
        if (upgradeLevel == 1) return 25; // Cost to upgrade Lv1 -> Lv2
        if (upgradeLevel == 2) return 40; // Cost to upgrade Lv2 -> Lv3
        return 0; // Already max level
    }

    public bool CanUpgrade()
    {
        return type != TileType.Normal && type != TileType.Upgrade && upgradeLevel < 3;
    }

    public bool Upgrade()
    {
        if (!CanUpgrade()) return false;

        upgradeLevel++;
        RecalculateHazardStats();
        ApplyHazardVisuals();
        return true;
    }

    public void RecalculateHazardStats()
    {
        switch (type)
        {
            case TileType.Normal:
                currentData = new HazardData { damage = 0, speedMult = 1f, dotDamage = 0, duration = 0 };
                break;

            case TileType.Spike:
                float spikeDmg = upgradeLevel == 1 ? 40f : (upgradeLevel == 2 ? 65f : 100f);
                currentData = new HazardData { damage = spikeDmg, speedMult = 1f, dotDamage = 0, duration = 0 };
                break;

            case TileType.Pitfall:
                float pitDmg = upgradeLevel == 1 ? 120f : (upgradeLevel == 2 ? 200f : 350f);
                currentData = new HazardData { damage = pitDmg, speedMult = 0f, dotDamage = 0, duration = 0 };
                break;

            case TileType.Slow:
                float slowSpeed = upgradeLevel == 1 ? 0.35f : (upgradeLevel == 2 ? 0.20f : 0.10f);
                float slowDur = upgradeLevel == 1 ? 3.5f : (upgradeLevel == 2 ? 5.0f : 7.0f);
                currentData = new HazardData { damage = 0, speedMult = slowSpeed, dotDamage = 0, duration = slowDur };
                break;

            case TileType.Static:
                float staticDmg = upgradeLevel == 1 ? 20f : (upgradeLevel == 2 ? 35f : 55f);
                float staticSpeed = upgradeLevel == 1 ? 0.55f : (upgradeLevel == 2 ? 0.40f : 0.25f);
                float staticDur = upgradeLevel == 1 ? 2.5f : (upgradeLevel == 2 ? 3.5f : 5.0f);
                currentData = new HazardData { damage = staticDmg, speedMult = staticSpeed, dotDamage = 0, duration = staticDur };
                break;

            case TileType.Freeze:
                float freezeDur = upgradeLevel == 1 ? 2.0f : (upgradeLevel == 2 ? 3.2f : 4.5f);
                currentData = new HazardData { damage = 0, speedMult = 0f, dotDamage = 0, duration = freezeDur };
                break;

            case TileType.Burn:
                float burnBurst = upgradeLevel == 1 ? 25f : (upgradeLevel == 2 ? 40f : 65f);
                float burnDot = upgradeLevel == 1 ? 8f : (upgradeLevel == 2 ? 15f : 25f);
                float burnDur = upgradeLevel == 1 ? 3.5f : (upgradeLevel == 2 ? 4.5f : 6.0f);
                currentData = new HazardData { damage = burnBurst, speedMult = 1f, dotDamage = burnDot, duration = burnDur };
                break;

            case TileType.Poison:
                float poisonDot = upgradeLevel == 1 ? 10f : (upgradeLevel == 2 ? 18f : 28f);
                currentData = new HazardData { damage = 0, speedMult = 1f, dotDamage = poisonDot, duration = -1f };
                break;

            case TileType.Bleed:
                float bleedMult = upgradeLevel == 1 ? 25f : (upgradeLevel == 2 ? 40f : 60f);
                float bleedDur = upgradeLevel == 1 ? 4.5f : (upgradeLevel == 2 ? 6.0f : 8.0f);
                currentData = new HazardData { damage = bleedMult, speedMult = 1f, dotDamage = 0f, duration = bleedDur };
                break;

            case TileType.Curse:
                float shred = upgradeLevel == 1 ? 0.35f : (upgradeLevel == 2 ? 0.60f : 1.00f);
                currentData = new HazardData { damage = shred, speedMult = 1f, dotDamage = 0f, duration = 999f };
                break;
        }
    }

    public Color GetBaseTypeColor()
    {
        switch (type)
        {
            case TileType.Burn:    return new Color(0.95f, 0.35f, 0.10f); // Magma / Lava Orange
            case TileType.Freeze:  return new Color(0.30f, 0.80f, 0.98f); // Frost Ice Cyan
            case TileType.Spike:   return new Color(0.40f, 0.44f, 0.50f); // Dark Iron Spike Steel
            case TileType.Pitfall: return new Color(0.10f, 0.12f, 0.16f); // Void Abyss Pitch Black
            case TileType.Poison:  return new Color(0.50f, 0.82f, 0.15f); // Acid Toxic Lime
            case TileType.Slow:    return new Color(0.58f, 0.38f, 0.22f); // Mud Earth Brown
            case TileType.Static:  return new Color(0.95f, 0.82f, 0.15f); // Lightning Shock Yellow
            case TileType.Bleed:   return new Color(0.78f, 0.12f, 0.18f); // Crimson Bleed Red
            case TileType.Curse:   return new Color(0.55f, 0.22f, 0.85f); // Void Curse Violet
            default:               return new Color(0.82f, 0.84f, 0.88f); // Clean Slate Gray Concrete
        }
    }

    public void SetTileColor(Color customColor)
    {
        if (blockRenderer == null) blockRenderer = GetComponent<Renderer>();
        if (blockRenderer != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            blockRenderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", customColor);
            block.SetColor("_Color", customColor);
            blockRenderer.SetPropertyBlock(block);
        }
    }

    public void ApplyHazardVisuals()
    {
        // 1. Apply base block color theme
        SetTileColor(GetBaseTypeColor());

        // 2. Clean up previous decals/VFX
        if (hazardDecalObj != null)
        {
            Destroy(hazardDecalObj);
            hazardDecalObj = null;
        }

        if (currentVFX != null)
        {
            Destroy(currentVFX);
            currentVFX = null;
        }

        if (starBadgeObj != null)
        {
            Destroy(starBadgeObj);
            starBadgeObj = null;
        }

        // 3. Create top-face hazard visual indicator if not a normal tile
        if (type != TileType.Normal && type != TileType.Upgrade)
        {
            hazardDecalObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            hazardDecalObj.name = $"HazardDecal_{type}";
            Destroy(hazardDecalObj.GetComponent<Collider>());
            hazardDecalObj.transform.SetParent(transform, false);
            hazardDecalObj.transform.localPosition = new Vector3(0f, 0.505f, 0f);
            hazardDecalObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            hazardDecalObj.transform.localScale = new Vector3(0.82f, 0.82f, 1f);

            Renderer decalR = hazardDecalObj.GetComponent<Renderer>();
            if (decalR != null)
            {
                Material decalMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                if (decalMat.shader == null) decalMat = new Material(Shader.Find("Unlit/Color"));

                Color decalColor = GetBaseTypeColor();
                decalColor.a = 0.55f;
                decalMat.color = decalColor;
                decalR.material = decalMat;
            }

            // Magma special light glow
            if (type == TileType.Burn)
            {
                GameObject lightObj = new GameObject("MagmaGlow");
                lightObj.transform.SetParent(transform, false);
                lightObj.transform.localPosition = new Vector3(0f, 0.6f, 0f);

                Light ptLight = lightObj.AddComponent<Light>();
                ptLight.type = LightType.Point;
                ptLight.color = new Color(1f, 0.45f, 0.1f);
                ptLight.range = 2.5f + (upgradeLevel * 0.5f);
                ptLight.intensity = 1.2f + (upgradeLevel * 0.4f);

                lightObj.AddComponent<LightFlicker>();
                currentVFX = lightObj;
            }

            // 4. Create High-Visibility Floating 3D Star Badge (Lv2: ★, Lv3: ★★)
            if (upgradeLevel > 1)
            {
                starBadgeObj = new GameObject("StarUpgradeBadge");
                starBadgeObj.transform.SetParent(transform, false);
                starBadgeObj.transform.localPosition = new Vector3(0.32f, 0.78f, 0.32f); // Floating above corner

                // Always face camera in both 2D and 2.5D view modes
                starBadgeObj.AddComponent<Billboard>();

                // Dark contrast backing plate
                GameObject bgPlate = GameObject.CreatePrimitive(PrimitiveType.Quad);
                bgPlate.name = "BadgeBacking";
                Destroy(bgPlate.GetComponent<Collider>());
                bgPlate.transform.SetParent(starBadgeObj.transform, false);
                bgPlate.transform.localScale = upgradeLevel == 2 ? new Vector3(0.40f, 0.40f, 1f) : new Vector3(0.60f, 0.40f, 1f);

                Renderer bgR = bgPlate.GetComponent<Renderer>();
                if (bgR != null)
                {
                    Material bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    if (bgMat.shader == null) bgMat = new Material(Shader.Find("Unlit/Color"));
                    bgMat.color = new Color(0.06f, 0.08f, 0.12f, 0.94f); // Dark tactical plate
                    bgR.material = bgMat;
                }

                // Glowing Golden Star Text
                GameObject textObj = new GameObject("BadgeText");
                textObj.transform.SetParent(starBadgeObj.transform, false);
                textObj.transform.localPosition = new Vector3(0f, 0f, -0.02f);

                TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
                tmp.text = upgradeLevel == 2 ? "★" : "★★";
                tmp.fontSize = upgradeLevel == 2 ? 4.5f : 4.8f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = upgradeLevel == 2 ? new Color(1f, 0.85f, 0.20f, 1f) : new Color(1f, 0.98f, 0.45f, 1f);
                tmp.sortingOrder = 60;
            }
        }
    }

    public void RefreshVisuals(bool isHighlighted)
    {
        if (isHighlighted)
        {
            Color pathGold = new Color(1f, 0.92f, 0.45f);
            SetTileColor(Color.Lerp(GetBaseTypeColor(), pathGold, 0.65f));
        }
        else
        {
            SetTileColor(GetBaseTypeColor());
        }
    }
}
