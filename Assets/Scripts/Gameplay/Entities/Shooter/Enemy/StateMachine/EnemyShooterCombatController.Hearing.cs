/*
 * [소리 입력·필터·우선순위]
 * 어떤 소리를 듣는지, 중복 소리를 언제 무시하는지, 기존 조사보다 우선할지를 수정할 때 봅니다.
 * 주요 입구: OnSoundStimulus. 메인 파일의 OnEnable/OnDisable에서 이벤트를 구독/해제합니다.
 * 연결: 통과한 소리의 조사 위치·대기 큐 관리는 Investigation으로 넘깁니다.
 * 주의: 중복 방지 키와 조사 기억 시간은 서로 다릅니다. 같은 발신자의 연속 소리를 무조건 새 조사로 만들지 않습니다.
 *
 * 이 파일도 EnemyShooterCombatController의 일부입니다. 별도 컴포넌트가 아닙니다.
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public partial class EnemyShooterCombatController
{
    // ── 외부 호출 입구: 기존 함수 이름을 유지하고 아래 Domain으로 일을 넘깁니다. ──
    private HearingDomain _hearingDomain;
    private HearingDomain Hearing => _hearingDomain ??= new HearingDomain(this);
    /// <summary>소리의 발신자·거리·중복·우선순위를 검사하고 현재 조사로 등록하거나 다음 조사로 예약합니다.</summary>
    private void OnSoundStimulus(SoundStimulus stimulus) => Hearing.OnSoundStimulus(stimulus);
    /// <summary>설정에 따라 같은 Enemy 계열 발신자의 소리를 무시할지 판정합니다.</summary>
    private bool ShouldIgnoreSoundStimulus(SoundStimulus stimulus, EnemyShooterConfigSO config) => Hearing.ShouldIgnoreSoundStimulus(stimulus, config);
    /// <summary>발신자/종류 기반 중복 키를 등록합니다. 제한 시간 안의 동일 자극은 거절합니다.</summary>
    private bool TryRegisterSoundStimulus(SoundStimulus stimulus, EnemyShooterConfigSO config, out float duplicateUntil) => Hearing.TryRegisterSoundStimulus(stimulus, config, out duplicateUntil);
    /// <summary>만료된 소리 중복 키를 제거해 오래된 기록이 계속 쌓이지 않게 합니다.</summary>
    private void CleanupExpiredSoundStimulusKeys() => Hearing.CleanupExpiredSoundStimulusKeys();
    /// <summary>소리 기억 시간이 지난 현재 조사 단서를 정리합니다.</summary>
    private void ExpireSoundInvestigationIfNeeded(EnemyShooterConfigSO config) => Hearing.ExpireSoundInvestigationIfNeeded(config);
    /// <summary>소리 채택/거절 이유와 거리·우선순위 등을 디버그 옵션이 켜져 있을 때 기록합니다.</summary>
    private void DebugSoundStimulus(SoundStimulus stimulus, float distance, int priority, string result, string reason) => Hearing.DebugSoundStimulus(stimulus, distance, priority, result, reason);
    // ── 실제 처리 코드: 소리 입력·필터·우선순위 ────────────────────────────────────
    // _owner는 별도 적이 아니라 이 기능을 소유한 원래 컨트롤러입니다. 공유 필드는 메인 파일에 있습니다.
    private sealed class HearingDomain
    {
        private readonly EnemyShooterCombatController _owner;
        public HearingDomain(EnemyShooterCombatController owner)
        {
            _owner = owner;
        }

        // 소리의 발신자·거리·중복·우선순위를 검사하고 현재 조사로 등록하거나 다음 조사로 예약합니다.
        public void OnSoundStimulus(SoundStimulus stimulus)
        {
            // 1. 지원하는 소리인지, 자기/아군 소리인지, 조사 제한·청취 거리·NavMesh 조건을 통과하는지 확인합니다.
            if (!stimulus.IsValid)
                return;
            EnemyShooterConfigSO config = _owner.Config;
            int priority = EnemyShooterCombatController.ResolveSoundStimulusPriority(stimulus.type);
            Vector3 from = _owner.transform.position;
            Vector3 to = stimulus.position;
            from.y = 0f;
            to.y = 0f;
            float distance = Vector3.Distance(from, to);
            if (priority <= 0)
            {
                _owner.DebugSoundStimulus(stimulus, distance, priority, "Rejected", "unsupported stimulus type");
                return;
            }

            if (_owner.IsOwnSource(stimulus.source))
            {
                _owner.DebugSoundStimulus(stimulus, distance, priority, "Rejected", "own source");
                return;
            }

            if (_owner.ShouldIgnoreSoundStimulus(stimulus, config))
            {
                _owner.DebugSoundStimulus(stimulus, distance, priority, "Rejected", "friendly source");
                return;
            }

            if (stimulus.type != SoundStimulusType.Footstep && Time.time < _owner._ignoreInvestigationUntilTime && !EnemyShooterCombatController.CanBypassInvestigationReentry(priority))
            {
                _owner.DebugSoundStimulus(stimulus, distance, priority, "Rejected", $"investigation reenter cooldown until {_owner._ignoreInvestigationUntilTime:0.00}");
                return;
            }

            if (distance > stimulus.radius)
            {
                _owner.DebugSoundStimulus(stimulus, distance, priority, "Rejected", "outside hearing radius");
                return;
            }

            if (!_owner.TryProjectToNavMesh(stimulus.position, out Vector3 navPosition))
            {
                _owner.DebugSoundStimulus(stimulus, distance, priority, "Rejected", "position cannot project to NavMesh");
                return;
            }

            // 2. 만료 단서를 정리하고 새 소리가 현재 단서를 교체할지, 대기 큐에 들어갈지 계산합니다.
            _owner.ExpireSoundInvestigationIfNeeded(config);
            _owner.ClearInvestigationReentryCooldown();
            float readyTime = Time.time + stimulus.investigateDelaySeconds;
            bool isWaitingAtInvestigationPoint = _owner._currentGoal == EnemyShooterGoal.Investigate && _owner._investigationArrived;
            bool shouldQueue = (isWaitingAtInvestigationPoint && config != null && config.QueueSoundDuringInvestigationWait) || (_owner._currentGoal == EnemyShooterGoal.Investigate && Time.time < _owner._nextInvestigationRetargetTime);
            bool forceReplace = !_owner._hasSoundInvestigation || priority > _owner._soundInvestigationPriority || priority >= EnemyShooterCombatController.ResolveSoundStimulusPriority(SoundStimulusType.Gunshot);
            string result;
            string reason;
            if (_owner._hasSoundInvestigation && priority < _owner._soundInvestigationPriority)
            {
                _owner.DebugSoundStimulus(stimulus, distance, priority, "Rejected", $"lower priority than active {_owner._soundInvestigationStimulusType}({_owner._soundInvestigationPriority})");
                return;
            }

            if (!forceReplace && shouldQueue && _owner._hasQueuedSoundInvestigation && priority < _owner._queuedSoundInvestigationPriority)
            {
                _owner.DebugSoundStimulus(stimulus, distance, priority, "Rejected", $"lower priority than queued {_owner._queuedSoundInvestigationStimulusType}({_owner._queuedSoundInvestigationPriority})");
                return;
            }

            // 3. 우선순위를 통과한 소리만 중복 키에 등록합니다. 거절된 낮은 우선순위 소리가 키를 선점하지 않게 합니다.
            if (!_owner.TryRegisterSoundStimulus(stimulus, config, out float duplicateUntil))
            {
                _owner.DebugSoundStimulus(stimulus, distance, priority, "Rejected", $"duplicate projectile stimulus until {duplicateUntil:0.00}");
                return;
            }

            if (forceReplace)
            {
                string replacementReason = !_owner._hasSoundInvestigation ? "no active sound clue" : priority > _owner._soundInvestigationPriority ? $"higher priority than active {_owner._soundInvestigationStimulusType}({_owner._soundInvestigationPriority})" : "immediate-response sound tier";
                _owner.SetSoundInvestigation(navPosition, readyTime, Time.time, stimulus.type, priority, stimulus.emitterId, stimulus.direction, replacementReason);
                result = "Accepted";
                reason = replacementReason;
            }
            else if (shouldQueue)
            {
                bool queued = _owner.QueueSoundInvestigation(navPosition, readyTime, Time.time, stimulus.type, priority, stimulus.emitterId, stimulus.direction, "investigation wait/retarget cooldown");
                if (!queued)
                {
                    _owner.DebugSoundStimulus(stimulus, distance, priority, "Rejected", $"lower priority than queued {_owner._queuedSoundInvestigationStimulusType}({_owner._queuedSoundInvestigationPriority})");
                    return;
                }

                result = "Queued";
                reason = "investigation wait or retarget cooldown";
            }
            else
            {
                _owner.SetSoundInvestigation(navPosition, readyTime, Time.time, stimulus.type, priority, stimulus.emitterId, stimulus.direction, "same-priority clue retarget");
                result = "Accepted";
                reason = "same-priority clue retargeted";
            }

            // 4. 받아들인 소리는 즉시 경계/시선에 반영합니다. 실제 이동 시작은 readyTime 이후입니다.
            _owner._isAlerted = true;
            _owner.RefreshGoal();
            Vector3 lookDirection = stimulus.HasDirection && (stimulus.type == SoundStimulusType.ProjectileFlyby || stimulus.type == SoundStimulusType.ProjectileImpact || stimulus.type == SoundStimulusType.Impact) ? -stimulus.direction : navPosition - _owner.transform.position;
            _owner.FaceDirection(lookDirection);
            _owner.DebugSoundStimulus(stimulus, distance, priority, result, reason);
        }

        // 설정에 따라 같은 Enemy 계열 발신자의 소리를 무시할지 판정합니다.
        public bool ShouldIgnoreSoundStimulus(SoundStimulus stimulus, EnemyShooterConfigSO config)
        {
            GameObject source = stimulus.source;
            if (source == null)
                return false;
            if (config != null && config.IgnoreEnemySoundStimuli)
            {
                Enemy sourceEnemy = source.GetComponent<Enemy>() ?? source.GetComponentInParent<Enemy>();
                if (sourceEnemy != null)
                {
                    _owner.DebugMovement($"Sound stimulus ignored because source is enemy. type={stimulus.type}, source={source.name}");
                    return true;
                }
            }

            return false;
        }

        // 발신자/종류 기반 중복 키를 등록합니다. 제한 시간 안의 동일 자극은 거절합니다.
        public bool TryRegisterSoundStimulus(SoundStimulus stimulus, EnemyShooterConfigSO config, out float duplicateUntil)
        {
            duplicateUntil = -1f;
            if (!stimulus.HasEmitter)
                return true;
            float cooldown = config != null ? config.SoundStimulusDuplicateCooldown : 0f;
            if (cooldown <= 0f)
                return true;
            _owner.CleanupExpiredSoundStimulusKeys();
            long key = EnemyShooterCombatController.BuildSoundStimulusKey(stimulus.emitterId, stimulus.type);
            if (_owner._soundStimulusDuplicateUntil.TryGetValue(key, out duplicateUntil) && Time.time < duplicateUntil)
            {
                return false;
            }

            duplicateUntil = Time.time + cooldown;
            _owner._soundStimulusDuplicateUntil[key] = duplicateUntil;
            return true;
        }

        // 만료된 소리 중복 키를 제거해 오래된 기록이 계속 쌓이지 않게 합니다.
        public void CleanupExpiredSoundStimulusKeys()
        {
            if (Time.time < _owner._nextSoundStimulusCleanupTime)
                return;
            _owner._nextSoundStimulusCleanupTime = Time.time + 2f;
            _owner._expiredSoundStimulusKeys.Clear();
            foreach (KeyValuePair<long, float> pair in _owner._soundStimulusDuplicateUntil)
            {
                if (Time.time >= pair.Value)
                    _owner._expiredSoundStimulusKeys.Add(pair.Key);
            }

            for (int i = 0; i < _owner._expiredSoundStimulusKeys.Count; i++)
                _owner._soundStimulusDuplicateUntil.Remove(_owner._expiredSoundStimulusKeys[i]);
            _owner._expiredSoundStimulusKeys.Clear();
        }

        // 소리 기억 시간이 지난 현재 조사 단서를 정리합니다.
        public void ExpireSoundInvestigationIfNeeded(EnemyShooterConfigSO config)
        {
            if (!_owner._hasSoundInvestigation)
                return;
            float memorySeconds = config != null ? config.SoundInvestigationMemorySeconds : 0f;
            if (memorySeconds > 0f && Time.time > _owner._soundInvestigationMoveTime + memorySeconds)
            {
                _owner.ClearSoundInvestigation();
            }
        }

        // 소리 채택/거절 이유와 거리·우선순위 등을 디버그 옵션이 켜져 있을 때 기록합니다.
        public void DebugSoundStimulus(SoundStimulus stimulus, float distance, int priority, string result, string reason)
        {
            _owner._lastSoundStimulusDistance = distance;
            _owner._lastSoundStimulusRadius = stimulus.radius;
            _owner._lastSoundStimulusResult = $"{result}: {stimulus.type} ({reason})";
            if (!_owner._debugSoundStimuli)
                return;
            Debug.Log($"[EnemyShooterCombatController][Sound] {result}. " + $"type={stimulus.type}, priority={priority}, distance={distance:0.00}, " + $"radius={stimulus.radius:0.00}, intensity={stimulus.intensity:0.00}, " + $"emitterId={stimulus.emitterId}, source={(stimulus.source != null ? stimulus.source.name : "null")}, " + $"position={stimulus.position}, direction={stimulus.direction}, reason={reason}", _owner);
        }
    }
}
