using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>퀘스트 완료 -> 상점 판매 후보 추가. 기본 화면은 ItemSO, 내부 묶음/ID는 고급에서만 편집한다.</summary>
public sealed partial class QuestDesignTreeWindow
{
    [SerializeField] private List<UnityEngine.Object> _pendingSaleSaves = new();
    [SerializeField] private ShopConfigSO _preferredSaleShop;
    private readonly Dictionary<string, ItemSO> _saleItemsToAdd = new();
    private readonly HashSet<string> _saleAdvanced = new();
    private List<QuestDefinitionSO> _saleQuests = new();
    internal Action<string, Rect> ObserveSaleControl;
    private Vector2 _saleGuiOrigin;
    private void ObserveSale(string id)
    {
        if (ObserveSaleControl == null || Event.current.type != EventType.Repaint) return;
        Rect rect = GUILayoutUtility.GetLastRect();
        rect.position = GUIUtility.GUIToScreenPoint(rect.position) - _saleGuiOrigin;
        ObserveSaleControl(id, rect);
    }
    private void EnableSaleRewardAuthoring()
    {
        ShopMakerAuthoring.Changed += OnSaleRewardChanged;
    }
    private void RefreshSaleRewardCatalog()
    {
        _saleQuests = AssetDatabase.FindAssets("t:QuestDefinitionSO").Select(g =>
            AssetDatabase.LoadAssetAtPath<QuestDefinitionSO>(AssetDatabase.GUIDToAssetPath(g))).Where(q => q != null).ToList();
    }
    private void OnSaleRewardChanged(UnityEngine.Object asset) { Invalidate(); }
    private void TrackSaleAsset(UnityEngine.Object asset)
    { if (asset != null && AssetDatabase.Contains(asset) && !_pendingSaleSaves.Contains(asset)) _pendingSaleSaves.Add(asset); }
    private void SaveSaleRewardAssets()
    {
        foreach (var asset in _pendingSaleSaves.Where(a => a != null && AssetDatabase.Contains(a)).ToArray()) AssetDatabase.SaveAssetIfDirty(asset);
        _pendingSaleSaves.RemoveAll(a => a == null || !EditorUtility.IsDirty(a));
    }
    private void EditSaleReward(SerializedObject draft, int index, Action edit)
    {
        // Commit this inspector's pending fields before another SerializedObject edits the same quest.
        draft.ApplyModifiedProperties(); edit();
        TrackSaleAsset(_selectedQuest); TrackSaleAsset(_selectedQuest.Rewards[index].ShopConfig);
        Invalidate(); GUIUtility.ExitGUI();
    }
    private bool ConfirmSharedSaleEdit(ShopConfigSO shop, string nodeId)
    {
        var all = _saleQuests.Concat(new[] { _selectedQuest }).Where(q => q != null).Distinct().ToList();
        int count = all.Sum(q => q.Rewards.Count(r => r != null && r.RewardType == QuestRewardType.ShopUnlock && r.ShopConfig == shop && r.ShopUnlockNodeId == nodeId));
        return count <= 1 || EditorUtility.DisplayDialog("여러 보상에 연결된 판매 해금 목록",
            "이 목록은 여러 퀘스트/보상에서 함께 사용합니다. 상품 추가·제거는 연결된 보상 모두에 반영됩니다. 기존 해금 ID는 유지됩니다. 수정할까요?", "함께 수정", "취소");
    }
    private void DrawSaleReward(SerializedProperty reward, int index)
    {
        string key = _selectedQuest.GetInstanceID() + ":" + index;
        EditorGUILayout.LabelField("완료 보상 · 상점 판매 해금", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("이 퀘스트 완료 → 아래 상품이 상점의 판매 후보에 추가됩니다. 아이템 지급이나 즉시 진열은 아닙니다.", MessageType.Info);
        var shopProperty = reward.FindPropertyRelative("_shopConfig");
        var idProperty = reward.FindPropertyRelative("_shopUnlockNodeId");
        var shop = shopProperty.objectReferenceValue as ShopConfigSO;
        EditorGUI.BeginChangeCheck();
        var nextShop = (ShopConfigSO)EditorGUILayout.ObjectField("해금할 상점", shop, typeof(ShopConfigSO), false);
        ObserveSale("sale:" + index + ":shop");
        if (EditorGUI.EndChangeCheck() && (string.IsNullOrEmpty(idProperty.stringValue) || EditorUtility.DisplayDialog("해금할 상점 변경",
                "현재 보상을 다른 상점으로 연결합니다. 기존 해금 묶음과 저장 ID는 삭제하지 않으며, 새 상점의 상품은 다시 선택해야 합니다.", "변경", "취소")))
            EditSaleReward(reward.serializedObject, index, () => ShopMakerAuthoring.BindRewardShop(_selectedQuest, index, nextShop));
        if (shop == null) { EditorGUILayout.HelpBox("① 상점 선택  →  ② 판매 해금할 ItemSO 추가  →  ③ 위의 Save", MessageType.None); return; }
        string id = idProperty.stringValue;
        bool blank = string.IsNullOrEmpty(id);
        bool found = shop.TryGetNode(id, out var node);
        if (!blank && !found) EditorGUILayout.HelpBox("기존 해금 연결이 누락됐어요. 저장 ID를 자동 교체하지 않습니다. 아래 고급 설정에서 연결을 확인하세요.", MessageType.Warning);
        if (found)
        {
            var others = ShopMakerAuthoring.QuestsFor(shop, id, _saleQuests).Where(q => q != _selectedQuest).ToArray();
            if (others.Length > 0) EditorGUILayout.HelpBox("같은 판매 해금 목록을 사용하는 퀘스트: " + string.Join(", ", others.Select(q => q.DisplayName)) +
                ". 이 중 하나가 완료되면 해금됩니다. 목록 수정은 모두에 반영됩니다.", MessageType.Warning);
            if (node.PrerequisiteNodeIds.Count > 0) EditorGUILayout.HelpBox("추가 조건: " + string.Join(", ", node.PrerequisiteNodeIds.Select(p =>
                shop.TryGetNode(p, out var prerequisite) ? prerequisite.DisplayName : "연결 누락: " + p)) + " 판매 해금도 먼저 완료해야 합니다.", MessageType.None);
        }
        EditorGUILayout.LabelField("판매 해금할 아이템", EditorStyles.boldLabel);
        ObserveSale("sale:" + index + ":items");
        if (found)
        foreach (string productId in node.UnlockedProductIds.ToArray())
        {
            var product = shop.Products.FirstOrDefault(p => p?.ProductId == productId);
            if (product == null) { EditorGUILayout.HelpBox("상품 연결 누락: " + productId, MessageType.Warning); continue; }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent(product.Item != null ? product.Item.name : "ItemSO 누락", "Shop Maker에서 이 상품의 판매 조건 보기")))
                    ShopProgressionTreeWindow.OpenForProduct(shop, productId);
                if (GUILayout.Button("목록에서 빼기", GUILayout.Width(95)) && ConfirmSharedSaleEdit(shop, id))
                    EditSaleReward(reward.serializedObject, index, () => ShopMakerAuthoring.RemoveRewardProduct(_selectedQuest, index, productId));
            }
            if (product.AvailableByDefault)
            {
                EditorGUILayout.HelpBox("이미 처음부터 판매하는 상품입니다. 현재 이 퀘스트로 제한되지 않아요.", MessageType.Warning);
                if (GUILayout.Button("이 상품을 퀘스트 완료 후 판매로 변경"))
                    EditSaleReward(reward.serializedObject, index, () => ShopMakerAuthoring.SetAvailableByDefault(shop, productId, false));
            }
        }
        if (!found || node.UnlockedProductIds.Count == 0) EditorGUILayout.LabelField("아직 해금할 아이템이 없어요. 아래에 ItemSO를 넣으세요.", EditorStyles.wordWrappedMiniLabel);
        using (new EditorGUI.DisabledScope(!blank && !found))
        {
            Rect drop = GUILayoutUtility.GetRect(1, 44, GUILayout.ExpandWidth(true));
            GUI.Box(drop, "+ 판매 해금할 ItemSO를 여기에 드래그\nItem Maker / Project · 여러 아이템 가능", EditorStyles.helpBox);
            ObserveSale("sale:" + index + ":drop");
            var e = Event.current;
            if (GUI.enabled && drop.Contains(e.mousePosition) && (e.type == EventType.DragUpdated || e.type == EventType.DragPerform))
            {
                var items = DragAndDrop.objectReferences.OfType<ItemSO>().ToArray();
                if (items.Length > 0)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (e.type == EventType.DragPerform && ConfirmSharedSaleEdit(shop, id))
                    { DragAndDrop.AcceptDrag(); EditSaleReward(reward.serializedObject, index, () => ShopMakerAuthoring.AddRewardProducts(_selectedQuest, index, items)); }
                    e.Use();
                }
            }
            _saleItemsToAdd.TryGetValue(key, out var item);
            using (new EditorGUILayout.HorizontalScope())
            {
                item = (ItemSO)EditorGUILayout.ObjectField(item, typeof(ItemSO), false); _saleItemsToAdd[key] = item;
                using (new EditorGUI.DisabledScope(item == null))
                    if (GUILayout.Button("+ 추가", GUILayout.Width(62)) && ConfirmSharedSaleEdit(shop, id))
                    { _saleItemsToAdd.Remove(key); EditSaleReward(reward.serializedObject, index, () => ShopMakerAuthoring.AddRewardProducts(_selectedQuest, index, new[] { item })); }
                ObserveSale("sale:" + index + ":add");
            }
            MakerRegistrationUi.ItemSummary(item, found && shop.Products.Any(p => p?.Item == item && node.UnlockedProductIds.Contains(p.ProductId))
                ? "이 퀘스트의 판매 해금 목록에 등록됨"
                : "이 퀘스트의 판매 해금 목록에는 미등록");
            EditorGUILayout.LabelField("추가하면 이 상점에 상품을 등록하고 '퀘스트 완료 후 판매'로 설정합니다. ItemSO의 가격·성능은 그대로입니다.", EditorStyles.wordWrappedMiniLabel);
        }
        if (GUILayout.Button("Shop Maker에서 연결된 상품 보기")) ShopProgressionTreeWindow.OpenForQuest(shop, _selectedQuest);
        bool advanced = EditorGUILayout.Foldout(_saleAdvanced.Contains(key), "고급 · 기존 해금 연결 / 저장용 ID", true);
        if (advanced) _saleAdvanced.Add(key); else _saleAdvanced.Remove(key);
        ObserveSale("sale:" + index + ":advanced");
        if (advanced)
        {
            EditorGUILayout.HelpBox("기존 묶음을 재사용할 때만 선택하세요. 연결된 다른 퀘스트에도 같은 상품 목록이 적용됩니다. 저장 ID는 직접 바꾸지 마세요.", MessageType.Info);
            DrawShopUnlockNodePicker(shopProperty, idProperty);
            EditorGUILayout.SelectableLabel(idProperty.stringValue, EditorStyles.textField, GUILayout.Height(20));
            if (GUILayout.Button("Shop Maker에서 기존 연결 관리")) ShopProgressionTreeWindow.OpenForGroup(shop, idProperty.stringValue);
        }
    }
}
