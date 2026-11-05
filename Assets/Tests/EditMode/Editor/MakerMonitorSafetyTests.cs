using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed class MakerMonitorSafetyTests
{
    private readonly List<Object> _temporary = new();
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    internal static object Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, Private).Invoke(target, args);
    internal static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, Private).SetValue(target, value);
    private T Asset<T>() where T : ScriptableObject
    {
        var asset = ScriptableObject.CreateInstance<T>(); _temporary.Add(asset); return asset;
    }
    private EntityStatusEffectRuntime Effects()
    {
        var root = new GameObject("Temporary monitor test"); root.SetActive(false); _temporary.Add(root);
        return root.AddComponent<EntityStatusEffectRuntime>();
    }
    private StatusEffectDefinitionSO Definition(bool persistent, bool bleeding = false)
    {
        var definition = Asset<StatusEffectDefinitionSO>();
        using var serialized = new SerializedObject(definition);
        serialized.FindProperty("_persistentStacks").boolValue = persistent;
        serialized.FindProperty("_isBleeding").boolValue = bleeding;
        serialized.FindProperty("_maximumStacks").intValue = 10;
        serialized.FindProperty("_damagePerStackPerSecond").floatValue = .5f;
        serialized.FindProperty("_durationSeconds").floatValue = 3f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return definition;
    }
    [TearDown] public void Cleanup()
    {
        for (int i = _temporary.Count - 1; i >= 0; i--) if (_temporary[i] != null) Object.DestroyImmediate(_temporary[i]);
        _temporary.Clear();
    }

    [Test] public void RegistrationScanContainsOnlySupportedGameDataIncludingDerivedItems()
    {
        Assert.IsTrue(MakerRegistrationIndex.IsIndexedType(typeof(RangedWeaponSO)));
        Assert.IsTrue(MakerRegistrationIndex.IsIndexedType(typeof(BackpackItemSO)));
        Assert.IsFalse(MakerRegistrationIndex.IsIndexedType(typeof(ScriptableObject)));
        Assert.IsFalse(MakerRegistrationIndex.IsIndexedType(typeof(StatusEffectDefinitionSO)));
        var assets = MakerRegistrationIndex.ScanAssets();
        Assert.IsNotEmpty(assets);
        Assert.IsTrue(assets.All(a => MakerRegistrationIndex.IsIndexedType(a.GetType())));
        Assert.IsTrue(assets.OfType<BackpackItemSO>().Any());
        Assert.AreEqual(assets.Length, assets.Distinct().Count());
    }

    [Test] public void PersistentDebugApplyAddsStacksAndNeverChangesItsSO()
    {
        var definition = Definition(true, true); var runtime = Effects();
        string before = EditorJsonUtility.ToJson(definition);
        Assert.IsTrue(DebugMonitorWindow.ApplyDebugStatusEffect(runtime, definition, 1f, 3));
        Assert.AreEqual(3, runtime.BleedingStacks);
        Assert.IsTrue(float.IsPositiveInfinity(runtime.ActiveEffects[0].RemainingSeconds));
        DebugMonitorWindow.ApplyDebugStatusEffect(runtime, definition, 1f, 100);
        Assert.AreEqual(10, runtime.BleedingStacks);
        Assert.AreEqual(before, EditorJsonUtility.ToJson(definition));
    }

    [Test] public void TimedDebugApplyRefreshesOnceWithoutStacking()
    {
        var runtime = Effects(); var definition = Definition(false);
        DebugMonitorWindow.ApplyDebugStatusEffect(runtime, definition, 6f, 10);
        Assert.AreEqual(1, runtime.ActiveEffects.Count);
        Assert.AreEqual(1, runtime.ActiveEffects[0].Stacks);
        Assert.AreEqual(6f, runtime.ActiveEffects[0].RemainingSeconds, .02f);
        Assert.That(DebugMonitorWindow.FormatDebugStatusEffect(runtime.ActiveEffects[0]), Does.Contain("남은 시간"));
    }

    [Test] public void BleedingTreatmentDoesNotClearOtherTimedOrPersistentEffects()
    {
        var runtime = Effects(); var bleeding = Definition(true, true);
        var timed = Definition(false); var persistent = Definition(true);
        DebugMonitorWindow.ApplyDebugStatusEffect(runtime, bleeding, -1, 3);
        DebugMonitorWindow.ApplyDebugStatusEffect(runtime, timed, -1, 1);
        DebugMonitorWindow.ApplyDebugStatusEffect(runtime, persistent, -1, 2);
        Assert.AreEqual(1, runtime.TreatBleeding(1)); Assert.AreEqual(2, runtime.BleedingStacks);
        Assert.AreEqual(2, runtime.TreatBleeding(int.MaxValue)); Assert.AreEqual(0, runtime.BleedingStacks);
        Assert.AreEqual(2, runtime.ActiveEffects.Count);
    }

    [Test] public void PersistentStatusDescriptionIncludesStacksAndDamageNotInfinity()
    {
        var runtime = Effects(); var definition = Definition(true, true);
        DebugMonitorWindow.ApplyDebugStatusEffect(runtime, definition, -1, 3);
        string text = DebugMonitorWindow.FormatDebugStatusEffect(runtime.ActiveEffects[0]);
        Assert.That(text, Does.Contain("영구").And.Contain("3/10").And.Contain("1.5"));
        Assert.That(text, Does.Not.Contain("Infinity"));
        string summary = (string)typeof(DebugMonitorWindow).GetMethod("BuildStatusSummary", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { runtime });
        Assert.That(summary, Does.Contain("3/10").And.Not.Contain("Infinity"));
    }

    [Test] public void PreviewAndPrefabStageObjectsAreNotSerializedInMakerLayouts()
    {
        foreach (string field in new[] { "_previewUtility", "_previewCamera", "_previewInstance", "_previewTexture" })
            Assert.IsTrue(typeof(ItemMakerWindow).GetField(field, Private).IsDefined(typeof(NonSerializedAttribute)));
        foreach (string field in new[] { "_activeSectorRuntime", "_sectorObjectParent", "_enemySpawnPointParent", "_sectorRuntime",
                     "_contentSlotParent", "_contentSectorRuntime", "_selectionEditor", "_selectionEditorTarget" })
            Assert.IsTrue(typeof(SectorMakerWindow).GetField(field, Private).IsDefined(typeof(NonSerializedAttribute)));
    }

    [Test] public void ItemPreviewRendersAndCleansUpWithoutAddingOrSavingWorkingScenes()
    {
        var before = Enumerable.Range(0, SceneManager.sceneCount)
            .Select(i => SceneManager.GetSceneAt(i)).Select(s => (s.handle, s.path, s.isDirty)).ToArray();
        var window = Asset<ItemMakerWindow>();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameScene/GameItemObject/BackPack/BagLv1.prefab");
        Assert.IsNotNull(prefab);
        Set(window, "_previewSourcePrefab", prefab); Call(window, "RebuildPreviewInstance");
        var camera = (Camera)typeof(ItemMakerWindow).GetField("_previewCamera", Private).GetValue(window);
        Assert.IsTrue(UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(camera.scene));
        var image = (Texture2D)Call(window, "CapturePreviewTexture", 256); _temporary.Add(image);
        Assert.IsNotNull(image); Assert.AreEqual(256, image.width); Assert.AreEqual(256, image.height);
        var pixels = image.GetPixels32();
        Assert.IsTrue(pixels.Any(p => p.r > 8 || p.g > 8 || p.b > 8), "Preview must not be blank/black.");
        Assert.IsTrue(pixels.Select(p => (p.r, p.g, p.b, p.a)).Distinct().Count() > 10, "Preview must contain rendered model detail.");
        Assert.IsTrue(pixels.Any(p => p.a == 0) && pixels.Any(p => p.a > 0), "Transparent PNG must contain both background alpha and visible geometry.");
        Directory.CreateDirectory(MakerMonitorVerification.Folder);
        File.WriteAllBytes(MakerMonitorVerification.Folder + "/item-preview-transparent.png", image.EncodeToPNG());
        Set(window, "_transparentPreviewBackground", false);
        var solid = (Texture2D)Call(window, "CapturePreviewTexture", 512); _temporary.Add(solid);
        Assert.AreEqual(512, solid.width); Assert.IsTrue(solid.GetPixels32().All(p => p.a == 255));
        File.WriteAllBytes(MakerMonitorVerification.Folder + "/item-preview-solid.png", solid.EncodeToPNG());
        string layout = EditorJsonUtility.ToJson(window);
        Assert.That(layout, Does.Not.Contain("_previewInstance").And.Not.Contain("_previewCamera").And.Not.Contain("_previewTexture"));
        Call(window, "DisposePreviewStudio");
        var after = Enumerable.Range(0, SceneManager.sceneCount)
            .Select(i => SceneManager.GetSceneAt(i)).Select(s => (s.handle, s.path, s.isDirty)).ToArray();
        CollectionAssert.AreEqual(before, after);
    }

    [Test] public void CurrentSectorDescriptionsReflectItsLiveGasAndPlayerModifiers()
    {
        var root = new GameObject("Temporary trait test"); root.SetActive(false); _temporary.Add(root);
        var sector = root.AddComponent<SectorRuntime>(); var traits = root.AddComponent<SectorRoomTraitRuntime>();
        var table = Asset<SectorRoomTraitTableSO>();
        traits.Configure(table, StageRoomType.Normal, 12, Vector2Int.zero, true,
            new StageDefinitionSO.BiochemicalGasSettings { enabled = true, baselineInfectionPerSecond = 1f }, 3, true, 2f);
        var previous = SectorRoomTraitRuntime.CurrentPlayerSectorTrait;
        try
        {
            SectorRoomTraitRuntime.SetCurrentPlayerSector(sector);
            traits.SetDebugRoomTraitOverride(SectorRoomTraitType.Fear);
            Assert.That(DebugMonitorWindow.DebugRoomTraitDescription(traits), Does.Contain("×3"));
            traits.SetDebugRoomTraitOverride(SectorRoomTraitType.GasBoost);
            Assert.That(DebugMonitorWindow.DebugRoomTraitDescription(traits), Does.Contain("2/초"));
            foreach (SectorRoomTraitType trait in Enum.GetValues(typeof(SectorRoomTraitType)))
                Assert.IsNotEmpty(DebugMonitorWindow.DebugRoomTraitName(trait));
        }
        finally { SectorRoomTraitRuntime.SetCurrentPlayerSector(previous != null ? previous.GetComponent<SectorRuntime>() : null); }
    }
}
