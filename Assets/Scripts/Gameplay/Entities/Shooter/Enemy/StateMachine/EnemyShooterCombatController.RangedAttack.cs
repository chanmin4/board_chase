/*
 * [원거리 사격·버스트]
 * 공격 시작/종료, 연사 묶음, 발사 간격, 공격 중 자리 조정을 수정할 때 봅니다.
 * 주요 입구: BeginAttack -> TickAttack -> EndAttack. 실제 한 발 발사는 TryFireOnce.
 * 연결: EquipmentAttacks가 총을 준비하고, Tactics가 엄폐/피킹을, Navigation이 이동을 담당합니다.
 * 주의: TickAttack은 상태이상 -> 긴급 회피 -> 대시 -> 엄폐/측면 행동을 먼저 확인한 뒤 일반 사격을 처리합니다.
 *
 * 이 파일도 EnemyShooterCombatController의 일부입니다. 별도 컴포넌트가 아닙니다.
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public partial class EnemyShooterCombatController
{
    // ── 외부 호출 입구: 기존 함수 이름을 유지하고 아래 Domain으로 일을 넘깁니다. ──
    private RangedAttackDomain _rangedAttackDomain;
    private RangedAttackDomain RangedAttack => _rangedAttackDomain ??= new RangedAttackDomain(this);
    /// <summary>행동 차단/원거리 무기를 확인하고 버스트 상태·공격 목표·전술을 준비합니다.</summary>
    public void BeginAttack() => RangedAttack.BeginAttack();
    /// <summary>긴급 행동과 엄폐/측면 이동을 우선 처리하고, 공격 가능 범위·반응 지연을 반영해 사격을 갱신합니다.</summary>
    public void TickAttack() => RangedAttack.TickAttack();
    /// <summary>공격 플래그와 남은 버스트를 해제합니다. 타겟도 없으면 엄폐 예약까지 정리합니다.</summary>
    public void EndAttack() => RangedAttack.EndAttack();
    /// <summary>버스트 시작·한 발 발사·다음 발 시각·버스트 휴식 단계를 진행합니다.</summary>
    private void UpdateBurstFire() => RangedAttack.UpdateBurstFire();
    /// <summary>엄폐 접근 중 사격 옵션과 발사 조건이 허용할 때 이동 중 발사를 시도합니다.</summary>
    private void TryFireWhileMovingToCover() => RangedAttack.TryFireWhileMovingToCover();
    /// <summary>공격 중 설정된 이동 방식을 갱신합니다. 경로 적용은 Navigation으로 넘깁니다.</summary>
    private void TickAttackMovement() => RangedAttack.TickAttackMovement();
    /// <summary>총구 경로가 막혔을 때 새 사격 위치를 찾고 이동을 요청합니다.</summary>
    private void TickMuzzleBlockedReposition() => RangedAttack.TickMuzzleBlockedReposition();
    /// <summary>현재 조준 방향으로 사격 컴포넌트에 한 발 발사를 요청하고 성공 여부를 반환합니다.</summary>
    private bool TryFireOnce() => RangedAttack.TryFireOnce();
    /// <summary>현재 버스트가 실제로 발사되었는지 추적하는 정보를 초기화합니다.</summary>
    private void ResetBurstTracking() => RangedAttack.ResetBurstTracking();
    /// <summary>현재 무기/설정에 적용할 초당 발사 수를 구합니다.</summary>
    private float ResolveShotsPerSecond() => RangedAttack.ResolveShotsPerSecond();
    /// <summary>버스트 사이 휴식 시간을 설정에서 구합니다.</summary>
    private float ResolveBurstCooldownSeconds() => RangedAttack.ResolveBurstCooldownSeconds();
    /// <summary>버스트 사이 이동할 수 있는 후보 위치를 찾습니다.</summary>
    private bool TryResolveBurstIntervalMoveDestination(out Vector3 destination) => RangedAttack.TryResolveBurstIntervalMoveDestination(out destination);
    /// <summary>탄도가 열린 위치를 우선 찾고, 없으면 장애물을 돌아갈 측면 이동 후보를 구합니다. true가 열린 탄도를 보장하지는 않습니다.</summary>
    private bool TryResolveMuzzleClearanceDestination(out Vector3 destination) => RangedAttack.TryResolveMuzzleClearanceDestination(out destination);
    /// <summary>총구에서 타겟까지 발사 경로를 검사합니다. 같은 프레임 검사 결과는 캐시를 사용합니다.</summary>
    private bool HasClearMuzzlePathToTarget() => RangedAttack.HasClearMuzzlePathToTarget();
    /// <summary>새 원거리 공격을 시작할 때 적용할 사거리를 구합니다.</summary>
    private float ResolveAttackStartRange() => RangedAttack.ResolveAttackStartRange();
    /// <summary>진행 중인 원거리 공격을 유지할 때 적용할 사거리를 구합니다.</summary>
    private float ResolveAttackKeepRange() => RangedAttack.ResolveAttackKeepRange();
    /// <summary>공격 시작/유지 여부와 선호 거리를 반영해 직접 사격에 쓸 범위를 구합니다.</summary>
    private float ResolveDirectAttackRange(bool keepingAttack) => RangedAttack.ResolveDirectAttackRange(keepingAttack);
    // ── 실제 처리 코드: 원거리 사격·버스트 ────────────────────────────────────
    // _owner는 별도 적이 아니라 이 기능을 소유한 원래 컨트롤러입니다. 공유 필드는 메인 파일에 있습니다.
    private sealed class RangedAttackDomain
    {
        private readonly EnemyShooterCombatController _owner;
        private Vector3 _shotAimKick;
        private float _lastKickTime;
        private RangedWeaponSO _kickWeapon;
        public RangedAttackDomain(EnemyShooterCombatController owner)
        {
            _owner = owner;
        }

        // 행동 차단/원거리 무기를 확인하고 버스트 상태·공격 목표·전술을 준비합니다.
        public void BeginAttack()
        {
            _owner._stamina?.StopSprint();
            if (_owner.TryHoldForStatusActionBlock())
                return;
            if (!_owner.TryActivateRangedWeaponForCombat())
                return;
            _owner._attackActive = true;
            _owner._burstShotsRemaining = 0;
            _owner.ResetBurstTracking();
            _owner._attackPhase = EnemyShooterAttackPhase.FiringBurst;
            _owner._currentGoal = EnemyShooterGoal.AttackTarget;
            _owner._attackStrafeSign = Random.value < 0.5f ? -1 : 1;
            _owner.TryStartCombatTactics();
            if (_owner.CanUseAgent())
            {
                _owner._agent.speed = _owner.ResolveMoveSpeed();
                _owner._agent.stoppingDistance = _owner.ResolveChaseStoppingDistance();
                _owner.StopAgentHard("BeginAttack");
            }
        }

        // 긴급 행동과 엄폐/측면 이동을 우선 처리하고, 공격 가능 범위·반응 지연을 반영해 사격을 갱신합니다.
        public void TickAttack()
        {
            // 1. 긴급 처리가 이번 Tick을 차지하면 일반 사격은 하지 않습니다. 이 우선순서는 동작에 영향을 줍니다.
            if (_owner.TryHoldForStatusActionBlock())
                return;
            if (_owner.TryTickIncomingThrowableEvasion())
                return;
            if (_owner.TickDashMovement())
                return;
            if (!_owner.MustFinishCurrentBurst && _owner.Navigation.TryTickRecovery())
                return;
            // 2. 엄폐/측면 이동이 우선입니다. 엄폐 접근 중 사격은 별도 허용 옵션으로만 진행합니다.
            // 이미 시작한 버스트는 완료/발사 실패까지 진행합니다. 엄폐·측면 이동 갱신이
            // 매번 먼저 return하여 남은 탄이 영원히 줄지 않는 변경 잠금도 방지합니다.
            bool finishingCommittedBurst = _owner.MustFinishCurrentBurst;
            bool isMaintainingCover = !finishingCommittedBurst && _owner.TryTickCoverTactic();
            bool isMovingToFlank = !finishingCommittedBurst && !isMaintainingCover && _owner.TryTickActiveCombatFlank();
            if (isMaintainingCover || isMovingToFlank)
            {
                _owner.UpdateAimDirection();
                if (isMaintainingCover)
                    _owner.TryFireWhileMovingToCover();
                return;
            }

            // 3. 직접 시야를 잃어도 이미 시작한 버스트/휴식을 유지하는 기존 예외를 구분합니다.
            bool finishingStartedBurst = !_owner.CanDirectlyAttackTarget && _owner.CanFinishStartedBurstAfterSightLost;
            bool holdingBurstCooldown = !_owner.CanDirectlyAttackTarget && _owner.CanHoldBurstCooldownAfterSightLost;
            if (!_owner.CanAttackTarget)
            {
                _owner._attackActive = false;
                // HFSM 조건 검사 이후 시야/거리가 바뀌었을 때를 위한 추적 대체 처리입니다.
                // 이 호출은 HFSM 상태 에셋을 직접 전환하는 것이 아니라 이동 행동을 이어갑니다.
                if (_owner.ShouldChaseTarget)
                {
                    _owner.BeginChase();
                    _owner.TickChase();
                    return;
                }

                _owner.StopAgentHard("Attack target unavailable");
                return;
            }

            if (!finishingStartedBurst && !holdingBurstCooldown && _owner.TryGetHarmfulAreaAvoidanceDestination(out Vector3 avoidanceDestination))
            {
                _owner.UpdateAimDirection();
                _owner.TrySetAgentDestination(avoidanceDestination, 0f, "Attack harmful-area avoidance");
                return;
            }

            if (finishingStartedBurst)
            {
                // 시작한 버스트를 마치는 동안에는 타겟의 현재 위치를 계속 조준합니다.
                // 벽을 무시하는 탄환이 되는 것은 아니며 실제 투사체 충돌은 그대로 적용됩니다.
                _owner.UpdateAimDirection();
            }
            else if (!holdingBurstCooldown)
            {
                _owner.UpdateAimDirection();
            }

            // 4. 발견/재발견 반응 시간이 끝나야 사격 단계를 진행합니다.
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null || _owner.ReactionTimeRemaining > 0f)
            {
                _owner.StopAgentHard("Attack waiting reaction");
                return;
            }

            // 5. 버스트 휴식 중에는 설정된 이동/대기만 처리합니다. 휴식 종료 후 다음 Tick부터 새 버스트를 시작합니다.
            if (_owner._attackPhase == EnemyShooterAttackPhase.BurstCooldown)
            {
                if (holdingBurstCooldown)
                {
                    _owner.StopAgentHard("Post-burst cooldown after sight lost");
                    return;
                }

                if (config.MoveDuringBurstInterval)
                    _owner.TickAttackMovement();
                else
                    _owner.StopAgentHard("Attack burst cooldown");
                if (Time.time >= _owner._nextFireTime)
                {
                    _owner._attackPhase = EnemyShooterAttackPhase.FiringBurst;
                    _owner._burstShotsRemaining = 0;
                    _owner._nextBurstShotTime = 0f;
                }

                return;
            }

            // 6. 발사 단계: 이동 설정을 반영한 뒤 발사 타이머/남은 탄 수를 진행합니다.
            if (config.MoveWhileAttacking)
                _owner.TickAttackMovement();
            else
                _owner.StopAgentHard("Attack firing");
            _owner.UpdateBurstFire();
        }

        // 공격 플래그와 남은 버스트를 해제합니다. 타겟도 없으면 엄폐 예약까지 정리합니다.
        public void EndAttack()
        {
            _owner._attackActive = false;
            _owner._burstShotsRemaining = 0;
            _owner.ResetBurstTracking();
            _owner._attackPhase = EnemyShooterAttackPhase.None;
            _owner._nextCombatMoveRefreshTime = 0f;
            if (!_owner.HasTarget)
                _owner.ReleaseCoverTactic();
        }

        // 버스트 시작·한 발 발사·다음 발 시각·버스트 휴식 단계를 진행합니다.
        public void UpdateBurstFire()
        {
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null)
                return;
            if (_owner._burstShotsRemaining <= 0)
            {
                if (Time.time < _owner._nextFireTime)
                    return;
                _owner._burstShotsRemaining = config.BurstCount;
                _owner._nextBurstShotTime = Time.time;
                _owner.ResetBurstTracking();
            }

            if (Time.time < _owner._nextBurstShotTime)
                return;
            if (_owner.TryFireOnce())
            {
                _owner._burstShotsRemaining--;
                _owner._nextBurstShotTime = Time.time + config.BurstInterval;
                if (_owner._burstShotsRemaining <= 0)
                {
                    float interval = _owner.ResolveBurstCooldownSeconds();
                    _owner._nextFireTime = Time.time + interval;
                    _owner._nextCombatMoveRefreshTime = 0f;
                    _owner._attackPhase = EnemyShooterAttackPhase.BurstCooldown;
                }
            }
            else
            {
                _owner._burstShotsRemaining = 0;
                _owner._nextFireTime = Time.time + 0.1f;
                _owner._attackPhase = EnemyShooterAttackPhase.BurstCooldown;
            }
        }

        // 엄폐 접근 중 사격 옵션과 발사 조건이 허용할 때 이동 중 발사를 시도합니다.
        public void TryFireWhileMovingToCover()
        {
            if (!_owner.EffectiveFireWhileMovingToCover || _owner._coverTacticPhase != EnemyShooterCoverTacticPhase.MovingToCover || !_owner.CanDirectlyAttackTarget || _owner.ReactionTimeRemaining > 0f)
            {
                return;
            }

            _owner.UpdateBurstFire();
        }

        // 공격 중 설정된 이동 방식을 갱신합니다. 경로 적용은 Navigation으로 넘깁니다.
        public void TickAttackMovement()
        {
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null || !_owner.CanUseAgent() || _owner._target == null)
                return;
            if (Time.time < _owner._nextCombatMoveRefreshTime)
                return;
            _owner._nextCombatMoveRefreshTime = Time.time + config.CombatMoveRefreshInterval;
            if (!_owner.TryResolveBurstIntervalMoveDestination(out Vector3 destination))
            {
                _owner.StopMovement();
                return;
            }

            _owner.TrySetAgentDestination(destination, 0f, "Burst interval move");
        }

        // 총구 경로가 막혔을 때 새 사격 위치를 찾고 이동을 요청합니다.
        public void TickMuzzleBlockedReposition()
        {
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null || !_owner.CanUseAgent() || _owner._target == null)
                return;
            if (Time.time < _owner._nextCombatMoveRefreshTime)
                return;
            _owner._nextCombatMoveRefreshTime = Time.time + config.CombatMoveRefreshInterval;
            if (!_owner.TryResolveMuzzleClearanceDestination(out Vector3 destination))
            {
                _owner.StopAgentHard("Muzzle blocked and no reposition destination");
                return;
            }

            _owner.TrySetAgentDestination(destination, 0.05f, "Muzzle-clearance reposition");
        }

        // 현재 조준 방향으로 사격 컴포넌트에 한 발 발사를 요청하고 성공 여부를 반환합니다.
        public bool TryFireOnce()
        {
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null || _owner._shoot == null || _owner._shoot.CurrentBullet == null || _owner._target == null)
                return false;
            Vector3 targetPoint = _owner.ResolveTargetPoint();
            Vector3 shotOrigin = _owner._shoot.TryGetAimOrigin(out Vector3 resolvedOrigin) ? resolvedOrigin : _owner.transform.position;
            Vector3 direction = targetPoint - shotOrigin;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = _owner._aimDirection;
                direction.y = 0f;
            }

            if (direction.sqrMagnitude < 0.0001f)
                return false;
            direction.Normalize();
            var weapon = _owner._shoot.CurrentRangedWeapon;
            if (_kickWeapon != weapon) { _kickWeapon = weapon; _shotAimKick = Vector3.zero; }
            float recovery = _owner._statsRuntime != null ? _owner._statsRuntime.ShotKickRecoveryPerSecond
                : weapon != null ? weapon.ShotKickRecoveryPerSecond : 0f;
            _shotAimKick = Vector3.MoveTowards(_shotAimKick, Vector3.zero, recovery * Mathf.Max(0f, Time.time - _lastKickTime));
            _lastKickTime = Time.time;
            float fullSpreadAngle = _owner._statsRuntime != null ? _owner._statsRuntime.HipFireSpreadAngleDeg
                : weapon != null ? weapon.HipFireSpreadAngleDeg : 0f;
            if (_owner._stamina != null) fullSpreadAngle += _owner._stamina.HitExtraSpreadDegrees;
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
            Vector3 aimPoint = shotOrigin + direction.normalized * targetDistance + _shotAimKick;
            bool fired = _owner._shoot.TryFireAt(aimPoint);
            if (fired)
            {
                _owner._hasFiredCurrentBurst = true;
                float kick = _owner._statsRuntime != null ? _owner._statsRuntime.ShotKickDistance
                    : weapon != null ? weapon.ShotKickDistance : 0f;
                float maximum = _owner._statsRuntime != null ? _owner._statsRuntime.MaximumShotKickDistance
                    : weapon != null ? weapon.MaximumShotKickDistance : 0f;
                _shotAimKick = Vector3.ClampMagnitude(_shotAimKick +
                    Quaternion.AngleAxis(Random.Range(-45f, 45f), Vector3.up) * -direction * kick, maximum);
            }
            return fired;
        }

        // 현재 버스트가 실제로 발사되었는지 추적하는 정보를 초기화합니다.
        public void ResetBurstTracking()
        {
            _owner._hasFiredCurrentBurst = false;
        }

        // 현재 무기/설정에 적용할 초당 발사 수를 구합니다.
        public float ResolveShotsPerSecond()
        {
            EnemyShooterConfigSO config = _owner.Config;
            if (_owner._statsRuntime != null)
                return Mathf.Max(0.01f, _owner._statsRuntime.ResolveShotsPerSecond(_owner._shoot != null ? _owner._shoot.CurrentBullet : null));
            return _owner._shoot != null && _owner._shoot.CurrentRangedWeapon != null
                ? _owner._shoot.CurrentRangedWeapon.ShotsPerSecond : 0.01f;
        }

        // 버스트 사이 휴식 시간을 설정에서 구합니다.
        public float ResolveBurstCooldownSeconds()
        {
            EnemyShooterConfigSO config = _owner.Config;
            float fallback = 1f / _owner.ResolveShotsPerSecond();
            if (config == null || config.BurstCooldownSeconds <= 0f)
                return fallback;
            return config.BurstCooldownSeconds;
        }

        // 버스트 사이 이동할 수 있는 후보 위치를 찾습니다.
        public bool TryResolveBurstIntervalMoveDestination(out Vector3 destination)
        {
            destination = _owner.transform.position;
            if (_owner._target == null)
                return false;
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null)
                return false;
            Vector3 fromTarget = _owner.transform.position - _owner._target.position;
            fromTarget.y = 0f;
            if (fromTarget.sqrMagnitude < 0.0001f)
                fromTarget = -_owner.transform.forward;
            Vector3 away = fromTarget.normalized;
            Vector3 right = new Vector3(away.z, 0f, -away.x) * _owner._attackStrafeSign;
            float currentDistance = _owner.GetPlanarDistanceToTarget();
            float preferredDistance = Mathf.Max(0f, config.AttackPreferredDistance);
            float strafeDistance = Mathf.Max(0f, config.AttackStrafeDistance);
            Vector3 moveDirection;
            if (preferredDistance > 0f && currentDistance < preferredDistance * 0.8f)
                moveDirection = away;
            else if (preferredDistance > 0f && currentDistance > preferredDistance * 1.2f)
                moveDirection = -away;
            else
                moveDirection = right;
            Vector3 rawDestination = _owner.transform.position + moveDirection.normalized * Mathf.Max(0.25f, strafeDistance);
            if (!_owner.TryProjectToNavMesh(rawDestination, out destination))
                return false;
            return true;
        }

        // 탄도가 열린 위치를 우선 찾고, 없으면 장애물을 돌아갈 측면 이동 후보를 구합니다. true가 열린 탄도를 보장하지는 않습니다.
        public bool TryResolveMuzzleClearanceDestination(out Vector3 destination)
        {
            destination = _owner.transform.position;
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null || _owner._target == null || _owner._shoot == null)
                return false;
            Vector3 fromTarget = _owner.transform.position - _owner._target.position;
            fromTarget.y = 0f;
            if (fromTarget.sqrMagnitude < 0.0001f)
                fromTarget = -_owner.transform.forward;
            Vector3 away = fromTarget.normalized;
            Vector3 right = new Vector3(away.z, 0f, -away.x);
            Vector3 targetPoint = _owner.ResolveTargetPoint();
            float baseDistance = Mathf.Max(1f, config.AttackStrafeDistance);
            float keepRange = _owner.ResolveAttackKeepRange();
            bool foundClearCandidate = false;
            float bestDistanceSqr = float.PositiveInfinity;
            int bestSign = _owner._attackStrafeSign;
            // Prefer a short lateral step, then widen/angle away when nearby cover is large.
            for (int distancePass = 0; distancePass < 2; distancePass++)
            {
                float moveDistance = baseDistance * (distancePass == 0 ? 0.5f : 1f);
                for (int directionPass = 0; directionPass < 5; directionPass++)
                {
                    int sign = directionPass % 2 == 0 ? _owner._attackStrafeSign : -_owner._attackStrafeSign;
                    Vector3 moveDirection;
                    if (directionPass <= 1)
                        moveDirection = right * sign;
                    else if (directionPass <= 3)
                        moveDirection = (right * sign + away).normalized;
                    else
                        moveDirection = away;
                    Vector3 rawCandidate = _owner.transform.position + moveDirection * moveDistance;
                    if (!_owner.TryProjectToNavMesh(rawCandidate, out Vector3 candidate))
                        continue;
                    if (EnemyShooterCombatController.PlanarSqrDistance(candidate, _owner.transform.position) < 0.0625f)
                        continue;
                    if (keepRange > 0f && EnemyShooterCombatController.PlanarSqrDistance(candidate, _owner._target.position) > keepRange * keepRange)
                        continue;
                    if (!_owner._shoot.HasClearBallisticPathFromRootPosition(candidate, targetPoint, _owner._target))
                    {
                        continue;
                    }

                    float candidateDistanceSqr = EnemyShooterCombatController.PlanarSqrDistance(candidate, _owner.transform.position);
                    if (candidateDistanceSqr >= bestDistanceSqr)
                        continue;
                    destination = candidate;
                    bestDistanceSqr = candidateDistanceSqr;
                    bestSign = Vector3.Dot(candidate - _owner.transform.position, right) >= 0f ? 1 : -1;
                    foundClearCandidate = true;
                }
            }

            if (foundClearCandidate)
            {
                _owner._attackStrafeSign = bestSign;
                return true;
            }

            // If one step cannot clear a large obstacle, keep circling on a stable side;
            // the next refresh evaluates the translated muzzle again from the new position.
            for (int sidePass = 0; sidePass < 2; sidePass++)
            {
                int sign = sidePass == 0 ? _owner._attackStrafeSign : -_owner._attackStrafeSign;
                Vector3 rawCandidate = _owner.transform.position + right * sign * baseDistance;
                if (!_owner.TryProjectToNavMesh(rawCandidate, out Vector3 candidate) || EnemyShooterCombatController.PlanarSqrDistance(candidate, _owner.transform.position) < 0.0625f)
                {
                    continue;
                }

                destination = candidate;
                _owner._attackStrafeSign = sign;
                return true;
            }

            return false;
        }

        // 총구에서 타겟까지 발사 경로를 검사합니다. 같은 프레임 검사 결과는 캐시를 사용합니다.
        public bool HasClearMuzzlePathToTarget()
        {
            if (!_owner.HasTarget || _owner._shoot == null)
                return false;
            if (_owner._muzzleClearanceFrame == Time.frameCount)
                return _owner._muzzleClearanceResult;
            _owner._muzzleClearanceFrame = Time.frameCount;
            _owner._muzzleClearanceResult = _owner._shoot.HasClearBallisticPathTo(_owner.ResolveTargetPoint(), _owner._target);
            return _owner._muzzleClearanceResult;
        }

        // 새 원거리 공격을 시작할 때 적용할 사거리를 구합니다.
        public float ResolveAttackStartRange()
        {
            if (_owner._statsRuntime != null && _owner._statsRuntime.MaxRange > 0f)
                return _owner._statsRuntime.MaxRange;
            return _owner._shoot != null && _owner._shoot.CurrentRangedWeapon != null
                ? _owner._shoot.CurrentRangedWeapon.MaxRange : 0f;
        }

        // 진행 중인 원거리 공격을 유지할 때 적용할 사거리를 구합니다.
        public float ResolveAttackKeepRange()
        {
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null)
                return _owner.ResolveAttackStartRange();
            if (config.UseWeaponRangeAsAttackKeepRange) return _owner.ResolveAttackStartRange();

            return config.AttackKeepRange;
        }

        // 공격 시작/유지 여부와 선호 거리를 반영해 직접 사격에 쓸 범위를 구합니다.
        public float ResolveDirectAttackRange(bool keepingAttack)
        {
            float range = keepingAttack ? _owner.ResolveAttackKeepRange() : _owner.ResolveAttackStartRange();
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null || config.AttackMovementMode != EnemyShooterAttackMovementMode.StopWhileAttacking || !_owner.EffectiveUsePreferredRangeBeforeStationaryAttack || config.AttackPreferredDistance <= 0f)
            {
                return range;
            }

            float preferredRange = config.AttackPreferredDistance;
            if (keepingAttack)
                preferredRange += _owner.EffectiveStationaryAttackKeepRangePadding;
            return Mathf.Min(range, preferredRange);
        }
    }
}
