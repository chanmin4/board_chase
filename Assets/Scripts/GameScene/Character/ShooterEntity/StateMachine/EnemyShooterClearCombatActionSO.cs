using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "EnemyShooterClearCombatAction",
    menuName = "State Machines/Enemy Shooter/Actions/Clear Combat")]
public class EnemyShooterClearCombatActionSO : StateActionSO<EnemyShooterClearCombatAction>
{
    [SerializeField] private bool _stopMovement = true;
    [SerializeField] private bool _clearTarget = true;

    public bool StopMovement => _stopMovement;
    public bool ClearTarget => _clearTarget;
}

public class EnemyShooterClearCombatAction : StateAction
{
    private EnemyShooterClearCombatActionSO _origin;
    private EnemyShooterCombatController _combat;

    public override void Awake(StateMachine stateMachine)
    {
        _origin = (EnemyShooterClearCombatActionSO)OriginSO;

        if (!stateMachine.TryGetComponent(out _combat))
            _combat = stateMachine.GetComponentInParent<EnemyShooterCombatController>();

        if (_combat == null)
            _combat = stateMachine.GetComponentInChildren<EnemyShooterCombatController>(true);
    }

    public override void OnStateEnter()
    {
        if (_combat == null)
            return;

        if (_origin.StopMovement)
            _combat.StopMovement();

        if (_origin.ClearTarget)
            _combat.ClearCombat();
    }

    public override void OnUpdate()
    {
    }
}
