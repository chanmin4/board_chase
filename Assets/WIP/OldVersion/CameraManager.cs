using System.Collections;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform cameraTarget;

    [Header("Events")]
    [Tooltip("SectorStateManager가 시작 섹터를 알려줄 때 받는 이벤트")]
    [SerializeField] private SectorRuntimeEventChannelSO startSectorReadyEvent=default;

    [Tooltip("외부에서 특정 섹터로 카메라 이동 요청할 때 받는 이벤트")]
    [SerializeField] private SectorRuntimeEventChannelSO moveSectorCameraEvent=default;

    [Header("Slide")]
    [SerializeField] private float slideDuration = 0.45f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Sector Framing")]
    [SerializeField] private bool useBoundsDrivenPose = true;

    [Tooltip("직하(top-down)에서 몇 도 비스듬하게 볼지. 0=완전 수직, 15=살짝 기울임")]
    [SerializeField, Range(0f, 45f)] private float tiltFromTopDeg = 15f;

    [Tooltip("섹터를 어느 방향에서 볼지 Yaw")]
    [SerializeField] private float yawDeg = 45f;

    [Tooltip("카메라가 섹터 중심에서 얼마나 떨어질지")]
    [SerializeField] private float cameraDistance = 10f;

    [Tooltip("섹터 중심을 약간 위/아래로 보정")]
    [SerializeField] private float centerYOffset = 0f;

    [Tooltip("카메라 로컬 기준 추가 위치 보정")]
    [SerializeField] private Vector3 localFramingOffset = Vector3.zero;
    [Header("Perspective Auto Fit")]
    [SerializeField] private bool autoFitPerspectiveDistance = true;
    [SerializeField] private float perspectivePadding = 1.05f;
    [Header("Broadcasting On")]
    [SerializeField] private WorldCameraEventChannelSO _worldCameraReadyChannel;


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
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (cameraTarget == null && mainCamera != null)
            cameraTarget = mainCamera.transform;
    }
    private void Reset()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (cameraTarget == null && mainCamera != null)
            cameraTarget = mainCamera.transform;
    }

    private void OnEnable()
    {
        if (startSectorReadyEvent != null)
            startSectorReadyEvent.OnEventRaised += HandleStartSectorReady;

        if (moveSectorCameraEvent != null)
            moveSectorCameraEvent.OnEventRaised += HandleMoveSectorCamera;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (_worldCameraReadyChannel != null && mainCamera != null)
            _worldCameraReadyChannel.RaiseEvent(mainCamera);
    }

    private void OnDisable()
    {
        if (startSectorReadyEvent != null)
            startSectorReadyEvent.OnEventRaised -= HandleStartSectorReady;

        if (moveSectorCameraEvent != null)
            moveSectorCameraEvent.OnEventRaised -= HandleMoveSectorCamera;

        if (_worldCameraReadyChannel != null && mainCamera != null)
            _worldCameraReadyChannel.Clear(mainCamera);
    }

    /// <summary>
    /// 시작 섹터 이벤트를 받으면 즉시 스냅한다.
    /// </summary>
    private void HandleStartSectorReady(SectorRuntime sector)
    {
        MoveToSectorCamera(sector, true);
    }

    /// <summary>
    /// 일반 섹터 이동 요청은 슬라이드 이동으로 처리한다.
    /// </summary>
    private void HandleMoveSectorCamera(SectorRuntime sector)
    {
        MoveToSectorCamera(sector, false);
    }

    /// <summary>
    /// 특정 섹터 카메라 포즈로 이동한다.
    /// instant=true면 즉시 이동, false면 부드럽게 이동한다.
    /// </summary>
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

    /// <summary>
    /// 섹터 bounds 기준으로 카메라 포즈를 계산한다.
    /// 
    /// useBoundsDrivenPose=true 이면:
    /// - SectorRuntime.GetWorldBounds() 중심을 기준으로
    /// - 살짝 비스듬한 2.5D 시점 포즈를 자동 생성한다.
    /// 
    /// useBoundsDrivenPose=false 이고 cameraPoint가 있으면:
    /// - cameraPoint의 위치/회전을 그대로 사용한다.
    /// </summary>
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
    /// <summary>
    /// 현재 섹터 공통 카메라 회전을 만든다.
    /// 
    /// tiltFromTopDeg:
    /// - 0이면 완전 수직 탑다운
    /// - 15면 위에서 15도 비스듬히 보는 느낌
    /// </summary>
    private Quaternion ResolveSectorRotation()
    {
        float pitch = 90f - tiltFromTopDeg;
        return Quaternion.Euler(pitch, yawDeg, 0f);
    }

}
