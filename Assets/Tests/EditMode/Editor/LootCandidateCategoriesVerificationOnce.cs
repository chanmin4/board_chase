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

// Temporary and explicitly armed: memory fixtures + a separate UI, never the user's Maker.
[InitializeOnLoad]
internal static class LootCandidateCategoriesVerificationOnce
{
    private const string Folder = "Library/LootCandidateCategoriesVerification/20261002";
    private static readonly List<string> Lines = new();
    private static Dictionary<string, string> _files, _scenes;
    private static Dictionary<UnityEngine.Object, (string, bool)> _memory;
    private static LootCandidateCategoriesProbe _probe;
    private static EditorWindow _previous;
    private static int _passed, _failed, _phase, _selected;
    private static double _ready, _deadline;
    private static LootMakerItemView.CandidateGroup _category, _first;
    private static string _term;
    static LootCandidateCategoriesVerificationOnce() => EditorApplication.update += Wait;
    private static void Wait()
    {
        if (File.Exists(Folder + "/started.txt")) { EditorApplication.update -= Wait; return; }
        if (!File.Exists(Folder + "/armed.txt")) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
        { _ready = EditorApplication.timeSinceStartup + 2; return; }
        if (EditorApplication.timeSinceStartup < _ready) return;
        EditorApplication.update -= Wait; Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "/started.txt", DateTime.UtcNow.ToString("O"));
        _files = Files(); _scenes = Scenes(); _previous = EditorWindow.focusedWindow;
        try
        {
            _memory = AssetDatabase.FindAssets("t:ScriptableObject").Select(AssetDatabase.GUIDToAssetPath).Distinct()
                .Select(AssetDatabase.LoadMainAssetAtPath).Where(a => a != null)
                .ToDictionary(a => a, a => (EditorJsonUtility.ToJson(a), EditorUtility.IsDirty(a)));
            foreach (var method in typeof(LootMakerItemViewTests).GetMethods().Where(m => m.IsDefined(typeof(TestAttribute))))
            {
                var fixture = new LootMakerItemViewTests();
                try { method.Invoke(fixture, null); Check(true, method.Name); }
                catch (Exception ex) { Check(false, method.Name + ": " + (ex.InnerException ?? ex)); }
                finally { fixture.Cleanup(); }
            }
            var groups = LootMakerItemView.CandidateGroups(MakerRegistrationIndex.Current.Items, null, false, "");
            _first = groups.First(); _category = groups.FirstOrDefault(g => g.Key == "Armor") ?? _first;
            _term = _category.Items.First().name;
            _probe = ScriptableObject.CreateInstance<LootCandidateCategoriesProbe>();
            _probe.titleContent = new GUIContent("Loot category verification");
            _probe.position = new Rect(70, 90, 600, 500); _probe.ShowUtility(); _probe.Focus();
            _deadline = EditorApplication.timeSinceStartup + 40;
            EditorApplication.update += Tick;
        }
        catch (Exception ex) { Check(false, ex.ToString()); Finish(); }
    }
    private static void Tick()
    {
        if (_probe == null || EditorApplication.timeSinceStartup > _deadline) { Check(false, "UI timeout"); Finish(); return; }
        if (_probe.Error != null) { Check(false, _probe.Error); Finish(); return; }
        _probe.Repaint(); if (_probe.Draws < 2) return;
        try
        {
            Check(_probe.Rects.Values.All(r => r.width >= 0 && r.xMin >= -2 && r.xMax <= _probe.position.width + 2), "UI bounds phase " + _phase);
            switch (_phase++)
            {
                case 0:
                    Check(_probe.Headers.Count > 0 && _probe.Items.Count == 0, "Default view shows collapsed categories, not all items");
                    _probe.Set("_candidateCategoryKey", _category.Key); break;
                case 1:
                    Check(_probe.Headers.SequenceEqual(new[] { "LootCandidates/Category/" + _category.Key }) && _probe.Items.Count == _category.Items.Length, "Category filter opens only its matching items");
                    _probe.Set("_candidateSearch", _term); break;
                case 2:
                    int expected = LootMakerItemView.CandidateGroups(MakerRegistrationIndex.Current.Items, null, false, _term, _category.Key).Sum(g => g.Items.Length);
                    Check(_probe.Items.Count == expected, "Name search composes with category filter");
                    _probe.Click("LootCandidates/Category/" + _category.Key, true); break;
                case 3:
                    Check(_probe.Items.Count == 0, "Real foldout click can collapse focused search results");
                    _probe.Set("_candidateCategoryKey", ""); _probe.Set("_candidateSearch", ""); break;
                case 4:
                    Check(_probe.Items.Count == 0, "Clearing search restores unfiltered folded view");
                    _probe.Click("LootCandidates/Category/" + _first.Key, true); break;
                case 5:
                    Check(_probe.Items.Count == _first.Items.Length, "Real foldout click opens one category only");
                    _probe.Click("LootCandidates/SelectVisible"); break;
                case 6:
                    _selected = _probe.Selection.Count;
                    Check(_selected == _first.Items.Length, "Visible selection excludes folded categories");
                    _probe.Click("LootCandidates/CollapseAll"); break;
                case 7:
                    Check(_probe.Items.Count == 0 && _probe.Selection.Count == _selected && _probe.Rects.ContainsKey("LootCandidates/HiddenSelection"), "Collapse preserves selection and shows hidden-selection notice");
                    _probe.Set("_candidateSearch", "NoMatches-6c2e1feb"); break;
                case 8:
                    Check(_probe.Items.Count == 0 && _probe.Headers.Count == 0 && _probe.Selection.Count == _selected, "Empty search results preserve selected items");
                    _probe.Set("_candidateSearch", _term); _probe.Set("_candidateCategoryKey", _category.Key);
                    _probe.position = new Rect(70, 90, 430, 500); break;
                default:
                    Check(_probe.Items.Count > 0, "Narrow 430px category/search panel renders successfully"); Finish(); return;
            }
            _probe.Draws = 0;
        }
        catch (Exception ex) { Check(false, ex.ToString()); Finish(); }
    }
    private static Dictionary<string, string> Files()
    {
        var result = new Dictionary<string, string>();
        foreach (string root in new[] { "Assets/ScriptableObject", "Assets/Prefabs/GameScene", "Assets/Scenes" })
            if (Directory.Exists(root)) foreach (string path in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            { using var stream = File.OpenRead(path); using var hash = SHA256.Create(); result[path] = Convert.ToBase64String(hash.ComputeHash(stream)); }
        return result;
    }
    private static Dictionary<string, string> Scenes()
    {
        var result = new Dictionary<string, string>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        { var scene = SceneManager.GetSceneAt(i); result[scene.handle.ToString()] = scene.path + "|" + scene.isDirty + "|" + string.Join(",", scene.GetRootGameObjects().Select(o => o.GetInstanceID())); }
        return result;
    }
    private static bool Equal(Dictionary<string, string> a, Dictionary<string, string> b) => a.Count == b.Count && a.All(p => b.TryGetValue(p.Key, out var value) && p.Value == value);
    private static void Check(bool passed, string message) { if (passed) _passed++; else _failed++; Lines.Add((passed ? "PASS " : "FAIL ") + message); }
    private static void Finish()
    {
        EditorApplication.update -= Tick;
        if (_probe != null) { _probe.Close(); _probe = null; }
        Check(_memory == null || _memory.All(p => p.Key != null && EditorJsonUtility.ToJson(p.Key) == p.Value.Item1 && EditorUtility.IsDirty(p.Key) == p.Value.Item2), "Actual asset memory/dirty flags preserved");
        Check(Equal(_files, Files()), "Actual item/loot/prefab/scene files preserved");
        Check(Equal(_scenes, Scenes()), "Open scene roots and dirty state preserved");
        Lines.Insert(0, _passed + " passed / " + _failed + " failed"); File.WriteAllLines(Folder + "/unity-tests.txt", Lines);
        Debug.Log("[Loot category verification] " + Lines[0]); if (_previous != null) _previous.Focus();
    }
}

