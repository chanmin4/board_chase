/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > StateMachine
 * Primary responsibility: Defines EnemyShooterThrowableActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "EnemyShooterThrowableAction",
    menuName = "State Machines/Enemy Shooter/Actions/Throwable")]
public class EnemyShooterThrowableActionSO : StateActionSO<EnemyShooterThrowableAction>
{
}

public class EnemyShooterThrowableAction : StateAction
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
        _combat?.BeginThrowableAttack();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
        if (_combat == null)
            return;

        _combat.TickAwareness();
        _combat.TickThrowableAttack();
    }

    // Purpose: Responds to the state exit callback.
    public override void OnStateExit()
    {
        _combat?.EndThrowableAttack();
    }
}
