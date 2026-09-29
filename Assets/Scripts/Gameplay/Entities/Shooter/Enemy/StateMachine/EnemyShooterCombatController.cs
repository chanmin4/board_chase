/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > StateMachine
 * Primary responsibility: Implements the focused EnemyShooterGoal responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyShooterGoal
{
    Patrol,
    Investigate,
    ChaseTarget,
    AttackTarget
}

public enum EnemyShooterInvestigationType
{
    None,
    Sound,
    LastKnownTarget
}

public enum EnemyShooterAttackPhase
{
    None,
    FiringBurst,
    BurstCooldown
}

public enum EnemyShooterCoverTacticPhase
{
    None,
    MovingToCover,
    Hidden,
    MovingToPeek,
    Peeking,
    ReturningToCover
}

[DisallowMultipleComponent]
/*
 * Refactor map:
 * - This file remains the orchestration owner for awareness, navigation,
 *   tactics, ranged/melee/throwable actions, and the live FSM-facing API.
 * - EnemyShooterCombatController.Diagnostics owns only Debug Monitor snapshot
 *   reads and runtime-only test overrides. It must not become another combat
 *   decision path or change shared config assets.
 * - Keep existing prefab references on this component while extracting one
 *   behavior domain at a time; do not replace it with parallel controllers.
 */
public partial class EnemyShooterCombatController : MonoBehaviour
{
    private const float IncomingThrowableSafetyPadding = 2f;
    private const float IncomingThrowableCheckInterval = 0.05f;
    private const float IncomingThrowableMinimumEscapeDistance = 3f;
    private const int IncomingThrowableEscapeSampleCount = 16;
    private const float IncomingBulletCheckInterval = 0.02f;
    private const float IncomingBulletPredictionSeconds = 0.45f;
    private const float FailedThrowableRetrySeconds = 1f;

    [Header("Refs")]
    [SerializeField] private EnemyShooterShoot _shoot;
    [SerializeField] private EnemyShooterStatsRuntime _statsRuntime;
    [SerializeField] private EntityVisionController _vision;
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private Damageable _damageable;
    [SerializeField] private ShooterEffectController _effects;
    [SerializeField] private EnemyShooterDashController _dashController;
    [SerializeField] private ShooterEquipmentController _equipmentController;
    [SerializeField] private EnemyLootInventoryRuntime _lootInventory;
    [SerializeField] private ShooterMeleeAttack _meleeAttack;
    [SerializeField] private ShooterThrowableAttack _throwableAttack;
    [SerializeField] private EntityStatusEffectRuntime _statusEffectRuntime;

    [Header("Optional Events")]
    [Tooltip("Optional global hit event. If assigned, this shooter alerts when this object receives a hit event.")]
    [SerializeField] private HitReceivedEventChannelSO _hitReceivedEvent;
    [SerializeField] private SoundStimulusEventChannelSO _soundStimulusEvent;

    [Header("Runtime Don't Touch")]
    [ReadOnly] [SerializeField] private Transform _target;
    [ReadOnly] [SerializeField] private bool _isAlerted;
    [ReadOnly] [SerializeField] private bool _canSeeTarget;
    [ReadOnly] [SerializeField] private bool _hasLastKnownPosition;
    [ReadOnly] [SerializeField] private Vector3 _lastKnownPosition;
    [ReadOnly] [SerializeField] private float _lastSeenTime = -1f;
    [ReadOnly] [SerializeField] private float _targetLockUntilTime = -1f;
    [ReadOnly] [SerializeField] private bool _attackActive;
    [ReadOnly] [SerializeField] private bool _hasSoundInvestigation;
    [ReadOnly] [SerializeField] private Vector3 _soundInvestigationPosition;
    [ReadOnly] [SerializeField] private float _soundInvestigationMoveTime;
    [ReadOnly] [SerializeField] private SoundStimulusType _soundInvestigationStimulusType = SoundStimulusType.Unknown;
    [ReadOnly] [SerializeField] private int _soundInvestigationPriority;
    [ReadOnly] [SerializeField] private int _soundInvestigationEmitterId;
    [ReadOnly] [SerializeField] private EnemyShooterGoal _currentGoal = EnemyShooterGoal.Patrol;
    [ReadOnly] [SerializeField] private EnemyShooterInvestigationType _currentInvestigationType = EnemyShooterInvestigationType.None;
    [ReadOnly] [SerializeField] private EnemyShooterAttackPhase _attackPhase = EnemyShooterAttackPhase.None;
    [ReadOnly] [SerializeField] private bool _patrolDestinationReached;
    [ReadOnly] [SerializeField] private bool _investigationArrived;
    [ReadOnly] [SerializeField] private bool _hasQueuedSoundInvestigation;
    [ReadOnly] [SerializeField] private SoundStimulusType _queuedSoundInvestigationStimulusType = SoundStimulusType.Unknown;
    [ReadOnly] [SerializeField] private int _queuedSoundInvestigationPriority;
    [ReadOnly] [SerializeField] private string _lastSoundStimulusResult = "None";
    [ReadOnly] [SerializeField] private float _lastSoundStimulusDistance = -1f;
    [ReadOnly] [SerializeField] private float _lastSoundStimulusRadius;
    [ReadOnly] [SerializeField] private bool _isEvadingIncomingThrowable;
    [ReadOnly] [SerializeField] private int _incomingThrowableId;
    [ReadOnly] [SerializeField] private Vector3 _incomingThrowableDangerCenter;
    [ReadOnly] [SerializeField] private float _incomingThrowableDangerRadius;
    [ReadOnly] [SerializeField] private Vector3 _incomingThrowableEscapeDestination;
    [ReadOnly] [SerializeField] private EnemyShooterCoverPoint _reservedCoverPoint;
    [ReadOnly] [SerializeField] private EnemyShooterCoverPoint _runtimeAutoCoverPoint;
    [ReadOnly] [SerializeField] private EnemyShooterCoverTacticPhase _coverTacticPhase;
    [ReadOnly] [SerializeField] private Vector3 _coverPeekDestination;

    [Header("Debug")]
    [SerializeField] private bool _debugMovement;
    [SerializeField, Min(0.1f)] private float _debugMovementInterval = 1f;

    [Tooltip("Logs sound-stimulus distance, priority, emitter ID, and exact accept/reject reason. This can be verbose during automatic fire.")]
    [SerializeField] private bool _debugSoundStimuli;

    private Damageable _targetDamageable;
    private Rigidbody _targetRigidbody;
    private Transform _targetAimAnchor;
    private Vector3 _homePosition;
    private Vector3 _patrolDestination;
    private bool _hasPatrolDestination;
    private float _nextPatrolRefreshTime;
    private float _patrolIdleUntil;
    private float _nextTargetSearchTime;
    private float _reactionStartedTime = -1f;
    private float _activeReactionDelaySeconds;
    private bool _hasSeenTargetDuringCurrentEngagement;
    private float _nextFireTime;
    [ReadOnly] [SerializeField] private int _burstShotsRemaining;
    [ReadOnly] [SerializeField] private float _nextBurstShotTime;
    [ReadOnly] [SerializeField] private bool _hasFiredCurrentBurst;
    private float _nextCombatMoveRefreshTime;
    private float _lastKnownSetTime = -1f;
    private float _soundInvestigationSetTime = -1f;
    private Vector3 _soundInvestigationDirection;
    private float _investigationFinishTime;
    private float _ignoreInvestigationUntilTime = -1f;
    private Vector3 _queuedSoundInvestigationPosition;
    private float _queuedSoundInvestigationMoveTime;
    private float _queuedSoundInvestigationSetTime = -1f;
    private int _queuedSoundInvestigationEmitterId;
    private Vector3 _queuedSoundInvestigationDirection;
    private float _nextInvestigationRetargetTime = -1f;
    private int _attackStrafeSign = 1;
    [ReadOnly] [SerializeField] private EnemyShooterTacticalIntent _tacticalIntent;
    [ReadOnly] [SerializeField] private Vector3 _tacticalDestination;
    private Transform _tacticalIntentTarget;
    private float _nextTacticalIntentRefreshTime;
    private float _coverPhaseEndsAt;
    private Vector3 _aimDirection;
    private float _lastHealth;
    private float _nextHarmfulAreaCheckTime;
    private float _nextIncomingThrowableCheckTime;
    private float _nextIncomingBulletCheckTime;
    private int _lastIncomingBulletThreatId;
    private float _nextMovementDebugTime;
    private float _nextMeleeAllowedTime;
    private float _nextCombatWeaponSwitchTime;
    private float _meleeActionFinishTime;
    private bool _meleeActionActive;
    private float _nextThrowableAllowedTime;
    private float _throwableActionFinishTime;
    private bool _throwableActionActive;
    private NavMeshPath _navigationPath;
    private bool _hasNavigationRequest;
    private Vector3 _lastNavigationRequest;
    private Vector3 _lastReachableDestination;
    private float _lastNavigationStoppingDistance;
    private float _nextNavigationRetryTime;
    private Vector3 _lastChaseProgressPosition;
    private float _lastChaseProgressTime;
    private readonly Dictionary<long, float> _soundStimulusDuplicateUntil = new();
    private readonly List<long> _expiredSoundStimulusKeys = new();
    private float _nextSoundStimulusCleanupTime;
    private int _muzzleClearanceFrame = -1;
    private bool _muzzleClearanceResult;
    private bool _wasStatusActionBlocked;

    // DebugMonitor changes these values only on the live spawned enemy. They are
    // intentionally non-serialized so authoring config assets stay untouched.
    private bool _debugCombatOverridesEnabled;
    private bool _debugUseTacticalIntent;
    private bool _debugUseCoverTactics;
    private bool _debugFireWhileMovingToCover;
    private bool _debugUseDuelToken;
    private bool _debugUseFlankingTactics;
    private bool _debugUseDash;
    private bool _debugDashOnDamaged;
    private bool _debugDashWhenHealthLow;
    private bool _debugUseThrowableAttack;
    private bool _debugUsePreferredRangeBeforeStationaryAttack;
    private float _debugCoverSearchRadius;
    private float _debugCoverHideSeconds;
    private float _debugCoverPeekSeconds;
    private float _debugCoverApproachSpeedMultiplier;
    private float _debugTacticalFlankDistance;
    private float _debugDashChanceOnDamaged;
    private float _debugDashHealthThreshold;
    private float _debugDashCooldownSeconds;
    private float _debugDashDurationSeconds;
    private float _debugDashSpeed;
    private float _debugStationaryAttackKeepRangePadding;

    // StatusEffectDefinitionSO is extended independently from the enemy FSM.
    // Reflection keeps old prefabs and intermediate script revisions compiling,
    // while the ID fallback makes the authored stun asset work immediately.
    private static readonly System.Reflection.PropertyInfo RuntimeBlocksActionsProperty =
        typeof(EntityStatusEffectRuntime).GetProperty("BlocksActions") ??
        typeof(EntityStatusEffectRuntime).GetProperty("BlocksAllActions") ??
        typeof(EntityStatusEffectRuntime).GetProperty("IsActionBlocked");
    private static readonly System.Reflection.PropertyInfo DefinitionBlocksActionsProperty =
        typeof(StatusEffectDefinitionSO).GetProperty("BlocksActions") ??
        typeof(StatusEffectDefinitionSO).GetProperty("BlocksAllActions") ??
        typeof(StatusEffectDefinitionSO).GetProperty("IsActionBlocked");

    public EnemyShooterConfigSO Config => _shoot != null ? _shoot.Config : null;
    public Transform Target => _target;
    public bool HasTarget => _target != null && (_targetDamageable == null || !_targetDamageable.IsDead);
    public bool IsAlerted => _isAlerted && (HasTarget || _hasSoundInvestigation || _hasLastKnownPosition);
    public bool IsEngaged =>
        HasTarget &&
        (_currentGoal == EnemyShooterGoal.ChaseTarget ||
         _currentGoal == EnemyShooterGoal.AttackTarget);
    public bool CanSeeTarget => HasTarget && _canSeeTarget;
    public bool IsTargetLocked => HasTarget && Time.time < _targetLockUntilTime;
    public bool CanTrackTargetForCombat => CanSeeTarget || IsTargetLocked;
    public bool HasLastKnownPosition => _hasLastKnownPosition;
    public Vector3 LastKnownPosition => _lastKnownPosition;
    public bool HasSoundInvestigation => _hasSoundInvestigation;
    public Vector3 SoundInvestigationPosition => _soundInvestigationPosition;
    public bool IsDashing => _dashController != null && _dashController.IsDashing;
    public bool HasRuntimeCombatOverrides => _debugCombatOverridesEnabled;
    public EnemyShooterGoal CurrentGoal => _currentGoal;
    public EnemyShooterInvestigationType CurrentInvestigationType => _currentInvestigationType;
    public EnemyShooterAttackPhase AttackPhase => _attackPhase;
    public bool IsAttackActive => _attackActive;
    public float TargetPlanarDistance => HasTarget ? GetPlanarDistanceToTarget() : -1f;
    public float VisionRange =>
        _vision != null
            ? _vision.VisionRange
            : _statsRuntime != null
                ? _statsRuntime.VisionRange
                : Config != null && Config.Vision != null
                    ? Config.Vision.VisionRange
                    : 0f;
    public float CloseVisionRadius => _vision != null ? _vision.CloseVisionRadius : 0f;
    public float VisionArcAngle => _vision != null ? _vision.ForwardArcAngle : 360f;
    public bool UsesForwardVisionArc => _vision != null && _vision.UsesForwardArc;
    public bool UsesVisionLineOfSight => _vision != null && _vision.UsesLineOfSight;
    public float AttackStartRange => ResolveDirectAttackRange(false);
    public float AttackKeepRange => ResolveDirectAttackRange(true);
    public float ActiveAttackRange => _attackActive ? AttackKeepRange : AttackStartRange;
    public float AttackCooldownRemaining =>
        Mathf.Max(0f, Mathf.Max(_nextFireTime, _nextBurstShotTime) - Time.time);
    public int BurstShotsRemaining => Mathf.Max(0, _burstShotsRemaining);
    public float ReactionTimeRemaining =>
        _reactionStartedTime >= 0f
            ? Mathf.Max(0f, _reactionStartedTime + _activeReactionDelaySeconds - Time.time)
            : 0f;
    public float TargetLockRemaining => Mathf.Max(0f, _targetLockUntilTime - Time.time);
    public float LastSeenSecondsAgo =>
        _lastSeenTime >= 0f ? Mathf.Max(0f, Time.time - _lastSeenTime) : -1f;
    public float LastKnownSecondsAgo =>
        _lastKnownSetTime >= 0f ? Mathf.Max(0f, Time.time - _lastKnownSetTime) : -1f;
    public float SoundReactionDelayRemaining =>
        _hasSoundInvestigation
            ? Mathf.Max(0f, _soundInvestigationMoveTime - Time.time)
            : 0f;
    public string LastSoundStimulusResult => _lastSoundStimulusResult;
    public float LastSoundStimulusDistance => _lastSoundStimulusDistance;
    public float LastSoundStimulusRadius => _lastSoundStimulusRadius;
    public string AttackDebugStatus => ResolveAttackDebugStatus();
    public bool IsActionBlockedByStatusEffect => ResolveStatusActionBlocked();
    public bool DebugCombatOverridesEnabled => _debugCombatOverridesEnabled;
    public bool EffectiveUseTacticalIntent =>
        _debugCombatOverridesEnabled
            ? _debugUseTacticalIntent
            : Config != null && Config.UseTacticalIntent;
    public bool EffectiveUseCoverTactics =>
        EffectiveUseTacticalIntent &&
        (_debugCombatOverridesEnabled
            ? _debugUseCoverTactics
            : Config != null && Config.UseCoverTactics);
    public bool EffectiveFireWhileMovingToCover =>
        _debugCombatOverridesEnabled &&
        _debugFireWhileMovingToCover &&
        EffectiveUseCoverTactics;
    public float EffectiveCoverApproachSpeedMultiplier =>
        EffectiveFireWhileMovingToCover
            ? Mathf.Clamp(_debugCoverApproachSpeedMultiplier, 0.1f, 1f)
            : 1f;
    public bool EffectiveUseDuelToken =>
        EffectiveUseTacticalIntent &&
        (_debugCombatOverridesEnabled
            ? _debugUseDuelToken
            : Config != null && Config.UseDuelToken);
    public bool EffectiveUseFlankingTactics =>
        EffectiveUseTacticalIntent &&
        (_debugCombatOverridesEnabled
            ? _debugUseFlankingTactics
            : Config != null && Config.UseFlankingTactics);
    public bool EffectiveUseDash =>
        _debugCombatOverridesEnabled
            ? _debugUseDash
            : Config != null && Config.UseDash;
    public bool EffectiveUseThrowableAttack =>
        _debugCombatOverridesEnabled
            ? _debugUseThrowableAttack
            : Config != null && Config.UseThrowableAttack;
    public bool EffectiveUsePreferredRangeBeforeStationaryAttack =>
        _debugCombatOverridesEnabled
            ? _debugUsePreferredRangeBeforeStationaryAttack
            : Config != null && Config.UsePreferredRangeBeforeStationaryAttack;
    public float EffectiveStationaryAttackKeepRangePadding =>
        _debugCombatOverridesEnabled
            ? _debugStationaryAttackKeepRangePadding
            : Config != null ? Config.StationaryAttackKeepRangePadding : 0f;
    public bool EffectiveDashOnIncomingProjectile =>
        EffectiveUseDash &&
        (_debugCombatOverridesEnabled
            ? _debugDashOnDamaged
            : Config != null && Config.DashOnIncomingProjectile);
    public bool EffectiveDashWhenHealthLow =>
        EffectiveUseDash &&
        (_debugCombatOverridesEnabled
            ? _debugDashWhenHealthLow
            : Config != null && Config.DashWhenHealthBelowThreshold);
    public float EffectiveDashCooldownSeconds =>
        _debugCombatOverridesEnabled
            ? _debugDashCooldownSeconds
            : Config != null ? Config.DashCooldownSeconds : 0f;
    public float EffectiveDashDurationSeconds =>
        _debugCombatOverridesEnabled
            ? _debugDashDurationSeconds
            : Config != null ? Config.DashDurationSeconds : 0.18f;
    public float EffectiveDashSpeed =>
        _debugCombatOverridesEnabled
            ? _debugDashSpeed
            : Config != null ? Config.DashSpeed : 14f;
    public float EffectiveCoverSearchRadius =>
        _debugCombatOverridesEnabled
            ? _debugCoverSearchRadius
            : Config != null ? Config.CoverSearchRadius : 0.5f;
    public float EffectiveCoverHideSeconds =>
        _debugCombatOverridesEnabled
            ? _debugCoverHideSeconds
            : Config != null ? Config.CoverHideSeconds : 0f;
    public float EffectiveCoverPeekSeconds =>
        _debugCombatOverridesEnabled
            ? _debugCoverPeekSeconds
            : Config != null ? Config.CoverPeekSeconds : 0.05f;
    public float EffectiveTacticalFlankDistance =>
        _debugCombatOverridesEnabled
            ? _debugTacticalFlankDistance
            : Config != null ? Config.TacticalFlankDistance : 0.1f;
    public float EffectiveDashChanceOnIncomingProjectile =>
        _debugCombatOverridesEnabled
            ? _debugDashChanceOnDamaged
            : Config != null ? Config.DashChanceOnIncomingProjectile : 0f;
    public float EffectiveDashHealthThreshold =>
        _debugCombatOverridesEnabled
            ? _debugDashHealthThreshold
            : Config != null ? Config.DashHealthNormalizedThreshold : 0f;
    public EnemyShooterTacticalIntent TacticalIntent => _tacticalIntent;
    public EnemyShooterCoverTacticPhase CoverTacticPhase => _coverTacticPhase;
    public bool HasReservedCover => _reservedCoverPoint != null;
    public bool HasRuntimeAutoCoverPoint => _runtimeAutoCoverPoint != null;
    public bool HoldsDuelToken =>
        HasTarget &&
        EnemyShooterTacticalIntentSelector.IsDuelHolder(this, _target);
    public Vector3 TacticalDestination => _tacticalDestination;
    public float ThrowableCooldownRemaining => Mathf.Max(0f, _nextThrowableAllowedTime - Time.time);
    public bool HasCarriedThrowable =>
        _lootInventory != null &&
        _lootInventory.TryGetFirstCarriedItem(out ThrowableUsableItemSO _);

