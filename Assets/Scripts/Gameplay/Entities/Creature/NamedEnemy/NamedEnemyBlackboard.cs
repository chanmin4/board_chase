/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy
 * Primary responsibility: Implements the focused NamedEnemyBlackboard responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Selects attack for the active workflow.
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

    // Purpose: Finishes the active selected attack workflow.
    public void FinishSelectedAttack(bool clearSelectedAttack = true)
    {
        attackFinished = true;

        if (clearSelectedAttack)
            ClearSelectedAttack();
    }

    // Purpose: Clears the current selected attack state.
    public void ClearSelectedAttack()
    {
        _selectedAttack = null;
    }

    // Purpose: Selects selected attack is for the active workflow.
    public bool SelectedAttackIs(EnemyAttackConfigSO attackConfig)
    {
        return _selectedAttack != null && _selectedAttack == attackConfig;
    }
}
