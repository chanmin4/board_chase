using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "NamedPatternReadyCondition",
    menuName = "State Machines/Named Enemy Conditions/Pattern Ready")]
public class NamedPatternReadyConditionSO : StateConditionSO<NamedPatternReadyCondition>
{
}

public class NamedPatternReadyCondition : Condition
{
    private NamedPatternController _pattern;

    public override void Awake(StateMachine stateMachine)
    {
        _pattern = stateMachine.GetComponent<NamedPatternController>();
    }

    protected override bool Statement()
    {
        return _pattern != null && _pattern.patternReady;
    }
}
