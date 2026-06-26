using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "PatternPrepareFinishedCondition",
    menuName = "State Machines/Named Enemy Conditions/Pattern Prepare Finished")]
public class PatternPrepareFinishedConditionSO : StateConditionSO<PatternPrepareFinishedCondition>
{
}

public class PatternPrepareFinishedCondition : Condition
{
    private NamedPatternController _pattern;

    public override void Awake(StateMachine stateMachine)
    {
        _pattern = stateMachine.GetComponent<NamedPatternController>();
    }

    protected override bool Statement()
    {
        return _pattern != null && _pattern.prepareFinished;
    }
}
