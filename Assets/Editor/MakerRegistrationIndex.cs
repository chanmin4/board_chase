using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Editor-only, read-only reverse references. No per-item registration flags or runtime changes.
internal static class MakerRegistrationIndex
{
    internal enum Kind { Box, Enemy, GlobalLoot, UnusedTable, Shop, QuestReward, QuestGoal, QuestSale, QuestWeapon }
    internal enum Filter { All, Unregistered, Registered, NeedsCheck }
    internal static readonly string[] FilterLabels = { "전체", "루팅 미등록", "루팅 등록됨", "확인 필요" };
    internal sealed class Usage
    {
        internal Kind Kind;
        internal Object Owner;
        internal LootTableSO Table;
        internal LootSourceDefinition Source;
        internal int Stage;
        internal string Label, Warning;
    }
    internal sealed class Record
    {
        internal readonly List<Usage> Uses = new();
        internal int BoxCount => Uses.Where(u => u.Kind == Kind.Box).Select(u => u.Owner).Distinct().Count();
        internal int EnemyCount => Uses.Where(u => u.Kind == Kind.Enemy).Select(u => u.Source.enemyShooterConfig).Distinct().Count();
        internal int GlobalLootCount => Uses.Where(u => u.Kind == Kind.GlobalLoot).Select(u => u.Owner).Distinct().Count();
        internal int ShopCount => Uses.Where(u => u.Kind == Kind.Shop).Select(u => u.Owner).Distinct().Count();
        internal int QuestCount => Uses.Where(u => u.Kind == Kind.QuestReward || u.Kind == Kind.QuestGoal || u.Kind == Kind.QuestSale || u.Kind == Kind.QuestWeapon).Select(u => u.Owner).Distinct().Count();
        internal bool Registered => BoxCount + EnemyCount + GlobalLootCount > 0;
        internal bool NeedsCheck => Uses.Any(u => !string.IsNullOrEmpty(u.Warning));
        internal string LootLabel => Registered ? $"루팅 등록 · 상자 {BoxCount}곳 / 적 {EnemyCount}종" +
            (GlobalLootCount > 0 ? $" / 공통목록 {GlobalLootCount}개" : "") : "루팅 미등록";
        internal string Tooltip => string.Join("\n", Uses.Select(u => u.Label + (string.IsNullOrEmpty(u.Warning) ? "" : " — " + u.Warning)).Distinct());
        internal bool Matches(Filter filter) => filter switch
        {
            Filter.Unregistered => !Registered, Filter.Registered => Registered,
            Filter.NeedsCheck => NeedsCheck, _ => true
        };
    }
    internal sealed class Snapshot
    {
        private readonly Dictionary<ItemSO, Record> _records = new();
        private readonly Dictionary<GameObject, List<ItemSO>> _prefabs = new();
        private readonly Dictionary<GameObject, List<Object>> _prefabRecords = new();
        internal ItemSO[] Items = Array.Empty<ItemSO>();
        internal Record For(ItemSO item) => item != null && _records.TryGetValue(item, out var value) ? value : new Record();
        internal IReadOnlyList<ItemSO> ForPrefab(GameObject prefab)
        {
            prefab = CanonicalPrefab(prefab);
            return prefab != null && _prefabs.TryGetValue(prefab, out var values) ? values : Array.Empty<ItemSO>();
        }
        internal IReadOnlyList<Object> RecordsForPrefab(GameObject prefab)
        {
            prefab = CanonicalPrefab(prefab);
            return prefab != null && _prefabRecords.TryGetValue(prefab, out var values) ? values : Array.Empty<Object>();
        }
        internal void AddPrefabRecord(GameObject prefab, Object owner)
        {
            prefab = CanonicalPrefab(prefab);
            if (prefab == null) return;
            if (!_prefabRecords.TryGetValue(prefab, out var values)) _prefabRecords[prefab] = values = new();
            if (!values.Contains(owner)) values.Add(owner);
        }
        internal void Add(ItemSO item, Usage usage)
        {
            if (item == null) return;
            if (!_records.TryGetValue(item, out var value)) _records[item] = value = new Record();
            value.Uses.Add(usage);
        }
        internal void AddPrefab(GameObject prefab, ItemSO item)
        {
            prefab = CanonicalPrefab(prefab);
            if (prefab == null) return;
            if (!_prefabs.TryGetValue(prefab, out var values)) _prefabs[prefab] = values = new();
            if (!values.Contains(item)) values.Add(item);
        }
    }

