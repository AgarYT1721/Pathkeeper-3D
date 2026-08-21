using System;
using UnityEngine;

/// <summary>
/// Manages the Dungeon Points (DP) Economy system for the Tower Master.
/// DP is earned by defeating adventurers and spent to swap or place/upgrade tiles in the Pre-Wave phase.
/// Self-instantiating singleton guarantees it is always active.
/// </summary>
public class EconomyManager : MonoBehaviour
{
    private static EconomyManager _instance;
    public static EconomyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<EconomyManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("EconomyManager");
                    _instance = obj.AddComponent<EconomyManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Dungeon Points (DP) Settings")]
    public int startingDP = 100;
    public int currentDP;
    public int tileSwapCost = 10;

    public static event Action<int> OnDPChanged;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            currentDP = startingDP;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public int GetCurrentDP()
    {
        return currentDP;
    }

    public bool CanAfford(int amount)
    {
        return currentDP >= amount;
    }

    public bool CanAffordSwap()
    {
        return currentDP >= tileSwapCost;
    }

    public bool SpendDP(int amount)
    {
        if (currentDP >= amount)
        {
            currentDP -= amount;
            Debug.Log($"[Tower Master] Spent {amount} DP. Remaining DP: {currentDP}");
            OnDPChanged?.Invoke(currentDP);
            return true;
        }
        return false;
    }

    public bool SpendSwapCost()
    {
        return SpendDP(tileSwapCost);
    }

    public void AddDP(int amount)
    {
        currentDP += amount;
        Debug.Log($"[Tower Master] Gained +{amount} DP! Total DP: {currentDP}");
        OnDPChanged?.Invoke(currentDP);
    }

    public void ResetEconomy()
    {
        currentDP = startingDP;
        OnDPChanged?.Invoke(currentDP);
    }
}
