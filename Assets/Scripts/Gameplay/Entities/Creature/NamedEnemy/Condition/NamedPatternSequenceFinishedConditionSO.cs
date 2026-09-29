/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > Condition
 * Primary responsibility: Defines NamedPatternSequenceFinishedConditionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _pattern = stateMachine.GetComponentInParent<NamedPatternController>();
    }

    // Purpose: Performs the statement workflow for this component.
    protected override bool Statement()
    {
        bool result = _pattern != null && _pattern.sequenceFinished;

        if (result)
            Debug.Log("[NamedPatternSequenceFinishedCondition] true", _pattern);

        return result;
    }
}
