using UnityEngine;

/// <summary>
/// Handles 3D Mouse Interaction: Left-click tile rotation, Right-click tile swapping, and dynamic hover indicators.
/// </summary>
public class SelectionManager : MonoBehaviour
{
    private TileBlock firstSelected;
    private TileBlock currentHoveredTile;
    public Color highlightColor = Color.cyan;
    private GridManager gridManager;

    void Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
    }

    void Update()
    {
        // Only allow tile interaction during the 2D Edit stage
        if (GameStageManager.Instance != null && (GameStageManager.Instance.currentStage != GameStageManager.Stage.Edit2D || GameStageManager.Instance.isTransitioning))
        {
            if (firstSelected != null) DeselectCurrent();
            if (currentHoveredTile != null)
            {
                currentHoveredTile.SetHover(false);
                currentHoveredTile = null;
            }
            return;
        }

        TileBlock tileUnderCursor = GetTileUnderMouse();

        // Hover Indicator
        if (tileUnderCursor != currentHoveredTile)
        {
            if (currentHoveredTile != null) currentHoveredTile.SetHover(false);
            currentHoveredTile = tileUnderCursor;
            if (currentHoveredTile != null) currentHoveredTile.SetHover(true);
        }

        // Left Click to Rotate Tile
        if (InputHelper.GetLeftMouseDown())
        {
            if (tileUnderCursor != null)
            {
                tileUnderCursor.RotateTile();
            }
        }

        // Right Click to Select & Swap Tiles
        if (InputHelper.GetRightMouseDown())
        {
            if (tileUnderCursor != null)
            {
                HandleSwapSelection(tileUnderCursor);
            }
            else
            {
                DeselectCurrent();
            }
        }

        // Escape to cancel selection
        if (InputHelper.GetEscapeKeyDown())
        {
            DeselectCurrent();
        }
    }

    private TileBlock GetTileUnderMouse()
    {
        if (Camera.main == null) return null;

        Vector2 mousePos = InputHelper.GetMousePosition();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, ~0, QueryTriggerInteraction.Collide))
        {
            TileBlock tile = hit.collider.GetComponent<TileBlock>();
            if (tile == null) tile = hit.collider.GetComponentInParent<TileBlock>();
            return tile;
        }

        return null;
    }

    void DeselectCurrent()
    {
        if (firstSelected != null)
        {
            TileProperty prop = firstSelected.GetComponent<TileProperty>();
            if (prop != null) prop.SetTileColor(Color.white);
            firstSelected = null;
        }
    }

    void HandleSwapSelection(TileBlock clickedTile)
    {
        if (clickedTile == null) return;

        if (firstSelected == null)
        {
            firstSelected = clickedTile;
            TileProperty prop = firstSelected.GetComponent<TileProperty>();
            if (prop != null) prop.SetTileColor(highlightColor);
        }
        else
        {
            if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
            if (gridManager != null)
            {
                TileProperty propA = firstSelected.GetComponent<TileProperty>();
                if (propA != null) propA.SetTileColor(Color.white);

                gridManager.SwapTiles(firstSelected, clickedTile);
            }
            firstSelected = null;
        }
    }
}
