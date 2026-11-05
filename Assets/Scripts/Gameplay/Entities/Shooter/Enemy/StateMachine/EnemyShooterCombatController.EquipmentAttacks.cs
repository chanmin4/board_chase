/*
 * [장비 선택·근접·투척]
 * 원거리/근접 슬롯 전환, 근접 공격 범위, 소지 투척물 소비/아군 안전 검사를 수정할 때 봅니다.
 * 주요 입구: Begin/Tick/EndMeleeAttack, Begin/Tick/EndThrowableAttack.
 * 연결: 장비·근접·투척 컴포넌트가 실제 공격을 수행하고, 이 파일은 사용 조건과 진행 시간을 관리합니다.
 * 주의: TryActivate 계열은 무기를 바꿀 수 있고 TryExecute 계열은 실제 공격/소지품 소비를 일으킵니다.
 *
 * 이 파일도 EnemyShooterCombatController의 일부입니다. 별도 컴포넌트가 아닙니다.
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public partial class EnemyShooterCombatController
{
    // ── 외부 호출 입구: 기존 함수 이름을 유지하고 아래 Domain으로 일을 넘깁니다. ──
    private EquipmentAttacksDomain _equipmentAttacksDomain;
    private EquipmentAttacksDomain EquipmentAttacks => _equipmentAttacksDomain ??= new EquipmentAttacksDomain(this);
    /// <summary>현재 장착 원거리 무기와 사용할 수 있는 탄환 조건을 확인합니다.</summary>
    private bool CanUseCurrentRangedWeapon() => EquipmentAttacks.CanUseCurrentRangedWeapon();
    /// <summary>자동 슬롯 선택 설정에 따라 전투용 원거리 무기로 전환을 시도합니다.</summary>
    private bool TryActivateRangedWeaponForCombat() => EquipmentAttacks.TryActivateRangedWeaponForCombat();
    /// <summary>자동 슬롯 선택 설정에 따라 전투용 근접 무기로 전환을 시도합니다.</summary>
    private bool TryActivateMeleeWeaponForCombat() => EquipmentAttacks.TryActivateMeleeWeaponForCombat();
    /// <summary>지정된 전투 슬롯을 장비 컴포넌트에서 사용할 수 있는지 확인합니다.</summary>
    private bool CanActivateConfiguredCombatWeaponSlot(int slotIndex) => EquipmentAttacks.CanActivateConfiguredCombatWeaponSlot(slotIndex);
    /// <summary>무기 전환 제한 시간을 확인하고 지정 슬롯 활성화를 시도합니다.</summary>
    private bool TryActivateConfiguredCombatWeaponSlot(int slotIndex) => EquipmentAttacks.TryActivateConfiguredCombatWeaponSlot(slotIndex);
    /// <summary>설정된 근접 무기의 공격 범위를 가져옵니다.</summary>
    private bool TryGetConfiguredMeleeRange(out float range) => EquipmentAttacks.TryGetConfiguredMeleeRange(out range);
    /// <summary>현재 타겟이 근접 무기로 공격할 거리 안에 있는지 확인합니다.</summary>
    private bool IsTargetInsideConfiguredMeleeRange() => EquipmentAttacks.IsTargetInsideConfiguredMeleeRange();
    /// <summary>근접 행동을 시작하고 실제 공격 시도와 행동 종료 시각을 준비합니다.</summary>
    public void BeginMeleeAttack() => EquipmentAttacks.BeginMeleeAttack();
    /// <summary>상태이상/긴급 회피 등을 먼저 확인한 뒤 진행 중인 근접 행동을 갱신합니다.</summary>
    public void TickMeleeAttack() => EquipmentAttacks.TickMeleeAttack();
    /// <summary>근접 행동의 활성 플래그를 내립니다. 새 공격을 실행하지 않습니다.</summary>
    public void EndMeleeAttack() => EquipmentAttacks.EndMeleeAttack();
    /// <summary>투척 행동을 시작하고 소지 투척물 사용 시도와 행동 종료 시각을 준비합니다.</summary>
    public void BeginThrowableAttack() => EquipmentAttacks.BeginThrowableAttack();
    /// <summary>상태이상/긴급 회피 등을 먼저 확인한 뒤 진행 중인 투척 행동을 갱신합니다.</summary>
    public void TickThrowableAttack() => EquipmentAttacks.TickThrowableAttack();
    /// <summary>투척 행동의 활성 플래그를 내립니다. 새 투척을 실행하지 않습니다.</summary>
    public void EndThrowableAttack() => EquipmentAttacks.EndThrowableAttack();
    /// <summary>타겟·거리·무기·재사용 대기 등 근접 행동 진입 조건을 검사합니다.</summary>
    private bool CanUseMeleeAttack() => EquipmentAttacks.CanUseMeleeAttack();
    /// <summary>근접 공격 컴포넌트에 실제 공격을 요청하고 성공 시 재사용 대기를 적용합니다.</summary>
    private bool TryExecuteMeleeAttack() => EquipmentAttacks.TryExecuteMeleeAttack();
    /// <summary>설정·타겟·소지 투척물·쿨다운·투척 지점의 사용 조건을 검사합니다.</summary>
    private bool CanUseThrowableAttack() => EquipmentAttacks.CanUseThrowableAttack();
    /// <summary>투척물 효과 범위가 아군에게 위험한 위치인지 확인합니다.</summary>
    private bool IsThrowablePointUnsafeForAllies(Vector3 throwTargetPoint, ThrowableUsableItemSO throwable, EnemyShooterConfigSO config) => EquipmentAttacks.IsThrowablePointUnsafeForAllies(throwTargetPoint, throwable, config);
    /// <summary>소지 투척물을 사용해 실제 투척을 시도하고 성공/실패에 따른 다음 시도 시간을 설정합니다.</summary>
    private bool TryExecuteThrowableAttack() => EquipmentAttacks.TryExecuteThrowableAttack();
    /// <summary>현재 추적 가능하거나 최근 확인한 타겟 위치를 사용하고, 없으면 최근 목격 위치로 투척 지점을 구합니다.</summary>
    private bool TryResolveThrowableTargetPoint(out Vector3 point) => EquipmentAttacks.TryResolveThrowableTargetPoint(out point);
    // ── 실제 처리 코드: 장비 선택·근접·투척 ────────────────────────────────────
    // _owner는 별도 적이 아니라 이 기능을 소유한 원래 컨트롤러입니다. 공유 필드는 메인 파일에 있습니다.
    private sealed class EquipmentAttacksDomain
    {
        private readonly EnemyShooterCombatController _owner;
        // 같은 프레임/같은 소지품·목표점의 경로 검사는 재사용합니다. 소모/발사는 실행하지 않습니다.
        private int _throwablePathFrame = -1;
        private ThrowableUsableItemSO _throwablePathItem;
        private Vector3 _throwablePathPoint;
        private Vector3 _throwablePathOrigin;
        private bool _throwablePathSafe;
        public EquipmentAttacksDomain(EnemyShooterCombatController owner)
        {
            _owner = owner;
        }

        // 현재 장착 원거리 무기와 사용할 수 있는 탄환 조건을 확인합니다.
        public bool CanUseCurrentRangedWeapon()
        {
            if (_owner._shoot == null || _owner._shoot.CurrentBullet == null)
                return false;
            if (_owner._equipmentController == null || _owner._equipmentController.CurrentWeapon is not RangedWeaponSO rangedWeapon || !rangedWeapon.IsCompatibleWith(_owner._shoot.CurrentBullet))
            {
                return false;
            }

            // Durability/jam gates belong to the player. Enemy AI still requires a compatible gun and ammo.
            return true;
        }

        // 자동 슬롯 선택 설정에 따라 전투용 원거리 무기로 전환을 시도합니다.
        public bool TryActivateRangedWeaponForCombat()
        {
            if (_owner._equipmentController == null)
                return false;
            if (_owner.UsesConfiguredCombatWeaponSlots)
            {
                int slotIndex = _owner.Config.RangedWeaponSlotIndex;
                if (_owner._equipmentController.GetWeapon(slotIndex)is not RangedWeaponSO)
                    return false;
                if (!_owner.TryActivateConfiguredCombatWeaponSlot(slotIndex))
                    return false;
            }

            return _owner._equipmentController.CurrentWeapon is RangedWeaponSO;
        }

        // 자동 슬롯 선택 설정에 따라 전투용 근접 무기로 전환을 시도합니다.
        public bool TryActivateMeleeWeaponForCombat()
        {
            if (_owner._equipmentController == null)
                return false;
            if (_owner.UsesConfiguredCombatWeaponSlots)
            {
                int slotIndex = _owner.Config.MeleeWeaponSlotIndex;
                if (_owner._equipmentController.GetWeapon(slotIndex)is not MeleeWeaponSO)
                    return false;
                if (!_owner.TryActivateConfiguredCombatWeaponSlot(slotIndex))
                    return false;
            }

            return _owner._equipmentController.CurrentWeapon is MeleeWeaponSO || _owner._equipmentController.CurrentWeapon == null;
        }

        // 지정된 전투 슬롯을 장비 컴포넌트에서 사용할 수 있는지 확인합니다.
        public bool CanActivateConfiguredCombatWeaponSlot(int slotIndex)
        {
            return _owner._equipmentController != null && (_owner._equipmentController.ActiveWeaponSlotIndex == slotIndex || Time.time >= _owner._nextCombatWeaponSwitchTime);
        }

        // 무기 전환 제한 시간을 확인하고 지정 슬롯 활성화를 시도합니다.
        public bool TryActivateConfiguredCombatWeaponSlot(int slotIndex)
        {
            if (_owner._equipmentController == null)
                return false;
            if (_owner._equipmentController.ActiveWeaponSlotIndex == slotIndex)
                return true;
            if (!_owner.CanActivateConfiguredCombatWeaponSlot(slotIndex))
                return false;
            _owner._equipmentController.SetActiveWeaponSlot(slotIndex);
            if (_owner._equipmentController.ActiveWeaponSlotIndex != slotIndex)
                return false;
            _owner._nextCombatWeaponSwitchTime = Time.time + (_owner.Config != null ? _owner.Config.CombatWeaponSwitchCooldownSeconds : 0f);
            return true;
        }

        // 설정된 근접 무기의 공격 범위를 가져옵니다.
        public bool TryGetConfiguredMeleeRange(out float range)
        {
            range = 0f;
            if (_owner._meleeAttack == null || _owner.Config == null)
                return false;
            if (_owner.UsesConfiguredCombatWeaponSlots)
            {
                if (_owner._equipmentController.GetWeapon(_owner.Config.MeleeWeaponSlotIndex)is not MeleeWeaponSO meleeWeapon)
                    return false;
                range = Mathf.Max(0f, meleeWeapon.Range);
                return range > 0f;
            }

            if (!_owner._meleeAttack.CanAttackCurrentWeaponOrUnarmed)
                return false;
            range = Mathf.Max(0f, _owner._meleeAttack.CurrentAttackRange);
            return range > 0f;
        }

        // 현재 타겟이 근접 무기로 공격할 거리 안에 있는지 확인합니다.
        public bool IsTargetInsideConfiguredMeleeRange()
        {
            if (_owner.Config == null || !_owner.Config.UseMeleeAttack || !_owner.HasTarget)
                return false;
            return _owner.TryGetConfiguredMeleeRange(out float range) && _owner.GetPlanarDistanceToTarget() <= range + _owner.Config.MeleeRangePadding;
        }

        // 근접 행동을 시작하고 실제 공격 시도와 행동 종료 시각을 준비합니다.
        public void BeginMeleeAttack()
        {
            if (_owner.TryHoldForStatusActionBlock())
                return;
            if (!_owner.TryActivateMeleeWeaponForCombat())
                return;
            _owner._meleeActionActive = true;
            _owner._meleeActionFinishTime = Time.time + (_owner.Config != null ? _owner.Config.MeleeActionSeconds : 0.35f);
            _owner._attackActive = false;
            _owner._currentGoal = EnemyShooterGoal.AttackTarget;
            _owner.StopAgentHard("BeginMeleeAttack");
            _owner.UpdateAimDirection();
            _owner.TryExecuteMeleeAttack();
        }

        // 상태이상/긴급 회피 등을 먼저 확인한 뒤 진행 중인 근접 행동을 갱신합니다.
        public void TickMeleeAttack()
        {
            if (_owner.TryHoldForStatusActionBlock())
                return;
            if (_owner.TryTickIncomingThrowableEvasion())
                return;
            if (_owner.TickDashMovement())
                return;
            _owner.StopAgentHard("MeleeAttack");
            _owner.UpdateAimDirection();
        }

        // 근접 행동의 활성 플래그를 내립니다. 새 공격을 실행하지 않습니다.
        public void EndMeleeAttack()
        {
            _owner._meleeActionActive = false;
        }

        // 투척 행동을 시작하고 소지 투척물 사용 시도와 행동 종료 시각을 준비합니다.
        public void BeginThrowableAttack()
        {
            if (_owner.TryHoldForStatusActionBlock())
                return;
            _owner._attackActive = false;
            _owner._currentGoal = EnemyShooterGoal.AttackTarget;
            _owner.StopAgentHard("BeginThrowableAttack");
            _owner.UpdateAimDirection();
            _owner._throwableActionActive = _owner.TryExecuteThrowableAttack();
            _owner._throwableActionFinishTime = _owner._throwableActionActive ? Time.time + (_owner.Config != null ? _owner.Config.ThrowableActionSeconds : 0.35f) : Time.time;
        }

        // 상태이상/긴급 회피 등을 먼저 확인한 뒤 진행 중인 투척 행동을 갱신합니다.
        public void TickThrowableAttack()
        {
            if (_owner.TryHoldForStatusActionBlock())
                return;
            if (_owner.TryTickIncomingThrowableEvasion())
                return;
            if (_owner.TickDashMovement())
                return;
            _owner.StopAgentHard("ThrowableAttack");
            _owner.UpdateAimDirection();
        }

        // 투척 행동의 활성 플래그를 내립니다. 새 투척을 실행하지 않습니다.
        public void EndThrowableAttack()
        {
            _owner._throwableActionActive = false;
        }

        // 타겟·거리·무기·재사용 대기 등 근접 행동 진입 조건을 검사합니다.
        public bool CanUseMeleeAttack()
        {
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null || !config.UseMeleeAttack || _owner._meleeAttack == null || !_owner.HasTarget || Time.time < _owner._nextMeleeAllowedTime)
            {
                return false;
            }

            if (!_owner.TryGetConfiguredMeleeRange(out float range))
                return false;
            if (_owner.UsesConfiguredCombatWeaponSlots && !_owner.CanActivateConfiguredCombatWeaponSlot(config.MeleeWeaponSlotIndex))
            {
                return false;
            }

            return _owner.GetPlanarDistanceToTarget() <= range + config.MeleeRangePadding;
        }

        // 근접 공격 컴포넌트에 실제 공격을 요청하고 성공 시 재사용 대기를 적용합니다.
        public bool TryExecuteMeleeAttack()
        {
            EnemyShooterConfigSO config = _owner.Config;
            if (!_owner.CanUseMeleeAttack())
                return false;
            Vector3 origin = _owner.transform.position;
            Vector3 direction = _owner.HasTarget && _owner._target != null ? _owner._target.position - _owner.transform.position : _owner.transform.forward;
            bool attacked = _owner._meleeAttack.TryAttackCurrentWeapon(ShooterFactionUtility.Resolve(_owner), origin, direction);
            if (attacked)
                _owner._nextMeleeAllowedTime = Time.time + (config != null ? config.MeleeAttackCooldownSeconds : 1.2f);
            return attacked;
        }

        // 설정·타겟·소지 투척물·쿨다운·투척 지점의 사용 조건을 검사합니다.
        public bool CanUseThrowableAttack()
        {
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null || !_owner.EffectiveUseThrowableAttack || _owner._throwableActionActive || _owner._throwableAttack == null || _owner._lootInventory == null || !_owner.HasTarget || Time.time < _owner._nextThrowableAllowedTime)
            {
                return false;
            }

            if (!_owner._lootInventory.TryGetFirstCarriedItem(out ThrowableUsableItemSO throwable) || throwable == null)
            {
                return false;
            }

            if (!_owner.TryResolveThrowableTargetPoint(out Vector3 targetPoint))
                return false;
            float targetDistance = EnemyShooterCombatController.GetPlanarDistance(_owner.transform.position, targetPoint);
            if (targetDistance < config.ThrowableMinRange || (config.ThrowableMaxRange > 0f && targetDistance > config.ThrowableMaxRange))
            {
                return false;
            }

            float selfSafetyDistance = Mathf.Max(0f, throwable.ExplosionRadius + config.ThrowableSelfSafetyPadding);
            if (selfSafetyDistance > 0f && EnemyShooterCombatController.GetPlanarDistance(_owner.transform.position, targetPoint) < selfSafetyDistance)
            {
                return false;
            }

            if (_owner.IsThrowablePointUnsafeForAllies(targetPoint, throwable, config))
                return false;
            if (_throwablePathFrame != Time.frameCount || _throwablePathItem != throwable || _throwablePathPoint != targetPoint || _throwablePathOrigin != _owner.transform.position)
            {
                _throwablePathFrame = Time.frameCount;
                _throwablePathItem = throwable;
                _throwablePathPoint = targetPoint;
                _throwablePathOrigin = _owner.transform.position;
                _throwablePathSafe = _owner._throwableAttack.CanThrowEnemyUsableWithSafeArc(throwable, targetPoint, out _);
            }
            return _throwablePathSafe;
        }

        // 투척물 효과 범위가 아군에게 위험한 위치인지 확인합니다.
        public bool IsThrowablePointUnsafeForAllies(Vector3 throwTargetPoint, ThrowableUsableItemSO throwable, EnemyShooterConfigSO config)
        {
            if (throwable == null || config == null)
                return false;
            int mask = config.ThrowableAllySafetyMask.value;
            if (mask == 0)
                mask = LayerMask.GetMask("Enemy");
            if (mask == 0)
                return true; // 안전 검사 대상 자체가 없으면 안전하다고 가정하지 않습니다.
            float radius = Mathf.Max(0f, throwable.ExplosionRadius + config.ThrowableAllySafetyPadding);
            if (radius <= 0.001f)
                return false;
            Collider[] hits = Physics.OverlapSphere(throwTargetPoint, radius, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null)
                    continue;
                Transform hitTransform = hit.transform;
                if (hitTransform == _owner.transform || hitTransform.IsChildOf(_owner.transform))
                    continue;
                if (_owner._damageable != null && hit.GetComponentInParent<Damageable>() == _owner._damageable)
                    continue;
                _owner.DebugMovement($"Throwable blocked by ally safety. ally={hit.name}, target={throwTargetPoint}, radius={radius}");
                return true;
            }

            return false;
        }

        // 소지 투척물을 사용해 실제 투척을 시도하고 성공/실패에 따른 다음 시도 시간을 설정합니다.
        public bool TryExecuteThrowableAttack()
        {
            EnemyShooterConfigSO config = _owner.Config;
            // 상태 진입 사이에 사격각/최근 목격 정보가 바뀔 수 있으므로 실제 소비 직전에도 이유를 확인합니다.
            if (!_owner.HasTacticalThrowableReason || !_owner.CanUseThrowableAttack() || !_owner.TryResolveThrowableTargetPoint(out Vector3 throwTargetPoint))
            {
                return false;
            }

            if (_owner._lootInventory == null || !_owner._lootInventory.TryGetFirstCarriedItem(out ThrowableUsableItemSO throwable))
                return false;
            if (!_owner._throwableAttack.TryThrowEnemyUsableWithSafeArc(throwable, throwTargetPoint, out string throwFailureReason))
            {
                // A blocked arc must not consume a carried throwable or re-enter the
                // Throwable state every frame. The normal state transition can now
                // fall back to chase/reposition while this short retry gate is open.
                _owner._nextThrowableAllowedTime = Time.time + EnemyShooterCombatController.FailedThrowableRetrySeconds;
                _owner.DebugMovement($"Throwable cancelled without consumption. usable={throwable.name}, " + $"target={throwTargetPoint}, retryAt={_owner._nextThrowableAllowedTime:0.000}, " + $"reason={throwFailureReason ?? "unknown"}");
                return false;
            }

            _owner._nextThrowableAllowedTime = Time.time + config.ThrowableCooldownSeconds;
            _owner.DebugMovement($"Throwable thrown. usable={throwable.name}, target={throwTargetPoint}, cooldownUntil={_owner._nextThrowableAllowedTime}");
            return true;
        }

        // 보이는 타겟은 현재 위치, 보이지 않는 타겟은 직접 마지막으로 목격했던 위치만 사용합니다.
        public bool TryResolveThrowableTargetPoint(out Vector3 point)
        {
            point = default;
            EnemyShooterConfigSO config = _owner.Config;
            if (config == null)
                return false;
            if (!_owner.HasTacticalThrowableReason)
                return false;
            if (_owner.CanSeeTarget)
            {
                point = _owner._target.position;
                return true;
            }
            point = _owner._lastVisibleThrowablePoint;
            return true;
        }
    }
}
