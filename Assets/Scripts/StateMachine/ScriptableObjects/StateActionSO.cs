/*
 * Role hierarchy: StateMachine > ScriptableObjects
 * Primary responsibility: Defines StateActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (VSplatter.StateMachine)
 */

using System.Collections.Generic;
using UnityEngine;

namespace VSplatter.StateMachine.ScriptableObjects
{
	public abstract class StateActionSO : DescriptionSMActionBaseSO
	{
		/// <summary>
		/// Will create a new custom <see cref="StateAction"/> or return an existing one inside <paramref name="createdInstances"/>
		/// </summary>
		internal StateAction GetAction(StateMachine stateMachine, Dictionary<ScriptableObject, object> createdInstances, StateMachineContext context = null)
		{
			if (createdInstances.TryGetValue(this, out var obj))
				return (StateAction)obj;

			var action = CreateAction();
			createdInstances.Add(this, action);
			action._originSO = this;
            action.Context = context ?? stateMachine?.Context;
			action.Awake(stateMachine);
			return action;
		}
		// Purpose: Creates the action owned by this workflow.
		protected abstract StateAction CreateAction();
	}

	public abstract class StateActionSO<T> : StateActionSO where T : StateAction, new()
	{
		// Purpose: Creates the action owned by this workflow.
		protected override StateAction CreateAction() => new T();
	}
}
