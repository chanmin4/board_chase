using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "ArrivedToInfectionTargetCondition", menuName = "State Machines/Enemy Conditions/Arrived To Infection Target")]
public class ArrivedToInfectionTargetConditionSO : StateConditionSO<ArrivedToInfectionTargetCondition>
{
	[Min(0f)]
	public float arriveDistance = 0.35f;
}

public class ArrivedToInfectionTargetCondition : Condition
{
	private Enemy _enemy;
	private Transform _transform;
	private ArrivedToInfectionTargetConditionSO _originSO => (ArrivedToInfectionTargetConditionSO)OriginSO;

	public override void Awake(StateMachine stateMachine)
	{
		_enemy = stateMachine.GetComponent<Enemy>();
		_transform = stateMachine.transform;
	}

	protected override bool Statement()
	{
		if (_enemy == null || !_enemy.HasInfectionTarget)
			return false;

		Vector3 offset = _enemy.InfectionTargetPosition - _transform.position;
		offset.y = 0f;
		return offset.sqrMagnitude <= _originSO.arriveDistance * _originSO.arriveDistance;
	}
}
