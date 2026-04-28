using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(fileName = "IsHoldingShockWave", menuName = "State Machines/Conditions/IsHoldingShockWave")]
public class IsHoldingShockWaveSO : StateConditionSO<IsHoldingShockWave> { }

public class IsHoldingShockWave : Condition
{
	private CharacterController _characterController;
	private VSplatter_Character _vsplatterScript;

	public override void Awake(StateMachine stateMachine)
	{
		_vsplatterScript = stateMachine.GetComponent<VSplatter_Character>();
	}

	protected override bool Statement()
	{
		return _vsplatterScript.ShockwaveInput;
	}
}
