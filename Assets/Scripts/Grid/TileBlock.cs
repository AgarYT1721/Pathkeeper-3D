using UnityEngine;
using System.Collections;

/// <summary>
/// Controls a single 3D Tile Block: its direction, 3D top-face arrow, hover animations, and click-rotation.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class TileBlock : MonoBehaviour
{
    public enum Direction { Up, Right, Down, Left }
    public Direction currentDirection;

    [HideInInspector] public int gridX;
    [HideInInspector] public int gridY;

    [Header("Visual References")]
    public Transform directionalArrow;    // Top-face chevron arrow
    public GameObject hoverIndicator;     // Rotating hover circle icon

    private GridManager gridManager;
    private BoxCollider boxCollider;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
        }
        boxCollider.size = new Vector3(1f, 0.4f, 1f);
        boxCollider.center = new Vector3(0f, 0.2f, 0f);

        if (directionalArrow == null)
        {
            Transform t = transform.Find("Arrow");
            if (t == null) t = transform.Find("Triangle");
            directionalArrow = t;
        }

        if (hoverIndicator == null)
        {
            Transform h = transform.Find("HoverIndicator");
            if (h == null) h = transform.Find("HoverSymbol");
            if (h != null) hoverIndicator = h.gameObject;
        }

        if (hoverIndicator != null)
        {
            hoverIndicator.SetActive(false);
        }
    }

    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        SetDirection(currentDirection);
    }

    public void SetHover(bool isHovered)
    {
        if (GameStageManager.Instance != null && (GameStageManager.Instance.currentStage != GameStageManager.Stage.Edit2D || GameStageManager.Instance.isTransitioning))
        {
            if (hoverIndicator != null && hoverIndicator.activeSelf) hoverIndicator.SetActive(false);
            return;
        }

        if (hoverIndicator != null)
        {
            hoverIndicator.SetActive(isHovered);
        }
    }

    public void SetDirection(Direction newDir)
    {
        currentDirection = newDir;
        float angle = (int)newDir * 90f; // 0=North (+Z), 90=East (+X), 180=South (-Z), 270=West (-X)

        if (directionalArrow != null)
        {
            directionalArrow.localRotation = Quaternion.Euler(0f, angle, 0f);
        }
    }

    void Update()
    {
        if (GameStageManager.Instance != null && GameStageManager.Instance.currentStage != GameStageManager.Stage.Edit2D)
        {
            if (hoverIndicator != null && hoverIndicator.activeSelf)
            {
                hoverIndicator.SetActive(false);
            }
            return;
        }

        if (hoverIndicator != null && hoverIndicator.activeSelf)
        {
            hoverIndicator.transform.Rotate(0, 0, -150f * Time.deltaTime);
        }
    }

    public void RotateTile()
    {
        if (GameStageManager.Instance != null && (GameStageManager.Instance.currentStage != GameStageManager.Stage.Edit2D || GameStageManager.Instance.isTransitioning))
        {
            return;
        }

        StartCoroutine(ClickBumpRoutine());

        // Cycle directions: Up -> Right -> Down -> Left
        if (currentDirection == Direction.Left)
            currentDirection = Direction.Up;
        else
            currentDirection++;

        SetDirection(currentDirection);

        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager != null) gridManager.TracePath();
    }

    IEnumerator ClickBumpRoutine()
    {
        Vector3 originalScale = transform.localScale;
        transform.localScale = originalScale * 0.92f;
        yield return new WaitForSeconds(0.05f);
        transform.localScale = originalScale;
    }
}
