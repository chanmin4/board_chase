/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Actions
 * Primary responsibility: Defines CancelActiveKnockbackActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _receiver = stateMachine.GetComponent<KnockbackReceiver>();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate() { }

    // Purpose: Responds to the state exit callback.
    public override void OnStateExit()
    {
        _receiver?.CancelActiveKnockback();
    }
}
