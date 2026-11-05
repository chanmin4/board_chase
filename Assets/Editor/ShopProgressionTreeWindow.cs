using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Shop Maker 창의 탐색/레이아웃. 기존 클래스와 .meta를 유지하여 열린 창의 연결도 보존한다.
/// 데이터 변경은 ShopMakerAuthoring, 선택 항목 편집은 .Workbench 파일이 담당한다.
/// </summary>
public sealed partial class ShopProgressionTreeWindow : EditorWindow
{
    [SerializeField] private ShopConfigSO _config;
    [SerializeField] private List<UnityEngine.Object> _pendingSaves = new();
    [SerializeField] private string _selectedProductId = "", _selectedGroupId = "";
    [SerializeField] private int _page, _productTab;
    private List<QuestDefinitionSO> _quests = new();
    private string _search = "", _category = "", _newGroupName = "New unlock group";
    private MakerSelectionUiState _selectionUi;
    private MakerSelectionUiState SelectionUi => _selectionUi ??= new MakerSelectionUiState();
    private ItemSO _registrationItem;
    private Vector2 _categoryScroll, _catalogScroll, _workbenchScroll;
    private float _leftWidth = 215, _rightWidth = 400;
    private int _draggingSplit;
    private bool _guide;
    private string _notice;
    private List<string> _validation;
    internal Action<string, Rect> ObserveControl;
    private Vector2 _guiScreenOrigin;
    private void Observe(string id, Rect rect)
    {
        if (ObserveControl == null || Event.current.type != EventType.Repaint) return;
        rect.position = GUIUtility.GUIToScreenPoint(rect.position) - _guiScreenOrigin;
        ObserveControl(id, rect);
    }

    private static string Pref(string key) => "BoardChase.ShopMaker." + Application.dataPath + "." + key;
    [MenuItem("Tools/Authoring/Shop Maker")]
    public static void Open() => GetWindow<ShopProgressionTreeWindow>("Shop Maker").Show();

    public static void OpenForGroup(ShopConfigSO config, string nodeId)
    {
        var window = GetWindow<ShopProgressionTreeWindow>("Shop Maker");
        window._config = config; window._selectedGroupId = nodeId; window._page = 1;
        window.SelectionUi.SelectionClicked();
        window._showLegacyUnlocks = true;
        window.Show(); window.Repaint();
    }

    public static void OpenForProduct(ShopConfigSO config, string productId)
    {
        var window = GetWindow<ShopProgressionTreeWindow>("Shop Maker");
        window._config = config; window._selectedProductId = productId; window._page = 0; window._productTab = 2;
        window.SelectionUi.SelectionClicked();
        window._category = window._search = ""; window._workbenchScroll = Vector2.zero;
        window.Show(); window.Repaint();
    }

