/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Actions
 * Primary responsibility: Defines the StopAgentSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.AI;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "StopAgent", menuName = "State Machines/Actions/Stop NavMesh Agent")]
public class StopAgentSO : StateActionSO
{
	// Purpose: Creates the action owned by this workflow.
	protected override StateAction CreateAction() => new StopAgent();
}

public class StopAgent : StateAction
{
	private NavMeshAgent _agent;
	private bool _agentDefined;

	// Purpose: Caches the component references required before runtime initialization.
	public override void Awake(StateMachine stateMachine)
	{
		_agent = stateMachine.gameObject.GetComponent<NavMeshAgent>();
		_agentDefined = _agent != null;
	}

	// Purpose: Responds to the update callback.
	public override void OnUpdate()
	{

	}

	// Purpose: Responds to the state enter callback.
	public override void OnStateEnter()
	{
		if (_agentDefined)
		{
			_agent.isStopped = true;
		}
	}
}
