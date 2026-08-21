using UnityEngine;
using System.Collections;

/// <summary>
/// Controls a single 3D Tile Block with smooth animations:
/// Snappy arrow rotation with ease-out bounce, lift-and-glide tile swapping, and click recoil.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class TileBlock : MonoBehaviour
{
    public enum Direction { Up, Right, Down, Left }
    public Direction currentDirection;

    [HideInInspector] public int gridX;
    [HideInInspector] public int gridY;

    [Header("Tile Restrictions & Roles")]
    public bool isImmutable = false;      // Fixed obstacle block; cannot be rotated or swapped
    public bool isCheckpoint = false;     // Mandatory waypoint required for path validity
    public bool isStartTile = false;
    public bool isEndTile = false;

    [Header("Visual References")]
    public Transform directionalArrow;    // Top-face chevron arrow
    public GameObject hoverIndicator;     // Rotating hover circle icon

    private GridManager gridManager;
    private BoxCollider boxCollider;
    private Coroutine rotateRoutine;
    private Coroutine swapRoutine;

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
        SnapDirection(currentDirection);
    }

    public void SetHover(bool isHovered)
    {
        if (GameStageManager.Instance != null && (GameStageManager.Instance.currentStage != GameStageManager.Stage.Edit2D || GameStageManager.Instance.isTransitioning))
        {
            if (hoverIndicator != null && hoverIndicator.activeSelf) hoverIndicator.SetActive(false);
            return;
        }

        if (isImmutable)
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
        SnapDirection(newDir);
    }

    public void SnapDirection(Direction newDir)
    {
        currentDirection = newDir;
        float angle = (int)newDir * 90f;
        if (directionalArrow != null)
        {
            directionalArrow.localRotation = Quaternion.Euler(0f, angle, 0f);
        }
    }

    public void SetDirectionAnimated(Direction newDir)
    {
        currentDirection = newDir;
        float targetAngle = (int)newDir * 90f;

        if (rotateRoutine != null) StopCoroutine(rotateRoutine);
        if (gameObject.activeInHierarchy)
        {
            rotateRoutine = StartCoroutine(SmoothRotateArrowRoutine(targetAngle));
        }
        else
        {
            SnapDirection(newDir);
        }
    }

    private IEnumerator SmoothRotateArrowRoutine(float targetY)
    {
        if (directionalArrow == null) yield break;

        Quaternion startRot = directionalArrow.localRotation;
        Quaternion targetRot = Quaternion.Euler(0f, targetY, 0f);

        float duration = 0.16f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Snappy ease-out cubic
            float easeT = 1f - Mathf.Pow(1f - t, 3);
            directionalArrow.localRotation = Quaternion.Slerp(startRot, targetRot, easeT);
            yield return null;
        }

        directionalArrow.localRotation = targetRot;
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
            hoverIndicator.transform.Rotate(0, 0, -160f * Time.deltaTime);
        }
    }

    public void RotateTile()
    {
        if (GameStageManager.Instance != null && (GameStageManager.Instance.currentStage != GameStageManager.Stage.Edit2D || GameStageManager.Instance.isTransitioning))
        {
            return;
        }

        if (WaveManager.Instance != null && WaveManager.Instance.currentState != WaveManager.WaveState.PreWavePlanning)
        {
            return;
        }

        if (isImmutable || isStartTile || isEndTile || isCheckpoint)
        {
            return;
        }

        StartCoroutine(ClickBounceRoutine());

        // Cycle directions: Up -> Right -> Down -> Left
        if (currentDirection == Direction.Left)
            currentDirection = Direction.Up;
        else
            currentDirection++;

        SetDirectionAnimated(currentDirection);

        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager != null) gridManager.TracePath();
    }

    public void AnimateSwapTo(Vector3 targetWorldPos)
    {
        if (swapRoutine != null) StopCoroutine(swapRoutine);
        swapRoutine = StartCoroutine(SwapGlideRoutine(targetWorldPos));
    }

    private IEnumerator SwapGlideRoutine(Vector3 targetPos)
    {
        Vector3 startPos = transform.position;
        float duration = 0.24f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float ease = Mathf.SmoothStep(0f, 1f, t);

            // Parabolic arc (lift up and glide)
            float arcHeight = Mathf.Sin(t * Mathf.PI) * 0.45f;
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, ease);
            currentPos.y += arcHeight;

            transform.position = currentPos;
            yield return null;
        }

        transform.position = targetPos;
        StartCoroutine(LandingThumpRoutine());
    }

    private IEnumerator ClickBounceRoutine()
    {
        Vector3 originalScale = new Vector3(1f, 0.35f, 1f);
        transform.localScale = new Vector3(0.92f, 0.28f, 0.92f); // Slight squash
        yield return new WaitForSeconds(0.06f);
        transform.localScale = new Vector3(1.04f, 0.38f, 1.04f); // Stretch
        yield return new WaitForSeconds(0.06f);
        transform.localScale = originalScale;
    }

    private IEnumerator LandingThumpRoutine()
    {
        Vector3 originalScale = new Vector3(1f, 0.35f, 1f);
        transform.localScale = new Vector3(1.06f, 0.30f, 1.06f); // Landing squash
        yield return new WaitForSeconds(0.06f);
        transform.localScale = originalScale;
    }
}
