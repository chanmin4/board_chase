/*
using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "IsPlayerInCurrentSectorCondition",
    menuName = "State Machines/Enemy Conditions/Is Player In Current Sector")]
public class IsPlayerInCurrentSectorConditionSO : StateConditionSO<IsPlayerInCurrentSectorCondition>
{
}

public class IsPlayerInCurrentSectorCondition : Condition
{
    private Enemy _enemy;

    public override void Awake(StateMachine stateMachine)
    {
        _enemy = stateMachine.GetComponent<Enemy>();
    }

    protected override bool Statement()
    {
        //return _enemy != null && _enemy.IsPlayerInCurrentSector;
    }
}
*/