using System;
using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;
using Moment = VSplatter.StateMachine.StateAction.SpecificMoment;

/// <summary>
/// Flexible StateActionSO for the StateMachine which allows to set any parameter on the Animator, in any moment of the state (OnStateEnter, OnStateExit, or each OnUpdate).
/// </summary>
[CreateAssetMenu(fileName = "AnimatorMoveSpeedAction", menuName = "State Machines/Actions/Set Animator Move Speed")]
public class AnimatorMoveSpeedActionSO : StateActionSO
{
	public string parameterName = default;

	protected override StateAction CreateAction() => new AnimatorMoveSpeedAction(Animator.StringToHash(parameterName));
}

public class AnimatorMoveSpeedAction : StateAction
{
	//Component references
	private Animator _animator;
	private VSplatter_Character _vsplatter;

	private AnimatorParameterActionSO _originSO => (AnimatorParameterActionSO)base.OriginSO; // The SO this StateAction spawned from
	private int _parameterHash;

	public AnimatorMoveSpeedAction(int parameterHash)
	{
		_parameterHash = parameterHash;
	}

	public override void Awake(StateMachine stateMachine)
	{
		_animator = stateMachine.GetComponent<Animator>();
		_vsplatter = stateMachine.GetComponent<VSplatter_Character>();
	}

	public override void OnUpdate()
	{
		//TODO: do we like that we're using the magnitude here, per frame? Can this be done in a smarter way?
		float normalisedSpeed = _vsplatter.movementInput.magnitude;
		_animator.SetFloat(_parameterHash, normalisedSpeed);
	}
}
