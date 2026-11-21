/*
 * [감지·타겟·조준]
 * 적이 플레이어를 발견/놓치는 기준, 피격 경계, 바라보는 방향을 수정할 때 봅니다.
 * 주요 입구: TickAwareness / AlertToPlayer / TryAlertToPlayerFromDamage.
 * 연결: 시야 컴포넌트로 감지하고 Investigation에 기억/목표 갱신을, Evasion/Status에 긴급 처리를 맡깁니다.
 * 주의: TickAwareness는 단순 조회가 아닙니다. 회피·대시를 먼저 처리하고 타겟/목표를 갱신합니다.
 *
 * 이 파일도 EnemyShooterCombatController의 일부입니다. 별도 컴포넌트가 아닙니다.
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public partial class EnemyShooterCombatController
{
    // ── 외부 호출 입구: 기존 함수 이름을 유지하고 아래 Domain으로 일을 넘깁니다. ──
    private PerceptionDomain _perceptionDomain;
    private PerceptionDomain Perception => _perceptionDomain ??= new PerceptionDomain(this);
    /// <summary>현재 타겟을 향하는 수평 조준 방향을 구합니다. 유효한 타겟/방향이 없으면 false입니다.</summary>
    public bool TryGetCurrentAimDirection(out Vector3 aimDirection) => Perception.TryGetCurrentAimDirection(out aimDirection);
    /// <summary>조준 기준점(타겟의 조준 앵커 등)을 가져옵니다. 타겟이 없으면 false입니다.</summary>
    public bool TryGetCurrentAimPoint(out Vector3 aimPoint) => Perception.TryGetCurrentAimPoint(out aimPoint);
    /// <summary>현재 행동에 맞춰 타겟·조사 단서·이동 방향 중 바라볼 지점을 선택합니다.</summary>
    public bool TryGetCurrentLookPoint(out Vector3 lookPoint) => Perception.TryGetCurrentLookPoint(out lookPoint);
    /// <summary>NavMesh 진행 방향을 우선 사용하고, 없으면 순찰/조사 목적지 방향을 구합니다.</summary>
    public bool TryGetCurrentMoveDirection(out Vector3 direction) => Perception.TryGetCurrentMoveDirection(out direction);
    /// <summary>공격 불가 이유나 현재 버스트/반응 대기 상태를 Debug Monitor용 문자열로 설명합니다.</summary>
    private string ResolveAttackDebugStatus() => Perception.ResolveAttackDebugStatus();
    /// <summary>행동 차단과 긴급 회피를 먼저 처리한 뒤 시야·타겟 기억·현재 목표를 갱신합니다.</summary>
    public void TickAwareness()
    {
        Perception.TickAwareness();
        ObserveThrowableOpportunity();
    }
    /// <summary>강제 탐색 옵션으로 플레이어 획득을 시도합니다.</summary>
    public void ForceAcquirePlayer() => Perception.ForceAcquirePlayer();
    /// <summary>찾은 플레이어를 타겟으로 지정하고 경계·추적 유지 시간·기억 위치를 갱신합니다.</summary>
    public void AlertToPlayer() => Perception.AlertToPlayer();
    /// <summary>피격 경계 설정과 공격자/인지 범위를 검사하고, 허용된 플레이어 공격자에게 경계합니다.</summary>
    public bool TryAlertToPlayerFromDamage(GameObject attacker) => Perception.TryAlertToPlayerFromDamage(attacker);
    /// <summary>타겟·전투·조사 등 교전 진행 정보를 정리합니다. 단순 이동 정지와는 다릅니다.</summary>
    public void ClearCombat() => Perception.ClearCombat();
    /// <summary>탐색 주기와 시야 조건을 검사해 플레이어를 새 타겟으로 획득합니다.</summary>
    private void TryAcquireVisiblePlayer(bool force) => Perception.TryAcquireVisiblePlayer(force);
    /// <summary>타겟과 체력/물리/조준용 컴포넌트 캐시를 연결합니다.</summary>
    private void SetTarget(Transform target) => Perception.SetTarget(target);
    /// <summary>타겟 연결을 해제합니다. 인자에 따라 조사에 사용할 기억은 보존할 수 있습니다.</summary>
    private void ClearTarget(bool keepLastKnownPosition) => Perception.ClearTarget(keepLastKnownPosition);
    /// <summary>타겟을 잠시 놓쳐도 전투 추적을 유지할 수 있는 보장 시간을 갱신합니다.</summary>
    private void RefreshTargetLock() => Perception.RefreshTargetLock();
    /// <summary>현재 목표점을 기준으로 조준 방향을 갱신하고 캐릭터가 바라보는 방향을 맞춥니다.</summary>
    private void UpdateAimDirection() => Perception.UpdateAimDirection();
    /// <summary>타겟 조준 앵커 등 이용 가능한 기준으로 실제 조준할 월드 좌표를 구합니다.</summary>
    private Vector3 ResolveTargetPoint() => Perception.ResolveTargetPoint();
    /// <summary>마지막 목격 시간과 타겟 기억 설정을 기준으로 타겟을 잊을 때인지 판정합니다.</summary>
    private bool HasForgottenTarget() => Perception.HasForgottenTarget();
    /// <summary>체력 변화 이벤트를 받아 이전 체력 기록과 관련 반응을 갱신합니다.</summary>
    private void OnHealthChanged(Damageable damageable) => Perception.OnHealthChanged(damageable);
    /// <summary>전역 피격 이벤트가 이 적에 해당하는지 확인하고 피격 경계를 처리합니다.</summary>
    private void OnHitReceived(GameObject hitTarget) => Perception.OnHitReceived(hitTarget);
    /// <summary>전달된 오브젝트가 이 적의 오브젝트 계층에 속하는지 확인합니다.</summary>
    private bool IsThisShooter(GameObject target) => Perception.IsThisShooter(target);
    /// <summary>소리/공격 발신원이 자기 자신인지 확인해 자기 자극을 거릅니다.</summary>
    private bool IsOwnSource(GameObject source) => Perception.IsOwnSource(source);
    /// <summary>전달된 오브젝트에서 플레이어 소유자를 찾아 타겟 Transform을 구합니다.</summary>
    private bool TryResolvePlayerTransform(GameObject attacker, out Transform playerTransform) => Perception.TryResolvePlayerTransform(attacker, out playerTransform);
    /// <summary>전달된 위치가 현재 인지 범위 안인지 확인합니다.</summary>
    private bool IsWithinAwarenessRange(Vector3 position) => Perception.IsWithinAwarenessRange(position);
    /// <summary>이동 디버그 옵션과 출력 간격을 적용해 이동 상태 로그를 남깁니다.</summary>
    private void DebugMovement(string message) => Perception.DebugMovement(message);
    // ── 실제 처리 코드: 감지·타겟·조준 ────────────────────────────────────
    // _owner는 별도 적이 아니라 이 기능을 소유한 원래 컨트롤러입니다. 공유 필드는 메인 파일에 있습니다.
    private sealed class PerceptionDomain
    {
        private readonly EnemyShooterCombatController _owner;
        public PerceptionDomain(EnemyShooterCombatController owner)
        {
            _owner = owner;
        }

        // 현재 타겟을 향하는 수평 조준 방향을 구합니다. 유효한 타겟/방향이 없으면 false입니다.
        public bool TryGetCurrentAimDirection(out Vector3 aimDirection)
        {
            aimDirection = default;
            if (!_owner.HasTarget || _owner._target == null)
                return false;
            aimDirection = _owner._aimDirection;
            aimDirection.y = 0f;
            if (aimDirection.sqrMagnitude < 0.0001f)
            {
                aimDirection = _owner.ResolveTargetPoint() - _owner.transform.position;
                aimDirection.y = 0f;
            }

            if (aimDirection.sqrMagnitude < 0.0001f)
                return false;
            aimDirection.Normalize();
            return true;
        }

        // 조준 기준점(타겟의 조준 앵커 등)을 가져옵니다. 타겟이 없으면 false입니다.
        public bool TryGetCurrentAimPoint(out Vector3 aimPoint)
        {
            aimPoint = default;
            if (!_owner.HasTarget || _owner._target == null)
                return false;
            aimPoint = _owner.ResolveTargetPoint();
            return true;
        }

        // 현재 행동에 맞춰 타겟·조사 단서·이동 방향 중 바라볼 지점을 선택합니다.
        public bool TryGetCurrentLookPoint(out Vector3 lookPoint)
        {
            lookPoint = default;
            if (_owner._currentGoal == EnemyShooterGoal.AttackTarget || _owner._currentGoal == EnemyShooterGoal.ChaseTarget)
            {
                if (_owner.HasTarget && _owner._target != null)
                {
                    lookPoint = _owner._target.position;
                    return true;
                }

                if (_owner._hasLastKnownPosition)
                {
                    lookPoint = _owner._lastKnownPosition;
                    return true;
                }
            }

            if (_owner._currentGoal == EnemyShooterGoal.Investigate && _owner._currentInvestigationType == EnemyShooterInvestigationType.Sound && _owner._investigationArrived && EnemyShooterCombatController.TryNormalizePlanar(-_owner._soundInvestigationDirection, out Vector3 incomingDirection))
            {
                lookPoint = _owner.transform.position + incomingDirection;
                return true;
            }

            if (_owner._currentGoal == EnemyShooterGoal.Investigate && _owner.TryResolveInvestigationDestination(out lookPoint))
            {
                return true;
            }

            if (_owner.TryGetCurrentMoveDirection(out Vector3 moveDirection))
            {
                lookPoint = _owner.transform.position + moveDirection;
                return true;
            }

            return false;
        }

        // NavMesh 진행 방향을 우선 사용하고, 없으면 순찰/조사 목적지 방향을 구합니다.
        public bool TryGetCurrentMoveDirection(out Vector3 direction)
        {
            direction = default;
            if (_owner.CanUseAgent())
            {
                if (EnemyShooterCombatController.TryNormalizePlanar(_owner._agent.desiredVelocity, out direction))
                    return true;
                if (EnemyShooterCombatController.TryNormalizePlanar(_owner._agent.velocity, out direction))
                    return true;
                if (_owner._agent.hasPath && EnemyShooterCombatController.TryNormalizePlanar(_owner._agent.steeringTarget - _owner.transform.position, out direction))
                {
                    return true;
                }
            }

            if (_owner._currentGoal == EnemyShooterGoal.Patrol && _owner._hasPatrolDestination && EnemyShooterCombatController.TryNormalizePlanar(_owner._patrolDestination - _owner.transform.position, out direction))
            {
                return true;
            }

            if (_owner._currentGoal == EnemyShooterGoal.Investigate && _owner.TryResolveInvestigationDestination(out Vector3 investigationDestination) && EnemyShooterCombatController.TryNormalizePlanar(investigationDestination - _owner.transform.position, out direction))
            {
                return true;
            }

            return false;
        }

        // 공격 불가 이유나 현재 버스트/반응 대기 상태를 Debug Monitor용 문자열로 설명합니다.
        public string ResolveAttackDebugStatus()
        {
            if (_owner._damageable != null && _owner._damageable.IsDead)
                return "Dead";
            if (_owner.IsActionBlockedByStatusEffect)
                return "Status effect blocks actions";
            if (!_owner.HasTarget)
                return _owner.HasInvestigation ? "Investigating" : "No target";
            float targetDistance = _owner.TargetPlanarDistance;
            if (!_owner.CanSeeTarget)
            {
                if (_owner.CanFinishStartedBurstAfterSightLost)
                    return $"Finishing burst ({_owner._burstShotsRemaining})";
                if (_owner.CanHoldBurstCooldownAfterSightLost)
                    return $"Post-burst cooldown ({_owner.AttackCooldownRemaining:0.00}s)";
                if (_owner._vision != null && _owner._target != null)
                {
                    bool insideCloseRange = targetDistance <= _owner.CloseVisionRadius;
                    bool insideForwardRange = _owner._vision.IsInsideVisionRange(_owner._target.position);
                    if (!insideCloseRange && !insideForwardRange)
                        return "Out of vision";
                    if (!insideCloseRange && _owner.UsesForwardVisionArc)
                    {
                        Vector3 toTarget = _owner._target.position - _owner._vision.VisualOrigin.position;
                        toTarget.y = 0f;
                        if (toTarget.sqrMagnitude > 0.0001f && Vector3.Angle(_owner._vision.ForwardDirection, toTarget) > _owner.VisionArcAngle * 0.5f)
                        {
                            return "Outside FOV";
                        }
                    }

                    if (_owner.UsesVisionLineOfSight)
                    {
                        Collider blocker = _owner._vision.LastLineOfSightBlocker;
                        return blocker != null ? $"LOS: {blocker.name}" : "LOS blocked";
                    }

                    return "Cannot see target";
                }

                return "Cannot see target";
            }

            if (targetDistance > _owner.ActiveAttackRange)
                return "Out of attack range";
            if (!_owner.HasClearMuzzlePathToTarget())
                return "Muzzle path blocked";
            if (_owner.ReactionTimeRemaining > 0f)
                return $"Reaction {_owner.ReactionTimeRemaining:0.00}s";
            if (_owner.AttackCooldownRemaining > 0f)
                return $"Cooldown {_owner.AttackCooldownRemaining:0.00}s";
            if (_owner._attackActive)
                return _owner._attackPhase == EnemyShooterAttackPhase.None ? "Attacking" : _owner._attackPhase.ToString();
            return _owner.CanAttackTarget ? "Ready" : "Attack blocked";
        }

        // 행동 차단과 긴급 회피를 먼저 처리한 뒤 시야·타겟 기억·현재 목표를 갱신합니다.
        public void TickAwareness()
        {
            if (_owner.TryHoldForStatusActionBlock())
                return;
            // Idle uses only the awareness action, while combat states also run a
            // dedicated movement/attack tick. Drive emergency evasion here as well
            // so every Enemy Shooter state responds to an incoming throwable.
            if (_owner.TryTickIncomingThrowableEvasion())
                return;
            if (_owner.TickDashMovement())
                return;
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null)
                return;
            _owner.TryDashWhenHealthLow();
            if (!_owner.HasTarget)
            {
                _owner.ClearTarget(_owner._hasLastKnownPosition || _owner._hasSoundInvestigation);
                _owner.TryAcquireVisiblePlayer(false);
                _owner.RefreshGoal();
                return;
            }

            bool couldSeeTarget = _owner._canSeeTarget;
            _owner._canSeeTarget = _owner._vision == null || _owner._vision.CanSeeTarget(_owner._target);
            if (_owner._canSeeTarget)
            {
                _owner.ShareDetectedTarget(_owner._target);
                if (!couldSeeTarget)
                    _owner.BeginDirectSightReaction(_owner._hasSeenTargetDuringCurrentEngagement);
                _owner._isAlerted = true;
                _owner._lastSeenTime = Time.time;
                _owner.RefreshTargetLock();
                _owner.SetLastKnownPosition(_owner._target.position);
                _owner.RefreshGoal();
                return;
            }

            if (couldSeeTarget || _owner._reactionStartedTime >= 0f)
                _owner.ClearActiveReaction();
            if (_owner.IsTargetLocked && _owner._target != null)
            {
                _owner._isAlerted = true;
                _owner.SetLastKnownPosition(_owner._target.position);
                _owner.RefreshGoal();
                return;
            }

            if (_owner.HasForgottenTarget())
                _owner.ClearTarget(true);
            _owner.RefreshGoal();
        }

        // 강제 탐색 옵션으로 플레이어 획득을 시도합니다.
        public void ForceAcquirePlayer()
        {
            _owner.TryAcquireVisiblePlayer(true);
        }

        // 찾은 플레이어를 타겟으로 지정하고 경계·추적 유지 시간·기억 위치를 갱신합니다.
        public void AlertToPlayer()
        {
            PlayerStatsRuntime player = UnityEngine.Object.FindAnyObjectByType<PlayerStatsRuntime>();
            if (player == null)
                return;
            _owner.SetTarget(player.transform);
            _owner._isAlerted = true;
            _owner.RefreshTargetLock();
            _owner.SetLastKnownPosition(player.transform.position);
            _owner._lastSeenTime = Time.time;
            _owner._hasSoundInvestigation = false;
            _owner.RefreshGoal();
        }

        // 피격 경계 설정과 공격자/인지 범위를 검사하고, 허용된 플레이어 공격자에게 경계합니다.
        public bool TryAlertToPlayerFromDamage(GameObject attacker)
        {
            EnemyShooterConfigSO config = _owner.Config;
            if (config != null && !config.AlertOnDamaged)
                return false;
            if (!_owner.TryResolvePlayerTransform(attacker, out Transform playerTransform))
                return false;
            if (!_owner.IsWithinAwarenessRange(playerTransform.position))
                return false;
            bool couldSeeTarget = _owner._target == playerTransform && _owner._canSeeTarget;
            bool isPursuitReacquisition = _owner._hasSeenTargetDuringCurrentEngagement;
            _owner.SetTarget(playerTransform);
            _owner._isAlerted = true;
            _owner._canSeeTarget = _owner._vision == null || _owner._vision.CanSeeTarget(playerTransform);
            if (_owner._canSeeTarget && !couldSeeTarget)
                _owner.BeginDirectSightReaction(isPursuitReacquisition);
            else if (!_owner._canSeeTarget)
                _owner.ClearActiveReaction();
            _owner.RefreshTargetLock();
            _owner.SetLastKnownPosition(playerTransform.position);
            _owner._lastSeenTime = Time.time;
            _owner._hasSoundInvestigation = false;
            _owner.FaceDirection(playerTransform.position - _owner.transform.position);
            _owner.RefreshGoal();
            _owner.ShareDetectedTarget(playerTransform, true);
            return true;
        }

        // 타겟·전투·조사 등 교전 진행 정보를 정리합니다. 단순 이동 정지와는 다릅니다.
        public void ClearCombat()
        {
            _owner.ClearTarget(false);
            _owner.ClearSoundInvestigation();
            _owner._aimDirection = Vector3.zero;
            _owner._burstShotsRemaining = 0;
            _owner.ResetBurstTracking();
            _owner._nextFireTime = 0f;
            _owner._nextBurstShotTime = 0f;
            _owner._attackPhase = EnemyShooterAttackPhase.None;
            _owner.ClearIncomingThrowableEvasion();
            _owner.RefreshGoal();
        }

        // 탐색 주기와 시야 조건을 검사해 플레이어를 새 타겟으로 획득합니다.
        public void TryAcquireVisiblePlayer(bool force)
        {
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null)
                return;
            if (!force && Time.time < _owner._nextTargetSearchTime)
                return;
            _owner._nextTargetSearchTime = Time.time + config.TargetSearchInterval;
            PlayerStatsRuntime player = UnityEngine.Object.FindAnyObjectByType<PlayerStatsRuntime>();
            if (player == null)
                return;
            Transform playerTransform = player.transform;
            bool canSeePlayer = _owner._vision == null || _owner._vision.CanSeeTarget(playerTransform);
            if (!canSeePlayer)
                return;
            bool isPursuitReacquisition = _owner._hasSeenTargetDuringCurrentEngagement && _owner._hasLastKnownPosition;
            _owner.SetTarget(playerTransform);
            _owner._isAlerted = true;
            _owner._canSeeTarget = true;
            _owner.ShareDetectedTarget(playerTransform, true);
            _owner.BeginDirectSightReaction(isPursuitReacquisition);
            _owner.RefreshTargetLock();
            _owner._lastSeenTime = Time.time;
            _owner.SetLastKnownPosition(playerTransform.position);
            _owner.RefreshGoal();
        }

        // 타겟과 체력/물리/조준용 컴포넌트 캐시를 연결합니다.
        public void SetTarget(Transform target)
        {
            if (_owner._target == target)
                return;
            _owner.ClearTarget(false);
            _owner._target = target;
            if (_owner._target != null)
            {
                _owner._targetDamageable = _owner._target.GetComponent<Damageable>() ?? _owner._target.GetComponentInParent<Damageable>();
                _owner._targetRigidbody = _owner._target.GetComponent<Rigidbody>() ?? _owner._target.GetComponentInParent<Rigidbody>();
                ShooterAimTargetAnchor aimTarget = EntityComponentResolver.Resolve<ShooterAimTargetAnchor>(_owner._target);
                _owner._targetAimAnchor = aimTarget != null ? aimTarget.Anchor : _owner._target;
                _owner.RefreshTargetLock();
            }
        }

        // 타겟 연결을 해제합니다. 인자에 따라 조사에 사용할 기억은 보존할 수 있습니다.
        public void ClearTarget(bool keepLastKnownPosition)
        {
            _owner.ResetThrowableOpportunity();
            _owner.ReleaseCoverTactic();
            EnemyShooterTacticalIntentSelector.ReleaseDuelHolder(_owner);
            _owner.ResetTacticalIntent();
            _owner._target = null;
            _owner._targetDamageable = null;
            _owner._targetRigidbody = null;
            _owner._targetAimAnchor = null;
            _owner._isAlerted = _owner._hasSoundInvestigation;
            _owner._canSeeTarget = false;
            _owner._targetLockUntilTime = -1f;
            // Sound and last-known-target are independent clues. Treating a sound
            // as last-known target data created a fake destination at Vector3.zero.
            _owner._hasLastKnownPosition = keepLastKnownPosition && _owner._hasLastKnownPosition;
            _owner.ClearActiveReaction();
            _owner._lastSeenTime = -1f;
            _owner._burstShotsRemaining = 0;
            _owner.ResetBurstTracking();
            if (!keepLastKnownPosition)
                _owner._hasSeenTargetDuringCurrentEngagement = false;
            if (!_owner._hasLastKnownPosition)
            {
                _owner._lastKnownPosition = Vector3.zero;
                _owner._lastKnownSetTime = -1f;
            }
        }

        // 타겟을 잠시 놓쳐도 전투 추적을 유지할 수 있는 보장 시간을 갱신합니다.
        public void RefreshTargetLock()
        {
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null || config.TargetLockMinSeconds <= 0f)
                return;
            _owner._targetLockUntilTime = Mathf.Max(_owner._targetLockUntilTime, Time.time + config.TargetLockMinSeconds);
        }

        // 현재 목표점을 기준으로 조준 방향을 갱신하고 캐릭터가 바라보는 방향을 맞춥니다.
        public void UpdateAimDirection()
        {
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null || _owner._target == null)
                return;
            Vector3 targetPoint = _owner.ResolveTargetPoint();
            Vector3 desired = targetPoint - _owner.transform.position;
            desired.y = 0f;
            if (desired.sqrMagnitude < 0.0001f)
                return;
            desired.Normalize();
            if (_owner._aimDirection.sqrMagnitude < 0.0001f || config.AimTrackingSpeedDegPerSecond <= 0f)
            {
                _owner._aimDirection = desired;
                _owner.FaceDirection(_owner._aimDirection);
                return;
            }

            _owner._aimDirection = Vector3.RotateTowards(_owner._aimDirection, desired, config.AimTrackingSpeedDegPerSecond * Mathf.Deg2Rad * Time.deltaTime, 0f);
            _owner.FaceDirection(_owner._aimDirection);
        }

        // 타겟 조준 앵커 등 이용 가능한 기준으로 실제 조준할 월드 좌표를 구합니다.
        public Vector3 ResolveTargetPoint()
        {
            EnemyShooterConfigSO config = _owner.Config;
            Vector3 targetPoint = _owner._targetAimAnchor != null ? _owner._targetAimAnchor.position : _owner._target != null ? _owner._target.position : _owner.transform.position;
            if (_owner._targetRigidbody != null && config != null && config.TargetLeadSeconds > 0f)
            {
                targetPoint += _owner._targetRigidbody.linearVelocity * config.TargetLeadSeconds;
            }

            return targetPoint;
        }

        // 마지막 목격 시간과 타겟 기억 설정을 기준으로 타겟을 잊을 때인지 판정합니다.
        public bool HasForgottenTarget()
        {
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null || _owner._lastSeenTime < 0f)
                return false;
            if (_owner.IsTargetLocked)
                return false;
            if (_owner.IsTargetBeyondChaseLoseRange(config))
                return true;
            float memory=config.TargetMemorySeconds;
            return memory>0f&&Time.time>=_owner._lastSeenTime+memory;
        }

        // 체력 변화 이벤트를 받아 이전 체력 기록과 관련 반응을 갱신합니다.
        public void OnHealthChanged(Damageable damageable)
        {
            if (damageable == null || damageable != _owner._damageable)
                return;
            _owner._lastHealth = damageable.CurrentHealth;
        }

        // 전역 피격 이벤트가 이 적에 해당하는지 확인하고 피격 경계를 처리합니다.
        public void OnHitReceived(GameObject hitTarget)
        {
            if (hitTarget == null)
                return;
            if (!_owner.IsThisShooter(hitTarget))
                return;
        // HitReceivedEvent has no attacker context. Do not globally alert from here.
        }

        // 전달된 오브젝트가 이 적의 오브젝트 계층에 속하는지 확인합니다.
        public bool IsThisShooter(GameObject target)
        {
            if (target == _owner.gameObject)
                return true;
            Transform current = target.transform;
            while (current != null)
            {
                if (current == _owner.transform)
                    return true;
                current = current.parent;
            }

            return false;
        }

        // 소리/공격 발신원이 자기 자신인지 확인해 자기 자극을 거릅니다.
        public bool IsOwnSource(GameObject source)
        {
            if (source == null)
                return false;
            if (source == _owner.gameObject)
                return true;
            return source.transform.IsChildOf(_owner.transform) || _owner.transform.IsChildOf(source.transform);
        }

        // 전달된 오브젝트에서 플레이어 소유자를 찾아 타겟 Transform을 구합니다.
        public bool TryResolvePlayerTransform(GameObject attacker, out Transform playerTransform)
        {
            playerTransform = null;
            if (attacker == null)
                return false;
            PlayerCharacter player = attacker.GetComponent<PlayerCharacter>() ?? attacker.GetComponentInParent<PlayerCharacter>();
            if (player == null)
                return false;
            Damageable playerDamageable = player.GetComponent<Damageable>() ?? player.GetComponentInParent<Damageable>();
            if (playerDamageable == null || playerDamageable.IsDead)
                return false;
            playerTransform = player.transform;
            return playerTransform != null;
        }

        // 전달된 위치가 현재 인지 범위 안인지 확인합니다.
        public bool IsWithinAwarenessRange(Vector3 position)
        {
            EnemyShooterConfigSO config = _owner.Config;
            float range = config != null ? config.Vision.VisionRange : 0f;
            if (range <= 0f)
                return false;
            Vector3 from = _owner.transform.position;
            from.y = 0f;
            position.y = 0f;
            return Vector3.Distance(from, position) <= range;
        }

        // 이동 디버그 옵션과 출력 간격을 적용해 이동 상태 로그를 남깁니다.
        public void DebugMovement(string message)
        {
            if (!_owner._debugMovement)
                return;
            if (Time.time < _owner._nextMovementDebugTime)
                return;
            _owner._nextMovementDebugTime = Time.time + Mathf.Max(0.1f, _owner._debugMovementInterval);
            Debug.Log($"[EnemyShooterCombatController] {message}", _owner);
        }
    }
}
