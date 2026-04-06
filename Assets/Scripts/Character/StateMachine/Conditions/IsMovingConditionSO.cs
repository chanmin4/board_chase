using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(menuName = "State Machines/Conditions/Started Moving")]
public class IsMovingConditionSO : StateConditionSO<IsMovingCondition>
{
	public float treshold = 0.02f;
}

public class IsMovingCondition : Condition
{
	private VSplatter_Character _vsplatterScript;
	private IsMovingConditionSO _originSO => (IsMovingConditionSO)base.OriginSO; // The SO this Condition spawned from

	public override void Awake(StateMachine stateMachine)
	{
		_vsplatterScript = stateMachine.GetComponent<VSplatter_Character>();
	}

	protected override bool Statement()
	{
		Vector3 movementVector = _vsplatterScript.movementInput;
		movementVector.y = 0f;
		Debug.Log(movementVector.sqrMagnitude +" " +_originSO.treshold);
		return movementVector.sqrMagnitude > _originSO.treshold;
	}
}
