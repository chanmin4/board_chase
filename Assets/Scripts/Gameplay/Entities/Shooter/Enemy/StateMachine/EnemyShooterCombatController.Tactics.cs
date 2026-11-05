/*
 * [전술·엄폐·피킹]
 * 엄폐 자리 확보, 숨기/노출/복귀 단계, 전술 토큰과 측면 접근을 수정할 때 봅니다.
 * 주요 입구: TryResolveTacticalChaseDestination / TryStartCombatTactics / TryTickCoverTactic.
 * 연결: 전술 선택기와 엄폐 지점을 이용하며 경로는 Navigation, 노출 중 사격은 RangedAttack으로 이어집니다.
 * 주의: 엄폐 예약은 ReleaseCoverTactic에서 해제합니다. TryTickCoverTactic의 true는 일반 사격보다 엄폐 행동을 우선 처리했다는 뜻입니다.
 *
 * 이 파일도 EnemyShooterCombatController의 일부입니다. 별도 컴포넌트가 아닙니다.
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public partial class EnemyShooterCombatController
{
    // ── 외부 호출 입구: 기존 함수 이름을 유지하고 아래 Domain으로 일을 넘깁니다. ──
    private TacticsDomain _tacticsDomain;
    private TacticsDomain Tactics => _tacticsDomain ??= new TacticsDomain(this);
    /// <summary>직접 추적 대신 사용할 전술 목적지가 있는지 선택/갱신합니다.</summary>
    private bool TryResolveTacticalChaseDestination(out Vector3 destination, out float stoppingDistance, out string reason) => Tactics.TryResolveTacticalChaseDestination(out destination, out stoppingDistance, out reason);
    /// <summary>전술 갱신 주기에 맞춰 직접 접근·엄폐·측면 접근 등의 의도를 선택합니다.</summary>
    private void RefreshTacticalIntent(EnemyShooterConfigSO config) => Tactics.RefreshTacticalIntent(config);
    /// <summary>타겟 옆으로 접근할 수 있는 NavMesh 목적지를 찾습니다.</summary>
    private bool TryResolveTacticalFlankDestination(Vector3 focus, out Vector3 destination, out bool flankLeft) => Tactics.TryResolveTacticalFlankDestination(focus, out destination, out flankLeft);
    /// <summary>원거리 공격을 시작할 때 사용할 전술 의도/엄폐 행동을 준비합니다.</summary>
    private bool TryStartCombatTactics() => Tactics.TryStartCombatTactics();
    /// <summary>적합한 엄폐 자리와 피킹 위치를 확보하고 엄폐 이동 단계로 진입합니다.</summary>
    private bool TryAcquireCoverTactic() => Tactics.TryAcquireCoverTactic();
    /// <summary>디버그 테스트에 사용할 런타임 엄폐 지점 생성을 시도합니다. 작성된 프리팹을 저장하지 않습니다.</summary>
    private bool TryCreateDebugRuntimeCoverPoint(float searchRadiusSqr, out EnemyShooterCoverPoint point, out Vector3 coverAnchor, out Vector3 peekPosition) => Tactics.TryCreateDebugRuntimeCoverPoint(searchRadiusSqr, out point, out coverAnchor, out peekPosition);
    /// <summary>런타임 테스트용 엄폐 후보로 사용할 장애물인지 확인합니다.</summary>
    private bool IsDebugCoverObstacle(Collider collider) => Tactics.IsDebugCoverObstacle(collider);
    /// <summary>테스트용 엄폐 장애물 옆에서 타겟에 사격할 수 있는 피킹 위치를 찾습니다.</summary>
    private bool TryResolveDebugCoverPeek(Vector3 outerSurface, Vector3 awayFromTarget, float planarExtent, float clearance, Vector3 targetPoint, out Vector3 peekPosition) => Tactics.TryResolveDebugCoverPeek(outerSurface, awayFromTarget, planarExtent, clearance, targetPoint, out peekPosition);
    /// <summary>엄폐 지점의 좌우 피킹 후보 중 NavMesh와 탄도 조건을 만족하는 위치를 찾습니다.</summary>
    private bool TryResolveCoverPeekDestination(EnemyShooterCoverPoint point, out Vector3 peekDestination) => Tactics.TryResolveCoverPeekDestination(point, out peekDestination);
    /// <summary>엄폐 이동/숨기/노출/복귀를 갱신합니다. true면 엄폐가 행동을 차지하며, 사격 가능한 피킹 중에는 false로 일반 사격을 허용합니다.</summary>
    private bool TryTickCoverTactic() => Tactics.TryTickCoverTactic();
    /// <summary>진행 중인 좌/우 측면 접근을 갱신합니다. 도착하거나 무효화되면 전술을 해제합니다.</summary>
    private bool TryTickActiveCombatFlank() => Tactics.TryTickActiveCombatFlank();
    /// <summary>엄폐 예약과 테스트용 런타임 엄폐 오브젝트를 해제하고 내부 단계를 초기화합니다.</summary>
    private void ReleaseCoverTactic() => Tactics.ReleaseCoverTactic();
    /// <summary>엄폐 접근 중 사격하는 옵션이 켜져 있으면 접근 속도 배율을 적용합니다.</summary>
    private void ApplyCoverApproachSpeed() => Tactics.ApplyCoverApproachSpeed();
    /// <summary>전술 목적지/타겟/갱신 시간을 비우고 직접 접근 의도로 되돌립니다.</summary>
    private void ResetTacticalIntent() => Tactics.ResetTacticalIntent();
    // ── 실제 처리 코드: 전술·엄폐·피킹 ────────────────────────────────────
    // _owner는 별도 적이 아니라 이 기능을 소유한 원래 컨트롤러입니다. 공유 필드는 메인 파일에 있습니다.
    private sealed class TacticsDomain
    {
        private readonly EnemyShooterCombatController _owner;
        public TacticsDomain(EnemyShooterCombatController owner)
        {
            _owner = owner;
        }

        // 직접 추적 대신 사용할 전술 목적지가 있는지 선택/갱신합니다.
        public bool TryResolveTacticalChaseDestination(out Vector3 destination, out float stoppingDistance, out string reason)
        {
            destination = _owner.transform.position;
            stoppingDistance = 0f;
            reason = string.Empty;
            EnemyShooterConfigSO config = _owner.Config;
            if (_owner.Navigation.SuppressCombatTactics || !_owner.ShouldMaintainTacticalPressure || config == null)
            {
                if (_owner.HasActiveCoverTactic)
                    return false;
                EnemyShooterTacticalIntentSelector.ReleaseDuelHolder(_owner);
                _owner.ResetTacticalIntent();
                return false;
            }

            if (_owner._tacticalIntentTarget != _owner._target || Time.time >= _owner._nextTacticalIntentRefreshTime)
            {
                _owner.RefreshTacticalIntent(config);
            }

            switch (_owner._tacticalIntent)
            {
                case EnemyShooterTacticalIntent.HoldLastKnownPosition:
                    destination = _owner._lastKnownPosition;
                    stoppingDistance = _owner.ResolveChaseStoppingDistance();
                    reason = "Tactical hold last-known angle";
                    return true;
                case EnemyShooterTacticalIntent.FlankLeft:
                case EnemyShooterTacticalIntent.FlankRight:
                    destination = _owner._tacticalDestination;
                    stoppingDistance = 0f;
                    reason = _owner._tacticalIntent == EnemyShooterTacticalIntent.FlankLeft ? "Tactical left flank" : "Tactical right flank";
                    return true;
                default:
                    return false;
            }
        }

        // 전술 갱신 주기에 맞춰 직접 접근·엄폐·측면 접근 등의 의도를 선택합니다.
        public void RefreshTacticalIntent(EnemyShooterConfigSO config)
        {
            if (_owner.HasActiveCoverTactic)
            {
                _owner._reservedCoverPoint.TryReserve(_owner);
                return;
            }

            _owner._tacticalIntentTarget = _owner._target;
            _owner._nextTacticalIntentRefreshTime = Time.time + config.TacticalIntentRefreshInterval;
            _owner._tacticalIntent = EnemyShooterTacticalIntent.Direct;
            _owner._tacticalDestination = _owner.transform.position;
            bool holdsDuelToken = _owner.EffectiveUseDuelToken && EnemyShooterTacticalIntentSelector.TryClaimDuelHolder(_owner, _owner._target);
            if (holdsDuelToken)
            {
                _owner._tacticalIntent = EnemyShooterTacticalIntent.HoldLastKnownPosition;
                return;
            }

            if (_owner.EffectiveUseFlankingTactics && EnemyShooterTacticalIntentSelector.HasDuelHolder(_owner._target) && _owner.TryResolveTacticalFlankDestination(_owner._lastKnownPosition, out Vector3 flankDestination, out bool flankLeft))
            {
                _owner._tacticalIntent = flankLeft ? EnemyShooterTacticalIntent.FlankLeft : EnemyShooterTacticalIntent.FlankRight;
                _owner._tacticalDestination = flankDestination;
            }
        }

        // 타겟 옆으로 접근할 수 있는 NavMesh 목적지를 찾습니다.
        public bool TryResolveTacticalFlankDestination(Vector3 focus, out Vector3 destination, out bool flankLeft)
        {
            destination = _owner.transform.position;
            flankLeft = false;
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null)
                return false;
            Vector3 awayFromFocus = _owner.transform.position - focus;
            awayFromFocus.y = 0f;
            if (awayFromFocus.sqrMagnitude <= 0.0001f)
            {
                awayFromFocus = _owner.transform.forward;
                awayFromFocus.y = 0f;
            }

            if (awayFromFocus.sqrMagnitude <= 0.0001f)
                awayFromFocus = Vector3.forward;
            awayFromFocus.Normalize();
            int slot = EnemyShooterTacticalIntentSelector.ClaimFlankSlot(_owner, _owner._target);
            int preferredSign = (slot & 1) == 0 ? 1 : -1;
            int lane = slot / 2;
            float laneAngle = Mathf.Min(85f, config.TacticalFlankAngleDegrees + lane * 8f);
            float laneDistance = _owner.EffectiveTacticalFlankDistance + lane *
                Mathf.Max(1f, _owner._agent != null ? _owner._agent.radius * 2.5f : 1f);
            for (int attempt = 0; attempt < 2; attempt++)
            {
                int sign = attempt == 0 ? preferredSign : -preferredSign;
                Vector3 direction = Quaternion.AngleAxis(laneAngle * sign, Vector3.up) * awayFromFocus;
                Vector3 rawCandidate = focus + direction.normalized * laneDistance;
                if (!_owner.TryProjectToNavMesh(rawCandidate, out Vector3 candidate) || EnemyShooterCombatController.PlanarSqrDistance(candidate, _owner.transform.position) < 0.25f)
                {
                    continue;
                }

                _owner._attackStrafeSign = sign;
                destination = candidate;
                flankLeft = sign > 0;
                return true;
            }

            return false;
        }

        // 원거리 공격을 시작할 때 사용할 전술 의도/엄폐 행동을 준비합니다.
        public bool TryStartCombatTactics()
        {
            EnemyShooterConfigSO config = _owner.Config;
            if (_owner.Navigation.SuppressCombatTactics || config == null || !_owner.EffectiveUseTacticalIntent || !_owner.HasTarget)
                return false;
            if (_owner.HasActiveCoverTactic)
                return true;
            bool ownsDuelToken = _owner.EffectiveUseDuelToken && EnemyShooterTacticalIntentSelector.TryClaimDuelHolder(_owner, _owner._target);
            bool targetHasDuelHolder = EnemyShooterTacticalIntentSelector.HasDuelHolder(_owner._target);
            // Once one shooter owns the stand-off token, teammates take a wider lane
            // before considering another cover point. This keeps a squad from stacking
            // beside the same obstacle and preserves an exposed pressure angle.
            if (!ownsDuelToken && _owner.EffectiveUseFlankingTactics && targetHasDuelHolder && _owner.TryResolveTacticalFlankDestination(_owner._target.position, out Vector3 flankDestination, out bool flankLeft))
            {
                _owner._tacticalIntent = flankLeft ? EnemyShooterTacticalIntent.FlankLeft : EnemyShooterTacticalIntent.FlankRight;
                _owner._tacticalDestination = flankDestination;
                _owner._tacticalIntentTarget = _owner._target;
                _owner._nextTacticalIntentRefreshTime = Time.time + config.TacticalIntentRefreshInterval;
                return true;
            }

            if (_owner.EffectiveUseCoverTactics && _owner.TryAcquireCoverTactic())
                return true;
            return ownsDuelToken;
        }

        // 적합한 엄폐 자리와 피킹 위치를 확보하고 엄폐 이동 단계로 진입합니다.
        public bool TryAcquireCoverTactic()
        {
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null || !_owner.EffectiveUseCoverTactics || !_owner.HasTarget || !_owner.CanSeeTarget || _owner._shoot == null)
            {
                return false;
            }

            float searchRadiusSqr = _owner.EffectiveCoverSearchRadius * _owner.EffectiveCoverSearchRadius;
            float bestScore = float.PositiveInfinity;
            EnemyShooterCoverPoint bestPoint = null;
            Vector3 bestAnchor = default;
            Vector3 bestPeek = default;
            IReadOnlyList<EnemyShooterCoverPoint> points = EnemyShooterCoverPoint.RegisteredPoints;
            for (int index = 0; index < points.Count; index++)
            {
                EnemyShooterCoverPoint point = points[index];
                if (point == null || !point.isActiveAndEnabled || !point.HasPeekPoint || (point.Occupant != null && point.Occupant != _owner))
                {
                    continue;
                }

                if (!_owner.TryProjectToNavMesh(point.CoverPosition, out Vector3 coverAnchor) || EnemyShooterCombatController.PlanarSqrDistance(coverAnchor, _owner.transform.position) > searchRadiusSqr)
                {
                    continue;
                }

                // A cover anchor is valid only when the obstacle really blocks its
                // direct shot. A separate reachable peek position must restore it.
                if (_owner._shoot.HasClearBallisticPathFromRootPosition(coverAnchor, _owner.ResolveTargetPoint(), _owner._target))
                {
                    continue;
                }

                if (!_owner.TryResolveCoverPeekDestination(point, out Vector3 peekDestination))
                    continue;
                float score = EnemyShooterCombatController.PlanarSqrDistance(coverAnchor, _owner.transform.position);
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
            if (bestPoint == null && _owner._debugCombatOverridesEnabled && _owner.TryCreateDebugRuntimeCoverPoint(searchRadiusSqr, out EnemyShooterCoverPoint runtimePoint, out Vector3 runtimeAnchor, out Vector3 runtimePeek))
            {
                bestPoint = runtimePoint;
                bestAnchor = runtimeAnchor;
                bestPeek = runtimePeek;
            }

            if (bestPoint == null || !bestPoint.TryReserve(_owner))
            {
                if (bestPoint != null && bestPoint == _owner._runtimeAutoCoverPoint)
                {
                    UnityEngine.Object.Destroy(bestPoint.gameObject);
                    _owner._runtimeAutoCoverPoint = null;
                }

                return false;
            }

            _owner._reservedCoverPoint = bestPoint;
            _owner._coverTacticPhase = EnemyShooterCoverTacticPhase.MovingToCover;
            _owner._coverPeekDestination = bestPeek;
            _owner._coverPhaseEndsAt = 0f;
            _owner._tacticalIntentTarget = _owner._target;
            _owner._tacticalDestination = bestAnchor;
            return true;
        }

        // 디버그 테스트에 사용할 런타임 엄폐 지점 생성을 시도합니다. 작성된 프리팹을 저장하지 않습니다.
        public bool TryCreateDebugRuntimeCoverPoint(float searchRadiusSqr, out EnemyShooterCoverPoint point, out Vector3 coverAnchor, out Vector3 peekPosition)
        {
            point = null;
            coverAnchor = default;
            peekPosition = default;
            if (!_owner.HasTarget || _owner._shoot == null || _owner._runtimeAutoCoverPoint != null)
                return false;
            Collider[] colliders = Physics.OverlapSphere(_owner.transform.position, _owner.EffectiveCoverSearchRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float bestScore = float.PositiveInfinity;
            Vector3 bestAnchor = default;
            Vector3 bestPeek = default;
            Vector3 targetPoint = _owner.ResolveTargetPoint();
            for (int index = 0; index < colliders.Length; index++)
            {
                Collider collider = colliders[index];
                if (!_owner.IsDebugCoverObstacle(collider))
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
                float clearance = Mathf.Max(0.45f, (_owner._agent != null ? _owner._agent.radius : 0.25f) + 0.2f);
                Vector3 outerSurface = collider.ClosestPoint(bounds.center + awayFromTarget * (planarExtent + 10f));
                Vector3 rawCoverAnchor = outerSurface + awayFromTarget * clearance;
                if (!_owner.TryProjectToNavMesh(rawCoverAnchor, out Vector3 candidateAnchor) || EnemyShooterCombatController.PlanarSqrDistance(candidateAnchor, _owner.transform.position) > searchRadiusSqr || _owner._shoot.HasClearBallisticPathFromRootPosition(candidateAnchor, targetPoint, _owner._target))
                {
                    continue;
                }

                if (!_owner.TryResolveDebugCoverPeek(outerSurface, awayFromTarget, planarExtent, clearance, targetPoint, out Vector3 candidatePeek))
                {
                    continue;
                }

                float score = EnemyShooterCombatController.PlanarSqrDistance(candidateAnchor, _owner.transform.position);
                if (score >= bestScore)
                    continue;
                bestScore = score;
                bestAnchor = candidateAnchor;
                bestPeek = candidatePeek;
            }

            if (float.IsPositiveInfinity(bestScore))
                return false;
            _owner._runtimeAutoCoverPoint = EnemyShooterCoverPoint.CreateRuntime(bestAnchor, bestPeek);
            point = _owner._runtimeAutoCoverPoint;
            coverAnchor = bestAnchor;
            peekPosition = bestPeek;
            return point != null;
        }

        // 런타임 테스트용 엄폐 후보로 사용할 장애물인지 확인합니다.
        public bool IsDebugCoverObstacle(Collider collider)
        {
            if (collider == null || !collider.enabled || collider.isTrigger || collider.transform.IsChildOf(_owner.transform) || (_owner._target != null && collider.transform.IsChildOf(_owner._target)))
            {
                return false;
            }

            // Damageable colliders belong to players or other shooters, not static
            // scenery. Do not turn an entity into temporary cover during testing.
            return collider.GetComponentInParent<Damageable>() == null;
        }

        // 테스트용 엄폐 장애물 옆에서 타겟에 사격할 수 있는 피킹 위치를 찾습니다.
        public bool TryResolveDebugCoverPeek(Vector3 outerSurface, Vector3 awayFromTarget, float planarExtent, float clearance, Vector3 targetPoint, out Vector3 peekPosition)
        {
            peekPosition = default;
            Vector3 side = Vector3.Cross(Vector3.up, awayFromTarget);
            if (side.sqrMagnitude < 0.0001f)
                return false;
            side.Normalize();
            float sideOffset = planarExtent + clearance;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                float sign = attempt == 0 ? (_owner._attackStrafeSign >= 0 ? 1f : -1f) : (_owner._attackStrafeSign >= 0 ? -1f : 1f);
                Vector3 rawPeek = outerSurface + side * (sideOffset * sign) + awayFromTarget * 0.05f;
                if (!_owner.TryProjectToNavMesh(rawPeek, out Vector3 candidate) || !_owner._shoot.HasClearBallisticPathFromRootPosition(candidate, targetPoint, _owner._target))
                {
                    continue;
                }

                peekPosition = candidate;
                return true;
            }

            return false;
        }

        // 엄폐 지점의 좌우 피킹 후보 중 NavMesh와 탄도 조건을 만족하는 위치를 찾습니다.
        public bool TryResolveCoverPeekDestination(EnemyShooterCoverPoint point, out Vector3 peekDestination)
        {
            peekDestination = default;
            if (point == null || _owner._shoot == null || !_owner.HasTarget)
                return false;
            bool preferLeft = _owner._attackStrafeSign >= 0;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                if (!point.TryGetPeekPosition(preferLeft, attempt, out Vector3 rawPeek) || !_owner.TryProjectToNavMesh(rawPeek, out Vector3 candidate))
                {
                    continue;
                }

                if (!_owner._shoot.HasClearBallisticPathFromRootPosition(candidate, _owner.ResolveTargetPoint(), _owner._target))
                {
                    continue;
                }

                peekDestination = candidate;
                return true;
            }

            return false;
        }

        // 엄폐 이동/숨기/노출/복귀를 갱신합니다. true면 엄폐가 행동을 차지하며, 사격 가능한 피킹 중에는 false로 일반 사격을 허용합니다.
        public bool TryTickCoverTactic()
        {
            if (!_owner.HasActiveCoverTactic)
                return false;
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null || !_owner.EffectiveUseCoverTactics || !_owner.HasTarget || _owner.HasForgottenTarget() || !_owner._reservedCoverPoint.TryReserve(_owner))
            {
                _owner.ReleaseCoverTactic();
                return false;
            }

            if (!_owner.CanUseAgent())
                return true;
            Vector3 coverDestination = _owner._reservedCoverPoint.CoverPosition;
            if (!_owner.TryProjectToNavMesh(coverDestination, out coverDestination))
            {
                _owner.ReleaseCoverTactic();
                return false;
            }

            float arriveDistance = config.CoverArriveDistance;
            switch (_owner._coverTacticPhase)
            {
                case EnemyShooterCoverTacticPhase.MovingToCover:
                    if (_owner.HasReached(coverDestination, arriveDistance))
                    {
                        _owner._coverTacticPhase = EnemyShooterCoverTacticPhase.Hidden;
                        _owner._coverPhaseEndsAt = Time.time + _owner.EffectiveCoverHideSeconds;
                        _owner.StopAgentHard("Cover reached");
                    }
                    else
                    {
                        _owner.TrySetAgentDestination(coverDestination, arriveDistance, "Move to cover");
                        _owner.ApplyCoverApproachSpeed();
                    }

                    return true;
                case EnemyShooterCoverTacticPhase.Hidden:
                    _owner.StopAgentHard("Waiting behind cover");
                    if (Time.time < _owner._coverPhaseEndsAt)
                        return true;
                    if (!_owner.TryResolveCoverPeekDestination(_owner._reservedCoverPoint, out _owner._coverPeekDestination))
                    {
                        _owner.ReleaseCoverTactic();
                        return false;
                    }

                    _owner._coverTacticPhase = EnemyShooterCoverTacticPhase.MovingToPeek;
                    return true;
                case EnemyShooterCoverTacticPhase.MovingToPeek:
                    if (_owner.HasReached(_owner._coverPeekDestination, arriveDistance))
                    {
                        _owner._coverTacticPhase = EnemyShooterCoverTacticPhase.Peeking;
                        _owner._coverPhaseEndsAt = Time.time + _owner.EffectiveCoverPeekSeconds;
                        _owner.StopAgentHard("Cover peek reached");
                    }
                    else
                    {
                        _owner.TrySetAgentDestination(_owner._coverPeekDestination, arriveDistance, "Move to cover peek");
                    }

                    return true;
                case EnemyShooterCoverTacticPhase.Peeking:
                    if (Time.time >= _owner._coverPhaseEndsAt || !_owner.HasClearMuzzlePathToTarget())
                    {
                        _owner._coverTacticPhase = EnemyShooterCoverTacticPhase.ReturningToCover;
                        _owner.TrySetAgentDestination(coverDestination, arriveDistance, "Return behind cover");
                        return true;
                    }

                    // Return false only while the peek has a clear shot. TickAttack
                    // then follows the normal reaction, burst, and hit rules.
                    return false;
                case EnemyShooterCoverTacticPhase.ReturningToCover:
                    if (_owner.HasReached(coverDestination, arriveDistance))
                    {
                        _owner._coverTacticPhase = EnemyShooterCoverTacticPhase.Hidden;
                        _owner._coverPhaseEndsAt = Time.time + _owner.EffectiveCoverHideSeconds;
                        _owner.StopAgentHard("Returned behind cover");
                    }
                    else
                    {
                        _owner.TrySetAgentDestination(coverDestination, arriveDistance, "Return behind cover");
                    }

                    return true;
            }

            _owner.ReleaseCoverTactic();
            return false;
        }

        // 진행 중인 좌/우 측면 접근을 갱신합니다. 도착하거나 무효화되면 전술을 해제합니다.
        public bool TryTickActiveCombatFlank()
        {
            if (_owner._tacticalIntent != EnemyShooterTacticalIntent.FlankLeft && _owner._tacticalIntent != EnemyShooterTacticalIntent.FlankRight)
            {
                return false;
            }

            EnemyShooterConfigSO config = _owner.Config;
            if (config == null || !_owner.EffectiveUseFlankingTactics || _owner._tacticalIntentTarget != _owner._target || !_owner.HasTarget)
            {
                _owner.ResetTacticalIntent();
                return false;
            }

            if (_owner.HasReached(_owner._tacticalDestination, config.CoverArriveDistance))
            {
                _owner.ResetTacticalIntent();
                return false;
            }

            if (!_owner.CanUseAgent())
                return true;
            _owner.TrySetAgentDestination(_owner._tacticalDestination, config.CoverArriveDistance, "Combat flank");
            return true;
        }

        // 엄폐 예약과 테스트용 런타임 엄폐 오브젝트를 해제하고 내부 단계를 초기화합니다.
        public void ReleaseCoverTactic()
        {
            if (_owner._reservedCoverPoint != null)
                _owner._reservedCoverPoint.Release(_owner);
            if (_owner._runtimeAutoCoverPoint != null)
            {
                UnityEngine.Object.Destroy(_owner._runtimeAutoCoverPoint.gameObject);
                _owner._runtimeAutoCoverPoint = null;
            }

            _owner._reservedCoverPoint = null;
            _owner._coverTacticPhase = EnemyShooterCoverTacticPhase.None;
            _owner._coverPeekDestination = default;
            _owner._coverPhaseEndsAt = 0f;
        }

        // 엄폐 접근 중 사격하는 옵션이 켜져 있으면 접근 속도 배율을 적용합니다.
        public void ApplyCoverApproachSpeed()
        {
            if (!_owner.EffectiveFireWhileMovingToCover || !_owner.CanUseAgent())
                return;
            _owner._agent.speed = Mathf.Max(0.1f, _owner.ResolveMoveSpeed() * _owner.EffectiveCoverApproachSpeedMultiplier);
        }

        // 전술 목적지/타겟/갱신 시간을 비우고 직접 접근 의도로 되돌립니다.
        public void ResetTacticalIntent()
        {
            _owner._tacticalIntent = EnemyShooterTacticalIntent.Direct;
            _owner._tacticalIntentTarget = null;
            _owner._tacticalDestination = _owner.transform.position;
            _owner._nextTacticalIntentRefreshTime = 0f;
        }
    }
}
