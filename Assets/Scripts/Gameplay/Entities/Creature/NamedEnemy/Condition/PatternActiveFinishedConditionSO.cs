/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > Condition
 * Primary responsibility: Defines PatternActiveFinishedConditionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _pattern = stateMachine.GetComponent<NamedPatternController>();
    }

    // Purpose: Performs the statement workflow for this component.
    protected override bool Statement()
    {
        return _pattern != null && _pattern.activeFinished;
    }
}
