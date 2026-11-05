using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>기본 탐색은 퀘스트 -> 판매 상품. 기존 묶음 편집은 고급 모드에 남겨 저장 호환성을 유지한다.</summary>
public sealed partial class ShopProgressionTreeWindow
{
    [SerializeField] private QuestDefinitionSO _selectedUnlockQuest;
    [SerializeField] private bool _showLegacyUnlocks, _legacyProductUnlocks;
    private QuestDefinitionSO _questToConfigure;

    private void DrawQuestPicker()
    {
        _questToConfigure = (QuestDefinitionSO)EditorGUILayout.ObjectField("퀘스트 선택", _questToConfigure, typeof(QuestDefinitionSO), false);
        using (new EditorGUI.DisabledScope(_questToConfigure == null))
            if (GUILayout.Button("Quest Maker에서 판매 해금 설정")) QuestDesignTreeWindow.OpenForShopReward(_questToConfigure, _config);
        EditorGUILayout.LabelField("Quest Maker → 보상 → + 상점 판매 해금 → ItemSO 추가 → Save", EditorStyles.wordWrappedMiniLabel);
    }
    private void DrawProductUnlocks(ShopConfigSO.ProductDefinition product)
    {
        var quests = ShopMakerAuthoring.QuestsForProduct(_config, product.ProductId, _quests).ToArray();
        if (product.AvailableByDefault) EditorGUILayout.HelpBox("처음부터 판매 후보입니다. 아래 퀘스트가 연결되어 있어도 완료를 기다리지 않습니다.", MessageType.Info);
        else if (quests.Length == 0) EditorGUILayout.HelpBox("이 상품을 판매 해금하는 퀘스트가 없습니다. 아래에서 퀘스트를 선택해 보상을 설정하세요.", MessageType.Warning);
        else EditorGUILayout.HelpBox("아래 퀘스트 중 하나가 완료되면 판매 후보에 추가됩니다. 기존 선행 해금 조건이 있으면 그것도 필요합니다. 즉시 진열되지는 않습니다.", MessageType.None);
        EditorGUILayout.LabelField("이 상품을 해금하는 퀘스트", EditorStyles.boldLabel);
        Observe("productUnlock:quests", GUILayoutUtility.GetLastRect());
        foreach (var quest in quests)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(quest.DisplayName, EditorStyles.wordWrappedLabel);
                if (GUILayout.Button("Quest Maker에서 해금 상품 수정")) QuestDesignTreeWindow.OpenForShopReward(quest, _config);
                Observe("productUnlock:quest:" + quest.QuestId, GUILayoutUtility.GetLastRect());
            }
        }
        DrawQuestPicker();
        _legacyProductUnlocks = EditorGUILayout.Foldout(_legacyProductUnlocks, "고급 · 기존 해금 묶음 연결", true);
        if (_legacyProductUnlocks) DrawLegacyProductUnlocks(product);
    }
    private void DrawQuestUnlockCatalog()
    {
        EditorGUILayout.LabelField("퀘스트 완료 → 판매 해금되는 상품", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("아이템 지급이 아닙니다. 퀘스트가 완료되면 해당 상점의 판매 후보에 추가됩니다. 상품 선택은 Quest Maker의 보상에서 합니다.", MessageType.None);
        DrawQuestPicker();
        _showLegacyUnlocks = EditorGUILayout.Foldout(_showLegacyUnlocks, "고급 · 기존 해금 묶음 / 연결 관리", true);
        Observe("unlock:advanced", GUILayoutUtility.GetLastRect());
        if (_showLegacyUnlocks) { DrawGroups(); return; }
        _catalogScroll = EditorGUILayout.BeginScrollView(_catalogScroll);
        var quests = _quests.Where(q => q.Rewards.Any(r => r != null && r.RewardType == QuestRewardType.ShopUnlock && r.ShopConfig == _config)).ToArray();
        foreach (var quest in quests)
        {
            Color old = GUI.backgroundColor;
            if (_selectedUnlockQuest == quest) GUI.backgroundColor = new Color(.3f, .65f, .8f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (GUILayout.Button(quest.DisplayName, GUILayout.Height(28)))
                { SelectionUi.SelectionClicked(); _selectedUnlockQuest = quest; _workbenchScroll = Vector2.zero; }
                Observe("unlockQuest:" + quest.QuestId, GUILayoutUtility.GetLastRect());
                GUI.backgroundColor = old;
                var products = ShopMakerAuthoring.ProductsForQuest(_config, quest).ToArray();
                EditorGUILayout.LabelField("완료하면 판매 후보 추가", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField(products.Length == 0 ? "상품 연결 확인 필요" : string.Join(", ", products.Select(p => p.Item != null ? p.Item.name : "ItemSO 누락")), EditorStyles.wordWrappedLabel);
                if (products.Any(p => p.AvailableByDefault)) EditorGUILayout.LabelField("⚠ 처음부터 판매하는 상품 포함 — 판매 조건 확인", EditorStyles.wordWrappedMiniLabel);
            }
        }
        if (quests.Length == 0) EditorGUILayout.HelpBox("아직 이 상점과 연결된 퀘스트가 없어요. 위에서 퀘스트를 선택하고 판매 해금 보상을 추가하세요.", MessageType.Info);
        EditorGUILayout.EndScrollView();
    }
    private void DrawQuestUnlockWorkbench()
    {
        SelectionUi.Synchronize(_selectedUnlockQuest, "quest-unlocks:" + (_config != null ? _config.GetInstanceID().ToString() : ""));
        EditorGUILayout.LabelField("선택한 퀘스트의 판매 해금", EditorStyles.boldLabel);
        if (_selectedUnlockQuest == null) { EditorGUILayout.HelpBox("가운데 퀘스트를 선택하세요. 완료 후 판매 해금되는 상품들을 볼 수 있어요.", MessageType.Info); return; }
        EditorGUILayout.LabelField(_selectedUnlockQuest.DisplayName, EditorStyles.wordWrappedLabel);
        if (GUILayout.Button("Quest Maker에서 해금 상품 수정")) QuestDesignTreeWindow.OpenForShopReward(_selectedUnlockQuest, _config);
        Observe("unlockQuest:edit", GUILayoutUtility.GetLastRect());
        foreach (var reward in _selectedUnlockQuest.Rewards.Where(r => r != null && r.RewardType == QuestRewardType.ShopUnlock && r.ShopConfig == _config))
        {
            if (!_config.TryGetNode(reward.ShopUnlockNodeId, out var node))
            { EditorGUILayout.HelpBox("상품 연결 누락. Quest Maker의 고급 설정에서 기존 연결을 확인하세요.", MessageType.Warning); continue; }
            if (node.PrerequisiteNodeIds.Count > 0) EditorGUILayout.HelpBox("추가 조건: " + string.Join(", ", node.PrerequisiteNodeIds.Select(id =>
                _config.TryGetNode(id, out var previous) ? previous.DisplayName : "연결 누락: " + id)) + " 판매 해금도 먼저 필요합니다.", MessageType.None);
        }
        var products = ShopMakerAuthoring.ProductsForQuest(_config, _selectedUnlockQuest).ToArray();
        EditorGUILayout.LabelField("완료 후 판매 해금되는 상품 · " + products.Length + "개", EditorStyles.boldLabel);
        foreach (var product in products)
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(product.Item != null ? product.Item.name : "ItemSO 누락", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(product.AvailableByDefault ? "⚠ 처음부터 판매 중 — 퀘스트 제한 없음" : "퀘스트 완료 후 판매 후보", EditorStyles.wordWrappedMiniLabel);
            if (GUILayout.Button("이 상품의 가격 / 판매 조건 보기"))
            { _selectedProductId = product.ProductId; _page = 0; _productTab = 2; _category = _search = ""; _workbenchScroll = Vector2.zero; }
        }
        EditorGUILayout.HelpBox("저장된 해금 ID와 기존 퀘스트 보상은 유지됩니다. 연결 변경은 Quest Maker, 가격·재고 변경은 Shop Maker에서 하세요.", MessageType.None);
    }
}
