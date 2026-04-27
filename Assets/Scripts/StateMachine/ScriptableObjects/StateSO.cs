using System.Collections.Generic;
using UnityEngine;

namespace VSplatter.StateMachine.ScriptableObjects
{
	[CreateAssetMenu(fileName = "New State", menuName = "State Machines/State")]
	public class StateSO : ScriptableObject
	{
		[SerializeField] private StateActionSO[] _actions = null;

		/// <summary>
		/// Will create a new state or return an existing one inside <paramref name="createdInstances"/>.
		/// </summary>
		internal State GetState(StateMachine stateMachine, Dictionary<ScriptableObject, object> createdInstances)
		{
			if (createdInstances.TryGetValue(this, out var obj))
				return (State)obj;

			var state = new State();
			createdInstances.Add(this, state);

			state._originSO = this;
			state._stateMachine = stateMachine;
			state._transitions = new StateTransition[0];
			state._actions = GetActions(_actions, stateMachine, createdInstances);

			return state;
		}

	private static StateAction[] GetActions(StateActionSO[] scriptableActions,
    StateMachine stateMachine, Dictionary<ScriptableObject, object> createdInstances)
	{
		if (scriptableActions == null || scriptableActions.Length == 0)
			return new StateAction[0];

		var validActions = new List<StateAction>();

		for (int i = 0; i < scriptableActions.Length; i++)
		{
			if (scriptableActions[i] == null)
			{
				//Debug.LogWarning($"State action is missing at index {i} on state machine asset.");
				continue;
			}

			validActions.Add(scriptableActions[i].GetAction(stateMachine, createdInstances));
		}

		return validActions.ToArray();
	}

	}
}
