/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Vision
 * Primary responsibility: Handles EntityVisionController camera, vision, range, or aiming calculations and presentation.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public class EntityVisionController : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Vision origin. If empty, this transform is used.")]
    [SerializeField] private Transform _origin;

    [Tooltip("Optional shooter stats runtime. Player and enemy shooter variants can both provide final VisionRange through this.")]
    [SerializeField] private ShooterStatsRuntime _statsRuntime;

    [Tooltip("Optional range provider used only to share the authored visual origin. Projectile MaxRange does not control vision.")]
    [SerializeField] private EntityRange _range;

    [Tooltip("Vision settings source. If empty, Damageable.StatConfig is used.")]
    [SerializeField] private EntityStatConfigSO _statConfig;

    [Header("Line Of Sight")]
    [Tooltip("How trigger colliders on the configured Line Of Sight Blocker Mask participate in vision checks.")]
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Debug")]
    [SerializeField] private bool _drawVisionDebug;
    [SerializeField] private Color _closeVisionDebugColor = new Color(0f, 1f, 1f, 0.7f);
    [SerializeField] private Color _forwardVisionDebugColor = new Color(1f, 0.9f, 0f, 0.7f);

    private Damageable _damageable;
    private EntityStatusEffectRuntime _statusEffects;
    private Vector3 _lastCheckedWorldPoint;
    private float _lastCheckedPlanarDistance = -1f;
    private bool _lastCheckInsideCloseRange;
    private bool _lastCheckInsideVisionRange;
    private bool _lastCheckInsideForwardArc;
    private bool _lastCheckHasLineOfSight;
    private Collider _lastLineOfSightBlocker;

    public Transform Origin => _origin != null ? _origin : transform;
    public Transform VisualOrigin => _range != null && _range.RangeOrigin != null ? _range.RangeOrigin : Origin;
    public Vector3 EyePosition => VisualOrigin.position + Vector3.up * EyeHeight;

    public float VisionRange => ResolveVisionRange();
    public float CloseVisionRadius => ResolveCloseVisionRadius();
    public bool UsesLineOfSight => ResolveSettings() != null && ResolveSettings().UseLineOfSight;
    public bool UsesForwardArc => ResolveSettings() != null && ResolveSettings().UseForwardArc;
    public float EyeHeight => ResolveSettings() != null ? ResolveSettings().EyeHeight : 0f;
    public float TargetHeight => ResolveSettings() != null ? ResolveSettings().TargetHeight : 0f;
    public LayerMask LineOfSightBlockerMask => ResolveSettings() != null ? ResolveSettings().LineOfSightBlockerMask : 0;
    public float ForwardArcAngle => ResolveForwardArcAngle();
    public Vector3 ForwardDirection => ResolveForwardDirection();
    public Vector3 LastCheckedWorldPoint => _lastCheckedWorldPoint;
    public float LastCheckedPlanarDistance => _lastCheckedPlanarDistance;
    public bool LastCheckInsideCloseRange => _lastCheckInsideCloseRange;
    public bool LastCheckInsideVisionRange => _lastCheckInsideVisionRange;
    public bool LastCheckInsideForwardArc => _lastCheckInsideForwardArc;
    public bool LastCheckHasLineOfSight => _lastCheckHasLineOfSight;
    public Collider LastLineOfSightBlocker => _lastLineOfSightBlocker;

    // Purpose: Initializes Inspector defaults for this component.
    protected virtual void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    protected virtual void Awake()
    {
        ResolveRefs();
    }

    // Purpose: Checks whether see target is currently permitted.
    public bool CanSeeTarget(Transform target)
    {
        if (target == null)
            return false;

        return CanSeeWorldPoint(target.position, TargetHeight);
    }

    // Purpose: Checks whether see damageable is currently permitted.
    public bool CanSeeDamageable(Damageable target)
    {
        if (target == null || target.IsDead)
            return false;

        Vector3 targetPoint = target.MainMeshRenderer != null
            ? target.MainMeshRenderer.bounds.center
            : target.transform.position + Vector3.up * TargetHeight;

        return CanSeeWorldPoint(targetPoint, 0f);
    }

    // Purpose: Checks whether see bounds is currently permitted.
    public bool CanSeeBounds(Bounds bounds)
    {
        if (CanSeeWorldPoint(bounds.center, 0f))
            return true;

        Vector3 top = bounds.center;
        top.y = bounds.max.y;

        return CanSeeWorldPoint(top, 0f);
    }

    // Purpose: Checks whether reveal visual point is currently permitted.
    public bool CanRevealVisualPoint(Vector3 worldPoint, float targetHeight)
    {
        // Renderer reveal and gameplay detection deliberately share one shape and LOS rule.
        return CanSeeWorldPoint(worldPoint, targetHeight);
    }

    // Purpose: Checks whether reveal visual bounds is currently permitted.
    public bool CanRevealVisualBounds(Bounds bounds)
    {
        if (CanRevealVisualPoint(bounds.center, 0f))
            return true;

        Vector3 top = bounds.center;
        top.y = bounds.max.y;

        return CanRevealVisualPoint(top, 0f);
    }

    // Purpose: Checks whether see world point is currently permitted.
    public bool CanSeeWorldPoint(Vector3 worldPoint, float targetHeight)
    {
        Vector3 targetPoint = worldPoint + Vector3.up * Mathf.Max(0f, targetHeight);
        Vector3 visionOrigin = VisualOrigin.position;
        _lastCheckedWorldPoint = targetPoint;
        _lastCheckedPlanarDistance = FlatDistance(visionOrigin, targetPoint);
        _lastCheckInsideCloseRange = false;
        _lastCheckInsideVisionRange = false;
        _lastCheckInsideForwardArc = false;
        _lastCheckHasLineOfSight = false;
        _lastLineOfSightBlocker = null;

        if (VisionRange <= 0f && CloseVisionRadius <= 0f)
            return false;

        _lastCheckInsideCloseRange =
            IsInsideFlatRange(visionOrigin, targetPoint, CloseVisionRadius);
        _lastCheckInsideVisionRange = IsInsideVisionRange(targetPoint);

        if (!_lastCheckInsideCloseRange && !_lastCheckInsideVisionRange)
            return false;

        _lastCheckInsideForwardArc = IsInsideForwardArc(targetPoint, visionOrigin);

        if (!_lastCheckInsideCloseRange && !_lastCheckInsideForwardArc)
            return false;

        _lastCheckHasLineOfSight = HasLineOfSightTo(
            targetPoint,
            out _lastLineOfSightBlocker);
        return _lastCheckHasLineOfSight;
    }

    // Purpose: Performs the flat distance workflow for this component.
    private static float FlatDistance(Vector3 origin, Vector3 worldPoint)
    {
        Vector3 delta = worldPoint - origin;
        delta.y = 0f;
        return delta.magnitude;
    }

    // Purpose: Checks whether this component has line of sight to.
    private bool HasLineOfSightTo(Vector3 targetPoint, out Collider blocker)
    {
        blocker = null;

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

        if (!Physics.Raycast(
                start,
                toTarget / distance,
                out RaycastHit hit,
                distance,
                mask,
                _triggerInteraction))
        {
            return true;
        }

        blocker = hit.collider;
        return false;
    }

    /// <summary>
    /// Returns how far vision remains open in one flat world direction using the
    /// same blocker mask and ray origin as entity visibility checks.
    /// </summary>
    public float GetVisibleDistance(Vector3 flatWorldDirection, float maxDistance)
    {
        return GetVisibleDistanceFrom(EyePosition, flatWorldDirection, maxDistance);
    }

    /// <summary>
    /// Returns how far vision remains open from a captured eye position. This keeps
    /// every ray in one visual fan tied to the exact same frame and origin.
    /// </summary>
    public float GetVisibleDistanceFrom(
        Vector3 eyePosition,
        Vector3 flatWorldDirection,
        float maxDistance)
    {
        maxDistance = Mathf.Max(0f, maxDistance);

        if (maxDistance <= 0f || !UsesLineOfSight || LineOfSightBlockerMask.value == 0)
            return maxDistance;

        flatWorldDirection.y = 0f;

        if (flatWorldDirection.sqrMagnitude <= 0.0001f)
            return 0f;

        flatWorldDirection.Normalize();

        return Physics.Raycast(
            eyePosition,
            flatWorldDirection,
            out RaycastHit hit,
            maxDistance,
            LineOfSightBlockerMask,
            _triggerInteraction)
            ? Mathf.Clamp(hit.distance, 0f, maxDistance)
            : maxDistance;
    }

    // Purpose: Checks whether inside vision range is currently true.
    public bool IsInsideVisionRange(Vector3 worldPoint)
    {
        Vector3 delta = worldPoint - VisualOrigin.position;
        delta.y = 0f;

        float range = VisionRange;
        return range > 0f && delta.sqrMagnitude <= range * range;
    }

    // Purpose: Resolves the refs needed by this component.
    protected virtual void ResolveRefs()
    {
        if (_origin == null)
            _origin = transform;

        if (_statsRuntime == null)
            _statsRuntime = EntityComponentResolver.Resolve<ShooterStatsRuntime>(this);

        if (_range == null)
            _range = EntityComponentResolver.Resolve<EntityRange>(this);

        if (_damageable == null)
            _damageable = EntityComponentResolver.Resolve<Damageable>(this);

        if (_statusEffects == null)
            _statusEffects = EntityComponentResolver.Resolve<EntityStatusEffectRuntime>(this);

        if (_statConfig == null && _statsRuntime != null)
            _statConfig = _statsRuntime.StatConfig;

        if (_statConfig == null && _damageable != null)
            _statConfig = _damageable.StatConfig;
    }

    // Purpose: Resolves the aim01 needed by this component.
    protected virtual float ResolveAim01()
    {
        return 0f;
    }

    // Purpose: Resolves the forward direction needed by this component.
    protected virtual Vector3 ResolveForwardDirection()
    {
        Vector3 forward = Origin.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.0001f)
            forward = transform.forward;

        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.0001f)
            return Vector3.forward;

        return forward.normalized;
    }

    // Purpose: Resolves the settings needed by this component.
    protected EntityVisionSettings ResolveSettings()
    {
        if (_statConfig != null)
            return _statConfig.Vision;

        if (_statsRuntime != null && _statsRuntime.StatConfig != null)
            return _statsRuntime.StatConfig.Vision;

        if (_damageable != null && _damageable.StatConfig != null)
            return _damageable.StatConfig.Vision;

        return null;
    }

    // Purpose: Resolves the vision range needed by this component.
    private float ResolveVisionRange()
    {
        float roomTraitMultiplier = ResolvePlayerRoomTraitVisionMultiplier();

        if (_statsRuntime != null)
        {
            float runtimeRange = _statsRuntime.VisionRange;

            if (runtimeRange > 0f)
                return runtimeRange * roomTraitMultiplier;
        }

        EntityVisionSettings settings = ResolveSettings();
        return settings != null ? settings.VisionRange * roomTraitMultiplier : 0f;
    }

    // Purpose: Resolves the close vision radius needed by this component.
    private float ResolveCloseVisionRadius()
    {
        EntityVisionSettings settings = ResolveSettings();
        return settings != null
            ? settings.CloseVisionRadius * ResolvePlayerRoomTraitVisionMultiplier()
            : 0f;
    }

    // Purpose: Resolves the player room trait vision multiplier needed by this component.
    private float ResolvePlayerRoomTraitVisionMultiplier()
    {
        return _statsRuntime is PlayerStatsRuntime
            ? SectorRoomTraitRuntime.CurrentPlayerVisionRangeMultiplier
            : 1f;
    }

    // Purpose: Checks whether inside flat range is currently true.
    private static bool IsInsideFlatRange(Vector3 origin, Vector3 worldPoint, float range)
    {
        if (range <= 0f)
            return false;

        Vector3 delta = worldPoint - origin;
        delta.y = 0f;

        return delta.sqrMagnitude <= range * range;
    }

    // Purpose: Checks whether inside forward arc is currently true.
    private bool IsInsideForwardArc(Vector3 worldPoint, Vector3 origin)
    {
        EntityVisionSettings settings = ResolveSettings();

        if (settings == null || !settings.UseForwardArc)
            return true;

        Vector3 toTarget = worldPoint - origin;
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

    // Purpose: Resolves the forward arc angle needed by this component.
    private float ResolveForwardArcAngle()
    {
        EntityVisionSettings settings = ResolveSettings();

        if (settings == null)
            return 360f;

        float arcAngle = settings.BaseArcAngle;
        float aim01 = Mathf.Clamp01(ResolveAim01());

        float multiplier = Mathf.Lerp(
            1f,
            settings.AimArcAngleMultiplier,
            aim01);

        float statusMultiplier = _statusEffects != null
            ? _statusEffects.GetMultiplier(StatusEffectModifierType.VisionArcMultiplier)
            : 1f;

        return Mathf.Clamp(arcAngle * multiplier * statusMultiplier, 1f, 360f);
    }

    // Purpose: Responds to the draw gizmos selected callback.
    private void OnDrawGizmosSelected()
    {
        if (!_drawVisionDebug)
            return;

        ResolveRefs();

        Vector3 origin = VisualOrigin.position;

        Gizmos.color = _closeVisionDebugColor;
        DrawFlatCircle(origin, CloseVisionRadius, 48);

        if (UsesForwardArc)
        {
            Gizmos.color = _forwardVisionDebugColor;
            DrawForwardArc(origin, ForwardDirection, ForwardArcAngle, VisionRange, 24);
        }
    }

    // Purpose: Draws the flat circle authoring UI.
    private static void DrawFlatCircle(Vector3 center, float radius, int segments)
    {
        if (radius <= 0f)
            return;

        Vector3 previous = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius);

            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }

    // Purpose: Draws the forward arc authoring UI.
    private static void DrawForwardArc(
        Vector3 center,
        Vector3 forward,
        float arcAngle,
        float range,
        int segments)
    {
        if (range <= 0f || forward.sqrMagnitude <= 0.0001f)
            return;

        forward.y = 0f;
        forward.Normalize();

        float half = Mathf.Clamp(arcAngle, 1f, 360f) * 0.5f;
        Vector3 left = Quaternion.AngleAxis(-half, Vector3.up) * forward;
        Vector3 right = Quaternion.AngleAxis(half, Vector3.up) * forward;

        Gizmos.DrawLine(center, center + left * range);
        Gizmos.DrawLine(center, center + right * range);

        Vector3 previous = center + left * range;

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(-half, half, t);
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            Vector3 next = center + dir * range;

            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
