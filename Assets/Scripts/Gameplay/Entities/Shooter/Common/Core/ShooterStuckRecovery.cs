/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Implements the focused ShooterStuckRecovery responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Resolves persistent solid penetration without fighting normal movement.
/// Players are repositioned through CharacterController disable/enable, while
/// enemies remain owned by NavMeshAgent and are moved with Warp.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(200)]
public sealed class ShooterStuckRecovery : MonoBehaviour
{
    private const float RecoveryLift = 0.04f;
    private const int RequiredPenetrationChecks = 3;

    [SerializeField, Min(0.05f)] private float _checkInterval = 0.15f;
    [SerializeField, Min(0.001f)] private float _minimumPenetration = 0.025f;
    [SerializeField, Min(0.05f)] private float _maximumRecoveryDistance = 1f;

    private Transform _entityRoot;
    private Collider _bodyCollider;
    private CharacterController _characterController;
    private NavMeshAgent _agent;
    private Damageable _damageable;
    private SectorRuntime _sector;
    private float _nextCheckTime;
    private int _penetrationCheckCount;
    private int _floorLayer;

    // Purpose: Ensures on is ready before the workflow continues.
    public static ShooterStuckRecovery EnsureOn(Component source)
    {
        Transform root = EntityComponentResolver.ResolveEntityRoot(source);
        if (root == null)
            return null;

        ShooterStuckRecovery recovery = root.GetComponent<ShooterStuckRecovery>();
        return recovery != null ? recovery : root.gameObject.AddComponent<ShooterStuckRecovery>();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
    }

    // Purpose: Processes frame work that must run after the regular Update pass.
    private void LateUpdate()
    {
        if (Time.unscaledTime < _nextCheckTime)
            return;

        _nextCheckTime = Time.unscaledTime + Mathf.Max(0.05f, _checkInterval);
        ResolveRefs();

        if (_bodyCollider == null || !_bodyCollider.enabled ||
            (_damageable != null && _damageable.IsDead))
        {
            _penetrationCheckCount = 0;
            return;
        }

        if (!TryResolvePenetration(out Vector3 separation, out float deepest))
        {
            _penetrationCheckCount = 0;
            return;
        }

        _penetrationCheckCount++;
        if (_penetrationCheckCount < RequiredPenetrationChecks)
            return;

        _penetrationCheckCount = 0;
        Vector3 planar = Vector3.ProjectOnPlane(separation, Vector3.up);

        if (planar.sqrMagnitude <= 0.0001f && _sector != null)
            planar = _sector.GetWorldBounds().center - _entityRoot.position;

        if (planar.sqrMagnitude <= 0.0001f)
            planar = _entityRoot.right;

        float distance = Mathf.Clamp(
            deepest + 0.12f,
            0.05f,
            Mathf.Max(0.05f, _maximumRecoveryDistance));
        Vector3 preferred = _entityRoot.position + planar.normalized * distance;

        if (_sector != null)
            preferred = ClampInsideSector(preferred, _sector, ResolveRadius());

        if (!TryResolveSafePosition(_entityRoot, _sector, preferred, out Vector3 safe))
            safe = preferred + Vector3.up * RecoveryLift;

        PlaceEntity(_entityRoot, safe, _characterController, _agent);
    }

    // Purpose: Attempts to resolve penetration and reports whether the request succeeded.
    private bool TryResolvePenetration(out Vector3 separation, out float deepest)
    {
        separation = Vector3.zero;
        deepest = 0f;
        Bounds bounds = _bodyCollider.bounds;
        Collider[] overlaps = Physics.OverlapBox(
            bounds.center,
            bounds.extents + Vector3.one * 0.02f,
            Quaternion.identity,
            ~0,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider other = overlaps[i];
            if (!IsBlockingCollider(other, _entityRoot, _floorLayer))
                continue;

            if (!Physics.ComputePenetration(
                    _bodyCollider,
                    _bodyCollider.transform.position,
                    _bodyCollider.transform.rotation,
                    other,
                    other.transform.position,
                    other.transform.rotation,
                    out Vector3 direction,
                    out float distance) ||
                distance < _minimumPenetration)
            {
                continue;
            }

            direction.y = 0f;
            separation += direction * distance;
            deepest = Mathf.Max(deepest, distance);
        }

        return deepest >= _minimumPenetration;
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_entityRoot == null)
            _entityRoot = EntityComponentResolver.ResolveEntityRoot(transform);

