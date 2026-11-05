using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Shop Maker의 데이터 편집 경계. 런타임 형식은 바꾸지 않는다.
/// ItemSO = 공통 가격, ShopConfigSO = 상품/해금 묶음, QuestDefinitionSO = 해금 지급 보상.
/// 조회는 읽기 전용이고, 명시적인 편집만 Undo/dirty 처리한다. 저장은 창의 Save가 담당한다.
/// </summary>
internal static partial class ShopMakerAuthoring
{
    internal static event Action<UnityEngine.Object> Changed;
    internal static void Notify(UnityEngine.Object asset)
    {
        EditorUtility.SetDirty(asset);
        Changed?.Invoke(asset);
    }

    private static void Edit(UnityEngine.Object asset, string label, Action<SerializedObject> edit)
    {
        Undo.RecordObject(asset, label);
        var serialized = new SerializedObject(asset);
        edit(serialized);
        if (serialized.ApplyModifiedProperties()) Notify(asset);
    }

    internal static int Register(ShopConfigSO config, ItemSO item)
    {
        if (config == null || item == null) throw new ArgumentException("Choose a shop and ItemSO.");
        int existing = config.Products.ToList().FindIndex(p => p != null && p.Item == item);
        if (existing >= 0) return existing;
        int index = config.Products.Count;
        Edit(config, "Register shop item", so =>
        {
            var products = so.FindProperty("_products");
            products.arraySize++;
            var p = products.GetArrayElementAtIndex(index);
            // Unity 배열 추가는 마지막 원소를 복제하므로 모든 필드를 명시적으로 초기화한다.
            p.FindPropertyRelative("_productId").stringValue = "product_" + Guid.NewGuid().ToString("N");
            p.FindPropertyRelative("_item").objectReferenceValue = item;
            p.FindPropertyRelative("_availableByDefault").boolValue = true;
            p.FindPropertyRelative("_offerWeight").floatValue = 1f;
            p.FindPropertyRelative("_stock").intValue = 1;
            p.FindPropertyRelative("_bundleAmount").intValue = 1;
            p.FindPropertyRelative("_priceOverride").intValue = -1;
            p.FindPropertyRelative("_tradeRequirements").ClearArray();
        });
        return index;
    }

    internal static string CreateGroup(ShopConfigSO config, string name)
    {
        string id = "unlock_" + Guid.NewGuid().ToString("N");
        Edit(config, "Create shop unlock group", so =>
        {
            var nodes = so.FindProperty("_unlockNodes");
            nodes.arraySize++;
            var n = nodes.GetArrayElementAtIndex(nodes.arraySize - 1);
            n.FindPropertyRelative("_nodeId").stringValue = id;
            n.FindPropertyRelative("_displayName").stringValue = string.IsNullOrWhiteSpace(name) ? "New unlock group" : name.Trim();
            n.FindPropertyRelative("_description").stringValue = "";
            n.FindPropertyRelative("_prerequisiteNodeIds").ClearArray();
            n.FindPropertyRelative("_unlockedProductIds").ClearArray();
        });
        return id;
    }

    internal static IEnumerable<QuestDefinitionSO> QuestsFor(ShopConfigSO config, string nodeId, IEnumerable<QuestDefinitionSO> quests) =>
        quests.Where(q => q != null && q.Rewards.Any(r => r != null && r.RewardType == QuestRewardType.ShopUnlock &&
            r.ShopConfig == config && r.ShopUnlockNodeId == nodeId));

    internal static IEnumerable<ShopConfigSO.UnlockNode> GroupsFor(ShopConfigSO config, string productId) =>
        config.UnlockNodes.Where(n => n != null && n.UnlockedProductIds.Contains(productId));

    /// <summary>기존 보상/지급 인덱스 보존: 같은 연결은 중복 등록하지 않고 새 보상만 마지막에 추가한다.</summary>
    internal static bool LinkQuest(ShopConfigSO config, string nodeId, QuestDefinitionSO quest)
    {
        RequireGroup(config, nodeId);
        if (quest == null) throw new ArgumentNullException(nameof(quest));
        if (QuestsFor(config, nodeId, new[] { quest }).Any()) return false;
        Edit(quest, "Link quest shop reward", so =>
        {
            var rewards = so.FindProperty("_rewards");
            rewards.arraySize++;
            var r = rewards.GetArrayElementAtIndex(rewards.arraySize - 1);
            r.FindPropertyRelative("_rewardType").enumValueIndex = (int)QuestRewardType.ShopUnlock;
            r.FindPropertyRelative("_shopConfig").objectReferenceValue = config;
            r.FindPropertyRelative("_shopUnlockNodeId").stringValue = nodeId;
            r.FindPropertyRelative("_item").objectReferenceValue = null;
            r.FindPropertyRelative("_itemAmount").intValue = 1;
            r.FindPropertyRelative("_stat").enumValueIndex = 0;
            r.FindPropertyRelative("_statModifierType").enumValueIndex = 0;
            r.FindPropertyRelative("_statValue").floatValue = 1;
            r.FindPropertyRelative("_stageToUnlock").objectReferenceValue = null;
            r.FindPropertyRelative("_warehouseSlotAmount").intValue = 1;
        });
        return true;
    }

