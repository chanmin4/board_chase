using UnityEngine;

[DisallowMultipleComponent]
public class EntityVisionController : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Vision origin. If empty, this transform is used.")]
    [SerializeField] private Transform _origin;

    [Tooltip("Optional shooter stats runtime. Player and enemy shooter variants can both provide final VisionRange through this.")]
    [SerializeField] private ShooterStatsRuntime _statsRuntime;

    [Tooltip("Optional gameplay range provider. Close circular vision uses this MaxRange, so aiming can widen nearby visibility through existing range modifiers.")]
    [SerializeField] private EntityRange _range;

    [Tooltip("Vision settings source. If empty, Damageable.StatConfig is used.")]
    [SerializeField] private EntityStatConfigSO _statConfig;

    [Tooltip("Optional player aim state. When assigned, aiming narrows the forward vision cone over Aim01.")]
    [SerializeField] private PlayerAimAction _playerAimAction;

    [Header("Line Of Sight")]
    [Tooltip("Raycast trigger handling for line-of-sight checks.")]
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore;

    private Damageable _damageable;

    public Transform Origin => _origin != null ? _origin : transform;
    public Vector3 EyePosition => Origin.position + Vector3.up * EyeHeight;

    public float VisionRange => ResolveVisionRange();
    public float CloseVisionRadius => ResolveCloseVisionRadius();
    public bool UsesLineOfSight => ResolveSettings() != null && ResolveSettings().UseLineOfSight;
    public bool UsesForwardArc => ResolveSettings() != null && ResolveSettings().UseForwardArc;
    public float EyeHeight => ResolveSettings() != null ? ResolveSettings().EyeHeight : 0f;
    public float TargetHeight => ResolveSettings() != null ? ResolveSettings().TargetHeight : 0f;
    public LayerMask LineOfSightBlockerMask => ResolveSettings() != null ? ResolveSettings().LineOfSightBlockerMask : 0;
    public float ForwardArcAngle => ResolveForwardArcAngle();
    public Vector3 ForwardDirection => ResolveForwardDirection();

    private void Reset()
    {
        ResolveRefs();
    }

    private void Awake()
    {
        ResolveRefs();
    }

    public bool CanSeeTarget(Transform target)
    {
        if (target == null)
            return false;

        return CanSeeWorldPoint(target.position, TargetHeight);
    }

    public bool CanSeeDamageable(Damageable target)
    {
        if (target == null || target.IsDead)
            return false;

        Vector3 targetPoint = target.MainMeshRenderer != null
            ? target.MainMeshRenderer.bounds.center
            : target.transform.position + Vector3.up * TargetHeight;

        return CanSeeWorldPoint(targetPoint, 0f);
    }

    public bool CanSeeBounds(Bounds bounds)
    {
        if (CanSeeWorldPoint(bounds.center, 0f))
            return true;

        Vector3 top = bounds.center;
        top.y = bounds.max.y;

        return CanSeeWorldPoint(top, 0f);
    }

    public bool CanSeeWorldPoint(Vector3 worldPoint, float targetHeight)
    {
        if (VisionRange <= 0f && ResolveCloseVisionRadius() <= 0f)
            return false;

        Vector3 targetPoint = worldPoint + Vector3.up * Mathf.Max(0f, targetHeight);

        bool insideCloseCircle = IsInsideCloseVisionCircle(targetPoint);

        if (!insideCloseCircle)
        {
            if (!IsInsideVisionRange(targetPoint))
                return false;

            if (!IsInsideForwardArc(targetPoint))
                return false;
        }

        if (!UsesLineOfSight)
            return true;

        LayerMask mask = LineOfSightBlockerMask;
        if (mask.value == 0)
            return true;

        Vector3 start = EyePosition;
        Vector3 toTarget = targetPoint - start;
        float distance = toTarget.magnitude;

        if (distance <= 0.001f)
            return true;

        return !Physics.Raycast(
            start,
            toTarget / distance,
            distance,
            mask,
            _triggerInteraction);
    }

    public bool IsInsideVisionRange(Vector3 worldPoint)
    {
        Vector3 delta = worldPoint - Origin.position;
        delta.y = 0f;

        float range = VisionRange;
        return range > 0f && delta.sqrMagnitude <= range * range;
    }

    private void ResolveRefs()
    {
        if (_origin == null)
            _origin = transform;

        if (_statsRuntime == null)
            _statsRuntime = GetComponent<ShooterStatsRuntime>() ?? GetComponentInParent<ShooterStatsRuntime>();

        if (_range == null)
            _range = GetComponent<EntityRange>() ?? GetComponentInParent<EntityRange>();

        if (_playerAimAction == null)
            _playerAimAction = GetComponent<PlayerAimAction>() ?? GetComponentInParent<PlayerAimAction>();

        if (_damageable == null)
            _damageable = GetComponent<Damageable>() ?? GetComponentInParent<Damageable>();

        if (_statConfig == null && _statsRuntime != null)
            _statConfig = _statsRuntime.StatConfig;

        if (_statConfig == null && _damageable != null)
            _statConfig = _damageable.StatConfig;
    }

    private EntityVisionSettings ResolveSettings()
    {
        if (_statConfig != null)
            return _statConfig.Vision;

        if (_statsRuntime != null && _statsRuntime.StatConfig != null)
            return _statsRuntime.StatConfig.Vision;

        if (_damageable != null && _damageable.StatConfig != null)
            return _damageable.StatConfig.Vision;

        return null;
    }

    private float ResolveVisionRange()
    {
        if (_statsRuntime != null)
        {
            float runtimeRange = _statsRuntime.VisionRange;

            if (runtimeRange > 0f)
                return runtimeRange;
        }

        EntityVisionSettings settings = ResolveSettings();
        return settings != null ? settings.VisionRange : 0f;
    }

    private float ResolveCloseVisionRadius()
    {
        if (_range != null && _range.MaxRange > 0f)
            return _range.MaxRange;

        if (_statsRuntime != null && _statsRuntime.MaxRange > 0f)
            return _statsRuntime.MaxRange;

        return 0f;
    }

    private bool IsInsideCloseVisionCircle(Vector3 worldPoint)
    {
        float range = ResolveCloseVisionRadius();

        if (range <= 0f)
            return false;

        Vector3 delta = worldPoint - Origin.position;
        delta.y = 0f;

        return delta.sqrMagnitude <= range * range;
    }

    private bool IsInsideForwardArc(Vector3 worldPoint)
    {
        EntityVisionSettings settings = ResolveSettings();

        if (settings == null || !settings.UseForwardArc)
            return true;

        Vector3 toTarget = worldPoint - Origin.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= 0.0001f)
            return true;

        Vector3 forward = ResolveForwardDirection();

        if (forward.sqrMagnitude <= 0.0001f)
            return true;

        float arcAngle = ResolveForwardArcAngle();
        float angle = Vector3.Angle(forward.normalized, toTarget.normalized);

        return angle <= arcAngle * 0.5f;
    }

    private float ResolveForwardArcAngle()
    {
        EntityVisionSettings settings = ResolveSettings();

        if (settings == null)
            return 360f;

        float arcAngle = settings.ResolveArcAngle(VisionRange);

        if (_playerAimAction == null)
            return arcAngle;

        float multiplier = Mathf.Lerp(
            1f,
            settings.AimArcAngleMultiplier,
            Mathf.Clamp01(_playerAimAction.Aim01));

        return Mathf.Clamp(arcAngle * multiplier, 1f, 360f);
    }

    private Vector3 ResolveForwardDirection()
    {
        if (_playerAimAction != null &&
            _playerAimAction.TryGetFlatAimDirection(Origin.position, out Vector3 aimDirection))
        {
            return aimDirection;
        }

        Vector3 forward = Origin.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.0001f)
            forward = transform.forward;

        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.0001f)
            return Vector3.forward;

        return forward.normalized;
    }
}
