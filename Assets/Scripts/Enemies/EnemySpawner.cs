using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns 2.5D Arknights-style billboard enemies across all 6 Master Stat Table classes.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefab Overrides (Optional)")]
    public GameObject[] enemyPrefabs;

    [Header("Custom 2D Sprite Overrides (Optional)")]
    public Sprite swordsmanSprite;
    public Sprite tankerSprite;
    public Sprite rogueSprite;
    public Sprite priestSprite;
    public Sprite supporterSprite;
    public Sprite paladinSprite;

    public float spawnInterval = 2.8f;
    public bool isSpawningActive = false;

    private float timer;
    private GridManager gridManager;

    private static Sprite cachedSwordsmanSprite;
    private static Sprite cachedTankerSprite;
    private static Sprite cachedRogueSprite;
    private static Sprite cachedPriestSprite;
    private static Sprite cachedSupporterSprite;
    private static Sprite cachedPaladinSprite;

    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        isSpawningActive = false;
        timer = 0f;
    }

    void Update()
    {
        // Strictly lock spawning to the 2.5D Action Stage
        if (GameStageManager.Instance != null && GameStageManager.Instance.currentStage != GameStageManager.Stage.Action25D)
        {
            isSpawningActive = false;
            return;
        }

        if (!isSpawningActive) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnEnemy();
            timer = 0;
        }
    }

    public void StartSpawning()
    {
        isSpawningActive = true;
        timer = spawnInterval * 0.85f;
    }

    public void StopAndClearEnemies()
    {
        isSpawningActive = false;
        timer = 0f;

        Enemy[] activeEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null && enemy.gameObject != null)
            {
                Destroy(enemy.gameObject);
            }
        }
    }

    public void SpawnEnemy()
    {
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null || gridManager.currentPathWorldPositions == null || gridManager.currentPathWorldPositions.Count == 0 || !gridManager.isPathValid)
        {
            return;
        }

        Vector3 spawnPos = gridManager.currentPathWorldPositions[0];
        GameObject enemy = null;
        Enemy.EnemyClass randomClass = (Enemy.EnemyClass)Random.Range(0, 6);

        if (enemyPrefabs != null && enemyPrefabs.Length > (int)randomClass && enemyPrefabs[(int)randomClass] != null)
        {
            enemy = Instantiate(enemyPrefabs[(int)randomClass], spawnPos, Quaternion.identity);
        }
        else
        {
            // Create 2D Billboard Character Sprite in 3D (Arknights 2.5D Chibi)
            enemy = new GameObject($"Enemy_{randomClass}");
            enemy.transform.position = spawnPos;

            // 1. Upright 2D Billboard Sprite
            GameObject spriteObj = new GameObject("Sprite");
            spriteObj.transform.SetParent(enemy.transform, false);
            spriteObj.transform.localPosition = new Vector3(0f, 0.05f, 0f); // Pivot at feet touches block top face
            spriteObj.transform.localScale = Vector3.one * 1.5f;

            SpriteRenderer sr = spriteObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;

            // Select or generate character sprite
            Sprite charSprite = GetSpriteForClass(randomClass);
            if (charSprite != null)
            {
                sr.sprite = charSprite;
            }

            // Attach Billboard component so sprite always faces camera in 2.5D
            spriteObj.AddComponent<Billboard>();

            // 2. Drop Shadow Oval underneath feet on the 3D block top face
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
        }

        // Ensure 3D trigger & kinematic Rigidbody for tile hazard collisions
        Collider col = enemy.GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sc = enemy.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.4f;
            sc.center = new Vector3(0f, 0.35f, 0f);
        }

        Rigidbody rb = enemy.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = enemy.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Add core Enemy stats script
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript == null) enemyScript = enemy.AddComponent<Enemy>();
        enemyScript.InitializeEnemy(randomClass);

        // Add 3D waypoint navigation
        EnemyPathFinding pathScript = enemy.GetComponent<EnemyPathFinding>();
        if (pathScript == null) pathScript = enemy.AddComponent<EnemyPathFinding>();

        pathScript.SetPath(gridManager.currentPathWorldPositions, randomClass);
        pathScript.speed = enemyScript.baseSpeed;
    }

    private Sprite GetSpriteForClass(Enemy.EnemyClass enemyClass)
    {
        switch (enemyClass)
        {
            case Enemy.EnemyClass.Swordsman:
                if (swordsmanSprite != null) return swordsmanSprite;
                if (cachedSwordsmanSprite == null) cachedSwordsmanSprite = GenerateChibiSprite(Enemy.EnemyClass.Swordsman);
                return cachedSwordsmanSprite;

            case Enemy.EnemyClass.Tanker:
                if (tankerSprite != null) return tankerSprite;
                if (cachedTankerSprite == null) cachedTankerSprite = GenerateChibiSprite(Enemy.EnemyClass.Tanker);
                return cachedTankerSprite;

            case Enemy.EnemyClass.Rogue:
                if (rogueSprite != null) return rogueSprite;
                if (cachedRogueSprite == null) cachedRogueSprite = GenerateChibiSprite(Enemy.EnemyClass.Rogue);
                return cachedRogueSprite;

            case Enemy.EnemyClass.Priest:
                if (priestSprite != null) return priestSprite;
                if (cachedPriestSprite == null) cachedPriestSprite = GenerateChibiSprite(Enemy.EnemyClass.Priest);
                return cachedPriestSprite;

            case Enemy.EnemyClass.Supporter:
                if (supporterSprite != null) return supporterSprite;
                if (cachedSupporterSprite == null) cachedSupporterSprite = GenerateChibiSprite(Enemy.EnemyClass.Supporter);
                return cachedSupporterSprite;

            case Enemy.EnemyClass.Paladin:
                if (paladinSprite != null) return paladinSprite;
                if (cachedPaladinSprite == null) cachedPaladinSprite = GenerateChibiSprite(Enemy.EnemyClass.Paladin);
                return cachedPaladinSprite;
        }
        return null;
    }

    /// <summary>
    /// Generates crisp stylized pixel-art chibi character sprites in memory for all 6 classes.
    /// </summary>
    public static Sprite GenerateChibiSprite(Enemy.EnemyClass enemyClass)
    {
        int w = 24;
        int h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(0, 0, 0, 0);
        Color[] pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        Color outline = new Color(0.1f, 0.12f, 0.16f, 1f);
        Color skin = new Color(1f, 0.85f, 0.72f, 1f);
        Color eye = new Color(0.1f, 0.1f, 0.15f, 1f);
        Color mainColor;
        Color accentColor;

        if (enemyClass == Enemy.EnemyClass.Swordsman)
        {
            mainColor = new Color(0.2f, 0.45f, 0.95f, 1f);   // Blue Knight Armor
            accentColor = new Color(0.85f, 0.9f, 0.98f, 1f);  // Steel Blade
        }
        else if (enemyClass == Enemy.EnemyClass.Tanker)
        {
            mainColor = new Color(0.28f, 0.32f, 0.38f, 1f);  // Heavy Dark Iron
            accentColor = new Color(0.95f, 0.75f, 0.2f, 1f);  // Golden Shield Emblem
        }
        else if (enemyClass == Enemy.EnemyClass.Rogue)
        {
            mainColor = new Color(0.85f, 0.18f, 0.22f, 1f);  // Crimson Hooded Cloak
            accentColor = new Color(0.98f, 0.88f, 0.3f, 1f);  // Gold Dagger
        }
        else if (enemyClass == Enemy.EnemyClass.Priest)
        {
            mainColor = new Color(0.92f, 0.94f, 0.98f, 1f);  // White Holy Robe
            accentColor = new Color(0.95f, 0.80f, 0.25f, 1f); // Golden Staff & Halo
        }
        else if (enemyClass == Enemy.EnemyClass.Supporter)
        {
            mainColor = new Color(0.65f, 0.20f, 0.75f, 1f);  // Magenta/Purple Bard Outfit
            accentColor = new Color(0.98f, 0.82f, 0.35f, 1f); // Golden Lute/Harp
        }
        else // Paladin
        {
            mainColor = new Color(0.95f, 0.82f, 0.28f, 1f);  // Golden Champion Plate
            accentColor = new Color(0.95f, 0.95f, 1f, 1f);    // Silver Radiant Warhammer
        }

        // Head / Helmet / Hair (Y: 16 to 28)
        DrawBox(pixels, w, h, 6, 17, 12, 11, outline);
        DrawBox(pixels, w, h, 7, 18, 10, 9, skin);
        DrawBox(pixels, w, h, 7, 23, 10, 5, mainColor); // Hair / Hat / Helmet

        // Priest Golden Halo
        if (enemyClass == Enemy.EnemyClass.Priest)
        {
            DrawBox(pixels, w, h, 8, 29, 8, 2, accentColor);
        }

        // Eyes
        SetPixel(pixels, w, h, 9, 20, eye);
        SetPixel(pixels, w, h, 14, 20, eye);

        // Body / Armor / Robe (Y: 7 to 17)
        DrawBox(pixels, w, h, 5, 8, 14, 9, outline);
        DrawBox(pixels, w, h, 6, 9, 12, 7, mainColor);

        // Weapons & Accessories
        if (enemyClass == Enemy.EnemyClass.Swordsman)
        {
            DrawBox(pixels, w, h, 18, 10, 3, 14, accentColor);
            SetPixel(pixels, w, h, 19, 9, outline);
        }
        else if (enemyClass == Enemy.EnemyClass.Tanker)
        {
            DrawBox(pixels, w, h, 2, 6, 6, 16, mainColor);
            DrawBox(pixels, w, h, 3, 10, 4, 8, accentColor);
        }
        else if (enemyClass == Enemy.EnemyClass.Rogue)
        {
            SetPixel(pixels, w, h, 4, 9, accentColor);
            SetPixel(pixels, w, h, 19, 9, accentColor);
        }
        else if (enemyClass == Enemy.EnemyClass.Priest)
        {
            // Holy Cross Staff
            DrawBox(pixels, w, h, 19, 6, 2, 18, accentColor);
            DrawBox(pixels, w, h, 17, 20, 6, 2, accentColor);
        }
        else if (enemyClass == Enemy.EnemyClass.Supporter)
        {
            // Golden Lute
            DrawBox(pixels, w, h, 17, 9, 5, 8, accentColor);
            DrawBox(pixels, w, h, 19, 17, 2, 6, mainColor);
        }
        else if (enemyClass == Enemy.EnemyClass.Paladin)
        {
            // Holy War Shield (Left) & Warhammer (Right)
            DrawBox(pixels, w, h, 2, 7, 5, 14, mainColor);
            DrawBox(pixels, w, h, 18, 8, 2, 16, outline);
            DrawBox(pixels, w, h, 16, 20, 6, 4, accentColor);
        }

        // Legs / Boots (Y: 1 to 7)
        DrawBox(pixels, w, h, 7, 1, 4, 7, outline);
        DrawBox(pixels, w, h, 13, 1, 4, 7, outline);

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.05f), 24f);
    }

    private static void DrawBox(Color[] pixels, int w, int h, int x, int y, int bw, int bh, Color col)
    {
        for (int px = x; px < x + bw && px < w; px++)
        {
            for (int py = y; py < y + bh && py < h; py++)
            {
                if (px >= 0 && py >= 0)
                {
                    pixels[py * w + px] = col;
                }
            }
        }
    }

    private static void SetPixel(Color[] pixels, int w, int h, int x, int y, Color col)
    {
        if (x >= 0 && x < w && y >= 0 && y < h)
        {
            pixels[y * w + x] = col;
        }
    }
}