        if (_characterController == null)
            _characterController = EntityComponentResolver.Resolve<CharacterController>(this);

        if (_agent == null)
            _agent = EntityComponentResolver.Resolve<NavMeshAgent>(this);

        if (_damageable == null)
            _damageable = EntityComponentResolver.Resolve<Damageable>(this);

        if (_bodyCollider == null)
        {
            _bodyCollider = _characterController;

            if (_bodyCollider == null && _entityRoot != null)
            {
                Collider[] colliders = _entityRoot.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null && !colliders[i].isTrigger)
                    {
                        _bodyCollider = colliders[i];
                        break;
                    }
                }
            }
        }

        if (_entityRoot != null)
        {
            SectorRuntime currentSector = _entityRoot.GetComponentInParent<SectorRuntime>();
            if (currentSector != null)
                _sector = currentSector;
        }

        if (_floorLayer == 0)
            _floorLayer = LayerMask.NameToLayer("SectorFloor");
    }

    // Purpose: Resolves the radius needed by this component.
    private float ResolveRadius()
    {
        if (_characterController != null)
            return Mathf.Max(0.1f, _characterController.radius);

        return _bodyCollider != null
            ? Mathf.Max(0.1f, Mathf.Min(_bodyCollider.bounds.extents.x, _bodyCollider.bounds.extents.z))
            : 0.35f;
    }

    // Purpose: Attempts to resolve safe position and reports whether the request succeeded.
    public static bool TryResolveSafePosition(
        Transform entityRoot,
        SectorRuntime sector,
        Vector3 preferred,
        out Vector3 safePosition)
    {
        safePosition = preferred;
        if (entityRoot == null || sector == null)
            return false;

        CharacterController controller =
            EntityComponentResolver.Resolve<CharacterController>(entityRoot);
        float radius = controller != null ? Mathf.Max(0.1f, controller.radius) : 0.35f;
        float height = controller != null
            ? Mathf.Max(radius * 2f, controller.height)
            : 1.2f;
        float centerY = controller != null ? controller.center.y : height * 0.5f;
        int floorLayer = LayerMask.NameToLayer("SectorFloor");
        Bounds sectorBounds = sector.GetWorldBounds();

        for (int ring = 0; ring <= 4; ring++)
        {
            int samples = ring == 0 ? 1 : 8;
            float ringRadius = ring * Mathf.Max(1.25f, radius * 3f);

            for (int i = 0; i < samples; i++)
            {
                float angle = samples == 1 ? 0f : i / (float)samples * Mathf.PI * 2f;
                Vector3 candidate = preferred + new Vector3(
                    Mathf.Cos(angle) * ringRadius,
                    0f,
                    Mathf.Sin(angle) * ringRadius);
                candidate = ClampInsideSector(candidate, sector, radius + 0.1f);

                float feetToRoot = Mathf.Max(0f, height * 0.5f - centerY);
                if (TryFindSupportingGround(
                        candidate,
                        sectorBounds,
                        entityRoot,
                        floorLayer,
                        out RaycastHit floorHit))
                {
                    candidate.y = floorHit.point.y + feetToRoot + RecoveryLift;
                }
                else
                {
                    // The CharacterController root is at its center in the Player
                    // prefab. Placing that root directly on floor Y starts the
                    // controller half buried and lets the downward locomotion step
                    // pass through the floor after a debug teleport.
                    candidate.y = sectorBounds.center.y + feetToRoot + RecoveryLift;
                }

                Vector3 capsuleCenter = candidate + Vector3.up * centerY;
                float halfLine = Mathf.Max(0f, height * 0.5f - radius);
                Vector3 bottom = capsuleCenter - Vector3.up * halfLine;
                Vector3 top = capsuleCenter + Vector3.up * halfLine;
                Collider[] overlaps = Physics.OverlapCapsule(
                    bottom,
                    top,
                    radius * 0.92f,
                    ~0,
                    QueryTriggerInteraction.Ignore);

                bool blocked = false;
                for (int overlapIndex = 0; overlapIndex < overlaps.Length; overlapIndex++)
                {
                    if (IsBlockingCollider(overlaps[overlapIndex], entityRoot, floorLayer))
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                {
                    safePosition = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    // Purpose: Attempts to find supporting ground and reports whether the request succeeded.
    private static bool TryFindSupportingGround(
        Vector3 candidate,
        Bounds sectorBounds,
        Transform entityRoot,
        int floorLayer,
        out RaycastHit groundHit)
    {
        Vector3 origin = new(
            candidate.x,
            sectorBounds.max.y + 6f,
            candidate.z);
        float distance = sectorBounds.size.y + 12f;
        int floorMask = floorLayer >= 0 ? 1 << floorLayer : 0;

        if (floorMask != 0 && Physics.Raycast(
                origin,
                Vector3.down,
                out groundHit,
                distance,
                floorMask,
                QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        // Some authored sector boards are not on SectorFloor. Use the lowest
        // upward-facing solid surface as a safe fallback instead of assuming Y=0.
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            Vector3.down,
            distance,
            ~0,
            QueryTriggerInteraction.Ignore);
        bool found = false;
        groundHit = default;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            Collider collider = hit.collider;
            if (collider == null || !collider.enabled || collider.isTrigger || hit.normal.y < 0.6f)
                continue;

            if (entityRoot != null && collider.transform.IsChildOf(entityRoot))
                continue;

            if (hit.point.y < sectorBounds.min.y - 0.5f ||
                hit.point.y > sectorBounds.max.y + 0.5f)
            {
                continue;
            }

            if (!found || hit.point.y < groundHit.point.y)
            {
                found = true;
                groundHit = hit;
            }
        }

        return found;
    }

    // Purpose: Performs the place entity workflow for this component.
    public static void PlaceEntity(
        Transform entityRoot,
        Vector3 worldPosition,
        CharacterController controller = null,
        NavMeshAgent agent = null)
    {
        if (entityRoot == null)
            return;

        if (agent == null)
            agent = EntityComponentResolver.Resolve<NavMeshAgent>(entityRoot);

        if (agent != null && agent.enabled)
        {
            if (NavMesh.SamplePosition(worldPosition, out NavMeshHit hit, 2f, agent.areaMask))
            {
                if (agent.isOnNavMesh)
                {
                    agent.ResetPath();
                    agent.velocity = Vector3.zero;
                }

                agent.Warp(hit.position + Vector3.up * RecoveryLift);
                return;
            }
        }

        if (controller == null)
            controller = EntityComponentResolver.Resolve<CharacterController>(entityRoot);

        bool controllerWasEnabled = controller != null && controller.enabled;
        if (controllerWasEnabled)
            controller.enabled = false;

        entityRoot.position = worldPosition;

        if (controllerWasEnabled)
        {
            Physics.SyncTransforms();
            controller.enabled = true;
            controller.Move(Vector3.down * RecoveryLift);
        }
    }

    // Purpose: Checks whether blocking collider is currently true.
    private static bool IsBlockingCollider(
        Collider collider,
        Transform entityRoot,
        int floorLayer)
    {
        if (collider == null || !collider.enabled || collider.isTrigger)
            return false;

        if (entityRoot != null && collider.transform.IsChildOf(entityRoot))
            return false;

        if (floorLayer >= 0 && collider.gameObject.layer == floorLayer)
            return false;

        if (collider.GetComponentInParent<SectorPortal>() != null)
            return false;

        return true;
    }

    // Purpose: Performs the clamp inside sector workflow for this component.
    private static Vector3 ClampInsideSector(
        Vector3 position,
        SectorRuntime sector,
        float margin)
    {
        if (sector == null)
            return position;

        Bounds bounds = sector.GetWorldBounds();
        margin = Mathf.Max(0f, margin);
        position.x = Mathf.Clamp(position.x, bounds.min.x + margin, bounds.max.x - margin);
        position.z = Mathf.Clamp(position.z, bounds.min.z + margin, bounds.max.z - margin);
        return position;
    }
}
