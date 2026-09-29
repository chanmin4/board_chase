/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > StateMachine
 * Primary responsibility: Defines EnemyShooterAttackActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "EnemyShooterAttackAction",
    menuName = "State Machines/Enemy Shooter/Actions/Attack")]
public class EnemyShooterAttackActionSO : StateActionSO<EnemyShooterAttackAction>
{
}

public class EnemyShooterAttackAction : StateAction
{
    private EnemyShooterCombatController _combat;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _combat = EnemyShooterStateMachineUtility.ResolveCombatController(stateMachine);
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        _combat?.BeginAttack();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
        if (_combat == null)
            return;

        _combat.TickAwareness();
        _combat.TickAttack();
    }

    // Purpose: Responds to the state exit callback.
    public override void OnStateExit()
    {
        _combat?.EndAttack();
    }
}
