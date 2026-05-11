using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "PatternResolvedCondition",
    menuName = "State Machines/Named Enemy Conditions/Pattern Resolved")]
public class PatternResolvedConditionSO : StateConditionSO<PatternResolvedCondition>
{
}

public class PatternResolvedCondition : Condition
{
    private NamedPatternController _pattern;

    public override void Awake(StateMachine stateMachine)
    {
        _pattern = stateMachine.GetComponent<NamedPatternController>();
    }

    protected override bool Statement()
    {
        return _pattern != null && _pattern.resolved;
    }
}
