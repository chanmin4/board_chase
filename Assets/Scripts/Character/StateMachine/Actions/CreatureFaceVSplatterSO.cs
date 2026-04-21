using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "CreatureFacePlayer", menuName = "State Machines/Actions/Creature Face Player")]
public class CreatureFacePlayerSO : StateActionSO
{
	public TransformAnchor playerAnchor;
	protected override StateAction CreateAction() => new CreatureFacePlayer();
}

public class CreatureFacePlayer : StateAction
{
	TransformAnchor _protagonist;
	Transform _actor;
	public override void Awake(StateMachine stateMachine)
	{
		_actor = stateMachine.transform;
		_protagonist = ((CreatureFacePlayerSO)OriginSO).playerAnchor;
	}

	public override void OnUpdate()
	{
		if (_protagonist.isSet)
		{
			Vector3 relativePos = _protagonist.Value.position - _actor.position;
			relativePos.y = 0f; // Force rotation to be only on Y axis.

			Quaternion rotation = Quaternion.LookRotation(relativePos);
			_actor.rotation = rotation;
		}
	}

	public override void OnStateEnter()
	{

	}
}
