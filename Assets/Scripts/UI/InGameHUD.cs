using UnityEngine;

/// <summary>
/// Tactical Arknights-Grade In-Game HUD for the Tower Master:
/// • Top Command Bar: DP, Core HP, Wave Tracker with dots, Path status.
/// • Bottom Command Tray: Trap buttons with hotkeys [1-9], [Space] deploy.
/// • Fast-Forward Speed Toggle: [1X] [2X] [3X] battle speed (Hotkey: F).
/// • Tactical Unit Inspector: Hover over marching adventurers to view Live HP, Armor %, and Abilities.
/// • Trap Tile Inspector: Hover over placed traps to view Tier, Damage, and Upgrade stats.
/// • Victory & Defeat Modals with After-Action Report (AAR) ratings.
/// </summary>
public class InGameHUD : MonoBehaviour
{
    private GridManager gridManager;

    // Speed multiplier state
    public float currentSpeedMult = 1.0f;
    private int speedIndex = 0;
    private readonly float[] speedOptions = new float[] { 1.0f, 2.0f, 3.0f };

    private GUIStyle headerStyle;
    private GUIStyle dpStyle;
    private GUIStyle coreHpStyle;
    private GUIStyle waveStyle;
    private GUIStyle statusStyle;
    private GUIStyle buttonStyle;
    private GUIStyle activeTrapBtnStyle;
    private GUIStyle inactiveTrapBtnStyle;
    private GUIStyle speedBtnStyle;
    private GUIStyle tipsStyle;
    private GUIStyle modalStyle;
    private GUIStyle modalTitleStyle;
    private GUIStyle inspectorCardStyle;
    private GUIStyle inspectorHeaderStyle;
    private GUIStyle inspectorBodyStyle;

