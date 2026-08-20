using UnityEngine;

/// <summary>
/// Triggers hazard damage and status effects when units step onto this 3D tile.
/// </summary>
public class TileTrigger : MonoBehaviour
{
    private TileProperty tileProperty;

    void Start()
    {
        tileProperty = GetComponent<TileProperty>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) enemy = other.GetComponentInParent<Enemy>();

        if (enemy != null && tileProperty != null)
        {
            enemy.ApplyTileHazard(tileProperty.currentData);
            Debug.Log($"[Hazard Trigger] Enemy ({enemy.currentClass}) stepped on: {tileProperty.type}");
        }
    }
}
