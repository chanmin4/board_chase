/*
 * Role hierarchy: StateMachine > Debugging
 * Primary responsibility: Implements the focused StateMachineDebugger responsibility and collaborates with its role group.
 * Execution boundary: Runtime (VSplatter.StateMachine)
 */

#if UNITY_EDITOR

using System;
using System.Text;
using UnityEngine;

namespace VSplatter.StateMachine.Debugging
{
	/// <summary>
	/// Class specialized in debugging the state transitions, should only be used while in editor mode.
	/// </summary>
	[Serializable]
	internal class StateMachineDebugger
	{
		[SerializeField]
		[Tooltip("Issues a debug log when a state transition is triggered")]
		internal bool debugTransitions = false;

		[SerializeField]
		[Tooltip("List all conditions evaluated, the result is read: ConditionName == BooleanResult [PassedTest]")]
		internal bool appendConditionsInfo = true;

		[SerializeField]
		[Tooltip("List all actions activated by the new State")]
		internal bool appendActionsInfo = true;

		[SerializeField]
		[Tooltip("The current State name [Readonly]")]
		internal string currentState;

		private StateMachine _stateMachine;
		private StringBuilder _logBuilder;
		private string _targetState = string.Empty;

		private const string CHECK_MARK = "\u2714";
		private const string UNCHECK_MARK = "\u2718";
		private const string THICK_ARROW = "\u279C";
		private const string SHARP_ARROW = "\u27A4";

		/// <summary>
		/// Must be called together with <c>StateMachine.Awake()</c>
		/// </summary>
		internal void Awake(StateMachine stateMachine)
		{
			_stateMachine = stateMachine;
			_logBuilder = new StringBuilder();

			currentState = stateMachine._currentState._originSO.name;
		}

		// Purpose: Performs the transition evaluation begin workflow for this component.
		internal void TransitionEvaluationBegin(string targetState)
		{
			_targetState = targetState;

			if (!debugTransitions)
				return;

			_logBuilder.Clear();
			_logBuilder.AppendLine($"{_stateMachine.name} state changed");
			_logBuilder.AppendLine($"{currentState}  {SHARP_ARROW}  {_targetState}");

			if (appendConditionsInfo)
			{
				_logBuilder.AppendLine();
				_logBuilder.AppendLine($"Transition Conditions:");
			}
		}

		// Purpose: Performs the transition condition result workflow for this component.
		internal void TransitionConditionResult(string conditionName, bool result, bool isMet)
		{
			if (!debugTransitions || _logBuilder.Length == 0 || !appendConditionsInfo)
				return;

			_logBuilder.Append($"    {THICK_ARROW} {conditionName} == {result}");

			if (isMet)
				_logBuilder.AppendLine($" [{CHECK_MARK}]");
			else
				_logBuilder.AppendLine($" [{UNCHECK_MARK}]");
		}

		// Purpose: Performs the transition evaluation end workflow for this component.
		internal void TransitionEvaluationEnd(bool passed, StateAction[] actions)
		{
			if (passed)
				currentState = _targetState;

			if (!debugTransitions || _logBuilder.Length == 0)
				return;

			if (passed)
			{
				LogActions(actions);
				PrintDebugLog();
			}

			_logBuilder.Clear();
		}

		// Purpose: Performs the log actions workflow for this component.
		private void LogActions(StateAction[] actions)
		{
			if (!appendActionsInfo)
				return;

			_logBuilder.AppendLine();
			_logBuilder.AppendLine("State Actions:");

			foreach (StateAction action in actions)
			{
				_logBuilder.AppendLine($"    {THICK_ARROW} {action._originSO.name}");
			}
		}

		// Purpose: Performs the print debug log workflow for this component.
		private void PrintDebugLog()
		{
			_logBuilder.AppendLine();
			_logBuilder.Append("--------------------------------");

			Debug.Log(_logBuilder.ToString());
		}
	}
}

#endif
