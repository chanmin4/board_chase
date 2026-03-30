using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "ClearInputCache_OnEnter", menuName = "State Machines/Actions/Clear Input Cache On Enter")]
public class ClearInputCache_OnEnterSO : StateActionSO
{
	protected override StateAction CreateAction() => new ClearInputCache_OnEnter();
}

public class ClearInputCache_OnEnter : StateAction
{
	private VSplatter_Character _vsplatter;
	private InteractionManager _interactionManager;

	public override void Awake(StateMachine stateMachine)
	{
		_vsplatter = stateMachine.GetComponent<VSplatter_Character>();
		_interactionManager = stateMachine.GetComponentInChildren<InteractionManager>();
	}

	public override void OnUpdate()
	{
	}

	public override void OnStateEnter()
	{
		_interactionManager.currentInteractionType = InteractionType.None;
	}
}