    private Texture2D panelTex;
    private Texture2D btnTexNormal;
    private Texture2D btnTexHover;
    private Texture2D btnTexActive;
    private Texture2D modalTex;
    private Texture2D cardTex;

    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        if (TrapShopManager.Instance == null)
        {
            GameObject shopObj = new GameObject("TrapShopManager");
            shopObj.AddComponent<TrapShopManager>();
        }
        SetBattleSpeed(1.0f);
    }

    void Update()
    {
        // Speed Toggle Hotkey [F]
        if (InputHelper.GetFKeyDown())
        {
            CycleBattleSpeed();
        }

        // Trap Placement Hotkeys [1 - 9, 0, -] in Edit Stage
        if (GameStageManager.Instance != null && GameStageManager.Instance.currentStage == GameStageManager.Stage.Edit2D)
        {
            if (TrapShopManager.Instance != null)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) TrapShopManager.Instance.activePlacingTrap = TileProperty.TileType.Normal;
                else if (Input.GetKeyDown(KeyCode.Alpha2)) TrapShopManager.Instance.activePlacingTrap = TileProperty.TileType.Upgrade;
                else if (Input.GetKeyDown(KeyCode.Alpha3)) TrapShopManager.Instance.activePlacingTrap = TileProperty.TileType.Spike;
                else if (Input.GetKeyDown(KeyCode.Alpha4)) TrapShopManager.Instance.activePlacingTrap = TileProperty.TileType.Burn;
                else if (Input.GetKeyDown(KeyCode.Alpha5)) TrapShopManager.Instance.activePlacingTrap = TileProperty.TileType.Poison;
                else if (Input.GetKeyDown(KeyCode.Alpha6)) TrapShopManager.Instance.activePlacingTrap = TileProperty.TileType.Freeze;
                else if (Input.GetKeyDown(KeyCode.Alpha7)) TrapShopManager.Instance.activePlacingTrap = TileProperty.TileType.Slow;
                else if (Input.GetKeyDown(KeyCode.Alpha8)) TrapShopManager.Instance.activePlacingTrap = TileProperty.TileType.Bleed;
                else if (Input.GetKeyDown(KeyCode.Alpha9)) TrapShopManager.Instance.activePlacingTrap = TileProperty.TileType.Curse;
                else if (Input.GetKeyDown(KeyCode.Alpha0)) TrapShopManager.Instance.activePlacingTrap = TileProperty.TileType.Static;
                else if (Input.GetKeyDown(KeyCode.Minus)) TrapShopManager.Instance.activePlacingTrap = TileProperty.TileType.Pitfall;
            }
        }
        else
        {
            // In Play Mode, ensure timescale is applied
            Time.timeScale = currentSpeedMult;
        }
    }

    public void CycleBattleSpeed()
    {
        speedIndex = (speedIndex + 1) % speedOptions.Length;
        SetBattleSpeed(speedOptions[speedIndex]);
    }

    public void SetBattleSpeed(float speed)
    {
        currentSpeedMult = speed;
        Time.timeScale = currentSpeedMult;
    }

    void InitStyles()
    {
        if (headerStyle != null) return;

        panelTex = MakeTex(2, 2, new Color(0.08f, 0.10f, 0.15f, 0.94f));
        modalTex = MakeTex(2, 2, new Color(0.06f, 0.08f, 0.12f, 0.97f));
        cardTex = MakeTex(2, 2, new Color(0.09f, 0.12f, 0.18f, 0.95f));
        btnTexNormal = MakeTex(2, 2, new Color(0.15f, 0.48f, 0.88f, 0.95f));
        btnTexHover = MakeTex(2, 2, new Color(0.22f, 0.60f, 0.98f, 1f));
        btnTexActive = MakeTex(2, 2, new Color(0.95f, 0.72f, 0.15f, 1f));

        headerStyle = new GUIStyle(GUI.skin.box);
        headerStyle.normal.background = panelTex;
        headerStyle.normal.textColor = Color.white;
        headerStyle.fontSize = 12;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;

        dpStyle = new GUIStyle(GUI.skin.box);
        dpStyle.normal.background = panelTex;
        dpStyle.normal.textColor = new Color(0.78f, 0.48f, 1f); // Mystic Violet DP
        dpStyle.fontSize = 13;
        dpStyle.fontStyle = FontStyle.Bold;
        dpStyle.alignment = TextAnchor.MiddleCenter;

        coreHpStyle = new GUIStyle(GUI.skin.box);
        coreHpStyle.normal.background = panelTex;
        coreHpStyle.normal.textColor = new Color(1f, 0.32f, 0.38f); // Crimson Core HP
        coreHpStyle.fontSize = 13;
        coreHpStyle.fontStyle = FontStyle.Bold;
        coreHpStyle.alignment = TextAnchor.MiddleCenter;

        waveStyle = new GUIStyle(GUI.skin.box);
        waveStyle.normal.background = panelTex;
        waveStyle.normal.textColor = new Color(1f, 0.88f, 0.35f); // Golden Wave
        waveStyle.fontSize = 12;
        waveStyle.fontStyle = FontStyle.Bold;
        waveStyle.alignment = TextAnchor.MiddleCenter;

        statusStyle = new GUIStyle(GUI.skin.box);
        statusStyle.normal.background = panelTex;
        statusStyle.fontSize = 12;
        statusStyle.fontStyle = FontStyle.Bold;
        statusStyle.alignment = TextAnchor.MiddleCenter;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.normal.background = btnTexNormal;
        buttonStyle.hover.background = btnTexHover;
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.hover.textColor = Color.white;
        buttonStyle.fontSize = 13;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.alignment = TextAnchor.MiddleCenter;

        speedBtnStyle = new GUIStyle(GUI.skin.button);
        speedBtnStyle.normal.background = panelTex;
        speedBtnStyle.hover.background = btnTexHover;
        speedBtnStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
        speedBtnStyle.fontSize = 12;
        speedBtnStyle.fontStyle = FontStyle.Bold;
        speedBtnStyle.alignment = TextAnchor.MiddleCenter;

        inactiveTrapBtnStyle = new GUIStyle(GUI.skin.button);
        inactiveTrapBtnStyle.normal.background = panelTex;
        inactiveTrapBtnStyle.hover.background = btnTexHover;
        inactiveTrapBtnStyle.normal.textColor = new Color(0.85f, 0.88f, 0.95f);
        inactiveTrapBtnStyle.hover.textColor = Color.white;
        inactiveTrapBtnStyle.fontSize = 10;
        inactiveTrapBtnStyle.fontStyle = FontStyle.Bold;
        inactiveTrapBtnStyle.alignment = TextAnchor.MiddleCenter;

        activeTrapBtnStyle = new GUIStyle(GUI.skin.button);
        activeTrapBtnStyle.normal.background = btnTexActive;
        activeTrapBtnStyle.hover.background = btnTexActive;
        activeTrapBtnStyle.normal.textColor = new Color(0.1f, 0.1f, 0.15f);
        activeTrapBtnStyle.hover.textColor = Color.black;
        activeTrapBtnStyle.fontSize = 10;
        activeTrapBtnStyle.fontStyle = FontStyle.Bold;
        activeTrapBtnStyle.alignment = TextAnchor.MiddleCenter;

        tipsStyle = new GUIStyle(GUI.skin.label);
        tipsStyle.normal.textColor = new Color(0.80f, 0.85f, 0.92f, 0.92f);
        tipsStyle.fontSize = 11;
        tipsStyle.fontStyle = FontStyle.Bold;
        tipsStyle.alignment = TextAnchor.MiddleLeft;

        modalStyle = new GUIStyle(GUI.skin.box);
        modalStyle.normal.background = modalTex;
        modalStyle.alignment = TextAnchor.MiddleCenter;

        modalTitleStyle = new GUIStyle(GUI.skin.label);
        modalTitleStyle.fontSize = 24;
        modalTitleStyle.fontStyle = FontStyle.Bold;
        modalTitleStyle.alignment = TextAnchor.MiddleCenter;

        inspectorCardStyle = new GUIStyle(GUI.skin.box);
        inspectorCardStyle.normal.background = cardTex;
        inspectorCardStyle.alignment = TextAnchor.UpperLeft;

        inspectorHeaderStyle = new GUIStyle(GUI.skin.label);
        inspectorHeaderStyle.fontSize = 13;
        inspectorHeaderStyle.fontStyle = FontStyle.Bold;
        inspectorHeaderStyle.normal.textColor = new Color(1f, 0.88f, 0.35f);

        inspectorBodyStyle = new GUIStyle(GUI.skin.label);
        inspectorBodyStyle.fontSize = 11;
        inspectorBodyStyle.normal.textColor = new Color(0.85f, 0.88f, 0.95f);
    }

    private void OnGUI()
    {
        InitStyles();

        int padding = 12;
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();

        int currentDP = EconomyManager.Instance != null ? EconomyManager.Instance.GetCurrentDP() : 100;
        int coreHP = WaveManager.Instance != null ? WaveManager.Instance.currentDungeonCoreHP : 10;
        int maxCoreHP = WaveManager.Instance != null ? WaveManager.Instance.maxDungeonCoreHP : 10;
        int currentWave = WaveManager.Instance != null ? WaveManager.Instance.currentWave : 1;
        int totalWaves = WaveManager.Instance != null ? WaveManager.Instance.totalWaves : 5;
        int enemiesRemaining = WaveManager.Instance != null ? WaveManager.Instance.enemiesRemainingInWave : 0;
        WaveManager.WaveState waveState = WaveManager.Instance != null ? WaveManager.Instance.currentState : WaveManager.WaveState.PreWavePlanning;

        GameStageManager.Stage stage = GameStageManager.Instance != null ? GameStageManager.Instance.currentStage : GameStageManager.Stage.Edit2D;
        bool isTransitioning = GameStageManager.Instance != null && GameStageManager.Instance.isTransitioning;

        // 1. TOP-LEFT: Stage Mode Badge
        string modeText = stage == GameStageManager.Stage.Edit2D
            ? "TOWER MASTER: 2D EDIT"
            : "DUNGEON INVASION: 2.5D ACTION";
        GUI.Box(new Rect(padding, padding, 220, 34), modeText, headerStyle);

        // 2. TOP-RIGHT 1: Battle Speed Toggle [1X / 2X / 3X]
        string speedLabel = $"▶▶ {currentSpeedMult:0.#}X [F]";
        if (GUI.Button(new Rect(Screen.width - 450 - padding, padding, 95, 34), speedLabel, speedBtnStyle))
        {
            CycleBattleSpeed();
        }

        // 3. TOP-RIGHT 2: Dungeon Core HP
        GUI.Box(new Rect(Screen.width - 340 - padding, padding, 160, 34), $"❤️ CORE: {coreHP}/{maxCoreHP}", coreHpStyle);

        // 4. TOP-RIGHT 3: Dungeon Points (DP)
        GUI.Box(new Rect(Screen.width - 165 - padding, padding, 165, 34), $"🔮 DP: {currentDP}", dpStyle);

        // 5. TOP-CENTER 1: Wave Incursion Tracker
        string waveText = stage == GameStageManager.Stage.Edit2D
            ? $"🚩 WAVE {currentWave}/{totalWaves} (PLANNING)"
            : $"🚩 WAVE {currentWave}/{totalWaves} (INVADERS: {enemiesRemaining})";
        GUI.Box(new Rect(Screen.width / 2 - 250, padding, 210, 34), waveText, waveStyle);

        // 6. TOP-CENTER 2: Path Validation & Checkpoint Status
        bool pathValid = gridManager != null && gridManager.isPathValid;
        string pathMsg = gridManager != null ? gridManager.pathValidationMessage : "● PATH READY";

        statusStyle.normal.textColor = pathValid ? new Color(0.25f, 0.95f, 0.45f) : new Color(0.98f, 0.35f, 0.35f);
        GUI.Box(new Rect(Screen.width / 2 - 30, padding, 280, 34), pathMsg, statusStyle);

        // 7. BOTTOM: Trap Toolbar & Control Tips
        if (stage == GameStageManager.Stage.Edit2D)
        {
            RenderTrapPlacementToolbar();

            string tips = "[1-9] Hotkeys   [L-Click] Morph/Rotate   [R-Click] Swap (10 DP)   [Space] Deploy   |   📍 Checkpoint";
            GUI.Label(new Rect(padding, Screen.height - 24, 900, 20), tips, tipsStyle);
        }
        else
        {
            string tips = "[Space / R] Return to 2D Edit Stage   |   [F] Fast-Forward Speed";
            GUI.Label(new Rect(padding, Screen.height - 24, 500, 20), tips, tipsStyle);
        }

        // 8. BOTTOM-RIGHT: Action Deploy Button
        int btnW = 210;
        int btnH = 42;
        float btnX = Screen.width - btnW - padding;
        float btnY = Screen.height - btnH - padding;

        if (stage == GameStageManager.Stage.Edit2D)
        {
            string btnText = pathValid ? "▶ START WAVE (SPACE)" : "⚠ INVALID PATH";
            GUI.enabled = pathValid && !isTransitioning;
            if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), btnText, buttonStyle))
            {
                if (GameStageManager.Instance != null) GameStageManager.Instance.SetStage(GameStageManager.Stage.Action25D);
            }
            GUI.enabled = true;
        }
        else
        {
            if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), "↺ EDIT GRID (SPACE/R)", buttonStyle) && !isTransitioning)
            {
                if (GameStageManager.Instance != null) GameStageManager.Instance.SetStage(GameStageManager.Stage.Edit2D);
            }
        }

        // 9. TACTICAL INSPECTORS (Unit Hover & Trap Hover)
        if (stage == GameStageManager.Stage.Action25D)
        {
            RenderUnitInspector();
        }
        else if (stage == GameStageManager.Stage.Edit2D)
        {
            RenderTileInspector();
        }

        // 10. MODALS: Victory / Defeat Overlays
        if (waveState == WaveManager.WaveState.GameOverDefeat)
        {
            RenderDefeatModal();
        }
        else if (waveState == WaveManager.WaveState.CampaignComplete)
        {
            RenderCampaignVictoryModal();
        }
        else if (waveState == WaveManager.WaveState.WaveVictory)
        {
            RenderWaveClearedBanner();
        }
    }

    private void RenderTrapPlacementToolbar()
    {
        if (TrapShopManager.Instance == null) return;

        TileProperty.TileType active = TrapShopManager.Instance.activePlacingTrap;

        (string label, TileProperty.TileType type)[] traps = new[]
        {
            ("1.🔄 ROTATE", TileProperty.TileType.Normal),
            ("2.⭐ UPGRADE\n25-40 DP", TileProperty.TileType.Upgrade),
            ("3.🗡️ SPIKE\n20 DP", TileProperty.TileType.Spike),
            ("4.🔥 BURN\n25 DP", TileProperty.TileType.Burn),
            ("5.☠️ POISON\n25 DP", TileProperty.TileType.Poison),
            ("6.❄️ FREEZE\n30 DP", TileProperty.TileType.Freeze),
            ("7.🕸️ SLOW\n20 DP", TileProperty.TileType.Slow),
            ("8.🩸 BLEED\n25 DP", TileProperty.TileType.Bleed),
            ("9.🔮 CURSE\n35 DP", TileProperty.TileType.Curse),
            ("0.⚡ STATIC\n20 DP", TileProperty.TileType.Static),
            ("-.🕳️ PITFALL\n50 DP", TileProperty.TileType.Pitfall)
        };

        int barW = traps.Length * 76;
        float startX = (Screen.width - barW) / 2f - 85f;
        float startY = Screen.height - 75f;

        for (int i = 0; i < traps.Length; i++)
        {
            bool isSelected = (active == traps[i].type);
            GUIStyle s = isSelected ? activeTrapBtnStyle : inactiveTrapBtnStyle;

            if (GUI.Button(new Rect(startX + (i * 77), startY, 75, 44), traps[i].label, s))
            {
                TrapShopManager.Instance.activePlacingTrap = traps[i].type;
            }
        }
    }

    private void RenderUnitInspector()
    {
        if (Camera.main == null) return;
        Ray ray = Camera.main.ScreenPointToRay(InputHelper.GetMousePosition());
        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
        {
            Enemy enemy = hit.collider.GetComponent<Enemy>();
            if (enemy == null) enemy = hit.collider.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                float cw = 280;
                float ch = 110;
                float cx = 12;
                float cy = 54;

                GUI.Box(new Rect(cx, cy, cw, ch), "", inspectorCardStyle);

                string title = $"⚔️ {enemy.currentClass.ToString().ToUpper()}";
                if (enemy.currentClass == Enemy.EnemyClass.Paladin) title = "👑 PALADIN CHAMPION (BOSS)";
                GUI.Label(new Rect(cx + 10, cy + 8, cw - 20, 22), title, inspectorHeaderStyle);

                string stats = $"HP: {Mathf.RoundToInt(enemy.currentHP)} / {Mathf.RoundToInt(enemy.maxHP)}   |   🛡️ ARMOR: {Mathf.RoundToInt(enemy.armorPercent * 100)}%";
                GUI.Label(new Rect(cx + 10, cy + 30, cw - 20, 20), stats, inspectorBodyStyle);

                string trait = "";
                switch (enemy.currentClass)
                {
                    case Enemy.EnemyClass.Swordsman: trait = "Standard human vanguard adventurer."; break;
                    case Enemy.EnemyClass.Tanker: trait = "Heavy iron plating absorbs 50% physical dmg."; break;
                    case Enemy.EnemyClass.Rogue: trait = "High speed runner (1.8x). Vulnerable to Bleed."; break;
                    case Enemy.EnemyClass.Priest: trait = "Cleansing Aura: Purges debuffs & heals ally."; break;
                    case Enemy.EnemyClass.Supporter: trait = "Inspirational Buff: Casts Group Haste/Armor/Heal."; break;
                    case Enemy.EnemyClass.Paladin: trait = "Lay on Hands: Full 350 HP heal + Cleanse at <50% HP."; break;
                }
                GUI.Label(new Rect(cx + 10, cy + 52, cw - 20, 48), trait, inspectorBodyStyle);
            }
        }
    }

    private void RenderTileInspector()
    {
        if (Camera.main == null) return;
        Ray ray = Camera.main.ScreenPointToRay(InputHelper.GetMousePosition());
        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
        {
            TileProperty prop = hit.collider.GetComponent<TileProperty>();
            if (prop == null) prop = hit.collider.GetComponentInParent<TileProperty>();

            if (prop != null && prop.type != TileProperty.TileType.Normal)
            {
                float cw = 290;
                float ch = 90;
                float cx = Screen.width - cw - 12;
                float cy = Screen.height - ch - 80;

                GUI.Box(new Rect(cx, cy, cw, ch), "", inspectorCardStyle);

                string stars = prop.upgradeLevel == 1 ? "TIER 1" : (prop.upgradeLevel == 2 ? "TIER 2 ★" : "TIER 3 ★★ (MAX)");
                GUI.Label(new Rect(cx + 10, cy + 8, cw - 20, 22), $"{prop.type.ToString().ToUpper()} TRAP ({stars})", inspectorHeaderStyle);

                string stats = $"Damage: {prop.currentData.damage:0}   DoT: {prop.currentData.dotDamage:0}/s   Slow: {(1f - prop.currentData.speedMult) * 100:0}%";
                GUI.Label(new Rect(cx + 10, cy + 30, cw - 20, 20), stats, inspectorBodyStyle);

                string upg = prop.CanUpgrade() ? $"Upgrade Cost: {prop.GetUpgradeCost()} DP ([⭐ UPGRADE] tool)" : "Max Upgrade Tier Reached";
                GUI.Label(new Rect(cx + 10, cy + 52, cw - 20, 20), upg, inspectorBodyStyle);
            }
        }
    }

    private void RenderWaveClearedBanner()
    {
        float mw = 440;
        float mh = 110;
        float mx = (Screen.width - mw) / 2f;
        float my = (Screen.height - mh) / 2f - 40f;

        GUI.Box(new Rect(mx, my, mw, mh), "", modalStyle);

        modalTitleStyle.normal.textColor = new Color(0.3f, 0.95f, 0.5f);
        GUI.Label(new Rect(mx, my + 15, mw, 40), "✨ WAVE CLEARED! ✨", modalTitleStyle);

        GUIStyle sub = new GUIStyle(GUI.skin.label);
        sub.alignment = TextAnchor.MiddleCenter;
        sub.fontSize = 14;
        sub.normal.textColor = new Color(1f, 0.88f, 0.35f);
        GUI.Label(new Rect(mx, my + 60, mw, 30), "+50 DP Reward! Relocating Checkpoint 📍...", sub);
    }

    private void RenderCampaignVictoryModal()
    {
        float mw = 520;
        float mh = 230;
        float mx = (Screen.width - mw) / 2f;
        float my = (Screen.height - mh) / 2f;

        GUI.Box(new Rect(mx, my, mw, mh), "", modalStyle);

        modalTitleStyle.normal.textColor = new Color(1f, 0.88f, 0.25f);
        GUI.Label(new Rect(mx, my + 20, mw, 40), "👑 DUNGEON DEFENDED! 👑", modalTitleStyle);

        GUIStyle sub = new GUIStyle(GUI.skin.label);
        sub.alignment = TextAnchor.MiddleCenter;
        sub.fontSize = 14;
        sub.normal.textColor = Color.white;
        GUI.Label(new Rect(mx, my + 70, mw, 25), "Rating: ⭐⭐⭐ SSS RANK (PERFECT DEFENSE)", sub);
        GUI.Label(new Rect(mx, my + 95, mw, 25), "All 5 Adventuring Parties Annihilated!", sub);
        GUI.Label(new Rect(mx, my + 120, mw, 25), "The Tower Master reigns supreme!", sub);

        if (GUI.Button(new Rect(mx + (mw - 200) / 2f, my + 160, 200, 42), "▶ PLAY AGAIN", buttonStyle))
        {
            if (WaveManager.Instance != null) WaveManager.Instance.RestartCampaign();
        }
    }

    private void RenderDefeatModal()
    {
        float mw = 520;
        float mh = 220;
        float mx = (Screen.width - mw) / 2f;
        float my = (Screen.height - mh) / 2f;

        GUI.Box(new Rect(mx, my, mw, mh), "", modalStyle);

        modalTitleStyle.normal.textColor = new Color(1f, 0.25f, 0.25f);
        GUI.Label(new Rect(mx, my + 20, mw, 40), "💀 DUNGEON BREACHED! 💀", modalTitleStyle);

        GUIStyle sub = new GUIStyle(GUI.skin.label);
        sub.alignment = TextAnchor.MiddleCenter;
        sub.fontSize = 14;
        sub.normal.textColor = Color.white;
        GUI.Label(new Rect(mx, my + 75, mw, 25), "The Adventurers Destroyed the Dungeon Core!", sub);

        if (GUI.Button(new Rect(mx + (mw - 200) / 2f, my + 140, 200, 42), "↺ RETRY DUNGEON", buttonStyle))
        {
            if (WaveManager.Instance != null) WaveManager.Instance.RestartCampaign();
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
