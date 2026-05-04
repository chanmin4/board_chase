using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera mainCamera;

    [Tooltip("실제로 움직일 카메라 리그. 비워두면 mainCamera.transform을 사용")]
    [SerializeField] private Transform cameraTarget;

    [Header("Don't Ref Auto Find")]
    [SerializeField] private bool autoFindPlayerByTag = true;
    [SerializeField] private string playerTag = "Player";
    [Tooltip("카메라가 따라갈 플레이어 Transform")]
    [SerializeField] private Transform followTarget;

    [Header("Top View Pose")]
    [Tooltip("0=완전 수직, 15~25=살짝 비스듬한 덕르코프식 탑뷰")]
    [SerializeField, Range(0f, 45f)] private float tiltFromTopDeg = 20f;

    [Tooltip("카메라가 바라보는 월드 방향")]
    [SerializeField] private float yawDeg = 0f;

    [Tooltip("플레이어 중심에서 카메라가 떨어질 거리")]
    [SerializeField] private float cameraDistance = 120f;

    [Tooltip("카메라가 바라볼 중심 높이")]
    [SerializeField] private float targetHeight = 1f;

    [Tooltip("카메라 로컬 기준 추가 위치 보정")]
    [SerializeField] private Vector3 localFramingOffset = Vector3.zero;

    [Header("Follow")]
    [SerializeField] private float followSmoothTime = 0.12f;
    [SerializeField] private float maxFollowSpeed = Mathf.Infinity;
    [SerializeField] private bool snapOnEnable = true;

    [Header("Look Ahead")]
    [Tooltip("플레이어 이동 방향으로 화면 중심을 조금 앞당김")]
    [SerializeField] private bool useLookAhead = true;

    [SerializeField] private float lookAheadDistance = 1.25f;
    [SerializeField] private float maxLookAheadSpeed = 7f;
    [SerializeField] private float lookAheadSmoothTime = 0.08f;

    [Header("Broadcasting")]
    [SerializeField] private WorldCameraEventChannelSO worldCameraReadyChannel;

    private Vector3 _followVelocity;
    private Vector3 _lookAheadVelocity;
    private Vector3 _smoothedLookAhead;
    private Vector3 _lastFollowPosition;
    private bool _hasLastFollowPosition;

    public bool IsMoving { get; private set; }

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
        ResolveCameraRefs();
        TryResolveFollowTarget();

        if (worldCameraReadyChannel != null && mainCamera != null)
            worldCameraReadyChannel.RaiseEvent(mainCamera);

        if (snapOnEnable)
            SnapToTarget();
    }

    private void OnDisable()
    {
        if (worldCameraReadyChannel != null && mainCamera != null)
            worldCameraReadyChannel.Clear(mainCamera);
    }

    private void LateUpdate()
    {
        if (!TryResolveFollowTarget())
            return;

        Transform target = TargetTransform;
        if (target == null)
            return;

        UpdateLookAhead();

        ResolveCameraPose(out Vector3 desiredPosition, out Quaternion desiredRotation);

        target.position = Vector3.SmoothDamp(
            target.position,
            desiredPosition,
            ref _followVelocity,
            followSmoothTime,
            maxFollowSpeed,
            Time.deltaTime
        );

        target.rotation = desiredRotation;

        IsMoving = _followVelocity.sqrMagnitude > 0.0001f;
    }

    public void SetFollowTarget(Transform target, bool instant = true)
    {
        followTarget = target;
        ResetFollowState();

        if (instant)
            SnapToTarget();
    }

    public void SnapToTarget()
    {
        if (!TryResolveFollowTarget())
            return;

        Transform target = TargetTransform;
        if (target == null)
            return;

        ResetFollowState();
        ResolveCameraPose(out Vector3 desiredPosition, out Quaternion desiredRotation);

        target.position = desiredPosition;
        target.rotation = desiredRotation;

        _followVelocity = Vector3.zero;
        IsMoving = false;
    }

    private void ResolveCameraPose(out Vector3 position, out Quaternion rotation)
    {
        rotation = ResolveCameraRotation();

        Vector3 focusPoint = followTarget.position;
        focusPoint += Vector3.up * targetHeight;
        focusPoint += _smoothedLookAhead;

        Vector3 forward = rotation * Vector3.forward;

        position = focusPoint - forward * cameraDistance;
        position += rotation * localFramingOffset;
    }

    private Quaternion ResolveCameraRotation()
    {
        float pitch = 90f - tiltFromTopDeg;
        return Quaternion.Euler(pitch, yawDeg, 0f);
    }

    private void UpdateLookAhead()
    {
        if (followTarget == null)
            return;

        if (!_hasLastFollowPosition)
        {
            ResetFollowState();
            return;
        }

        Vector3 currentPosition = followTarget.position;
        Vector3 desiredLookAhead = Vector3.zero;

        if (useLookAhead && Time.deltaTime > 0f)
        {
            Vector3 velocity = (currentPosition - _lastFollowPosition) / Time.deltaTime;
            velocity.y = 0f;

            if (velocity.sqrMagnitude > 0.0001f)
            {
                float speed01 = Mathf.Clamp01(velocity.magnitude / maxLookAheadSpeed);
                desiredLookAhead = velocity.normalized * lookAheadDistance * speed01;
            }
        }

        _smoothedLookAhead = Vector3.SmoothDamp(
            _smoothedLookAhead,
            desiredLookAhead,
            ref _lookAheadVelocity,
            lookAheadSmoothTime
        );

        _lastFollowPosition = currentPosition;
    }

    private void ResetFollowState()
    {
        _followVelocity = Vector3.zero;
        _lookAheadVelocity = Vector3.zero;
        _smoothedLookAhead = Vector3.zero;

        if (followTarget != null)
        {
            _lastFollowPosition = followTarget.position;
            _hasLastFollowPosition = true;
        }
        else
        {
            _hasLastFollowPosition = false;
        }
    }

    private bool TryResolveFollowTarget()
    {
        if (followTarget != null)
            return true;

        if (!autoFindPlayerByTag || string.IsNullOrEmpty(playerTag))
            return false;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject == null)
            return false;

        followTarget = playerObject.transform;
        ResetFollowState();

        return true;
    }

    private void ResolveCameraRefs()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (cameraTarget == null && mainCamera != null)
            cameraTarget = mainCamera.transform;
    }
}
