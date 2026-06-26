using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "NamedSpawnInitializedCondition",
    menuName = "State Machines/Named Enemy Conditions/Named Spawn Initialized")]
public class NamedSpawnInitializedConditionSO : StateConditionSO<NamedSpawnInitializedCondition>
{
}

public class NamedSpawnInitializedCondition : Condition
{
    private NamedEnemyBlackboard _blackboard;

    public override void Awake(StateMachine stateMachine)
    {
        _blackboard = stateMachine.GetComponent<NamedEnemyBlackboard>();
    }

    protected override bool Statement()
    {
        return _blackboard != null && _blackboard.spawnInitialized;
    }
}
