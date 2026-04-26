using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "ConsumePendingKnockbackAction",
    menuName = "State Machines/Common Actions/Consume Pending Knockback")]
public class ConsumePendingKnockbackActionSO : StateActionSO<ConsumePendingKnockbackAction>
{
}

public class ConsumePendingKnockbackAction : StateAction
{
    private KnockbackReceiver _receiver;
     public override void OnUpdate()
    {
        
    }

    public override void Awake(StateMachine stateMachine)
    {
        _receiver = stateMachine.GetComponent<KnockbackReceiver>();
    }

    public override void OnStateEnter()
    {
        _receiver?.ConsumePendingRequest();
    }
}