    // Purpose: Attempts to get current aim direction and reports whether the request succeeded.
    public bool TryGetCurrentAimDirection(out Vector3 aimDirection)
    {
        aimDirection = default;

        if (!HasTarget || _target == null)
            return false;

        aimDirection = _aimDirection;
        aimDirection.y = 0f;

        if (aimDirection.sqrMagnitude < 0.0001f)
        {
            aimDirection = ResolveTargetPoint() - transform.position;
            aimDirection.y = 0f;
        }

        if (aimDirection.sqrMagnitude < 0.0001f)
            return false;

        aimDirection.Normalize();
        return true;
    }

    // Purpose: Attempts to get current aim point and reports whether the request succeeded.
    public bool TryGetCurrentAimPoint(out Vector3 aimPoint)
    {
        aimPoint = default;

        if (!HasTarget || _target == null)
            return false;

        aimPoint = ResolveTargetPoint();
        return true;
    }

    // Purpose: Attempts to get current look point and reports whether the request succeeded.
    public bool TryGetCurrentLookPoint(out Vector3 lookPoint)
    {
        lookPoint = default;

        if (_currentGoal == EnemyShooterGoal.AttackTarget ||
            _currentGoal == EnemyShooterGoal.ChaseTarget)
        {
            if (HasTarget && _target != null)
            {
                lookPoint = _target.position;
                return true;
            }

            if (_hasLastKnownPosition)
            {
                lookPoint = _lastKnownPosition;
                return true;
            }
        }

        if (_currentGoal == EnemyShooterGoal.Investigate &&
            _currentInvestigationType == EnemyShooterInvestigationType.Sound &&
            _investigationArrived &&
            TryNormalizePlanar(-_soundInvestigationDirection, out Vector3 incomingDirection))
        {
            lookPoint = transform.position + incomingDirection;
            return true;
        }

        if (_currentGoal == EnemyShooterGoal.Investigate &&
            TryResolveInvestigationDestination(out lookPoint))
        {
            return true;
        }

        if (TryGetCurrentMoveDirection(out Vector3 moveDirection))
        {
            lookPoint = transform.position + moveDirection;
            return true;
        }

        return false;
    }

    // Purpose: Attempts to get current move direction and reports whether the request succeeded.
    public bool TryGetCurrentMoveDirection(out Vector3 direction)
    {
        direction = default;

        if (CanUseAgent())
        {
            if (TryNormalizePlanar(_agent.desiredVelocity, out direction))
                return true;

            if (TryNormalizePlanar(_agent.velocity, out direction))
                return true;

            if (_agent.hasPath &&
                TryNormalizePlanar(_agent.steeringTarget - transform.position, out direction))
            {
                return true;
            }
        }

        if (_currentGoal == EnemyShooterGoal.Patrol &&
            _hasPatrolDestination &&
            TryNormalizePlanar(_patrolDestination - transform.position, out direction))
        {
            return true;
        }

        if (_currentGoal == EnemyShooterGoal.Investigate &&
            TryResolveInvestigationDestination(out Vector3 investigationDestination) &&
            TryNormalizePlanar(investigationDestination - transform.position, out direction))
        {
            return true;
        }

        return false;
    }

    public bool CanAttackTarget
    {
        get
        {
            if (_isEvadingIncomingThrowable || IsActionBlockedByStatusEffect)
                return false;

            if (!HasTarget)
                return false;

            // The AttackTarget state is exclusively a ranged state. Without a
            // usable ranged weapon and compatible runtime bullet, do not keep a
            // melee-only enemy in the stationary firing/cover flow.
            if (IsTargetInsideConfiguredMeleeRange() ||
                !TryActivateRangedWeaponForCombat() ||
                !CanUseCurrentRangedWeapon())
            {
                return false;
            }

            return CanDirectlyAttackTarget ||
                   CanMaintainCoverTactic ||
                   CanFinishStartedBurstAfterSightLost ||
                   CanHoldBurstCooldownAfterSightLost;
        }
    }

    private bool HasActiveCoverTactic =>
        _reservedCoverPoint != null &&
        _coverTacticPhase != EnemyShooterCoverTacticPhase.None;

    private bool CanMaintainCoverTactic
    {
        get
        {
            if (!HasActiveCoverTactic || !HasTarget || Config == null)
                return false;

            return GetPlanarDistanceToTarget() <= ResolveAttackKeepRange();
        }
    }

    private bool CanDirectlyAttackTarget
    {
        get
        {
            if (!HasTarget || !CanSeeTarget || Config == null)
                return false;

            // A melee-capable enemy owns close range. This prevents a hybrid enemy
            // from changing back to its firearm while it is already inside its
            // authored strike distance, including the short melee cooldown window.
            if (IsTargetInsideConfiguredMeleeRange())
                return false;

            if (!TryActivateRangedWeaponForCombat() || !CanUseCurrentRangedWeapon())
                return false;

            float range = _attackActive ? AttackKeepRange : AttackStartRange;
            return GetPlanarDistanceToTarget() <= range && HasClearMuzzlePathToTarget();
        }
    }

    // Purpose: Checks whether use current ranged weapon is currently permitted.
    private bool CanUseCurrentRangedWeapon()
    {
        if (_shoot == null || _shoot.CurrentBullet == null)
            return false;

        if (_equipmentController == null ||
            _equipmentController.CurrentWeapon is not RangedWeaponSO rangedWeapon ||
            !rangedWeapon.IsCompatibleWith(_shoot.CurrentBullet))
        {
            return false;
        }

        EntityEquipmentRuntime equipmentRuntime = _equipmentController.EquipmentRuntime;
        return equipmentRuntime == null || equipmentRuntime.CanFireCurrentWeapon;
    }

    private bool UsesConfiguredCombatWeaponSlots =>
        Config != null &&
        Config.AutoSwitchCombatWeaponSlots &&
        _equipmentController != null;

    // Purpose: Attempts to activate ranged weapon for combat and reports whether the request succeeded.
    private bool TryActivateRangedWeaponForCombat()
    {
        if (_equipmentController == null)
            return false;

        if (UsesConfiguredCombatWeaponSlots)
        {
            int slotIndex = Config.RangedWeaponSlotIndex;
            if (_equipmentController.GetWeapon(slotIndex) is not RangedWeaponSO)
                return false;

            if (!TryActivateConfiguredCombatWeaponSlot(slotIndex))
                return false;
        }

        return _equipmentController.CurrentWeapon is RangedWeaponSO;
    }

    // Purpose: Attempts to activate melee weapon for combat and reports whether the request succeeded.
    private bool TryActivateMeleeWeaponForCombat()
    {
        if (_equipmentController == null)
            return false;

        if (UsesConfiguredCombatWeaponSlots)
        {
            int slotIndex = Config.MeleeWeaponSlotIndex;
            if (_equipmentController.GetWeapon(slotIndex) is not MeleeWeaponSO)
                return false;

            if (!TryActivateConfiguredCombatWeaponSlot(slotIndex))
                return false;
        }

        return _equipmentController.CurrentWeapon is MeleeWeaponSO ||
               _equipmentController.CurrentWeapon == null;
    }

    // Purpose: Checks whether the configured combat slot can be activated without bypassing a hybrid enemy's weapon-switch cooldown.
    private bool CanActivateConfiguredCombatWeaponSlot(int slotIndex)
    {
        return _equipmentController != null &&
               (_equipmentController.ActiveWeaponSlotIndex == slotIndex ||
                Time.time >= _nextCombatWeaponSwitchTime);
    }

    // Purpose: Activates one configured combat slot and starts the authored switch cooldown only after an actual slot change.
    private bool TryActivateConfiguredCombatWeaponSlot(int slotIndex)
    {
        if (_equipmentController == null)
            return false;

        if (_equipmentController.ActiveWeaponSlotIndex == slotIndex)
            return true;

        if (!CanActivateConfiguredCombatWeaponSlot(slotIndex))
            return false;

        _equipmentController.SetActiveWeaponSlot(slotIndex);
        if (_equipmentController.ActiveWeaponSlotIndex != slotIndex)
            return false;

        _nextCombatWeaponSwitchTime = Time.time +
            (Config != null ? Config.CombatWeaponSwitchCooldownSeconds : 0f);
        return true;
    }

    // Purpose: Attempts to get configured melee range and reports whether the request succeeded.
    private bool TryGetConfiguredMeleeRange(out float range)
    {
        range = 0f;

        if (_meleeAttack == null || Config == null)
            return false;

        if (UsesConfiguredCombatWeaponSlots)
        {
            if (_equipmentController.GetWeapon(Config.MeleeWeaponSlotIndex) is not MeleeWeaponSO meleeWeapon)
                return false;

            range = Mathf.Max(0f, meleeWeapon.Range);
            return range > 0f;
        }

        if (!_meleeAttack.CanAttackCurrentWeaponOrUnarmed)
            return false;

        range = Mathf.Max(0f, _meleeAttack.CurrentAttackRange);
        return range > 0f;
    }

    // Purpose: Checks whether target inside configured melee range is currently true.
    private bool IsTargetInsideConfiguredMeleeRange()
    {
        if (Config == null || !Config.UseMeleeAttack || !HasTarget)
            return false;

        return TryGetConfiguredMeleeRange(out float range) &&
               GetPlanarDistanceToTarget() <= range + Config.MeleeRangePadding;
    }

    private bool CanFinishStartedBurstAfterSightLost
    {
        get
        {
            EnemyShooterConfigSO config = Config;
            return config != null &&
                   config.FinishStartedBurstAfterSightLost &&
                   HasTarget &&
                   (!CanSeeTarget || !HasClearMuzzlePathToTarget()) &&
                   _attackActive &&
                   _attackPhase == EnemyShooterAttackPhase.FiringBurst &&
                   _hasFiredCurrentBurst &&
                   _burstShotsRemaining > 0 &&
                   GetPlanarDistanceToTarget() <= AttackKeepRange;
        }
    }

    private bool CanHoldBurstCooldownAfterSightLost
    {
        get
        {
            EnemyShooterConfigSO config = Config;
            return config != null &&
                   config.FinishStartedBurstAfterSightLost &&
                   HasTarget &&
                   (!CanSeeTarget || !HasClearMuzzlePathToTarget()) &&
                   _attackActive &&
                   _attackPhase == EnemyShooterAttackPhase.BurstCooldown &&
                   Time.time < _nextFireTime;
        }
    }

    public bool ShouldAttack => CanAttackTarget;

    public bool ShouldChaseTarget
    {
        get
        {
            if (IsActionBlockedByStatusEffect || !HasTarget)
                return false;

            if (CanAttackTarget)
                return false;

            // A remembered target is not a live tracking source. Once direct vision
            // and the minimum target lock expire, investigate the stored clue rather
            // than chasing the target's current Transform through cover.
            return CanSeeTarget || IsTargetLocked || ShouldMaintainTacticalPressure;
        }
    }

    public bool ShouldChase => ShouldChaseTarget || ShouldInvestigate;
    public bool HasInvestigation =>
        Time.time >= _ignoreInvestigationUntilTime &&
        ResolveInvestigationType() != EnemyShooterInvestigationType.None;
    public bool ShouldInvestigate =>
        !IsActionBlockedByStatusEffect &&
        !CanAttackTarget &&
        !CanSeeTarget &&
        HasInvestigation &&
        !InvestigationFinished;
    public bool InvestigationFinished => _investigationArrived && Time.time >= _investigationFinishTime;
    public bool ShouldPatrol =>
        !IsActionBlockedByStatusEffect &&
        !CanAttackTarget &&
        !CanSeeTarget &&
        !ShouldChaseTarget &&
        !HasInvestigation &&
        Config != null &&
        Config.PatrolRadius > 0f;
    public bool PatrolIdleFinished => Time.time >= _patrolIdleUntil;
    public bool PatrolDestinationReached => _patrolDestinationReached;
    public bool TargetLost => _isAlerted && !HasInvestigation && (!HasTarget || HasForgottenTarget());
    public bool ShouldUseMelee =>
        !IsActionBlockedByStatusEffect &&
        !_isEvadingIncomingThrowable &&
        CanUseMeleeAttack();
    public bool MeleeActionFinished => _meleeActionActive && Time.time >= _meleeActionFinishTime;
    public bool ShouldUseThrowable =>
        !IsActionBlockedByStatusEffect &&
        !_isEvadingIncomingThrowable &&
        CanUseThrowableAttack();
    public bool ThrowableActionFinished =>
        !_throwableActionActive || Time.time >= _throwableActionFinishTime;

    // Purpose: Resolves the attack debug status needed by this component.
    private string ResolveAttackDebugStatus()
    {
        if (_damageable != null && _damageable.IsDead)
            return "Dead";

        if (IsActionBlockedByStatusEffect)
            return "Status effect blocks actions";

        if (!HasTarget)
            return HasInvestigation ? "Investigating" : "No target";

        float targetDistance = TargetPlanarDistance;

        if (!CanSeeTarget)
        {
            if (CanFinishStartedBurstAfterSightLost)
                return $"Finishing burst ({_burstShotsRemaining})";

            if (CanHoldBurstCooldownAfterSightLost)
                return $"Post-burst cooldown ({AttackCooldownRemaining:0.00}s)";

            if (_vision != null && _target != null)
            {
                bool insideCloseRange = targetDistance <= CloseVisionRadius;
                bool insideForwardRange = _vision.IsInsideVisionRange(_target.position);

                if (!insideCloseRange && !insideForwardRange)
                    return "Out of vision";

                if (!insideCloseRange && UsesForwardVisionArc)
                {
                    Vector3 toTarget = _target.position - _vision.VisualOrigin.position;
                    toTarget.y = 0f;

                    if (toTarget.sqrMagnitude > 0.0001f &&
                        Vector3.Angle(_vision.ForwardDirection, toTarget) > VisionArcAngle * 0.5f)
                    {
                        return "Outside FOV";
                    }
                }

                if (UsesVisionLineOfSight)
                {
                    Collider blocker = _vision.LastLineOfSightBlocker;
                    return blocker != null
                        ? $"LOS: {blocker.name}"
                        : "LOS blocked";
                }

                return "Cannot see target";
            }

            return "Cannot see target";
        }

        if (targetDistance > ActiveAttackRange)
            return "Out of attack range";

        if (!HasClearMuzzlePathToTarget())
            return "Muzzle path blocked";

        if (ReactionTimeRemaining > 0f)
            return $"Reaction {ReactionTimeRemaining:0.00}s";

        if (AttackCooldownRemaining > 0f)
            return $"Cooldown {AttackCooldownRemaining:0.00}s";

        if (_attackActive)
            return _attackPhase == EnemyShooterAttackPhase.None
                ? "Attacking"
                : _attackPhase.ToString();

        return CanAttackTarget ? "Ready" : "Attack blocked";
    }

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        _navigationPath ??= new NavMeshPath();

        ResolveRefs();
        ConfigureAgent();
        _homePosition = transform.position;
        _lastHealth = _damageable != null ? _damageable.CurrentHealth : 0f;
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveRefs();
        ConfigureAgent();

        if (_damageable != null)
            _damageable.OnHealthChanged += OnHealthChanged;

        if (_hitReceivedEvent != null)
            _hitReceivedEvent.OnEventRaised += OnHitReceived;

        if (_soundStimulusEvent != null)
            _soundStimulusEvent.OnEventRaised += OnSoundStimulus;

