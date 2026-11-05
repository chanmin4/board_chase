using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VSplatter.StateMachine.ScriptableObjects;

namespace VSplatter.StateMachine.Editor
{
    /// <summary>All graph mutations pass here; rendering never writes runtime definitions.</summary>
    public static class HfsmGraphAuthoring
    {
        public static void SetEntry(TransitionTableSO table, StateSO state)
        { Edit(table, "Set HFSM entry", so => so.FindProperty("_initialState").objectReferenceValue = state); }

        public static int AddTransition(TransitionTableSO table, StateSO from, StateSO to)
        {
            if (table == null || from == null || to == null) throw new ArgumentException("A table, source and destination are required.");
            int result = -1;
            Edit(table, "Add HFSM transition", so =>
            {
                PreserveEntry(so, table);
                var rows = so.FindProperty("_transitions"); result = rows.arraySize++;
                var row = rows.GetArrayElementAtIndex(result);
                row.FindPropertyRelative("FromState").objectReferenceValue = from;
                row.FindPropertyRelative("ToState").objectReferenceValue = to;
                row.FindPropertyRelative("Conditions").ClearArray();
                row.FindPropertyRelative("Priority").intValue = 0;
                row.FindPropertyRelative("Always").boolValue = false;
                row.FindPropertyRelative("ExitReason").enumValueIndex = 0;
                row.FindPropertyRelative("UseSharedRule").boolValue = false;
                row.FindPropertyRelative("SharedRule").objectReferenceValue = null;
            });
            return result;
        }

        public static void DeleteTransition(TransitionTableSO table, int index)
        {
            RequireRow(table, index);
            Edit(table, "Delete HFSM transition", so => { PreserveEntry(so, table); so.FindProperty("_transitions").DeleteArrayElementAtIndex(index); });
        }

        public static void Reconnect(TransitionTableSO table, int index, StateSO target)
        {
            RequireRow(table, index); if (target == null) throw new ArgumentNullException(nameof(target));
            if (table.Transitions[index].UseSharedRule) HfsmSharedRuleAuthoring.Detach(table, index);
            Edit(table, "Reconnect HFSM transition", so => so.FindProperty("_transitions").GetArrayElementAtIndex(index).FindPropertyRelative("ToState").objectReferenceValue = target);
        }

        public static void RegisterState(TransitionTableSO table, StateSO state, StateSO parent = null)
        {
            if (state == null || table == null || state == parent) throw new ArgumentException("Invalid state/parent.");
            var seen = new HashSet<StateSO>();
            for (var current = parent; current != null; current = table.Hierarchy.FirstOrDefault(h => h.State == current).Parent)
                if (current == state || !seen.Add(current)) throw new InvalidOperationException("Cannot create a hierarchy cycle.");
            Edit(table, "Register HFSM state", so =>
            {
                PreserveEntry(so, table);
                if (table.InitialState == null) so.FindProperty("_initialState").objectReferenceValue = state;
                var hierarchy = so.FindProperty("_hierarchy"); int index = -1;
                for (int i = 0; i < hierarchy.arraySize; i++)
                    if (hierarchy.GetArrayElementAtIndex(i).FindPropertyRelative("State").objectReferenceValue == state) { index = i; break; }
                bool added = index < 0;
                if (added) index = hierarchy.arraySize++;
                var row = hierarchy.GetArrayElementAtIndex(index);
                row.FindPropertyRelative("State").objectReferenceValue = state;
                row.FindPropertyRelative("Parent").objectReferenceValue = parent;
                if (added) row.FindPropertyRelative("InitialChild").objectReferenceValue = null;
                if (parent != null)
                {
                    int parentIndex = -1;
                    for (int i = 0; i < hierarchy.arraySize; i++)
                        if (hierarchy.GetArrayElementAtIndex(i).FindPropertyRelative("State").objectReferenceValue == parent) { parentIndex = i; break; }
                    if (parentIndex < 0)
                    {
                        parentIndex = hierarchy.arraySize++;
                        var addedParent = hierarchy.GetArrayElementAtIndex(parentIndex);
                        addedParent.FindPropertyRelative("State").objectReferenceValue = parent;
                        addedParent.FindPropertyRelative("Parent").objectReferenceValue = null;
                        addedParent.FindPropertyRelative("InitialChild").objectReferenceValue = null;
                    }
                    var initial = hierarchy.GetArrayElementAtIndex(parentIndex).FindPropertyRelative("InitialChild");
                    if (initial.objectReferenceValue == null) initial.objectReferenceValue = state;
                }
            });
        }

        public static List<string> Validate(TransitionTableSO table)
        {
            var issues = new List<string>();
            if (table == null) { issues.Add("Choose a transition table."); return issues; }
            if (table.InitialState == null) issues.Add("No entry state.");
            var registered = new HashSet<StateSO>();
            foreach (var h in table.Hierarchy)
            {
                if (h.State == null || !registered.Add(h.State)) issues.Add("Missing or duplicate hierarchy state.");
                var seen = new HashSet<StateSO>();
                for (var current = h.State; current != null; current = table.Hierarchy.FirstOrDefault(r => r.State == current).Parent)
                    if (!seen.Add(current)) { issues.Add("Parent cycle: " + current.name); break; }
                if (h.InitialChild != null && table.Hierarchy.FirstOrDefault(r => r.State == h.InitialChild).Parent != h.State) issues.Add("Initial child does not belong to " + h.State.name);
            }
            for (int i = 0; i < table.Transitions.Count; i++)
            {
                TransitionTableSO.TransitionItem row;
                try { row = table.ResolveTransition(i); }
                catch (InvalidOperationException ex) { issues.Add(ex.Message); continue; }
                string label = "Transition " + (i + 1) + ": ";
                if (row.FromState == null || row.ToState == null) issues.Add(label + "missing endpoint.");
                if (!row.Always && (row.Conditions == null || row.Conditions.Length == 0)) issues.Add(label + "no conditions; will not fire.");
                if (row.Conditions != null && row.Conditions.Any(c => c.Condition == null)) issues.Add(label + "missing condition.");
            }
            return issues;
        }

        private static void RequireRow(TransitionTableSO table, int index)
        { if (table == null || index < 0 || index >= table.Transitions.Count) throw new ArgumentOutOfRangeException(nameof(index)); }
        private static void PreserveEntry(SerializedObject so, TransitionTableSO table)
        { if (!table.HasExplicitInitialState && table.InitialState != null) so.FindProperty("_initialState").objectReferenceValue = table.InitialState; }
        private static void Edit(TransitionTableSO table, string label, Action<SerializedObject> edit)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before authoring HFSM assets.");
            Undo.RecordObject(table, label);
            var so = new SerializedObject(table); so.Update(); edit(so); so.ApplyModifiedProperties(); EditorUtility.SetDirty(table);
        }
    }
}
