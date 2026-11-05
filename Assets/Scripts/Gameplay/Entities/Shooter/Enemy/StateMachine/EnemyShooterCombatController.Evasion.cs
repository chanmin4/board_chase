/*
 * [긴급 회피·대시·위험 지역]
 * 날아오는 수류탄/탄환, 낮은 체력, 위험 장판에 대응하는 이동을 수정할 때 봅니다.
 * 주요 입구: TryTickIncomingThrowableEvasion / TickDashMovement / TryGetHarmfulAreaAvoidanceDestination.
 * 연결: 여러 행동 Tick에서 공통으로 호출하며, 대시 컴포넌트와 Navigation을 통해 이동합니다.
 * 주의: 투척물 회피의 true는 이번 Tick을 회피에 사용하라는 신호입니다. 실제 이동 불가 상황에도 true일 수 있습니다.
 *
 * 이 파일도 EnemyShooterCombatController의 일부입니다. 별도 컴포넌트가 아닙니다.
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public partial class EnemyShooterCombatController
{
    // ── 외부 호출 입구: 기존 함수 이름을 유지하고 아래 Domain으로 일을 넘깁니다. ──
    private EvasionDomain _evasionDomain;
    private EvasionDomain Evasion => _evasionDomain ??= new EvasionDomain(this);
    /// <summary>설정과 현재 대시 상태를 확인해 회피 대시를 시도합니다. 방향 지정이 없으면 회피 방향을 선택합니다.</summary>
    private bool TryStartDefensiveDash() => Evasion.TryStartDefensiveDash();
    /// <summary>설정과 현재 대시 상태를 확인해 회피 대시를 시도합니다. 방향 지정이 없으면 회피 방향을 선택합니다.</summary>
    private bool TryStartDefensiveDash(Vector3 preferredDirection) => Evasion.TryStartDefensiveDash(preferredDirection);
    /// <summary>NavMesh에서 대시 이동을 진행합니다. 장애물/유효 이동 지점이 없으면 취소하며, 대시가 진행 중이면 true입니다.</summary>
    private bool TickDashMovement() => Evasion.TickDashMovement();
    /// <summary>타겟 또는 마지막 목격 위치에서 멀어지는 방향을 우선하고, 없으면 임의 수평 방향을 고릅니다.</summary>
    private Vector3 ResolveDashDirection(EnemyShooterConfigSO config) => Evasion.ResolveDashDirection(config);
    /// <summary>체력 비율이 설정 임계값 아래이면 회피 대시를 시도합니다.</summary>
    private void TryDashWhenHealthLow() => Evasion.TryDashWhenHealthLow();
    /// <summary>검사 주기에 맞춰 적대 투척물 위험을 찾고, 새 위협이면 공격을 끊고 탈출 지점을 구합니다.</summary>
    private void RefreshIncomingThrowableThreat() => Evasion.RefreshIncomingThrowableThreat();
    /// <summary>투척물 위험 갱신 후 탈출 이동을 처리합니다. true면 일반 행동을 쉬어야 하며, 위험이 없으면 탄환 회피도 검사합니다.</summary>
    private bool TryTickIncomingThrowableEvasion() => Evasion.TryTickIncomingThrowableEvasion();
    /// <summary>예약된 버스트와 진행 행동 시간을 정리해 긴급 회피에 제어를 넘깁니다. 이미 발사된 탄환을 되돌리지는 않습니다.</summary>
    private void AbortCurrentAttackForIncomingThrowable() => Evasion.AbortCurrentAttackForIncomingThrowable();
    /// <summary>도달 가능한 후보 경로를 검사해 폭발 위험이 낮고 위험 중심에서 먼 탈출 지점을 고릅니다.</summary>
    private bool TryResolveIncomingThrowableEscapeDestination(Vector3 dangerCenter, float dangerRadius, out Vector3 destination) => Evasion.TryResolveIncomingThrowableEscapeDestination(dangerCenter, dangerRadius, out destination);
    /// <summary>추적 중인 투척물 ID·위험 범위·탈출 목적지를 초기화합니다.</summary>
    private void ClearIncomingThrowableEvasion() => Evasion.ClearIncomingThrowableEvasion();
    /// <summary>새 적대 탄환 위협을 검사하고 설정 확률을 통과하면 탄도 옆으로 대시를 시도합니다.</summary>
    private void TryDashFromIncomingBullet() => Evasion.TryDashFromIncomingBullet();
    /// <summary>탄환 진행 방향의 좌우 중 대시 가능한 방향을 선택합니다.</summary>
    private Vector3 ResolveIncomingBulletDashDirection(Vector3 incomingDirection) => Evasion.ResolveIncomingBulletDashDirection(incomingDirection);
    /// <summary>예상 대시 끝 지점을 NavMesh에 투영하고 장애물에 막히는 방향인지 검사합니다.</summary>
    private bool CanDashInDirection(Vector3 direction) => Evasion.CanDashInDirection(direction);
    /// <summary>위험 장판 안에 있다면 주변 NavMesh 후보 중 위험도가 가장 낮은 이동 지점을 구합니다.</summary>
    private bool TryGetHarmfulAreaAvoidanceDestination(out Vector3 destination) => Evasion.TryGetHarmfulAreaAvoidanceDestination(out destination);
    /// <summary>지정 위치에서 이 적에게 적용되는 위험 지역의 합산 위험도를 조회합니다.</summary>
    private float EvaluateHarmfulAreaThreat(Vector3 worldPosition) => Evasion.EvaluateHarmfulAreaThreat(worldPosition);
    // ── 실제 처리 코드: 긴급 회피·대시·위험 지역 ────────────────────────────────────
    // _owner는 별도 적이 아니라 이 기능을 소유한 원래 컨트롤러입니다. 공유 필드는 메인 파일에 있습니다.
    private sealed class EvasionDomain
    {
        private readonly EnemyShooterCombatController _owner;
        public EvasionDomain(EnemyShooterCombatController owner)
        {
            _owner = owner;
        }

        // 설정과 현재 대시 상태를 확인해 회피 대시를 시도합니다. 방향 지정이 없으면 회피 방향을 선택합니다.
        public bool TryStartDefensiveDash()
        {
            return _owner.TryStartDefensiveDash(Vector3.zero);
        }

        // 설정과 현재 대시 상태를 확인해 회피 대시를 시도합니다. 방향 지정이 없으면 회피 방향을 선택합니다.
        public bool TryStartDefensiveDash(Vector3 preferredDirection)
        {
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null || !_owner.EffectiveUseDash || _owner._dashController == null || _owner._dashController.IsDashing)
                return false;
            Vector3 direction = preferredDirection.sqrMagnitude > 0.0001f ? preferredDirection.normalized : _owner.ResolveDashDirection(config);
            if (direction.sqrMagnitude < 0.0001f)
                return false;
            if (_owner.CanUseAgent())
            {
                _owner._agent.ResetPath();
                _owner._agent.isStopped = false;
            }

            bool started = _owner._dashController.TryStartDefensiveDash(direction);
            if (!started)
                return false;
            _owner._effects?.SetFootTrailActive(false);
            _owner.FaceDirection(_owner._dashController.DashDirection);
            return true;
        }

        // NavMesh에서 대시 이동을 진행합니다. 장애물/유효 이동 지점이 없으면 취소하며, 대시가 진행 중이면 true입니다.
        public bool TickDashMovement()
        {
            if (_owner._dashController == null || !_owner._dashController.IsDashing)
                return false;
            if (!_owner.CanUseAgent())
            {
                _owner._dashController.CancelDash();
                _owner.DebugMovement("Dash stopped because NavMeshAgent is unavailable or off NavMesh.");
                return false;
            }

            Vector3 velocity = _owner._dashController.BuildDashMovementVector(0f);
            Vector3 movement = new Vector3(velocity.x, 0f, velocity.z) * Time.deltaTime;
            if (movement.sqrMagnitude <= 0.000001f)
                return true;
            EnemyShooterConfigSO config = _owner.Config;
            float sampleDistance = config != null ? config.NavMeshSampleDistance : 1f;
            Vector3 desiredPosition = _owner.transform.position + movement;
            if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, Mathf.Max(0.1f, sampleDistance), NavMesh.AllAreas))
            {
                if (NavMesh.Raycast(_owner.transform.position, hit.position, out NavMeshHit blockHit, NavMesh.AllAreas))
                {
                    _owner._dashController.CancelDash();
                    _owner.DebugMovement($"Dash stopped by NavMesh obstacle. desired={desiredPosition}, hit={blockHit.position}");
                    return false;
                }

                ShooterBodyCollisionMotor.Ensure(_owner._agent).CommitPlannedPosition(hit.position);
            }
            else
            {
                _owner._dashController.CancelDash();
                _owner.DebugMovement($"Dash stopped because target position is outside NavMesh. desired={desiredPosition}");
                return false;
            }

            return true;
        }

        // 타겟 또는 마지막 목격 위치에서 멀어지는 방향을 우선하고, 없으면 임의 수평 방향을 고릅니다.
        public Vector3 ResolveDashDirection(EnemyShooterConfigSO config)
        {
            Vector3 direction = Vector3.zero;
            if (config.DashAwayFromTarget && _owner._target != null)
                direction = _owner.transform.position - _owner._target.position;
            else if (_owner._hasLastKnownPosition)
                direction = _owner.transform.position - _owner._lastKnownPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                return direction.normalized;
            Vector2 random = Random.insideUnitCircle;
            if (random.sqrMagnitude <= 0.0001f)
                random = Vector2.right;
            return new Vector3(random.x, 0f, random.y).normalized;
        }

        // 체력 비율이 설정 임계값 아래이면 회피 대시를 시도합니다.
        public void TryDashWhenHealthLow()
        {
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null || !_owner.EffectiveUseDash || !_owner.EffectiveDashWhenHealthLow || _owner._damageable == null || _owner._damageable.MaxHealth <= 0f)
            {
                return;
            }

            float normalizedHealth = Mathf.Clamp01(_owner._damageable.CurrentHealth / _owner._damageable.MaxHealth);
            if (normalizedHealth > _owner.EffectiveDashHealthThreshold)
                return;
            _owner.TryStartDefensiveDash();
        }

        // 검사 주기에 맞춰 적대 투척물 위험을 찾고, 새 위협이면 공격을 끊고 탈출 지점을 구합니다.
        public void RefreshIncomingThrowableThreat()
        {
            if (Time.time < _owner._nextIncomingThrowableCheckTime)
                return;
            _owner._nextIncomingThrowableCheckTime = Time.time + EnemyShooterCombatController.IncomingThrowableCheckInterval;
            bool found = ThrowableProjectile.TryGetClosestHostileThreat(_owner.gameObject, ShooterFactionUtility.Resolve(_owner), _owner.transform.position, EnemyShooterCombatController.IncomingThrowableSafetyPadding, out Vector3 dangerCenter, out float dangerRadius, out int projectileId);
            if (!found)
            {
                _owner.ClearIncomingThrowableEvasion();
                return;
            }

            bool isNewThreat = !_owner._isEvadingIncomingThrowable || _owner._incomingThrowableId != projectileId;
            if (isNewThreat)
                _owner._incomingThrowableEscapeDestination = default;
            _owner._isEvadingIncomingThrowable = true;
            _owner._incomingThrowableId = projectileId;
            _owner._incomingThrowableDangerCenter = dangerCenter;
            _owner._incomingThrowableDangerRadius = dangerRadius;
            _owner._isAlerted = true;
            if (isNewThreat)
                _owner.AbortCurrentAttackForIncomingThrowable();
            if (_owner.TryResolveIncomingThrowableEscapeDestination(dangerCenter, dangerRadius, out Vector3 escapeDestination))
            {
                _owner._incomingThrowableEscapeDestination = escapeDestination;
            }
        }

        // 투척물 위험 갱신 후 탈출 이동을 처리합니다. true면 일반 행동을 쉬어야 하며, 위험이 없으면 탄환 회피도 검사합니다.
        public bool TryTickIncomingThrowableEvasion()
        {
            _owner.RefreshIncomingThrowableThreat();
            if (!_owner._isEvadingIncomingThrowable)
            {
                _owner.TryDashFromIncomingBullet();
                return false;
            }

            if (!_owner.CanUseAgent())
            {
                _owner.DebugMovement($"Incoming throwable detected but agent cannot move. " + $"projectileId={_owner._incomingThrowableId}, center={_owner._incomingThrowableDangerCenter}");
                return true;
            }

            _owner._agent.speed = _owner.ResolveMoveSpeed();
            _owner._agent.stoppingDistance = 0f;
            bool needsDestination = _owner._incomingThrowableEscapeDestination == default || _owner.HasReached(_owner._incomingThrowableEscapeDestination, 0.25f);
            if (needsDestination)
            {
                if (!_owner.TryResolveIncomingThrowableEscapeDestination(_owner._incomingThrowableDangerCenter, _owner._incomingThrowableDangerRadius, out Vector3 refreshedDestination))
                {
                    _owner.StopAgentHard("Incoming throwable has no reachable escape destination");
                    return true;
                }

                _owner._incomingThrowableEscapeDestination = refreshedDestination;
            }

            _owner.TrySetAgentDestination(_owner._incomingThrowableEscapeDestination, 0f, "Incoming throwable evasion");
            return true;
        }

        // 예약된 버스트와 진행 행동 시간을 정리해 긴급 회피에 제어를 넘깁니다. 이미 발사된 탄환을 되돌리지는 않습니다.
        public void AbortCurrentAttackForIncomingThrowable()
        {
            // A shot is synchronous: by the time the throwable is observed, the
            // currently executing shot has already completed. Remove every queued
            // burst shot so the next frame belongs to emergency movement.
            _owner._burstShotsRemaining = 0;
            _owner._nextBurstShotTime = 0f;
            _owner.ResetBurstTracking();
            if (_owner._attackActive)
            {
                _owner._attackPhase = EnemyShooterAttackPhase.BurstCooldown;
                _owner._nextFireTime = Mathf.Max(_owner._nextFireTime, Time.time + 0.1f);
            }

            if (_owner._meleeActionActive)
                _owner._meleeActionFinishTime = Time.time;
            if (_owner._throwableActionActive)
                _owner._throwableActionFinishTime = Time.time;
            _owner._dashController?.CancelDash();
            _owner.ClearNavigationRequest();
            _owner.DebugMovement($"Incoming throwable preempted combat. projectileId={_owner._incomingThrowableId}, " + $"dangerCenter={_owner._incomingThrowableDangerCenter}, dangerRadius={_owner._incomingThrowableDangerRadius:0.00}");
        }

        // 도달 가능한 후보 경로를 검사해 폭발 위험이 낮고 위험 중심에서 먼 탈출 지점을 고릅니다.
        public bool TryResolveIncomingThrowableEscapeDestination(Vector3 dangerCenter, float dangerRadius, out Vector3 destination)
        {
            destination = default;
            if (!_owner.CanUseAgent())
                return false;
            Vector3 away = _owner.transform.position - dangerCenter;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
                away = _owner.transform.forward;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
                away = Vector3.forward;
            away.Normalize();
            float escapeDistance = Mathf.Max(EnemyShooterCombatController.IncomingThrowableMinimumEscapeDistance, dangerRadius + 0.75f);
            float bestThreat = float.PositiveInfinity;
            float bestDangerDistance = float.NegativeInfinity;
            bool found = false;
            for (int i = 0; i < EnemyShooterCombatController.IncomingThrowableEscapeSampleCount; i++)
            {
                float angle = i * (360f / EnemyShooterCombatController.IncomingThrowableEscapeSampleCount);
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * away;
                Vector3 rawCandidate = _owner.transform.position + direction * escapeDistance;
                if (!_owner.TryProjectToNavMesh(rawCandidate, out Vector3 candidate))
                    continue;
                // Only score a point the agent can actually reach. This prevents an
                // apparently safe point on the opposite side of a structure from
                // winning while the shooter remains trapped inside the blast area.
                _owner._navigationPath.ClearCorners();
                if (!_owner._agent.CalculatePath(candidate, _owner._navigationPath) || _owner._navigationPath.status == NavMeshPathStatus.PathInvalid)
                {
                    continue;
                }

                Vector3[] corners = _owner._navigationPath.corners;
                if (_owner._navigationPath.status == NavMeshPathStatus.PathPartial)
                {
                    if (corners == null || corners.Length == 0)
                        continue;
                    candidate = corners[corners.Length - 1];
                }

                float threat = ThrowableProjectile.EvaluateHostileThreatAt(_owner.gameObject, ShooterFactionUtility.Resolve(_owner), candidate, EnemyShooterCombatController.IncomingThrowableSafetyPadding);
                float dangerDistance = EnemyShooterCombatController.PlanarSqrDistance(candidate, dangerCenter);
                bool isBetter = !found || threat < bestThreat - 0.001f || (Mathf.Abs(threat - bestThreat) <= 0.001f && dangerDistance > bestDangerDistance);
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

        // 추적 중인 투척물 ID·위험 범위·탈출 목적지를 초기화합니다.
        public void ClearIncomingThrowableEvasion()
        {
            _owner._isEvadingIncomingThrowable = false;
            _owner._incomingThrowableId = 0;
            _owner._incomingThrowableDangerCenter = default;
            _owner._incomingThrowableDangerRadius = 0f;
            _owner._incomingThrowableEscapeDestination = default;
        }

        // 새 적대 탄환 위협을 검사하고 설정 확률을 통과하면 탄도 옆으로 대시를 시도합니다.
        public void TryDashFromIncomingBullet()
        {
            if (Time.time < _owner._nextIncomingBulletCheckTime)
                return;
            _owner._nextIncomingBulletCheckTime = Time.time + EnemyShooterCombatController.IncomingBulletCheckInterval;
            if (!_owner.EffectiveDashOnIncomingProjectile || _owner._damageable == null || _owner._damageable.IsDead || _owner.IsDashing)
            {
                return;
            }

            if (!BasicBullet.TryGetClosestHostileIncomingThreat(_owner.gameObject, ShooterFactionUtility.Resolve(_owner), EnemyShooterCombatController.IncomingBulletPredictionSeconds, out Vector3 incomingDirection, out int projectileId))
            {
                _owner._lastIncomingBulletThreatId = 0;
                return;
            }

            if (_owner._lastIncomingBulletThreatId == projectileId)
                return;
            _owner._lastIncomingBulletThreatId = projectileId;
            if (Random.value > _owner.EffectiveDashChanceOnIncomingProjectile)
                return;
            _owner.TryStartDefensiveDash(_owner.ResolveIncomingBulletDashDirection(incomingDirection));
        }

        // 탄환 진행 방향의 좌우 중 대시 가능한 방향을 선택합니다.
        public Vector3 ResolveIncomingBulletDashDirection(Vector3 incomingDirection)
        {
            incomingDirection.y = 0f;
            if (incomingDirection.sqrMagnitude <= 0.0001f)
                return Vector3.zero;
            incomingDirection.Normalize();
            Vector3 left = new Vector3(-incomingDirection.z, 0f, incomingDirection.x);
            Vector3 right = -left;
            Vector3 first = Random.value < 0.5f ? left : right;
            Vector3 second = first == left ? right : left;
            if (_owner.CanDashInDirection(first))
                return first;
            return _owner.CanDashInDirection(second) ? second : Vector3.zero;
        }

        // 예상 대시 끝 지점을 NavMesh에 투영하고 장애물에 막히는 방향인지 검사합니다.
        public bool CanDashInDirection(Vector3 direction)
        {
            if (!_owner.CanUseAgent() || direction.sqrMagnitude <= 0.0001f)
                return false;
            EnemyShooterConfigSO config = _owner.Config;
            float distance = config != null ? config.DashSpeed * config.DashDurationSeconds : 0f;
            Vector3 desiredPosition = _owner.transform.position + direction.normalized * Mathf.Max(0.5f, distance);
            if (!NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, Mathf.Max(0.1f, config != null ? config.NavMeshSampleDistance : 1f), NavMesh.AllAreas))
            {
                return false;
            }

            return !NavMesh.Raycast(_owner.transform.position, hit.position, out _, NavMesh.AllAreas);
        }

        // 위험 장판 안에 있다면 주변 NavMesh 후보 중 위험도가 가장 낮은 이동 지점을 구합니다.
        public bool TryGetHarmfulAreaAvoidanceDestination(out Vector3 destination)
        {
            destination = default;
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null || !config.AvoidHarmfulAreas)
                return false;
            if (_owner._attackActive && !config.AvoidHarmfulAreasWhileAttacking)
                return false;
            if (Time.time < _owner._nextHarmfulAreaCheckTime)
                return false;
            _owner._nextHarmfulAreaCheckTime = Time.time + config.HarmfulAreaCheckInterval;
            float currentThreat = _owner.EvaluateHarmfulAreaThreat(_owner.transform.position);
            if (currentThreat <= 0f)
                return false;
            int sampleCount = Mathf.Max(1, config.HarmfulAreaSampleCount);
            float radius = Mathf.Max(0.1f, config.HarmfulAreaSearchRadius);
            bool found = false;
            float bestThreat = float.PositiveInfinity;
            for (int i = 0; i < sampleCount; i++)
            {
                float angle = (i / (float)sampleCount) * Mathf.PI * 2f;
                Vector3 raw = _owner.transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                if (!NavMesh.SamplePosition(raw, out NavMeshHit hit, config.HarmfulAreaNavMeshSampleDistance, NavMesh.AllAreas))
                {
                    continue;
                }

                float candidateThreat = _owner.EvaluateHarmfulAreaThreat(hit.position);
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

        // 지정 위치에서 이 적에게 적용되는 위험 지역의 합산 위험도를 조회합니다.
        public float EvaluateHarmfulAreaThreat(Vector3 worldPosition)
        {
            return SpecialAreaRuntime.EvaluateCombinedThreat(_owner.gameObject, worldPosition);
        }
    }
}
