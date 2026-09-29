/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > StateMachine
 * Primary responsibility: Defines EnemyShooterClearCombatActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _origin = (EnemyShooterClearCombatActionSO)OriginSO;

        if (!stateMachine.TryGetComponent(out _combat))
            _combat = stateMachine.GetComponentInParent<EnemyShooterCombatController>();

        if (_combat == null)
            _combat = stateMachine.GetComponentInChildren<EnemyShooterCombatController>(true);
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        if (_combat == null)
            return;

        if (_origin.StopMovement)
            _combat.StopMovement();

        if (_origin.ClearTarget)
            _combat.ClearCombat();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
    }
}
