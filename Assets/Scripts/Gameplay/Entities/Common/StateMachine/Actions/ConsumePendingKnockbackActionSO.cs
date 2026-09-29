/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Actions
 * Primary responsibility: Defines ConsumePendingKnockbackActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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
     // Purpose: Responds to the update callback.
     public override void OnUpdate()
    {
        
    }

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _receiver = stateMachine.GetComponent<KnockbackReceiver>();
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        _receiver?.ConsumePendingRequest();
    }
}