internal sealed class LootCandidateCategoriesProbe : EditorWindow
{
    internal int Draws;
    internal string Error;
    internal readonly Dictionary<string, Rect> Rects = new();
    internal readonly List<string> Headers = new(), Items = new();
    private LootProgressionEditorWindow _host;
    private LootTableSO _table;
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private void OnEnable()
    {
        _host = ScriptableObject.CreateInstance<LootProgressionEditorWindow>(); _table = ScriptableObject.CreateInstance<LootTableSO>();
        _host.GuiControlObserved = (name, rect) => { Rects[name] = rect; if (name.StartsWith("LootCandidates/Category/")) Headers.Add(name); if (name.StartsWith("LootCandidates/Item/")) Items.Add(name); };
    }
    internal void Set(string field, object value) => typeof(LootProgressionEditorWindow).GetField(field, Flags).SetValue(_host, value);
    internal HashSet<ItemSO> Selection => (HashSet<ItemSO>)typeof(LootProgressionEditorWindow).GetField("_candidateSelection", Flags).GetValue(_host);
    internal void Click(string control, bool arrow = false)
    {
        Rect rect = Rects[control]; Vector2 point = arrow ? new Vector2(rect.x + 8, rect.center.y) : rect.center;
        foreach (var type in new[] { EventType.MouseDown, EventType.MouseUp }) SendEvent(new Event { type = type, button = 0, mousePosition = point });
    }
    private void OnGUI()
    {
        bool repaint = Event.current.type == EventType.Repaint;
        if (repaint) { Rects.Clear(); Headers.Clear(); Items.Clear(); }
        Set("_guiScreenOrigin", GUIUtility.GUIToScreenPoint(Vector2.zero));
        try { typeof(LootProgressionEditorWindow).GetMethod("DrawRegistrationCandidates", Flags).Invoke(_host, new object[] { _table }); }
        catch (TargetInvocationException ex) when (ex.InnerException is ExitGUIException) { throw ex.InnerException; }
        catch (Exception ex) { Error = (ex.InnerException ?? ex).ToString(); }
        if (repaint) Draws++;
    }
    private void OnDisable()
    {
        if (_host != null) DestroyImmediate(_host); if (_table != null) DestroyImmediate(_table);
    }
}
