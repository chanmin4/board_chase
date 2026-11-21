using UnityEngine;

/// <summary>Demo tuning. Never derives enemy strength from player quest completion.</summary>
public static class CombatBalanceRules
{
    private static readonly int[] Protection = { 0, 30, 37, 44, 51, 59, 67, 76, 85 };
    public static int Penetration(int tier)
    {
        int t=Mathf.Clamp(tier,1,8);
        var values=CombatBalanceRulesSO.Active?.protectionByTier;
        return values!=null&&values.Length>=t?Mathf.Max(0,values[t-1]):Protection[t];
    }
    public static float AmmoDamageAdjustment(BulletTier tier)
    {
        int t = Mathf.Clamp((int)tier, 1, 8);
        var values=CombatBalanceRulesSO.Active?.ammoDamageByTier;
        if(values!=null&&values.Length>=t)return values[t-1];
        return t == 1 ? 0f : -t;
    }
    public static float EnemyHealth(int stage, EnemyShooterRole role, float fallback)
    {
        if (stage < 1 || stage > 4) return fallback;
        var rules=CombatBalanceRulesSO.Active;
        int band=stage==1?0:stage==4?2:1;
        var values=role==EnemyShooterRole.Boss?rules?.bossHealth:
            role==EnemyShooterRole.BossGuard||role==EnemyShooterRole.NamedEnemy?rules?.eliteHealth:rules?.normalHealth;
        if(values!=null&&values.Length>band)return Mathf.Max(1,values[band]);
        if (role == EnemyShooterRole.Boss) return stage == 1 ? 450 : stage == 4 ? 850 : 650;
        if (role == EnemyShooterRole.BossGuard || role == EnemyShooterRole.NamedEnemy) return stage == 1 ? 205 : stage == 4 ? 400 : 295;
        return stage == 1 ? 150 : stage == 4 ? 300 : 220;
    }
    public static float EnemyDamage(int stage, EnemyShooterRole role, float fallback)
    {
        if (stage < 1 || stage > 4) return fallback;
        var rules=CombatBalanceRulesSO.Active;
        int band=stage==1?0:stage==4?2:1;
        var values=role==EnemyShooterRole.Boss?rules?.bossDamage:
            role==EnemyShooterRole.BossGuard||role==EnemyShooterRole.NamedEnemy?rules?.eliteDamage:rules?.normalDamage;
        if(values!=null&&values.Length>band)return Mathf.Max(0,values[band]);
        if (role == EnemyShooterRole.Boss) return stage == 1 ? 32 : stage == 4 ? 52 : 42;
        if (role == EnemyShooterRole.BossGuard || role == EnemyShooterRole.NamedEnemy) return stage == 1 ? 24 : stage == 4 ? 38 : 30;
        return stage == 1 ? 20 : stage == 4 ? 32 : 26;
    }
}
