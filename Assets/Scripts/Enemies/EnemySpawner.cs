using UnityEngine;

/// <summary>
/// Spawns 2D billboard sprite character units (Arknights Chibi Style) with distinct character sprites, drop shadows, and walking animations.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Custom Sprites (Optional overrides)")]
    public Sprite swordsmanSprite;
    public Sprite tankerSprite;
    public Sprite rogueSprite;

    [Header("Enemy Prefabs (Optional overrides)")]
    public GameObject[] enemyPrefabs;

    public float spawnInterval = 2.8f;
    public bool isSpawningActive = false;

    private float timer;
    private GridManager gridManager;

    private static Sprite cachedSwordsmanSprite;
    private static Sprite cachedTankerSprite;
    private static Sprite cachedRogueSprite;

    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();

        if (GameStageManager.Instance == null)
        {
            isSpawningActive = true;
        }
    }

    void Update()
    {
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
        Enemy.EnemyClass randomClass = (Enemy.EnemyClass)Random.Range(0, 3);

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
        else
        {
            col.isTrigger = true;
        }

        if (enemy.GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = enemy.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript == null) enemyScript = enemy.AddComponent<Enemy>();

        EnemyPathFinding movementScript = enemy.GetComponent<EnemyPathFinding>();
        if (movementScript == null) movementScript = enemy.AddComponent<EnemyPathFinding>();

        enemyScript.InitializeEnemy(randomClass);
        movementScript.SetPath(gridManager.currentPathWorldPositions, randomClass);
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
        }
        return null;
    }

    /// <summary>
    /// Generates crisp stylized pixel-art chibi character sprites in memory (Arknights style).
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
        else // Rogue
        {
            mainColor = new Color(0.85f, 0.18f, 0.22f, 1f);  // Crimson Hooded Cloak
            accentColor = new Color(0.98f, 0.88f, 0.3f, 1f);  // Gold Dagger
        }

        // Draw Chibi Character:
        // Head / Helmet (Y: 16 to 28)
        DrawBox(pixels, w, h, 6, 17, 12, 11, outline);
        DrawBox(pixels, w, h, 7, 18, 10, 9, skin);
        DrawBox(pixels, w, h, 7, 23, 10, 5, mainColor); // Hair / Helmet / Hood

        // Eyes
        SetPixel(pixels, w, h, 9, 20, eye);
        SetPixel(pixels, w, h, 14, 20, eye);

        // Body / Armor / Cape (Y: 7 to 17)
        DrawBox(pixels, w, h, 5, 8, 14, 9, outline);
        DrawBox(pixels, w, h, 6, 9, 12, 7, mainColor);

        // Weapon / Shield
        if (enemyClass == Enemy.EnemyClass.Swordsman)
        {
            // Sword blade on right
            DrawBox(pixels, w, h, 18, 10, 3, 14, accentColor);
            SetPixel(pixels, w, h, 19, 9, outline);
        }
        else if (enemyClass == Enemy.EnemyClass.Tanker)
        {
            // Heavy Tower Shield on left
            DrawBox(pixels, w, h, 2, 6, 6, 16, mainColor);
            DrawBox(pixels, w, h, 3, 10, 4, 8, accentColor);
        }
        else
        {
            // Dual Daggers
            SetPixel(pixels, w, h, 4, 9, accentColor);
            SetPixel(pixels, w, h, 19, 9, accentColor);
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
