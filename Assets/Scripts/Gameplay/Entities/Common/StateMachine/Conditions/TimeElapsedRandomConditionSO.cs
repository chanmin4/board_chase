/*
 * Role hierarchy: Gameplay > Entities > Common > StateMachine > Conditions
 * Primary responsibility: Defines TimeElapsedRandomConditionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(menuName = "State Machines/Conditions/Time elapsed random")]
public class TimeElapsedRandomConditionSO : StateConditionSO<TimeElapsedRandomCondition>
{
	public float minTimerLength = .5f;
	public float maxTimerLength = .5f;
}

public class TimeElapsedRandomCondition : Condition
{
	private float _startTime;
	private float timerLength = .5f;
	private TimeElapsedRandomConditionSO _originSO => (TimeElapsedRandomConditionSO)base.OriginSO; // The SO this Condition spawned from

	// Purpose: Responds to the state enter callback.
	public override void OnStateEnter()
	{
		_startTime = Time.time;
		timerLength = Random.Range(_originSO.minTimerLength, _originSO.maxTimerLength);
	}

	// Purpose: Performs the statement workflow for this component.
	protected override bool Statement() => Time.time >= _startTime + timerLength;
}
