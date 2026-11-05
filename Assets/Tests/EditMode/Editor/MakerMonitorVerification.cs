using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Explicit batch-only verification. Only temporary in-memory objects/preview scenes; no SaveAssets/scene save.</summary>
public static class MakerMonitorVerification
{
    internal const string Folder = "Library/MakerMonitorVerification";
    private static readonly List<string> Lines = new();
    private static readonly List<Object> Temporary = new();
    private static SceneSetup[] _sceneSetup;
    private static Dictionary<string, string> _hashes;
    private static MakerMonitorUiVerificationWindow _ui;
    private static DebugMonitorWindow _monitor;
    private static object _row;
    private static int _passed, _failed, _phase, _frames;
    private static double _started;

    public static void Run()
    {
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Close the editor and use the batch verification entry point.");
        Directory.CreateDirectory(Folder);
        Lines.Clear(); _passed = _failed = _phase = _frames = 0;
        _sceneSetup = EditorSceneManager.GetSceneManagerSetup();
        _hashes = Hashes();
        Application.logMessageReceived += OnLog;
        try
        {
            // Reproduce the previously failing untitled/unsaved scene, without touching authored scenes.
            var empty = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.MarkSceneDirty(empty);
            Assert.IsTrue(string.IsNullOrEmpty(empty.path)); Assert.IsTrue(empty.isDirty);
            foreach (var fixture in new[] { typeof(MakerMonitorSafetyTests), typeof(MakerRegistrationIndexTests),
                         typeof(RaidRedesignRegressionTests), typeof(RaidRuntimeContractTests), typeof(RaidRedesignEconomyTests) })
                foreach (var test in fixture.GetMethods())
                {
                    bool hadCase = false;
                    foreach (var testCase in test.GetCustomAttributes<TestCaseAttribute>())
                    { hadCase = true; Test(fixture, test, testCase.Arguments); }
                    if (!hadCase && test.IsDefined(typeof(TestAttribute))) Test(fixture, test, Array.Empty<object>());
                }
            SetupUi();
            _started = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }
        catch (Exception ex) { Fail(ex); Finish(); }
    }

    private static void Test(Type fixture, MethodInfo test, object[] args)
    {
        object instance = Activator.CreateInstance(fixture);
        try
        {
            foreach (var setup in fixture.GetMethods().Where(m => m.IsDefined(typeof(SetUpAttribute)))) setup.Invoke(instance, null);
            test.Invoke(instance, args); _passed++; Lines.Add("PASS " + fixture.Name + "." + test.Name);
        }
        catch (Exception ex) { Fail(ex.InnerException ?? ex); }
        finally
        {
            foreach (var teardown in fixture.GetMethods().Where(m => m.IsDefined(typeof(TearDownAttribute))))
                try { teardown.Invoke(instance, null); } catch (Exception ex) { Fail(ex); }
        }
    }

