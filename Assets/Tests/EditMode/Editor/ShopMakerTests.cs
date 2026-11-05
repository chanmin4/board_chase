using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>메모리상의 SO만 사용한다. 실제 상점/퀘스트/아이템, 저장 게임 및 씬은 변경하지 않는다.</summary>
public sealed partial class ShopMakerTests
{
    private readonly List<Object> _objects = new();
    private ShopConfigSO _shop;
    private ItemSO _item;
    private QuestDefinitionSO _quest;
    [SetUp] public void SetUp()
    {
        Undo.IncrementCurrentGroup();
        _shop = Make<ShopConfigSO>(); _item = Make<ItemSO>(); _item.name = "Test Item";
        _quest = Make<QuestDefinitionSO>();
        var money = Make<CurrencyItemSO>();
        Change(_shop, so => so.FindProperty("_defaultCurrencyItem").objectReferenceValue = money);
    }
    [TearDown] public void TearDown()
    {
        foreach (var obj in _objects) if (obj != null) { Undo.ClearUndo(obj); Object.DestroyImmediate(obj); }
        _objects.Clear();
    }
    private T Make<T>() where T : ScriptableObject { var obj = ScriptableObject.CreateInstance<T>(); _objects.Add(obj); return obj; }
    private static void Change(Object obj, Action<SerializedObject> edit)
    { var so = new SerializedObject(obj); edit(so); so.ApplyModifiedPropertiesWithoutUndo(); }
    private static string Json(Object obj) => EditorJsonUtility.ToJson(obj);
    private string Group() => ShopMakerAuthoring.CreateGroup(_shop, "Medical Unlock");
    private string Product() { int i = ShopMakerAuthoring.Register(_shop, _item); return _shop.Products[i].ProductId; }

