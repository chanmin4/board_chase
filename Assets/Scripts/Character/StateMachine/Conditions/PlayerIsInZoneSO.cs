using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;


public enum ZoneType
{
	Alert,
	Attack
}

[CreateAssetMenu(fileName = "PlayerIsInZone", menuName = "State Machines/Conditions/Player Is In Zone")]
public class PlayerIsInZoneSO : StateConditionSO<PlayerIsInZone>
{
	public ZoneType zone;

	protected override Condition CreateCondition() => new PlayerIsInZone();
}

public class PlayerIsInZone : Condition
{

	private Enemy _enemy;

	public override void Awake(StateMachine stateMachine)
	{
		_enemy = stateMachine.GetComponent<Enemy>();
	}

	protected override bool Statement()
	{
		bool result = false;
		if (_enemy != null)
		{
			switch (((PlayerIsInZoneSO)OriginSO).zone)
			{
				case ZoneType.Alert:
					result = _enemy.isPlayerInAlertZone;
					break;
				case ZoneType.Attack:
					result = _enemy.isPlayerInAttackZone;
					break;
				default:
					break;
			}
		}
		return result;
	}
}