    private static T Keep<T>(T obj) where T : Object { Temporary.Add(obj); return obj; }
    private static void SetupUi()
    {
        var root = Keep(new GameObject("Temporary monitor verification")); root.SetActive(false);
        var effects = root.AddComponent<EntityStatusEffectRuntime>();
        var bleeding = Resources.Load<StatusEffectDefinitionSO>("Bleeding");
        Assert.IsNotNull(bleeding); DebugMonitorWindow.ApplyDebugStatusEffect(effects, bleeding, -1, 3);
        var sector = root.AddComponent<SectorRuntime>(); var traits = root.AddComponent<SectorRoomTraitRuntime>();
        var table = AssetDatabase.LoadAssetAtPath<SectorRoomTraitTableSO>("Assets/ScriptableObject/GameScene/Stage/RaidAnomalyTable.asset");
        traits.Configure(table, StageRoomType.Normal, 12, Vector2Int.zero, true,
            new StageDefinitionSO.BiochemicalGasSettings { enabled = true, baselineInfectionPerSecond = 1f }, 3, true, 2f);
        traits.SetDebugRoomTraitOverride(SectorRoomTraitType.GasBoost);
        var manager = root.AddComponent<SectorStateManager>();
        typeof(SectorStateManager).GetProperty("CurrentSector").SetValue(manager, sector);
        _monitor = Keep(ScriptableObject.CreateInstance<DebugMonitorWindow>());
        MakerMonitorSafetyTests.Set(_monitor, "_sectorStateManager", manager);
        MakerMonitorSafetyTests.Set(_monitor, "_debugStatusEffectDefinition", bleeding);
        Type rowType = typeof(DebugMonitorWindow).GetNestedType("CharacterRow", BindingFlags.NonPublic);
        _row = Activator.CreateInstance(rowType);
        rowType.GetField("statusEffects").SetValue(_row, effects);
        _ui = Keep(ScriptableObject.CreateInstance<MakerMonitorUiVerificationWindow>());
        _ui.DrawContents = () =>
        {
            MakerMonitorSafetyTests.Call(_monitor, "DrawStatusEffectDebugControls", _row);
            MakerMonitorSafetyTests.Call(_monitor, "DrawCurrentRoomTraitDebugControls");
        };
        _ui.titleContent = new GUIContent("Maker / Monitor UI verification");
        _ui.position = new Rect(60, 65, 480, 1000); _ui.ShowUtility();
    }

    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup - _started > 35) throw new TimeoutException("UI verification timeout.");
            _ui.Repaint();
            if (++_frames < 15) return;
            Assert.Greater(_ui.Repaints, 0, "Actual IMGUI repaint is required.");
            Assert.IsTrue(_ui.ValidLayout, "Status/trait controls must remain inside the scroll content.");
            _passed++; Lines.Add("PASS Actual status/trait IMGUI layout at width " + _ui.position.width);
            if (_phase++ == 0)
            { _frames = 0; _ui.Repaints = 0; _ui.position = new Rect(60, 65, 330, 1000); return; }
            // Force editor-window serialization while preview/runtime references are alive.
            EditorJsonUtility.ToJson(_monitor); EditorJsonUtility.ToJson(_ui);
            _passed++; Lines.Add("PASS Editor window layout serialization excludes transient objects");
            Finish();
        }
        catch (Exception ex) { Fail(ex.InnerException ?? ex); Finish(); }
    }

    private static Dictionary<string, string> Hashes()
    {
        using var hash = SHA256.Create();
        return Directory.EnumerateFiles("Assets", "*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".prefab") || p.EndsWith(".unity") || p.EndsWith(".asset"))
            .ToDictionary(p => p, p => { using var stream = File.OpenRead(p); return Convert.ToBase64String(hash.ComputeHash(stream)); });
    }
    private static void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        { _failed++; Lines.Add("UNITY " + type + ": " + message + "\n" + trace); }
    }
    private static void Fail(Exception ex) { _failed++; Lines.Add("FAIL " + ex); }
    private static void Finish()
    {
        EditorApplication.update -= Tick;
        try
        {
            for (int i = Temporary.Count - 1; i >= 0; i--)
                if (Temporary[i] != null) Object.DestroyImmediate(Temporary[i]);
            Temporary.Clear();
            var after = Hashes();
            Assert.AreEqual(_hashes.Count, after.Count);
            foreach (var pair in _hashes) Assert.AreEqual(pair.Value, after[pair.Key], "Authored data changed: " + pair.Key);
            _passed++; Lines.Add("PASS All authored prefab/scene/SO files unchanged");
            // Restore the previous named scenes for the next interactive launch; never save the test scene.
            if (_sceneSetup.Length > 0 && _sceneSetup.All(s => !string.IsNullOrEmpty(s.path)))
                EditorSceneManager.RestoreSceneManagerSetup(_sceneSetup);
            else if (File.Exists("Assets/Scenes/RUNScene.unity"))
                EditorSceneManager.OpenScene("Assets/Scenes/RUNScene.unity", OpenSceneMode.Single);
        }
        catch (Exception ex) { Fail(ex); }
        Application.logMessageReceived -= OnLog;
        string result = $"PASSED={_passed} FAILED={_failed}"; Lines.Add(result);
        File.WriteAllLines(Folder + "/results.txt", Lines);
        Debug.Log("[Maker / Monitor verification] " + result);
        EditorApplication.Exit(_failed == 0 ? 0 : 1);
    }
}

internal sealed class MakerMonitorUiVerificationWindow : EditorWindow
{
    [NonSerialized] internal Action DrawContents;
    [NonSerialized] internal int Repaints;
    [NonSerialized] internal bool ValidLayout;
    private Vector2 _scroll;
    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        using (new EditorGUILayout.VerticalScope())
        {
            DrawContents?.Invoke();
        }
        if (Event.current.type == EventType.Repaint)
        {
            Rect content = GUILayoutUtility.GetLastRect();
            ValidLayout = content.width <= position.width + 1 && content.height > 100;
            Repaints++;
        }
        EditorGUILayout.EndScrollView();
    }
}
