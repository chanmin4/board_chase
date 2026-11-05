/*
 * [HFSM 조건·외부 상태 조회]
 * CanAttackTarget/ShouldChaseTarget 등 전환에 쓰는 판정과 외부 공개 프로퍼티를 찾을 때 봅니다.
 * 단순 현재 값, 유효 설정(Effective...), 복합 행동 조건 순서로 배치되어 있습니다.
 * Effective...는 Debug Monitor 덮어쓰기가 켜져 있으면 그 값을, 아니면 Config 값을 선택합니다.
 * 주의: 기존 동작 보존 때문에 일부 getter는 순수 조회가 아닙니다. 공격 판정은 무기 전환, 조사 판정은 만료 단서 정리를 수행할 수 있습니다.
 *
 * 이 파일도 EnemyShooterCombatController의 일부입니다. 별도 컴포넌트가 아닙니다.
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public partial class EnemyShooterCombatController
{
    // ── 1. 현재 타겟·경계·행동 진행 상태 ──────────────────────────────────
    // 단순 플래그/참조 읽기. CurrentGoal은 HFSM 상태 에셋이 아니라 선택한 행동 목표입니다.
    public EnemyShooterConfigSO Config => _shoot != null ? _shoot.Config : null;
    public Transform Target => _target;
    public bool HasTarget => _target != null && (_targetDamageable == null || !_targetDamageable.IsDead);
    public bool IsAlerted => _isAlerted && (HasTarget || _hasSoundInvestigation || _hasLastKnownPosition);
    public bool IsEngaged => HasTarget && (_currentGoal == EnemyShooterGoal.ChaseTarget || _currentGoal == EnemyShooterGoal.AttackTarget);
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
    // ── 2. 거리·시야·사거리와 남은 시간 ────────────────────────────────────
    // 거리 계산은 XZ 평면 기준. 타겟 부재/기록 없음은 아래 일부 프로퍼티에서 -1로 표현합니다.
    public float TargetPlanarDistance => HasTarget ? GetPlanarDistanceToTarget() : -1f;
    public float VisionRange => _vision != null ? _vision.VisionRange : _statsRuntime != null ? _statsRuntime.VisionRange : Config != null && Config.Vision != null ? Config.Vision.VisionRange : 0f;
    public float CloseVisionRadius => _vision != null ? _vision.CloseVisionRadius : 0f;
    public float VisionArcAngle => _vision != null ? _vision.ForwardArcAngle : 360f;
    public bool UsesForwardVisionArc => _vision != null && _vision.UsesForwardArc;
    public bool UsesVisionLineOfSight => _vision != null && _vision.UsesLineOfSight;
    public float AttackStartRange => ResolveDirectAttackRange(false);
    public float AttackKeepRange => ResolveDirectAttackRange(true);
    public float ActiveAttackRange => _attackActive ? AttackKeepRange : AttackStartRange;
    public float AttackCooldownRemaining => Mathf.Max(0f, Mathf.Max(_nextFireTime, _nextBurstShotTime) - Time.time);
    public int BurstShotsRemaining => Mathf.Max(0, _burstShotsRemaining);
    public float ReactionTimeRemaining => _reactionStartedTime >= 0f ? Mathf.Max(0f, _reactionStartedTime + _activeReactionDelaySeconds - Time.time) : 0f;
    public float TargetLockRemaining => Mathf.Max(0f, _targetLockUntilTime - Time.time);
    public float LastSeenSecondsAgo => _lastSeenTime >= 0f ? Mathf.Max(0f, Time.time - _lastSeenTime) : -1f;
    public float LastKnownSecondsAgo => _lastKnownSetTime >= 0f ? Mathf.Max(0f, Time.time - _lastKnownSetTime) : -1f;
    public float SoundReactionDelayRemaining => _hasSoundInvestigation ? Mathf.Max(0f, _soundInvestigationMoveTime - Time.time) : 0f;
    // ── 3. 마지막 소리 판정과 공격/상태이상 진단 ───────────────────────────
    public string LastSoundStimulusResult => _lastSoundStimulusResult;
    public float LastSoundStimulusDistance => _lastSoundStimulusDistance;
    public float LastSoundStimulusRadius => _lastSoundStimulusRadius;
    public string AttackDebugStatus => ResolveAttackDebugStatus();
    public bool IsActionBlockedByStatusEffect => ResolveStatusActionBlocked();
    // ── 4. 유효 설정: Debug Monitor 덮어쓰기 또는 원래 Config ───────────────
    // 여기서는 값을 선택할 뿐, 공유 Config SO의 값을 저장/수정하지 않습니다.
    public bool DebugCombatOverridesEnabled => _debugCombatOverridesEnabled;
    public bool EffectiveUseTacticalIntent => _debugCombatOverridesEnabled ? _debugUseTacticalIntent : Config != null && Config.UseTacticalIntent;
    public bool EffectiveUseCoverTactics => EffectiveUseTacticalIntent && (_debugCombatOverridesEnabled ? _debugUseCoverTactics : Config != null && Config.UseCoverTactics);
    public bool EffectiveFireWhileMovingToCover => _debugCombatOverridesEnabled && _debugFireWhileMovingToCover && EffectiveUseCoverTactics;
    public float EffectiveCoverApproachSpeedMultiplier => EffectiveFireWhileMovingToCover ? Mathf.Clamp(_debugCoverApproachSpeedMultiplier, 0.1f, 1f) : 1f;
    public bool EffectiveUseDuelToken => EffectiveUseTacticalIntent && (_debugCombatOverridesEnabled ? _debugUseDuelToken : Config != null && Config.UseDuelToken);
    public bool EffectiveUseFlankingTactics => EffectiveUseTacticalIntent && (_debugCombatOverridesEnabled ? _debugUseFlankingTactics : Config != null && Config.UseFlankingTactics);
    public bool EffectiveUseDash => _debugCombatOverridesEnabled ? _debugUseDash : Config != null && Config.UseDash;
    public bool EffectiveUseThrowableAttack => _debugCombatOverridesEnabled ? _debugUseThrowableAttack : Config != null && Config.UseThrowableAttack;
    public bool EffectiveUsePreferredRangeBeforeStationaryAttack => _debugCombatOverridesEnabled ? _debugUsePreferredRangeBeforeStationaryAttack : Config != null && Config.UsePreferredRangeBeforeStationaryAttack;
    public float EffectiveStationaryAttackKeepRangePadding => _debugCombatOverridesEnabled ? _debugStationaryAttackKeepRangePadding : Config != null ? Config.StationaryAttackKeepRangePadding : 0f;
    public bool EffectiveDashOnIncomingProjectile => EffectiveUseDash && (_debugCombatOverridesEnabled ? _debugDashOnDamaged : Config != null && Config.DashOnIncomingProjectile);
    public bool EffectiveDashWhenHealthLow => EffectiveUseDash && (_debugCombatOverridesEnabled ? _debugDashWhenHealthLow : Config != null && Config.DashWhenHealthBelowThreshold);
    public float EffectiveDashCooldownSeconds => _debugCombatOverridesEnabled ? _debugDashCooldownSeconds : _statsRuntime != null ? _statsRuntime.DashCooldownSeconds : Config != null ? Config.DashCooldownSeconds : 0f;
    public float EffectiveDashDurationSeconds => _debugCombatOverridesEnabled ? _debugDashDurationSeconds : Config != null ? Config.DashDurationSeconds : 0.18f;
    public float EffectiveDashSpeed => _debugCombatOverridesEnabled ? _debugDashSpeed : _dashController != null ? _dashController.DashSpeed : Config != null ? Config.DashSpeed : 14f;
    public float EffectiveCoverSearchRadius => _debugCombatOverridesEnabled ? _debugCoverSearchRadius : Config != null ? Config.CoverSearchRadius : 0.5f;
    public float EffectiveCoverHideSeconds => _debugCombatOverridesEnabled ? _debugCoverHideSeconds : Config != null ? Config.CoverHideSeconds : 0f;
    public float EffectiveCoverPeekSeconds => _debugCombatOverridesEnabled ? _debugCoverPeekSeconds : Config != null ? Config.CoverPeekSeconds : 0.05f;
    public float EffectiveTacticalFlankDistance => _debugCombatOverridesEnabled ? _debugTacticalFlankDistance : Config != null ? Config.TacticalFlankDistance : 0.1f;
    public float EffectiveDashChanceOnIncomingProjectile => _debugCombatOverridesEnabled ? _debugDashChanceOnDamaged : Config != null ? Config.DashChanceOnIncomingProjectile : 0f;
    public float EffectiveDashHealthThreshold => _debugCombatOverridesEnabled ? _debugDashHealthThreshold : Config != null ? Config.DashHealthNormalizedThreshold : 0f;
    // ── 5. 전술·엄폐·소지 투척물 상태 ──────────────────────────────────────
    public EnemyShooterTacticalIntent TacticalIntent => _tacticalIntent;
    public EnemyShooterCoverTacticPhase CoverTacticPhase => _coverTacticPhase;
    public bool HasReservedCover => _reservedCoverPoint != null;
    public bool HasRuntimeAutoCoverPoint => _runtimeAutoCoverPoint != null;
    public bool HoldsDuelToken => HasTarget && EnemyShooterTacticalIntentSelector.IsDuelHolder(this, _target);
    public Vector3 TacticalDestination => _tacticalDestination;
    public float ThrowableCooldownRemaining => Mathf.Max(0f, _nextThrowableAllowedTime - Time.time);
    public bool HasCarriedThrowable => _lootInventory != null && _lootInventory.TryGetFirstCarriedItem(out ThrowableUsableItemSO _);
    // ── 6. 원거리 Attack 상태에 들어가거나 머무를 수 있는지 ────────────────
    /// <summary>직접 사격뿐 아니라 엄폐 유지·시야 상실 후 버스트/휴식 유지도 포함한 공격 상태 판정입니다.</summary>
    /// <remarks>기존 로직에 따라 원거리 무기 슬롯 전환을 시도할 수 있습니다. 순수 조회로 가정하지 마세요.</remarks>
    public bool CanAttackTarget
    {
        get
        {
            bool result = EvaluateCanAttackTarget();
            _observedCanAttackTarget = result;
            _observedCanAttackFrame = Time.frameCount;
            return result;
        }
    }
    private bool EvaluateCanAttackTarget()
    {
        if (_isEvadingIncomingThrowable)
            return RecordAttackDecision(false, "투척물 회피 중");
        if (IsActionBlockedByStatusEffect)
            return RecordAttackDecision(false, "상태이상 행동 차단");
        if (!HasTarget)
            return RecordAttackDecision(false, "유효한 타겟 없음");
        if (MustFinishCurrentBurst)
            return RecordAttackDecision(true, "시작한 버스트 완료 중");
        if (IsTargetInsideConfiguredMeleeRange())
            return RecordAttackDecision(false, "근접 범위 · 원거리 대신 근접 행동 필요");
        if (!TryActivateRangedWeaponForCombat())
            return RecordAttackDecision(false, "원거리 무기 슬롯 없음 / 무기 전환 대기");
        if (!CanUseCurrentRangedWeapon())
            return RecordAttackDecision(false, ResolveRangedWeaponBlockedReason());

        if (EvaluateDirectAttack(out string reason))
            return RecordAttackDecision(true, reason);
        if (CanMaintainCoverTactic)
            return RecordAttackDecision(true, "엄폐 전술 유지 · 직접 사격 조건과 별개");
        if (CanFinishStartedBurstAfterSightLost)
            return RecordAttackDecision(true, "시야 상실 후 버스트 완료 중");
        if (CanHoldBurstCooldownAfterSightLost)
            return RecordAttackDecision(true, "시야 상실 후 버스트 휴식 유지");
        return RecordAttackDecision(false, reason);
    }

    private bool RecordAttackDecision(bool result, string reason)
    {
        _observedAttackDecisionReason = reason;
        return result;
    }

    private string ResolveRangedWeaponBlockedReason()
    {
        if (_shoot == null || _shoot.CurrentBullet == null)
            return "사격 컴포넌트 / 사용할 탄환 없음";
        if (_equipmentController == null || _equipmentController.CurrentWeapon is not RangedWeaponSO rangedWeapon)
            return "활성 슬롯이 원거리 무기가 아님";
        if (!rangedWeapon.IsCompatibleWith(_shoot.CurrentBullet))
            return "무기와 탄환 호환 불일치";
        return "장비 런타임이 현재 무기 발사를 차단함";
    }

    private bool HasActiveCoverTactic => _reservedCoverPoint != null && _coverTacticPhase != EnemyShooterCoverTacticPhase.None;
    // 엄폐 뒤에서 직접 시야가 막혀 있어도 유지 사거리 안이면 엄폐 전술을 계속할 수 있습니다.
    private bool CanMaintainCoverTactic
    {
        get
        {
            if (!HasActiveCoverTactic || !HasTarget || Config == null)
                return false;
            return GetPlanarDistanceToTarget() <= ResolveAttackKeepRange();
        }
    }

    // 직접 사격 조건: 보이는 타겟 + 근접 범위 제외 + 사용할 총 + 사거리 + 열린 총구 경로.
    // 총 슬롯을 활성화할 수 있고 총구 검사 캐시를 갱신할 수 있습니다.
    private bool CanDirectlyAttackTarget => EvaluateDirectAttack(out _);

    // 실제 조건 평가에서 탈락한 첫 이유를 함께 기록합니다. Debug Monitor가 재검사하지 않습니다.
    private bool EvaluateDirectAttack(out string reason)
    {
        if (!HasTarget || Config == null)
        {
            reason = "유효한 타겟 / Config 없음";
            return false;
        }
        if (!CanSeeTarget)
        {
            reason = "시야 미확보 · 시야각/거리/차폐 확인";
            return false;
        }
        if (!HasUnfinishedRangedBurst && IsTargetInsideConfiguredMeleeRange())
        {
            reason = "근접 범위 · 원거리 대신 근접 행동 필요";
            return false;
        }
        if (!HasUnfinishedRangedBurst && !TryActivateRangedWeaponForCombat())
        {
            reason = "원거리 무기 슬롯 없음 / 무기 전환 대기";
            return false;
        }
        if (!CanUseCurrentRangedWeapon())
        {
            reason = ResolveRangedWeaponBlockedReason();
            return false;
        }
        if (GetPlanarDistanceToTarget() > (_attackActive ? AttackKeepRange : AttackStartRange))
        {
            reason = "공격 사거리 밖";
            return false;
        }
        if (!HasClearMuzzlePathToTarget())
        {
            reason = "타겟은 보이지만 총구 발사 경로가 막힘";
            return false;
        }
        reason = "직접 사격 조건 충족";
        return true;
    }

    private bool UsesConfiguredCombatWeaponSlots => Config != null && Config.AutoSwitchCombatWeaponSlots && _equipmentController != null;
    // 이미 한 발 이상 시작한 버스트를 시야 상실 뒤에도 마칠 수 있는지 검사합니다.
    private bool CanFinishStartedBurstAfterSightLost
    {
        get
        {
            EnemyShooterConfigSO config = Config;
            return config != null && config.FinishStartedBurstAfterSightLost && HasTarget && (!CanSeeTarget || !HasClearMuzzlePathToTarget()) && _attackActive && _attackPhase == EnemyShooterAttackPhase.FiringBurst && _hasFiredCurrentBurst && _burstShotsRemaining > 0 && GetPlanarDistanceToTarget() <= AttackKeepRange;
        }
    }

    // 시야를 잃은 직후 공격/추적을 급히 왕복하지 않도록 기존 버스트 휴식을 유지하는 조건입니다.
    private bool CanHoldBurstCooldownAfterSightLost
    {
        get
        {
            EnemyShooterConfigSO config = Config;
            return config != null && config.FinishStartedBurstAfterSightLost && HasTarget && (!CanSeeTarget || !HasClearMuzzlePathToTarget()) && _attackActive && _attackPhase == EnemyShooterAttackPhase.BurstCooldown && Time.time < _nextFireTime;
        }
    }

    // ── 7. HFSM 전환에서 사용하는 행동 선택 조건 ───────────────────────────
    // 여기서 true를 반환해도 직접 상태를 바꾸지는 않습니다. 실제 전환 선택은 HFSM이 수행합니다.
    public bool ShouldAttack => CanAttackTarget;
    /// <summary>사격 불가 상태에서 직접 시야·추적 유지 시간·전술 압박이 남아 있으면 추적을 요구합니다.</summary>
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
    // ResolveInvestigationType 호출 중 만료된 소리 단서를 지울 수 있으므로 부수효과가 있습니다.
    public bool HasInvestigation => Time.time >= _ignoreInvestigationUntilTime && ResolveInvestigationType() != EnemyShooterInvestigationType.None;
    public bool ShouldInvestigate => !IsActionBlockedByStatusEffect && !CanAttackTarget && !CanSeeTarget && HasInvestigation && !InvestigationFinished;
    // 도착 후 기다리는 시간까지 끝났는지만 읽습니다. 실제 단서 정리는 TryCompleteFinishedInvestigation입니다.
    public bool InvestigationFinished => _investigationArrived && Time.time >= _investigationFinishTime;
    public bool ShouldPatrol => !IsActionBlockedByStatusEffect && !CanAttackTarget && !CanSeeTarget && !ShouldChaseTarget && !HasInvestigation && Config != null && Config.PatrolRadius > 0f;
    public bool PatrolIdleFinished => Time.time >= _patrolIdleUntil;
    public bool PatrolDestinationReached => _patrolDestinationReached;
    public bool TargetLost => _isAlerted && !HasInvestigation && (!HasTarget || HasForgottenTarget());
    // ── 8. 근접/투척 행동 진입 및 완료 조건 ─────────────────────────────────
    // 변경 가능 시점을 먼저 검사합니다. 버스트 도중 근접 무기 조회/전환으로 현재 사격을 끊지 않습니다.
    public bool ShouldUseMelee => !IsActionBlockedByStatusEffect && !_isEvadingIncomingThrowable && !IsDashing &&
        CanChangeCombatAction && CanUseMeleeAttack();
    public bool MeleeActionFinished => _meleeActionActive && Time.time >= _meleeActionFinishTime;
    public bool ShouldUseThrowable => !IsActionBlockedByStatusEffect && !_isEvadingIncomingThrowable && !IsDashing &&
        CanChangeCombatAction && HasTacticalThrowableReason && CanUseThrowableAttack();
    public bool ThrowableActionFinished => !_throwableActionActive || Time.time >= _throwableActionFinishTime;
    private bool ShouldMaintainTacticalPressure
    {
        get
        {
            EnemyShooterConfigSO config = Config;
            return config != null && EffectiveUseTacticalIntent && HasTarget && _hasLastKnownPosition && (!CanSeeTarget || HasActiveCoverTactic) && !HasForgottenTarget();
        }
    }
}
