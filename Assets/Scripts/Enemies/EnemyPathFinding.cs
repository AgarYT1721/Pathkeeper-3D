using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Moves enemies along the 3D ground path with standing elevation and sprite direction flipping.
/// </summary>
public class EnemyPathFinding : MonoBehaviour
{
    [HideInInspector] public float speed = 1f;
    public float heightOffset = 0f; // Sits flush on 3D block top face
    private List<Vector3> localPathPoints = new List<Vector3>();
    private int targetIndex = 0;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void SetPath(List<Vector3> masterPath, Enemy.EnemyClass enemyClass)
    {
        if (masterPath == null || masterPath.Count == 0) return;

        localPathPoints = new List<Vector3>(masterPath);
        targetIndex = 0;

        Vector3 startPos = localPathPoints[0];
        startPos.y += heightOffset;
        transform.position = startPos;

        switch (enemyClass)
        {
            case Enemy.EnemyClass.Swordsman: speed = 1.0f; break;
            case Enemy.EnemyClass.Tanker: speed = 0.6f; break;
            case Enemy.EnemyClass.Rogue: speed = 1.8f; break;
        }
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

        transform.position = Vector3.MoveTowards(transform.position, targetPoint, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPoint) < 0.15f)
        {
            targetIndex++;
        }

        if (targetIndex >= localPathPoints.Count)
        {
            Destroy(gameObject);
        }
    }
}
