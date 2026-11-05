using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class MakerRegistrationIndexTests
{
    private readonly List<Object> _assets = new();
    private T Asset<T>(string name = "Test") where T : ScriptableObject
    { var value = ScriptableObject.CreateInstance<T>(); value.name = name; _assets.Add(value); return value; }
    private static GameObject Prefab() => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameScene/GameItemObject/BackPack/BagLv1.prefab");
    [TearDown] public void Cleanup() { foreach (var value in _assets) Object.DestroyImmediate(value); _assets.Clear(); }
    private static void Reference(Object owner, string property, Object value)
    { using var so = new SerializedObject(owner); so.FindProperty(property).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    private static void Array(Object owner, string property, Action<SerializedProperty> setup)
    { using var so = new SerializedObject(owner); var rows = so.FindProperty(property); rows.arraySize = 1; setup(rows.GetArrayElementAtIndex(0)); so.ApplyModifiedPropertiesWithoutUndo(); }
    private static LootRarityTier Tier(ItemSO item, PlayerShopItemRarity rarity = PlayerShopItemRarity.Normal) => new()
    { rarity = rarity, items = new() { new() { item = item, minAmount = 2, maxAmount = 4, useManualShare = true, manualSharePercent = 23 } } };
    private LootTableSO Table(ItemSO item)
    { var table = Asset<LootTableSO>(); table.Pool.FindTier(PlayerShopItemRarity.Normal).items.Add(Tier(item).items[0]); return table; }
    private LootObjectSO Box(LootTableSO table, string name = "Box")
    { var box = Asset<LootObjectSO>(name); box.AssignOwnLootTable(table); return box; }
    private MakerRegistrationIndex.Snapshot Build() => MakerRegistrationIndex.Build(_assets);

    [Test] public void EmptyItemIsUnregisteredAndMatchesOnlyTheUnregisteredFilter()
    {
        var item = Asset<ItemSO>(); var record = Build().For(item);
        Assert.That(record.LootLabel, Is.EqualTo("루팅 미등록")); Assert.That(record.Registered, Is.False);
        Assert.That(record.Matches(MakerRegistrationIndex.Filter.Unregistered), Is.True);
        Assert.That(record.Matches(MakerRegistrationIndex.Filter.Registered), Is.False);
        Assert.That(record.Matches(MakerRegistrationIndex.Filter.NeedsCheck), Is.False);
    }
    [Test] public void BoxReferencesCountOwnersNotRepeatedEntriesOrSharedTableAssets()
    {
        var item = Asset<ItemSO>(); var table = Table(item); Box(table, "A"); Box(table, "B");
        table.Pool.FindTier(PlayerShopItemRarity.Normal).items.Add(Tier(item).items[0]);
        var record = Build().For(item);
        Assert.That(record.BoxCount, Is.EqualTo(2)); Assert.That(record.Registered, Is.True);
        Assert.That(record.Uses.Any(u => u.Kind == MakerRegistrationIndex.Kind.UnusedTable), Is.False);
    }
    [Test] public void UnusedTablesAreWarningsNotActiveBoxRegistrations()
    {
        var item = Asset<ItemSO>(); Table(item); var record = Build().For(item);
        Assert.That(record.Registered, Is.False); Assert.That(record.BoxCount, Is.Zero);
        Assert.That(record.NeedsCheck, Is.True); Assert.That(record.Tooltip, Does.Contain("상자 연결 없음"));
    }
    [Test] public void DuplicateNamesNeverConflateDifferentItemObjects()
    {
        var first = Asset<ItemSO>("Same"); var second = Asset<ItemSO>("Same"); Box(Table(first));
        Assert.That(Build().For(first).Registered, Is.True); Assert.That(Build().For(second).Registered, Is.False);
    }
    [Test] public void EnemyStageDuplicatesCountOneEnemyTypeAndPreserveStageDetails()
    {
        var item = Asset<ItemSO>(); var enemy = Asset<EnemyShooterConfigSO>("Enemy"); var config = Asset<LootProgressionConfigSO>();
        var source = config.CreateSource(LootSourceKind.EnemyShooter); source.enemyShooterConfig = enemy;
        source.stagePools = new() { new() { stageIndex = 0, rarityTiers = new() { Tier(item) } }, new() { stageIndex = 3, rarityTiers = new() { Tier(item) } } };
        var record = Build().For(item);
        Assert.That(record.EnemyCount, Is.EqualTo(1)); Assert.That(record.Tooltip, Does.Contain("Stage 3"));
    }
    [Test] public void LegacyWorldSourcesAndUnboundEnemySourcesDoNotCountAsEnemyTypes()
    {
        var item = Asset<ItemSO>(); var config = Asset<LootProgressionConfigSO>();
        foreach (var kind in new[] { LootSourceKind.LootableObject, LootSourceKind.EnemyShooter })
        { var source = config.CreateSource(kind); source.stagePools = new() { new() { rarityTiers = new() { Tier(item) } } }; }
        Assert.That(Build().For(item).EnemyCount, Is.Zero);
    }
    [Test] public void GlobalValuableCatalogCountsAsGlobalNotOneFakeEnemyPerSource()
    {
        var item = Asset<ItemSO>(); var config = Asset<LootProgressionConfigSO>();
        Array(config, "_valuableLootItems", row => row.objectReferenceValue = item);
        var record = Build().For(item);
        Assert.That(record.GlobalLootCount, Is.EqualTo(1)); Assert.That(record.Registered, Is.True);
        Assert.That(record.EnemyCount, Is.Zero); Assert.That(record.BoxCount, Is.Zero);
    }
    [Test] public void RarityChangesWarnWithoutMovingEntriesOrChangingQuantitiesAndShares()
    {
        var item = Asset<ItemSO>(); var table = Table(item); Box(table);
        using (var so = new SerializedObject(item)) { so.FindProperty("_rarity").intValue = (int)PlayerShopItemRarity.Rare; so.ApplyModifiedPropertiesWithoutUndo(); }
        string before = EditorJsonUtility.ToJson(table); var record = Build().For(item);
        Assert.That(record.NeedsCheck, Is.True); Assert.That(record.Tooltip, Does.Contain("희귀도 불일치"));
        Assert.That(EditorJsonUtility.ToJson(table), Is.EqualTo(before));
    }
    [Test] public void ShopAndQuestReferencesDoNotImplyLootRegistration()
    {
        var item = Asset<ItemSO>(); var shop = Asset<ShopConfigSO>(); var quest = Asset<QuestDefinitionSO>();
        Array(shop, "_products", row => row.FindPropertyRelative("_item").objectReferenceValue = item);
        Array(quest, "_rewards", row => { row.FindPropertyRelative("_rewardType").intValue = (int)QuestRewardType.Item; row.FindPropertyRelative("_item").objectReferenceValue = item; });
        var record = Build().For(item);
        Assert.That(record.ShopCount, Is.EqualTo(1)); Assert.That(record.QuestCount, Is.EqualTo(1)); Assert.That(record.Registered, Is.False);
    }
    [Test] public void QuestCollectItemsAndSaleUnlocksAppearAsSeparateUsageRoles()
    {
        var item = Asset<ItemSO>(); var quest = Asset<QuestDefinitionSO>();
        Array(quest, "_objectives", row => { row.FindPropertyRelative("_objectiveType").intValue = (int)QuestObjectiveType.CollectItem; row.FindPropertyRelative("_targetItem").objectReferenceValue = item; });
        var shop = Asset<ShopConfigSO>();
        Array(shop, "_products", row => { row.FindPropertyRelative("_item").objectReferenceValue = item; row.FindPropertyRelative("_productId").stringValue = "p"; });
        Array(shop, "_unlockNodes", row => { row.FindPropertyRelative("_nodeId").stringValue = "g"; var ids = row.FindPropertyRelative("_unlockedProductIds"); ids.arraySize = 1; ids.GetArrayElementAtIndex(0).stringValue = "p"; });
        Array(quest, "_rewards", row => { row.FindPropertyRelative("_rewardType").intValue = (int)QuestRewardType.ShopUnlock; row.FindPropertyRelative("_shopConfig").objectReferenceValue = shop; row.FindPropertyRelative("_shopUnlockNodeId").stringValue = "g"; });
        var record = Build().For(item);
        Assert.That(record.QuestCount, Is.EqualTo(1)); Assert.That(record.Uses.Any(u => u.Kind == MakerRegistrationIndex.Kind.QuestSale), Is.True);
        Assert.That(record.Uses.Any(u => u.Kind == MakerRegistrationIndex.Kind.QuestGoal), Is.True);
    }
    [Test] public void PrefabLinksIncludeWorldEquippedAndComponentBasedWeaponModels()
    {
        var model = Prefab(); var item = Asset<BackpackItemSO>();
        Reference(item, "_worldVisualPrefab", model); Reference(item, "_equippedVisualPrefab", model);
        Assert.That(Build().ForPrefab(model), Is.EqualTo(new[] { item }));
        var source = AssetDatabase.FindAssets("t:RangedWeaponSO").Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<RangedWeaponSO>).First(w => w != null && w.WeaponViewPrefab != null);
        var weapon = Asset<RangedWeaponSO>(); Reference(weapon, "_weaponViewPrefab", source.WeaponViewPrefab);
        Assert.That(Build().ForPrefab(source.WeaponViewPrefab.gameObject).Contains(weapon), Is.True);
    }
    [Test] public void CurrentTableMembershipIsIndependentFromRegistrationElsewhere()
    {
        var item = Asset<ItemSO>(); var first = Table(item); Box(first); var second = Asset<LootTableSO>(); Box(second);
        Assert.That(Build().For(item).Registered, Is.True);
        Assert.That(MakerRegistrationIndex.Contains(first, item), Is.True); Assert.That(MakerRegistrationIndex.Contains(second, item), Is.False);
        Assert.That(MakerRegistrationIndex.ItemsIn(first).Count, Is.EqualTo(1));
    }
    [Test] public void RemovingOrRenamingReferencesIsReflectedWithoutPerItemFlags()
    {
        var item = Asset<ItemSO>(); var table = Table(item); var box = Box(table);
        box.name = "NewBox"; Assert.That(Build().For(item).Tooltip, Does.Contain("NewBox"));
        table.Pool.FindTier(PlayerShopItemRarity.Normal).items.Clear(); Assert.That(Build().For(item).Registered, Is.False);
    }
    [Test] public void ReadingAnUnnormalizedPoolNeverNormalizesOrDirtiesIt()
    {
        var item = Asset<ItemSO>(); var table = Table(item); Box(table);
        table.Pool.rarityTiers = new() { Tier(item, PlayerShopItemRarity.Unique) }; table.Pool.minRollCount = -3;
        // Set the nonstandard data after setup; querying Pool again would normalize it.
        using (var so = new SerializedObject(table)) { so.FindProperty("_pool.minRollCount").intValue = -3; so.FindProperty("_pool.rarityTiers").arraySize = 1; so.ApplyModifiedPropertiesWithoutUndo(); }
        string before = EditorJsonUtility.ToJson(table); bool dirty = EditorUtility.IsDirty(table);
        Build(); MakerRegistrationIndex.Contains(table, item); MakerRegistrationIndex.ItemsIn(table);
        Assert.That(EditorJsonUtility.ToJson(table), Is.EqualTo(before)); Assert.That(EditorUtility.IsDirty(table), Is.EqualTo(dirty));
    }
    [Test] public void BatchAddingCandidatesPreservesExistingDataAndSkipsDuplicates()
    {
        var existing = Asset<ItemSO>("A"); var added = Asset<ItemSO>("B"); var table = Table(existing);
        var entry = table.Pool.FindTier(PlayerShopItemRarity.Normal).items[0]; string before = JsonUtility.ToJson(entry);
        int count = LootProgressionEditorWindow.AddItemsToWorldPool(table.Pool, new[] { existing, added, added });
        Assert.That(count, Is.EqualTo(1)); Assert.That(JsonUtility.ToJson(entry), Is.EqualTo(before));
        Assert.That(MakerRegistrationIndex.ItemsIn(table).Count, Is.EqualTo(2));
    }
}
