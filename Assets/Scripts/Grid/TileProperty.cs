using UnityEngine;

/// <summary>
/// Master hazard and visual property controller for 3D tiles with full Arknights-style hazard themes.
/// </summary>
public class TileProperty : MonoBehaviour
{
    [System.Serializable]
    public struct HazardData
    {
        public float damage;            // Instant damage (Spikes/Pitfall)
        public float speedMult;         // 1.0 = normal, 0.5 = slow, 0 = freeze
        public float dotDamage;         // Damage per second (Burn/Poison)
        public float duration;          // Duration in seconds (-1 for infinite)
    }

    public HazardData currentData;

    public enum TileType { Normal, Spike, Slow, Burn, Freeze, Pitfall, Poison, Static, Bleed, Curse }
    public TileType type = TileType.Normal;

    private Renderer blockRenderer;
    private GameObject currentVFX;
    private GameObject hazardDecalObj;

    void Awake()
    {
        blockRenderer = GetComponent<Renderer>();
        ApplyHazardVisuals();
    }

    public void SetType(TileType newType)
    {
        type = newType;
        ApplyHazardVisuals();
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
            block.SetColor("_BaseColor", customColor);  // URP standard property
            block.SetColor("_Color", customColor);      // Built-in fallback
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

        // 3. Create top-face hazard visual indicator if not a normal tile
        if (type != TileType.Normal)
        {
            hazardDecalObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            hazardDecalObj.name = $"HazardDecal_{type}";
            Destroy(hazardDecalObj.GetComponent<Collider>());
            hazardDecalObj.transform.SetParent(transform, false);
            hazardDecalObj.transform.localPosition = new Vector3(0f, 0.505f, 0f); // Sits on top face
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
                ptLight.range = 2.5f;
                ptLight.intensity = 1.2f;

                lightObj.AddComponent<LightFlicker>();
                currentVFX = lightObj;
            }
        }
    }

    public void RefreshVisuals(bool isHighlighted)
    {
        if (isHighlighted)
        {
            // Blend hazard color with bright golden yellow path color
            Color pathGold = new Color(1f, 0.92f, 0.45f);
            SetTileColor(Color.Lerp(GetBaseTypeColor(), pathGold, 0.65f));
        }
        else
        {
            SetTileColor(GetBaseTypeColor());
        }
    }
}