    private static Snapshot _snapshot;
    private static Object[] _assets;
    private static bool _dirty = true;
    private static double _nextScan;
    // Never load unrelated vendor ScriptableObjects just to inspect game registrations.
    internal static readonly Type[] IndexedTypes =
    {
        typeof(ItemSO), typeof(SectorPresetSO), typeof(LootObjectSO), typeof(LootTableSO),
        typeof(LootProgressionConfigSO), typeof(ShopConfigSO), typeof(QuestDefinitionSO)
    };
    internal static bool IsIndexedType(Type type) => type != null && IndexedTypes.Any(t => t.IsAssignableFrom(type));
    internal static Object[] ScanAssets() => IndexedTypes
        .SelectMany(type => AssetDatabase.FindAssets("t:" + type.Name, new[] { "Assets" }))
        .Distinct().Select(AssetDatabase.GUIDToAssetPath)
        .Select(AssetDatabase.LoadMainAssetAtPath)
        .Where(asset => asset != null && IsIndexedType(asset.GetType())).ToArray();
    static MakerRegistrationIndex()
    {
        EditorApplication.projectChanged += () => { _assets = null; Invalidate(); };
        Undo.undoRedoPerformed += Invalidate;
        Undo.postprocessModifications += changes => { Invalidate(); return changes; };
    }
    internal static void Invalidate() => _dirty = true;
    internal static Snapshot Current
    {
        get
        {
            // One cached scan, not a project search per card. Poll loaded authoring data too:
            // direct list edits may not emit projectChanged until the author clicks Save.
            if (_snapshot == null || _dirty || EditorApplication.timeSinceStartup >= _nextScan)
            {
                _assets ??= ScanAssets();
                _snapshot = Build(_assets); _dirty = false; _nextScan = EditorApplication.timeSinceStartup + 1;
            }
            return _snapshot;
        }
    }

    internal static Snapshot Build(IEnumerable<Object> assets)
    {
        var all = assets.Where(a => a != null).Distinct().ToArray();
        var result = new Snapshot { Items = all.OfType<ItemSO>().OrderBy(i => i.name, Comparer<string>.Create(EditorUtility.NaturalCompare)).ToArray() };
        foreach (var item in result.Items)
        {
            using var serialized = new SerializedObject(item);
            foreach (string field in new[] { "_worldVisualPrefab", "_equippedVisualPrefab", "_heldViewPrefab", "_weaponViewPrefab" })
            {
                var model = serialized.FindProperty(field)?.objectReferenceValue;
                result.AddPrefab(model is Component component ? component.gameObject : model as GameObject, item);
            }
        }
        foreach (var preset in all.OfType<SectorPresetSO>())
            if (preset.SectorPrefab != null) result.AddPrefabRecord(preset.SectorPrefab.gameObject, preset);
        var usedTables = new HashSet<LootTableSO>();
        foreach (var box in all.OfType<LootObjectSO>())
        {
            result.AddPrefabRecord(box.WorldPrefab, box);
            var table = box.LootTable;
            if (table == null) continue;
            usedTables.Add(table);
            ReadTable(table, (item, rarity) => result.Add(item, new Usage { Kind = Kind.Box, Owner = box, Table = table,
                Label = "상자: " + (box.WorldPrefab != null ? box.WorldPrefab.name : box.name), Warning = RarityWarning(item, rarity) }));
        }
        foreach (var table in all.OfType<LootTableSO>().Where(t => !usedTables.Contains(t)))
            ReadTable(table, (item, rarity) => result.Add(item, new Usage { Kind = Kind.UnusedTable, Owner = table, Table = table,
                Label = "미사용 테이블: " + table.name, Warning = "상자 연결 없음" + (RarityWarning(item, rarity) is string warning && warning.Length > 0 ? " · " + warning : "") }));
        foreach (var config in all.OfType<LootProgressionConfigSO>())
        {
            foreach (var item in config.ValuableLootItems ?? Array.Empty<ItemSO>())
                result.Add(item, new Usage { Kind = Kind.GlobalLoot, Owner = config, Stage = -1,
                    Label = "공통 귀중품 후보: " + config.name });
            foreach (var source in config.Sources ?? Array.Empty<LootSourceDefinition>())
            {
                if (source == null || source.sourceKind != LootSourceKind.EnemyShooter || source.enemyShooterConfig == null) continue;
                foreach (var pool in source.stagePools ?? new())
                    foreach (var tier in pool?.rarityTiers ?? new())
                        foreach (var entry in tier?.items ?? new())
                            if (entry?.item != null)
                                result.Add(entry.item, new Usage { Kind = Kind.Enemy, Owner = config, Source = source, Stage = pool.stageIndex,
                                    Label = "적: " + source.enemyShooterConfig.name + (pool.stageIndex == 0 ? " · 공통" : " · Stage " + pool.stageIndex),
                                    Warning = RarityWarning(entry.item, tier.rarity) });
            }
        }
        foreach (var shop in all.OfType<ShopConfigSO>())
            foreach (var product in shop.Products ?? Array.Empty<ShopConfigSO.ProductDefinition>())
                if (product?.Item != null) result.Add(product.Item, new Usage { Kind = Kind.Shop, Owner = shop, Label = "상점 상품: " + shop.name });
        foreach (var quest in all.OfType<QuestDefinitionSO>())
        {
            foreach (var reward in quest.Rewards)
            {
                if (reward != null && reward.RewardType == QuestRewardType.Item)
                    result.Add(reward.Item, new Usage { Kind = Kind.QuestReward, Owner = quest, Label = "퀘스트 보상: " + quest.name });
                if (reward != null && reward.RewardType == QuestRewardType.ShopUnlock && reward.ShopConfig != null && reward.ShopConfig.TryGetNode(reward.ShopUnlockNodeId, out var node))
                    foreach (var product in reward.ShopConfig.Products.Where(p => p != null && node.UnlockedProductIds.Contains(p.ProductId)))
                        result.Add(product.Item, new Usage { Kind = Kind.QuestSale, Owner = quest, Label = "퀘스트 판매 해금: " + quest.name });
            }
            foreach (var objective in quest.Objectives)
            {
                if (objective != null && objective.ObjectiveType == QuestObjectiveType.CollectItem)
                    result.Add(objective.TargetItem, new Usage { Kind = Kind.QuestGoal, Owner = quest, Label = "퀘스트 수집 목표: " + quest.name });
                if (objective != null && objective.ObjectiveType != QuestObjectiveType.CollectItem)
                    result.Add(objective.RequiredWeapon, new Usage { Kind = Kind.QuestWeapon, Owner = quest, Label = "퀘스트 지정 무기: " + quest.name });
            }
        }
        return result;
    }
    private static string RarityWarning(ItemSO item, PlayerShopItemRarity rarity) => item.Rarity == rarity ? "" : "희귀도 불일치";

