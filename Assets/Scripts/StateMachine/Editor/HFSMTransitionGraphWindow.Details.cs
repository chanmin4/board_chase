using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VSplatter.StateMachine.ScriptableObjects;

namespace VSplatter.StateMachine.Editor
{
    internal sealed partial class HFSMTransitionGraphWindow
    {
        private float _detailWidth = 250;
        private GUIStyle _wrappedText, _wrappedHeading, _wrappedButton;
        private readonly HashSet<string> _expandedTransitionConditions = new();

        private void EnsureDetailStyles()
        {
            // Built-in miniButton has a fixed single-line height. WordWrap alone is insufficient.
            _wrappedText ??= new GUIStyle(EditorStyles.wordWrappedLabel) { fixedHeight = 0, wordWrap = true };
            _wrappedHeading ??= new GUIStyle(_wrappedText) { fontStyle = FontStyle.Bold };
            _wrappedButton ??= new GUIStyle(EditorStyles.miniButton)
            {
                fixedHeight = 0, wordWrap = true, alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(6, 6, 6, 6)
            };
            // Do not retain a stale light-skin text color after an editor reload/theme change.
            Color text = EditorGUIUtility.isProSkin ? HfsmGraphPresentation.CardText : new Color(.12f, .12f, .12f);
            HfsmGraphPresentation.SetTextColor(_wrappedText, text);
            HfsmGraphPresentation.SetTextColor(_wrappedHeading, text);
            HfsmGraphPresentation.SetTextColor(_wrappedButton, text);
        }
        private void DrawWrappedText(string text, bool heading = false, string observation = null)
        {
            EnsureDetailStyles(); var style = heading ? _wrappedHeading : _wrappedText;
            GUILayout.Label(new GUIContent(HfsmGraphPresentation.WrapText(style, text, _detailWidth), text), style, GUILayout.Width(_detailWidth),
                GUILayout.Height(HfsmGraphPresentation.TextHeight(style, text, _detailWidth)));
            if (observation != null) Observe(observation, GUILayoutUtility.GetLastRect());
        }
        private bool DrawWrappedButton(string text)
        {
            EnsureDetailStyles();
            return GUILayout.Button(new GUIContent(HfsmGraphPresentation.WrapText(_wrappedButton, text, _detailWidth), text), _wrappedButton, GUILayout.Width(_detailWidth),
                GUILayout.Height(HfsmGraphPresentation.TextHeight(_wrappedButton, text, _detailWidth)));
        }
        private void DrawConditionNames(HfsmGraphModel.Edge edge)
        {
            var row = edge.Scope.Table.ResolveTransition(edge.Row);
            if (row.Always) { DrawWrappedText("무조건 전환 (Always) · 작성된 조건과 관계없이 전환"); return; }
            if (row.Conditions == null || row.Conditions.Length == 0) { DrawWrappedText("조건 없음 · 현재 규칙상 전환되지 않습니다."); return; }
            for (int i = 0; i < row.Conditions.Length; i++)
            {
                var condition = row.Conditions[i];
                string prefix = i == 0 ? "" : row.Conditions[i - 1].Operator == TransitionTableSO.Operator.And ? "AND · " : "OR · ";
                string name = condition.Condition != null ? condition.Condition.name : "Missing condition";
                DrawWrappedText(prefix + name, false, "ConditionText/" + edge.Scope.Key + "/" + edge.Row + "/" + i);
                DrawWrappedText("기대 결과: " + (condition.ExpectedResult == TransitionTableSO.Result.True ? "참 (True)" : "거짓 (False)"));
                GUILayout.Space(4);
            }
        }
        private void DrawStateActions(StateSO state)
        {
            var so = new SerializedObject(state); so.Update();
            var actions = so.FindProperty("_actions");
            bool changed = false;
            for (int i = 0; i < actions.arraySize; i++)
            {
                var entry = actions.GetArrayElementAtIndex(i);
                var action = entry.objectReferenceValue as StateActionSO;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawWrappedText($"{i + 1}. " + (action != null ? action.name : "행동 SO를 넣어주세요"), true, "ActionText/" + i);
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(entry, GUIContent.none);
                    changed |= EditorGUI.EndChangeCheck();
                    if (action is DescriptionSMActionBaseSO described && !string.IsNullOrWhiteSpace(described.description))
                        DrawWrappedText(described.description, false, "ActionDescription/" + i);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUI.DisabledScope(i == 0))
                            if (GUILayout.Button("↑", GUILayout.Width(28))) { actions.MoveArrayElement(i, i - 1); changed = true; }
                        using (new EditorGUI.DisabledScope(i == actions.arraySize - 1))
                            if (GUILayout.Button("↓", GUILayout.Width(28))) { actions.MoveArrayElement(i, i + 1); changed = true; }
                        bool remove = GUILayout.Button("목록에서 빼기");
                        if (remove) { entry.objectReferenceValue = null; actions.DeleteArrayElementAtIndex(i); changed = true; }
                    }
                }
                GUILayout.Space(4);
                // Re-layout before drawing the rest if the collection moved or shrank.
                if (changed) break;
            }
            if (GUILayout.Button("+ 행동 추가"))
            { int index = actions.arraySize++; actions.GetArrayElementAtIndex(index).objectReferenceValue = null; changed = true; }
            if (changed) { so.ApplyModifiedProperties(); _notice = "행동 목록을 수정했어요. 완료 후 Save를 눌러주세요."; Repaint(); }
        }
    }
}
