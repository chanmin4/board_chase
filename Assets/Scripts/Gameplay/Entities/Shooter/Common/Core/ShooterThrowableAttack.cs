/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Handles ShooterThrowableAttack combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public class ShooterThrowableAttack : MonoBehaviour
{
    private const float MinimumFlightSeconds = 0.15f;
    private const float MinimumRollSecondsAfterLanding = 0.3f;
    private const int EnemySafeArcSampleCount = 18;
    private const int EnemySafeArcHighArcAttempts = 2;
    private const float EnemySafeArcRadius = 0.16f;
    private const float EnemySafeArcLandingAcceptance01 = 0.88f;
    private const float EnemySafeArcMinimumVerticalVelocity = 9f;
    private const float EnemySafeArcVerticalVelocityStep = 5f;

    [Header("Refs")]
    [SerializeField] private ShooterEquipmentController _equipmentController;
    [SerializeField] private ShooterDashController _dashController;
    [SerializeField] private EntityShooterCharacterAudio _audio;
    [SerializeField] private PlayerQuickSlotRuntime _playerQuickSlots;
    [SerializeField] private EnemyLootInventoryRuntime _enemyInventory;
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Tooltip("Shared AI-hearing event channel used by throwable flyby, bounce, and explosion stimuli.")]
    [SerializeField] private SoundStimulusEventChannelSO _soundStimulusEvent;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs;

    private MaskRenderManager _maskRenderManager;
    private float _nextThrowTime;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset() => ResolveRefs();

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake() => ResolveRefs();

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveRefs();

        if (_maskRenderManagerReadyChannel == null)
            return;

        _maskRenderManagerReadyChannel.OnEventRaised += HandleMaskRenderManagerReady;
        _maskRenderManager = _maskRenderManagerReadyChannel.Current;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.OnEventRaised -= HandleMaskRenderManagerReady;
    }

    // Purpose: Attempts to throw usable and reports whether the request succeeded.
    public bool TryThrowUsable(
        ShooterFaction faction,
        ThrowableUsableItemSO usable,
        Vector3 aimPoint)
    {
        return TryThrowUsableInternal(
            faction,
            usable,
            aimPoint,
            false,
            out _);
    }

    /// <summary>
    /// Enemy-only entry point. It validates the full ballistic route before a
    /// carried throwable is consumed and retries with a higher arc when cover
    /// blocks the normal route. Player direct throws keep their current behavior.
    /// </summary>
    public bool TryThrowEnemyUsableWithSafeArc(
        ThrowableUsableItemSO usable,
        Vector3 aimPoint,
        out string failureReason)
    {
        return TryThrowUsableInternal(
            ShooterFactionUtility.Resolve(this),
            usable,
            aimPoint,
            true,
            out failureReason);
    }

    /// <summary>적 투척 후보의 경로를 미리 검사합니다. 소지품을 소비하거나 발사체/View를 생성하지 않습니다.</summary>
    public bool CanThrowEnemyUsableWithSafeArc(ThrowableUsableItemSO usable, Vector3 aimPoint, out string failureReason)
    {
        failureReason = null;
        var stamina = EntityComponentResolver.Resolve<ShooterStaminaRuntime>(this);
        if (stamina != null && stamina.IsSprinting)
        { failureReason = "cannot throw while sprinting"; return false; }
        if (usable == null || usable.HeldViewPrefab == null || GamePause.IsPaused || Time.timeScale <= 0f ||
            (_dashController != null && _dashController.IsDashing) || Time.time < _nextThrowTime)
        {
            failureReason = "throw unavailable";
            return false;
        }
        ResolveRefs();
        return TryResolveSafeEnemyThrowSolution(usable, aimPoint, out _, out _, out _, out _, out _, out failureReason, false);
    }

    // Purpose: Attempts to throw usable internal and reports whether the request succeeded.
    private bool TryThrowUsableInternal(
        ShooterFaction faction,
        ThrowableUsableItemSO usable,
        Vector3 aimPoint,
        bool requireSafeEnemyArc,
        out string failureReason)
    {
        failureReason = null;
        var stamina = EntityComponentResolver.Resolve<ShooterStaminaRuntime>(this);
        if (stamina != null && stamina.IsSprinting)
        { failureReason = "cannot throw while sprinting"; return false; }

        if (usable == null)
        {
            Debug.LogError("[ThrowableDebug] Throw rejected: usable item is null.", this);
            failureReason = "usable item is null";
            return false;
        }

        if (GamePause.IsPaused || Time.timeScale <= 0f)
        {
            Debug.LogWarning(
                $"[ThrowableDebug] Throw rejected: game is paused. usable={usable.name}",
                this);
            failureReason = "game is paused";
            return false;
        }

        if (faction == ShooterFaction.Player &&
            GameplayActionInputBlocker.IsAttackBlocked)
        {
            Debug.LogWarning(
                $"[ThrowableDebug] Throw rejected: gameplay attack input is blocked. usable={usable.name}",
                this);
            failureReason = "gameplay attack input is blocked";
            return false;
        }

        if (_dashController != null && _dashController.IsDashing)
        {
            Debug.LogWarning(
                $"[ThrowableDebug] Throw rejected: shooter is dashing. usable={usable.name}",
                this);
            failureReason = "shooter is dashing";
            return false;
        }

        if (Time.time < _nextThrowTime)
        {
            Debug.LogWarning(
                $"[ThrowableDebug] Throw rejected: cooldown. usable={usable.name}, " +
                $"remaining={_nextThrowTime - Time.time:0.000}s",
                this);
            failureReason = "throw cooldown";
            return false;
        }

        ResolveRefs();

        bool hasSolution;
        Vector3 origin;
        Vector3 targetPoint;
        Vector3 velocity;
        float flightSeconds;
        bool detonateImmediately;

        if (requireSafeEnemyArc)
        {
            hasSolution = TryResolveSafeEnemyThrowSolution(
                usable,
                aimPoint,
                out origin,
                out targetPoint,
                out velocity,
                out flightSeconds,
                out detonateImmediately,
                out failureReason);
        }
        else
        {
            hasSolution = TryResolveThrowSolution(
                usable,
                aimPoint,
                out origin,
                out targetPoint,
                out velocity,
                out flightSeconds,
                out detonateImmediately);
        }

        if (!hasSolution)
        {
            failureReason ??= "throw solution failed";
            Debug.LogWarning(
                $"[ThrowableDebug] Throw rejected before inventory consumption. usable={usable.name}, " +
                $"aim={aimPoint}, reason={failureReason}",
                this);
            return false;
        }

        WeaponView viewPrefab = usable.HeldViewPrefab;

        if (viewPrefab == null)
        {
            Debug.LogError(
                $"[ShooterThrowableAttack] Held View Prefab is missing. usable={usable.name}",
                usable);
            failureReason = "held view prefab is missing";
            return false;
        }

        Vector3 flatDirection = targetPoint - origin;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude <= 0.0001f)
            flatDirection = transform.forward;

        flatDirection.y = 0f;
        flatDirection.Normalize();

        // Rigidbody projectiles must simulate in world space. Parenting here
        // converted the correct world launch point into a local trajectory and
        // made the throwable explode in a different sector.
        Vector3 launchPosition =
            detonateImmediately ? targetPoint : origin;
        Quaternion launchRotation =
            Quaternion.LookRotation(flatDirection, Vector3.up);
        WeaponView releasedView = Instantiate(
            viewPrefab,
            launchPosition,
            launchRotation);
        releasedView.transform.localScale = usable.ResolveHeldViewScale();

        ThrowableProjectile projectile =
            releasedView.GetComponent<ThrowableProjectile>() ??
            releasedView.GetComponentInChildren<ThrowableProjectile>(true);

        if (projectile == null)
        {
            Debug.LogError(
                $"[ShooterThrowableAttack] Throwable held view requires ThrowableProjectile. " +
                $"usable={usable.name}, view={viewPrefab.name}",
                viewPrefab);
            Destroy(releasedView.gameObject);
            failureReason = "held view has no ThrowableProjectile";
            return false;
        }

        if (!TryConsumeThrowable(faction, usable, out string consumeMessage))
        {
            if (!string.IsNullOrWhiteSpace(consumeMessage))
                Debug.LogWarning($"[ShooterThrowableAttack] {consumeMessage}", this);

            Destroy(releasedView.gameObject);
            failureReason = string.IsNullOrWhiteSpace(consumeMessage)
                ? "inventory consumption failed"
                : consumeMessage;
            return false;
        }

        projectile.Launch(
            usable,
            velocity,
            targetPoint,
            gameObject,
            faction,
            _maskRenderManager,
            _audio,
            _soundStimulusEvent,
            flightSeconds + MinimumRollSecondsAfterLanding,
            detonateImmediately);

        _audio?.PlayThrowableThrow(usable);
        _nextThrowTime = Time.time + Mathf.Max(0.01f, usable.FuseSeconds * 0.15f);

        Debug.Log(
            $"[ThrowableDebug] Launched. faction={faction}, usable={usable.name}, " +
            $"origin={origin}, target={targetPoint}, velocity={velocity}, " +
            $"flight={flightSeconds:0.000}s, immediate={detonateImmediately}, " +
            $"maskManager={_maskRenderManager != null}",
            this);

        return true;
    }

    // Purpose: Attempts to get prediction and reports whether the request succeeded.
    public bool TryGetPrediction(
        ThrowableUsableItemSO usable,
        Vector3 aimPoint,
        out Vector3 predictedImpactPoint,
        out float explosionRadius)
    {
        predictedImpactPoint = default;
        explosionRadius = 0f;

        if (usable == null)
            return false;

        predictedImpactPoint = aimPoint;
        explosionRadius = usable.ExplosionRadius;
        return true;
    }

    // Purpose: Attempts to resolve throw solution and reports whether the request succeeded.
    private bool TryResolveThrowSolution(
        ThrowableUsableItemSO usable,
        Vector3 aimPoint,
        out Vector3 origin,
        out Vector3 targetPoint,
        out Vector3 velocity,
        out float flightSeconds,
        out bool detonateImmediately)
    {
        origin = ResolveThrowOrigin();
        targetPoint = aimPoint;
        velocity = Vector3.zero;
        flightSeconds = MinimumFlightSeconds;
        detonateImmediately = false;

        if (usable == null)
            return false;

        detonateImmediately = usable.ShouldDetonateImmediately(origin, targetPoint);

        if (detonateImmediately)
        {
            flightSeconds = 0f;
            return true;
        }

        Vector3 flatOffset = targetPoint - origin;
        flatOffset.y = 0f;

        float naturalFlightSeconds = ResolveNaturalFlightSeconds(
            origin.y,
            targetPoint.y,
            usable.UpwardVelocity);
        float horizontalFlightSeconds = flatOffset.magnitude / usable.ThrowSpeed;
        flightSeconds = Mathf.Max(
            MinimumFlightSeconds,
            naturalFlightSeconds,
            horizontalFlightSeconds);

        Vector3 horizontalVelocity = flatOffset / flightSeconds;
        float verticalVelocity =
            (targetPoint.y - origin.y -
             0.5f * Physics.gravity.y * flightSeconds * flightSeconds) /
            flightSeconds;

        velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        return true;
    }

    // Purpose: Attempts to resolve safe enemy throw solution and reports whether the request succeeded.
    private bool TryResolveSafeEnemyThrowSolution(
        ThrowableUsableItemSO usable,
        Vector3 aimPoint,
        out Vector3 origin,
        out Vector3 targetPoint,
        out Vector3 velocity,
        out float flightSeconds,
        out bool detonateImmediately,
        out string failureReason,
        bool logHighArc = true)
    {
        failureReason = null;

        if (!TryResolveThrowSolution(
                usable,
                aimPoint,
                out origin,
                out targetPoint,
                out velocity,
                out flightSeconds,
                out detonateImmediately))
        {
            failureReason = "base ballistic solution failed";
            return false;
        }

        if (detonateImmediately ||
            !TryFindEnemyArcBlocker(
                origin,
                velocity,
                flightSeconds,
                out Collider blocker,
                out Vector3 blockerPoint,
                out float blockerProgress01))
        {
            return true;
        }

        string normalArcBlocker = DescribeArcBlocker(
            blocker,
            blockerPoint,
            blockerProgress01);

        for (int attempt = 0; attempt < EnemySafeArcHighArcAttempts; attempt++)
        {
            if (!TryResolveHighArcThrowSolution(
                    usable,
                    origin,
                    targetPoint,
                    attempt,
                    out Vector3 highArcVelocity,
                    out float highArcFlightSeconds))
            {
                continue;
            }

            if (TryFindEnemyArcBlocker(
                    origin,
                    highArcVelocity,
                    highArcFlightSeconds,
                    out blocker,
                    out blockerPoint,
                    out blockerProgress01))
            {
                continue;
            }

            velocity = highArcVelocity;
            flightSeconds = highArcFlightSeconds;

            if (logHighArc) Debug.Log(
                $"[ThrowableDebug] Enemy safe throw selected high arc. item={usable.name}, " +
                $"origin={origin}, target={targetPoint}, flight={flightSeconds:0.000}s, " +
                $"velocity={velocity}, blockedNormal={normalArcBlocker}",
                this);
            return true;
        }

        failureReason =
            $"normal arc blocked ({normalArcBlocker}) and no clear high arc was found";
        return false;
    }

    // Purpose: Attempts to resolve high arc throw solution and reports whether the request succeeded.
    private static bool TryResolveHighArcThrowSolution(
        ThrowableUsableItemSO usable,
        Vector3 origin,
        Vector3 targetPoint,
        int attempt,
        out Vector3 velocity,
        out float flightSeconds)
    {
        velocity = Vector3.zero;
        flightSeconds = 0f;

        if (usable == null)
            return false;

        float upwardVelocity = Mathf.Max(
            EnemySafeArcMinimumVerticalVelocity,
            usable.UpwardVelocity + EnemySafeArcVerticalVelocityStep * (attempt + 1));
        flightSeconds = ResolveNaturalFlightSeconds(
            origin.y,
            targetPoint.y,
            upwardVelocity);

        if (flightSeconds <= 0f)
            return false;

        Vector3 flatOffset = targetPoint - origin;
        flatOffset.y = 0f;

        Vector3 horizontalVelocity = flatOffset / flightSeconds;
        float verticalVelocity =
            (targetPoint.y - origin.y -
             0.5f * Physics.gravity.y * flightSeconds * flightSeconds) /
            flightSeconds;

        velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        return true;
    }

    // Purpose: Attempts to find enemy arc blocker and reports whether the request succeeded.
    private bool TryFindEnemyArcBlocker(
        Vector3 origin,
        Vector3 velocity,
        float flightSeconds,
        out Collider blocker,
        out Vector3 blockerPoint,
        out float blockerProgress01)
    {
        blocker = null;
        blockerPoint = default;
        blockerProgress01 = 0f;

        if (flightSeconds <= 0f)
            return false;

        Vector3 previousPoint = origin;

        for (int sample = 1; sample <= EnemySafeArcSampleCount; sample++)
        {
            float currentProgress01 = sample / (float)EnemySafeArcSampleCount;
            float time = flightSeconds * currentProgress01;
            Vector3 currentPoint =
                origin + velocity * time + 0.5f * Physics.gravity * time * time;
            Vector3 segment = currentPoint - previousPoint;
            float segmentLength = segment.magnitude;

            if (segmentLength > 0.0001f)
            {
                RaycastHit[] hits = Physics.SphereCastAll(
                    previousPoint,
                    EnemySafeArcRadius,
                    segment / segmentLength,
                    segmentLength,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);

                float closestProgress01 = float.PositiveInfinity;

                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit hit = hits[i];
                    Collider hitCollider = hit.collider;

                    if (hitCollider == null || IsIgnoredEnemyArcCollider(hitCollider))
                        continue;

                    float hitProgress01 =
                        ((sample - 1) + hit.distance / segmentLength) /
                        EnemySafeArcSampleCount;

                    // Hitting the intended target or floor immediately before the
                    // planned landing is safe. Any earlier collision would cause a
                    // bounce and make a timed-fuse grenade land unpredictably.
                    if (hitProgress01 >= EnemySafeArcLandingAcceptance01)
                        continue;

                    if (hitProgress01 >= closestProgress01)
                        continue;

                    closestProgress01 = hitProgress01;
                    blocker = hitCollider;
                    blockerPoint = hit.point;
                    blockerProgress01 = hitProgress01;
                }

                if (blocker != null)
                    return true;
            }

            previousPoint = currentPoint;
        }

        return false;
    }

    // Purpose: Checks whether ignored enemy arc collider is currently true.
    private bool IsIgnoredEnemyArcCollider(Collider collider)
    {
        return collider != null &&
               (collider.transform == transform || collider.transform.IsChildOf(transform));
    }

    // Purpose: Performs the describe arc blocker workflow for this component.
    private static string DescribeArcBlocker(
        Collider blocker,
        Vector3 point,
        float progress01)
    {
        if (blocker == null)
            return "unknown obstruction";

        return $"{blocker.name} at {point} ({progress01:P0} of flight)";
    }

    // Purpose: Resolves the natural flight seconds needed by this component.
    private static float ResolveNaturalFlightSeconds(
        float originY,
        float targetY,
        float initialUpwardVelocity)
    {
        float acceleration = 0.5f * Physics.gravity.y;
        float heightOffset = originY - targetY;
        float discriminant =
            initialUpwardVelocity * initialUpwardVelocity -
            4f * acceleration * heightOffset;

        if (discriminant < 0f || Mathf.Abs(acceleration) <= 0.0001f)
            return MinimumFlightSeconds;

        float sqrt = Mathf.Sqrt(discriminant);
        float denominator = 2f * acceleration;
        float first = (-initialUpwardVelocity + sqrt) / denominator;
        float second = (-initialUpwardVelocity - sqrt) / denominator;

        return Mathf.Max(MinimumFlightSeconds, Mathf.Max(first, second));
    }

    // Purpose: Attempts to consume throwable and reports whether the request succeeded.
    private bool TryConsumeThrowable(
        ShooterFaction faction,
        ThrowableUsableItemSO usable,
        out string message)
    {
        if (faction == ShooterFaction.Player)
        {
            if (_playerQuickSlots == null)
            {
                message = "Player quick-slot runtime is missing.";
                return false;
            }

            return _playerQuickSlots.TryConsumeUsable(usable, 1, out message);
        }

        if (_enemyInventory == null)
        {
            message = "Enemy carried inventory is missing.";
            return false;
        }

        return _enemyInventory.TryConsumeCarriedItem(usable, 1, out message);
    }

    // Purpose: Resolves the throw origin needed by this component.
    private Vector3 ResolveThrowOrigin()
    {
        if (_playerQuickSlots != null && _playerQuickSlots.ActiveUsableFireOrigin != null)
            return _playerQuickSlots.ActiveUsableFireOrigin.position;

        if (_equipmentController != null && _equipmentController.GameplayFireOrigin != null)
            return _equipmentController.GameplayFireOrigin.position;

        return transform.position + Vector3.up;
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_equipmentController == null)
            _equipmentController = EntityComponentResolver.Resolve<ShooterEquipmentController>(this);

        if (_dashController == null)
            _dashController = EntityComponentResolver.Resolve<ShooterDashController>(this);

        if (_audio == null)
            _audio = EntityComponentResolver.Resolve<EntityShooterCharacterAudio>(this);

        if (_playerQuickSlots == null)
            _playerQuickSlots = EntityComponentResolver.Resolve<PlayerQuickSlotRuntime>(this);

        if (_enemyInventory == null)
            _enemyInventory = EntityComponentResolver.Resolve<EnemyLootInventoryRuntime>(this);

        if (_soundStimulusEvent == null)
        {
            PlayerShooterShoot playerShoot =
                EntityComponentResolver.Resolve<PlayerShooterShoot>(this);

            if (playerShoot != null)
                _soundStimulusEvent = playerShoot.SoundStimulusEventChannel;
        }

        if (_soundStimulusEvent == null)
        {
            EnemyShooterShoot enemyShoot =
                EntityComponentResolver.Resolve<EnemyShooterShoot>(this);

            if (enemyShoot != null)
                _soundStimulusEvent = enemyShoot.SoundStimulusEventChannel;
        }
    }

    // Purpose: Handles the incoming mask render manager ready request or event.
    private void HandleMaskRenderManagerReady(MaskRenderManager manager)
    {
        _maskRenderManager = manager;
    }
}
