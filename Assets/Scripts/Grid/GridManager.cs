using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Master Grid and Path Engine in native 3D space with customizable tile spacing, procedural chevron arrows, and glowing Start/Goal beacons.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Grid Dimensions (13x9 Widescreen)")]
    public GameObject tilePrefab;
    public int width = 13;
    public int height = 9;
    public float tileSpacing = 1.08f;

    [Header("Level Balance")]
    [Range(0f, 1f)]
    public float hazardChance = 0.18f;

    [Header("Master Hazard Balancer")]
    public TileProperty.HazardData spikeStats = new TileProperty.HazardData { damage = 10f, speedMult = 1f, dotDamage = 0f, duration = 0f };
    public TileProperty.HazardData pitfallStats = new TileProperty.HazardData { damage = 999f, speedMult = 0f, dotDamage = 0f, duration = 0f };
    public TileProperty.HazardData slowStats = new TileProperty.HazardData { damage = 0f, speedMult = 0.5f, dotDamage = 0f, duration = 0f };
    public TileProperty.HazardData freezeStats = new TileProperty.HazardData { damage = 0f, speedMult = 0f, dotDamage = 0f, duration = 1.5f };
    public TileProperty.HazardData burnStats = new TileProperty.HazardData { damage = 5f, speedMult = 1f, dotDamage = 4f, duration = 3f };
    public TileProperty.HazardData poisonStats = new TileProperty.HazardData { damage = 0f, speedMult = 1f, dotDamage = 1f, duration = -1f };
    public TileProperty.HazardData staticStats = new TileProperty.HazardData { damage = 2f, speedMult = 0.8f, dotDamage = 2f, duration = 2f };
    public TileProperty.HazardData bleedStats = new TileProperty.HazardData { damage = 0f, speedMult = 1f, dotDamage = 3f, duration = 5f };
    public TileProperty.HazardData curseStats = new TileProperty.HazardData { damage = 0f, speedMult = 1f, dotDamage = 0f, duration = 10f };

    public TileBlock[,] allTiles;
    public Vector2Int startCoords = new Vector2Int(0, 0);
    public Vector2Int endCoords = new Vector2Int(12, 8);

    public List<Vector3> currentPathWorldPositions = new List<Vector3>();
    public bool isPathValid { get; private set; } = false;

    private static Mesh cachedChevronMesh;

    void Start()
    {
        allTiles = new TileBlock[width, height];

        int randomStartY = Random.Range(0, height);
        int randomEndY = Random.Range(0, height);

        startCoords = new Vector2Int(0, randomStartY);
        endCoords = new Vector2Int(width - 1, randomEndY);

        GenerateGrid();
        TracePath();

        if (GameStageManager.Instance == null)
        {
            GameObject stageObj = new GameObject("GameStageManager");
            stageObj.AddComponent<GameStageManager>();
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
                    // Clean procedural 3D block (matching Blender render)
                    newTile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    newTile.name = $"Tile_{x}_{y}";
                    newTile.transform.position = spawnPos;
                    newTile.transform.localScale = new Vector3(1.0f, 0.35f, 1.0f);

                    // Add clean default clay material
                    Renderer r = newTile.GetComponent<Renderer>();
                    if (r != null)
                    {
                        Material blockMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        if (blockMat.shader == null) blockMat = new Material(Shader.Find("Standard"));
                        blockMat.color = new Color(0.82f, 0.84f, 0.88f);
                        r.material = blockMat;
                    }

                    // Add top-face Chevron Arrow indicator
                    GameObject arrowObj = new GameObject("Arrow");
                    arrowObj.transform.SetParent(newTile.transform, false);
                    arrowObj.transform.localPosition = new Vector3(0f, 0.52f, 0f); // Top face
                    arrowObj.transform.localScale = Vector3.one * 1.5f;

                    MeshFilter mf = arrowObj.AddComponent<MeshFilter>();
                    mf.sharedMesh = GetOrCreateChevronMesh();

                    MeshRenderer arrowR = arrowObj.AddComponent<MeshRenderer>();
                    Material arrowMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    if (arrowMat.shader == null) arrowMat = new Material(Shader.Find("Unlit/Color"));
                    arrowMat.color = new Color(0.15f, 0.18f, 0.25f, 1f);
                    arrowR.material = arrowMat;

                    newTile.AddComponent<TileBlock>();
                    newTile.AddComponent<TileProperty>();
                    newTile.AddComponent<TileTrigger>();
                }

                TileBlock tileScript = newTile.GetComponent<TileBlock>();
                if (tileScript == null) tileScript = newTile.AddComponent<TileBlock>();

                tileScript.gridX = x;
                tileScript.gridY = y;
                allTiles[x, y] = tileScript;

                TileBlock.Direction finalDir;
                if (x == startCoords.x && y == startCoords.y)
                {
                    finalDir = GetValidStartDirection(x, y);
                    CreateStartBeacon(newTile.transform);
                }
                else if (x == endCoords.x && y == endCoords.y)
                {
                    finalDir = (TileBlock.Direction)Random.Range(0, 4);
                    CreateGoalBeacon(newTile.transform);
                }
                else
                {
                    finalDir = (TileBlock.Direction)Random.Range(0, 4);
                }

                tileScript.SetDirection(finalDir);

                // Random hazard generation (excluding start & goal)
                if (Random.value < hazardChance && !IsStartOrEnd(x, y))
                {
                    int randomHazard = Random.Range(1, System.Enum.GetValues(typeof(TileProperty.TileType)).Length);
                    TileProperty tp = newTile.GetComponent<TileProperty>();
                    if (tp != null)
                    {
                        tp.SetType((TileProperty.TileType)randomHazard);

                        switch (tp.type)
                        {
                            case TileProperty.TileType.Spike: tp.currentData = spikeStats; break;
                            case TileProperty.TileType.Slow: tp.currentData = slowStats; break;
                            case TileProperty.TileType.Burn: tp.currentData = burnStats; break;
                            case TileProperty.TileType.Freeze: tp.currentData = freezeStats; break;
                            case TileProperty.TileType.Pitfall: tp.currentData = pitfallStats; break;
                            case TileProperty.TileType.Poison: tp.currentData = poisonStats; break;
                            case TileProperty.TileType.Static: tp.currentData = staticStats; break;
                            case TileProperty.TileType.Bleed: tp.currentData = bleedStats; break;
                            case TileProperty.TileType.Curse: tp.currentData = curseStats; break;
                        }
                    }
                }
            }
        }
    }

    private void CreateStartBeacon(Transform parent)
    {
        // Glowing Green Start Marker
        GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beacon.name = "StartBeacon";
        Destroy(beacon.GetComponent<Collider>());
        beacon.transform.SetParent(parent, false);
        beacon.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        beacon.transform.localScale = new Vector3(0.5f, 0.15f, 0.5f);

        Renderer r = beacon.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (mat.shader == null) mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(0.1f, 0.95f, 0.35f, 1f); // Vibrant Emerald Green
            r.material = mat;
        }

        // Add slow rotation animation
        beacon.AddComponent<ObjectRotator>();
    }

    private void CreateGoalBeacon(Transform parent)
    {
        // Glowing Red Goal Marker (Diamond / Cube)
        GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beacon.name = "GoalBeacon";
        Destroy(beacon.GetComponent<Collider>());
        beacon.transform.SetParent(parent, false);
        beacon.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        beacon.transform.localRotation = Quaternion.Euler(45f, 45f, 45f);
        beacon.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

        Renderer r = beacon.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (mat.shader == null) mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(0.95f, 0.15f, 0.2f, 1f); // Vibrant Crimson Red
            r.material = mat;
        }

        beacon.AddComponent<ObjectRotator>();
    }

    public static Mesh GetOrCreateChevronMesh()
    {
        if (cachedChevronMesh != null) return cachedChevronMesh;

        Mesh mesh = new Mesh();
        mesh.name = "ProceduralChevron";

        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0f, 0f, 0.28f),        // 0: Arrow Tip (+Z Forward)
            new Vector3(-0.22f, 0f, -0.22f),    // 1: Left Wing
            new Vector3(0f, 0f, -0.06f),       // 2: Center Notch
            new Vector3(0.22f, 0f, -0.22f)     // 3: Right Wing
        };

        int[] triangles = new int[]
        {
            0, 2, 1, // Left half
            0, 3, 2  // Right half
        };

        Vector3[] normals = new Vector3[]
        {
            Vector3.up, Vector3.up, Vector3.up, Vector3.up
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.RecalculateBounds();

        cachedChevronMesh = mesh;
        return cachedChevronMesh;
    }

    public void TracePath()
    {
        List<TileBlock> pathList = new List<TileBlock>();
        currentPathWorldPositions.Clear();
        Vector2Int currentPos = startCoords;
        bool goalReached = false;

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

            Vector3 tileCenter = currentTile.transform.position;
            tileCenter.y = 0.22f;
            currentPathWorldPositions.Add(tileCenter);

            if (currentPos == endCoords)
            {
                goalReached = true;
                break;
            }

            currentPos = GetNextCoords(currentPos, currentTile.currentDirection);
        }

        isPathValid = goalReached;
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

                    // 1. Start block ALWAYS stays Emerald Green
                    if (x == startCoords.x && y == startCoords.y)
                    {
                        tp.SetTileColor(new Color(0.15f, 0.85f, 0.35f));
                    }
                    // 2. Goal block ALWAYS stays Crimson Red
                    else if (x == endCoords.x && y == endCoords.y)
                    {
                        tp.SetTileColor(new Color(0.95f, 0.2f, 0.25f));
                    }
                    // 3. Blocks on the active path get a bright golden highlight
                    else if (path != null && path.Contains(tile))
                    {
                        tp.SetTileColor(new Color(1f, 0.92f, 0.45f));
                    }
                    // 4. Inactive blocks return to slate gray
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

    bool IsStartOrEnd(int x, int y)
    {
        return (x == startCoords.x && y == startCoords.y) || (x == endCoords.x && y == endCoords.y);
    }

    public void SwapTiles(TileBlock scriptA, TileBlock scriptB)
    {
        if (scriptA == null || scriptB == null) return;
        if (IsStartOrEnd(scriptA.gridX, scriptA.gridY) || IsStartOrEnd(scriptB.gridX, scriptB.gridY)) return;

        TileProperty propA = scriptA.GetComponent<TileProperty>();
        TileProperty propB = scriptB.GetComponent<TileProperty>();

        TileProperty.TileType typeA = propA.type;
        TileProperty.HazardData dataA = propA.currentData;
        TileBlock.Direction dirA = scriptA.currentDirection;

        propA.SetType(propB.type);
        propA.currentData = propB.currentData;
        scriptA.SetDirection(scriptB.currentDirection);

        propB.SetType(typeA);
        propB.currentData = dataA;
        scriptB.SetDirection(dirA);

        TracePath();
    }
}

/// <summary>
/// Simple helper to slowly rotate beacons / portals in 3D.
/// </summary>
public class ObjectRotator : MonoBehaviour
{
    public float rotationSpeed = 45f;

    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }
}
