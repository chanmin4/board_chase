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
using VSplatter.StateMachine.Editor;
using VSplatter.StateMachine.ScriptableObjects;

/// <summary>Explicit verification; migration requires an explicit argument. Default is read-only. No scene/prefab saves, global Undo or GameObjects.</summary>
public static class HfsmSharedRuleVerificationRunner
{
    public const string Report = "Library/HfsmRefactor-20260929/shared-rules-verification-v2.txt";
    private const string TablePath = "Assets/ScriptableObject/StateMachine/Shooter/EnemyShooter/Enemy_ShooterBase_Alive.asset";
    private const string PrefabPath = "Assets/Prefabs/GameScene/Shooter/EnemyShooter/EnemyBasePrefab.prefab";
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static readonly List<string> Lines = new();
    private static readonly List<UnityEngine.Object> Owned = new();
    private static readonly Dictionary<string, Rect> Controls = new();
    private static EditorWindow _window, _previous;
    private static TransitionTableSO _real, _preview;
    private static Dictionary<string, string> _files;
    private static string _sceneSignature, _semantic, _memoryJson;
    private static bool _tableWasDirty, _allowMigration;
    private static int _passed, _failed, _phase, _frames, _expectedGroups, _expectedRows, _expectedShared;
    private static double _started;
    private static readonly Dictionary<string, (bool exists, float value)> Preferences = new();

