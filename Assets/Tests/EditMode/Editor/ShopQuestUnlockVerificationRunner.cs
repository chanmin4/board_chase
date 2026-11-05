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
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>메모리 SO와 별도 UI 창만 사용. 실제 상품/퀘스트/씬/세이브는 저장하지 않으며 글로벌 Undo는 실행하지 않는다.</summary>
public static class ShopQuestUnlockVerificationRunner
{
    public const string Report = "Library/ShopMakerVerification/quest-unlocks-v1.txt";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly List<string> Lines = new();
    private static readonly List<Object> Objects = new();
    private static readonly Dictionary<string, Rect> Controls = new();
    private static ShopProgressionTreeWindow _shopWindow;
    private static QuestDesignTreeWindow _questWindow;
    private static EditorWindow _previous;
    private static ShopConfigSO _shop;
    private static QuestDefinitionSO _quest;
    private static ItemSO _first, _second;
    private static string _shopBefore, _questBefore, _itemsBefore, _id;
    private static Dictionary<string, string> _filesBefore;
    private static Dictionary<int, string> _scenesBefore;
    private static int _passed, _failed, _phase, _frames;
    private static double _started;
    public static void Run()
    {
        if (_shopWindow != null || _questWindow != null || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        Lines.Clear(); Controls.Clear(); _passed = _failed = _phase = _frames = 0;
        _previous = EditorWindow.focusedWindow; _filesBefore = Files(); _scenesBefore = Scenes();
        foreach (var method in typeof(ShopMakerTests).GetMethods().Where(m => m.IsDefined(typeof(TestAttribute))))
        {
            if (method.GetCustomAttributes<CategoryAttribute>().Any(c => c.Name == "GlobalEditorUndo"))
            { Lines.Add("SKIP " + method.Name + " (global Undo is unsafe in the user's live editor)"); continue; }
            var fixture = new ShopMakerTests();
            try { fixture.SetUp(); method.Invoke(fixture, null); Check(true, method.Name); }
            catch (Exception ex) { Fail(method.Name + ": " + (ex.InnerException ?? ex)); }
            finally { try { fixture.TearDown(); } catch (Exception ex) { Fail("Test cleanup: " + ex); } }
        }
        try
        {
            _shop = Make<ShopConfigSO>(); _quest = Make<QuestDefinitionSO>();
            _first = Make<ItemSO>(); _first.name = "First temporary sale item";
            _second = Make<ItemSO>(); _second.name = "Second temporary sale item";
            var so = new SerializedObject(_quest);
            so.FindProperty("_questId").stringValue = "temporary_sale_quest";
            so.FindProperty("_displayName").stringValue = "Temporary quest -> sale unlock";
            so.FindProperty("_rewards").arraySize = 1;
            var reward = so.FindProperty("_rewards").GetArrayElementAtIndex(0);
            reward.FindPropertyRelative("_rewardType").intValue = (int)QuestRewardType.ShopUnlock;
            reward.FindPropertyRelative("_shopConfig").objectReferenceValue = _shop;
            so.ApplyModifiedPropertiesWithoutUndo();
            ShopMakerAuthoring.AddRewardProducts(_quest, 0, new[] { _first }); _id = _quest.Rewards[0].ShopUnlockNodeId;
            _shopBefore = Json(_shop); _questBefore = Json(_quest); _itemsBefore = Json(_first) + Json(_second);
            _shopWindow = ScriptableObject.CreateInstance<ShopProgressionTreeWindow>();
            Set(_shopWindow, "_config", _shop); Set(_shopWindow, "_quests", new List<QuestDefinitionSO> { _quest });
            Set(_shopWindow, "_guide", false); Set(_shopWindow, "_page", 1);
            Set(_shopWindow, "_showLegacyUnlocks", false);
            _shopWindow.ObserveControl = (id, rect) => Controls[id] = rect;
            _shopWindow.titleContent = new GUIContent("Shop Maker UI verification"); _shopWindow.position = new Rect(35, 40, 1250, 800);
            _shopWindow.ShowUtility(); _started = EditorApplication.timeSinceStartup;
            Application.logMessageReceived += OnLog; EditorApplication.update += Tick;
        }
        catch (Exception ex) { Fail("UI setup: " + ex); Finish(); }
    }
    private static void Tick()
    {
        try
        {
            if (_shopWindow == null && _questWindow == null) throw new Exception("Verification window closed early.");
            if (EditorApplication.timeSinceStartup - _started > 50) throw new Exception("UI verification timed out.");
            if (_questWindow != null) _questWindow.Repaint(); else _shopWindow.Repaint();
            if (++_frames < 12) return; _frames = 0;
            switch (_phase++)
            {
                case 0:
                    Check(!Get<bool>(_shopWindow, "_showLegacyUnlocks"), "Internal unlock groups hidden by default");
                    Click(_shopWindow, "unlockQuest:" + _quest.QuestId); break;
                case 1:
                    Check(Get<QuestDefinitionSO>(_shopWindow, "_selectedUnlockQuest") == _quest && Controls.ContainsKey("unlockQuest:edit"), "Quest card selects sale-unlock details and Quest Maker shortcut");
                    Set(_shopWindow, "_page", 0); Set(_shopWindow, "_selectedProductId", _shop.Products[0].ProductId); Set(_shopWindow, "_productTab", 2);
                    Controls.Clear(); break;
                case 2:
                    Check(Controls.ContainsKey("productUnlock:quest:" + _quest.QuestId), "Product sale condition shows the connected quest");
                    Check(Json(_shop) == _shopBefore && Json(_quest) == _questBefore, "Browsing Shop Maker never alters existing connections or IDs");
                    _shopWindow.Close(); _shopWindow = null; Controls.Clear();
                    _questWindow = ScriptableObject.CreateInstance<QuestDesignTreeWindow>();
                    var line = Make<QuestLineSO>(); QuestMakerGraph.AddToLine(line, _quest);
                    Set(_questWindow, "_questLine", line); Set(_questWindow, "_selectedQuest", _quest); Set(_questWindow, "_detailTab", 2);
                    Set(_questWindow, "_saleQuests", new List<QuestDefinitionSO> { _quest }); Set(_questWindow, "_workbenchWidth", 450f);
                    _questWindow.ObserveSaleControl = (id, rect) => Controls[id] = rect;
                    _questWindow.titleContent = new GUIContent("Quest Maker UI verification"); _questWindow.position = new Rect(35, 40, 1250, 800); _questWindow.ShowUtility(); break;
                case 3:
                    Check(Controls.ContainsKey("sale:0:shop") && Controls.ContainsKey("sale:0:drop") && Controls.ContainsKey("sale:0:items"), "Quest rewards render shop + ItemSO list + drop area");
                    Check(Json(_quest) == _questBefore && Json(_shop) == _shopBefore, "Viewing existing quest reward does not create or migrate nodes");
                    Click(_questWindow, "sale:0:advanced"); break;
                case 4:
                    Check(Get<HashSet<string>>(_questWindow, "_saleAdvanced").Count == 1, "Legacy connection controls can be explicitly expanded");
                    Click(_questWindow, "sale:0:advanced"); break;
                case 5:
                    Check(Get<HashSet<string>>(_questWindow, "_saleAdvanced").Count == 0, "Legacy controls collapse again");
                    Get<Dictionary<string, ItemSO>>(_questWindow, "_saleItemsToAdd")[_quest.GetInstanceID() + ":0"] = _second;
                    break;
                case 6: Click(_questWindow, "sale:0:add"); break;
                case 7:
                    Check(_shop.Products.Count == 2 && _shop.Products.All(p => !p.AvailableByDefault), "ItemSO add button registers quest-gated shop products");
                    Check(_quest.Rewards.Length == 1 && _quest.Rewards[0].ShopUnlockNodeId == _id && _shop.UnlockNodes.Count == 1,
                        "Adding items retains original reward index and saved unlock ID");
                    Check(Json(_first) + Json(_second) == _itemsBefore, "ItemSO identity, price and stats are unchanged");
                    Finish(); break;
            }
        }
        catch (Exception ex) { Fail("UI: " + ex); Finish(); }
    }
    private static T Make<T>() where T : ScriptableObject { var obj = ScriptableObject.CreateInstance<T>(); Objects.Add(obj); return obj; }
    private static string Json(Object obj) => EditorJsonUtility.ToJson(obj);
    private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Flags).SetValue(obj, value);
    private static T Get<T>(object obj, string name) => (T)obj.GetType().GetField(name, Flags).GetValue(obj);
    private static void Click(EditorWindow window, string id)
    {
        if (!Controls.TryGetValue(id, out var rect)) throw new Exception("Missing control " + id);
        window.SendEvent(new Event { type = EventType.MouseDown, button = 0, clickCount = 1, mousePosition = rect.center });
        window.SendEvent(new Event { type = EventType.MouseUp, button = 0, clickCount = 1, mousePosition = rect.center });
    }
    private static void OnLog(string condition, string stack, LogType type)
    {
        if ((type == LogType.Exception || type == LogType.Error) &&
            (stack.Contains("ShopProgressionTreeWindow") || stack.Contains("QuestDesignTreeWindow") || condition.Contains("GUILayout"))) Fail("GUI: " + condition + "\n" + stack);
    }
    private static Dictionary<string, string> Files()
    {
        using var sha = SHA256.Create();
        return new[] { "t:ShopConfigSO", "t:QuestDefinitionSO", "t:QuestLineSO", "t:ItemSO" }.SelectMany(AssetDatabase.FindAssets).Distinct()
            .Select(AssetDatabase.GUIDToAssetPath).Where(File.Exists).ToDictionary(p => p, p => Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(p))));
    }
    private static Dictionary<int, string> Scenes()
    {
        var result = new Dictionary<int, string>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i); if (!scene.isLoaded || EditorSceneManager.IsPreviewScene(scene)) continue;
            result[scene.handle] = scene.isDirty + "|" + string.Join("\n", scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .OrderBy(t => t.GetInstanceID()).Select(t => t.GetInstanceID() + ":" + Json(t.gameObject) + "|" +
                    string.Join("|", t.GetComponents<Component>().Where(c => c != null).Select(Json))));
        }
        return result;
    }
    private static void Check(bool condition, string label) { if (!condition) throw new Exception(label); Lines.Add("PASS " + label); _passed++; }
    private static void Fail(string label) { Lines.Add("FAIL " + label); _failed++; }
    private static void Finish()
    {
        EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
        if (_shopWindow != null) _shopWindow.Close(); if (_questWindow != null) _questWindow.Close();
        _shopWindow = null; _questWindow = null;
        foreach (var obj in Objects) if (obj != null) { Undo.ClearUndo(obj); Object.DestroyImmediate(obj); } Objects.Clear();
        try
        {
            var files = Files(); var scenes = Scenes();
            Check(_filesBefore.Count == files.Count && _filesBefore.All(p => files.TryGetValue(p.Key, out var value) && value == p.Value), "Real shop/quest/item asset files unchanged");
            Check(_scenesBefore.Count == scenes.Count && _scenesBefore.All(p => scenes.TryGetValue(p.Key, out var value) && value == p.Value), "Working scenes and dirty flags unchanged");
        }
        catch (Exception ex) { Fail("Preservation: " + ex); }
        if (_previous != null) _previous.Focus();
        Lines.Add($"{_passed} passed / {_failed} failed / 2 global-Undo tests skipped. Memory fixtures only; no actual shop, quest, save or scene written.");
        Directory.CreateDirectory(Path.GetDirectoryName(Report)); File.WriteAllLines(Report, Lines);
        Debug.Log("[Quest sale-unlock verification] " + Lines.Last() + " " + Report);
    }
}
