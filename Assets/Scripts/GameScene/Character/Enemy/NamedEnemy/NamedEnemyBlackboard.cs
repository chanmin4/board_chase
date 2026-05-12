using UnityEngine;

public enum NamedEnemyAttackType
{
    None,
    Charge,
    Projectile,
    PoisonPuddle
}

public class NamedEnemyBlackboard : MonoBehaviour
{
    [Header("Runtime Lifecycle")]
    public bool spawnInitialized;
    public bool introFinished;

    [Header("Runtime Combat")]
    public bool canEnterPattern = true;
    public bool shouldStopChase;
    public bool attackFinished = true;
    public NamedEnemyAttackType selectedAttack = NamedEnemyAttackType.None;
    public float nextNormalAttackTime;

    public bool HasSelectedAttack => selectedAttack != NamedEnemyAttackType.None;
    public bool IsNormalAttackCooldownReady => Time.time >= nextNormalAttackTime;

    public void BeginNormalAttackWindow()
    {
        selectedAttack = NamedEnemyAttackType.None;
        attackFinished = false;
    }
    public void SelectNormalAttack(NamedEnemyAttackType attackType)
    {
        selectedAttack = attackType;
        attackFinished = false;
    }

    public void FinishNormalAttack(float cooldown)
    {
        selectedAttack = NamedEnemyAttackType.None;
        attackFinished = true;
        nextNormalAttackTime = Time.time + Mathf.Max(0f, cooldown);
    }

    public void ClearSelectedAttack()
    {
        selectedAttack = NamedEnemyAttackType.None;
    }
}