    public static void Run(bool allowMigration = false)
    {
        if (_window != null || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        Lines.Clear(); Controls.Clear(); Owned.Clear(); Preferences.Clear(); _passed = _failed = _phase = _frames = 0;
        _allowMigration = allowMigration;
        _files = Files(); _sceneSignature = Scenes(); _previous = EditorWindow.focusedWindow;
        foreach (var type in new[] { typeof(HfsmRuntimeTests), typeof(HfsmCompatibilityTests), typeof(HfsmDefinitionTests), typeof(HfsmSharedRuleTests), typeof(HfsmGraphPresentationTests) })
        foreach (var method in type.GetMethods().Where(m => m.IsDefined(typeof(TestAttribute))))
        {
            object fixture = Activator.CreateInstance(type);
            try { method.Invoke(fixture, null); Check(true, type.Name + "." + method.Name); }
            catch (Exception ex) { Fail(type.Name + "." + method.Name + ": " + (ex.InnerException ?? ex)); }
            finally { foreach (var cleanup in type.GetMethods().Where(m => m.IsDefined(typeof(TearDownAttribute)))) cleanup.Invoke(fixture, null); }
        }
        if (_failed > 0) { Finish(); return; }
        try
        {
            _real = AssetDatabase.LoadAssetAtPath<TransitionTableSO>(TablePath); if (_real == null) throw new Exception("Missing Enemy Base Alive table");
            _semantic = HfsmSharedRuleMigration.SemanticSignature(_real); _tableWasDirty = EditorUtility.IsDirty(_real);
            _preview = HfsmSharedRuleMigration.Preview(_real, Owned); _expectedRows = _real.Transitions.Count;
            _expectedShared = _preview.Transitions.Where(r => r.UseSharedRule).Select(r => r.SharedRule).Distinct().Count();
            _memoryJson = string.Join("\n", Owned.Select(o => EditorJsonUtility.ToJson(o)));
            foreach (string key in new[] { "HFSM.BrowserWidth.v2", "HFSM.InspectorWidth.v2" }) Preferences[key] = (EditorPrefs.HasKey(key), EditorPrefs.GetFloat(key));
            var type = typeof(HfsmGraphAuthoring).Assembly.GetType("VSplatter.StateMachine.Editor.HFSMTransitionGraphWindow");
            _window = (EditorWindow)ScriptableObject.CreateInstance(type); _window.titleContent = new GUIContent("HFSM shared rule verification");
            _window.position = new Rect(35, 45, 1450, 880);
            Set("_browserWidth", 215f); Set("_inspectorWidth", 330f); Set("_showGuide", false);
            Set("ObserveControl", (Action<string, Rect>)((id, rect) => Controls[id] = rect));
            Call("SetTable", _preview); _window.ShowUtility(); _started = EditorApplication.timeSinceStartup;
            Application.logMessageReceived += OnLog; EditorApplication.update += Tick;
        }
        catch (Exception ex) { Fail("Setup: " + ex); Finish(); }
    }

    private static object Get(string field) => _window.GetType().GetField(field, Flags)?.GetValue(_window) ?? _window.GetType().GetProperty(field, Flags)?.GetValue(_window);
    private static void Set(string field, object value) => _window.GetType().GetField(field, Flags).SetValue(_window, value);
    private static object Call(string method, params object[] args) => _window.GetType().GetMethod(method, Flags).Invoke(_window, args);
    private static object[] Items(object value) => ((IEnumerable)value).Cast<object>().ToArray();
    private static object Field(object value, string name) => value.GetType().GetField(name, Flags).GetValue(value);
    private static object Root => Get("_model").GetType().GetProperty("Root").GetValue(Get("_model"));
    private static void Tick()
    {
        try
        {
            if (_window == null) throw new Exception("Verification window closed early");
            if (EditorApplication.timeSinceStartup - _started > 90) throw new Exception("Verification timed out");
            _window.Repaint(); if (++_frames < 14 || !Controls.ContainsKey("Canvas")) return; _frames = 0;
            switch (_phase++)
            {
                case 0:
                    Check(Controls.ContainsKey("RuleMapCanvas"), "Shared table opens the orderly rule view by default");
                    CheckGroups(); CheckCards(); _expectedGroups = Items(Get("_ruleMapGroups")).Length;
                    Click(Controls.Keys.First(k => k.StartsWith("RuleMapGroup/shared/"))); break;
                case 1:
                    Check(Get("_selectedRule") != null && Get("_selectedNode") == null, "Shared card selects a rule, not a fake state or hierarchy");
                    Check(Controls.Keys.Any(k => k.StartsWith("SharedUsage/")), "Rule inspector displays explicit sources and their local evaluation positions");
                    Set("_showRuleMap", false); Set("_selectedRule", null); break;
                case 2:
                    Check(Items(Get("_shownLinks")).Length == _expectedRows, "Original line view still includes every real transition");
                    Check(Items(Get("_visibleNodes")).Length == _real.Transitions.SelectMany(r => new[] { r.FromState, r.ToState }).Distinct().Count(), "Rule grouping did not remove states");
                    Call("SelectNode", Items(Field(Root, "Nodes"))[0]); break;
                case 3:
                    Check(_failed == 0, "No GUI errors occurred before the production migration");
                    Check(Controls.Keys.Any(k => k.StartsWith("TransitionRow/")), "Selected state shows the ordered original row identities");
                    Check(_memoryJson == string.Join("\n", Owned.Select(o => EditorJsonUtility.ToJson(o))), "Preview navigation/selection did not edit any memory definition");
                    Check(_semantic == HfsmSharedRuleMigration.SemanticSignature(_real) && EditorUtility.IsDirty(_real) == _tableWasDirty, "Real table remained unchanged during memory/UI checks");
                    Check(_files.All(p => Hash(p.Key) == p.Value), "All production definitions/prefabs stayed unchanged before migration");
                    Check(_sceneSignature == Scenes(), "Work scenes stayed unchanged before migration");
                    if (_allowMigration)
                    {
                        var created = HfsmSharedRuleMigration.ConsolidateSavedTable(_real, "Library/HfsmRefactor-20260929/SharedRuleBackups");
                        Lines.Add("INFO migrated new rule assets: " + created.Length + "\n" + string.Join("\n", created));
                    }
                    else
                    {
                        Check(HfsmSharedRuleMigration.RepeatedInlineGroups(_real).Count == 0, "Read-only follow-up: migration is already complete; no asset save is executed");
                        Lines.Add("INFO read-only verification of the already migrated table");
                    }
                    Check(_semantic == HfsmSharedRuleMigration.SemanticSignature(_real), "Migration kept entry, row order, priority, full condition sequence, destinations, exit reasons and hierarchy identical");
                    Set("_showRuleMap", true); Call("SetOwner", AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)); break;
                case 4:
                    Check(Items(Field(Root, "Nodes")).Length == 3, "Actual Enemy Base Mode remains Spawn / Alive / Dead");
                    var alive = Items(Field(Root, "Nodes")).Single(n => ((StateSO)Field(n, "State")).name == "Alive_Shooter");
                    Call("OpenInside", alive); break;
                case 5:
                    Check(Controls.ContainsKey("RuleMapCanvas") && Items(Get("_ruleMapGroups")).Length == _expectedGroups, "Actual Enemy Base Alive uses the same clean rule map");
                    Check(Items(Field(Get("Scope"), "Nodes")).All(n => (int)Field(n, "Level") == 2), "Actual hierarchy remains level 2, not an invented grouping layer");
                    CheckGroups(); CheckCards();
                    ClickTab(1); break;
                case 6:
                    Check(Items(Get("_shownLinks")).Length == _expectedRows, "Actual migrated table can still show all original arrows");
                    ClickTab(0); _window.position = new Rect(35, 45, 800, 600); Set("_browserWidth", 215f); Set("_inspectorWidth", 220f); break;
                case 7:
                    Check(Controls.ContainsKey("RuleMapCanvas"), "Narrow window keeps the scrollable rule view"); CheckCards();
                    var browser = Controls["Browser"]; var inspector = Controls["Inspector"]; var canvas = Controls["Canvas"];
                    Check(browser.xMax <= canvas.x && canvas.xMax <= inspector.x, "Hierarchy, rule view and inspector panels do not overlap at width 800");
                    Check(_sceneSignature == Scenes(), "Work scenes and their dirty flags stayed unchanged");
                    Check(_files.Where(p => p.Key != TablePath).All(p => Hash(p.Key) == p.Value), "All other state assets, prefabs, runtime executor and combat controller stayed unchanged");
                    if (!_allowMigration) Check(_files.All(p => Hash(p.Key) == p.Value), "Read-only follow-up saved or changed no production assets");
                    Check(_semantic == HfsmSharedRuleMigration.SemanticSignature(_real), "Final resolved execution signature equals the saved baseline");
                    Finish(); return;
            }
            Controls.Clear();
        }
        catch (Exception ex) { Fail("UI/migration: " + (ex.InnerException ?? ex)); Finish(); }
    }

