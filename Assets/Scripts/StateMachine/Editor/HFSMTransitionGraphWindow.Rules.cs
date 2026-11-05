using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VSplatter.StateMachine.ScriptableObjects;

namespace VSplatter.StateMachine.Editor
{
    internal sealed partial class HFSMTransitionGraphWindow
    {
        private Vector2 _ruleMapScroll;
        private List<HfsmRuleMap.Group> _ruleMapGroups = new();
        private SharedTransitionRuleSO _ruleToLink;
        private readonly Dictionary<string, Rect> _ruleMapRects = new();
        private GUIStyle _ruleCardText, _ruleMapHeader;

        private void EnsureRuleCardStyles()
        {
            _ruleCardText ??= new GUIStyle(EditorStyles.wordWrappedLabel)
            { fixedHeight = 0, wordWrap = true, fontSize = 12, alignment = TextAnchor.UpperLeft, padding = new RectOffset(0, 0, 0, 0) };
            _ruleMapHeader ??= new GUIStyle(_ruleCardText) { fontStyle = FontStyle.Bold };
            HfsmGraphPresentation.SetTextColor(_ruleCardText, HfsmGraphPresentation.CardText);
            HfsmGraphPresentation.SetTextColor(_ruleMapHeader, HfsmGraphPresentation.CardDetailText);
        }

        // Visible card is drawn explicitly; invisible button preserves the existing click / keyboard behavior.
        private bool DrawRuleCard(Rect rect, string text, bool active, bool selected, bool composite, string observation)
        {
            HfsmGraphPresentation.DrawCardBackground(rect, active, selected, composite);
            var content = new Rect(rect.x + 12, rect.y + 12, rect.width - 24, rect.height - 24);
            GUI.Label(content, new GUIContent(text, text), _ruleCardText);
            Observe(observation + "/Background", rect); Observe(observation + "/Text", content);
            return GUI.Button(rect, new GUIContent("", text), GUIStyle.none);
        }

        private void SelectRule(SharedTransitionRuleSO rule)
        { SelectionUi.SelectionClicked(); _selectedRule = rule; _selectedNode = null; _selectedEdge = null; _inspectorScroll = Vector2.zero; Repaint(); }

