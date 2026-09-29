/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > StateMachine
 * Primary responsibility: Owns the read-only diagnostics snapshot and Debug Monitor-only overrides for EnemyShooterCombatController.
 * Execution boundary: Runtime (Assembly-CSharp)
 *
 * Split note: this is the first safe extraction from the legacy 4k-line combat controller.
 * It must not decide combat, move the agent, or change shared EnemyShooterConfigSO data.
 * Future tactical, awareness, navigation, and attack extractions should keep their
 * public observability surface here so Debug Monitor does not couple to private state.
 */

using UnityEngine;

public readonly struct EnemyShooterCombatDiagnosticsSnapshot
{
    public EnemyShooterCombatDiagnosticsSnapshot(
        EnemyShooterGoal goal,
        EnemyShooterInvestigationType investigationType,
        EnemyShooterAttackPhase attackPhase,
        bool isEngaged,
        bool isAlerted,
        bool hasTarget,
        bool canSeeTarget,
        bool isTargetLocked,
        float targetPlanarDistance,
        float targetLockRemaining,
        bool canAttackTarget,
        string attackStatus,
        float attackStartRange,
        float attackKeepRange,
        float activeAttackRange,
        int burstShotsRemaining,
        float attackCooldownRemaining,
        float reactionTimeRemaining,
        float lastSeenSecondsAgo,
        bool hasLastKnownPosition,
        float lastKnownSecondsAgo,
        bool hasSoundInvestigation,
        string lastSoundResult,
        float lastSoundDistance,
        float lastSoundRadius,
        float soundReactionDelayRemaining,
        bool isActionBlockedByStatusEffect,
        EnemyShooterTacticalIntent tacticalIntent,
        EnemyShooterCoverTacticPhase coverTacticPhase,
        bool hasReservedCover,
        bool hasRuntimeAutoCoverPoint,
        bool holdsDuelToken,
        Vector3 tacticalDestination,
        bool isDashing,
        bool hasCarriedThrowable,
        float throwableCooldownRemaining,
        bool hasRuntimeOverrides)
    {
        Goal = goal;
        InvestigationType = investigationType;
        AttackPhase = attackPhase;
        IsEngaged = isEngaged;
        IsAlerted = isAlerted;
        HasTarget = hasTarget;
        CanSeeTarget = canSeeTarget;
        IsTargetLocked = isTargetLocked;
        TargetPlanarDistance = targetPlanarDistance;
        TargetLockRemaining = targetLockRemaining;
        CanAttackTarget = canAttackTarget;
        AttackStatus = attackStatus;
        AttackStartRange = attackStartRange;
        AttackKeepRange = attackKeepRange;
        ActiveAttackRange = activeAttackRange;
        BurstShotsRemaining = burstShotsRemaining;
        AttackCooldownRemaining = attackCooldownRemaining;
        ReactionTimeRemaining = reactionTimeRemaining;
        LastSeenSecondsAgo = lastSeenSecondsAgo;
        HasLastKnownPosition = hasLastKnownPosition;
        LastKnownSecondsAgo = lastKnownSecondsAgo;
        HasSoundInvestigation = hasSoundInvestigation;
        LastSoundResult = lastSoundResult;
        LastSoundDistance = lastSoundDistance;
        LastSoundRadius = lastSoundRadius;
        SoundReactionDelayRemaining = soundReactionDelayRemaining;
        IsActionBlockedByStatusEffect = isActionBlockedByStatusEffect;
        TacticalIntent = tacticalIntent;
        CoverTacticPhase = coverTacticPhase;
        HasReservedCover = hasReservedCover;
        HasRuntimeAutoCoverPoint = hasRuntimeAutoCoverPoint;
        HoldsDuelToken = holdsDuelToken;
        TacticalDestination = tacticalDestination;
        IsDashing = isDashing;
        HasCarriedThrowable = hasCarriedThrowable;
        ThrowableCooldownRemaining = throwableCooldownRemaining;
        HasRuntimeOverrides = hasRuntimeOverrides;
    }

    public EnemyShooterGoal Goal { get; }
    public EnemyShooterInvestigationType InvestigationType { get; }
    public EnemyShooterAttackPhase AttackPhase { get; }
    public bool IsEngaged { get; }
    public bool IsAlerted { get; }
    public bool HasTarget { get; }
    public bool CanSeeTarget { get; }
    public bool IsTargetLocked { get; }
    public float TargetPlanarDistance { get; }
    public float TargetLockRemaining { get; }
    public bool CanAttackTarget { get; }
    public string AttackStatus { get; }
    public float AttackStartRange { get; }
    public float AttackKeepRange { get; }
    public float ActiveAttackRange { get; }
    public int BurstShotsRemaining { get; }
    public float AttackCooldownRemaining { get; }
    public float ReactionTimeRemaining { get; }
    public float LastSeenSecondsAgo { get; }
    public bool HasLastKnownPosition { get; }
    public float LastKnownSecondsAgo { get; }
    public bool HasSoundInvestigation { get; }
    public string LastSoundResult { get; }
    public float LastSoundDistance { get; }
    public float LastSoundRadius { get; }
    public float SoundReactionDelayRemaining { get; }
    public bool IsActionBlockedByStatusEffect { get; }
    public EnemyShooterTacticalIntent TacticalIntent { get; }
    public EnemyShooterCoverTacticPhase CoverTacticPhase { get; }
    public bool HasReservedCover { get; }
    public bool HasRuntimeAutoCoverPoint { get; }
    public bool HoldsDuelToken { get; }
    public Vector3 TacticalDestination { get; }
    public bool IsDashing { get; }
    public bool HasCarriedThrowable { get; }
    public float ThrowableCooldownRemaining { get; }
    public bool HasRuntimeOverrides { get; }
}

