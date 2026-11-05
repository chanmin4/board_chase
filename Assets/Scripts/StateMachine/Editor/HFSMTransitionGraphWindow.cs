using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VSplatter.StateMachine.ScriptableObjects;

namespace VSplatter.StateMachine.Editor
{
    /// <summary>Window orchestration only. Model, canvas and mutations have separate owners.</summary>
    internal sealed partial class HFSMTransitionGraphWindow : EditorWindow
    {
        private const string EnemyBasePath = "Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyBasePrefab.prefab";
        [SerializeField] private GameObject _owner;
        [SerializeField] private StateMachine _machine;
        [SerializeField] private TransitionTableSO _table;
        [SerializeField] private HfsmDefinitionSO _definition;
        private readonly HfsmGraphModel _model = new();
        private readonly List<HfsmGraphModel.Scope> _navigation = new();
        private HfsmGraphModel.Scope Scope => _navigation.Count > 0 ? _navigation[_navigation.Count - 1] : _model.Root;
        private HfsmGraphModel.Node _selectedNode;
        private HfsmGraphModel.Edge _selectedEdge;
        private SharedTransitionRuleSO _selectedRule;
        private MakerSelectionUiState _selectionUi;
        private MakerSelectionUiState SelectionUi => _selectionUi ??= new MakerSelectionUiState();
        private bool _showRuleMap = true;
        private HfsmGraphModel.Node _connectFrom;
        private HfsmGraphModel.Edge _reconnect;
        private Vector2 _pan = new(30, 35), _browserScroll, _inspectorScroll;
        private float _zoom = 1, _browserWidth = 215, _inspectorWidth = 330;
        private int _resize;
        private bool _dirty = true, _fitPending = true, _showGuide = true, _showAllTransitions = true;
        private bool _overviewFit;
        private string _notice;
        private Vector2 _windowOrigin;
        private float _workspaceTop;
        internal System.Action<string, Rect> ObserveControl;

        [MenuItem("Tools/Authoring/HFSM Transition Graph")]
        private static void Open()
        {
            var window = GetWindow<HFSMTransitionGraphWindow>("HFSM Transition Graph");
            window.minSize = new Vector2(800, 520); window.UseSelection(); window.Show();
        }

        internal static void OpenTable(TransitionTableSO table)
        {
            var window = GetWindow<HFSMTransitionGraphWindow>("HFSM Transition Graph");
            window.minSize = new Vector2(800, 520); window.SetTable(table); window.Show();
        }
        internal static void OpenDefinition(HfsmDefinitionSO definition, GameObject owner = null)
        {
            var window = GetWindow<HFSMTransitionGraphWindow>("HFSM Transition Graph");
            window.minSize = new Vector2(800, 520);
            if (owner != null) window.SetOwner(owner); else window.SetDefinition(definition);
            window.Show();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            _dirty = true;
            _fitPending = true; _overviewFit = false;
            _browserWidth = EditorPrefs.GetFloat("HFSM.BrowserWidth.v2", 215);
            _inspectorWidth = EditorPrefs.GetFloat("HFSM.InspectorWidth.v2", 330);
            _showGuide = EditorPrefs.GetBool("HFSM.Guide.v2", true);
            Undo.undoRedoPerformed += RefreshGraph; EditorApplication.playModeStateChanged += OnPlayMode;
            SharedTransitionRuleSO.DefinitionChanged += SharedDefinitionChanged;
            HfsmDefinitionSO.DefinitionChanged += SharedDefinitionChanged;
        }
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= RefreshGraph; EditorApplication.playModeStateChanged -= OnPlayMode;
            SharedTransitionRuleSO.DefinitionChanged -= SharedDefinitionChanged;
            HfsmDefinitionSO.DefinitionChanged -= SharedDefinitionChanged;
        }
        private void SharedDefinitionChanged() { _dirty = true; Repaint(); }
        private void OnPlayMode(PlayModeStateChange state) => RefreshGraph();
        private void OnInspectorUpdate() { if (EditorApplication.isPlaying) Repaint(); }
        private void OnSelectionChange()
        {
            if (_owner != null) return;
            if (Selection.activeObject is HfsmDefinitionSO definition) SetDefinition(definition);
            else if (Selection.activeObject is TransitionTableSO table) SetTable(table);
        }
        private void UseSelection()
        {
            if (Selection.activeObject is HfsmDefinitionSO definition) SetDefinition(definition);
            else if (Selection.activeObject is TransitionTableSO table) SetTable(table);
            else if (Selection.activeGameObject != null) SetOwner(Selection.activeGameObject);
        }
        private void SetTable(TransitionTableSO table) { _owner = null; _machine = null; _definition = null; _table = table; RefreshGraph(); }
        private void SetDefinition(HfsmDefinitionSO definition) { _owner = null; _machine = null; _definition = definition; _table = definition != null ? definition.RootTable : null; RefreshGraph(); }
        private void SetOwner(GameObject owner)
        {
            _owner = owner; _machine = null;
            if (owner != null)
            {
                var bindings = new List<StateMachineChildBinding>();
                foreach (var source in owner.GetComponentsInChildren<MonoBehaviour>(true).OfType<IStateMachineHierarchySource>()) source.GetHierarchyBindings(bindings);
                var children = new HashSet<StateMachine>(bindings.Where(b => b.Child != null).Select(b => b.Child));
                var machines = owner.GetComponentsInChildren<StateMachine>(true);
                _machine = machines.FirstOrDefault(m => !children.Contains(m)) ?? machines.FirstOrDefault();
            }
            _definition = (_machine as HfsmStateMachine)?.Definition;
            _table = _machine != null ? _machine.Table : null; RefreshGraph();
        }
        private void RefreshGraph()
        { _dirty = true; _navigation.Clear(); _selectedNode = null; _selectedEdge = null; _selectedRule = null; _connectFrom = null; _reconnect = null; _fitPending = true; Repaint(); }