        // Destination lanes replace the dense web with short branches joining ONE destination.
        // The source set is printed in each card; it is not an implicit Any State transition.
        private void DrawRuleMap(Rect area)
        {
            _canvas = area; _visibleNodes.Clear(); _shownLinks.Clear(); _edgeBadgeBounds.Clear();
            _ruleMapGroups = HfsmRuleMap.Build(_model.Root, Scope); _ruleMapRects.Clear();
            EnsureDetailStyles(); EnsureRuleCardStyles();
            GUI.Box(area, GUIContent.none); GUI.BeginGroup(area);
            try
            {
                EditorGUI.DrawRect(new Rect(Vector2.zero, area.size), HfsmGraphPresentation.CanvasColor);
                string header = "규칙 정리 · 묶음 " + _ruleMapGroups.Count + "개 / 실제 전환 " + _ruleMapGroups.Sum(g => g.Usages.Length)
                    + "개\n실행 상태가 아닌 작성 규칙입니다. 검사 순서는 왼쪽 상태를 선택해 확인하세요.";
                float headerHeight = Mathf.Max(46, _ruleMapHeader.CalcHeight(new GUIContent(header), Mathf.Max(100, area.width - 20)) + 10);
                GUI.Label(new Rect(10, 6, area.width - 20, headerHeight - 8), header, _ruleMapHeader);
                var viewport = new Rect(0, headerHeight, area.width, Mathf.Max(50, area.height - headerHeight));
                float width = Mathf.Max(580, area.width - 24), leftWidth = Mathf.Min(500, width * .57f), targetX = leftWidth + 80;
                float y = 12;
                var cards = new List<(HfsmRuleMap.Group group, Rect rect, string text)>();
                var targets = new List<(HfsmGraphModel.Node node, Rect rect, Rect[] sources)>();
                foreach (var lane in _ruleMapGroups.GroupBy(g => g.Target))
                {
                    var laneSources = new List<Rect>(); float start = y;
                    foreach (var group in lane)
                    {
                        string text = HfsmGraphPresentation.WrapText(_ruleCardText, group.Title + "\n적용: " + group.Sources
                            + "\n" + (group.Rule != null ? group.Usages.Length + "개 사용처 · 내용은 한 곳에서 편집" : "#" + (group.Usages[0].Edge.Row + 1) + " · 이 사용처만 편집"), leftWidth - 24);
                        float height = Mathf.Max(100, _ruleCardText.CalcHeight(new GUIContent(text), leftWidth - 24) + 24);
                        var rect = new Rect(12, y, leftWidth, height); cards.Add((group, rect, text)); laneSources.Add(rect); y += height + 10;
                    }
                    float laneHeight = y - start - 10;
                    string targetLabel = lane.Key != null ? HfsmGraphPresentation.StateLabel(lane.Key) : "상위 / 다른 계층";
                    float targetWidth = width - targetX - 12;
                    string targetText = "목적지 상태\n" + targetLabel;
                    float targetHeight = Mathf.Max(110, _ruleCardText.CalcHeight(new GUIContent(HfsmGraphPresentation.WrapText(_ruleCardText, targetText, targetWidth - 24)), targetWidth - 24) + 24);
                    if (laneHeight < targetHeight) y += targetHeight - laneHeight;
                    var target = new Rect(targetX, start + Mathf.Max(0, (laneHeight - targetHeight) * .5f), targetWidth, targetHeight);
                    targets.Add((lane.Key, target, laneSources.ToArray())); y += 28;
                }
                _ruleMapScroll = GUI.BeginScrollView(viewport, _ruleMapScroll, new Rect(0, 0, width, y + 12));
                try
                {
                    foreach (var target in targets)
                    {
                        float busX = target.rect.x - 30;
                        Handles.BeginGUI(); Handles.color = new Color(.52f, .73f, .83f);
                        foreach (var source in target.sources)
                            Handles.DrawAAPolyLine(1.8f, new Vector3(source.xMax, source.center.y), new Vector3(busX, source.center.y));
                        float min = Mathf.Min(target.sources.Min(r => r.center.y), target.rect.center.y), max = Mathf.Max(target.sources.Max(r => r.center.y), target.rect.center.y);
                        Handles.DrawLine(new Vector3(busX, min), new Vector3(busX, max));
                        Vector3 tip = new(target.rect.x, target.rect.center.y);
                        Handles.DrawAAPolyLine(2, new Vector3(busX, tip.y), tip);
                        Handles.DrawAAConvexPolygon(tip, tip + new Vector3(-9, -4), tip + new Vector3(-9, 4)); Handles.EndGUI();
                        string id = target.node != null ? target.node.State.name : "boundary";
                        Observe("RuleMapArrow/" + id, new Rect(busX, target.rect.center.y - 4, target.rect.x - busX, 8));
                        string label = target.node != null ? HfsmGraphPresentation.StateLabel(target.node) : "상위 / 다른 계층";
                        if (target.node != null && target.node.IsComposite && Event.current.type == EventType.MouseDown && Event.current.clickCount == 2 && target.rect.Contains(Event.current.mousePosition))
                        { OpenInside(target.node); Event.current.Use(); }
                        if (DrawRuleCard(target.rect, HfsmGraphPresentation.WrapText(_ruleCardText, "목적지 상태\n" + label, target.rect.width - 24),
                            target.node?.IsActive == true, target.node == _selectedNode && target.node != null, true, "RuleMapCardTarget/" + id) && target.node != null) SelectNode(target.node);
                        Observe("RuleMapTarget/" + id, target.rect);
                    }
                    foreach (var card in cards)
                    {
                        bool selected = card.group.Rule != null ? _selectedRule == card.group.Rule : _selectedEdge == card.group.Usages[0].Edge;
                        if (DrawRuleCard(card.rect, card.text, false, selected, card.group.Rule != null, "RuleMapCardGroup/" + card.group.Key))
                        { if (card.group.Rule != null) SelectRule(card.group.Rule); else SelectEdge(card.group.Usages[0].Edge); }
                        _ruleMapRects[card.group.Key] = card.rect;
                        Observe("RuleMapGroup/" + card.group.Key, card.rect);
                    }
                }
                finally { GUI.EndScrollView(); }
                Observe("Canvas", new Rect(Vector2.zero, area.size)); Observe("RuleMapCanvas", new Rect(Vector2.zero, area.size));
            }
            finally { GUI.EndGroup(); }
        }

