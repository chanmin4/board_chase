/*
 * [소리·마지막 목격 위치 조사]
 * 조사 위치로 이동하고 도착 후 기다리는 시간, 단서 교체/만료/조사 종료를 수정할 때 봅니다.
 * 주요 입구: BeginInvestigate -> TickInvestigate -> TryCompleteFinishedInvestigation.
 * 연결: Hearing이 소리 단서를 등록하고, Perception이 목격 위치를 등록하며, Navigation이 이동을 수행합니다.
 * 주의: TryCompleteFinishedInvestigation은 완료 여부만 읽지 않고 단서·대기·다음 목표를 바꿉니다.
 *
 * 이 파일도 EnemyShooterCombatController의 일부입니다. 별도 컴포넌트가 아닙니다.
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public partial class EnemyShooterCombatController
{
    // ── 외부 호출 입구: 기존 함수 이름을 유지하고 아래 Domain으로 일을 넘깁니다. ──
    private InvestigationDomain _investigationDomain;
    private InvestigationDomain Investigation => _investigationDomain ??= new InvestigationDomain(this);
    /// <summary>공격을 내려놓고 조사 종류/도착 대기 상태를 준비합니다. 소리 반응 지연 중에는 이동하지 않습니다.</summary>
    public void BeginInvestigate() => Investigation.BeginInvestigate();
    /// <summary>긴급 행동 -> 위험 지역 회피 -> 단서 확인 -> 반응 대기 -> 이동/도착 후 대기 순서로 조사합니다.</summary>
    public void TickInvestigate() => Investigation.TickInvestigate();
    /// <summary>단서를 더 찾기 위해 제자리에서 기다리는 종료 시각을 설정합니다. 이미 기다리면 다시 시작하지 않습니다.</summary>
    private void BeginInvestigationSearchWait(string reason) => Investigation.BeginInvestigationSearchWait(reason);
    /// <summary>대기가 끝났으면 예약된 소리를 먼저 처리하고, 없으면 단서를 지우고 조사 재진입 제한/다음 목표를 설정합니다.</summary>
    public bool TryCompleteFinishedInvestigation() => Investigation.TryCompleteFinishedInvestigation();
    /// <summary>현재 소리 조사와 예약된 소리 정보를 비웁니다.</summary>
    private void ClearSoundInvestigation() => Investigation.ClearSoundInvestigation();
    /// <summary>마지막 목격 단서를 비우고 필요하면 반응 시간과 경계 상태를 정리합니다.</summary>
    private void ClearLastKnownInvestigation() => Investigation.ClearLastKnownInvestigation();
    /// <summary>소리 조사와 마지막 목격 조사 단서를 함께 정리합니다.</summary>
    private void ClearInvestigation() => Investigation.ClearInvestigation();
    /// <summary>조사 재진입 제한 시간을 해제해 새 조사에 반응할 수 있게 합니다.</summary>
    private void ClearInvestigationReentryCooldown() => Investigation.ClearInvestigationReentryCooldown();
    /// <summary>다음 조사로 대기 중인 소리의 위치·시간·발신자·우선순위를 비웁니다.</summary>
    private void ClearQueuedSoundInvestigation() => Investigation.ClearQueuedSoundInvestigation();
    /// <summary>마지막으로 알게 된 타겟 위치와 그 기록 시각을 저장합니다.</summary>
    private void SetLastKnownPosition(Vector3 position) => Investigation.SetLastKnownPosition(position);
    /// <summary>첫 발견인지 추적 중 재발견인지에 맞춰 사격 전 반응 지연을 시작합니다.</summary>
    private void BeginDirectSightReaction(bool isPursuitReacquisition) => Investigation.BeginDirectSightReaction(isPursuitReacquisition);
    /// <summary>진행 중인 발견/재발견 반응 지연을 해제합니다.</summary>
    private void ClearActiveReaction() => Investigation.ClearActiveReaction();
    /// <summary>소리 위치·이동 시작 시각·우선순위를 현재 조사로 등록하고 재목표 설정 시간을 갱신합니다.</summary>
    private void SetSoundInvestigation(Vector3 position, float moveTime, float setTime, SoundStimulusType stimulusType, int priority, int emitterId, Vector3 direction, string reason) => Investigation.SetSoundInvestigation(position, moveTime, setTime, stimulusType, priority, emitterId, direction, reason);
    /// <summary>현재 조사를 즉시 바꾸지 않고 다음에 처리할 소리를 예약합니다. 더 낮은 우선순위는 거절합니다.</summary>
    private bool QueueSoundInvestigation(Vector3 position, float moveTime, float setTime, SoundStimulusType stimulusType, int priority, int emitterId, Vector3 direction, string reason) => Investigation.QueueSoundInvestigation(position, moveTime, setTime, stimulusType, priority, emitterId, direction, reason);
    /// <summary>예약된 소리를 현재 조사로 승격하고 이동/대기 상태를 새 조사에 맞게 되돌립니다.</summary>
    private bool TryPromoteQueuedSoundInvestigation() => Investigation.TryPromoteQueuedSoundInvestigation();
    /// <summary>공격 -> 추적 -> 조사 -> 순찰 순서로 현재 행동 목표를 선택합니다. HFSM 전환을 직접 실행하지는 않습니다.</summary>
    private void RefreshGoal() => Investigation.RefreshGoal();
    /// <summary>직접 추적 가능한 타겟이 있으면 조사를 미루고, 남은 소리/목격 단서로 조사 종류를 고릅니다. 만료된 소리는 정리할 수 있습니다.</summary>
    private EnemyShooterInvestigationType ResolveInvestigationType() => Investigation.ResolveInvestigationType();
    /// <summary>현재 조사 종류에 해당하는 소리 위치 또는 마지막 목격 위치를 가져옵니다.</summary>
    private bool TryResolveInvestigationDestination(out Vector3 destination) => Investigation.TryResolveInvestigationDestination(out destination);
    /// <summary>타겟이 설정된 추적 포기 거리 밖으로 나갔는지 판정합니다.</summary>
    private bool IsTargetBeyondChaseLoseRange(EnemyShooterConfigSO config) => Investigation.IsTargetBeyondChaseLoseRange(config);
    /// <summary>조사 목적지에 도착했다고 볼 허용 거리를 설정에서 구합니다.</summary>
    private float ResolveInvestigateArriveDistance() => Investigation.ResolveInvestigateArriveDistance();
    /// <summary>조사 지점 도착 후 주변을 살피며 기다릴 시간을 설정에서 구합니다.</summary>
    private float ResolveInvestigationWaitSeconds(EnemyShooterConfigSO config) => Investigation.ResolveInvestigationWaitSeconds(config);
    // ── 실제 처리 코드: 소리·마지막 목격 위치 조사 ────────────────────────────────────
    // _owner는 별도 적이 아니라 이 기능을 소유한 원래 컨트롤러입니다. 공유 필드는 메인 파일에 있습니다.
    private sealed class InvestigationDomain
    {
        private readonly EnemyShooterCombatController _owner;
        public InvestigationDomain(EnemyShooterCombatController owner)
        {
            _owner = owner;
        }

        // 공격을 내려놓고 조사 종류/도착 대기 상태를 준비합니다. 소리 반응 지연 중에는 이동하지 않습니다.
        public void BeginInvestigate()
        {
            _owner._attackActive = false;
            _owner._investigationArrived = false;
            _owner._investigationFinishTime = 0f;
            _owner._currentGoal = EnemyShooterGoal.Investigate;
            _owner._currentInvestigationType = _owner.ResolveInvestigationType();
            _owner.ConfigureAgent();
            if (_owner.CanUseAgent())
            {
                _owner._agent.speed = _owner.ResolveMoveSpeed();
                _owner._agent.stoppingDistance = _owner.ResolveInvestigateArriveDistance();
                if (_owner._currentInvestigationType == EnemyShooterInvestigationType.Sound && Time.time < _owner._soundInvestigationMoveTime)
                {
                    _owner.StopAgentHard("Begin sound reaction delay");
                    _owner.FaceDirection(_owner._soundInvestigationPosition - _owner.transform.position);
                }
                else
                {
                    _owner._agent.isStopped = false;
                }

                _owner.DebugMovement($"BeginInvestigate. type={_owner._currentInvestigationType}, pos={_owner.transform.position}");
            }
            else
            {
                _owner.DebugMovement($"BeginInvestigate skipped because agent is unavailable. {_owner.BuildAgentDebugInfo()}");
            }
        }

        // 긴급 행동 -> 위험 지역 회피 -> 단서 확인 -> 반응 대기 -> 이동/도착 후 대기 순서로 조사합니다.
        public void TickInvestigate()
        {
            if (_owner.TryHoldForStatusActionBlock())
                return;
            if (_owner.TryTickIncomingThrowableEvasion())
                return;
            if (_owner.TickDashMovement())
                return;
            if (!_owner.CanUseAgent())
            {
                _owner.DebugMovement($"TickInvestigate skipped because agent is unavailable. {_owner.BuildAgentDebugInfo()}");
                return;
            }

            if (_owner.TryGetHarmfulAreaAvoidanceDestination(out Vector3 avoidanceDestination))
            {
                _owner.TrySetAgentDestination(avoidanceDestination, 0f, "Investigate harmful-area avoidance");
                return;
            }

            _owner._currentInvestigationType = _owner.ResolveInvestigationType();
            if (_owner._currentInvestigationType == EnemyShooterInvestigationType.None || !_owner.TryResolveInvestigationDestination(out Vector3 destination))
            {
                _owner.DebugMovement($"Investigation lost clue. waiting before patrol. type={_owner._currentInvestigationType}, hasSound={_owner._hasSoundInvestigation}, hasLastKnown={_owner._hasLastKnownPosition}");
                _owner.ClearInvestigation();
                _owner.StopAgentHard("Investigation lost clue");
                _owner.BeginInvestigationSearchWait("No investigation destination");
                return;
            }

            if (_owner._currentInvestigationType == EnemyShooterInvestigationType.Sound && Time.time < _owner._soundInvestigationMoveTime)
            {
                _owner.StopAgentHard("Waiting for sound reaction delay");
                _owner.FaceDirection(destination - _owner.transform.position);
                return;
            }

            float arriveDistance = _owner.ResolveInvestigateArriveDistance();
            if (_owner.HasReached(_owner.ResolveInvestigationArrivalDestination(destination), arriveDistance))
            {
                if (!_owner._investigationArrived)
                {
                    EnemyShooterConfigSO config = _owner.Config;
                    _owner._investigationArrived = true;
                    _owner._investigationFinishTime = Time.time + _owner.ResolveInvestigationWaitSeconds(config);
                    _owner.StopAgentHard("Investigation arrived");
                    _owner.DebugMovement($"Investigation arrived. type={_owner._currentInvestigationType}, finishAt={_owner._investigationFinishTime}");
                }

                if (_owner.InvestigationFinished)
                    _owner.TryCompleteFinishedInvestigation();
                return;
            }

            _owner._investigationArrived = false;
            _owner.TrySetAgentDestination(destination, arriveDistance, $"Investigate {_owner._currentInvestigationType}");
        }

        // 단서를 더 찾기 위해 제자리에서 기다리는 종료 시각을 설정합니다. 이미 기다리면 다시 시작하지 않습니다.
        public void BeginInvestigationSearchWait(string reason)
        {
            if (_owner._investigationArrived)
                return;
            EnemyShooterConfigSO config = _owner.Config;
            _owner._investigationArrived = true;
            _owner._investigationFinishTime = Time.time + _owner.ResolveInvestigationWaitSeconds(config);
            _owner.DebugMovement($"Investigation search wait started. reason={reason}, finishAt={_owner._investigationFinishTime}, searchSeconds={(config != null ? config.InvestigateSearchSeconds : 0f)}");
        }

        // 대기가 끝났으면 예약된 소리를 먼저 처리하고, 없으면 단서를 지우고 조사 재진입 제한/다음 목표를 설정합니다.
        public bool TryCompleteFinishedInvestigation()
        {
            if (!_owner.InvestigationFinished)
                return false;
            if (_owner.TryPromoteQueuedSoundInvestigation())
                return false;
            _owner.DebugMovement($"Investigation finished. clearing. type={_owner._currentInvestigationType}, hasSound={_owner._hasSoundInvestigation}, hasLastKnown={_owner._hasLastKnownPosition}");
            _owner.ClearInvestigation();
            _owner._investigationArrived = false;
            _owner._investigationFinishTime = 0f;
            EnemyShooterConfigSO config = _owner.Config;
            _owner._ignoreInvestigationUntilTime = Time.time + (config != null ? config.InvestigationReenterCooldown : 0f);
            if (_owner.CanUseAgent())
                _owner.StopAgentHard("Investigation finished");
            _owner.RefreshGoal();
            return true;
        }

        // 현재 소리 조사와 예약된 소리 정보를 비웁니다.
        public void ClearSoundInvestigation()
        {
            _owner._hasSoundInvestigation = false;
            _owner._soundInvestigationPosition = Vector3.zero;
            _owner._soundInvestigationMoveTime = 0f;
            _owner._soundInvestigationSetTime = -1f;
            _owner._soundInvestigationStimulusType = SoundStimulusType.Unknown;
            _owner._soundInvestigationPriority = 0;
            _owner._soundInvestigationEmitterId = 0;
            _owner._soundInvestigationDirection = Vector3.zero;
            _owner.ClearQueuedSoundInvestigation();
            if (!_owner.HasTarget)
            {
                _owner._isAlerted = _owner._hasLastKnownPosition;
            }
        }

        // 마지막 목격 단서를 비우고 필요하면 반응 시간과 경계 상태를 정리합니다.
        public void ClearLastKnownInvestigation()
        {
            _owner._hasLastKnownPosition = false;
            _owner._lastKnownPosition = Vector3.zero;
            _owner._lastKnownSetTime = -1f;
            if (!_owner.HasTarget)
            {
                _owner.ClearActiveReaction();
                _owner._hasSeenTargetDuringCurrentEngagement = false;
            }

            if (!_owner.HasTarget && !_owner._hasSoundInvestigation)
                _owner._isAlerted = false;
        }

        // 소리 조사와 마지막 목격 조사 단서를 함께 정리합니다.
        public void ClearInvestigation()
        {
            _owner.ClearSoundInvestigation();
            _owner.ClearLastKnownInvestigation();
        }

        // 조사 재진입 제한 시간을 해제해 새 조사에 반응할 수 있게 합니다.
        public void ClearInvestigationReentryCooldown()
        {
            _owner._ignoreInvestigationUntilTime = -1f;
        }

        // 다음 조사로 대기 중인 소리의 위치·시간·발신자·우선순위를 비웁니다.
        public void ClearQueuedSoundInvestigation()
        {
            _owner._hasQueuedSoundInvestigation = false;
            _owner._queuedSoundInvestigationPosition = Vector3.zero;
            _owner._queuedSoundInvestigationMoveTime = 0f;
            _owner._queuedSoundInvestigationSetTime = -1f;
            _owner._queuedSoundInvestigationStimulusType = SoundStimulusType.Unknown;
            _owner._queuedSoundInvestigationPriority = 0;
            _owner._queuedSoundInvestigationEmitterId = 0;
            _owner._queuedSoundInvestigationDirection = Vector3.zero;
        }

        // 마지막으로 알게 된 타겟 위치와 그 기록 시각을 저장합니다.
        public void SetLastKnownPosition(Vector3 position)
        {
            _owner._lastKnownPosition = position;
            _owner._hasLastKnownPosition = true;
            _owner._lastKnownSetTime = Time.time;
        }

        // 첫 발견인지 추적 중 재발견인지에 맞춰 사격 전 반응 지연을 시작합니다.
        public void BeginDirectSightReaction(bool isPursuitReacquisition)
        {
            EnemyShooterConfigSO config = _owner.Config;
            _owner._reactionStartedTime = Time.time;
            _owner._activeReactionDelaySeconds = config == null ? 0f : isPursuitReacquisition ? config.PursuitReacquireReactionTime : config.PlayerDiscoveryReactionTime;
            if(_owner._isAlerted||isPursuitReacquisition)_owner._activeReactionDelaySeconds*=config != null ? config.AlertReactionMultiplier : 1f;
            _owner._hasSeenTargetDuringCurrentEngagement = true;
        }

        // 진행 중인 발견/재발견 반응 지연을 해제합니다.
        public void ClearActiveReaction()
        {
            _owner._reactionStartedTime = -1f;
            _owner._activeReactionDelaySeconds = 0f;
        }

        // 소리 위치·이동 시작 시각·우선순위를 현재 조사로 등록하고 재목표 설정 시간을 갱신합니다.
        public void SetSoundInvestigation(Vector3 position, float moveTime, float setTime, SoundStimulusType stimulusType, int priority, int emitterId, Vector3 direction, string reason)
        {
            _owner._hasSoundInvestigation = true;
            _owner._soundInvestigationPosition = position;
            _owner._soundInvestigationMoveTime = moveTime;
            _owner._soundInvestigationSetTime = setTime;
            _owner._soundInvestigationStimulusType = stimulusType;
            _owner._soundInvestigationPriority = priority;
            _owner._soundInvestigationEmitterId = emitterId;
            _owner._soundInvestigationDirection = direction;
            _owner._nextInvestigationRetargetTime = Time.time + (_owner.Config != null ? _owner.Config.InvestigateRetargetCooldown : 0f);
            if (_owner._currentGoal == EnemyShooterGoal.Investigate)
            {
                _owner._investigationArrived = false;
                _owner._investigationFinishTime = 0f;
            }

            _owner.DebugMovement($"Sound investigation set. reason={reason}, type={stimulusType}, priority={priority}, " + $"emitterId={emitterId}, nav={position}, readyTime={moveTime}, retargetAfter={_owner._nextInvestigationRetargetTime}");
        }

        // 현재 조사를 즉시 바꾸지 않고 다음에 처리할 소리를 예약합니다. 더 낮은 우선순위는 거절합니다.
        public bool QueueSoundInvestigation(Vector3 position, float moveTime, float setTime, SoundStimulusType stimulusType, int priority, int emitterId, Vector3 direction, string reason)
        {
            if (_owner._hasQueuedSoundInvestigation && priority < _owner._queuedSoundInvestigationPriority)
            {
                return false;
            }

            _owner._hasQueuedSoundInvestigation = true;
            _owner._queuedSoundInvestigationPosition = position;
            _owner._queuedSoundInvestigationMoveTime = moveTime;
            _owner._queuedSoundInvestigationSetTime = setTime;
            _owner._queuedSoundInvestigationStimulusType = stimulusType;
            _owner._queuedSoundInvestigationPriority = priority;
            _owner._queuedSoundInvestigationEmitterId = emitterId;
            _owner._queuedSoundInvestigationDirection = direction;
            _owner.DebugMovement($"Sound investigation queued. reason={reason}, type={stimulusType}, priority={priority}, " + $"emitterId={emitterId}, nav={position}, readyTime={moveTime}");
            return true;
        }

        // 예약된 소리를 현재 조사로 승격하고 이동/대기 상태를 새 조사에 맞게 되돌립니다.
        public bool TryPromoteQueuedSoundInvestigation()
        {
            if (!_owner._hasQueuedSoundInvestigation)
                return false;
            _owner.SetSoundInvestigation(_owner._queuedSoundInvestigationPosition, _owner._queuedSoundInvestigationMoveTime, _owner._queuedSoundInvestigationSetTime, _owner._queuedSoundInvestigationStimulusType, _owner._queuedSoundInvestigationPriority, _owner._queuedSoundInvestigationEmitterId, _owner._queuedSoundInvestigationDirection, "Queued sound after investigation wait");
            _owner.ClearQueuedSoundInvestigation();
            _owner._investigationArrived = false;
            _owner._investigationFinishTime = 0f;
            _owner._currentInvestigationType = EnemyShooterInvestigationType.Sound;
            _owner._currentGoal = EnemyShooterGoal.Investigate;
            if (_owner.CanUseAgent())
                _owner._agent.isStopped = false;
            return true;
        }

        // 공격 -> 추적 -> 조사 -> 순찰 순서로 현재 행동 목표를 선택합니다. HFSM 전환을 직접 실행하지는 않습니다.
        public void RefreshGoal()
        {
            EnemyShooterGoal previousGoal = _owner._currentGoal;
            _owner._currentInvestigationType = _owner.ResolveInvestigationType();
            if (_owner.CanAttackTarget)
            {
                _owner._currentGoal = EnemyShooterGoal.AttackTarget;
            }
            else if (_owner.ShouldChaseTarget)
            {
                _owner._currentGoal = EnemyShooterGoal.ChaseTarget;
            }
            else if (_owner._currentInvestigationType != EnemyShooterInvestigationType.None)
            {
                _owner._currentGoal = EnemyShooterGoal.Investigate;
            }
            else
            {
                _owner._currentGoal = EnemyShooterGoal.Patrol;
            }

            if (_owner._debugMovement && previousGoal != _owner._currentGoal)
            {
                Debug.Log($"[EnemyShooterCombatController] Goal changed. {previousGoal} -> {_owner._currentGoal}, " + $"hasTarget={_owner.HasTarget}, canSee={_owner.CanSeeTarget}, targetLocked={_owner.IsTargetLocked}, " + $"canAttack={_owner.CanAttackTarget}, investigation={_owner._currentInvestigationType}, " + $"hasSound={_owner._hasSoundInvestigation}, hasLastKnown={_owner._hasLastKnownPosition}, " + $"forgotten={_owner.HasForgottenTarget()}", _owner);
            }
        }

        // 직접 추적 가능한 타겟이 있으면 조사를 미루고, 남은 소리/목격 단서로 조사 종류를 고릅니다. 만료된 소리는 정리할 수 있습니다.
        public EnemyShooterInvestigationType ResolveInvestigationType()
        {
            // While the target is visible or still inside the guaranteed lock window,
            // combat tracking wins. A stale target reference alone must not suppress
            // sound or last-known-position investigation.
            if (_owner.HasTarget && (_owner.CanSeeTarget || _owner.IsTargetLocked))
                return EnemyShooterInvestigationType.None;
            EnemyShooterConfigSO config = _owner.Config;
            if (_owner._hasSoundInvestigation)
            {
                float readyTime = _owner._soundInvestigationMoveTime;
                float memorySeconds = config != null ? config.SoundInvestigationMemorySeconds : 0f;
                bool expired = memorySeconds > 0f && Time.time > readyTime + memorySeconds;
                // Hearing and looking happen immediately. The ready time delays only
                // NavMesh movement, so the FSM and the '?' icon can react at once.
                if (!expired)
                    return EnemyShooterInvestigationType.Sound;
                if (expired)
                    _owner.ClearSoundInvestigation();
            }

            if (_owner._hasLastKnownPosition)
            {
                float memorySeconds = config != null ? config.LastKnownInvestigationSeconds : 0f;
                bool expired = memorySeconds > 0f && _owner._lastKnownSetTime >= 0f && Time.time > _owner._lastKnownSetTime + memorySeconds;
                if (!expired)
                    return EnemyShooterInvestigationType.LastKnownTarget;
                _owner.ClearLastKnownInvestigation();
            }

            return EnemyShooterInvestigationType.None;
        }

        // 현재 조사 종류에 해당하는 소리 위치 또는 마지막 목격 위치를 가져옵니다.
        public bool TryResolveInvestigationDestination(out Vector3 destination)
        {
            destination = _owner.transform.position;
            switch (_owner.ResolveInvestigationType())
            {
                case EnemyShooterInvestigationType.Sound:
                    destination = _owner._soundInvestigationPosition;
                    return true;
                case EnemyShooterInvestigationType.LastKnownTarget:
                    destination = _owner._lastKnownPosition;
                    return true;
                default:
                    return false;
            }
        }

        // 타겟이 설정된 추적 포기 거리 밖으로 나갔는지 판정합니다.
        public bool IsTargetBeyondChaseLoseRange(EnemyShooterConfigSO config)
        {
            if (config == null || config.ChaseLoseRange <= 0f || _owner._target == null)
                return false;
            Vector3 from = _owner.transform.position;
            Vector3 to = _owner._target.position;
            from.y = 0f;
            to.y = 0f;
            return Vector3.Distance(from, to) > config.ChaseLoseRange;
        }

        // 조사 목적지에 도착했다고 볼 허용 거리를 설정에서 구합니다.
        public float ResolveInvestigateArriveDistance()
        {
            EnemyShooterConfigSO config = _owner.Config;
            return config != null ? config.InvestigateArriveDistance : 0.5f;
        }

        // 조사 지점 도착 후 주변을 살피며 기다릴 시간을 설정에서 구합니다.
        public float ResolveInvestigationWaitSeconds(EnemyShooterConfigSO config)
        {
            if (config == null)
                return 0f;
            return Mathf.Max(config.InvestigateSearchSeconds, config.InvestigateMinimumCommitSeconds);
        }
    }
}
