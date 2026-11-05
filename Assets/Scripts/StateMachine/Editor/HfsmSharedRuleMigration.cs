using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using VSplatter.StateMachine.ScriptableObjects;

namespace VSplatter.StateMachine.Editor
{
    /// <summary>명시적으로 호출하는 제한된 이전. 자동 실행/메뉴 없음. 동일 정의만 참조로 바꿉니다.</summary>
    public static class HfsmSharedRuleMigration
    {
        public static List<int[]> RepeatedInlineGroups(TransitionTableSO table)
        {
            var used = new HashSet<int>(); var result = new List<int[]>();
            for (int i = 0; i < table.Transitions.Count; i++)
            {
                if (used.Contains(i) || table.Transitions[i].UseSharedRule) continue;
                var matches = HfsmSharedRuleAuthoring.MatchingInlineRows(table, i);
                foreach (int index in matches) used.Add(index);
                if (matches.Length > 1) result.Add(matches);
            }
            return result;
        }

        public static string SemanticSignature(TransitionTableSO table)
        {
            int Id(UnityEngine.Object o) => o != null ? o.GetInstanceID() : 0;
            var text = new StringBuilder().Append("entry=").Append(Id(table.InitialState)).Append(" explicit=").Append(table.HasExplicitInitialState);
            for (int i = 0; i < table.Transitions.Count; i++)
            {
                var r = table.ResolveTransition(i);
                text.Append('\n').Append(i).Append(':').Append(Id(r.FromState)).Append('>').Append(Id(r.ToState))
                    .Append('|').Append(r.Priority).Append('|').Append(r.Always).Append('|').Append((int)r.ExitReason);
                foreach (var c in r.Conditions ?? Array.Empty<TransitionTableSO.ConditionUsage>())
                    text.Append('|').Append(Id(c.Condition)).Append(',').Append((int)c.ExpectedResult).Append(',').Append((int)c.Operator);
            }
            foreach (var h in table.Hierarchy) text.Append("\nh:").Append(Id(h.State)).Append(',').Append(Id(h.Parent)).Append(',').Append(Id(h.InitialChild));
            return text.ToString();
        }

        public static TransitionTableSO Preview(TransitionTableSO source, List<UnityEngine.Object> owned)
        {
            var clone = UnityEngine.Object.Instantiate(source); clone.name = source.name + " (memory preview)"; owned.Add(clone);
            foreach (var group in RepeatedInlineGroups(clone))
            {
                var rule = ScriptableObject.CreateInstance<SharedTransitionRuleSO>(); rule.name = Title(clone.ResolveTransition(group[0]).ToState);
                rule.InitializeForAuthoring(clone.ResolveTransition(group[0])); owned.Add(rule);
                var so = new SerializedObject(clone); so.Update();
                foreach (int i in group)
                {
                    var row = so.FindProperty("_transitions").GetArrayElementAtIndex(i);
                    row.FindPropertyRelative("UseSharedRule").boolValue = true;
                    row.FindPropertyRelative("SharedRule").objectReferenceValue = rule;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            if (SemanticSignature(source) != SemanticSignature(clone)) throw new InvalidOperationException("Preview changed the resolved execution data.");
            return clone;
        }

        public static string[] ConsolidateSavedTable(TransitionTableSO table, string backupDirectory)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Stop Play Mode and leave Prefab Mode before migration.");
            if (table == null || EditorUtility.IsDirty(table)) throw new InvalidOperationException("Save the transition table before migration; unsaved authoring will not be overwritten.");
            string path = AssetDatabase.GetAssetPath(table);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) throw new InvalidOperationException("A saved table asset is required.");
            var groups = RepeatedInlineGroups(table); if (groups.Count == 0) return Array.Empty<string>();
            var owned = new List<UnityEngine.Object>();
            try { Preview(table, owned); }
            finally { foreach (var o in owned) UnityEngine.Object.DestroyImmediate(o); }

            Directory.CreateDirectory(backupDirectory);
            // New timestamped backup, never overwrite a previous migration snapshot.
            string backup = Path.Combine(backupDirectory, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Path.GetFileName(path));
            File.Copy(path, backup, false); File.Copy(path + ".meta", backup + ".meta", false);
            string signature = SemanticSignature(table), json = EditorJsonUtility.ToJson(table);
            string folder = Path.GetDirectoryName(path).Replace('\\', '/') + "/SharedRules";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), "SharedRules");
            var created = new List<string>();
            try
            {
                foreach (var group in groups)
                {
                    var destination = table.ResolveTransition(group[0]).ToState;
                    string rulePath = AssetDatabase.GenerateUniqueAssetPath(folder + "/Shared_" + destination.name + ".asset");
                    var rule = HfsmSharedRuleAuthoring.Create(table, group, rulePath); created.Add(rulePath);
                    rule.SetDisplayNameForAuthoring(Title(destination)); EditorUtility.SetDirty(rule); AssetDatabase.SaveAssetIfDirty(rule);
                }
                if (signature != SemanticSignature(table)) throw new InvalidOperationException("Resolved row/condition/priority/hierarchy signature changed.");
                AssetDatabase.SaveAssetIfDirty(table); File.WriteAllText(backup + ".resolved.txt", signature);
                return created.ToArray();
            }
            catch
            {
                EditorJsonUtility.FromJsonOverwrite(json, table); EditorUtility.SetDirty(table); AssetDatabase.SaveAssetIfDirty(table);
                // These are exact paths created by this operation, never pre-existing assets.
                foreach (string createdPath in created) AssetDatabase.DeleteAsset(createdPath);
                throw;
            }
        }

        private static string Title(StateSO state)
        {
            string name = state != null ? state.name : "전환";
            if (name.Contains("Melee")) return "근접 공격 선택";
            if (name.Contains("Throwable")) return "투척 공격 선택";
            if (name.Contains("AttackTarget")) return "사격 선택";
            if (name.Contains("ChaseTarget")) return "추적 선택";
            if (name.Contains("Investigate")) return "조사 선택";
            if (name.Contains("Patrol")) return "순찰 복귀";
            return name + " 선택";
        }
    }
}
