/*
 * Role hierarchy: StateMachine > Core
 * Primary responsibility: Defines State state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (VSplatter.StateMachine)
 */

using VSplatter.StateMachine.ScriptableObjects;

namespace VSplatter.StateMachine
{
	public class State : HfsmNode
	{
		internal StateSO _originSO;
		internal StateMachine _stateMachine;
        internal StateMachineContext _context;
		internal StateTransition[] _transitions;
		internal StateAction[] _actions;
		private StateExitReason _selectedExitReason;

		public override string Name => _originSO != null ? _originSO.name : "State";
		public StateSO Definition => _originSO;
		public override void Enter() => OnStateEnter();
		public override void Tick(float deltaTime) => OnUpdate();
		public override void Exit(StateExitReason reason)
		{
			if (_transitions != null) foreach (var transition in _transitions) transition?.OnStateExit();
			if (_actions != null) foreach (var action in _actions) action?.OnStateExit(reason);
		}
		public override bool TryTransition(out HfsmTransition transition)
		{
			bool found = TryGetTransition(out var target);
			transition = new HfsmTransition(target, _selectedExitReason);
			return found;
		}

		internal State() { }

		public State(
			StateSO originSO,
			StateMachine stateMachine,
			StateTransition[] transitions,
			StateAction[] actions)
		{
			_originSO = originSO;
			_stateMachine = stateMachine;
			_transitions = transitions;
			_actions = actions;
		}

		// Purpose: Responds to the state enter callback.
		public void OnStateEnter()
		{
			void OnStateEnter(IStateComponent[] comps)
			{
				for (int i = 0; i < comps.Length; i++)
					comps[i].OnStateEnter();
			}
			OnStateEnter(_transitions);
			OnStateEnter(_actions);
		}

		// Purpose: Responds to the update callback.
		public void OnUpdate()
		{
			if (_actions == null)
				return;

			for (int i = 0; i < _actions.Length; i++)
				_actions[i]?.OnUpdate();
		}

		// Purpose: Responds to the state exit callback.
		public void OnStateExit()
		{
			void OnStateExit(IStateComponent[] comps)
			{
				for (int i = 0; i < comps.Length; i++)
					comps[i].OnStateExit();
			}
			OnStateExit(_transitions);
			OnStateExit(_actions);
		}

		// Purpose: Attempts to get transition and reports whether the request succeeded.
		public bool TryGetTransition(out State state)
		{
			state = null;

			if (_transitions == null)
				return false;

			for (int i = 0; i < _transitions.Length; i++)
			{
				if (_transitions[i] != null && _transitions[i].TryGetTransiton(out state))
				{
					_selectedExitReason = _transitions[i].ExitReason;
					break;
				}
			}

			for (int i = 0; i < _transitions.Length; i++)
				_transitions[i]?.ClearConditionsCache();

			return state != null;
		}
	}
}
