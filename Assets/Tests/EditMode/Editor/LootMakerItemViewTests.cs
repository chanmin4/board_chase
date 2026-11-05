using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class LootMakerItemViewTests
{
    private readonly List<ItemSO> _items = new();
    private readonly List<LootTableSO> _tables = new();
    private readonly HashSet<string> _preferenceKeys = new();
    [TearDown] public void Cleanup()
    {
        foreach (var item in _items) Object.DestroyImmediate(item);
        foreach (var table in _tables) Object.DestroyImmediate(table);
        foreach (string key in _preferenceKeys) EditorPrefs.DeleteKey(key);
        _items.Clear();
        _tables.Clear();
        _preferenceKeys.Clear();
    }
    private T Item<T>(string name) where T : ItemSO
    {
        var item = ScriptableObject.CreateInstance<T>(); item.name = name; _items.Add(item); return item;
    }
    private static LootItemEntry Entry(ItemSO item) => new() { item = item, minAmount = 2, maxAmount = 4, useManualShare = true, manualSharePercent = 17 };

    private LootTableSO Table()
    {
        var table = ScriptableObject.CreateInstance<LootTableSO>(); _tables.Add(table); return table;
    }
    private void TrackPreference(LootTableSO table, PlayerShopItemRarity rarity, string category)
    {
        string key = LootMakerItemView.CategoryPreferenceKey(table, rarity, category);
        _preferenceKeys.Add(key); EditorPrefs.DeleteKey(key);
    }

    [Test] public void CategoryFoldoutsDefaultOpenAndAreIndependentPerTableRarityAndCategory()
    {
        var table = Table(); var other = Table();
        TrackPreference(table, PlayerShopItemRarity.Normal, "Armor");
        TrackPreference(table, PlayerShopItemRarity.Normal, "Passive");
        TrackPreference(table, PlayerShopItemRarity.Rare, "Armor");
        TrackPreference(other, PlayerShopItemRarity.Normal, "Armor");
        Assert.That(LootMakerItemView.IsCategoryExpanded(table, PlayerShopItemRarity.Normal, "Armor"), Is.True);
        LootMakerItemView.SetCategoryExpanded(table, PlayerShopItemRarity.Normal, "Armor", false);
        Assert.That(LootMakerItemView.IsCategoryExpanded(table, PlayerShopItemRarity.Normal, "Armor"), Is.False);
        Assert.That(LootMakerItemView.IsCategoryExpanded(table, PlayerShopItemRarity.Normal, "Passive"), Is.True);
        Assert.That(LootMakerItemView.IsCategoryExpanded(table, PlayerShopItemRarity.Rare, "Armor"), Is.True);
        Assert.That(LootMakerItemView.IsCategoryExpanded(other, PlayerShopItemRarity.Normal, "Armor"), Is.True);
    }
    [Test] public void BulkCategoryFoldoutsPreserveEveryEntryAndProbability()
    {
        var table = Table();
        table.Pool.FindTier(PlayerShopItemRarity.Normal).items.Add(Entry(Item<ArmorItemSO>("Armor")));
        table.Pool.FindTier(PlayerShopItemRarity.Normal).items.Add(Entry(Item<PassiveItemSO>("Passive")));
        table.Pool.FindTier(PlayerShopItemRarity.Rare).items.Add(Entry(Item<ArmorItemSO>("Rare armor")));
        foreach (var tier in table.Pool.rarityTiers)
            foreach (var row in LootMakerItemView.SortedRows(tier)) TrackPreference(table, tier.rarity, row.CategoryKey);
        string before = JsonUtility.ToJson(table.Pool);
        foreach (bool expanded in new[] { false, true })
        {
            LootMakerItemView.SetAllCategoriesExpanded(table, expanded);
            foreach (var tier in table.Pool.rarityTiers)
                foreach (var row in LootMakerItemView.SortedRows(tier))
                    Assert.That(LootMakerItemView.IsCategoryExpanded(table, tier.rarity, row.CategoryKey), Is.EqualTo(expanded));
            Assert.That(JsonUtility.ToJson(table.Pool), Is.EqualTo(before));
        }
    }
    [Test] public void CategoryFoldoutKeysSurviveTableRenameAndUseStableCategoryIdentity()
    {
        var table = Table(); var item = Item<PassiveItemSO>("A");
        var row = LootMakerItemView.SortedRows(new LootRarityTier { items = new List<LootItemEntry> { Entry(item) } })[0];
        Assert.That(row.CategoryKey, Is.EqualTo("Passive"));
        TrackPreference(table, PlayerShopItemRarity.Normal, row.CategoryKey);
        string key = LootMakerItemView.CategoryPreferenceKey(table, PlayerShopItemRarity.Normal, row.CategoryKey);
        LootMakerItemView.SetCategoryExpanded(table, PlayerShopItemRarity.Normal, row.CategoryKey, false);
        table.name = "Renamed";
        Assert.That(LootMakerItemView.CategoryPreferenceKey(table, PlayerShopItemRarity.Normal, row.CategoryKey), Is.EqualTo(key));
        Assert.That(EditorPrefs.GetBool(key, true), Is.False);
    }

    [Test] public void TierOrderUsesNormalRareUniqueLegendaryWithoutMutatingSource()
    {
        var source = new List<LootRarityTier> {
            new() { rarity = PlayerShopItemRarity.Legendary }, null,
            new() { rarity = PlayerShopItemRarity.Unique }, new() { rarity = PlayerShopItemRarity.Normal },
            new() { rarity = PlayerShopItemRarity.Rare }, new() { rarity = (PlayerShopItemRarity)99 } };
        var before = source.ToArray();
        CollectionAssert.AreEqual(new[] { PlayerShopItemRarity.Normal, PlayerShopItemRarity.Rare,
            PlayerShopItemRarity.Unique, PlayerShopItemRarity.Legendary, (PlayerShopItemRarity)99 },
            LootMakerItemView.SortedTiers(source).Select(t => t.rarity));
        CollectionAssert.AreEqual(before, source);
    }
    [Test] public void CategoryThenCaseInsensitiveNameAndOriginalIndexAreStable()
    {
        var tier = new LootRarityTier { items = new List<LootItemEntry> {
            Entry(Item<PassiveItemSO>("A passive")), Entry(Item<ArmorItemSO>("zulu")),
            Entry(Item<ArmorItemSO>("Beta")), Entry(Item<ArmorItemSO>("alpha")) } };
        var rows = LootMakerItemView.SortedRows(tier);
        CollectionAssert.AreEqual(new[] { 3, 2, 1, 0 }, rows.Select(r => r.SourceIndex));
        CollectionAssert.AreEqual(new[] { "Armor", "Armor", "Armor", "Passives" }, rows.Select(r => r.CategoryLabel));
        Assert.That(rows[0].Entry, Is.SameAs(tier.items[3]));
    }
    [Test] public void ViewingKeepsDataProbabilitiesAndSerializedDrawOrderUnchanged()
    {
        var tier = new LootRarityTier { items = new List<LootItemEntry> {
            Entry(Item<PassiveItemSO>("A")), Entry(Item<ArmorItemSO>("Z")) } };
        string before = JsonUtility.ToJson(tier);
        var rows = LootMakerItemView.SortedRows(tier);
        Assert.That(rows[0].SourceIndex, Is.EqualTo(1));
        Assert.That(JsonUtility.ToJson(tier), Is.EqualTo(before));
        foreach (var row in rows)
        {
            Assert.That(row.Entry.minAmount, Is.EqualTo(2));
            Assert.That(row.Entry.maxAmount, Is.EqualTo(4));
            Assert.That(row.Entry.manualSharePercent, Is.EqualTo(17));
        }
    }
    [Test] public void DeletingSortedRowTargetsOriginalEntryAndEditingKeepsItsIdentity()
    {
        var passive = Entry(Item<PassiveItemSO>("A")); var armor = Entry(Item<ArmorItemSO>("Z"));
        var tier = new LootRarityTier { items = new List<LootItemEntry> { passive, armor, armor } };
        var rows = LootMakerItemView.SortedRows(tier);
        Assert.That(rows[1].SourceIndex, Is.EqualTo(2));
        rows[0].Entry.minAmount = 3;
        Assert.That(tier.items[1].minAmount, Is.EqualTo(3));
        tier.items.RemoveAt(rows[1].SourceIndex);
        CollectionAssert.AreEqual(new[] { passive, armor }, tier.items);
    }
    [Test] public void MissingItemsStayVisibleAtEndAndNullEntriesAreSafe()
    {
        var missing = Entry(null); var armor = Entry(Item<ArmorItemSO>("A"));
        var tier = new LootRarityTier { items = new List<LootItemEntry> { null, missing, armor } };
        var rows = LootMakerItemView.SortedRows(tier);
        CollectionAssert.AreEqual(new[] { 2, 1 }, rows.Select(r => r.SourceIndex));
        Assert.That(rows.Last().CategoryLabel, Is.EqualTo("Missing Items"));
        Assert.That(LootMakerItemView.SortedRows(null), Is.Empty);
        Assert.That(LootMakerItemView.SortedTiers(null), Is.Empty);
        Assert.That(tier.items.Count, Is.EqualTo(3));
    }
    [Test] public void LiveRenamesReorderAndItemMakerCategoriesAreReused()
    {
        var a = Item<ArmorItemSO>("A"); var b = Item<ArmorItemSO>("B");
        var tier = new LootRarityTier { items = new List<LootItemEntry> { Entry(a), Entry(b) } };
        a.name = "Z";
        var rows = LootMakerItemView.SortedRows(tier);
        Assert.That(rows[0].Entry.item, Is.SameAs(b));
        Assert.That(rows[0].CategoryOrder, Is.EqualTo(ItemMakerWindow.LootCategoryOrder(b)));
        Assert.That(rows[0].CategoryLabel, Is.EqualTo(ItemMakerWindow.LootCategoryLabel(b)));
    }
    [Test] public void IdenticalNamesKeepInsertionOrderAndAssignedTierIsNotChanged()
    {
        var a = Entry(Item<ArmorItemSO>("Same")); var b = Entry(Item<ArmorItemSO>("Same"));
        var tier = new LootRarityTier { rarity = PlayerShopItemRarity.Legendary, items = new List<LootItemEntry> { a, b } };
        CollectionAssert.AreEqual(new[] { 0, 1 }, LootMakerItemView.SortedRows(tier).Select(r => r.SourceIndex));
        Assert.That(tier.rarity, Is.EqualTo(PlayerShopItemRarity.Legendary));
        Assert.That(a.item.Rarity, Is.EqualTo(PlayerShopItemRarity.Normal));
    }

    [Test] public void CandidatesUseSharedCategoriesAndNaturalNameOrder()
    {
        var passive = Item<PassiveItemSO>("Passive");
        var armor10 = Item<ArmorItemSO>("ArmorLV10"); var armor2 = Item<ArmorItemSO>("ArmorLV2");
        var groups = LootMakerItemView.CandidateGroups(new ItemSO[] { passive, armor10, null, armor2, armor2 }, null, false, "");
        CollectionAssert.AreEqual(new[] { "Armor", "Passive" }, groups.Select(g => g.Key));
        CollectionAssert.AreEqual(new[] { armor2, armor10 }, groups[0].Items);
        Assert.That(groups[0].Label, Is.EqualTo(ItemMakerWindow.LootCategoryLabel(armor2)));
    }
    [Test] public void CandidatesSortByRarityBeforeNaturalNameWithoutChangingSource()
    {
        var legendary = Item<ArmorItemSO>("A Legendary"); var rareZ = Item<ArmorItemSO>("Z Rare");
        var normal10 = Item<ArmorItemSO>("ArmorLV10"); var unique = Item<ArmorItemSO>("A Unique");
        var rareA = Item<ArmorItemSO>("A Rare"); var normal2 = Item<ArmorItemSO>("ArmorLV2");
        foreach (var pair in new[] { (legendary, PlayerShopItemRarity.Legendary), (rareZ, PlayerShopItemRarity.Rare),
            (unique, PlayerShopItemRarity.Unique), (rareA, PlayerShopItemRarity.Rare) })
        {
            using var serialized = new SerializedObject(pair.Item1);
            serialized.FindProperty("_rarity").intValue = (int)pair.Item2;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        var source = new ItemSO[] { legendary, rareZ, normal10, unique, rareA, normal2 };
        var before = source.Select(EditorJsonUtility.ToJson).ToArray();
        var group = LootMakerItemView.CandidateGroups(source, null, false, "", "Armor").Single();
        CollectionAssert.AreEqual(new ItemSO[] { normal2, normal10, rareA, rareZ, unique, legendary }, group.Items);
        CollectionAssert.AreEqual(new ItemSO[] { legendary, rareZ, normal10, unique, rareA, normal2 }, source);
        CollectionAssert.AreEqual(before, source.Select(EditorJsonUtility.ToJson));
        var filtered = LootMakerItemView.CandidateGroups(source, new HashSet<ItemSO> { rareA }, true, "rare", "Armor").Single();
        CollectionAssert.AreEqual(new[] { rareZ }, filtered.Items);
    }
    [Test] public void CandidateCategoryAndSearchAndRegistrationFiltersCompose()
    {
        var existing = Item<ArmorItemSO>("ArmorLV1"); var armor = Item<ArmorItemSO>("ArmorLV2");
        var passive = Item<PassiveItemSO>("PassiveLV2"); var registered = new HashSet<ItemSO> { existing };
        var groups = LootMakerItemView.CandidateGroups(new ItemSO[] { passive, existing, armor }, registered, true, "  lv2  ", "Armor");
        Assert.That(groups.Count, Is.EqualTo(1)); CollectionAssert.AreEqual(new[] { armor }, groups[0].Items);
        Assert.That(LootMakerItemView.CandidateGroups(new[] { existing }, registered, false, "", "Armor")[0].Items, Contains.Item(existing));
    }
    [Test] public void CandidateSearchAlsoMatchesCategoryLabels()
    {
        var armor = Item<ArmorItemSO>("Unrelated name"); var passive = Item<PassiveItemSO>("Other name");
        var groups = LootMakerItemView.CandidateGroups(new ItemSO[] { armor, passive }, null, false, "armor");
        CollectionAssert.AreEqual(new[] { armor }, groups.SelectMany(g => g.Items));
    }
    [Test] public void CandidateWeaponAndAmmoLabelsAreNotAmbiguous()
    {
        var gun = Item<RangedWeaponSO>("Gun"); var bullet = Item<BulletSO>("Ammo");
        var groups = LootMakerItemView.CandidateGroups(new ItemSO[] { gun, bullet }, null, false, "");
        Assert.That(groups.Single(g => g.Items.Contains(gun)).Label, Does.StartWith("Weapons / "));
        Assert.That(groups.Single(g => g.Items.Contains(bullet)).Label, Does.StartWith("Bullets / "));
    }
    [Test] public void OnlyExpandedCandidatesAreEligibleForVisibleSelection()
    {
        var armor = Item<ArmorItemSO>("A"); var passive = Item<PassiveItemSO>("B");
        var groups = LootMakerItemView.CandidateGroups(new ItemSO[] { armor, passive }, null, false, "");
        Assert.That(LootMakerItemView.ExpandedCandidates(groups, _ => false), Is.Empty);
        CollectionAssert.AreEqual(new[] { armor }, LootMakerItemView.ExpandedCandidates(groups, key => key == "Armor"));
        var selected = new HashSet<ItemSO> { passive };
        selected.UnionWith(LootMakerItemView.ExpandedCandidates(groups, key => key == "Armor"));
        CollectionAssert.AreEquivalent(new ItemSO[] { armor, passive }, selected);
    }
    [Test] public void CandidateBrowsingNeverChangesItemsOrTableEntries()
    {
        var armor = Item<ArmorItemSO>("Armor"); var passive = Item<PassiveItemSO>("Passive"); var table = Table();
        table.Pool.FindTier(PlayerShopItemRarity.Normal).items.Add(Entry(armor));
        string before = EditorJsonUtility.ToJson(table), itemBefore = EditorJsonUtility.ToJson(armor);
        var source = new ItemSO[] { passive, armor }; var registered = MakerRegistrationIndex.ItemsIn(table);
        foreach (bool onlyUnregistered in new[] { true, false })
            LootMakerItemView.CandidateGroups(source, registered, onlyUnregistered, "", "Armor");
        CollectionAssert.AreEqual(new ItemSO[] { passive, armor }, source);
        Assert.That(EditorJsonUtility.ToJson(table), Is.EqualTo(before));
        Assert.That(EditorJsonUtility.ToJson(armor), Is.EqualTo(itemBefore));
        CollectionAssert.AreEqual(new[] { armor }, registered);
    }
    [Test] public void EmptyOrMissingCandidateCategoriesProduceNoResults()
    {
        Assert.That(LootMakerItemView.CandidateGroups(null, null, true, null), Is.Empty);
        var armor = Item<ArmorItemSO>("A");
        Assert.That(LootMakerItemView.CandidateGroups(new[] { armor }, null, false, "", "NoSuchCategory"), Is.Empty);
        Assert.That(LootMakerItemView.CandidateGroups(new[] { armor }, null, false, "NoMatch"), Is.Empty);
    }
}
