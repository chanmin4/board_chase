/*
 * Role hierarchy: StateMachine > Core
 * Primary responsibility: Implements the focused StateTransition responsibility and collaborates with its role group.
 * Execution boundary: Runtime (VSplatter.StateMachine)
 */

namespace VSplatter.StateMachine
{
	public class StateTransition : IStateComponent
	{
		private State _targetState;
		private StateCondition[] _conditions;
		private int[] _resultGroups;
		private bool[] _results;
		private bool _always;
#if UNITY_EDITOR
        internal string TraceSource, TraceTable, TraceRule;
        internal State TraceSourceState;
        internal int TraceRow, TracePriority;
#endif
		public StateExitReason ExitReason { get; private set; }

		internal StateTransition() { }
		public StateTransition(State targetState, StateCondition[] conditions, int[] resultGroups = null,
			bool always = false, StateExitReason exitReason = StateExitReason.Transition)
		{
			Init(targetState, conditions, resultGroups);
			_always = always;
			ExitReason = exitReason;
		}

		// Purpose: Performs the init workflow for this component.
		internal void Init(State targetState, StateCondition[] conditions, int[] resultGroups = null)
		{
			_targetState = targetState;
			_conditions = conditions;
			_resultGroups = resultGroups != null && resultGroups.Length > 0 ? resultGroups : new int[1];
			_results = new bool[_resultGroups.Length];
		}

		/// <summary>
		/// Checks wether the conditions to transition to the target state are met.
		/// </summary>
		/// <param name="state">Returns the state to transition to. Null if the conditions aren't met.</param>
		/// <returns>True if the conditions are met.</returns>
		public bool TryGetTransiton(out State state)
		{
			state = ShouldTransition() ? _targetState : null;
			return state != null;
		}

		// Purpose: Responds to the state enter callback.
		public void OnStateEnter()
		{
			for (int i = 0; i < _conditions.Length; i++)
				_conditions[i]._condition.OnStateEnter();
		}

		// Purpose: Responds to the state exit callback.
		public void OnStateExit()
		{
			for (int i = 0; i < _conditions.Length; i++)
				_conditions[i]._condition.OnStateExit();
		}

		// Purpose: Performs the should transition workflow for this component.
		private bool ShouldTransition()
		{
#if UNITY_EDITOR
            var traceContext = _targetState?._context;
            Debugging.HfsmExecutionTrace.Entry trace = null;
            if (Debugging.HfsmExecutionTrace.Enabled && traceContext?.Owner != null)
            {
                trace = new Debugging.HfsmExecutionTrace.Entry
                {
                    Owner = traceContext.Owner, Frame = UnityEngine.Time.frameCount, Time = UnityEngine.Time.time,
                    Level = TraceLevel(TraceSourceState, traceContext), TargetLevel = TraceLevel(_targetState, traceContext),
                    From = TraceSource ?? traceContext.CurrentState?.name,
                    To = _targetState._originSO?.name, Table = TraceTable, Rule = TraceRule,
                    Row = TraceRow, Priority = TracePriority, Always = _always, ExitReason = ExitReason,
                    Checks = new Debugging.HfsmExecutionTrace.Check[_conditions.Length]
                };
                int index = 0;
                for (int group = 0; group < _resultGroups.Length; group++)
                    for (int j = 0; j < _resultGroups[group]; j++, index++)
                        trace.Checks[index] = new Debugging.HfsmExecutionTrace.Check
                        {
                            Name = _conditions[index]._condition._originSO?.name ?? "Condition",
                            Expected = _conditions[index]._expectedResult,
                            Join = index == 0 ? "" : j == 0 ? "OR" : "AND"
                        };
            }
            if (_always)
            {
                if (trace != null) { trace.Passed = true; Debugging.HfsmExecutionTrace.Commit(trace); }
                return true;
            }
#else
            if (_always) return true;
#endif
#if UNITY_EDITOR
            (_targetState?._context?.Debugger ?? _targetState?._stateMachine?._debugger)?.TransitionEvaluationBegin(_targetState._originSO != null ? _targetState._originSO.name : "State");
#endif

			int count = _resultGroups.Length;
			for (int i = 0, idx = 0; i < count && idx < _conditions.Length; i++)
				for (int j = 0; j < _resultGroups[i]; j++, idx++)
                {
                    // Preserve the original lazy AND and condition-cache semantics exactly.
                    if (j != 0 && !_results[i]) continue;
                    bool met = _conditions[idx].IsMet();
                    _results[i] = met;
#if UNITY_EDITOR
                    if (trace != null) trace.Checks[idx].Actual = met ?
                        _conditions[idx]._expectedResult : !_conditions[idx]._expectedResult;
#endif
                }

			bool ret = false;
			for (int i = 0; i < count && !ret; i++)
				ret = ret || _results[i];

#if UNITY_EDITOR
            if (trace != null) { trace.Passed = ret; Debugging.HfsmExecutionTrace.Commit(trace); }
            (_targetState?._context?.Debugger ?? _targetState?._stateMachine?._debugger)?.TransitionEvaluationEnd(ret, _targetState._actions);
#endif

			return ret;
		}
#if UNITY_EDITOR
        private static int TraceLevel(State state, StateMachineContext context)
        {
            int level = context.Scope?.Level ?? 1;
            for (var parent = state?.Parent; parent != null; parent = parent.Parent) level++;
            return level;
        }
#endif

		// Purpose: Clears the current conditions cache state.
		internal void ClearConditionsCache()
		{
			for (int i = 0; i < _conditions.Length; i++)
				_conditions[i]._condition.ClearStatementCache();
		}
	}
}
