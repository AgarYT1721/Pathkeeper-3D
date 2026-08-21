using UnityEngine;

/// <summary>
/// Manages the Trap Placement & Tile Morph System for the Tower Master.
/// Allows purchasing, morphing, and upgrading floor tiles into multi-tier traps with Dungeon Points (DP).
/// Self-instantiating singleton guarantees it is always active.
/// </summary>
public class TrapShopManager : MonoBehaviour
{
    private static TrapShopManager _instance;
    public static TrapShopManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<TrapShopManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("TrapShopManager");
                    _instance = obj.AddComponent<TrapShopManager>();
                }
            }
            return _instance;
        }
    }

    public TileProperty.TileType activePlacingTrap = TileProperty.TileType.Normal; // Normal = Standard Rotate/Swap Mode

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public static int GetCostForTrap(TileProperty.TileType type)
    {
        switch (type)
        {
            case TileProperty.TileType.Spike:   return 20; // 40 Instant Physical Damage
            case TileProperty.TileType.Burn:    return 25; // 25 Burst + 8 DPS Burn DoT
            case TileProperty.TileType.Poison:  return 25; // 10 DPS Permanent Acid DoT
            case TileProperty.TileType.Freeze:  return 30; // Complete 2.0s Root Lock
            case TileProperty.TileType.Bleed:   return 25; // Kinetic Movement DoT (Anti-Rogue)
            case TileProperty.TileType.Curse:   return 35; // Permanent -35% Armor Shred (Anti-Tank)
            case TileProperty.TileType.Static:  return 20; // 20 Shock Damage + 45% Friction Slow
            case TileProperty.TileType.Slow:    return 20; // Mud Slow Trap
            case TileProperty.TileType.Pitfall: return 50; // 120 Massive Lethal Abyss Trap
            case TileProperty.TileType.Upgrade: return 25; // Upgrade tool
            default: return 0;
        }
    }

    public bool TryMorphTile(TileBlock tile, TileProperty.TileType targetType)
    {
        if (tile == null) return false;
        if (tile.isImmutable || tile.isStartTile || tile.isEndTile || tile.isCheckpoint)
        {
            FloatingTextManager.SpawnStatus(tile.transform.position, "LOCKED TILE", FloatingTextManager.TextType.Physical);
            return false;
        }

        TileProperty prop = tile.GetComponent<TileProperty>();
        if (prop == null) return false;

        // 1. Upgrade Tool
        if (targetType == TileProperty.TileType.Upgrade)
        {
            return TryUpgradeTile(tile);
        }

        // 2. Normal / Clear Tool
        if (targetType == TileProperty.TileType.Normal)
        {
            if (prop.type == TileProperty.TileType.Normal) return false;
            prop.SetType(TileProperty.TileType.Normal);
            FloatingTextManager.SpawnStatus(tile.transform.position, "CLEARED", FloatingTextManager.TextType.Cleanse);
            RefreshPath();
            return true;
        }

        // 3. If clicking existing trap with matching tool -> Upgrade it!
        if (prop.type == targetType)
        {
            return TryUpgradeTile(tile);
        }

        // 4. Morph tile into new trap
        int cost = GetCostForTrap(targetType);
        if (EconomyManager.Instance != null)
        {
            if (!EconomyManager.Instance.CanAfford(cost))
            {
                FloatingTextManager.SpawnStatus(tile.transform.position, "NOT ENOUGH DP!", FloatingTextManager.TextType.Physical);
                return false;
            }

            EconomyManager.Instance.SpendDP(cost);
        }

        prop.SetType(targetType);
        FloatingTextManager.SpawnStatus(tile.transform.position, $"+{targetType.ToString().ToUpper()} (-{cost} DP)", FloatingTextManager.TextType.Buff);
        RefreshPath();
        return true;
    }

    public bool TryUpgradeTile(TileBlock tile)
    {
        if (tile == null) return false;
        if (tile.isImmutable || tile.isStartTile || tile.isEndTile || tile.isCheckpoint)
        {
            FloatingTextManager.SpawnStatus(tile.transform.position, "LOCKED TILE", FloatingTextManager.TextType.Physical);
            return false;
        }

        TileProperty prop = tile.GetComponent<TileProperty>();
        if (prop == null || prop.type == TileProperty.TileType.Normal)
        {
            FloatingTextManager.SpawnStatus(tile.transform.position, "PLACE A TRAP FIRST", FloatingTextManager.TextType.Physical);
            return false;
        }

        if (!prop.CanUpgrade())
        {
            FloatingTextManager.SpawnStatus(tile.transform.position, "MAX TIER (★★)", FloatingTextManager.TextType.Cleanse);
            return false;
        }

        int cost = prop.GetUpgradeCost();
        if (EconomyManager.Instance != null)
        {
            if (!EconomyManager.Instance.CanAfford(cost))
            {
                FloatingTextManager.SpawnStatus(tile.transform.position, $"NEED {cost} DP TO UPGRADE", FloatingTextManager.TextType.Physical);
                return false;
            }

            EconomyManager.Instance.SpendDP(cost);
        }

        prop.Upgrade();
        string starStr = prop.upgradeLevel == 2 ? "★ TIER 2" : "★★ TIER 3 (MAX)";
        FloatingTextManager.SpawnStatus(tile.transform.position, $"UPGRADED! {starStr} (-{cost} DP)", FloatingTextManager.TextType.Buff);
        RefreshPath();
        return true;
    }

    private void RefreshPath()
    {
        GridManager gm = FindFirstObjectByType<GridManager>();
        if (gm != null) gm.TracePath();
    }
}
