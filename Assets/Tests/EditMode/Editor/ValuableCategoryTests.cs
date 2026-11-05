using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

public sealed class ValuableCategoryTests
{
    private readonly List<Object> _temporary = new();
    private ValuableCategoryCatalogSO _catalog;
    private object _previous;
    private static FieldInfo ActiveField => typeof(ValuableCategoryCatalogSO).GetField("_active", BindingFlags.Static | BindingFlags.NonPublic);
    private T Temp<T>() where T : ScriptableObject
    { var result = ScriptableObject.CreateInstance<T>(); _temporary.Add(result); return result; }
    [SetUp] public void Setup()
    {
        _previous = ActiveField.GetValue(null);
        _catalog = Temp<ValuableCategoryCatalogSO>();
        _catalog.categories.Add(new ValuableCategoryCatalogSO.Category { id = "test-tools", label = "도구" });
        _catalog.categories.Add(new ValuableCategoryCatalogSO.Category { id = "test-building", label = "건설자재" });
        ActiveField.SetValue(null, _catalog);
    }
    [TearDown] public void Cleanup()
    {
        ActiveField.SetValue(null, _previous);
        foreach (var item in _temporary) { Undo.ClearUndo(item); Object.DestroyImmediate(item); }
        _temporary.Clear();
    }
    private ValuableItemSO Item(string id)
    {
        var item = Temp<ValuableItemSO>();
        ValuableCategoryEditorWindow.Assign(new[] { item }, id);
        return item;
    }
    [Test] public void LabelRenameKeepsLootKeyAndItemGuid()
    {
        var item = Item("test-tools"); var guid = item.Guid;
        string key = LootItemCategories.GetKey(item);
        _catalog.categories[0].label = "변경된 도구";
        Assert.That(LootItemCategories.GetKey(item), Is.EqualTo(key));
        Assert.That(item.ValuableCategoryLabel, Is.EqualTo("변경된 도구"));
        Assert.That(item.Guid, Is.EqualTo(guid));
    }
    [Test] public void AssignmentSupportsUndoWithoutTouchingItemIdentity()
    {
        var item = Item("test-tools"); var guid = item.Guid;
        Undo.IncrementCurrentGroup();
        ValuableCategoryEditorWindow.Assign(new[] { item }, "test-building");
        Undo.FlushUndoRecordObjects(); Undo.PerformUndo();
        Assert.That(item.ValuableCategoryId, Is.EqualTo("test-tools"));
        Assert.That(item.Guid, Is.EqualTo(guid));
    }
    [Test] public void OldAggregateWeightSplitsWithoutChangingAnyItemChance()
    {
        var table = Temp<LootTableSO>(); table.EnsurePool();
        var tier = table.Pool.FindTier(PlayerShopItemRarity.Normal);
        var a = new LootItemEntry { item = Item("test-tools"), useManualShare = true, manualSharePercent = 20 };
        var b = new LootItemEntry { item = Item("test-building") };
        var armor = new LootItemEntry { item = Temp<ArmorItemSO>() };
        tier.items.AddRange(new[] { a, b, armor });
        var settings = table.Pool.EnsureCategorySettings(tier.rarity); settings.useCustomCategoryWeights = true;
        settings.SetWeight("Valuable", 40); settings.SetWeight("Armor", 60);
        var before = table.GetCategorySelection(tier);
        Assert.That(before.ItemPercentInTier(a), Is.EqualTo(8).Within(.001));
        Assert.That(before.ItemPercentInTier(b), Is.EqualTo(32).Within(.001));
        Assert.That(before.ItemPercentInTier(armor), Is.EqualTo(60).Within(.001));
        // Setting a child to its current value materializes, but does not retune, siblings.
        LootMakerAuthoring.SetCategoryWeight(table, tier, "Valuable:test-tools", 8);
        var after = table.GetCategorySelection(tier);
        Assert.That(after.ItemPercentInTier(a), Is.EqualTo(8).Within(.001));
        Assert.That(after.ItemPercentInTier(b), Is.EqualTo(32).Within(.001));
        LootMakerAuthoring.SetCategoryWeight(table, tier, "Valuable:test-building", 0);
        Assert.That(table.GetCategorySelection(tier).ItemPercentInTier(b), Is.Zero);
    }
    [Test] public void CandidateBrowserSeparatesValuableSubcategories()
    {
        var a = Item("test-tools"); var b = Item("test-building");
        var groups = LootMakerItemView.CandidateGroups(new ItemSO[] { b, a }, null, false, "");
        Assert.That(groups.Count, Is.EqualTo(2));
        Assert.That(groups[0].Label, Does.Contain("도구"));
        Assert.That(LootMakerItemView.CandidateGroups(new ItemSO[] { a, b }, null, false, "", "Valuable:test-building")[0].Items, Is.EqualTo(new[] { b }));
        _catalog.categories.RemoveAt(0);
        Assert.That(LootItemCategories.GetKey(a), Is.EqualTo("Valuable"));
        Assert.That(a.ValuableCategoryLabel, Is.EqualTo("미분류"));
    }
    [Test] public void EditingChildWeightPreservesMixedManualSiblingRatios()
    {
        var table = Temp<LootTableSO>(); table.EnsurePool();
        var tier = table.Pool.FindTier(PlayerShopItemRarity.Normal);
        var a = new LootItemEntry { item = Item("test-tools"), useManualShare = true, manualSharePercent = 20 };
        var b = new LootItemEntry { item = Item("test-tools") };
        var c = new LootItemEntry { item = Item("test-building") };
        tier.items.AddRange(new[] { a, b, c });
        var settings = table.Pool.EnsureCategorySettings(tier.rarity); settings.useCustomCategoryWeights = true;
        settings.SetWeight("Valuable", 40);
        var before = table.GetCategorySelection(tier);
        LootMakerAuthoring.SetCategoryWeight(table, tier, "Valuable:test-building", 16);
        var after = table.GetCategorySelection(tier);
        Assert.That(after.ItemPercentInTier(a), Is.EqualTo(before.ItemPercentInTier(a)).Within(.001));
        Assert.That(after.ItemPercentInTier(b), Is.EqualTo(before.ItemPercentInTier(b)).Within(.001));
        Assert.That(after.ItemPercentInTier(c), Is.EqualTo(before.ItemPercentInTier(c)).Within(.001));
    }
}
