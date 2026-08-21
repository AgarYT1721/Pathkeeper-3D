using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Master Wave & Campaign Controller managing the 5-Wave Adventurer Campaign,
/// Dungeon Core Life Points, wave compositions, and Victory/Defeat states.
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    public enum WaveState { PreWavePlanning, WaveInProgress, WaveVictory, GameOverDefeat, CampaignComplete }
    public WaveState currentState = WaveState.PreWavePlanning;

    [Header("Dungeon Core (End Tile Life Points)")]
    public int maxDungeonCoreHP = 10;
    public int currentDungeonCoreHP = 10;

    [Header("Campaign Progression")]
    public int currentWave = 1;
    public int totalWaves = 5;
    public int enemiesRemainingInWave = 0;

    public static event Action<int, int> OnCoreHealthChanged;
    public static event Action<int, int> OnWaveChanged;
    public static event Action<WaveState> OnWaveStateChanged;

    private GridManager gridManager;
    private EnemySpawner enemySpawner;
    private Coroutine activeWaveRoutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            currentDungeonCoreHP = maxDungeonCoreHP;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        enemySpawner = FindFirstObjectByType<EnemySpawner>();
    }

    public void StartCurrentWave()
    {
        if (currentState == WaveState.WaveInProgress || currentState == WaveState.GameOverDefeat || currentState == WaveState.CampaignComplete)
        {
            return;
        }

        currentState = WaveState.WaveInProgress;
        OnWaveStateChanged?.Invoke(currentState);

        List<Enemy.EnemyClass> waveRoster = GetRosterForWave(currentWave);
        enemiesRemainingInWave = waveRoster.Count;
        OnWaveChanged?.Invoke(currentWave, enemiesRemainingInWave);

        if (activeWaveRoutine != null) StopCoroutine(activeWaveRoutine);
        activeWaveRoutine = StartCoroutine(SpawnWaveRoutine(waveRoster));
    }

    private List<Enemy.EnemyClass> GetRosterForWave(int waveIndex)
    {
        List<Enemy.EnemyClass> roster = new List<Enemy.EnemyClass>();

        switch (waveIndex)
        {
            case 1: // Wave 1: Scout Party (4 Swordsmen, 2 Rogues)
                roster.AddRange(new[] { Enemy.EnemyClass.Swordsman, Enemy.EnemyClass.Swordsman, Enemy.EnemyClass.Rogue, Enemy.EnemyClass.Swordsman, Enemy.EnemyClass.Rogue, Enemy.EnemyClass.Swordsman });
                break;

            case 2: // Wave 2: Armored Incursion (3 Swordsmen, 2 Tankers, 2 Rogues)
                roster.AddRange(new[] { Enemy.EnemyClass.Swordsman, Enemy.EnemyClass.Tanker, Enemy.EnemyClass.Rogue, Enemy.EnemyClass.Swordsman, Enemy.EnemyClass.Tanker, Enemy.EnemyClass.Rogue, Enemy.EnemyClass.Swordsman });
                break;

            case 3: // Wave 3: Organized Raid Party (2 Tankers, 2 Swordsmen, 1 Supporter Bard, 1 Priest)
                roster.AddRange(new[] { Enemy.EnemyClass.Tanker, Enemy.EnemyClass.Swordsman, Enemy.EnemyClass.Supporter, Enemy.EnemyClass.Priest, Enemy.EnemyClass.Swordsman, Enemy.EnemyClass.Tanker, Enemy.EnemyClass.Rogue });
                break;

            case 4: // Wave 4: Elite Guild Expedition (3 Tankers, 2 Priests, 2 Supporters, 3 Rogues)
                roster.AddRange(new[] { Enemy.EnemyClass.Tanker, Enemy.EnemyClass.Supporter, Enemy.EnemyClass.Priest, Enemy.EnemyClass.Tanker, Enemy.EnemyClass.Rogue, Enemy.EnemyClass.Priest, Enemy.EnemyClass.Supporter, Enemy.EnemyClass.Tanker, Enemy.EnemyClass.Rogue, Enemy.EnemyClass.Rogue });
                break;

            case 5: // Wave 5: Final Boss Raid (Paladin Champion + Holy Escort)
                roster.AddRange(new[] { Enemy.EnemyClass.Tanker, Enemy.EnemyClass.Supporter, Enemy.EnemyClass.Priest, Enemy.EnemyClass.Paladin, Enemy.EnemyClass.Priest, Enemy.EnemyClass.Tanker, Enemy.EnemyClass.Rogue, Enemy.EnemyClass.Rogue });
                break;

            default:
                roster.AddRange(new[] { Enemy.EnemyClass.Swordsman, Enemy.EnemyClass.Tanker, Enemy.EnemyClass.Rogue });
                break;
        }

        return roster;
    }

    private IEnumerator SpawnWaveRoutine(List<Enemy.EnemyClass> roster)
    {
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < roster.Count; i++)
        {
            if (currentState != WaveState.WaveInProgress) yield break;

            SpawnSingleEnemy(roster[i]);
            yield return new WaitForSeconds(2.2f); // Spacing between adventurers
        }
    }

    private void SpawnSingleEnemy(Enemy.EnemyClass enemyClass)
    {
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null || gridManager.currentPathWorldPositions == null || gridManager.currentPathWorldPositions.Count == 0) return;

        Vector3 spawnPos = gridManager.currentPathWorldPositions[0];

        // Create 2D Billboard Character Sprite in 3D (Arknights 2.5D Chibi)
        GameObject enemy = new GameObject($"Enemy_{enemyClass}");
        enemy.transform.position = spawnPos;

        // 1. Upright 2D Billboard Sprite
        GameObject spriteObj = new GameObject("Sprite");
        spriteObj.transform.SetParent(enemy.transform, false);
        spriteObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        spriteObj.transform.localScale = Vector3.one * 1.5f;

        SpriteRenderer sr = spriteObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;
        sr.sprite = EnemySpawner.GenerateChibiSprite(enemyClass);

        spriteObj.AddComponent<Billboard>();

        // 2. Drop Shadow Oval
        GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
        shadow.name = "DropShadow";
        Destroy(shadow.GetComponent<Collider>());
        shadow.transform.SetParent(enemy.transform, false);
        shadow.transform.localPosition = new Vector3(0f, 0.01f, 0f);
        shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        shadow.transform.localScale = new Vector3(0.55f, 0.35f, 1f);

        Renderer shadowR = shadow.GetComponent<Renderer>();
        if (shadowR != null)
        {
            Material shadowMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (shadowMat.shader == null) shadowMat = new Material(Shader.Find("Unlit/Color"));
            shadowMat.color = new Color(0f, 0f, 0f, 0.45f);
            shadowR.material = shadowMat;
        }

        // 3. Collision & Physics
        SphereCollider sc = enemy.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.4f;
        sc.center = new Vector3(0f, 0.35f, 0f);

        Rigidbody rb = enemy.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // 4. Enemy Stats & Pathfinding
        Enemy enemyScript = enemy.AddComponent<Enemy>();
        enemyScript.InitializeEnemy(enemyClass);

        EnemyPathFinding pathScript = enemy.AddComponent<EnemyPathFinding>();
        if (gridManager.currentPathTiles != null && gridManager.currentPathTiles.Count > 0)
        {
            pathScript.SetPathWithTiles(gridManager.currentPathTiles, enemyClass);
        }
        else
        {
            pathScript.SetPath(gridManager.currentPathWorldPositions, enemyClass);
        }
    }

    public void OnEnemyDefeated(Enemy enemy)
    {
        CheckWaveCompletion();
    }

    public void OnEnemyReachedCore(Enemy enemy)
    {
        DamageDungeonCore(1);
        CheckWaveCompletion();
    }

    public void DamageDungeonCore(int damage)
    {
        currentDungeonCoreHP = Mathf.Max(0, currentDungeonCoreHP - damage);
        OnCoreHealthChanged?.Invoke(currentDungeonCoreHP, maxDungeonCoreHP);

        if (currentDungeonCoreHP <= 0)
        {
            currentState = WaveState.GameOverDefeat;
            OnWaveStateChanged?.Invoke(currentState);
            Debug.LogError("[Tower Master] The Dungeon Core has been destroyed! Adventurers prevail!");
        }
    }

    private void CheckWaveCompletion()
    {
        enemiesRemainingInWave = Mathf.Max(0, enemiesRemainingInWave - 1);
        OnWaveChanged?.Invoke(currentWave, enemiesRemainingInWave);

        if (enemiesRemainingInWave <= 0 && currentDungeonCoreHP > 0)
        {
            if (currentWave < totalWaves)
            {
                StartCoroutine(WaveVictoryTransitionRoutine());
            }
            else
            {
                currentState = WaveState.CampaignComplete;
                OnWaveStateChanged?.Invoke(currentState);
                Debug.Log("[Tower Master] Victory! All adventuring parties annihilated! The Dungeon is secure!");
            }
        }
    }

    private IEnumerator WaveVictoryTransitionRoutine()
    {
        currentState = WaveState.WaveVictory;
        OnWaveStateChanged?.Invoke(currentState);

        // Award Wave Clear DP bonus to Tower Master
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddDP(50);
        }

        yield return new WaitForSeconds(2.0f);

        // Advance to next wave
        currentWave++;
        currentState = WaveState.PreWavePlanning;
        OnWaveStateChanged?.Invoke(currentState);

        // Shift Checkpoint to a new location (GDD: shifts every wave)
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager != null)
        {
            gridManager.ShiftCheckpoint();
        }

        // Return to 2D Edit Mode for pre-wave planning
        if (GameStageManager.Instance != null)
        {
            GameStageManager.Instance.SetStage(GameStageManager.Stage.Edit2D);
        }
    }

    public void RestartCampaign()
    {
        currentDungeonCoreHP = maxDungeonCoreHP;
        currentWave = 1;
        currentState = WaveState.PreWavePlanning;

        if (EconomyManager.Instance != null) EconomyManager.Instance.ResetEconomy();

        // Clear active enemies
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy e in enemies) if (e != null) Destroy(e.gameObject);

        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager != null) gridManager.ShiftCheckpoint();

        if (GameStageManager.Instance != null) GameStageManager.Instance.SetStage(GameStageManager.Stage.Edit2D);

        OnCoreHealthChanged?.Invoke(currentDungeonCoreHP, maxDungeonCoreHP);
        OnWaveChanged?.Invoke(currentWave, 0);
        OnWaveStateChanged?.Invoke(currentState);
    }
}
