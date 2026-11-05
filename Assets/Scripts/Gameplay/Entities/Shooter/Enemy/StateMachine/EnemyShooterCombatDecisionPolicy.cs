using System;

/// <summary>
/// 공격 선택의 순수 규칙. HFSM 순서/전환은 바꾸지 않으며 무기 전환·발사·난수를 실행하지 않습니다.
/// 테스트에서는 명시적인 시간/관측값을 넣고, 게임에서는 캐릭터별 인스턴스가 관측을 보관합니다.
/// </summary>
public static class EnemyShooterCombatDecisionPolicy
{
    /// <summary>첫 발이 실제 발사된 버스트만 보호합니다. 휴식/발사 실패/긴급 중단은 보호하지 않습니다.</summary>
    public static bool MustFinishBurst(bool attackActive, bool firingBurst, bool hasFired,
        int shotsRemaining, bool targetValid, bool weaponReady, bool withinRange,
        bool routeClear, bool finishAfterSightLost, bool interrupted)
        => attackActive && firingBurst && hasFired && shotsRemaining > 0 && targetValid &&
           weaponReady && withinRange && !interrupted && (routeClear || finishAfterSightLost);
}

/// <summary>
/// 한 캐릭터/한 타겟의 투척 기회. 한 번이라도 직접 목격한 적의 사격 경로가 연속해서 막힌 경우만 인정합니다.
/// 조건 getter는 시간을 누적하지 않습니다. Observe는 기존 인지 갱신 뒤에서만 호출합니다.
/// </summary>
public sealed class EnemyShooterThrowableOpportunity
{
    private int _targetId;
    private bool _hasSeenTarget;
    private bool _hasBlockedRoute;
    private bool _hasObservation;
    private float _lastSeenAt = -1f;
    private float _blockedSince = -1f;
    private float _lastObservationAt = -1f;

    public void Reset()
    {
        _targetId = 0;
        _hasSeenTarget = false;
        _hasBlockedRoute = _hasObservation = false;
        _lastSeenAt = _blockedSince = _lastObservationAt = -1f;
    }

    public void Observe(int targetId, bool visible, bool muzzlePathClear, bool interrupted, float now)
    {
        if (targetId != _targetId || (_hasObservation && now < _lastObservationAt))
            Reset();
        if (targetId == 0 || interrupted)
        {
            Reset();
            return;
        }

        _targetId = targetId;
        _lastObservationAt = now;
        _hasObservation = true;
        if (visible)
        {
            _hasSeenTarget = true;
            _lastSeenAt = now;
        }

        // 열린 탄도는 즉시 누적 시간을 지웁니다. 시야각 밖으로 나갔다는 이유만으로 투척하지 않습니다.
        // 짧은 차폐가 여러 번 있었다고 합산하지 않습니다.
        if (!_hasSeenTarget || muzzlePathClear)
            _hasBlockedRoute = false;
        else if (!_hasBlockedRoute)
        {
            _blockedSince = now;
            _hasBlockedRoute = true;
        }
    }

    public bool IsReady(float now, float blockedSeconds, float recentlyKnownSeconds)
        => _hasSeenTarget && _hasBlockedRoute && now >= _lastObservationAt &&
           now - _lastSeenAt <= Math.Max(0f, recentlyKnownSeconds) &&
           now - _blockedSince >= Math.Max(0.1f, blockedSeconds);
}
