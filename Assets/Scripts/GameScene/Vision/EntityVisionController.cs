using UnityEngine;

[DisallowMultipleComponent]
public class EntityVisionController : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Vision origin. If empty, this transform is used.")]
    [SerializeField] private Transform _origin;

    [Tooltip("Optional shooter stats runtime. Player and enemy shooter variants can both provide final VisionRange through this.")]
    [SerializeField] private ShooterStatsRuntime _statsRuntime;

    [Tooltip("Vision settings source. If empty, Damageable.StatConfig is used.")]
    [SerializeField] private EntityStatConfigSO _statConfig;

    [Header("Line Of Sight")]
    [Tooltip("Raycast trigger handling for line-of-sight checks.")]
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore;

    private Damageable _damageable;

    public Transform Origin => _origin != null ? _origin : transform;
    public Vector3 EyePosition => Origin.position + Vector3.up * EyeHeight;

    public float VisionRange => ResolveVisionRange();
    public bool UsesLineOfSight => ResolveSettings() != null && ResolveSettings().UseLineOfSight;
    public float EyeHeight => ResolveSettings() != null ? ResolveSettings().EyeHeight : 0f;
    public float TargetHeight => ResolveSettings() != null ? ResolveSettings().TargetHeight : 0f;
    public LayerMask LineOfSightBlockerMask => ResolveSettings() != null ? ResolveSettings().LineOfSightBlockerMask : 0;

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
        if (VisionRange <= 0f)
            return false;

        Vector3 targetPoint = worldPoint + Vector3.up * Mathf.Max(0f, targetHeight);

        if (!IsInsideVisionRange(targetPoint))
            return false;

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
}
