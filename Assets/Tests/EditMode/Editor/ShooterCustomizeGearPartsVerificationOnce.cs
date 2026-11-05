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

// 일회성 격리 검사. 새 Tools 메뉴 / 실제 에셋 저장 / 사용자 Undo 실행을 하지 않는다.
[InitializeOnLoad]
internal static class ShooterCustomizeGearPartsVerificationOnce
{
    private const string Folder = "Library/ShooterCustomizeVerification/GearParts";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly List<string> Lines = new();
    private static readonly Dictionary<string, Rect> Controls = new();
    private static Dictionary<string, string> _files;
    private static Dictionary<int, string> _scene;
    private static Scene _active;
    private static bool _dirty;
    private static EditorWindow _previous;
    private static ShooterCustomizeStationWindow _window;
    private static int _passed, _failed, _skipped, _phase, _frames, _dirtyEvents;
    private static double _ready, _start;
    private static bool _finishing;
    private static readonly HashSet<string> Images = new();

    static ShooterCustomizeGearPartsVerificationOnce() { EditorApplication.update += WaitUntilReady; }
    private static void WaitUntilReady()
    {
        if (File.Exists(Folder + "/started.txt")) { EditorApplication.update -= WaitUntilReady; return; }
        // 사용자가 저장·Refresh 완료를 알려준 뒤에만 검사 창을 연다.
        if (!File.Exists(Folder + "/armed.txt")) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
        { _ready = EditorApplication.timeSinceStartup + 2; return; }
        if (EditorApplication.timeSinceStartup < _ready) return;
        EditorApplication.update -= WaitUntilReady;
        Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "/started.txt", DateTime.UtcNow.ToString("O"));
        Run();
    }

    private static void Run()
    {
        _active = SceneManager.GetActiveScene(); _dirty = _active.isDirty; _scene = SceneSnapshot(); _files = Files();
        _previous = EditorWindow.focusedWindow;
        EditorSceneManager.sceneDirtied += SceneDirtied;
        Application.logMessageReceived += OnLog;
        try
        {
            foreach (var method in typeof(ShooterCustomizeStationTests).GetMethods().Where(m => m.IsDefined(typeof(TestAttribute))))
            {
                if (method.GetCustomAttributes<CategoryAttribute>().Any(c => c.Name == "GlobalEditorUndo"))
                { _skipped++; Lines.Add("SKIP " + method.Name + ": do not replay the user's global Undo history"); continue; }
                var fixture = new ShooterCustomizeStationTests();
                try { fixture.Setup(); method.Invoke(fixture, null); Check(true, method.Name); }
                catch (Exception ex) { Fail(ex.InnerException ?? ex); }
                finally { fixture.Cleanup(); }
            }
            _window = ScriptableObject.CreateInstance<ShooterCustomizeStationWindow>();
            _window.titleContent = new GUIContent("Gear parts UI verification"); _window.position = new Rect(25, 45, 1250, 900);
            _window.ObserveControl = (key, rect) => Controls[key] = rect;
            _window.ObservePreview = CapturePreview;
            var target = ShooterCustomizeCatalog.Discover().First(t => t.Name.Contains("Scraper") && t.HasVisualRig);
            Invoke("LoadTarget", target);
            Draft.LoadWeapon(Find<RangedWeaponSO>("AssaultRifle1")); Draft.LoadArmor(Find<ArmorItemSO>("ArmorLV1"));
            Draft.LoadEquipment(Find<GasMaskItemSO>("GasMaskLV1")); Draft.LoadBackpack(Find<BackpackItemSO>("BackPack_LV1"));
            Invoke("RebuildPreview", false); Invoke("OpenFittingPart", ShooterVisualEquipmentKind.Armor);
            _window.ShowUtility(); _start = EditorApplication.timeSinceStartup; EditorApplication.update += Tick;
        }
        catch (Exception ex) { Fail(ex); Finish(); }
    }
    private static T Find<T>(string name) where T : UnityEngine.Object => AssetDatabase.FindAssets("t:" + typeof(T).Name)
        .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<T>).First(o => o != null && o.name == name);
    private static ShooterCustomizeDraft Draft => Get<ShooterCustomizeDraft>("_draft");
    private static ShooterCustomizePreview Preview => Get<ShooterCustomizePreview>("_preview");
    private static T Get<T>(string field) => (T)typeof(ShooterCustomizeStationWindow).GetField(field, Flags).GetValue(_window);
    private static void Set(string field, object value) => typeof(ShooterCustomizeStationWindow).GetField(field, Flags).SetValue(_window, value);
    private static void Invoke(string method, params object[] args) => typeof(ShooterCustomizeStationWindow).GetMethod(method, Flags).Invoke(_window, args);
    private static void Tick()
    {
        if (_finishing) return;
        try
        {
            if (_window == null) throw new Exception("Verification window closed before completion");
            if (EditorApplication.timeSinceStartup - _start > 45) throw new Exception("UI verification timed out; keep the utility window visible");
            _window.Repaint(); if (++_frames < 8) return; _frames = 0;
            Check(string.IsNullOrEmpty(Get<string>("_error")), "No workbench / preview error in phase " + _phase);
            switch (_phase++)
            {
                case 0:
                    Card(ShooterVisualEquipmentKind.Armor);
                    Check(Get<int>("_part") == 0 && !Get<bool>("_equipmentMode"), "Armor is editable directly in Body");
                    Invoke("EditFitting", ShooterVisualEquipmentKind.Armor, (Action)(() => Draft.armorPose.localPosition += Vector3.up * .01f));
                    break;
                case 1:
                    Check(Draft.armorDirty && !Draft.lookDirty && !Get<bool>("_bindingDirty"), "Item placement marks only its own draft, not the look or spawn data");
                    Check(Preview.ArmorVisual.transform.localPosition == Draft.armorPose.localPosition, "Body editor placement reaches the rendered armor");
                    Click("fitting:visible:Armor"); break;
                case 2:
                    Check(Preview.ArmorVisual == null && Preview.WeaponVisual != null && Preview.MaskVisual != null && Preview.BackpackVisual != null,
                        "Body visibility toggle hides only the armor");
                    Check(Draft.armorDirty, "Hiding armor retains its unsaved placement");
                    Click("fitting:visible:Armor"); Invoke("OpenFittingPart", ShooterVisualEquipmentKind.GasMask); break;
                case 3:
                    Card(ShooterVisualEquipmentKind.GasMask);
                    Check(Get<int>("_part") == 3, "Gas mask is editable directly in Mouth");
                    Check(Preview.MaskVisual != null && Draft.armorDirty, "Part switching keeps all gear and pending armor changes");
                    Invoke("OpenFittingPart", ShooterVisualEquipmentKind.Weapon); break;
                case 4:
                    Card(ShooterVisualEquipmentKind.Weapon);
                    Check(Preview.WeaponVisual.transform.parent == Preview.Rig.ResolveSocket(ShooterVisualSocketId.RightHand), "Actual weapon mount remains RightHand");
                    Set("_part", 9); Set("_editScroll", Vector2.zero); break;
                case 5:
                    Card(ShooterVisualEquipmentKind.Weapon);
                    Check(Preview.WeaponVisual.transform.parent == Preview.Rig.ResolveSocket(ShooterVisualSocketId.RightHand), "LeftHand editor shortcut does not reparent the actual weapon");
                    Invoke("OpenFittingPart", ShooterVisualEquipmentKind.Backpack); break;
                case 6:
                    Check(Controls.ContainsKey("fitting:select:Backpack") && Controls.ContainsKey("gear:backpack-save"), "Back lists the tested backpack with its existing Item SO save editor");
                    Check(Controls.ContainsKey("anchor:backpack") && Preview.BackpackVisual != null, "Back editor uses the actual backpack socket and renderer");
                    Draft.ReloadEquipmentEdits(); Invoke("RebuildPreview", false);
                    Invoke("OpenFittingPart", ShooterVisualEquipmentKind.Armor); Set("_workbenchWidth", 330f); break;
                case 7:
                    Card(ShooterVisualEquipmentKind.Armor);
                    Check(!Draft.HasEquipmentEdits && !Draft.lookDirty, "Reload discards only temporary Item placements and keeps authored data unchanged");
                    var rect = Controls["fitting:select:Armor"];
                    Check(rect.width > 90 && rect.height >= 42 && rect.x >= _window.position.width - 335 && rect.xMax < _window.position.width,
                        "Gear card fits the minimum-width 330px workbench");
                    Finish(); break;
            }
        }
        catch (Exception ex) { Fail(ex.InnerException ?? ex); Finish(); }
    }
    private static void Card(ShooterVisualEquipmentKind kind)
    {
        Check(Controls.ContainsKey("fitting:select:" + kind) && Controls.ContainsKey("fitting:pose:" + kind) &&
            Controls.ContainsKey("fitting:save:" + kind) && Controls.ContainsKey("fitting:reload:" + kind), kind + " card, transform and explicit save/reload controls are drawn");
    }
    private static void Click(string key)
    {
        if (!Controls.TryGetValue(key, out var rect)) throw new Exception("Missing UI control: " + key);
        if (rect.y < 0 || rect.yMax > _window.position.height) throw new Exception("Control outside the visible window: " + key);
        _window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = rect.center });
        _window.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = rect.center });
    }
    private static void CapturePreview(Texture texture)
    {
        string name = "phase-" + _phase;
        if (!Images.Add(name)) return;
        var rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active; Texture2D pixels = null;
        try
        {
            Graphics.Blit(texture, rt); RenderTexture.active = rt;
            pixels = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); pixels.Apply();
            File.WriteAllBytes(Folder + "/" + name + ".png", pixels.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels); }
    }
    private static void Check(bool ok, string label) { if (!ok) throw new Exception(label); _passed++; Lines.Add("PASS " + label); }
    private static void Fail(Exception ex) { _failed++; Lines.Add("FAIL " + ex); }
    private static void SceneDirtied(Scene scene) { if (scene == _active) _dirtyEvents++; }
    private static void OnLog(string message, string stack, LogType kind)
    {
        if ((kind == LogType.Error || kind == LogType.Exception) && (stack.Contains("ShooterCustomize") || message.Contains("GUILayout")))
        { _failed++; Lines.Add("FAIL Unity log: " + message + "\n" + stack); }
    }
    private static Dictionary<int, string> SceneSnapshot() => SceneManager.GetActiveScene().GetRootGameObjects()
        .SelectMany(r => r.GetComponentsInChildren<Transform>(true)).SelectMany(t => new UnityEngine.Object[] { t.gameObject }
            .Concat(t.GetComponents<Component>().Where(c => c != null))).ToDictionary(o => o.GetInstanceID(), o => EditorJsonUtility.ToJson(o));
    private static Dictionary<string, string> Files()
    {
        using var sha = SHA256.Create();
        return new[] { "Assets/ScriptableObject/GameScene", "Assets/Prefabs/GameScene/Shooter" }
            .SelectMany(p => Directory.GetFiles(p, "*", SearchOption.AllDirectories))
            .ToDictionary(p => p, p => Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(p))));
    }
    private static void Finish()
    {
        _finishing = true; EditorApplication.update -= Tick;
        try
        {
            if (_window != null) { _window.DiscardChanges(); _window.Close(); _window = null; }
            var files = Files(); var scene = SceneSnapshot();
            Check(_files.Count == files.Count && _files.All(p => files.TryGetValue(p.Key, out var value) && value == p.Value), "Real character / look / Item SO files unchanged");
            Check(_active == SceneManager.GetActiveScene() && _active.isDirty == _dirty && _dirtyEvents == 0, "Active user scene and dirty flags unchanged");
            Check(_scene.Count == scene.Count && _scene.All(p => scene.TryGetValue(p.Key, out var value) && value == p.Value), "All user scene objects / components unchanged");
        }
        catch (Exception ex) { Fail(ex); }
        finally { EditorSceneManager.sceneDirtied -= SceneDirtied; Application.logMessageReceived -= OnLog; if (_previous != null) _previous.Focus(); }
        Lines.Add($"{_passed} passed / {_failed} failed / {_skipped} skipped (global Undo). No real assets or user scenes saved.");
        File.WriteAllLines(Folder + "/unity-tests.txt", Lines); Debug.Log("[Gear parts verification] " + Lines.Last());
    }
}
