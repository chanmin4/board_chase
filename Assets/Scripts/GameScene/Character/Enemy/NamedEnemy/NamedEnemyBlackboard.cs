using UnityEngine;

public enum NamedEnemyAttackType
{
    None,
    Bite,
    Charge,
    Projectile,
    PoisonPuddle
}

public class NamedEnemyBlackboard : MonoBehaviour
{
    [Header("Lifecycle")]
    public bool spawnInitialized;
    public bool introFinished;

    [Header("Combat")]
    public bool canEnterPattern = true;
    public bool shouldStopChase;
    public bool attackFinished = true;
    public NamedEnemyAttackType selectedAttack = NamedEnemyAttackType.None;

    public bool HasSelectedAttack => selectedAttack != NamedEnemyAttackType.None;

    public void ClearSelectedAttack()
    {
        selectedAttack = NamedEnemyAttackType.None;
    }
}
