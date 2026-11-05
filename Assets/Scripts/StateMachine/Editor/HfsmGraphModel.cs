using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VSplatter.StateMachine.ScriptableObjects;

namespace VSplatter.StateMachine.Editor
{
    /// <summary>Read-only projection of SO definitions and actual prefab coordinator bindings.</summary>
    internal sealed class HfsmGraphModel
    {
        internal sealed class Scope
        {
            public string Key, Label;
            public int Level = 1;
            public Node OwnerNode;
            public TransitionTableSO Table;
            public StateMachine Machine;
            public HfsmDefinitionSO Definition;
            public int DefinitionScope = -1;
            public StateSO NativeEntry;
            public IHfsmScope Execution => DefinitionScope >= 0 && Machine is HfsmStateMachine host
                ? DefinitionScope < host.ScopeCount ? host.GetScope(DefinitionScope) : null : Machine;
            public StateSO Entry => NativeEntry != null ? NativeEntry : Definition == null || DefinitionScope < 0 ? Table?.InitialState
                : DefinitionScope == 0 ? Definition.Entry
                : Definition.Scopes[DefinitionScope - 1].Entry != null ? Definition.Scopes[DefinitionScope - 1].Entry : Table?.InitialState;
            public readonly List<Node> Nodes = new();
            public readonly List<Edge> Edges = new();
            public Rect Bounds;
        }
        internal sealed class Node
        {
            public StateSO State;
            public Scope Scope;
            public string Key;
            public int Level = 1;
            public readonly List<Scope> Children = new();
            public Rect Bounds;
            public bool Expanded;
            public bool IsComposite => Children.Count > 0;
            public bool IsActive => Application.isPlaying && Scope.Execution != null && Scope.Execution.IsScopeActive
                && Scope.Execution.Runtime.ActivePath.Any(s => s is State runtime && runtime.Definition == State);
        }
        internal sealed class Edge
        {
            public Scope Scope;
            public Node From, To;
            public int Row;
            public SharedTransitionRuleSO Rule => Scope.Table.Transitions[Row].UseSharedRule ? Scope.Table.Transitions[Row].SharedRule : null;
        }

        public Scope Root { get; private set; }
        public readonly List<string> Warnings = new();
        public readonly List<StateMachineChildBinding> Bindings = new();
        private HfsmDefinitionSO _definitionView;

        public void Build(TransitionTableSO table, StateMachine machine, GameObject owner)
            => BuildDefinition(table, machine, owner, (machine as HfsmStateMachine)?.Definition);

        public void BuildDefinition(TransitionTableSO table, StateMachine machine, GameObject owner, HfsmDefinitionSO definition)
        {
            Bindings.Clear(); Warnings.Clear();
            _definitionView = definition != null ? definition : (machine as HfsmStateMachine)?.Definition;
            if (_definitionView != null) Warnings.AddRange(_definitionView.Validate());
            if (owner != null)
                foreach (var source in owner.GetComponentsInChildren<MonoBehaviour>(true).OfType<IStateMachineHierarchySource>()) source.GetHierarchyBindings(Bindings);
            Root = BuildScope(table, machine, "root", new HashSet<StateMachine>(), 0, _definitionView != null ? 0 : -1);
            AssignLevels(Root, LevelForMachine(machine));
        }

        // Display metadata only. A directly selected child machine keeps its actual level
        // in the owner's coordinator graph; it is not relabelled as level 1.
        internal int LevelForMachine(StateMachine machine)
            => MachineLevel(machine, new HashSet<StateMachine>());

        private int MachineLevel(StateMachine machine, HashSet<StateMachine> visited)
        {
            if (machine == null || !visited.Add(machine)) return 1;
            var parents = Bindings.Where(b => b.Enabled && b.Child == machine && b.Parent != null && b.ParentState != null).ToArray();
            if (parents.Length == 0) return 1;
            return parents.Min(b => MachineLevel(b.Parent, new HashSet<StateMachine>(visited)) + LocalStateLevel(b.Parent.Table, b.ParentState));
        }

        private static int LocalStateLevel(TransitionTableSO table, StateSO state)
        {
            int level = 1;
            var visited = new HashSet<StateSO>();
            while (table != null && state != null && visited.Add(state))
            {
                state = table.Hierarchy.FirstOrDefault(h => h.State == state).Parent;
                if (state != null) level++;
            }
            return level;
        }

        private static void AssignLevels(Scope scope, int level, Node owner = null)
        {
            scope.Level = level; scope.OwnerNode = owner;
            foreach (var node in scope.Nodes)
            {
                node.Level = level;
                foreach (var child in node.Children) AssignLevels(child, level + 1, node);
            }
        }

