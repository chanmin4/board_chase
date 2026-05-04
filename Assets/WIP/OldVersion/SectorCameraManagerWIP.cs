using System.Collections;
using UnityEngine;

public class SectorCameraManagerWip : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform cameraTarget;

    [Header("Events")]
    [SerializeField] private SectorRuntimeEventChannelSO startSectorReadyEvent = default;
    [SerializeField] private SectorRuntimeEventChannelSO moveSectorCameraEvent = default;

    [Header("Slide")]
    [SerializeField] private float slideDuration = 0.45f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Sector Framing")]
    [SerializeField] private bool useBoundsDrivenPose = true;
    [SerializeField, Range(0f, 45f)] private float tiltFromTopDeg = 15f;
    [SerializeField] private float yawDeg = 45f;
    [SerializeField] private float cameraDistance = 10f;
    [SerializeField] private float centerYOffset = 0f;
    [SerializeField] private Vector3 localFramingOffset = Vector3.zero;

    [Header("Perspective Auto Fit")]
    [SerializeField] private bool autoFitPerspectiveDistance = true;
    [SerializeField] private float perspectivePadding = 1.05f;

    [Header("Broadcasting")]
    [SerializeField] private WorldCameraEventChannelSO worldCameraReadyChannel;

    private Coroutine _moveCoroutine;
    private bool _isMoving;

    public bool IsMoving => _isMoving;

    private Transform TargetTransform
    {
        get
        {
            if (cameraTarget != null)
                return cameraTarget;

            if (mainCamera != null)
                return mainCamera.transform;

            return null;
        }
    }

    private void Awake()
    {
        ResolveCameraRefs();
    }

    private void Reset()
    {
        ResolveCameraRefs();
    }

    private void OnEnable()
    {
        if (startSectorReadyEvent != null)
            startSectorReadyEvent.OnEventRaised += HandleStartSectorReady;

        if (moveSectorCameraEvent != null)
            moveSectorCameraEvent.OnEventRaised += HandleMoveSectorCamera;

        ResolveCameraRefs();

        if (worldCameraReadyChannel != null && mainCamera != null)
            worldCameraReadyChannel.RaiseEvent(mainCamera);
    }

    private void OnDisable()
    {
        if (startSectorReadyEvent != null)
            startSectorReadyEvent.OnEventRaised -= HandleStartSectorReady;

        if (moveSectorCameraEvent != null)
            moveSectorCameraEvent.OnEventRaised -= HandleMoveSectorCamera;

        if (worldCameraReadyChannel != null && mainCamera != null)
            worldCameraReadyChannel.Clear(mainCamera);
    }

    private void HandleStartSectorReady(SectorRuntime sector)
    {
        MoveToSectorCamera(sector, true);
    }

    private void HandleMoveSectorCamera(SectorRuntime sector)
    {
        MoveToSectorCamera(sector, false);
    }

    public void MoveToSectorCamera(SectorRuntime sector, bool instant = false)
    {
        if (sector == null)
            return;

        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        if (instant)
        {
            ApplySectorPoseInstant(sector);
            return;
        }

        _moveCoroutine = StartCoroutine(SlideTo(sector));
    }

    private IEnumerator SlideTo(SectorRuntime sector)
    {
        Transform target = TargetTransform;
        if (target == null)
            yield break;

        _isMoving = true;

        Vector3 startPos = target.position;
        Quaternion startRot = target.rotation;

        ResolveSectorPose(sector, out Vector3 endPos, out Quaternion endRot);

        float t = 0f;

        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float k = slideCurve.Evaluate(Mathf.Clamp01(t / slideDuration));

            target.position = Vector3.Lerp(startPos, endPos, k);
            target.rotation = Quaternion.Slerp(startRot, endRot, k);

            yield return null;
        }

        target.position = endPos;
        target.rotation = endRot;

        _isMoving = false;
        _moveCoroutine = null;
    }

    private void ApplySectorPoseInstant(SectorRuntime sector)
    {
        Transform target = TargetTransform;
        if (target == null)
            return;

        ResolveSectorPose(sector, out Vector3 endPos, out Quaternion endRot);

        target.position = endPos;
        target.rotation = endRot;

        _isMoving = false;
        _moveCoroutine = null;
    }

    private void ResolveSectorPose(SectorRuntime sector, out Vector3 pos, out Quaternion rot)
    {
        if (!useBoundsDrivenPose && sector.cameraPoint != null)
        {
            pos = sector.cameraPoint.position;
            rot = sector.cameraPoint.rotation;
            return;
        }

        Bounds bounds = sector.GetWorldBounds();
        Vector3 center = bounds.center + Vector3.up * centerYOffset;

        rot = ResolveSectorRotation();

        float distance = cameraDistance;

        if (mainCamera != null && !mainCamera.orthographic && autoFitPerspectiveDistance)
            distance = ResolvePerspectiveDistance(sector, rot);

        Vector3 forward = rot * Vector3.forward;
        pos = center - forward * distance;
        pos += rot * localFramingOffset;
    }

    private float ResolvePerspectiveDistance(SectorRuntime sector, Quaternion camRot)
    {
        if (mainCamera == null)
            return cameraDistance;

        Bounds bounds = sector.GetWorldBounds();
        Vector3 center = bounds.center + Vector3.up * centerYOffset;

        Vector3[] corners = new Vector3[4];
        corners[0] = new Vector3(bounds.min.x, center.y, bounds.min.z);
        corners[1] = new Vector3(bounds.max.x, center.y, bounds.min.z);
        corners[2] = new Vector3(bounds.max.x, center.y, bounds.max.z);
        corners[3] = new Vector3(bounds.min.x, center.y, bounds.max.z);

        Quaternion invRot = Quaternion.Inverse(camRot);

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 local = invRot * (corners[i] - center);

            if (local.x < minX) minX = local.x;
            if (local.x > maxX) maxX = local.x;
            if (local.y < minY) minY = local.y;
            if (local.y > maxY) maxY = local.y;
        }

        float halfWidth = (maxX - minX) * 0.5f;
        float halfHeight = (maxY - minY) * 0.5f;

        float verticalFovRad = mainCamera.fieldOfView * Mathf.Deg2Rad;
        float horizontalFovRad = 2f * Mathf.Atan(Mathf.Tan(verticalFovRad * 0.5f) * mainCamera.aspect);

        float distanceByHeight = halfHeight / Mathf.Tan(verticalFovRad * 0.5f);
        float distanceByWidth = halfWidth / Mathf.Tan(horizontalFovRad * 0.5f);

        return Mathf.Max(distanceByHeight, distanceByWidth) * perspectivePadding;
    }

    private Quaternion ResolveSectorRotation()
    {
        float pitch = 90f - tiltFromTopDeg;
        return Quaternion.Euler(pitch, yawDeg, 0f);
    }

    private void ResolveCameraRefs()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (cameraTarget == null && mainCamera != null)
            cameraTarget = mainCamera.transform;
    }
}
