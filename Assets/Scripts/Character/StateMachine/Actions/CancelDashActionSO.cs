using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "CancelDashAction", menuName = "State Machines/Player Actions/Cancel Dash")]
public class CancelDashActionSO : StateActionSO<CancelDashAction>
{
}

public class CancelDashAction : StateAction
{
    private VSplatterDashController _dashController;

    public override void Awake(StateMachine stateMachine)
    {
        stateMachine.TryGetComponent(out _dashController);
    }

    public override void OnUpdate()
    {
    }

    public override void OnStateEnter()
    {
        _dashController?.CancelDash();
    }
}
