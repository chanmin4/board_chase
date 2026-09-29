/*
 * Role hierarchy: Gameplay > Items > World
 * Primary responsibility: Handles RotatingPickupVisual interaction, pickup, trade, loot, or storage flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public sealed class RotatingPickupVisual : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField]
    private Vector3 rotationSpeed = new Vector3(0f, 90f, 0f);

    [Header("Floating")]
    [SerializeField, Min(0f)]
    private float floatHeight = 0.1f;

    [SerializeField, Min(0f)]
    private float floatFrequency = 1.5f;

    [Header("Editor Preview")]
    [SerializeField]
    private bool previewInEditMode = true;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;

    private float elapsedTime;
    private bool basePoseCached;

#if UNITY_EDITOR
    private double lastEditorTime;
#endif

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        CaptureCurrentAsBasePose();
        elapsedTime = 0f;

#if UNITY_EDITOR
        EditorApplication.update -= EditorUpdate;
        EditorApplication.update += EditorUpdate;

        lastEditorTime = EditorApplication.timeSinceStartup;
#endif
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= EditorUpdate;
#endif

        if (!Application.isPlaying)
            RestoreBasePose();
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        if (!Application.isPlaying)
            return;

        elapsedTime += Time.deltaTime;
        ApplyPose(elapsedTime);
    }

#if UNITY_EDITOR
    // Purpose: Performs the editor update workflow for this component.
    private void EditorUpdate()
    {
        if (Application.isPlaying || !previewInEditMode || !isActiveAndEnabled)
        {
            lastEditorTime = EditorApplication.timeSinceStartup;
            return;
        }

        double currentTime = EditorApplication.timeSinceStartup;
        float deltaTime = (float)(currentTime - lastEditorTime);
        lastEditorTime = currentTime;

        // 에디터가 잠깐 멈췄다가 돌아왔을 때 갑자기 크게 점프하는 것 방지
        deltaTime = Mathf.Min(deltaTime, 0.05f);

        elapsedTime += deltaTime;
        ApplyPose(elapsedTime);

        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }
#endif

    // Purpose: Applies the requested pose change.
    private void ApplyPose(float time)
    {
        transform.localRotation =
            baseLocalRotation *
            Quaternion.Euler(rotationSpeed * time);

        Vector3 position = baseLocalPosition;

        position.y += Mathf.Sin(
            time * floatFrequency * Mathf.PI * 2f
        ) * floatHeight;

        transform.localPosition = position;
    }

    [ContextMenu("Capture Current As Base Pose")]
    // Purpose: Performs the capture current as base pose workflow for this component.
    private void CaptureCurrentAsBasePose()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
        basePoseCached = true;
    }

    [ContextMenu("Reset Preview Pose")]
    // Purpose: Restores the saved base pose state.
    private void RestoreBasePose()
    {
        if (!basePoseCached)
            return;

        transform.localPosition = baseLocalPosition;
        transform.localRotation = baseLocalRotation;
        elapsedTime = 0f;
    }
}
