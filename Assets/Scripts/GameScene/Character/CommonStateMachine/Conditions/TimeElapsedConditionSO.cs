using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(menuName = "State Machines/Conditions/Time elapsed")]
public class TimeElapsedConditionSO : StateConditionSO<TimeElapsedCondition>
{
	public float timerLength = .5f;
	[SerializeField] private DifficultyTimerScaleMode _difficultyScaleMode = DifficultyTimerScaleMode.None;

	public float ResolvedTimerLength
	{
		get
		{
			float length = Mathf.Max(0.01f, timerLength);

			switch (_difficultyScaleMode)
			{
				case DifficultyTimerScaleMode.EnemyInfectionCast:
					return DifficultyRuntime.ApplyEnemyInfectionCastDuration(length);

				default:
					return length;
			}
		}
	}
}

public enum DifficultyTimerScaleMode
{
	None,
	EnemyInfectionCast
}

public class TimeElapsedCondition : Condition
{
	private float _startTime;
	private TimeElapsedConditionSO _originSO => (TimeElapsedConditionSO)base.OriginSO; // The SO this Condition spawned from

	public override void OnStateEnter()
	{
		_startTime = Time.time;
	}

	protected override bool Statement() => Time.time >= _startTime + _originSO.ResolvedTimerLength;
}
