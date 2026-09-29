using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ItemMakerBatchTests
{
    private string _folder;

    [SetUp]
    public void SetUp()
    {
        _folder = "Assets/__ItemMakerBatchTests_" + Guid.NewGuid().ToString("N");
        AssetDatabase.CreateFolder("Assets", Path.GetFileName(_folder));
    }

    [TearDown]
    public void TearDown() => AssetDatabase.DeleteAsset(_folder);

    [Test]
    public void ControlClickTogglesAndDraggingSelectedCardKeepsTheGroup()
    {
        ItemSO a = Asset<PassiveItemSO>("A"), b = Asset<PassiveItemSO>("B"), c = Asset<PassiveItemSO>("C");
        ItemSO[] visible = { a, b, c };
        ItemMakerSelection selection = new();
        selection.Click(a, visible, false, false, true);
        selection.Click(b, visible, true, false, true);
        CollectionAssert.AreEquivalent(new[] { a, b }, selection.Items);
        Assert.AreEqual(b, selection.Active);
        selection.Click(a, visible, false, false, true);
        CollectionAssert.AreEquivalent(new[] { a, b }, selection.Items, "MouseDown on the group must preserve the drag payload.");
        Assert.AreEqual(a, selection.Active);
        selection.Click(b, visible, true, false, true);
        CollectionAssert.AreEqual(new[] { a }, selection.Items);
        selection.Click(a, visible, true, false, true);
        Assert.IsEmpty(selection.Items);
        Assert.IsNull(selection.Active);
    }

    [Test]
    public void ShiftSelectsVisibleRangeAndFilteringRemovesHiddenSelections()
    {
        ItemSO[] items = { Asset<PassiveItemSO>("A"), Asset<PassiveItemSO>("B"), Asset<PassiveItemSO>("C") };
        ItemMakerSelection selection = new();
        selection.Click(items[0], items, false, false, true);
        selection.Click(items[2], items, false, true, true);
        CollectionAssert.AreEqual(items, selection.Items);
        Assert.IsTrue(selection.Retain(new[] { items[1] }));
        CollectionAssert.AreEqual(new[] { items[1] }, selection.Items);
        Assert.AreEqual(items[1], selection.Active);
        selection.Click(items[0], items, false, false, false);
        CollectionAssert.AreEqual(new[] { items[0] }, selection.Items, "A completed plain click selects only one card.");
    }

    [Test]
    public void BatchMoveAndRestorePreserveBothIdentitiesAndLoadedLootReferences()
    {
        ItemSO a = Asset<PassiveItemSO>("A"), b = Asset<PassiveItemSO>("B");
        string[] paths = { AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b) };
        string[] guids = paths.Select(AssetDatabase.AssetPathToGUID).ToArray();
        LootTableSO loot = Asset<LootTableSO>("Loot");
        foreach (ItemSO item in new[] { a, b })
            loot.Pool.rarityTiers[0].items.Add(new LootItemEntry { item = item, minAmount = 1, maxAmount = 1 });
        EditorUtility.SetDirty(loot);
        AssetDatabase.SaveAssetIfDirty(loot);

        ItemSO[] moved = ItemCategoryMoveUtility.MoveBatch(new[] { Request(a), Request(b) }, out var batch, false);
        Assert.AreEqual(2, batch.moves.Count);
        for (int i = 0; i < 2; i++)
        {
            Assert.IsInstanceOf<MedicalItemSO>(moved[i]);
            Assert.AreEqual(guids[i], moved[i].Guid);
            Assert.AreEqual(moved[i], loot.Pool.rarityTiers[0].items[i].item);
        }
        ItemSO[] restored = ItemCategoryMoveUtility.RestoreBatch(batch);
        for (int i = 0; i < 2; i++)
        {
            Assert.IsInstanceOf<PassiveItemSO>(restored[i]);
            Assert.AreEqual(paths[i], AssetDatabase.GetAssetPath(restored[i]));
            Assert.AreEqual(guids[i], AssetDatabase.AssetPathToGUID(paths[i]));
            Assert.AreEqual(restored[i], loot.Pool.rarityTiers[0].items[i].item);
        }
    }

    [Test]
    public void CollisionInLaterItemPreventsEveryMove()
    {
        ItemSO a = Asset<PassiveItemSO>("A"), b = Asset<PassiveItemSO>("B");
        AssetDatabase.CreateFolder(_folder, "Medical");
        Asset<MedicalItemSO>("Medical/B");
        Assert.Throws<IOException>(() => ItemCategoryMoveUtility.MoveBatch(new[] { Request(a), Request(b) }, out _, false));
        Assert.AreEqual(_folder + "/A.asset", AssetDatabase.GetAssetPath(a));
        Assert.AreEqual(_folder + "/B.asset", AssetDatabase.GetAssetPath(b));
        Assert.IsFalse(File.Exists(_folder + "/Medical/A.asset"));
    }

    [Test]
    public void IdenticalNamesInsideSelectionAreBlockedWithoutOverwriting()
    {
        AssetDatabase.CreateFolder(_folder, "One");
        AssetDatabase.CreateFolder(_folder, "Two");
        ItemSO a = Asset<PassiveItemSO>("One/Item"), b = Asset<PassiveItemSO>("Two/Item");
        Assert.Throws<IOException>(() => ItemCategoryMoveUtility.MoveBatch(new[] { Request(a), Request(b) }, out _, false));
        Assert.IsTrue(a != null && b != null);
        Assert.IsFalse(AssetDatabase.IsValidFolder(_folder + "/Medical"));
    }

    [Test]
    public void OneChangedItemBlocksTheWholeBatchRestore()
    {
        ItemSO[] moved = ItemCategoryMoveUtility.MoveBatch(new[] { Request(Asset<PassiveItemSO>("A")), Request(Asset<PassiveItemSO>("B")) }, out var batch, false);
        using (SerializedObject so = new(moved[0]))
        {
            so.FindProperty("_purchasePrice").intValue = 888;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        AssetDatabase.SaveAssetIfDirty(moved[0]);
        Assert.Throws<InvalidOperationException>(() => ItemCategoryMoveUtility.RestoreBatch(batch));
        Assert.AreEqual(2, batch.moves.Count);
        foreach (ItemSO item in moved)
        {
            Assert.IsInstanceOf<MedicalItemSO>(item);
            StringAssert.Contains("/Medical/", AssetDatabase.GetAssetPath(item));
        }
        Assert.AreEqual(888, moved[0].PurchasePrice);
    }

    [Test]
    public void IncompatibleLaterItemBlocksEarlierValidItem()
    {
        ItemSO first = Asset<PassiveItemSO>("A");
        RangedWeaponSO weapon = Asset<RangedWeaponSO>("Gun");
        QuestDefinitionSO quest = Asset<QuestDefinitionSO>("Quest");
        using (SerializedObject so = new(quest))
        {
            SerializedProperty objectives = so.FindProperty("_objectives");
            objectives.arraySize = 1;
            objectives.GetArrayElementAtIndex(0).FindPropertyRelative("_requiredWeapon").objectReferenceValue = weapon;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        AssetDatabase.SaveAssetIfDirty(quest);
        Assert.Throws<InvalidOperationException>(() => ItemCategoryMoveUtility.MoveBatch(new[] { Request(first), Request(weapon) }, out _, false));
        Assert.AreEqual(_folder + "/A.asset", AssetDatabase.GetAssetPath(first));
        Assert.IsInstanceOf<RangedWeaponSO>(weapon);
    }

    private ItemCategoryMoveUtility.MoveRequest Request(ItemSO item) => new()
    {
        item = item, destinationType = typeof(MedicalItemSO), destinationFolder = _folder + "/Medical"
    };

    private T Asset<T>(string name) where T : ScriptableObject
    {
        T value = ScriptableObject.CreateInstance<T>();
        value.name = Path.GetFileName(name);
        AssetDatabase.CreateAsset(value, _folder + "/" + name + ".asset");
        return value;
    }
}