        private Scope BuildScope(TransitionTableSO table, StateMachine machine, string key, HashSet<StateMachine> ancestors, int depth, int definitionScope = -1)
        {
            var scope = new Scope { Table = table, Machine = machine, Key = key, Label = table != null ? table.name : "Missing table", Definition = _definitionView, DefinitionScope = definitionScope };
            if (table == null || depth > 12) { Warnings.Add(scope.Label + ": missing table or excessive hierarchy depth."); return scope; }
            if (definitionScope < 0 && machine != null && !ancestors.Add(machine)) { Warnings.Add("Coordinator cycle: " + machine.name); return scope; }
            var states = new List<StateSO>();
            void Add(StateSO state) { if (state != null && !states.Contains(state)) states.Add(state); }
            Add(table.InitialState);
            var resolved = new Dictionary<int, TransitionTableSO.TransitionItem>();
            for (int i = 0; i < table.Transitions.Count; i++)
            {
                try { var row = table.ResolveTransition(i); resolved.Add(i, row); Add(row.FromState); Add(row.ToState); }
                catch (InvalidOperationException ex) { Warnings.Add(ex.Message); }
            }
            foreach (var relation in table.Hierarchy) { Add(relation.State); Add(relation.Parent); Add(relation.InitialChild); }
            var nodes = new Dictionary<StateSO, Node>();
            foreach (var state in states)
            {
                string stateKey = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(state));
                if (string.IsNullOrEmpty(stateKey)) stateKey = "memory-" + state.GetInstanceID();
                var node = new Node { State = state, Scope = scope, Key = key + "/" + stateKey };
                node.Expanded = EditorPrefs.GetBool(LayoutKey(node) + ".expanded", depth == 0);
                nodes.Add(state, node); scope.Nodes.Add(node);
            }
            for (int i = 0; i < table.Transitions.Count; i++)
            {
                if (!resolved.TryGetValue(i, out var row)) continue;
                if (row.FromState == null || row.ToState == null) { Warnings.Add(table.name + ": incomplete transition " + (i + 1)); continue; }
                scope.Edges.Add(new Edge { Scope = scope, From = nodes[row.FromState], To = nodes[row.ToState], Row = i });
                if (!row.Always && (row.Conditions == null || row.Conditions.Length == 0)) Warnings.Add(table.name + ": transition " + (i + 1) + " has no condition and will not fire (legacy rule).");
            }

            // Native SO hierarchy is represented by nested scopes using the SAME table/row identity.
            var nativeScopes = new Dictionary<StateSO, Scope>();
            foreach (var relation in table.Hierarchy)
            {
                if (relation.Parent == null || relation.State == null) continue;
                if (IsParentCycle(table, relation.State)) { Warnings.Add("Native hierarchy cycle: " + relation.State.name); continue; }
                if (!nativeScopes.TryGetValue(relation.Parent, out var nested))
                {
                    nested = new Scope { Table = table, Machine = machine, Key = nodes[relation.Parent].Key + "/children", Label = relation.Parent.name, Definition = _definitionView, DefinitionScope = definitionScope,
                        NativeEntry = table.Hierarchy.FirstOrDefault(h => h.State == relation.Parent).InitialChild };
                    nativeScopes.Add(relation.Parent, nested); nodes[relation.Parent].Children.Add(nested);
                }
                var child = nodes[relation.State]; scope.Nodes.Remove(child); nested.Nodes.Add(child);
            }
            foreach (var nested in nativeScopes.Values)
                nested.Edges.AddRange(scope.Edges.Where(e => nested.Nodes.Contains(e.From) && nested.Nodes.Contains(e.To)));
            foreach (var edge in nativeScopes.Values.SelectMany(s => s.Edges)) scope.Edges.Remove(edge);

            foreach (var node in nodes.Values)
            {
                if (definitionScope >= 0)
                {
                    for (int i = 0; i < _definitionView.Scopes.Count; i++)
                    {
                        var binding = _definitionView.Scopes[i];
                        if (binding.OwnerScope != definitionScope || binding.OwnerState != node.State) continue;
                        var nested = BuildScope(binding.Table, machine, node.Key + "/scope-" + (i + 1), ancestors, depth + 1, i + 1);
                        if (!string.IsNullOrWhiteSpace(binding.Label)) nested.Label = binding.Label;
                        node.Children.Add(nested);
                    }
                    continue;
                }
                var children = Bindings.Where(b => b.Parent == machine && b.ParentState == node.State && b.Enabled && b.Child != null).Select(b => b.Child).Distinct().ToArray();
                foreach (var child in children)
                {
                    string childKey = node.Key + "/" + MachineKey(child);
                    node.Children.Add(BuildScope(child.Table, child, childKey, new HashSet<StateMachine>(ancestors), depth + 1));
                }
            }
            return scope;
        }

        private static bool IsParentCycle(TransitionTableSO table, StateSO state)
        {
            var seen = new HashSet<StateSO>();
            for (var current = state; current != null;)
            {
                if (!seen.Add(current)) return true;
                current = table.Hierarchy.FirstOrDefault(h => h.State == current).Parent;
            }
            return false;
        }

        internal static string MachineKey(StateMachine machine)
        {
            var names = new List<string>();
            for (var t = machine.transform; t != null; t = t.parent) names.Add(t.name + ":" + t.GetSiblingIndex());
            names.Reverse();
            return string.Join("/", names) + ":" + Array.IndexOf(machine.GetComponents<StateMachine>(), machine);
        }

        public static IEnumerable<Node> AllNodes(Scope scope)
        {
            if (scope == null) yield break;
            foreach (var node in scope.Nodes) { yield return node; foreach (var child in node.Children) foreach (var nested in AllNodes(child)) yield return nested; }
        }

        public static IEnumerable<Scope> AllScopes(Scope scope)
        {
            if (scope == null) yield break;
            yield return scope;
            foreach (var child in scope.Nodes.SelectMany(n => n.Children))
                foreach (var nested in AllScopes(child)) yield return nested;
        }

        public static string LayoutKey(Node node) => "HFSM.Layout.v2." + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(node.Scope.Table)) + "." + node.Key;
        public static void SetExpanded(Node node, bool expanded)
        { node.Expanded = expanded; EditorPrefs.SetBool(LayoutKey(node) + ".expanded", expanded); }
    }
}
