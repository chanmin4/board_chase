using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VSplatter.StateMachine;
using VSplatter.StateMachine.Editor;
using VSplatter.StateMachine.ScriptableObjects;

/// <summary>Explicit UI check in a separate window. No global Undo, scene objects or production saves.</summary>
public static class HfsmGraphUiVerificationRunner
{
    public const string Report = "Library/HfsmUnified-20261002/graph-ui-readability-v3.txt";
    private const string PrefabPath = "Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyBasePrefab.prefab";
    private static readonly string[] CharacterPaths = {
        PrefabPath, "Assets/Prefabs/GameScene/Shooter/Player/Player.prefab",
        "Assets/Prefabs/GameScene/Shooter/EnemyShooter/Scraper/Scraper.prefab",
        "Assets/Prefabs/GameScene/Shooter/EnemyShooter/Raider/Raider.prefab",
        "Assets/Prefabs/GameScene/Shooter/EnemyShooter/Agent/Agent.prefab",
        "Assets/Prefabs/GameScene/Shooter/EnemyShooter/Boss/Punisher.prefab",
        "Assets/Prefabs/GameScene/Shooter/EnemyShooter/Boss/Stage1RevolverBoss.prefab",
        "Assets/Prefabs/GameScene/Shooter/EnemyShooter/Boss/Stage2UniqueSniperBoss.prefab",
        "Assets/Prefabs/GameScene/Shooter/EnemyShooter/Boss/Stage3RocketBoss.prefab",
        "Assets/Prefabs/GameScene/Shooter/EnemyShooter/Boss/Stage4LMGBoss.prefab",
        "Assets/Prefabs/GameScene/Shooter/EnemyShooter/Boss/Stage5BossTemplate.prefab"
    };
    private static readonly BindingFlags Flags = HfsmGraphPresentationTests.Flags;
    private static readonly List<string> Lines = new();
    private static readonly Dictionary<string, Rect> Controls = new();
    private static EditorWindow _window, _previous;
    private static GameObject _prefab;
    private static string _prefabBefore;
    private static bool _prefabDirty;
    private static Dictionary<string, string> _filesBefore;
    private static Dictionary<int, string> _scenesBefore;
    private static int _phase, _frames, _passed, _failed;
    private static double _started;
    private static string _layoutKey, _foldKey;
    private static readonly Dictionary<string, (bool exists, float value)> FloatPrefs = new();
    private static readonly Dictionary<string, (bool exists, bool value)> BoolPrefs = new();
    private static readonly List<UnityEngine.Object> TempObjects = new();
    private static string _memoryBefore;
    private static int _routeCacheAtSelection;
    private static double _inspectionStarted;
    public static void Run()
    {
        if (_window != null || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        Lines.Clear(); Controls.Clear(); FloatPrefs.Clear(); BoolPrefs.Clear(); _phase = _frames = _passed = _failed = 0;
        _layoutKey = _foldKey = null;
        _previous = EditorWindow.focusedWindow; _filesBefore = Files(); _scenesBefore = Scenes();
        // These fixtures only allocate managed objects / memory SOs; no live-editor global Undo tests.
        foreach (var fixtureType in new[] { typeof(HfsmGraphPresentationTests), typeof(HfsmRuntimeTests), typeof(HfsmDefinitionTests) })
        foreach (var test in fixtureType.GetMethods().Where(m => m.IsDefined(typeof(TestAttribute))))
        {
            if (test.Name == nameof(HfsmGraphPresentationTests.LongIdentifiersAndDescriptionsFitNarrowCardsWithoutFixedHeightClipping)) continue;
            var fixture = Activator.CreateInstance(fixtureType);
            try { test.Invoke(fixture, null); Check(true, fixtureType.Name + "." + test.Name); }
            catch (Exception ex) { Fail(test.Name + ": " + (ex.InnerException ?? ex)); }
            finally { foreach (var cleanup in fixtureType.GetMethods().Where(m => m.IsDefined(typeof(TearDownAttribute)))) cleanup.Invoke(fixture, null); }
        }
        try
        {
            VerifyCharacterHierarchyLabels();
            RememberFloat("HFSM.BrowserWidth.v2"); RememberFloat("HFSM.InspectorWidth.v2");
            _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            _prefabBefore = EditorJsonUtility.ToJson(_prefab); _prefabDirty = EditorUtility.IsDirty(_prefab);
            _window = (EditorWindow)ScriptableObject.CreateInstance(HfsmGraphPresentationTests.EditorType("HFSMTransitionGraphWindow"));
            Set("_browserWidth", 215f); Set("_inspectorWidth", 330f); Set("_showGuide", true); Set("_showRuleMap", false);
            _window.titleContent = new GUIContent("HFSM UI verification"); _window.position = new Rect(35, 45, 1470, 860);
            Set("ObserveControl", (Action<string, Rect>)((id, rect) => Controls[id] = rect));
            Call("SetOwner", _prefab); _window.ShowUtility();
            _started = EditorApplication.timeSinceStartup; Application.logMessageReceived += OnLog; EditorApplication.update += Tick;
        }
        catch (Exception ex) { Fail("Setup: " + ex); Finish(); }
    }
    private static object Get(string name) => HfsmGraphPresentationTests.Get(_window, name);
    private static void Set(string name, object value) => _window.GetType().GetField(name, Flags).SetValue(_window, value);
    private static object Call(string name, params object[] args) => HfsmGraphPresentationTests.Call(_window, name, args);
    private static object[] Items(object list) => HfsmGraphPresentationTests.Items(list);
    private static string Name(object node) => ((StateSO)HfsmGraphPresentationTests.Get(node, "State")).name;
    private static object Root => HfsmGraphPresentationTests.Get(Get("_model"), "Root");
    private static object Alive => Items(HfsmGraphPresentationTests.Get(Root, "Nodes")).Single(n => Name(n) == "Alive_Shooter");
    private static void Tick()
    {
        try
        {
            if (_window == null) throw new Exception("Verification window closed early.");
            if (EditorApplication.timeSinceStartup - _started > 80) throw new Exception("UI verification timed out.");
            _window.Repaint(); if (++_frames < 12 || !Controls.ContainsKey("Canvas")) return; _frames = 0;
            switch (_phase++)
            {
                case 0:
                    // EditorStyles only has a valid skin after the first actual GUI frame.
                    new HfsmGraphPresentationTests().LongIdentifiersAndDescriptionsFitNarrowCardsWithoutFixedHeightClipping();
                    Check(true, "Long-name font measurements run after a real GUI frame");
                    Check(Items(Get("_visibleNodes")).Length == 3, "Enemy Base root shows only Spawn / Alive / Dead");
                    Check((bool)Get("_showAllTransitions") && Items(Get("_shownLinks")).Length == 2, "All root arrows are visible by default without clicking a state");
                    Check(Items(Get("_visibleNodes")).All(n => (int)HfsmGraphPresentationTests.Get(n, "Level") == 1), "Root state labels show level 1");
                    CheckRoutes(); Check((float)Get("_zoom") >= .8f, "Default zoom keeps state cards readable instead of shrinking everything");
                    CheckPanels(); Click("State/Alive_Shooter", 2);
                    _window.SendEvent(new Event { type = EventType.MouseMove, mousePosition = Vector2.zero }); break;
                case 1:
                    Check(Items(Get("_navigation")).Length == 1 && Items(Get("_visibleNodes")).Length == 7, "Double-click Alive opens its actual seven-state child FSM");
                    Check(Items(Get("_shownLinks")).Length == Items(Get("_scopeLinks")).Length, "Internal level arrows are also visible before a state is selected");
                    Check(Items(Get("_visibleNodes")).All(n => (int)HfsmGraphPresentationTests.Get(n, "Level") == 2), "Alive internal state labels show level 2");
                    CheckRoutes();
                    Check(!Controls.Keys.Any(k => k.StartsWith("EdgeBadge/")), "Dense default view shows all arrows without a wall of number buttons");
                    _routeCacheAtSelection = (int)Get("_routeBuildCount");
                    var childNode = Items(Get("_visibleNodes"))[0]; Click("State/" + Name(childNode)); break;
                case 2:
                    Check(Get("_selectedNode") != null && Items(Get("_shownLinks")).Length > 0, "State click selects inspector and related transition lines");
                    Check(Items(Get("_shownLinks")).Length == Items(Get("_scopeLinks")).Length, "Selecting a state keeps other arrows visible in the default view");
                    Check((int)Get("_routeBuildCount") == _routeCacheAtSelection, "State selection reuses routed geometry instead of recalculating paths");
                    Check(Controls.Keys.Any(k => k.StartsWith("TransitionRow/")), "Inspector renders real transition row buttons");
                    CheckDetailRows(); CheckBadges(); CheckRoutes();
                    Click(Controls.Keys.First(k => k.StartsWith("TransitionRow/"))); break;
                case 3:
                    Check(Get("_selectedEdge") != null && Get("_selectedNode") == null, "Transition list click selects the original table row");
                    Click("AllTransitionsToggle"); break;
                case 4:
                    Check(!(bool)Get("_showAllTransitions") && Items(Get("_shownLinks")).Length == 1, "Optional related-only toggle leaves the selected transition visible");
                    Click("AllTransitionsToggle"); Call("NavigateToScope", Root); break;
                case 5:
                    Check(Items(Get("_navigation")).Length == 0 && Items(Get("_visibleNodes")).Length == 3, "Breadcrumb navigation returns to the uncluttered parent level");
                    Check((bool)Get("_showAllTransitions") && Items(Get("_shownLinks")).Length == 2, "Returning to root preserves all-arrow visibility");
                    _foldKey = (string)HfsmGraphPresentationTests.Static("HfsmGraphModel", "LayoutKey", Alive) + ".expanded"; RememberBool(_foldKey);
                    Click("TreeFold/" + HfsmGraphPresentationTests.Get(Alive, "Key")); break;
                case 6:
                    Check(Items(Get("_visibleNodes")).Length == 3, "Tree fold never expands all child states onto the canvas");
                    var state = Items(Get("_visibleNodes"))[0];
                    _layoutKey = (string)_window.GetType().GetMethod("CanvasLayoutKey", Flags).Invoke(null, new[] { state });
                    RememberFloat(_layoutKey + ".x"); RememberFloat(_layoutKey + ".y");
                    var card = Controls["State/" + Name(state)]; Drag(card.center, new Vector2(25, 14)); break;
                case 7:
                    Check(EditorPrefs.HasKey(_layoutKey + ".x") && EditorPrefs.HasKey(_layoutKey + ".y"), "Card drag changes only editor layout preferences");
                    CheckRoutes();
                    var browser = Controls["Browser"]; Drag(new Vector2(browser.xMax + 2, browser.center.y), new Vector2(26, 0)); break;
                case 8:
                    Check((float)Get("_browserWidth") > 215, "Panel boundary drag resizes the hierarchy pane"); CheckPanels();
                    _window.position = new Rect(35, 45, 820, 620); break;
                case 9:
                    CheckPanels(); Check(_window.position.width >= 800, "Small-window toolbar and panels render without layout errors");
                    Set("_dirty", true); break;
                case 10:
                    Check(Items(Get("_visibleNodes")).Length == 3, "Graph rebuild preserves a one-level view");
                    Call("OpenInside", Alive); break;
                case 11:
                    Set("_inspectorWidth", 250f); Set("_stateDetailTab", 1);
                    Call("SelectNode", Items(Get("_visibleNodes"))[0]); break;
                case 12:
                    Check(Controls.Keys.Any(k => k.StartsWith("ActionText/")), "Actual Enemy state actions use readable wrapped cards");
                    CheckDetailRows(); CheckBadges();
                    CreateLongNameFixture(); break;
                case 13:
                    Call("SelectNode", Items(Get("_visibleNodes"))[0]); Set("_outgoingExpanded", true); Set("_stateDetailTab", 0); Set("_inspectorWidth", 220f); break;
                case 14:
                    CheckDetailRows(); CheckBadges();
                    Check(Controls.Where(p => p.Key.StartsWith("TransitionRow/")).All(p => p.Value.height > 38), "Long target names allocate more than the old fixed row height");
                    string conditionControl = Controls.Keys.First(k => k.StartsWith("TransitionConditions/"));
                    if (!RevealInspectorControl(conditionControl)) { _phase--; break; }
                    Click(conditionControl); break;
                case 15:
                    Check(Controls.Keys.Any(k => k.StartsWith("ConditionText/")), "Expanded conditions render full wrapped names");
                    CheckDetailRows();
                    if (!RevealInspectorControl("StateDetailTab")) { _phase--; break; }
                    var tabs = Controls["StateDetailTab"];
                    ClickAt(new Vector2(tabs.x + tabs.width * .75f, tabs.center.y)); break;
                case 16:
                    Check(Controls.Keys.Any(k => k.StartsWith("ActionDescription/")), "Long action description renders in its own wrapped text block");
                    Check((int)Get("_stateDetailTab") == 1 && !Controls.Keys.Any(k => k.StartsWith("TransitionRow/")), "Action tab separates actions from the long transition list");
                    CheckDetailRows();
                    Set("_inspectorWidth", 430f); _window.position = new Rect(35, 45, 1470, 860); break;
                case 17:
                    CheckDetailRows(); CheckBadges();
                    Check(_memoryBefore == MemoryJson(), "Browsing long-name fixture never changes action order or transition data");
                    _window.position = new Rect(35, 45, 820, 620); Set("_inspectorWidth", 220f); break;
                case 18:
                    Click("OverviewButton"); break;
                case 19:
                    Check((float)Get("_zoom") < .8f, "Overview button explicitly zooms out to fit the larger graph");
                    Click("ReadableButton"); break;
                case 20:
                    Check((float)Get("_zoom") >= .8f, "Readable button restores legible state cards");
                    CheckRoutes(); Call("SetOwner", _prefab); Set("_showRuleMap", true); break;
                case 21:
                    Call("OpenInside", Alive); break;
                case 22:
                    Check(Items(Get("_ruleMapGroups")).Length == 15, "Unified definition still exposes six shared rules plus nine local exceptions");
                    Check(Controls.Keys.Any(k => k.StartsWith("RuleMapArrow/")), "Rule-map arrows remain visible without selecting a state");
                    Check(((UnityEngine.Object)Get("_definition")).name == "EnemyShooter_HFSM", "Graph navigates the unified definition instead of a coordinator");
                    var textStyle = (GUIStyle)Get("_ruleCardText");
                    Check(textStyle.normal.textColor == (Color)HfsmGraphPresentationTests.EditorType("HfsmGraphPresentation").GetProperty("CardText", Flags).GetValue(null), "Rule cards use explicit bright text instead of skin-dependent black text");
                    Check(Controls.Keys.Any(k => k.StartsWith("RuleMapCardGroup/") && k.EndsWith("/Background")), "Rule cards draw solid bordered rectangular backgrounds");
                    Check(Controls.Keys.Any(k => k.StartsWith("RuleMapCardTarget/") && k.EndsWith("/Background")), "Destination states also draw solid rectangular backgrounds");
                    foreach (var cardPair in Controls.Where(p => p.Key.StartsWith("RuleMapCard") && p.Key.EndsWith("/Background")))
                    {
                        var textRect = Controls[cardPair.Key.Replace("/Background", "/Text")];
                        Check(cardPair.Value.Contains(textRect.min) && cardPair.Value.Contains(textRect.max), "Bright card text stays inside its bordered background");
                    }
                    var sharedGroup = Items(Get("_ruleMapGroups")).First(g => HfsmGraphPresentationTests.Get(g, "Rule") != null);
                    Click("RuleMapGroup/" + HfsmGraphPresentationTests.Get(sharedGroup, "Key")); break;
                case 23:
                    Check(Get("_selectedRule") != null, "Bordered shared rule card still opens the shared-rule inspector");
                    var localGroup = Items(Get("_ruleMapGroups")).First(g => HfsmGraphPresentationTests.Get(g, "Rule") == null);
                    Click("RuleMapGroup/" + HfsmGraphPresentationTests.Get(localGroup, "Key")); break;
                case 24:
                    Check(Get("_selectedEdge") != null && Get("_selectedRule") == null, "Bordered local exception card still selects the original transition row");
                    Click(Controls.Keys.First(k => k.StartsWith("RuleMapTarget/"))); break;
                case 25:
                    Check(Get("_selectedNode") != null && Get("_selectedEdge") == null, "Bordered destination card still selects the real state");
                    Set("_inspectorWidth", 330f); _window.position = new Rect(35, 45, 1470, 860);
                    Lines.Add("INFO readability inspection hold (no asset writes)"); _inspectionStarted = EditorApplication.timeSinceStartup; break;
                case 26:
                    if (EditorApplication.timeSinceStartup - _inspectionStarted < 15) { _phase--; break; }
                    Finish(); break;
            }
            Controls.Clear();
        }
        catch (Exception ex) { Fail("UI: " + (ex.InnerException ?? ex)); Finish(); }
    }
    private static void CheckRoutes()
    {
        var paths = Items(Get("_edgeRoutes")).Select(pair => (Vector2[])HfsmGraphPresentationTests.Get(pair, "Value")).ToArray();
        var boxes = Items(Get("_visibleNodes")).Select(n => (Rect)HfsmGraphPresentationTests.Get(n, "Bounds")).ToArray();
        Check(paths.Length == Items(Get("_scopeLinks")).Length, "Every original transition has a separate routed path");
        Check(paths.All(path => Enumerable.Range(1, path.Length - 1).All(i =>
            Mathf.Abs(path[i].x - path[i - 1].x) < .05f || Mathf.Abs(path[i].y - path[i - 1].y) < .05f)), "Arrows follow orthogonal corridors instead of diagonal curves");
        Check(paths.All(path => Enumerable.Range(1, path.Length - 1).All(i => boxes.All(box =>
            !(bool)HfsmGraphPresentationTests.Static("HfsmGraphRouting", "CrossesCard", path[i - 1], path[i], box)))), "Routed arrows never pass through state cards");
        Lines.Add("INFO route build ms=" + ((double)Get("_routeBuildMilliseconds")).ToString("F1"));
    }
    private static void VerifyCharacterHierarchyLabels()
    {
        foreach (string path in CharacterPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(prefab != null, "Character hierarchy exists: " + path);
            string before = EditorJsonUtility.ToJson(prefab); bool dirty = EditorUtility.IsDirty(prefab);
            var machines = prefab.GetComponentsInChildren<StateMachine>(true).Where(m => m.Table != null).ToArray();
            Check(machines.Length > 0, "Character has state machine data: " + prefab.name);
            foreach (var machine in machines)
            {
                var model = Activator.CreateInstance(HfsmGraphPresentationTests.EditorType("HfsmGraphModel"));
                HfsmGraphPresentationTests.Call(model, "Build", machine.Table, machine, prefab);
                var root = HfsmGraphPresentationTests.Get(model, "Root");
                int level = (int)HfsmGraphPresentationTests.Call(model, "LevelForMachine", machine);
                Check((int)HfsmGraphPresentationTests.Get(root, "Level") == level, "Direct machine selection retains its owner hierarchy level: " + prefab.name + "/" + machine.Table.name);
                foreach (var scope in Items(HfsmGraphPresentationTests.Static("HfsmGraphModel", "AllScopes", root)))
                {
                    int depth = (int)HfsmGraphPresentationTests.Get(scope, "Level");
                    var owner = HfsmGraphPresentationTests.Get(scope, "OwnerNode");
                    Check(owner == null || depth == (int)HfsmGraphPresentationTests.Get(owner, "Level") + 1, "Nested scope increments the visible state level: " + prefab.name);
                    Check(Items(HfsmGraphPresentationTests.Get(scope, "Nodes")).All(n =>
                        (string)HfsmGraphPresentationTests.Static("HfsmGraphPresentation", "StateLabel", n) == Name(n) + "_" + depth), "Every state uses name_level labels: " + prefab.name + "/" + depth);
                }
                var bindings = Items(HfsmGraphPresentationTests.Get(model, "Bindings")).Cast<StateMachineChildBinding>();
                if (bindings.Any(b => b.Enabled && b.Child == machine && b.Parent != null && b.ParentState != null))
                    Check(level > 1, "Controlled machine stays below level 1 even when selected directly: " + prefab.name + "/" + machine.Table.name);
            }
            Check(before == EditorJsonUtility.ToJson(prefab) && dirty == EditorUtility.IsDirty(prefab), "Hierarchy labels never change loaded character data: " + prefab.name);
        }
    }
    private static void CheckDetailRows()
    {
        var pane = Controls["Inspector"];
        var rows = Controls.Where(p => p.Key.StartsWith("TransitionRow/") || p.Key.StartsWith("ConditionText/")
            || p.Key.StartsWith("ActionText/") || p.Key.StartsWith("ActionDescription/")).Select(p => p.Value).OrderBy(r => r.y).ToArray();
        Check(rows.Length > 0, "Inspector text geometry is captured");
        for (int i = 0; i < rows.Length; i++)
        {
            Check(rows[i].x >= pane.x && rows[i].xMax <= pane.xMax, "Wrapped detail stays within the narrow inspector width");
            if (i > 0) Check(rows[i].y >= rows[i - 1].yMax - 1, "Long detail rows do not overlap vertically");
        }
    }
    private static void CheckBadges()
    {
        var badges = Controls.Where(p => p.Key.StartsWith("EdgeBadge/")).Select(p => p.Value).ToArray();
        var states = Controls.Where(p => p.Key.StartsWith("State/")).Select(p => p.Value).ToArray();
        Check(badges.All(b => states.All(s => !s.Overlaps(b))), "Transition number badges never cover state cards");
        for (int i = 0; i < badges.Length; i++) for (int j = i + 1; j < badges.Length; j++)
            Check(!badges[i].Overlaps(badges[j]), "Transition number badges never overlap each other");
        if (Items(Get("_shownLinks")).Length > 0 && (float)Get("_zoom") >= .45f)
            Check(badges.Length > 0, "Shown transitions expose compact number badges at readable zoom");
    }
    private static T Temp<T>() where T : ScriptableObject { var obj = ScriptableObject.CreateInstance<T>(); TempObjects.Add(obj); return obj; }
    private static void CreateLongNameFixture()
    {
        var table = Temp<TransitionTableSO>(); table.name = "Temporary long-name rendering test";
        var a = Temp<StateSO>(); a.name = "EnemyShooter_ReallyLongIdleStateNameForNarrowInspectorVerification";
        var b = Temp<StateSO>(); b.name = "EnemyShooter_MeleeAttackAfterInvestigatingLastKnownTargetPositionState";
        var condition = Temp<HfsmProbeConditionSO>(); condition.name = "EnemyShooter_ShouldTransitionIntoMeleeAttackAfterInvestigatingLastKnownTargetPositionCondition";
        var action = Temp<HfsmProbeActionSO>(); action.name = "EnemyShooter_AcquireTargetAndUpdateAnimatorParametersWithVeryLongActionName";
        action.description = "메모리 테스트 설명입니다. 좁은 오른쪽 패널에서도 설명의 모든 줄이 보이고 다음 행동 카드와 겹치지 않아야 합니다. This long description must wrap rather than overlap the next row.";
        var state = new SerializedObject(a); state.FindProperty("_actions").arraySize = 1; state.FindProperty("_actions").GetArrayElementAtIndex(0).objectReferenceValue = action; state.ApplyModifiedPropertiesWithoutUndo();
        typeof(TransitionTableSO).GetField("_transitions", Flags).SetValue(table, Enumerable.Range(0, 6).Select(i => new TransitionTableSO.TransitionItem {
            FromState = a, ToState = b, Priority = i, Conditions = new[] { new TransitionTableSO.ConditionUsage { Condition = condition, ExpectedResult = TransitionTableSO.Result.True } }
        }).ToArray());
        _memoryBefore = MemoryJson(); Call("SetTable", table);
    }
    private static string MemoryJson() => string.Join("\n", TempObjects.Select(obj => EditorJsonUtility.ToJson(obj)));
    private static void CheckPanels()
    {
        var b = Controls["Browser"]; var c = Controls["Canvas"]; var i = Controls["Inspector"];
        Check(b.y > 70 && Mathf.Abs(b.y - c.y) < 2 && Mathf.Abs(i.y - c.y) < 2, "Side panels start below the toolbar/guide at the canvas height");
        Check(!b.Overlaps(c) && !i.Overlaps(c) && c.width >= 290, "Side panels cannot overlap or squeeze away the canvas");
        foreach (var card in Controls.Where(p => p.Key.StartsWith("Tree/")))
            Check(card.Value.y >= b.y && card.Value.x >= b.x, "Tree button is below its pane header");
    }
    private static void Click(string id, int count = 1)
    {
        ClickAt(Controls[id].center, count);
    }
    private static bool RevealInspectorControl(string id)
    {
        var pane = Controls["Inspector"]; var control = Controls[id];
        if (control.center.y >= pane.y + 35 && control.center.y <= pane.yMax - 24) return true;
        var scroll = (Vector2)Get("_inspectorScroll");
        scroll.y = Mathf.Max(0, scroll.y + control.center.y - (pane.y + 100));
        Set("_inspectorScroll", scroll);
        Lines.Add("INFO scroll temporary inspector to visible control: " + id);
        return false; // Wait for the next GUI frame before sending the real mouse click.
    }
    private static void ClickAt(Vector2 point, int count = 1)
    {
        _window.SendEvent(new Event { type = EventType.MouseDown, button = 0, clickCount = count, mousePosition = point });
        _window.SendEvent(new Event { type = EventType.MouseUp, button = 0, clickCount = count, mousePosition = point });
    }
    private static void Drag(Vector2 from, Vector2 delta)
    {
        _window.SendEvent(new Event { type = EventType.MouseDown, button = 0, clickCount = 1, mousePosition = from });
        _window.SendEvent(new Event { type = EventType.MouseDrag, button = 0, mousePosition = from + delta, delta = delta });
        _window.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = from + delta });
    }
    private static void RememberFloat(string key) { if (!FloatPrefs.ContainsKey(key)) FloatPrefs[key] = (EditorPrefs.HasKey(key), EditorPrefs.GetFloat(key)); }
    private static void RememberBool(string key) { if (!BoolPrefs.ContainsKey(key)) BoolPrefs[key] = (EditorPrefs.HasKey(key), EditorPrefs.GetBool(key)); }
    private static void OnLog(string condition, string stack, LogType type)
    { if ((type == LogType.Error || type == LogType.Exception) && (stack.Contains("HFSMTransitionGraphWindow") || condition.Contains("GUILayout"))) Fail("GUI: " + condition + "\n" + stack); }
    private static Dictionary<string, string> Files()
    {
        using var sha = SHA256.Create();
        return new[] { "t:TransitionTableSO", "t:StateSO" }.SelectMany(AssetDatabase.FindAssets).Select(AssetDatabase.GUIDToAssetPath)
            .Concat(CharacterPaths).Distinct().Where(File.Exists).ToDictionary(p => p, p => Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(p))));
    }
    private static Dictionary<int, string> Scenes()
    {
        var result = new Dictionary<int, string>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            result[s.handle] = s.path + "|" + s.isDirty + "|" + string.Join(",", s.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.GetInstanceID()).OrderBy(id => id));
        }
        return result;
    }
    private static void Check(bool ok, string label) { if (!ok) throw new Exception(label); Lines.Add("PASS " + label); _passed++; }
    private static void Fail(string label) { Lines.Add("FAIL " + label); _failed++; }
    private static void Finish()
    {
        EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
        if (_window != null) _window.Close(); _window = null;
        foreach (var obj in TempObjects) if (obj != null) UnityEngine.Object.DestroyImmediate(obj); TempObjects.Clear();
        foreach (var p in FloatPrefs) { if (p.Value.exists) EditorPrefs.SetFloat(p.Key, p.Value.value); else EditorPrefs.DeleteKey(p.Key); }
        foreach (var p in BoolPrefs) { if (p.Value.exists) EditorPrefs.SetBool(p.Key, p.Value.value); else EditorPrefs.DeleteKey(p.Key); }
        try
        {
            var files = Files(); var scenes = Scenes();
            Check(_filesBefore.Count == files.Count && _filesBefore.All(p => files.TryGetValue(p.Key, out var value) && value == p.Value), "Real state/table/prefab asset files unchanged");
            Check(_scenesBefore.Count == scenes.Count && _scenesBefore.All(p => scenes.TryGetValue(p.Key, out var value) && value == p.Value), "Working scene membership and dirty flags unchanged");
            Check(_prefab != null && _prefabBefore == EditorJsonUtility.ToJson(_prefab) && _prefabDirty == EditorUtility.IsDirty(_prefab), "Loaded Enemy Base prefab and dirty flag unchanged");
        }
        catch (Exception ex) { Fail("Preservation: " + ex); }
        if (_previous != null) _previous.Focus();
        Lines.Add($"{_passed} passed / {_failed} failed. Editor presentation only; no global Undo, prefab/scene save or runtime change.");
        Directory.CreateDirectory(Path.GetDirectoryName(Report)); File.WriteAllLines(Report, Lines);
        Debug.Log("[HFSM level UI verification] " + Lines.Last() + " " + Report);
    }
}
