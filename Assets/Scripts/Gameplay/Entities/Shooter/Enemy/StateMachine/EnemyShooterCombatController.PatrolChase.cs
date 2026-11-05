/*
 * [순찰·추적 행동]
 * HFSM의 Patrol/Chase 행동에서 호출하는 시작/갱신 코드를 수정할 때 봅니다.
 * 주요 입구: BeginPatrol -> TickPatrol, BeginChase -> TickChase.
 * 연결: 감지 갱신은 Perception, 엄폐/측면 목적지는 Tactics, 실제 경로 요청은 Navigation.
 * 주의: 각 Tick의 상태이상/긴급 회피/대시 우선 처리를 일반 이동보다 뒤로 옮기지 마세요.
 *
 * 이 파일도 EnemyShooterCombatController의 일부입니다. 별도 컴포넌트가 아닙니다.
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public partial class EnemyShooterCombatController
{
    // ── 외부 호출 입구: 기존 함수 이름을 유지하고 아래 Domain으로 일을 넘깁니다. ──
    private PatrolChaseDomain _patrolChaseDomain;
    private PatrolChaseDomain PatrolChase => _patrolChaseDomain ??= new PatrolChaseDomain(this);
    /// <summary>순찰용 이동/대기 상태를 준비합니다. 새 목적지 선택과 이동은 TickPatrol에서 이어집니다.</summary>
    public void BeginPatrol() => PatrolChase.BeginPatrol();
    /// <summary>긴급 행동을 우선 처리하고, 순찰 대기·목적지 선택·이동·도착을 갱신합니다.</summary>
    public void TickPatrol() => PatrolChase.TickPatrol();
    /// <summary>추적 목표와 이동 설정을 준비합니다. 상태 왕복만으로 끼임 감시 시간을 초기화하지 않습니다.</summary>
    public void BeginChase() => PatrolChase.BeginChase();
    /// <summary>긴급 행동·전술 목적지를 반영해 타겟을 추적하고, 멈춘 경로의 복구를 시도합니다.</summary>
    public void TickChase() => PatrolChase.TickChase();
    // ── 실제 처리 코드: 순찰·추적 행동 ────────────────────────────────────
    // _owner는 별도 적이 아니라 이 기능을 소유한 원래 컨트롤러입니다. 공유 필드는 메인 파일에 있습니다.
    private sealed class PatrolChaseDomain
    {
        private readonly EnemyShooterCombatController _owner;
        public PatrolChaseDomain(EnemyShooterCombatController owner)
        {
            _owner = owner;
        }

        // 순찰용 이동/대기 상태를 준비합니다. 새 목적지 선택과 이동은 TickPatrol에서 이어집니다.
        public void BeginPatrol()
        {
            _owner._attackActive = false;
            _owner._hasPatrolDestination = false;
            _owner._patrolDestinationReached = false;
            _owner._investigationArrived = false;
            _owner._currentGoal = EnemyShooterGoal.Patrol;
            _owner._currentInvestigationType = EnemyShooterInvestigationType.None;
            _owner._nextPatrolRefreshTime = 0f;
            _owner.ConfigureAgent();
            if (_owner.CanUseAgent())
            {
                _owner._agent.speed = _owner.ResolveMoveSpeed();
                _owner._agent.stoppingDistance = 0f;
                _owner._agent.isStopped = false;
                _owner.DebugMovement($"BeginPatrol. speed={_owner._agent.speed}, pos={_owner.transform.position}");
            }
        }

        // 긴급 행동을 우선 처리하고, 순찰 대기·목적지 선택·이동·도착을 갱신합니다.
        public void TickPatrol()
        {
            if (_owner.TryHoldForStatusActionBlock())
                return;
            if (_owner.TryTickIncomingThrowableEvasion())
                return;
            if (_owner.TickDashMovement())
                return;
            if (!_owner.CanUseAgent())
                return;
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null)
            {
                _owner.DebugMovement("TickPatrol skipped because config is missing.");
                return;
            }

            _owner._agent.speed = _owner.ResolveMoveSpeed();
            _owner._agent.stoppingDistance = 0f;
            if (_owner._patrolDestinationReached)
            {
                if (Time.time < _owner._patrolIdleUntil)
                {
                    _owner.StopAgentHard("Patrol idle");
                    return;
                }

                _owner._patrolDestinationReached = false;
                _owner._hasPatrolDestination = false;
            }

            if (_owner._hasPatrolDestination && _owner.HasReached(_owner._patrolDestination, config.PatrolArriveDistance))
            {
                _owner._hasPatrolDestination = false;
                _owner._patrolDestinationReached = true;
                _owner._patrolIdleUntil = Time.time + config.PatrolIdleSeconds;
                _owner.StopAgentHard("Patrol destination reached");
                _owner.DebugMovement($"Patrol destination reached. idleUntil={_owner._patrolIdleUntil}, pos={_owner.transform.position}");
                return;
            }

            if (_owner.TryGetHarmfulAreaAvoidanceDestination(out Vector3 avoidanceDestination))
            {
                _owner._patrolDestination = avoidanceDestination;
                _owner._hasPatrolDestination = true;
                _owner._patrolDestinationReached = false;
                _owner._nextPatrolRefreshTime = Time.time + config.PatrolDestinationRefreshInterval;
                _owner.TrySetAgentDestination(_owner._patrolDestination, 0f, "Patrol harmful-area avoidance");
                return;
            }

            bool currentPathEndedBeforeDestination = _owner._hasPatrolDestination && !_owner._agent.pathPending && !_owner._agent.hasPath && Time.time >= _owner._nextPatrolRefreshTime;
            bool needsDestination = !_owner._hasPatrolDestination || currentPathEndedBeforeDestination;
            if (!needsDestination)
                return;
            if (_owner.TryPickPatrolDestination(out _owner._patrolDestination))
            {
                _owner._hasPatrolDestination = true;
                _owner._patrolDestinationReached = false;
                _owner._nextPatrolRefreshTime = Time.time + config.PatrolDestinationRefreshInterval;
                _owner.TrySetAgentDestination(_owner._patrolDestination, 0f, "Patrol");
            }
            else
            {
                _owner.StopAgentHard("Patrol failed to pick destination");
                _owner.DebugMovement($"Patrol failed to pick destination. home={_owner._homePosition}, radius={config.PatrolRadius}, pos={_owner.transform.position}");
            }
        }

        // 추적 목표와 이동 설정을 준비합니다.
        public void BeginChase()
        {
            _owner._attackActive = false;
            _owner._investigationArrived = false;
            _owner._currentGoal = EnemyShooterGoal.ChaseTarget;
            _owner.ConfigureAgent();
            if (_owner.CanUseAgent())
            {
                _owner._agent.speed = _owner.ResolveMoveSpeed();
                _owner._agent.stoppingDistance = _owner.ResolveChaseStoppingDistance();
                _owner._agent.isStopped = false;
                _owner.DebugMovement($"BeginChaseTarget. speed={_owner._agent.speed}, stopping={_owner._agent.stoppingDistance}, target={(_owner._target != null ? _owner._target.name : "null")}");
            }
        }

        // 긴급 행동·전술 목적지를 반영해 타겟을 추적하고, 멈춘 경로의 복구를 시도합니다.
        public void TickChase()
        {
            if (_owner.TryHoldForStatusActionBlock())
                return;
            if (_owner.TryTickIncomingThrowableEvasion())
                return;
            if (_owner.TickDashMovement())
                return;
            if (!_owner.CanUseAgent())
                return;
            if (_owner.TryGetHarmfulAreaAvoidanceDestination(out Vector3 avoidanceDestination))
            {
                _owner.TrySetAgentDestination(avoidanceDestination, 0f, "Chase harmful-area avoidance");
                return;
            }

            if (!_owner.HasTarget)
            {
                _owner.ReleaseCoverTactic();
                _owner._agent.ResetPath();
                _owner._agent.isStopped = true;
                return;
            }

            if (_owner.Navigation.TryTickRecovery())
                return;
            if (_owner.TryTickCoverTactic() || _owner.TryTickActiveCombatFlank())
                return;
            if (_owner.Config != null && _owner.EffectiveUseCoverTactics && _owner.CanSeeTarget && _owner.GetPlanarDistanceToTarget() <= _owner.ResolveAttackStartRange() && !_owner.HasClearMuzzlePathToTarget())
            {
                if (_owner.TryStartCombatTactics() && (_owner.TryTickCoverTactic() || _owner.TryTickActiveCombatFlank()))
                {
                    return;
                }

                _owner.UpdateAimDirection();
                _owner.TickMuzzleBlockedReposition();
                return;
            }

            if (_owner.TryResolveTacticalChaseDestination(out Vector3 tacticalDestination, out float tacticalStoppingDistance, out string tacticalReason))
            {
                if (_owner._tacticalIntent == EnemyShooterTacticalIntent.HoldLastKnownPosition && _owner.HasReached(tacticalDestination, tacticalStoppingDistance))
                {
                    _owner.StopAgentHard("Tactical hold last-known angle");
                    return;
                }

                if (!_owner.TrySetAgentDestination(tacticalDestination, tacticalStoppingDistance, tacticalReason))
                {
                    _owner.DebugMovement($"Tactical destination update deferred. intent={_owner._tacticalIntent}, " + _owner.BuildAgentDebugInfo());
                }

                return;
            }

            Vector3 rawDestination = (_owner.CanSeeTarget || _owner.IsTargetLocked) && _owner._target != null ? _owner._target.position : _owner._lastKnownPosition;
            bool destinationAccepted = _owner.TrySetAgentDestination(rawDestination, _owner.ResolveChaseStoppingDistance(), "Chase target");
            if (!destinationAccepted)
            {
                // A moving target can invalidate a path request for one frame while the
                // agent still owns a usable previous path. Do not erase that path and
                // leave the shooter staring at the target; the next tick will retry.
                _owner.DebugMovement($"Chase destination update deferred. distance={_owner.GetPlanarDistanceToTarget():0.00}, " + $"attackRange={_owner.AttackStartRange:0.00}, {_owner.BuildAgentDebugInfo()}");
            }

        }
    }
}
