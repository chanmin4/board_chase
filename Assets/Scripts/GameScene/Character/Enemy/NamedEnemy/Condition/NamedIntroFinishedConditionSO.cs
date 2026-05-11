using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "NamedIntroFinishedCondition",
    menuName = "State Machines/Named Enemy Conditions/Named Intro Finished")]
public class NamedIntroFinishedConditionSO : StateConditionSO<NamedIntroFinishedCondition>
{
}

public class NamedIntroFinishedCondition : Condition
{
    private NamedEnemyBlackboard _blackboard;

    public override void Awake(StateMachine stateMachine)
    {
        _blackboard = stateMachine.GetComponent<NamedEnemyBlackboard>();
    }

    protected override bool Statement()
    {
        return _blackboard != null && _blackboard.introFinished;
    }
}