    internal static void SetMembership(ShopConfigSO config, string nodeId, string productId, bool member)
    {
        RequireGroup(config, nodeId);
        if (!config.Products.Any(p => p != null && p.ProductId == productId)) throw new ArgumentException("Missing product.");
        Edit(config, "Edit unlock group products", so => SetString(Node(so, nodeId).FindPropertyRelative("_unlockedProductIds"), productId, member));
    }

    internal static void SetPrerequisite(ShopConfigSO config, string nodeId, string prerequisite, bool enabled)
    {
        RequireGroup(config, nodeId);
        if (enabled)
        {
            RequireGroup(config, prerequisite);
            if (WouldCycle(config, nodeId, prerequisite)) throw new InvalidOperationException("This prerequisite would create a cycle.");
        }
        Edit(config, "Edit unlock prerequisites", so => SetString(Node(so, nodeId).FindPropertyRelative("_prerequisiteNodeIds"), prerequisite, enabled));
    }

    internal static bool WouldCycle(ShopConfigSO config, string nodeId, string prerequisite)
    {
        var visited = new HashSet<string>();
        var pending = new Stack<string>(); pending.Push(prerequisite);
        while (pending.Count > 0)
        {
            string id = pending.Pop();
            if (id == nodeId) return true;
            if (!visited.Add(id)) continue;
            var node = config.UnlockNodes.FirstOrDefault(n => n != null && n.NodeId == id);
            if (node != null) foreach (string next in node.PrerequisiteNodeIds) pending.Push(next);
        }
        return false;
    }

    internal static void RemoveProduct(ShopConfigSO config, int index)
    {
        string id = config.Products[index].ProductId;
        Edit(config, "Remove shop registration", so =>
        {
            var nodes = so.FindProperty("_unlockNodes");
            for (int i = 0; i < nodes.arraySize; i++)
                SetString(nodes.GetArrayElementAtIndex(i).FindPropertyRelative("_unlockedProductIds"), id, false);
            so.FindProperty("_products").DeleteArrayElementAtIndex(index);
        });
    }

    internal static bool CanRemoveGroup(ShopConfigSO config, string id, IEnumerable<QuestDefinitionSO> quests) =>
        !QuestsFor(config, id, quests).Any() && !config.UnlockNodes.Any(n => n != null && n.PrerequisiteNodeIds.Contains(id)) &&
        !RequireGroup(config, id).UnlockedProductIds.Any();

    internal static void RemoveEmptyGroup(ShopConfigSO config, string id, IEnumerable<QuestDefinitionSO> quests)
    {
        if (!CanRemoveGroup(config, id, quests)) throw new InvalidOperationException("Referenced groups cannot be removed. Keep saved unlock IDs stable.");
        Edit(config, "Remove empty unlock group", so =>
        {
            var nodes = so.FindProperty("_unlockNodes");
            for (int i = 0; i < nodes.arraySize; i++)
                if (nodes.GetArrayElementAtIndex(i).FindPropertyRelative("_nodeId").stringValue == id) { nodes.DeleteArrayElementAtIndex(i); break; }
        });
    }

    internal static void SetBasePrice(ItemSO item, int price) =>
        Edit(item, "Edit ItemSO base price", so => so.FindProperty("_purchasePrice").intValue = Mathf.Max(0, price));

