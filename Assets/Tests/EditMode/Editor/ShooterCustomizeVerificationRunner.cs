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

/// <summary>Temporary fixture tests + a separate, read-only UI verification window.</summary>
public static class ShooterCustomizeVerificationRunner
{
    public const string Report = "Library/ShooterCustomizeVerification/unity-tests.txt";
    public static Action<bool> Completed;
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly List<string> Lines = new();
    private static readonly Dictionary<string, Rect> Controls = new();
    private static ShooterCustomizeStationWindow _window;
    private static EditorWindow _previous;
    private static int _passed, _failed, _skipped, _frames, _phase;
    private static double _start;
    private static Scene _active;
    private static bool _dirty;
    private static int _roots;
    private static Dictionary<string, string> _before;
    private static float _width;
    private static Vector2 _orbitBefore;
    private static Vector3 _panBefore, _cameraBefore;
    private static bool _lookDirtyBeforeNavigation, _equipmentDirtyBeforeNavigation, _bindingDirtyBeforeNavigation;
    private static ShooterVisualAttachmentSO _uiDefinition;
    private static string _operation;
    private static int _sceneDirtyEvents;
    private static Dictionary<int, string> _sceneObjects;
    private static bool _backAnchorLookDirtyBefore, _previewImageSaved;

    [MenuItem("Tools/Authoring/Verification/Run Shooter Customize Tests")]
    public static void Run()
    {
        if (_window != null || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || PrefabStageUtility.GetCurrentPrefabStage() != null) return;
        Lines.Clear(); Controls.Clear(); _passed = _failed = _skipped = _frames = _phase = 0;
        _previewImageSaved = false;
        _active = SceneManager.GetActiveScene(); _dirty = _active.isDirty; _roots = _active.rootCount;
        _sceneDirtyEvents = 0; _sceneObjects = SceneSnapshot(_active);
        _operation = "initial asset snapshots";
        EditorSceneManager.sceneDirtied += OnSceneDirtied;
        Application.logMessageReceived += OnLog;
        _before = ShooterCustomizeCatalog.Discover().Select(t => t.Path)
            .Concat(new[] { "Assets/Prefabs/GameScene/Shooter/Visual_Shooter.prefab" })
            .Concat(AssetDatabase.FindAssets("t:ItemSO").Select(AssetDatabase.GUIDToAssetPath))
            .Concat(AssetDatabase.FindAssets("t:ShooterCustomizeSO").Select(AssetDatabase.GUIDToAssetPath))
            .Concat(AssetDatabase.FindAssets("t:ShooterVisualAttachmentSO").Select(AssetDatabase.GUIDToAssetPath))
            .Concat(AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/GameScene/Shooter/Accesory" }).Select(AssetDatabase.GUIDToAssetPath))
            .Distinct().ToDictionary(p => p, File.ReadAllText);
        foreach (var type in new[] { typeof(ShooterSpawnLookTests), typeof(ShooterCustomizeStationTests) })
        foreach (var method in type.GetMethods().Where(m => m.IsDefined(typeof(TestAttribute))))
        {
            // This read-only verifier runs alongside the user's live editor session.
            // Do not flush or replay its global Undo stack, even for temporary SOs.
            if (method.GetCustomAttributes<CategoryAttribute>().Any(category => category.Name == "GlobalEditorUndo"))
            {
                Lines.Add("SKIP " + method.Name + ": Global editor Undo requires an isolated Test Runner session.");
                _skipped++;
                continue;
            }
            _operation = type.Name + "." + method.Name;
            var fixture = Activator.CreateInstance(type);
            var setup = type.GetMethods().Single(m => m.IsDefined(typeof(SetUpAttribute)));
            var cleanup = type.GetMethods().Single(m => m.IsDefined(typeof(TearDownAttribute)));
            try { setup.Invoke(fixture, null); method.Invoke(fixture, null); Pass(method.Name); }
            catch (Exception ex) { Fail(method.Name + ": " + (ex.InnerException ?? ex)); }
            finally { try { cleanup.Invoke(fixture, null); } catch (Exception ex) { Fail("Cleanup: " + ex); } }
        }
        try
        {
            _operation = "UI setup";
            _previous = EditorWindow.focusedWindow;
            _window = ScriptableObject.CreateInstance<ShooterCustomizeStationWindow>();
            _window.titleContent = new GUIContent("Shooter Customize UI verification");
            _window.position = new Rect(25, 45, 1250, 780);
            _window.ObserveControl = (key, rect) => Controls[key] = rect;
            _window.ShowUtility();
            _start = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }
        catch (Exception ex) { Fail("UI setup: " + ex); Finish(); }
    }

    private static void OnSceneDirtied(Scene scene)
    {
        if (scene == _active)
        {
            _sceneDirtyEvents++;
            Lines.Add("DIAGNOSTIC User scene dirtied during " + _operation + "\n" + Environment.StackTrace);
        }
    }

    private static Dictionary<int, string> SceneSnapshot(Scene scene) => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
        .SelectMany(transform => new UnityEngine.Object[] { transform.gameObject }.Concat(transform.GetComponents<Component>().Where(c => c != null)))
        .ToDictionary(obj => obj.GetInstanceID(), obj => EditorJsonUtility.ToJson(obj));

    private static void OnLog(string condition, string stack, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception) && (stack.Contains("ShooterCustomize") || condition.Contains("GUILayout")))
            Fail(_operation + ": " + condition + "\n" + stack);
    }

    private static void Tick()
    {
        try
        {
            if (_window == null) throw new Exception("Verification window closed early.");
            if (EditorApplication.timeSinceStartup - _start > 50) throw new Exception("UI verification timed out. Keep the window visible.");
            _window.Repaint(); if (++_frames < 10) return; _frames = 0;
            _operation = "UI phase " + _phase;
            switch (_phase++)
            {
                case 0:
                    Require(Controls.ContainsKey("group:Player") && Controls.ContainsKey("group:NPC"), "Player and NPC categories are visible");
                    Click("target:Player"); break;
                case 1:
                    Require(Get<string>("_targetPath").EndsWith("/Player.prefab"), "Click selects actual Player target");
                    Require(Controls.ContainsKey("parts"), "Body part workbench remains available");
                    Click("group:NPC"); break;
                case 2:
                    Require(!Get<List<string>>("_collapsedGroups").Contains("NPC"), "NPC group expands");
                    Click("group:NPC"); break;
                case 3:
                    Require(Get<List<string>>("_collapsedGroups").Contains("NPC"), "NPC group collapses");
                    Click("group:NPC"); break;
                case 4:
                    var npc = ShooterCustomizeCatalog.Discover().First(t => t.Group == "NPC");
                    Click("target:" + npc.Name); break;
                case 5:
                    Require(Get<string>("_targetPath").Contains("/HomeNPC/"), "NPC target selection updates independently");
                    _width = Get<float>("_catalogWidth"); var rect = Controls["split:left"];
                    Send(EventType.MouseDown, rect.center); Send(EventType.MouseDrag, rect.center + Vector2.right * 25, Vector2.right * 25); Send(EventType.MouseUp, rect.center + Vector2.right * 25);
                    break;
                case 6:
                    Require(Get<float>("_catalogWidth") > _width, "Category pane resizes by dragging");
                    typeof(ShooterCustomizeStationWindow).GetField("_targetSearch", Flags).SetValue(_window, "RaiderShooterConfig"); break;
                case 7:
                    Require(Controls.ContainsKey("target:RaiderShooterConfig"), "Search finds Raider target");
                    Click("target:RaiderShooterConfig"); break;
                case 8:
                    Require(Get<string>("_targetPath").EndsWith("/RaiderShooterConfig.asset"), "Raider selects its own config, not a shared prefab default");
                    Require(!Get<bool>("_bindingDirty"), "Browsing does not dirty spawn bindings");
                    Require(Controls.ContainsKey("appearance-mode"), "Real inherited rig enables the three explicit appearance modes");
                    typeof(ShooterCustomizeStationWindow).GetField("_targetSearch", Flags).SetValue(_window, "Scraper"); break;
                case 9:
                    Click("target:ScraperShooterConfig"); break;
                case 10:
                    Require(Get<string>("_targetPath").EndsWith("/ScraperShooterConfig.asset"), "One Scraper target replaces all stage configs");
                    Require(Controls.ContainsKey("appearance-mode"), "Scraper appearance controls are usable");
                    Rect mode = Controls["appearance-mode"];
                    Vector2 point = new(mode.x + mode.width * 5 / 6, mode.center.y);
                    Send(EventType.MouseDown, point); Send(EventType.MouseUp, point); break;
                case 11:
                    Require(Get<ShooterSpawnLookMode>("_spawnMode") == ShooterSpawnLookMode.RandomPool, "Several random looks can be selected directly without a hidden dropdown");
                    Require(Get<bool>("_bindingDirty"), "Mode edits remain unsaved until explicit Apply");
                    Require(Controls.ContainsKey("anchor:position") && Controls.ContainsKey("anchor:reset"), "Anchor position and reset are visible outside Advanced");
                    Rect parts = Controls["parts"];
                    Vector2 gearPoint = new(parts.center.x, parts.y + parts.height * 7 / 8);
                    Send(EventType.MouseDown, gearPoint); Send(EventType.MouseUp, gearPoint); break;
                case 12:
                    Require(Get<bool>("_equipmentMode"), "Equipment test opens from the part selector");
                    Require(Controls.ContainsKey("gear:weapon") && Controls.ContainsKey("gear:armor") && Controls.ContainsKey("gear:mask") && Controls.ContainsKey("gear:backpack"), "All four Item SO fitting controls are visible");
                    var draft = Get<ShooterCustomizeDraft>("_draft");
                    draft.previewWeapon = FindItem<RangedWeaponSO>(w => w.WeaponViewPrefab != null);
                    draft.previewArmor = FindItem<ArmorItemSO>(a => a.EquippedVisualPrefab != null);
                    draft.LoadEquipment(FindItem<GasMaskItemSO>(m => m.EquippedVisualPrefab != null));
                    draft.LoadBackpack(FindItem<BackpackItemSO>(b => b.EquippedVisualPrefab != null));
                    Set("_applyNeeded", true); break;
                case 13:
                    var preview = Get<ShooterCustomizePreview>("_preview");
                    Require(preview.WeaponVisual != null && preview.ArmorVisual != null && preview.MaskVisual != null && preview.BackpackVisual != null, "Weapon armor mask and backpack render together on the real Scraper preview");
                    Require(!Get<ShooterCustomizeDraft>("_draft").lookDirty, "Trying real gear does not dirty the appearance");
                    Require(Controls.ContainsKey("anchor:backpack"), "Equipment viewport displays the actual Back anchor, not Face");
                    Set("_orbit", new Vector2(90, 0)); Set("_zoom", 1.25f); Set("_pan", Vector3.zero); preview.Frame();
                    _window.ObservePreview = texture =>
                    {
                        SavePreviewImage(texture); _previewImageSaved = true; _window.ObservePreview = null;
                    };
                    Click("gear:ignore"); break;
                case 14:
                    Require(_previewImageSaved, "Isolated side-view image saved for visual QA");
                    Require(Get<ShooterCustomizeDraft>("_draft").ignoreEquipmentHiding, "Comparison mode can be toggled directly");
                    Click("gear:clear"); break;
                case 15:
                    Require(!Get<ShooterCustomizeDraft>("_draft").HasPreviewEquipment, "Remove all test gear clears selections without saving");
                    // Add only to the test window's in-memory draft; never save a real look.
                    var temporaryDraft = Get<ShooterCustomizeDraft>("_draft");
                    var accessory = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/GameScene/Shooter/Accesory" })
                        .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<GameObject>).First();
                    temporaryDraft.attachments.Clear();
                    temporaryDraft.attachments.Add(new ShooterCustomizeAttachment { socket = ShooterVisualSocketId.Hair,
                        visualPrefab = accessory, localTransform = ShooterVisualLocalTransform.Identity });
                    Set("_equipmentMode", false); Set("_part", 4); Set("_applyNeeded", true); break;
                case 16:
                    Require(Controls.ContainsKey("accessory:remove:0"), "Face editor can see and remove a Hair accessory through the all-parts list");
                    Click("accessory:remove:0"); break;
                case 17:
                    Require(Get<ShooterCustomizeDraft>("_draft").attachments.Count == 0, "Remove accessory acts on the selected draft entry");
                    _lookDirtyBeforeNavigation = Get<ShooterCustomizeDraft>("_draft").lookDirty;
                    _equipmentDirtyBeforeNavigation = Get<ShooterCustomizeDraft>("_draft").equipmentDirty;
                    _bindingDirtyBeforeNavigation = Get<bool>("_bindingDirty");
                    _orbitBefore = Get<Vector2>("_orbit");
                    DragViewport(0); break;
                case 18:
                    Require(Get<Vector2>("_orbit") == _orbitBefore, "Unmodified left drag no longer rotates the preview");
                    _cameraBefore = Get<ShooterCustomizePreview>("_preview").Camera.transform.position;
                    DragViewport(1); break;
                case 19:
                    Require(Get<Vector2>("_orbit") != _orbitBefore, "Right drag rotates the view");
                    Require(Vector3.Distance(_cameraBefore, Get<ShooterCustomizePreview>("_preview").Camera.transform.position) < .001f, "RMB look preserves camera position like Scene view");
                    _panBefore = Get<Vector3>("_pan");
                    Send(EventType.MouseDown, Controls["viewport"].center, button: 1);
                    Key(EventType.KeyDown, KeyCode.W); break;
                case 20:
                    Require(Vector3.Distance(_panBefore, Get<Vector3>("_pan")) > .001f, "Held RMB plus W flies forward across editor updates");
                    Key(EventType.KeyUp, KeyCode.W); Send(EventType.MouseUp, Controls["viewport"].center, button: 1);
                    _panBefore = Get<Vector3>("_pan"); _orbitBefore = Get<Vector2>("_orbit"); break;
                case 21:
                    Require(Get<Vector3>("_pan") == _panBefore, "Releasing navigation stops movement");
                    Key(EventType.KeyDown, KeyCode.W); Key(EventType.KeyUp, KeyCode.W);
                    DragViewport(0, true); break;
                case 22:
                    Require(Get<Vector3>("_pan") == _panBefore, "W outside RMB navigation is not captured");
                    Require(Get<Vector2>("_orbit") != _orbitBefore, "Alt left drag orbits around the character");
                    Key(EventType.KeyDown, KeyCode.F); break;
                case 23:
                    Require(Get<Vector3>("_pan") == Vector3.zero && Get<float>("_zoom") == 1, "F frames the character after flying");
                    Require(Get<ShooterCustomizeDraft>("_draft").lookDirty == _lookDirtyBeforeNavigation &&
                        Get<ShooterCustomizeDraft>("_draft").equipmentDirty == _equipmentDirtyBeforeNavigation &&
                        Get<bool>("_bindingDirty") == _bindingDirtyBeforeNavigation, "Camera input leaves all authoring dirty flags unchanged");
                    _uiDefinition = ScriptableObject.CreateInstance<ShooterVisualAttachmentSO>(); _uiDefinition.hideFlags = HideFlags.HideAndDontSave;
                    _uiDefinition.Configure("UI only", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameScene/Shooter/Accesory/Eyes_Thief_Eye_Mask_01.prefab"),
                        ShooterVisualSocketId.Eyes, new ShooterVisualLocalTransform(Vector3.back * .5f, Vector3.zero, Vector3.one),
                        ShooterVisualOccupancy.None, ShooterVisualEquipmentKind.None);
                    typeof(ShooterCustomizeStationWindow).GetMethod("AddAccessoryAsset", Flags).Invoke(_window, new object[] { _uiDefinition });
                    Set("_editScroll", new Vector2(0, 280)); break;
                case 24:
                    Require(Get<ShooterCustomizeDraft>("_draft").attachments.Last().localTransform.localPosition.z == -.5f, "Adding a definition copies its calibrated placement");
                    Require(Controls.ContainsKey("accessory:save-default") && Controls.ContainsKey("accessory:restore-default"), "Global default save and per-character restore are separate controls");
                    var changed = Get<ShooterCustomizeDraft>("_draft").attachments[0]; changed.localTransform.localPosition = Vector3.forward * .2f;
                    Get<ShooterCustomizeDraft>("_draft").attachments[0] = changed; Set("_applyNeeded", true); break;
                case 25:
                    Require(_uiDefinition.LocalTransform.localPosition.z == -.5f, "Per-character fitting keeps the reusable default unchanged");
                    Click("accessory:restore-default"); break;
                case 26:
                    Require(Get<ShooterCustomizeDraft>("_draft").attachments[0].localTransform.localPosition.z == -.5f, "Restore default affects only the test window's current attachment");
                    Get<ShooterCustomizeDraft>("_draft").LoadBackpack(FindItem<BackpackItemSO>(b => b.EquippedVisualPrefab != null));
                    _backAnchorLookDirtyBefore = Get<ShooterCustomizeDraft>("_draft").lookDirty;
                    Set("_equipmentMode", true); Set("_editScroll", Vector2.zero); Set("_applyNeeded", true); break;
                case 27:
                    Require(Controls.ContainsKey("gear:backpack-anchor"), "Back anchor editor has a direct equipment-panel shortcut");
                    Click("gear:backpack-anchor"); break;
                case 28:
                    Require(!Get<bool>("_equipmentMode") && Get<int>("_part") == 7, "Shortcut selects the Back part and keeps the backpack preview");
                    Require(Get<ShooterCustomizePreview>("_preview").BackpackVisual != null && Controls.ContainsKey("anchor:position"), "Back anchor is editable while the backpack remains worn");
                    Require(Get<ShooterCustomizeDraft>("_draft").lookDirty == _backAnchorLookDirtyBefore && !Get<ShooterCustomizeDraft>("_draft").backpackDirty, "Opening the Back anchor does not edit or save placements");
                    Set("_equipmentMode", true); Set("_editScroll", new Vector2(0, 380)); break;
                case 29:
                    Require(Controls.ContainsKey("gear:backpack-fit"), "Backpack has an explicit fit-behind-body control");
                    // Invoke the same action on the isolated draft; no actual Item SO save.
                    typeof(ShooterCustomizeStationWindow).GetMethod("FitBackpackBehindBody", Flags).Invoke(_window, null); break;
                case 30:
                    Require(Get<ShooterCustomizeDraft>("_draft").backpackDirty && Get<ShooterCustomizeDraft>("_draft").lookDirty == _backAnchorLookDirtyBefore, "Backpack fitting marks only the Item SO draft, not character anchors");
                    Finish(); break;
            }
        }
        catch (Exception ex) { Fail("UI: " + ex); Finish(); }
    }

    private static T Get<T>(string name) => (T)typeof(ShooterCustomizeStationWindow).GetField(name, Flags).GetValue(_window);
    private static void Set(string name, object value) => typeof(ShooterCustomizeStationWindow).GetField(name, Flags).SetValue(_window, value);
    private static T FindItem<T>(Func<T, bool> predicate) where T : UnityEngine.Object => AssetDatabase.FindAssets("t:" + typeof(T).Name)
        .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<T>).First(item => item != null && predicate(item));
    private static void Click(string key)
    {
        if (!Controls.TryGetValue(key, out var rect)) throw new Exception("Missing visible control: " + key);
        Send(EventType.MouseDown, rect.center); Send(EventType.MouseUp, rect.center);
    }
    private static void Send(EventType type, Vector2 point, Vector2 delta = default, int button = 0, bool alt = false) =>
        _window.SendEvent(new Event { type = type, button = button, mousePosition = point, delta = delta, alt = alt });
    private static void Key(EventType type, KeyCode key) => _window.SendEvent(new Event { type = type, keyCode = key, mousePosition = Controls["viewport"].center });
    private static void DragViewport(int button, bool alt = false)
    {
        Vector2 point = Controls["viewport"].center, delta = new(20, 8);
        Send(EventType.MouseDown, point, button: button, alt: alt);
        Send(EventType.MouseDrag, point + delta, delta, button, alt);
        Send(EventType.MouseUp, point + delta, button: button, alt: alt);
    }
    private static void Require(bool condition, string label) { if (!condition) throw new Exception(label); Pass(label); }
    private static void Pass(string label) { Lines.Add("PASS " + label); _passed++; }
    private static void Fail(string label) { Lines.Add("FAIL " + label); _failed++; }
    private static void SavePreviewImage(Texture source)
    {
        int width = source.width, height = source.height;
        var buffer = RenderTexture.GetTemporary(width, height, 0);
        var previous = RenderTexture.active;
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            Graphics.Blit(source, buffer); RenderTexture.active = buffer;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(Report));
            File.WriteAllBytes("Library/ShooterCustomizeVerification/backpack-four-gear-side.png", image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous; RenderTexture.ReleaseTemporary(buffer);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }
    private static void CheckGuard(Func<bool> condition, string label)
    {
        try { Require(condition(), label); }
        catch (Exception ex) { Fail("Read-only guard: " + label + "\n" + ex); }
    }
    private static void Finish()
    {
        _operation = "UI cleanup";
        EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
        if (_window != null) { _window.DiscardChanges(); _window.Close(); _window = null; }
        if (_uiDefinition != null) UnityEngine.Object.DestroyImmediate(_uiDefinition); _uiDefinition = null;
        if (_previous != null) _previous.Focus();
        EditorSceneManager.sceneDirtied -= OnSceneDirtied;
        // Report each preservation check independently; a dirty flag mismatch must
        // not hide whether any real object data changed. Never save/clear the scene.
        CheckGuard(() => _before.All(p => File.ReadAllText(p.Key) == p.Value), "Real character/item/accessory files unchanged");
        CheckGuard(() => SceneManager.GetActiveScene() == _active && _active.isDirty == _dirty && _active.rootCount == _roots,
            $"User scene unchanged (dirty {_dirty}->{_active.isDirty}, roots {_roots}->{_active.rootCount})");
        CheckGuard(() =>
        {
            var sceneAfter = SceneSnapshot(_active);
            return _sceneObjects.Count == sceneAfter.Count && _sceneObjects.All(p => sceneAfter.TryGetValue(p.Key, out var value) && value == p.Value);
        }, "All user scene objects and component fields unchanged");
        CheckGuard(() => _sceneDirtyEvents == 0, "No user scene dirty events during verification (" + _sceneDirtyEvents + ")");
        Lines.Add($"{_passed} passed / {_failed} failed / {_skipped} skipped (global Undo)");
        Directory.CreateDirectory(Path.GetDirectoryName(Report)); File.WriteAllLines(Report, Lines);
        Debug.Log("[Shooter Customize verification] " + Lines.Last() + " " + Report);
        var callback = Completed; Completed = null;
        callback?.Invoke(_failed == 0);
    }
}
