using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "SearchSucceededCondition", menuName = "State Machines/Enemy Conditions/Search Succeeded")]
public class SearchSucceededConditionSO : StateConditionSO<SearchSucceededCondition>
{
}

public class SearchSucceededCondition : Condition
{
	private Enemy _enemy;

	public override void Awake(StateMachine stateMachine)
	{
		_enemy = stateMachine.GetComponent<Enemy>();
	}

	protected override bool Statement()
	{
		return _enemy != null && _enemy.SearchSucceeded;
	}
}
