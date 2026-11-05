using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VSplatter.StateMachine.Editor
{
    internal sealed partial class HFSMTransitionGraphWindow
    {
        private readonly List<HfsmGraphModel.Node> _visibleNodes = new();
        private List<HfsmGraphPresentation.Link> _scopeLinks = new();
        private readonly List<HfsmGraphPresentation.Link> _shownLinks = new();
        private HfsmGraphModel.Node _dragNode;
        private Vector2 _dragStart, _dragOffset;
        private bool _panning;
        private Rect _canvas;
        private GUIStyle _nodeTitle;
        private GUIStyle _nodeDetail;
        private readonly List<Rect> _edgeBadgeBounds = new();
        private readonly Dictionary<HfsmGraphModel.Edge, Vector2[]> _edgeRoutes = new();
        private HfsmGraphModel.Scope _routedScope;
        private HfsmGraphModel.Edge _hoveredEdge;
        private Rect[] _routeBoxes;
        private Vector2Int[] _routePairs;
        private Rect _routeBounds;
        private int _routeBuildCount;
        private double _routeBuildMilliseconds;

        // Do not reuse the old densely packed .level coordinates for routed corridors.
        // Previous editor preferences remain intact; no authored asset is migrated.
        private static string CanvasLayoutKey(HfsmGraphModel.Node node) => HfsmGraphModel.LayoutKey(node) + ".routed.v1";

        private Vector2 LayoutScope(HfsmGraphModel.Scope scope, Vector2 origin)
        {
            float maxX = 280, maxY = 180;
            for (int i = 0; i < scope.Nodes.Count; i++)
            {
                var node = scope.Nodes[i];
                var key = CanvasLayoutKey(node);
                var fallback = HfsmGraphPresentation.DefaultPosition(i);
                var local = new Vector2(EditorPrefs.GetFloat(key + ".x", fallback.x), EditorPrefs.GetFloat(key + ".y", fallback.y));
                Vector2 size = HfsmGraphPresentation.CardSize;
                node.Bounds = new Rect(origin + local, size); _visibleNodes.Add(node);
                maxX = Mathf.Max(maxX, local.x + size.x + 20); maxY = Mathf.Max(maxY, local.y + size.y + 20);
            }
            scope.Bounds = new Rect(origin, new Vector2(maxX, maxY));
            return scope.Bounds.size;
        }

        private Rect Screen(Rect world) => new Rect(world.position * _zoom + _pan, world.size * _zoom);
        private void DrawCanvas(Rect area)
        {
            if (_showRuleMap && Scope != null && Scope.Edges.Any(e => e.Rule != null)) { DrawRuleMap(area); return; }
            _canvas = area; GUI.Box(area, GUIContent.none); GUI.BeginGroup(area);
            var bounds = new Rect(Vector2.zero, area.size);
            EditorGUI.DrawRect(bounds, HfsmGraphPresentation.CanvasColor);
            _visibleNodes.Clear(); _shownLinks.Clear(); _edgeBadgeBounds.Clear();
            if (Scope == null || _table == null)
            { GUI.Label(bounds, "프리팹을 선택하면 실제 HFSM 계층이 여기에 표시됩니다.", EditorStyles.centeredGreyMiniLabel); GUI.EndGroup(); return; }
            LayoutScope(Scope, Vector2.zero);
            _scopeLinks = HfsmGraphPresentation.Links(_model.Root, Scope);
            _shownLinks.AddRange(_scopeLinks.Where(link => HfsmGraphPresentation.Show(link, _showAllTransitions,
                _selectedNode, _selectedEdge, IsRecentLive(link.Edge))));
            EnsureRoutes();
            if (_fitPending && Event.current.type == EventType.Repaint)
            {
                _zoom = HfsmGraphPresentation.FitZoom(area.size, _routeBounds.size, _overviewFit);
                _pan = new Vector2(Mathf.Max(32, (area.width - _routeBounds.width * _zoom) / 2), 60) - _routeBounds.position * _zoom;
                _fitPending = false; _overviewFit = false;
            }
            _hoveredEdge = bounds.Contains(Event.current.mousePosition) && !_visibleNodes.Any(n => Screen(n.Bounds).Contains(Event.current.mousePosition))
                ? HitEdge(Event.current.mousePosition)?.Edge : null;
            if (Event.current.type == EventType.MouseMove) Repaint();
            DrawCanvasGrid(bounds);
            // All arrows are visible by default. Draw behind cards so labels stay readable.
            foreach (var link in _shownLinks.OrderBy(l => IsEmphasized(l) ? 1 : 0)) DrawEdge(link);
            DrawBoundaryPorts();
            foreach (var node in _visibleNodes) DrawNode(node);
            foreach (var link in _shownLinks.OrderByDescending(l => l.Edge == _selectedEdge)) DrawEdgeBadge(link, bounds);
            HandleCanvasInput(bounds);
            EditorGUI.DrawRect(new Rect(0, 0, area.width, 43), EditorGUIUtility.isProSkin ? new Color(.10f, .12f, .14f, .95f) : new Color(.78f, .80f, .82f, .95f));
            GUI.Label(new Rect(8, 6, area.width - 16, 18), $"{HfsmGraphPresentation.ScopeLabel(Scope)}  ·  {_zoom:P0}  ·  상태 {_visibleNodes.Count}개  ·  전환 {_shownLinks.Count}/{_scopeLinks.Count}", EditorStyles.miniLabel);
            if (!_showAllTransitions && _selectedNode == null && _selectedEdge == null)
                GUI.Label(new Rect(8, 25, area.width - 16, 18), "관련 선만 보기 중 · 전체 전환 보기를 켜면 클릭 없이 모든 화살표가 보입니다.", EditorStyles.miniLabel);
            else GUI.Label(new Rect(8, 25, area.width - 16, 18), "중간 버튼: 이동 · 전체 보기: 축소 개요 · 읽기 크기: 확대 · 선에 마우스: #번호", EditorStyles.miniLabel);
            Observe("Canvas", bounds);
            GUI.EndGroup();
        }

        private void DrawCanvasGrid(Rect area)
        {
            float step = Mathf.Max(16, 32 * _zoom); Handles.BeginGUI(); Handles.color = new Color(.5f, .5f, .5f, .1f);
            for (float x = _pan.x % step; x < area.width; x += step) Handles.DrawLine(new Vector3(x, 0), new Vector3(x, area.height));
            for (float y = _pan.y % step; y < area.height; y += step) Handles.DrawLine(new Vector3(0, y), new Vector3(area.width, y));
            Handles.EndGUI();
        }

        private void DrawNode(HfsmGraphModel.Node node)
        {
            Rect rect = Screen(node.Bounds);
            bool selected = _selectedNode == node;
            HfsmGraphPresentation.DrawCardBackground(rect, node.IsActive, selected, node.IsComposite);
            _nodeTitle ??= new GUIStyle(EditorStyles.boldLabel) { wordWrap = true, alignment = TextAnchor.MiddleLeft };
            HfsmGraphPresentation.SetTextColor(_nodeTitle, HfsmGraphPresentation.CardText);
            _nodeTitle.fontSize = Mathf.Clamp(Mathf.RoundToInt(12 * _zoom), 9, 18);
            var title = new Rect(rect.x + 8, rect.y + 4, rect.width - 16, rect.height * .58f);
            GUI.Label(title, new GUIContent(HfsmGraphPresentation.StateLabel(node), node.Level + "계층\n원본 SO: " + node.State.name + "\n" + node.Scope.Table.name), _nodeTitle);
            if (_zoom > .5f)
            {
                var detail = new Rect(rect.x + 8, rect.yMax - 25 * _zoom, rect.width - 16, 20);
                string label = node.Level + "계층 · " + (node.IsActive ? "● ACTIVE  " : node.Scope.Entry == node.State ? "● 시작  " : "")
                    + (node.IsComposite ? "▸ 내부 " + node.Children.Sum(c => c.Nodes.Count) + "개 · 더블클릭" : "클릭: 전환 / 행동");
                _nodeDetail ??= new GUIStyle(EditorStyles.miniLabel);
                HfsmGraphPresentation.SetTextColor(_nodeDetail, HfsmGraphPresentation.CardDetailText);
                GUI.Label(detail, label, _nodeDetail);
            }
            Observe("State/" + node.State.name, rect);
        }

        private Rect WorldBoundaryPort(bool incoming) => new Rect(incoming ? 16 : Scope.Bounds.xMax - 172, 30, 156, 32);
        private Rect BoundaryPort(bool incoming) => Screen(WorldBoundaryPort(incoming));
        private void DrawBoundaryPorts()
        {
            if (_shownLinks.Any(l => l.From == null)) GUI.Box(BoundaryPort(true), "상위 / 다른 계층", EditorStyles.helpBox);
            if (_shownLinks.Any(l => l.To == null)) GUI.Box(BoundaryPort(false), "상위 / 다른 계층", EditorStyles.helpBox);
        }
        private void EnsureRoutes()
        {
            var boxes = _visibleNodes.Select(n => n.Bounds).ToList();
            int incoming = boxes.Count; if (_scopeLinks.Any(l => l.From == null)) boxes.Add(WorldBoundaryPort(true));
            int outgoing = boxes.Count; if (_scopeLinks.Any(l => l.To == null)) boxes.Add(WorldBoundaryPort(false));
            var pairs = _scopeLinks.Select(l => new Vector2Int(l.From != null ? _visibleNodes.IndexOf(l.From) : incoming, l.To != null ? _visibleNodes.IndexOf(l.To) : outgoing)).ToArray();
            if (_routedScope == Scope && _routeBoxes.SequenceEqual(boxes) && _routePairs.SequenceEqual(pairs)) return;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            _routeBoxes = boxes.ToArray(); _routePairs = pairs; _routedScope = Scope; _edgeRoutes.Clear();
            var paths = HfsmGraphRouting.Build(_routeBoxes, pairs);
            for (int i = 0; i < paths.Count; i++) _edgeRoutes.Add(_scopeLinks[i].Edge, paths[i]);
            var points = boxes.SelectMany(r => new[] { r.min, r.max }).Concat(paths.SelectMany(p => p)).ToArray();
            _routeBounds = points.Length == 0 ? new Rect(0, 0, 300, 180) : Rect.MinMaxRect(points.Min(p => p.x) - 24, points.Min(p => p.y) - 24, points.Max(p => p.x) + 24, points.Max(p => p.y) + 24);
            _routeBuildCount++; _routeBuildMilliseconds = timer.Elapsed.TotalMilliseconds;
        }
        private Vector2[] ScreenRoute(HfsmGraphModel.Edge edge) => _edgeRoutes[edge].Select(p => p * _zoom + _pan).ToArray();
        private HfsmGraphPresentation.Link HitEdge(Vector2 point)
            => _shownLinks.Select(l => (link: l, distance: HfsmGraphRouting.Distance(point, ScreenRoute(l.Edge))))
                .Where(pair => pair.distance < 7).OrderBy(pair => pair.distance).Select(pair => pair.link).FirstOrDefault();
        private bool IsEmphasized(HfsmGraphPresentation.Link link) => link.Edge == _selectedEdge || link.Edge == _hoveredEdge || IsRecentLive(link.Edge)
            || _selectedNode != null && (link.From == _selectedNode || link.To == _selectedNode);
        private static bool IsRecentLive(HfsmGraphModel.Edge edge)
            => EditorApplication.isPlaying && edge.Scope.Execution != null && edge.Scope.Execution.IsScopeActive
                && edge.Scope.Execution.PreviousStateSO == edge.From.State && edge.Scope.Execution.CurrentStateSO == edge.To.State
                && Time.frameCount - edge.Scope.Execution.LastTransitionFrame < 20;
        private void DrawEdge(HfsmGraphPresentation.Link link)
        {
            var edge = link.Edge;
            var points = ScreenRoute(edge);
            bool live = IsRecentLive(edge);
            bool related = _selectedNode != null && (link.From == _selectedNode || link.To == _selectedNode);
            Color color = _selectedEdge == edge || _hoveredEdge == edge ? new Color(.3f, .85f, 1) : live ? new Color(.4f, 1, .5f) : related ? new Color(.7f, .85f, 1) : new Color(.6f, .67f, .74f, .55f);
            Handles.BeginGUI(); Handles.color = color;
            Handles.DrawAAPolyLine(IsEmphasized(link) ? 2.5f : 1.35f, points.Select(p => new Vector3(p.x, p.y)).ToArray());
            Vector2 to = points[points.Length - 1], direction = (to - points[points.Length - 2]).normalized, side = new(-direction.y, direction.x);
            Handles.DrawAAConvexPolygon(to, to - direction * 8 + side * 3.5f, to - direction * 8 - side * 3.5f); Handles.EndGUI();
            Observe("Edge/" + edge.Scope.Key + "/" + edge.Row, new Rect(RoutePoint(points, .5f) - new Vector2(5, 5), new Vector2(10, 10)));
        }
        private static Vector2 RoutePoint(Vector2[] points, float fraction)
        {
            float length = Enumerable.Range(1, points.Length - 1).Sum(i => Vector2.Distance(points[i - 1], points[i]));
            float remaining = length * fraction;
            for (int i = 1; i < points.Length; i++)
            {
                float segment = Vector2.Distance(points[i - 1], points[i]);
                if (remaining <= segment) return Vector2.Lerp(points[i - 1], points[i], segment > 0 ? remaining / segment : 0);
                remaining -= segment;
            }
            return points[points.Length - 1];
        }
        private void DrawEdgeBadge(HfsmGraphPresentation.Link link, Rect canvas)
        {
            if (_zoom < .45f || !HfsmGraphPresentation.ShowBadge(link, _scopeLinks.Count, _selectedNode, _selectedEdge, _hoveredEdge)) return;
            var points = ScreenRoute(link.Edge);
            var obstacles = _visibleNodes.Select(n => Screen(n.Bounds)).Concat(_edgeBadgeBounds).ToList();
            obstacles.Add(BoundaryPort(true)); obstacles.Add(BoundaryPort(false));
            var allowed = new Rect(4, 74, canvas.width - 8, Mathf.Max(0, canvas.height - 78));
            foreach (float t in new[] { .5f, .35f, .65f, .2f, .8f, .1f, .9f })
            {
                if (!HfsmGraphPresentation.TryBadge(RoutePoint(points, t), allowed, obstacles, out var badge)) continue;
                var edge = link.Edge;
                var row = edge.Scope.Table.ResolveTransition(edge.Row);
                string hint = $"#{edge.Row + 1} · {HfsmGraphPresentation.StateLabel(edge.From)} → {HfsmGraphPresentation.StateLabel(edge.To)}\n우선순위 {row.Priority}\n클릭하면 오른쪽에 전체 조건을 표시합니다.";
                if (GUI.Button(badge, new GUIContent("#" + (edge.Row + 1), hint), EditorStyles.miniButton)) SelectEdge(edge);
                _edgeBadgeBounds.Add(badge);
                Observe("EdgeBadge/" + edge.Scope.Key + "/" + edge.Row, badge);
                return;
            }
        }
        private void SelectEdge(HfsmGraphModel.Edge edge)
        {
            SelectionUi.SelectionClicked();
            if (!HfsmGraphPresentation.Links(_model.Root, Scope).Any(l => l.Edge == edge))
                NavigateToScope(FindContainingScope(_model.Root, edge.From));
            _selectedEdge = edge; _selectedNode = null; _selectedRule = null; _inspectorScroll = Vector2.zero; Repaint();
        }

        private void HandleCanvasInput(Rect area)
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { _connectFrom = null; _reconnect = null; _dragNode = null; e.Use(); }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F && _selectedNode != null)
            { _pan = area.size / 2 - _selectedNode.Bounds.center * _zoom; e.Use(); Repaint(); }
            if (e.type == EventType.MouseUp) { _dragNode = null; _panning = false; }
            if (e.type == EventType.MouseDrag && _panning) { _pan += e.delta; e.Use(); Repaint(); return; }
            if (e.type == EventType.MouseDrag && _dragNode != null)
            {
                Vector2 local = Vector2.Max(Vector2.zero, _dragOffset + (e.mousePosition - _dragStart) / _zoom);
                string key = CanvasLayoutKey(_dragNode); EditorPrefs.SetFloat(key + ".x", local.x); EditorPrefs.SetFloat(key + ".y", local.y); e.Use(); Repaint(); return;
            }
            if (!area.Contains(e.mousePosition)) return;
            if (e.type == EventType.ScrollWheel)
            { Vector2 anchor = (e.mousePosition - _pan) / _zoom; _zoom = Mathf.Clamp(_zoom * Mathf.Pow(1.08f, -e.delta.y), .15f, 2); _pan = e.mousePosition - anchor * _zoom; e.Use(); Repaint(); return; }
            if (e.type == EventType.MouseDown && e.button == 2) { _panning = true; e.Use(); return; }
            if (e.type != EventType.MouseDown && e.type != EventType.ContextClick) return;
            var hit = _visibleNodes.Where(n => Screen(n.Bounds).Contains(e.mousePosition)).OrderBy(n => n.Bounds.width * n.Bounds.height).FirstOrDefault();
            var edgeHit = HitEdge(e.mousePosition);
            if (edgeHit != null && hit == null && _connectFrom == null && _reconnect == null)
            { SelectEdge(edgeHit.Edge); if (e.type == EventType.ContextClick) EdgeMenu(edgeHit.Edge); e.Use(); return; }
            if (hit != null)
            {
                if (e.type == EventType.ContextClick) { NodeMenu(hit); e.Use(); return; }
                if (e.button != 0) return;
                if (_connectFrom != null || _reconnect != null) { FinishConnection(hit); e.Use(); return; }
                SelectNode(hit);
                if (e.clickCount == 2 && hit.IsComposite) { OpenInside(hit); e.Use(); return; }
                _dragNode = hit; _dragStart = e.mousePosition;
                // Use the containing visible scope, not the table identity, for native nested nodes.
                var containing = FindContainingScope(Scope, hit);
                _dragOffset = hit.Bounds.position - (containing?.Bounds.position ?? Vector2.zero);
                e.Use(); return;
            }
            if (e.type == EventType.ContextClick && !EditorApplication.isPlaying) { var menu = new GenericMenu(); menu.AddItem(new GUIContent("Create State..."), false, () => CreateState(Scope.Table)); menu.ShowAsContext(); e.Use(); }
            else if (e.type == EventType.MouseDown && e.button == 0 && _connectFrom == null && _reconnect == null)
            { _selectedNode = null; _selectedEdge = null; e.Use(); Repaint(); }
        }

        private static HfsmGraphModel.Scope FindContainingScope(HfsmGraphModel.Scope scope, HfsmGraphModel.Node node)
        {
            if (scope == null) return null;
            if (scope.Nodes.Contains(node)) return scope;
            foreach (var child in scope.Nodes.SelectMany(n => n.Children)) { var found = FindContainingScope(child, node); if (found != null) return found; }
            return null;
        }
        private void FinishConnection(HfsmGraphModel.Node target)
        {
            if (EditorApplication.isPlaying) return;
            var table = _connectFrom?.Scope.Table ?? _reconnect?.Scope.Table;
            var sourceScope = _connectFrom?.Scope ?? _reconnect?.Scope;
            if (target.Scope.Table != table || target.Scope.DefinitionScope != sourceScope.DefinitionScope)
            { _notice = "같은 계층 테이블 안에서 전환을 연결하세요. 내부 행동 연결은 HFSM 그래프 SO에서 설정합니다."; return; }
            if (_connectFrom != null) HfsmGraphAuthoring.AddTransition(table, _connectFrom.State, target.State);
            else HfsmGraphAuthoring.Reconnect(table, _reconnect.Row, target.State);
            RefreshGraph();
        }
        private void NodeMenu(HfsmGraphModel.Node node)
        {
            var menu = new GenericMenu();
            if (EditorApplication.isPlaying) { menu.AddDisabledItem(new GUIContent("Play Mode: read-only")); menu.ShowAsContext(); return; }
            menu.AddItem(new GUIContent("Make Transition"), false, () => { _connectFrom = node; _reconnect = null; _notice = null; Repaint(); });
            menu.AddItem(new GUIContent("시작 상태로"), false, () => SetScopeEntry(node));
            menu.AddItem(new GUIContent("다음 계층 상태 만들기..."), false, () => CreateState(node.Scope.Table, node.State));
            menu.AddItem(new GUIContent("Select State Asset"), false, () => Selection.activeObject = node.State);
            if (node.IsComposite) menu.AddItem(new GUIContent("다음 계층 보기"), false, () => OpenInside(node));
            menu.ShowAsContext();
        }
        private void EdgeMenu(HfsmGraphModel.Edge edge)
        {
            var menu = new GenericMenu();
            if (EditorApplication.isPlaying) { menu.AddDisabledItem(new GUIContent("Play Mode: read-only")); menu.ShowAsContext(); return; }
            menu.AddItem(new GUIContent("Reconnect Target"), false, () => { _reconnect = edge; _connectFrom = null; _notice = null; Repaint(); });
            menu.AddItem(new GUIContent("Delete Transition"), false, () => { HfsmGraphAuthoring.DeleteTransition(edge.Scope.Table, edge.Row); RefreshGraph(); });
            menu.ShowAsContext();
        }
    }
}
