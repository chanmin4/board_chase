using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "CancelShockwaveChargeAction",
    menuName = "State Machines/Player Actions/Cancel Shockwave Charge")]
public class CancelShockwaveChargeActionSO : StateActionSO<CancelShockwaveChargeAction>
{
}

public class CancelShockwaveChargeAction : StateAction
{
    private VSplatterShockwaveController _shockwaveController;

    public override void Awake(StateMachine stateMachine)
    {
        stateMachine.TryGetComponent(out _shockwaveController);
    }

    public override void OnUpdate()
    {
    }

    public override void OnStateEnter()
    {
        _shockwaveController?.CancelCharge();
    }
}
