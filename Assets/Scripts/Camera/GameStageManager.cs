using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Controls 2D Top-Down Editing Stage <-> 2.5D Arknights Action Stage transitions in 3D space.
/// </summary>
public class GameStageManager : MonoBehaviour
{
    public static GameStageManager Instance { get; private set; }

    public enum Stage { Edit2D, Action25D }

    [Header("Current State")]
    public Stage currentStage = Stage.Edit2D;
    public bool isTransitioning = false;

    [Header("Camera Configuration")]
    public Camera targetCamera;
    public float transitionDuration = 1.2f;

    [Header("Edit Mode View (Image 1: Overhead 3D)")]
    public Vector3 edit2DRotation = new Vector3(76f, 0f, 0f);
    public float edit2DFov = 48f;

    [Header("Action Mode View (Image 2: Low-Angle 2.5D)")]
    public Vector3 action25DRotation = new Vector3(35f, 0f, 0f);
    public float action25DFov = 48f;

    public static event Action<Stage> OnStageChanged;

    private GridManager gridManager;
    private EnemySpawner enemySpawner;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;

        gridManager = FindFirstObjectByType<GridManager>();
        enemySpawner = FindFirstObjectByType<EnemySpawner>();

        if (targetCamera != null)
        {
            targetCamera.orthographic = false;
            ApplyStageCameraImmediate(Stage.Edit2D);
        }

        // Ensure adequate Directional Lighting for 3D depth and shadows
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        bool hasDirLight = false;
        foreach (Light l in allLights)
        {
            if (l != null && l.type == LightType.Directional)
            {
                hasDirLight = true;
                break;
            }
        }

        if (!hasDirLight)
        {
            GameObject lightObj = new GameObject("3D Directional Light");
            Light lightComp = lightObj.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            lightComp.intensity = 1.2f;
            lightComp.color = Color.white;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }

    private void Update()
    {
        if (InputHelper.GetSpaceKeyDown())
        {
            ToggleStage();
        }

        if (InputHelper.GetRKeyDown() && currentStage == Stage.Action25D)
        {
            SetStage(Stage.Edit2D);
        }
    }

    public void ToggleStage()
    {
        if (isTransitioning) return;

        if (currentStage == Stage.Edit2D) SetStage(Stage.Action25D);
        else SetStage(Stage.Edit2D);
    }

    public void SetStage(Stage newStage)
    {
        if (isTransitioning) return;

        if (newStage == Stage.Action25D)
        {
            if (gridManager != null && !gridManager.isPathValid)
            {
                Debug.LogWarning("[GameStageManager] Cannot deploy: Path to goal is incomplete!");
                return;
            }
        }

        StartCoroutine(TransitionRoutine(newStage));
    }

    private IEnumerator TransitionRoutine(Stage newStage)
    {
        isTransitioning = true;
        currentStage = newStage;

        if (newStage == Stage.Edit2D)
        {
            if (enemySpawner != null) enemySpawner.StopAndClearEnemies();
        }

        if (targetCamera != null)
        {
            Vector3 startPos = targetCamera.transform.position;
            Quaternion startRot = targetCamera.transform.rotation;
            float startFov = targetCamera.fieldOfView;

            CalculateTargetCameraTransform(newStage, out Vector3 targetPos, out Quaternion targetRot, out float targetFov);

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);

                targetCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
                targetCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                targetCamera.fieldOfView = Mathf.Lerp(startFov, targetFov, t);

                yield return null;
            }

            targetCamera.transform.position = targetPos;
            targetCamera.transform.rotation = targetRot;
            targetCamera.fieldOfView = targetFov;
        }

        isTransitioning = false;
        OnStageChanged?.Invoke(currentStage);

        if (newStage == Stage.Action25D)
        {
            if (enemySpawner != null) enemySpawner.StartSpawning();
        }
    }

    private void CalculateTargetCameraTransform(Stage stage, out Vector3 pos, out Quaternion rot, out float fov)
    {
        Vector3 center = Vector3.zero;
        float w = gridManager != null ? gridManager.width : 13f;
        float h = gridManager != null ? gridManager.height : 9f;
        float spacing = gridManager != null ? gridManager.tileSpacing : 1.06f;

        float boardWidth = w * spacing;
        float boardDepth = h * spacing;
        float maxDim = Mathf.Max(boardWidth * 0.62f, boardDepth);

        if (stage == Stage.Edit2D)
        {
            // Matches Image 1: High-angle overhead perspective (76 deg) framing the full board
            float camY = maxDim * 1.32f;
            float camZ = -maxDim * 0.32f;
            pos = center + new Vector3(0f, camY, camZ);
            rot = Quaternion.Euler(edit2DRotation);
            fov = edit2DFov;
        }
        else
        {
            // Matches Image 2: Low-angle dramatic 2.5D perspective (35 deg)
            float camY = maxDim * 0.56f;
            float camZ = -maxDim * 0.88f;
            pos = center + new Vector3(0f, camY, camZ);
            rot = Quaternion.Euler(action25DRotation);
            fov = action25DFov;
        }
    }

    private void ApplyStageCameraImmediate(Stage stage)
    {
        if (targetCamera == null) return;
        CalculateTargetCameraTransform(stage, out Vector3 pos, out Quaternion rot, out float fov);
        targetCamera.transform.position = pos;
        targetCamera.transform.rotation = rot;
        targetCamera.fieldOfView = fov;
    }

    private void OnGUI()
    {
        int buttonWidth = 250;
        int buttonHeight = 45;
        int padding = 20;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.box);
        labelStyle.fontSize = 14;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.alignment = TextAnchor.MiddleCenter;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 14;
        buttonStyle.fontStyle = FontStyle.Bold;

        // Mode Status Badge (Top-Left)
        string statusText = currentStage == Stage.Edit2D 
            ? "MODE: 2D EDIT STAGE (Click / Swap 3D Tiles)" 
            : "MODE: 2.5D ARKNIGHTS ACTION (Wave Active)";
        GUI.Box(new Rect(padding, padding, 360, 34), statusText, labelStyle);

        // Action Button (Bottom-Right)
        float btnX = Screen.width - buttonWidth - padding;
        float btnY = Screen.height - buttonHeight - padding;

        if (currentStage == Stage.Edit2D)
        {
            bool pathOk = gridManager != null && gridManager.isPathValid;
            string btnText = pathOk ? "▶ START WAVE (SPACE)" : "⚠ INCOMPLETE PATH";

            GUI.enabled = pathOk && !isTransitioning;
            if (GUI.Button(new Rect(btnX, btnY, buttonWidth, buttonHeight), btnText, buttonStyle))
            {
                SetStage(Stage.Action25D);
            }
            GUI.enabled = true;
        }
        else
        {
            if (GUI.Button(new Rect(btnX, btnY, buttonWidth, buttonHeight), "↺ EDIT GRID (SPACE / R)", buttonStyle) && !isTransitioning)
            {
                SetStage(Stage.Edit2D);
            }
        }
    }
}
