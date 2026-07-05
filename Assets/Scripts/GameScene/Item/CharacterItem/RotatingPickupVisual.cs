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

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= EditorUpdate;
#endif

        if (!Application.isPlaying)
            RestoreBasePose();
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        elapsedTime += Time.deltaTime;
        ApplyPose(elapsedTime);
    }

#if UNITY_EDITOR
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
    private void CaptureCurrentAsBasePose()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
        basePoseCached = true;
    }

    [ContextMenu("Reset Preview Pose")]
    private void RestoreBasePose()
    {
        if (!basePoseCached)
            return;

        transform.localPosition = baseLocalPosition;
        transform.localRotation = baseLocalRotation;
        elapsedTime = 0f;
    }
}