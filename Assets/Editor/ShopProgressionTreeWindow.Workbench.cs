using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>상품 가격/판매 조건과 퀘스트-해금 묶음-상품 연결을 편집하는 오른쪽 작업 영역.</summary>
public sealed partial class ShopProgressionTreeWindow
{
    private QuestDefinitionSO _questToLink;
    private int _groupToLink;
    private bool _advancedProduct, _advancedGroup, _advancedSettings;
    private string _groupProductSearch = "";

    private void DrawProductWorkbench()
    {
        SelectionUi.Synchronize(_config, "product:" + _selectedProductId + ":" + _productTab);
        int index = _config.Products.ToList().FindIndex(p => p != null && p.ProductId == _selectedProductId);
        EditorGUILayout.LabelField("PRODUCT WORKBENCH", EditorStyles.boldLabel);
        if (index < 0) { EditorGUILayout.HelpBox("가운데 상품 카드를 선택하세요.\nItemSO를 드래그하면 새 상품이 등록됩니다.", MessageType.Info); return; }
        var definition = _config.Products[index];
        var item = definition.Item;
        EditorGUILayout.LabelField(item != null ? item.name : "Missing Item", EditorStyles.boldLabel);
        MakerRegistrationUi.ItemSummary(item, "이 상점에 등록됨");
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("ItemSO 선택")) { Selection.activeObject = item; EditorGUIUtility.PingObject(item); }
            if (GUILayout.Button("등록 해제...", GUILayout.Width(100)) && EditorUtility.DisplayDialog("상점 등록 해제",
                "이 상점의 상품 등록과 묶음의 상품 연결만 제거합니다. ItemSO·퀘스트·해금 묶음은 삭제하지 않습니다.", "등록 해제", "취소"))
            { ShopMakerAuthoring.RemoveProduct(_config, index); _selectedProductId = ""; GUIUtility.ExitGUI(); }
        }
        _productTab = GUILayout.Toolbar(_productTab, new[] { "가격 / 교환", "판매 설정", "판매 시작 조건" });
        var so = new SerializedObject(_config);
        var product = so.FindProperty("_products").GetArrayElementAtIndex(index);
        if (_productTab == 0) DrawPricing(definition, product);
        if (_productTab == 1)
        {
            EditorGUILayout.PropertyField(product.FindPropertyRelative("_stock"), new GUIContent("재고", "한 번 진열할 때 구매할 수 있는 횟수"));
            if (definition.IsBullet) EditorGUILayout.PropertyField(product.FindPropertyRelative("_bundleAmount"), new GUIContent("1회 구매 탄약 수량"));
            EditorGUILayout.PropertyField(product.FindPropertyRelative("_offerWeight"), new GUIContent("등장 비중", "같은 탄약/일반상품 추첨 후보 사이에서 사용하는 상대 비중. 고정 확률이 아닙니다."));
            EditorGUILayout.HelpBox("탄약과 일반 상품은 별도 추첨입니다. 해금된 후보들의 비중에 따라 재입고 때 선택됩니다. 비중 0은 등장하지 않습니다.", MessageType.Info);
            _advancedProduct = EditorGUILayout.Foldout(_advancedProduct, "Advanced: 저장용 ID / ItemSO 복구", true);
            if (_advancedProduct)
            {
                using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(product.FindPropertyRelative("_productId"), new GUIContent("Product ID"));
                EditorGUILayout.PropertyField(product.FindPropertyRelative("_item"), new GUIContent("ItemSO"));
                EditorGUILayout.HelpBox("ID는 저장 데이터와 묶음 연결에 사용하므로 변경하지 않습니다.", MessageType.None);
            }
        }
        if (_productTab == 2)
        {
            var available = product.FindPropertyRelative("_availableByDefault");
            available.boolValue = EditorGUILayout.Popup("판매 시작", available.boolValue ? 0 : 1,
                new[] { "처음부터 판매 후보", "퀘스트 완료 후 판매 후보" }) == 0;
        }
        if (so.ApplyModifiedProperties()) { ShopMakerAuthoring.Notify(_config); }
        if (_productTab == 2) DrawProductUnlocks(_config.Products[index]);
    }

    private void DrawPricing(ShopConfigSO.ProductDefinition definition, SerializedProperty product)
    {
        ItemSO item = definition.Item;
        if (item != null)
        {
            EditorGUI.BeginChangeCheck();
            int price = EditorGUILayout.DelayedIntField(new GUIContent("아이템 기본 가격", "기존 ItemSO의 PurchasePrice를 직접 편집"), item.PurchasePrice);
            if (EditorGUI.EndChangeCheck()) ShopMakerAuthoring.SetBasePrice(item, price);
        }
        EditorGUILayout.HelpBox("ItemSO 공통 가격입니다. Item Maker에도 반영되며, 되팔기·방어구 수리비·이 아이템을 쓰는 다른 상점에도 영향을 줍니다.", MessageType.Info);
        var priceOverride = product.FindPropertyRelative("_priceOverride");
        bool useOverride = priceOverride.intValue >= 0;
        bool next = EditorGUILayout.Toggle("이 상점만 별도 가격", useOverride);
        if (next != useOverride) priceOverride.intValue = next ? (item != null ? item.PurchasePrice : 1) : -1;
        if (next) priceOverride.intValue = Mathf.Max(0, EditorGUILayout.DelayedIntField("상점 전용 가격", priceOverride.intValue));
        EditorGUILayout.Space(5);
        var requirements = product.FindPropertyRelative("_tradeRequirements");
        EditorGUILayout.LabelField("결제 방식", EditorStyles.boldLabel);
        if (requirements.arraySize == 0)
        {
            int effective = next ? priceOverride.intValue : item != null ? item.PurchasePrice : 0;
            EditorGUILayout.LabelField("실제 구매 비용", (_config.DefaultCurrencyItem != null ? _config.DefaultCurrencyItem.name : "Money Item 미설정") + " × " + effective);
            if (_config.DefaultCurrencyItem == null || effective <= 0)
                EditorGUILayout.HelpBox("현재 런타임은 Money Item과 양수 가격이 있어야 상품을 추첨합니다. 상점 설정에서 결제 아이템을 지정하세요.", MessageType.Warning);
        }
        else EditorGUILayout.HelpBox("물물교환 재료가 있으면 아래 재료를 지불합니다. 위 숫자 가격은 결제에 사용되지 않습니다.", MessageType.Info);
        for (int i = 0; i < requirements.arraySize; i++)
        {
            var row = requirements.GetArrayElementAtIndex(i);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(row.FindPropertyRelative("_item"), GUIContent.none);
                var amount = row.FindPropertyRelative("_amount");
                EditorGUI.BeginChangeCheck();
                int count = EditorGUILayout.IntField(amount.intValue, GUILayout.Width(55));
                if (EditorGUI.EndChangeCheck()) amount.intValue = Mathf.Max(1, count);
                if (GUILayout.Button("×", GUILayout.Width(24))) { requirements.DeleteArrayElementAtIndex(i); break; }
            }
        }
        if (GUILayout.Button("+ 물물교환 재료"))
        {
            requirements.arraySize++;
            var added = requirements.GetArrayElementAtIndex(requirements.arraySize - 1);
            added.FindPropertyRelative("_item").objectReferenceValue = null;
            added.FindPropertyRelative("_amount").intValue = 1;
        }
    }

    private void DrawLegacyProductUnlocks(ShopConfigSO.ProductDefinition product)
    {
        var linked = ShopMakerAuthoring.GroupsFor(_config, product.ProductId).ToList();
        if (product.AvailableByDefault)
            EditorGUILayout.HelpBox("처음부터 판매 후보입니다. 묶음에 연결되어 있어도 퀘스트 완료를 기다리지 않습니다.", MessageType.Info);
        else if (linked.Count == 0)
            EditorGUILayout.HelpBox("이 상품을 해금할 묶음이 없습니다. 아래에서 묶음을 연결하세요.", MessageType.Warning);
        else EditorGUILayout.HelpBox("아래 묶음 중 하나만 해금되어도 추첨 후보가 됩니다 (OR). 실제 진열은 이후 상품 추첨에서 결정됩니다.", MessageType.None);
        foreach (var group in linked)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(group.DisplayName, EditorStyles.boldLabel);
                var quests = ShopMakerAuthoring.QuestsFor(_config, group.NodeId, _quests).ToList();
                EditorGUILayout.LabelField(quests.Count == 0 ? "⚠ 지급 퀘스트 없음" : "퀘스트: " + string.Join(", ", quests.Select(q => q.DisplayName)), EditorStyles.wordWrappedLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("묶음 / 퀘스트 편집")) { _selectedGroupId = group.NodeId; _page = 1; }
                    if (GUILayout.Button("상품 연결 해제", GUILayout.Width(115)))
                    { ShopMakerAuthoring.SetMembership(_config, group.NodeId, product.ProductId, false); GUIUtility.ExitGUI(); }
                }
            }
        }
        var available = _config.UnlockNodes.Where(n => n != null && !n.UnlockedProductIds.Contains(product.ProductId)).ToList();
        _groupToLink = Mathf.Clamp(_groupToLink, 0, Math.Max(0, available.Count - 1));
        using (new EditorGUI.DisabledScope(available.Count == 0))
        {
            _groupToLink = EditorGUILayout.Popup("기존 묶음", _groupToLink, available.Count > 0 ? available.Select(n => n.DisplayName + " [" + n.NodeId + "]").ToArray() : new[] { "연결할 묶음 없음" });
            if (GUILayout.Button("선택한 묶음에 상품 추가"))
            { ShopMakerAuthoring.SetMembership(_config, available[_groupToLink].NodeId, product.ProductId, true); GUIUtility.ExitGUI(); }
        }
        _newGroupName = EditorGUILayout.TextField("새 묶음 이름", _newGroupName);
        if (GUILayout.Button("+ 이 상품의 새 해금 묶음 만들기"))
        {
            Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup();
            _selectedGroupId = ShopMakerAuthoring.CreateGroup(_config, _newGroupName);
            ShopMakerAuthoring.SetMembership(_config, _selectedGroupId, product.ProductId, true);
            Undo.CollapseUndoOperations(undo); _page = 1; GUIUtility.ExitGUI();
        }
    }

    private void DrawGroups()
    {
        EditorGUILayout.LabelField("QUEST REWARD → UNLOCK GROUP → PRODUCTS", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("퀘스트 완료 시 연결된 묶음을 영구 해금합니다. 같은 묶음의 여러 퀘스트는 대체 지급 경로(OR)입니다.", MessageType.None);
        using (new EditorGUILayout.HorizontalScope())
        {
            _newGroupName = EditorGUILayout.TextField(_newGroupName);
            if (GUILayout.Button("+ 묶음", GUILayout.Width(75)))
            { _selectedGroupId = ShopMakerAuthoring.CreateGroup(_config, _newGroupName); GUIUtility.ExitGUI(); }
        }
        _catalogScroll = EditorGUILayout.BeginScrollView(_catalogScroll);
        foreach (var group in _config.UnlockNodes.Where(n => n != null))
        {
            Color old = GUI.backgroundColor;
            if (_selectedGroupId == group.NodeId) GUI.backgroundColor = new Color(.3f, .65f, .8f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (GUILayout.Button(group.DisplayName, EditorStyles.miniButton, GUILayout.Height(27)))
                { SelectionUi.SelectionClicked(); _selectedGroupId = group.NodeId; _workbenchScroll = Vector2.zero; }
                Observe("group:" + group.NodeId, GUILayoutUtility.GetLastRect());
                GUI.backgroundColor = old;
                var quests = ShopMakerAuthoring.QuestsFor(_config, group.NodeId, _quests).ToList();
                EditorGUILayout.LabelField("① 퀘스트  " + (quests.Count == 0 ? "연결 필요" : string.Join(" / ", quests.Select(q => q.DisplayName))), EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField("     ↓  ② " + group.DisplayName, EditorStyles.wordWrappedLabel);
                string[] products = group.UnlockedProductIds.Select(id =>
                {
                    var p = _config.Products.FirstOrDefault(p => p != null && p.ProductId == id);
                    return p?.Item != null ? p.Item.name + (p.AvailableByDefault ? " (기본 판매)" : "") : "Missing: " + id;
                }).ToArray();
                EditorGUILayout.LabelField("     ↓  ③ " + (products.Length == 0 ? "상품 연결 필요" : string.Join(", ", products)), EditorStyles.wordWrappedLabel);
                if (group.PrerequisiteNodeIds.Count > 0)
                    EditorGUILayout.LabelField("선행 묶음 (모두 필요): " + string.Join(", ", group.PrerequisiteNodeIds.Select(id =>
                        _config.UnlockNodes.FirstOrDefault(n => n != null && n.NodeId == id)?.DisplayName ?? "Missing: " + id)), EditorStyles.wordWrappedMiniLabel);
            }
        }
        if (_config.UnlockNodes.Count == 0) EditorGUILayout.HelpBox("+ 묶음을 눌러 첫 해금 묶음을 만드세요.", MessageType.Info);
        EditorGUILayout.EndScrollView();
    }

    private void DrawGroupWorkbench()
    {
        SelectionUi.Synchronize(_config, "group:" + _selectedGroupId);
        EditorGUILayout.LabelField("UNLOCK WORKBENCH", EditorStyles.boldLabel);
        int index = _config.UnlockNodes.ToList().FindIndex(n => n != null && n.NodeId == _selectedGroupId);
        if (index < 0) { EditorGUILayout.HelpBox("가운데 해금 묶음을 선택하세요.", MessageType.Info); return; }
        var group = _config.UnlockNodes[index];
        var so = new SerializedObject(_config);
        var node = so.FindProperty("_unlockNodes").GetArrayElementAtIndex(index);
        EditorGUILayout.PropertyField(node.FindPropertyRelative("_displayName"), new GUIContent("묶음 이름"));
        EditorGUILayout.PropertyField(node.FindPropertyRelative("_description"), new GUIContent("설명"));
        if (so.ApplyModifiedProperties()) ShopMakerAuthoring.Notify(_config);
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("1. 지급 퀘스트", EditorStyles.boldLabel);
        DrawQuestLinks(group);
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("2. 해금할 상품", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("체크는 이 묶음의 상품 연결만 변경합니다. '기본 판매' 상품을 잠그려면 상품의 판매 조건을 '퀘스트 해금 필요'로 바꾸세요.", MessageType.None);
        _groupProductSearch = EditorGUILayout.TextField("상품 찾기", _groupProductSearch);
        foreach (int productIndex in ShopMakerAuthoring.SortedProducts(_config, "", _groupProductSearch))
        {
            var p = _config.Products[productIndex];
            bool member = group.UnlockedProductIds.Contains(p.ProductId);
            using (new EditorGUILayout.HorizontalScope())
            {
                bool next = EditorGUILayout.ToggleLeft((p.Item != null ? p.Item.name : "Missing Item") + (p.AvailableByDefault ? " · 기본 판매" : ""), member);
                if (next != member) { ShopMakerAuthoring.SetMembership(_config, group.NodeId, p.ProductId, next); GUIUtility.ExitGUI(); }
                if (GUILayout.Button("편집", GUILayout.Width(45))) { _selectedProductId = p.ProductId; _page = 0; _productTab = 2; }
            }
        }
        foreach (string missing in group.UnlockedProductIds.Where(id => !_config.Products.Any(p => p != null && p.ProductId == id)))
            EditorGUILayout.HelpBox("Missing product link: " + missing + " — Shop Config Inspector에서 확인하세요.", MessageType.Warning);
        _advancedGroup = EditorGUILayout.Foldout(_advancedGroup, "Advanced: 선행 묶음 / 저장용 ID", true);
        if (_advancedGroup)
        {
            EditorGUILayout.SelectableLabel(group.NodeId, EditorStyles.textField, GUILayout.Height(20));
            EditorGUILayout.HelpBox("선행 묶음은 모두 해금되어야 지급됩니다(AND). 퀘스트 진행 순서와 맞추세요. 이 설정 자체가 자동 해금을 실행하지는 않습니다.", MessageType.Info);
            foreach (var other in _config.UnlockNodes.Where(n => n != null && n.NodeId != group.NodeId))
            {
                bool member = group.PrerequisiteNodeIds.Contains(other.NodeId);
                using (new EditorGUI.DisabledScope(!member && ShopMakerAuthoring.WouldCycle(_config, group.NodeId, other.NodeId)))
                {
                    bool next = EditorGUILayout.ToggleLeft(other.DisplayName, member);
                    if (next != member) { ShopMakerAuthoring.SetPrerequisite(_config, group.NodeId, other.NodeId, next); GUIUtility.ExitGUI(); }
                }
            }
            foreach (string missing in group.PrerequisiteNodeIds.Where(id => !_config.UnlockNodes.Any(n => n != null && n.NodeId == id)).ToArray())
                if (GUILayout.Button("없는 선행 연결 제거: " + missing))
                { ShopMakerAuthoring.SetPrerequisite(_config, group.NodeId, missing, false); GUIUtility.ExitGUI(); }
            bool canDelete = ShopMakerAuthoring.CanRemoveGroup(_config, group.NodeId, _quests);
            using (new EditorGUI.DisabledScope(!canDelete))
                if (GUILayout.Button("빈 묶음 삭제...") && EditorUtility.DisplayDialog("빈 묶음 삭제", "이 ID로 저장된 과거 해금 기록은 새 묶음으로 이전되지 않습니다. 삭제할까요?", "삭제", "취소"))
                { ShopMakerAuthoring.RemoveEmptyGroup(_config, group.NodeId, _quests); _selectedGroupId = ""; GUIUtility.ExitGUI(); }
            if (!canDelete) EditorGUILayout.LabelField("상품·퀘스트·선행 참조가 있는 묶음은 삭제하지 않습니다.", EditorStyles.wordWrappedMiniLabel);
        }
    }

    private void DrawQuestLinks(ShopConfigSO.UnlockNode group)
    {
        var quests = ShopMakerAuthoring.QuestsFor(_config, group.NodeId, _quests).ToList();
        if (quests.Count == 0) EditorGUILayout.HelpBox("이 묶음을 지급하는 퀘스트가 없습니다. 아래에서 QuestSO를 선택하고 연결하세요.", MessageType.Warning);
        foreach (var quest in quests)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(quest.DisplayName, EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField(quest, typeof(QuestDefinitionSO), false);
                if (GUILayout.Button("Quest Maker에서 이 보상 보기")) QuestDesignTreeWindow.OpenForShopReward(quest);
            }
        }
        _questToLink = (QuestDefinitionSO)EditorGUILayout.ObjectField("추가할 QuestSO", _questToLink, typeof(QuestDefinitionSO), false);
        using (new EditorGUI.DisabledScope(_questToLink == null || !AssetDatabase.Contains(_questToLink)))
            if (GUILayout.Button("+ 이 퀘스트의 해금 보상으로 연결"))
            {
                ShopMakerAuthoring.LinkQuest(_config, group.NodeId, _questToLink);
                if (!_quests.Contains(_questToLink)) _quests.Add(_questToLink);
                _questToLink = null; GUIUtility.ExitGUI();
            }
        EditorGUILayout.LabelField("기존 퀘스트 보상은 그대로 두고 새 해금 보상을 뒤에 추가합니다. 보상 삭제·순서 변경은 기존 저장의 지급 기록에 영향을 줄 수 있어 Quest Maker에서 별도로 검토하세요.", EditorStyles.wordWrappedMiniLabel);
    }

    private void DrawSettings()
    {
        SelectionUi.Synchronize(_config, "settings");
        EditorGUILayout.LabelField("SHOP SETTINGS", EditorStyles.boldLabel);
        _catalogScroll = EditorGUILayout.BeginScrollView(_catalogScroll);
        var so = new SerializedObject(_config);
        EditorGUILayout.LabelField("결제 / 가격 비율", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("_defaultCurrencyItem"), new GUIContent("기본 Money Item"));
        EditorGUILayout.PropertyField(so.FindProperty("_sellPriceRate"), new GUIContent("되팔기 가격 비율"));
        if (EquipmentRepairPolicy.Enabled)
            EditorGUILayout.PropertyField(so.FindProperty("_armorRepairPriceRate"), new GUIContent("방어구 수리비 비율"));
        else EditorGUILayout.HelpBox("장비 수리 기능은 현재 제외되어 있습니다. 기존 수리 계수와 내구도 저장 데이터는 보존됩니다.", MessageType.None);
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("진열 / 재입고", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("_bulletOfferCount"), new GUIContent("탄약 진열 수"));
        EditorGUILayout.PropertyField(so.FindProperty("_itemOfferCount"), new GUIContent("일반 상품 진열 수"));
        EditorGUILayout.PropertyField(so.FindProperty("_allowDuplicateBulletOffers"), new GUIContent("같은 탄약 중복 진열"));
        EditorGUILayout.PropertyField(so.FindProperty("_allowDuplicateItemOffers"), new GUIContent("같은 아이템 중복 진열"));
        EditorGUILayout.PropertyField(so.FindProperty("_restockIntervalSeconds"), new GUIContent("재입고 간격 (초)", "0이면 시간에 따른 재입고 없음"));
        EditorGUILayout.HelpBox("저장된 진열/재고와 이미 지급된 해금 기록은 여기서 초기화하지 않습니다. 편집한 목록·가격은 새 상품 추첨에 사용됩니다.", MessageType.Info);
        _advancedSettings = EditorGUILayout.Foldout(_advancedSettings, "Advanced: 리롤 / 저장용 ID", true);
        if (_advancedSettings)
        {
            EditorGUILayout.PropertyField(so.FindProperty("_allowReroll"), new GUIContent("리롤 허용"));
            EditorGUILayout.PropertyField(so.FindProperty("_rerollCost"), new GUIContent("리롤 비용 (기존 WIP 값)"));
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(so.FindProperty("_configId"));
            if (GUILayout.Button("Shop Config 선택")) Selection.activeObject = _config;
        }
        if (so.ApplyModifiedProperties()) ShopMakerAuthoring.Notify(_config);
        EditorGUILayout.Space(8);
        if (GUILayout.Button("연결 / 가격 / 해금 조건 검사")) _validation = ShopMakerAuthoring.Validate(_config, _quests);
        if (_validation != null)
        {
            EditorGUILayout.HelpBox(_validation.Count == 0 ? "검사 통과. 연결/결제 설정 문제를 찾지 못했습니다." : _validation.Count + "개 확인 필요. 검사 중 데이터는 변경하지 않았습니다.", _validation.Count == 0 ? MessageType.Info : MessageType.Warning);
            foreach (string issue in _validation) EditorGUILayout.HelpBox(issue, MessageType.Warning);
        }
        EditorGUILayout.EndScrollView();
    }
}