public partial class EnemyShooterCombatController
{
    /// <summary>
    /// Returns one coherent frame snapshot for tooling. Keep editor code on this
    /// boundary instead of introducing new direct reads of controller internals.
    /// </summary>
    public EnemyShooterCombatDiagnosticsSnapshot GetDiagnosticsSnapshot()
    {
        return new EnemyShooterCombatDiagnosticsSnapshot(
            CurrentGoal,
            CurrentInvestigationType,
            AttackPhase,
            IsEngaged,
            IsAlerted,
            HasTarget,
            CanSeeTarget,
            IsTargetLocked,
            TargetPlanarDistance,
            TargetLockRemaining,
            CanAttackTarget,
            AttackDebugStatus,
            AttackStartRange,
            AttackKeepRange,
            ActiveAttackRange,
            BurstShotsRemaining,
            AttackCooldownRemaining,
            ReactionTimeRemaining,
            LastSeenSecondsAgo,
            HasLastKnownPosition,
            LastKnownSecondsAgo,
            HasSoundInvestigation,
            LastSoundStimulusResult,
            LastSoundStimulusDistance,
            LastSoundStimulusRadius,
            SoundReactionDelayRemaining,
            IsActionBlockedByStatusEffect,
            TacticalIntent,
            CoverTacticPhase,
            HasReservedCover,
            HasRuntimeAutoCoverPoint,
            HoldsDuelToken,
            TacticalDestination,
            IsDashing,
            HasCarriedThrowable,
            ThrowableCooldownRemaining,
            DebugCombatOverridesEnabled);
    }

    // Purpose: Updates the debug combat overrides enabled state.
    public void SetDebugCombatOverridesEnabled(bool enabled)
    {
        if (enabled && !_debugCombatOverridesEnabled)
            CopyConfigIntoDebugCombatOverrides();

        _debugCombatOverridesEnabled = enabled;

        if (!enabled)
        {
            ReleaseCoverTactic();
            EnemyShooterTacticalIntentSelector.ReleaseDuelHolder(this);
            ResetTacticalIntent();
        }
    }

    // Purpose: Updates the debug combat toggles state.
    public void SetDebugCombatToggles(
        bool tacticalIntent,
        bool cover,
        bool fireWhileMovingToCover,
        bool duelToken,
        bool flanking,
        bool dash,
        bool dashOnDamaged,
        bool dashWhenHealthLow,
        bool throwable,
        bool usePreferredRangeBeforeStationaryAttack)
    {
        if (!_debugCombatOverridesEnabled)
            SetDebugCombatOverridesEnabled(true);

        _debugUseTacticalIntent = tacticalIntent;
        _debugUseCoverTactics = cover;
        _debugFireWhileMovingToCover = fireWhileMovingToCover;
        _debugUseDuelToken = duelToken;
        _debugUseFlankingTactics = flanking;
        _debugUseDash = dash;
        _debugDashOnDamaged = dashOnDamaged;
        _debugDashWhenHealthLow = dashWhenHealthLow;
        _debugUseThrowableAttack = throwable;
        _debugUsePreferredRangeBeforeStationaryAttack = usePreferredRangeBeforeStationaryAttack;

        if (!tacticalIntent || !cover)
            ReleaseCoverTactic();

        if (!tacticalIntent || !duelToken)
            EnemyShooterTacticalIntentSelector.ReleaseDuelHolder(this);

        if (!tacticalIntent || !flanking)
            ResetTacticalIntent();

        if (!dash)
            _dashController?.CancelDash();
    }

