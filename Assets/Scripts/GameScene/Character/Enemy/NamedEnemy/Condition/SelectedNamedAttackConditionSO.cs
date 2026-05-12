using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "SelectedNamedAttackCondition",
    menuName = "State Machines/Named Enemy Conditions/Selected Named Attack")]
public class SelectedNamedAttackConditionSO : StateConditionSO<SelectedNamedAttackCondition>
{
    [SerializeField] private NamedAttackIdSO _expectedAttack;

    public NamedAttackIdSO ExpectedAttack => _expectedAttack;
}

public class SelectedNamedAttackCondition : Condition
{
    private SelectedNamedAttackConditionSO _origin;
    private NamedEnemyBlackboard _blackboard;

    public override void Awake(StateMachine stateMachine)
    {
        _origin = (SelectedNamedAttackConditionSO)OriginSO;
        _blackboard = stateMachine.GetComponentInParent<NamedEnemyBlackboard>();
    }

    protected override bool Statement()
    {
        if (_blackboard == null || _origin.ExpectedAttack == null)
            return false;

        return _blackboard.SelectedAttackIs(_origin.ExpectedAttack);
    }
}
