/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > StateMachine
 * Primary responsibility: Defines EnemyShooterAcquireTargetActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "EnemyShooterAcquireTargetAction",
    menuName = "State Machines/Enemy Shooter/Actions/Acquire Target")]
public class EnemyShooterAcquireTargetActionSO : StateActionSO<EnemyShooterAcquireTargetAction>
{
    [Tooltip("If true, scans immediately when entering the state instead of waiting for Target Search Interval.")]
    [SerializeField] private bool _forceScanOnEnter = true;

    public bool ForceScanOnEnter => _forceScanOnEnter;
}

public class EnemyShooterAcquireTargetAction : StateAction
{
    private EnemyShooterAcquireTargetActionSO _origin;
    private EnemyShooterCombatController _combat;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _origin = (EnemyShooterAcquireTargetActionSO)OriginSO;
        _combat = EnemyShooterStateMachineUtility.ResolveCombatController(stateMachine);
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        if (_origin.ForceScanOnEnter)
            _combat?.ForceAcquirePlayer();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
        _combat?.TickAwareness();
    }
}
