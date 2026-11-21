/*
 * Role hierarchy: Authoring > Editor > Items
 * Primary responsibility: Browses, creates, edits, previews, and connects ItemSO assets to fixed world loot tables.
 * Execution boundary: Editor only (Assembly-CSharp-Editor)
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Single authoring workspace for all physical ItemSO assets. This deliberately
/// keeps item data, thumbnail generation, and fixed world-loot registration in
/// one place so authors do not have to jump between the Project browser,
/// IconMaker, and Loot Maker for everyday content work.
/// </summary>
public sealed partial class ItemMakerWindow : EditorWindow
{
    private const string ItemRootFolder = "Assets/ScriptableObject/GameScene/GameItem";
    private const string DefaultIconOutputFolder = "Assets/Art/Texture/GeneratedIcons";
    private const int PreviewResolution = 512;
    private const int PreviewCaptureLayer = 31;
    private const int PreviewViewportControlHint = 0x1A7E9;
    private const float CompactWindowScale = 0.8f;
    private const string CompactWindowLayoutPreferenceKey = "BoardChase.ItemMakerWindow.CompactLayout.v1";
    private static readonly Vector2 CompactMinimumWindowSize = new(1008f, 560f);

    private enum ItemCatalogCategory
    {
        All,
        Weapon,
        Bullet,
        Consumable,
        Medical,
        Throwable,
        Armor,
        HeadEquipment,
        Backpack,
        Key,
        Passive,
        Currency,
        Valuable,
        Other,
        MeleeWip,
        WeaponSmg,
        WeaponAr,
        WeaponLmg,
        WeaponPistol,
        WeaponShotgun,
        WeaponSniper,
        WeaponSpecial,
        BulletLight,
        BulletMedium,
        BulletHeavy,
        BulletShotgun,
        BulletSniper,
        BulletSpecial
    }

    // Explicit hierarchy order keeps existing enum identities stable.
    private static readonly ItemCatalogCategory[] CategoryOrder =
    {
        ItemCatalogCategory.All,
        ItemCatalogCategory.Weapon,
        ItemCatalogCategory.WeaponSmg, ItemCatalogCategory.WeaponAr, ItemCatalogCategory.WeaponLmg,
        ItemCatalogCategory.WeaponPistol, ItemCatalogCategory.WeaponShotgun,
        ItemCatalogCategory.WeaponSniper, ItemCatalogCategory.WeaponSpecial,
        ItemCatalogCategory.Bullet,
        ItemCatalogCategory.BulletLight, ItemCatalogCategory.BulletMedium, ItemCatalogCategory.BulletHeavy,
        ItemCatalogCategory.BulletShotgun, ItemCatalogCategory.BulletSniper, ItemCatalogCategory.BulletSpecial,
        ItemCatalogCategory.Consumable, ItemCatalogCategory.Medical, ItemCatalogCategory.Throwable,
        ItemCatalogCategory.Armor, ItemCatalogCategory.HeadEquipment, ItemCatalogCategory.Backpack,
        ItemCatalogCategory.Key, ItemCatalogCategory.Passive,
        ItemCatalogCategory.Valuable, ItemCatalogCategory.Other, ItemCatalogCategory.MeleeWip
    };

    private enum WorkbenchTab
    {
        Details,
        PreviewImage,
        LootLinks,
        Module
    }

    private readonly struct ItemTemplate
    {
        public ItemTemplate(string label, Type type, ItemCatalogCategory category, string folder,
            RangedWeaponType? weaponType = null)
        {
            Label = label;
            Type = type;
            Category = category;
            Folder = folder;
            WeaponType = weaponType;
        }

        public string Label { get; }
        public Type Type { get; }
        public ItemCatalogCategory Category { get; }
        public string Folder { get; }
        public RangedWeaponType? WeaponType { get; }
    }

    private static readonly ItemTemplate[] ItemTemplates =
    {
        new("Misc Item", typeof(ItemSO), ItemCatalogCategory.Other, ItemRootFolder + "/Misc"),
        new("Weapon / SMG", typeof(RangedWeaponSO), ItemCatalogCategory.WeaponSmg, ItemRootFolder + "/Weapon/SMG", RangedWeaponType.SubmachineGun),
        new("Weapon / AR", typeof(RangedWeaponSO), ItemCatalogCategory.WeaponAr, ItemRootFolder + "/Weapon/AR", RangedWeaponType.AssaultRifle),
        new("Weapon / LMG", typeof(RangedWeaponSO), ItemCatalogCategory.WeaponLmg, ItemRootFolder + "/Weapon/LMG", RangedWeaponType.LightMachineGun),
        new("Weapon / Pistol", typeof(RangedWeaponSO), ItemCatalogCategory.WeaponPistol, ItemRootFolder + "/Weapon/Pistol", RangedWeaponType.Pistol),
        new("Weapon / Revolver", typeof(RangedWeaponSO), ItemCatalogCategory.WeaponPistol, ItemRootFolder + "/Weapon/Pistol", RangedWeaponType.Revolver),
        new("Weapon / Shotgun", typeof(ShotgunWeaponSO), ItemCatalogCategory.WeaponShotgun, ItemRootFolder + "/Weapon/Shotgun", RangedWeaponType.Shotgun),
        new("Weapon / Sniper Rifle", typeof(RangedWeaponSO), ItemCatalogCategory.WeaponSniper, ItemRootFolder + "/Weapon/SniperRifle", RangedWeaponType.SniperRifle),
        new("Weapon / Special / Rocket Launcher", typeof(RangedWeaponSO), ItemCatalogCategory.WeaponSpecial, ItemRootFolder + "/Weapon/Special", RangedWeaponType.RocketLauncher),
        new("WIP Melee Weapon", typeof(MeleeWeaponSO), ItemCatalogCategory.MeleeWip, ItemRootFolder + "/Weapon/WIP"),
        new("Bullet / Light (Pistol / SMG)", typeof(LightBulletSO), ItemCatalogCategory.BulletLight, ItemRootFolder + "/Bullet/Light"),
        new("Bullet / Medium (AR)", typeof(MediumBulletSO), ItemCatalogCategory.BulletMedium, ItemRootFolder + "/Bullet/Medium"),
        new("Bullet / Heavy (LMG / Revolver)", typeof(HeavyBulletSO), ItemCatalogCategory.BulletHeavy, ItemRootFolder + "/Bullet/Heavy"),
        new("Bullet / Sniper", typeof(SniperBulletSO), ItemCatalogCategory.BulletSniper, ItemRootFolder + "/Bullet/Sniper"),
        new("Bullet / Shotgun Shell", typeof(ShotgunShellSO), ItemCatalogCategory.BulletShotgun, ItemRootFolder + "/Bullet/Shotgun"),
        new("Bullet / Special / Rocket", typeof(RocketBulletSO), ItemCatalogCategory.BulletSpecial, ItemRootFolder + "/Bullet/Special"),
        new("Consumable", typeof(ConsumableItemSO), ItemCatalogCategory.Consumable, ItemRootFolder + "/Consumable"),
        new("Medical Item", typeof(MedicalItemSO), ItemCatalogCategory.Medical, ItemRootFolder + "/Consumable/Medical"),
        new("Throwable", typeof(ThrowableUsableItemSO), ItemCatalogCategory.Throwable, ItemRootFolder + "/Consumable/Throwable"),
        new("Armor", typeof(ArmorItemSO), ItemCatalogCategory.Armor, ItemRootFolder + "/Armor"),
        new("Gas Mask", typeof(GasMaskItemSO), ItemCatalogCategory.HeadEquipment, ItemRootFolder + "/GasMask"),
        new("Backpack", typeof(BackpackItemSO), ItemCatalogCategory.Backpack, ItemRootFolder + "/Backpack"),
        new("Key", typeof(KeyItemSO), ItemCatalogCategory.Key, ItemRootFolder + "/Key"),
        new("Key Tool", typeof(KeyToolItemSO), ItemCatalogCategory.Key, ItemRootFolder + "/Key"),
        new("모듈", typeof(PassiveItemSO), ItemCatalogCategory.Passive, ItemRootFolder + "/Module"),
        new("Valuable", typeof(ValuableItemSO), ItemCatalogCategory.Valuable, ItemRootFolder + "/Valuable"),
        new("Valuable / 화폐", typeof(CurrencyItemSO), ItemCatalogCategory.Valuable, ItemRootFolder + "/Valuable"),
        new("Medical / Infection Ampoule or Syringe", typeof(InfectionCureItemSO), ItemCatalogCategory.Medical, ItemRootFolder + "/Consumable/Medical")
    };

    private readonly List<ItemSO> _items = new();
    private readonly List<LootTableSO> _lootTables = new();
    private readonly List<LootObjectSO> _lootContainers = new();
    private ItemCatalogCategory _category = ItemCatalogCategory.All;
    private const string CategoryTreeScope = "ItemMaker";
    private static readonly string[] CategoryTreeGroups = { "Weapon", "Bullet", "Consumable" };
    private WorkbenchTab _workbenchTab;
    private ItemSO _selectedItem;
    private readonly ItemMakerSelection _selection = new();
    private LootTableSO _selectedLootTable;
    private string _search = string.Empty;
    private string _newItemName = "New Item";
    private string _assetNameDraft = string.Empty;
    private MakerSelectionUiState _itemNameUi;
    private MakerSelectionUiState ItemNameUi => _itemNameUi ??= new MakerSelectionUiState();
    private int _templateIndex;
    private int _lootMinAmount = 1;
    private int _lootMaxAmount = 1;
    private Vector2 _categoryScroll;
    private Vector2 _createItemScroll;
    private float _createPanelHeight = DefaultCreatePanelHeight;
    private bool _createPanelExpanded = true;
    private int _categorySplitterControl;
    private float _categorySplitStartY, _categorySplitStartHeight;
    internal const float DefaultCreatePanelHeight = 184f;
    internal static string SidebarPreferenceKey => "BoardChase.ItemMaker.Sidebar.v1." + Application.dataPath;
    private Vector2 _catalogScroll;
    private Vector2 _workbenchScroll;
    private ItemSO _dragCandidate;
    private Vector2 _itemDragStart;
    private int _itemDragControl;
    private bool _collapseSelectionOnMouseUp;

    private GameObject _previewSourcePrefab;
    private bool _previewSourceFollowsItem = true;
    private string _iconOutputFolder = DefaultIconOutputFolder;
    private int _iconResolution = PreviewResolution;
    private float _previewYaw = 28f;
    private float _previewPitch = 14f;
    private float _previewZoom = 1.18f;
    private Vector2 _previewDragLastPosition;
    private bool _transparentPreviewBackground = true;
    private Color _previewBackgroundColor = new(0.045f, 0.075f, 0.09f, 1f);
    // Window layouts/domain reload must not serialize DontSave preview resources.
    [NonSerialized] private PreviewRenderUtility _previewUtility;
    [NonSerialized] private Camera _previewCamera;
    [NonSerialized] private GameObject _previewInstance;
    [NonSerialized] private RenderTexture _previewTexture;
    private bool _previewDirty = true;

    [MenuItem("Tools/Authoring/Item Maker")]
    // Purpose: Opens the consolidated ItemSO authoring workspace.
    public static void Open()
    {
        ItemMakerWindow window = GetWindow<ItemMakerWindow>("Item Maker");
        window.ApplyCompactWindowLayoutOnce();
        window.TryUseSelectedItem();
        window.Show();
    }

    // Purpose: Loads catalog data and the isolated thumbnail capture scene when the window is opened.
    private void OnEnable()
    {
        ApplyCompactWindowLayoutOnce();
        _createPanelHeight = EditorPrefs.GetFloat(SidebarPreferenceKey + ".CreateHeight", DefaultCreatePanelHeight);
        _createPanelExpanded = EditorPrefs.GetBool(SidebarPreferenceKey + ".CreateExpanded", true);
        RefreshCatalogs();
        EnsurePreviewStudio();
        EnsurePreviewInstance();
    }

    // Purpose: Applies the requested 20% compact editor-window layout once without overriding later manual resizing.
    private void ApplyCompactWindowLayoutOnce()
    {
        minSize = CompactMinimumWindowSize;

        if (EditorPrefs.GetBool(CompactWindowLayoutPreferenceKey, false))
            return;

        Rect bounds = position;
        bounds.width = Mathf.Max(CompactMinimumWindowSize.x, bounds.width * CompactWindowScale);
        bounds.height = Mathf.Max(CompactMinimumWindowSize.y, bounds.height * CompactWindowScale);
        position = bounds;

        EditorPrefs.SetBool(CompactWindowLayoutPreferenceKey, true);
    }

    // Purpose: Releases only temporary preview objects when the window closes.
    private void OnDisable()
    {
        ReleaseCategorySplitter();
        DisposePreviewStudio();
    }

    private void OnLostFocus() => ReleaseCategorySplitter();

    private void ReleaseCategorySplitter()
    {
        if (_categorySplitterControl == 0) return;
        if (GUIUtility.hotControl == _categorySplitterControl) GUIUtility.hotControl = 0;
        _categorySplitterControl = 0;
        SaveSidebarLayout();
    }

    private void SaveSidebarLayout()
    {
        EditorPrefs.SetFloat(SidebarPreferenceKey + ".CreateHeight", _createPanelHeight);
        EditorPrefs.SetBool(SidebarPreferenceKey + ".CreateExpanded", _createPanelExpanded);
    }

    // Purpose: Refreshes cached asset catalogues after Project-window asset changes.
    private void OnProjectChange()
    {
        RefreshCatalogs();
        Repaint();
    }

    // Purpose: Makes a Project-window ItemSO selection immediately editable in this workspace.
    private void OnSelectionChange()
    {
        if (Selection.activeObject is ItemSO item)
        {
            ItemSO[] selected = Selection.objects.OfType<ItemSO>().ToArray();
            // Our own Project selection update must not collapse a multi-selection.
            if (selected.Length == _selection.Items.Count && selected.All(_selection.Contains)) return;
            _selection.Set(selected, item);
            if (item is ValuableItemSO selectedValuable && _valuableCategoryFilter != null &&
                selectedValuable.ValuableCategoryId != _valuableCategoryFilter) _valuableCategoryFilter = null;
            if (!MakerRegistrationIndex.Current.For(item).Matches(_registrationFilter)) _registrationFilter = MakerRegistrationIndex.Filter.All;
            if (!MatchesCategory(item, _category) || !MatchesSearch(item, _search))
            {
                _category = ItemCatalogCategory.All;
                _search = string.Empty;
            }
            ApplyCatalogSelection(false);
            ItemMakerCatalogView.SetExpanded(item.Rarity, true);
            Repaint();
        }
        else if (Selection.activeObject is GameObject prefab && EditorUtility.IsPersistent(prefab))
        {
            _registrationPrefab = MakerRegistrationIndex.CanonicalPrefab(prefab);
            _showPrefabRegistration = true;
            Repaint();
        }
    }

    // Purpose: Draws the catalogue, card browser, and selected-item workbench.
    private void OnGUI()
    {
        MakerCategoryTreeUi.BeginWindow(this);
        EnsurePreviewStudio();
        EnsurePreviewInstance();
        AuthoringWorkspaceUi.DrawHeader(
            "Item Maker",
            "Owner: Items / Loot / UI",
            "Create and maintain every physical ItemSO from one catalogue. Select a card to edit data, capture its UI image, or add it to a container's fixed loot pool.",
            "1 Browse", "2 Create", "3 Edit", "4 Preview Image", "5 Add To Loot");

        DrawToolbar();
        float workspaceHeight = Mathf.Max(376f, position.height - 144f);
        using (new EditorGUILayout.HorizontalScope(GUILayout.Height(workspaceHeight)))
        {
            DrawCategoryPane(workspaceHeight);
            DrawItemCatalogPane(workspaceHeight);
            DrawWorkbenchPane(workspaceHeight);
        }
        if ((Event.current.rawType == EventType.MouseUp || Event.current.type == EventType.DragExited) && _dragCandidate != null)
        {
            _dragCandidate = null;
            _collapseSelectionOnMouseUp = false;
            if (GUIUtility.hotControl == _itemDragControl) GUIUtility.hotControl = 0;
        }
    }

    // Purpose: Draws project-level actions that do not mutate any selected item by themselves.
    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Refresh Catalog", EditorStyles.toolbarButton, GUILayout.Width(106f)))
                RefreshCatalogs();

            if (GUILayout.Button("Open Loot Maker", EditorStyles.toolbarButton, GUILayout.Width(108f)))
                LootProgressionEditorWindow.Open();
            if (GUILayout.Button("제작 Maker", EditorStyles.toolbarButton, GUILayout.Width(88f)))
                CraftingMakerWindow.Open();
            if (GUILayout.Button("물품 분류 관리", EditorStyles.toolbarButton, GUILayout.Width(105f)))
                ValuableCategoryEditorWindow.Open();

            using (new EditorGUI.DisabledScope(!ItemCategoryMoveUtility.HasLastMove || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button(new GUIContent("Restore Last Move", "Restore the entire last moved selection. Newer edits are never overwritten."),
                        EditorStyles.toolbarButton, GUILayout.Width(114f)))
                    EditorApplication.delayCall += RestoreLastCategoryMove;
            }

            using (new EditorGUI.DisabledScope(_selectedItem == null))
            {
                if (GUILayout.Button("Select Items", EditorStyles.toolbarButton, GUILayout.Width(82f)))
                    SyncProjectSelection();
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label($"{_items.Count} ItemSO assets", EditorStyles.miniLabel);
        }
    }

    // Use explicit viewports: flexible GUILayout scroll views can otherwise leave unused space below the palette.
    private void DrawCategoryPane(float height)
    {
        Rect pane = GUILayoutUtility.GetRect(244f, height, GUILayout.Width(244f), GUILayout.Height(height));
        // Layout supplies dummy rects. Never derive persisted dimensions from them.
        pane.width = 244f;
        pane.height = height;
        GUI.Box(pane, GUIContent.none, EditorStyles.helpBox);
        GUI.BeginGroup(pane);
        try
        {
            MakerCategoryTreeUi.Observe("ItemMaker/SidebarViewport", new Rect(0, 0, pane.width, pane.height));
            const float width = 234f;
            var hint = new GUIContent("Ctrl+click: toggle selection. Shift+click: select a range. Drag selected cards onto a category to move them together.");
            float hintHeight = EditorStyles.wordWrappedMiniLabel.CalcHeight(hint, width);
            GUI.Label(new Rect(5, 5, width, 18), "ITEM CATEGORIES", EditorStyles.boldLabel);
            GUI.Label(new Rect(5, 25, width, hintHeight), hint, EditorStyles.wordWrappedMiniLabel);
            float toolbarY = 28 + hintHeight;
            GUILayout.BeginArea(new Rect(5, toolbarY, width, 22));
            try { MakerCategoryTreeUi.DrawToolbar(CategoryTreeScope, CategoryTreeGroups); }
            finally { GUILayout.EndArea(); }
            GUI.Label(new Rect(5, toolbarY + 24, width, 18), "Viewing: " + GetCategoryLabel(_category), EditorStyles.miniLabel);

            SidebarLayout layout = CalculateSidebarLayout(height, toolbarY + 44, _createPanelHeight, _createPanelExpanded);
            MakerCategoryTreeUi.Observe("ItemMaker/CategoryViewport", layout.Categories);
            GUILayout.BeginArea(layout.Categories);
            try
            {
                _categoryScroll = EditorGUILayout.BeginScrollView(_categoryScroll, GUILayout.Height(layout.Categories.height));
                foreach (ItemCatalogCategory category in CategoryOrder)
                {
                    var parent = GetParentCategory(category);
                    if (!parent.HasValue || MakerCategoryTreeUi.IsExpanded(CategoryTreeScope, parent.Value.ToString()))
                        DrawCategoryCard(category);
                    if (category == ItemCatalogCategory.Valuable) DrawValuableCategoryFilters();
                }
                EditorGUILayout.EndScrollView();
            }
            finally { GUILayout.EndArea(); }

            DrawCategorySplitter(layout, height, toolbarY + 44);
            MakerCategoryTreeUi.Observe("ItemMaker/CreateFoldout", layout.CreateHeader);
            bool expanded = EditorGUI.Foldout(layout.CreateHeader, _createPanelExpanded,
                new GUIContent("CREATE ITEM", "Collapse to give categories all available height. Drag the divider above to resize."), true, EditorStyles.foldoutHeader);
            if (expanded != _createPanelExpanded)
            {
                _createPanelExpanded = expanded;
                SaveSidebarLayout();
                Repaint();
                GUIUtility.ExitGUI();
            }
            if (_createPanelExpanded)
            {
                MakerCategoryTreeUi.Observe("ItemMaker/CreateViewport", layout.CreateBody);
                GUILayout.BeginArea(layout.CreateBody);
                try
                {
                    _createItemScroll = EditorGUILayout.BeginScrollView(_createItemScroll, GUILayout.Height(layout.CreateBody.height));
                    DrawCreateItemPalette();
                    EditorGUILayout.EndScrollView();
                }
                finally { GUILayout.EndArea(); }
            }
        }
        finally { GUI.EndGroup(); }
    }

    internal readonly struct SidebarLayout
    {
        internal readonly Rect Categories, Splitter, CreateHeader, CreateBody;
        internal SidebarLayout(Rect categories, Rect splitter, Rect header, Rect body)
        { Categories = categories; Splitter = splitter; CreateHeader = header; CreateBody = body; }
    }

    internal static SidebarLayout CalculateSidebarLayout(float height, float listTop, float requestedCreateHeight, bool expanded)
    {
        const float padding = 5f, width = 234f, divider = 8f, header = 22f;
        float available = Mathf.Max(0, height - padding - listTop - divider - header);
        float createHeight = 0;
        if (expanded)
        {
            float max = Mathf.Max(0, available - 100f);
            if (float.IsNaN(requestedCreateHeight) || float.IsInfinity(requestedCreateHeight)) requestedCreateHeight = DefaultCreatePanelHeight;
            createHeight = Mathf.Clamp(requestedCreateHeight, Mathf.Min(100f, max), max);
        }
        var categories = new Rect(padding, listTop, width, available - createHeight);
        var splitter = new Rect(padding, categories.yMax, width, divider);
        var createHeader = new Rect(padding, splitter.yMax, width, header);
        return new SidebarLayout(categories, splitter, createHeader, new Rect(padding, createHeader.yMax, width, createHeight));
    }

    private void DrawCategorySplitter(SidebarLayout layout, float height, float listTop)
    {
        Rect rect = layout.Splitter;
        int id = GUIUtility.GetControlID(0x1A7E8, FocusType.Passive, rect);
        MakerCategoryTreeUi.Observe("ItemMaker/CategorySplitter", rect);
        bool active = GUIUtility.hotControl == id && _categorySplitterControl == id;
        bool hovered = _createPanelExpanded && rect.Contains(Event.current.mousePosition);
        if (_createPanelExpanded) EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical, id);
        EditorGUI.DrawRect(new Rect(rect.x, rect.center.y - 1, rect.width, 2), active || hovered
            ? new Color(0.25f, 0.7f, 0.8f) : new Color(0.35f, 0.35f, 0.35f));
        GUI.Label(rect, new GUIContent("", "Drag up/down to resize categories. Double-click to reset."));
        Event e = Event.current;
        if (_createPanelExpanded && e.type == EventType.MouseDown && e.button == 0 && hovered)
        {
            if (e.clickCount == 2)
            {
                _createPanelHeight = DefaultCreatePanelHeight;
                SaveSidebarLayout(); e.Use(); Repaint(); GUIUtility.ExitGUI();
            }
            _categorySplitStartY = e.mousePosition.y;
            _categorySplitStartHeight = layout.CreateBody.height;
            _categorySplitterControl = id;
            GUIUtility.hotControl = id;
            GUI.FocusControl(null);
            e.Use();
        }
        else if (active && e.type == EventType.MouseDrag)
        {
            // Dragging down grants more height to categories; the create panel keeps its own scroll bar.
            _createPanelHeight = CalculateSidebarLayout(height, listTop,
                _categorySplitStartHeight - (e.mousePosition.y - _categorySplitStartY), true).CreateBody.height;
            e.Use(); Repaint(); GUIUtility.ExitGUI();
        }
        else if (active && e.rawType == EventType.MouseUp)
        {
            ReleaseCategorySplitter(); e.Use(); Repaint();
        }
    }

    // Purpose: Draws one selectable category with its live number of matching ItemSO assets.
    private void DrawCategoryCard(ItemCatalogCategory category)
    {
        bool selected = _category == category || GetParentCategory(_category) == category;
        string label = $"{GetCategoryLabel(category)}  ({CountItems(category)})";
        Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.button, GUILayout.Height(25f), GUILayout.ExpandWidth(true));
        // The complete row remains a drop target, including the collapsed parent and disclosure arrow.
        HandleCategoryDrop(rect, category);
        if (MakerCategoryTreeUi.DrawRow(rect, CategoryTreeScope, category.ToString(),
            new GUIContent(label, label + "\nArrow: expand / collapse. Name: filter. Drop: move items."),
            selected, IsUmbrellaCategory(category), GetParentCategory(category).HasValue ? 18 : 0))
        {
            _category = category;
            if (category == ItemCatalogCategory.Valuable) _valuableCategoryFilter = null;
            _catalogScroll = Vector2.zero;
            int matchingTemplate = Array.FindIndex(ItemTemplates, template => template.Category == category);
            if (matchingTemplate >= 0) _templateIndex = matchingTemplate;
        }
    }

    private void RevealSelectedCategory()
    {
        var parent = GetParentCategory(_category);
        if (parent.HasValue) MakerCategoryTreeUi.SetExpanded(CategoryTreeScope, parent.Value.ToString(), true);
    }

    // Purpose: Creates one new ItemSO in the canonical folder chosen by its template.
    private void DrawCreateItemPalette()
    {
        float previousLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 66f;
        try
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                _newItemName = EditorGUILayout.TextField("Name", _newItemName);
                _templateIndex = EditorGUILayout.Popup("Template", _templateIndex, GetTemplateLabels());
                ItemTemplate template = ItemTemplates[Mathf.Clamp(_templateIndex, 0, ItemTemplates.Length - 1)];
                EditorGUILayout.LabelField("Category", GetCategoryLabel(template.Category), EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Folder", EditorStyles.miniLabel);
                EditorGUILayout.LabelField(template.Folder, EditorStyles.wordWrappedMiniLabel);

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_newItemName)))
                {
                    if (GUILayout.Button("+ Create ItemSO", GUILayout.Height(26f)))
                        CreateItem(template);
                }
            }
        }
        finally { EditorGUIUtility.labelWidth = previousLabelWidth; }
    }

    // Purpose: Draws the searchable item thumbnail catalogue in Asset Store-style cards.
    private void DrawItemCatalogPane(float height)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(Mathf.Max(360f, position.width - 704f)), GUILayout.Height(height)))
        {
            EditorGUILayout.LabelField("ITEM CATALOG", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _search = EditorGUILayout.TextField("Search", _search);
                GUILayout.Label($"{GetFilteredItems().Count} items / {_selection.Items.Count} selected", EditorStyles.miniLabel, GUILayout.Width(135f));
            }
            DrawRegistrationFilters();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(new GUIContent("Rarity > Name (A-Z)", "Normal > Rare > Unique > Legendary. Within each group: card/asset name, case-insensitive A-Z."), EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Expand All", EditorStyles.toolbarButton, GUILayout.Width(78)))
                {
                    ItemMakerCatalogView.SetAllExpanded(true, _items);
                    Repaint();
                    GUIUtility.ExitGUI();
                }
                MakerCategoryTreeUi.Observe("ItemMaker/Rarity/ExpandAll", GUILayoutUtility.GetLastRect());
                if (GUILayout.Button("Collapse All", EditorStyles.toolbarButton, GUILayout.Width(82)))
                {
                    ItemMakerCatalogView.SetAllExpanded(false, _items);
                    Repaint();
                    GUIUtility.ExitGUI();
                }
                MakerCategoryTreeUi.Observe("ItemMaker/Rarity/CollapseAll", GUILayoutUtility.GetLastRect());
            }
            EditorGUILayout.Space(3f);
            using (var scroll = new EditorGUILayout.ScrollViewScope(_catalogScroll))
            {
                _catalogScroll = scroll.scrollPosition;
                List<ItemSO> filtered = GetFilteredItems();
                // Changing the category/search prunes selection. Folding a rarity keeps the workbench intact.
                if (_selection.Retain(filtered)) ApplyCatalogSelection(true);
                DrawRarityGroups(filtered);
            }
        }
    }

    private void DrawRarityGroups(List<ItemSO> items)
    {
        if (items.Count == 0)
        {
            GUILayout.Space(24f);
            EditorGUILayout.LabelField("No ItemSO matches this filter.", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        foreach (var group in items.GroupBy(item => item.Rarity))
        {
            List<ItemSO> groupItems = group.ToList();
            Rect header = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.foldoutHeader, GUILayout.Height(27), GUILayout.ExpandWidth(true));
            Color color = ItemMakerCatalogView.RarityColor(group.Key);
            EditorGUI.DrawRect(header, new Color(color.r, color.g, color.b, .18f));
            EditorGUI.DrawRect(new Rect(header.x, header.y, 3, header.height), color);
            Rect label = new Rect(header.x + 7, header.y + 3, Mathf.Max(0, header.width - 10), 21);
            bool expanded = ItemMakerCatalogView.IsExpanded(group.Key);
            bool next = EditorGUI.Foldout(label, expanded,
                new GUIContent($"{group.Key}  ({groupItems.Count})", "Click to expand/collapse this rarity. Items are sorted by card name (A-Z)."),
                true, EditorStyles.foldoutHeader);
            MakerCategoryTreeUi.Observe("ItemMaker/Rarity/" + group.Key + "/toggle", label);
            if (next != expanded)
            {
                ItemMakerCatalogView.SetExpanded(group.Key, next);
                Repaint();
                GUIUtility.ExitGUI();
            }
            if (expanded) DrawItemCardGrid(groupItems);
            GUILayout.Space(6);
        }
    }

    // Purpose: Builds a responsive card grid within a single rarity group.
    private void DrawItemCardGrid(List<ItemSO> items)
    {
        const float cardWidth = 154f;
        int columnCount = Mathf.Max(1, Mathf.FloorToInt(Mathf.Max(160f, position.width - 705f) / cardWidth));
        for (int rowStart = 0; rowStart < items.Count; rowStart += columnCount)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                int rowEnd = Mathf.Min(items.Count, rowStart + columnCount);
                for (int index = rowStart; index < rowEnd; index++)
                    DrawItemCard(items[index], cardWidth);
                GUILayout.FlexibleSpace();
            }
        }
    }

    // Purpose: Draws one icon-first asset card that selects the matching ItemSO without editing it.
    private void DrawItemCard(ItemSO item, float cardWidth)
    {
        if (item == null)
            return;

        var registration = MakerRegistrationIndex.Current.For(item);
        string registrationText = (registration.NeedsCheck ? "확인 필요\n" : "") + registration.LootLabel;
        float badgeHeight = Mathf.Max(51, MakerRegistrationUi.BadgeHeight(registrationText, cardWidth - 14));
        float cardHeight = 143 + badgeHeight + 26;
        Rect cardRect = GUILayoutUtility.GetRect(cardWidth, cardHeight, GUILayout.Width(cardWidth), GUILayout.Height(cardHeight));
        MakerCategoryTreeUi.Observe("ItemMaker/Card/" + item.GetInstanceID(), cardRect);
        bool selected = _selection.Contains(item);
        Color fill = selected
            ? new Color(0.10f, 0.39f, 0.52f, 0.95f)
            : new Color(0.12f, 0.15f, 0.18f, 0.92f);
        EditorGUI.DrawRect(cardRect, fill);
        GUI.Box(cardRect, GUIContent.none, EditorStyles.helpBox);

        Rect imageRect = new(cardRect.x + 7f, cardRect.y + 7f, cardRect.width - 14f, 76f);
        DrawCatalogItemIcon(imageRect, item);
        Rect nameRect = new(cardRect.x + 7f, cardRect.y + 87f, cardRect.width - 14f, 34f);
        GUI.Label(nameRect, item.name, EditorStyles.wordWrappedMiniLabel);
        Rect typeRect = new(cardRect.x + 7f, cardRect.y + 123f, cardRect.width - 14f, 18f);
        GUI.Label(typeRect, $"{LootCategoryLabel(item)}  ·  {item.Rarity}", EditorStyles.centeredGreyMiniLabel);
        MakerRegistrationUi.Badge(new Rect(cardRect.x + 7, cardRect.y + 143, cardRect.width - 14, badgeHeight), item, registrationText);
        GUI.Label(new Rect(cardRect.x + 7, cardRect.y + 144 + badgeHeight, cardRect.width - 14, 18),
            $"상점 {registration.ShopCount} · 퀘스트 {registration.QuestCount}", EditorStyles.miniLabel);

        HandleItemCardInput(cardRect, item);
    }

    private void HandleItemCardInput(Rect rect, ItemSO item)
    {
        Event evt = Event.current;
        int control = GUIUtility.GetControlID(item.GetInstanceID(), FocusType.Passive, rect);
        if (evt.type == EventType.MouseDown && evt.button == 0 && rect.Contains(evt.mousePosition))
        {
            _dragCandidate = item;
            _itemDragStart = evt.mousePosition;
            _itemDragControl = control;
            GUIUtility.hotControl = control;
            GUI.FocusControl(null);
            bool toggle = evt.control || evt.command;
            List<ItemSO> visible = ItemMakerCatalogView.ExpandedItems(GetFilteredItems());
            // Keep collapsed selections in the workbench until another card is clicked,
            // but never let invisible cards enter a range selection or drag payload.
            _selection.Retain(visible);
            _collapseSelectionOnMouseUp = !toggle && !evt.shift && _selection.Contains(item) && _selection.Items.Count > 1;
            _selection.Click(item, visible, toggle, evt.shift, true);
            ApplyCatalogSelection(true);
            evt.Use();
        }
        else if (evt.type == EventType.MouseDrag && _dragCandidate == item && GUIUtility.hotControl == control &&
                 (evt.mousePosition - _itemDragStart).sqrMagnitude >= 25f)
        {
            // Ctrl-clicking a selected card removes it; do not drag a removed card.
            if (!_selection.Contains(item))
            {
                GUIUtility.hotControl = 0;
                _dragCandidate = null;
                _collapseSelectionOnMouseUp = false;
                evt.Use();
                return;
            }
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = _selection.Items.Cast<UnityEngine.Object>().ToArray();
            DragAndDrop.paths = _selection.Items.Select(AssetDatabase.GetAssetPath).ToArray();
            GUIUtility.hotControl = 0;
            DragAndDrop.StartDrag(_selection.Items.Count == 1 ? item.name : $"{_selection.Items.Count} items");
            _dragCandidate = null;
            _collapseSelectionOnMouseUp = false;
            evt.Use();
        }
        else if (evt.type == EventType.MouseUp && GUIUtility.hotControl == control)
        {
            GUIUtility.hotControl = 0;
            _dragCandidate = null;
            if (_collapseSelectionOnMouseUp && rect.Contains(evt.mousePosition)) SelectItem(item, true);
            _collapseSelectionOnMouseUp = false;
            evt.Use();
        }
    }

    private void HandleCategoryDrop(Rect rect, ItemCatalogCategory category)
    {
        Event evt = Event.current;
        if (!rect.Contains(evt.mousePosition) || (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform))
            return;
        ItemSO[] items = DragAndDrop.objectReferences.OfType<ItemSO>().Distinct().ToArray();
        bool valid = items.Length > 0 && items.Length == DragAndDrop.objectReferences.Length &&
                     items.All(item => item != null && AssetDatabase.IsMainAsset(item)) && category != ItemCatalogCategory.All &&
                     !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;
        DragAndDrop.visualMode = valid ? DragAndDropVisualMode.Move : DragAndDropVisualMode.Rejected;
        if (evt.type == EventType.DragPerform)
        {
            if (valid)
            {
                DragAndDrop.AcceptDrag();
                string[] guids = items.Select(item => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(item))).ToArray();
                // ShowAsContext needs this live OnGUI event. Only defer the asset
                // mutation, not the menu that asks which concrete type to use.
                ChooseCategoryMove(guids, category);
            }
            else ShowNotification(new GUIContent("Drop ItemSO assets on a category (not All Items), outside Play Mode."));
        }
        evt.Use();
    }

    private void ChooseCategoryMove(string[] guids, ItemCatalogCategory category)
    {
        if (this == null) return;
        ItemSO[] items = guids.Select(guid => AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();
        if (items.Any(item => item == null)) return;

        List<ItemTemplate> targets = new();
        foreach (ItemTemplate template in ItemTemplates)
            if (template.Category == category || GetParentCategory(template.Category) == category) targets.Add(template);
        if (targets.Count == 0) return;
        if (targets.Count == 1 || items.All(item => MatchesCategory(item, category)))
        {
            QueueCategoryMove(guids, targets[0], category);
            return;
        }
        GenericMenu menu = new();
        foreach (ItemTemplate template in targets)
        {
            ItemTemplate choice = template;
            menu.AddItem(new GUIContent("Convert new entries to " + choice.Label), false, () => QueueCategoryMove(guids, choice, category));
        }
        menu.ShowAsContext();
    }

    private void QueueCategoryMove(string[] guids, ItemTemplate target, ItemCatalogCategory dropCategory)
    {
        // Confirmation dialogs, imports and file moves run after the GUI callback.
        // Both direct drops and menu selections use this same deferred execution.
        EditorApplication.delayCall += () => ExecuteCategoryMove(guids, target, dropCategory);
    }

    private void ExecuteCategoryMove(string[] guids, ItemTemplate target, ItemCatalogCategory dropCategory)
    {
        if (this == null) return;
        List<ItemCategoryMoveUtility.MoveRequest> requests = new();
        List<string> changes = new();
        foreach (string guid in guids)
        {
            ItemSO item = AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (item == null)
            {
                EditorUtility.DisplayDialog("Item move stopped", "A selected asset no longer exists. Refresh and select again.", "OK");
                return;
            }
            // Dropping on a parent is not a conversion of its existing children.
            if (IsUmbrellaCategory(dropCategory) && MatchesCategory(item, dropCategory))
                continue;
            Type destinationType = GetCategory(item) == target.Category ? item.GetType() : target.Type;
            RangedWeaponType? weaponType = GetCategory(item) == target.Category ? null : target.WeaponType;
            bool changesType = item.GetType() != destinationType;
            bool changesWeaponType = weaponType.HasValue &&
                (item is not RangedWeaponSO firearm || firearm.WeaponType != weaponType.Value);
            if (!changesType && !changesWeaponType && Path.GetDirectoryName(AssetDatabase.GetAssetPath(item))?.Replace('\\', '/') == target.Folder)
                continue;
            requests.Add(new ItemCategoryMoveUtility.MoveRequest { item = item, destinationType = destinationType,
                destinationFolder = target.Folder, destinationWeaponType = weaponType });
            if (changesType)
            {
                string[] retired = ItemCategoryMoveUtility.GetRetiredFields(item, destinationType);
                changes.Add($"{item.name}: {item.GetType().Name} → {destinationType.Name}" +
                    (retired.Length > 0 ? "\n  백업에 보존할 이전 전용 설정: " + string.Join(", ", retired) : ""));
            }
            else if (changesWeaponType)
                changes.Add($"{item.name}: {((RangedWeaponSO)item).WeaponType} → {weaponType.Value} (호환 탄종도 변경)");
        }
        if (requests.Count == 0)
        {
            _category = dropCategory;
            RevealSelectedCategory();
            ShowNotification(new GUIContent("Already in this category. Existing subtypes are preserved."));
            Repaint();
            return;
        }
        if (changes.Count > 0 || requests.Count > 1)
        {
            string details = changes.Count > 0 ? string.Join("\n", changes.Take(8)) : string.Join(", ", requests.Take(12).Select(request => request.item.name));
            if (changes.Count > 8) details += $"\n… 외 {changes.Count - 8}개 종류 변경";
            if (!EditorUtility.DisplayDialog("Move Item Categories",
                    $"{requests.Count}개 이동 / {changes.Count}개 종류 변경 / {guids.Length - requests.Count}개 유지\n→ {GetCategoryLabel(target.Category)}\n\n" + details + "\n\n" +
                    "이름·아이콘·월드 모델·공통 데이터·GUID는 유지합니다. 새 타입의 효과 설정은 확인해 주세요. " +
                    "하나라도 이름 중복이나 비호환 참조가 있으면 전체 이동을 중단합니다." +
                    "\n\n원본은 UserSettings/ItemMakerMoveBackups에 백업됩니다. Restore Last Move로 이번 묶음을 복구할 수 있습니다.",
                    "선택 항목 이동", "취소")) return;
        }
        try
        {
            ItemCategoryMoveUtility.MoveBatch(requests, out _);
            _category = dropCategory;
            RevealSelectedCategory();
            _search = string.Empty;
            _catalogScroll = Vector2.zero;
            RefreshCatalogs();
            SetSelectionFromGuids(guids);
            _workbenchTab = WorkbenchTab.Details;
            ShowNotification(new GUIContent($"Moved {requests.Count} items. {guids.Length - requests.Count} unchanged."));
        }
        catch (Exception exception)
        {
            RefreshCatalogs();
            SetSelectionFromGuids(guids);
            EditorUtility.DisplayDialog("Item move stopped", exception.Message, "OK");
        }
        Repaint();
    }

    private void RestoreLastCategoryMove()
    {
        if (this == null) return;
        try
        {
            ItemSO[] items = ItemCategoryMoveUtility.RestoreLastMoves();
            RefreshCatalogs();
            _category = items.Select(GetCategory).Distinct().Count() == 1 ? GetCategory(items[0]) : ItemCatalogCategory.All;
            RevealSelectedCategory();
            _search = string.Empty;
            _selection.Set(items);
            ApplyCatalogSelection(true);
            ShowNotification(new GUIContent($"Restored {items.Length} items: original types, data and folders."));
        }
        catch (Exception exception) { EditorUtility.DisplayDialog("Restore stopped", exception.Message, "OK"); }
        Repaint();
    }

    // Purpose: Draws all operations for the currently selected item without collapsing them into the main card browser.
    private void DrawWorkbenchPane(float height)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(430f), GUILayout.Height(height)))
        {
            EditorGUILayout.LabelField("ITEM WORKBENCH", EditorStyles.boldLabel);
            if (_selection.Items.Count > 1)
                EditorGUILayout.LabelField($"{_selection.Items.Count} selected — edits below affect only {_selectedItem?.name}.", EditorStyles.wordWrappedMiniLabel);
            if (_selectedItem == null)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Select an ItemSO card to inspect, capture, or register it in a loot pool.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                return;
            }

            if (_selectedItem is PassiveItemSO && _workbenchTab == WorkbenchTab.LootLinks)
                _workbenchTab = WorkbenchTab.Module;
            else if (_selectedItem is not PassiveItemSO && _workbenchTab == WorkbenchTab.Module)
                _workbenchTab = WorkbenchTab.Details;
            DrawSelectedItemHeader();
            MakerRegistrationUi.ItemSummary(_selectedItem);
            DrawWorkbenchTabs();
            _workbenchScroll = EditorGUILayout.BeginScrollView(_workbenchScroll);
            switch (_workbenchTab)
            {
                case WorkbenchTab.Details:
                    DrawItemDetails();
                    break;
                case WorkbenchTab.PreviewImage:
                    DrawPreviewImageAuthoring();
                    break;
                case WorkbenchTab.LootLinks:
                    DrawLootLinks();
                    break;
                case WorkbenchTab.Module:
                    if (_selectedItem is PassiveItemSO module) DrawModuleWorkbench(module);
                    else DrawItemDetails();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }
    }

    // Purpose: Shows a compact selected-item identity card at the top of every workbench mode.
    private void DrawSelectedItemHeader()
    {
        using (new EditorGUILayout.HorizontalScope(GUI.skin.box, GUILayout.Height(74f)))
        {
            DrawCatalogItemIcon(GUILayoutUtility.GetRect(64f, 64f, GUILayout.Width(64f), GUILayout.Height(64f)), _selectedItem);
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField(_selectedItem.name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"{LootCategoryLabel(_selectedItem)}  |  {GetFriendlyTypeName(_selectedItem.GetType())}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Rarity: {_selectedItem.Rarity}  |  Stack: {_selectedItem.MaxStack}", EditorStyles.miniLabel);
            }
        }
    }

    // Purpose: Switches among direct data editing, isolated icon generation, and fixed-pool registration.
    private void DrawWorkbenchTabs()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            DrawWorkbenchTabButton(WorkbenchTab.Details, "Item Data");
            DrawWorkbenchTabButton(WorkbenchTab.PreviewImage, "Preview Image");
            if (_selectedItem is PassiveItemSO)
                DrawWorkbenchTabButton(WorkbenchTab.Module, "모듈 제작 · 룬");
            else
                DrawWorkbenchTabButton(WorkbenchTab.LootLinks, "Loot Links");
        }
    }

    // Purpose: Draws one tinted workbench tab button while preserving the selected mode.
    private void DrawWorkbenchTabButton(WorkbenchTab tab, string label)
    {
        Color originalColor = GUI.backgroundColor;
        if (_workbenchTab == tab)
            GUI.backgroundColor = new Color(0.18f, 0.56f, 0.68f, 0.78f);
        if (GUILayout.Button(label, EditorStyles.toolbarButton))
        {
            _workbenchTab = tab;
            _workbenchScroll = Vector2.zero;
        }

        GUI.backgroundColor = originalColor;
    }

    // Purpose: Renders every serialized field of the selected concrete ItemSO subtype without a custom data duplicate.
    private void DrawItemDetails()
    {
        if (GUILayout.Button("공용 시스템 설정 · 랜덤 패시브 / 방탄 / 스태미나")) ProgressionFoundationWindow.OpenFor(_selectedItem);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Select Asset"))
                Selection.activeObject = _selectedItem;
            if (GUILayout.Button("Duplicate"))
                DuplicateSelectedItem();
            if (GUILayout.Button("Delete", GUILayout.Width(62f)))
                DeleteSelectedItem();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            _assetNameDraft = ItemNameUi.NameFor(_selectedItem, _assetNameDraft);
            _assetNameDraft = EditorGUILayout.TextField("Asset File Name", _assetNameDraft);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_assetNameDraft) || _assetNameDraft == _selectedItem.name))
            {
                if (GUILayout.Button("Rename", GUILayout.Width(64f)))
                    RenameSelectedItemAsset();
            }
        }

        EditorGUILayout.LabelField(
            "Rename keeps the ItemSO GUID, so existing shop, loot, quest, and prefab references stay connected.",
            EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.LabelField("Asset Path", AssetDatabase.GetAssetPath(_selectedItem), EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(5f);
        EditorGUILayout.LabelField("AUTHORED DATA", EditorStyles.boldLabel);
        MakerRegistrationUi.PrefabSummary(_selectedItem.WorldVisualPrefab);

        if (_selectedItem is UsableItemSO usable)
        {
            if (usable is ThrowableUsableItemSO)
                EditorGUILayout.HelpBox("Throwables keep their dedicated Hand View Override with ThrowableProjectile. It is also the released projectile.", MessageType.Info);
            else if (usable.UsesWorldVisualInHand)
                EditorGUILayout.HelpBox("Hand socket: uses World Visual Prefab by default, preserving its saved transform. Position / Rotation / Scale below are extra mount corrections. Set an override only for a different hand model.", MessageType.Info);
            else
                EditorGUILayout.HelpBox("Using a dedicated hand override. Its existing absolute position / rotation / scale settings are preserved. Clear the override to use the saved World Visual Prefab instead.", MessageType.Info);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Resolved Hand Visual", usable.HandVisualPrefab, typeof(GameObject), false);
            if (usable.HandVisualPrefab == null)
                EditorGUILayout.HelpBox("No hand visual is assigned. Set World Visual Prefab (or the dedicated throwable override).", MessageType.Warning);
        }

        if (_selectedItem is PassiveItemSO passiveItem)
        {
            EditorGUILayout.HelpBox("효과가 있는 장착품은 모듈입니다. 모듈 제작 · 룬 탭에서 외형과 효과를 편집합니다. 기존 귀중품은 장착 효과가 없습니다.", MessageType.Info);
            if (GUILayout.Button("공용 분해 파츠 · 리롤 비용 · 적 패시브 풀 설정"))
                Selection.activeObject = PassiveModuleRulesSO.Active;
        }
        SerializedObject serialized = new(_selectedItem);
        serialized.Update();
        EditorGUI.BeginChangeCheck();
        SerializedProperty property = serialized.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (property.name == "m_Script")
                continue;
            if (property.name == "_weightKg")
            {
                if (_selectedItem is not PassiveItemSO)
                {
                    EditorGUILayout.PropertyField(property, new GUIContent("개당 무게 (kg)", property.tooltip));
                    if (property.floatValue < 0f)
                        EditorGUILayout.LabelField($"종류 기본값: {_selectedItem.WeightKg:0.###} kg", EditorStyles.miniLabel);
                }
                continue;
            }
            if (property.name == "_maxStack" && _selectedItem is not BulletSO)
            {
                EditorGUILayout.LabelField("최대 스택", _selectedItem.MaxStack.ToString("N0"));
                continue;
            }
            if (property.name == "_valuableCategoryId")
            {
                ValuableCategoryEditorWindow.DrawAssignment(property);
                continue;
            }
            if (property.name == "_heldViewPrefab")
                EditorGUILayout.PropertyField(property, new GUIContent("Hand View Override", property.tooltip), true);
            else if (property.name == "_heldViewLocalPosition")
                EditorGUILayout.PropertyField(property, new GUIContent("Hand Position Offset", property.tooltip), true);
            else if (property.name == "_heldViewLocalEulerAngles")
                EditorGUILayout.PropertyField(property, new GUIContent("Hand Rotation Offset", property.tooltip), true);
            else if (property.name == "_heldViewLocalScale")
                EditorGUILayout.PropertyField(property, new GUIContent("Hand Scale Multiplier", property.tooltip), true);
            else if (property.name == "_category" && _selectedItem is PassiveItemSO)
                property.enumValueIndex = EditorGUILayout.Popup("패시브 분류", property.enumValueIndex, new[] { "전투", "생존" });
            else if (property.name == "_medicineKind")
                EditorGUILayout.PropertyField(property, new GUIContent("감염 약품 종류 · 앰플 / 최대 HP 회복 주사기"));
            else if (property.name == "_infectionRecoveryAmount" && _selectedItem is InfectionCureItemSO medicine)
                EditorGUILayout.PropertyField(property, new GUIContent(medicine.IsSyringe ? "복구할 최대 HP 잠식량" : "지속시간 동안 게이지 추가 회복 총량"));
            else if (property.name == "_infectionRecoveryDurationSeconds" && _selectedItem is InfectionCureItemSO durationMedicine)
            {
                if (!durationMedicine.IsSyringe) EditorGUILayout.PropertyField(property, new GUIContent("앰플 지속시간 (초)"));
            }
            else if (property.name == "_exposureGainMultiplier" && _selectedItem is InfectionCureItemSO gainMedicine)
            {
                if (!gainMedicine.IsSyringe) EditorGUILayout.PropertyField(property, new GUIContent("앰플 감염 유입 배율 · 0.5 = 절반"));
            }
            else if (property.name == "_usageLevelOverride")
                EditorGUILayout.PropertyField(property, new GUIContent("사용 레벨 개별 지정 · 0=공통 규칙", property.tooltip));
            else if (property.name == "_effectGroup")
                EditorGUILayout.PropertyField(property, new GUIContent("중복 장착 금지 효과 그룹 ID", property.tooltip));
            else if (property.name == "_randomStatsEnabled")
                EditorGUILayout.PropertyField(property, new GUIContent("개체별 랜덤 스탯 사용"));
            else if (property.name == "_rarityRolls")
                EditorGUILayout.PropertyField(property, new GUIContent("레어리티별 스탯 범위 · 합연산"), true);
            else EditorGUILayout.PropertyField(property, true);
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_selectedItem, "Edit ItemSO");
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(_selectedItem);
            if (_previewSourceFollowsItem)
                SyncPreviewSourceFromItem();
            RefreshCatalogs();
        }
    }

    // Purpose: Provides a scene-safe icon capture workflow embedded in the selected item workbench.
    private void DrawPreviewImageAuthoring()
    {
        EditorGUILayout.HelpBox(
            "Capture happens in an isolated temporary preview scene. It never modifies the source prefab, IconMaker scene, or current gameplay scene.",
            MessageType.None);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(_selectedItem.WorldVisualPrefab == null))
            {
                if (GUILayout.Button("Use Item World Visual"))
                {
                    _previewSourceFollowsItem = true;
                    SyncPreviewSourceFromItem();
                }
            }

            if (GUILayout.Button("Select Source"))
                Selection.activeObject = _previewSourcePrefab;
        }

        EditorGUI.BeginChangeCheck();
        _previewSourcePrefab = (GameObject)EditorGUILayout.ObjectField(
            "Source Prefab",
            _previewSourcePrefab,
            typeof(GameObject),
            false);
        if (EditorGUI.EndChangeCheck())
        {
            _previewSourceFollowsItem = false;
            RebuildPreviewInstance();
        }
        MakerRegistrationUi.PrefabSummary(_previewSourcePrefab);

        EditorGUI.BeginChangeCheck();
        _previewYaw = EditorGUILayout.Slider("Yaw", _previewYaw, -180f, 180f);
        _previewPitch = EditorGUILayout.Slider("Pitch", _previewPitch, -70f, 70f);
        _previewZoom = EditorGUILayout.Slider("Zoom", _previewZoom, 0.65f, 2.4f);
        if (EditorGUI.EndChangeCheck())
            _previewDirty = true;

        _iconResolution = EditorGUILayout.IntPopup(
            "PNG Size",
            Mathf.Clamp(_iconResolution, 128, 2048),
            new[] { "256", "512", "1024" },
            new[] { 256, 512, 1024 });
        _transparentPreviewBackground = EditorGUILayout.Toggle("Transparent", _transparentPreviewBackground);
        using (new EditorGUI.DisabledScope(_transparentPreviewBackground))
            _previewBackgroundColor = EditorGUILayout.ColorField("Background", _previewBackgroundColor);
        _iconOutputFolder = EditorGUILayout.TextField("Output Folder", _iconOutputFolder);
        EditorGUILayout.LabelField("Generated File", GetIconFileName(), EditorStyles.miniLabel);

        Rect previewRect = GUILayoutUtility.GetAspectRect(1f, GUILayout.ExpandWidth(true));
        DrawPreviewBackdrop(previewRect);
        HandlePreviewViewportInput(previewRect);
        if (_previewSourcePrefab == null)
        {
            GUI.Label(previewRect, "Assign a World Visual Prefab to preview.", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            if (Event.current.type == EventType.Repaint)
                RenderPreview(PreviewResolution);
            if (_previewTexture != null)
                GUI.DrawTexture(previewRect, _previewTexture, ScaleMode.ScaleToFit, true);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Preview: left-drag rotate · wheel zoom", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset View", EditorStyles.miniButton, GUILayout.Width(78f)))
            {
                _previewYaw = 28f;
                _previewPitch = 14f;
                _previewZoom = 1.18f;
                _previewDirty = true;
            }
        }

        using (new EditorGUI.DisabledScope(_previewSourcePrefab == null))
        {
            if (GUILayout.Button("Generate PNG + Apply To ItemSO", GUILayout.Height(32f)))
                GenerateAndApplySprite();
        }
    }

    // Purpose: Registers or removes this item from a selected world container's one fixed LootTableSO.
    private void DrawLootLinks()
    {
        MakerRegistrationUi.UsageList(_selectedItem);
        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "World containers own fixed pools. Add this ItemSO once and it is placed automatically in the tier defined by the item's own rarity.",
            MessageType.None);

        using (new EditorGUILayout.HorizontalScope())
        {
            _selectedLootTable = (LootTableSO)EditorGUILayout.ObjectField(
                "Fixed Loot Table",
                _selectedLootTable,
                typeof(LootTableSO),
                false);
            if (GUILayout.Button("Refresh", GUILayout.Width(64f)))
                RefreshLootTables();
        }

        if (_selectedLootTable == null)
        {
            DrawLootTableSuggestions();
            return;
        }

        var owners = _lootContainers.Where(box => box != null && box.LootTable == _selectedLootTable).ToArray();
        if (owners.Length != 1 || owners[0].LootObjectType != null)
        {
            EditorGUILayout.HelpBox("Choose an independent box table. Legacy/shared or unused tables cannot be edited here. Open the box in Loot Maker and separate it first.", MessageType.Warning);
            if (GUILayout.Button("Open Loot Maker")) LootProgressionEditorWindow.OpenAndFocusWorldTable(_selectedLootTable, _selectedItem);
            return;
        }
        _selectedLootTable.EnsurePool();
        EditorGUILayout.LabelField("Selected Pool", _selectedLootTable.name, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Item Rarity", _selectedItem.Rarity.ToString(), EditorStyles.miniLabel);
        _lootMinAmount = Mathf.Max(1, EditorGUILayout.IntField("Min Amount", _lootMinAmount));
        _lootMaxAmount = Mathf.Max(_lootMinAmount, EditorGUILayout.IntField("Max Amount", _lootMaxAmount));
        bool alreadyRegistered = ContainsItem(_selectedLootTable, _selectedItem);
        EditorGUILayout.LabelField(alreadyRegistered ? "이 상자에 등록됨" : "이 상자에는 미등록", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(alreadyRegistered))
            {
                if (GUILayout.Button("Add To ItemSO Rarity", GUILayout.Height(25f)))
                    AddSelectedItemToLootTable();
            }

            using (new EditorGUI.DisabledScope(!alreadyRegistered))
            {
                if (GUILayout.Button("Remove From Pool", GUILayout.Height(25f)))
                    RemoveSelectedItemFromLootTable();
            }
        }

        if (GUILayout.Button("Open This Fixed Pool In Loot Maker"))
            LootProgressionEditorWindow.OpenAndFocusWorldTable(_selectedLootTable, _selectedItem);

        DrawLootTableUsage(_selectedLootTable);
    }

    // Purpose: Offers direct selections for world loot tables when no table has been chosen yet.
    private void DrawLootTableSuggestions()
    {
        EditorGUILayout.LabelField("CHOOSE A FIXED POOL", EditorStyles.boldLabel);
        if (_lootTables.Count == 0)
        {
            EditorGUILayout.HelpBox("No box tables found. Register a container in Loot Maker first.", MessageType.Info);
            return;
        }

        for (int index = 0; index < _lootTables.Count; index++)
        {
            LootTableSO table = _lootTables[index];
            if (table == null)
                continue;
            if (GUILayout.Button(table.name))
                _selectedLootTable = table;
        }
    }

    // Purpose: Shows the actual box owner, not retired shared-family assets.
    private void DrawLootTableUsage(LootTableSO table)
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("BOX USING THIS TABLE", EditorStyles.boldLabel);
        int shown = 0;
        for (int index = 0; index < _lootContainers.Count; index++)
        {
            LootObjectSO box = _lootContainers[index];
            if (box == null || box.LootTable != table)
                continue;
            shown++;
            if (GUILayout.Button(box.WorldPrefab != null ? box.WorldPrefab.name : box.name, EditorStyles.miniButton))
                Selection.activeObject = box;
        }

        if (shown == 0)
            EditorGUILayout.LabelField("No box uses this table.", EditorStyles.miniLabel);
    }

    // Purpose: Adds the selected item exactly once to its rarity tier in a fixed world pool.
    private void AddSelectedItemToLootTable()
    {
        if (_selectedLootTable == null || _selectedItem == null)
            return;

        _selectedLootTable.EnsurePool();
        LootRarityTier tier = _selectedLootTable.Pool.FindTier(_selectedItem.Rarity);
        if (tier == null)
        {
            ShowNotification(new GUIContent("The selected table is missing the item's rarity tier."));
            return;
        }

        tier.items ??= new List<LootItemEntry>();
        if (ContainsItem(tier.items, _selectedItem))
        {
            ShowNotification(new GUIContent("This ItemSO is already in that rarity tier."));
            return;
        }

        Undo.RecordObject(_selectedLootTable, "Add Item To Fixed Loot Pool");
        tier.items.Add(new LootItemEntry
        {
            item = _selectedItem,
            minAmount = _lootMinAmount,
            maxAmount = _lootMaxAmount
        });
        EditorUtility.SetDirty(_selectedLootTable);
        AssetDatabase.SaveAssets();
        MakerRegistrationIndex.Invalidate();
        ShowNotification(new GUIContent($"Added {_selectedItem.name} to {_selectedItem.Rarity}."));
    }

    // Purpose: Removes this item from every tier of one fixed pool after explicit author action.
    private void RemoveSelectedItemFromLootTable()
    {
        if (_selectedLootTable == null || _selectedItem == null)
            return;

        Undo.RecordObject(_selectedLootTable, "Remove Item From Fixed Loot Pool");
        int removedCount = 0;
        List<LootRarityTier> tiers = _selectedLootTable.Pool.rarityTiers;
        for (int tierIndex = 0; tierIndex < tiers.Count; tierIndex++)
        {
            LootRarityTier tier = tiers[tierIndex];
            if (tier?.items == null)
                continue;

            for (int itemIndex = tier.items.Count - 1; itemIndex >= 0; itemIndex--)
            {
                if (tier.items[itemIndex]?.item != _selectedItem)
                    continue;
                tier.items.RemoveAt(itemIndex);
                removedCount++;
            }
        }

        if (removedCount == 0)
        {
            ShowNotification(new GUIContent("The selected ItemSO was not in this pool."));
            return;
        }

        EditorUtility.SetDirty(_selectedLootTable);
        AssetDatabase.SaveAssets();
        ShowNotification(new GUIContent($"Removed {removedCount} pool entr{(removedCount == 1 ? "y" : "ies")}."));
        MakerRegistrationIndex.Invalidate();
    }

    // Purpose: Creates one configured asset file, selects it, and immediately opens its data workbench.
    private void CreateItem(ItemTemplate template)
    {
        if (!TryEnsureAssetFolder(template.Folder, out string folder))
        {
            ShowNotification(new GUIContent("Could not create the selected ItemSO folder."));
            return;
        }

        ItemSO item = CreateInstance(template.Type) as ItemSO;
        if (item == null)
        {
            Debug.LogError($"[Item Maker] Could not create ItemSO template type {template.Type.Name}.");
            return;
        }

        string fileName = SanitizeFileName(_newItemName);
        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{fileName}.asset");
        item.name = Path.GetFileNameWithoutExtension(path);
        if (template.WeaponType.HasValue)
            ItemCategoryMoveUtility.ConfigureWeaponType(item, template.WeaponType.Value);
        AssetDatabase.CreateAsset(item, path);
        if (item is PassiveItemSO) RegisterModuleForPersistence(item);
        AssetDatabase.SaveAssets();
        _category = template.Category;
        RevealSelectedCategory();
        _search = string.Empty;
        _catalogScroll = Vector2.zero;
        RefreshCatalogs();
        SelectItem(item, true);
        _workbenchTab = item is PassiveItemSO ? WorkbenchTab.Module : WorkbenchTab.Details;
        ShowNotification(new GUIContent($"Created {item.name}."));
    }

    // Purpose: Duplicates the current ItemSO into the same authored category folder with a safe unique name.
    private void DuplicateSelectedItem()
    {
        if (_selectedItem == null)
            return;

        string sourcePath = AssetDatabase.GetAssetPath(_selectedItem);
        if (string.IsNullOrWhiteSpace(sourcePath))
            return;

        string copyPath = AssetDatabase.GenerateUniqueAssetPath(
            Path.Combine(Path.GetDirectoryName(sourcePath) ?? ItemRootFolder, _selectedItem.name + "_Copy.asset").Replace('\\', '/'));
        if (!AssetDatabase.CopyAsset(sourcePath, copyPath))
        {
            ShowNotification(new GUIContent("Could not duplicate this ItemSO."));
            return;
        }

        ItemSO copy = AssetDatabase.LoadAssetAtPath<ItemSO>(copyPath);
        if (copy is PassiveItemSO) RegisterModuleForPersistence(copy);
        AssetDatabase.SaveAssets();
        RefreshCatalogs();
        SelectItem(copy, true);
        ShowNotification(new GUIContent($"Duplicated {copy.name}."));
    }

    // Purpose: Renames the underlying ItemSO asset while preserving its GUID and every existing serialized reference.
    private void RenameSelectedItemAsset()
    {
        if (_selectedItem == null)
            return;

        string sourcePath = AssetDatabase.GetAssetPath(_selectedItem);
        if (string.IsNullOrWhiteSpace(sourcePath))
            return;

        string requestedName = Path.GetFileNameWithoutExtension(_assetNameDraft?.Trim() ?? string.Empty);
        requestedName = SanitizeFileName(requestedName);
        if (requestedName == _selectedItem.name)
        {
            _assetNameDraft = _selectedItem.name;
            return;
        }

        string error = AssetDatabase.RenameAsset(sourcePath, requestedName);
        if (!string.IsNullOrWhiteSpace(error))
        {
            EditorUtility.DisplayDialog("Rename ItemSO", error, "OK");
            return;
        }

        AssetDatabase.SaveAssets();
        _assetNameDraft = _selectedItem.name;
        RefreshCatalogs();
        Selection.activeObject = _selectedItem;
        ShowNotification(new GUIContent($"Renamed ItemSO to {_selectedItem.name}."));
    }

    // Purpose: Deletes one authored ItemSO only after a clear path-specific confirmation.
    private void DeleteSelectedItem()
    {
        if (_selectedItem == null)
            return;

        string path = AssetDatabase.GetAssetPath(_selectedItem);
        bool confirmed = EditorUtility.DisplayDialog(
            "Delete ItemSO",
            $"Delete this ItemSO asset?\n\n{path}\n\nExisting shop, loot, loadout, and quest references will become missing and must be repaired.",
            "Delete Asset",
            "Cancel");
        if (!confirmed)
            return;

        ItemSO deletedItem = _selectedItem;
        _selectedItem = null;
        _previewSourcePrefab = null;
        RebuildPreviewInstance();
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.SaveAssets();
        RefreshCatalogs();
        if (Selection.activeObject == deletedItem)
            Selection.activeObject = null;
        ShowNotification(new GUIContent("Deleted ItemSO asset."));
    }

    // Purpose: Reloads the ItemSO, fixed-pool, and reusable-container catalogues from their authored asset folders.
    private void RefreshCatalogs()
    {
        MakerRegistrationIndex.Invalidate();
        RefreshItems();
        RefreshLootTables();
    }

    // Purpose: Finds every ItemSO derived asset under the canonical GameItem root, including concrete subtype assets.
    private void RefreshItems()
    {
        _items.Clear();
        string[] roots = AssetDatabase.IsValidFolder(ItemRootFolder)
            ? (AssetDatabase.IsValidFolder("Assets/Resources/PassiveModules") ? new[] { ItemRootFolder, "Assets/Resources/PassiveModules" } : new[] { ItemRootFolder })
            : new[] { "Assets" };
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", roots);
        for (int index = 0; index < guids.Length; index++)
        {
            ItemSO item = AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(guids[index]));
            if (item != null)
                _items.Add(item);
        }

        _items.Sort(ItemMakerCatalogView.Compare);
        if (_selection.Retain(_items)) ApplyCatalogSelection(false);
    }

    // Purpose: Finds all fixed world LootTableSO assets for quick item registration.
    private void RefreshLootTables()
    {
        RefreshLootContainers();
        _lootTables.Clear();
        _lootTables.AddRange(_lootContainers.Where(box => box != null && box.LootTable != null)
            .Select(box => box.LootTable).Distinct());

        _lootTables.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
        if (_selectedLootTable != null && !_lootTables.Contains(_selectedLootTable))
            _selectedLootTable = null;
    }

    // Purpose: Finds box records to show the actual owner of each independent table.
    private void RefreshLootContainers()
    {
        _lootContainers.Clear();
        string[] guids = AssetDatabase.FindAssets("t:LootObjectSO");
        for (int index = 0; index < guids.Length; index++)
        {
            LootObjectSO box = AssetDatabase.LoadAssetAtPath<LootObjectSO>(AssetDatabase.GUIDToAssetPath(guids[index]));
            if (box != null)
                _lootContainers.Add(box);
        }

        _lootContainers.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
    }

    // Purpose: Uses the active Project selection only when it is an ItemSO.
    private void TryUseSelectedItem()
    {
        if (Selection.activeObject is ItemSO item)
            SelectItem(item, false);
    }

    // Purpose: Centralizes selection so the icon preview source always follows a newly selected ItemSO by default.
    private void SelectItem(ItemSO item, bool selectProjectAsset)
    {
        if (item == null)
            return;

        _selection.Set(new[] { item });
        ItemMakerCatalogView.SetExpanded(item.Rarity, true);
        ApplyCatalogSelection(selectProjectAsset);
    }

    private void SetSelectionFromGuids(IEnumerable<string> guids)
    {
        _selection.Set(guids.Select(guid => AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(guid))));
        foreach (ItemSO item in _selection.Items) ItemMakerCatalogView.SetExpanded(item.Rarity, true);
        ApplyCatalogSelection(true);
    }

    private void ApplyCatalogSelection(bool selectProjectAssets)
    {
        ItemNameUi.SelectionClicked();
        _assetNameDraft = _selection.Active != null ? _selection.Active.name : string.Empty;
        if (_selectedItem != _selection.Active)
        {
            _selectedItem = _selection.Active;
            _assetNameDraft = _selectedItem != null ? _selectedItem.name : string.Empty;
            _previewSourceFollowsItem = true;
            SyncPreviewSourceFromItem();
            _workbenchScroll = Vector2.zero;
        }
        if (selectProjectAssets) SyncProjectSelection();
        Repaint();
    }

    private void SyncProjectSelection()
    {
        Selection.objects = _selection.Items.OrderByDescending(item => item == _selection.Active)
            .Cast<UnityEngine.Object>().ToArray();
    }

    // Purpose: Restores the selected ItemSO's World Visual Prefab as the image capture source.
    private void SyncPreviewSourceFromItem()
    {
        _previewSourcePrefab = _selectedItem != null ? _selectedItem.WorldVisualPrefab : null;
        RebuildPreviewInstance();
    }

    // Purpose: Resolves matching items, even in collapsed groups; edits to rarity/name reorder immediately.
    private List<ItemSO> GetFilteredItems()
    {
        List<ItemSO> result = new();
        for (int index = 0; index < _items.Count; index++)
        {
            ItemSO item = _items[index];
            if (item == null || !MatchesCategory(item, _category) ||
                (_category == ItemCatalogCategory.Valuable && _valuableCategoryFilter != null &&
                 (item is not ValuableItemSO valuable || (_valuableCategoryFilter.Length == 0
                    ? ValuableCategoryCatalogSO.Active?.Find(valuable.ValuableCategoryId) != null
                    : valuable.ValuableCategoryId != _valuableCategoryFilter))))
                continue;
            if (!MatchesSearch(item, _search))
                continue;
            if (!MakerRegistrationIndex.Current.For(item).Matches(_registrationFilter))
                continue;
            result.Add(item);
        }

        result.Sort(ItemMakerCatalogView.Compare);
        return result;
    }

    // Purpose: Counts one category from the cached ItemSO library without changing the active selection.
    private int CountItems(ItemCatalogCategory category)
    {
        int count = 0;
        for (int index = 0; index < _items.Count; index++)
        {
            if (_items[index] != null && MatchesCategory(_items[index], category))
                count++;
        }

        return count;
    }

    // Shared read-only classification for Loot Maker; keeps category order/labels in one place.
    internal static int LootCategoryOrder(ItemSO item) => Array.IndexOf(CategoryOrder, GetCategory(item));
    internal static string LootCategoryLabel(ItemSO item) => item is ValuableItemSO valuable
        ? "귀중품 / " + valuable.ValuableCategoryLabel : GetCategoryLabel(GetCategory(item));
    internal static string LootCategoryKey(ItemSO item) => LootItemCategories.GetKey(item);

    // Purpose: Maps concrete ItemSO roles to author-friendly Item Maker categories.
    private static ItemCatalogCategory GetCategory(ItemSO item) =>
        item is ValuableItemSO ? ItemCatalogCategory.Valuable :
        Enum.TryParse(LootItemCategories.GetKey(item), out ItemCatalogCategory category) ? category : ItemCatalogCategory.Other;

    private static bool IsUmbrellaCategory(ItemCatalogCategory category)
        => category is ItemCatalogCategory.Weapon or ItemCatalogCategory.Bullet or ItemCatalogCategory.Consumable;

    private static ItemCatalogCategory? GetParentCategory(ItemCatalogCategory category)
    {
        if (category >= ItemCatalogCategory.WeaponSmg && category <= ItemCatalogCategory.WeaponSpecial)
            return ItemCatalogCategory.Weapon;
        if (category >= ItemCatalogCategory.BulletLight && category <= ItemCatalogCategory.BulletSpecial)
            return ItemCatalogCategory.Bullet;
        if (category is ItemCatalogCategory.Medical or ItemCatalogCategory.Throwable)
            return ItemCatalogCategory.Consumable;
        return null;
    }

    // Parent categories include their children; WIP melee stays outside Weapons.
    private static bool MatchesCategory(ItemSO item, ItemCatalogCategory category)
    {
        if (item == null)
            return false;

        if (category == ItemCatalogCategory.All)
            return true;

        if (category == ItemCatalogCategory.Weapon)
            return item is WeaponSO && item is not MeleeWeaponSO;

        if (category == ItemCatalogCategory.Bullet)
            return item is BulletSO;

        if (category == ItemCatalogCategory.Consumable)
            return item is ConsumableItemSO || item is ThrowableUsableItemSO;

        if (category == ItemCatalogCategory.Medical)
            return item is MedicalItemSO;

        return GetCategory(item) == category;
    }

    // Purpose: Returns a readable category name consistently across card, filter, and inspector UI.
    private static string GetCategoryLabel(ItemCatalogCategory category)
    {
        return category switch
        {
            ItemCatalogCategory.All => "All Items",
            ItemCatalogCategory.Weapon => "Weapons",
            ItemCatalogCategory.MeleeWip => "WIP / Melee Weapons",
            ItemCatalogCategory.Bullet => "Bullets",
            ItemCatalogCategory.WeaponSmg => "SMG",
            ItemCatalogCategory.WeaponAr => "AR",
            ItemCatalogCategory.WeaponLmg => "LMG",
            ItemCatalogCategory.WeaponPistol => "Pistol / Revolver",
            ItemCatalogCategory.WeaponShotgun => "Shotgun",
            ItemCatalogCategory.WeaponSniper => "Sniper Rifle",
            ItemCatalogCategory.WeaponSpecial => "Special (Rocket etc.)",
            ItemCatalogCategory.BulletLight => "Light (Pistol / SMG)",
            ItemCatalogCategory.BulletMedium => "Medium (AR)",
            ItemCatalogCategory.BulletHeavy => "Heavy (LMG / Revolver)",
            ItemCatalogCategory.BulletShotgun => "Shotgun Shells",
            ItemCatalogCategory.BulletSniper => "Sniper Rounds",
            ItemCatalogCategory.BulletSpecial => "Special / Rockets",
            ItemCatalogCategory.Throwable => "Throwables",
            ItemCatalogCategory.Consumable => "Consumables",
            ItemCatalogCategory.Medical => "Medical Items",
            ItemCatalogCategory.Armor => "Armor",
            ItemCatalogCategory.HeadEquipment => "Head / Gas Mask",
            ItemCatalogCategory.Backpack => "Backpacks",
            ItemCatalogCategory.Key => "Keys / Key Tools",
            ItemCatalogCategory.Passive => "모듈",
            ItemCatalogCategory.Currency => "Currency",
            ItemCatalogCategory.Valuable => "Valuables",
            _ => "Other"
        };
    }

    // Indent all children while keeping card/workbench labels concise.
    private static string GetCategoryFilterLabel(ItemCatalogCategory category)
    {
        return (GetParentCategory(category).HasValue ? "  └ " : "") + GetCategoryLabel(category);
    }

    // Purpose: Keeps the list search lightweight by matching asset name and canonical project path.
    private static bool MatchesSearch(ItemSO item, string search)
    {
        if (item == null || string.IsNullOrWhiteSpace(search))
            return item != null;

        string term = search.Trim();
        return item.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
               AssetDatabase.GetAssetPath(item).IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // Purpose: Uses a generated image first, then Unity's asynchronous preview fallback for visual card recognition.
    private static Texture GetItemPreview(ItemSO item)
    {
        if (item?.PreviewImage != null)
            return item.PreviewImage.texture;
        if (item == null)
            return Texture2D.grayTexture;

        GameObject worldVisualPrefab = item.WorldVisualPrefab;
        if (worldVisualPrefab != null)
        {
            return AssetPreview.GetAssetPreview(worldVisualPrefab)
                   ?? AssetPreview.GetMiniThumbnail(worldVisualPrefab)
                   ?? Texture2D.grayTexture;
        }

        return AssetPreview.GetMiniThumbnail(item) ?? Texture2D.grayTexture;
    }

    // Purpose: Formats concrete C# type names for authors without hiding which subtype owns the data.
    private static string GetFriendlyTypeName(Type type)
    {
        if (type == typeof(PassiveItemSO)) return "모듈";
        string name = type != null ? type.Name : "ItemSO";
        return name.EndsWith("SO", StringComparison.Ordinal)
            ? name.Substring(0, name.Length - 2)
            : name;
    }

    // Purpose: Supplies a stable popup source for every supported item creation template.
    private static string[] GetTemplateLabels()
    {
        string[] labels = new string[ItemTemplates.Length];
        for (int index = 0; index < ItemTemplates.Length; index++)
            labels[index] = ItemTemplates[index].Label;
        return labels;
    }

    // Purpose: Checks whether one item already exists anywhere in a fixed table's standardized rarity rows.
    private static bool ContainsItem(LootTableSO table, ItemSO item)
    {
        if (table?.Pool?.rarityTiers == null || item == null)
            return false;

        for (int tierIndex = 0; tierIndex < table.Pool.rarityTiers.Count; tierIndex++)
        {
            LootRarityTier tier = table.Pool.rarityTiers[tierIndex];
            if (tier?.items != null && ContainsItem(tier.items, item))
                return true;
        }

        return false;
    }

    // Purpose: Checks one rarity row without resolving or changing its automatic item shares.
    private static bool ContainsItem(List<LootItemEntry> entries, ItemSO item)
    {
        if (entries == null || item == null)
            return false;

        for (int index = 0; index < entries.Count; index++)
        {
            if (entries[index]?.item == item)
                return true;
        }

        return false;
    }

    // Purpose: Rebuilds the editor-only prefab copy used for thumbnail capture.
    private void RebuildPreviewInstance()
    {
        EnsurePreviewStudio();
        if (_previewInstance != null)
            DestroyImmediate(_previewInstance);

        _previewInstance = null;
        if (_previewSourcePrefab == null)
        {
            _previewDirty = true;
            return;
        }

        _previewInstance = _previewUtility.InstantiatePrefabInScene(_previewSourcePrefab);
        if (_previewInstance == null)
        {
            _previewInstance = UnityEngine.Object.Instantiate(_previewSourcePrefab);
            _previewUtility.AddSingleGO(_previewInstance);
        }

        _previewInstance.name = "ItemMaker_TemporaryPreviewModel";
        // The clone is owned only by the isolated preview scene, never by a layout or authored asset.
        _previewInstance.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        _previewInstance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        _previewInstance.SetActive(true);
        PreparePreviewModel(_previewInstance);
        _previewDirty = true;
    }

    // Purpose: Recreates the temporary model after a script/domain reload without requiring the author to reassign the prefab.
    private void EnsurePreviewInstance()
    {
        if (_previewSourcePrefab == null || _previewInstance != null)
            return;

        RebuildPreviewInstance();
    }

    // Purpose: Uses Unity's preview renderer, including SRP scene culling, without adding/saving a working scene.
    private void EnsurePreviewStudio()
    {
        if (_previewUtility != null && _previewCamera != null)
            return;

        _previewUtility = new PreviewRenderUtility();
        _previewUtility.ambientColor = new Color(.2f, .2f, .2f, 1f);
        _previewCamera = _previewUtility.camera;
        _previewCamera.clearFlags = CameraClearFlags.SolidColor;
        _previewCamera.fieldOfView = 30f;
        _previewCamera.nearClipPlane = 0.01f;
        _previewCamera.farClipPlane = 1000f;
        _previewCamera.cullingMask = 1 << PreviewCaptureLayer;
        _previewCamera.useOcclusionCulling = false;
        _previewCamera.allowHDR = false;
        _previewCamera.allowMSAA = true;

        ConfigurePreviewLight(_previewUtility.lights[0], new Vector3(38f, -32f, 0f), 1.15f);
        ConfigurePreviewLight(_previewUtility.lights[1], new Vector3(42f, 142f, 0f), 0.5f);
    }

    // Purpose: Creates one temporary directional light in the isolated editor preview scene.
    private static void ConfigurePreviewLight(Light light, Vector3 rotation, float intensity)
    {
        light.transform.rotation = Quaternion.Euler(rotation);
        light.type = LightType.Directional;
        light.intensity = intensity;
        light.color = Color.white;
        light.shadows = LightShadows.Soft;
        light.cullingMask = 1 << PreviewCaptureLayer;
    }

    // Purpose: Renders isolated prefab contents with the project's SRP and restores preview lighting/targets.
    private void RenderPreview(int size)
    {
        EnsurePreviewInstance();
        if (_previewInstance == null || _previewCamera == null)
            return;
        if (_previewDirty)
            FramePreviewCamera();

        _previewCamera.backgroundColor = _transparentPreviewBackground ? Color.clear : _previewBackgroundColor;
        float points = size / EditorGUIUtility.pixelsPerPoint;
        _previewUtility.BeginPreview(new Rect(0, 0, points, points), GUIStyle.none);
        try { _previewUtility.Render(allowScriptableRenderPipeline: true, updatefov: false); }
        finally { _previewTexture = _previewUtility.EndPreview() as RenderTexture; }
    }

    // Purpose: Isolates the capture model from every authored camera and editor scene object.
    private static void PreparePreviewModel(GameObject root)
    {
        SetLayerRecursively(root.transform, PreviewCaptureLayer);
        foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            behaviour.enabled = false;
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = true;
    }

    // Purpose: Applies the capture-only layer to the model hierarchy without changing the source prefab asset.
    private static void SetLayerRecursively(Transform target, int layer)
    {
        target.gameObject.layer = layer;
        for (int index = 0; index < target.childCount; index++)
            SetLayerRecursively(target.GetChild(index), layer);
    }

    // Purpose: Gives transparent captures a readable editor-only checkerboard so dark models remain visible before export.
    private void DrawPreviewBackdrop(Rect previewRect)
    {
        if (!_transparentPreviewBackground)
        {
            EditorGUI.DrawRect(previewRect, _previewBackgroundColor);
            return;
        }

        const float cellSize = 20f;
        Color light = new(0.18f, 0.23f, 0.27f, 1f);
        Color dark = new(0.11f, 0.15f, 0.18f, 1f);
        int columns = Mathf.CeilToInt(previewRect.width / cellSize);
        int rows = Mathf.CeilToInt(previewRect.height / cellSize);
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                Rect cell = new(
                    previewRect.x + column * cellSize,
                    previewRect.y + row * cellSize,
                    Mathf.Min(cellSize, previewRect.xMax - (previewRect.x + column * cellSize)),
                    Mathf.Min(cellSize, previewRect.yMax - (previewRect.y + row * cellSize)));
                EditorGUI.DrawRect(cell, (row + column) % 2 == 0 ? light : dark);
            }
        }
    }

    // Purpose: Makes the capture area behave like a compact 3D viewport without leaking input to the surrounding EditorWindow.
    private void HandlePreviewViewportInput(Rect previewRect)
    {
        Event current = Event.current;
        if (current == null)
            return;

        int controlId = GUIUtility.GetControlID(PreviewViewportControlHint, FocusType.Passive, previewRect);
        switch (current.type)
        {
            case EventType.MouseDown when current.button == 0 && previewRect.Contains(current.mousePosition):
                GUIUtility.hotControl = controlId;
                _previewDragLastPosition = current.mousePosition;
                current.Use();
                break;

            case EventType.MouseDrag when GUIUtility.hotControl == controlId:
            {
                Vector2 delta = current.mousePosition - _previewDragLastPosition;
                _previewDragLastPosition = current.mousePosition;
                _previewYaw = Mathf.Repeat(_previewYaw + delta.x * 0.45f + 180f, 360f) - 180f;
                _previewPitch = Mathf.Clamp(_previewPitch - delta.y * 0.35f, -70f, 70f);
                _previewDirty = true;
                current.Use();
                Repaint();
                break;
            }

            case EventType.MouseUp when current.button == 0 && GUIUtility.hotControl == controlId:
                GUIUtility.hotControl = 0;
                current.Use();
                break;

            case EventType.ScrollWheel when previewRect.Contains(current.mousePosition):
                _previewZoom = Mathf.Clamp(_previewZoom + current.delta.y * 0.08f, 0.65f, 2.4f);
                _previewDirty = true;
                current.Use();
                Repaint();
                break;
        }
    }

    // Purpose: Fits the preview camera around visible renderer bounds while respecting the author's yaw, pitch, and zoom settings.
    private void FramePreviewCamera()
    {
        _previewDirty = false;
        if (_previewInstance == null || _previewCamera == null)
            return;

        Bounds bounds = CalculatePreviewBounds(_previewInstance);
        Quaternion rotation = Quaternion.Euler(_previewPitch, _previewYaw, 0f);
        float radius = Mathf.Max(0.12f, bounds.extents.magnitude);
        float distance = radius / Mathf.Sin(_previewCamera.fieldOfView * Mathf.Deg2Rad * 0.5f) * _previewZoom;
        _previewCamera.transform.rotation = rotation;
        _previewCamera.transform.position = bounds.center - _previewCamera.transform.forward * distance;
        _previewCamera.nearClipPlane = Mathf.Max(0.01f, distance - radius * 2.2f);
        _previewCamera.farClipPlane = distance + radius * 4f;
    }

    // Purpose: Calculates bounds from visible child renderers and falls back safely for helper-only prefabs.
    private static Bounds CalculatePreviewBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = new(root.transform.position, Vector3.one * 0.5f);
        for (int index = 0; index < renderers.Length; index++)
        {
            Renderer renderer = renderers[index];
            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return bounds;
    }

    // Purpose: Reads the isolated preview render into a PNG-ready CPU texture.
    private Texture2D CapturePreviewTexture(int resolution)
    {
        RenderPreview(resolution);
        if (_previewTexture == null)
            return null;

        RenderTexture previous = RenderTexture.active;
        Texture2D texture = new(_previewTexture.width, _previewTexture.height, TextureFormat.RGBA32, false);
        try
        {
            RenderTexture.active = _previewTexture;
            texture.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
            texture.Apply(false, false);
            return texture;
        }
        catch { DestroyImmediate(texture); throw; }
        finally { RenderTexture.active = previous; }
    }

    // Purpose: Generates one stable PNG file, imports it as a Sprite, and assigns it to ItemSO._previewImage.
    private void GenerateAndApplySprite()
    {
        if (_selectedItem == null || _previewSourcePrefab == null)
            return;

        if (!TryEnsureAssetFolder(_iconOutputFolder, out string normalizedFolder))
        {
            ShowNotification(new GUIContent("Output folder must be inside Assets."));
            return;
        }

        Texture2D texture = CapturePreviewTexture(_iconResolution);
        if (texture == null)
        {
            Debug.LogError("[Item Maker] Could not capture the current icon preview.");
            return;
        }

        string assetPath = normalizedFolder + "/" + GetIconFileName();
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrWhiteSpace(projectRoot))
        {
            DestroyImmediate(texture);
            Debug.LogError("[Item Maker] Could not resolve the Unity project root.");
            return;
        }

        string absolutePath = Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
        DestroyImmediate(texture);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
        {
            Debug.LogError($"[Item Maker] PNG imported but no Sprite was created. path={assetPath}");
            return;
        }

        Undo.RecordObject(_selectedItem, "Apply Item Preview Image");
        SerializedObject serialized = new(_selectedItem);
        serialized.Update();
        serialized.FindProperty("_previewImage").objectReferenceValue = sprite;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(_selectedItem);
        AssetDatabase.SaveAssets();
        RefreshCatalogs();
        ShowNotification(new GUIContent($"Generated and applied {sprite.name}."));
    }

    // Purpose: Returns one deterministic per-item icon file name so recapturing replaces the old preview instead of creating duplicates.
    private string GetIconFileName()
    {
        string sourceName = _selectedItem != null
            ? _selectedItem.name
            : _previewSourcePrefab != null ? _previewSourcePrefab.name : "Item";
        return SanitizeFileName(sourceName) + "_Preview.png";
    }

    // Purpose: Creates nested AssetDatabase folders and returns a normalized Unity asset path.
    private static bool TryEnsureAssetFolder(string requestedFolder, out string normalizedFolder)
    {
        normalizedFolder = string.IsNullOrWhiteSpace(requestedFolder)
            ? DefaultIconOutputFolder
            : requestedFolder.Replace('\\', '/').TrimEnd('/');
        if (!normalizedFolder.StartsWith("Assets", StringComparison.Ordinal))
            return false;

        string[] segments = normalizedFolder.Split('/');
        string current = segments[0];
        for (int index = 1; index < segments.Length; index++)
        {
            string next = current + "/" + segments[index];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, segments[index]);
            current = next;
        }

        normalizedFolder = current;
        return true;
    }

    // Purpose: Converts an author-entered name into a valid deterministic Unity asset file name.
    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Item";
        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return value.Trim().Replace(' ', '_');
    }

    // Purpose: Releases all temporary thumbnail-preview resources without touching authored scenes or prefabs.
    private void DisposePreviewStudio()
    {
        _previewUtility?.Cleanup();
        _previewUtility = null;
        _previewTexture = null;
        _previewCamera = null;
        _previewInstance = null;
    }
}