        DebugMovement(
            $"Enabled. pos={transform.position}, agentNull={_agent == null}, isOnNavMesh={(_agent != null && _agent.isOnNavMesh)}, targetNull={_target == null}");
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_damageable != null)
            _damageable.OnHealthChanged -= OnHealthChanged;

        if (_hitReceivedEvent != null)
            _hitReceivedEvent.OnEventRaised -= OnHitReceived;

        if (_soundStimulusEvent != null)
            _soundStimulusEvent.OnEventRaised -= OnSoundStimulus;

        _dashController?.CancelDash();
        ClearIncomingThrowableEvasion();
        ReleaseCoverTactic();
        EnemyShooterTacticalIntentSelector.ReleaseDuelHolder(this);
        ResetTacticalIntent();
        _wasStatusActionBlocked = false;
        _effects?.SetFootTrailActive(false);
        _effects?.StopDashParticles();
    }

    // Purpose: Processes frame work that must run after the regular Update pass.
    private void LateUpdate()
    {
        if (_effects == null)
            return;

        if (IsActionBlockedByStatusEffect || IsDashing)
        {
            _effects.SetFootTrailActive(false);
            return;
        }

        bool moving =
            CanUseAgent() &&
            !_agent.isStopped &&
            _agent.velocity.sqrMagnitude > 0.01f;

        _effects.SetFootTrailActive(moving);
    }

    // Purpose: Processes one update step for awareness.
    public void TickAwareness()
    {
        if (TryHoldForStatusActionBlock())
            return;

        // Idle uses only the awareness action, while combat states also run a
        // dedicated movement/attack tick. Drive emergency evasion here as well
        // so every Enemy Shooter state responds to an incoming throwable.
        if (TryTickIncomingThrowableEvasion())
            return;

        if (TickDashMovement())
            return;

        EnemyShooterConfigSO config = Config;

        if (config == null)
            return;

        TryDashWhenHealthLow();

        if (!HasTarget)
        {
            ClearTarget(_hasLastKnownPosition || _hasSoundInvestigation);
            TryAcquireVisiblePlayer(false);
            RefreshGoal();
            return;
        }

        bool couldSeeTarget = _canSeeTarget;
        _canSeeTarget = _vision == null || _vision.CanSeeTarget(_target);

        if (_canSeeTarget)
        {
            if (!couldSeeTarget)
                BeginDirectSightReaction(_hasSeenTargetDuringCurrentEngagement);

            _isAlerted = true;
            _lastSeenTime = Time.time;
            RefreshTargetLock();
            SetLastKnownPosition(_target.position);

            RefreshGoal();
            return;
        }

        if (couldSeeTarget || _reactionStartedTime >= 0f)
            ClearActiveReaction();

        if (IsTargetLocked && _target != null)
        {
            _isAlerted = true;
            SetLastKnownPosition(_target.position);
            RefreshGoal();
            return;
        }

        if (HasForgottenTarget())
            ClearTarget(true);

        RefreshGoal();
    }

    // Purpose: Performs the force acquire player workflow for this component.
    public void ForceAcquirePlayer()
    {
        TryAcquireVisiblePlayer(true);
    }

    // Purpose: Performs the alert to player workflow for this component.
    public void AlertToPlayer()
    {
        PlayerStatsRuntime player = FindAnyObjectByType<PlayerStatsRuntime>();

        if (player == null)
            return;

        SetTarget(player.transform);
        _isAlerted = true;
        RefreshTargetLock();
        SetLastKnownPosition(player.transform.position);
        _lastSeenTime = Time.time;
        _hasSoundInvestigation = false;
        RefreshGoal();
    }

    // Purpose: Attempts to alert to player from damage and reports whether the request succeeded.
    public bool TryAlertToPlayerFromDamage(GameObject attacker)
    {
        EnemyShooterConfigSO config = Config;

        if (config != null && !config.AlertOnDamaged)
            return false;

        if (!TryResolvePlayerTransform(attacker, out Transform playerTransform))
            return false;

        if (!IsWithinAwarenessRange(playerTransform.position))
            return false;

        bool couldSeeTarget = _target == playerTransform && _canSeeTarget;
        bool isPursuitReacquisition = _hasSeenTargetDuringCurrentEngagement;

        SetTarget(playerTransform);
        _isAlerted = true;
        _canSeeTarget = _vision == null || _vision.CanSeeTarget(playerTransform);

        if (_canSeeTarget && !couldSeeTarget)
            BeginDirectSightReaction(isPursuitReacquisition);
        else if (!_canSeeTarget)
            ClearActiveReaction();

        RefreshTargetLock();
        SetLastKnownPosition(playerTransform.position);
        _lastSeenTime = Time.time;
        _hasSoundInvestigation = false;

        FaceDirection(playerTransform.position - transform.position);
        RefreshGoal();
        return true;
    }

    // Purpose: Clears the current combat state.
    public void ClearCombat()
    {
        ClearTarget(false);
        ClearSoundInvestigation();
        _aimDirection = Vector3.zero;
        _burstShotsRemaining = 0;
        ResetBurstTracking();
        _nextFireTime = 0f;
        _nextBurstShotTime = 0f;
        _attackPhase = EnemyShooterAttackPhase.None;
        ClearIncomingThrowableEvasion();
        RefreshGoal();
    }

    // Purpose: Begins the patrol workflow.
    public void BeginPatrol()
    {
        _attackActive = false;
        _hasPatrolDestination = false;
        _patrolDestinationReached = false;
        _investigationArrived = false;
        _currentGoal = EnemyShooterGoal.Patrol;
        _currentInvestigationType = EnemyShooterInvestigationType.None;
        _nextPatrolRefreshTime = 0f;
        ConfigureAgent();

        if (CanUseAgent())
        {
            _agent.speed = ResolveMoveSpeed();
            _agent.stoppingDistance = 0f;
            _agent.isStopped = false;
            DebugMovement($"BeginPatrol. speed={_agent.speed}, pos={transform.position}");
        }
    }

    // Purpose: Processes one update step for patrol.
    public void TickPatrol()
    {
        if (TryHoldForStatusActionBlock())
            return;

        if (TryTickIncomingThrowableEvasion())
            return;

        if (TickDashMovement())
            return;

        if (!CanUseAgent())
            return;

        EnemyShooterConfigSO config = Config;
        if (config == null)
        {
            DebugMovement("TickPatrol skipped because config is missing.");
            return;
        }

        _agent.speed = ResolveMoveSpeed();
        _agent.stoppingDistance = 0f;

        if (_patrolDestinationReached)
        {
            if (Time.time < _patrolIdleUntil)
            {
                StopAgentHard("Patrol idle");
                return;
            }

            _patrolDestinationReached = false;
            _hasPatrolDestination = false;
        }

        if (_hasPatrolDestination && HasReached(_patrolDestination, config.PatrolArriveDistance))
        {
            _hasPatrolDestination = false;
            _patrolDestinationReached = true;
            _patrolIdleUntil = Time.time + config.PatrolIdleSeconds;
            StopAgentHard("Patrol destination reached");
            DebugMovement($"Patrol destination reached. idleUntil={_patrolIdleUntil}, pos={transform.position}");
            return;
        }

        if (TryGetHarmfulAreaAvoidanceDestination(out Vector3 avoidanceDestination))
        {
            _patrolDestination = avoidanceDestination;
            _hasPatrolDestination = true;
            _patrolDestinationReached = false;
            _nextPatrolRefreshTime = Time.time + config.PatrolDestinationRefreshInterval;
            TrySetAgentDestination(_patrolDestination, 0f, "Patrol harmful-area avoidance");
            return;
        }

        bool currentPathEndedBeforeDestination =
            _hasPatrolDestination &&
            !_agent.pathPending &&
            !_agent.hasPath &&
            Time.time >= _nextPatrolRefreshTime;

        bool needsDestination =
            !_hasPatrolDestination ||
            currentPathEndedBeforeDestination;

        if (!needsDestination)
            return;

        if (TryPickPatrolDestination(out _patrolDestination))
        {
            _hasPatrolDestination = true;
            _patrolDestinationReached = false;
            _nextPatrolRefreshTime = Time.time + config.PatrolDestinationRefreshInterval;
            TrySetAgentDestination(_patrolDestination, 0f, "Patrol");
        }
        else
        {
            StopAgentHard("Patrol failed to pick destination");
            DebugMovement(
                $"Patrol failed to pick destination. home={_homePosition}, radius={config.PatrolRadius}, pos={transform.position}");
        }
    }

    // Purpose: Begins the chase workflow.
    public void BeginChase()
    {
        _attackActive = false;
        _investigationArrived = false;
        _currentGoal = EnemyShooterGoal.ChaseTarget;
        _lastChaseProgressPosition = transform.position;
        _lastChaseProgressTime = Time.time;
        ConfigureAgent();

        if (CanUseAgent())
        {
            _agent.speed = ResolveMoveSpeed();
            _agent.stoppingDistance = ResolveChaseStoppingDistance();
            _agent.isStopped = false;
            DebugMovement($"BeginChaseTarget. speed={_agent.speed}, stopping={_agent.stoppingDistance}, target={(_target != null ? _target.name : "null")}");
        }
    }

    // Purpose: Processes one update step for chase.
    public void TickChase()
    {
        if (TryHoldForStatusActionBlock())
            return;

        if (TryTickIncomingThrowableEvasion())
            return;

        if (TickDashMovement())
            return;

        if (!CanUseAgent())
            return;

        if (TryGetHarmfulAreaAvoidanceDestination(out Vector3 avoidanceDestination))
        {
            TrySetAgentDestination(
                avoidanceDestination,
                0f,
                "Chase harmful-area avoidance");

            return;
        }

        if (!HasTarget)
        {
            ReleaseCoverTactic();
            _agent.ResetPath();
            _agent.isStopped = true;
            return;
        }

        if (TryTickCoverTactic() || TryTickActiveCombatFlank())
            return;

        if (Config != null &&
            EffectiveUseCoverTactics &&
            CanSeeTarget &&
            GetPlanarDistanceToTarget() <= ResolveAttackStartRange() &&
            !HasClearMuzzlePathToTarget())
        {
            if (TryStartCombatTactics() &&
                (TryTickCoverTactic() || TryTickActiveCombatFlank()))
            {
                return;
            }

            UpdateAimDirection();
            TickMuzzleBlockedReposition();
            return;
        }

        if (TryResolveTacticalChaseDestination(
                out Vector3 tacticalDestination,
                out float tacticalStoppingDistance,
                out string tacticalReason))
        {
            if (_tacticalIntent == EnemyShooterTacticalIntent.HoldLastKnownPosition &&
                HasReached(tacticalDestination, tacticalStoppingDistance))
            {
                StopAgentHard("Tactical hold last-known angle");
                return;
            }

            if (!TrySetAgentDestination(
                    tacticalDestination,
                    tacticalStoppingDistance,
                    tacticalReason))
            {
                DebugMovement(
                    $"Tactical destination update deferred. intent={_tacticalIntent}, " +
                    BuildAgentDebugInfo());
            }

            RecoverStalledChase(tacticalDestination);
            return;
        }

        Vector3 rawDestination = (CanSeeTarget || IsTargetLocked) && _target != null
            ? _target.position
            : _lastKnownPosition;

        bool destinationAccepted = TrySetAgentDestination(
                rawDestination,
                ResolveChaseStoppingDistance(),
                "Chase target");

        if (!destinationAccepted)
        {
            // A moving target can invalidate a path request for one frame while the
            // agent still owns a usable previous path. Do not erase that path and
            // leave the shooter staring at the target; the next tick will retry.
            DebugMovement(
                $"Chase destination update deferred. distance={GetPlanarDistanceToTarget():0.00}, " +
                $"attackRange={AttackStartRange:0.00}, {BuildAgentDebugInfo()}");
        }

        RecoverStalledChase(rawDestination);
    }

    private bool ShouldMaintainTacticalPressure
    {
        get
        {
            EnemyShooterConfigSO config = Config;
            return config != null &&
                   EffectiveUseTacticalIntent &&
                   HasTarget &&
                   _hasLastKnownPosition &&
                   (!CanSeeTarget || HasActiveCoverTactic) &&
                   !HasForgottenTarget();
        }
    }

    // Purpose: Attempts to resolve tactical chase destination and reports whether the request succeeded.
    private bool TryResolveTacticalChaseDestination(
        out Vector3 destination,
        out float stoppingDistance,
        out string reason)
    {
        destination = transform.position;
        stoppingDistance = 0f;
        reason = string.Empty;

        EnemyShooterConfigSO config = Config;

        if (!ShouldMaintainTacticalPressure || config == null)
        {
            if (HasActiveCoverTactic)
                return false;

            EnemyShooterTacticalIntentSelector.ReleaseDuelHolder(this);
            ResetTacticalIntent();
            return false;
        }

        if (_tacticalIntentTarget != _target ||
            Time.time >= _nextTacticalIntentRefreshTime)
        {
            RefreshTacticalIntent(config);
        }

        switch (_tacticalIntent)
        {
            case EnemyShooterTacticalIntent.HoldLastKnownPosition:
                destination = _lastKnownPosition;
                stoppingDistance = ResolveChaseStoppingDistance();
                reason = "Tactical hold last-known angle";
                return true;

            case EnemyShooterTacticalIntent.FlankLeft:
            case EnemyShooterTacticalIntent.FlankRight:
                destination = _tacticalDestination;
                stoppingDistance = 0f;
                reason = _tacticalIntent == EnemyShooterTacticalIntent.FlankLeft
                    ? "Tactical left flank"
                    : "Tactical right flank";
                return true;

            default:
                return false;
        }
    }

    // Purpose: Refreshes the displayed or cached tactical intent.
    private void RefreshTacticalIntent(EnemyShooterConfigSO config)
    {
        if (HasActiveCoverTactic)
        {
            _reservedCoverPoint.TryReserve(this);
            return;
        }

        _tacticalIntentTarget = _target;
        _nextTacticalIntentRefreshTime = Time.time + config.TacticalIntentRefreshInterval;
        _tacticalIntent = EnemyShooterTacticalIntent.Direct;
        _tacticalDestination = transform.position;

        bool holdsDuelToken = EffectiveUseDuelToken &&
                              EnemyShooterTacticalIntentSelector.TryClaimDuelHolder(this, _target);

        if (holdsDuelToken)
        {
            _tacticalIntent = EnemyShooterTacticalIntent.HoldLastKnownPosition;
            return;
        }

        if (EffectiveUseFlankingTactics &&
            EnemyShooterTacticalIntentSelector.HasDuelHolder(_target) &&
            TryResolveTacticalFlankDestination(
                _lastKnownPosition,
                out Vector3 flankDestination,
                out bool flankLeft))
        {
            _tacticalIntent = flankLeft
                ? EnemyShooterTacticalIntent.FlankLeft
                : EnemyShooterTacticalIntent.FlankRight;
            _tacticalDestination = flankDestination;
        }
    }

    // Purpose: Attempts to resolve tactical flank destination and reports whether the request succeeded.
    private bool TryResolveTacticalFlankDestination(
        Vector3 focus,
        out Vector3 destination,
        out bool flankLeft)
    {
        destination = transform.position;
        flankLeft = false;

        EnemyShooterConfigSO config = Config;

        if (config == null)
            return false;

        Vector3 awayFromFocus = transform.position - focus;
        awayFromFocus.y = 0f;

        if (awayFromFocus.sqrMagnitude <= 0.0001f)
        {
            awayFromFocus = transform.forward;
            awayFromFocus.y = 0f;
        }

        if (awayFromFocus.sqrMagnitude <= 0.0001f)
            awayFromFocus = Vector3.forward;

        awayFromFocus.Normalize();
        int preferredSign = _attackStrafeSign >= 0 ? 1 : -1;

        for (int attempt = 0; attempt < 2; attempt++)
        {
            int sign = attempt == 0 ? preferredSign : -preferredSign;
            Vector3 direction = Quaternion.AngleAxis(
                                    config.TacticalFlankAngleDegrees * sign,
                                    Vector3.up) * awayFromFocus;
            Vector3 rawCandidate = focus + direction.normalized * EffectiveTacticalFlankDistance;

            if (!TryProjectToNavMesh(rawCandidate, out Vector3 candidate) ||
                PlanarSqrDistance(candidate, transform.position) < 0.25f)
            {
                continue;
            }

            _attackStrafeSign = sign;
            destination = candidate;
            flankLeft = sign > 0;
            return true;
        }

        return false;
    }

    // Purpose: Attempts to start combat tactics and reports whether the request succeeded.
    private bool TryStartCombatTactics()
    {
        EnemyShooterConfigSO config = Config;

        if (config == null || !EffectiveUseTacticalIntent || !HasTarget)
            return false;

        if (HasActiveCoverTactic)
            return true;

        bool ownsDuelToken = EffectiveUseDuelToken &&
                             EnemyShooterTacticalIntentSelector.TryClaimDuelHolder(this, _target);
        bool targetHasDuelHolder = EnemyShooterTacticalIntentSelector.HasDuelHolder(_target);

        // Once one shooter owns the stand-off token, teammates take a wider lane
        // before considering another cover point. This keeps a squad from stacking
        // beside the same obstacle and preserves an exposed pressure angle.
        if (!ownsDuelToken &&
            EffectiveUseFlankingTactics &&
            targetHasDuelHolder &&
            TryResolveTacticalFlankDestination(
                _target.position,
                out Vector3 flankDestination,
                out bool flankLeft))
        {
            _tacticalIntent = flankLeft
                ? EnemyShooterTacticalIntent.FlankLeft
                : EnemyShooterTacticalIntent.FlankRight;
            _tacticalDestination = flankDestination;
            _tacticalIntentTarget = _target;
            _nextTacticalIntentRefreshTime = Time.time + config.TacticalIntentRefreshInterval;
            return true;
        }

        if (EffectiveUseCoverTactics && TryAcquireCoverTactic())
            return true;

        return ownsDuelToken;
    }

    // Purpose: Attempts to acquire cover tactic and reports whether the request succeeded.
    private bool TryAcquireCoverTactic()
    {
        EnemyShooterConfigSO config = Config;

        if (config == null ||
            !EffectiveUseCoverTactics ||
            !HasTarget ||
            !CanSeeTarget ||
            _shoot == null)
        {
            return false;
        }

        float searchRadiusSqr = EffectiveCoverSearchRadius * EffectiveCoverSearchRadius;
        float bestScore = float.PositiveInfinity;
        EnemyShooterCoverPoint bestPoint = null;
        Vector3 bestAnchor = default;
        Vector3 bestPeek = default;

        IReadOnlyList<EnemyShooterCoverPoint> points = EnemyShooterCoverPoint.RegisteredPoints;
        for (int index = 0; index < points.Count; index++)
        {
            EnemyShooterCoverPoint point = points[index];

            if (point == null ||
                !point.isActiveAndEnabled ||
                !point.HasPeekPoint ||
                (point.Occupant != null && point.Occupant != this))
            {
                continue;
            }

            if (!TryProjectToNavMesh(point.CoverPosition, out Vector3 coverAnchor) ||
                PlanarSqrDistance(coverAnchor, transform.position) > searchRadiusSqr)
            {
                continue;
            }

            // A cover anchor is valid only when the obstacle really blocks its
            // direct shot. A separate reachable peek position must restore it.
            if (_shoot.HasClearBallisticPathFromRootPosition(
                    coverAnchor,
                    ResolveTargetPoint(),
                    _target))
            {
                continue;
            }

            if (!TryResolveCoverPeekDestination(point, out Vector3 peekDestination))
                continue;

            float score = PlanarSqrDistance(coverAnchor, transform.position);
            if (score >= bestScore)
                continue;

            bestScore = score;
            bestPoint = point;
            bestAnchor = coverAnchor;
            bestPeek = peekDestination;
        }

        // Test sectors currently contain physical crate/wall colliders but may not
        // have authored EnemyShooterCoverPoint markers yet. When the Debug Monitor
        // explicitly enables cover, derive one temporary point from a blocking
        // obstacle so the cover movement and advance-fire paths can be verified.
        if (bestPoint == null &&
            _debugCombatOverridesEnabled &&
            TryCreateDebugRuntimeCoverPoint(
                searchRadiusSqr,
                out EnemyShooterCoverPoint runtimePoint,
                out Vector3 runtimeAnchor,
                out Vector3 runtimePeek))
        {
            bestPoint = runtimePoint;
            bestAnchor = runtimeAnchor;
            bestPeek = runtimePeek;
        }

        if (bestPoint == null || !bestPoint.TryReserve(this))
        {
            if (bestPoint != null && bestPoint == _runtimeAutoCoverPoint)
            {
                Destroy(bestPoint.gameObject);
                _runtimeAutoCoverPoint = null;
            }

            return false;
        }

        _reservedCoverPoint = bestPoint;
        _coverTacticPhase = EnemyShooterCoverTacticPhase.MovingToCover;
        _coverPeekDestination = bestPeek;
        _coverPhaseEndsAt = 0f;
        _tacticalIntentTarget = _target;
        _tacticalDestination = bestAnchor;
        return true;
    }

    // Purpose: Attempts to create debug runtime cover point and reports whether the request succeeded.
    private bool TryCreateDebugRuntimeCoverPoint(
        float searchRadiusSqr,
        out EnemyShooterCoverPoint point,
        out Vector3 coverAnchor,
        out Vector3 peekPosition)
    {
        point = null;
        coverAnchor = default;
        peekPosition = default;

        if (!HasTarget || _shoot == null || _runtimeAutoCoverPoint != null)
            return false;

        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            EffectiveCoverSearchRadius,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        float bestScore = float.PositiveInfinity;
        Vector3 bestAnchor = default;
        Vector3 bestPeek = default;
        Vector3 targetPoint = ResolveTargetPoint();

        for (int index = 0; index < colliders.Length; index++)
        {
            Collider collider = colliders[index];
            if (!IsDebugCoverObstacle(collider))
                continue;

            Bounds bounds = collider.bounds;
            if (bounds.size.y < 0.35f)
                continue;

            Vector3 awayFromTarget = bounds.center - targetPoint;
            awayFromTarget.y = 0f;
            if (awayFromTarget.sqrMagnitude < 0.0001f)
                continue;

            awayFromTarget.Normalize();
            float planarExtent = Mathf.Max(bounds.extents.x, bounds.extents.z);
            float clearance = Mathf.Max(0.45f, (_agent != null ? _agent.radius : 0.25f) + 0.2f);
            Vector3 outerSurface = collider.ClosestPoint(
                bounds.center + awayFromTarget * (planarExtent + 10f));
            Vector3 rawCoverAnchor = outerSurface + awayFromTarget * clearance;

            if (!TryProjectToNavMesh(rawCoverAnchor, out Vector3 candidateAnchor) ||
                PlanarSqrDistance(candidateAnchor, transform.position) > searchRadiusSqr ||
                _shoot.HasClearBallisticPathFromRootPosition(
                    candidateAnchor,
                    targetPoint,
                    _target))
            {
                continue;
            }

            if (!TryResolveDebugCoverPeek(
                    outerSurface,
                    awayFromTarget,
                    planarExtent,
                    clearance,
                    targetPoint,
                    out Vector3 candidatePeek))
            {
                continue;
            }

            float score = PlanarSqrDistance(candidateAnchor, transform.position);
            if (score >= bestScore)
                continue;

            bestScore = score;
            bestAnchor = candidateAnchor;
            bestPeek = candidatePeek;
        }

        if (float.IsPositiveInfinity(bestScore))
            return false;

        _runtimeAutoCoverPoint = EnemyShooterCoverPoint.CreateRuntime(bestAnchor, bestPeek);
        point = _runtimeAutoCoverPoint;
        coverAnchor = bestAnchor;
        peekPosition = bestPeek;
        return point != null;
    }

    // Purpose: Checks whether debug cover obstacle is currently true.
    private bool IsDebugCoverObstacle(Collider collider)
    {
        if (collider == null || !collider.enabled || collider.isTrigger ||
            collider.transform.IsChildOf(transform) ||
            (_target != null && collider.transform.IsChildOf(_target)))
        {
            return false;
        }

        // Damageable colliders belong to players or other shooters, not static
        // scenery. Do not turn an entity into temporary cover during testing.
        return collider.GetComponentInParent<Damageable>() == null;
    }

    // Purpose: Attempts to resolve debug cover peek and reports whether the request succeeded.
    private bool TryResolveDebugCoverPeek(
        Vector3 outerSurface,
        Vector3 awayFromTarget,
        float planarExtent,
        float clearance,
        Vector3 targetPoint,
        out Vector3 peekPosition)
    {
        peekPosition = default;
        Vector3 side = Vector3.Cross(Vector3.up, awayFromTarget);
        if (side.sqrMagnitude < 0.0001f)
            return false;

        side.Normalize();
        float sideOffset = planarExtent + clearance;

        for (int attempt = 0; attempt < 2; attempt++)
        {
            float sign = attempt == 0
                ? (_attackStrafeSign >= 0 ? 1f : -1f)
                : (_attackStrafeSign >= 0 ? -1f : 1f);
            Vector3 rawPeek = outerSurface + side * (sideOffset * sign) + awayFromTarget * 0.05f;

            if (!TryProjectToNavMesh(rawPeek, out Vector3 candidate) ||
                !_shoot.HasClearBallisticPathFromRootPosition(candidate, targetPoint, _target))
            {
                continue;
            }

            peekPosition = candidate;
            return true;
        }

        return false;
    }

    // Purpose: Attempts to resolve cover peek destination and reports whether the request succeeded.
    private bool TryResolveCoverPeekDestination(
        EnemyShooterCoverPoint point,
        out Vector3 peekDestination)
    {
        peekDestination = default;

        if (point == null || _shoot == null || !HasTarget)
            return false;

        bool preferLeft = _attackStrafeSign >= 0;
        for (int attempt = 0; attempt < 2; attempt++)
        {
            if (!point.TryGetPeekPosition(preferLeft, attempt, out Vector3 rawPeek) ||
                !TryProjectToNavMesh(rawPeek, out Vector3 candidate))
            {
                continue;
            }

            if (!_shoot.HasClearBallisticPathFromRootPosition(
                    candidate,
                    ResolveTargetPoint(),
                    _target))
            {
                continue;
            }

            peekDestination = candidate;
            return true;
        }

        return false;
    }

    // Purpose: Attempts to tick cover tactic and reports whether the request succeeded.
    private bool TryTickCoverTactic()
    {
        if (!HasActiveCoverTactic)
            return false;

        EnemyShooterConfigSO config = Config;

        if (config == null ||
            !EffectiveUseCoverTactics ||
            !HasTarget ||
            HasForgottenTarget() ||
            !_reservedCoverPoint.TryReserve(this))
        {
            ReleaseCoverTactic();
            return false;
        }

        if (!CanUseAgent())
            return true;

        Vector3 coverDestination = _reservedCoverPoint.CoverPosition;
        if (!TryProjectToNavMesh(coverDestination, out coverDestination))
        {
            ReleaseCoverTactic();
            return false;
        }

        float arriveDistance = config.CoverArriveDistance;

        switch (_coverTacticPhase)
        {
            case EnemyShooterCoverTacticPhase.MovingToCover:
                if (HasReached(coverDestination, arriveDistance))
                {
                    _coverTacticPhase = EnemyShooterCoverTacticPhase.Hidden;
                    _coverPhaseEndsAt = Time.time + EffectiveCoverHideSeconds;
                    StopAgentHard("Cover reached");
                }
                else
                {
                    TrySetAgentDestination(coverDestination, arriveDistance, "Move to cover");
                    ApplyCoverApproachSpeed();
                }

                return true;

            case EnemyShooterCoverTacticPhase.Hidden:
                StopAgentHard("Waiting behind cover");
                if (Time.time < _coverPhaseEndsAt)
                    return true;

                if (!TryResolveCoverPeekDestination(_reservedCoverPoint, out _coverPeekDestination))
                {
                    ReleaseCoverTactic();
                    return false;
                }

                _coverTacticPhase = EnemyShooterCoverTacticPhase.MovingToPeek;
                return true;

            case EnemyShooterCoverTacticPhase.MovingToPeek:
                if (HasReached(_coverPeekDestination, arriveDistance))
                {
                    _coverTacticPhase = EnemyShooterCoverTacticPhase.Peeking;
                    _coverPhaseEndsAt = Time.time + EffectiveCoverPeekSeconds;
                    StopAgentHard("Cover peek reached");
                }
                else
                {
                    TrySetAgentDestination(_coverPeekDestination, arriveDistance, "Move to cover peek");
                }

                return true;

            case EnemyShooterCoverTacticPhase.Peeking:
                if (Time.time >= _coverPhaseEndsAt || !HasClearMuzzlePathToTarget())
                {
                    _coverTacticPhase = EnemyShooterCoverTacticPhase.ReturningToCover;
                    TrySetAgentDestination(coverDestination, arriveDistance, "Return behind cover");
                    return true;
                }

                // Return false only while the peek has a clear shot. TickAttack
                // then follows the normal reaction, burst, and hit rules.
                return false;

            case EnemyShooterCoverTacticPhase.ReturningToCover:
                if (HasReached(coverDestination, arriveDistance))
                {
                    _coverTacticPhase = EnemyShooterCoverTacticPhase.Hidden;
                    _coverPhaseEndsAt = Time.time + EffectiveCoverHideSeconds;
                    StopAgentHard("Returned behind cover");
                }
                else
                {
                    TrySetAgentDestination(coverDestination, arriveDistance, "Return behind cover");
                }

                return true;
        }

        ReleaseCoverTactic();
        return false;
    }

    // Purpose: Attempts to tick active combat flank and reports whether the request succeeded.
    private bool TryTickActiveCombatFlank()
    {
        if (_tacticalIntent != EnemyShooterTacticalIntent.FlankLeft &&
            _tacticalIntent != EnemyShooterTacticalIntent.FlankRight)
        {
            return false;
        }

        EnemyShooterConfigSO config = Config;
        if (config == null ||
            !EffectiveUseFlankingTactics ||
            _tacticalIntentTarget != _target ||
            !HasTarget)
        {
            ResetTacticalIntent();
            return false;
        }

        if (HasReached(_tacticalDestination, config.CoverArriveDistance))
        {
            ResetTacticalIntent();
            return false;
        }

        if (!CanUseAgent())
            return true;

        TrySetAgentDestination(_tacticalDestination, config.CoverArriveDistance, "Combat flank");
        return true;
    }

    // Purpose: Releases the owned cover tactic resource.
    private void ReleaseCoverTactic()
    {
        if (_reservedCoverPoint != null)
            _reservedCoverPoint.Release(this);

        if (_runtimeAutoCoverPoint != null)
        {
            Destroy(_runtimeAutoCoverPoint.gameObject);
            _runtimeAutoCoverPoint = null;
        }

        _reservedCoverPoint = null;
        _coverTacticPhase = EnemyShooterCoverTacticPhase.None;
        _coverPeekDestination = default;
        _coverPhaseEndsAt = 0f;
    }

    // Purpose: Applies the requested cover approach speed change.
    private void ApplyCoverApproachSpeed()
    {
        if (!EffectiveFireWhileMovingToCover || !CanUseAgent())
            return;

        _agent.speed = Mathf.Max(0.1f, ResolveMoveSpeed() * EffectiveCoverApproachSpeedMultiplier);
    }

    // Purpose: Performs the reset tactical intent workflow for this component.
    private void ResetTacticalIntent()
    {
        _tacticalIntent = EnemyShooterTacticalIntent.Direct;
        _tacticalIntentTarget = null;
        _tacticalDestination = transform.position;
        _nextTacticalIntentRefreshTime = 0f;
    }

    // Purpose: Performs the recover stalled chase workflow for this component.
    private void RecoverStalledChase(Vector3 rawDestination)
    {
        if (!CanUseAgent() || _agent.pathPending)
            return;

        float targetDistance = GetPlanarDistanceToTarget();
        float attackRange = AttackStartRange;

        if (targetDistance <= attackRange)
        {
            _lastChaseProgressPosition = transform.position;
            _lastChaseProgressTime = Time.time;
            return;
        }

        float movedSqr = PlanarSqrDistance(
            transform.position,
            _lastChaseProgressPosition);

        if (movedSqr >= 0.0625f || _agent.velocity.sqrMagnitude >= 0.04f)
        {
            _lastChaseProgressPosition = transform.position;
            _lastChaseProgressTime = Time.time;
            return;
        }

        if (Time.time < _lastChaseProgressTime + 0.75f)
            return;

        // The target is still outside firing range but the agent has made no
        // progress. Rebuild once with zero stopping distance so a stale/partial
        // path cannot hold the shooter at an artificial combat radius.
        _agent.ResetPath();
        _agent.isStopped = false;
        ClearNavigationRequest();
        TrySetAgentDestination(rawDestination, 0f, "Chase stalled recovery");
        _lastChaseProgressPosition = transform.position;
        _lastChaseProgressTime = Time.time;
    }

    // Purpose: Begins the investigate workflow.
    public void BeginInvestigate()
    {
        _attackActive = false;
        _investigationArrived = false;
        _investigationFinishTime = 0f;
        _currentGoal = EnemyShooterGoal.Investigate;
        _currentInvestigationType = ResolveInvestigationType();
        ConfigureAgent();

        if (CanUseAgent())
        {
            _agent.speed = ResolveMoveSpeed();
            _agent.stoppingDistance = ResolveInvestigateArriveDistance();

            if (_currentInvestigationType == EnemyShooterInvestigationType.Sound &&
                Time.time < _soundInvestigationMoveTime)
            {
                StopAgentHard("Begin sound reaction delay");
                FaceDirection(_soundInvestigationPosition - transform.position);
            }
            else
            {
                _agent.isStopped = false;
            }

            DebugMovement($"BeginInvestigate. type={_currentInvestigationType}, pos={transform.position}");
        }
        else
        {
            DebugMovement($"BeginInvestigate skipped because agent is unavailable. {BuildAgentDebugInfo()}");
        }
    }

    // Purpose: Processes one update step for investigate.
    public void TickInvestigate()
    {
        if (TryHoldForStatusActionBlock())
            return;

        if (TryTickIncomingThrowableEvasion())
            return;

        if (TickDashMovement())
            return;

        if (!CanUseAgent())
        {
            DebugMovement($"TickInvestigate skipped because agent is unavailable. {BuildAgentDebugInfo()}");
            return;
        }

        if (TryGetHarmfulAreaAvoidanceDestination(out Vector3 avoidanceDestination))
        {
            TrySetAgentDestination(
                avoidanceDestination,
                0f,
                "Investigate harmful-area avoidance");
            return;
        }

        _currentInvestigationType = ResolveInvestigationType();

        if (_currentInvestigationType == EnemyShooterInvestigationType.None ||
            !TryResolveInvestigationDestination(out Vector3 destination))
        {
            DebugMovement(
                $"Investigation lost clue. waiting before patrol. type={_currentInvestigationType}, hasSound={_hasSoundInvestigation}, hasLastKnown={_hasLastKnownPosition}");
            ClearInvestigation();
            StopAgentHard("Investigation lost clue");

            BeginInvestigationSearchWait("No investigation destination");

            return;
        }

        if (_currentInvestigationType == EnemyShooterInvestigationType.Sound &&
            Time.time < _soundInvestigationMoveTime)
        {
            StopAgentHard("Waiting for sound reaction delay");
            FaceDirection(destination - transform.position);
            return;
        }

        float arriveDistance = ResolveInvestigateArriveDistance();

        if (HasReached(
                ResolveInvestigationArrivalDestination(destination),
                arriveDistance))
        {
            if (!_investigationArrived)
            {
                EnemyShooterConfigSO config = Config;
                _investigationArrived = true;
                _investigationFinishTime =
                    Time.time + ResolveInvestigationWaitSeconds(config);
                StopAgentHard("Investigation arrived");
                DebugMovement($"Investigation arrived. type={_currentInvestigationType}, finishAt={_investigationFinishTime}");
            }

            if (InvestigationFinished)
                TryCompleteFinishedInvestigation();

            return;
        }

        _investigationArrived = false;
        TrySetAgentDestination(destination, arriveDistance, $"Investigate {_currentInvestigationType}");
    }

    // Purpose: Begins the investigation search wait workflow.
    private void BeginInvestigationSearchWait(string reason)
    {
        if (_investigationArrived)
            return;

        EnemyShooterConfigSO config = Config;
        _investigationArrived = true;
        _investigationFinishTime =
            Time.time + ResolveInvestigationWaitSeconds(config);

        DebugMovement(
            $"Investigation search wait started. reason={reason}, finishAt={_investigationFinishTime}, searchSeconds={(config != null ? config.InvestigateSearchSeconds : 0f)}");
    }

    // Purpose: Attempts to complete finished investigation and reports whether the request succeeded.
    public bool TryCompleteFinishedInvestigation()
    {
        if (!InvestigationFinished)
            return false;

        if (TryPromoteQueuedSoundInvestigation())
            return false;

        DebugMovement(
            $"Investigation finished. clearing. type={_currentInvestigationType}, hasSound={_hasSoundInvestigation}, hasLastKnown={_hasLastKnownPosition}");

        ClearInvestigation();
        _investigationArrived = false;
        _investigationFinishTime = 0f;

        EnemyShooterConfigSO config = Config;
        _ignoreInvestigationUntilTime =
            Time.time + (config != null ? config.InvestigationReenterCooldown : 0f);

        if (CanUseAgent())
            StopAgentHard("Investigation finished");

        RefreshGoal();
        return true;
    }

    // Purpose: Stops the active movement behavior.
    public void StopMovement()
    {
        StopAgentHard("StopMovement");
    }

    // Purpose: Attempts to hold for status action block and reports whether the request succeeded.
    private bool TryHoldForStatusActionBlock()
    {
        if (!ResolveStatusActionBlocked())
        {
            if (_wasStatusActionBlocked)
            {
                _wasStatusActionBlocked = false;

                // Reacquiring a visible target after a stun still respects the
                // pursuit reaction delay instead of firing on the release frame.
                if (HasTarget && _canSeeTarget)
                    BeginDirectSightReaction(true);

                RefreshGoal();
            }

            return false;
        }

        if (!_wasStatusActionBlocked)
        {
            _wasStatusActionBlocked = true;
            _burstShotsRemaining = 0;
            _nextBurstShotTime = 0f;
            _nextFireTime = 0f;
            ResetBurstTracking();
            _attackActive = false;
            _attackPhase = EnemyShooterAttackPhase.None;
            ClearActiveReaction();

            if (_meleeActionActive)
                _meleeActionFinishTime = Time.time;

            _throwableActionActive = false;
            _throwableActionFinishTime = 0f;
            _dashController?.CancelDash();
            ClearNavigationRequest();
        }

        StopAgentHard("Status effect blocks actions");
        _effects?.SetFootTrailActive(false);
        _effects?.StopDashParticles();
        return true;
    }

    // Purpose: Resolves the status action blocked needed by this component.
    private bool ResolveStatusActionBlocked()
    {
        if (_statusEffectRuntime == null || !_statusEffectRuntime.HasAny)
            return false;

        if (TryReadBooleanProperty(
                RuntimeBlocksActionsProperty,
                _statusEffectRuntime,
                out bool runtimeBlocksActions) &&
            runtimeBlocksActions)
        {
            return true;
        }

        IReadOnlyList<EntityStatusEffectRuntime.ActiveStatusEffect> activeEffects =
            _statusEffectRuntime.ActiveEffects;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            StatusEffectDefinitionSO definition = activeEffects[i].Definition;

            if (definition == null)
                continue;

            if (TryReadBooleanProperty(
                    DefinitionBlocksActionsProperty,
                    definition,
                    out bool definitionBlocksActions) &&
                definitionBlocksActions)
            {
                return true;
            }

            // Compatibility for the authored flash/stun SO while the generic
            // status schema is being migrated. Beneficial effects never block.
            string id = definition.Id;
            if (definition.IsDebuff &&
                !string.IsNullOrWhiteSpace(id) &&
                (id.IndexOf("stun", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 id.IndexOf("flash", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return true;
            }
        }

        return false;
    }

    // Purpose: Attempts to read boolean property and reports whether the request succeeded.
    private static bool TryReadBooleanProperty(
        System.Reflection.PropertyInfo property,
        object owner,
        out bool value)
    {
        value = false;

        if (property == null ||
            owner == null ||
            property.PropertyType != typeof(bool))
        {
            return false;
        }

        object propertyValue = property.GetValue(owner);
        if (!(propertyValue is bool boolValue))
            return false;

        value = boolValue;
        return true;
    }

    // Purpose: Begins the attack workflow.
    public void BeginAttack()
    {
        if (TryHoldForStatusActionBlock())
            return;

        if (!TryActivateRangedWeaponForCombat())
            return;

        _attackActive = true;
        _burstShotsRemaining = 0;
        ResetBurstTracking();
        _attackPhase = EnemyShooterAttackPhase.FiringBurst;
        _currentGoal = EnemyShooterGoal.AttackTarget;
        _attackStrafeSign = Random.value < 0.5f ? -1 : 1;
        TryStartCombatTactics();

        if (CanUseAgent())
        {
            _agent.speed = ResolveMoveSpeed();
            _agent.stoppingDistance = ResolveChaseStoppingDistance();
            StopAgentHard("BeginAttack");
        }
    }

    // Purpose: Processes one update step for attack.
    public void TickAttack()
    {
        if (TryHoldForStatusActionBlock())
            return;

        if (TryTickIncomingThrowableEvasion())
            return;

        if (TickDashMovement())
            return;

        bool isMaintainingCover = TryTickCoverTactic();
        bool isMovingToFlank = !isMaintainingCover && TryTickActiveCombatFlank();
        if (isMaintainingCover || isMovingToFlank)
        {
            UpdateAimDirection();

            if (isMaintainingCover)
                TryFireWhileMovingToCover();

            return;
        }

        bool finishingStartedBurst =
            !CanDirectlyAttackTarget && CanFinishStartedBurstAfterSightLost;
        bool holdingBurstCooldown =
            !CanDirectlyAttackTarget && CanHoldBurstCooldownAfterSightLost;

        if (!CanAttackTarget)
        {
            _attackActive = false;

            // Transition evaluation normally moves Attack -> Chase before this
            // update. This fallback keeps movement responsive when sight/range
            // changes between condition evaluation and the action update.
            if (ShouldChaseTarget)
            {
                BeginChase();
                TickChase();
                return;
            }

            StopAgentHard("Attack target unavailable");
            return;
        }

        if (!finishingStartedBurst &&
            !holdingBurstCooldown &&
            TryGetHarmfulAreaAvoidanceDestination(out Vector3 avoidanceDestination))
        {
            UpdateAimDirection();
            TrySetAgentDestination(
                avoidanceDestination,
                0f,
                "Attack harmful-area avoidance");
            return;
        }

        if (finishingStartedBurst)
        {
            // This is the one intentional through-cover tracking window: an
            // already-started burst keeps aiming at the live target Transform,
            // while projectile collision still lets the structure absorb shots.
            UpdateAimDirection();
        }
        else if (!holdingBurstCooldown)
        {
            UpdateAimDirection();
        }

        EnemyShooterConfigSO config = Config;
        if (config == null || ReactionTimeRemaining > 0f)
        {
            StopAgentHard("Attack waiting reaction");
            return;
        }

        if (_attackPhase == EnemyShooterAttackPhase.BurstCooldown)
        {
            if (holdingBurstCooldown)
            {
                StopAgentHard("Post-burst cooldown after sight lost");
                return;
            }

            if (config.MoveDuringBurstInterval)
                TickAttackMovement();
            else
                StopAgentHard("Attack burst cooldown");

            if (Time.time >= _nextFireTime)
            {
                _attackPhase = EnemyShooterAttackPhase.FiringBurst;
                _burstShotsRemaining = 0;
                _nextBurstShotTime = 0f;
            }

            return;
        }

        if (config.MoveWhileAttacking)
            TickAttackMovement();
        else
            StopAgentHard("Attack firing");

        UpdateBurstFire();
    }

    // Purpose: Finishes the active attack workflow.
    public void EndAttack()
    {
        _attackActive = false;
        _burstShotsRemaining = 0;
        ResetBurstTracking();
        _attackPhase = EnemyShooterAttackPhase.None;
        _nextCombatMoveRefreshTime = 0f;

        if (!HasTarget)
            ReleaseCoverTactic();
    }

    // Purpose: Begins the melee attack workflow.
    public void BeginMeleeAttack()
    {
        if (TryHoldForStatusActionBlock())
            return;

        if (!TryActivateMeleeWeaponForCombat())
            return;

        _meleeActionActive = true;
        _meleeActionFinishTime = Time.time + (Config != null ? Config.MeleeActionSeconds : 0.35f);
        _attackActive = false;
        _currentGoal = EnemyShooterGoal.AttackTarget;
        StopAgentHard("BeginMeleeAttack");
        UpdateAimDirection();
        TryExecuteMeleeAttack();
    }

    // Purpose: Processes one update step for melee attack.
    public void TickMeleeAttack()
    {
        if (TryHoldForStatusActionBlock())
            return;

        if (TryTickIncomingThrowableEvasion())
            return;

        if (TickDashMovement())
            return;

        StopAgentHard("MeleeAttack");
        UpdateAimDirection();
    }

    // Purpose: Finishes the active melee attack workflow.
    public void EndMeleeAttack()
    {
        _meleeActionActive = false;
    }

    // Purpose: Begins the throwable attack workflow.
    public void BeginThrowableAttack()
    {
        if (TryHoldForStatusActionBlock())
            return;

        _attackActive = false;
        _currentGoal = EnemyShooterGoal.AttackTarget;
        StopAgentHard("BeginThrowableAttack");
        UpdateAimDirection();

        _throwableActionActive = TryExecuteThrowableAttack();
        _throwableActionFinishTime = _throwableActionActive
            ? Time.time + (Config != null ? Config.ThrowableActionSeconds : 0.35f)
            : Time.time;
    }

    // Purpose: Processes one update step for throwable attack.
    public void TickThrowableAttack()
    {
        if (TryHoldForStatusActionBlock())
            return;

        if (TryTickIncomingThrowableEvasion())
            return;

        if (TickDashMovement())
            return;

        StopAgentHard("ThrowableAttack");
        UpdateAimDirection();
    }

    // Purpose: Finishes the active throwable attack workflow.
    public void EndThrowableAttack()
    {
        _throwableActionActive = false;
    }

    // Purpose: Checks whether use melee attack is currently permitted.
    private bool CanUseMeleeAttack()
    {
        EnemyShooterConfigSO config = Config;

        if (config == null ||
            !config.UseMeleeAttack ||
            _meleeAttack == null ||
            !HasTarget ||
            Time.time < _nextMeleeAllowedTime)
        {
            return false;
        }

        if (!TryGetConfiguredMeleeRange(out float range))
            return false;

        if (UsesConfiguredCombatWeaponSlots &&
            !CanActivateConfiguredCombatWeaponSlot(config.MeleeWeaponSlotIndex))
        {
            return false;
        }

        return GetPlanarDistanceToTarget() <= range + config.MeleeRangePadding;
    }

    // Purpose: Attempts to execute melee attack and reports whether the request succeeded.
    private bool TryExecuteMeleeAttack()
    {
        EnemyShooterConfigSO config = Config;

        if (!CanUseMeleeAttack())
            return false;

        Vector3 origin = transform.position;
        Vector3 direction = HasTarget && _target != null
            ? _target.position - transform.position
            : transform.forward;

        bool attacked = _meleeAttack.TryAttackCurrentWeapon(
            ShooterFaction.Enemy,
            origin,
            direction);

        if (attacked)
            _nextMeleeAllowedTime = Time.time + (config != null ? config.MeleeAttackCooldownSeconds : 1.2f);

        return attacked;
    }

    // Purpose: Checks whether use throwable attack is currently permitted.
    private bool CanUseThrowableAttack()
    {
        EnemyShooterConfigSO config = Config;

        if (config == null ||
            !EffectiveUseThrowableAttack ||
            _throwableActionActive ||
            _throwableAttack == null ||
            _lootInventory == null ||
            !HasTarget ||
            Time.time < _nextThrowableAllowedTime)
        {
            return false;
        }

        if (!_lootInventory.TryGetFirstCarriedItem(out ThrowableUsableItemSO throwable) ||
            throwable == null)
        {
            return false;
        }

        float targetDistance = GetPlanarDistanceToTarget();
        if (targetDistance < config.ThrowableMinRange ||
            (config.ThrowableMaxRange > 0f && targetDistance > config.ThrowableMaxRange))
        {
            return false;
        }

        if (config.ThrowableBlockWhenCanAttack && CanDirectlyAttackTarget)
            return false;

        if (config.ThrowablePreferWhenNoLineOfSight && CanSeeTarget && HasClearMuzzlePathToTarget())
            return false;

        if (!TryResolveThrowableTargetPoint(out Vector3 targetPoint))
            return false;

        float selfSafetyDistance = Mathf.Max(
            0f,
            throwable.ExplosionRadius + config.ThrowableSelfSafetyPadding);
        if (selfSafetyDistance > 0f &&
            GetPlanarDistance(transform.position, targetPoint) < selfSafetyDistance)
        {
            return false;
        }

        return !IsThrowablePointUnsafeForAllies(targetPoint, throwable, config);
    }

    // Purpose: Checks whether throwable point unsafe for allies is currently true.
    private bool IsThrowablePointUnsafeForAllies(
        Vector3 throwTargetPoint,
        ThrowableUsableItemSO throwable,
        EnemyShooterConfigSO config)
    {
        if (throwable == null || config == null)
            return false;

        int mask = config.ThrowableAllySafetyMask.value;
        if (mask == 0)
            return false;

        float radius = Mathf.Max(
            0f,
            throwable.ExplosionRadius + config.ThrowableAllySafetyPadding);

        if (radius <= 0.001f)
            return false;

        Collider[] hits = Physics.OverlapSphere(
            throwTargetPoint,
            radius,
            mask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
                continue;

            Transform hitTransform = hit.transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                continue;

            DebugMovement(
                $"Throwable blocked by ally safety. ally={hit.name}, target={throwTargetPoint}, radius={radius}");

            return true;
        }

        return false;
    }

    // Purpose: Attempts to execute throwable attack and reports whether the request succeeded.
    private bool TryExecuteThrowableAttack()
    {
        EnemyShooterConfigSO config = Config;

        if (!CanUseThrowableAttack() ||
            !TryResolveThrowableTargetPoint(out Vector3 throwTargetPoint))
        {
            return false;
        }

        if (_lootInventory == null ||
            !_lootInventory.TryGetFirstCarriedItem(out ThrowableUsableItemSO throwable))
            return false;

        if (!_throwableAttack.TryThrowEnemyUsableWithSafeArc(
                throwable,
                throwTargetPoint,
                out string throwFailureReason))
        {
            // A blocked arc must not consume a carried throwable or re-enter the
            // Throwable state every frame. The normal state transition can now
            // fall back to chase/reposition while this short retry gate is open.
            _nextThrowableAllowedTime =
                Time.time + FailedThrowableRetrySeconds;
            DebugMovement(
                $"Throwable cancelled without consumption. usable={throwable.name}, " +
                $"target={throwTargetPoint}, retryAt={_nextThrowableAllowedTime:0.000}, " +
                $"reason={throwFailureReason ?? "unknown"}");
            return false;
        }

        _nextThrowableAllowedTime = Time.time + config.ThrowableCooldownSeconds;

        DebugMovement(
            $"Throwable thrown. usable={throwable.name}, target={throwTargetPoint}, cooldownUntil={_nextThrowableAllowedTime}");

        return true;
    }

    // Purpose: Attempts to resolve throwable target point and reports whether the request succeeded.
    private bool TryResolveThrowableTargetPoint(out Vector3 point)
    {
        point = default;

        EnemyShooterConfigSO config = Config;
        if (config == null)
            return false;

        bool recentlySawTarget =
            _lastSeenTime >= 0f &&
            Time.time - _lastSeenTime <= config.ThrowableRecentlyKnownSeconds;

        if (HasTarget && (CanSeeTarget || IsTargetLocked || recentlySawTarget))
        {
            point = _target.position;
            return true;
        }

        if (_hasLastKnownPosition &&
            _lastKnownSetTime >= 0f &&
            Time.time - _lastKnownSetTime <= config.ThrowableRecentlyKnownSeconds)
        {
            point = _lastKnownPosition;
            return true;
        }

        return false;
    }

    // Purpose: Returns the current planar distance.
    private static float GetPlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // Purpose: Attempts to acquire visible player and reports whether the request succeeded.
    private void TryAcquireVisiblePlayer(bool force)
    {
        EnemyShooterConfigSO config = Config;

        if (config == null)
            return;

        if (!force && Time.time < _nextTargetSearchTime)
            return;

        _nextTargetSearchTime = Time.time + config.TargetSearchInterval;

        PlayerStatsRuntime player = FindAnyObjectByType<PlayerStatsRuntime>();

        if (player == null)
            return;

        Transform playerTransform = player.transform;
        bool canSeePlayer = _vision == null || _vision.CanSeeTarget(playerTransform);

        if (!canSeePlayer)
            return;

        bool isPursuitReacquisition =
            _hasSeenTargetDuringCurrentEngagement && _hasLastKnownPosition;

        SetTarget(playerTransform);
        _isAlerted = true;
        _canSeeTarget = true;
        BeginDirectSightReaction(isPursuitReacquisition);
        RefreshTargetLock();
        _lastSeenTime = Time.time;
        SetLastKnownPosition(playerTransform.position);
        RefreshGoal();
    }

    // Purpose: Updates the target state.
    private void SetTarget(Transform target)
    {
        if (_target == target)
            return;

        ClearTarget(false);

        _target = target;

        if (_target != null)
        {
            _targetDamageable = _target.GetComponent<Damageable>() ?? _target.GetComponentInParent<Damageable>();
            _targetRigidbody = _target.GetComponent<Rigidbody>() ?? _target.GetComponentInParent<Rigidbody>();
            ShooterAimTargetAnchor aimTarget =
                EntityComponentResolver.Resolve<ShooterAimTargetAnchor>(_target);
            _targetAimAnchor = aimTarget != null ? aimTarget.Anchor : _target;
            RefreshTargetLock();
        }
    }

    // Purpose: Clears the current target state.
    private void ClearTarget(bool keepLastKnownPosition)
    {
        ReleaseCoverTactic();
        EnemyShooterTacticalIntentSelector.ReleaseDuelHolder(this);
        ResetTacticalIntent();

        _target = null;
        _targetDamageable = null;
        _targetRigidbody = null;
        _targetAimAnchor = null;
        _isAlerted = _hasSoundInvestigation;
        _canSeeTarget = false;
        _targetLockUntilTime = -1f;
        // Sound and last-known-target are independent clues. Treating a sound
        // as last-known target data created a fake destination at Vector3.zero.
        _hasLastKnownPosition =
            keepLastKnownPosition && _hasLastKnownPosition;
        ClearActiveReaction();
        _lastSeenTime = -1f;
        _burstShotsRemaining = 0;
        ResetBurstTracking();

        if (!keepLastKnownPosition)
            _hasSeenTargetDuringCurrentEngagement = false;

        if (!_hasLastKnownPosition)
        {
            _lastKnownPosition = Vector3.zero;
            _lastKnownSetTime = -1f;
        }
    }

    // Purpose: Clears the current sound investigation state.
    private void ClearSoundInvestigation()
    {
        _hasSoundInvestigation = false;
        _soundInvestigationPosition = Vector3.zero;
        _soundInvestigationMoveTime = 0f;
        _soundInvestigationSetTime = -1f;
        _soundInvestigationStimulusType = SoundStimulusType.Unknown;
        _soundInvestigationPriority = 0;
        _soundInvestigationEmitterId = 0;
        _soundInvestigationDirection = Vector3.zero;
        ClearQueuedSoundInvestigation();

        if (!HasTarget)
        {
            _isAlerted = _hasLastKnownPosition;
        }
    }

    // Purpose: Clears the current last known investigation state.
    private void ClearLastKnownInvestigation()
    {
        _hasLastKnownPosition = false;
        _lastKnownPosition = Vector3.zero;
        _lastKnownSetTime = -1f;

        if (!HasTarget)
        {
            ClearActiveReaction();
            _hasSeenTargetDuringCurrentEngagement = false;
        }

        if (!HasTarget && !_hasSoundInvestigation)
            _isAlerted = false;
    }

    // Purpose: Clears the current investigation state.
    private void ClearInvestigation()
    {
        ClearSoundInvestigation();
        ClearLastKnownInvestigation();
    }

    // Purpose: Clears the current investigation reentry cooldown state.
    private void ClearInvestigationReentryCooldown()
    {
        _ignoreInvestigationUntilTime = -1f;
    }

    // Purpose: Clears the current queued sound investigation state.
    private void ClearQueuedSoundInvestigation()
    {
        _hasQueuedSoundInvestigation = false;
        _queuedSoundInvestigationPosition = Vector3.zero;
        _queuedSoundInvestigationMoveTime = 0f;
        _queuedSoundInvestigationSetTime = -1f;
        _queuedSoundInvestigationStimulusType = SoundStimulusType.Unknown;
        _queuedSoundInvestigationPriority = 0;
        _queuedSoundInvestigationEmitterId = 0;
        _queuedSoundInvestigationDirection = Vector3.zero;
    }

    // Purpose: Updates the last known position state.
    private void SetLastKnownPosition(Vector3 position)
    {
        _lastKnownPosition = position;
        _hasLastKnownPosition = true;
        _lastKnownSetTime = Time.time;
    }

    // Purpose: Begins the direct sight reaction workflow.
    private void BeginDirectSightReaction(bool isPursuitReacquisition)
    {
        EnemyShooterConfigSO config = Config;

        _reactionStartedTime = Time.time;
        _activeReactionDelaySeconds = config == null
            ? 0f
            : isPursuitReacquisition
                ? config.PursuitReacquireReactionTime
                : config.PlayerDiscoveryReactionTime;
        _hasSeenTargetDuringCurrentEngagement = true;
    }

    // Purpose: Clears the current active reaction state.
    private void ClearActiveReaction()
    {
        _reactionStartedTime = -1f;
        _activeReactionDelaySeconds = 0f;
    }

    // Purpose: Updates the sound investigation state.
    private void SetSoundInvestigation(
        Vector3 position,
        float moveTime,
        float setTime,
        SoundStimulusType stimulusType,
        int priority,
        int emitterId,
        Vector3 direction,
        string reason)
    {
        _hasSoundInvestigation = true;
        _soundInvestigationPosition = position;
        _soundInvestigationMoveTime = moveTime;
        _soundInvestigationSetTime = setTime;
        _soundInvestigationStimulusType = stimulusType;
        _soundInvestigationPriority = priority;
        _soundInvestigationEmitterId = emitterId;
        _soundInvestigationDirection = direction;
        _nextInvestigationRetargetTime =
            Time.time + (Config != null ? Config.InvestigateRetargetCooldown : 0f);

        if (_currentGoal == EnemyShooterGoal.Investigate)
        {
            _investigationArrived = false;
            _investigationFinishTime = 0f;
        }

        DebugMovement(
            $"Sound investigation set. reason={reason}, type={stimulusType}, priority={priority}, " +
            $"emitterId={emitterId}, nav={position}, readyTime={moveTime}, retargetAfter={_nextInvestigationRetargetTime}");
    }

    // Purpose: Queues sound investigation for deferred processing.
    private bool QueueSoundInvestigation(
        Vector3 position,
        float moveTime,
        float setTime,
        SoundStimulusType stimulusType,
        int priority,
        int emitterId,
        Vector3 direction,
        string reason)
    {
        if (_hasQueuedSoundInvestigation &&
            priority < _queuedSoundInvestigationPriority)
        {
            return false;
        }

        _hasQueuedSoundInvestigation = true;
        _queuedSoundInvestigationPosition = position;
        _queuedSoundInvestigationMoveTime = moveTime;
        _queuedSoundInvestigationSetTime = setTime;
        _queuedSoundInvestigationStimulusType = stimulusType;
        _queuedSoundInvestigationPriority = priority;
        _queuedSoundInvestigationEmitterId = emitterId;
        _queuedSoundInvestigationDirection = direction;

        DebugMovement(
            $"Sound investigation queued. reason={reason}, type={stimulusType}, priority={priority}, " +
            $"emitterId={emitterId}, nav={position}, readyTime={moveTime}");

        return true;
    }

    // Purpose: Attempts to promote queued sound investigation and reports whether the request succeeded.
    private bool TryPromoteQueuedSoundInvestigation()
    {
        if (!_hasQueuedSoundInvestigation)
            return false;

        SetSoundInvestigation(
            _queuedSoundInvestigationPosition,
            _queuedSoundInvestigationMoveTime,
            _queuedSoundInvestigationSetTime,
            _queuedSoundInvestigationStimulusType,
            _queuedSoundInvestigationPriority,
            _queuedSoundInvestigationEmitterId,
            _queuedSoundInvestigationDirection,
            "Queued sound after investigation wait");

        ClearQueuedSoundInvestigation();
        _investigationArrived = false;
        _investigationFinishTime = 0f;
        _currentInvestigationType = EnemyShooterInvestigationType.Sound;
        _currentGoal = EnemyShooterGoal.Investigate;

        if (CanUseAgent())
            _agent.isStopped = false;

        return true;
    }

    // Purpose: Refreshes the displayed or cached target lock.
    private void RefreshTargetLock()
    {
        EnemyShooterConfigSO config = Config;

        if (config == null || config.TargetLockMinSeconds <= 0f)
            return;

        _targetLockUntilTime = Mathf.Max(
            _targetLockUntilTime,
            Time.time + config.TargetLockMinSeconds);
    }

    // Purpose: Updates the aim direction from the current runtime state.
    private void UpdateAimDirection()
    {
        EnemyShooterConfigSO config = Config;

        if (config == null || _target == null)
            return;

        Vector3 targetPoint = ResolveTargetPoint();
        Vector3 desired = targetPoint - transform.position;
        desired.y = 0f;

        if (desired.sqrMagnitude < 0.0001f)
            return;

        desired.Normalize();

        if (_aimDirection.sqrMagnitude < 0.0001f ||
            config.AimTrackingSpeedDegPerSecond <= 0f)
        {
            _aimDirection = desired;
            FaceDirection(_aimDirection);
            return;
        }

        _aimDirection = Vector3.RotateTowards(
            _aimDirection,
            desired,
            config.AimTrackingSpeedDegPerSecond * Mathf.Deg2Rad * Time.deltaTime,
            0f);

        FaceDirection(_aimDirection);
    }

    // Purpose: Resolves the target point needed by this component.
    private Vector3 ResolveTargetPoint()
    {
        EnemyShooterConfigSO config = Config;
        Vector3 targetPoint = _targetAimAnchor != null
            ? _targetAimAnchor.position
            : _target != null ? _target.position : transform.position;

        if (_targetRigidbody != null &&
            config != null &&
            config.TargetLeadSeconds > 0f)
        {
            targetPoint += _targetRigidbody.linearVelocity * config.TargetLeadSeconds;
        }

        return targetPoint;
    }

    // Purpose: Updates the burst fire from the current runtime state.
    private void UpdateBurstFire()
    {
        EnemyShooterConfigSO config = Config;

        if (config == null)
            return;

        if (_burstShotsRemaining <= 0)
        {
            if (Time.time < _nextFireTime)
                return;

            _burstShotsRemaining = config.BurstCount;
            _nextBurstShotTime = Time.time;
            ResetBurstTracking();
        }

        if (Time.time < _nextBurstShotTime)
            return;

        if (TryFireOnce())
        {
            _burstShotsRemaining--;
            _nextBurstShotTime = Time.time + config.BurstInterval;

            if (_burstShotsRemaining <= 0)
            {
                float interval = ResolveBurstCooldownSeconds();
                _nextFireTime = Time.time + interval;
                _nextCombatMoveRefreshTime = 0f;
                _attackPhase = EnemyShooterAttackPhase.BurstCooldown;
            }
        }
        else
        {
            _burstShotsRemaining = 0;
            _nextFireTime = Time.time + 0.1f;
            _attackPhase = EnemyShooterAttackPhase.BurstCooldown;
        }
    }

    // Purpose: Attempts to fire while moving to cover and reports whether the request succeeded.
    private void TryFireWhileMovingToCover()
    {
        if (!EffectiveFireWhileMovingToCover ||
            _coverTacticPhase != EnemyShooterCoverTacticPhase.MovingToCover ||
            !CanDirectlyAttackTarget ||
            ReactionTimeRemaining > 0f)
        {
            return;
        }

        UpdateBurstFire();
    }

    // Purpose: Processes one update step for attack movement.
    private void TickAttackMovement()
    {
        EnemyShooterConfigSO config = Config;

        if (config == null || !CanUseAgent() || _target == null)
            return;

        if (Time.time < _nextCombatMoveRefreshTime)
            return;

        _nextCombatMoveRefreshTime = Time.time + config.CombatMoveRefreshInterval;

        if (!TryResolveBurstIntervalMoveDestination(out Vector3 destination))
        {
            StopMovement();
            return;
        }

        TrySetAgentDestination(
            destination,
            0f,
            "Burst interval move");
    }

    // Purpose: Processes one update step for muzzle blocked reposition.
    private void TickMuzzleBlockedReposition()
    {
        EnemyShooterConfigSO config = Config;

        if (config == null || !CanUseAgent() || _target == null)
            return;

        if (Time.time < _nextCombatMoveRefreshTime)
            return;

        _nextCombatMoveRefreshTime = Time.time + config.CombatMoveRefreshInterval;

        if (!TryResolveMuzzleClearanceDestination(out Vector3 destination))
        {
            StopAgentHard("Muzzle blocked and no reposition destination");
            return;
        }

        TrySetAgentDestination(
            destination,
            0.05f,
            "Muzzle-clearance reposition");
    }
    // Purpose: Attempts to fire once and reports whether the request succeeded.
    private bool TryFireOnce()
    {
        EnemyShooterConfigSO config = Config;

        if (config == null || _shoot == null || _shoot.CurrentBullet == null || _target == null)
            return false;

        Vector3 targetPoint = ResolveTargetPoint();
        Vector3 shotOrigin = _shoot.TryGetAimOrigin(out Vector3 resolvedOrigin)
            ? resolvedOrigin
            : transform.position;
        Vector3 direction = targetPoint - shotOrigin;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = _aimDirection;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude < 0.0001f)
            return false;

        direction.Normalize();

        float fullSpreadAngle = _statsRuntime != null
            ? _statsRuntime.HipFireSpreadAngleDeg
            : config.HipFireSpreadAngleDeg;

        if (fullSpreadAngle > 0f)
        {
            float halfSpreadAngle = fullSpreadAngle * 0.5f;
            float error = Random.Range(-halfSpreadAngle, halfSpreadAngle);
            direction = Quaternion.AngleAxis(error, Vector3.up) * direction;
        }

        // The endpoint follows the current target even through cover only while
        // an already-started burst is being completed. Projectile collision still
        // owns the actual obstruction hit, and EntityShootCore owns max range.
        Vector3 planarTargetOffset = targetPoint - shotOrigin;
        planarTargetOffset.y = 0f;
        float targetDistance = Mathf.Max(0.01f, planarTargetOffset.magnitude);
        Vector3 aimPoint = shotOrigin + direction.normalized * targetDistance;
        bool fired = _shoot.TryFireAt(aimPoint);

        if (fired)
            _hasFiredCurrentBurst = true;

        return fired;
    }

    // Purpose: Performs the reset burst tracking workflow for this component.
    private void ResetBurstTracking()
    {
        _hasFiredCurrentBurst = false;
    }

    // Purpose: Attempts to start defensive dash and reports whether the request succeeded.
    private bool TryStartDefensiveDash()
    {
        return TryStartDefensiveDash(Vector3.zero);
    }

    // Purpose: Attempts to start defensive dash and reports whether the request succeeded.
    private bool TryStartDefensiveDash(Vector3 preferredDirection)
    {
        EnemyShooterConfigSO config = Config;

        if (config == null || !EffectiveUseDash || _dashController == null || _dashController.IsDashing)
            return false;

        Vector3 direction = preferredDirection.sqrMagnitude > 0.0001f
            ? preferredDirection.normalized
            : ResolveDashDirection(config);
        if (direction.sqrMagnitude < 0.0001f)
            return false;

        if (CanUseAgent())
        {
            _agent.ResetPath();
            _agent.isStopped = false;
        }

        bool started = _dashController.TryStartDefensiveDash(direction);

        if (!started)
            return false;

        _effects?.SetFootTrailActive(false);
        FaceDirection(_dashController.DashDirection);
        return true;
    }

    // Purpose: Processes one update step for dash movement.
    private bool TickDashMovement()
    {
        if (_dashController == null || !_dashController.IsDashing)
            return false;

        if (!CanUseAgent())
        {
            _dashController.CancelDash();
            DebugMovement("Dash stopped because NavMeshAgent is unavailable or off NavMesh.");
            return false;
        }

        Vector3 velocity = _dashController.BuildDashMovementVector(0f);
        Vector3 movement = new Vector3(velocity.x, 0f, velocity.z) * Time.deltaTime;

        if (movement.sqrMagnitude <= 0.000001f)
            return true;

        EnemyShooterConfigSO config = Config;
        float sampleDistance = config != null ? config.NavMeshSampleDistance : 1f;
        Vector3 desiredPosition = transform.position + movement;

        if (NavMesh.SamplePosition(
                desiredPosition,
                out NavMeshHit hit,
                Mathf.Max(0.1f, sampleDistance),
                NavMesh.AllAreas))
        {
            if (NavMesh.Raycast(transform.position, hit.position, out NavMeshHit blockHit, NavMesh.AllAreas))
            {
                _dashController.CancelDash();
                DebugMovement($"Dash stopped by NavMesh obstacle. desired={desiredPosition}, hit={blockHit.position}");
                return false;
            }

            _agent.Move(hit.position - transform.position);
        }
        else
        {
            _dashController.CancelDash();
            DebugMovement($"Dash stopped because target position is outside NavMesh. desired={desiredPosition}");
            return false;
        }

        return true;
    }

    // Purpose: Resolves the dash direction needed by this component.
    private Vector3 ResolveDashDirection(EnemyShooterConfigSO config)
    {
        Vector3 direction = Vector3.zero;

        if (config.DashAwayFromTarget && _target != null)
            direction = transform.position - _target.position;
        else if (_hasLastKnownPosition)
            direction = transform.position - _lastKnownPosition;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
            return direction.normalized;

        Vector2 random = Random.insideUnitCircle;
        if (random.sqrMagnitude <= 0.0001f)
            random = Vector2.right;

        return new Vector3(random.x, 0f, random.y).normalized;
    }

    // Purpose: Attempts to dash when health low and reports whether the request succeeded.
    private void TryDashWhenHealthLow()
    {
        EnemyShooterConfigSO config = Config;

        if (config == null ||
            !EffectiveUseDash ||
            !EffectiveDashWhenHealthLow ||
            _damageable == null ||
            _damageable.MaxHealth <= 0f)
        {
            return;
        }

        float normalizedHealth = Mathf.Clamp01(_damageable.CurrentHealth / _damageable.MaxHealth);
        if (normalizedHealth > EffectiveDashHealthThreshold)
            return;

        TryStartDefensiveDash();
    }

    // Purpose: Resolves the shots per second needed by this component.
    private float ResolveShotsPerSecond()
    {
        EnemyShooterConfigSO config = Config;

        if (_statsRuntime != null)
            return Mathf.Max(0.01f, _statsRuntime.ResolveShotsPerSecond(_shoot != null ? _shoot.CurrentBullet : null));

        return config != null ? config.ShotsPerSecond : 0.01f;
    }

    // Purpose: Attempts to pick patrol destination and reports whether the request succeeded.
    private bool TryPickPatrolDestination(out Vector3 destination)
    {
        destination = transform.position;

        EnemyShooterConfigSO config = Config;

        if (config == null)
            return false;

        float radius = Mathf.Max(0f, config.PatrolRadius);

        if (radius <= 0f)
            return false;

        for (int i = 0; i < 8; i++)
        {
            Vector2 random = Random.insideUnitCircle * radius;
            Vector3 candidate = _homePosition + new Vector3(random.x, 0f, random.y);

            if (NavMesh.SamplePosition(
                    candidate,
                    out NavMeshHit hit,
                    Mathf.Max(0.1f, config.NavMeshSampleDistance),
                    NavMesh.AllAreas))
            {
                destination = hit.position;
                return true;
            }
        }

        return false;
    }

    // Purpose: Refreshes the displayed or cached incoming throwable threat.
    private void RefreshIncomingThrowableThreat()
    {
        if (Time.time < _nextIncomingThrowableCheckTime)
            return;

        _nextIncomingThrowableCheckTime =
            Time.time + IncomingThrowableCheckInterval;

        bool found = ThrowableProjectile.TryGetClosestHostileThreat(
            gameObject,
            ShooterFaction.Enemy,
            transform.position,
            IncomingThrowableSafetyPadding,
            out Vector3 dangerCenter,
            out float dangerRadius,
            out int projectileId);

        if (!found)
        {
            ClearIncomingThrowableEvasion();
            return;
        }

        bool isNewThreat =
            !_isEvadingIncomingThrowable ||
            _incomingThrowableId != projectileId;

        if (isNewThreat)
            _incomingThrowableEscapeDestination = default;

        _isEvadingIncomingThrowable = true;
        _incomingThrowableId = projectileId;
        _incomingThrowableDangerCenter = dangerCenter;
        _incomingThrowableDangerRadius = dangerRadius;
        _isAlerted = true;

        if (isNewThreat)
            AbortCurrentAttackForIncomingThrowable();

        if (TryResolveIncomingThrowableEscapeDestination(
                dangerCenter,
                dangerRadius,
                out Vector3 escapeDestination))
        {
            _incomingThrowableEscapeDestination = escapeDestination;
        }
    }

    // Purpose: Attempts to tick incoming throwable evasion and reports whether the request succeeded.
    private bool TryTickIncomingThrowableEvasion()
    {
        RefreshIncomingThrowableThreat();

        if (!_isEvadingIncomingThrowable)
        {
            TryDashFromIncomingBullet();
            return false;
        }

        if (!CanUseAgent())
        {
            DebugMovement(
                $"Incoming throwable detected but agent cannot move. " +
                $"projectileId={_incomingThrowableId}, center={_incomingThrowableDangerCenter}");
            return true;
        }

        _agent.speed = ResolveMoveSpeed();
        _agent.stoppingDistance = 0f;

        bool needsDestination =
            _incomingThrowableEscapeDestination == default ||
            HasReached(_incomingThrowableEscapeDestination, 0.25f);

        if (needsDestination)
        {
            if (!TryResolveIncomingThrowableEscapeDestination(
                    _incomingThrowableDangerCenter,
                    _incomingThrowableDangerRadius,
                    out Vector3 refreshedDestination))
            {
                StopAgentHard("Incoming throwable has no reachable escape destination");
                return true;
            }

            _incomingThrowableEscapeDestination = refreshedDestination;
        }

        TrySetAgentDestination(
            _incomingThrowableEscapeDestination,
            0f,
            "Incoming throwable evasion");
        return true;
    }

    // Purpose: Cancels the active abort current attack for incoming throwable workflow.
    private void AbortCurrentAttackForIncomingThrowable()
    {
        // A shot is synchronous: by the time the throwable is observed, the
        // currently executing shot has already completed. Remove every queued
        // burst shot so the next frame belongs to emergency movement.
        _burstShotsRemaining = 0;
        _nextBurstShotTime = 0f;
        ResetBurstTracking();

        if (_attackActive)
        {
            _attackPhase = EnemyShooterAttackPhase.BurstCooldown;
            _nextFireTime = Mathf.Max(_nextFireTime, Time.time + 0.1f);
        }

        if (_meleeActionActive)
            _meleeActionFinishTime = Time.time;

        if (_throwableActionActive)
            _throwableActionFinishTime = Time.time;

        _dashController?.CancelDash();
        ClearNavigationRequest();

        DebugMovement(
            $"Incoming throwable preempted combat. projectileId={_incomingThrowableId}, " +
            $"dangerCenter={_incomingThrowableDangerCenter}, dangerRadius={_incomingThrowableDangerRadius:0.00}");
    }

    // Purpose: Attempts to resolve incoming throwable escape destination and reports whether the request succeeded.
    private bool TryResolveIncomingThrowableEscapeDestination(
        Vector3 dangerCenter,
        float dangerRadius,
        out Vector3 destination)
    {
        destination = default;

        if (!CanUseAgent())
            return false;

        Vector3 away = transform.position - dangerCenter;
        away.y = 0f;

        if (away.sqrMagnitude < 0.0001f)
            away = transform.forward;

        away.y = 0f;

        if (away.sqrMagnitude < 0.0001f)
            away = Vector3.forward;

        away.Normalize();

        float escapeDistance = Mathf.Max(
            IncomingThrowableMinimumEscapeDistance,
            dangerRadius + 0.75f);
        float bestThreat = float.PositiveInfinity;
        float bestDangerDistance = float.NegativeInfinity;
        bool found = false;

        for (int i = 0; i < IncomingThrowableEscapeSampleCount; i++)
        {
            float angle = i * (360f / IncomingThrowableEscapeSampleCount);
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * away;
            Vector3 rawCandidate = transform.position + direction * escapeDistance;

            if (!TryProjectToNavMesh(rawCandidate, out Vector3 candidate))
                continue;

            // Only score a point the agent can actually reach. This prevents an
            // apparently safe point on the opposite side of a structure from
            // winning while the shooter remains trapped inside the blast area.
            _navigationPath.ClearCorners();

            if (!_agent.CalculatePath(candidate, _navigationPath) ||
                _navigationPath.status == NavMeshPathStatus.PathInvalid)
            {
                continue;
            }

            Vector3[] corners = _navigationPath.corners;

            if (_navigationPath.status == NavMeshPathStatus.PathPartial)
            {
                if (corners == null || corners.Length == 0)
                    continue;

                candidate = corners[corners.Length - 1];
            }

            float threat = ThrowableProjectile.EvaluateHostileThreatAt(
                gameObject,
                ShooterFaction.Enemy,
                candidate,
                IncomingThrowableSafetyPadding);
            float dangerDistance = PlanarSqrDistance(candidate, dangerCenter);

            bool isBetter =
                !found ||
                threat < bestThreat - 0.001f ||
                (Mathf.Abs(threat - bestThreat) <= 0.001f &&
                 dangerDistance > bestDangerDistance);

            if (!isBetter)
                continue;

            destination = candidate;
            bestThreat = threat;
            bestDangerDistance = dangerDistance;
            found = true;

            if (threat <= 0f && i == 0)
                break;
        }

        return found;
    }

    // Purpose: Clears the current incoming throwable evasion state.
    private void ClearIncomingThrowableEvasion()
    {
        _isEvadingIncomingThrowable = false;
        _incomingThrowableId = 0;
        _incomingThrowableDangerCenter = default;
        _incomingThrowableDangerRadius = 0f;
        _incomingThrowableEscapeDestination = default;
    }

    // Purpose: Attempts to dash from incoming bullet and reports whether the request succeeded.
    private void TryDashFromIncomingBullet()
    {
        if (Time.time < _nextIncomingBulletCheckTime)
            return;

        _nextIncomingBulletCheckTime = Time.time + IncomingBulletCheckInterval;

        if (!EffectiveDashOnIncomingProjectile ||
            _damageable == null ||
            _damageable.IsDead ||
            IsDashing)
        {
            return;
        }

        if (!BasicBullet.TryGetClosestHostileIncomingThreat(
                gameObject,
                ShooterFaction.Enemy,
                IncomingBulletPredictionSeconds,
                out Vector3 incomingDirection,
                out int projectileId))
        {
            _lastIncomingBulletThreatId = 0;
            return;
        }

        if (_lastIncomingBulletThreatId == projectileId)
            return;

        _lastIncomingBulletThreatId = projectileId;

        if (Random.value > EffectiveDashChanceOnIncomingProjectile)
            return;

        TryStartDefensiveDash(ResolveIncomingBulletDashDirection(incomingDirection));
    }

    // Purpose: Resolves the incoming bullet dash direction needed by this component.
    private Vector3 ResolveIncomingBulletDashDirection(Vector3 incomingDirection)
    {
        incomingDirection.y = 0f;
        if (incomingDirection.sqrMagnitude <= 0.0001f)
            return Vector3.zero;

        incomingDirection.Normalize();
        Vector3 left = new Vector3(-incomingDirection.z, 0f, incomingDirection.x);
        Vector3 right = -left;

        Vector3 first = Random.value < 0.5f ? left : right;
        Vector3 second = first == left ? right : left;

        if (CanDashInDirection(first))
            return first;

        return CanDashInDirection(second) ? second : Vector3.zero;
    }

    // Purpose: Checks whether dash in direction is currently permitted.
    private bool CanDashInDirection(Vector3 direction)
    {
        if (!CanUseAgent() || direction.sqrMagnitude <= 0.0001f)
            return false;

        EnemyShooterConfigSO config = Config;
        float distance = config != null
            ? config.DashSpeed * config.DashDurationSeconds
            : 0f;
        Vector3 desiredPosition = transform.position + direction.normalized * Mathf.Max(0.5f, distance);

        if (!NavMesh.SamplePosition(
                desiredPosition,
                out NavMeshHit hit,
                Mathf.Max(0.1f, config != null ? config.NavMeshSampleDistance : 1f),
                NavMesh.AllAreas))
        {
            return false;
        }

        return !NavMesh.Raycast(transform.position, hit.position, out _, NavMesh.AllAreas);
    }

    // Purpose: Attempts to get harmful area avoidance destination and reports whether the request succeeded.
    private bool TryGetHarmfulAreaAvoidanceDestination(out Vector3 destination)
    {
        destination = default;

        EnemyShooterConfigSO config = Config;

        if (config == null || !config.AvoidHarmfulAreas)
            return false;

        if (_attackActive && !config.AvoidHarmfulAreasWhileAttacking)
            return false;

        if (Time.time < _nextHarmfulAreaCheckTime)
            return false;

        _nextHarmfulAreaCheckTime = Time.time + config.HarmfulAreaCheckInterval;

        float currentThreat = EvaluateHarmfulAreaThreat(transform.position);

        if (currentThreat <= 0f)
            return false;

        int sampleCount = Mathf.Max(1, config.HarmfulAreaSampleCount);
        float radius = Mathf.Max(0.1f, config.HarmfulAreaSearchRadius);
        bool found = false;
        float bestThreat = float.PositiveInfinity;

        for (int i = 0; i < sampleCount; i++)
        {
            float angle = (i / (float)sampleCount) * Mathf.PI * 2f;
            Vector3 raw = transform.position + new Vector3(
                Mathf.Cos(angle),
                0f,
                Mathf.Sin(angle)) * radius;

            if (!NavMesh.SamplePosition(
                    raw,
                    out NavMeshHit hit,
                    config.HarmfulAreaNavMeshSampleDistance,
                    NavMesh.AllAreas))
            {
                continue;
            }

            float candidateThreat = EvaluateHarmfulAreaThreat(hit.position);

            if (candidateThreat >= bestThreat - 0.001f)
                continue;

            destination = hit.position;
            bestThreat = candidateThreat;
            found = true;

            if (candidateThreat <= 0f)
                break;
        }

        // A fully surrounded shooter still takes the least-dangerous NavMesh
        // route. Treating a harmful area as an obstacle would otherwise freeze it.
        return found;
    }

    // Purpose: Calculates harmful area threat from the current inputs.
    private float EvaluateHarmfulAreaThreat(Vector3 worldPosition)
    {
        return SpecialAreaRuntime.EvaluateCombinedThreat(gameObject, worldPosition);
    }

    // Purpose: Checks whether this component has reached.
    private bool HasReached(Vector3 point, float arriveDistance)
    {
        Vector3 from = transform.position;
        from.y = 0f;
        point.y = 0f;

        return Vector3.Distance(from, point) <= Mathf.Max(0f, arriveDistance);
    }

    // Purpose: Checks whether this component has forgotten target.
    private bool HasForgottenTarget()
    {
        EnemyShooterConfigSO config = Config;

        if (config == null || _lastSeenTime < 0f)
            return false;

        if (IsTargetLocked)
            return false;

        if (IsTargetBeyondChaseLoseRange(config))
            return true;

        return config.TargetMemorySeconds > 0f &&
               Time.time >= _lastSeenTime + config.TargetMemorySeconds;
    }

    // Purpose: Refreshes the displayed or cached goal.
    private void RefreshGoal()
    {
        EnemyShooterGoal previousGoal = _currentGoal;
        _currentInvestigationType = ResolveInvestigationType();

        if (CanAttackTarget)
        {
            _currentGoal = EnemyShooterGoal.AttackTarget;
        }
        else if (ShouldChaseTarget)
        {
            _currentGoal = EnemyShooterGoal.ChaseTarget;
        }
        else if (_currentInvestigationType != EnemyShooterInvestigationType.None)
        {
            _currentGoal = EnemyShooterGoal.Investigate;
        }
        else
        {
            _currentGoal = EnemyShooterGoal.Patrol;
        }

        if (_debugMovement && previousGoal != _currentGoal)
        {
            Debug.Log(
                $"[EnemyShooterCombatController] Goal changed. {previousGoal} -> {_currentGoal}, " +
                $"hasTarget={HasTarget}, canSee={CanSeeTarget}, targetLocked={IsTargetLocked}, " +
                $"canAttack={CanAttackTarget}, investigation={_currentInvestigationType}, " +
                $"hasSound={_hasSoundInvestigation}, hasLastKnown={_hasLastKnownPosition}, " +
                $"forgotten={HasForgottenTarget()}",
                this);
        }
    }

    // Purpose: Resolves the investigation type needed by this component.
    private EnemyShooterInvestigationType ResolveInvestigationType()
    {
        // While the target is visible or still inside the guaranteed lock window,
        // combat tracking wins. A stale target reference alone must not suppress
        // sound or last-known-position investigation.
        if (HasTarget && (CanSeeTarget || IsTargetLocked))
            return EnemyShooterInvestigationType.None;

        EnemyShooterConfigSO config = Config;

        if (_hasSoundInvestigation)
        {
            float readyTime = _soundInvestigationMoveTime;
            float memorySeconds = config != null ? config.SoundInvestigationMemorySeconds : 0f;
            bool expired = memorySeconds > 0f && Time.time > readyTime + memorySeconds;

            // Hearing and looking happen immediately. The ready time delays only
            // NavMesh movement, so the FSM and the '?' icon can react at once.
            if (!expired)
                return EnemyShooterInvestigationType.Sound;

            if (expired)
                ClearSoundInvestigation();
        }

        if (_hasLastKnownPosition)
        {
            float memorySeconds = config != null ? config.LastKnownInvestigationSeconds : 0f;
            bool expired =
                memorySeconds > 0f &&
                _lastKnownSetTime >= 0f &&
                Time.time > _lastKnownSetTime + memorySeconds;

            if (!expired)
                return EnemyShooterInvestigationType.LastKnownTarget;

            ClearLastKnownInvestigation();
        }

        return EnemyShooterInvestigationType.None;
    }

    // Purpose: Attempts to resolve investigation destination and reports whether the request succeeded.
    private bool TryResolveInvestigationDestination(out Vector3 destination)
    {
        destination = transform.position;

        switch (ResolveInvestigationType())
        {
            case EnemyShooterInvestigationType.Sound:
                destination = _soundInvestigationPosition;
                return true;
            case EnemyShooterInvestigationType.LastKnownTarget:
                destination = _lastKnownPosition;
                return true;
            default:
                return false;
        }
    }

    // Purpose: Checks whether target beyond chase lose range is currently true.
    private bool IsTargetBeyondChaseLoseRange(EnemyShooterConfigSO config)
    {
        if (config == null || config.ChaseLoseRange <= 0f || _target == null)
            return false;

        Vector3 from = transform.position;
        Vector3 to = _target.position;
        from.y = 0f;
        to.y = 0f;

        return Vector3.Distance(from, to) > config.ChaseLoseRange;
    }

    // Purpose: Attempts to normalize planar and reports whether the request succeeded.
    private static bool TryNormalizePlanar(Vector3 value, out Vector3 direction)
    {
        value.y = 0f;

        if (value.sqrMagnitude <= 0.0001f)
        {
            direction = default;
            return false;
        }

        direction = value.normalized;
        return true;
    }

    // Purpose: Resolves the investigate arrive distance needed by this component.
    private float ResolveInvestigateArriveDistance()
    {
        EnemyShooterConfigSO config = Config;
        return config != null ? config.InvestigateArriveDistance : 0.5f;
    }

    // Purpose: Resolves the investigation wait seconds needed by this component.
    private float ResolveInvestigationWaitSeconds(EnemyShooterConfigSO config)
    {
        if (config == null)
            return 0f;

        return Mathf.Max(
            config.InvestigateSearchSeconds,
            config.InvestigateMinimumCommitSeconds);
    }

    // Purpose: Resolves the burst cooldown seconds needed by this component.
    private float ResolveBurstCooldownSeconds()
    {
        EnemyShooterConfigSO config = Config;
        float fallback = 1f / ResolveShotsPerSecond();

        if (config == null || config.BurstCooldownSeconds <= 0f)
            return fallback;

        return config.BurstCooldownSeconds;
    }

    // Purpose: Attempts to resolve burst interval move destination and reports whether the request succeeded.
    private bool TryResolveBurstIntervalMoveDestination(out Vector3 destination)
    {
        destination = transform.position;

        if (_target == null)
            return false;

        EnemyShooterConfigSO config = Config;
        if (config == null)
            return false;

        Vector3 fromTarget = transform.position - _target.position;
        fromTarget.y = 0f;

        if (fromTarget.sqrMagnitude < 0.0001f)
            fromTarget = -transform.forward;

        Vector3 away = fromTarget.normalized;
        Vector3 right = new Vector3(away.z, 0f, -away.x) * _attackStrafeSign;
        float currentDistance = GetPlanarDistanceToTarget();
        float preferredDistance = Mathf.Max(0f, config.AttackPreferredDistance);
        float strafeDistance = Mathf.Max(0f, config.AttackStrafeDistance);

        Vector3 moveDirection;

        if (preferredDistance > 0f && currentDistance < preferredDistance * 0.8f)
            moveDirection = away;
        else if (preferredDistance > 0f && currentDistance > preferredDistance * 1.2f)
            moveDirection = -away;
        else
            moveDirection = right;

        Vector3 rawDestination = transform.position + moveDirection.normalized * Mathf.Max(0.25f, strafeDistance);

        if (!TryProjectToNavMesh(rawDestination, out destination))
            return false;

        return true;
    }

    // Purpose: Attempts to resolve muzzle clearance destination and reports whether the request succeeded.
    private bool TryResolveMuzzleClearanceDestination(out Vector3 destination)
    {
        destination = transform.position;

        EnemyShooterConfigSO config = Config;

        if (config == null || _target == null || _shoot == null)
            return false;

        Vector3 fromTarget = transform.position - _target.position;
        fromTarget.y = 0f;

        if (fromTarget.sqrMagnitude < 0.0001f)
            fromTarget = -transform.forward;

        Vector3 away = fromTarget.normalized;
        Vector3 right = new Vector3(away.z, 0f, -away.x);
        Vector3 targetPoint = ResolveTargetPoint();
        float baseDistance = Mathf.Max(1f, config.AttackStrafeDistance);
        float keepRange = ResolveAttackKeepRange();
        bool foundClearCandidate = false;
        float bestDistanceSqr = float.PositiveInfinity;
        int bestSign = _attackStrafeSign;

        // Prefer a short lateral step, then widen/angle away when nearby cover is large.
        for (int distancePass = 0; distancePass < 2; distancePass++)
        {
            float moveDistance = baseDistance * (distancePass == 0 ? 0.5f : 1f);

            for (int directionPass = 0; directionPass < 5; directionPass++)
            {
                int sign = directionPass % 2 == 0
                    ? _attackStrafeSign
                    : -_attackStrafeSign;
                Vector3 moveDirection;

                if (directionPass <= 1)
                    moveDirection = right * sign;
                else if (directionPass <= 3)
                    moveDirection = (right * sign + away).normalized;
                else
                    moveDirection = away;

                Vector3 rawCandidate = transform.position + moveDirection * moveDistance;

                if (!TryProjectToNavMesh(rawCandidate, out Vector3 candidate))
                    continue;

                if (PlanarSqrDistance(candidate, transform.position) < 0.0625f)
                    continue;

                if (keepRange > 0f && PlanarSqrDistance(candidate, _target.position) > keepRange * keepRange)
                    continue;

                if (!_shoot.HasClearBallisticPathFromRootPosition(
                        candidate,
                        targetPoint,
                        _target))
                {
                    continue;
                }

                float candidateDistanceSqr = PlanarSqrDistance(candidate, transform.position);

                if (candidateDistanceSqr >= bestDistanceSqr)
                    continue;

                destination = candidate;
                bestDistanceSqr = candidateDistanceSqr;
                bestSign = Vector3.Dot(candidate - transform.position, right) >= 0f ? 1 : -1;
                foundClearCandidate = true;
            }
        }

        if (foundClearCandidate)
        {
            _attackStrafeSign = bestSign;
            return true;
        }

        // If one step cannot clear a large obstacle, keep circling on a stable side;
        // the next refresh evaluates the translated muzzle again from the new position.
        for (int sidePass = 0; sidePass < 2; sidePass++)
        {
            int sign = sidePass == 0 ? _attackStrafeSign : -_attackStrafeSign;
            Vector3 rawCandidate = transform.position + right * sign * baseDistance;

            if (!TryProjectToNavMesh(rawCandidate, out Vector3 candidate) ||
                PlanarSqrDistance(candidate, transform.position) < 0.0625f)
            {
                continue;
            }

            destination = candidate;
            _attackStrafeSign = sign;
            return true;
        }

        return false;
    }

    // Purpose: Checks whether this component has clear muzzle path to target.
    private bool HasClearMuzzlePathToTarget()
    {
        if (!HasTarget || _shoot == null)
            return false;

        if (_muzzleClearanceFrame == Time.frameCount)
            return _muzzleClearanceResult;

        _muzzleClearanceFrame = Time.frameCount;
        _muzzleClearanceResult = _shoot.HasClearBallisticPathTo(
            ResolveTargetPoint(),
            _target);

        return _muzzleClearanceResult;
    }

    // Purpose: Returns the current planar distance to target.
    private float GetPlanarDistanceToTarget()
    {
        if (_target == null)
            return float.PositiveInfinity;

        Vector3 from = transform.position;
        Vector3 to = _target.position;
        from.y = 0f;
        to.y = 0f;

        return Vector3.Distance(from, to);
    }

    // Purpose: Resolves the move speed needed by this component.
    private float ResolveMoveSpeed()
    {
        if (_statsRuntime != null && _statsRuntime.MoveSpeed > 0f)
            return _statsRuntime.MoveSpeed;

        EnemyShooterConfigSO config = Config;
        return config != null ? config.MoveSpeed : (_agent != null ? _agent.speed : 0f);
    }

    // Purpose: Resolves the chase stopping distance needed by this component.
    private float ResolveChaseStoppingDistance()
    {
        EnemyShooterConfigSO config = Config;
        float configuredDistance = config != null
            ? config.ChaseStoppingDistance
            : 0f;
        float attackRange = AttackStartRange;

        if (attackRange <= 0f)
            return Mathf.Max(0f, configuredDistance);

        // Always enter the attack range by a small margin. This prevents a
        // NavMesh stopping radius from holding the shooter just outside the
        // range comparison due to different target/agent centers.
        float maximumStoppingDistance = Mathf.Max(0f, attackRange - 0.5f);
        return Mathf.Min(Mathf.Max(0f, configuredDistance), maximumStoppingDistance);
    }

    // Purpose: Resolves the attack start range needed by this component.
    private float ResolveAttackStartRange()
    {
        if (_statsRuntime != null && _statsRuntime.MaxRange > 0f)
            return _statsRuntime.MaxRange;

        EnemyShooterConfigSO config = Config;
        return config != null ? config.AttackStartRange : 0f;
    }

    // Purpose: Resolves the attack keep range needed by this component.
    private float ResolveAttackKeepRange()
    {
        EnemyShooterConfigSO config = Config;

        if (config == null)
            return ResolveAttackStartRange();

        if (Mathf.Approximately(config.AttackKeepRange, config.MaxRange) &&
            _statsRuntime != null &&
            _statsRuntime.MaxRange > 0f)
        {
            return _statsRuntime.MaxRange;
        }

        return config.AttackKeepRange;
    }

    // Purpose: Resolves the direct attack range needed by this component.
    private float ResolveDirectAttackRange(bool keepingAttack)
    {
        float range = keepingAttack
            ? ResolveAttackKeepRange()
            : ResolveAttackStartRange();

        EnemyShooterConfigSO config = Config;
        if (config == null ||
            config.AttackMovementMode != EnemyShooterAttackMovementMode.StopWhileAttacking ||
            !EffectiveUsePreferredRangeBeforeStationaryAttack ||
            config.AttackPreferredDistance <= 0f)
        {
            return range;
        }

        float preferredRange = config.AttackPreferredDistance;
        if (keepingAttack)
            preferredRange += EffectiveStationaryAttackKeepRangePadding;

        return Mathf.Min(range, preferredRange);
    }

    // Purpose: Checks whether use agent is currently permitted.
    private bool CanUseAgent()
    {
        if (_agent == null || !_agent.isActiveAndEnabled)
            return false;

        if (_agent.isOnNavMesh)
            return true;

        DebugMovement(
            $"NavMeshAgent is off NavMesh. Movement skipped without runtime warp. pos={transform.position}, {BuildAgentDebugInfo()}");

        return false;
    }

    // Purpose: Configures this component''s agent.
    private void ConfigureAgent()
    {
        if (_agent == null)
            return;

        _agent.updatePosition = true;
        _agent.updateRotation = false;
        _agent.autoRepath = true;
        _agent.autoBraking = true;

        EnemyShooterConfigSO config = Config;
        if (config != null)
            _agent.acceleration = config.NavMeshAcceleration;
    }

    // Purpose: Stops the active agent hard behavior.
    private void StopAgentHard(string reason)
    {
        if (!CanUseAgent())
            return;

        _agent.isStopped = true;
        _agent.ResetPath();
        _agent.velocity = Vector3.zero;
        _agent.nextPosition = transform.position;
        ClearNavigationRequest();

        DebugMovement(
            $"Agent hard stopped. reason={reason}, pos={transform.position}, {BuildAgentDebugInfo()}");
    }

    // Purpose: Aligns this component toward direction.
    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    // Purpose: Responds to the health changed callback.
    private void OnHealthChanged(Damageable damageable)
    {
        if (damageable == null || damageable != _damageable)
            return;

        _lastHealth = damageable.CurrentHealth;
    }

    // Purpose: Responds to the hit received callback.
    private void OnHitReceived(GameObject hitTarget)
    {
        if (hitTarget == null)
            return;

        if (!IsThisShooter(hitTarget))
            return;

        // HitReceivedEvent has no attacker context. Do not globally alert from here.
    }

    // Purpose: Responds to the sound stimulus callback.
    private void OnSoundStimulus(SoundStimulus stimulus)
    {
        if (!stimulus.IsValid)
            return;

        EnemyShooterConfigSO config = Config;
        int priority = ResolveSoundStimulusPriority(stimulus.type);

        Vector3 from = transform.position;
        Vector3 to = stimulus.position;
        from.y = 0f;
        to.y = 0f;
        float distance = Vector3.Distance(from, to);

        if (priority <= 0)
        {
            DebugSoundStimulus(stimulus, distance, priority, "Rejected", "unsupported stimulus type");
            return;
        }

        if (IsOwnSource(stimulus.source))
        {
            DebugSoundStimulus(stimulus, distance, priority, "Rejected", "own source");
            return;
        }

        if (ShouldIgnoreSoundStimulus(stimulus, config))
        {
            DebugSoundStimulus(stimulus, distance, priority, "Rejected", "friendly source");
            return;
        }

        if (stimulus.type != SoundStimulusType.Footstep &&
            Time.time < _ignoreInvestigationUntilTime &&
            !CanBypassInvestigationReentry(priority))
        {
            DebugSoundStimulus(
                stimulus,
                distance,
                priority,
                "Rejected",
                $"investigation reenter cooldown until {_ignoreInvestigationUntilTime:0.00}");
            return;
        }

        if (distance > stimulus.radius)
        {
            DebugSoundStimulus(stimulus, distance, priority, "Rejected", "outside hearing radius");
            return;
        }

        if (!TryProjectToNavMesh(stimulus.position, out Vector3 navPosition))
        {
            DebugSoundStimulus(stimulus, distance, priority, "Rejected", "position cannot project to NavMesh");
            return;
        }

        ExpireSoundInvestigationIfNeeded(config);
        ClearInvestigationReentryCooldown();
        float readyTime = Time.time + stimulus.investigateDelaySeconds;
        bool isWaitingAtInvestigationPoint =
            _currentGoal == EnemyShooterGoal.Investigate &&
            _investigationArrived;
        bool shouldQueue =
            (isWaitingAtInvestigationPoint &&
             config != null &&
             config.QueueSoundDuringInvestigationWait) ||
            (_currentGoal == EnemyShooterGoal.Investigate &&
             Time.time < _nextInvestigationRetargetTime);
        bool forceReplace =
            !_hasSoundInvestigation ||
            priority > _soundInvestigationPriority ||
            priority >= ResolveSoundStimulusPriority(SoundStimulusType.Gunshot);
        string result;
        string reason;

        if (_hasSoundInvestigation && priority < _soundInvestigationPriority)
        {
            DebugSoundStimulus(
                stimulus,
                distance,
                priority,
                "Rejected",
                $"lower priority than active {_soundInvestigationStimulusType}({_soundInvestigationPriority})");
            return;
        }

        if (!forceReplace &&
            shouldQueue &&
            _hasQueuedSoundInvestigation &&
            priority < _queuedSoundInvestigationPriority)
        {
            DebugSoundStimulus(
                stimulus,
                distance,
                priority,
                "Rejected",
                $"lower priority than queued {_queuedSoundInvestigationStimulusType}({_queuedSoundInvestigationPriority})");
            return;
        }

        if (!TryRegisterSoundStimulus(stimulus, config, out float duplicateUntil))
        {
            DebugSoundStimulus(
                stimulus,
                distance,
                priority,
                "Rejected",
                $"duplicate projectile stimulus until {duplicateUntil:0.00}");
            return;
        }

        if (forceReplace)
        {
            string replacementReason = !_hasSoundInvestigation
                ? "no active sound clue"
                : priority > _soundInvestigationPriority
                    ? $"higher priority than active {_soundInvestigationStimulusType}({_soundInvestigationPriority})"
                    : "immediate-response sound tier";

            SetSoundInvestigation(
                navPosition,
                readyTime,
                Time.time,
                stimulus.type,
                priority,
                stimulus.emitterId,
                stimulus.direction,
                replacementReason);
            result = "Accepted";
            reason = replacementReason;
        }
        else if (shouldQueue)
        {
            bool queued = QueueSoundInvestigation(
                navPosition,
                readyTime,
                Time.time,
                stimulus.type,
                priority,
                stimulus.emitterId,
                stimulus.direction,
                "investigation wait/retarget cooldown");

            if (!queued)
            {
                DebugSoundStimulus(
                    stimulus,
                    distance,
                    priority,
                    "Rejected",
                    $"lower priority than queued {_queuedSoundInvestigationStimulusType}({_queuedSoundInvestigationPriority})");
                return;
            }

            result = "Queued";
            reason = "investigation wait or retarget cooldown";
        }
        else
        {
            SetSoundInvestigation(
                navPosition,
                readyTime,
                Time.time,
                stimulus.type,
                priority,
                stimulus.emitterId,
                stimulus.direction,
                "same-priority clue retarget");
            result = "Accepted";
            reason = "same-priority clue retargeted";
        }

        _isAlerted = true;
        RefreshGoal();

        Vector3 lookDirection = stimulus.HasDirection &&
                                (stimulus.type == SoundStimulusType.ProjectileFlyby ||
                                 stimulus.type == SoundStimulusType.ProjectileImpact ||
                                 stimulus.type == SoundStimulusType.Impact)
            ? -stimulus.direction
            : navPosition - transform.position;

        FaceDirection(lookDirection);

        DebugSoundStimulus(stimulus, distance, priority, result, reason);
    }

    // Purpose: Performs the should ignore sound stimulus workflow for this component.
    private bool ShouldIgnoreSoundStimulus(
        SoundStimulus stimulus,
        EnemyShooterConfigSO config)
    {
        GameObject source = stimulus.source;

        if (source == null)
            return false;

        if (config != null && config.IgnoreEnemySoundStimuli)
        {
            Enemy sourceEnemy =
                source.GetComponent<Enemy>() ??
                source.GetComponentInParent<Enemy>();

            if (sourceEnemy != null)
            {
                DebugMovement(
                    $"Sound stimulus ignored because source is enemy. type={stimulus.type}, source={source.name}");
                return true;
            }
        }

        return false;
    }

    // Purpose: Resolves the sound stimulus priority needed by this component.
    private static int ResolveSoundStimulusPriority(SoundStimulusType type)
    {
        return type switch
        {
            SoundStimulusType.Explosion => 500,
            SoundStimulusType.Gunshot => 400,
            SoundStimulusType.ProjectileImpact => 300,
            SoundStimulusType.Impact => 300,
            SoundStimulusType.ProjectileFlyby => 200,
            SoundStimulusType.Footstep => 100,
            _ => 0
        };
    }

    // Purpose: Checks whether bypass investigation reentry is currently permitted.
    private static bool CanBypassInvestigationReentry(int priority)
    {
        return priority > ResolveSoundStimulusPriority(SoundStimulusType.Footstep);
    }

    // Purpose: Attempts to register sound stimulus and reports whether the request succeeded.
    private bool TryRegisterSoundStimulus(
        SoundStimulus stimulus,
        EnemyShooterConfigSO config,
        out float duplicateUntil)
    {
        duplicateUntil = -1f;

        if (!stimulus.HasEmitter)
            return true;

        float cooldown = config != null
            ? config.SoundStimulusDuplicateCooldown
            : 0f;

        if (cooldown <= 0f)
            return true;

        CleanupExpiredSoundStimulusKeys();

        long key = BuildSoundStimulusKey(stimulus.emitterId, stimulus.type);

        if (_soundStimulusDuplicateUntil.TryGetValue(key, out duplicateUntil) &&
            Time.time < duplicateUntil)
        {
            return false;
        }

        duplicateUntil = Time.time + cooldown;
        _soundStimulusDuplicateUntil[key] = duplicateUntil;
        return true;
    }

    // Purpose: Performs the cleanup expired sound stimulus keys workflow for this component.
    private void CleanupExpiredSoundStimulusKeys()
    {
        if (Time.time < _nextSoundStimulusCleanupTime)
            return;

        _nextSoundStimulusCleanupTime = Time.time + 2f;
        _expiredSoundStimulusKeys.Clear();

        foreach (KeyValuePair<long, float> pair in _soundStimulusDuplicateUntil)
        {
            if (Time.time >= pair.Value)
                _expiredSoundStimulusKeys.Add(pair.Key);
        }

        for (int i = 0; i < _expiredSoundStimulusKeys.Count; i++)
            _soundStimulusDuplicateUntil.Remove(_expiredSoundStimulusKeys[i]);

        _expiredSoundStimulusKeys.Clear();
    }

    // Purpose: Builds the sound stimulus key from the current authored data.
    private static long BuildSoundStimulusKey(
        int emitterId,
        SoundStimulusType type)
    {
        return ((long)emitterId << 32) ^ (uint)type;
    }

    // Purpose: Performs the expire sound investigation if needed workflow for this component.
    private void ExpireSoundInvestigationIfNeeded(EnemyShooterConfigSO config)
    {
        if (!_hasSoundInvestigation)
            return;

        float memorySeconds = config != null
            ? config.SoundInvestigationMemorySeconds
            : 0f;

        if (memorySeconds > 0f &&
            Time.time > _soundInvestigationMoveTime + memorySeconds)
        {
            ClearSoundInvestigation();
        }
    }

    // Purpose: Performs the debug sound stimulus workflow for this component.
    private void DebugSoundStimulus(
        SoundStimulus stimulus,
        float distance,
        int priority,
        string result,
        string reason)
    {
        _lastSoundStimulusDistance = distance;
        _lastSoundStimulusRadius = stimulus.radius;
        _lastSoundStimulusResult =
            $"{result}: {stimulus.type} ({reason})";

        if (!_debugSoundStimuli)
            return;

        Debug.Log(
            $"[EnemyShooterCombatController][Sound] {result}. " +
            $"type={stimulus.type}, priority={priority}, distance={distance:0.00}, " +
            $"radius={stimulus.radius:0.00}, intensity={stimulus.intensity:0.00}, " +
            $"emitterId={stimulus.emitterId}, source={(stimulus.source != null ? stimulus.source.name : "null")}, " +
            $"position={stimulus.position}, direction={stimulus.direction}, reason={reason}",
            this);
    }

    // Purpose: Checks whether this shooter is currently true.
    private bool IsThisShooter(GameObject target)
    {
        if (target == gameObject)
            return true;

        Transform current = target.transform;

        while (current != null)
        {
            if (current == transform)
                return true;

            current = current.parent;
        }

        return false;
    }

    // Purpose: Checks whether own source is currently true.
    private bool IsOwnSource(GameObject source)
    {
        if (source == null)
            return false;

        if (source == gameObject)
            return true;

        return source.transform.IsChildOf(transform) || transform.IsChildOf(source.transform);
    }

    // Purpose: Attempts to resolve player transform and reports whether the request succeeded.
    private bool TryResolvePlayerTransform(GameObject attacker, out Transform playerTransform)
    {
        playerTransform = null;

        if (attacker == null)
            return false;

        PlayerCharacter player =
            attacker.GetComponent<PlayerCharacter>() ??
            attacker.GetComponentInParent<PlayerCharacter>();

        if (player == null)
            return false;

        Damageable playerDamageable =
            player.GetComponent<Damageable>() ??
            player.GetComponentInParent<Damageable>();

        if (playerDamageable == null || playerDamageable.IsDead)
            return false;

        playerTransform = player.transform;
        return playerTransform != null;
    }

    // Purpose: Checks whether within awareness range is currently true.
    private bool IsWithinAwarenessRange(Vector3 position)
    {
        EnemyShooterConfigSO config = Config;
        float range = config != null ? config.Vision.VisionRange : 0f;

        if (range <= 0f)
            return false;

        Vector3 from = transform.position;
        from.y = 0f;
        position.y = 0f;

        return Vector3.Distance(from, position) <= range;
    }

    // Purpose: Performs the debug movement workflow for this component.
    private void DebugMovement(string message)
    {
        if (!_debugMovement)
            return;

        if (Time.time < _nextMovementDebugTime)
            return;

        _nextMovementDebugTime = Time.time + Mathf.Max(0.1f, _debugMovementInterval);
        Debug.Log($"[EnemyShooterCombatController] {message}", this);
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_shoot == null)
            _shoot = ResolveComponentInOwnerHierarchy<EnemyShooterShoot>();

        if (_statsRuntime == null)
            _statsRuntime = ResolveComponentInOwnerHierarchy<EnemyShooterStatsRuntime>();

        if (_vision == null)
            _vision = ResolveComponentInOwnerHierarchy<EntityVisionController>();

        if (_agent == null)
            _agent = ResolveComponentInOwnerHierarchy<NavMeshAgent>();

        if (_damageable == null)
            _damageable = ResolveComponentInOwnerHierarchy<Damageable>();

        if (_effects == null)
            _effects = ResolveComponentInOwnerHierarchy<ShooterEffectController>();

        if (_dashController == null)
            _dashController = ResolveComponentInOwnerHierarchy<EnemyShooterDashController>();

        if (_equipmentController == null)
            _equipmentController = ResolveComponentInOwnerHierarchy<ShooterEquipmentController>();

        if (_lootInventory == null)
            _lootInventory = ResolveComponentInOwnerHierarchy<EnemyLootInventoryRuntime>();

        if (_meleeAttack == null)
            _meleeAttack = ResolveComponentInOwnerHierarchy<ShooterMeleeAttack>();

        if (_throwableAttack == null)
            _throwableAttack = ResolveComponentInOwnerHierarchy<ShooterThrowableAttack>();

        if (_statusEffectRuntime == null)
            _statusEffectRuntime = ResolveComponentInOwnerHierarchy<EntityStatusEffectRuntime>();
    }

    private T ResolveComponentInOwnerHierarchy<T>() where T : Component
    {
        T component = GetComponent<T>();

        if (component != null)
            return component;

        component = GetComponentInChildren<T>();

        if (component != null)
            return component;

        Transform current = transform.parent;

        while (current != null)
        {
            component = current.GetComponent<T>();

            if (component != null)
                return component;

            component = current.GetComponentInChildren<T>();

            if (component != null)
                return component;

            current = current.parent;
        }

        return null;
    }

    // Purpose: Attempts to set agent destination and reports whether the request succeeded.
    private bool TrySetAgentDestination(Vector3 rawDestination,
        float stoppingDistance,string reason)
    {
        if (!CanUseAgent())
            return false;

        if (!TryProjectToNavMesh(rawDestination, out Vector3 navDestination))
        {
            DebugMovement(
                $"{reason} failed. Destination cannot project to NavMesh. raw={rawDestination}, {BuildAgentDebugInfo()}");
            return false;
        }

        float resolvedStoppingDistance = Mathf.Max(0f, stoppingDistance);
        bool sameRequest =
            _hasNavigationRequest &&
            PlanarSqrDistance(_lastNavigationRequest, navDestination) <= 0.01f &&
            Mathf.Abs(_lastNavigationStoppingDistance - resolvedStoppingDistance) <= 0.01f;

        _agent.speed = ResolveMoveSpeed();
        _agent.stoppingDistance = resolvedStoppingDistance;

        if (sameRequest && (_agent.pathPending || _agent.hasPath) && !_agent.isStopped)
            return true;

        if (sameRequest && Time.time < _nextNavigationRetryTime)
            return HasUsableNavigationPath();

        _navigationPath.ClearCorners();

        bool calculated = _agent.CalculatePath(navDestination, _navigationPath);
        if (!calculated || _navigationPath.status == NavMeshPathStatus.PathInvalid)
        {
            _nextNavigationRetryTime = Time.time + 0.25f;
            DebugMovement(
                $"{reason} failed. No valid path. raw={rawDestination}, nav={navDestination}, {BuildAgentDebugInfo()}");
            return HasUsableNavigationPath();
        }

        Vector3[] corners = _navigationPath.corners;
        Vector3 reachableDestination = navDestination;
        bool pathIsPartial = _navigationPath.status == NavMeshPathStatus.PathPartial;

        if (pathIsPartial && corners != null && corners.Length > 0)
            reachableDestination = corners[corners.Length - 1];

        _agent.isStopped = false;
        bool accepted = _agent.SetPath(_navigationPath);

        if (!accepted)
        {
            _nextNavigationRetryTime = Time.time + 0.25f;
            DebugMovement(
                $"{reason} SetPath returned false. raw={rawDestination}, nav={navDestination}, pathStatus={_navigationPath.status}, {BuildAgentDebugInfo()}");
            return HasUsableNavigationPath();
        }

        _hasNavigationRequest = true;
        _lastNavigationRequest = navDestination;
        _lastReachableDestination = reachableDestination;
        _lastNavigationStoppingDistance = resolvedStoppingDistance;
        _nextNavigationRetryTime = Time.time + 0.25f;

        DebugMovement(
            $"{reason} path set. raw={rawDestination}, nav={navDestination}, reachable={reachableDestination}, " +
            $"pathStatus={_navigationPath.status}, speed={_agent.speed}, stopping={_agent.stoppingDistance}, {BuildAgentDebugInfo()}");

        return true;
    }

    // Purpose: Checks whether this component has usable navigation path.
    private bool HasUsableNavigationPath()
    {
        return _agent != null &&
               _agent.isActiveAndEnabled &&
               _agent.isOnNavMesh &&
               !_agent.isStopped &&
               (_agent.pathPending || _agent.hasPath);
    }

    // Purpose: Resolves the investigation arrival destination needed by this component.
    private Vector3 ResolveInvestigationArrivalDestination(Vector3 requestedDestination)
    {
        if (!_hasNavigationRequest ||
            PlanarSqrDistance(_lastNavigationRequest, requestedDestination) > 0.01f)
        {
            return requestedDestination;
        }

        return _lastReachableDestination;
    }

    // Purpose: Clears the current navigation request state.
    private void ClearNavigationRequest()
    {
        _hasNavigationRequest = false;
        _lastNavigationRequest = default;
        _lastReachableDestination = default;
        _lastNavigationStoppingDistance = 0f;
        _nextNavigationRetryTime = 0f;
    }

    // Purpose: Performs the planar sqr distance workflow for this component.
    private static float PlanarSqrDistance(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float z = a.z - b.z;
        return x * x + z * z;
    }

    // Purpose: Attempts to project to nav mesh and reports whether the request succeeded.
    private bool TryProjectToNavMesh(Vector3 rawPosition, out Vector3 navPosition)
    {
        EnemyShooterConfigSO config = Config;
        float sampleDistance = config != null ? config.NavMeshSampleDistance : 3f;
        sampleDistance = Mathf.Max(0.5f, sampleDistance);

        Vector3 planarPosition = rawPosition;
        planarPosition.y = transform.position.y;

        if (NavMesh.SamplePosition(
                planarPosition,
                out NavMeshHit planarHit,
                sampleDistance,
                NavMesh.AllAreas))
        {
            navPosition = planarHit.position;
            return true;
        }

        float verticalDistance = Mathf.Abs(rawPosition.y - transform.position.y);
        float expandedDistance = Mathf.Max(sampleDistance, verticalDistance + sampleDistance);

        if (NavMesh.SamplePosition(
                rawPosition,
                out NavMeshHit rawHit,
                expandedDistance,
                NavMesh.AllAreas))
        {
            navPosition = rawHit.position;
            return true;
        }

        navPosition = rawPosition;
        return false;
    }
    // Purpose: Builds the agent debug info from the current authored data.
    private string BuildAgentDebugInfo()
    {
        if (_agent == null)
            return "agent=null";

        return
            $"agentEnabled={_agent.isActiveAndEnabled}, " +
            $"onNavMesh={_agent.isOnNavMesh}, " +
            $"isStopped={_agent.isStopped}, " +
            $"hasPath={_agent.hasPath}, " +
            $"pathPending={_agent.pathPending}, " +
            $"pathStatus={_agent.pathStatus}, " +
            $"remaining={_agent.remainingDistance}, " +
            $"velocity={_agent.velocity}, " +
            $"desiredVelocity={_agent.desiredVelocity}, " +
            $"destination={_agent.destination}";
    }
}
