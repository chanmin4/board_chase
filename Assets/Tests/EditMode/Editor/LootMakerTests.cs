using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class LootMakerTests
{
    private readonly List<Object> _created = new();

    [TearDown]
    public void Cleanup()
    {
        for (int i = _created.Count - 1; i >= 0; i--)
            if (_created[i] != null) Object.DestroyImmediate(_created[i]);
        _created.Clear();
    }

    [Test]
    public void DefaultsKeepExistingCapacityAndTableRolls()
    {
        var variant = Asset<LootObjectSO>();
        Assert.That(variant.LootSlotCount, Is.EqualTo(6));
        Assert.That(variant.SizeClass, Is.EqualTo(LootContainerSize.Custom));
        Assert.That(variant.RollSettings.overrideTableRollCount, Is.False);
        Assert.That(variant.RollSettings.SuccessChance, Is.EqualTo(100));
        Assert.That(Asset<LootObjectTypeSO>().Category, Is.EqualTo(LootContainerCategory.Uncategorized));
        Assert.That(Enum.GetValues(typeof(LootContainerCategory)).Cast<LootContainerCategory>().Select(value => (int)value).ToArray(),
            Is.EqualTo(Enumerable.Range(0, 8).ToArray()));
    }

    [TestCase(LootContainerSize.Small, 4, 1, 3, 65)]
    [TestCase(LootContainerSize.Medium, 8, 2, 5, 80)]
    [TestCase(LootContainerSize.Large, 12, 4, 8, 90)]
    public void PresetsChangeOnlySelectedVariant(LootContainerSize size, int slots, int min, int max, int chance)
    {
        var table = Table();
        var family = Family(table);
        var selected = Variant(family);
        var sibling = Variant(family);
        string originalTable = JsonUtility.ToJson(table);
        selected.ApplySizePreset(size);
        Assert.That(selected.LootSlotCount, Is.EqualTo(slots));
        Assert.That(selected.RollSettings.overrideTableRollCount, Is.True);
        Assert.That(selected.RollSettings.MinRolls, Is.EqualTo(min));
        Assert.That(selected.RollSettings.MaxRolls, Is.EqualTo(max));
        Assert.That(selected.RollSettings.SuccessChance, Is.EqualTo(chance));
        Assert.That(selected.LootTable, Is.SameAs(sibling.LootTable));
        Assert.That(sibling.LootSlotCount, Is.EqualTo(6));
        Assert.That(sibling.RollSettings.overrideTableRollCount, Is.False);
        Assert.That(JsonUtility.ToJson(table), Is.EqualTo(originalTable));
        selected.ApplySizePreset(LootContainerSize.Custom);
        Assert.That(selected.LootSlotCount, Is.EqualTo(slots), "Custom is a label, not a reset.");
    }

    [TestCase(0)]
    [TestCase(42)]
    [TestCase(12345)]
    public void DefaultVariantPreservesLegacySeededRolls(int seed)
    {
        var table = Table();
        table.Pool.minRollCount = 3;
        table.Pool.maxRollCount = 12;
        Add(table, Item(PlayerShopItemRarity.Rare));
        var legacy = new List<LootRollResult>();
        var current = new List<LootRollResult>();
        var firstRandom = new System.Random(seed);
        var secondRandom = new System.Random(seed);
        table.TryRoll(firstRandom, legacy, null);
        table.TryRoll(secondRandom, current, null, new LootContainerRollSettings());
        AssertRollsEqual(legacy, current);
        Assert.That(secondRandom.Next(), Is.EqualTo(firstRandom.Next()), "100% must not add random draws.");
    }

    [TestCase(0, 5, 0)]
    [TestCase(100, 5, 5)]
    [TestCase(100, 0, 0)]
    public void ChanceAndOverrideCountsAreApplied(int chance, int attempts, int expected)
    {
        var results = new List<LootRollResult>();
        Table().TryRoll(new System.Random(123), results, null, Rules(attempts, chance));
        Assert.That(results.Count, Is.EqualTo(expected));
    }

    [Test]
    public void PartialChanceIsDeterministicAndDoesNotChangePool()
    {
        var table = Table();
        string before = JsonUtility.ToJson(table);
        var first = new List<LootRollResult>();
        var second = new List<LootRollResult>();
        table.TryRoll(new System.Random(901), first, null, Rules(128, 50));
        table.TryRoll(new System.Random(901), second, null, Rules(128, 50));
        Assert.That(first.Count, Is.InRange(35, 95));
        AssertRollsEqual(first, second);
        Assert.That(JsonUtility.ToJson(table), Is.EqualTo(before));
    }

    [Test]
    public void SharedPoolSupportsIndependentVariantYield()
    {
        var table = Table();
        var family = Family(table);
        var small = Variant(family);
        var large = Variant(family);
        Set(small, "_rollSettings", Rules(1, 100));
        Set(large, "_rollSettings", Rules(8, 100));
        var results = new List<LootRollResult>();
        small.LootTable.TryRoll(new System.Random(1), results, null, small.RollSettings);
        Assert.That(results.Count, Is.EqualTo(1));
        large.LootTable.TryRoll(new System.Random(1), results, null, large.RollSettings);
        Assert.That(results.Count, Is.EqualTo(8));
        Assert.That(table.Pool.maxRollCount, Is.EqualTo(1));
    }

    [Test]
    public void LockBonusesAndRarityMultipliersStillApply()
    {
        var table = Table();
        var rare = Item(PlayerShopItemRarity.Rare);
        Add(table, rare);
        var profile = Asset<LockedLootProfileSO>();
        Set(profile, "_normalWeightMultiplier", 0f);
        Set(profile, "_minAdditionalRolls", 2);
        Set(profile, "_maxAdditionalRolls", 2);
        var results = new List<LootRollResult>();
        table.TryRoll(new System.Random(10), results, profile, Rules(3, 100));
        Assert.That(results.Count, Is.EqualTo(5));
        Assert.That(results.All(result => result.Item == rare), Is.True);
        table.TryRoll(new System.Random(10), results, profile, Rules(3, 0));
        Assert.That(results, Is.Empty, "The variant chance also applies to lock bonus attempts.");
    }

    [Test]
    public void InvalidSettingsAreClampedAndNullLegacySettingsAreInitialized()
    {
        var settings = Rules(-10, float.NaN);
        settings.maxRollCount = 10000;
        settings.Normalize();
        Assert.That(settings.MinRolls, Is.Zero);
        Assert.That(settings.MaxRolls, Is.EqualTo(128));
        Assert.That(settings.SuccessChance, Is.EqualTo(100));
        settings.minRollCount = 10;
        settings.maxRollCount = 2;
        settings.rollSuccessChancePercent = -1;
        settings.Normalize();
        Assert.That(settings.MaxRolls, Is.EqualTo(10));
        Assert.That(settings.SuccessChance, Is.Zero);
        var variant = Asset<LootObjectSO>();
        Set(variant, "_rollSettings", null);
        Assert.That(variant.RollSettings.SuccessChance, Is.EqualTo(100));
    }

    [Test]
    public void ValidEmptyContainerIsCommittedOnceAndSurvivesSaveRestore()
    {
        var source = Variant(Family(Table()));
        Set(source, "_rollSettings", Rules(4, 0));
        var container = Container(source);
        container.BindRaidState("loot-test", 123, null, null);
        Assert.That(container.TryPrepareLoot(), Is.True);
        Assert.That(container.IsRolled, Is.True);
        var state = container.CaptureRaidState();
        Assert.That(state.rolled, Is.True);
        Assert.That(state.items, Is.Empty);
        source.RollSettings.rollSuccessChancePercent = 100;
        Assert.That(container.TryPrepareLoot(), Is.True);
        Assert.That(container.CaptureRaidState().items, Is.Empty, "Reopening must not reroll.");
        var restored = Container(source);
        var saved = JsonUtility.FromJson<RaidLootContainerSaveData>(JsonUtility.ToJson(state));
        restored.BindRaidState("loot-test", 123, saved, null);
        Assert.That(restored.TryPrepareLoot(), Is.True);
        Assert.That(restored.CaptureRaidState().items, Is.Empty, "Save restoration must not reroll.");
    }

    [Test]
    public void UnconfiguredPoolIsNotCommittedAsAnEmptyRoll()
    {
        var table = Asset<LootTableSO>();
        Assert.That(table.HasRollableItems(), Is.False);
        var container = Container(Variant(Family(table)));
        Assert.That(container.TryPrepareLoot(), Is.False);
        Assert.That(container.IsRolled, Is.False);
    }

    [Test]
    public void ZeroWeightEntriesDoNotCountAsConfiguredLoot()
    {
        var table = Table();
        var entry = table.Pool.FindTier(PlayerShopItemRarity.Normal).items[0];
        entry.useManualShare = true;
        entry.manualSharePercent = 0;
        Assert.That(table.HasRollableItems(), Is.False);
    }

    [Test]
    public void ItemBatchDropUsesOwnRarityAndSkipsDuplicatesWithoutEditingItems()
    {
        var pool = new LootObjectPool();
        var normal = Item(PlayerShopItemRarity.Normal);
        var rare = Item(PlayerShopItemRarity.Rare);
        var unique = Item(PlayerShopItemRarity.Unique);
        var legendary = Item(PlayerShopItemRarity.Legendary);
        string before = JsonUtility.ToJson(rare);
        int added = LootProgressionEditorWindow.AddItemsToWorldPool(pool,
            new ItemSO[] { normal, rare, null, unique, legendary, rare });
        Assert.That(added, Is.EqualTo(4));
        Assert.That(LootProgressionEditorWindow.AddItemsToWorldPool(pool, new[] { rare }), Is.Zero);
        foreach (var item in new[] { normal, rare, unique, legendary })
            Assert.That(pool.FindTier(item.Rarity).items.Single().item, Is.SameAs(item));
        Assert.That(JsonUtility.ToJson(rare), Is.EqualTo(before));
        Set(rare, "_rarity", PlayerShopItemRarity.Normal);
        Assert.That(LootProgressionEditorWindow.AddItemsToWorldPool(pool, new[] { rare }), Is.Zero,
            "Duplicates are detected across tiers, even when an item rarity changed.");
    }

    [Test]
    public void LegacyAssetWithoutVariantFieldsImportsWithCompatibleDefaults()
    {
        var template = Asset<LootObjectSO>();
        var script = MonoScript.FromScriptableObject(template);
        Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(script, out string guid, out long fileId), Is.True);
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/LootMakerLegacyTest.asset");
        try
        {
            // Deliberately omit the new fields, as in existing authored LootObjectSO assets.
            System.IO.File.WriteAllText(path,
                "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n" +
                "  m_ObjectHideFlags: 0\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n" +
                $"  m_Script: {{fileID: {fileId}, guid: {guid}, type: 3}}\n" +
                "  m_Name: LootMakerLegacyTest\n  _displayName: Legacy\n  _lootSlotCount: 9\n");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var loaded = AssetDatabase.LoadAssetAtPath<LootObjectSO>(path);
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.LootSlotCount, Is.EqualTo(9));
            Assert.That(loaded.SizeClass, Is.EqualTo(LootContainerSize.Custom));
            Assert.That(loaded.RollSettings.overrideTableRollCount, Is.False);
            Assert.That(loaded.RollSettings.SuccessChance, Is.EqualTo(100));
        }
        finally { AssetDatabase.DeleteAsset(path); }
    }

    [Test]
    public void VariantEditorPropertiesMatchSerializedFields()
    {
        var source = Asset<LootObjectSO>();
        source.ApplySizePreset(LootContainerSize.Large);
        using var serialized = new SerializedObject(source);
        Assert.That(serialized.FindProperty("_sizeClass").intValue, Is.EqualTo((int)LootContainerSize.Large));
        Assert.That(serialized.FindProperty("_lootSlotCount").intValue, Is.EqualTo(12));
        var settings = serialized.FindProperty("_rollSettings");
        Assert.That(settings.FindPropertyRelative("overrideTableRollCount").boolValue, Is.True);
        Assert.That(settings.FindPropertyRelative("minRollCount").intValue, Is.EqualTo(4));
        Assert.That(settings.FindPropertyRelative("maxRollCount").intValue, Is.EqualTo(8));
        Assert.That(settings.FindPropertyRelative("rollSuccessChancePercent").floatValue, Is.EqualTo(90));
        Assert.That(serialized.FindProperty("_lootObjectType"), Is.Not.Null);
    }

    [Test]
    public void ContainerCategoryShowsOnlyMatchingBoxesWithoutChangingBindings()
    {
        var backpackFamily = Family(Table());
        var medicalFamily = Family(Table());
        medicalFamily.AssignCategory(LootContainerCategory.Medical);
        var backpack = Variant(backpackFamily);
        var medical = Variant(medicalFamily);
        var unassigned = Asset<LootObjectSO>();
        var all = new[] { backpack, medical, unassigned, null };
        string before = JsonUtility.ToJson(backpack);
        Assert.That(LootProgressionEditorWindow.FilterWorldContainers(all, LootContainerCategory.Medical, false, null),
            Is.EqualTo(new[] { medical }));
        Assert.That(LootProgressionEditorWindow.FilterWorldContainers(all, LootContainerCategory.Backpack, false, null),
            Is.EqualTo(new[] { backpack }));
        Assert.That(LootProgressionEditorWindow.FilterWorldContainers(all, null, false, null).Count, Is.EqualTo(3));
        Assert.That(JsonUtility.ToJson(backpack), Is.EqualTo(before));
        Assert.That(medical.LootObjectType, Is.SameAs(medicalFamily));
    }

    [Test]
    public void UncategorizedAndUnassignedAreSeparateFilters()
    {
        var family = Family(Table());
        family.AssignCategory(LootContainerCategory.Uncategorized);
        var other = Variant(family);
        var unassigned = Asset<LootObjectSO>();
        var all = new[] { other, unassigned };
        Assert.That(LootProgressionEditorWindow.FilterWorldContainers(all, LootContainerCategory.Uncategorized, false, null),
            Is.EqualTo(new[] { other }));
        Assert.That(LootProgressionEditorWindow.FilterWorldContainers(all, null, true, null), Is.EqualTo(new[] { unassigned }));
        Assert.That(LootProgressionEditorWindow.FilterWorldContainers(null, null, false, null), Is.Empty);
    }

    [Test]
    public void CardTitleAndSearchUseActualPrefabNameAsWellAsLegacyNames()
    {
        var container = Asset<LootObjectSO>();
        container.name = "LootObject_MilitaryCase_05_WornGreenDamaged";
        Set(container, "_displayName", "Old Military Case");
        var prefab = new GameObject("MilitaryBox_verySmall");
        _created.Add(prefab);
        Set(container, "_worldPrefab", prefab);
        Assert.That(LootProgressionEditorWindow.GetContainerTitle(container), Is.EqualTo(prefab.name));
        foreach (string query in new[] { "verysmall", " WornGreenDamaged ", "old military" })
            Assert.That(LootProgressionEditorWindow.FilterWorldContainers(new[] { container }, null, false, query),
                Is.EqualTo(new[] { container }));
        Assert.That(LootProgressionEditorWindow.FilterWorldContainers(new[] { container }, null, false, "no such box"), Is.Empty);
        Set(container, "_worldPrefab", null);
        Assert.That(LootProgressionEditorWindow.GetContainerTitle(container), Is.EqualTo("Old Military Case"));
    }

    [Test]
    public void SharedWarningCountsActualTableUsersAcrossDifferentFamilies()
    {
        var shared = Table();
        var first = Variant(Family(shared));
        var second = Variant(Family(shared));
        var standalone = Asset<LootObjectSO>();
        Set(standalone, "_lootTable", shared);
        var unrelated = Variant(Family(Table()));
        var all = new[] { first, second, standalone, unrelated, null };
        Assert.That(LootProgressionEditorWindow.CountSharedTableUsers(all, shared), Is.EqualTo(3));
        Assert.That(LootProgressionEditorWindow.CountSharedTableUsers(all, null), Is.Zero);
        Assert.That(LootProgressionEditorWindow.CountSharedTableUsers(null, shared), Is.Zero);
    }

    [Test]
    public void SelectingCardChangesOnlyWorkbenchSelectionAndResetsItsTab()
    {
        var window = Asset<LootProgressionEditorWindow>();
        var first = Variant(Family(Table()));
        var second = Variant(Family(Table()));
        var select = typeof(LootProgressionEditorWindow).GetMethod("SelectContainer", BindingFlags.NonPublic | BindingFlags.Instance);
        var currentTable = typeof(LootProgressionEditorWindow).GetProperty("CurrentWorldTable", BindingFlags.NonPublic | BindingFlags.Instance);
        string firstBefore = JsonUtility.ToJson(first);
        select.Invoke(window, new object[] { first });
        Assert.That(currentTable.GetValue(window), Is.SameAs(first.LootTable));
        select.Invoke(window, new object[] { second });
        Assert.That(currentTable.GetValue(window), Is.SameAs(second.LootTable));
        Assert.That(typeof(LootProgressionEditorWindow).GetField("_worldTypeDetailTab", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(window).ToString(), Is.EqualTo("Items"));
        select.Invoke(window, new object[] { null });
        Assert.That(currentTable.GetValue(window), Is.Null);
        Assert.That(JsonUtility.ToJson(first), Is.EqualTo(firstBefore));
    }

    private T Asset<T>() where T : ScriptableObject
    {
        var asset = ScriptableObject.CreateInstance<T>();
        _created.Add(asset);
        return asset;
    }

    private ItemSO Item(PlayerShopItemRarity rarity)
    {
        var item = Asset<ValuableItemSO>();
        Set(item, "_rarity", rarity);
        return item;
    }

    private LootTableSO Table()
    {
        var table = Asset<LootTableSO>();
        Add(table, Item(PlayerShopItemRarity.Normal));
        return table;
    }

    private static void Add(LootTableSO table, ItemSO item) =>
        table.Pool.FindTier(item.Rarity).items.Add(new LootItemEntry { item = item, minAmount = 1, maxAmount = 3 });

    private LootObjectTypeSO Family(LootTableSO table)
    {
        var family = Asset<LootObjectTypeSO>();
        family.Configure("Test family", null, table);
        family.AssignCategory(LootContainerCategory.Backpack);
        return family;
    }

    private LootObjectSO Variant(LootObjectTypeSO family)
    {
        var variant = Asset<LootObjectSO>();
        variant.AssignLootObjectType(family);
        return variant;
    }

    private LootableObject Container(LootObjectSO source)
    {
        var go = new GameObject("Loot Maker test");
        _created.Add(go);
        var component = go.AddComponent<LootableObject>();
        component.ConfigureRuntimeSource(source);
        return component;
    }

    private static LootContainerRollSettings Rules(int count, float chance) => new()
    {
        overrideTableRollCount = true, minRollCount = count, maxRollCount = count,
        rollSuccessChancePercent = chance
    };

    private static void AssertRollsEqual(List<LootRollResult> first, List<LootRollResult> second)
    {
        Assert.That(second.Count, Is.EqualTo(first.Count));
        for (int i = 0; i < first.Count; i++)
        {
            Assert.That(second[i].Item, Is.SameAs(first[i].Item));
            Assert.That(second[i].Amount, Is.EqualTo(first[i].Amount));
            Assert.That(second[i].Rarity, Is.EqualTo(first[i].Rarity));
        }
    }

    private static void Set(object target, string name, object value)
    {
        for (Type type = target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (field == null) continue;
            field.SetValue(target, value);
            return;
        }
        throw new MissingFieldException(target.GetType().Name, name);
    }
}
