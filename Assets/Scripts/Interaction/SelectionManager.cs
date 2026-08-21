using UnityEngine;

/// <summary>
/// Handles 3D Mouse Interaction: Left-click tile rotation, Right-click DP tile swapping,
/// and Immutable/PlayMode block protection.
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
        // Only allow tile interaction during the 2D Edit stage AND when wave is in pre-wave planning phase
        bool isPlayMode = (GameStageManager.Instance != null && GameStageManager.Instance.currentStage != GameStageManager.Stage.Edit2D)
                          || (GameStageManager.Instance != null && GameStageManager.Instance.isTransitioning)
                          || (WaveManager.Instance != null && WaveManager.Instance.currentState != WaveManager.WaveState.PreWavePlanning);

        if (isPlayMode)
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
            if (currentHoveredTile != null && !currentHoveredTile.isImmutable)
            {
                currentHoveredTile.SetHover(true);
            }
        }

        // Left Click to Rotate Tile OR Apply Selected Trap
        if (InputHelper.GetLeftMouseDown())
        {
            if (tileUnderCursor != null && !tileUnderCursor.isImmutable && !tileUnderCursor.isCheckpoint && !tileUnderCursor.isStartTile && !tileUnderCursor.isEndTile)
            {
                if (TrapShopManager.Instance != null && TrapShopManager.Instance.activePlacingTrap != TileProperty.TileType.Normal)
                {
                    TrapShopManager.Instance.TryMorphTile(tileUnderCursor, TrapShopManager.Instance.activePlacingTrap);
                }
                else
                {
                    tileUnderCursor.RotateTile();
                }
            }
        }

        // Right Click to Select & Swap Tiles OR Cancel Active Trap Tool
        if (InputHelper.GetRightMouseDown())
        {
            if (TrapShopManager.Instance != null && TrapShopManager.Instance.activePlacingTrap != TileProperty.TileType.Normal)
            {
                TrapShopManager.Instance.activePlacingTrap = TileProperty.TileType.Normal;
                return;
            }

            if (tileUnderCursor != null && !tileUnderCursor.isImmutable && !tileUnderCursor.isStartTile && !tileUnderCursor.isEndTile && !tileUnderCursor.isCheckpoint)
            {
                HandleSwapSelection(tileUnderCursor);
            }
            else
            {
                DeselectCurrent();
            }
        }

        // Escape to cancel selection or exit trap placement mode
        if (InputHelper.GetEscapeKeyDown())
        {
            if (TrapShopManager.Instance != null) TrapShopManager.Instance.activePlacingTrap = TileProperty.TileType.Normal;
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
            if (prop != null) prop.SetTileColor(prop.GetBaseTypeColor());
            firstSelected = null;
        }
    }

    void HandleSwapSelection(TileBlock clickedTile)
    {
        if (clickedTile == null || clickedTile.isImmutable || clickedTile.isStartTile || clickedTile.isEndTile || clickedTile.isCheckpoint) return;

        if (firstSelected == null)
        {
            // Check if player can afford at least 1 swap
            if (EconomyManager.Instance != null && !EconomyManager.Instance.CanAffordSwap())
            {
                Debug.LogWarning("[Tower Master] Not enough Dungeon Points (DP) to swap tiles!");
                return;
            }

            firstSelected = clickedTile;
            TileProperty prop = firstSelected.GetComponent<TileProperty>();
            if (prop != null) prop.SetTileColor(highlightColor);
        }
        else
        {
            if (firstSelected == clickedTile)
            {
                DeselectCurrent();
                return;
            }

            // Check DP cost (GDD: each swap consumes Dungeon Points)
            if (EconomyManager.Instance != null)
            {
                if (!EconomyManager.Instance.CanAffordSwap())
                {
                    Debug.LogWarning("[Tower Master] Not enough DP to complete swap!");
                    DeselectCurrent();
                    return;
                }

                EconomyManager.Instance.SpendSwapCost();
            }

            if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
            if (gridManager != null)
            {
                TileProperty propA = firstSelected.GetComponent<TileProperty>();
                if (propA != null) propA.SetTileColor(propA.GetBaseTypeColor());

                gridManager.SwapTiles(firstSelected, clickedTile);
            }
            firstSelected = null;
        }
    }
}
