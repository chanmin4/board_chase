/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > StateMachine
 * Primary responsibility: Defines EnemyShooterPatrolActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "EnemyShooterPatrolAction",
    menuName = "State Machines/Enemy Shooter/Actions/Patrol")]
public class EnemyShooterPatrolActionSO : StateActionSO<EnemyShooterPatrolAction>
{
    [Tooltip("If true, stops the NavMeshAgent when this state exits.")]
    [SerializeField] private bool _stopMovementOnExit = false;

    public bool StopMovementOnExit => _stopMovementOnExit;
}

public class EnemyShooterPatrolAction : StateAction
{
    private EnemyShooterPatrolActionSO _origin;
    private EnemyShooterCombatController _combat;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _origin = (EnemyShooterPatrolActionSO)OriginSO;
        _combat = EnemyShooterStateMachineUtility.ResolveCombatController(stateMachine);
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        _combat?.BeginPatrol();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
        if (_combat == null)
            return;

        _combat.TickAwareness();
        _combat.TickPatrol();
    }

    // Purpose: Responds to the state exit callback.
    public override void OnStateExit()
    {
        if (_origin.StopMovementOnExit)
            _combat?.StopMovement();
    }
}
