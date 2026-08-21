using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Moves enemies along the 3D ground path with standing elevation, sprite direction flipping,
/// procedural chibi walking hop & squash-stretch animations, direct tile hazard triggering,
/// and End Tile (Dungeon Core) impact notification.
/// </summary>
public class EnemyPathFinding : MonoBehaviour
{
    [HideInInspector] public float speed = 1f;
    public float heightOffset = 0f; // Sits flush on 3D block top face
    private List<Vector3> localPathPoints = new List<Vector3>();
    private List<TileBlock> localPathTiles = new List<TileBlock>();
    private int targetIndex = 0;
    private int lastTriggeredTileIndex = -1;

    private SpriteRenderer spriteRenderer;
    private Enemy enemyScript;
    private Vector3 initialSpriteScale = Vector3.one * 1.5f;
    private float walkCycleTimer = 0f;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) initialSpriteScale = spriteRenderer.transform.localScale;
        enemyScript = GetComponent<Enemy>();
    }

    public void SetPath(List<Vector3> masterPath, Enemy.EnemyClass enemyClass)
    {
        if (masterPath == null || masterPath.Count == 0) return;

        localPathPoints = new List<Vector3>(masterPath);
        targetIndex = 0;
        lastTriggeredTileIndex = -1;

        Vector3 startPos = localPathPoints[0];
        startPos.y += heightOffset;
        transform.position = startPos;

        GridManager gm = FindFirstObjectByType<GridManager>();
        if (gm != null && gm.currentPathTiles != null)
        {
            localPathTiles = new List<TileBlock>(gm.currentPathTiles);
        }
    }

    public void SetPathWithTiles(List<TileBlock> pathTiles, Enemy.EnemyClass enemyClass)
    {
        if (pathTiles == null || pathTiles.Count == 0) return;

        localPathTiles = new List<TileBlock>(pathTiles);
        localPathPoints = new List<Vector3>();
        for (int i = 0; i < pathTiles.Count; i++)
        {
            Vector3 pos = pathTiles[i].transform.position;
            pos.y = 0.22f;
            localPathPoints.Add(pos);
        }

        targetIndex = 0;
        lastTriggeredTileIndex = -1;

        Vector3 startPos = localPathPoints[0];
        startPos.y += heightOffset;
        transform.position = startPos;
    }

    void Update()
    {
        if (localPathPoints == null || localPathPoints.Count == 0 || targetIndex >= localPathPoints.Count) return;

        Vector3 targetPoint = localPathPoints[targetIndex];
        targetPoint.y += heightOffset;

        // Visual direction flipping based on movement direction
        if (spriteRenderer != null)
        {
            if (targetPoint.x < transform.position.x - 0.02f)
            {
                spriteRenderer.flipX = true;
            }
            else if (targetPoint.x > transform.position.x + 0.02f)
            {
                spriteRenderer.flipX = false;
            }
        }

        // Procedural Chibi Walking Hop & Squash/Stretch Animation (Arknights Feel)
        if (spriteRenderer != null && speed > 0.05f)
        {
            walkCycleTimer += Time.deltaTime * speed * 9.5f;
            float hop = Mathf.Abs(Mathf.Sin(walkCycleTimer)) * 0.065f;
            float stretch = 1f + (Mathf.Sin(walkCycleTimer * 2f) * 0.045f);

            spriteRenderer.transform.localPosition = new Vector3(0f, 0.05f + hop, 0f);
            spriteRenderer.transform.localScale = new Vector3(initialSpriteScale.x / stretch, initialSpriteScale.y * stretch, initialSpriteScale.z);
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPoint, speed * Time.deltaTime);

        // Step hazard trigger check
        if (targetIndex != lastTriggeredTileIndex && localPathTiles != null && targetIndex < localPathTiles.Count)
        {
            float distToTile = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                                                new Vector3(targetPoint.x, 0, targetPoint.z));
            if (distToTile < 0.45f)
            {
                lastTriggeredTileIndex = targetIndex;
                TriggerHazardOnTile(localPathTiles[targetIndex]);
            }
        }

        if (Vector3.Distance(transform.position, targetPoint) < 0.15f)
        {
            targetIndex++;
        }

        if (targetIndex >= localPathPoints.Count)
        {
            // Human adventurer has reached the Dungeon Core (End Tile)!
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.OnEnemyReachedCore(enemyScript != null ? enemyScript : GetComponent<Enemy>());
            }
            Destroy(gameObject);
        }
    }

    private void TriggerHazardOnTile(TileBlock tile)
    {
        if (tile == null) return;
        TileProperty prop = tile.GetComponent<TileProperty>();
        if (prop != null && prop.type != TileProperty.TileType.Normal)
        {
            if (enemyScript == null) enemyScript = GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.ApplyTileHazard(prop.type, prop.currentData);
            }
        }
    }
}
