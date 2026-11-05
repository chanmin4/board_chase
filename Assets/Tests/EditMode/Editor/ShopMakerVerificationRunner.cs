using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>명시적 검사 메뉴. 테스트는 메모리 SO와 별도 창만 사용하며 작업 중인 씬/에셋은 저장하지 않는다.</summary>
public static class ShopMakerVerificationRunner
{
    public const string Report = "Library/ShopMakerVerification/unity-tests.txt";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly List<string> Lines = new();
    private static readonly List<Object> Objects = new();
    private static readonly Dictionary<string, Rect> Controls = new();
    private static ShopProgressionTreeWindow _window;
    private static EditorWindow _previous;
    private static string _productId, _groupId, _before, _pref;
    private static bool _hadPref, _oldPref;
    private static ShopConfigSO _shop;
    private static int _passed, _failed, _phase, _frames;
    private static double _started;
    private static float _initialWidth;
    private static string _leftPref;
    private static bool _hadLeft;
    private static float _oldLeft;

    [MenuItem("Tools/Authoring/Verification/Run Shop Maker Tests")]
    public static void Run()
    {
        if (_window != null || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        Lines.Clear(); Controls.Clear(); _passed = _failed = _phase = _frames = 0;
        foreach (var method in typeof(ShopMakerTests).GetMethods().Where(m => m.IsDefined(typeof(TestAttribute))))
        {
            var fixture = new ShopMakerTests();
            try { fixture.SetUp(); method.Invoke(fixture, null); Pass(method.Name); }
            catch (Exception ex) { Fail(method.Name + ": " + (ex.InnerException ?? ex)); }
            finally { fixture.TearDown(); }
        }
        try
        {
            _previous = EditorWindow.focusedWindow;
            _shop = Make<ShopConfigSO>();
            var item = Make<ItemSO>(); item.name = "Shop Maker temporary item";
            int index = ShopMakerAuthoring.Register(_shop, item); _productId = _shop.Products[index].ProductId;
            _groupId = ShopMakerAuthoring.CreateGroup(_shop, "Temporary unlock group");
            ShopMakerAuthoring.SetMembership(_shop, _groupId, _productId, true);
            _before = EditorJsonUtility.ToJson(_shop);
            _window = ScriptableObject.CreateInstance<ShopProgressionTreeWindow>();
            Set("_config", _shop); Set("_guide", false);
            _window.titleContent = new GUIContent("Shop Maker UI verification");
            _window.position = new Rect(30, 40, 1280, 780);
            _pref = "BoardChase.ShopMaker." + Application.dataPath + ".Rarity.Normal";
            _hadPref = EditorPrefs.HasKey(_pref); _oldPref = EditorPrefs.GetBool(_pref, true);
            _leftPref = "BoardChase.ShopMaker." + Application.dataPath + ".LeftWidth";
            _hadLeft = EditorPrefs.HasKey(_leftPref); _oldLeft = EditorPrefs.GetFloat(_leftPref, 215);
            EditorPrefs.SetBool(_pref, true);
            _window.ObserveControl = (id, rect) => Controls[id] = rect;
            _window.ShowUtility(); _started = EditorApplication.timeSinceStartup;
            Application.logMessageReceived += OnLog;
            EditorApplication.update += Tick;
        }
        catch (Exception ex) { Fail("UI setup: " + ex); Finish(); }
    }
    private static void OnLog(string condition, string stack, LogType type)
    {
        if ((type == LogType.Exception || type == LogType.Error) &&
            (stack.Contains("ShopProgressionTreeWindow") || condition.Contains("GUILayout")))
            Fail("GUI: " + condition + "\n" + stack);
    }
    private static void Tick()
    {
        try
        {
            if (_window == null) throw new Exception("Verification window closed.");
            if (EditorApplication.timeSinceStartup - _started > 50) throw new Exception("UI verification timed out (window must be visible).");
            _window.Repaint(); if (++_frames < 8) return; _frames = 0;
            switch (_phase++)
            {
                case 0:
                    Require(Controls.ContainsKey("product:" + _productId), "Product card rendered");
                    Click("product:" + _productId); break;
                case 1:
                    Require(Get<string>("_selectedProductId") == _productId, "Card click selects correct product");
                    Click("rarity:Normal"); break;
                case 2:
                    Require(!EditorPrefs.GetBool(_pref, true), "Rarity fold click persists collapsed state");
                    Click("rarity:Normal"); Set("_productTab", 1); break;
                case 3:
                    Require(EditorPrefs.GetBool(_pref, false), "Rarity group reopens");
                    Set("_productTab", 2); break;
                case 4:
                    _initialWidth = Get<float>("_leftWidth");
                    Rect rect = Controls["split:1"];
                    _window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = rect.center });
                    _window.SendEvent(new Event { type = EventType.MouseDrag, button = 0, mousePosition = rect.center + Vector2.right * 30, delta = Vector2.right * 30 });
                    _window.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = rect.center + Vector2.right * 30 });
                    break;
                case 5:
                    Require(Get<float>("_leftWidth") > _initialWidth, "Category pane drag resizes width");
                    Set("_page", 1); Set("_showLegacyUnlocks", true); break;
                case 6:
                    Click("group:" + _groupId); break;
                case 7:
                    Require(Get<string>("_selectedGroupId") == _groupId, "Group click selects unlock workbench");
                    Set("_advancedGroup", true); break;
                case 8:
                    Set("_page", 2); Set("_advancedSettings", true); break;
                case 9:
                    Require(EditorJsonUtility.ToJson(_shop) == _before, "Browsing all tabs/foldouts leaves data unchanged");
                    Finish(); break;
            }
        }
        catch (Exception ex) { Fail("UI: " + ex); Finish(); }
    }
    private static T Make<T>() where T : ScriptableObject { var obj = ScriptableObject.CreateInstance<T>(); Objects.Add(obj); return obj; }
    private static void Set(string name, object value) => typeof(ShopProgressionTreeWindow).GetField(name, Flags).SetValue(_window, value);
    private static T Get<T>(string name) => (T)typeof(ShopProgressionTreeWindow).GetField(name, Flags).GetValue(_window);
    private static void Click(string id)
    {
        if (!Controls.TryGetValue(id, out var rect)) throw new Exception("Missing control: " + id);
        _window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = rect.center });
        _window.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = rect.center });
    }
    private static void Require(bool condition, string label) { if (!condition) throw new Exception(label); Pass(label); }
    private static void Pass(string label) { Lines.Add("PASS " + label); _passed++; }
    private static void Fail(string label) { Lines.Add("FAIL " + label); _failed++; }
    private static void Finish()
    {
        EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
        if (_pref != null) { if (_hadPref) EditorPrefs.SetBool(_pref, _oldPref); else EditorPrefs.DeleteKey(_pref); }
        if (_leftPref != null) { if (_hadLeft) EditorPrefs.SetFloat(_leftPref, _oldLeft); else EditorPrefs.DeleteKey(_leftPref); }
        if (_window != null) _window.Close(); if (_previous != null) _previous.Focus();
        foreach (var obj in Objects) if (obj != null) { Undo.ClearUndo(obj); Object.DestroyImmediate(obj); }
        Objects.Clear();
        Lines.Add($"{_passed} passed / {_failed} failed. Memory fixtures only. No production asset or scene writes.");
        Directory.CreateDirectory(Path.GetDirectoryName(Report)); File.WriteAllLines(Report, Lines);
        Debug.Log("[Shop Maker verification] " + Lines.Last() + " " + Report);
    }
}