        private IEnumerable<(TransitionTableSO table, int row)> RuleUsages(SharedTransitionRuleSO rule)
        {
            var tables = HfsmGraphModel.AllScopes(_model.Root).Select(s => s.Table).Where(t => t != null)
                .Concat(AssetDatabase.FindAssets("t:TransitionTableSO").Select(g => AssetDatabase.LoadAssetAtPath<TransitionTableSO>(AssetDatabase.GUIDToAssetPath(g)))).Where(t => t != null).Distinct();
            foreach (var table in tables)
                for (int i = 0; i < table.Transitions.Count; i++)
                    if (table.Transitions[i].UseSharedRule && table.Transitions[i].SharedRule == rule) yield return (table, i);
        }

        private void DrawSharedRuleInspector(SharedTransitionRuleSO rule)
        {
            DrawWrappedText("공유 전환 규칙 · " + rule.DisplayName, true);
            EditorGUILayout.HelpBox("이 카드는 실행 상태가 아닙니다. 지정된 사용처가 내용을 공유하며, 검사 위치와 우선순위는 각 사용처가 따로 가집니다.", MessageType.Info);
            var usages = RuleUsages(rule).ToArray();
            DrawWrappedText("수정 영향: " + usages.Length + "개 사용처", true);
            foreach (var use in usages)
            {
                var row = use.table.Transitions[use.row];
                int order = use.table.Transitions.Select((r, i) => (r, i)).Where(p => p.r.FromState == row.FromState).OrderByDescending(p => p.r.Priority).ThenBy(p => p.i).Select(p => p.i).ToList().IndexOf(use.row) + 1;
                DrawWrappedText(use.table.name + " / " + (row.FromState != null ? row.FromState.name : "없음") + $"\n검사 {order} · 우선순위 {row.Priority} · #행 {use.row + 1}", false, "SharedUsage/" + use.table.GetInstanceID() + "/" + use.row);
                var edge = HfsmGraphPresentation.AllEdges(_model.Root).FirstOrDefault(e => e.Scope.Table == use.table && e.Row == use.row);
                if (edge != null && DrawWrappedButton("이 사용처 / 검사 순서 보기")) { SelectEdge(edge); return; }
            }
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                var so = new SerializedObject(rule); so.Update(); EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(so.FindProperty("_displayName"), new GUIContent("화면 표시 이름"));
                EditorGUILayout.PropertyField(so.FindProperty("_toState"), new GUIContent("공유 도착 상태"));
                DrawSharedConditions(so.FindProperty("_conditions"));
                EditorGUILayout.PropertyField(so.FindProperty("_always"), new GUIContent("무조건 전환"));
                EditorGUILayout.PropertyField(so.FindProperty("_exitReason"), new GUIContent("공유 종료 사유"));
                if (EditorGUI.EndChangeCheck()) { so.ApplyModifiedProperties(); _dirty = true; _notice = $"공유 규칙 수정: {usages.Length}개 사용처에 적용됩니다. Save로 SO에 저장하세요."; }
                if (DrawWrappedButton("이 공유 규칙 SO만 저장")) { AssetDatabase.SaveAssetIfDirty(rule); _notice = "공유 규칙 SO를 저장했어요. 씬/프리팹은 저장하지 않았습니다."; }
                Observe("SaveSharedRule", GUILayoutUtility.GetLastRect());
            }
        }

