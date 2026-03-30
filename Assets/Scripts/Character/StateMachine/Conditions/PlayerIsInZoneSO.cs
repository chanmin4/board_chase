using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;


public enum ZoneType
{
	Alert,
	Attack
}

[CreateAssetMenu(fileName = "PlayerIsInZone", menuName = "State Machines/Conditions/Player Is In Zone")]
public class PlayerIsInZoneSO : StateConditionSO
{
	public ZoneType zone;

	protected override Condition CreateCondition() => new PlayerIsInZone();
}

public class PlayerIsInZone : Condition
{

	private Creature _creature;

	public override void Awake(StateMachine stateMachine)
	{
		_creature = stateMachine.GetComponent<Creature>();
	}

	protected override bool Statement()
	{
		bool result = false;
		if (_creature != null)
		{
			switch (((PlayerIsInZoneSO)OriginSO).zone)
			{
				case ZoneType.Alert:
					result = _creature.isPlayerInAlertZone;
					break;
				case ZoneType.Attack:
					result = _creature.isPlayerInAttackZone;
					break;
				default:
					break;
			}
		}
		return result;
	}
}