    [Test] public void RegistrationHasExplicitIndependentDefaults()
    {
        Product();
        Change(_shop, so =>
        {
            var p = so.FindProperty("_products").GetArrayElementAtIndex(0);
            p.FindPropertyRelative("_availableByDefault").boolValue = false;
            p.FindPropertyRelative("_stock").intValue = 8;
            p.FindPropertyRelative("_priceOverride").intValue = 99;
            var trade = p.FindPropertyRelative("_tradeRequirements"); trade.arraySize = 1;
            trade.GetArrayElementAtIndex(0).FindPropertyRelative("_item").objectReferenceValue = _item;
            trade.GetArrayElementAtIndex(0).FindPropertyRelative("_amount").intValue = 2;
        });
        var other = Make<ItemSO>(); int added = ShopMakerAuthoring.Register(_shop, other);
        var p2 = _shop.Products[added];
        Assert.That(p2.Item, Is.SameAs(other)); Assert.That(p2.Stock, Is.EqualTo(1));
        Assert.That(p2.BundleAmount, Is.EqualTo(1)); Assert.That(p2.OfferWeight, Is.EqualTo(1));
        Assert.That(p2.AvailableByDefault, Is.True); Assert.That(p2.TradeRequirements.Count, Is.Zero);
        Assert.That(p2.ResolvePrice(), Is.EqualTo(other.PurchasePrice));
        Assert.That(_shop.Products[0].ResolvePrice(), Is.EqualTo(99));
    }
    [Test] public void DuplicateRegistrationIsReadOnly()
    { Product(); string before = Json(_shop); Assert.That(ShopMakerAuthoring.Register(_shop, _item), Is.Zero); Assert.That(Json(_shop), Is.EqualTo(before)); }
    [Test] public void IDsNeverReuseArrayPositions()
    {
        string first = Product(); ShopMakerAuthoring.RemoveProduct(_shop, 0); string next = Product();
        Assert.That(next, Is.Not.EqualTo(first));
        string group = Group(); ShopMakerAuthoring.RemoveEmptyGroup(_shop, group, Array.Empty<QuestDefinitionSO>());
        Assert.That(Group(), Is.Not.EqualTo(group));
    }
    [Test] public void NewGroupDoesNotCopyPreviousLinks()
    {
        string a = Group(); ShopMakerAuthoring.SetMembership(_shop, a, Product(), true); string b = Group();
        ShopMakerAuthoring.SetPrerequisite(_shop, b, a, true); string c = Group();
        var last = _shop.UnlockNodes.Last(); Assert.That(last.NodeId, Is.EqualTo(c));
        Assert.That(last.UnlockedProductIds, Is.Empty); Assert.That(last.PrerequisiteNodeIds, Is.Empty);
    }
    [Test] public void QuestLinkAppendsAndPreservesOriginalRewardIndices()
    {
        Change(_quest, so =>
        {
            var rewards = so.FindProperty("_rewards"); rewards.arraySize = 1;
            var r = rewards.GetArrayElementAtIndex(0); r.FindPropertyRelative("_rewardType").enumValueIndex = 0;
            r.FindPropertyRelative("_item").objectReferenceValue = _item; r.FindPropertyRelative("_itemAmount").intValue = 7;
        });
        string first = JsonUtility.ToJson(_quest.Rewards[0]), group = Group(), shop = Json(_shop);
        Assert.That(ShopMakerAuthoring.LinkQuest(_shop, group, _quest), Is.True);
        Assert.That(_quest.Rewards.Length, Is.EqualTo(2)); Assert.That(JsonUtility.ToJson(_quest.Rewards[0]), Is.EqualTo(first));
        var linked = _quest.Rewards[1]; Assert.That(linked.RewardType, Is.EqualTo(QuestRewardType.ShopUnlock));
        Assert.That(linked.ShopConfig, Is.SameAs(_shop)); Assert.That(linked.ShopUnlockNodeId, Is.EqualTo(group));
        Assert.That(linked.Item, Is.Null); Assert.That(Json(_shop), Is.EqualTo(shop));
    }
    [Test] public void QuestLinkIsIdempotentAndConfigSpecific()
    {
        string group = Group(); ShopMakerAuthoring.LinkQuest(_shop, group, _quest); string before = Json(_quest);
        Assert.That(ShopMakerAuthoring.LinkQuest(_shop, group, _quest), Is.False); Assert.That(Json(_quest), Is.EqualTo(before));
        var other = Make<ShopConfigSO>();
        Assert.That(ShopMakerAuthoring.QuestsFor(other, group, new[] { _quest }), Is.Empty);
    }
    [Test] public void LinkingMissingGroupFailsWithoutWriting()
    {
        string before = Json(_quest);
        Assert.Throws<ArgumentException>(() => ShopMakerAuthoring.LinkQuest(_shop, "missing", _quest));
        Assert.That(Json(_quest), Is.EqualTo(before));
    }
    [Test] public void MultipleQuestsUseTheSameAuthoredGroup()
    {
        string group = Group(); var q2 = Make<QuestDefinitionSO>();
        ShopMakerAuthoring.LinkQuest(_shop, group, _quest); ShopMakerAuthoring.LinkQuest(_shop, group, q2);
        Assert.That(ShopMakerAuthoring.QuestsFor(_shop, group, new[] { _quest, q2 }).Count(), Is.EqualTo(2));
        Assert.That(_shop.UnlockNodes.Count, Is.EqualTo(1));
    }
    [Test] public void MembershipDoesNotSilentlyChangeAvailability()
    {
        string product = Product(), group = Group(); ShopMakerAuthoring.SetMembership(_shop, group, product, true);
        Assert.That(_shop.Products[0].AvailableByDefault, Is.True);
        ShopMakerAuthoring.SetMembership(_shop, group, product, true);
        Assert.That(_shop.UnlockNodes[0].UnlockedProductIds.Count, Is.EqualTo(1));
    }
    [Test] public void ProductCanBelongToMultipleGroups()
    {
        string product = Product(), a = Group(), b = Group();
        ShopMakerAuthoring.SetMembership(_shop, a, product, true); ShopMakerAuthoring.SetMembership(_shop, b, product, true);
        Assert.That(ShopMakerAuthoring.GroupsFor(_shop, product).Count(), Is.EqualTo(2));
        ShopMakerAuthoring.SetMembership(_shop, a, product, false);
        Assert.That(ShopMakerAuthoring.GroupsFor(_shop, product).Single().NodeId, Is.EqualTo(b));
    }
    [Test] public void PrerequisiteCyclesAndSelfLinksAreRejected()
    {
        string a = Group(), b = Group(), c = Group();
        ShopMakerAuthoring.SetPrerequisite(_shop, b, a, true); ShopMakerAuthoring.SetPrerequisite(_shop, c, b, true);
        string before = Json(_shop);
        Assert.Throws<InvalidOperationException>(() => ShopMakerAuthoring.SetPrerequisite(_shop, a, c, true));
        Assert.Throws<InvalidOperationException>(() => ShopMakerAuthoring.SetPrerequisite(_shop, a, a, true));
        Assert.That(Json(_shop), Is.EqualTo(before));
    }
    [Test] public void PrerequisiteRemovalDoesNotRemoveNodes()
    {
        string a = Group(), b = Group(); ShopMakerAuthoring.SetPrerequisite(_shop, b, a, true);
        ShopMakerAuthoring.SetPrerequisite(_shop, b, a, false);
        Assert.That(_shop.UnlockNodes.Count, Is.EqualTo(2)); Assert.That(_shop.UnlockNodes[1].PrerequisiteNodeIds, Is.Empty);
    }
    [Test] public void ReferencedGroupCannotBeDeleted()
    {
        string group = Group(); ShopMakerAuthoring.LinkQuest(_shop, group, _quest);
        Assert.Throws<InvalidOperationException>(() => ShopMakerAuthoring.RemoveEmptyGroup(_shop, group, new[] { _quest }));
        Assert.That(_shop.UnlockNodes.Single().NodeId, Is.EqualTo(group));
    }
    [Test] public void RemovingRegistrationKeepsAssetsRewardsAndOtherProducts()
    {
        string product = Product(), group = Group(); var other = Make<ItemSO>(); ShopMakerAuthoring.Register(_shop, other);
        ShopMakerAuthoring.SetMembership(_shop, group, product, true); ShopMakerAuthoring.LinkQuest(_shop, group, _quest);
        string quest = Json(_quest), item = Json(_item), nodeId = _shop.UnlockNodes[0].NodeId;
        ShopMakerAuthoring.RemoveProduct(_shop, 0);
        Assert.That(_shop.Products.Single().Item, Is.SameAs(other)); Assert.That(_shop.UnlockNodes.Single().NodeId, Is.EqualTo(nodeId));
        Assert.That(_shop.UnlockNodes[0].UnlockedProductIds, Is.Empty); Assert.That(Json(_quest), Is.EqualTo(quest)); Assert.That(Json(_item), Is.EqualTo(item));
    }
    [Test] public void BasePriceWritesExistingItemFieldOnly()
    {
        Product(); string config = Json(_shop);
        ShopMakerAuthoring.SetBasePrice(_item, 123);
        Assert.That(_item.PurchasePrice, Is.EqualTo(123)); Assert.That(_shop.Products[0].ResolvePrice(), Is.EqualTo(123));
        Assert.That(Json(_shop), Is.EqualTo(config));
    }
    [Test] public void PriceOverrideAndBarterRemainAuthoritative()
    {
        Product(); Change(_shop, so =>
        {
            var p = so.FindProperty("_products").GetArrayElementAtIndex(0); p.FindPropertyRelative("_priceOverride").intValue = 44;
            var rows = p.FindPropertyRelative("_tradeRequirements"); rows.arraySize = 1;
            rows.GetArrayElementAtIndex(0).FindPropertyRelative("_item").objectReferenceValue = _item;
            rows.GetArrayElementAtIndex(0).FindPropertyRelative("_amount").intValue = 3;
        });
        ShopMakerAuthoring.SetBasePrice(_item, 200);
        Assert.That(_shop.Products[0].ResolvePrice(), Is.EqualTo(44));
        var trade = _shop.ResolveTradeRequirements(_shop.Products[0]).Single(); Assert.That(trade.Item, Is.SameAs(_item)); Assert.That(trade.Amount, Is.EqualTo(3));
    }
    [Test] public void NegativeBasePriceIsClamped()
    { ShopMakerAuthoring.SetBasePrice(_item, -5); Assert.That(_item.PurchasePrice, Is.Zero); }
    [Test] public void SortingUsesRarityCategoryNameWithoutReorderingData()
    {
        _item.name = "Zed"; Product();
        var armor = Make<ArmorItemSO>(); armor.name = "Armor"; ShopMakerAuthoring.Register(_shop, armor);
        var rare = Make<ItemSO>(); rare.name = "AAA Rare"; Change(rare, so => so.FindProperty("_rarity").enumValueIndex = 1); ShopMakerAuthoring.Register(_shop, rare);
        var alpha = Make<ItemSO>(); alpha.name = "Alpha"; ShopMakerAuthoring.Register(_shop, alpha);
        string before = Json(_shop);
        Assert.That(ShopMakerAuthoring.SortedProducts(_shop), Is.EqualTo(new[] { 1, 3, 0, 2 }));
        Assert.That(Json(_shop), Is.EqualTo(before));
        Assert.That(ShopMakerAuthoring.SortedProducts(_shop, "Armor"), Is.EqualTo(new[] { 1 }));
        Assert.That(ShopMakerAuthoring.SortedProducts(_shop, "", "alpha"), Is.EqualTo(new[] { 3 }));
    }
    [Test] public void ValidationFindsMissingQuestAndIsReadOnly()
    {
        Product(); string group = Group(); ShopMakerAuthoring.SetMembership(_shop, group, _shop.Products[0].ProductId, true);
        string before = Json(_shop);
        var messages = ShopMakerAuthoring.Validate(_shop, new[] { _quest });
        Assert.That(messages.Any(m => m.Contains("no quest")), Is.True);
        Assert.That(messages.Any(m => m.Contains("available from start")), Is.True);
        Assert.That(Json(_shop), Is.EqualTo(before));
    }
    [Test] public void ValidGatedProductPassesValidation()
    {
        string p = Product(), g = Group(); ShopMakerAuthoring.SetMembership(_shop, g, p, true); ShopMakerAuthoring.LinkQuest(_shop, g, _quest);
        Change(_shop, so => so.FindProperty("_products").GetArrayElementAtIndex(0).FindPropertyRelative("_availableByDefault").boolValue = false);
        Assert.That(ShopMakerAuthoring.Validate(_shop, new[] { _quest }), Is.Empty);
    }
    [Test] public void MissingMoneyAndZeroWeightAreReported()
    {
        Product(); Change(_shop, so => { so.FindProperty("_defaultCurrencyItem").objectReferenceValue = null; so.FindProperty("_products").GetArrayElementAtIndex(0).FindPropertyRelative("_offerWeight").floatValue = 0; });
        var issues = ShopMakerAuthoring.Validate(_shop, new[] { _quest });
        Assert.That(issues.Any(m => m.Contains("invalid payment")), Is.True); Assert.That(issues.Any(m => m.Contains("weight is 0")), Is.True);
    }
    [Test, Category("GlobalEditorUndo")] public void BasePriceUndoRedoRestoresOriginal()
    {
        int original = _item.PurchasePrice; Undo.IncrementCurrentGroup(); ShopMakerAuthoring.SetBasePrice(_item, 543); Undo.FlushUndoRecordObjects();
        Undo.PerformUndo(); Assert.That(_item.PurchasePrice, Is.EqualTo(original));
        Undo.PerformRedo(); Assert.That(_item.PurchasePrice, Is.EqualTo(543));
    }
    [Test, Category("GlobalEditorUndo")] public void QuestAppendUndoRestoresExistingRewards()
    {
        string g = Group(); Undo.IncrementCurrentGroup(); string before = Json(_quest);
        ShopMakerAuthoring.LinkQuest(_shop, g, _quest); Undo.FlushUndoRecordObjects();
        Undo.PerformUndo(); Assert.That(Json(_quest), Is.EqualTo(before));
    }
}