        private void DrawSharedConditions(SerializedProperty conditions)
        {
            DrawWrappedText("공유 조건 · 작성 순서대로 검사", true);
            for (int i = 0; i < conditions.arraySize; i++)
            {
                var condition = conditions.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var reference = condition.FindPropertyRelative("Condition");
                    DrawWrappedText((i + 1) + ". " + (reference.objectReferenceValue != null ? reference.objectReferenceValue.name : "조건 SO를 넣어주세요"));
                    EditorGUILayout.PropertyField(reference, new GUIContent("조건 SO"));
                    EditorGUILayout.PropertyField(condition.FindPropertyRelative("ExpectedResult"), new GUIContent("필요 결과"));
                    if (i < conditions.arraySize - 1) EditorGUILayout.PropertyField(condition.FindPropertyRelative("Operator"), new GUIContent("다음 조건과", "AND: 둘 다 필요 / OR: 다른 조건 묶음도 허용. 기존 조합을 유지하세요."));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUI.DisabledScope(i == 0))
                            if (GUILayout.Button("↑")) { conditions.MoveArrayElement(i, i - 1); GUI.changed = true; break; }
                        using (new EditorGUI.DisabledScope(i == conditions.arraySize - 1))
                            if (GUILayout.Button("↓")) { conditions.MoveArrayElement(i, i + 1); GUI.changed = true; break; }
                        if (GUILayout.Button("조건 빼기")) { conditions.DeleteArrayElementAtIndex(i); GUI.changed = true; break; }
                    }
                }
            }
            if (DrawWrappedButton("+ 조건 추가"))
            {
                int index = conditions.arraySize++; var row = conditions.GetArrayElementAtIndex(index);
                row.FindPropertyRelative("Condition").objectReferenceValue = null;
                row.FindPropertyRelative("ExpectedResult").enumValueIndex = 0;
                row.FindPropertyRelative("Operator").enumValueIndex = 0; GUI.changed = true;
            }
        }

        private void DrawInlineSharingControls(HfsmGraphModel.Edge edge)
        {
            var matches = HfsmSharedRuleAuthoring.MatchingInlineRows(edge.Scope.Table, edge.Row);
            if (DrawWrappedButton("이 내용으로 공유 규칙 만들기... (일치 " + matches.Length + "개)"))
            {
                string sources = string.Join("\n", matches.Select(i => "#" + (i + 1) + " · " + edge.Scope.Table.Transitions[i].FromState.name));
                if (!EditorUtility.DisplayDialog("동일 전환만 공유", "아래 사용처의 내용만 공유합니다. 시작 상태·행 위치·우선순위는 유지됩니다.\n\n" + sources, "규칙 만들기", "취소")) return;
                string path = EditorUtility.SaveFilePanelInProject("공유 규칙 SO 저장", "Shared_" + edge.To.State.name, "asset", "내용을 공유할 규칙 SO", System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(edge.Scope.Table)));
                if (string.IsNullOrEmpty(path)) return;
                var rule = HfsmSharedRuleAuthoring.Create(edge.Scope.Table, matches, path); _dirty = true; SelectRule(rule);
            }
            _ruleToLink = (SharedTransitionRuleSO)EditorGUILayout.ObjectField("사용할 공유 규칙", _ruleToLink, typeof(SharedTransitionRuleSO), false);
            using (new EditorGUI.DisabledScope(_ruleToLink == null))
                if (DrawWrappedButton("이 사용처에만 공유 규칙 연결"))
                {
                    if (EditorUtility.DisplayDialog("전환 내용 변경", "이 행의 조건·목적지·종료 사유가 선택한 공유 규칙으로 바뀝니다. 위치와 우선순위는 유지됩니다.", "연결", "취소"))
                    { HfsmSharedRuleAuthoring.Bind(edge.Scope.Table, new[] { edge.Row }, _ruleToLink); _dirty = true; }
                }
        }

        private void DrawMissingSharedRuleRepairs(TransitionTableSO table)
        {
            if (table == null) return;
            for (int i = 0; i < table.Transitions.Count; i++)
            {
                var row = table.Transitions[i];
                if (!row.UseSharedRule || row.SharedRule != null) continue;
                EditorGUILayout.HelpBox($"#행 {i + 1}: 공유 규칙 연결이 없습니다. 이전 직접 필드로 자동 실행하지 않습니다. 복구할 규칙을 넣어주세요.", MessageType.Error);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                {
                    var rule = (SharedTransitionRuleSO)EditorGUILayout.ObjectField("복구할 공유 규칙", null, typeof(SharedTransitionRuleSO), false);
                    if (rule != null) { HfsmSharedRuleAuthoring.Bind(table, new[] { i }, rule); _dirty = true; }
                }
            }
        }
    }
}
