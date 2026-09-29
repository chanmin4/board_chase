/*
 * Role hierarchy: Gameplay > Entities > Creature > CommonEnemyStateMachine > AttackExecutor
 * Primary responsibility: Handles EnemyAttackExecutorController combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public class EnemyAttackExecutorController : MonoBehaviour
{
    [Header("Runtime Don't Touch")]
    [SerializeField, ReadOnly] private EnemyAttackConfigSO _selectedAttack;
    [SerializeField, ReadOnly] private bool _attackRunning;
    [SerializeField, ReadOnly] private bool _attackFinished = true;

    public EnemyAttackConfigSO SelectedAttack => _selectedAttack;
    public bool HasSelectedAttack => _selectedAttack != null;
    public bool AttackRunning => _attackRunning;
    public bool AttackFinished => _attackFinished;

    // Purpose: Begins the attack workflow.
    public void BeginAttack(EnemyAttackConfigSO attackConfig)
    {
        _selectedAttack = attackConfig;
        _attackRunning = attackConfig != null;
        _attackFinished = attackConfig == null;
    }

    // Purpose: Finishes the active attack workflow.
    public void FinishAttack(bool clearSelectedAttack = true)
    {
        _attackRunning = false;
        _attackFinished = true;

        if (clearSelectedAttack)
            _selectedAttack = null;
    }

    // Purpose: Clears the current attack state.
    public void ClearAttack()
    {
        _selectedAttack = null;
        _attackRunning = false;
        _attackFinished = true;
    }
}
