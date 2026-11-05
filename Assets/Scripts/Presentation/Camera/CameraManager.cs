/*
 * Role hierarchy: Presentation > Camera
 * Primary responsibility: Handles CameraManager camera, vision, range, or aiming calculations and presentation.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Serialization;

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

    [Header("Character Camera Occlusion")]
    [Tooltip("Automatically adds the shared hidden-character silhouette controller. It affects only PlayerCharacter, Enemy, and Shooter visuals; structure and ordinary object materials are never changed.")]
    [FormerlySerializedAs("enableEntityOccluderDither")]
    [SerializeField] private bool enableCharacterOcclusionSilhouette = true;

    [Header("Environment Camera Cutout")]
    [Tooltip("플레이어 주변을 가리는 전경 구조물만 국소 투시합니다. 적 발견·시야·충돌은 바꾸지 않습니다.")]
    [SerializeField] private bool enableEnvironmentCameraCutout = true;
    [SerializeField, Min(1f)] private float environmentRevealRadius = 9f;
    [SerializeField, Min(.1f)] private float environmentRevealSoftEdge = 1.5f;
    [SerializeField, Min(.02f)] private float environmentRevealFadeSeconds = .18f;
    private CameraEnvironmentCutoutController _environmentCutout;
    private Camera _occlusionCamera;
    private bool _lastEnvironmentCutoutEnabled, _lastSilhouetteEnabled;

    internal Transform CurrentFollowTarget => followTarget;
    internal float EnvironmentRevealRadius => Mathf.Max(1f, environmentRevealRadius);
    internal float EnvironmentRevealSoftEdge => Mathf.Clamp(environmentRevealSoftEdge, .1f, EnvironmentRevealRadius);
    internal float EnvironmentRevealFadeSeconds => Mathf.Max(.02f, environmentRevealFadeSeconds);

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

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveCameraRefs();
        EnsureEntityOcclusionController();
    }

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        ResolveCameraRefs();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveCameraRefs();
        EnsureEntityOcclusionController();
        TryResolveFollowTarget();

        if (worldCameraReadyChannel != null && mainCamera != null)
            worldCameraReadyChannel.RaiseEvent(mainCamera);

        if (snapOnEnable)
            SnapToTarget();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_environmentCutout != null) _environmentCutout.enabled = false;
        _occlusionCamera = null;
        if (worldCameraReadyChannel != null && mainCamera != null)
            worldCameraReadyChannel.Clear(mainCamera);
    }

    // Purpose: Processes frame work that must run after the regular Update pass.
    private void LateUpdate()
    {
        EnsureEntityOcclusionController();
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

    // Purpose: Updates the follow target state.
    public void SetFollowTarget(Transform target, bool instant = true)
    {
        followTarget = target;
        ResetFollowState();

        if (instant)
            SnapToTarget();
    }

    // Purpose: Performs the snap to target workflow for this component.
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

    // Purpose: Resolves the camera pose needed by this component.
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

    // Purpose: Resolves the camera rotation needed by this component.
    private Quaternion ResolveCameraRotation()
    {
        float pitch = 90f - tiltFromTopDeg;
        return Quaternion.Euler(pitch, yawDeg, 0f);
    }

    // Purpose: Updates the look ahead from the current runtime state.
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

    // Purpose: Performs the reset follow state workflow for this component.
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

    // Purpose: Attempts to resolve follow target and reports whether the request succeeded.
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

    // Purpose: Resolves the camera refs needed by this component.
    private void ResolveCameraRefs()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (cameraTarget == null && mainCamera != null)
            cameraTarget = mainCamera.transform;
    }

    // Purpose: Ensures entity occlusion controller is ready before the workflow continues.
    private void EnsureEntityOcclusionController()
    {
        if (mainCamera == null)
            return;
        if (_occlusionCamera == mainCamera && _lastEnvironmentCutoutEnabled == enableEnvironmentCameraCutout &&
            _lastSilhouetteEnabled == enableCharacterOcclusionSilhouette) return;
        if (_environmentCutout != null && _occlusionCamera != mainCamera) _environmentCutout.enabled = false;
        if (enableCharacterOcclusionSilhouette) CameraEntityOcclusionFadeController.EnsureFor(mainCamera);
        _environmentCutout = mainCamera.GetComponent<CameraEnvironmentCutoutController>();
        if (enableEnvironmentCameraCutout)
        {
            if (_environmentCutout == null) _environmentCutout = mainCamera.gameObject.AddComponent<CameraEnvironmentCutoutController>();
            _environmentCutout.Configure(mainCamera, this);
        }
        else if (_environmentCutout != null) _environmentCutout.enabled = false;
        _occlusionCamera = mainCamera;
        _lastEnvironmentCutoutEnabled = enableEnvironmentCameraCutout;
        _lastSilhouetteEnabled = enableCharacterOcclusionSilhouette;
    }
}
