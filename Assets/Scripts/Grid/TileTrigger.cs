using UnityEngine;

/// <summary>
/// Helper component on tiles. Hazard triggering is precisely managed by EnemyPathFinding
/// to prevent false-positive trigger brushes from adjacent diagonal/neighbor tiles.
/// </summary>
public class TileTrigger : MonoBehaviour
{
    // Path-based step hazard detection in EnemyPathFinding handles exact tile stepping
}
