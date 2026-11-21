using UnityEngine;

[CreateAssetMenu(menuName="Game/Systems/전투 성장 수치",fileName="CombatBalanceRules")]
public sealed class CombatBalanceRulesSO : ScriptableObject
{
    [Min(.01f)] public float penetrationCurve=2.5f;
    [Range(0,1)] public float minimumPenetration=.05f;
    [Range(0,1)] public float maximumPenetration=.95f;
    [Range(0,1)] public float failedHitDamage=.15f;
    public int[] protectionByTier={30,37,44,51,59,67,76,85};
    public float[] ammoDamageByTier={0,-2,-3,-4,-5,-6,-7,-8};
    public float[] normalHealth={150,220,300};
    public float[] eliteHealth={205,295,400};
    public float[] bossHealth={450,650,850};
    public float[] normalDamage={20,26,32};
    public float[] eliteDamage={24,30,38};
    public float[] bossDamage={32,42,52};
    public static CombatBalanceRulesSO Active=>Resources.Load<CombatBalanceRulesSO>("CombatBalanceRules");
}
