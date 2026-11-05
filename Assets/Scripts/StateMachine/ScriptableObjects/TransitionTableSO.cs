/*
 * Role hierarchy: StateMachine > ScriptableObjects
 * Primary responsibility: Defines the TransitionTableSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (VSplatter.StateMachine)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VSplatter.StateMachine.ScriptableObjects
{
	[CreateAssetMenu(fileName = "NewTransitionTable", menuName = "State Machines/Transition Table")]
	public class TransitionTableSO : ScriptableObject
	{
		[SerializeField] private TransitionItem[] _transitions = default;
		[Tooltip("Optional. Unset preserves the first authored source as the entry.")]
		[SerializeField] private StateSO _initialState;
		[Tooltip("Optional native hierarchy. Empty preserves existing flat tables.")]
		[SerializeField] private StateHierarchyItem[] _hierarchy = Array.Empty<StateHierarchyItem>();
		public IReadOnlyList<TransitionItem> Transitions => _transitions ?? Array.Empty<TransitionItem>();
		/// <summary>작성 행 위치는 그대로 두고 공유 정의만 펼칩니다. 검사 순서를 새로 만들지 않습니다.</summary>
		public IEnumerable<TransitionItem> ResolvedTransitions
		{
			get { for (int i = 0; i < Transitions.Count; i++) yield return ResolveTransition(i); }
		}
		public TransitionItem ResolveTransition(int index)
		{
			var row = Transitions[index];
			if (!row.UseSharedRule) return row;
			if (row.SharedRule == null)
				throw new InvalidOperationException($"{name}: transition #{index + 1} has a missing shared rule. Restore the rule or explicitly detach it; stale inline data is not a fallback.");
			return row.SharedRule.Resolve(row);
		}
		public IReadOnlyList<StateHierarchyItem> Hierarchy => _hierarchy ?? Array.Empty<StateHierarchyItem>();
		public StateSO InitialState => _initialState != null ? _initialState : Transitions.Count > 0 ? Transitions[0].FromState : null;
		public bool HasExplicitInitialState => _initialState != null;
		internal State GetInitialState(StateMachine machine) => StateGraphCompiler.Compile(this, machine, machine?.Context, machine?.EntryOverride);

#if UNITY_EDITOR
		/// <summary>
		/// Will get the initial state and instantiate all subsequent states, transitions, actions and conditions.
		/// </summary>
		internal State CompileLegacyForCharacterization(StateMachine stateMachine)
		{
			var states = new List<State>();
			var transitions = new List<StateTransition>();
			var createdInstances = new Dictionary<ScriptableObject, object>();

			var fromStates = _transitions.GroupBy(transition => transition.FromState);

			foreach (var fromState in fromStates)
			{
				if (fromState.Key == null)
					throw new ArgumentNullException(nameof(fromState.Key), $"TransitionTable: {name}");

				var state = fromState.Key.GetState(stateMachine, createdInstances);
				states.Add(state);

				transitions.Clear();
				foreach (var transitionItem in fromState)
				{
					if (transitionItem.ToState == null)
						throw new ArgumentNullException(nameof(transitionItem.ToState), $"TransitionTable: {name}, From State: {fromState.Key.name}");

					var toState = transitionItem.ToState.GetState(stateMachine, createdInstances);
					ProcessConditionUsages(stateMachine, transitionItem.Conditions, createdInstances, out var conditions, out var resultGroups);
					transitions.Add(new StateTransition(toState, conditions, resultGroups));
				}

				state._transitions = transitions.ToArray();
			}

			return states.Count > 0 ? states[0]
				: throw new InvalidOperationException($"TransitionTable {name} is empty.");
		}

		// Purpose: Performs the process condition usages workflow for this component.
		private static void ProcessConditionUsages(
			StateMachine stateMachine,
			ConditionUsage[] conditionUsages,
			Dictionary<ScriptableObject, object> createdInstances,
			out StateCondition[] conditions,
			out int[] resultGroups)
		{
			int count = conditionUsages.Length;
			conditions = new StateCondition[count];
			for (int i = 0; i < count; i++)
				conditions[i] = conditionUsages[i].Condition.GetCondition(
					stateMachine, conditionUsages[i].ExpectedResult == Result.True, createdInstances);


			List<int> resultGroupsList = new List<int>();
			for (int i = 0; i < count; i++)
			{
				int idx = resultGroupsList.Count;
				resultGroupsList.Add(1);
				while (i < count - 1 && conditionUsages[i].Operator == Operator.And)
				{
					i++;
					resultGroupsList[idx]++;
				}
			}

			resultGroups = resultGroupsList.ToArray();
		}

#endif
		[Serializable]
		public struct StateHierarchyItem
		{
			public StateSO State;
			public StateSO Parent;
			public StateSO InitialChild;
		}

		[Serializable]
		public struct TransitionItem
		{
			public StateSO FromState;
			public StateSO ToState;
			public ConditionUsage[] Conditions;
			[Tooltip("Higher wins. Equal priorities retain authored row order.")]
			public int Priority;
			[Tooltip("Explicit unconditional transition. Empty legacy conditions remain false.")]
			public bool Always;
			public StateExitReason ExitReason;
			// 규칙의 내용만 공유합니다. FromState / Priority / 배열 위치는 이 사용처의 소유입니다.
			// 기존 직접 필드는 이전 스냅샷으로 보존하되, 공유 중에는 실행하거나 편집하지 않습니다.
			public bool UseSharedRule;
			public SharedTransitionRuleSO SharedRule;
		}

		[Serializable]
		public struct ConditionUsage
		{
			public Result ExpectedResult;
			public StateConditionSO Condition;
			public Operator Operator;
		}

		public enum Result { True, False }
		public enum Operator { And, Or }
	}
}
