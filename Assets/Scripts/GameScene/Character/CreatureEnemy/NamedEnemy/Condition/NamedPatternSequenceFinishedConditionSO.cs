using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "NamedPatternSequenceFinishedCondition",
    menuName = "State Machines/Named Enemy Conditions/Pattern Sequence Finished")]
public class NamedPatternSequenceFinishedConditionSO : StateConditionSO<NamedPatternSequenceFinishedCondition>
{
}

public class NamedPatternSequenceFinishedCondition : Condition
{
    private NamedPatternController _pattern;

    public override void Awake(StateMachine stateMachine)
    {
        _pattern = stateMachine.GetComponentInParent<NamedPatternController>();
    }

    protected override bool Statement()
    {
        bool result = _pattern != null && _pattern.sequenceFinished;

        if (result)
            Debug.Log("[NamedPatternSequenceFinishedCondition] true", _pattern);

        return result;
    }
}