    internal static List<int> SortedProducts(ShopConfigSO config, string category = "", string search = "") =>
        Enumerable.Range(0, config.Products.Count).Where(i => config.Products[i] != null)
            .Where(i => string.IsNullOrEmpty(category) || ItemMakerWindow.LootCategoryKey(config.Products[i].Item) == category)
            .Where(i => string.IsNullOrEmpty(search) || (config.Products[i].Item != null ? config.Products[i].Item.name : "Missing Item")
                .IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 || (config.Products[i].ProductId ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(i => config.Products[i].Item == null ? int.MaxValue : ItemMakerCatalogView.RarityOrder(config.Products[i].Item.Rarity))
            .ThenBy(i => config.Products[i].Item == null ? int.MaxValue : ItemMakerWindow.LootCategoryOrder(config.Products[i].Item))
            .ThenBy(i => config.Products[i].Item != null ? config.Products[i].Item.name : "", StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => config.Products[i].ProductId, StringComparer.Ordinal).ToList();

    /// <summary>교차 연결 검사는 실제 에셋을 고치지 않는다. 잘못된 설정은 사용자가 선택해서 수정한다.</summary>
    internal static List<string> Validate(ShopConfigSO config, IEnumerable<QuestDefinitionSO> quests)
    {
        var messages = new List<string>();
        var questList = quests.Where(q => q != null).ToList();
        var productIds = new HashSet<string>(); var nodeIds = new HashSet<string>();
        foreach (var n in config.UnlockNodes.Where(n => n != null))
            if (string.IsNullOrWhiteSpace(n.NodeId) || !nodeIds.Add(n.NodeId)) messages.Add("Missing/duplicate unlock ID: " + n.NodeId);
        foreach (var p in config.Products)
        {
            if (p == null) { messages.Add("Null product entry."); continue; }
            string label = p.Item != null ? p.Item.name : p.ProductId;
            if (string.IsNullOrWhiteSpace(p.ProductId) || !productIds.Add(p.ProductId)) messages.Add("Missing/duplicate product ID: " + p.ProductId);
            if (p.Item == null) messages.Add(label + ": missing ItemSO.");
            if (p.OfferWeight <= 0) messages.Add(label + ": weight is 0; never appears.");
            if (!p.AvailableByDefault && !GroupsFor(config, p.ProductId).Any()) messages.Add(label + ": locked but no unlock group.");
            if (p.AvailableByDefault && GroupsFor(config, p.ProductId).Any()) messages.Add(label + ": available from start; unlock groups do not gate this product.");
            var payment = config.ResolveTradeRequirements(p);
            if (payment.Count == 0 || payment.Any(r => r == null || !r.IsValid)) messages.Add(label + ": invalid payment (Money Item/positive price or barter required).");
        }
        foreach (var n in config.UnlockNodes.Where(n => n != null))
        {
            if (!QuestsFor(config, n.NodeId, questList).Any()) messages.Add(n.DisplayName + ": no quest grants this group.");
            foreach (string p in n.UnlockedProductIds) if (!productIds.Contains(p)) messages.Add(n.DisplayName + ": missing product " + p);
            foreach (string id in n.PrerequisiteNodeIds)
            {
                if (!nodeIds.Contains(id)) messages.Add(n.DisplayName + ": missing prerequisite " + id);
                else if (WouldCycle(config, n.NodeId, id)) messages.Add(n.DisplayName + ": prerequisite cycle.");
            }
        }
        foreach (var q in questList)
            foreach (var reward in q.Rewards.Where(r => r != null && r.RewardType == QuestRewardType.ShopUnlock && r.ShopConfig == config))
                if (!nodeIds.Contains(reward.ShopUnlockNodeId)) messages.Add(q.DisplayName + ": missing unlock group " + reward.ShopUnlockNodeId);
        return messages;
    }

    private static ShopConfigSO.UnlockNode RequireGroup(ShopConfigSO config, string id) =>
        (config != null ? config.UnlockNodes.FirstOrDefault(n => n != null && n.NodeId == id) : null) ?? throw new ArgumentException("Missing unlock group: " + id);
    private static SerializedProperty Node(SerializedObject so, string id)
    {
        var nodes = so.FindProperty("_unlockNodes");
        for (int i = 0; i < nodes.arraySize; i++)
            if (nodes.GetArrayElementAtIndex(i).FindPropertyRelative("_nodeId").stringValue == id) return nodes.GetArrayElementAtIndex(i);
        throw new ArgumentException("Missing unlock group: " + id);
    }
    private static void SetString(SerializedProperty array, string value, bool enabled)
    {
        for (int i = array.arraySize - 1; i >= 0; i--)
            if (array.GetArrayElementAtIndex(i).stringValue == value)
            { if (enabled) return; array.DeleteArrayElementAtIndex(i); }
        if (enabled) { array.arraySize++; array.GetArrayElementAtIndex(array.arraySize - 1).stringValue = value; }
    }
}
