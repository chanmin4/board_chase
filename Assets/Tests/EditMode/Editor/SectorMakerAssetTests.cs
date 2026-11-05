using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SectorMakerAssetTests
{
    private SectorMakerWorkspaceTests _fixture;
    private string Folder => SectorMakerWorkspaceTests.Get<string>(_fixture, "_folder");
    private GameObject Prefab => SectorMakerWorkspaceTests.Get<GameObject>(_fixture, "_prefab");
    private SectorPresetSO Preset => SectorMakerWorkspaceTests.Get<SectorPresetSO>(_fixture, "_preset");
    [SetUp] public void Setup() { _fixture = new SectorMakerWorkspaceTests(); _fixture.SetUp(); }
    [TearDown] public void Cleanup() { _fixture?.TearDown(); }

    [Test] public void NewRoomCreatesVariantAndIndependentPresetContentWithoutChangingSource()
    {
        var content = ScriptableObject.CreateInstance<SectorContentProfileSO>(); AssetDatabase.CreateAsset(content, Folder + "/SourceContent.asset");
        Preset.SetContentProfile(content); EditorUtility.SetDirty(Preset); AssetDatabase.SaveAssetIfDirty(Preset);
        string prefabBefore = File.ReadAllText(AssetDatabase.GetAssetPath(Prefab));
        string presetBefore = File.ReadAllText(AssetDatabase.GetAssetPath(Preset));
        var created = SectorMakerAssets.CreateRoom(Prefab, Preset, Folder + "/New.prefab", Folder + "/New.asset", StageRoomType.Normal);
        Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(created.SectorPrefab));
        Assert.AreSame(Prefab, PrefabUtility.GetCorrespondingObjectFromSource(created.SectorPrefab.gameObject));
        Assert.AreNotSame(Preset, created); Assert.AreNotSame(content, created.ContentProfile);
        Assert.AreEqual(AssetDatabase.GetAssetPath(created.SectorPrefab), Folder + "/New.prefab");
        Assert.IsInstanceOf<SectorNormalBattlePresetSO>(created);
        Assert.AreEqual(prefabBefore, File.ReadAllText(AssetDatabase.GetAssetPath(Prefab)));
        Assert.AreEqual(presetBefore, File.ReadAllText(AssetDatabase.GetAssetPath(Preset)));
    }
    [Test] public void UnsafeOrOccupiedDestinationsNeverLeaveHalfCreatedRoom()
    {
        Assert.Throws<ArgumentException>(() => SectorMakerAssets.CreateRoom(Prefab, Preset, Folder + "/Failed.prefab", AssetDatabase.GetAssetPath(Preset), StageRoomType.Normal));
        Assert.IsFalse(File.Exists(Folder + "/Failed.prefab"));
        Assert.Throws<ArgumentException>(() => SectorMakerAssets.RequireNewPath("Assets/../Library/Fake.asset", ".asset"));
        Assert.Throws<ArgumentException>(() => SectorMakerAssets.CreateRoom(Prefab, null, Folder + "/Failed.prefab", Folder + "/Failed.asset", StageRoomType.Empty));
        Assert.IsFalse(File.Exists(Folder + "/Failed.prefab")); Assert.IsFalse(File.Exists(Folder + "/Failed.asset"));
    }
    [Test] public void CopyPresetOnSameRoomDoesNotShareContentOrChangeStageConnections()
    {
        var copy = SectorMakerAssets.CopyPreset(Preset, Prefab, Folder + "/Copied.asset");
        Assert.AreSame(Preset.SectorPrefab, copy.SectorPrefab); Assert.AreNotSame(Preset, copy);
        Assert.IsNull(copy.ContentProfile); Assert.AreEqual(Preset.RoomType, copy.RoomType);
        var rooms = SectorMakerCatalog.Scan(); Assert.AreEqual(2, rooms.Single(r => r.prefab == Prefab).presets.Count);
    }
    [Test] public void UsedPresetCannotBeDeletedIncludingUnsavedStageAssignment()
    {
        var original = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>("Assets/ScriptableObject/GameScene/Stage/Stage1Definition.asset");
        Assert.IsNotNull(original);
        var stage = UnityEngine.Object.Instantiate(original); AssetDatabase.CreateAsset(stage, Folder + "/Stage.asset");
        var data = new SerializedObject(stage);
        var list = data.FindProperty("_normalBattle").FindPropertyRelative("sectorPresets"); list.arraySize = 1;
        list.GetArrayElementAtIndex(0).FindPropertyRelative("preset").objectReferenceValue = Preset;
        data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(stage);
        Assert.That(SectorMakerAssets.ReferencesTo(Preset), Does.Contain(Folder + "/Stage.asset"));
        Assert.Throws<InvalidOperationException>(() => SectorMakerAssets.TrashPreset(Preset));
        Assert.IsTrue(File.Exists(AssetDatabase.GetAssetPath(Preset)));
        AssetDatabase.SaveAssetIfDirty(stage);
        Assert.That(SectorMakerAssets.ReferencesTo(Preset), Does.Contain(Folder + "/Stage.asset"), "Saved dependencies also block deletion");
    }
    [Test] public void UnusedPresetTrashDoesNotDeletePrefabOrContent()
    {
        var profile = ScriptableObject.CreateInstance<SectorContentProfileSO>(); AssetDatabase.CreateAsset(profile, Folder + "/KeepContent.asset");
        var copy = SectorMakerAssets.CopyPreset(Preset, Prefab, Folder + "/TrashOnlyThis.asset");
        copy.SetContentProfile(profile); EditorUtility.SetDirty(copy); AssetDatabase.SaveAssetIfDirty(copy);
        Assert.IsTrue(SectorMakerAssets.TrashPreset(copy));
        Assert.IsFalse(File.Exists(Folder + "/TrashOnlyThis.asset"));
        Assert.IsTrue(File.Exists(AssetDatabase.GetAssetPath(Prefab))); Assert.IsTrue(File.Exists(Folder + "/KeepContent.asset"));
    }
    [Test] public void AllBaseSizesAreDiscoverableAndHaveDedicatedTemplatePresets()
    {
        var rooms = SectorMakerCatalog.Scan();
        foreach (string name in new[] { "NormalSectorBase", "NormalSectorBase_HalfSize", "NormalSectorBase_QuarterSize" })
        {
            var room = rooms.Single(r => r.prefab.name == name && r.isBase);
            Assert.IsTrue(room.presets.Any(SectorMakerAssets.IsTemplate), name);
        }
        Assert.AreEqual(11, rooms.Count(r => r.isBase));
    }
    [Test] public void DeletedObjectAndDestroyedRuntimeReferencesAreRejectedWithoutThrowing()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var memoryPreset = ScriptableObject.CreateInstance<SectorPresetSO>();
        try
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(Prefab, scene);
            typeof(SectorPresetSO).GetField("_sectorPrefab", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(memoryPreset, instance.GetComponent<SectorRuntime>());
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            Assert.IsFalse(SectorMakerCatalog.TryPrefab(memoryPreset, out _));
            UnityEngine.Object.DestroyImmediate(memoryPreset);
            Assert.IsFalse(SectorMakerCatalog.TryPrefab(memoryPreset, out _));
        }
        finally
        {
            if (scene.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            if (memoryPreset != null) UnityEngine.Object.DestroyImmediate(memoryPreset);
        }
    }
    [Test] public void SelectingDifferentOrOrphanPresetAlwaysReflectsActualAsset()
    {
        var window = SectorMakerWorkspaceTests.Get<SectorMakerWindow>(_fixture, "_window");
        var other = SectorMakerCatalog.CreatePreset(Prefab, Folder + "/Other.asset", StageRoomType.Normal);
        SectorMakerWorkspaceTests.Call(window, "RefreshCatalog"); SectorMakerWorkspaceTests.Call(window, "SelectPreset", other, false);
        Assert.AreSame(other, SectorMakerWorkspaceTests.Get<SectorPresetSO>(window, "_activeSectorPreset"));
        var data = new SerializedObject(other); data.FindProperty("_sectorPrefab").objectReferenceValue = null; data.ApplyModifiedPropertiesWithoutUndo();
        SectorMakerWorkspaceTests.Call(window, "SelectPreset", other, false);
        Assert.IsNull(SectorMakerWorkspaceTests.Get<GameObject>(window, "_roomPrefab"));
        Assert.AreSame(other, SectorMakerWorkspaceTests.Get<SectorPresetSO>(window, "_activeSectorPreset"));
    }
    private SectorMakerWindow Window => SectorMakerWorkspaceTests.Get<SectorMakerWindow>(_fixture, "_window");

    [Test] public void PrefabRenamePreservesGuidMetaVariantAncestryAndStageReferences()
    {
        string originalPath = AssetDatabase.GetAssetPath(Prefab);
        string guid = AssetDatabase.AssetPathToGUID(originalPath);
        string meta = File.ReadAllText(originalPath + ".meta");
        var parent = PrefabUtility.GetCorrespondingObjectFromSource(Prefab);
        var child = SectorMakerCatalog.CreateVariant(Prefab, Folder + "/Child.prefab");
        string childBefore = File.ReadAllText(AssetDatabase.GetAssetPath(child));
        var stage = NewStageUsing(Preset, true);
        string stageBefore = File.ReadAllText(AssetDatabase.GetAssetPath(stage));
        string presetBefore = File.ReadAllText(AssetDatabase.GetAssetPath(Preset));
        string newPath = SectorMakerAssets.RenameRoomPrefab(Prefab, "임시방_테스트.prefab");
        Assert.AreEqual(Folder + "/임시방_테스트.prefab", newPath);
        Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(newPath));
        Assert.AreEqual(meta, File.ReadAllText(newPath + ".meta")); Assert.IsFalse(File.Exists(originalPath));
        var renamed = AssetDatabase.LoadAssetAtPath<GameObject>(newPath);
        Assert.AreSame(parent, PrefabUtility.GetCorrespondingObjectFromSource(renamed));
        Assert.AreSame(renamed, PrefabUtility.GetCorrespondingObjectFromSource(child));
        Assert.AreEqual(newPath, AssetDatabase.GetAssetPath(Preset.SectorPrefab));
        Assert.AreEqual(presetBefore, File.ReadAllText(AssetDatabase.GetAssetPath(Preset)));
        Assert.AreEqual(stageBefore, File.ReadAllText(AssetDatabase.GetAssetPath(stage)));
        Assert.AreEqual(childBefore, File.ReadAllText(AssetDatabase.GetAssetPath(child)));
        Assert.AreEqual(StageRoomType.Normal, Preset.RoomType, "A file rename is not a role migration.");
    }

    [Test] public void PrefabRenameRejectsDuplicatesAndInvalidNamesWithoutChangingAnything()
    {
        string path = AssetDatabase.GetAssetPath(Prefab), before = File.ReadAllText(path);
        string guid = AssetDatabase.AssetPathToGUID(path);
        var other = SectorMakerCatalog.CreateVariant(Prefab, Folder + "/Taken.prefab");
        string otherBefore = File.ReadAllText(AssetDatabase.GetAssetPath(other));
        Assert.Throws<InvalidOperationException>(() => SectorMakerAssets.RenameRoomPrefab(Prefab, "Taken"));
        foreach (string name in new[] { "", " ", ".", "..", "../Escape", "Folder\\Escape", "End.", "CON", "NUL.txt", "Temp .prefab" })
            Assert.Throws<ArgumentException>(() => SectorMakerAssets.RenameRoomPrefab(Prefab, name), name);
        Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(path));
        Assert.AreEqual(before, File.ReadAllText(path));
        Assert.AreEqual(otherBefore, File.ReadAllText(AssetDatabase.GetAssetPath(other)));
        Assert.AreEqual(path, SectorMakerAssets.RenameRoomPrefab(Prefab, Prefab.name));
    }

    [Test] public void PrefabRenameIsBlockedWhilePrefabModeIsOpen()
    {
        string path = AssetDatabase.GetAssetPath(Prefab), before = File.ReadAllText(path);
        SectorMakerWorkspaceTests.Call(Window, "OpenRoomForEditing");
        var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        var marker = new GameObject("Unsaved rename guard"); marker.transform.SetParent(stage.prefabContentsRoot.transform);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(stage.scene);
        Assert.Throws<InvalidOperationException>(() => SectorMakerAssets.RenameRoomPrefab(Prefab, "Changed"));
        Assert.IsTrue(stage.scene.isDirty); Assert.AreEqual(path, stage.assetPath);
        Assert.AreEqual(before, File.ReadAllText(path)); Assert.IsFalse(File.Exists(Folder + "/Changed.prefab"));
    }

    [Test] public void WindowRenameUpdatesSelectedPrefabWithoutChangingSelectedPreset()
    {
        SectorMakerWorkspaceTests.Set(Window, "_prefabNameDraft", "Room_Renamed");
        SectorMakerWorkspaceTests.Call(Window, "RenameRoomPrefab");
        var selected = SectorMakerWorkspaceTests.Get<GameObject>(Window, "_roomPrefab");
        Assert.AreEqual(Folder + "/Room_Renamed.prefab", AssetDatabase.GetAssetPath(selected));
        Assert.AreSame(Preset, SectorMakerWorkspaceTests.Get<SectorPresetSO>(Window, "_activeSectorPreset"));
        Assert.AreEqual("Room_Renamed", SectorMakerWorkspaceTests.Get<string>(Window, "_prefabNameDraft"));
        var other = SectorMakerCatalog.CreateVariant(selected, Folder + "/OtherRoom.prefab");
        SectorMakerWorkspaceTests.Call(Window, "RefreshCatalog");
        SectorMakerWorkspaceTests.Call(Window, "SelectRoom", other, false, null);
        var state = SectorMakerWorkspaceTests.Get<MakerSelectionUiState>(Window, "_prefabNameUi") ?? new MakerSelectionUiState();
        Assert.AreEqual("OtherRoom", state.NameFor(other, "Previously typed name"));
    }

    [Test] public void TemporaryRoomLabelKeepsTheOldSerializedCategoryIndex()
    {
        Assert.AreEqual(8, (int)SectorMakerCategory.Start);
        Assert.AreEqual("임시방", SectorMakerCatalog.Labels[(int)SectorMakerCategory.Start]);
        Assert.AreEqual(SectorMakerCategory.Start, SectorMakerCatalog.Infer("Assets/Rooms/Start/StartSector.prefab"));
        Assert.AreEqual("해치", SectorMakerCatalog.Labels[(int)SectorMakerCategory.Hatch]);
    }
    private SectorPresetSO[] Selected => SectorMakerWorkspaceTests.Get<System.Collections.Generic.List<SectorPresetSO>>(Window, "_selectedPresets").ToArray();
    private SectorPresetSO NewPreset(string name) => SectorMakerCatalog.CreatePreset(Prefab, Folder + "/" + name + ".asset", StageRoomType.Normal);
    private StageDefinitionSO NewStageUsing(SectorPresetSO preset, bool save)
    {
        var original = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>("Assets/ScriptableObject/GameScene/Stage/Stage1Definition.asset");
        var stage = UnityEngine.Object.Instantiate(original); AssetDatabase.CreateAsset(stage, Folder + "/BulkStage.asset");
        var data = new SerializedObject(stage);
        var list = data.FindProperty("_normalBattle").FindPropertyRelative("sectorPresets"); list.arraySize = 1;
        list.GetArrayElementAtIndex(0).FindPropertyRelative("preset").objectReferenceValue = preset;
        data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(stage);
        if (save) AssetDatabase.SaveAssetIfDirty(stage);
        return stage;
    }
    [Test] public void CtrlClickAddsAndTogglesWhileNormalClickReplacesSelection()
    {
        var second = NewPreset("Second"); var third = NewPreset("Third");
        var visible = new[] { Preset, second, third };
        SectorMakerWorkspaceTests.Call(Window, "ClickPreset", Preset, false, false, visible);
        SectorMakerWorkspaceTests.Call(Window, "ClickPreset", second, true, false, visible);
        Assert.That(Selected, Is.EquivalentTo(new[] { Preset, second }));
        Assert.AreSame(second, SectorMakerWorkspaceTests.Get<SectorPresetSO>(Window, "_activeSectorPreset"));
        SectorMakerWorkspaceTests.Call(Window, "ClickPreset", second, true, false, visible);
        Assert.That(Selected, Is.EqualTo(new[] { Preset }));
        SectorMakerWorkspaceTests.Call(Window, "ClickPreset", third, false, false, visible);
        Assert.That(Selected, Is.EqualTo(new[] { third }));
        SectorMakerWorkspaceTests.Call(Window, "ClickPreset", third, true, false, visible);
        Assert.IsEmpty(Selected); Assert.IsNull(SectorMakerWorkspaceTests.Get<SectorPresetSO>(Window, "_activeSectorPreset"));
    }
    [Test] public void ShiftRangeKeepsAnchorAndCtrlShiftAddsToExistingSelection()
    {
        var second = NewPreset("Second"); var third = NewPreset("Third"); var fourth = NewPreset("Fourth");
        var visible = new[] { Preset, second, third, fourth };
        SectorMakerWorkspaceTests.Call(Window, "ClickPreset", second, false, false, visible);
        SectorMakerWorkspaceTests.Call(Window, "ClickPreset", fourth, false, true, visible);
        Assert.That(Selected, Is.EqualTo(new[] { second, third, fourth }));
        SectorMakerWorkspaceTests.Call(Window, "ClickPreset", third, false, true, visible);
        Assert.That(Selected, Is.EqualTo(new[] { second, third }));
        SectorMakerWorkspaceTests.Call(Window, "ClickPreset", Preset, true, true, visible);
        Assert.That(Selected, Is.EquivalentTo(new[] { Preset, second, third }));
    }
    [Test] public void VisibleSelectionRespectsSearchAndFoldersAndSurvivesSerialization()
    {
        var first = NewPreset("BatchSelection1"); var second = NewPreset("BatchSelection2");
        SectorMakerWorkspaceTests.Call(Window, "RefreshCatalog");
        SectorMakerWorkspaceTests.Set(Window, "_roomGroups", new bool[SectorMakerCatalog.Labels.Length]);
        SectorMakerWorkspaceTests.Set(Window, "_roomSearch", "BatchSelection");
        var visible = (SectorPresetSO[])SectorMakerWorkspaceTests.Call(Window, "VisiblePresets");
        Assert.That(visible, Is.EqualTo(new[] { first, second }));
        SectorMakerWorkspaceTests.Call(Window, "SelectPresetRange", (object)visible);
        SectorMakerWorkspaceTests.Set(Window, "_roomSearch", "");
        Assert.IsEmpty((SectorPresetSO[])SectorMakerWorkspaceTests.Call(Window, "VisiblePresets"));
        Assert.That(Selected, Is.EqualTo(visible), "Filtering must not silently change the selected deletion targets");
        var restored = ScriptableObject.CreateInstance<SectorMakerWindow>();
        try
        {
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(Window), restored);
            Assert.That(SectorMakerWorkspaceTests.Get<System.Collections.Generic.List<SectorPresetSO>>(restored, "_selectedPresets"), Is.EqualTo(visible));
        }
        finally { UnityEngine.Object.DestroyImmediate(restored); }
    }
    [Test] public void BatchTrashSkipsUsedPresetAndPreservesPrefabContentAndStage()
    {
        var unused1 = NewPreset("Unused1"); var unused2 = NewPreset("Unused2");
        var profile = ScriptableObject.CreateInstance<SectorContentProfileSO>(); AssetDatabase.CreateAsset(profile, Folder + "/KeepBulkContent.asset");
        unused1.SetContentProfile(profile); EditorUtility.SetDirty(unused1); AssetDatabase.SaveAssetIfDirty(unused1);
        var stage = NewStageUsing(Preset, true);
        string before = File.ReadAllText(AssetDatabase.GetAssetPath(stage));
        var plan = SectorMakerAssets.PlanTrashPresets(new[] { Preset, unused1, unused2, unused1 });
        Assert.AreEqual(3, plan.Length); Assert.AreEqual(2, plan.Count(e => e.CanDelete));
        Assert.That(plan.Single(e => e.preset == Preset).users, Does.Contain(Folder + "/BulkStage.asset"));
        var result = SectorMakerAssets.TrashPresets(plan);
        Assert.AreEqual(2, result.deleted.Count); Assert.AreEqual(1, result.skipped.Count); Assert.IsEmpty(result.failed);
        Assert.IsTrue(File.Exists(AssetDatabase.GetAssetPath(Preset)));
        Assert.IsTrue(File.Exists(AssetDatabase.GetAssetPath(Prefab))); Assert.IsTrue(File.Exists(Folder + "/KeepBulkContent.asset"));
        Assert.AreEqual(before, File.ReadAllText(AssetDatabase.GetAssetPath(stage)));
    }
    [Test] public void BatchTrashRechecksReferencesAddedAfterConfirmationAndBlocksDirtyPresets()
    {
        var plan = SectorMakerAssets.PlanTrashPresets(new[] { Preset }); Assert.IsTrue(plan.Single().CanDelete);
        NewStageUsing(Preset, false);
        var result = SectorMakerAssets.TrashPresets(plan);
        Assert.IsEmpty(result.deleted); Assert.AreEqual(1, result.skipped.Count);
        Assert.IsTrue(File.Exists(AssetDatabase.GetAssetPath(Preset)));
        var dirty = NewPreset("Dirty"); EditorUtility.SetDirty(dirty);
        Assert.IsFalse(SectorMakerAssets.PlanTrashPresets(new[] { dirty }).Single().CanDelete);
    }
}
