/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Handles ShooterMeleeHitZone combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShooterMeleeHitZone : MonoBehaviour
{
    private const float VisualHitForgivenessMultiplier = 1.2f;

    [Header("Collider")]
    [Tooltip(
        "Manually assigned melee-only BoxCollider. It always remains disabled; " +
        "ShooterMeleeAttack supplies the live dimensions and this component uses them for OverlapBox queries.")]
    [SerializeField] private BoxCollider _meleeCollider;

    private float _range = 2f;
    private float _radius = 0.5f;
    private float _arcAngle = 80f;
    private float _verticalOffset = 0.4f;
    private float _verticalSize = 1f;

    private Vector3 _lockedOrigin;
    private Vector3 _lockedDirection = Vector3.forward;
    private Vector3 _lockedCenter;
    private Vector3 _lockedHalfExtents;
    private Quaternion _lockedOrientation = Quaternion.identity;

    private bool _hasLockedPose;
    private bool _isHitWindowActive;

    public BoxCollider MeleeCollider => _meleeCollider;
    public float VerticalOffset => _verticalOffset;
    public bool IsConfigured => _meleeCollider != null;
    public bool IsHitWindowActive => _isHitWindowActive;
    public bool HasLockedPose => _hasLockedPose;
    public Vector3 LockedCenter => _lockedCenter;
    public Vector3 LockedSize => _lockedHalfExtents * 2f;
    public Quaternion LockedOrientation => _lockedOrientation;
    public float EffectiveRange => _range * VisualHitForgivenessMultiplier;
    public float EffectiveRadius => _radius * VisualHitForgivenessMultiplier;
    public float EffectiveArcAngle => Mathf.Min(
        180f,
        _arcAngle * VisualHitForgivenessMultiplier);

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_meleeCollider == null)
        {
            Debug.LogError(
                $"[ShooterMeleeHitZone] Melee BoxCollider is not assigned. " +
                $"owner={transform.root.name}",
                this);

            return;
        }

        ConfigureCollider();
    }

    // Purpose: Normalizes Inspector-authored data after it changes in the Editor.
    private void OnValidate()
    {
        if (_meleeCollider == null)
            return;

        _meleeCollider.isTrigger = true;
        _meleeCollider.enabled = false;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        _isHitWindowActive = false;
        _hasLockedPose = false;

        if (_meleeCollider != null)
            _meleeCollider.enabled = false;
    }

    // Purpose: Configures this component''s shape.
    public void ConfigureShape(
        float range,
        float radius,
        float arcAngle,
        float verticalOffset,
        float verticalSize)
    {
        _range = Mathf.Max(0.01f, range);
        _radius = Mathf.Max(0.01f, radius);
        _arcAngle = Mathf.Clamp(arcAngle, 1f, 180f);
        _verticalOffset = Mathf.Max(0f, verticalOffset);
        _verticalSize = Mathf.Max(0.01f, verticalSize);

        ConfigureCollider();
    }

    // Purpose: Begins the swing workflow.
    public void BeginSwing(
        Vector3 origin,
        Vector3 direction,
        float range,
        float radius,
        float arcAngle,
        float verticalOffset,
        float verticalSize)
    {
        if (_meleeCollider == null)
            return;

        ConfigureShape(
            range,
            radius,
            arcAngle,
            verticalOffset,
            verticalSize);

        NormalizeDirection(ref direction);
        LockWorldPose(origin, direction);
        _isHitWindowActive = false;
    }

    // Purpose: Updates the hit window active state.
    public void SetHitWindowActive(bool active)
    {
        _isHitWindowActive = active;

        // Only the fixed OverlapBox query is allowed to deal damage.
        if (_meleeCollider != null)
            _meleeCollider.enabled = false;
    }

    // Purpose: Performs the overlap workflow for this component.
    public int Overlap(
        Collider[] results,
        LayerMask targetMask,
        QueryTriggerInteraction triggerInteraction)
    {
        if (_meleeCollider == null ||
            !_hasLockedPose ||
            results == null ||
            results.Length == 0)
        {
            return 0;
        }

        return Physics.OverlapBoxNonAlloc(
            _lockedCenter,
            _lockedHalfExtents,
            results,
            _lockedOrientation,
            targetMask,
            triggerInteraction);
    }

    // Purpose: Resolves the raised origin needed by this component.
    public Vector3 ResolveRaisedOrigin(Vector3 origin)
    {
        return origin + Vector3.up * _verticalOffset;
    }

    // Purpose: Draws the gizmos authoring UI.
    public void DrawGizmos(Color idleColor, Color activeColor)
    {
        if (_meleeCollider == null)
            return;

        ConfigureCollider();

        Gizmos.color = _isHitWindowActive
            ? activeColor
            : idleColor;

        Matrix4x4 previousMatrix = Gizmos.matrix;

        if (_hasLockedPose)
        {
            Gizmos.matrix = Matrix4x4.TRS(
                _lockedCenter,
                _lockedOrientation,
                Vector3.one);

            Gizmos.DrawWireCube(Vector3.zero, _lockedHalfExtents * 2f);
        }
        else
        {
            Gizmos.matrix = Matrix4x4.TRS(
                _meleeCollider.transform.position,
                _meleeCollider.transform.rotation,
                _meleeCollider.transform.lossyScale);

            Gizmos.DrawWireCube(
                _meleeCollider.center,
                _meleeCollider.size);
        }

        Gizmos.matrix = previousMatrix;
        DrawAttackArc();
    }

    // Purpose: Configures this component''s collider.
    private void ConfigureCollider()
    {
        if (_meleeCollider == null)
            return;

        _meleeCollider.isTrigger = true;
        float maximumDistance = EffectiveRange + EffectiveRadius;
        float halfArcRadians = EffectiveArcAngle * 0.5f * Mathf.Deg2Rad;
        float halfWidth = Mathf.Max(
            EffectiveRadius,
            Mathf.Sin(halfArcRadians) * maximumDistance);

        // The disabled collider only supplies the candidate OverlapBox. It must
        // contain the whole fan so the precise filters do not clip slash edges.
        _meleeCollider.size = new Vector3(
            halfWidth * 2f,
            _verticalSize,
            maximumDistance);

        _meleeCollider.center = new Vector3(
            0f,
            _verticalOffset,
            maximumDistance * 0.5f);

        _meleeCollider.enabled = false;
    }

    // Purpose: Performs the lock world pose workflow for this component.
    private void LockWorldPose(Vector3 origin, Vector3 direction)
    {
        Quaternion attackRotation =
            Quaternion.LookRotation(direction, Vector3.up);

        Vector3 hitZoneScale = Abs(transform.lossyScale);
        Vector3 colliderScale = Abs(_meleeCollider.transform.lossyScale);

        Vector3 currentWorldCenter =
            _meleeCollider.transform.TransformPoint(_meleeCollider.center);

        Vector3 localCenterFromHitZone =
            transform.InverseTransformPoint(currentWorldCenter);

        Quaternion colliderRotationOffset =
            Quaternion.Inverse(transform.rotation) *
            _meleeCollider.transform.rotation;

        _lockedOrigin = origin;
        _lockedDirection = direction;
        _lockedCenter = origin + attackRotation * Vector3.Scale(
            localCenterFromHitZone,
            hitZoneScale);

        _lockedHalfExtents = Vector3.Scale(
            _meleeCollider.size * 0.5f,
            colliderScale);

        _lockedOrientation = attackRotation * colliderRotationOffset;
        _hasLockedPose = true;
    }

    // Purpose: Draws the attack arc authoring UI.
    private void DrawAttackArc()
    {
        Vector3 origin = _hasLockedPose
            ? ResolveRaisedOrigin(_lockedOrigin)
            : transform.position + Vector3.up * _verticalOffset;

        Vector3 direction = _hasLockedPose
            ? _lockedDirection
            : transform.forward;

        NormalizeDirection(ref direction);

        float effectiveDistance = EffectiveRange + EffectiveRadius;
        float halfArc = EffectiveArcAngle * 0.5f;
        Vector3 left = Quaternion.AngleAxis(-halfArc, Vector3.up) * direction;
        Vector3 right = Quaternion.AngleAxis(halfArc, Vector3.up) * direction;

        Gizmos.DrawLine(origin, origin + left * effectiveDistance);
        Gizmos.DrawLine(origin, origin + right * effectiveDistance);

        const int ArcSegments = 20;
        Vector3 previous = origin + left * effectiveDistance;

        for (int i = 1; i <= ArcSegments; i++)
        {
            float angle = Mathf.Lerp(
                -halfArc,
                halfArc,
                i / (float)ArcSegments);

            Vector3 arcDirection =
                Quaternion.AngleAxis(angle, Vector3.up) * direction;

            Vector3 next = origin + arcDirection * effectiveDistance;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }

    // Purpose: Normalizes direction to a safe authored value.
    private static void NormalizeDirection(ref Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector3.forward;

        direction.Normalize();
    }

    // Purpose: Performs the abs workflow for this component.
    private static Vector3 Abs(Vector3 value)
    {
        return new Vector3(
            Mathf.Abs(value.x),
            Mathf.Abs(value.y),
            Mathf.Abs(value.z));
    }
}
