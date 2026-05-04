using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;
//*** 중간 인터럽트 대비용
[CreateAssetMenu(
    fileName = "CancelActiveKnockbackAction",
    menuName = "State Machines/Enemy Actions/Cancel Active Knockback")]
public class CancelActiveKnockbackActionSO : StateActionSO<CancelActiveKnockbackAction>
{
}

public class CancelActiveKnockbackAction : StateAction
{
    private KnockbackReceiver _receiver;

    public override void Awake(StateMachine stateMachine)
    {
        _receiver = stateMachine.GetComponent<KnockbackReceiver>();
    }

    public override void OnUpdate() { }

    public override void OnStateExit()
    {
        _receiver?.CancelActiveKnockback();
    }
}
