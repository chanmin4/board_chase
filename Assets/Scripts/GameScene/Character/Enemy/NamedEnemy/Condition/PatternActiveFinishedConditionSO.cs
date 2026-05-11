using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "PatternActiveFinishedCondition",
    menuName = "State Machines/Named Enemy Conditions/Pattern Active Finished")]
public class PatternActiveFinishedConditionSO : StateConditionSO<PatternActiveFinishedCondition>
{
}

public class PatternActiveFinishedCondition : Condition
{
    private NamedPatternController _pattern;

    public override void Awake(StateMachine stateMachine)
    {
        _pattern = stateMachine.GetComponent<NamedPatternController>();
    }

    protected override bool Statement()
    {
        return _pattern != null && _pattern.activeFinished;
    }
}
