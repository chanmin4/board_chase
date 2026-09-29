/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > Common
 * Primary responsibility: Handles EnemyShooterAimLaserController camera, vision, range, or aiming calculations and presentation.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(950)]
public class EnemyShooterAimLaserController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnemyShooterShoot _shoot;
    [SerializeField] private EnemyShooterCombatController _combat;
    [SerializeField] private Damageable _damageable;

    [Header("Visual")]
    [Tooltip("Shared unlit material used by every enemy aim LineRenderer. Keep this assigned so player builds do not depend on a runtime shader lookup.")]
    [SerializeField] private Material _laserMaterial;
    [SerializeField] private Color _laserColor = new Color(1f, 0.02f, 0.02f, 0.92f);
    [SerializeField, Min(0.001f)] private float _lineWidth = 0.05f;
    [SerializeField] private int _sortingOrder = 450;

    [Tooltip("When this enemy is aiming at the player, allow the full warning laser to render if any part of the laser reaches player vision even while the enemy body is hidden.")]
    [SerializeField] private bool _showPlayerTargetedLaserInsidePlayerVision = true;

    [Tooltip("Number of points sampled along a hidden enemy laser when deciding whether it enters player vision. The player endpoint is always included.")]
    [SerializeField, Range(2, 16)] private int _hiddenLaserVisionSamples = 6;

    [Header("Collision")]
    [Tooltip("Only these layers shorten the visual laser. Assign SectorStructure so the line stops at authored walls and cover.")]
    [SerializeField] private LayerMask _structureMask;

    private LineRenderer _lineRenderer;
    private bool _loggedMissingMaterial;
    private Vector3 _lastValidAimPoint;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
        EnsureLineRenderer();
        SetVisible(false);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        SetVisible(false);
    }

    // Purpose: Processes frame work that must run after the regular Update pass.
    private void LateUpdate()
    {
        ResolveRefs();

        if (!ShouldRender() ||
            !TryResolveAlertAimPoint(out Vector3 aimPoint) ||
            !_shoot.TryGetAimLaserRay(
                aimPoint,
                out Vector3 origin,
                out Vector3 direction,
                out float maxDistance))
        {
            SetVisible(false);
            return;
        }

        EnsureLineRenderer();

        if (_lineRenderer == null || _laserMaterial == null)
        {
            if (!_loggedMissingMaterial)
            {
                _loggedMissingMaterial = true;
                Debug.LogError("[EnemyShooterAimLaserController] Laser material is missing.", this);
            }

            SetVisible(false);
            return;
        }

        float distance = maxDistance;

        if (_structureMask.value != 0 &&
            Physics.Raycast(
                origin,
                direction,
                out RaycastHit hit,
                maxDistance,
                _structureMask,
                QueryTriggerInteraction.Ignore))
        {
            distance = hit.distance;
        }

        _lineRenderer.SetPosition(0, origin);
        Vector3 end = origin + direction * Mathf.Max(0.01f, distance);
        _lineRenderer.SetPosition(1, end);
        SetVisible(ShouldRenderForPlayerVision(origin, end));
    }

    // Purpose: Performs the should render workflow for this component.
    private bool ShouldRender()
    {
        if (_shoot == null || _combat == null ||
            (_damageable != null && _damageable.IsDead))
            return false;

        if (!_combat.IsAlerted)
        {
            _lastValidAimPoint = default;
            return false;
        }

        return true;
    }

    // Purpose: Performs the should render for player vision workflow for this component.
    private bool ShouldRenderForPlayerVision(Vector3 origin, Vector3 end)
    {
        VisionVisibilityTarget sourceVisibility =
            EntityComponentResolver.Resolve<VisionVisibilityTarget>(this);

        if (sourceVisibility == null || sourceVisibility.IsVisible)
            return true;

        if (!_showPlayerTargetedLaserInsidePlayerVision || !IsAimingAtPlayer())
            return false;

        EntityVisionController playerVision = VisionVisibilitySystem.Instance != null
            ? VisionVisibilitySystem.Instance.Viewer
            : null;

        if (playerVision == null)
            return false;

        int sampleCount = Mathf.Max(2, _hiddenLaserVisionSamples);
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            Vector3 samplePoint = Vector3.Lerp(origin, end, t);

            if (playerVision.CanRevealVisualPoint(samplePoint, 0f))
                return true;
        }

        return false;
    }

    // Purpose: Checks whether aiming at player is currently true.
    private bool IsAimingAtPlayer()
    {
        Transform target = _combat != null ? _combat.Target : null;

        return target != null &&
               (target.GetComponent<PlayerCharacter>() != null ||
                target.GetComponentInParent<PlayerCharacter>() != null ||
                target.GetComponentInChildren<PlayerCharacter>(true) != null);
    }

    // Purpose: Attempts to resolve alert aim point and reports whether the request succeeded.
    private bool TryResolveAlertAimPoint(out Vector3 aimPoint)
    {
        aimPoint = default;

        if (_combat == null)
            return false;

        if (_combat.TryGetCurrentAimPoint(out Vector3 combatAimPoint))
            return CacheAimPoint(combatAimPoint, out aimPoint);

        if (_combat.TryGetCurrentLookPoint(out Vector3 lookPoint))
            return CacheAimPoint(lookPoint, out aimPoint);

        // Brief state transitions must not blink the laser. Keep the last
        // meaningful alerted direction until alert is actually cleared.
        if (_lastValidAimPoint.sqrMagnitude >= 0.0001f)
        {
            aimPoint = _lastValidAimPoint;
            return true;
        }

        return CacheAimPoint(transform.position + transform.forward, out aimPoint);
    }

    // Purpose: Caches aim point for later reuse.
    private bool CacheAimPoint(Vector3 point, out Vector3 aimPoint)
    {
        if (!float.IsFinite(point.x) || !float.IsFinite(point.y) || !float.IsFinite(point.z))
        {
            aimPoint = default;
            return false;
        }

        _lastValidAimPoint = point;
        aimPoint = _lastValidAimPoint;
        return true;
    }

    // Purpose: Ensures line renderer is ready before the workflow continues.
    private void EnsureLineRenderer()
    {
        if (_lineRenderer != null)
            return;

        GameObject visual = new GameObject("EnemyAimLaser");
        visual.layer = gameObject.layer;
        visual.transform.SetParent(transform, false);

        _lineRenderer = visual.AddComponent<LineRenderer>();
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.loop = false;
        _lineRenderer.positionCount = 2;
        _lineRenderer.alignment = LineAlignment.View;
        _lineRenderer.textureMode = LineTextureMode.Stretch;
        _lineRenderer.numCapVertices = 0;
        _lineRenderer.numCornerVertices = 0;
        _lineRenderer.startWidth = Mathf.Max(0.001f, _lineWidth);
        _lineRenderer.endWidth = Mathf.Max(0.001f, _lineWidth);
        _lineRenderer.startColor = _laserColor;
        _lineRenderer.endColor = _laserColor;
        _lineRenderer.sortingOrder = _sortingOrder;
        _lineRenderer.sharedMaterial = _laserMaterial;

        VisionVisibilityTarget sourceVisibility =
            EntityComponentResolver.Resolve<VisionVisibilityTarget>(this);
        sourceVisibility?.ExcludeRendererFromVisionGate(_lineRenderer);
    }

    // Purpose: Updates the visible state.
    private void SetVisible(bool visible)
    {
        if (_lineRenderer != null)
            _lineRenderer.enabled = visible;
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_shoot == null)
            _shoot = EntityComponentResolver.Resolve<EnemyShooterShoot>(this);

        if (_combat == null)
            _combat = EntityComponentResolver.Resolve<EnemyShooterCombatController>(this);

        if (_damageable == null)
            _damageable = EntityComponentResolver.Resolve<Damageable>(this);
    }
}
