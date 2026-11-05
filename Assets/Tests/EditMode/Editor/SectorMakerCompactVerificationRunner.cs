using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Generated Variant + separate utility window only. No Tools menu and no global Undo.</summary>
public static class SectorMakerCompactVerificationRunner
{
    public const string Report = "Library/SectorMakerVerification/compact-v1.txt";
    private static readonly List<string> Lines = new();
    private static readonly Dictionary<string, Rect> Controls = new();
    private static SectorMakerWorkspaceTests _fixture;
    private static SectorMakerWindow _window;
    private static EditorWindow _previous;
    private static Rect _expandedRect;
    private static PrefabStage _testStage;
    private static int _phase, _frames, _passed, _failed;
    private static double _started;
    private static Dictionary<string, string> _filesBefore;
    private static Dictionary<int, string> _sceneBefore;

    public static void Run()
    {
        if (_window != null || PrefabStageUtility.GetCurrentPrefabStage() != null ||
            EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        Lines.Clear(); Controls.Clear(); _phase = _frames = _passed = _failed = 0;
        _previous = EditorWindow.focusedWindow; _filesBefore = Files(); _sceneBefore = Scenes();
        Application.logMessageReceived += OnLog;
        foreach (var method in typeof(SectorMakerWorkspaceTests).GetMethods().Where(m =>
                     m.Name.StartsWith("Compact", StringComparison.Ordinal) && m.IsDefined(typeof(TestAttribute))))
        {
            var fixture = new SectorMakerWorkspaceTests();
            try { fixture.SetUp(); method.Invoke(fixture, null); Check(true, method.Name); }
            catch (Exception ex) { Fail(method.Name + ": " + (ex.InnerException ?? ex)); }
            finally { try { fixture.TearDown(); } catch (Exception ex) { Fail("Test cleanup: " + ex); } }
        }
        try
        {
            _fixture = new SectorMakerWorkspaceTests(); _fixture.SetUp();
            _window = SectorMakerWorkspaceTests.Get<SectorMakerWindow>(_fixture, "_window");
            _window.titleContent = new GUIContent("Sector Maker UI verification");
            _window.position = new Rect(60, 65, 1100, 700);
            _window.ObserveControl = (id, rect) => Controls[id] = rect;
            _window.ShowUtility(); _expandedRect = _window.position;
            _started = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }
        catch (Exception ex) { Fail("UI setup: " + ex); Finish(); }
    }
    private static void Tick()
    {
        try
        {
            if (_window == null) throw new Exception("Verification window closed early.");
            if (EditorApplication.timeSinceStartup - _started > 50) throw new Exception("UI verification timed out.");
            _window.Repaint(); if (++_frames < 12) return; _frames = 0;
            switch (_phase++)
            {
                case 0:
                    Check(Controls.ContainsKey("CompactMode"), "Full workspace has manual compact button");
                    Call("OpenRoomForEditing"); _testStage = PrefabStageUtility.GetCurrentPrefabStage();
                    Controls.Clear(); break;
                case 1:
                    Check(Get<bool>("_compactMode") && _window.position.width < 450 && _window.position.height < 450,
                        "Entering Scene editing automatically shrinks floating window");
                    Check(Editing && _testStage != null, "Same generated room stays open in Prefab Mode");
                    foreach (string id in new[] { "CompactBack", "CompactTools", "CompactPlacement", "CompactSelect", "CompactSave" })
                        Check(Controls.TryGetValue(id, out var rect) && rect.width > 0 && rect.yMin >= 0 && rect.yMax <= _window.position.height,
                            id + " is visible in small window");
                    ClickToolbar(1); break;
                case 2:
                    Check(Get<object>("_mode").ToString() == "EnemySpawnPoint", "Small toolbar selects enemy markers");
                    // Exercise the stop/back path without creating any objects or touching global Undo.
                    Set("_paintingEnabled", true); Click("CompactSelect"); break;
                case 3:
                    Check(!Get<bool>("_paintingEnabled"), "Selection mode disarms placement");
                    Set("_paintingEnabled", true); Click("CompactBack"); Controls.Clear(); break;
                case 4:
                    Check(!Get<bool>("_compactMode") && !Get<bool>("_paintingEnabled"), "Back stops placement and restores full workspace");
                    Check(Near(_window.position, _expandedRect), "Back restores original size and screen position");
                    Check(Editing && PrefabStageUtility.GetCurrentPrefabStage() == _testStage, "Back does not close/save the editing prefab");
                    Check(Get<Transform>("_enemySpawnPointParent") != null &&
                          Get<GameObject>("_roomPrefab") == SectorMakerWorkspaceTests.Get<GameObject>(_fixture, "_prefab"),
                        "Room and automatic placement-parent links survive restoration");
                    Click("CompactMode"); Controls.Clear(); break;
                case 5:
                    Check(Get<bool>("_compactMode"), "Manual compact button shrinks window again");
                    ClickToolbar(0); break;
                case 6:
                    Check(Get<object>("_mode").ToString() == "FixedSectorStructure", "Small toolbar selects static objects");
                    _window.position = new Rect(_window.position.position, new Vector2(405, 365));
                    Click("CompactBack"); Controls.Clear(); break;
                case 7:
                    Check(Get<Vector2>("_compactWindowSize") == new Vector2(405, 365), "User-resized compact dimensions remembered");
                    Click("CompactMode"); Controls.Clear(); break;
                case 8:
                    Check(_window.position.size == new Vector2(405, 365), "Re-entering compact mode restores preferred small size");
                    _testStage.ClearDirtiness(); StageUtility.GoToMainStage(); Controls.Clear(); break;
                case 9:
                    Check(!Get<bool>("_compactMode") && Near(_window.position, _expandedRect), "Leaving own Prefab Mode restores full Maker");
                    Check(!Editing && Get<Transform>("_sectorObjectParent") == null, "Closing own room disarms placement and clears stage references");
                    Finish(); break;
            }
        }
        catch (Exception ex) { Fail("UI: " + ex); Finish(); }
    }
    private static bool Near(Rect a, Rect b) => Vector2.Distance(a.position, b.position) < 2 && Vector2.Distance(a.size, b.size) < 2;
    private static bool Editing => (bool)typeof(SectorMakerWindow).GetProperty("IsEditingRoom", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_window);
    private static void Call(string method) => SectorMakerWorkspaceTests.Call(_window, method);
    private static T Get<T>(string field) => SectorMakerWorkspaceTests.Get<T>(_window, field);
    private static void Set(string field, object value) => SectorMakerWorkspaceTests.Set(_window, field, value);
    private static void Click(string id)
    {
        if (!Controls.TryGetValue(id, out var rect)) throw new Exception("Missing control " + id);
        ClickAt(rect.center);
    }
    private static void ClickToolbar(int index)
    {
        var rect = Controls["CompactTools"];
        ClickAt(new Vector2(rect.x + rect.width * (index + .5f) / 3, rect.center.y));
    }
    private static void ClickAt(Vector2 point)
    {
        _window.SendEvent(new Event { type = EventType.MouseDown, button = 0, clickCount = 1, mousePosition = point });
        _window.SendEvent(new Event { type = EventType.MouseUp, button = 0, clickCount = 1, mousePosition = point });
    }
    private static void OnLog(string condition, string stack, LogType type)
    {
        if ((type == LogType.Exception || type == LogType.Error) && (stack.Contains("SectorMakerWindow") || condition.Contains("GUILayout")))
            Fail("GUI: " + condition + "\n" + stack);
    }
    private static Dictionary<string, string> Files()
    {
        var paths = new HashSet<string>(Directory.GetFiles(SectorMakerCatalog.Root, "*", SearchOption.AllDirectories));
        foreach (string guid in AssetDatabase.FindAssets("t:SectorPresetSO")) paths.Add(AssetDatabase.GUIDToAssetPath(guid));
        paths.Add("ProjectSettings/SectorMakerCatalog.asset");
        return paths.Where(File.Exists).ToDictionary(p => p, File.ReadAllText);
    }
    private static Dictionary<int, string> Scenes()
    {
        var result = new Dictionary<int, string>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || EditorSceneManager.IsPreviewScene(scene)) continue;
            var objects = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .OrderBy(t => t.GetInstanceID()).ToArray();
            result[scene.handle] = scene.isDirty + "|" + string.Join("\n", objects.Select(t => t.GetInstanceID() + ":" +
                EditorJsonUtility.ToJson(t.gameObject) + "|" + string.Join("|", t.GetComponents<Component>().Where(c => c != null).Select(EditorJsonUtility.ToJson))));
        }
        return result;
    }
    private static void Check(bool value, string label)
    { if (!value) throw new Exception(label); Lines.Add("PASS " + label); _passed++; }
    private static void Fail(string label) { Lines.Add("FAIL " + label); _failed++; }
    private static void Finish()
    {
        EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
        if (_window != null) _window.ObserveControl = null;
        try { _fixture?.TearDown(); } catch (Exception ex) { Fail("UI cleanup: " + ex); }
        _fixture = null; _window = null;
        try
        {
            var after = Files(); var scenes = Scenes();
            Check(_filesBefore.Count == after.Count && _filesBefore.All(p => after.TryGetValue(p.Key, out var value) && value == p.Value),
                "Real sector prefabs/presets and catalog settings unchanged");
            Check(_sceneBefore.Count == scenes.Count && _sceneBefore.All(p => scenes.TryGetValue(p.Key, out var value) && value == p.Value),
                "Working scenes, objects, components and dirty flags unchanged");
        }
        catch (Exception ex) { Fail("Preservation: " + ex); }
        if (_previous != null) _previous.Focus();
        Lines.Add($"{_passed} passed / {_failed} failed. Generated test assets only; no real room or working scene saved.");
        Directory.CreateDirectory(Path.GetDirectoryName(Report)); File.WriteAllLines(Report, Lines);
        Debug.Log("[Sector Maker compact verification] " + Lines.Last() + " " + Report);
    }
}
