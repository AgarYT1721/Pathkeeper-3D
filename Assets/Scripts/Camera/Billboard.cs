using UnityEngine;

/// <summary>
/// Keeps 2D sprites standing upright and facing the active 3D camera (Arknights 2.5D style).
/// </summary>
public class Billboard : MonoBehaviour
{
    [Tooltip("If true, matches the camera's full rotation. If false, only aligns yaw/forward.")]
    public bool matchCameraRotation = true;

    [Tooltip("Optional rotation offset in degrees.")]
    public Vector3 rotationOffset = Vector3.zero;

    private Camera targetCamera;

    void Start()
    {
        targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null) return;
        }

        if (matchCameraRotation)
        {
            transform.rotation = targetCamera.transform.rotation * Quaternion.Euler(rotationOffset);
        }
        else
        {
            Vector3 forward = targetCamera.transform.forward;
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(rotationOffset);
        }
    }
}
