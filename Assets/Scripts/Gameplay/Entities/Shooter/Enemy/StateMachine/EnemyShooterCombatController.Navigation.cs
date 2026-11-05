/*
 * [NavMesh 이동 공통 처리]
 * 목적지 투영, 경로 요청/재사용, 도착 판정, 추적이 멈췄을 때 복구를 수정할 때 봅니다.
 * 주요 입구: TrySetAgentDestination / StopAgentHard / HasReached.
 * 연결: PatrolChase, Investigation, Tactics, RangedAttack 등이 선택한 목적지를 실제 경로로 바꿉니다.
 * 주의: 요청한 목적지와 부분 경로에서 도달 가능한 지점은 다릅니다. 조사 도착 판정에는 ResolveInvestigationArrivalDestination을 사용합니다.
 *
 * 이 파일도 EnemyShooterCombatController의 일부입니다. 별도 컴포넌트가 아닙니다.
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public partial class EnemyShooterCombatController
{
    private ShooterStaminaRuntime _stamina;
    private void TickStaminaLocomotion()
    {
        if (_stamina == null) return;
        bool chase = _currentGoal == EnemyShooterGoal.ChaseTarget;
        float distance = _target != null ? Vector3.Distance(transform.position, _target.position) : 0f;
        float threshold = _stamina.IsSprinting ? _stamina.EnemySprintStopDistance : _stamina.EnemySprintStartDistance;
        bool moving = CanUseAgent() && !_agent.isStopped && _agent.hasPath && _agent.remainingDistance > _agent.stoppingDistance + 0.25f;
        _stamina.SetSprintRequested(chase && moving && distance > threshold && _stamina.EnemyCanSprint &&
            (_stamina.IsSprinting || _stamina.CanSpend) &&
            !IsActionBlockedByStatusEffect && !IsDashing && !_attackActive &&
            (_shoot == null || !_shoot.IsReloading) && (_meleeAttack == null || !_meleeAttack.IsAttacking));
        if (CanUseAgent())
            _agent.speed = ResolveMoveSpeed() * (_coverTacticPhase == EnemyShooterCoverTacticPhase.MovingToCover
                ? EffectiveCoverApproachSpeedMultiplier : 1f);
    }
    // ── 외부 호출 입구: 기존 함수 이름을 유지하고 아래 Domain으로 일을 넘깁니다. ──
    private NavigationDomain _navigationDomain;
    private NavigationDomain Navigation => _navigationDomain ??= new NavigationDomain(this);
    public string NavigationRecoveryStatus => _navigationDomain?.LastRecoveryStatus ?? "없음";
    public float CombatStalledSeconds => _navigationDomain?.StalledSeconds ?? 0f;
    /// <summary>외부 행동에서 호출하는 이동 정지 입구입니다. 경로/속도 정리는 StopAgentHard가 수행합니다.</summary>
    public void StopMovement() => Navigation.StopMovement();
    /// <summary>최초 위치 주변의 순찰 반경에서 NavMesh에 놓을 수 있는 임의 목적지를 찾습니다.</summary>
    private bool TryPickPatrolDestination(out Vector3 destination) => Navigation.TryPickPatrolDestination(out destination);
    /// <summary>높이를 제외한 XZ 평면 거리로 허용 도착 범위 안인지 확인합니다.</summary>
    private bool HasReached(Vector3 point, float arriveDistance) => Navigation.HasReached(point, arriveDistance);
    /// <summary>타겟까지 XZ 평면 거리를 구합니다. 타겟이 없으면 무한대를 반환합니다.</summary>
    private float GetPlanarDistanceToTarget() => Navigation.GetPlanarDistanceToTarget();
    /// <summary>실시간 스탯을 우선하고, 없으면 Config 또는 현재 Agent의 이동 속도를 사용합니다.</summary>
    private float ResolveMoveSpeed() => Navigation.ResolveMoveSpeed();
    /// <summary>공격 사거리 바로 밖에서 멈추지 않도록 추적 정지 거리를 제한합니다.</summary>
    private float ResolveChaseStoppingDistance() => Navigation.ResolveChaseStoppingDistance();
    /// <summary>Agent가 활성화되어 NavMesh 위에 있는지 확인합니다. 바깥에 있어도 강제 워프하지 않습니다.</summary>
    private bool CanUseAgent() => Navigation.CanUseAgent();
    /// <summary>NavMeshAgent의 위치 갱신/회전/재탐색/제동과 가속도 설정을 맞춥니다.</summary>
    private void ConfigureAgent() => Navigation.ConfigureAgent();
    /// <summary>Agent 경로와 속도를 비우고 이동 요청 캐시도 초기화해 완전히 정지시킵니다.</summary>
    private void StopAgentHard(string reason) => Navigation.StopAgentHard(reason);
    /// <summary>높이를 제외한 유효 방향으로 캐릭터 회전을 맞춥니다.</summary>
    private void FaceDirection(Vector3 direction) => Navigation.FaceDirection(direction);
    /// <summary>목적지를 투영하고 기존 요청을 재사용하거나 경로를 계산합니다. 실패해도 사용 가능한 기존 경로가 남으면 true일 수 있습니다.</summary>
    private bool TrySetAgentDestination(Vector3 rawDestination, float stoppingDistance, string reason) => Navigation.TrySetAgentDestination(rawDestination, stoppingDistance, reason);
    /// <summary>Agent가 이동 가능한 상태이고 대기/진행 중인 경로가 있는지 확인합니다.</summary>
    private bool HasUsableNavigationPath() => Navigation.HasUsableNavigationPath();
    /// <summary>부분 경로라면 원래 요청 좌표 대신 마지막 도달 가능 지점을 조사 도착 기준으로 사용합니다.</summary>
    private Vector3 ResolveInvestigationArrivalDestination(Vector3 requestedDestination) => Navigation.ResolveInvestigationArrivalDestination(requestedDestination);
    /// <summary>마지막 경로 요청·도달점·정지 거리·재시도 시각 캐시를 비웁니다.</summary>
    private void ClearNavigationRequest() => Navigation.ClearNavigationRequest();
    /// <summary>우선 캐릭터 높이에서 목적지를 투영하고, 실패하면 원래 높이를 포함해 탐색 범위를 넓힙니다.</summary>
    private bool TryProjectToNavMesh(Vector3 rawPosition, out Vector3 navPosition) => Navigation.TryProjectToNavMesh(rawPosition, out navPosition);
    /// <summary>NavMeshAgent의 활성/경로/속도/목적지 상태를 디버그 문자열로 묶습니다.</summary>
    private string BuildAgentDebugInfo() => Navigation.BuildAgentDebugInfo();
    // ── 실제 처리 코드: NavMesh 이동 공통 처리 ────────────────────────────────────
    // _owner는 별도 적이 아니라 이 기능을 소유한 원래 컨트롤러입니다. 공유 필드는 메인 파일에 있습니다.
    private sealed class NavigationDomain
    {
        private readonly EnemyShooterCombatController _owner;
        private readonly NavMeshPath _recoveryPath = new();
        private readonly RaycastHit[] _recoveryHits = new RaycastHit[24];
        private bool _hasProgressSample;
        private Vector3 _progressPosition;
        private float _lastProgressTime;
        private bool _recoveryActive;
        private Vector3 _recoveryDestination;
        private float _recoveryUntil;
        private float _nextRecoveryTime;
        private float _suppressTacticsUntil;
        private int _recoveryAttempts;
        private CharacterController _body;
        private Transform _entityRoot;
        public string LastRecoveryStatus { get; private set; } = "없음";
        public float StalledSeconds => _hasProgressSample ? Mathf.Max(0f, Time.time - _lastProgressTime) : 0f;
        public bool SuppressCombatTactics => _recoveryActive || Time.time < _suppressTacticsUntil;
        public NavigationDomain(EnemyShooterCombatController owner)
        {
            _owner = owner;
        }

        public void ResetRecovery()
        {
            _hasProgressSample = false;
            _recoveryActive = false;
            _nextRecoveryTime = 0f;
            _suppressTacticsUntil = 0f;
            _recoveryAttempts = 0;
            LastRecoveryStatus = "없음";
        }

        // 엄폐/측면 이동의 조기 return도 빠뜨리지 않도록 HFSM Tick 밖에서 감시합니다.
        // Agent.velocity는 계획된 속도입니다. 몸이 벽에 막힌 경우 실제 진행의 증거가 아닙니다.
        public void TickProgressWatch()
        {
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null || !config.UseCombatStuckRecovery || !_owner.IsEngaged ||
                (_owner._damageable != null && _owner._damageable.IsDead) ||
                _owner.IsActionBlockedByStatusEffect || _owner.IsDashing || _owner._isEvadingIncomingThrowable ||
                _owner._meleeActionActive || _owner._throwableActionActive || !_owner.CanUseAgent())
            {
                _hasProgressSample = false;
                _recoveryActive = false;
                return;
            }

            NavMeshAgent agent = _owner._agent;
            bool blockedVisibleChase = _owner.CurrentGoal == EnemyShooterGoal.ChaseTarget &&
                _owner.CanSeeTarget && !_owner._observedCanAttackTarget;
            // 정지 반경 도착/경로 실패로 hasPath가 사라져도, 보이는 타겟에 공격하지
            // 못하는 추적은 감시합니다. 정상 사격 대기나 엄폐 휴식은 감시하지 않습니다.
            if (agent.pathPending || _owner.ResolveMoveSpeed() <= 0f ||
                (!blockedVisibleChase && (agent.isStopped || !_owner._hasNavigationRequest || !agent.hasPath)))
            {
                _hasProgressSample = false;
                return;
            }

            // 부분 경로의 끝/의도적인 도착은 끼임이 아닙니다. 단, 보이는 타겟에 사격을
            // 못 하는 추적은 정지 반경 안에서도 다시 판정해야 합니다.
            if (!blockedVisibleChase && _owner.HasReached(_owner._lastReachableDestination,
                    Mathf.Max(0.1f, agent.stoppingDistance)))
            {
                _hasProgressSample = false;
                return;
            }

            Vector3 position = agent.transform.position;
            float minimumProgress = config.CombatStuckMinimumProgress;
            if (!_hasProgressSample || PlanarSqrDistance(position, _progressPosition) >= minimumProgress * minimumProgress)
            {
                _hasProgressSample = true;
                _progressPosition = position;
                _lastProgressTime = Time.time;
                return;
            }

            if (Time.time - _lastProgressTime < config.CombatStuckSeconds || Time.time < _nextRecoveryTime)
                return;

            Vector3 originalDestination = _owner._hasNavigationRequest ? _owner._lastNavigationRequest :
                _owner._target != null ? _owner._target.position : _owner._lastKnownPosition;
            _lastProgressTime = Time.time;
            _nextRecoveryTime = Time.time + config.CombatStuckSeconds;
            _recoveryAttempts++;
            _owner._muzzleClearanceFrame = -1;
            bool canShoot = _owner.CanDirectlyAttackTarget;
            bool meleeRange = _owner.IsTargetInsideConfiguredMeleeRange();
            _owner.ReleaseCoverTactic();
            _owner.ResetTacticalIntent();
            EnemyShooterTacticalIntentSelector.ReleaseDuelHolder(_owner);
            _suppressTacticsUntil = Time.time + config.CombatStuckStepSeconds + config.CombatStuckSeconds;
            _owner.RefreshGoal(); // 기존 조건으로 재평가합니다. HFSM 상태나 공격 허용을 강제하지 않습니다.
            string attackReason = _owner.ObservedAttackDecisionReason;

            if (canShoot || meleeRange)
            {
                _owner.StopAgentHard("Stalled movement: combat re-evaluated");
                RecordRecovery(canShoot ? "직접 사격 조건 확보 · 공격 재판정" : "근접 범위 · 근접 행동 재판정", attackReason);
                return;
            }

            _recoveryActive = false;
            if (TryFindRecoveryStep(originalDestination, config, out Vector3 destination))
            {
                _owner._agent.ResetPath();
                _owner.ClearNavigationRequest();
                if (_owner.TrySetAgentDestination(destination, 0.05f, "Combat stuck sidestep"))
                {
                    _recoveryDestination = destination;
                    _recoveryUntil = Time.time + config.CombatStuckStepSeconds;
                    _recoveryActive = true;
                    RecordRecovery($"짧은 우회 이동 → {destination}", attackReason);
                    return;
                }
            }

            // 안전한 옆자리가 없으면 기존 목표로만 재탐색합니다. 워프/충돌 해제는 하지 않습니다.
            _owner._agent.ResetPath();
            _owner.ClearNavigationRequest();
            _owner.TrySetAgentDestination(originalDestination, 0f, "Combat stuck repath");
            RecordRecovery("안전한 우회점 없음 · 기존 목표 경로 재탐색", attackReason);
        }

        public bool TryTickRecovery()
        {
            if (!_recoveryActive)
                return false;
            if (_owner.Config == null || !_owner.Config.UseCombatStuckRecovery || !_owner.HasTarget ||
                Time.time >= _recoveryUntil || _owner.HasReached(_recoveryDestination, 0.1f))
            {
                _recoveryActive = false;
                return false;
            }
            if (_owner.CanDirectlyAttackTarget)
            {
                _owner.StopAgentHard("Recovery opened direct fire path");
                return false;
            }
            if (_owner.TryGetHarmfulAreaAvoidanceDestination(out Vector3 avoidanceDestination))
            {
                _recoveryActive = false;
                _owner.TrySetAgentDestination(avoidanceDestination, 0f, "Recovery harmful-area avoidance");
                return true;
            }
            _owner.UpdateAimDirection();
            _owner.TrySetAgentDestination(_recoveryDestination, 0.05f, "Combat stuck sidestep");
            return true;
        }

        private bool TryFindRecoveryStep(Vector3 originalDestination, EnemyShooterConfigSO config, out Vector3 destination)
        {
            NavMeshAgent agent = _owner._agent;
            destination = agent.transform.position;
            if (_body == null) _body = agent.GetComponent<CharacterController>();
            if (_entityRoot == null) _entityRoot = EntityComponentResolver.ResolveEntityRoot(_owner);
            Vector3 forward = originalDestination - destination;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = _owner.transform.forward;
            forward.Normalize();
            float distance = Mathf.Max(config.CombatStuckStepDistance, agent.radius * 2.5f);
            int side = ((_owner.GetInstanceID() + _recoveryAttempts) & 1) == 0 ? 1 : -1;
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            for (int pass = 0; pass < 2; pass++)
            {
                float step = distance * (pass == 0 ? 0.5f : 1f);
                for (int i = 0; i < 7; i++)
                {
                    float angle = i < 2 ? 90f * (i == 0 ? side : -side) :
                        i < 4 ? 135f * (i == 2 ? side : -side) :
                        i == 4 ? 180f : 45f * (i == 5 ? side : -side);
                    Vector3 raw = agent.transform.position + Quaternion.AngleAxis(angle, Vector3.up) * forward * step;
                    if (!NavMesh.SamplePosition(raw, out NavMeshHit hit, Mathf.Min(0.5f, step * 0.25f), filter) ||
                        PlanarSqrDistance(hit.position, agent.transform.position) < 0.0625f ||
                        agent.Raycast(hit.position, out _) || !IsRecoveryStepClear(hit.position))
                        continue;
                    _recoveryPath.ClearCorners();
                    if (!agent.CalculatePath(hit.position, _recoveryPath) || _recoveryPath.status != NavMeshPathStatus.PathComplete)
                        continue;
                    destination = hit.position;
                    return true;
                }
            }
            return false;
        }

        private bool IsRecoveryStepClear(Vector3 destination)
        {
            if (_body == null || !_body.enabled)
                return false;
            if (!ShooterBodyCollisionMotor.CanPlaceControllerAt(_body, destination))
                return false;
            Bounds bounds = _body.bounds;
            float radius = Mathf.Max(0.05f, Mathf.Max(bounds.extents.x, bounds.extents.z) - 0.01f);
            float halfSegment = Mathf.Max(0f, bounds.extents.y - radius);
            Vector3 delta = destination - _owner._agent.transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance < 0.05f) return false;
            int count = Physics.CapsuleCastNonAlloc(bounds.center + Vector3.up * halfSegment,
                bounds.center - Vector3.up * halfSegment, radius, delta / distance, _recoveryHits,
                distance, ~0, QueryTriggerInteraction.Ignore);
            if (count == _recoveryHits.Length) return false; // 결과가 잘렸다면 안전한 이동으로 취급하지 않습니다.
            for (int i = 0; i < count; i++)
            {
                Collider blocker = _recoveryHits[i].collider;
                if (blocker == null || (_entityRoot != null &&
                    (blocker.transform == _entityRoot || blocker.transform.IsChildOf(_entityRoot))) ||
                    Physics.GetIgnoreLayerCollision(_body.gameObject.layer, blocker.gameObject.layer) ||
                    Physics.GetIgnoreCollision(_body, blocker) || _recoveryHits[i].normal.y > 0.7f)
                    continue;
                return false;
            }
            return true;
        }

        private void RecordRecovery(string result, string attackReason)
        {
            LastRecoveryStatus = $"{Time.time:0.0}s / {_recoveryAttempts}회 / {result} / 공격: {attackReason}";
            _owner.DebugMovement($"Combat stuck recovery. {LastRecoveryStatus}, {_owner.BuildAgentDebugInfo()}");
        }

        // 외부 행동에서 호출하는 이동 정지 입구입니다. 경로/속도 정리는 StopAgentHard가 수행합니다.
        public void StopMovement()
        {
            _owner.StopAgentHard("StopMovement");
        }

        // 최초 위치 주변의 순찰 반경에서 NavMesh에 놓을 수 있는 임의 목적지를 찾습니다.
        public bool TryPickPatrolDestination(out Vector3 destination)
        {
            destination = _owner.transform.position;
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null)
                return false;
            float radius = Mathf.Max(0f, config.PatrolRadius);
            if (radius <= 0f)
                return false;
            for (int i = 0; i < 8; i++)
            {
                Vector2 random = Random.insideUnitCircle * radius;
                Vector3 candidate = _owner._homePosition + new Vector3(random.x, 0f, random.y);
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, Mathf.Max(0.1f, config.NavMeshSampleDistance), NavMesh.AllAreas))
                {
                    destination = hit.position;
                    return true;
                }
            }

            return false;
        }

        // 높이를 제외한 XZ 평면 거리로 허용 도착 범위 안인지 확인합니다.
        public bool HasReached(Vector3 point, float arriveDistance)
        {
            Vector3 from = _owner.transform.position;
            from.y = 0f;
            point.y = 0f;
            return Vector3.Distance(from, point) <= Mathf.Max(0f, arriveDistance);
        }

        // 타겟까지 XZ 평면 거리를 구합니다. 타겟이 없으면 무한대를 반환합니다.
        public float GetPlanarDistanceToTarget()
        {
            if (_owner._target == null)
                return float.PositiveInfinity;
            Vector3 from = _owner.transform.position;
            Vector3 to = _owner._target.position;
            from.y = 0f;
            to.y = 0f;
            return Vector3.Distance(from, to);
        }

        // 실시간 스탯을 우선하고, 없으면 Config 또는 현재 Agent의 이동 속도를 사용합니다.
        public float ResolveMoveSpeed()
        {
            if (_owner._statsRuntime != null && _owner._statsRuntime.MoveSpeed > 0f)
                return _owner._statsRuntime.MoveSpeed * (_owner._stamina != null ? _owner._stamina.SpeedMultiplier : 1f);
            EnemyShooterConfigSO config = _owner.Config;
            return config != null ? config.MoveSpeed : (_owner._agent != null ? _owner._agent.speed : 0f);
        }

        // 공격 사거리 바로 밖에서 멈추지 않도록 추적 정지 거리를 제한합니다.
        public float ResolveChaseStoppingDistance()
        {
            EnemyShooterConfigSO config = _owner.Config;
            float configuredDistance = config != null ? config.ChaseStoppingDistance : 0f;
            float attackRange = _owner.AttackStartRange;
            if (attackRange <= 0f)
                return Mathf.Max(0f, configuredDistance);
            // Always enter the attack range by a small margin. This prevents a
            // NavMesh stopping radius from holding the shooter just outside the
            // range comparison due to different target/agent centers.
            float maximumStoppingDistance = Mathf.Max(0f, attackRange - 0.5f);
            return Mathf.Min(Mathf.Max(0f, configuredDistance), maximumStoppingDistance);
        }

        // Agent가 활성화되어 NavMesh 위에 있는지 확인합니다. 바깥에 있어도 강제 워프하지 않습니다.
        public bool CanUseAgent()
        {
            if (_owner._agent == null || !_owner._agent.isActiveAndEnabled)
                return false;
            if (_owner._agent.isOnNavMesh)
                return true;
            _owner.DebugMovement($"NavMeshAgent is off NavMesh. Movement skipped without runtime warp. pos={_owner.transform.position}, {_owner.BuildAgentDebugInfo()}");
            return false;
        }

        // NavMeshAgent의 위치 갱신/회전/재탐색/제동과 가속도 설정을 맞춥니다.
        public void ConfigureAgent()
        {
            if (_owner._agent == null)
                return;
            // Navigation alone does not resolve contacts with player/corpse
            // colliders. Commit each planned step through the solid-body motor.
            if (Application.isPlaying) ShooterBodyCollisionMotor.Ensure(_owner._agent);
            else _owner._agent.updatePosition = true;
            _owner._agent.updateRotation = false;
            _owner._agent.autoRepath = true;
            _owner._agent.autoBraking = true;
            EnemyShooterConfigSO config = _owner.Config;
            if (config != null)
                _owner._agent.acceleration = config.NavMeshAcceleration;
        }

        // Agent 경로와 속도를 비우고 이동 요청 캐시도 초기화해 완전히 정지시킵니다.
        public void StopAgentHard(string reason)
        {
            _recoveryActive = false;
            // 의도적인 정지는 LateUpdate의 감시 조건에서 제외합니다. 공격↔추적 왕복이나
            // 총구 재배치 실패의 반복 정지가 실제 무진행 시간을 지우지는 않게 합니다.
            if (!_owner.CanUseAgent())
                return;
            _owner._agent.isStopped = true;
            _owner._agent.ResetPath();
            _owner._agent.velocity = Vector3.zero;
            _owner._agent.nextPosition = _owner.transform.position;
            _owner.ClearNavigationRequest();
            _owner.DebugMovement($"Agent hard stopped. reason={reason}, pos={_owner.transform.position}, {_owner.BuildAgentDebugInfo()}");
        }

        // 높이를 제외한 유효 방향으로 캐릭터 회전을 맞춥니다.
        public void FaceDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return;
            _owner.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        // 목적지를 투영하고 기존 요청을 재사용하거나 경로를 계산합니다. 실패해도 사용 가능한 기존 경로가 남으면 true일 수 있습니다.
        public bool TrySetAgentDestination(Vector3 rawDestination, float stoppingDistance, string reason)
        {
            if (!_owner.CanUseAgent())
                return false;
            if (!_owner.TryProjectToNavMesh(rawDestination, out Vector3 navDestination))
            {
                _owner.DebugMovement($"{reason} failed. Destination cannot project to NavMesh. raw={rawDestination}, {_owner.BuildAgentDebugInfo()}");
                return false;
            }

            float resolvedStoppingDistance = Mathf.Max(0f, stoppingDistance);
            bool sameRequest = _owner._hasNavigationRequest && EnemyShooterCombatController.PlanarSqrDistance(_owner._lastNavigationRequest, navDestination) <= 0.01f && Mathf.Abs(_owner._lastNavigationStoppingDistance - resolvedStoppingDistance) <= 0.01f;
            _owner._agent.speed = _owner.ResolveMoveSpeed();
            _owner._agent.stoppingDistance = resolvedStoppingDistance;
            if (sameRequest && (_owner._agent.pathPending || _owner._agent.hasPath) && !_owner._agent.isStopped)
                return true;
            if (sameRequest && Time.time < _owner._nextNavigationRetryTime)
                return _owner.HasUsableNavigationPath();
            _owner._navigationPath.ClearCorners();
            bool calculated = _owner._agent.CalculatePath(navDestination, _owner._navigationPath);
            if (!calculated || _owner._navigationPath.status == NavMeshPathStatus.PathInvalid)
            {
                _owner._nextNavigationRetryTime = Time.time + 0.25f;
                _owner.DebugMovement($"{reason} failed. No valid path. raw={rawDestination}, nav={navDestination}, {_owner.BuildAgentDebugInfo()}");
                return _owner.HasUsableNavigationPath();
            }

            Vector3[] corners = _owner._navigationPath.corners;
            Vector3 reachableDestination = navDestination;
            bool pathIsPartial = _owner._navigationPath.status == NavMeshPathStatus.PathPartial;
            if (pathIsPartial && corners != null && corners.Length > 0)
                reachableDestination = corners[corners.Length - 1];
            _owner._agent.isStopped = false;
            bool accepted = _owner._agent.SetPath(_owner._navigationPath);
            if (!accepted)
            {
                _owner._nextNavigationRetryTime = Time.time + 0.25f;
                _owner.DebugMovement($"{reason} SetPath returned false. raw={rawDestination}, nav={navDestination}, pathStatus={_owner._navigationPath.status}, {_owner.BuildAgentDebugInfo()}");
                return _owner.HasUsableNavigationPath();
            }

            _owner._hasNavigationRequest = true;
            _owner._lastNavigationRequest = navDestination;
            _owner._lastReachableDestination = reachableDestination;
            _owner._lastNavigationStoppingDistance = resolvedStoppingDistance;
            _owner._nextNavigationRetryTime = Time.time + 0.25f;
            _owner.DebugMovement($"{reason} path set. raw={rawDestination}, nav={navDestination}, reachable={reachableDestination}, " + $"pathStatus={_owner._navigationPath.status}, speed={_owner._agent.speed}, stopping={_owner._agent.stoppingDistance}, {_owner.BuildAgentDebugInfo()}");
            return true;
        }

        // Agent가 이동 가능한 상태이고 대기/진행 중인 경로가 있는지 확인합니다.
        public bool HasUsableNavigationPath()
        {
            return _owner._agent != null && _owner._agent.isActiveAndEnabled && _owner._agent.isOnNavMesh && !_owner._agent.isStopped && (_owner._agent.pathPending || _owner._agent.hasPath);
        }

        // 부분 경로라면 원래 요청 좌표 대신 마지막 도달 가능 지점을 조사 도착 기준으로 사용합니다.
        public Vector3 ResolveInvestigationArrivalDestination(Vector3 requestedDestination)
        {
            if (!_owner._hasNavigationRequest || EnemyShooterCombatController.PlanarSqrDistance(_owner._lastNavigationRequest, requestedDestination) > 0.01f)
            {
                return requestedDestination;
            }

            return _owner._lastReachableDestination;
        }

        // 마지막 경로 요청·도달점·정지 거리·재시도 시각 캐시를 비웁니다.
        public void ClearNavigationRequest()
        {
            _owner._hasNavigationRequest = false;
            _owner._lastNavigationRequest = default;
            _owner._lastReachableDestination = default;
            _owner._lastNavigationStoppingDistance = 0f;
            _owner._nextNavigationRetryTime = 0f;
        }

        // 우선 캐릭터 높이에서 목적지를 투영하고, 실패하면 원래 높이를 포함해 탐색 범위를 넓힙니다.
        public bool TryProjectToNavMesh(Vector3 rawPosition, out Vector3 navPosition)
        {
            EnemyShooterConfigSO config = _owner.Config;
            float sampleDistance = config != null ? config.NavMeshSampleDistance : 3f;
            sampleDistance = Mathf.Max(0.5f, sampleDistance);
            Vector3 planarPosition = rawPosition;
            planarPosition.y = _owner.transform.position.y;
            if (NavMesh.SamplePosition(planarPosition, out NavMeshHit planarHit, sampleDistance, NavMesh.AllAreas))
            {
                navPosition = planarHit.position;
                return true;
            }

            float verticalDistance = Mathf.Abs(rawPosition.y - _owner.transform.position.y);
            float expandedDistance = Mathf.Max(sampleDistance, verticalDistance + sampleDistance);
            if (NavMesh.SamplePosition(rawPosition, out NavMeshHit rawHit, expandedDistance, NavMesh.AllAreas))
            {
                navPosition = rawHit.position;
                return true;
            }

            navPosition = rawPosition;
            return false;
        }

        // NavMeshAgent의 활성/경로/속도/목적지 상태를 디버그 문자열로 묶습니다.
        public string BuildAgentDebugInfo()
        {
            if (_owner._agent == null)
                return "agent=null";
            return $"agentEnabled={_owner._agent.isActiveAndEnabled}, " + $"onNavMesh={_owner._agent.isOnNavMesh}, " + $"isStopped={_owner._agent.isStopped}, " + $"hasPath={_owner._agent.hasPath}, " + $"pathPending={_owner._agent.pathPending}, " + $"pathStatus={_owner._agent.pathStatus}, " + $"remaining={_owner._agent.remainingDistance}, " + $"velocity={_owner._agent.velocity}, " + $"desiredVelocity={_owner._agent.desiredVelocity}, " + $"destination={_owner._agent.destination}";
        }
    }
}
