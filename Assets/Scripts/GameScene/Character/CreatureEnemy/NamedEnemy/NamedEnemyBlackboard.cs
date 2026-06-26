using UnityEngine;

public class NamedEnemyBlackboard : MonoBehaviour
{
    [Header("Runtime Lifecycle")]
    public bool spawnInitialized;
    public bool introFinished;

    [Header("Runtime Combat")]
    public bool canEnterPattern = true;
    public bool shouldStopChase;
    public bool attackFinished = true;

    [Header("Runtime Selected Attack")]
    [SerializeField, ReadOnly] private EnemyAttackConfigSO _selectedAttack;

    public EnemyAttackConfigSO SelectedAttack => _selectedAttack;
    public bool HasSelectedAttack => _selectedAttack != null;

    public void SelectAttack(EnemyAttackConfigSO attackConfig)
    {
        if (attackConfig == null)
        {
            ClearSelectedAttack();
            attackFinished = true;
            return;
        }

        _selectedAttack = attackConfig;
        attackFinished = false;
    }

    public void FinishSelectedAttack(bool clearSelectedAttack = true)
    {
        attackFinished = true;

        if (clearSelectedAttack)
            ClearSelectedAttack();
    }

    public void ClearSelectedAttack()
    {
        _selectedAttack = null;
    }

    public bool SelectedAttackIs(EnemyAttackConfigSO attackConfig)
    {
        return _selectedAttack != null && _selectedAttack == attackConfig;
    }
}