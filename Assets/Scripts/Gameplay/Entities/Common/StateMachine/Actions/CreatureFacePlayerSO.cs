/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Actions
 * Primary responsibility: Defines the CreatureFacePlayerSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;
//only face on update!! 
[CreateAssetMenu(fileName = "CreatureFacePlayer", menuName = "State Machines/Actions/Creature Face Player")]
public class CreatureFacePlayerSO : StateActionSO
{
	public TransformAnchor playerAnchor;
	// Purpose: Creates the action owned by this workflow.
	protected override StateAction CreateAction() => new CreatureFacePlayer();
}

public class CreatureFacePlayer : StateAction
{
	TransformAnchor _protagonist;
	Transform _actor;
	// Purpose: Caches the component references required before runtime initialization.
	public override void Awake(StateMachine stateMachine)
	{
		_actor = stateMachine.transform;
		_protagonist = ((CreatureFacePlayerSO)OriginSO).playerAnchor;
	}

	// Purpose: Responds to the update callback.
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

	// Purpose: Responds to the state enter callback.
	public override void OnStateEnter()
	{

	}
}
