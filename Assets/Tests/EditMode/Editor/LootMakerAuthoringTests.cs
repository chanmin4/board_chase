using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class LootMakerAuthoringTests
{
    private string _folder;
    private readonly List<Object> _transient = new();

    [SetUp]
    public void Setup()
    {
        _folder = "Assets/__LootMakerTests_" + Guid.NewGuid().ToString("N");
        AssetDatabase.CreateFolder("Assets", Path.GetFileName(_folder));
    }

    [TearDown]
    public void Cleanup()
    {
        foreach (var obj in _transient)
            if (obj != null) { Undo.ClearUndo(obj); Object.DestroyImmediate(obj); }
        _transient.Clear();
        // Only this test's uniquely named assets; no project content or user Undo history.
        if (!string.IsNullOrEmpty(_folder) && _folder.StartsWith("Assets/__LootMakerTests_", StringComparison.Ordinal))
            AssetDatabase.DeleteAsset(_folder);
    }

    private T Temp<T>() where T : ScriptableObject
    {
        var obj = ScriptableObject.CreateInstance<T>(); _transient.Add(obj); return obj;
    }

    private LootTableSO Table()
    {
        var table = Temp<LootTableSO>(); table.name = "Medical source";
        var item = Temp<ValuableItemSO>();
        AssetDatabase.CreateAsset(item, _folder + "/Item_" + Guid.NewGuid().ToString("N") + ".asset");
        _transient.Remove(item);
        table.Pool.minRollCount = 2; table.Pool.maxRollCount = 5;
        table.Pool.FindTier(PlayerShopItemRarity.Normal).items.Add(new LootItemEntry
        { item = item, minAmount = 2, maxAmount = 7, useManualShare = true, manualSharePercent = 38 });
        return table;
    }

    private LootObjectSO Box(LootObjectTypeSO family)
    {
        var box = Temp<LootObjectSO>(); box.name = "Medical box";
        box.AssignLootObjectType(family); box.ApplySizePreset(LootContainerSize.Large);
        box.RollSettings.overrideTableRollCount = true;
        box.RollSettings.minRollCount = 4; box.RollSettings.maxRollCount = 8;
        box.RollSettings.rollSuccessChancePercent = 90f;
        return box;
    }

    private LootObjectTypeSO Family(LootTableSO table)
    {
        var family = Temp<LootObjectTypeSO>(); family.Configure("Medical", null, table);
        family.AssignCategory(LootContainerCategory.Medical); return family;
    }

    [Test]
    public void ClipboardIsADeepSnapshotAndRetainsItemReferencesAndAllEntryFields()
    {
        var source = Table(); var copy = LootMakerAuthoring.Snapshot(source); _transient.Add(copy);
        var before = source.Pool.rarityTiers[0].items[0]; var entry = copy.Pool.rarityTiers[0].items[0];
        Assert.That(entry, Is.Not.SameAs(before)); Assert.That(entry.item, Is.SameAs(before.item));
        Assert.That(entry.minAmount, Is.EqualTo(2)); Assert.That(entry.maxAmount, Is.EqualTo(7));
        Assert.That(entry.useManualShare, Is.True); Assert.That(entry.manualSharePercent, Is.EqualTo(38));
        source.Pool.rarityTiers[0].items.Clear(); source.Pool.maxRollCount = 1;
        Assert.That(copy.Pool.rarityTiers[0].items.Count, Is.EqualTo(1));
        Assert.That(copy.Pool.maxRollCount, Is.EqualTo(5));
    }

    [Test]
    public void CategoryMoveChangesOnlyDraggedBoxAndKeepsSharedTableAndYield()
    {
        var table = Table(); var family = Family(table); var box = Box(family); var sibling = Box(family);
        box.SetGeneratedSlotRange(2, 7);
        box.RarityWeights.legendary = 17;
        string tableBefore = JsonUtility.ToJson(table), familyBefore = JsonUtility.ToJson(family);
        string siblingBefore = JsonUtility.ToJson(sibling), weights = JsonUtility.ToJson(box.RarityWeights);
        string legacy = JsonUtility.ToJson(box.RollSettings);
        Assert.That(LootMakerAuthoring.MoveContainerCategory(box, LootContainerCategory.Ammo), Is.True);
        Assert.That(box.Category, Is.EqualTo(LootContainerCategory.Ammo));
        Assert.That(sibling.Category, Is.EqualTo(LootContainerCategory.Medical));
        Assert.That(box.LootObjectType, Is.SameAs(family)); Assert.That(box.LootTable, Is.SameAs(table));
        Assert.That(JsonUtility.ToJson(table), Is.EqualTo(tableBefore));
        Assert.That(JsonUtility.ToJson(family), Is.EqualTo(familyBefore));
        Assert.That(JsonUtility.ToJson(sibling), Is.EqualTo(siblingBefore));
        Assert.That(box.LootSlotCount, Is.EqualTo(9)); Assert.That(box.SizeClass, Is.EqualTo(LootContainerSize.Large));
        Assert.That(box.MinGeneratedSlots, Is.EqualTo(2)); Assert.That(box.MaxGeneratedSlots, Is.EqualTo(7));
        Assert.That(JsonUtility.ToJson(box.RarityWeights), Is.EqualTo(weights));
        Assert.That(JsonUtility.ToJson(box.RollSettings), Is.EqualTo(legacy));
        var all = new[] { box, sibling };
        Assert.That(LootProgressionEditorWindow.FilterWorldContainers(all, LootContainerCategory.Ammo, false, null), Is.EqualTo(new[] { box }));
        Assert.That(LootProgressionEditorWindow.FilterWorldContainers(all, LootContainerCategory.Medical, false, null), Is.EqualTo(new[] { sibling }));
    }

    [Test]
    public void CategoryMoveSupportsUndoAndRedoIncludingLegacyFallback()
    {
        var family = Family(Table()); var box = Box(family);
        string before = JsonUtility.ToJson(box);
        Undo.IncrementCurrentGroup();
        LootMakerAuthoring.MoveContainerCategory(box, LootContainerCategory.Safe);
        Undo.FlushUndoRecordObjects(); Undo.PerformUndo();
        Assert.That(JsonUtility.ToJson(box), Is.EqualTo(before));
        Assert.That(box.Category, Is.EqualTo(LootContainerCategory.Medical));
        Undo.PerformRedo();
        Assert.That(box.Category, Is.EqualTo(LootContainerCategory.Safe));
        Assert.That(box.LootObjectType, Is.SameAs(family));
    }

    [TestCase(LootContainerCategory.Ammo)]
    [TestCase(LootContainerCategory.Uncategorized)]
    public void MovingUnassignedBoxAssignsCategoryWithoutCreatingOrLosingTable(LootContainerCategory category)
    {
        var box = Temp<LootObjectSO>(); var table = Table();
        using (var data = new SerializedObject(box))
        {
            data.FindProperty("_lootTable").objectReferenceValue = table;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        Assert.That(box.HasCategoryAssignment, Is.False);
        Assert.That(LootMakerAuthoring.MoveContainerCategory(box, category), Is.True);
        Assert.That(box.HasCategoryAssignment, Is.True);
        Assert.That(box.LootObjectType, Is.Null); Assert.That(box.LootTable, Is.SameAs(table));
        Assert.That(box.HasSlotGenerationSettings, Is.False, "A category move is not a loot-data migration.");
        var all = new[] { box };
        Assert.That(LootProgressionEditorWindow.FilterWorldContainers(all, null, true, null), Is.Empty);
        Assert.That(LootProgressionEditorWindow.FilterWorldContainers(all, category, false, null), Is.EqualTo(all));
    }

    [Test]
    public void SameCategoryMoveIsNoOpAndInvalidCategoryIsRejected()
    {
        var box = Box(Family(Table())); string before = JsonUtility.ToJson(box);
        Assert.That(LootMakerAuthoring.MoveContainerCategory(box, LootContainerCategory.Medical), Is.False);
        Assert.Throws<ArgumentOutOfRangeException>(() => LootMakerAuthoring.MoveContainerCategory(box, (LootContainerCategory)999));
        Assert.Throws<ArgumentNullException>(() => LootMakerAuthoring.MoveContainerCategory(null, LootContainerCategory.Ammo));
        Assert.That(JsonUtility.ToJson(box), Is.EqualTo(before));
    }

    [Test]
    public void CategoryOverrideSurvivesLegacySourceAndIndependentCopy()
    {
        var box = Box(Family(Table()));
        LootMakerAuthoring.MoveContainerCategory(box, LootContainerCategory.Ammo);
        var other = Family(Table()); other.AssignCategory(LootContainerCategory.Safe);
        box.AssignLootObjectType(other); // Legacy fixture only; no shared-link authoring API.
        Assert.That(box.Category, Is.EqualTo(LootContainerCategory.Ammo));
        var own = LootMakerAuthoring.MakeIndependent(box, box.LootTable, _folder);
        Assert.That(box.LootTable, Is.SameAs(own));
        Assert.That(box.LootObjectType, Is.Null);
        Assert.That(box.Category, Is.EqualTo(LootContainerCategory.Ammo));
    }

    [Test]
    public void LegacyCategoryStillInheritsFamilyUntilExplicitlyMoved()
    {
        var family = Family(Table()); var box = Box(family);
        Assert.That(box.Category, Is.EqualTo(LootContainerCategory.Medical));
        family.AssignCategory(LootContainerCategory.Backpack);
        Assert.That(box.Category, Is.EqualTo(LootContainerCategory.Backpack));
        LootMakerAuthoring.MoveContainerCategory(box, LootContainerCategory.Ammo);
        family.AssignCategory(LootContainerCategory.Safe);
        Assert.That(box.Category, Is.EqualTo(LootContainerCategory.Ammo));
    }

    [Test]
    public void CategoryMoveSavesWithoutRenamingOrChangingPrefabAndRecordGuids()
    {
        var prefab = Prefab("Ammo_big");
        var box = LootMakerAuthoring.Register(prefab, Family(Table()), LootContainerCategory.Medical, out _, _folder);
        string path = AssetDatabase.GetAssetPath(box), guid = AssetDatabase.AssetPathToGUID(path);
        string prefabPath = AssetDatabase.GetAssetPath(prefab), prefabGuid = AssetDatabase.AssetPathToGUID(prefabPath);
        string prefabBefore = File.ReadAllText(prefabPath), tableBefore = JsonUtility.ToJson(box.LootTable);
        var family = box.LootObjectType;
        LootMakerAuthoring.MoveContainerCategory(box, LootContainerCategory.Ammo);
        AssetDatabase.SaveAssetIfDirty(box);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var loaded = AssetDatabase.LoadAssetAtPath<LootObjectSO>(path);
        Assert.That(loaded.Category, Is.EqualTo(LootContainerCategory.Ammo));
        Assert.That(AssetDatabase.GUIDToAssetPath(guid), Is.EqualTo(path));
        Assert.That(AssetDatabase.GUIDToAssetPath(prefabGuid), Is.EqualTo(prefabPath));
        Assert.That(File.ReadAllText(prefabPath), Is.EqualTo(prefabBefore));
        Assert.That(loaded.WorldPrefab, Is.SameAs(prefab)); Assert.That(loaded.LootObjectType, Is.SameAs(family));
        Assert.That(JsonUtility.ToJson(loaded.LootTable), Is.EqualTo(tableBefore));
        Undo.ClearUndo(loaded);
    }

    [Test]
    public void IndependentCopyKeepsCategoryAndBoxSettingsWithoutChangingSiblings()
    {
        var source = Table(); var family = Family(source); var a = Box(family); var b = Box(family);
        string settings = JsonUtility.ToJson(b.RollSettings);
        var created = LootMakerAuthoring.MakeIndependent(b, source, _folder);
        Assert.That(b.Category, Is.EqualTo(LootContainerCategory.Medical));
        Assert.That(b.LootObjectType, Is.Null);
        Assert.That(b.LootTable, Is.Not.SameAs(source)); Assert.That(a.LootTable, Is.SameAs(source));
        Assert.That(b.LootSlotCount, Is.EqualTo(9)); Assert.That(JsonUtility.ToJson(b.RollSettings), Is.EqualTo(settings));
        b.LootTable.Pool.rarityTiers[0].items.Clear(); Assert.That(source.Pool.rarityTiers[0].items.Count, Is.EqualTo(1));
    }

    [Test]
    public void IndependentLinkCanBeUndoneWithoutDeletingTheReusableCopy()
    {
        var source = Table(); var original = Family(source); var box = Box(original);
        Undo.IncrementCurrentGroup();
        var created = LootMakerAuthoring.MakeIndependent(box, source, _folder);
        Undo.FlushUndoRecordObjects(); Undo.PerformUndo();
        Assert.That(box.LootObjectType, Is.EqualTo(original));
        Assert.That(AssetDatabase.Contains(created), Is.True);
    }

    [Test]
    public void PasteContentsPreservesAssetIdentityAndSupportsUndo()
    {
        var source = Table(); var destination = Temp<LootTableSO>(); destination.name = "Keep identity";
        Undo.IncrementCurrentGroup();
        LootMakerAuthoring.PasteContents(source, destination);
        Assert.That(destination.name, Is.EqualTo("Keep identity"));
        Assert.That(destination.Pool.rarityTiers[0].items[0].item, Is.EqualTo(source.Pool.rarityTiers[0].items[0].item));
        Undo.FlushUndoRecordObjects(); Undo.PerformUndo();
        Assert.That(destination.Pool.rarityTiers[0].items, Is.Empty);
        Assert.That(source.Pool.rarityTiers[0].items.Count, Is.EqualTo(1));
    }

    [Test]
    public void CopyFromOtherBoxNeverCreatesASharedConnection()
    {
        var original = Family(Table()); var box = Box(original); var other = Family(Table());
        LootMakerAuthoring.MakeIndependent(box, other.LootTable, _folder);
        Assert.That(box.LootTable, Is.Not.SameAs(other.LootTable)); Assert.That(box.LootSlotCount, Is.EqualTo(9));
        Assert.That(box.LootObjectType, Is.Null);
        Assert.That(box.RollSettings.MaxRolls, Is.EqualTo(8)); Assert.That(original.LootTable.Pool.rarityTiers[0].items.Count, Is.EqualTo(1));
    }

    [Test]
    public void PasteToLegacyBoxCopiesContentsWithoutChangingSiblingOrBoxSettings()
    {
        var old = Table(); var family = Family(old); var a = Box(family); var b = Box(family);
        b.SetGeneratedSlotRange(3, 6); b.RarityWeights.legendary = 9;
        string oldPool = JsonUtility.ToJson(old.Pool), weights = JsonUtility.ToJson(b.RarityWeights);
        var copied = Table(); copied.Pool.rarityTiers[0].items[0].minAmount = 5;
        LootMakerAuthoring.PasteToBox(copied, b, _folder);
        Assert.That(b.LootObjectType, Is.Null);
        Assert.That(b.LootTable, Is.Not.SameAs(old)); Assert.That(b.LootTable, Is.Not.SameAs(copied));
        Assert.That(JsonUtility.ToJson(a.LootTable.Pool), Is.EqualTo(oldPool));
        Assert.That(b.LootTable.Pool.rarityTiers[0].items[0].minAmount, Is.EqualTo(5));
        Assert.That(b.MinGeneratedSlots, Is.EqualTo(3)); Assert.That(b.MaxGeneratedSlots, Is.EqualTo(6));
        Assert.That(JsonUtility.ToJson(b.RarityWeights), Is.EqualTo(weights));
        Assert.That(b.Category, Is.EqualTo(LootContainerCategory.Medical));
        copied.Pool.rarityTiers[0].items.Clear();
        Assert.That(b.LootTable.Pool.rarityTiers[0].items.Count, Is.EqualTo(1));
    }

    [Test]
    public void DuplicatedOwnedRecordIsDetectedAndPasteSeparatesOnlyThatCopy()
    {
        var a = Box(Family(Table())); LootMakerAuthoring.MakeIndependent(a, a.LootTable, _folder);
        AssetDatabase.CreateAsset(a, _folder + "/A.asset"); _transient.Remove(a);
        var b = Object.Instantiate(a); AssetDatabase.CreateAsset(b, _folder + "/B.asset");
        var old = a.LootTable; string before = JsonUtility.ToJson(old.Pool);
        Assert.That(LootMakerAuthoring.NeedsIndependentTable(b), Is.True);
        Assert.Throws<InvalidOperationException>(() => LootMakerAuthoring.PasteContents(Table(), old));
        var source = Table(); source.Pool.rarityTiers[0].items.Clear();
        LootMakerAuthoring.PasteToBox(source, b, _folder);
        Assert.That(a.LootTable, Is.SameAs(old)); Assert.That(JsonUtility.ToJson(old.Pool), Is.EqualTo(before));
        Assert.That(b.LootTable, Is.Not.SameAs(old)); Assert.That(LootMakerAuthoring.NeedsIndependentTable(b), Is.False);
        Assert.That(LootMakerAuthoring.NeedsIndependentTable(a), Is.False);
        AssetDatabase.SaveAssetIfDirty(b);
        AssetDatabase.ImportAsset(_folder + "/B.asset", ImportAssetOptions.ForceUpdate);
        Assert.That(AssetDatabase.LoadAssetAtPath<LootObjectSO>(_folder + "/B.asset").LootTable, Is.Not.SameAs(old));
    }

    [Test]
    public void DetachingLegacyFamilyFreezesCategoryAndEffectiveGenerationRange()
    {
        var source = Table(); var family = Family(source); var box = Temp<LootObjectSO>();
        box.AssignLootObjectType(family); box.RarityWeights.unique = 42;
        int min = box.MinGeneratedSlots, max = box.MaxGeneratedSlots;
        LootMakerAuthoring.MakeIndependent(box, source, _folder);
        family.AssignCategory(LootContainerCategory.Safe); source.Pool.minRollCount = 0;
        Assert.That(box.Category, Is.EqualTo(LootContainerCategory.Medical));
        Assert.That(box.MinGeneratedSlots, Is.EqualTo(min)); Assert.That(box.MaxGeneratedSlots, Is.EqualTo(max));
        Assert.That(box.RarityWeights.unique, Is.EqualTo(42)); Assert.That(box.LootObjectType, Is.Null);
    }

    [Test]
    public void DuplicateNamesNeverOverwriteEarlierTableAssets()
    {
        var a = LootMakerAuthoring.CreateTable(Table(), "Same/name", _folder);
        var b = LootMakerAuthoring.CreateTable(Table(), "Same/name", _folder);
        Assert.That(AssetDatabase.GetAssetPath(a), Is.Not.EqualTo(AssetDatabase.GetAssetPath(b)));
        Assert.That(a, Is.Not.SameAs(b));
    }

    [Test]
    public void RenamePrefabKeepsGuidRecordPoolAndVariantInheritance()
    {
        var prefab = Prefab("RenameSource");
        var box = LootMakerAuthoring.Register(prefab, Family(Table()), LootContainerCategory.Medical, out _, _folder);
        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
        string record = JsonUtility.ToJson(box), table = JsonUtility.ToJson(box.LootTable);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        GameObject variant;
        try { variant = PrefabUtility.SaveAsPrefabAsset(instance, _folder + "/RenameChild.prefab"); }
        finally { Object.DestroyImmediate(instance); }
        string variantGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(variant));
        string renamedPath = LootMakerAuthoring.RenameContainerPrefab(box, "RenamedMedical");
        Assert.That(renamedPath, Is.EqualTo(_folder + "/RenamedMedical.prefab"));
        Assert.That(AssetDatabase.AssetPathToGUID(renamedPath), Is.EqualTo(guid));
        Assert.That(box.WorldPrefab.name, Is.EqualTo("RenamedMedical"));
        Assert.That(LootProgressionEditorWindow.GetContainerTitle(box), Is.EqualTo("RenamedMedical"));
        Assert.That(JsonUtility.ToJson(box), Is.EqualTo(record));
        Assert.That(JsonUtility.ToJson(box.LootTable), Is.EqualTo(table));
        Assert.That(box.WorldPrefab.GetComponent<LootableObject>().LootObject, Is.EqualTo(box));
        Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(variant)), Is.EqualTo(variantGuid));
        Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(variant), Is.EqualTo(box.WorldPrefab));
        Assert.That(LootProgressionEditorWindow.FilterWorldContainers(new[] { box }, null, false, "RenamedMedical"), Does.Contain(box));
    }

    [Test]
    public void RenameRejectsCollisionsWithoutModifyingEitherPrefab()
    {
        var prefab = Prefab("KeepSource"); var occupied = Prefab("AlreadyThere");
        var box = LootMakerAuthoring.Register(prefab, Family(Table()), LootContainerCategory.Medical, out _, _folder);
        string source = File.ReadAllText(AssetDatabase.GetAssetPath(prefab));
        string target = File.ReadAllText(AssetDatabase.GetAssetPath(occupied));
        Assert.Throws<InvalidOperationException>(() => LootMakerAuthoring.RenameContainerPrefab(box, "AlreadyThere"));
        Assert.That(File.ReadAllText(AssetDatabase.GetAssetPath(prefab)), Is.EqualTo(source));
        Assert.That(File.ReadAllText(AssetDatabase.GetAssetPath(occupied)), Is.EqualTo(target));
    }

    [TestCase("")]
    [TestCase("../MoveOut")]
    [TestCase("Sub/MoveOut")]
    [TestCase("Sub\\MoveOut")]
    [TestCase("CON")]
    [TestCase("Invalid.")]
    [TestCase("Bad:Name")]
    public void RenameRejectsInvalidNamesWithoutMovingAsset(string requested)
    {
        var prefab = Prefab("KeepName");
        var box = Temp<LootObjectSO>();
        using (var data = new SerializedObject(box))
        { data.FindProperty("_worldPrefab").objectReferenceValue = prefab; data.ApplyModifiedPropertiesWithoutUndo(); }
        string path = AssetDatabase.GetAssetPath(prefab);
        Assert.Throws<ArgumentException>(() => LootMakerAuthoring.RenameContainerPrefab(box, requested));
        Assert.That(AssetDatabase.GetAssetPath(prefab), Is.EqualTo(path));
    }

    [Test]
    public void RenameAcceptsExtensionAndCanChangeCaseWithoutChangingGuid()
    {
        var prefab = Prefab("CaseSource"); var box = Temp<LootObjectSO>();
        using (var data = new SerializedObject(box))
        { data.FindProperty("_worldPrefab").objectReferenceValue = prefab; data.ApplyModifiedPropertiesWithoutUndo(); }
        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
        string renamed = LootMakerAuthoring.RenameContainerPrefab(box, "casesource.prefab");
        Assert.That(renamed, Is.EqualTo(_folder + "/casesource.prefab"));
        Assert.That(AssetDatabase.AssetPathToGUID(renamed), Is.EqualTo(guid));
        Assert.That(LootMakerAuthoring.RenameContainerPrefab(box, "casesource"), Is.EqualTo(renamed));
    }

    [Test]
    public void DisplayNameEditingSupportsUndoWithoutRenamingSharedData()
    {
        var table = Table(); var family = Family(table); var box = Box(family);
        string before = box.ResolveDisplayName(), familyBefore = JsonUtility.ToJson(family), tableBefore = JsonUtility.ToJson(table);
        Undo.IncrementCurrentGroup();
        LootMakerAuthoring.SetContainerDisplayName(box, "Medical case title");
        Assert.That(box.ResolveDisplayName(), Is.EqualTo("Medical case title"));
        Undo.FlushUndoRecordObjects(); Undo.PerformUndo();
        Assert.That(box.ResolveDisplayName(), Is.EqualTo(before));
        Assert.That(JsonUtility.ToJson(family), Is.EqualTo(familyBefore));
        Assert.That(JsonUtility.ToJson(table), Is.EqualTo(tableBefore));
    }

    [Test]
    public void RenameWithoutPrefabIsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => LootMakerAuthoring.RenameContainerPrefab(Temp<LootObjectSO>(), "Name"));
    }

    private GameObject Prefab(string name)
    {
        var go = new GameObject(name);
        try
        {
            go.AddComponent<StructureLootInventoryRuntime>(); go.AddComponent<LootableObject>();
            return PrefabUtility.SaveAsPrefabAsset(go, _folder + "/" + name + ".prefab");
        }
        finally { Object.DestroyImmediate(go); }
    }

    private LootObjectSO DuplicateRecord(LootObjectSO source, string name)
    {
        var duplicate = Object.Instantiate(source);
        duplicate.name = name;
        AssetDatabase.CreateAsset(duplicate, _folder + "/" + name + ".asset");
        return duplicate;
    }

    private static object WindowField(LootProgressionEditorWindow window, string name) =>
        typeof(LootProgressionEditorWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);

    private static void WindowCall(LootProgressionEditorWindow window, string name, params object[] args) =>
        typeof(LootProgressionEditorWindow).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(window, args);

    [Test]
    public void DeletionReviewOpensImmediatelyBeforeDeferredScan()
    {
        var window = Temp<LootProgressionEditorWindow>(); var box = Temp<LootObjectSO>();
        WindowCall(window, "SelectContainer", box);
        WindowCall(window, "QueueContainerDeleteCheck", box);
        Assert.That(WindowField(window, "_deleteBox"), Is.SameAs(box));
        Assert.That(WindowField(window, "_deletePending"), Is.True);
        Assert.That(WindowField(window, "_deleteWork"), Is.Not.Null);
        Assert.That(WindowField(window, "_deleteUsage"), Is.Null);
        WindowCall(window, "ClearContainerDeleteReview"); // Never run a scan or delete a record in this state test.
    }

    [Test]
    public void DeletionReviewSurvivesSelectingTheSameCard()
    {
        var window = Temp<LootProgressionEditorWindow>(); var box = Temp<LootObjectSO>();
        WindowCall(window, "SelectContainer", box);
        WindowCall(window, "QueueContainerDeleteCheck", box);
        object work = WindowField(window, "_deleteWork"), request = WindowField(window, "_deleteRequest");
        WindowCall(window, "SelectContainer", box);
        Assert.That(WindowField(window, "_deleteBox"), Is.SameAs(box));
        Assert.That(WindowField(window, "_deleteWork"), Is.SameAs(work));
        Assert.That(WindowField(window, "_deleteRequest"), Is.EqualTo(request));
        WindowCall(window, "ClearContainerDeleteReview");
    }

    [Test]
    public void DeletionReviewNavigationCancelsQueuedWorkAndInvalidatesStaleResult()
    {
        var window = Temp<LootProgressionEditorWindow>(); var box = Temp<LootObjectSO>();
        WindowCall(window, "SelectContainer", box);
        WindowCall(window, "QueueContainerDeleteCheck", box);
        int request = (int)WindowField(window, "_deleteRequest");
        WindowCall(window, "SelectContainer", Temp<LootObjectSO>());
        Assert.That(WindowField(window, "_deleteBox"), Is.Null);
        Assert.That(WindowField(window, "_deletePending"), Is.False);
        Assert.That(WindowField(window, "_deleteWork"), Is.Null);
        Assert.That((int)WindowField(window, "_deleteRequest"), Is.GreaterThan(request));
    }

    [Test]
    public void DeletionReviewDisableUnsubscribesWorkWithoutChangingSelectionAsset()
    {
        var window = Temp<LootProgressionEditorWindow>(); var box = Temp<LootObjectSO>();
        string before = JsonUtility.ToJson(box);
        WindowCall(window, "SelectContainer", box);
        WindowCall(window, "QueueContainerDeleteCheck", box);
        WindowCall(window, "OnDisable");
        Assert.That(WindowField(window, "_deleteWork"), Is.Null);
        Assert.That(WindowField(window, "_deletePending"), Is.False);
        Assert.That(JsonUtility.ToJson(box), Is.EqualTo(before));
    }

    private StageDefinitionSO StageReferencing(LootObjectSO box)
    {
        // Copy a valid stage into the test folder; never change the real stage or open its scene.
        var template = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>("Assets/ScriptableObject/GameScene/Stage/Stage1Definition.asset");
        var stage = Object.Instantiate(template);
        using (var data = new SerializedObject(stage))
        {
            var rooms = data.FindProperty("_raidRuntime.rooms");
            rooms.arraySize = 1;
            rooms.GetArrayElementAtIndex(0).FindPropertyRelative("lootObject").objectReferenceValue = box;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        AssetDatabase.CreateAsset(stage, _folder + "/TestStage.asset");
        AssetDatabase.SaveAssetIfDirty(stage);
        return stage;
    }

    [Test]
    public void DeleteUnusedDuplicateKeepsPrefabFamilyTableAndCanonicalRecord()
    {
        var prefab = Prefab("KeepModel");
        var canonical = LootMakerAuthoring.Register(prefab, Family(Table()), LootContainerCategory.Medical, out _, _folder);
        var duplicate = DuplicateRecord(canonical, "UnusedDuplicate");
        string path = AssetDatabase.GetAssetPath(duplicate), content = File.ReadAllText(path), meta = File.ReadAllText(path + ".meta");
        string prefabPath = AssetDatabase.GetAssetPath(prefab), prefabBefore = File.ReadAllText(prefabPath);
        string tableBefore = JsonUtility.ToJson(canonical.LootTable);
        var usage = LootMakerDeletion.FindUsage(duplicate);
        Assert.That(usage.References, Is.Empty); Assert.That(usage.Blockers, Is.Empty);
        string backup = LootMakerDeletion.DeleteRecord(duplicate, null);
        Assert.That(File.Exists(path), Is.False); Assert.That(File.Exists(path + ".meta"), Is.False);
        Assert.That(File.ReadAllText(backup + "/000.asset"), Is.EqualTo(content));
        Assert.That(File.ReadAllText(backup + "/000.asset.meta"), Is.EqualTo(meta));
        Assert.That(File.Exists(backup + "/manifest.json"), Is.True);
        Assert.That(File.ReadAllText(prefabPath), Is.EqualTo(prefabBefore));
        Assert.That(prefab.GetComponent<LootableObject>().LootObject, Is.EqualTo(canonical));
        Assert.That(JsonUtility.ToJson(canonical.LootTable), Is.EqualTo(tableBefore));
        Assert.That(canonical.LootObjectType, Is.Null);
        Assert.That(AssetDatabase.Contains(canonical.LootTable), Is.True);
    }

    [Test]
    public void DeleteUsedRecordRequiresReplacementAndRejectsSelfWithoutChangingFiles()
    {
        var prefab = Prefab("UsedModel");
        var box = LootMakerAuthoring.Register(prefab, Family(Table()), LootContainerCategory.Medical, out _, _folder);
        string path = AssetDatabase.GetAssetPath(box), before = File.ReadAllText(path);
        Assert.Throws<InvalidOperationException>(() => LootMakerDeletion.DeleteRecord(box, null));
        Assert.Throws<InvalidOperationException>(() => LootMakerDeletion.DeleteRecord(box, box));
        Assert.That(File.ReadAllText(path), Is.EqualTo(before));
        Assert.That(prefab.GetComponent<LootableObject>().LootObject, Is.EqualTo(box));
    }

    [Test]
    public void DeleteRedirectsStageAndPrefabThenRetainsBothLootTables()
    {
        var prefab = Prefab("RedirectModel");
        var old = LootMakerAuthoring.Register(prefab, Family(Table()), LootContainerCategory.Medical, out _, _folder);
        var replacement = DuplicateRecord(old, "Replacement");
        LootMakerAuthoring.MakeIndependent(replacement, old.LootTable, _folder);
        AssetDatabase.SaveAssetIfDirty(replacement);
        var stage = StageReferencing(old);
        string sourcePath = AssetDatabase.GetAssetPath(old), stagePath = AssetDatabase.GetAssetPath(stage);
        string tablePath = AssetDatabase.GetAssetPath(old.LootTable), tableBefore = File.ReadAllText(tablePath);
        string newTableBefore = JsonUtility.ToJson(replacement.LootTable);
        var usage = LootMakerDeletion.FindUsage(old);
        Assert.That(usage.Blockers, Is.Empty);
        Assert.That(usage.References.Any(r => r.StartsWith(stagePath)), Is.True);
        Assert.That(usage.References.Any(r => r.StartsWith(AssetDatabase.GetAssetPath(prefab))), Is.True);
        LootMakerDeletion.DeleteRecord(old, replacement);
        Assert.That(File.Exists(sourcePath), Is.False);
        AssetDatabase.ImportAsset(stagePath, ImportAssetOptions.ForceUpdate);
        using var data = new SerializedObject(AssetDatabase.LoadAssetAtPath<StageDefinitionSO>(stagePath));
        Assert.That(data.FindProperty("_raidRuntime.rooms").GetArrayElementAtIndex(0).FindPropertyRelative("lootObject").objectReferenceValue,
            Is.EqualTo(replacement));
        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(prefab), ImportAssetOptions.ForceUpdate);
        Assert.That(prefab.GetComponent<LootableObject>().LootObject, Is.EqualTo(replacement));
        Assert.That(File.ReadAllText(tablePath), Is.EqualTo(tableBefore));
        Assert.That(JsonUtility.ToJson(replacement.LootTable), Is.EqualTo(newTableBefore));
    }

    [Test]
    public void DeleteDetectsUnsavedNewStageReferenceAndRefusesToSaveUserEdits()
    {
        var prefab = Prefab("UnsavedModel");
        var canonical = LootMakerAuthoring.Register(prefab, Family(Table()), LootContainerCategory.Medical, out _, _folder);
        var duplicate = DuplicateRecord(canonical, "Duplicate");
        var stage = StageReferencing(null);
        string stagePath = AssetDatabase.GetAssetPath(stage), before = File.ReadAllText(stagePath);
        using (var data = new SerializedObject(stage))
        {
            data.FindProperty("_raidRuntime.rooms").GetArrayElementAtIndex(0).FindPropertyRelative("lootObject").objectReferenceValue = duplicate;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorUtility.SetDirty(stage);
        var usage = LootMakerDeletion.FindUsage(duplicate);
        Assert.That(usage.Blockers.Any(b => b.Contains("Unsaved")), Is.True);
        Assert.Throws<InvalidOperationException>(() => LootMakerDeletion.DeleteRecord(duplicate, canonical));
        Assert.That(File.ReadAllText(stagePath), Is.EqualTo(before));
        Assert.That(AssetDatabase.Contains(duplicate), Is.True);
    }

    [Test]
    public void DeleteBlocksGuidStringReferencesRatherThanSilentlyBreakingThem()
    {
        var prefab = Prefab("StringReferenceModel");
        var canonical = LootMakerAuthoring.Register(prefab, Family(Table()), LootContainerCategory.Medical, out _, _folder);
        var duplicate = DuplicateRecord(canonical, "StringReferencedDuplicate");
        var holder = DuplicateRecord(canonical, "StringHolder");
        LootMakerAuthoring.SetContainerDisplayName(holder, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(duplicate)));
        AssetDatabase.SaveAssetIfDirty(holder);
        var usage = LootMakerDeletion.FindUsage(duplicate);
        Assert.That(usage.Blockers.Any(b => b.Contains("GUID/string")), Is.True);
        Assert.Throws<InvalidOperationException>(() => LootMakerDeletion.DeleteRecord(duplicate, canonical));
        Assert.That(AssetDatabase.Contains(duplicate), Is.True);
        Undo.ClearUndo(holder);
    }

    [Test]
    public void DeleteRejectsTransientRecordOrTransientReplacement()
    {
        Assert.Throws<InvalidOperationException>(() => LootMakerDeletion.DeleteRecord(Temp<LootObjectSO>(), null));
        var prefab = Prefab("SavedModel");
        var box = LootMakerAuthoring.Register(prefab, Family(Table()), LootContainerCategory.Medical, out _, _folder);
        Assert.Throws<InvalidOperationException>(() => LootMakerDeletion.DeleteRecord(box, Temp<LootObjectSO>()));
        Assert.That(AssetDatabase.Contains(box), Is.True);
    }

    [Test]
    public void RegisterCreatesBidirectionalLinksAndCopiesTheSelectedList()
    {
        var prefab = Prefab("Box"); var source = Family(Table());
        var box = LootMakerAuthoring.Register(prefab, source, LootContainerCategory.Medical, out bool created, _folder);
        Assert.That(created, Is.True); Assert.That(box.WorldPrefab, Is.EqualTo(prefab));
        Assert.That(prefab.GetComponent<LootableObject>().LootObject, Is.EqualTo(box));
        Assert.That(box.LootTable, Is.Not.SameAs(source.LootTable));
        Assert.That(box.LootTable.Pool.rarityTiers[0].items[0].item, Is.EqualTo(source.LootTable.Pool.rarityTiers[0].items[0].item));
    }

    [Test]
    public void ReRegisterSelectsExistingBoxWithoutChangingAnyData()
    {
        var prefab = Prefab("Existing");
        var first = LootMakerAuthoring.Register(prefab, Family(Table()), LootContainerCategory.Medical, out _, _folder);
        var unrelated = Family(Table());
        string before = File.ReadAllText(AssetDatabase.GetAssetPath(prefab));
        int count = AssetDatabase.FindAssets("", new[] { _folder }).Length;
        var again = LootMakerAuthoring.Register(prefab, unrelated, LootContainerCategory.Ammo, out bool created, _folder);
        Assert.That(created, Is.False); Assert.That(again, Is.EqualTo(first));
        Assert.That(File.ReadAllText(AssetDatabase.GetAssetPath(prefab)), Is.EqualTo(before));
        Assert.That(AssetDatabase.FindAssets("", new[] { _folder }).Length, Is.EqualTo(count));
    }

    [Test]
    public void RegisteringVariantDoesNotRebindItsParentAndKeepsVariantInheritance()
    {
        var original = Prefab("Original");
        var first = LootMakerAuthoring.Register(original, Family(Table()), LootContainerCategory.Medical, out _, _folder);
        first.ApplySizePreset(LootContainerSize.Large);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(original);
        GameObject variant;
        try { variant = PrefabUtility.SaveAsPrefabAsset(instance, _folder + "/Variant.prefab"); }
        finally { Object.DestroyImmediate(instance); }
        var second = LootMakerAuthoring.Register(variant, null, LootContainerCategory.Medical, out bool created, _folder);
        Assert.That(created, Is.True); Assert.That(second, Is.Not.EqualTo(first));
        Assert.That(second.LootSlotCount, Is.EqualTo(9));
        Assert.That(original.GetComponent<LootableObject>().LootObject, Is.EqualTo(first));
        Assert.That(variant.GetComponent<LootableObject>().LootObject, Is.EqualTo(second));
        Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(variant), Is.EqualTo(original));
        Assert.That(second.LootTable, Is.Not.EqualTo(first.LootTable));
    }

    [Test]
    public void RegistrationRejectsSceneObjectsAndNonLootPrefabs()
    {
        var go = new GameObject("Not a loot prefab");
        try
        {
            Assert.That(LootMakerAuthoring.CanRegister(go, out _, out _), Is.False);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, _folder + "/NotLoot.prefab");
            Assert.That(LootMakerAuthoring.CanRegister(prefab, out _, out _), Is.False);
            Assert.Throws<InvalidOperationException>(() => LootMakerAuthoring.Register(prefab, null, LootContainerCategory.Medical, out _, _folder));
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void RegistrationRejectsAmbiguousMultiContainerPrefabs()
    {
        var parent = new GameObject("Two boxes");
        try
        {
            foreach (string name in new[] { "A", "B" })
            {
                var child = new GameObject(name); child.transform.SetParent(parent.transform);
                child.AddComponent<StructureLootInventoryRuntime>(); child.AddComponent<LootableObject>();
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(parent, _folder + "/Ambiguous.prefab");
            Assert.That(LootMakerAuthoring.CanRegister(prefab, out _, out string reason), Is.False);
            Assert.That(reason, Does.Contain("exactly one"));
        }
        finally { Object.DestroyImmediate(parent); }
    }

    [Test]
    public void AuthoredContainerPreviewsPointToActualRootGameObjects()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:LootObjectSO", new[] { LootMakerAuthoring.Root }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var box = AssetDatabase.LoadAssetAtPath<LootObjectSO>(path);
            Assert.That(box.WorldPrefab, Is.Not.Null, path);
            string prefabPath = AssetDatabase.GetAssetPath(box.WorldPrefab);
            Assert.That(box.WorldPrefab, Is.EqualTo(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)), path);
            Assert.That(box.WorldPrefab.name, Is.Not.Empty, path);
        }
    }
}
