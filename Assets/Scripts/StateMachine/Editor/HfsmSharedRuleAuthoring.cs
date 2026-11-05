using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VSplatter.StateMachine.ScriptableObjects;

namespace VSplatter.StateMachine.Editor
{
    /// <summary>규칙 생성/연결/해제의 소유자. 행을 추가하거나 정렬하지 않고 현재 위치에서 정의만 바꿉니다.</summary>
    public static class HfsmSharedRuleAuthoring
    {
        public static bool SameDefinition(TransitionTableSO.TransitionItem a, TransitionTableSO.TransitionItem b)
        {
            if (a.ToState != b.ToState || a.Always != b.Always || a.ExitReason != b.ExitReason) return false;
            var x = a.Conditions ?? Array.Empty<TransitionTableSO.ConditionUsage>();
            var y = b.Conditions ?? Array.Empty<TransitionTableSO.ConditionUsage>();
            if (x.Length != y.Length) return false;
            for (int i = 0; i < x.Length; i++)
                if (x[i].Condition != y[i].Condition || x[i].ExpectedResult != y[i].ExpectedResult || x[i].Operator != y[i].Operator) return false;
            return true; // Source and priority are intentionally NOT part of the shared payload.
        }

        public static int[] MatchingInlineRows(TransitionTableSO table, int seed)
        {
            var definition = table.ResolveTransition(seed);
            return Enumerable.Range(0, table.Transitions.Count).Where(i => !table.Transitions[i].UseSharedRule
                && SameDefinition(definition, table.ResolveTransition(i))).ToArray();
        }

        public static SharedTransitionRuleSO Create(TransitionTableSO table, int[] rows, string path)
        {
            RequireEdit();
            if (rows == null || rows.Length == 0 || string.IsNullOrEmpty(path)) throw new ArgumentException("Choose usages and a new asset path.");
            if (AssetDatabase.LoadMainAssetAtPath(path) != null || System.IO.File.Exists(path)) throw new InvalidOperationException("Refusing to overwrite " + path);
            var definition = table.ResolveTransition(rows[0]);
            if (rows.Any(i => !SameDefinition(definition, table.ResolveTransition(i)))) throw new InvalidOperationException("Only identical definitions can be consolidated without a behavior change.");
            var rule = ScriptableObject.CreateInstance<SharedTransitionRuleSO>();
            rule.InitializeForAuthoring(definition);
            AssetDatabase.CreateAsset(rule, path);
            Undo.RegisterCreatedObjectUndo(rule, "Create shared HFSM rule");
            Bind(table, rows, rule);
            return rule;
        }

        public static void Bind(TransitionTableSO table, IEnumerable<int> rows, SharedTransitionRuleSO rule)
        {
            RequireEdit();
            if (table == null || rule == null || rule.ToState == null) throw new ArgumentException("A valid table and rule destination are required.");
            int[] slots = rows.Distinct().ToArray();
            if (slots.Any(i => i < 0 || i >= table.Transitions.Count)) throw new ArgumentOutOfRangeException(nameof(rows));
            bool persistent = EditorUtility.IsPersistent(table);
            if (persistent) Undo.RecordObject(table, "Use shared HFSM rule");
            var so = new SerializedObject(table); so.Update();
            foreach (int index in slots)
            {
                var row = so.FindProperty("_transitions").GetArrayElementAtIndex(index);
                row.FindPropertyRelative("SharedRule").objectReferenceValue = rule;
                row.FindPropertyRelative("UseSharedRule").boolValue = true;
            }
            ApplyAuthoringChanges(so, table, persistent);
        }

        public static void Detach(TransitionTableSO table, int index)
        {
            RequireEdit();
            var resolved = table.ResolveTransition(index); // Missing reference is an error, never a silent fallback.
            bool persistent = EditorUtility.IsPersistent(table);
            if (persistent) Undo.RecordObject(table, "Copy shared HFSM rule into local transition");
            var so = new SerializedObject(table); so.Update();
            WriteInline(so.FindProperty("_transitions").GetArrayElementAtIndex(index), resolved);
            ApplyAuthoringChanges(so, table, persistent);
        }

        private static void ApplyAuthoringChanges(SerializedObject serialized, TransitionTableSO table, bool persistent)
        {
            // 실제 에셋 편집에는 Undo/저장 표시를 유지합니다. 미리보기·검사용 메모리 SO는
            // 전역 Undo에 등록하지 않아 관계없는 열린 씬에 변경 표시가 붙지 않게 합니다.
            if (persistent) { serialized.ApplyModifiedProperties(); EditorUtility.SetDirty(table); }
            else serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void WriteInline(SerializedProperty row, TransitionTableSO.TransitionItem definition)
        {
            row.FindPropertyRelative("ToState").objectReferenceValue = definition.ToState;
            row.FindPropertyRelative("Always").boolValue = definition.Always;
            row.FindPropertyRelative("ExitReason").enumValueIndex = (int)definition.ExitReason;
            var conditions = row.FindPropertyRelative("Conditions");
            conditions.arraySize = definition.Conditions?.Length ?? 0;
            for (int i = 0; i < conditions.arraySize; i++)
            {
                var usage = conditions.GetArrayElementAtIndex(i); var source = definition.Conditions[i];
                usage.FindPropertyRelative("Condition").objectReferenceValue = source.Condition;
                usage.FindPropertyRelative("ExpectedResult").enumValueIndex = (int)source.ExpectedResult;
                usage.FindPropertyRelative("Operator").enumValueIndex = (int)source.Operator;
            }
            row.FindPropertyRelative("UseSharedRule").boolValue = false;
            row.FindPropertyRelative("SharedRule").objectReferenceValue = null;
        }

        private static void RequireEdit()
        { if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before editing shared rules."); }
    }
}