        private void OnGUI()
        {
            _windowOrigin = GUIUtility.GUIToScreenPoint(Vector2.zero);
            DrawToolbar();
            bool guide = EditorGUILayout.Foldout(_showGuide, "사용 가이드 / Quick guide", true);
            if (guide != _showGuide) { _showGuide = guide; EditorPrefs.SetBool("HFSM.Guide.v2", guide); }
            if (_showGuide)
                EditorGUILayout.HelpBox("① 캐릭터 선택 → 실제 계층  ② 내부 상태 카드 더블클릭 → 다음 계층  ③ 위 경로 → 돌아가기\n이름_1 = 최상위 · 이름_2 = 안쪽 상태. 규칙 묶음은 새 계층이나 실행 상태가 아닙니다.\n공유 규칙이 있는 계층은 규칙 정리 보기: 적용 상태 → 공유 규칙 → 목적지. 클릭 없이 화살표 표시.\n상태 클릭 → 실제 검사 순서 · 전체 전환선 → 원본 연결 / 휠 확대 / 중간 버튼 이동 · 좌우 경계 드래그: 패널 폭", MessageType.Info);
            if (_dirty) RebuildModel();
            DrawBreadcrumbs();
            Rect workspace = GUILayoutUtility.GetRect(1, 10000, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (Event.current.type == EventType.Layout) workspace.y = _workspaceTop;
            else _workspaceTop = workspace.y;
            workspace.height = Mathf.Max(160, position.height - workspace.y - 26);
            HfsmGraphPresentation.PanelWidths(position.width, ref _browserWidth, ref _inspectorWidth);
            var browser = new Rect(0, workspace.y, _browserWidth, workspace.height);
            var inspector = new Rect(position.width - _inspectorWidth, workspace.y, _inspectorWidth, workspace.height);
            var canvas = new Rect(browser.xMax + 5, workspace.y, Mathf.Max(100, inspector.x - browser.xMax - 10), workspace.height);
            DrawBrowser(browser); DrawCanvas(canvas); DrawInspector(inspector);
            Observe("Browser", browser); Observe("Inspector", inspector);
            DrawSplitter(new Rect(browser.xMax, workspace.y, 5, workspace.height), 1);
            DrawSplitter(new Rect(inspector.x - 5, workspace.y, 5, workspace.height), 2);
            GUI.Label(new Rect(6, position.height - 23, position.width - 12, 21),
                _notice ?? (_connectFrom != null ? "연결할 대상 상태를 클릭하세요. Esc: 취소" : _reconnect != null ? "새 연결 대상을 클릭하세요. Esc: 취소" : "이름 뒤 숫자 = 계층 · 내부 상태가 있는 카드 더블클릭: 다음 계층 · 화살표 = 전환 · 화면 배치는 실행 순서와 무관"), EditorStyles.miniLabel);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("캐릭터", GUILayout.Width(52));
                var owner = (GameObject)EditorGUILayout.ObjectField(_owner, typeof(GameObject), true, GUILayout.MinWidth(120));
                if (owner != _owner) SetOwner(owner);
                if (GUILayout.Button("Use Selected", EditorStyles.toolbarButton, GUILayout.Width(88))) UseSelection();
                if (GUILayout.Button("Enemy Base", EditorStyles.toolbarButton, GUILayout.Width(80))) SetOwner(AssetDatabase.LoadAssetAtPath<GameObject>(EnemyBasePath));
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60))) RefreshGraph();
            }
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (_owner != null)
                {
                    var machines = _owner.GetComponentsInChildren<StateMachine>(true);
                    int index = System.Array.IndexOf(machines, _machine);
                    int chosen = machines.Length == 0 ? -1 : EditorGUILayout.Popup(Mathf.Max(0, index), machines.Select(m => HfsmGraphPresentation.MachineLabel(m, _model.LevelForMachine(m))).ToArray(), GUILayout.MinWidth(120));
                    if (machines.Length == 0) GUILayout.Label("이 캐릭터에 StateMachine이 없습니다.", EditorStyles.miniLabel);
                    if (chosen >= 0 && chosen < machines.Length && chosen != index) { _machine = machines[chosen]; _definition = (_machine as HfsmStateMachine)?.Definition; _table = _machine.Table; RefreshGraph(); }
                }
                else
                {
                    if (_definition != null)
                    { var definition = (HfsmDefinitionSO)EditorGUILayout.ObjectField(_definition, typeof(HfsmDefinitionSO), false, GUILayout.MinWidth(120)); if (definition != _definition) SetDefinition(definition); }
                    else
                    { var table = (TransitionTableSO)EditorGUILayout.ObjectField(_table, typeof(TransitionTableSO), false, GUILayout.MinWidth(120)); if (table != _table) SetTable(table); }
                }
                if (GUILayout.Button(new GUIContent("전체 보기", "현재 계층을 화면 안에 축소해 배치합니다. 읽기 크기로 다시 확대할 수 있습니다."), EditorStyles.toolbarButton, GUILayout.Width(68))) { _overviewFit = true; _fitPending = true; }
                Observe("OverviewButton", GUILayoutUtility.GetLastRect());
                if (GUILayout.Button(new GUIContent("읽기 크기", "카드와 이름을 읽을 수 있는 크기로 표시합니다. 좁은 창에서는 가운데 버튼으로 이동하세요."), EditorStyles.toolbarButton, GUILayout.Width(68))) { _overviewFit = false; _fitPending = true; }
                Observe("ReadableButton", GUILayoutUtility.GetLastRect());
                _showAllTransitions = GUILayout.Toggle(_showAllTransitions, new GUIContent("전체 전환 보기", "기본값: 켜짐. 선택하지 않아도 현재 계층의 모든 화살표가 보입니다. 끄면 선택한 상태/전환과 최근 실행 선만 표시합니다."), EditorStyles.toolbarButton, GUILayout.Width(104));
                Observe("AllTransitionsToggle", GUILayoutUtility.GetLastRect());
                if (GUILayout.Button("배치 정리", EditorStyles.toolbarButton, GUILayout.Width(68))) ResetScopeLayout();
                if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(63)))
                {
                    var issues = HfsmGraphAuthoring.Validate(Scope?.Table).Concat(_model.Warnings).Distinct().ToArray();
                    _notice = issues.Length == 0 ? "Validation passed." : string.Join(" | ", issues.Take(3));
                }
                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                    if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(45))) SaveGraphAssets();
            }
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                int view = GUILayout.Toolbar(_showRuleMap ? 0 : 1, new[] { "규칙 정리 (공유 / 개별)", "전체 전환선 (원본)" }, EditorStyles.toolbarButton, GUILayout.MaxWidth(400));
                Observe("RuleViewTabs", GUILayoutUtility.GetLastRect());
                if (view != (_showRuleMap ? 0 : 1)) { _showRuleMap = view == 0; _fitPending = true; }
                GUILayout.Label("화면 순서 ≠ 검사 순서 · 상태 선택 → 실제 검사 순서", EditorStyles.miniLabel);
            }
        }

        private void SaveGraphAssets()
        {
            if (_definition != null) AssetDatabase.SaveAssetIfDirty(_definition);
            var nodes = HfsmGraphModel.AllNodes(_model.Root).ToArray();
            var tables = nodes.Select(n => n.Scope.Table).Append(_table).Where(t => t != null).Distinct();
            foreach (var table in tables) AssetDatabase.SaveAssetIfDirty(table);
            foreach (var rule in tables.SelectMany(t => t.Transitions).Where(r => r.UseSharedRule && r.SharedRule != null).Select(r => r.SharedRule).Distinct()) AssetDatabase.SaveAssetIfDirty(rule);
            foreach (var state in nodes.Select(n => n.State).Where(s => s != null).Distinct()) AssetDatabase.SaveAssetIfDirty(state);
            _notice = "HFSM assets saved. Scene/prefab not saved.";
        }

        private void RebuildModel()
        {
            var navigationKeys = _navigation.Select(s => s.Key).ToArray();
            string nodeKey = _selectedNode?.Key, edgeScopeKey = _selectedEdge?.Scope.Key;
            int row = _selectedEdge?.Row ?? -1;
            _model.BuildDefinition(_table, _machine, _owner, _definition);
            var scopes = HfsmGraphModel.AllScopes(_model.Root).ToArray();
            _navigation.Clear();
            foreach (var key in navigationKeys)
            {
                var scope = scopes.FirstOrDefault(s => s.Key == key);
                if (scope == null) break;
                _navigation.Add(scope);
            }
            _selectedNode = HfsmGraphModel.AllNodes(_model.Root).FirstOrDefault(n => n.Key == nodeKey);
            _selectedEdge = scopes.SelectMany(s => s.Edges).FirstOrDefault(e => e.Scope.Key == edgeScopeKey && e.Row == row);
            _dirty = false;
        }
        private void DrawBreadcrumbs()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button(_model.Root?.Level > 1 ? _model.Root.Level + "계층" : "최상위 · 1계층", EditorStyles.toolbarButton, GUILayout.Width(94))) NavigateToScope(_model.Root);
                for (int i = 0; i < _navigation.Count; i++)
                {
                    int keep = i + 1;
                    string label = HfsmGraphPresentation.NavigationLabel(_navigation[i]);
                    if (GUILayout.Button(new GUIContent("› " + label, label), EditorStyles.toolbarButton, GUILayout.MaxWidth(220))) { NavigateToScope(_navigation[keep - 1]); break; }
                }
                GUILayout.FlexibleSpace(); GUILayout.Label(EditorApplication.isPlaying ? "LIVE · read-only" : "Authoring", EditorStyles.miniLabel);
            }
        }
        private void DrawBrowser(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            // BeginArea gets a stable LOCAL rectangle even during IMGUI's Layout pass.
            // The dynamic window offset belongs only to BeginGroup (also clips overflow).
            GUI.BeginGroup(rect); GUILayout.BeginArea(new Rect(6, 6, rect.width - 12, rect.height - 12));
            try
            {
                GUILayout.Label("상태 계층", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("목록 펼치기", EditorStyles.miniButtonLeft)) foreach (var node in HfsmGraphModel.AllNodes(_model.Root)) HfsmGraphModel.SetExpanded(node, true);
                    if (GUILayout.Button("목록 접기", EditorStyles.miniButtonRight)) foreach (var node in HfsmGraphModel.AllNodes(_model.Root)) HfsmGraphModel.SetExpanded(node, false);
                }
                GUILayout.Label("이름_숫자 = 계층 깊이\n▸ 목록 접기/펼치기 · 이름 클릭: 해당 계층", EditorStyles.wordWrappedMiniLabel);
                using (var scroll = new EditorGUILayout.ScrollViewScope(_browserScroll))
                { _browserScroll = scroll.scrollPosition; if (_table == null) EditorGUILayout.HelpBox("캐릭터 프리팹이나 TransitionTableSO를 선택하세요.", MessageType.Info); else DrawScopeTree(_model.Root, 0); }
            }
            finally { GUILayout.EndArea(); GUI.EndGroup(); }
        }
        private void DrawScopeTree(HfsmGraphModel.Scope scope, int indent)
        {
            foreach (var node in scope.Nodes)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(indent * 12);
                    if (node.IsComposite)
                    {
                        if (GUILayout.Button(node.Expanded ? "▾" : "▸", GUILayout.Width(22))) HfsmGraphModel.SetExpanded(node, !node.Expanded);
                        Observe("TreeFold/" + node.Key, GUILayoutUtility.GetLastRect());
                    }
                    else GUILayout.Space(22);
                    if (GUILayout.Button(new GUIContent((node.IsActive ? "● " : "") + HfsmGraphPresentation.StateLabel(node), HfsmGraphPresentation.StateLabel(node) + "\n원본 SO: " + node.State.name), _selectedNode == node ? EditorStyles.miniButtonMid : EditorStyles.miniButton)) SelectNode(node);
                    Observe("Tree/" + node.Key, GUILayoutUtility.GetLastRect());
                }
                if (node.Expanded) foreach (var child in node.Children) DrawScopeTree(child, indent + 1);
            }
        }
        private void SelectNode(HfsmGraphModel.Node node)
        {
            SelectionUi.SelectionClicked();
            var containing = FindContainingScope(_model.Root, node);
            if (containing != null && containing != Scope) NavigateToScope(containing);
            _selectedNode = node; _selectedEdge = null; _selectedRule = null; _inspectorScroll = Vector2.zero; Repaint();
        }
        private void NavigateToScope(HfsmGraphModel.Scope target)
        {
            if (target == null) return;
            var path = new List<HfsmGraphModel.Scope>();
            if (!FindScopePath(_model.Root, target, path)) return;
            _navigation.Clear(); _navigation.AddRange(path.Skip(1));
            _selectedNode = null; _selectedEdge = null; _selectedRule = null; _dragNode = null; _panning = false;
            _inspectorScroll = Vector2.zero; _fitPending = true; _notice = null; Repaint();
        }
        private static bool FindScopePath(HfsmGraphModel.Scope current, HfsmGraphModel.Scope target, List<HfsmGraphModel.Scope> path)
        {
            if (current == null) return false;
            path.Add(current);
            if (current == target) return true;
            foreach (var child in current.Nodes.SelectMany(n => n.Children)) if (FindScopePath(child, target, path)) return true;
            path.RemoveAt(path.Count - 1); return false;
        }
        private void OpenInside(HfsmGraphModel.Node node)
        {
            if (node.Children.Count == 1) { NavigateToScope(node.Children[0]); return; }
            var menu = new GenericMenu();
            foreach (var child in node.Children) { var target = child; menu.AddItem(new GUIContent(HfsmGraphPresentation.ScopeLabel(target) + " / " + target.Key), false, () => NavigateToScope(target)); }
            menu.ShowAsContext();
        }
        private void ResetScopeLayout()
        {
            if (Scope == null) return;
            foreach (var node in Scope.Nodes) { string key = CanvasLayoutKey(node); EditorPrefs.DeleteKey(key + ".x"); EditorPrefs.DeleteKey(key + ".y"); }
            _fitPending = true;
            _notice = "현재 계층의 화면 배치만 정리했어요. 상태·전환 데이터는 그대로입니다.";
        }
        private void Observe(string name, Rect local)
        {
            if (Event.current.type == EventType.Repaint)
                ObserveControl?.Invoke(name, new Rect(GUIUtility.GUIToScreenPoint(local.position) - _windowOrigin, local.size));
        }
        private void DrawSplitter(Rect rect, int id)
        {
            int control = GUIUtility.GetControlID(0x48534d + id, FocusType.Passive, rect);
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal); var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition)) { _resize = id; GUIUtility.hotControl = control; e.Use(); }
            if (_resize != id) return;
            if (e.type == EventType.MouseDrag) { if (id == 1) _browserWidth += e.delta.x; else _inspectorWidth -= e.delta.x; HfsmGraphPresentation.PanelWidths(position.width, ref _browserWidth, ref _inspectorWidth); e.Use(); Repaint(); }
            if (e.type == EventType.MouseUp) { _resize = 0; if (GUIUtility.hotControl == control) GUIUtility.hotControl = 0; EditorPrefs.SetFloat("HFSM.BrowserWidth.v2", _browserWidth); EditorPrefs.SetFloat("HFSM.InspectorWidth.v2", _inspectorWidth); e.Use(); }
        }
    }
}
