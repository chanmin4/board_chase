using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ItemMakerCatalogViewTests
{
    private readonly List<ItemSO> _items = new();
    private string _scope;
    [SetUp] public void Setup() => _scope = "Tests-" + Guid.NewGuid().ToString("N");
    [TearDown] public void Cleanup()
    {
        foreach (var rarity in Enum.GetValues(typeof(PlayerShopItemRarity)).Cast<PlayerShopItemRarity>().Append((PlayerShopItemRarity)99))
            foreach (string scope in new[] { _scope, _scope + "Other" })
                EditorPrefs.DeleteKey(ItemMakerCatalogView.PreferenceKey(rarity, scope));
        foreach (ItemSO item in _items) UnityEngine.Object.DestroyImmediate(item);
        _items.Clear();
    }

    private ItemSO Item(string name, PlayerShopItemRarity rarity)
    {
        ItemSO item = ScriptableObject.CreateInstance<ItemSO>();
        item.name = name;
        SetRarity(item, rarity);
        _items.Add(item);
        return item;
    }

    private static void SetRarity(ItemSO item, PlayerShopItemRarity rarity)
    {
        using var so = new SerializedObject(item);
        so.FindProperty("_rarity").intValue = (int)rarity;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [Test] public void RarityOrderIsSemanticNotSerializedEnumOrder()
    {
        var items = new List<ItemSO>
        {
            Item("A", PlayerShopItemRarity.Legendary), Item("Z", PlayerShopItemRarity.Normal),
            Item("B", PlayerShopItemRarity.Unique), Item("Y", PlayerShopItemRarity.Rare)
        };
        items.Sort(ItemMakerCatalogView.Compare);
        CollectionAssert.AreEqual(new[] { PlayerShopItemRarity.Normal, PlayerShopItemRarity.Rare,
            PlayerShopItemRarity.Unique, PlayerShopItemRarity.Legendary }, items.Select(item => item.Rarity));
        Assert.That((int)PlayerShopItemRarity.Legendary, Is.EqualTo(2), "Serialized identities must not change.");
        Assert.That((int)PlayerShopItemRarity.Unique, Is.EqualTo(3));
    }

    [Test] public void WithinEachRarityNamesAreCaseInsensitiveAlphabetical()
    {
        var items = new List<ItemSO>
        {
            Item("zebra", PlayerShopItemRarity.Normal), Item("apple", PlayerShopItemRarity.Rare),
            Item("Banana", PlayerShopItemRarity.Normal), Item("apple", PlayerShopItemRarity.Normal)
        };
        items.Sort(ItemMakerCatalogView.Compare);
        CollectionAssert.AreEqual(new[] { "apple", "Banana", "zebra", "apple" }, items.Select(item => item.name));
    }

    [Test] public void IdenticalNamesInDifferentRaritiesRemainSeparateAndNullsSortLast()
    {
        ItemSO normal = Item("same", PlayerShopItemRarity.Normal), rare = Item("same", PlayerShopItemRarity.Rare);
        var items = new List<ItemSO> { null, rare, normal };
        items.Sort(ItemMakerCatalogView.Compare);
        CollectionAssert.AreEqual(new[] { normal, rare, null }, items);
    }

    [Test] public void LiveNameAndRarityEditsReorderWithoutRebuildingAssets()
    {
        ItemSO a = Item("A", PlayerShopItemRarity.Normal), b = Item("B", PlayerShopItemRarity.Normal);
        var items = new List<ItemSO> { a, b };
        a.name = "Z";
        items.Sort(ItemMakerCatalogView.Compare);
        Assert.That(items[0], Is.EqualTo(b));
        SetRarity(b, PlayerShopItemRarity.Legendary);
        items.Sort(ItemMakerCatalogView.Compare);
        Assert.That(items[0], Is.EqualTo(a));
    }

    [Test] public void NewGroupsStartExpandedAndPersistIndependently()
    {
        Assert.That(ItemMakerCatalogView.IsExpanded(PlayerShopItemRarity.Normal, _scope), Is.True);
        Assert.That(EditorPrefs.HasKey(ItemMakerCatalogView.PreferenceKey(PlayerShopItemRarity.Normal, _scope)), Is.False);
        ItemMakerCatalogView.SetExpanded(PlayerShopItemRarity.Normal, false, _scope);
        Assert.That(ItemMakerCatalogView.IsExpanded(PlayerShopItemRarity.Normal, _scope), Is.False);
        Assert.That(ItemMakerCatalogView.IsExpanded(PlayerShopItemRarity.Rare, _scope), Is.True);
        Assert.That(ItemMakerCatalogView.IsExpanded(PlayerShopItemRarity.Normal, _scope + "Other"), Is.True);
    }

    [Test] public void ExpandCollapseAllIncludesCurrentlyEmptyRarities()
    {
        ItemMakerCatalogView.SetAllExpanded(false, new[] { Item("N", PlayerShopItemRarity.Normal) }, _scope);
        foreach (PlayerShopItemRarity rarity in Enum.GetValues(typeof(PlayerShopItemRarity)))
            Assert.That(ItemMakerCatalogView.IsExpanded(rarity, _scope), Is.False);
        ItemMakerCatalogView.SetAllExpanded(true, Array.Empty<ItemSO>(), _scope);
        foreach (PlayerShopItemRarity rarity in Enum.GetValues(typeof(PlayerShopItemRarity)))
            Assert.That(ItemMakerCatalogView.IsExpanded(rarity, _scope), Is.True);
    }

    [Test] public void CollapsedRarityIsExcludedFromShiftRangeAndNextDragSelection()
    {
        ItemSO a = Item("A", PlayerShopItemRarity.Normal), b = Item("B", PlayerShopItemRarity.Rare),
            c = Item("C", PlayerShopItemRarity.Unique);
        ItemSO[] sorted = { a, b, c };
        var selection = new ItemMakerSelection();
        selection.Set(sorted, a);
        ItemMakerCatalogView.SetExpanded(PlayerShopItemRarity.Rare, false, _scope);
        // Merely folding keeps selection; a subsequent card click uses only expanded cards.
        CollectionAssert.AreEqual(sorted, selection.Items);
        var visible = ItemMakerCatalogView.ExpandedItems(sorted, _scope);
        selection.Retain(visible);
        selection.Click(c, visible, false, true, true);
        CollectionAssert.AreEqual(new[] { a, c }, selection.Items);
        selection.Click(a, visible, false, false, true);
        CollectionAssert.AreEqual(new[] { a, c }, selection.Items, "Dragging a selected visible card preserves the visible batch only.");
    }

    [Test] public void ControlClickStillTogglesWithinSortedVisibleCards()
    {
        var items = new List<ItemSO> { Item("Z", PlayerShopItemRarity.Normal), Item("A", PlayerShopItemRarity.Normal) };
        items.Sort(ItemMakerCatalogView.Compare);
        var selection = new ItemMakerSelection();
        selection.Click(items[0], items, false, false, true);
        selection.Click(items[1], items, true, false, true);
        CollectionAssert.AreEqual(items, selection.Items);
        selection.Click(items[0], items, true, false, true);
        CollectionAssert.AreEqual(new[] { items[1] }, selection.Items);
    }

    [Test] public void UnknownFutureRarityRemainsVisibleAfterKnownGroups()
    {
        var items = new List<ItemSO> { Item("A", (PlayerShopItemRarity)99), Item("Z", PlayerShopItemRarity.Legendary) };
        items.Sort(ItemMakerCatalogView.Compare);
        Assert.That(items[1].Rarity, Is.EqualTo((PlayerShopItemRarity)99));
        Assert.That(ItemMakerCatalogView.ExpandedItems(items, _scope).Count, Is.EqualTo(2));
    }
}
