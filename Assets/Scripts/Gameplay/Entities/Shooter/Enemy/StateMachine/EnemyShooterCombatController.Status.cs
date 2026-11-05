/*
 * [상태이상에 의한 행동 차단]
 * 스턴/섬광 등으로 이동·공격을 멈추고 차단 해제 후 재개하는 규칙을 수정할 때 봅니다.
 * 주요 입구: TryHoldForStatusActionBlock. true면 호출한 행동 Tick은 나머지 처리를 멈춥니다.
 * 연결: 버스트/근접/투척 진행과 대시·NavMesh·이펙트를 정리하고, 해제 시 인지 반응 시간을 다시 적용합니다.
 * 주의: ResolveStatusActionBlocked는 판정, TryHoldForStatusActionBlock는 실제 정지/복구 처리입니다.
 *
 * 이 파일도 EnemyShooterCombatController의 일부입니다. 별도 컴포넌트가 아닙니다.
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public partial class EnemyShooterCombatController
{
    // ── 외부 호출 입구: 기존 함수 이름을 유지하고 아래 Domain으로 일을 넘깁니다. ──
    private StatusDomain _statusDomain;
    private StatusDomain Status => _statusDomain ??= new StatusDomain(this);
    /// <summary>행동 차단 시작 시 공격/대시를 끊고 이동을 정지시킵니다. 해제 시 재발견 반응을 준비하며, 차단 중이면 true입니다.</summary>
    private bool TryHoldForStatusActionBlock() => Status.TryHoldForStatusActionBlock();
    /// <summary>현재 상태이상에 행동 차단이 있는지 검사합니다. 스키마 호환을 위해 stun/flash 디버프 ID도 확인합니다.</summary>
    private bool ResolveStatusActionBlocked() => Status.ResolveStatusActionBlocked();
    // ── 실제 처리 코드: 상태이상에 의한 행동 차단 ────────────────────────────────────
    // _owner는 별도 적이 아니라 이 기능을 소유한 원래 컨트롤러입니다. 공유 필드는 메인 파일에 있습니다.
    private sealed class StatusDomain
    {
        private readonly EnemyShooterCombatController _owner;
        public StatusDomain(EnemyShooterCombatController owner)
        {
            _owner = owner;
        }

        // 행동 차단 시작 시 공격/대시를 끊고 이동을 정지시킵니다. 해제 시 재발견 반응을 준비하며, 차단 중이면 true입니다.
        public bool TryHoldForStatusActionBlock()
        {
            if (!_owner.ResolveStatusActionBlocked())
            {
                if (_owner._wasStatusActionBlocked)
                {
                    _owner._wasStatusActionBlocked = false;
                    // Reacquiring a visible target after a stun still respects the
                    // pursuit reaction delay instead of firing on the release frame.
                    if (_owner.HasTarget && _owner._canSeeTarget)
                        _owner.BeginDirectSightReaction(true);
                    _owner.RefreshGoal();
                }

                return false;
            }

            if (!_owner._wasStatusActionBlocked)
            {
                _owner._wasStatusActionBlocked = true;
                _owner._burstShotsRemaining = 0;
                _owner._nextBurstShotTime = 0f;
                _owner._nextFireTime = 0f;
                _owner.ResetBurstTracking();
                _owner._attackActive = false;
                _owner._attackPhase = EnemyShooterAttackPhase.None;
                _owner.ClearActiveReaction();
                if (_owner._meleeActionActive)
                    _owner._meleeActionFinishTime = Time.time;
                _owner._throwableActionActive = false;
                _owner._throwableActionFinishTime = 0f;
                _owner._dashController?.CancelDash();
                _owner.ClearNavigationRequest();
            }

            _owner.StopAgentHard("Status effect blocks actions");
            _owner._effects?.SetFootTrailActive(false);
            _owner._effects?.StopDashParticles();
            return true;
        }

        // 현재 상태이상에 행동 차단이 있는지 검사합니다. 스키마 호환을 위해 stun/flash 디버프 ID도 확인합니다.
        public bool ResolveStatusActionBlocked()
        {
            if (_owner._statusEffectRuntime == null || !_owner._statusEffectRuntime.HasAny)
                return false;
            if (EnemyShooterCombatController.TryReadBooleanProperty(EnemyShooterCombatController.RuntimeBlocksActionsProperty, _owner._statusEffectRuntime, out bool runtimeBlocksActions) && runtimeBlocksActions)
            {
                return true;
            }

            IReadOnlyList<EntityStatusEffectRuntime.ActiveStatusEffect> activeEffects = _owner._statusEffectRuntime.ActiveEffects;
            for (int i = 0; i < activeEffects.Count; i++)
            {
                StatusEffectDefinitionSO definition = activeEffects[i].Definition;
                if (definition == null)
                    continue;
                if (EnemyShooterCombatController.TryReadBooleanProperty(EnemyShooterCombatController.DefinitionBlocksActionsProperty, definition, out bool definitionBlocksActions) && definitionBlocksActions)
                {
                    return true;
                }

                // Compatibility for the authored flash/stun SO while the generic
                // status schema is being migrated. Beneficial effects never block.
                string id = definition.Id;
                if (definition.IsDebuff && !string.IsNullOrWhiteSpace(id) && (id.IndexOf("stun", System.StringComparison.OrdinalIgnoreCase) >= 0 || id.IndexOf("flash", System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
