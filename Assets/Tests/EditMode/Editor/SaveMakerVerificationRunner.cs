using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>임시 Library ES3 파일과 별도 검사 창만 사용한다. 실제 세이브/씬/PlayerPrefs를 저장하지 않는다.</summary>
public static class SaveMakerVerificationRunner
{
    public const string Report = "Library/SaveMakerVerification/unity-tests.txt";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly List<string> Lines = new();
    private static readonly Dictionary<string, Rect> Controls = new();
    private static SaveMakerTests _fixture;
    private static SaveInspectorWindow _window;
    private static EditorWindow _previous;
    private static int _passed, _failed, _frames, _phase;
    private static double _start;
    private static Dictionary<string, string> _before;
    private static string _widthKey;
    private static bool _hadWidth;
    private static float _oldWidth, _initialWidth;

    [MenuItem("Tools/Authoring/Verification/Run Save Maker Tests")]
    public static void Run()
    {
        if (_window != null || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        _passed = _failed = _frames = _phase = 0; Lines.Clear(); Controls.Clear();
        foreach (var type in new[] { typeof(SaveMakerTests), typeof(RunLifecycleAndSaveTests) })
        foreach (var method in type.GetMethods().Where(m => m.IsDefined(typeof(TestAttribute))))
        {
            object fixture = Activator.CreateInstance(type);
            try
            {
                foreach (var setup in type.GetMethods().Where(m => m.IsDefined(typeof(SetUpAttribute)))) setup.Invoke(fixture, null);
                method.Invoke(fixture, null); Pass(type.Name + "." + method.Name);
            }
            catch (Exception ex) { Fail(method.Name + ": " + (ex.InnerException ?? ex)); }
            finally
            {
                foreach (var teardown in type.GetMethods().Where(m => m.IsDefined(typeof(TearDownAttribute))))
                    try { teardown.Invoke(fixture, null); } catch (Exception ex) { Fail("Cleanup: " + ex); }
            }
        }
        try
        {
            _fixture = new SaveMakerTests(); _fixture.SetUp();
            _before = Hashes(); _previous = EditorWindow.focusedWindow;
            _window = ScriptableObject.CreateInstance<SaveInspectorWindow>();
            _window.TestRoot = _fixture.Root;
            Set("_showGuide", false); Set("_leftWidth", 215f); Set("_section", 0);
            typeof(SaveInspectorWindow).GetMethod("RefreshSnapshots", Flags).Invoke(_window, null);
            _window.titleContent = new GUIContent("Save Maker UI verification");
            _window.position = new Rect(35, 40, 1200, 800);
            _window.ObserveControl = (id, rect) => Controls[id] = rect;
            _widthKey = "BoardChase.SaveMaker." + Application.dataPath + ".LeftWidth";
            _hadWidth = EditorPrefs.HasKey(_widthKey); _oldWidth = EditorPrefs.GetFloat(_widthKey);
            _window.ShowUtility(); _start = EditorApplication.timeSinceStartup;
            Application.logMessageReceived += OnLog; EditorApplication.update += Tick;
        }
        catch (Exception ex) { Fail("UI setup: " + ex); Finish(); }
    }
    private static void OnLog(string condition, string stack, LogType type)
    {
        if ((type == LogType.Exception || type == LogType.Error) && (stack.Contains("SaveInspectorWindow") || condition.Contains("GUILayout")))
            Fail("GUI: " + condition + "\n" + stack);
    }
    private static void Tick()
    {
        try
        {
            if (_window == null) throw new Exception("Verification window closed.");
            if (EditorApplication.timeSinceStartup - _start > 50) throw new Exception("UI verification timed out; window must be visible.");
            _window.Repaint(); if (++_frames < 8) return; _frames = 0;
            switch (_phase++)
            {
                case 0: Require(Controls.ContainsKey("section:5"), "All six navigation sections render"); Click("section:1"); break;
                case 1: Require(Get<int>("_section") == 1, "Project configuration selection"); Click("section:2"); break;
                case 2: Require(Get<int>("_section") == 2, "Device settings selection"); Click("section:3"); break;
                case 3: Require(Get<int>("_section") == 3, "Permanent profile selection"); Click("section:4"); break;
                case 4: Require(Get<int>("_section") == 4, "Current run selection"); Click("section:5"); break;
                case 5:
                    Require(Get<int>("_section") == 5 && Controls.ContainsKey("backup:3"), "Recovery page and backup rows render");
                    Require(Get<string>("_confirmation") == "", "Destructive operations start locked");
                    _initialWidth = Get<float>("_leftWidth"); var rect = Controls["split"];
                    _window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = rect.center });
                    _window.SendEvent(new Event { type = EventType.MouseDrag, button = 0, mousePosition = rect.center + Vector2.right * 24, delta = Vector2.right * 24 });
                    _window.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = rect.center + Vector2.right * 24 }); break;
                case 6:
                    Require(Get<float>("_leftWidth") > _initialWidth, "Navigation pane can be resized by dragging");
                    Require(_before.Count == Hashes().Count && _before.All(p => Hashes().TryGetValue(p.Key, out var hash) && hash == p.Value), "Browsing all pages never changes ES3 files");
                    Require(!Get<bool>("_configDirty") && !Get<bool>("_settingsDirty"), "Browsing does not dirty setting drafts");
                    Finish(); break;
            }
        }
        catch (Exception ex) { Fail("UI: " + ex); Finish(); }
    }
    private static Dictionary<string, string> Hashes() => Directory.GetFiles(_fixture.Root, "*", SearchOption.AllDirectories).ToDictionary(p => p, SaveMakerStorage.Fingerprint);
    private static void Set(string name, object value) => typeof(SaveInspectorWindow).GetField(name, Flags).SetValue(_window, value);
    private static T Get<T>(string name) => (T)typeof(SaveInspectorWindow).GetField(name, Flags).GetValue(_window);
    private static void Click(string id)
    {
        if (!Controls.TryGetValue(id, out var rect)) throw new Exception("Missing control " + id);
        _window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = rect.center });
        _window.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = rect.center });
    }
    private static void Require(bool value, string label) { if (!value) throw new Exception(label); Pass(label); }
    private static void Pass(string label) { Lines.Add("PASS " + label); _passed++; }
    private static void Fail(string label) { Lines.Add("FAIL " + label); _failed++; }
    private static void Finish()
    {
        EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
        if (_widthKey != null) { if (_hadWidth) EditorPrefs.SetFloat(_widthKey, _oldWidth); else EditorPrefs.DeleteKey(_widthKey); }
        if (_window != null) _window.Close(); if (_previous != null) _previous.Focus();
        try { _fixture?.TearDown(); } catch (Exception ex) { Fail("UI cleanup: " + ex); } _fixture = null;
        Lines.Add($"{_passed} passed / {_failed} failed. Library fixture files only; no real saves, assets, PlayerPrefs or scenes modified.");
        Directory.CreateDirectory(Path.GetDirectoryName(Report)); File.WriteAllLines(Report, Lines);
        Debug.Log("[Save Maker verification] " + Lines.Last() + " " + Report);
    }
}
