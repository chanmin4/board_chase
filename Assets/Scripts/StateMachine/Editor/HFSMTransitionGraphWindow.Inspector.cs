using System.Linq;
using UnityEditor;
using UnityEngine;
using VSplatter.StateMachine.ScriptableObjects;

namespace VSplatter.StateMachine.Editor
{
    internal sealed partial class HFSMTransitionGraphWindow
    {
        private bool _outgoingExpanded = true, _incomingExpanded;
        private int _stateDetailTab;
        private void DrawInspector(Rect rect)
        {
            if (_selectedRule != null) SelectionUi.Synchronize(_selectedRule, "rule");
            else if (_selectedEdge != null) SelectionUi.Synchronize(_selectedEdge.Scope.Table, "edge:" + _selectedEdge.Row);
            else if (_selectedNode != null) SelectionUi.Synchronize(_selectedNode.State, "state:" + _stateDetailTab);
            else SelectionUi.Synchronize(Scope?.Table, "scope:" + Scope?.Key);
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            GUI.BeginGroup(rect); GUILayout.BeginArea(new Rect(8, 8, rect.width - 16, rect.height - 16));
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 105;
            _detailWidth = Mathf.Max(80, rect.width - 46);
            try
            {
                using (var scroll = new EditorGUILayout.ScrollViewScope(_inspectorScroll))
                {
                    _inspectorScroll = scroll.scrollPosition;
                    if (_selectedRule != null) DrawSharedRuleInspector(_selectedRule);
                    else if (_selectedEdge != null) DrawTransitionInspector(_selectedEdge);
                    else if (_selectedNode != null) DrawStateInspector(_selectedNode);
                    else
                    {
                        GUILayout.Label("상태 / 전환 설정", EditorStyles.boldLabel);
                        EditorGUILayout.HelpBox("① 이름 뒤 _숫자 = 계층 깊이 (최상위 1)\n② 내부 상태가 있는 카드 더블클릭 → 다음 계층\n③ 규칙 정리: 적용 상태 → 규칙 → 도착 상태\n공유 규칙 클릭 → 내용 편집 / 왼쪽 상태 클릭 → 실제 검사 순서\n전체 전환선 (원본)에서도 모든 화살표를 기본 표시합니다.", MessageType.Info);
                        var active = HfsmGraphModel.AllNodes(_model.Root).Where(n => n.IsActive).Select(HfsmGraphPresentation.StateLabel).ToArray();
                        if (active.Length > 0) EditorGUILayout.HelpBox("현재 실행 경로\n" + string.Join(" → ", active), MessageType.None);
                        if (_owner == null && _table != null && _definition == null) EditorGUILayout.HelpBox("Table-only view. 전체 계층은 캐릭터 칸에 프리팹을 넣어 확인하세요.", MessageType.Info);
                        foreach (var issue in _model.Warnings.Take(8)) EditorGUILayout.HelpBox(issue, MessageType.Warning);
                        DrawMissingSharedRuleRepairs(Scope?.Table);
                        if (Scope?.Table != null)
                        {
                            var entry = Scope.Nodes.FirstOrDefault(n => n.State == Scope.Entry);
                            DrawWrappedText("테이블 시작: " + HfsmGraphPresentation.StateLabel(entry));
                            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                            {
                                var entryProperty = ScopeEntryProperty(Scope, out var so);
                                EditorGUI.BeginChangeCheck();
                                if (entryProperty != null) EditorGUILayout.PropertyField(entryProperty, new GUIContent("시작 상태"));
                                if (Scope.DefinitionScope < 0) EditorGUILayout.PropertyField(so.FindProperty("_hierarchy"), new GUIContent("SO 내부 계층 (고급)"), true);
                                if (EditorGUI.EndChangeCheck()) { so.ApplyModifiedProperties(); RefreshGraph(); }
                            }
                        }
                    }
                }
            }
            finally { EditorGUIUtility.labelWidth = oldLabelWidth; GUILayout.EndArea(); GUI.EndGroup(); }
        }
        private void DrawStateInspector(HfsmGraphModel.Node node)
        {
            DrawWrappedText(HfsmGraphPresentation.StateLabel(node), true);
            DrawWrappedText(node.Level == 1 ? "1계층 · 최상위 상태" : node.Level + "계층 · 안쪽 상태");
            EditorGUILayout.ObjectField("원본 상태 SO", node.State, typeof(StateSO), false);
            DrawWrappedText("테이블: " + node.Scope.Table.name);
            if (node.Scope.Machine != null)
            {
                DrawWrappedText("실행기: " + node.Level + "계층 · " + node.Scope.Table.name + " / " + node.Scope.Machine.name);
                if (GUILayout.Button("실행 컴포넌트 선택")) Selection.activeObject = node.Scope.Machine;
            }
            if (node.IsComposite)
            {
                EditorGUILayout.HelpBox("이 상태 안에 다음 계층의 상태들이 있습니다. 기존 활성화 규칙과 실행 순서는 그대로입니다.", MessageType.Info);
                foreach (var child in node.Children)
                {
                    if (DrawWrappedButton(child.Level + "계층 보기: " + child.Label)) NavigateToScope(child);
                    var binding = _model.Bindings.FirstOrDefault(b => b.Parent == node.Scope.Machine && b.ParentState == node.State && b.Child == child.Machine);
                    DrawWrappedText(child.DefinitionScope > 0 && child.DefinitionScope != node.Scope.DefinitionScope
                        ? child.Definition.Scopes[child.DefinitionScope - 1].Reentry == HfsmDefinitionSO.ReentryPolicy.RestartFromEntry ? "재진입: 시작 상태부터" : "재진입: 이전 행동 이어서"
                        : child.Machine == node.Scope.Machine
                        ? "내부 계층 진입: 지정된 시작 상태로"
                        : binding.ResetOnEnable ? "재활성화: 시작 상태로 초기화" : "재활성화: 이전 상태에서 재개 (기존 동작)");
                }
            }
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("전환 추가")) { _showRuleMap = false; _connectFrom = node; _reconnect = null; _notice = null; _fitPending = true; }
                    if (GUILayout.Button("시작 상태로")) { SetScopeEntry(node); return; }
                }
            }
            EditorGUILayout.Space();
            _stateDetailTab = GUILayout.Toolbar(_stateDetailTab, new[] { "전환 보기", "행동 보기" });
            Observe("StateDetailTab", GUILayoutUtility.GetLastRect());
            if (_stateDetailTab == 0) DrawStateTransitions(node);
            else
            {
                DrawWrappedText("상태 행동 · 위에서 아래로 실행", true);
                EditorGUILayout.HelpBox("행동은 다음 계층의 상태가 아니라, 이 상태가 수행하는 작업입니다. 작성 순서대로 Enter / Update / Exit을 실행합니다.", MessageType.None);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying)) DrawStateActions(node.State);
            }
        }

        private void DrawStateTransitions(HfsmGraphModel.Node node)
        {
            var edges = HfsmGraphPresentation.AllEdges(_model.Root).Where(e => e.Scope.Table == node.Scope.Table
                && e.Scope.Machine == node.Scope.Machine && e.Scope.DefinitionScope == node.Scope.DefinitionScope).OrderByDescending(e => e.Scope.Table.Transitions[e.Row].Priority).ThenBy(e => e.Row).ToArray();
            var outgoing = edges.Where(e => e.From == node).ToArray();
            var incoming = edges.Where(e => e.To == node).ToArray();
            EditorGUILayout.Space();
            _outgoingExpanded = EditorGUILayout.Foldout(_outgoingExpanded, $"나가는 전환 ({outgoing.Length})", true);
            if (_outgoingExpanded && outgoing.Length > 0) DrawWrappedText("실제 검사 순서 · 우선순위 높은 순 / 같으면 #행 번호 순\n첫 성공 전환을 선택하고 뒤쪽 규칙은 검사하지 않습니다.");
            if (_outgoingExpanded) for (int i = 0; i < outgoing.Length; i++) DrawTransitionRow(outgoing[i], true, i + 1);
            _incomingExpanded = EditorGUILayout.Foldout(_incomingExpanded, $"들어오는 전환 ({incoming.Length})", true);
            if (_incomingExpanded) foreach (var edge in incoming) DrawTransitionRow(edge, false);
            var boundary = edges.Count(e => (e.From != node && node.Children.Any(c => HfsmGraphModel.AllNodes(c).Contains(e.From))
                || e.To != node && node.Children.Any(c => HfsmGraphModel.AllNodes(c).Contains(e.To)))
                && HfsmGraphPresentation.Links(_model.Root, Scope).Any(l => l.Edge == e));
            if (boundary > 0) EditorGUILayout.HelpBox($"안쪽 상태와 다른 계층을 잇는 전환 {boundary}개도 이 카드에 표시됩니다. ↗ 선을 선택하면 실제 출발/도착 상태와 계층을 확인할 수 있어요.", MessageType.None);
        }
        private void DrawTransitionRow(HfsmGraphModel.Edge edge, bool outgoing, int order = 0)
        {
            var row = edge.Scope.Table.ResolveTransition(edge.Row);
            var target = outgoing ? edge.To : edge.From;
            string key = edge.Scope.Key + "/" + edge.Row;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (DrawWrappedButton((order > 0 ? $"검사 {order} · " : "") + $"#{edge.Row + 1}  {(outgoing ? "→" : "←")} {HfsmGraphPresentation.StateLabel(target)}")) SelectEdge(edge);
                Observe("TransitionRow/" + key, GUILayoutUtility.GetLastRect());
                DrawWrappedText("우선순위 " + row.Priority);
                if (edge.Rule != null)
                {
                    if (DrawWrappedButton("[공유 규칙] " + edge.Rule.DisplayName)) SelectRule(edge.Rule);
                }
                else DrawWrappedText("[개별 전환] 이 사용처만 변경됩니다.");
                if (row.Always) DrawWrappedText("무조건 전환 (Always)");
                else if (row.Conditions == null || row.Conditions.Length == 0) DrawWrappedText("조건 없음 · 실행되지 않는 전환");
                else
                {
                    bool expanded = _expandedTransitionConditions.Contains(key);
                    bool chosen = EditorGUILayout.Foldout(expanded, "조건 " + row.Conditions.Length + "개 펼치기", true);
                    Observe("TransitionConditions/" + key, GUILayoutUtility.GetLastRect());
                    if (chosen) _expandedTransitionConditions.Add(key); else _expandedTransitionConditions.Remove(key);
                    if (chosen) DrawConditionNames(edge);
                }
            }
            GUILayout.Space(4);
        }

        private void DrawTransitionInspector(HfsmGraphModel.Edge edge)
        {
            if (edge.Row < 0 || edge.Row >= edge.Scope.Table.Transitions.Count) { _selectedEdge = null; return; }
            GUILayout.Label("TRANSITION", EditorStyles.boldLabel);
            DrawWrappedText(HfsmGraphPresentation.StateLabel(edge.From) + "\n→ " + HfsmGraphPresentation.StateLabel(edge.To), true);
            EditorGUILayout.LabelField("Authored row " + (edge.Row + 1), EditorStyles.miniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("출발 상태 보기")) SelectNode(edge.From);
                if (GUILayout.Button("도착 상태 보기")) SelectNode(edge.To);
            }
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                DrawConditionNames(edge);
                var so = new SerializedObject(edge.Scope.Table); so.Update();
                var row = so.FindProperty("_transitions").GetArrayElementAtIndex(edge.Row);
                if (edge.Rule != null)
                {
                    EditorGUILayout.HelpBox("내용은 공유 규칙 SO에서 관리합니다. 아래 우선순위는 이 출발 상태에만 적용됩니다.", MessageType.Info);
                    if (DrawWrappedButton("공유 규칙 편집: " + edge.Rule.DisplayName)) { SelectRule(edge.Rule); return; }
                    if (DrawWrappedButton("공유 해제 → 현재 내용을 개별 전환으로 복사")) { HfsmSharedRuleAuthoring.Detach(edge.Scope.Table, edge.Row); _dirty = true; return; }
                }
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(row.FindPropertyRelative("Priority"), new GUIContent("우선순위", "숫자가 높을수록 먼저 검사합니다. 같으면 기존 행 순서입니다."));
                if (edge.Rule == null)
                {
                    EditorGUILayout.PropertyField(row.FindPropertyRelative("ToState"), new GUIContent("도착 상태"));
                    EditorGUILayout.PropertyField(row.FindPropertyRelative("Always"), new GUIContent("무조건 전환"));
                    EditorGUILayout.PropertyField(row.FindPropertyRelative("ExitReason"), new GUIContent("종료 사유"));
                    EditorGUILayout.PropertyField(row.FindPropertyRelative("Conditions"), new GUIContent("조건 편집"), true);
                }
                if (EditorGUI.EndChangeCheck()) { so.ApplyModifiedProperties(); _dirty = true; _notice = "전환을 수정했어요. 완료 후 Save를 눌러주세요."; }
                if (edge.Rule == null) DrawInlineSharingControls(edge);
                EditorGUILayout.HelpBox("같은 우선순위는 기존 행 순서대로 검사합니다. 빈 조건은 자동 전환이 아닙니다. 무조건 전환은 Always를 명시하세요.", MessageType.Info);
                EditorGUILayout.HelpBox("Interrupted/Stopped는 기존 Exit 완료 효과를 호출하지 않습니다. 취소 처리를 구현한 행동에만 사용하세요. 기존 에셋은 Transition을 유지합니다.", MessageType.Warning);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(edge.Rule != null ? "개별로 다시 연결" : "다시 연결")) { _showRuleMap = false; _reconnect = edge; _connectFrom = null; _notice = null; }
                    if (GUILayout.Button("전환 삭제")) { HfsmGraphAuthoring.DeleteTransition(edge.Scope.Table, edge.Row); RefreshGraph(); }
                }
            }
        }

        private void CreateState(TransitionTableSO table, StateSO parent = null)
        {
            if (table == null || EditorApplication.isPlaying) return;
            string directory = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(table));
            string path = EditorUtility.SaveFilePanelInProject("Create HFSM State", "NewState", "asset", "새 상태를 저장할 위치", directory);
            if (string.IsNullOrEmpty(path)) return;
            var state = CreateInstance<StateSO>(); AssetDatabase.CreateAsset(state, path); Undo.RegisterCreatedObjectUndo(state, "Create HFSM state");
            HfsmGraphAuthoring.RegisterState(table, state, parent); RefreshGraph();
        }
    }
}
