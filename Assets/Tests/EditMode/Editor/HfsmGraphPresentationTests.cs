using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VSplatter.StateMachine.Editor;
using VSplatter.StateMachine.ScriptableObjects;

/// <summary>Presentation-only tests: memory SOs, no GameObjects, global Undo or file saves.</summary>
public sealed class HfsmGraphPresentationTests
{
    internal const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<UnityEngine.Object> _objects = new();
    internal static Type EditorType(string name) => typeof(HfsmGraphAuthoring).Assembly.GetType("VSplatter.StateMachine.Editor." + name, true);
    internal static object Get(object obj, string name) => obj.GetType().GetField(name, Flags)?.GetValue(obj) ?? obj.GetType().GetProperty(name, Flags)?.GetValue(obj);
    internal static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Flags).Invoke(obj, args);
    internal static object Static(string type, string name, params object[] args) => EditorType(type).GetMethod(name, Flags).Invoke(null, args);
    internal static object[] Items(object list) => ((IEnumerable)list).Cast<object>().ToArray();
    private T Temp<T>(string name) where T : ScriptableObject { var obj = ScriptableObject.CreateInstance<T>(); obj.name = name; _objects.Add(obj); return obj; }
    private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Flags).SetValue(obj, value);
    [TearDown] public void Cleanup() { foreach (var obj in _objects) UnityEngine.Object.DestroyImmediate(obj); _objects.Clear(); }

    private (TransitionTableSO table, object model, object root, object child) Fixture()
    {
        var table = Temp<TransitionTableSO>("Memory table");
        var spawn = Temp<StateSO>("Spawn"); var alive = Temp<StateSO>("Alive");
        var a = Temp<StateSO>("Patrol"); var b = Temp<StateSO>("Attack"); var dead = Temp<StateSO>("Dead");
        Set(table, "_initialState", spawn);
        Set(table, "_hierarchy", new[] {
            new TransitionTableSO.StateHierarchyItem { State = alive, InitialChild = a },
            new TransitionTableSO.StateHierarchyItem { State = a, Parent = alive },
            new TransitionTableSO.StateHierarchyItem { State = b, Parent = alive } });
        Set(table, "_transitions", new[] {
            new TransitionTableSO.TransitionItem { FromState = spawn, ToState = alive, Always = true },
            new TransitionTableSO.TransitionItem { FromState = a, ToState = b, Always = true, Priority = 5 },
            new TransitionTableSO.TransitionItem { FromState = b, ToState = a, Always = true },
            new TransitionTableSO.TransitionItem { FromState = b, ToState = dead, Always = true, Priority = 10 },
            new TransitionTableSO.TransitionItem { FromState = a, ToState = b, Always = true, Priority = 5 },
            new TransitionTableSO.TransitionItem { FromState = a, ToState = a, Always = true } });
        var model = Activator.CreateInstance(EditorType("HfsmGraphModel"));
        Call(model, "Build", table, null, null);
        var root = Get(model, "Root");
        var parent = Items(Get(root, "Nodes")).Single(n => ((StateSO)Get(n, "State")).name == "Alive");
        return (table, model, root, Items(Get(parent, "Children"))[0]);
    }
    private static object[] Links(object root, object scope) => Items(Static("HfsmGraphPresentation", "Links", root, scope));

    [Test] public void RootShowsParentsOnly_NotEveryDescendant()
    { var f = Fixture(); CollectionAssert.AreEquivalent(new[] { "Spawn", "Alive", "Dead" }, Items(Get(f.root, "Nodes")).Select(n => ((StateSO)Get(n, "State")).name)); Assert.AreEqual(2, Items(Get(f.child, "Nodes")).Length); }

    [Test] public void CollapsedParentsDoNotShowInternalArrowsOrFakeLoops()
    { var f = Fixture(); CollectionAssert.AreEqual(new[] { 0, 3 }, Links(f.root, f.root).Select(l => (int)Get(Get(l, "Edge"), "Row"))); }

    [Test] public void BoundaryProjectionRetainsOriginalRowAndRealEndpoints()
    {
        var f = Fixture(); var link = Links(f.root, f.root).Single(l => (int)Get(Get(l, "Edge"), "Row") == 3);
        Assert.AreEqual("Alive", ((StateSO)Get(Get(link, "From"), "State")).name);
        Assert.AreEqual("Attack", ((StateSO)Get(Get(Get(link, "Edge"), "From"), "State")).name);
        Assert.IsTrue((bool)Get(link, "CrossesHierarchy"));
        Assert.AreSame(f.table, Get(Get(Get(link, "Edge"), "Scope"), "Table"));
    }

    [Test] public void ChildViewKeepsExitToAnotherHierarchyAsBoundaryLink()
    { var f = Fixture(); var link = Links(f.root, f.child).Single(l => (int)Get(Get(l, "Edge"), "Row") == 3); Assert.IsNull(Get(link, "To")); Assert.AreEqual("Attack", ((StateSO)Get(Get(link, "From"), "State")).name); }

    [Test] public void ParallelRowsAndSelfTransitionsAreNeverMerged()
    { var f = Fixture(); CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, Links(f.root, f.child).Select(l => (int)Get(Get(l, "Edge"), "Row"))); }

    [Test] public void OptionalRelatedOnlyFilterShowsSelectedOrLiveEdges()
    {
        var f = Fixture(); var links = Links(f.root, f.child); var a = Get(links[0], "From");
        bool Show(object l, bool all, object node, object edge, bool live) => (bool)Static("HfsmGraphPresentation", "Show", l, all, node, edge, live);
        Assert.IsTrue(links.All(l => !Show(l, false, null, null, false)));
        Assert.IsFalse(Show(links.Single(l => (int)Get(Get(l, "Edge"), "Row") == 3), false, a, null, false));
        Assert.IsTrue(Show(links[0], false, a, null, false));
        Assert.IsTrue(Show(links[0], false, null, Get(links[0], "Edge"), false));
        Assert.IsTrue(links.All(l => Show(l, true, null, null, false)));
        Assert.IsTrue(Show(links[0], false, null, null, true));
    }

    [Test] public void DefaultWindowShowsAllTransitionsBeforeSelection()
    {
        var window = ScriptableObject.CreateInstance(EditorType("HFSMTransitionGraphWindow")); _objects.Add(window);
        Assert.IsTrue((bool)Get(window, "_showAllTransitions"));
        Assert.IsNull(Get(window, "_selectedNode")); Assert.IsNull(Get(window, "_selectedEdge"));
    }

    [Test] public void StateNamesShowActualNestedLevelsWithoutRenamingAssets()
    {
        var f = Fixture();
        var leaf = Temp<StateSO>("Search");
        var patrol = Items(Get(f.child, "Nodes")).Select(n => (StateSO)Get(n, "State")).Single(s => s.name == "Patrol");
        Set(f.table, "_hierarchy", f.table.Hierarchy.Concat(new[] {
            new TransitionTableSO.StateHierarchyItem { State = patrol, Parent = f.table.Hierarchy[1].Parent, InitialChild = leaf },
            new TransitionTableSO.StateHierarchyItem { State = leaf, Parent = patrol }
        }).Where((h, i) => i != 1).ToArray());
        string before = EditorJsonUtility.ToJson(f.table);
        Call(f.model, "Build", f.table, null, null);
        var root = Get(f.model, "Root");
        var nodes = Items(Static("HfsmGraphModel", "AllNodes", root));
        foreach (var pair in new[] { ("Alive", 1), ("Patrol", 2), ("Attack", 2), ("Search", 3) })
        {
            var node = nodes.Single(n => ((StateSO)Get(n, "State")).name == pair.Item1);
            Assert.AreEqual(pair.Item2, Get(node, "Level"));
            Assert.AreEqual(pair.Item1 + "_" + pair.Item2, Static("HfsmGraphPresentation", "StateLabel", node));
        }
        Assert.AreEqual(before, EditorJsonUtility.ToJson(f.table));
        Assert.AreEqual("Search", leaf.name);
    }

    [Test] public void PresentationNeverReordersOrRewritesSerializedDefinition()
    {
        var f = Fixture(); string before = EditorJsonUtility.ToJson(f.table);
        foreach (var scope in Items(Static("HfsmGraphModel", "AllScopes", f.root))) Links(f.root, scope);
        Assert.AreEqual(before, EditorJsonUtility.ToJson(f.table));
        Assert.AreEqual(5, f.table.Transitions[1].Priority); Assert.AreEqual(5, f.table.Transitions[4].Priority);
        Assert.AreEqual("Spawn", f.table.InitialState.name);
    }

    [Test] public void ChangingTreeExpansionDoesNotChangeCanvasScope()
    { var f = Fixture(); var parent = Items(Get(f.root, "Nodes")).Single(n => ((StateSO)Get(n, "State")).name == "Alive"); Set(parent, "Expanded", false); var first = Links(f.root, f.root).Length; Set(parent, "Expanded", true); Assert.AreEqual(first, Links(f.root, f.root).Length); Assert.AreEqual(3, Items(Get(f.root, "Nodes")).Length); }

    [Test] public void PanelWidthsReserveCanvasAtSmallAndLargeWindowSizes()
    {
        foreach (float width in new[] { 800f, 1100f, 1500f, 1920f })
        foreach (float left in new[] { -100f, 215f, 10000f })
        foreach (float right in new[] { -100f, 330f, 10000f })
        {
            var args = new object[] { width, left, right }; Static("HfsmGraphPresentation", "PanelWidths", args);
            Assert.GreaterOrEqual((float)args[1], 150); Assert.GreaterOrEqual((float)args[2], 220);
            Assert.GreaterOrEqual(width - (float)args[1] - (float)args[2] - 10, 300);
        }
    }

    [Test] public void DefaultCardLayoutDoesNotOverlap()
    {
        var size = (Vector2)EditorType("HfsmGraphPresentation").GetProperty("CardSize", Flags).GetValue(null);
        var cards = Enumerable.Range(0, 20).Select(i => new Rect((Vector2)Static("HfsmGraphPresentation", "DefaultPosition", i), size)).ToArray();
        for (int i = 0; i < cards.Length; i++) for (int j = i + 1; j < cards.Length; j++) Assert.IsFalse(cards[i].Overlaps(cards[j]));
        Assert.GreaterOrEqual(cards[1].xMin - cards[0].xMax, 240);
        Assert.GreaterOrEqual(cards[2].yMin - cards[0].yMax, 140);
    }

    private static Vector2[][] Routes(Rect[] boxes, Vector2Int[] endpoints)
        => Items(Static("HfsmGraphRouting", "Build", boxes, endpoints)).Cast<Vector2[]>().ToArray();

    [Test] public void DenseRoutesAreOrthogonalAndNeverPassThroughCards()
    {
        var boxes = Enumerable.Range(0, 7).Select(i => new Rect((Vector2)Static("HfsmGraphPresentation", "DefaultPosition", i), new Vector2(300, 120))).ToArray();
        var endpoints = Enumerable.Range(0, 7).SelectMany(from => Enumerable.Range(0, 7)
            .Where(to => from != to && (from + to) % 3 != 0).Select(to => new Vector2Int(from, to)))
            .Concat(new[] { new Vector2Int(1, 1), new Vector2Int(3, 3), new Vector2Int(0, 1), new Vector2Int(0, 1) }).ToArray();
        var paths = Routes(boxes, endpoints); Assert.AreEqual(endpoints.Length, paths.Length);
        foreach (var path in paths)
        {
            Assert.GreaterOrEqual(path.Length, 2);
            for (int i = 1; i < path.Length; i++)
            {
                Assert.IsTrue(Mathf.Abs(path[i].x - path[i - 1].x) < .05f || Mathf.Abs(path[i].y - path[i - 1].y) < .05f);
                foreach (var box in boxes) Assert.IsFalse((bool)Static("HfsmGraphRouting", "CrossesCard", path[i - 1], path[i], box));
            }
        }
    }

    [Test] public void ParallelReverseAndSelfTransitionsHaveSeparatedPorts()
    {
        var boxes = new[] { new Rect(110, 140, 300, 120), new Rect(680, 140, 300, 120), new Rect(480, 110, 80, 190) };
        var paths = Routes(boxes, new[] { new Vector2Int(0, 1), new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, 0) });
        Assert.AreNotEqual(paths[0][0], paths[1][0]);
        Assert.AreNotEqual(paths[0][0], paths[2][paths[2].Length - 1]);
        Assert.GreaterOrEqual(paths[3].Length, 4);
        foreach (var path in paths) for (int i = 1; i < path.Length; i++) foreach (var box in boxes)
            Assert.IsFalse((bool)Static("HfsmGraphRouting", "CrossesCard", path[i - 1], path[i], box));
    }

    [Test] public void RoutingIsDeterministicAndDoesNotRewriteInputPositionsOrRowOrder()
    {
        var boxes = new[] { new Rect(110, 140, 300, 120), new Rect(680, 410, 300, 120) };
        var endpoints = new[] { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, 1) };
        var before = (Rect[])boxes.Clone(); var rows = (Vector2Int[])endpoints.Clone();
        var first = Routes(boxes, endpoints); var second = Routes(boxes, endpoints);
        for (int i = 0; i < first.Length; i++) CollectionAssert.AreEqual(first[i], second[i]);
        CollectionAssert.AreEqual(before, boxes); CollectionAssert.AreEqual(rows, endpoints);
    }

    [Test] public void PolylineHitTestFollowsActualRenderedArrow()
    {
        var path = new[] { Vector2.zero, new Vector2(100, 0), new Vector2(100, 80) };
        Assert.AreEqual(2, (float)Static("HfsmGraphRouting", "Distance", new Vector2(70, 2), path), .01f);
        Assert.AreEqual(2, (float)Static("HfsmGraphRouting", "Distance", new Vector2(98, 50), path), .01f);
        Assert.Greater((float)Static("HfsmGraphRouting", "Distance", new Vector2(50, 50), path), 30);
    }

    [Test] public void DefaultFitKeepsReadableCardSizeAndOverviewRemainsAvailable()
    {
        var view = new Vector2(300, 420); var content = new Vector2(1000, 1100);
        Assert.GreaterOrEqual((float)Static("HfsmGraphPresentation", "FitZoom", view, content, false), .8f);
        Assert.Less((float)Static("HfsmGraphPresentation", "FitZoom", view, content, true), .8f);
    }

    [Test] public void DenseViewKeepsAllArrowsWithoutNumberBadgeClutter()
    {
        var f = Fixture(); var links = Links(f.root, f.child);
        Assert.IsTrue(links.All(l => (bool)Static("HfsmGraphPresentation", "Show", l, true, null, null, false)));
        Assert.IsTrue(links.All(l => !(bool)Static("HfsmGraphPresentation", "ShowBadge", l, 30, null, null, null)));
        Assert.IsTrue((bool)Static("HfsmGraphPresentation", "ShowBadge", links[0], 30, null, Get(links[0], "Edge"), null));
        Assert.IsTrue((bool)Static("HfsmGraphPresentation", "ShowBadge", links[0], 30, null, null, Get(links[0], "Edge")));
    }

    [Test] public void NumberBadgesRejectNodesOtherBadgesAndCanvasEdges()
    {
        var allowed = new Rect(0, 0, 400, 400);
        var obstacles = new[] { new Rect(100, 100, 100, 80), new Rect(260, 260, 42, 22) };
        bool Place(Vector2 point) { var args = new object[] { point, allowed, obstacles, new Rect() }; return (bool)Static("HfsmGraphPresentation", "TryBadge", args); }
        Assert.IsFalse(Place(new Vector2(130, 130)));
        Assert.IsFalse(Place(new Vector2(280, 271)));
        Assert.IsFalse(Place(Vector2.zero));
        Assert.IsTrue(Place(new Vector2(50, 230)));
    }

    [Test] public void LongIdentifiersAndDescriptionsFitNarrowCardsWithoutFixedHeightClipping()
    {
        var style = new GUIStyle(EditorStyles.miniButton) { fixedHeight = 0, wordWrap = true, padding = new RectOffset(6, 6, 6, 6) };
        string text = "EnemyShooter_ShouldTransitionIntoMeleeAttackAfterInvestigatingLastKnownTargetPositionCondition\n우선순위 10 · 설명이 여러 줄에 걸쳐 표시되어도 행이 겹치지 않아야 합니다.";
        foreach (float width in new[] { 174f, 204f, 284f, 500f })
        {
            string wrapped = (string)Static("HfsmGraphPresentation", "WrapText", style, text, width);
            foreach (string line in wrapped.Split('\n')) Assert.LessOrEqual(style.CalcSize(new GUIContent(line)).x, width + 1);
            float height = (float)Static("HfsmGraphPresentation", "TextHeight", style, text, width);
            Assert.GreaterOrEqual(height, style.CalcHeight(new GUIContent(wrapped), width));
            Assert.Greater(height, 38);
        }
    }
    [Test] public void GraphCardTextHasReadableContrastForNormalSelectedCompositeAndActiveCards()
    {
        float Channel(float x) => x <= .04045f ? x / 12.92f : Mathf.Pow((x + .055f) / 1.055f, 2.4f);
        float Luminance(Color c) => .2126f * Channel(c.r) + .7152f * Channel(c.g) + .0722f * Channel(c.b);
        foreach (bool active in new[] { false, true }) foreach (bool selected in new[] { false, true }) foreach (bool composite in new[] { false, true })
        {
            var background = (Color)Static("HfsmGraphPresentation", "CardColor", active, selected, composite);
            foreach (string field in new[] { "CardText", "CardDetailText" })
            {
                var foreground = (Color)EditorType("HfsmGraphPresentation").GetProperty(field, Flags).GetValue(null);
                Assert.GreaterOrEqual((Luminance(foreground) + .05f) / (Luminance(background) + .05f), 4.5f, field);
            }
        }
    }
}
