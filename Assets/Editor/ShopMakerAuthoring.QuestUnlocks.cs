using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Quest Maker의 '상점 판매 해금' 편집. 기존 QuestReward의 위치와 UnlockNode ID를 유지한다.
/// ItemSO/런타임/플레이어 세이브는 수정하지 않으며, 빈 새 보상에만 내부 연결을 생성한다.
/// </summary>
internal static partial class ShopMakerAuthoring
{
    internal static IEnumerable<QuestDefinitionSO> QuestsForProduct(ShopConfigSO shop, string productId, IEnumerable<QuestDefinitionSO> quests) =>
        quests.Where(q => q != null && q.Rewards.Any(r => r != null && r.RewardType == QuestRewardType.ShopUnlock &&
            r.ShopConfig == shop && shop.TryGetNode(r.ShopUnlockNodeId, out var node) && node.UnlockedProductIds.Contains(productId))).Distinct();

    internal static IEnumerable<ShopConfigSO.ProductDefinition> ProductsForQuest(ShopConfigSO shop, QuestDefinitionSO quest)
    {
        if (shop == null || quest == null) return Enumerable.Empty<ShopConfigSO.ProductDefinition>();
        var ids = new HashSet<string>(quest.Rewards.Where(r => r != null && r.RewardType == QuestRewardType.ShopUnlock && r.ShopConfig == shop)
            .SelectMany(r => shop.TryGetNode(r.ShopUnlockNodeId, out var node) ? node.UnlockedProductIds : Array.Empty<string>()));
        return SortedProducts(shop).Select(i => shop.Products[i]).Where(p => ids.Contains(p.ProductId));
    }

    internal static string ShopRewardSummary(QuestRewardDefinition reward)
    {
        if (reward?.RewardType != QuestRewardType.ShopUnlock) return reward?.BuildSummary() ?? "보상 없음";
        if (reward.ShopConfig == null) return "상점 판매 해금: 상점 선택 필요";
        if (!reward.ShopConfig.TryGetNode(reward.ShopUnlockNodeId, out var node)) return "상점 판매 해금: 상품 연결 필요";
        var products = node.UnlockedProductIds.Select(id => reward.ShopConfig.Products.FirstOrDefault(p => p?.ProductId == id))
            .Select(p => p?.Item != null ? p.Item.name : "연결 누락").ToArray();
        return "상점 판매 해금: " + (products.Length > 0 ? string.Join(", ", products) : "상품 추가 필요");
    }

    internal static void SetAvailableByDefault(ShopConfigSO shop, string productId, bool available)
    {
        int index = shop.Products.ToList().FindIndex(p => p?.ProductId == productId);
        if (index < 0) throw new ArgumentException("Missing product.");
        if (shop.Products[index].AvailableByDefault == available) return;
        Edit(shop, "Change shop sale start condition", so => so.FindProperty("_products").GetArrayElementAtIndex(index)
            .FindPropertyRelative("_availableByDefault").boolValue = available);
    }

    // Changing the shop is explicit. The old node remains intact; never remap saved IDs silently.
    internal static void BindRewardShop(QuestDefinitionSO quest, int rewardIndex, ShopConfigSO shop)
    {
        var reward = RequireShopReward(quest, rewardIndex);
        if (reward.ShopConfig == shop) return;
        Edit(quest, "Choose quest sale-unlock shop", so =>
        {
            var row = so.FindProperty("_rewards").GetArrayElementAtIndex(rewardIndex);
            row.FindPropertyRelative("_shopConfig").objectReferenceValue = shop;
            row.FindPropertyRelative("_shopUnlockNodeId").stringValue = "";
        });
    }

    internal static void AddRewardProducts(QuestDefinitionSO quest, int rewardIndex, IEnumerable<ItemSO> items)
    {
        var reward = RequireShopReward(quest, rewardIndex);
        var shop = reward.ShopConfig;
        if (shop == null) throw new ArgumentException("Choose the shop first.");
        var add = (items ?? throw new ArgumentNullException(nameof(items))).ToArray();
        if (add.Any(i => i == null)) throw new ArgumentException("Every sale-unlock entry needs an ItemSO.");
        if (add.Length == 0) return;
        // Stale authored IDs must be repaired explicitly, not overwritten with a new save identity.
        string id = reward.ShopUnlockNodeId;
        if (!string.IsNullOrEmpty(id)) RequireGroup(shop, id);
        Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup();
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                id = CreateGroup(shop, quest.DisplayName + " · 판매 해금 " + (rewardIndex + 1));
                Edit(quest, "Connect quest sale-unlock products", so => so.FindProperty("_rewards").GetArrayElementAtIndex(rewardIndex)
                    .FindPropertyRelative("_shopUnlockNodeId").stringValue = id);
            }
            foreach (var item in add.Distinct())
            {
                var product = shop.Products[Register(shop, item)];
                SetMembership(shop, id, product.ProductId, true);
                // This is the explicit 'quest completion -> sale' operation, not a browsing side effect.
                SetAvailableByDefault(shop, product.ProductId, false);
            }
        }
        finally { Undo.CollapseUndoOperations(undo); }
    }

    internal static void RemoveRewardProduct(QuestDefinitionSO quest, int rewardIndex, string productId)
    {
        var reward = RequireShopReward(quest, rewardIndex);
        RequireGroup(reward.ShopConfig, reward.ShopUnlockNodeId);
        SetMembership(reward.ShopConfig, reward.ShopUnlockNodeId, productId, false);
        // Leave the product, price, availability, other unlock paths, node ID and reward index intact.
    }

    private static QuestRewardDefinition RequireShopReward(QuestDefinitionSO quest, int index)
    {
        if (quest == null || index < 0 || index >= quest.Rewards.Length || quest.Rewards[index]?.RewardType != QuestRewardType.ShopUnlock)
            throw new ArgumentException("Choose a ShopUnlock quest reward.");
        return quest.Rewards[index];
    }
}
