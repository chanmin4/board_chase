using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class SectorMakerWorkspaceTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private string _folder;
    private GameObject _prefab;
    private SectorPresetSO _preset;
    private SectorMakerWindow _window;
    private Object _selection;
    private bool _mainDirty;
    private string _catalogBefore;
    private SceneView _sceneView;
    private Vector3 _pivot;
    private Quaternion _rotation;
    private float _size;
    private bool _orthographic;
    internal static object Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Flags).Invoke(obj, args);
    internal static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Flags).SetValue(obj, value);
    internal static T Get<T>(object obj, string field) => (T)obj.GetType().GetField(field, Flags).GetValue(obj);
    private bool Editing => (bool)typeof(SectorMakerWindow).GetProperty("IsEditingRoom", Flags).GetValue(_window);

    [SetUp] public void SetUp()
    {
        Assert.That(PrefabStageUtility.GetCurrentPrefabStage(), Is.Null, "Run outside the user's Prefab Mode.");
        _selection = Selection.activeObject;
        _sceneView = SceneView.lastActiveSceneView;
        if (_sceneView != null) { _pivot = _sceneView.pivot; _rotation = _sceneView.rotation; _size = _sceneView.size; _orthographic = _sceneView.orthographic; }
        _mainDirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;
        _catalogBefore = File.Exists("ProjectSettings/SectorMakerCatalog.asset") ? File.ReadAllText("ProjectSettings/SectorMakerCatalog.asset") : null;
        _folder = "Assets/__SectorMakerTest_" + Guid.NewGuid().ToString("N");
        AssetDatabase.CreateFolder("Assets", Path.GetFileName(_folder));
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(SectorMakerCatalog.Root + "Bases/NormalSectorBase.prefab");
        _prefab = SectorMakerCatalog.CreateVariant(source, _folder + "/TestRoom.prefab");
        _preset = SectorMakerCatalog.CreatePreset(_prefab, _folder + "/Preset.asset", StageRoomType.Normal);
        _window = ScriptableObject.CreateInstance<SectorMakerWindow>();
        Call(_window, "SelectRoom", _prefab, false, _preset);
    }
    [TearDown] public void TearDown()
    {
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && _folder != null && stage.assetPath.StartsWith(_folder + "/"))
        { stage.ClearDirtiness(); StageUtility.GoToMainStage(); }
        if (_window != null) Object.DestroyImmediate(_window);
        Selection.activeObject = _selection;
        if (_sceneView != null) { _sceneView.pivot = _pivot; _sceneView.rotation = _rotation; _sceneView.size = _size; _sceneView.orthographic = _orthographic; _sceneView.Repaint(); }
        if (_folder != null && AssetDatabase.IsValidFolder(_folder)) AssetDatabase.DeleteAsset(_folder);
        Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty, Is.EqualTo(_mainDirty), "Working scene dirtiness unchanged");
        Assert.That(File.Exists("ProjectSettings/SectorMakerCatalog.asset") ? File.ReadAllText("ProjectSettings/SectorMakerCatalog.asset") : null, Is.EqualTo(_catalogBefore), "Tests must not register temporary rooms in project settings");
    }
    [Test] public void BrowsingIsReadOnlyAndDoesNotEnablePlacement()
    {
        string before = File.ReadAllText(AssetDatabase.GetAssetPath(_prefab));
        Call(_window, "RefreshCatalog"); Call(_window, "RunRoomReadinessCheck");
        Assert.That(File.ReadAllText(AssetDatabase.GetAssetPath(_prefab)), Is.EqualTo(before));
        Assert.That(Editing, Is.False); Assert.That(Get<bool>(_window, "_paintingEnabled"), Is.False);
        Assert.That(Get<SectorPresetSO>(_window, "_activeSectorPreset"), Is.EqualTo(_preset));
    }
    [Test] public void OpeningAutoBindsRoomAndChangingSelectionStopsPainting()
    {
        Call(_window, "OpenRoomForEditing"); Assert.That(Editing, Is.True);
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        Assert.That(Get<SectorRuntime>(_window, "_activeSectorRuntime"), Is.EqualTo(stage.prefabContentsRoot.GetComponent<SectorRuntime>()));
        Assert.That(Get<Transform>(_window, "_sectorObjectParent").IsChildOf(stage.prefabContentsRoot.transform), Is.True);
        Set(_window, "_paintingEnabled", true);
        var other = SectorMakerCatalog.CreateVariant(_prefab, _folder + "/Other.prefab");
        var otherPreset = SectorMakerCatalog.CreatePreset(other, _folder + "/Other.asset", StageRoomType.Normal);
        Call(_window, "RefreshCatalog"); Call(_window, "SelectRoom", other, false, otherPreset);
        Assert.That(Editing, Is.False); Assert.That(Get<bool>(_window, "_paintingEnabled"), Is.False);
        Assert.That(Get<Transform>(_window, "_sectorObjectParent"), Is.Null);
        Assert.That(PrefabStageUtility.GetCurrentPrefabStage(), Is.SameAs(stage), "Browse selection doesn't switch/close current stage");
    }
    [Test] public void SaveTargetsOnlyOpenRoomAndPreservesVariantParent()
    {
        var parent = PrefabUtility.GetCorrespondingObjectFromSource(_prefab);
        string presetBefore = File.ReadAllText(AssetDatabase.GetAssetPath(_preset));
        Call(_window, "OpenRoomForEditing");
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        var child = new GameObject("VerificationMarker"); child.transform.SetParent(stage.prefabContentsRoot.transform, false);
        EditorSceneManager.MarkSceneDirty(stage.scene);
        Call(_window, "SaveEditingRoom");
        Assert.That(stage.scene.isDirty, Is.False);
        var saved = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(_prefab));
        Assert.That(saved.transform.Find("VerificationMarker"), Is.Not.Null);
        Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(saved), Is.EqualTo(parent));
        Assert.That(File.ReadAllText(AssetDatabase.GetAssetPath(_preset)), Is.EqualTo(presetBefore));
    }
    [Test] public void ClosingPrefabDisarmsAndClearsParents()
    {
        Call(_window, "OpenRoomForEditing"); Set(_window, "_paintingEnabled", true);
        StageUtility.GoToMainStage(); Call(_window, "SyncRoomStage");
        Assert.That(Editing, Is.False); Assert.That(Get<bool>(_window, "_paintingEnabled"), Is.False);
        Assert.That(Get<Transform>(_window, "_enemySpawnPointParent"), Is.Null);
    }
    [Test] public void SceneObjectCannotBeAcceptedAsPrefabInput()
    {
        Assert.That(SectorMakerCatalog.TryPrefab(_prefab, out _), Is.True);
        Assert.That(SectorMakerCatalog.TryPrefab(_preset, out var resolved), Is.True); Assert.That(resolved, Is.EqualTo(_prefab));
        Call(_window, "OpenRoomForEditing");
        Assert.That(SectorMakerCatalog.TryPrefab(PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot, out _), Is.False);
    }
    [Test] public void MultiplePresetsNeverSilentlyChooseOne()
    {
        SectorMakerCatalog.CreatePreset(_prefab, _folder + "/Second.asset", StageRoomType.Normal);
        Call(_window, "RefreshCatalog"); Call(_window, "SelectRoom", _prefab, false, null);
        Assert.That(Get<SectorPresetSO>(_window, "_activeSectorPreset"), Is.Null);
        Assert.That((bool)Call(_window, "CanEditContentPlan"), Is.False);
    }
    [Test] public void CategoryInferenceKeepsMedicalVaultAndBossSeparate()
    {
        Assert.That(SectorMakerCatalog.Infer("Assets/Rooms/Medical/Room.prefab"), Is.EqualTo(SectorMakerCategory.Medical));
        Assert.That(SectorMakerCatalog.Infer("Assets/Bases/TreasureSectorBase.prefab"), Is.EqualTo(SectorMakerCategory.Vault));
        Assert.That(SectorMakerCatalog.Infer("Assets/Rooms/Boss/Boss_01.prefab"), Is.EqualTo(SectorMakerCategory.Boss));
        Assert.That(SectorMakerCatalog.Infer("Assets/HomeSector.prefab"), Is.EqualTo(SectorMakerCategory.Home));
    }
    [Test] public void CatalogDeduplicatesSharedPrefabAndFindsRealStageLinks()
    {
        var rooms = SectorMakerCatalog.Scan();
        Assert.That(rooms.Select(r => r.guid).Distinct().Count(), Is.EqualTo(rooms.Count));
        Assert.That(rooms.Single(r => r.path == SectorMakerCatalog.Root + "Rooms/Medical/Medical_01.prefab").stages.Count, Is.GreaterThan(0));
        Assert.That(rooms.Single(r => r.prefab == _prefab).presets, Has.Count.EqualTo(1));
    }
    [Test] public void VariantCreationDoesNotOverwriteSourceOrTarget()
    {
        string before = File.ReadAllText(AssetDatabase.GetAssetPath(_prefab));
        var variant = SectorMakerCatalog.CreateVariant(_prefab, _folder + "/Variant.prefab");
        Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(variant), Is.EqualTo(_prefab));
        Assert.Throws<ArgumentException>(() => SectorMakerCatalog.CreateVariant(_prefab, AssetDatabase.GetAssetPath(_prefab)));
        Assert.That(File.ReadAllText(AssetDatabase.GetAssetPath(_prefab)), Is.EqualTo(before));
    }
    [Test] public void NewPresetHasExplicitRuntimeTypeAndNoSharedContent()
    {
        var preset = SectorMakerCatalog.CreatePreset(_prefab, _folder + "/Boss.asset", StageRoomType.Boss);
        Assert.That(preset, Is.TypeOf<SectorBossBattlePresetSO>());
        Assert.That(preset.RoomType, Is.EqualTo(StageRoomType.Boss)); Assert.That(preset.ContentProfile, Is.Null);
        Assert.Throws<ArgumentException>(() => SectorMakerCatalog.CreatePreset(_prefab, _folder + "/Preset.asset", StageRoomType.Normal));
        Assert.Throws<ArgumentException>(() => SectorMakerCatalog.CreatePreset(_prefab, _folder + "/Empty.asset", StageRoomType.Empty));
    }
    [Test] public void ContentNamesAdvanceWithoutCollisions()
    {
        Call(_window, "OpenRoomForEditing");
        var parent = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot.transform;
        foreach (string id in new[] { "loot_01", "loot_02", "loot_04" })
        { var go = new GameObject(id); go.transform.SetParent(parent, false); go.AddComponent<SectorContentSlotAnchor>().ConfigureForAuthoring(id, SectorContentSlotKind.LootContainer); }
        Assert.That(SectorMakerWindow.NextSlotId(parent, "loot_01"), Is.EqualTo("loot_03"));
        Assert.That(SectorMakerWindow.NextSlotId(parent, " chest "), Is.EqualTo("chest"));
    }
    [Test] public void ContentEditingRequiresUnambiguousIndependentProfile()
    {
        var profile = ScriptableObject.CreateInstance<SectorContentProfileSO>(); AssetDatabase.CreateAsset(profile, _folder + "/Content.asset");
        _preset.SetContentProfile(profile); Call(_window, "SetActiveSectorPreset", _preset);
        Assert.That((bool)Call(_window, "CanEditContentPlan"), Is.True);
        var second = SectorMakerCatalog.CreatePreset(_prefab, _folder + "/Second.asset", StageRoomType.Normal); second.SetContentProfile(profile);
        Call(_window, "RefreshCatalog");
        Assert.That((bool)Call(_window, "CanEditContentPlan"), Is.False);
    }
    [Test] public void PlacementOutsideOpenedRoomIsRejected()
    {
        object[] args = { null }; Assert.That((bool)Call(_window, "TryValidateConfiguration", args), Is.False);
        Call(_window, "OpenRoomForEditing");
        Set(_window, "_structurePrefab", _prefab);
        // The prefab asset itself can never be a placement parent, even while its stage is open.
        Set(_window, "_sectorObjectParent", _prefab.transform);
        Assert.That((bool)Call(_window, "TryValidateConfiguration", args), Is.False);
    }

    // Compact tests only change the window and, where needed, the generated test room.
    // They never place objects or invoke global Undo in the user's working scene.
    [Test] public void CompactTogglePreservesRoomAndPlacementOptions()
    {
        Set(_window, "_structurePrefab", _prefab); Set(_window, "_rotationY", 75f);
        Set(_window, "_structureBottomOffset", .42f); Set(_window, "_catalogScroll", new Vector2(0, 135));
        Call(_window, "SetCompactMode", true); Call(_window, "SetCompactMode", false);
        Assert.That(Get<GameObject>(_window, "_roomPrefab"), Is.EqualTo(_prefab));
        Assert.That(Get<SectorPresetSO>(_window, "_activeSectorPreset"), Is.EqualTo(_preset));
        Assert.That(Get<GameObject>(_window, "_structurePrefab"), Is.EqualTo(_prefab));
        Assert.That(Get<float>(_window, "_rotationY"), Is.EqualTo(75f));
        Assert.That(Get<float>(_window, "_structureBottomOffset"), Is.EqualTo(.42f));
        Assert.That(Get<Vector2>(_window, "_catalogScroll"), Is.EqualTo(new Vector2(0, 135)));
    }
    [Test] public void CompactRestoresOriginalWindowRectAndStopsPainting()
    {
        _window.position = new Rect(80, 95, 1100, 700); Rect original = _window.position;
        Call(_window, "SetCompactMode", true);
        Assert.That(_window.minSize, Is.EqualTo(new Vector2(330, 280)));
        Assert.That(_window.position.width, Is.LessThan(450));
        Set(_window, "_paintingEnabled", true); Call(_window, "SetCompactMode", false);
        Assert.That(_window.position, Is.EqualTo(original));
        Assert.That(_window.minSize, Is.EqualTo(new Vector2(980, 620)));
        Assert.That(Get<bool>(_window, "_paintingEnabled"), Is.False);
    }
    [Test] public void CompactRemembersUserResizedSmallWindow()
    {
        _window.position = new Rect(80, 95, 1100, 700);
        Call(_window, "SetCompactMode", true); _window.position = new Rect(200, 180, 405, 365);
        Call(_window, "SetCompactMode", false); Call(_window, "SetCompactMode", true);
        Assert.That(_window.position.size, Is.EqualTo(new Vector2(405, 365)));
        Assert.That(_window.position.position, Is.EqualTo(new Vector2(80, 95)));
    }
    [Test] public void CompactDeferredRequestsUseLatestModeAndDoNotChangeLayoutImmediately()
    {
        Call(_window, "RequestCompactMode", true);
        Assert.That(Get<bool>(_window, "_compactMode"), Is.False, "OnGUI must finish before resizing");
        Call(_window, "RequestCompactMode", false); Call(_window, "ApplyRequestedWorkspaceMode");
        Assert.That(Get<bool>(_window, "_compactMode"), Is.False);
        Call(_window, "RequestCompactMode", true); Call(_window, "ApplyRequestedWorkspaceMode");
        Assert.That(Get<bool>(_window, "_compactMode"), Is.True);
    }
    [Test] public void CompactOpeningAndClosingOwnPrefabRestoresModeWithoutSaving()
    {
        string before = File.ReadAllText(AssetDatabase.GetAssetPath(_prefab));
        Call(_window, "OpenRoomForEditing"); Call(_window, "ApplyRequestedWorkspaceMode");
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        Assert.That(Editing, Is.True); Assert.That(Get<bool>(_window, "_compactMode"), Is.True);
        Set(_window, "_paintingEnabled", true);
        StageUtility.GoToMainStage(); Call(_window, "ApplyRequestedWorkspaceMode");
        Assert.That(Get<bool>(_window, "_compactMode"), Is.False);
        Assert.That(Get<bool>(_window, "_paintingEnabled"), Is.False);
        Assert.That(Get<Transform>(_window, "_sectorObjectParent"), Is.Null);
        Assert.That(File.ReadAllText(AssetDatabase.GetAssetPath(_prefab)), Is.EqualTo(before));
    }
    [Test] public void CompactSizeAndExpandedRectSurviveWindowSerialization()
    {
        _window.position = new Rect(80, 95, 1100, 700); Call(_window, "SetCompactMode", true);
        Set(_window, "_compactWindowSize", new Vector2(405, 365));
        var restored = ScriptableObject.CreateInstance<SectorMakerWindow>();
        try
        {
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(_window), restored);
            Call(restored, "ApplyWorkspaceSizeLimits");
            Assert.That(Get<bool>(restored, "_compactMode"), Is.True);
            Assert.That(Get<Rect>(restored, "_expandedWindowRect"), Is.EqualTo(Get<Rect>(_window, "_expandedWindowRect")));
            Assert.That(Get<Vector2>(restored, "_compactWindowSize"), Is.EqualTo(new Vector2(405, 365)));
            Assert.That(restored.minSize, Is.EqualTo(new Vector2(330, 280)));
        }
        finally { Object.DestroyImmediate(restored); }
    }
}
