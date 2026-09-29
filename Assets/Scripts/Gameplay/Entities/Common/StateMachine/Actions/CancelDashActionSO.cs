/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Actions
 * Primary responsibility: Defines CancelDashActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "CancelDashAction", menuName = "State Machines/Player Actions/Cancel Dash")]
public class CancelDashActionSO : StateActionSO<CancelDashAction>
{
}

public class CancelDashAction : StateAction
{
    private ShooterDashController _dashController;

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        if (!stateMachine.TryGetComponent(out _dashController))
            _dashController = stateMachine.GetComponentInChildren<ShooterDashController>(true);
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        _dashController?.CancelDash();
    }
}
