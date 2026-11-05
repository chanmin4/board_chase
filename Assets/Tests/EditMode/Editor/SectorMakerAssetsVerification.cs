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

/// <summary>One explicitly requested migration + isolated asset/UI tests. No original scene/asset saves.</summary>
[InitializeOnLoad]
public static class SectorMakerAssetsVerification
{
    private const string Folder = "Library/SectorMakerAssetsVerification";
    private const string Request = Folder + "/request.json";
    private static Dictionary<string, string> _before;
    private static int _passed, _failed, _fontAssertions, _phase, _frames;
    private static double _started;
    private static double _nextRequestCheck;
    private static string _lastWaitReason;
    private static SectorMakerWorkspaceTests _fixture;
    private static SectorMakerWindow _window;
    private static readonly Dictionary<string, Rect> Controls = new();
    private static UnityEngine.Object _selection;
    private static EditorWindow _previousWindow;
    private static SectorPresetSO[] _picks;
    static SectorMakerAssetsVerification()
    {
        // The request may be written after Unity finishes compiling the test assembly.
        // Keep an inexpensive watcher alive rather than relying on another domain reload.
        EditorApplication.update -= MaybeRun;
        EditorApplication.update += MaybeRun;
        if (File.Exists(Request)) RecordWait("Request watcher initialized; waiting for an Editor update.");
    }
    private static void MaybeRun()
    {
        if (_window != null || EditorApplication.timeSinceStartup < _nextRequestCheck) return;
        _nextRequestCheck = EditorApplication.timeSinceStartup + 1;
        if (!File.Exists(Request)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { RecordWait("Unity is compiling or importing assets."); return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { RecordWait("Waiting for Play Mode to end."); return; }
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null)
        { RecordWait("Waiting for Prefab Mode to end: " + stage.assetPath); return; }
        RecordWait("Starting isolated rename verification.");
        Run();
    }
    private static void RecordWait(string reason)
    {
        if (_lastWaitReason == reason) return;
        _lastWaitReason = reason;
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "/waiting.txt", DateTime.Now.ToString("s") + " " + reason);
    }
    [MenuItem("Tools/Authoring/Verification/Sector Maker Rename")]
    private static void RequestRenameVerification()
    {
        if (_window != null || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            Debug.LogWarning("컴파일이 끝난 뒤 Play / Prefab Mode 밖에서 검사하세요.");
            return;
        }
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Request, "{\"mode\":\"prefab-rename-ui-and-guid-preservation\"}");
        Run();
    }
    public static void Run()
    {
        if (_window != null || EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
            throw new InvalidOperationException("Play / Prefab Mode 밖에서 실행하세요.");
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "/results.txt", "Sector Maker assets + Base templates verification\n");
        bool renameOnly = File.Exists(Request) && File.ReadAllText(Request).Contains("prefab-rename");
        if (File.Exists(Request)) File.Delete(Request);
        _selection = Selection.activeObject; _previousWindow = EditorWindow.focusedWindow;
        _before = Hashes(); _passed = _failed = _fontAssertions = _phase = _frames = 0;
        Application.logMessageReceived += OnLog;
        try
        {
            // This new data is the requested deliverable, not test data. Existing files stay byte-identical.
            if (!renameOnly)
            {
                var templates = SectorMakerAssets.EnsureBasePresets();
                Assert.AreEqual(11, templates.Length);
                Pass("all 11 Base template SOs registered, including 1/2 and 1/4");
                var again = SectorMakerAssets.EnsureBasePresets(); Assert.AreEqual(templates.Length, again.Length);
                Assert.That(again, Is.EquivalentTo(templates)); Pass("Base registration is idempotent; no duplicate or overwrite");
            }
            if (renameOnly)
            {
                string[] tests = { "PrefabRenamePreservesGuidMetaVariantAncestryAndStageReferences",
                    "PrefabRenameRejectsDuplicatesAndInvalidNamesWithoutChangingAnything", "PrefabRenameIsBlockedWhilePrefabModeIsOpen",
                    "WindowRenameUpdatesSelectedPrefabWithoutChangingSelectedPreset", "TemporaryRoomLabelKeepsTheOldSerializedCategoryIndex" };
                foreach (var method in typeof(SectorMakerAssetTests).GetMethods().Where(m => tests.Contains(m.Name)))
                    RunTest(typeof(SectorMakerAssetTests), method);
            }
            else
                foreach (var fixture in new[] { typeof(SectorMakerWorkspaceTests), typeof(SectorMakerAssetTests) })
                    foreach (var method in fixture.GetMethods().Where(m => m.IsDefined(typeof(TestAttribute)))) RunTest(fixture, method);
            if (Application.isBatchMode)
            {
                Line("INFO headless Unity: asset operations and window-state tests only; rendered button checks require an interactive Editor.");
                Finish(); return;
            }
            _fixture = new SectorMakerWorkspaceTests(); _fixture.SetUp();
            _window = SectorMakerWorkspaceTests.Get<SectorMakerWindow>(_fixture, "_window");
            _window.titleContent = new GUIContent("Sector Maker asset UI verification");
            _window.ObserveControl = (id, rect) => Controls[id] = rect;
            _window.ShowUtility(); _window.position = new Rect(60, 65, 1100, 900);
            _started = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }
        catch (Exception ex) { Fail(ex); Finish(); }
    }
    private static void RunTest(Type type, MethodInfo test)
    {
        var instance = Activator.CreateInstance(type);
        try
        {
            foreach (var setup in type.GetMethods().Where(m => m.IsDefined(typeof(SetUpAttribute)))) setup.Invoke(instance, null);
            test.Invoke(instance, null); Pass(type.Name + "." + test.Name);
        }
        catch (Exception ex) { Fail(ex.InnerException ?? ex); }
        finally
        {
            foreach (var cleanup in type.GetMethods().Where(m => m.IsDefined(typeof(TearDownAttribute))))
                try { cleanup.Invoke(instance, null); } catch (Exception ex) { Fail(ex.InnerException ?? ex); }
        }
    }
    private static void Set(string name, object value) => SectorMakerWorkspaceTests.Set(_window, name, value);
    private static void Call(string name, params object[] args) => SectorMakerWorkspaceTests.Call(_window, name, args);
    private static void Tick()
    {
        try
        {
            if (_window == null || EditorApplication.timeSinceStartup - _started > 40) throw new TimeoutException("Sector asset UI verification timeout.");
            _window.Repaint(); if (++_frames < 10) return; _frames = 0;
            switch (_phase++)
            {
                case 0:
                    Assert.IsTrue(Controls.ContainsKey("LibraryTabs")); Pass("explicit Prefab / Room SO / Base tabs rendered");
                    Set("_libraryTab", 2); Set("_roomSearch", "NormalSectorBase");
                    var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SectorMakerCatalog.Root + "Bases/NormalSectorBase_HalfSize.prefab");
                    var template = SectorMakerCatalog.Presets.First(p => p.SectorPrefab != null && p.SectorPrefab.gameObject == basePrefab && SectorMakerAssets.IsTemplate(p));
                    Call("SelectRoom", basePrefab, false, template); Controls.Clear(); break;
                case 1:
                    foreach (string id in new[] { "Room/NormalSectorBase", "Room/NormalSectorBase_HalfSize", "Room/NormalSectorBase_QuarterSize", "RegisterBases", "CreateRoomPair" })
                        Assert.IsTrue(Controls.ContainsKey(id), "visible Base control: " + id);
                    Assert.AreEqual(2, SectorMakerWorkspaceTests.Get<int>(_window, "_roomWorkbenchTab"));
                    Pass("all 3 normal sizes visible and Base selection opens new-room workflow");
                    Set("_libraryTab", 1); Set("_roomSearch", "");
                    Call("SelectPreset", SectorMakerWorkspaceTests.Get<SectorPresetSO>(_fixture, "_preset"), false);
                    Controls.Clear(); break;
                case 2:
                    foreach (string id in new[] { "CreatePreset", "CopyPreset", "DeletePreset", "RenamePreset", "PrefabName", "RenameRoomPrefab" })
                        Assert.IsTrue(Controls.TryGetValue(id, out var rect) && rect.width > 0, "SO management control " + id);
                    Pass("SO add / copy / rename / delete controls rendered in separate management panel");
                    string folder = SectorMakerWorkspaceTests.Get<string>(_fixture, "_folder");
                    var prefab = SectorMakerWorkspaceTests.Get<GameObject>(_fixture, "_prefab");
                    _picks = Enumerable.Range(1, 3).Select(i => SectorMakerCatalog.CreatePreset(prefab, folder + "/MultiPick" + i + ".asset", StageRoomType.Normal)).ToArray();
                    Call("RefreshCatalog"); Set("_roomSearch", "MultiPick"); Controls.Clear(); break;
                case 3:
                    Click("Preset/MultiPick1"); Click("Preset/MultiPick2", EventModifiers.Control);
                    Assert.That(Picked(), Is.EqualTo(new[] { _picks[0], _picks[1] }));
                    Click("Preset/MultiPick2", EventModifiers.Control); Assert.That(Picked(), Is.EqualTo(new[] { _picks[0] }));
                    Click("Preset/MultiPick2", EventModifiers.Control); Click("Preset/MultiPick3", EventModifiers.Shift);
                    Assert.That(Picked(), Is.EqualTo(new[] { _picks[1], _picks[2] }));
                    Pass("real list mouse events: normal / Ctrl add / Ctrl toggle / Shift range");
                    Controls.Clear(); break;
                case 4:
                    Assert.IsTrue(Controls.ContainsKey("DeleteSelectedPresetsWorkbench"));
                    Click("SelectVisiblePresets"); Assert.AreEqual(3, Picked().Length);
                    Click("ClearPresetSelection"); Assert.IsEmpty(Picked());
                    Click("Preset/MultiPick1"); Click("Preset/MultiPick2", EventModifiers.Control);
                    Pass("batch-delete control, visible select-all and clear-selection rendered and operated");
                    _window.position = new Rect(60, 65, 980, 620); Controls.Clear(); break;
                case 5:
                    Assert.IsTrue(Controls.TryGetValue("DeleteSelectedPresets", out var deleteRect) && deleteRect.width > 0 && deleteRect.yMax <= _window.position.height);
                    Assert.IsTrue(Controls.ContainsKey("DeleteSelectedPresetsWorkbench"));
                    Click("Preset/MultiPick1"); Assert.AreEqual(1, Picked().Length); Controls.Clear(); break;
                case 6:
                    Assert.IsTrue(Controls.ContainsKey("RoomWorkbenchTabs"));
                    EditorJsonUtility.ToJson(_window); Pass("minimum window size and editor-window serialization");
                    var oldPrefab = SectorMakerWorkspaceTests.Get<GameObject>(_window, "_roomPrefab");
                    string oldGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(oldPrefab));
                    var oldPreset = SectorMakerWorkspaceTests.Get<SectorPresetSO>(_window, "_activeSectorPreset");
                    Set("_prefabNameDraft", "Room_RenamedByButton"); Click("RenameRoomPrefab");
                    var renamed = SectorMakerWorkspaceTests.Get<GameObject>(_window, "_roomPrefab");
                    Assert.AreEqual("Room_RenamedByButton", renamed.name);
                    Assert.AreEqual(oldGuid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(renamed)));
                    Assert.AreSame(oldPreset, SectorMakerWorkspaceTests.Get<SectorPresetSO>(_window, "_activeSectorPreset"));
                    Pass("real prefab-name button rename retains GUID and selected SO");
                    Finish(); break;
            }
        }
        catch (Exception ex) { Fail(ex); Finish(); }
    }
    private static SectorPresetSO[] Picked() => SectorMakerWorkspaceTests.Get<List<SectorPresetSO>>(_window, "_selectedPresets").ToArray();
    private static void Click(string id, EventModifiers modifiers = EventModifiers.None)
    {
        Assert.IsTrue(Controls.TryGetValue(id, out var rect), "click target: " + id);
        foreach (var type in new[] { EventType.MouseDown, EventType.MouseUp })
            _window.SendEvent(new Event { type = type, button = 0, clickCount = 1, mousePosition = rect.center, modifiers = modifiers });
    }
    private static Dictionary<string, string> Hashes()
    {
        using var sha = SHA256.Create();
        var paths = Directory.EnumerateFiles("Assets", "*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".asset") || p.EndsWith(".prefab") || p.EndsWith(".unity") || p.EndsWith(".meta")).ToList();
        if (File.Exists("ProjectSettings/SectorMakerCatalog.asset")) paths.Add("ProjectSettings/SectorMakerCatalog.asset");
        if (Directory.Exists(Application.persistentDataPath)) paths.AddRange(Directory.EnumerateFiles(Application.persistentDataPath)
            .Where(p => p.EndsWith(".es3") || p.EndsWith(".vaccine")));
        return paths
            .ToDictionary(p => p.Replace('\\', '/'), p => { using var stream = File.OpenRead(p); return Convert.ToBase64String(sha.ComputeHash(stream)); });
    }
    private static void Finish()
    {
        EditorApplication.update -= Tick;
        try
        {
            if (_window != null) _window.ObserveControl = null;
            _fixture?.TearDown(); _fixture = null; _window = null;
            SectorMakerCatalog.Scan(); // Discard deleted temporary SOs before other open Maker windows repaint.
            Selection.activeObject = _selection; if (_previousWindow != null) _previousWindow.Focus();
            var after = Hashes();
            foreach (var original in _before) Assert.AreEqual(original.Value, after[original.Key], "Original modified: " + original.Key);
            var additions = after.Keys.Except(_before.Keys).ToArray();
            Assert.IsTrue(additions.All(p => p.StartsWith(SectorMakerAssets.TemplateRoot + "/")), "Only requested template SOs may remain");
            Pass("all pre-existing authored SO / prefab / scene bytes unchanged; only requested Base SOs added");
        }
        catch (Exception ex) { Fail(ex); }
        Application.logMessageReceived -= OnLog;
        Line($"PASSED={_passed} FAILED={_failed} UNITY_FONT_ASSERTIONS={_fontAssertions}");
        Debug.Log($"[Sector Maker asset verification] passed={_passed}, failed={_failed}, known Unity font assertions={_fontAssertions}");
        if (Application.isBatchMode) EditorApplication.Exit(_failed == 0 ? 0 : 1);
    }
    private static void OnLog(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        if (message.Contains("kAllowDontSaveObjectsToBePersistent") && stack.Contains("GUIUtility"))
        { _fontAssertions++; Line("KNOWN UNITY FONT ASSERTION (not suppressed): " + message + "\n" + stack); return; }
        Fail(new Exception(message + "\n" + stack));
    }
    private static void Pass(string message) { _passed++; Line("PASS " + message); }
    private static void Fail(Exception ex) { _failed++; Line("FAIL " + ex); }
    private static void Line(string message) => File.AppendAllText(Folder + "/results.txt", message + "\n");
}
