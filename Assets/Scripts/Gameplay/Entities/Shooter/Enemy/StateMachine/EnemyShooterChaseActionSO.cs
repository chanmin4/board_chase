/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > StateMachine
 * Primary responsibility: Defines EnemyShooterChaseActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "EnemyShooterChaseAction",
    menuName = "State Machines/Enemy Shooter/Actions/Chase")]
public class EnemyShooterChaseActionSO : StateActionSO<EnemyShooterChaseAction>
{
    public enum ChaseMode
    {
        Target,
        Investigation
    }

    [Header("Mode")]
    [SerializeField] private ChaseMode _mode = ChaseMode.Target;

    [Tooltip("If true, stops the NavMeshAgent when this state exits.")]
    [SerializeField] private bool _stopMovementOnExit = false;

    public ChaseMode Mode => _mode;
    public bool StopMovementOnExit => _stopMovementOnExit;
}

public class EnemyShooterChaseAction : StateAction
{
    private EnemyShooterChaseActionSO _origin;
    private EnemyShooterCombatController _combat;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _origin = (EnemyShooterChaseActionSO)OriginSO;
        _combat = EnemyShooterStateMachineUtility.ResolveCombatController(stateMachine);
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        if (_combat == null)
            return;

        if (_origin.Mode == EnemyShooterChaseActionSO.ChaseMode.Investigation)
            _combat.BeginInvestigate();
        else
            _combat.BeginChase();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
        if (_combat == null)
            return;

        _combat.TickAwareness();

        if (_origin.Mode == EnemyShooterChaseActionSO.ChaseMode.Investigation)
            _combat.TickInvestigate();
        else
            _combat.TickChase();
    }

    // Purpose: Responds to the state exit callback.
    public override void OnStateExit()
    {
        if (_origin.StopMovementOnExit)
            _combat?.StopMovement();
    }
}
