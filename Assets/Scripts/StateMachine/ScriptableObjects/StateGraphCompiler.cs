using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VSplatter.StateMachine.ScriptableObjects
{
    /// <summary>SO compatibility adapter. Runtime instances/caches belong to one machine graph.</summary>
    internal static class StateGraphCompiler
    {
        public static State Compile(TransitionTableSO table, StateMachine machine, StateMachineContext context = null, StateSO entry = null)
        {
            var instances = new Dictionary<ScriptableObject, object>();
            State Resolve(StateSO definition)
            {
                if (definition == null) throw new InvalidOperationException($"Missing state in transition table {table.name}.");
                return definition.GetState(machine, instances, context ?? machine?.Context);
            }
            // Preserve creation/Awake order, even when a new graph uses explicit priorities.
            // A shared rule expands IN PLACE, before the unchanged legacy compilation loop.
            // No global selector, extra state, eager condition evaluation or shared actor cache.
            foreach (var group in table.ResolvedTransitions.Select((row, index) => (row, index)).GroupBy(pair => pair.row.FromState))
            {
                var state = Resolve(group.Key);
                var outgoing = new List<(StateTransition transition, int priority)>();
                foreach (var pair in group)
                {
                    var row = pair.row;
                    var target = Resolve(row.ToState);
                    BuildConditions(machine, row.Conditions, instances, context, out var conditions, out var groups);
                    var transition = new StateTransition(target, conditions, groups, row.Always, row.ExitReason);
#if UNITY_EDITOR
                    transition.TraceSource = group.Key.name;
                    transition.TraceSourceState = state;
                    transition.TraceTable = table.name;
                    transition.TraceRow = pair.index + 1;
                    transition.TracePriority = row.Priority;
                    var authored = table.Transitions[pair.index];
                    transition.TraceRule = authored.UseSharedRule ? authored.SharedRule?.name : null;
#endif
                    outgoing.Add((transition, row.Priority));
                }
                state._transitions = outgoing.OrderByDescending(pair => pair.priority).Select(pair => pair.transition).ToArray();
            }
            var defined = new HashSet<StateSO>();
            foreach (var relation in table.Hierarchy)
            {
                if (!defined.Add(relation.State)) throw new InvalidOperationException($"Duplicate hierarchy node in {table.name}.");
                Resolve(relation.State).SetParent(relation.Parent != null ? Resolve(relation.Parent) : null);
            }
            foreach (var relation in table.Hierarchy)
                Resolve(relation.State).SetInitialChild(relation.InitialChild != null ? Resolve(relation.InitialChild) : null);
            return Resolve(entry != null ? entry : table.InitialState);
        }

        private static void BuildConditions(StateMachine machine, TransitionTableSO.ConditionUsage[] usages,
            Dictionary<ScriptableObject, object> instances, StateMachineContext context, out StateCondition[] conditions, out int[] groups)
        {
            usages ??= Array.Empty<TransitionTableSO.ConditionUsage>();
            conditions = new StateCondition[usages.Length];
            for (int i = 0; i < usages.Length; i++)
            {
                if (usages[i].Condition == null) throw new InvalidOperationException("Transition has a missing condition.");
                conditions[i] = usages[i].Condition.GetCondition(machine, usages[i].ExpectedResult == TransitionTableSO.Result.True, instances, context);
            }
            var sizes = new List<int>();
            for (int i = 0; i < usages.Length; i++)
            {
                int count = 1;
                while (i < usages.Length - 1 && usages[i].Operator == TransitionTableSO.Operator.And) { i++; count++; }
                sizes.Add(count);
            }
            groups = sizes.ToArray();
        }
    }
}
