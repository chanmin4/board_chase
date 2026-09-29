/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Actions > MovementActions
 * Primary responsibility: Implements the focused RoamingMovementAction responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.AI;

public class RoamingMovementAction : NPCMovementAction
{
	private NavMeshAgent _agent;
	private bool _isActiveAgent;
	private Vector3 _startPosition;

	private float _roamingSpeed;
	private float _roamingDistance;

	private Vector3 _roamingTargetPosition;

	public RoamingMovementAction(
		RoamingAroundCenterConfigSO config, NavMeshAgent agent, Vector3 startPosition)
	{
		_agent = agent;
		_isActiveAgent = _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;
		if (config.FromSpawningPoint)
		{
			_startPosition = startPosition;
		}
		else
		{
			_startPosition = config.CustomCenter;
		}
		_roamingSpeed = config.Speed;
		_roamingDistance = config.Radius;
	}

	// Purpose: Responds to the update callback.
	public override void OnUpdate()
	{

	}

	// Purpose: Responds to the state enter callback.
	public override void OnStateEnter()
	{
		if (_isActiveAgent)
		{
			_roamingTargetPosition = GetRoamingPositionAroundPosition(_startPosition);
			_agent.speed = _roamingSpeed;
			_agent.isStopped = false;
			_agent.SetDestination(_roamingTargetPosition);
		}
	}

	// Purpose: Responds to the state exit callback.
	public override void OnStateExit()
	{

	}

	// Compute a random target position around the starting position.
	// Purpose: Returns the current roaming position around position.
	private Vector3 GetRoamingPositionAroundPosition(Vector3 position)
	{
		return position + new Vector3(Random.Range(-1, 1), 0.0f, Random.Range(-1, 1)).normalized * Random.Range(_roamingDistance / 2, _roamingDistance);
	}
}