    // Do not use LootTableSO.Pool here: its getter normalizes/mutates the asset in memory.
    private static void ReadTable(LootTableSO table, Action<ItemSO, PlayerShopItemRarity> visitor)
    {
        using var serialized = new SerializedObject(table);
        var tiers = serialized.FindProperty("_pool.rarityTiers");
        if (tiers == null || !tiers.isArray) return;
        for (int i = 0; i < tiers.arraySize; i++)
        {
            var tier = tiers.GetArrayElementAtIndex(i);
            var rarity = (PlayerShopItemRarity)tier.FindPropertyRelative("rarity").intValue;
            var entries = tier.FindPropertyRelative("items");
            for (int j = 0; j < entries.arraySize; j++)
                if (entries.GetArrayElementAtIndex(j).FindPropertyRelative("item").objectReferenceValue is ItemSO item) visitor(item, rarity);
        }
    }
    internal static bool Contains(LootTableSO table, ItemSO target)
    {
        bool found = false;
        if (table != null && target != null) ReadTable(table, (item, rarity) => found |= item == target);
        return found;
    }
    internal static HashSet<ItemSO> ItemsIn(LootTableSO table)
    {
        var result = new HashSet<ItemSO>();
        if (table != null) ReadTable(table, (item, rarity) => result.Add(item));
        return result;
    }
    internal static GameObject CanonicalPrefab(GameObject prefab)
    {
        if (prefab == null) return null;
        var source = EditorUtility.IsPersistent(prefab) ? prefab : PrefabUtility.GetCorrespondingObjectFromSource(prefab);
        string path = AssetDatabase.GetAssetPath(source);
        return string.IsNullOrEmpty(path) ? prefab : AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? prefab;
    }
    internal static bool Contains(LootStagePool pool, ItemSO item) => item != null &&
        (pool?.rarityTiers?.Any(t => t?.items?.Any(e => e?.item == item) == true) ?? false);
}
