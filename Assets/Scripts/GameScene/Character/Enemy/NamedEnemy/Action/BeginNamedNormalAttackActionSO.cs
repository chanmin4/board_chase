using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "BeginNamedNormalAttackAction",
    menuName = "State Machines/Named Enemy Actions/Begin Normal Attack")]
public class BeginNamedNormalAttackActionSO : StateActionSO<BeginNamedNormalAttackAction>
{
}

public class BeginNamedNormalAttackAction : StateAction
{
    private NamedEnemyBlackboard _blackboard;

    public override void Awake(StateMachine stateMachine)
    {
        _blackboard = stateMachine.GetComponentInParent<NamedEnemyBlackboard>();
    }

    public override void OnStateEnter()
    {
        _blackboard?.BeginNormalAttackWindow();
    }

    public override void OnUpdate()
    {
    }
}
