using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Master Grid, Tile Cap, Shifting Checkpoint, and Path Engine in 3D space.
/// Implements GDD Tile Caps, Immutable Obstacles, and mandatory Checkpoint Waypoints.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Grid Dimensions (13x9 Widescreen)")]
    public GameObject tilePrefab;
    public int width = 13;
    public int height = 9;
    public float tileSpacing = 1.08f;

    [Header("Tile Cap Rules (GDD Specification)")]
    public int minPathLength = 6;
    public int maxPathLength = 50;

    [Header("Level Balance")]
    [Range(0f, 1f)]
    public float hazardChance = 0.20f;
    public int immutableObstacleCount = 5;

    public TileBlock[,] allTiles;
    public Vector2Int startCoords = new Vector2Int(0, 4);
    public Vector2Int endCoords = new Vector2Int(12, 4);
    public Vector2Int checkpointCoords;

    public List<Vector3> currentPathWorldPositions = new List<Vector3>();
    public List<TileBlock> currentPathTiles = new List<TileBlock>();
    public bool isPathValid { get; private set; } = false;
    public string pathValidationMessage { get; private set; } = "⚠ CONNECTING PATH...";

    private static Mesh cachedChevronMesh;
    private GameObject checkpointBeacon;

    void Start()
    {
        allTiles = new TileBlock[width, height];

        int randomStartY = Random.Range(1, height - 1);
        int randomEndY = Random.Range(1, height - 1);

        startCoords = new Vector2Int(0, randomStartY);
        endCoords = new Vector2Int(width - 1, randomEndY);

        GenerateGrid();
        ShiftCheckpoint();
        TracePath();

        if (EconomyManager.Instance == null) { }
        if (TrapShopManager.Instance == null) { }

        if (GameStageManager.Instance == null)
        {
            GameObject stageObj = new GameObject("GameStageManager");
            stageObj.AddComponent<GameStageManager>();
        }

        if (WaveManager.Instance == null)
        {
            GameObject waveObj = new GameObject("WaveManager");
            waveObj.AddComponent<WaveManager>();
        }

        if (FindFirstObjectByType<InGameHUD>() == null)
        {
            GameObject hudObj = new GameObject("InGameHUD");
            hudObj.AddComponent<InGameHUD>();
        }
    }

    public void GenerateGrid()
    {
        float xOffset = (width - 1) / 2f;
        float zOffset = (height - 1) / 2f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 spawnPos = new Vector3((x - xOffset) * tileSpacing, 0f, (y - zOffset) * tileSpacing);
                GameObject newTile = null;

                if (tilePrefab != null)
                {
                    newTile = Instantiate(tilePrefab, spawnPos, Quaternion.identity);
                }
                else
                {
                    // Clean procedural 3D block
                    newTile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    newTile.name = $"Tile_{x}_{y}";
                    newTile.transform.position = spawnPos;
                    newTile.transform.localScale = new Vector3(1.0f, 0.35f, 1.0f);

                    Renderer r = newTile.GetComponent<Renderer>();
                    if (r != null)
                    {
                        Material blockMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        if (blockMat.shader == null) blockMat = new Material(Shader.Find("Standard"));
                        blockMat.color = new Color(0.82f, 0.84f, 0.88f);
                        r.material = blockMat;
                    }

                    // Top-face Chevron Arrow indicator
                    GameObject arrowObj = new GameObject("Arrow");
                    arrowObj.transform.SetParent(newTile.transform, false);
                    arrowObj.transform.localPosition = new Vector3(0f, 0.51f, 0f);

                    MeshFilter arrowFilter = arrowObj.AddComponent<MeshFilter>();
                    arrowFilter.sharedMesh = GetOrCreateChevronMesh();

                    MeshRenderer arrowRenderer = arrowObj.AddComponent<MeshRenderer>();
                    Material arrowMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    if (arrowMat.shader == null) arrowMat = new Material(Shader.Find("Unlit/Color"));
                    arrowMat.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
                    arrowRenderer.material = arrowMat;
                }

                TileBlock tileComp = newTile.GetComponent<TileBlock>();
                if (tileComp == null) tileComp = newTile.AddComponent<TileBlock>();

                TileProperty propComp = newTile.GetComponent<TileProperty>();
                if (propComp == null) propComp = newTile.AddComponent<TileProperty>();

                TileTrigger trigComp = newTile.GetComponent<TileTrigger>();
                if (trigComp == null) trigComp = newTile.AddComponent<TileTrigger>();

                tileComp.gridX = x;
                tileComp.gridY = y;
                allTiles[x, y] = tileComp;

                // Configure Start / End roles
                if (x == startCoords.x && y == startCoords.y)
                {
                    tileComp.isStartTile = true;
                    tileComp.SetDirection(GetValidStartDirection(x, y));
                    propComp.SetType(TileProperty.TileType.Normal);
                    CreateStartBeacon(newTile.transform);
                }
                else if (x == endCoords.x && y == endCoords.y)
                {
                    tileComp.isEndTile = true;
                    tileComp.SetDirection(TileBlock.Direction.Right);
                    propComp.SetType(TileProperty.TileType.Normal);
                    CreateGoalBeacon(newTile.transform);
                }
                else
                {
                    tileComp.SetDirection((TileBlock.Direction)Random.Range(0, 4));

                    if (Random.value < hazardChance)
                    {
                        TileProperty.TileType randomHazard = (TileProperty.TileType)Random.Range(1, 10);
                        propComp.SetType(randomHazard);
                    }
                    else
                    {
                        propComp.SetType(TileProperty.TileType.Normal);
                    }
                }
            }
        }

        // Generate Immutable Obstacle Blocks (GDD: fixed blocks that cannot be moved or rotated)
        GenerateImmutableObstacles();
    }

    private void GenerateImmutableObstacles()
    {
        int placed = 0;
        int attempts = 0;

        while (placed < immutableObstacleCount && attempts < 100)
        {
            attempts++;
            int rx = Random.Range(2, width - 2);
            int ry = Random.Range(0, height);

            TileBlock t = allTiles[rx, ry];
            if (t != null && !t.isStartTile && !t.isEndTile && !t.isImmutable)
            {
                t.isImmutable = true;
                TileProperty tp = t.GetComponent<TileProperty>();
                if (tp != null)
                {
                    tp.SetTileColor(new Color(0.22f, 0.24f, 0.28f)); // Dark iron bedrock
                }

                // Add immutable pillar decoration
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = "ImmutablePillar";
                Destroy(pillar.GetComponent<Collider>());
                pillar.transform.SetParent(t.transform, false);
                pillar.transform.localPosition = new Vector3(0f, 0.65f, 0f);
                pillar.transform.localScale = new Vector3(0.4f, 0.35f, 0.4f);

                Renderer r = pillar.GetComponent<Renderer>();
                if (r != null)
                {
                    Material pMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    if (pMat.shader == null) pMat = new Material(Shader.Find("Standard"));
                    pMat.color = new Color(0.35f, 0.38f, 0.44f);
                    r.material = pMat;
                }

                placed++;
            }
        }
    }

    public void ShiftCheckpoint()
    {
        // 1. Clear old checkpoint flag
        if (allTiles != null && checkpointCoords.x >= 0 && checkpointCoords.x < width && checkpointCoords.y >= 0 && checkpointCoords.y < height)
        {
            TileBlock old = allTiles[checkpointCoords.x, checkpointCoords.y];
            if (old != null) old.isCheckpoint = false;
        }

        // 2. Pick a new valid checkpoint coordinate
        int attempts = 0;
        while (attempts < 100)
        {
            attempts++;
            int cx = Random.Range(3, width - 3);
            int cy = Random.Range(0, height);

            TileBlock t = allTiles[cx, cy];
            if (t != null && !t.isStartTile && !t.isEndTile && !t.isImmutable)
            {
                checkpointCoords = new Vector2Int(cx, cy);
                t.isCheckpoint = true;
                break;
            }
        }

        // 3. Spawn / Move the Checkpoint Beacon
        if (checkpointBeacon != null) Destroy(checkpointBeacon);

        TileBlock cpTile = allTiles[checkpointCoords.x, checkpointCoords.y];
        if (cpTile != null)
        {
            checkpointBeacon = new GameObject("CheckpointBeacon");
            checkpointBeacon.transform.SetParent(cpTile.transform, false);
            checkpointBeacon.transform.localPosition = new Vector3(0f, 0.65f, 0f);

            // Glowing Waypoint Rune Crystal
            GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crystal.name = "RuneCrystal";
            Destroy(crystal.GetComponent<Collider>());
            crystal.transform.SetParent(checkpointBeacon.transform, false);
            crystal.transform.localPosition = Vector3.zero;
            crystal.transform.localScale = new Vector3(0.45f, 0.15f, 0.45f);

            crystal.AddComponent<BeaconAnimator>();

            Renderer cr = crystal.GetComponent<Renderer>();
            if (cr != null)
            {
                Material cMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                if (cMat.shader == null) cMat = new Material(Shader.Find("Unlit/Color"));
                cMat.color = new Color(0.15f, 0.90f, 1f, 0.95f); // Cyan Waypoint Rune
                cr.material = cMat;
            }

            // Light pulse
            Light cpLight = checkpointBeacon.AddComponent<Light>();
            cpLight.type = LightType.Point;
            cpLight.color = new Color(0.2f, 0.85f, 1f);
            cpLight.range = 3.5f;
            cpLight.intensity = 1.8f;
        }

        TracePath();
    }

    public void TracePath()
    {
        List<TileBlock> pathList = new List<TileBlock>();
        currentPathWorldPositions.Clear();
        Vector2Int currentPos = startCoords;
        bool goalReached = false;
        bool checkpointVisited = false;

        for (int i = 0; i < (width * height); i++)
        {
            if (currentPos.x < 0 || currentPos.x >= width || currentPos.y < 0 || currentPos.y >= height)
            {
                break;
            }

            TileBlock currentTile = allTiles[currentPos.x, currentPos.y];
            if (currentTile == null || pathList.Contains(currentTile))
            {
                break;
            }

            pathList.Add(currentTile);

            Vector3 tileCenter = GetGridWorldPosition(currentPos.x, currentPos.y);
            currentPathWorldPositions.Add(tileCenter);

            if (currentPos == checkpointCoords)
            {
                checkpointVisited = true;
            }

            if (currentPos == endCoords)
            {
                goalReached = true;
                break;
            }

            currentPos = GetNextCoords(currentPos, currentTile.currentDirection);
        }

        currentPathTiles = new List<TileBlock>(pathList);

        // Validate GDD Constraints
        if (!goalReached)
        {
            isPathValid = false;
            pathValidationMessage = "⚠ NO PATH TO GOAL";
        }
        else if (!checkpointVisited)
        {
            isPathValid = false;
            pathValidationMessage = "⚠ MUST VISIT CHECKPOINT 📍";
        }
        else if (pathList.Count < minPathLength)
        {
            isPathValid = false;
            pathValidationMessage = $"⚠ PATH TOO SHORT (MIN: {minPathLength})";
        }
        else if (pathList.Count > maxPathLength)
        {
            isPathValid = false;
            pathValidationMessage = $"⚠ PATH TOO LONG (MAX: {maxPathLength})";
        }
        else
        {
            isPathValid = true;
            pathValidationMessage = $"● PATH READY ({pathList.Count} TILES)";
        }

        HighlightPath(pathList);
    }

    Vector2Int GetNextCoords(Vector2Int pos, TileBlock.Direction dir)
    {
        if (dir == TileBlock.Direction.Up) return new Vector2Int(pos.x, pos.y + 1);     // +Z North
        if (dir == TileBlock.Direction.Right) return new Vector2Int(pos.x + 1, pos.y);  // +X East
        if (dir == TileBlock.Direction.Down) return new Vector2Int(pos.x, pos.y - 1);   // -Z South
        if (dir == TileBlock.Direction.Left) return new Vector2Int(pos.x - 1, pos.y);   // -X West
        return pos;
    }

    void HighlightPath(List<TileBlock> path)
    {
        if (allTiles != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileBlock tile = allTiles[x, y];
                    if (tile == null) continue;
                    TileProperty tp = tile.GetComponent<TileProperty>();
                    if (tp == null) continue;

                    // 1. Start Portal: Emerald Green
                    if (x == startCoords.x && y == startCoords.y)
                    {
                        tp.SetTileColor(new Color(0.15f, 0.85f, 0.35f));
                    }
                    // 2. Goal Core: Crimson Red
                    else if (x == endCoords.x && y == endCoords.y)
                    {
                        tp.SetTileColor(new Color(0.95f, 0.2f, 0.25f));
                    }
                    // 3. Checkpoint Tile: Vibrant Cyan
                    else if (x == checkpointCoords.x && y == checkpointCoords.y)
                    {
                        tp.SetTileColor(new Color(0.15f, 0.90f, 1.0f));
                    }
                    // 4. Immutable Obstacle: Dark Charcoal Bedrock
                    else if (tile.isImmutable)
                    {
                        tp.SetTileColor(new Color(0.24f, 0.26f, 0.30f));
                    }
                    // 5. Active Path: Golden Yellow
                    else if (path != null && path.Contains(tile))
                    {
                        tp.SetTileColor(new Color(1f, 0.92f, 0.45f));
                    }
                    // 6. Inactive blocks: Slate Gray
                    else
                    {
                        tp.SetTileColor(new Color(0.82f, 0.84f, 0.88f));
                    }
                }
            }
        }

        PathVisualizer visualizer = FindFirstObjectByType<PathVisualizer>();
        if (visualizer == null)
        {
            GameObject pvObj = new GameObject("PathVisualizer");
            visualizer = pvObj.AddComponent<PathVisualizer>();
        }

        if (visualizer != null)
        {
            visualizer.UpdatePath(currentPathWorldPositions);
        }
    }

    TileBlock.Direction GetValidStartDirection(int x, int y)
    {
        List<TileBlock.Direction> validDirections = new List<TileBlock.Direction>
        {
            TileBlock.Direction.Up,
            TileBlock.Direction.Right,
            TileBlock.Direction.Down
        };

        if (y == 0) validDirections.Remove(TileBlock.Direction.Down);
        if (y == height - 1) validDirections.Remove(TileBlock.Direction.Up);

        return validDirections[Random.Range(0, validDirections.Count)];
    }

    public Vector3 GetGridWorldPosition(int x, int y)
    {
        float xOffset = (width - 1) / 2f;
        float zOffset = (height - 1) / 2f;
        return new Vector3((x - xOffset) * tileSpacing, 0.22f, (y - zOffset) * tileSpacing);
    }

    public void SwapTiles(TileBlock tileA, TileBlock tileB)
    {
        if (tileA == null || tileB == null || tileA == tileB) return;
        if (tileA.isImmutable || tileB.isImmutable) return;
        if (tileA.isStartTile || tileB.isStartTile || tileA.isEndTile || tileB.isEndTile) return;
        if (tileA.isCheckpoint || tileB.isCheckpoint) return;

        int ax = tileA.gridX; int ay = tileA.gridY;
        int bx = tileB.gridX; int by = tileB.gridY;

        Vector3 posA = GetGridWorldPosition(ax, ay);
        posA.y = 0f;
        Vector3 posB = GetGridWorldPosition(bx, by);
        posB.y = 0f;

        tileA.AnimateSwapTo(posB);
        tileB.AnimateSwapTo(posA);

        tileA.gridX = bx; tileA.gridY = by;
        tileB.gridX = ax; tileB.gridY = ay;

        allTiles[bx, by] = tileA;
        allTiles[ax, ay] = tileB;

        TracePath();
    }

    void CreateStartBeacon(Transform parent)
    {
        GameObject beacon = new GameObject("StartBeacon");
        beacon.transform.SetParent(parent, false);
        beacon.transform.localPosition = new Vector3(0f, 0.6f, 0f);

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "PortalRing";
        Destroy(ring.GetComponent<Collider>());
        ring.transform.SetParent(beacon.transform, false);
        ring.transform.localPosition = Vector3.zero;
        ring.transform.localScale = new Vector3(0.65f, 0.08f, 0.65f);
        ring.AddComponent<BeaconAnimator>();

        Renderer r = ring.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (mat.shader == null) mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(0.2f, 0.95f, 0.4f, 0.9f);
            r.material = mat;
        }

        Light lt = beacon.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.color = new Color(0.2f, 0.95f, 0.4f);
        lt.range = 3.5f;
        lt.intensity = 2.0f;
    }

    void CreateGoalBeacon(Transform parent)
    {
        GameObject beacon = new GameObject("GoalDiamond");
        beacon.transform.SetParent(parent, false);
        beacon.transform.localPosition = new Vector3(0f, 0.75f, 0f);

        GameObject diamond = GameObject.CreatePrimitive(PrimitiveType.Cube);
        diamond.name = "CoreCrystal";
        Destroy(diamond.GetComponent<Collider>());
        diamond.transform.SetParent(beacon.transform, false);
        diamond.transform.localPosition = Vector3.zero;
        diamond.transform.localRotation = Quaternion.Euler(45f, 45f, 45f);
        diamond.transform.localScale = Vector3.one * 0.45f;
        diamond.AddComponent<BeaconAnimator>();

        Renderer r = diamond.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (mat.shader == null) mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(1f, 0.15f, 0.25f, 0.95f);
            r.material = mat;
        }

        Light lt = beacon.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.color = new Color(1f, 0.15f, 0.25f);
        lt.range = 3.5f;
        lt.intensity = 2.0f;
    }

    private static Mesh GetOrCreateChevronMesh()
    {
        if (cachedChevronMesh != null) return cachedChevronMesh;

        Mesh mesh = new Mesh();
        mesh.name = "ChevronArrowMesh";

        Vector3[] verts = new Vector3[]
        {
            new Vector3( 0.00f, 0f,  0.35f), // Tip top
            new Vector3(-0.30f, 0f, -0.25f), // Left back
            new Vector3(-0.15f, 0f, -0.32f), // Left bottom notch
            new Vector3( 0.00f, 0f, -0.12f), // Center inner crook
            new Vector3( 0.15f, 0f, -0.32f), // Right bottom notch
            new Vector3( 0.30f, 0f, -0.25f), // Right back
        };

        int[] tris = new int[]
        {
            0, 5, 3,
            5, 4, 3,
            0, 3, 1,
            1, 3, 2
        };

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        cachedChevronMesh = mesh;
        return cachedChevronMesh;
    }
}
