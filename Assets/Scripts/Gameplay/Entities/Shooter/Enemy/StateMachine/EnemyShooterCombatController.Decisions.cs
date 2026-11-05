/*
 * [공격 선택 시점·투척 전술 이유]
 * 공유 규칙/검사 순서는 그대로 두고, 현재 캐릭터가 언제 다른 공격을 선택할 수 있는지 관리합니다.
 * 사격 완료 = 탄창 전체가 아니라 이미 첫 발을 쏜 현재 버스트 완료입니다.
 * 투척 이유 = 직접 목격한 타겟의 사격 경로가 일정 시간 연속해서 막힘. 선제 투척/매 프레임 확률은 없습니다.
 * SO에는 설정만, 아래 인스턴스에는 타겟별 관측만 저장합니다. 실제 발사/장비 소비는 하지 않습니다.
 */
using UnityEngine;

public partial class EnemyShooterCombatController
{
    private readonly EnemyShooterThrowableOpportunity _throwableOpportunity = new();
    private Vector3 _lastVisibleThrowablePoint;

    private bool HasUnfinishedRangedBurst => _attackActive &&
        _attackPhase == EnemyShooterAttackPhase.FiringBurst && _hasFiredCurrentBurst && _burstShotsRemaining > 0;

    /// <summary>무기를 바꾸지 않고 현재 버스트를 계속할 수 있는지 판정합니다. 근접 범위에 들어와도 남은 버스트는 마칩니다.</summary>
    private bool MustFinishCurrentBurst
    {
        get
        {
            if (!HasUnfinishedRangedBurst)
                return false;
            EnemyShooterConfigSO config = Config;
            bool interrupted = config == null || (_damageable != null && _damageable.IsDead) ||
                IsActionBlockedByStatusEffect || _isEvadingIncomingThrowable || IsDashing;
            if (interrupted || !HasTarget || !CanUseCurrentRangedWeapon() || _shoot.IsReloading || _shoot.MagazineAmmo <= 0)
                return false;
            return EnemyShooterCombatDecisionPolicy.MustFinishBurst(_attackActive,
                _attackPhase == EnemyShooterAttackPhase.FiringBurst, _hasFiredCurrentBurst, _burstShotsRemaining,
                HasTarget, true, GetPlanarDistanceToTarget() <= AttackKeepRange,
                CanSeeTarget && HasClearMuzzlePathToTarget(), config.FinishStartedBurstAfterSightLost, false);
        }
    }

    /// <summary>일반 근접·투척으로 변경할 수 있는 시점입니다. 사망/상태이상/긴급 회피를 기다리게 하는 잠금은 아닙니다.</summary>
    public bool CanChangeCombatAction => !MustFinishCurrentBurst;

    // 기존 TickAwareness의 갱신 횟수/순서를 바꾸지 않고 그 결과만 관측합니다.
    private void ObserveThrowableOpportunity()
    {
        if (!EffectiveUseThrowableAttack)
        {
            ResetThrowableOpportunity();
            return;
        }
        bool interrupted = (_damageable != null && _damageable.IsDead) ||
            IsActionBlockedByStatusEffect || _isEvadingIncomingThrowable || IsDashing;
        bool visible = CanSeeTarget;
        if (visible)
            _lastVisibleThrowablePoint = _target.position;
        _throwableOpportunity.Observe(HasTarget ? _target.GetInstanceID() : 0, visible,
            HasTarget && HasClearMuzzlePathToTarget(), interrupted, Time.time);
    }

    private void ResetThrowableOpportunity()
    {
        _throwableOpportunity.Reset();
        _lastVisibleThrowablePoint = default;
    }

    // 직접 사격각이 다시 열리면 관측 갱신 전이라도 투척 금지. 거리/총 고장만으로 투척 이유를 만들지 않습니다.
    private bool HasTacticalThrowableReason => Config != null && HasTarget &&
        _throwableOpportunity.IsReady(Time.time, Config.ThrowableBlockedFireSeconds, Config.ThrowableRecentlyKnownSeconds) &&
        !HasClearMuzzlePathToTarget();
}
