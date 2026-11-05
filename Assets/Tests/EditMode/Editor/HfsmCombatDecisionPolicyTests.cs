using NUnit.Framework;

/// <summary>Unity 시간/물리/에셋 없이 실행할 수 있는 공격 선택 정책 회귀 검사.</summary>
public sealed class HfsmCombatDecisionPolicyTests
{
    private static bool Burst(bool active = true, bool firing = true, bool fired = true, int shots = 2,
        bool target = true, bool weapon = true, bool range = true, bool clear = true,
        bool finishLost = false, bool interrupted = false)
        => EnemyShooterCombatDecisionPolicy.MustFinishBurst(active, firing, fired, shots, target, weapon, range, clear, finishLost, interrupted);

    [Test] public void FirstDiscoveryWithClearShotNeverCreatesThrowableOpportunity()
    {
        var p = new EnemyShooterThrowableOpportunity();
        p.Observe(1, true, true, false, 0);
        p.Observe(1, true, true, false, 10);
        Assert.That(p.IsReady(10, 1.5f, 3), Is.False);
    }
    [Test] public void UnknownTargetCannotBeBombedEvenAfterLongObstruction()
    {
        var p = new EnemyShooterThrowableOpportunity();
        p.Observe(1, false, false, false, 0);
        p.Observe(1, false, false, false, 10);
        Assert.That(p.IsReady(10, 1.5f, 3), Is.False);
    }
    [Test] public void LosingVisionWithoutPhysicalObstructionDoesNotJustifyGrenade()
    {
        var p = new EnemyShooterThrowableOpportunity(); p.Observe(1, true, true, false, 0);
        p.Observe(1, false, true, false, 1);
        Assert.That(p.IsReady(2.5f, 1.5f, 3), Is.False);
    }
    [Test] public void BriefSightLossDoesNotSelectThrowable()
    {
        var p = new EnemyShooterThrowableOpportunity();
        p.Observe(1, true, true, false, 0);
        p.Observe(1, false, false, false, 0.1f);
        Assert.That(p.IsReady(1.59f, 1.5f, 3), Is.False);
        Assert.That(p.IsReady(1.6f, 1.5f, 3), Is.True);
    }
    [Test] public void VisibleTargetWithBlockedMuzzleBecomesTacticalOpportunity()
    {
        var p = new EnemyShooterThrowableOpportunity();
        p.Observe(1, true, false, false, 0);
        p.Observe(1, true, false, false, 2);
        Assert.That(p.IsReady(2, 1.5f, 3), Is.True);
    }
    [Test] public void ReopenedShotResetsRatherThanAccumulatesObstructions()
    {
        var p = new EnemyShooterThrowableOpportunity();
        p.Observe(1, true, true, false, 0);
        p.Observe(1, false, false, false, 0.1f);
        p.Observe(1, true, true, false, 1);
        p.Observe(1, false, false, false, 1.1f);
        Assert.That(p.IsReady(2, 1.5f, 3), Is.False);
        Assert.That(p.IsReady(2.7f, 1.5f, 3), Is.True);
    }
    [Test] public void StaleKnowledgeExpiresDespiteTargetLockOrLongWait()
    {
        var p = new EnemyShooterThrowableOpportunity();
        p.Observe(1, true, true, false, 0);
        p.Observe(1, false, false, false, 1);
        Assert.That(p.IsReady(2.5f, 1.5f, 3), Is.True);
        Assert.That(p.IsReady(3.01f, 1.5f, 3), Is.False);
    }
    [Test] public void TargetReplacementDoesNotInheritKnowledgeOrBlockedTime()
    {
        var p = new EnemyShooterThrowableOpportunity();
        p.Observe(1, true, false, false, 0);
        p.Observe(2, false, false, false, 2);
        Assert.That(p.IsReady(2, 1.5f, 3), Is.False);
        p.Observe(2, true, false, false, 3);
        Assert.That(p.IsReady(4, 1.5f, 3), Is.False);
    }
    [Test] public void ResetDisableAndEmergencyClearTheOpportunity()
    {
        var p = new EnemyShooterThrowableOpportunity();
        p.Observe(1, true, false, false, 0);
        Assert.That(p.IsReady(2, 1.5f, 3), Is.True);
        p.Observe(1, true, false, true, 2);
        Assert.That(p.IsReady(2, 1.5f, 3), Is.False);
        p.Observe(1, true, false, false, 3);
        p.Reset();
        Assert.That(p.IsReady(5, 1.5f, 3), Is.False);
    }
    [Test] public void NoTargetAndClockResetDoNotReuseOldEngagement()
    {
        var p = new EnemyShooterThrowableOpportunity();
        p.Observe(1, true, false, false, 10);
        p.Observe(1, false, false, false, 0);
        Assert.That(p.IsReady(2, 1.5f, 3), Is.False);
        p.Observe(1, true, false, false, 3);
        p.Observe(0, false, false, false, 5);
        Assert.That(p.IsReady(5, 1.5f, 3), Is.False);
    }
    [Test] public void CharactersHaveIndependentDecisionTimers()
    {
        var a = new EnemyShooterThrowableOpportunity(); var b = new EnemyShooterThrowableOpportunity();
        a.Observe(1, true, false, false, 0); b.Observe(1, true, false, false, 1);
        Assert.That(a.IsReady(2, 1.5f, 3), Is.True);
        Assert.That(b.IsReady(2, 1.5f, 3), Is.False);
    }
    [Test] public void RepeatedReadsAndSameTimeObservationDoNotAdvanceDecision()
    {
        var p = new EnemyShooterThrowableOpportunity();
        p.Observe(1, true, false, false, 0);
        for (int i = 0; i < 1000; i++)
        {
            p.Observe(1, true, false, false, 0);
            Assert.That(p.IsReady(0, 1.5f, 3), Is.False);
        }
        Assert.That(p.IsReady(1.5f, 1.5f, 3), Is.True);
    }
    [Test] public void ZeroDelayStillCannotProduceInstantGrenadeOpener()
    {
        var p = new EnemyShooterThrowableOpportunity(); p.Observe(1, true, false, false, 0);
        Assert.That(p.IsReady(0, 0, 3), Is.False);
        Assert.That(p.IsReady(0.1f, 0, 3), Is.True);
    }
    [Test] public void StartedBurstIsProtectedButPreparationAndCooldownAreNot()
    {
        Assert.That(Burst(), Is.True);
        Assert.That(Burst(active: false), Is.False);
        Assert.That(Burst(firing: false), Is.False);
        Assert.That(Burst(fired: false), Is.False);
        Assert.That(Burst(shots: 0), Is.False);
    }
    [Test] public void LostTargetUnusableWeaponRangeAndEmergencyReleaseBurst()
    {
        Assert.That(Burst(target: false), Is.False);
        Assert.That(Burst(weapon: false), Is.False);
        Assert.That(Burst(range: false), Is.False);
        Assert.That(Burst(interrupted: true, finishLost: true), Is.False);
    }
    [Test] public void ExistingFinishAfterSightLostPolicyIsRespected()
    {
        Assert.That(Burst(clear: false, finishLost: false), Is.False);
        Assert.That(Burst(clear: false, finishLost: true), Is.True);
    }
}
