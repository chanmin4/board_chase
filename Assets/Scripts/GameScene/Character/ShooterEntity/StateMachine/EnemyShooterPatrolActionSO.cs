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

    public override void Awake(StateMachine stateMachine)
    {
        _origin = (EnemyShooterPatrolActionSO)OriginSO;
        _combat = stateMachine.GetOrAddComponent<EnemyShooterCombatController>();
    }

    public override void OnStateEnter()
    {
        _combat?.BeginPatrol();
    }

    public override void OnUpdate()
    {
        if (_combat == null)
            return;

        _combat.TickAwareness();
        _combat.TickPatrol();
    }

    public override void OnStateExit()
    {
        if (_origin.StopMovementOnExit)
            _combat?.StopMovement();
    }
}
