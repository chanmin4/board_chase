using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VSplatter.StateMachine.Editor
{
    /// <summary>
    /// Editor-only, one-level view of the existing graph. Collapsing a parent projects
    /// boundary arrows onto that parent; Edge still identifies the ORIGINAL table row.
    /// Nothing here changes the runtime hierarchy, transition order or authored data.
    /// </summary>
    internal static class HfsmGraphPresentation
    {
        internal static string StateLabel(HfsmGraphModel.Node node)
            => node == null ? "없음" : node.State.name + "_" + node.Level;

        internal static string ScopeLabel(HfsmGraphModel.Scope scope)
            => scope == null ? "없음" : scope.Level + "계층 · " + scope.Label;

        internal static string NavigationLabel(HfsmGraphModel.Scope scope)
            => scope.OwnerNode == null ? ScopeLabel(scope) : StateLabel(scope.OwnerNode) + " 내부 · " + scope.Level + "계층";

        internal static string MachineLabel(StateMachine machine, int level)
            => level + "계층 · " + (machine.Table != null ? machine.Table.name : "테이블 없음") + " / " + machine.name;

        internal sealed class Link
        {
            public HfsmGraphModel.Edge Edge;
            public HfsmGraphModel.Node From, To;
            public bool CrossesHierarchy => From != Edge.From || To != Edge.To;
        }

        internal static IEnumerable<HfsmGraphModel.Edge> AllEdges(HfsmGraphModel.Scope root)
            => HfsmGraphModel.AllScopes(root).SelectMany(s => s.Edges);

        internal static List<Link> Links(HfsmGraphModel.Scope root, HfsmGraphModel.Scope scope)
        {
            var result = new List<Link>();
            if (scope == null) return result;
            foreach (var edge in AllEdges(root).Where(e => e.Scope.Table == scope.Table
                         && e.Scope.Machine == scope.Machine && e.Scope.DefinitionScope == scope.DefinitionScope).OrderBy(e => e.Row))
            {
                var from = Frontier(scope, edge.From);
                var to = Frontier(scope, edge.To);
                if (from == null && to == null) continue;
                // Internal transitions belong to the child's screen, not a fake parent loop.
                if (from == to && edge.From != edge.To) continue;
                if (from == to && from != edge.From) continue;
                result.Add(new Link { Edge = edge, From = from, To = to });
            }
            return result;
        }

        private static HfsmGraphModel.Node Frontier(HfsmGraphModel.Scope scope, HfsmGraphModel.Node endpoint)
            => scope.Nodes.FirstOrDefault(n => n == endpoint || n.Children.Any(c => HfsmGraphModel.AllNodes(c).Contains(endpoint)));

        internal static bool Show(Link link, bool showAll, HfsmGraphModel.Node selectedNode,
            HfsmGraphModel.Edge selectedEdge, bool recentLiveTransition)
            => showAll || link.Edge == selectedEdge || recentLiveTransition
                || selectedNode != null && (link.From == selectedNode || link.To == selectedNode
                    || link.Edge.From == selectedNode || link.Edge.To == selectedNode);

        internal static Vector2 CardSize => new(300, 120);
        // Canvas cards deliberately keep the same readable palette in light/dark editor skins.
        // Built-in miniButton styles can have no texture or black text after a skin/reload change.
        internal static Color CanvasColor => new(.095f, .115f, .14f);
        internal static Color CardText => new(.95f, .97f, 1f);
        internal static Color CardDetailText => new(.80f, .87f, .93f);
        internal static Color CardColor(bool active, bool selected, bool composite)
            => active ? new(.16f, .36f, .23f) : selected ? new(.15f, .35f, .47f)
                : composite ? new(.19f, .26f, .30f) : new(.24f, .27f, .33f);
        internal static void SetTextColor(GUIStyle style, Color color)
        {
            foreach (var state in new[] { style.normal, style.hover, style.active, style.focused,
                style.onNormal, style.onHover, style.onActive, style.onFocused }) state.textColor = color;
        }
        internal static void DrawCardBackground(Rect rect, bool active, bool selected, bool composite)
        {
            EditorCardBorder(rect, selected ? new Color(.36f, .79f, 1f) : new Color(.44f, .55f, .63f));
            UnityEditor.EditorGUI.DrawRect(new Rect(rect.x + 1, rect.y + 1, Mathf.Max(0, rect.width - 2), Mathf.Max(0, rect.height - 2)), CardColor(active, selected, composite));
        }
        private static void EditorCardBorder(Rect rect, Color color) => UnityEditor.EditorGUI.DrawRect(rect, color);
        internal static Vector2 DefaultPosition(int index)
            => new(110 + index % 2 * 570, 140 + index / 2 * 270);

        internal static float FitZoom(Vector2 view, Vector2 content, bool overview)
            => Mathf.Clamp(Mathf.Min((view.x - 64) / Mathf.Max(1, content.x), (view.y - 100) / Mathf.Max(1, content.y)), overview ? .15f : .8f, 1);

        internal static bool ShowBadge(Link link, int count, HfsmGraphModel.Node selectedNode, HfsmGraphModel.Edge selectedEdge, HfsmGraphModel.Edge hoveredEdge)
            => count <= 10 || link.Edge == selectedEdge || link.Edge == hoveredEdge
                || selectedNode != null && (link.From == selectedNode || link.To == selectedNode);

        internal static float TextHeight(GUIStyle style, string text, float width)
            => Mathf.Max(20, Mathf.Ceil(style.CalcHeight(new GUIContent(WrapText(style, text, width)), Mathf.Max(60, width))) + 2);

        internal static string WrapText(GUIStyle style, string text, float width)
        {
            // Unity's word wrap may not split long underscore-separated asset identifiers.
            // Insert DISPLAY-ONLY newlines, retaining the original name in the tooltip/data.
            width = Mathf.Max(60, width);
            var output = new StringBuilder();
            foreach (var paragraph in (text ?? "").Split('\n'))
            {
                string line = "";
                foreach (char c in paragraph)
                {
                    while (line.Length > 0 && style.CalcSize(new GUIContent(line + c)).x > width)
                    {
                        int at = Mathf.Max(line.LastIndexOf(' '), line.LastIndexOf('_'));
                        int length = at >= 0 ? at + 1 : line.Length;
                        output.Append(line.Substring(0, length)).Append('\n');
                        line = line.Substring(length);
                    }
                    line += c;
                }
                output.Append(line).Append('\n');
            }
            if (output.Length > 0) output.Length--;
            return output.ToString();
        }

        // A badge is optional. Never cover a state or another badge merely to show a number.
        internal static bool TryBadge(Vector2 point, Rect allowed, IEnumerable<Rect> obstacles, out Rect badge)
        {
            var candidate = new Rect(point - new Vector2(21, 11), new Vector2(42, 22));
            badge = candidate;
            return allowed.Contains(candidate.min) && allowed.Contains(candidate.max) && !obstacles.Any(r => r.Overlaps(candidate));
        }

        internal static void PanelWidths(float width, ref float browser, ref float inspector)
        {
            // Reserve useful canvas space first; neither splitter can push the other pane away.
            float budget = Mathf.Max(350, width - 310);
            browser = Mathf.Clamp(browser, 150, Mathf.Max(150, budget - 220));
            inspector = Mathf.Clamp(inspector, 220, Mathf.Max(220, budget - browser));
            browser = Mathf.Min(browser, Mathf.Max(150, budget - inspector));
        }
    }
}
