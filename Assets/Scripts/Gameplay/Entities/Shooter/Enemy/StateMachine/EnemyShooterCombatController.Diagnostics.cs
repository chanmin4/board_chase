/*
 * [Debug Monitor 관찰·런타임 테스트 설정]
 * 화면에 보여줄 스냅샷과 현재 적 한 개의 테스트용 설정을 관리합니다.
 * SetDebug...는 공유 Config SO를 수정하지 않고 메인 파일의 비직렬화 _debug... 필드에 값을 보관합니다.
 * DebugTry...는 조회가 아니라 실제 대시/투척을 시도하는 테스트 명령입니다.
 * 스냅샷은 실제 AI의 마지막 공격 평가를 관찰합니다. 표시 때문에 조건/총구 Cast/무기 선택을 재실행하지 않습니다.
 *
 * 이 파일도 EnemyShooterCombatController의 일부입니다. 별도 컴포넌트가 아닙니다.
 */

using UnityEngine;

// Debug Monitor에 넘기는 수집 결과 묶음입니다. 이 구조체 자체가 AI를 갱신하지 않습니다.
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
    [System.NonSerialized] private bool _observedCanAttackTarget;
    [System.NonSerialized] private int _observedCanAttackFrame = -1;
    [System.NonSerialized] private string _observedAttackDecisionReason = "AI 미검사";
    public bool ObservedCanAttackTarget => _observedCanAttackTarget;
    public int ObservedCanAttackFrame => _observedCanAttackFrame;
    public string ObservedAttackDecisionReason => _observedAttackDecisionReason;
    public string ObservedAttackStatus => _observedCanAttackFrame < 0 ? "AI 미검사" :
        _damageable != null && _damageable.IsDead ? "Dead" :
        !_observedCanAttackTarget ? _observedAttackDecisionReason :
        _attackActive && _attackPhase != EnemyShooterAttackPhase.None ? _attackPhase.ToString() :
        _observedCanAttackTarget ? "Ready" : "Attack blocked";
    /// <summary>
    /// 현재 적의 행동·인지·사격·전술 정보를 한 번에 수집합니다.
    /// 에디터는 private 필드를 직접 읽는 대신 이 스냅샷을 사용합니다.
    /// CanAttackTarget / AttackDebugStatus를 호출하지 않는 관찰 전용 API입니다.
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
            ObservedCanAttackTarget,
            ObservedAttackStatus,
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

    /// <summary>현재 적의 테스트용 덮어쓰기를 켜거나 끕니다. 끌 때는 테스트 중 확보한 전술 상태를 정리합니다.</summary>
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

    /// <summary>현재 적 한 개의 전술/엄폐/대시/투척 옵션을 테스트용 값으로 바꿉니다.</summary>
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

    /// <summary>현재 적 한 개의 엄폐·대시 수치를 유효 범위로 제한해 테스트용 필드에 저장합니다.</summary>
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

    /// <summary>Debug Monitor 명령으로 실제 회피 대시를 시도합니다.</summary>
    public bool DebugTryStartDefensiveDash()
    {
        return TryStartDefensiveDash();
    }

    /// <summary>Debug Monitor 명령으로 실제 소지 투척물 사용을 시도합니다.</summary>
    public bool DebugTryThrowCarriedThrowable()
    {
        return TryExecuteThrowableAttack();
    }

    /// <summary>기존 Config 값을 테스트용 필드에 복사합니다. 원본 Config를 변경하지 않습니다.</summary>
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

    /// <summary>공유 대시 설정을 테스트용 필드에 복사합니다.</summary>
    private void CopySharedDashConfigIntoDebugOverrides(EnemyShooterConfigSO enemyConfig)
    {
        ShooterDashSettings dashConfig = enemyConfig.Dash;

        if (dashConfig == null)
        {
            _debugDashCooldownSeconds = enemyConfig.DashCooldownSeconds;
            _debugDashDurationSeconds = enemyConfig.DashDurationSeconds;
            _debugDashSpeed = enemyConfig.DashSpeed;
            return;
        }

        _debugDashCooldownSeconds = _statsRuntime != null ? _statsRuntime.DashCooldownSeconds : enemyConfig.DashCooldownSeconds;
        _debugDashDurationSeconds = Mathf.Max(0.01f, dashConfig.durationSeconds);

        if (!dashConfig.useMoveSpeedBasedDash)
        {
            _debugDashSpeed = Mathf.Max(0f, dashConfig.fixedSpeed);
            return;
        }

        float moveSpeed = _statsRuntime != null
            ? Mathf.Max(0.01f, _statsRuntime.MoveSpeed)
            : Mathf.Max(0.01f, enemyConfig.MoveSpeed);
        float distanceMultiplier = _statsRuntime != null
            ? Mathf.Max(0f, _statsRuntime.DashDistanceMultiplier)
            : 1f;

        _debugDashSpeed = moveSpeed *
                          Mathf.Max(0f, dashConfig.speedMultiplier) *
                          distanceMultiplier;
    }
}