    private static void CheckGroups()
    {
        var groups = Items(Get("_ruleMapGroups"));
        var edges = groups.SelectMany(g => Items(Field(g, "Usages"))).Select(l => Field(l, "Edge")).ToArray();
        Check(edges.Length == _expectedRows && edges.Select(e => (int)Field(e, "Row")).Distinct().Count() == _expectedRows, "Rule map accounts for each original row exactly once");
        Check(groups.Count(g => Field(g, "Rule") != null) == _expectedShared, "Shared groups retain explicit rule identities");
        Check(Controls.Keys.Count(k => k.StartsWith("RuleMapArrow/")) > 0, "Destination arrows are visible without selection");
    }
    private static void CheckCards()
    {
        var rectangles = ((IDictionary)Get("_ruleMapRects")).Values.Cast<Rect>().ToArray();
        Check(rectangles.Length > 0, "All rule cards have measured layout rectangles");
        Check(rectangles.All(r => r.width > 150 && r.height >= 100), "Rule cards keep readable size even in a narrow viewport");
        for (int i = 0; i < rectangles.Length; i++) for (int j = i + 1; j < rectangles.Length; j++)
            Check(!rectangles[i].Overlaps(rectangles[j]), "Rule cards do not overlap " + i + "/" + j);
    }
    private static void Click(string id)
    {
        var p = Controls[id].center;
        _window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = p });
        _window.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = p });
    }
    private static void ClickTab(int index)
    {
        Rect r = Controls["RuleViewTabs"]; Vector2 p = new(r.x + r.width * (index == 0 ? .25f : .75f), r.center.y);
        _window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = p });
        _window.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = p });
    }
    private static string Hash(string path) { using var sha = SHA256.Create(); return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path))); }
    private static Dictionary<string, string> Files()
        => Directory.GetFiles("Assets/ScriptableObject/StateMachine", "*", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles("Assets/Prefabs/GameScene/Shooter", "*", SearchOption.AllDirectories))
            .Concat(new[] { "Assets/Scripts/StateMachine/Core/HfsmRuntime.cs", "Assets/Scripts/StateMachine/Core/State.cs", "Assets/Scripts/StateMachine/Core/StateTransition.cs", "Assets/Scripts/StateMachine/Core/StateMachine.cs" })
            .Concat(Directory.GetFiles("Assets/Scripts/Gameplay/Entities/Shooter/Enemy/StateMachine", "EnemyShooterCombatController*.cs"))
            .Where(File.Exists).Distinct().ToDictionary(p => p.Replace('\\', '/'), Hash);
    private static string Scenes()
        => string.Join("\n", Enumerable.Range(0, SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i))
            .Select(s => s.handle + ":" + s.path + ":" + s.isDirty + ":" + string.Join(",", s.GetRootGameObjects().Select(o => o.GetInstanceID()))));
    private static void OnLog(string message, string stack, LogType type)
    { if ((type == LogType.Error || type == LogType.Exception) && stack.Contains("HFSMTransitionGraphWindow")) Fail("GUI error: " + message); }
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); Lines.Add("PASS " + message); _passed++; }
    private static void Fail(string message) { Lines.Add("FAIL " + message); _failed++; }
    private static void Finish()
    {
        EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
        if (_window != null) _window.Close(); _window = null;
        foreach (var o in Owned) { Undo.ClearUndo(o); UnityEngine.Object.DestroyImmediate(o); } Owned.Clear();
        foreach (var p in Preferences) { if (p.Value.exists) EditorPrefs.SetFloat(p.Key, p.Value.value); else EditorPrefs.DeleteKey(p.Key); }
        if (_previous != null) _previous.Focus();
        Lines.Add($"{_passed} passed / {_failed} failed. " + (_allowMigration ? "Only the explicitly scoped Alive table and newly created shared-rule assets may be saved." : "Read-only: no production asset saves.") + " No scene/prefab saves or global Undo.");
        Directory.CreateDirectory(Path.GetDirectoryName(Report)); File.WriteAllLines(Report, Lines); Debug.Log("[HFSM shared rules verification] " + Lines.Last());
    }
}