    public static void OpenForQuest(ShopConfigSO config, QuestDefinitionSO quest)
    {
        var window = GetWindow<ShopProgressionTreeWindow>("Shop Maker");
        window._config = config; window._selectedUnlockQuest = quest; window._page = 1; window._showLegacyUnlocks = false;
        window.SelectionUi.SelectionClicked();
        window._workbenchScroll = Vector2.zero; window.RefreshCatalog(); window.Show(); window.Repaint();
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("Shop Maker");
        minSize = new Vector2(1060, 600);
        _leftWidth = EditorPrefs.GetFloat(Pref("LeftWidth"), 215);
        _rightWidth = EditorPrefs.GetFloat(Pref("RightWidth"), 400);
        _guide = EditorPrefs.GetBool(Pref("Guide"), true);
        if (_config == null)
        {
            _config = AssetDatabase.LoadAssetAtPath<ShopConfigSO>("Assets/ScriptableObject/GameScene/Shop/HomeShopConfig.asset");
            if (_config == null) _config = AssetDatabase.FindAssets("t:ShopConfigSO").Select(g =>
                AssetDatabase.LoadAssetAtPath<ShopConfigSO>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault();
        }
        RefreshCatalog();
        Undo.undoRedoPerformed += OnUndo;
        EditorApplication.projectChanged += OnProjectChanged;
        ShopMakerAuthoring.Changed += Track;
    }
    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndo;
        EditorApplication.projectChanged -= OnProjectChanged;
        ShopMakerAuthoring.Changed -= Track;
    }
    private void OnUndo() { _validation = null; Repaint(); }
    private void OnProjectChanged() { RefreshCatalog(); Repaint(); }
    private void RefreshCatalog()
    {
        _quests = AssetDatabase.FindAssets("t:QuestDefinitionSO")
            .Select(g => AssetDatabase.LoadAssetAtPath<QuestDefinitionSO>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(q => q != null).OrderBy(q => q.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
        _validation = null;
    }
    private void Track(UnityEngine.Object asset)
    {
        if (asset == null || !AssetDatabase.Contains(asset)) return;
        if (asset != null && !_pendingSaves.Contains(asset)) _pendingSaves.Add(asset);
        _validation = null; Repaint();
    }
    private void SaveTrackedAssets()
    {
        // 전역 SaveAssets로 다른 편집창/씬의 미저장 작업을 함께 저장하지 않는다.
        foreach (var asset in _pendingSaves.Where(a => a != null).ToArray()) AssetDatabase.SaveAssetIfDirty(asset);
        _pendingSaves.RemoveAll(a => a == null || !EditorUtility.IsDirty(a));
        ShowNotification(new GUIContent("Shop Maker changes saved"));
    }

    private void OnGUI()
    {
        _guiScreenOrigin = GUIUtility.GUIToScreenPoint(Vector2.zero);
        EditorGUILayout.LabelField("SHOP MAKER", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            EditorGUI.BeginChangeCheck();
            var next = (ShopConfigSO)EditorGUILayout.ObjectField(_config, typeof(ShopConfigSO), false, GUILayout.MinWidth(160));
            if (EditorGUI.EndChangeCheck()) { _config = next; _selectedProductId = _selectedGroupId = ""; _validation = null; }
            if (GUILayout.Button("New Shop...", EditorStyles.toolbarButton, GUILayout.Width(85))) CreateConfig();
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(65))) RefreshCatalog();
            if (GUILayout.Button("Item Maker", EditorStyles.toolbarButton, GUILayout.Width(85))) EditorApplication.ExecuteMenuItem("Tools/Authoring/Item Maker");
            if (GUILayout.Button("Guide", EditorStyles.toolbarButton, GUILayout.Width(50)))
            { _guide = !_guide; EditorPrefs.SetBool(Pref("Guide"), _guide); }
            using (new EditorGUI.DisabledScope(_config == null))
                if (GUILayout.Button("Check", EditorStyles.toolbarButton, GUILayout.Width(52)))
                { _validation = ShopMakerAuthoring.Validate(_config, _quests); _page = 2; }
            if (GUILayout.Button("Save" + (_pendingSaves.Any(a => a != null && EditorUtility.IsDirty(a)) ? " *" : ""),
                    EditorStyles.toolbarButton, GUILayout.Width(60))) SaveTrackedAssets();
        }
        if (_guide)
            EditorGUILayout.HelpBox("① ItemSO를 드래그해 상품 등록  →  ② 기본 가격·재고 설정  →  ③ 처음부터 판매 또는 퀘스트 해금 연결\n" +
                "판매 해금 상품은 Quest Maker의 보상에서 ItemSO로 선택합니다. '퀘스트별 판매 해금'에서 연결을 확인하세요. 해금은 다음 재입고 추첨의 후보 추가이며 즉시 진열 보장이 아닙니다. 변경 후 Save · Ctrl+Z: Undo", MessageType.Info);
        if (_config == null) { EditorGUILayout.HelpBox("Shop Config를 선택하거나 New Shop으로 시작하세요.", MessageType.Info); return; }
        _page = GUILayout.Toolbar(_page, new[] { "상품 목록", "퀘스트별 판매 해금", "상점 설정 / 검사" }, GUILayout.Height(26));
        if (!string.IsNullOrEmpty(_notice)) EditorGUILayout.HelpBox(_notice, MessageType.Info);
        float height = Mathf.Max(250, position.height - (_guide ? 142 : 94) - (!string.IsNullOrEmpty(_notice) ? 40 : 0));
        ClampWidths();
        using (new EditorGUILayout.HorizontalScope(GUILayout.Height(height)))
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(_leftWidth), GUILayout.Height(height)))
                DrawNavigation();
            DrawSplitter(1, height);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.Height(height)))
            {
                if (_page == 0) DrawCatalog();
                else if (_page == 1) DrawQuestUnlockCatalog();
                else DrawSettings();
            }
            if (_page != 2)
            {
                DrawSplitter(2, height);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(_rightWidth), GUILayout.Height(height)))
                {
                    _workbenchScroll = EditorGUILayout.BeginScrollView(_workbenchScroll);
                    if (_page == 0) DrawProductWorkbench();
                    else if (_showLegacyUnlocks) DrawGroupWorkbench();
                    else DrawQuestUnlockWorkbench();
                    EditorGUILayout.EndScrollView();
                }
            }
        }
    }

    private void ClampWidths()
    {
        _rightWidth = Mathf.Clamp(_rightWidth, 320, Mathf.Max(320, position.width - 510));
        _leftWidth = Mathf.Clamp(_leftWidth, 160, Mathf.Max(160, position.width - _rightWidth - 350));
    }
    private void DrawSplitter(int id, float height)
    {
        Rect rect = GUILayoutUtility.GetRect(6, height, GUILayout.Width(6), GUILayout.ExpandHeight(true));
        Observe("split:" + id, rect);
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
        EditorGUI.DrawRect(new Rect(rect.center.x - 1, rect.y, 2, rect.height), new Color(.25f, .45f, .5f));
        var e = Event.current;
        int control = GUIUtility.GetControlID(0x5140 + id, FocusType.Passive, rect);
        if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
        { _draggingSplit = id; GUIUtility.hotControl = control; e.Use(); }
        if (_draggingSplit == id && e.type == EventType.MouseDrag)
        { if (id == 1) _leftWidth += e.delta.x; else _rightWidth -= e.delta.x; ClampWidths(); e.Use(); Repaint(); }
        if (_draggingSplit == id && e.type == EventType.MouseUp)
        {
            _draggingSplit = 0; GUIUtility.hotControl = 0;
            EditorPrefs.SetFloat(Pref("LeftWidth"), _leftWidth); EditorPrefs.SetFloat(Pref("RightWidth"), _rightWidth); e.Use();
        }
    }

    private void DrawNavigation()
    {
        EditorGUILayout.LabelField(_page == 0 ? "ITEM CATEGORIES" : "WORKFLOW", EditorStyles.boldLabel);
        if (_page != 0)
        {
            EditorGUILayout.HelpBox(_page == 1
                ? "① 퀘스트 선택\n② Quest Maker에서 '상점 판매 해금' 보상 추가\n③ 해금할 ItemSO 추가 → Save\n\n여기서는 퀘스트별 상품 연결과 가격·판매 조건을 확인합니다."
                : "설정은 현재 Shop Config에 적용됩니다. 가격은 상품 목록에서 ItemSO를 직접 편집합니다.", MessageType.None);
            if (GUILayout.Button("← 상품 목록")) _page = 0;
            return;
        }
        _categoryScroll = EditorGUILayout.BeginScrollView(_categoryScroll);
        NavButton("", "All Items", _config.Products.Count);
        var items = _config.Products.Where(p => p?.Item != null).Select(p => p.Item).ToList();
        foreach (var family in new[] { ("Weapon", "Weapons"), ("Bullet", "Bullets"), ("Consumable", "Consumables") })
        {
            bool Match(ItemSO item)
            {
                string key = ItemMakerWindow.LootCategoryKey(item);
                return family.Item1 == "Consumable" ? key == "Consumable" || key == "Medical" || key == "Throwable" : key.StartsWith(family.Item1, StringComparison.Ordinal);
            }
            var members = items.Where(Match).ToList();
            if (members.Count == 0) continue;
            string pref = Pref("Category." + family.Item1);
            bool expanded = EditorPrefs.GetBool(pref, true);
            bool next = EditorGUILayout.Foldout(expanded, family.Item2 + " (" + members.Count + ")", true);
            if (next != expanded) EditorPrefs.SetBool(pref, next);
            if (next) DrawCategoryButtons(members);
        }
        DrawCategoryButtons(items.Where(i => { string k = ItemMakerWindow.LootCategoryKey(i); return !k.StartsWith("Weapon") && !k.StartsWith("Bullet") && k != "Consumable" && k != "Medical" && k != "Throwable"; }));
        EditorGUILayout.EndScrollView();
        EditorGUILayout.LabelField("분류는 ItemSO 기준입니다.\n상품 등록은 가운데로 드래그하세요.", EditorStyles.wordWrappedMiniLabel);
    }
    private void DrawCategoryButtons(IEnumerable<ItemSO> items)
    {
        foreach (var category in items.GroupBy(ItemMakerWindow.LootCategoryKey).OrderBy(g => ItemMakerWindow.LootCategoryOrder(g.First())))
            NavButton(category.Key, ItemMakerWindow.LootCategoryLabel(category.First()), category.Count());
    }
    private void NavButton(string key, string label, int count)
    {
        Color old = GUI.backgroundColor;
        if (_category == key) GUI.backgroundColor = new Color(.3f, .65f, .8f);
        if (GUILayout.Button(label + " (" + count + ")", GUILayout.Height(24))) { _category = key; _catalogScroll = Vector2.zero; }
        GUI.backgroundColor = old;
    }

    private void DrawCatalog()
    {
        EditorGUILayout.LabelField("SHOP PRODUCTS", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            _registrationItem = (ItemSO)EditorGUILayout.ObjectField(_registrationItem, typeof(ItemSO), false);
            using (new EditorGUI.DisabledScope(_registrationItem == null))
                if (GUILayout.Button("+ 등록", GUILayout.Width(60))) RegisterItems(new[] { _registrationItem });
        }
        if (_registrationItem != null)
            MakerRegistrationUi.ItemSummary(_registrationItem, _config.Products.Any(p => p?.Item == _registrationItem)
                ? "이 상점에 등록됨 — 다시 등록하면 기존 상품을 선택" : "이 상점에는 미등록");
        Rect drop = GUILayoutUtility.GetRect(1, 38, GUILayout.ExpandWidth(true));
        GUI.Box(drop, "Item Maker / Project의 ItemSO를 여기에 드래그\n중복 등록은 기존 상품을 선택합니다.", EditorStyles.helpBox);
        var e = Event.current;
        if (drop.Contains(e.mousePosition) && (e.type == EventType.DragUpdated || e.type == EventType.DragPerform))
        {
            var items = DragAndDrop.objectReferences.OfType<ItemSO>().Where(AssetDatabase.Contains).ToArray();
            if (items.Length > 0)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (e.type == EventType.DragPerform) { DragAndDrop.AcceptDrag(); RegisterItems(items); }
                e.Use();
            }
        }
        _search = EditorGUILayout.TextField("Search", _search);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("등급 → 카테고리 → 이름", EditorStyles.miniLabel);
            if (GUILayout.Button("Expand All", EditorStyles.miniButton)) SetRarityFoldouts(true);
            if (GUILayout.Button("Collapse All", EditorStyles.miniButton)) SetRarityFoldouts(false);
        }
        var rows = ShopMakerAuthoring.SortedProducts(_config, _category, _search);
        _catalogScroll = EditorGUILayout.BeginScrollView(_catalogScroll);
        foreach (var group in rows.GroupBy(i => _config.Products[i].Item != null ? _config.Products[i].Item.Rarity.ToString() : "Missing Item"))
        {
            string key = Pref("Rarity." + group.Key);
            bool open = EditorPrefs.GetBool(key, true);
            bool next = EditorGUILayout.Foldout(open, group.Key + " (" + group.Count() + ")", true, EditorStyles.foldoutHeader);
            Observe("rarity:" + group.Key, GUILayoutUtility.GetLastRect());
            if (next != open) EditorPrefs.SetBool(key, next);
            if (!next) continue;
            int columns = Mathf.Max(1, Mathf.FloorToInt((position.width - _leftWidth - _rightWidth - 65) / 145));
            var indices = group.ToArray();
            for (int start = 0; start < indices.Length; start += columns)
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int j = start; j < Math.Min(indices.Length, start + columns); j++) DrawCard(indices[j]);
                    GUILayout.FlexibleSpace();
                }
        }
        if (rows.Count == 0) EditorGUILayout.HelpBox("표시할 상품이 없습니다. 필터를 바꾸거나 ItemSO를 등록하세요.", MessageType.Info);
        EditorGUILayout.EndScrollView();
    }
    private void SetRarityFoldouts(bool value)
    {
        foreach (var p in _config.Products.Where(p => p != null))
            EditorPrefs.SetBool(Pref("Rarity." + (p.Item != null ? p.Item.Rarity.ToString() : "Missing Item")), value);
    }
    private void RegisterItems(IEnumerable<ItemSO> items)
    {
        Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup();
        foreach (var item in items)
        {
            int index = ShopMakerAuthoring.Register(_config, item);
            _selectedProductId = _config.Products[index].ProductId;
        }
        Undo.CollapseUndoOperations(undo);
        _category = _search = ""; _registrationItem = null; SetRarityFoldouts(true);
        _notice = null; Repaint();
    }
    private void DrawCard(int index)
    {
        var p = _config.Products[index];
        string registrationText = p.Item != null ? MakerRegistrationIndex.Current.For(p.Item).LootLabel : "";
        float registrationHeight = Mathf.Max(37, MakerRegistrationUi.BadgeHeight(registrationText, 128));
        float height = 177 + registrationHeight;
        Rect r = GUILayoutUtility.GetRect(140, height, GUILayout.Width(140), GUILayout.Height(height));
        Color old = GUI.backgroundColor;
        if (_selectedProductId == p.ProductId) GUI.backgroundColor = new Color(.3f, .65f, .8f);
        if (GUI.Button(r, GUIContent.none)) { SelectionUi.SelectionClicked(); _selectedProductId = p.ProductId; _workbenchScroll = Vector2.zero; }
        GUI.backgroundColor = old;
        Observe("product:" + p.ProductId, r);
        if (p.Item != null)
        {
            Texture texture = p.Item.PreviewImage != null ? p.Item.PreviewImage.texture : AssetPreview.GetAssetPreview(p.Item);
            if (texture == null) texture = AssetPreview.GetMiniThumbnail(p.Item);
            if (texture != null) GUI.DrawTexture(new Rect(r.x + 30, r.y + 5, 80, 75), texture, ScaleMode.ScaleToFit);
            if (AssetPreview.IsLoadingAssetPreviews()) Repaint();
        }
        GUI.Label(new Rect(r.x + 6, r.y + 82, 128, 34), new GUIContent(p.Item != null ? p.Item.name : "Missing Item", p.ProductId), EditorStyles.wordWrappedMiniLabel);
        GUI.Label(new Rect(r.x + 6, r.y + 117, 128, 18), p.UsesPhysicalPayment ? "물물교환" : "가격 " + p.ResolvePrice(), EditorStyles.miniLabel);
        GUI.Label(new Rect(r.x + 6, r.y + 136, 128, 18), p.AvailableByDefault ? "처음부터 추첨 가능" : "퀘스트 해금 필요", EditorStyles.miniLabel);
        GUI.Label(new Rect(r.x + 6, r.y + 151, 128, 18), "재고 " + p.Stock + "  |  비중 " + p.OfferWeight.ToString("0.##"), EditorStyles.miniLabel);
        MakerRegistrationUi.Badge(new Rect(r.x + 6, r.y + 173, 128, registrationHeight), p.Item);
    }

    private void CreateConfig()
    {
        string path = EditorUtility.SaveFilePanelInProject("New Shop", "ShopConfig", "asset", "새 상점 설정 저장 위치");
        if (string.IsNullOrWhiteSpace(path)) return;
        if (AssetDatabase.LoadMainAssetAtPath(path) != null) { _notice = "기존 에셋을 덮어쓸 수 없습니다."; return; }
        var config = CreateInstance<ShopConfigSO>();
        var so = new SerializedObject(config);
        so.FindProperty("_configId").stringValue = "shop_" + Guid.NewGuid().ToString("N");
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(config, path); _config = config;
        _selectedProductId = _selectedGroupId = ""; _page = 2; _validation = null;
    }
}
