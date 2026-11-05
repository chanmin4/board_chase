using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

public sealed partial class ShopMakerTests
{
    private int SaleReward(QuestDefinitionSO quest = null, ShopConfigSO shop = null, string id = "")
    {
        quest ??= _quest; shop ??= _shop;
        int index = quest.Rewards.Length;
        Change(quest, so =>
        {
            var rows = so.FindProperty("_rewards"); rows.arraySize++;
            var row = rows.GetArrayElementAtIndex(index);
            row.FindPropertyRelative("_rewardType").intValue = (int)QuestRewardType.ShopUnlock;
            row.FindPropertyRelative("_shopConfig").objectReferenceValue = shop;
            row.FindPropertyRelative("_shopUnlockNodeId").stringValue = id;
        });
        return index;
    }
    [Test] public void DirectSaleItemsCreateInternalConnectionOnlyOnExplicitAdd()
    {
        int reward = SaleReward(); string before = Json(_shop);
        Assert.That(ShopMakerAuthoring.ProductsForQuest(_shop, _quest), Is.Empty);
        Assert.That(ShopMakerAuthoring.ShopRewardSummary(_quest.Rewards[reward]), Does.Contain("상품"));
        Assert.That(Json(_shop), Is.EqualTo(before)); Assert.That(_shop.UnlockNodes, Is.Empty);
        ShopMakerAuthoring.AddRewardProducts(_quest, reward, new[] { _item });
        Assert.That(_quest.Rewards.Length, Is.EqualTo(1));
        Assert.That(_quest.Rewards[0].ShopUnlockNodeId, Is.EqualTo(_shop.UnlockNodes.Single().NodeId));
        Assert.That(_shop.Products.Single().AvailableByDefault, Is.False);
        Assert.That(ShopMakerAuthoring.ProductsForQuest(_shop, _quest).Single().Item, Is.SameAs(_item));
    }
    [Test] public void DirectSaleRegistrationIsIdempotentAndDoesNotChangeItemSO()
    {
        int reward = SaleReward(); string item = Json(_item);
        ShopMakerAuthoring.AddRewardProducts(_quest, reward, new[] { _item, _item });
        string quest = Json(_quest), shop = Json(_shop);
        ShopMakerAuthoring.AddRewardProducts(_quest, reward, new[] { _item });
        Assert.That(Json(_quest), Is.EqualTo(quest)); Assert.That(Json(_shop), Is.EqualTo(shop)); Assert.That(Json(_item), Is.EqualTo(item));
        Assert.That(_shop.Products.Count, Is.EqualTo(1)); Assert.That(_shop.UnlockNodes.Single().UnlockedProductIds.Count, Is.EqualTo(1));
    }
    [Test] public void ExistingSaleConnectionKeepsNodeAndOriginalRewardIndices()
    {
        Change(_quest, so => { var rows = so.FindProperty("_rewards"); rows.arraySize = 1;
            rows.GetArrayElementAtIndex(0).FindPropertyRelative("_item").objectReferenceValue = _item;
            rows.GetArrayElementAtIndex(0).FindPropertyRelative("_itemAmount").intValue = 7; });
        string existing = JsonUtilityForReward(0), group = Group(); int index = SaleReward(id: group);
        ShopMakerAuthoring.AddRewardProducts(_quest, index, new[] { _item });
        Assert.That(_quest.Rewards.Length, Is.EqualTo(2)); Assert.That(JsonUtilityForReward(0), Is.EqualTo(existing));
        Assert.That(_quest.Rewards[1].ShopUnlockNodeId, Is.EqualTo(group)); Assert.That(_shop.UnlockNodes.Count, Is.EqualTo(1));
    }
    private string JsonUtilityForReward(int index) => UnityEngine.JsonUtility.ToJson(_quest.Rewards[index]);
    [Test] public void EachNewSaleRewardHasIndependentConnection()
    {
        int a = SaleReward(), b = SaleReward(); var other = Make<ItemSO>();
        ShopMakerAuthoring.AddRewardProducts(_quest, a, new[] { _item });
        ShopMakerAuthoring.AddRewardProducts(_quest, b, new[] { other });
        Assert.That(_quest.Rewards[a].ShopUnlockNodeId, Is.Not.EqualTo(_quest.Rewards[b].ShopUnlockNodeId));
        Assert.That(_shop.UnlockNodes.Count, Is.EqualTo(2));
        Assert.That(_shop.UnlockNodes[0].UnlockedProductIds, Has.Count.EqualTo(1));
        Assert.That(_shop.UnlockNodes[1].UnlockedProductIds, Has.Count.EqualTo(1));
    }
    [Test] public void InvalidSaleRewardOrNullItemsNeverPartiallyWrite()
    {
        int index = SaleReward(); string before = Json(_shop), quest = Json(_quest);
        Assert.Throws<ArgumentException>(() => ShopMakerAuthoring.AddRewardProducts(_quest, index, new ItemSO[] { _item, null }));
        Assert.Throws<ArgumentException>(() => ShopMakerAuthoring.AddRewardProducts(_quest, 50, new[] { _item }));
        Assert.That(Json(_shop), Is.EqualTo(before)); Assert.That(Json(_quest), Is.EqualTo(quest));
    }
    [Test] public void StaleSaleIdIsNotAutomaticallyReplaced()
    {
        int reward = SaleReward(id: "saved_but_missing"); string shop = Json(_shop), quest = Json(_quest);
        Assert.Throws<ArgumentException>(() => ShopMakerAuthoring.AddRewardProducts(_quest, reward, new[] { _item }));
        Assert.That(Json(_shop), Is.EqualTo(shop)); Assert.That(Json(_quest), Is.EqualTo(quest));
    }
    [Test] public void DirectSaleAdditionPreservesProductIdPriceStockWeightAndTrade()
    {
        string productId = Product();
        Change(_shop, so => { var p = so.FindProperty("_products").GetArrayElementAtIndex(0);
            p.FindPropertyRelative("_stock").intValue = 8; p.FindPropertyRelative("_priceOverride").intValue = 77;
            p.FindPropertyRelative("_offerWeight").floatValue = 4; });
        int reward = SaleReward(); ShopMakerAuthoring.AddRewardProducts(_quest, reward, new[] { _item });
        var product = _shop.Products.Single(); Assert.That(product.ProductId, Is.EqualTo(productId));
        Assert.That(product.Stock, Is.EqualTo(8)); Assert.That(product.ResolvePrice(), Is.EqualTo(77));
        Assert.That(product.OfferWeight, Is.EqualTo(4)); Assert.That(product.AvailableByDefault, Is.False);
    }
    [Test] public void SharedSalePathsRemainSharedWithoutCreatingOrReplacingIDs()
    {
        string group = Group(); int index = SaleReward(id: group); var otherQuest = Make<QuestDefinitionSO>();
        SaleReward(otherQuest, id: group); string before = Json(otherQuest);
        ShopMakerAuthoring.AddRewardProducts(_quest, index, new[] { _item });
        Assert.That(_shop.UnlockNodes.Count, Is.EqualTo(1)); Assert.That(Json(otherQuest), Is.EqualTo(before));
        var quests = ShopMakerAuthoring.QuestsForProduct(_shop, _shop.Products.Single().ProductId, new[] { _quest, otherQuest, _quest });
        Assert.That(quests, Is.EquivalentTo(new[] { _quest, otherQuest }));
    }
    [Test] public void RemovingSaleItemKeepsRewardNodeProductAndOtherUnlockPaths()
    {
        int index = SaleReward(); ShopMakerAuthoring.AddRewardProducts(_quest, index, new[] { _item });
        string id = _quest.Rewards[index].ShopUnlockNodeId, p = _shop.Products.Single().ProductId, other = Group();
        ShopMakerAuthoring.SetMembership(_shop, other, p, true); string quest = Json(_quest), item = Json(_item);
        ShopMakerAuthoring.RemoveRewardProduct(_quest, index, p);
        Assert.That(Json(_quest), Is.EqualTo(quest)); Assert.That(Json(_item), Is.EqualTo(item));
        Assert.That(_shop.Products.Single().AvailableByDefault, Is.False);
        Assert.That(_shop.TryGetNode(id, out var node), Is.True); Assert.That(node.UnlockedProductIds, Is.Empty);
        Assert.That(ShopMakerAuthoring.GroupsFor(_shop, p).Single().NodeId, Is.EqualTo(other));
    }
    [Test] public void ExplicitShopChangeKeepsOldNodesProductsAndOtherRewards()
    {
        string old = Group(); int index = SaleReward(id: old); var otherShop = Make<ShopConfigSO>();
        string before = Json(_shop); ShopMakerAuthoring.BindRewardShop(_quest, index, otherShop);
        Assert.That(Json(_shop), Is.EqualTo(before)); Assert.That(_quest.Rewards.Length, Is.EqualTo(1));
        Assert.That(_quest.Rewards[index].ShopConfig, Is.SameAs(otherShop)); Assert.That(_quest.Rewards[index].ShopUnlockNodeId, Is.Empty);
    }
    [Test] public void SaleQueriesAndSummaryAreReadOnlySortedAndConfigSpecific()
    {
        int a = SaleReward(); var armor = Make<ArmorItemSO>(); armor.name = "AAA Armor";
        ShopMakerAuthoring.AddRewardProducts(_quest, a, new[] { _item, armor });
        string shop = Json(_shop), quest = Json(_quest);
        Assert.That(ShopMakerAuthoring.ProductsForQuest(_shop, _quest).First().Item, Is.SameAs(armor));
        Assert.That(ShopMakerAuthoring.ShopRewardSummary(_quest.Rewards[a]), Does.Contain("AAA Armor"));
        Assert.That(ShopMakerAuthoring.ProductsForQuest(Make<ShopConfigSO>(), _quest), Is.Empty);
        Assert.That(Json(_shop), Is.EqualTo(shop)); Assert.That(Json(_quest), Is.EqualTo(quest));
    }
    [Test] public void DefaultSaleCanBeRestoredExplicitlyWithoutDeletingQuestPaths()
    {
        int index = SaleReward(); ShopMakerAuthoring.AddRewardProducts(_quest, index, new[] { _item });
        string quest = Json(_quest), group = _shop.UnlockNodes.Single().NodeId;
        ShopMakerAuthoring.SetAvailableByDefault(_shop, _shop.Products.Single().ProductId, true);
        Assert.That(_shop.Products.Single().AvailableByDefault, Is.True); Assert.That(Json(_quest), Is.EqualTo(quest));
        Assert.That(_shop.UnlockNodes.Single().NodeId, Is.EqualTo(group));
    }
}