    // Purpose: Updates the debug combat tuning state.
    public void SetDebugCombatTuning(
        float coverSearchRadius,
        float coverHideSeconds,
        float coverPeekSeconds,
        float coverApproachSpeedMultiplier,
        float flankDistance,
        float dashChanceOnDamaged,
        float dashHealthThreshold,
        float dashCooldownSeconds,
        float dashDurationSeconds,
        float dashSpeed,
        float stationaryAttackKeepRangePadding)
    {
        if (!_debugCombatOverridesEnabled)
            SetDebugCombatOverridesEnabled(true);

        _debugCoverSearchRadius = Mathf.Max(0.5f, coverSearchRadius);
        _debugCoverHideSeconds = Mathf.Max(0f, coverHideSeconds);
        _debugCoverPeekSeconds = Mathf.Max(0.05f, coverPeekSeconds);
        _debugCoverApproachSpeedMultiplier = Mathf.Clamp(coverApproachSpeedMultiplier, 0.1f, 1f);
        _debugTacticalFlankDistance = Mathf.Max(0.1f, flankDistance);
        _debugDashChanceOnDamaged = Mathf.Clamp01(dashChanceOnDamaged);
        _debugDashHealthThreshold = Mathf.Clamp01(dashHealthThreshold);
        _debugDashCooldownSeconds = Mathf.Max(0f, dashCooldownSeconds);
        _debugDashDurationSeconds = Mathf.Max(0.01f, dashDurationSeconds);
        _debugDashSpeed = Mathf.Max(0f, dashSpeed);
        _debugStationaryAttackKeepRangePadding = Mathf.Max(0f, stationaryAttackKeepRangePadding);
    }

    // Purpose: Performs the debug try start defensive dash workflow for this component.
    public bool DebugTryStartDefensiveDash()
    {
        return TryStartDefensiveDash();
    }

    // Purpose: Performs the debug try throw carried throwable workflow for this component.
    public bool DebugTryThrowCarriedThrowable()
    {
        return TryExecuteThrowableAttack();
    }

    // Purpose: Copies config into debug combat overrides into the destination state.
    private void CopyConfigIntoDebugCombatOverrides()
    {
        EnemyShooterConfigSO config = Config;
        if (config == null)
            return;

        _debugUseTacticalIntent = config.UseTacticalIntent;
        _debugUseCoverTactics = config.UseCoverTactics;
        _debugFireWhileMovingToCover = false;
        _debugUseDuelToken = config.UseDuelToken;
        _debugUseFlankingTactics = config.UseFlankingTactics;
        _debugUseDash = config.UseDash;
        _debugDashOnDamaged = config.DashOnIncomingProjectile;
        _debugDashWhenHealthLow = config.DashWhenHealthBelowThreshold;
        _debugUseThrowableAttack = config.UseThrowableAttack;
        _debugUsePreferredRangeBeforeStationaryAttack = config.UsePreferredRangeBeforeStationaryAttack;
        _debugCoverSearchRadius = config.CoverSearchRadius;
        _debugCoverHideSeconds = config.CoverHideSeconds;
        _debugCoverPeekSeconds = config.CoverPeekSeconds;
        _debugCoverApproachSpeedMultiplier = 0.55f;
        _debugTacticalFlankDistance = config.TacticalFlankDistance;
        _debugDashChanceOnDamaged = config.DashChanceOnIncomingProjectile;
        _debugDashHealthThreshold = config.DashHealthNormalizedThreshold;
        CopySharedDashConfigIntoDebugOverrides(config);
        _debugStationaryAttackKeepRangePadding = config.StationaryAttackKeepRangePadding;
    }

    // Purpose: Copies shared dash config into debug overrides into the destination state.
    private void CopySharedDashConfigIntoDebugOverrides(EnemyShooterConfigSO enemyConfig)
    {
        DashConfigSO dashConfig = _dashController != null
            ? _dashController.Config
            : null;

        if (dashConfig == null)
        {
            _debugDashCooldownSeconds = enemyConfig.DashCooldownSeconds;
            _debugDashDurationSeconds = enemyConfig.DashDurationSeconds;
            _debugDashSpeed = enemyConfig.DashSpeed;
            return;
        }

        _debugDashCooldownSeconds = dashConfig.CooldownSeconds;
        _debugDashDurationSeconds = dashConfig.DashDuration;

        if (!dashConfig.UseMoveSpeedBasedDash)
        {
            _debugDashSpeed = dashConfig.FixedDashSpeed;
            return;
        }

        float moveSpeed = _statsRuntime != null
            ? Mathf.Max(0.01f, _statsRuntime.MoveSpeed)
            : Mathf.Max(0.01f, enemyConfig.MoveSpeed);
        float distanceMultiplier = _statsRuntime != null
            ? Mathf.Max(0f, _statsRuntime.DashDistanceMultiplier)
            : 1f;

        _debugDashSpeed = moveSpeed *
                          Mathf.Max(0f, dashConfig.DashSpeedMultiplier) *
                          distanceMultiplier;
    }
}
