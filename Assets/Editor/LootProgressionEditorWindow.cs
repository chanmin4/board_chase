/*
 * Role hierarchy: Editor > Loot
 * Primary responsibility: Authors independent per-container loot and yield, separately from enemy stage drops.
 * Execution boundary: Editor (Assembly-CSharp-Editor)
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed partial class LootProgressionEditorWindow : EditorWindow
{
    private enum AuthoringMode
    {
        WorldContainers,
        EnemyDrops
    }

    private enum WorldTypeDetailTab
    {
        Items,
        Settings
    }

    private LootProgressionConfigSO _config;
    private MakerSelectionUiState _sourceSelectionUi;
    private MakerSelectionUiState SourceSelectionUi => _sourceSelectionUi ??= new MakerSelectionUiState();
    private Vector2 _sourceScroll;
    private Vector2 _detailScroll;
    private Vector2 _worldLootObjectScroll;
    private Vector2 _worldLootTableScroll;
    private int _selectedSourceIndex = -1;
    private int _selectedStageIndex;
    [SerializeField] private int _lootStagePreview = 1;
    private LootStagePool _poolClipboard;
    private string _poolClipboardLabel = string.Empty;
    private ItemSO _quickAddItem;
    private PlayerShopItemRarity _quickAddRarity = PlayerShopItemRarity.Normal;
    private int _quickAddMinAmount = 1;
    private int _quickAddMaxAmount = 1;
    private readonly List<string> _sourceValidationIssues = new();
    private int _validatedSourceIndex = -1;
    private string _sourceSearch = string.Empty;
    private string _worldLootObjectSearch = string.Empty;
    private LootObjectSO _selectedWorldLootObject;
    private ItemSO _worldQuickAddItem;
    private int _sourceFilterIndex;
    private AuthoringMode _authoringMode = AuthoringMode.WorldContainers;
    private WorldTypeDetailTab _worldTypeDetailTab = WorldTypeDetailTab.Items;
    private bool _showSourceBinding = true;
    private bool _showCommonPool = true;
    private bool _showQuickAdd = true;
    private bool _showGlobalValuableCatalog;
    private readonly Dictionary<PlayerShopItemRarity, bool> _tierExpanded = new();
    private readonly Dictionary<string, bool> _worldTierExpanded = new();
    private LootContainerCategory? _worldCategoryFilter;
    private bool _showUnassignedVariants;
    private List<LootObjectSO> _containerCatalog = new();
    private LootTableSO _focusedWorldTable;
    private Vector2 _categoryScroll;
    private const string CategoryTreeScope = "LootMaker";
    private bool _showContainerAdvanced;
    private bool _showPoolAdvanced;
    private bool _showItemProbabilities;
    // Optional observer for IMGUI interaction regression tests; never edits assets.
    internal System.Action<string, Rect> GuiControlObserved;
    private Vector2 _guiScreenOrigin;

    private void ObserveGuiControl(string name, Rect rect)
    {
        if (GuiControlObserved == null || Event.current.type != EventType.Repaint) return;
        rect.position = GUIUtility.GUIToScreenPoint(rect.position) - _guiScreenOrigin;
        GuiControlObserved(name, rect);
    }

    // Preserve class/GUID so saved dock layouts and existing navigation helpers survive.
    private void OnEnable()
    {
        titleContent = new GUIContent("Loot Maker");
        minSize = new Vector2(1060f, 600f);
        // Reconcile any selection left by the old family-first layout after a domain reload.
        _focusedWorldTable = _selectedWorldLootObject?.LootTable;
        _worldTypeDetailTab = WorldTypeDetailTab.Items;
        RefreshWorldCatalog();
        Undo.undoRedoPerformed -= OnLootUndoRedo;
        Undo.undoRedoPerformed += OnLootUndoRedo;
    }

    private void OnProjectChange() { RefreshWorldCatalog(); Repaint(); }
    private void OnInspectorUpdate()
    {
        // Unity may finish a thumbnail between ticks. Repaint once loaded too, not only while loading.
        if (_authoringMode == AuthoringMode.WorldContainers) Repaint();
    }

    [MenuItem("Tools/Authoring/Loot Maker")]
    // Purpose: Opens the open UI or workflow.
    public static void Open()
    {
        LootProgressionEditorWindow window = GetWindow<LootProgressionEditorWindow>("Loot Maker");
        window.minSize = new Vector2(1060f, 600f);
        window.Show();
    }

    /// <summary>
    /// Opens the World Containers workspace on the reusable type that owns a
    /// fixed LootTableSO. Item Maker uses this as a navigation helper only;
    /// it never creates an alternate pool or changes runtime loot rules.
    /// </summary>
    public static void OpenAndFocusWorldTable(LootTableSO table, ItemSO quickAddItem = null)
    {
        LootProgressionEditorWindow window = GetWindow<LootProgressionEditorWindow>("Loot Maker");
        window.minSize = new Vector2(1060f, 600f);
        window._authoringMode = AuthoringMode.WorldContainers;
        window._worldCategoryFilter = null;
        window._worldLootObjectSearch = string.Empty;
        window._showUnassignedVariants = false;
        window._selectedWorldLootObject = null;
        window._focusedWorldTable = table;
        window.RefreshWorldCatalog();

        if (table != null)
        {
            List<LootObjectSO> objects = FindWorldLootObjects();
            for (int index = 0; index < objects.Count; index++)
            {
                LootObjectSO lootObject = objects[index];
                if (lootObject != null && lootObject.LootTable == table)
                {
                    window._selectedWorldLootObject = lootObject;
                    break;
                }
            }
        }

        window._worldCategoryFilter = window._selectedWorldLootObject?.Category;
        window._worldTypeDetailTab = WorldTypeDetailTab.Items;
        window._worldQuickAddItem = quickAddItem;
        window._worldLootTableScroll = Vector2.zero;
        window.Show();
        window.Repaint();
    }

    /// <summary>
    /// Opens the common-loot editor focused on one already-resolved source.
    /// Selection helpers such as Sector Maker use this to navigate to the
    /// owner data; they do not duplicate or edit any loot entries themselves.
    /// </summary>
    public static void OpenAndSelectSource(
        LootProgressionConfigSO config,
        LootSourceDefinition source,
        int stageIndex = 0)
    {
        LootProgressionEditorWindow window = GetWindow<LootProgressionEditorWindow>("Loot Maker");
        window.minSize = new Vector2(1180f, 640f);
        window._config = config;
        window._selectedSourceIndex = config != null && source != null
            ? window.IndexOfSource(source)
            : -1;
        window._selectedStageIndex = Mathf.Max(0, stageIndex);
        window._validatedSourceIndex = -1;
        window._sourceSearch = string.Empty;
        window._authoringMode = AuthoringMode.EnemyDrops;
        window.Show();
        window.Repaint();
    }

    // Purpose: Responds to the gui callback.
    private void OnGUI()
    {
        MakerCategoryTreeUi.BeginWindow(this);
        _guiScreenOrigin = GUIUtility.GUIToScreenPoint(Vector2.zero);
        HandleContainerDragCancellation();
        if (_authoringMode == AuthoringMode.WorldContainers)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Loot Maker", EditorStyles.boldLabel, GUILayout.Width(92f));
                GUILayout.Label("Choose a category  >  Click a box  >  Edit its loot", EditorStyles.miniLabel);
            }
            DrawAuthoringModeToolbar();
            DrawWorldContainerMarketplace();
            return;
        }
        AuthoringWorkspaceUi.DrawHeader(
            "Loot Maker",
            "Owner: Loot / Encounter",
            "Enemy drop tables. Container contents are edited separately in the Container Loot tab.",
            "1 Enemy Config",
            "2 Enemy Source",
            "3 Stage",
            "4 Item Pool",
            "5 Validate");

        DrawAuthoringModeToolbar();

        EditorGUI.BeginChangeCheck();
        _config = (LootProgressionConfigSO)EditorGUILayout.ObjectField(
            "Loot Progression Config",
            _config,
            typeof(LootProgressionConfigSO),
            false);
        if (EditorGUI.EndChangeCheck())
        {
            _selectedSourceIndex = -1;
            _selectedStageIndex = 0;
            _validatedSourceIndex = -1;
            _sourceSearch = string.Empty;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create Config", GUILayout.Width(130f)))
                CreateConfig();

            if (GUILayout.Button("Find Config", GUILayout.Width(100f)))
                FindFirstConfig();

            GUI.enabled = _config != null;
            if (GUILayout.Button("Select Config", GUILayout.Width(130f)))
                Selection.activeObject = _config;
            if (GUILayout.Button("Save", GUILayout.Width(70f)))
                AssetDatabase.SaveAssets();
            GUI.enabled = true;
        }

        if (_config == null)
        {
            EditorGUILayout.HelpBox(
                "A Loot Progression Config is required only for enemy drops. Switch to World Containers to author interactable container loot.",
                MessageType.Info);
            return;
        }

        EditorGUILayout.Space(6f);
        DrawConfigOverview();
        float workspaceHeight = Mathf.Max(400f, position.height - 196f);
        using (new EditorGUILayout.HorizontalScope(GUILayout.Height(workspaceHeight)))
        {
            DrawSourceList(workspaceHeight);
            DrawLootFlow(workspaceHeight);
            DrawSelectedSource(workspaceHeight);
        }
    }

    // Purpose: Draws the config overview authoring UI.
    private void DrawConfigOverview()
    {
        int sourceCount = _config.Sources.Count;
        int boundCount = 0;
        int itemCount = 0;

        for (int i = 0; i < _config.Sources.Count; i++)
        {
            LootSourceDefinition source = _config.Sources[i];
            if (source == null)
                continue;

            if (IsSourceBindingComplete(source))
                boundCount++;

            itemCount += CountPoolItems(GetCommonPoolForDisplay(source));
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Enemy Loot Overview", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"{sourceCount} Enemy Sources  |  {boundCount}/{sourceCount} Bound  |  {itemCount} Default Item Entries  |  {_config.ValuableLootItems.Count} Shared Valuables",
                EditorStyles.wordWrappedMiniLabel);

            if (boundCount < sourceCount)
            {
                EditorGUILayout.HelpBox(
                    "Some enemy sources are not bound to their EnemyShooterConfigSO yet. They remain visible in the list with a red ! marker.",
                    MessageType.Warning);
            }
        }

        DrawGlobalValuableCatalog();
    }

    // Purpose: Switches between the separate enemy-drop and world-container authoring workspaces.
    private void DrawAuthoringModeToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            Color originalColor = GUI.backgroundColor;
            if (_authoringMode == AuthoringMode.WorldContainers)
                GUI.backgroundColor = new Color(0.18f, 0.56f, 0.68f, 0.78f);
            if (GUILayout.Button("Container Loot", EditorStyles.toolbarButton, GUILayout.Width(108f)))
                _authoringMode = AuthoringMode.WorldContainers;

            GUI.backgroundColor = _authoringMode == AuthoringMode.EnemyDrops
                ? new Color(0.18f, 0.56f, 0.68f, 0.78f)
                : originalColor;
            if (GUILayout.Button("Enemy Drops", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                _authoringMode = AuthoringMode.EnemyDrops;

            GUI.backgroundColor = originalColor;

            Vector2 requiredSize = _authoringMode == AuthoringMode.WorldContainers
                ? new Vector2(1060f, 600f) : new Vector2(1180f, 640f);
            if (minSize != requiredSize) minSize = requiredSize;
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(75f))) RefreshWorldCatalog();
            if (GUILayout.Button("Open Item Maker", EditorStyles.toolbarButton, GUILayout.Width(112f)))
                EditorApplication.ExecuteMenuItem("Tools/Authoring/Item Maker");
            GUILayout.FlexibleSpace();
            GUILayout.Label("Save when finished", EditorStyles.miniLabel);
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(52f)))
            {
                foreach (var box in _containerCatalog)
                {
                    if (box == null || box.HasSlotGenerationSettings) continue;
                    Undo.RecordObject(box, "Save Box Generation Settings");
                    box.InitializeSlotGenerationSettings();
                    EditorUtility.SetDirty(box);
                }
                AssetDatabase.SaveAssets(); ShowNotification(new GUIContent("Loot settings saved."));
            }
        }
    }

    // One browsing hierarchy: category -> actual containers -> selected container workbench.
    private void RefreshWorldCatalog()
    {
        _containerCatalog = FindWorldLootObjects();
    }

    private void DrawWorldContainerMarketplace()
    {
        float height = Mathf.Max(440f, position.height - 70f);
        float leftWidth = Mathf.Clamp(position.width * .17f, 174f, 210f);
        float rightWidth = Mathf.Clamp(position.width * .38f, 410f, 550f);
        float centerWidth = Mathf.Max(280f, position.width - leftWidth - rightWidth - 28f);
        using (new EditorGUILayout.HorizontalScope(GUILayout.Height(height)))
        {
            DrawContainerCategories(leftWidth, height);
            DrawContainerCatalog(centerWidth, height);
            DrawContainerWorkbench(rightWidth, height);
        }
    }

    private void DrawContainerCategories(float width, float height)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(width), GUILayout.Height(height)))
        {
            EditorGUILayout.LabelField("1. CATEGORIES", EditorStyles.boldLabel);
            MakerCategoryTreeUi.DrawToolbar(CategoryTreeScope, ContainerTreeKeys());
            _categoryScroll = EditorGUILayout.BeginScrollView(_categoryScroll);
            DrawContainerCategoryButton($"All Containers ({_containerCatalog.Count})", null, false);
            foreach (LootContainerCategory category in System.Enum.GetValues(typeof(LootContainerCategory)))
            {
                if (category == LootContainerCategory.Uncategorized) continue;
                int count = FilterWorldContainers(_containerCatalog, category, false, null).Count;
                DrawContainerCategoryButton($"{CategoryLabel(category)} ({count})", category, false);
            }
            int other = FilterWorldContainers(_containerCatalog, LootContainerCategory.Uncategorized, false, null).Count;
            DrawContainerCategoryButton($"Other ({other})", LootContainerCategory.Uncategorized, false);
            int unassigned = FilterWorldContainers(_containerCatalog, null, true, null).Count;
            DrawContainerCategoryButton($"Unassigned ({unassigned})", null, true);
            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Arrow: expand / collapse boxes. Name: filter. Drag a box card onto a category to move it, even when collapsed. Ctrl+Z: undo.\nAll / Unassigned are filters, not drop targets.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawContainerCategoryButton(string label, LootContainerCategory? category, bool unassigned)
    {
        Rect rect = GUILayoutUtility.GetRect(new GUIContent(label), GUI.skin.button, GUILayout.Height(28));
        HandleContainerCategoryDrop(rect, category, unassigned);
        string key = unassigned ? "Unassigned" : category?.ToString() ?? "All";
        bool group = category.HasValue || unassigned;
        bool hover = _containerDropHover == category && category.HasValue && !unassigned;
        bool selected = _worldCategoryFilter == category && _showUnassignedVariants == unassigned;
        var children = group ? FilterWorldContainers(_containerCatalog, category, unassigned, null) : null;
        if (MakerCategoryTreeUi.DrawRow(rect, CategoryTreeScope, key,
            new GUIContent(label, label + "\nArrow: expand / collapse. Name: filter boxes."), selected || hover,
            group && children.Count > 0, tint: hover ? new Color(.35f, .85f, .55f) : (Color?)null))
        {
            _worldCategoryFilter = category;
            _showUnassignedVariants = unassigned;
            _worldLootObjectSearch = string.Empty;
            _worldLootObjectScroll = Vector2.zero;
            var visible = FilterWorldContainers(_containerCatalog, category, unassigned, null);
            SelectContainer(visible.Count > 0 ? visible[0] : null);
        }
        if (!group || !MakerCategoryTreeUi.IsExpanded(CategoryTreeScope, key)) return;
        foreach (var box in children)
        {
            Rect childRect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.button, GUILayout.Height(24));
            string title = GetContainerTitle(box);
            if (MakerCategoryTreeUi.DrawRow(childRect, CategoryTreeScope, "Box:" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(box)),
                new GUIContent(title, title + "\n" + AssetDatabase.GetAssetPath(box)), box == _selectedWorldLootObject, false, 18))
            {
                _worldCategoryFilter = category;
                _showUnassignedVariants = unassigned;
                _worldLootObjectSearch = string.Empty;
                _worldLootObjectScroll = Vector2.zero;
                SelectContainer(box);
            }
        }
    }

    private static IEnumerable<string> ContainerTreeKeys()
    {
        foreach (LootContainerCategory category in System.Enum.GetValues(typeof(LootContainerCategory)))
            yield return category.ToString();
        yield return "Unassigned";
    }

    private static string CategoryLabel(LootContainerCategory category) => category switch
    {
        LootContainerCategory.Ammo => "Ammo",
        LootContainerCategory.Medical => "Medical",
        LootContainerCategory.MilitaryGear => "Military Gear",
        LootContainerCategory.TreasureBox => "Treasure Box",
        LootContainerCategory.Safe => "Safe",
        LootContainerCategory.Backpack => "Backpack",
        LootContainerCategory.MetalBox => "Metal Box",
        _ => "Other"
    };

    // Pure selection helpers shared with regression tests. Filtering never reassigns a family.
    public static string GetContainerTitle(LootObjectSO container) => container == null ? string.Empty :
        container.WorldPrefab != null ? container.WorldPrefab.name : container.ResolveDisplayName();

    public static List<LootObjectSO> FilterWorldContainers(
        IEnumerable<LootObjectSO> containers, LootContainerCategory? category, bool unassignedOnly, string search)
    {
        var result = new List<LootObjectSO>();
        if (containers == null) return result;
        search = search?.Trim();
        foreach (var container in containers)
        {
            if (container == null) continue;
            if (unassignedOnly && container.HasCategoryAssignment) continue;
            if (!unassignedOnly && category.HasValue &&
                (!container.HasCategoryAssignment || container.Category != category.Value)) continue;
            if (!string.IsNullOrEmpty(search) &&
                GetContainerTitle(container).IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0 &&
                container.name.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0 &&
                container.ResolveDisplayName().IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            result.Add(container);
        }
        return result;
    }

    public static int CountSharedTableUsers(IEnumerable<LootObjectSO> containers, LootTableSO table)
    {
        if (containers == null || table == null) return 0;
        int count = 0;
        foreach (var container in containers)
            if (container != null && container.LootTable == table) count++;
        return count;
    }

    private void DrawContainerCatalog(float width, float height)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(width), GUILayout.Height(height)))
        {
            EditorGUILayout.LabelField("2. CONTAINERS", EditorStyles.boldLabel);
            DrawPrefabRegistration();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Find", GUILayout.Width(32));
                _worldLootObjectSearch = EditorGUILayout.TextField(_worldLootObjectSearch);
            }
            var visible = FilterWorldContainers(_containerCatalog, _worldCategoryFilter, _showUnassignedVariants, _worldLootObjectSearch);
            EditorGUILayout.LabelField($"{visible.Count} boxes - click to edit / drag to a category", EditorStyles.miniLabel);
            // Searches must not leave a hidden, previously selected container in the workbench.
            if (_selectedWorldLootObject != null && !visible.Contains(_selectedWorldLootObject)) SelectContainer(null);
            _worldLootObjectScroll = EditorGUILayout.BeginScrollView(_worldLootObjectScroll, false, true);
            int columns = Mathf.Max(1, Mathf.FloorToInt((width - 22f) / 146f));
            float cardWidth = Mathf.Floor((width - 22f) / columns) - 4f;
            for (int start = 0; start < visible.Count; start += columns)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int i = start; i < Mathf.Min(start + columns, visible.Count); i++)
                        DrawContainerCard(visible[i], cardWidth);
                    GUILayout.FlexibleSpace();
                }
            }
            if (visible.Count == 0)
                EditorGUILayout.HelpBox("No boxes here. Clear the search or choose another category.", MessageType.Info);
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawContainerCard(LootObjectSO container, float width)
    {
        Rect rect = GUILayoutUtility.GetRect(width, 166f, GUILayout.Width(width), GUILayout.Height(166f));
        ObserveGuiControl("Card:" + container.name, rect);
        bool selected = _selectedWorldLootObject == container;
        EditorGUI.DrawRect(rect, selected ? new Color(.16f, .39f, .48f) : new Color(.18f, .20f, .22f));
        GUI.Box(rect, GUIContent.none);
        DrawBoxPreview(new Rect(rect.x + 10, rect.y + 5, rect.width - 20, 91), container.WorldPrefab);
        var nameStyle = new GUIStyle(EditorStyles.miniBoldLabel) { wordWrap = true };
        GUI.Label(new Rect(rect.x + 7, rect.y + 98, rect.width - 14, 32), GetContainerTitle(container), nameStyle);
        string size = SizeLabel(container.SizeClass);
        GUI.Label(new Rect(rect.x + 7, rect.y + 132, rect.width - 14, 16), $"{size} | {container.LootSlotCount} slots", EditorStyles.miniLabel);
        int count = CountWorldPoolItems(container.LootTable);
        GUI.Label(new Rect(rect.x + 7, rect.y + 148, rect.width - 14, 16),
            container.LootTable == null ? "Choose a loot list" : count == 0 ? "Add loot items" : $"Fill {container.MinGeneratedSlots}-{container.MaxGeneratedSlots} | {count} items", EditorStyles.miniLabel);
        HandleContainerCardInput(rect, container);
    }

    private void SelectContainer(LootObjectSO container)
    {
        ContainerNameUi.SelectionClicked();
        // Re-clicking the same card must not silently dismiss an in-flight/completed delete review.
        if (_selectedWorldLootObject != container) ClearContainerDeleteReview();
        _selectedWorldLootObject = container;
        _focusedWorldTable = container?.LootTable;
        _worldLootTableScroll = Vector2.zero;
        _worldTypeDetailTab = WorldTypeDetailTab.Items;
        _showContainerAdvanced = false;
        _namingBox = null;
    }

    private LootTableSO CurrentWorldTable => _selectedWorldLootObject != null
        ? _selectedWorldLootObject.LootTable : _focusedWorldTable;

    private void DrawContainerWorkbench(float width, float height)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(width), GUILayout.Height(height)))
        {
            EditorGUILayout.LabelField("3. EDIT SELECTED BOX", EditorStyles.boldLabel);
            var container = _selectedWorldLootObject;
            var table = CurrentWorldTable;
            if (container == null)
            {
                GUILayout.Space(24);
                EditorGUILayout.HelpBox("Click a container card in the middle.\n\nIts loot items, capacity and drop chance appear here.", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField(GetContainerTitle(container),
                new GUIStyle(EditorStyles.boldLabel) { wordWrap = true });
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select Asset", EditorStyles.miniButton))
                    Selection.activeObject = container;
                using (new EditorGUI.DisabledScope(container == null || container.WorldPrefab == null))
                    if (GUILayout.Button("Open Prefab", EditorStyles.miniButton)) AssetDatabase.OpenAsset(container.WorldPrefab);
                using (new EditorGUI.DisabledScope(container == null || _deletePending || EditorApplication.isPlayingOrWillChangePlaymode))
                    if (GUILayout.Button(new GUIContent("Delete...", "Delete this catalog record only. The prefab and loot table are kept. Checks references first."),
                        EditorStyles.miniButton, GUILayout.Width(72))) QueueContainerDeleteCheck(container);
                ObserveGuiControl("Delete", GUILayoutUtility.GetLastRect());
            }
            if (_deleteBox != null)
            {
                // A dedicated, scrollable mode: never bury review actions above the loot-table editor.
                _worldLootTableScroll = EditorGUILayout.BeginScrollView(_worldLootTableScroll, false, true);
                DrawContainerDeletePanel();
                EditorGUILayout.EndScrollView();
                return;
            }
            if (container != null) DrawContainerNaming(container);
            _worldTypeDetailTab = (WorldTypeDetailTab)GUILayout.Toolbar((int)_worldTypeDetailTab,
                new[] { "Loot Items", "Box Settings" }, GUILayout.Height(28));
            _worldLootTableScroll = EditorGUILayout.BeginScrollView(_worldLootTableScroll, false, true);
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 125;
            try
            {
                _lootStagePreview = EditorGUILayout.Popup("Stage preview", _lootStagePreview,
                    new[] { "Home / Base weights", "Stage 1 · LV1–4", "Stage 2 · LV2–5", "Stage 3 · LV3–6", "Stage 4 · LV4–7", "Stage 5 · LV5–8" });
                DrawEquipmentStageHint(_lootStagePreview);
                if (_worldTypeDetailTab == WorldTypeDetailTab.Items) DrawSimpleItemPool(table);
                else if (container != null) DrawSimpleContainerSettings(container);
                else EditorGUILayout.HelpBox("Choose a box card to edit its capacity.", MessageType.Info);
            }
            finally { EditorGUIUtility.labelWidth = previousLabelWidth; }
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawSimpleItemPool(LootTableSO table)
    {
        ObserveGuiControl("LootItems/Begin", Rect.zero);
        if (table == null)
        {
            EditorGUILayout.HelpBox("Create this box's own table to get started.", MessageType.Warning);
            DrawWorldTableActions(null, 0);
            return;
        }
        int users = CountSharedTableUsers(_containerCatalog, table);
        string title = table.name;
        bool needsConversion = users > 1 || _selectedWorldLootObject?.LootObjectType != null;
        EditorGUILayout.HelpBox(needsConversion
            ? "Legacy or duplicated table detected. Editing is blocked until this box has an independent copy."
            : $"OWN TABLE: {title}\nOnly this box changes. Copy / Paste copies contents, never links boxes.",
            needsConversion ? MessageType.Warning : MessageType.Info);
        DrawWorldTableActions(table, users);
        if (needsConversion) return;
        LootObjectPool pool = table.Pool;
        EditorGUILayout.Space(4);
        DrawWorldItemDropTarget(table, pool);
        bool quickItemRegistered = MakerRegistrationIndex.Contains(table, _worldQuickAddItem);
        using (new EditorGUILayout.HorizontalScope())
        {
            _worldQuickAddItem = (ItemSO)EditorGUILayout.ObjectField(_worldQuickAddItem, typeof(ItemSO), false);
            quickItemRegistered = MakerRegistrationIndex.Contains(table, _worldQuickAddItem);
            using (new EditorGUI.DisabledScope(_worldQuickAddItem == null || quickItemRegistered))
                if (GUILayout.Button("+ Add", GUILayout.Width(64)))
                {
                    RecordLootTable(table, "Add Loot Item");
                    int added = AddItemsToWorldPool(pool, new[] { _worldQuickAddItem });
                    ShowNotification(new GUIContent(added > 0 ? "Item added." : "This item is already in the list."));
                    _worldQuickAddItem = null;
                }
        }
        MakerRegistrationUi.ItemSummary(_worldQuickAddItem, quickItemRegistered ? "이 상자에 등록됨" : "이 상자에는 미등록");
        DrawRegistrationCandidates(table);
        if (CountWorldPoolItems(table) == 0)
            EditorGUILayout.HelpBox("Empty list. Choose an item above and click + Add, or drag items from Item Maker.", MessageType.Warning);
        EditorGUILayout.Space(5);
        _showItemProbabilities = EditorGUILayout.ToggleLeft("Edit individual item chances", _showItemProbabilities);
        EditorGUILayout.LabelField("Sort: Rarity > Item Category > Name (A-Z)", EditorStyles.miniLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Expand Categories", EditorStyles.miniButtonLeft))
                LootMakerItemView.SetAllCategoriesExpanded(table, true);
            ObserveGuiControl("LootItems/ExpandCategories", GUILayoutUtility.GetLastRect());
            if (GUILayout.Button("Collapse Categories", EditorStyles.miniButtonRight))
                LootMakerItemView.SetAllCategoriesExpanded(table, false);
            ObserveGuiControl("LootItems/CollapseCategories", GUILayoutUtility.GetLastRect());
        }
        foreach (var tier in LootMakerItemView.SortedTiers(pool.rarityTiers)) DrawSimpleLootTier(table, tier);
        EditorGUILayout.Space(8);
        _showPoolAdvanced = EditorGUILayout.Foldout(_showPoolAdvanced, "Advanced: Table Asset", true);
        if (_showPoolAdvanced)
        {
            EditorGUILayout.HelpBox("Each filled slot draws Rarity > Category > Item. Auto category mode preserves existing item chances. Custom mode uses category weights and within-category item shares. Filled slots and rarity weights: Box Settings.", MessageType.None);
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("Table Asset", table, typeof(LootTableSO), false);
        }
    }

    private void DrawWorldItemDropTarget(LootTableSO table, LootObjectPool pool)
    {
        Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.box, GUILayout.Height(48), GUILayout.ExpandWidth(true));
        GUI.Box(rect, GUIContent.none);
        GUI.Label(rect, "+ Drop items here\nFrom Item Maker or Project - multiple items supported",
            new GUIStyle(EditorStyles.centeredGreyMiniLabel) { wordWrap = true });
        Event current = Event.current;
        if (!rect.Contains(current.mousePosition) ||
            (current.type != EventType.DragUpdated && current.type != EventType.DragPerform)) return;
        var items = new List<ItemSO>();
        foreach (var obj in DragAndDrop.objectReferences) if (obj is ItemSO item) items.Add(item);
        if (items.Count == 0) return;
        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        if (current.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            RecordLootTable(table, "Add Items to Box Loot Table");
            int added = AddItemsToWorldPool(pool, items);
            ShowNotification(new GUIContent($"Added {added} items. Existing entries kept."));
        }
        current.Use();
    }

    // Shared by drag/drop and tests. Never moves an item or modifies its ItemSO.
    public static int AddItemsToWorldPool(LootObjectPool pool, IEnumerable<ItemSO> items)
    {
        if (pool == null || items == null) return 0;
        pool.EnsureStandardTiers();
        int count = 0;
        foreach (var item in items)
        {
            if (item == null) continue;
            bool exists = false;
            foreach (var tier in pool.rarityTiers)
                if (ContainsItem(tier.items, item)) { exists = true; break; }
            if (exists) continue;
            var targetTier = pool.FindTier(item.Rarity);
            if (targetTier == null) continue;
            targetTier.items.Add(new LootItemEntry { item = item, minAmount = 1, maxAmount = 1 });
            count++;
        }
        return count;
    }

    private void DrawSimpleLootTier(LootTableSO table, LootRarityTier tier)
    {
        if (tier == null) return;
        string key = $"{table.GetInstanceID()}:{tier.rarity}";
        bool expanded = !_worldTierExpanded.TryGetValue(key, out bool saved) || saved;
        expanded = EditorGUILayout.Foldout(expanded, $"{tier.rarity} - {CountValidItemEntries(tier)} items", true, EditorStyles.foldoutHeader);
        ObserveGuiControl("LootItems/Tier/" + tier.rarity, GUILayoutUtility.GetLastRect());
        _worldTierExpanded[key] = expanded;
        if (!expanded || tier.items == null) return;
        var settings = table.Pool.FindCategorySettings(tier.rarity);
        bool customCategories = settings?.useCustomCategoryWeights == true;
        bool nextCustom = EditorGUILayout.ToggleLeft(new GUIContent("Custom category chances",
            "Auto preserves existing tier-wide item probabilities. Custom chooses a category by weight, then applies each item's Custom Share only INSIDE that category."), customCategories);
        ObserveGuiControl("LootItems/CategoryChances/" + tier.rarity, GUILayoutUtility.GetLastRect());
        if (nextCustom != customCategories)
        {
            LootMakerAuthoring.SetCustomCategoryChances(table, tier, nextCustom);
            settings = table.Pool.FindCategorySettings(tier.rarity);
            customCategories = nextCustom;
        }
        EditorGUILayout.LabelField(customCategories
            ? "Category weights are relative (70:30 = 70%:30%). 0 excludes a category. New categories start at weight 1."
            : "Auto: existing item chances preserved. Enable Custom to set category weights.", EditorStyles.wordWrappedMiniLabel);
        if (customCategories && tier.items.Exists(item => item?.useManualShare == true))
            EditorGUILayout.LabelField("Custom item shares now apply within each category, not across the whole tier.", EditorStyles.wordWrappedMiniLabel);
        var distribution = table.GetCategorySelection(tier);
        if (tier.items.Count > 0 && !distribution.HasCandidates)
            EditorGUILayout.HelpBox("No eligible category/items in this tier. Give a category and at least one of its items a positive weight; this tier is currently excluded.", MessageType.Warning);
        string lastCategory = null;
        bool categoryExpanded = true;
        var rows = LootMakerItemView.SortedRows(tier);
        foreach (var row in rows)
        {
            var entry = row.Entry;
            if (row.CategoryKey != lastCategory)
            {
                EditorGUILayout.Space(3);
                categoryExpanded = LootMakerItemView.IsCategoryExpanded(table, tier.rarity, row.CategoryKey);
                int count = rows.FindAll(other => other.CategoryKey == row.CategoryKey).Count;
                bool next;
                using (new EditorGUI.IndentLevelScope())
                    next = EditorGUILayout.Foldout(categoryExpanded, $"{row.CategoryLabel} ({count})", true);
                if (next != categoryExpanded)
                    LootMakerItemView.SetCategoryExpanded(table, tier.rarity, row.CategoryKey, next);
                categoryExpanded = next;
                ObserveGuiControl("LootItems/Category/" + tier.rarity + "/" + row.CategoryOrder, GUILayoutUtility.GetLastRect());
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!customCategories || entry.item == null))
                    {
                        EditorGUI.BeginChangeCheck();
                        float value = EditorGUILayout.FloatField(new GUIContent("Category Weight", "Relative weight, not a fixed percentage. Empty or disabled categories are excluded and the remaining weights are normalized."),
                            customCategories ? (float)(distribution.Groups.Find(g => g.Key == row.CategoryKey)?.CategoryWeight ?? 0d) : distribution.CategoryPercent(row.CategoryKey));
                        ObserveGuiControl("LootItems/CategoryWeight/" + tier.rarity + "/" + row.CategoryKey, GUILayoutUtility.GetLastRect());
                        if (EditorGUI.EndChangeCheck())
                        {
                            LootMakerAuthoring.SetCategoryWeight(table, tier, row.CategoryKey, value);
                            distribution = table.GetCategorySelection(tier);
                        }
                    }
                    GUILayout.Label($"{distribution.CategoryPercent(row.CategoryKey):0.##}% in tier", EditorStyles.miniLabel, GUILayout.Width(100));
                }
                if (_showItemProbabilities && customCategories)
                {
                    float reserved = 0f;
                    foreach (var candidate in rows)
                        if (candidate.CategoryKey == row.CategoryKey && candidate.Entry.item != null && candidate.Entry.useManualShare)
                            reserved += (float)LootCategorySelection.Positive(candidate.Entry.manualSharePercent);
                    if (reserved > 100f) EditorGUILayout.HelpBox("Custom item shares exceed 100% in this category. Manual entries are normalized; Automatic entries get 0%.", MessageType.Warning);
                }
                lastCategory = row.CategoryKey;
            }
            if (!categoryExpanded) continue;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    Texture icon = entry.item != null && entry.item.PreviewImage != null
                        ? entry.item.PreviewImage.texture : entry.item != null ? AssetPreview.GetMiniThumbnail(entry.item) : Texture2D.grayTexture;
                    GUILayout.Label(icon, GUILayout.Width(28), GUILayout.Height(28));
                    // Replacing an item through add/remove prevents accidental cross-rarity entries.
                    using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField(entry.item, typeof(ItemSO), false);
                    ObserveGuiControl("LootItems/Row/" + tier.rarity + "/" + row.SourceIndex, GUILayoutUtility.GetLastRect());
                    if (GUILayout.Button(new GUIContent("×", "Remove from this list only. The original item asset is kept."), GUILayout.Width(24)))
                    {
                        RecordLootTable(table, "Remove Loot Item");
                        tier.items.RemoveAt(row.SourceIndex);
                        return;
                    }
                    ObserveGuiControl("LootItems/Delete/" + tier.rarity + "/" + row.SourceIndex, GUILayoutUtility.GetLastRect());
                }
                MakerRegistrationUi.ItemSummary(entry.item, "이 상자에 등록됨");
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(new GUIContent("Qty", "Item units in ONE filled slot. Clamped to the item's Max Stack."), GUILayout.Width(30));
                    EditorGUI.BeginChangeCheck();
                    int min = EditorGUILayout.IntField(entry.minAmount, GUILayout.MinWidth(35));
                    GUILayout.Label("~", GUILayout.Width(12));
                    int max = EditorGUILayout.IntField(entry.maxAmount, GUILayout.MinWidth(35));
                    if (EditorGUI.EndChangeCheck())
                    {
                        RecordLootTable(table, "Edit Loot Item Amount");
                        entry.minAmount = Mathf.Max(1, min);
                        entry.maxAmount = Mathf.Max(entry.minAmount, max);
                    }
                    float share = customCategories ? distribution.ItemPercentInCategory(entry) : LootProgressionConfigSO.GetResolvedItemSharePercent(tier, entry);
                    GUILayout.Label($"{(customCategories ? "Category" : "Tier")} share: {share:0.#}%",
                        EditorStyles.miniLabel, GUILayout.Width(123));
                }
                if (_selectedWorldLootObject != null)
                    EditorGUILayout.LabelField($"{(_lootStagePreview > 0 ? "Stage " + _lootStagePreview : "Base")}: {table.GetEffectiveItemPercent(tier, entry, _selectedWorldLootObject.RarityWeights, _lootStagePreview):0.##}% per filled slot", EditorStyles.miniLabel);
                if (entry.item != null && entry.maxAmount > Mathf.Max(1, entry.item.MaxStack))
                    EditorGUILayout.HelpBox($"One slot holds at most {Mathf.Max(1, entry.item.MaxStack)}. Generated quantity will be capped, not split into extra slots.", MessageType.Warning);
                if (_showItemProbabilities)
                {
                    EditorGUI.BeginChangeCheck();
                    bool manual = EditorGUILayout.Toggle("Custom Share", entry.useManualShare);
                    float percent = entry.manualSharePercent;
                    if (manual) percent = EditorGUILayout.Slider(customCategories ? "Share in Category (%)" : "Share in Tier (%)", percent, 0, 100);
                    if (EditorGUI.EndChangeCheck())
                    {
                        RecordLootTable(table, "Edit Loot Item Share");
                        entry.useManualShare = manual;
                        entry.manualSharePercent = percent;
                    }
                }
                if (entry.item != null && entry.item.Rarity != tier.rarity)
                    EditorGUILayout.HelpBox("Item rarity changed. Remove it from this list, then add it again to use its current rarity.", MessageType.Warning);
            }
        }
        float reservedShare = 0f;
        foreach (var entry in tier.items)
            if (entry?.item != null && entry.useManualShare) reservedShare += Mathf.Clamp(entry.manualSharePercent, 0f, 100f);
        if (_showItemProbabilities && !customCategories && reservedShare > 100f)
            EditorGUILayout.HelpBox("Custom shares exceed 100% in this tier.", MessageType.Warning);
    }

    private static string SizeLabel(LootContainerSize size) => size switch
    {
        LootContainerSize.VerySmall => "Very Small",
        LootContainerSize.Small => "Small",
        LootContainerSize.Medium => "Medium",
        LootContainerSize.Large => "Large",
        LootContainerSize.VeryLarge => "Very Large",
        _ => "Custom"
    };

    private void DrawSimpleContainerSettings(LootObjectSO container)
    {
        EditorGUILayout.HelpBox("Only this box changes here. Other boxes are unchanged.", MessageType.Info);
        EditorGUILayout.LabelField("QUICK SIZE PRESETS", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawSizePreset(container, LootContainerSize.VerySmall, "Very Small | 3 slots");
            DrawSizePreset(container, LootContainerSize.Small, "Small | 4 slots");
            DrawSizePreset(container, LootContainerSize.Medium, "Medium | 6 slots");
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawSizePreset(container, LootContainerSize.Large, "Large | 9 slots");
            DrawSizePreset(container, LootContainerSize.VeryLarge, "Very Large | 12 slots");
        }
        EditorGUILayout.LabelField("Changes capacity only. Fill range is limited by capacity; rarity weights stay unchanged.", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(8);
        DrawBoxGenerationFields(container, _lootStagePreview);
        _showContainerAdvanced = EditorGUILayout.Foldout(_showContainerAdvanced, "Advanced: Box Assets", true);
        if (_showContainerAdvanced)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Box Data", container, typeof(LootObjectSO), false);
                EditorGUILayout.ObjectField("Prefab", container.WorldPrefab, typeof(GameObject), false);
            }
            if (container.WorldPrefab == null)
                EditorGUILayout.HelpBox("Preview prefab is missing. Use Select Asset and check World Prefab.", MessageType.Warning);
        }
    }

    private void DrawSizePreset(LootObjectSO container, LootContainerSize size, string label)
    {
        const string tooltip = "Set capacity only. Filled-slot range cannot exceed capacity. Keeps rarity weights and loot list.";
        if (!GUILayout.Button(new GUIContent(label, tooltip), GUILayout.Height(28))) return;
        Undo.RecordObject(container, "Apply Loot Size Preset");
        container.ApplySizePreset(size);
        EditorUtility.SetDirty(container);
    }

    internal static void DrawBoxGenerationFields(LootObjectSO container, int previewStage = 0)
    {
        EditorGUI.BeginChangeCheck();
        int capacity = EditorGUILayout.IntField("Total Slots", container.LootSlotCount);
        int min = EditorGUILayout.IntField(new GUIContent("Min Filled Slots", "Guaranteed occupied slots. Use 0 to allow empty boxes."), container.MinGeneratedSlots);
        int max = EditorGUILayout.IntField("Max Filled Slots", container.MaxGeneratedSlots);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(container, "Edit Box Filled Slots");
            using var data = new SerializedObject(container);
            data.FindProperty("_lootSlotCount").intValue = Mathf.Max(1, capacity);
            data.ApplyModifiedProperties();
            container.SetGeneratedSlotRange(min, max);
            EditorUtility.SetDirty(container);
        }
        EditorGUILayout.HelpBox($"Fill {container.MinGeneratedSlots}-{container.MaxGeneratedSlots} of {container.LootSlotCount} slots.\n" +
            "Each filled slot gets one stack. Duplicate items stay in separate slots. No second success/failure check.", MessageType.None);
        if (!container.HasSlotGenerationSettings)
            EditorGUILayout.HelpBox("Using the previous count range, capped to capacity. Old per-attempt success chance is no longer used. Save to store these as box-specific settings.", MessageType.Info);
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("THIS BOX'S RARITY WEIGHTS", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Base weights, not fixed percentages. Actual includes the selected stage's armor/ammo LV rule; other loot keeps its relative share.", EditorStyles.wordWrappedMiniLabel);
        using (var data = new SerializedObject(container))
        {
            var weights = data.FindProperty("_rarityWeights");
            DrawBoxRarityWeight(container, weights, "normal", PlayerShopItemRarity.Normal, previewStage);
            DrawBoxRarityWeight(container, weights, "rare", PlayerShopItemRarity.Rare, previewStage);
            DrawBoxRarityWeight(container, weights, "unique", PlayerShopItemRarity.Unique, previewStage);
            DrawBoxRarityWeight(container, weights, "legendary", PlayerShopItemRarity.Legendary, previewStage);
            data.ApplyModifiedProperties();
        }
        if (container.MaxGeneratedSlots > 0 && (container.LootTable == null || !container.LootTable.HasSlotItems(container.RarityWeights, previewStage)))
            EditorGUILayout.HelpBox("Cannot generate in this stage: add eligible items with positive weights. Armor/ammo also need candidates inside the stage's LV range. This is a configuration error, not an empty-box chance.", MessageType.Error);
        if (container.WorldPrefab != null && container.WorldPrefab.GetComponentInChildren<LockableLootObject>(true) != null)
            EditorGUILayout.HelpBox("The lock controls access only for slot-based boxes. Legacy lock bonus rolls / rarity multipliers are not applied; author all rewards here.", MessageType.Info);
    }

    private static void DrawBoxRarityWeight(LootObjectSO box, SerializedProperty weights, string field, PlayerShopItemRarity rarity, int previewStage)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(weights.FindPropertyRelative(field), new GUIContent(rarity.ToString()));
            float actual = box.LootTable != null ? box.LootTable.GetEffectiveTierPercent(rarity, box.RarityWeights, previewStage) : 0f;
            GUILayout.Label($"Actual {actual:0.##}%", EditorStyles.miniLabel, GUILayout.Width(92));
        }
    }

    private static void DrawEquipmentStageHint(int stage)
    {
        string message = stage > 0
            ? $"방탄복·일반 탄약: LV{stage} / {stage + 1} / {stage + 2} / {stage + 3} = 60 / 30 / 9 / 1\n" +
              "기존 방탄복·각 탄종의 총 비중은 유지하고 LV 비율만 교체합니다. 없는 LV·비중 0 후보는 제외 후 재계산합니다."
            : "Home / 기본 비중 미리보기입니다. Stage 1–5에서는 방탄복·일반 탄약의 LV 비율에 공통 60 / 30 / 9 / 1 규칙이 적용됩니다.";
        EditorGUILayout.HelpBox(message + "\n미리보기는 데이터·실제 스테이지를 변경하지 않습니다.", MessageType.Info);
    }


    private static List<LootObjectSO> FindWorldLootObjects()
    {
        var result = new List<LootObjectSO>();
        foreach (string guid in AssetDatabase.FindAssets("t:LootObjectSO"))
        {
            var container = AssetDatabase.LoadAssetAtPath<LootObjectSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (container != null) result.Add(container);
        }
        result.Sort((a, b) => string.Compare(GetContainerTitle(a), GetContainerTitle(b), System.StringComparison.OrdinalIgnoreCase));
        return result;
    }

    private static Texture GetAssetPreview(Object asset) => asset == null
        ? Texture2D.grayTexture : AssetPreview.GetAssetPreview(asset) ?? AssetPreview.GetMiniThumbnail(asset) ?? Texture2D.grayTexture;

    private static int CountWorldPoolItems(LootTableSO table)
    {
        if (table == null) return 0;
        int count = 0;
        foreach (var tier in table.Pool.rarityTiers) count += CountValidItemEntries(tier);
        return count;
    }


    // Purpose: Counts filled entries for the direct world-object pool display.
    private static int CountValidItemEntries(LootRarityTier tier)
    {
        if (tier?.items == null)
            return 0;

        int count = 0;
        for (int i = 0; i < tier.items.Count; i++)
        {
            if (tier.items[i]?.item != null)
                count++;
        }

        return count;
    }

    // Purpose: Registers Undo and persistence for one directly authored LootTableSO.
    private static void RecordLootTable(LootTableSO table, string action)
    {
        MakerRegistrationIndex.Invalidate();
        if (table == null)
            return;

        Undo.RecordObject(table, action);
        EditorUtility.SetDirty(table);
    }

    /// <summary>
    /// Keeps sale-only valuables visible at the config level instead of making
    /// designers copy the same entries into every enemy and chest pool. The
    /// runtime rolls this catalog additively after a source pool rolls.
    /// </summary>
    private void DrawGlobalValuableCatalog()
    {
        EditorGUILayout.Space(4f);
        _showGlobalValuableCatalog = EditorGUILayout.Foldout(
            _showGlobalValuableCatalog,
            $"Shared Valuable Catalog ({_config.ValuableLootItems.Count} sale-only items)",
            true,
            EditorStyles.foldoutHeader);
        if (!_showGlobalValuableCatalog)
            return;

        SerializedObject serializedConfig = new SerializedObject(_config);
        SerializedProperty chance = serializedConfig.FindProperty("_valuableLootRollChance");
        SerializedProperty items = serializedConfig.FindProperty("_valuableLootItems");

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(
                "Each valid enemy or search container can add one sell-only valuable after its normal source-pool roll. This catalog is not sold by the Home shop; it is only loot the player can sell there.",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(chance, new GUIContent("Additional Valuable Roll Chance"));
            EditorGUILayout.PropertyField(items, new GUIContent("Valuable Item Assets"), true);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_config, "Edit Shared Valuable Catalog");
                serializedConfig.ApplyModifiedProperties();
                EditorUtility.SetDirty(_config);
            }

            if (GUILayout.Button("Select Valuable Assets", GUILayout.Width(180f)))
                Selection.objects = ToObjectArray(_config.ValuableLootItems);
        }
    }

    // Purpose: Performs the to object array workflow for this component.
    private static Object[] ToObjectArray(IReadOnlyList<ItemSO> items)
    {
        if (items == null || items.Count == 0)
            return System.Array.Empty<Object>();

        List<Object> objects = new List<Object>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
                objects.Add(items[i]);
        }

        return objects.ToArray();
    }

    // Purpose: Counts the available sources.
    private int CountSources(LootSourceKind kind)
    {
        int count = 0;
        IReadOnlyList<LootSourceDefinition> sources = _config.Sources;
        for (int i = 0; i < sources.Count; i++)
        {
            if (sources[i] != null && sources[i].sourceKind == kind)
                count++;
        }

        return count;
    }

    // Purpose: Checks whether the current data matches source filter.
    private bool MatchesSourceFilter(LootSourceDefinition source)
    {
        if (_sourceFilterIndex == 1 && source.sourceKind != LootSourceKind.EnemyShooter)
            return false;
        if (_sourceFilterIndex == 2 && source.sourceKind != LootSourceKind.LootableObject)
            return false;

        if (string.IsNullOrWhiteSpace(_sourceSearch))
            return true;

        string search = _sourceSearch.Trim();
        string title = GetSourceTitle(source, 0);
        string target = source.sourceKind == LootSourceKind.EnemyShooter
            ? source.enemyShooterConfig?.name
            : source.sourceId;
        return title.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               (!string.IsNullOrWhiteSpace(target) &&
                target.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    // Purpose: Draws the source row authoring UI.
    private bool DrawSourceRow(LootSourceDefinition source, int sourceIndex)
    {
        bool isBound = IsSourceBindingComplete(source);
        string kind = source.sourceKind == LootSourceKind.EnemyShooter ? "ENEMY" : "OBJECT";
        string title = GetSourceTitle(source, sourceIndex + 1);
        string binding = source.sourceKind == LootSourceKind.EnemyShooter
            ? (source.enemyShooterConfig != null ? source.enemyShooterConfig.name : "Enemy Config missing")
            : (!string.IsNullOrWhiteSpace(source.sourceId) ? $"ID: {source.sourceId}" : "Source Id missing");
        int itemCount = CountPoolItems(GetCommonPoolForDisplay(source));
        bool isSelected = sourceIndex == _selectedSourceIndex;
        Rect card = GUILayoutUtility.GetRect(1f, 58f, GUILayout.ExpandWidth(true));
        Color background = isSelected
            ? new Color(0.08f, 0.38f, 0.48f, 1f)
            : isBound
                ? new Color(0.07f, 0.19f, 0.23f, 1f)
                : new Color(0.37f, 0.18f, 0.12f, 1f);
        EditorGUI.DrawRect(card, background);
        GUI.Box(card, GUIContent.none, EditorStyles.helpBox);
        GUI.Label(new Rect(card.x + 9f, card.y + 6f, card.width - 18f, 18f), $"{kind}  |  {title}", EditorStyles.boldLabel);
        GUI.Label(new Rect(card.x + 9f, card.y + 25f, card.width - 18f, 15f), binding, EditorStyles.miniLabel);
        GUI.Label(
            new Rect(card.x + 9f, card.y + 41f, card.width - 18f, 14f),
            isBound ? $"Default: {itemCount} entries  |  {CountStageOverrides(source)} stage override(s)" : "Runtime binding is incomplete",
            EditorStyles.miniLabel);
        return GUI.Button(card, new GUIContent(string.Empty, $"Runtime Binding: {binding}\nRegistered Item Entries: {itemCount}"), GUIStyle.none);
    }

    // Purpose: Draws the selected source overview authoring UI.
    private void DrawSelectedSourceOverview(LootSourceDefinition source)
    {
        string kind = source.sourceKind == LootSourceKind.EnemyShooter ? "Enemy Drop Source" : "Search Container Source";
        string binding = source.sourceKind == LootSourceKind.EnemyShooter
            ? (source.enemyShooterConfig != null ? source.enemyShooterConfig.name : "Missing EnemyShooterConfigSO")
            : (source.lootObjectPrefab != null
                ? $"{source.lootObjectPrefab.name}  |  ID: {source.sourceId}"
                : $"Missing Loot Object Prefab  |  ID: {source.sourceId}");
        LootStagePool selectedPool = source.ResolvePool(_selectedStageIndex);
        bool hasExactOverride = _selectedStageIndex > 0 && source.HasStageOverride(_selectedStageIndex);
        int itemCount = CountPoolItems(selectedPool);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField($"{kind}  /  {GetSourceTitle(source, _selectedSourceIndex + 1)}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Runtime Binding: {binding}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                _selectedStageIndex == 0
                    ? $"Default Table: {itemCount} item entries  •  Fallback for all stages without an override"
                    : $"Stage {_selectedStageIndex}: {itemCount} item entries  •  {(hasExactOverride ? "Exact override" : "Inherits Default")}",
                EditorStyles.miniLabel);
        }

        DrawSourcePreview(source);
    }

    // Purpose: Returns the current source title.
    private static string GetSourceTitle(LootSourceDefinition source, int fallbackIndex)
    {
        return string.IsNullOrWhiteSpace(source.displayName)
            ? $"Source {fallbackIndex}"
            : source.displayName;
    }

    // Purpose: Checks whether source binding complete is currently true.
    private static bool IsSourceBindingComplete(LootSourceDefinition source)
    {
        if (source == null)
            return false;

        return source.sourceKind == LootSourceKind.EnemyShooter
            ? source.enemyShooterConfig != null
            : source.lootObjectPrefab != null && !string.IsNullOrWhiteSpace(source.sourceId);
    }

    // Purpose: Counts the available pool items.
    private static int CountPoolItems(LootStagePool pool)
    {
        if (pool?.rarityTiers == null)
            return 0;

        int count = 0;
        for (int tierIndex = 0; tierIndex < pool.rarityTiers.Count; tierIndex++)
        {
            LootRarityTier tier = pool.rarityTiers[tierIndex];
            if (tier?.items == null)
                continue;

            for (int itemIndex = 0; itemIndex < tier.items.Count; itemIndex++)
            {
                if (tier.items[itemIndex]?.item != null)
                    count++;
            }
        }

        return count;
    }

    // Purpose: Counts authored stage tables without counting the default fallback table.
    private static int CountStageOverrides(LootSourceDefinition source)
    {
        if (source?.stagePools == null)
            return 0;

        int count = 0;
        for (int i = 0; i < source.stagePools.Count; i++)
        {
            if (source.stagePools[i] != null && source.stagePools[i].stageIndex > 0)
                count++;
        }

        return count;
    }

    // Purpose: Shows the source prefab image so object authors can verify the selected container at a glance.
    private void DrawSourcePreview(LootSourceDefinition source)
    {
        if (source == null)
            return;

        Object previewTarget = source.sourceKind == LootSourceKind.LootableObject
            ? source.lootObjectPrefab
            : source.enemyShooterConfig;
        if (previewTarget == null)
            return;

        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            Texture2D preview = AssetPreview.GetAssetPreview(previewTarget) ??
                                AssetPreview.GetMiniThumbnail(previewTarget);
            Rect previewRect = GUILayoutUtility.GetRect(78f, 78f, GUILayout.Width(78f), GUILayout.Height(78f));
            if (preview != null)
                GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit, true);
            else
            {
                GUI.Box(previewRect, "Preview\nloading", EditorStyles.centeredGreyMiniLabel);
                Repaint();
            }

            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField(
                    source.sourceKind == LootSourceKind.LootableObject ? "OBJECT PREFAB PREVIEW" : "ENEMY CONFIG PREVIEW",
                    EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField(previewTarget.name, EditorStyles.wordWrappedMiniLabel);
                if (source.sourceKind == LootSourceKind.LootableObject)
                    EditorGUILayout.LabelField($"Source Id: {source.sourceId}", EditorStyles.miniLabel);

                if (GUILayout.Button("Select Bound Asset", GUILayout.Width(132f)))
                    Selection.activeObject = previewTarget;
            }
        }
    }

    // Purpose: Returns the default fallback pool for source-card summaries.
    private static LootStagePool GetCommonPoolForDisplay(LootSourceDefinition source)
    {
        return source?.CommonPool;
    }

    // Purpose: Draws the source list authoring UI.
    private void DrawSourceList(float workspaceHeight)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(292f), GUILayout.Height(workspaceHeight)))
        {
            IReadOnlyList<LootSourceDefinition> sources = _config.Sources;
            int enemyCount = CountSources(LootSourceKind.EnemyShooter);
            int objectCount = CountSources(LootSourceKind.LootableObject);

            EditorGUILayout.LabelField("1. LOOT SOURCES", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"{enemyCount} Enemy Drops  •  {objectCount} Search Containers",
                EditorStyles.miniLabel);

            _sourceSearch = EditorGUILayout.TextField("Search", _sourceSearch);
            _sourceFilterIndex = GUILayout.Toolbar(_sourceFilterIndex, new[] { "All Enemy Sources", "Enemy" });

            EditorGUILayout.LabelField(
                Selection.activeObject == null
                    ? "Quick add: select an EnemyShooterConfigSO, or select a LootObjectSO / LootTableSO from the direct object browser above."
                    : $"Quick add selection: {Selection.activeObject.name}",
                EditorStyles.miniLabel);

            if (GUILayout.Button("Add Selected as Loot Source", GUILayout.Height(24f)))
                AddSelectedSource();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Enemy Drop Template"))
                    AddSource(LootSourceKind.EnemyShooter);
            }

            EditorGUILayout.Space(4f);
            _sourceScroll = EditorGUILayout.BeginScrollView(_sourceScroll, GUILayout.ExpandHeight(true));
            int visibleCount = 0;
            for (int i = 0; i < sources.Count; i++)
            {
                LootSourceDefinition source = sources[i];
                if (source == null || !MatchesSourceFilter(source))
                    continue;

                visibleCount++;
                if (DrawSourceRow(source, i))
                {
                    SourceSelectionUi.SelectionClicked();
                    _selectedSourceIndex = i;
                }
            }

            if (visibleCount == 0)
                EditorGUILayout.HelpBox("No source matches this filter.", MessageType.None);

            EditorGUILayout.EndScrollView();
        }
    }

    // Purpose: Draws the loot flow authoring UI.
    private void DrawLootFlow(float workspaceHeight)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(390f), GUILayout.Height(workspaceHeight)))
        {
            EditorGUILayout.LabelField("2. LOOT FLOW", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Source -> selected default/stage table -> rarity tiers. Select a Stage tab in the inspector, then click a tier card to edit it.",
                EditorStyles.wordWrappedMiniLabel);

            if (_selectedSourceIndex < 0 || _selectedSourceIndex >= _config.Sources.Count)
            {
                EditorGUILayout.HelpBox("Select a loot source to visualize its default or stage-specific table.", MessageType.Info);
                return;
            }

            LootSourceDefinition source = _config.Sources[_selectedSourceIndex];
            if (source != null)
                DrawLootFlowCanvas(source);
        }
    }

    // Purpose: Draws the loot flow canvas authoring UI.
    private void DrawLootFlowCanvas(LootSourceDefinition source)
    {
        const float canvasHeight = 338f;
        Rect canvas = GUILayoutUtility.GetRect(1f, canvasHeight, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(canvas, new Color(0.045f, 0.08f, 0.10f));
        GUI.Box(canvas, GUIContent.none, EditorStyles.helpBox);

        bool hasExactOverride = _selectedStageIndex > 0 && source.HasStageOverride(_selectedStageIndex);
        LootStagePool pool = source.ResolvePool(_selectedStageIndex);
        float sourceWidth = Mathf.Min(258f, canvas.width - 32f);
        Rect sourceRect = new Rect(canvas.center.x - sourceWidth * 0.5f, canvas.y + 14f, sourceWidth, 56f);
        Rect poolRect = new Rect(canvas.x + 22f, canvas.y + 102f, canvas.width - 44f, 42f);
        DrawFlowConnector(
            new Vector2(sourceRect.center.x, sourceRect.yMax),
            new Vector2(poolRect.center.x, poolRect.yMin),
            new Color(0.28f, 0.82f, 0.88f, 0.85f));

        DrawLootFlowCard(
            sourceRect,
            GetSourceTitle(source, _selectedSourceIndex + 1),
            source.sourceKind == LootSourceKind.EnemyShooter ? "Enemy drop source" : "Search container source",
            IsSourceBindingComplete(source) ? "Runtime bound" : "Binding required",
            IsSourceBindingComplete(source) ? new Color(0.08f, 0.32f, 0.39f) : new Color(0.37f, 0.18f, 0.12f),
            null);
        DrawLootFlowCard(
            poolRect,
            GetStagePoolLabel(_selectedStageIndex),
            pool == null ? "Pool missing" : $"{CountPoolItems(pool)} item entries  |  {pool.minRollCount}-{pool.maxRollCount} rolls",
            _selectedStageIndex == 0
                ? "Fallback for stages without overrides"
                : hasExactOverride ? "Exact stage override" : "Currently inherits Default",
            new Color(0.12f, 0.25f, 0.30f),
            null);

        if (pool?.rarityTiers == null || pool.rarityTiers.Count == 0)
            return;

        int tierCount = pool.rarityTiers.Count;
        int columns = tierCount <= 2 ? tierCount : 2;
        float cardWidth = (canvas.width - 48f - (columns - 1) * 12f) / columns;
        for (int index = 0; index < tierCount; index++)
        {
            LootRarityTier tier = pool.rarityTiers[index];
            if (tier == null)
                continue;

            int row = index / columns;
            int column = index % columns;
            Rect tierRect = new Rect(
                canvas.x + 18f + column * (cardWidth + 12f),
                canvas.y + 182f + row * 70f,
                cardWidth,
                58f);
            DrawFlowConnector(
                new Vector2(poolRect.center.x, poolRect.yMax),
                new Vector2(tierRect.center.x, tierRect.yMin),
                GetRarityColor(tier.rarity));

            int itemCount = 0;
            if (tier.items != null)
            {
                for (int itemIndex = 0; itemIndex < tier.items.Count; itemIndex++)
                {
                    if (tier.items[itemIndex]?.item != null)
                        itemCount++;
                }
            }

            DrawLootFlowCard(
                tierRect,
                tier.rarity.ToString(),
                $"{tier.chancePercent:0.#}% chance  |  {itemCount} item(s)",
                "Click to edit this tier",
                GetRarityColor(tier.rarity),
                () => _tierExpanded[tier.rarity] = true);
        }
    }

    // Purpose: Draws the flow connector authoring UI.
    private static void DrawFlowConnector(Vector2 from, Vector2 to, Color color)
    {
        const float thickness = 2f;
        float middleY = (from.y + to.y) * 0.5f;
        EditorGUI.DrawRect(new Rect(from.x - thickness * 0.5f, from.y, thickness, middleY - from.y), color);
        EditorGUI.DrawRect(new Rect(Mathf.Min(from.x, to.x), middleY - thickness * 0.5f, Mathf.Abs(to.x - from.x), thickness), color);
        EditorGUI.DrawRect(new Rect(to.x - thickness * 0.5f, middleY, thickness, to.y - middleY), color);
    }

    // Purpose: Draws the loot flow card authoring UI.
    private static void DrawLootFlowCard(Rect rect, string title, string summary, string footer, Color background, System.Action onClick)
    {
        EditorGUI.DrawRect(rect, background);
        GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
        GUI.Label(new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, 17f), title, EditorStyles.boldLabel);
        GUI.Label(new Rect(rect.x + 8f, rect.y + 25f, rect.width - 16f, 15f), summary, EditorStyles.miniLabel);
        GUI.Label(new Rect(rect.x + 8f, rect.y + 41f, rect.width - 16f, 13f), footer, EditorStyles.miniLabel);
        if (onClick != null && GUI.Button(rect, GUIContent.none, GUIStyle.none))
            onClick();
    }

    // Purpose: Returns the current rarity color.
    private static Color GetRarityColor(PlayerShopItemRarity rarity)
    {
        return rarity switch
        {
            PlayerShopItemRarity.Normal => new Color(0.13f, 0.30f, 0.33f),
            PlayerShopItemRarity.Rare => new Color(0.12f, 0.24f, 0.48f),
            PlayerShopItemRarity.Unique => new Color(0.37f, 0.23f, 0.49f),
            PlayerShopItemRarity.Legendary => new Color(0.48f, 0.30f, 0.08f),
            _ => new Color(0.18f, 0.24f, 0.28f)
        };
    }

    // Purpose: Draws the selected source authoring UI.
    private void DrawSelectedSource(float workspaceHeight)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.Height(workspaceHeight)))
        {
            EditorGUILayout.LabelField("3. LOOT INSPECTOR", EditorStyles.boldLabel);
            if (_selectedSourceIndex < 0 || _selectedSourceIndex >= _config.Sources.Count)
            {
                EditorGUILayout.HelpBox("Select a loot source on the left, then edit its default table or one Stage 1-5 override.", MessageType.Info);
                return;
            }

            LootSourceDefinition source = _config.Sources[_selectedSourceIndex];
            if (source == null)
                return;

            DrawSelectedSourceOverview(source);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Duplicate as New", GUILayout.Width(116f)))
                    DuplicateSelectedSource(source);

                if (GUILayout.Button("Validate", GUILayout.Width(72f)))
                    ValidateSelectedSource(source);

                if (GUILayout.Button("Remove Source", GUILayout.Width(110f)))
                {
                    if (EditorUtility.DisplayDialog(
                            "Remove Loot Source",
                            $"Remove '{source.displayName}' from this config?",
                            "Remove",
                            "Cancel"))
                    {
                        Undo.RecordObject(_config, "Remove Loot Source");
                        _config.RemoveSource(source);
                        EditorUtility.SetDirty(_config);
                        _selectedSourceIndex = -1;
                        return;
                    }
                }
            }

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            DrawSourceBinding(source);
            DrawStageTable(source);
            DrawSourceValidationSummary();
            EditorGUILayout.EndScrollView();
        }
    }

    // Purpose: Draws the source validation summary authoring UI.
    private void DrawSourceValidationSummary()
    {
        if (_validatedSourceIndex != _selectedSourceIndex)
            return;

        if (_sourceValidationIssues.Count == 0)
        {
            EditorGUILayout.HelpBox("Selected Loot Source validation passed.", MessageType.Info);
            return;
        }

        EditorGUILayout.HelpBox(
            $"Selected Loot Source has {_sourceValidationIssues.Count} issue(s).",
            MessageType.Warning);
        for (int i = 0; i < _sourceValidationIssues.Count; i++)
            EditorGUILayout.LabelField($"• {_sourceValidationIssues[i]}", EditorStyles.wordWrappedMiniLabel);
    }

    // Purpose: Draws the source binding authoring UI.
    private void DrawSourceBinding(LootSourceDefinition source)
    {
        EditorGUILayout.Space(5f);
        _showSourceBinding = EditorGUILayout.Foldout(
            _showSourceBinding,
            "1. Runtime Binding",
            true,
            EditorStyles.foldoutHeader);
        if (!_showSourceBinding)
            return;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(
                source.sourceKind == LootSourceKind.EnemyShooter
                    ? "Bind the exact EnemyShooterConfigSO used by encounter waves."
                    : "Bind the matching loot-object prefab and keep Source Id equal to its LootableObject component.",
                EditorStyles.wordWrappedMiniLabel);
            DrawSourceFields(source);

            if (source.sourceKind == LootSourceKind.LootableObject)
                DrawLootObjectBindingCheck(source);
        }
    }

    // Purpose: Verifies that a selected object prefab can resolve this authored source at runtime.
    private void DrawLootObjectBindingCheck(LootSourceDefinition source)
    {
        if (source?.lootObjectPrefab == null)
            return;

        LootableObject lootable = source.lootObjectPrefab.GetComponentInChildren<LootableObject>(true);
        if (lootable == null)
        {
            EditorGUILayout.HelpBox(
                "This prefab does not contain LootableObject. It can be registered now, but it will not open the authored loot table until that component is added.",
                MessageType.Warning);
            return;
        }

        LootObjectSO lootObject = lootable.LootObject;
        if (lootObject == null)
        {
            EditorGUILayout.HelpBox(
                "This prefab has no direct LootObjectSO. World containers no longer resolve a config Source Id at runtime.",
                MessageType.Error);
            return;
        }

        EditorGUILayout.ObjectField("Prefab Loot Object", lootObject, typeof(LootObjectSO), false);
        EditorGUILayout.ObjectField("Direct Loot Table", lootObject.LootTable, typeof(LootTableSO), false);
        if (lootObject.WorldPrefab != null && lootObject.WorldPrefab != source.lootObjectPrefab)
        {
            EditorGUILayout.HelpBox(
                "The LootObjectSO World Prefab differs from this previous config row. Runtime uses the LootObjectSO directly.",
                MessageType.Info);
        }
    }

    // Purpose: Draws the quick add item authoring UI.
    private void DrawQuickAddItem(LootStagePool pool)
    {
        if (pool == null)
            return;

        EditorGUILayout.Space(5f);
        _showQuickAdd = EditorGUILayout.Foldout(
            _showQuickAdd,
            "Quick Add Item",
            true,
            EditorStyles.foldoutHeader);
        if (!_showQuickAdd)
            return;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(
                "Pick an ItemSO and rarity. Automatic entries rebalance themselves inside their rarity tier.",
                EditorStyles.wordWrappedMiniLabel);

            _quickAddItem = (ItemSO)EditorGUILayout.ObjectField(
                "Item",
                _quickAddItem,
                typeof(ItemSO),
                false);
            MakerRegistrationUi.ItemSummary(_quickAddItem, MakerRegistrationIndex.Contains(pool, _quickAddItem)
                ? "이 적의 선택된 루팅 목록에 등록됨"
                : "이 적의 선택된 루팅 목록에는 미등록");
            _quickAddRarity = (PlayerShopItemRarity)EditorGUILayout.EnumPopup(
                "Rarity Tier",
                _quickAddRarity);

            using (new EditorGUILayout.HorizontalScope())
            {
                _quickAddMinAmount = Mathf.Max(1, EditorGUILayout.IntField("Min Amount", _quickAddMinAmount));
                _quickAddMaxAmount = Mathf.Max(_quickAddMinAmount, EditorGUILayout.IntField("Max Amount", _quickAddMaxAmount));
            }

            using (new EditorGUI.DisabledScope(_quickAddItem == null))
            {
                if (GUILayout.Button($"Add Item to {GetStagePoolLabel(_selectedStageIndex)}", GUILayout.Height(24f)))
                    AddQuickItem(pool);
            }
        }
    }

    // Purpose: Chooses one default or exact stage table and exposes safe copy, paste, and override actions.
    private void DrawStageTable(LootSourceDefinition source)
    {
        if (source == null)
            return;

        source.EnsureDefaultPool();

        EditorGUILayout.Space(5f);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("2. STAGE TABLE", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Default is the fallback. A Stage tab only becomes independent after you create or paste an override; this keeps shared tables easy to maintain.",
                EditorStyles.wordWrappedMiniLabel);

            int nextStageIndex = GUILayout.Toolbar(_selectedStageIndex, new[]
            {
                "Default",
                "Stage 1",
                "Stage 2",
                "Stage 3",
                "Stage 4",
                "Stage 5"
            });
            if (nextStageIndex != _selectedStageIndex)
            {
                _selectedStageIndex = nextStageIndex;
                _validatedSourceIndex = -1;
            }

            bool isDefault = _selectedStageIndex == 0;
            bool hasExactOverride = !isDefault && source.HasStageOverride(_selectedStageIndex);
            LootStagePool pool = isDefault
                ? source.CommonPool
                : source.FindExactPool(_selectedStageIndex);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    isDefault
                        ? "Default Table — inherited by stages without an override"
                        : hasExactOverride
                            ? $"Stage {_selectedStageIndex} Override — runtime uses this exact table"
                            : $"Stage {_selectedStageIndex} currently inherits Default",
                    EditorStyles.miniBoldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(
                    _poolClipboard != null
                        ? $"Copied: {_poolClipboardLabel}"
                        : "No copied table",
                    EditorStyles.miniLabel);
            }

            if (!isDefault && !hasExactOverride)
            {
                EditorGUILayout.HelpBox(
                    $"Stage {_selectedStageIndex} has no authored table. It will currently roll the Default table at runtime.",
                    MessageType.Info);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Create Override from Default", GUILayout.Height(24f)))
                    {
                        RecordConfig("Create Loot Stage Override");
                        source.GetOrCreatePool(_selectedStageIndex, copyDefaultPool: true);
                        _validatedSourceIndex = -1;
                    }

                    using (new EditorGUI.DisabledScope(_poolClipboard == null))
                    {
                        if (GUILayout.Button("Paste Copied Table as Override", GUILayout.Height(24f)))
                            PasteClipboardInto(source, _selectedStageIndex);
                    }
                }

                return;
            }

            if (pool == null)
                return;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Copy This Table", GUILayout.Width(116f)))
                {
                    _poolClipboard = LootStagePool.Clone(pool, pool.stageIndex);
                    _poolClipboardLabel = $"{GetSourceTitle(source, _selectedSourceIndex + 1)} / {GetStagePoolLabel(_selectedStageIndex)}";
                    ShowNotification(new GUIContent($"Copied {_poolClipboardLabel}."));
                }

                using (new EditorGUI.DisabledScope(_poolClipboard == null))
                {
                    if (GUILayout.Button("Paste Copied Table", GUILayout.Width(130f)))
                        PasteClipboardInto(source, _selectedStageIndex);
                }

                if (!isDefault)
                {
                    if (GUILayout.Button("Copy Default Here", GUILayout.Width(126f)))
                    {
                        RecordConfig("Copy Default Loot Table to Stage");
                        source.CopyPool(0, _selectedStageIndex);
                        _validatedSourceIndex = -1;
                    }

                    if (GUILayout.Button("Remove Override", GUILayout.Width(112f)))
                    {
                        if (EditorUtility.DisplayDialog(
                                "Remove Stage Loot Override",
                                $"Remove the Stage {_selectedStageIndex} table for '{source.displayName}'? Runtime will fall back to Default.",
                                "Remove Override",
                                "Cancel"))
                        {
                            RecordConfig("Remove Loot Stage Override");
                            source.RemoveStageOverride(_selectedStageIndex);
                            _validatedSourceIndex = -1;
                            return;
                        }
                    }
                }
            }

            DrawQuickAddItem(pool);
            DrawSelectedPool(pool, !isDefault);
        }
    }

    // Purpose: Applies the copied table as a detached exact table on the current source and stage.
    private void PasteClipboardInto(LootSourceDefinition source, int targetStageIndex)
    {
        if (source == null || _poolClipboard == null)
            return;

        RecordConfig("Paste Loot Table");
        LootStagePool target = source.GetOrCreatePool(targetStageIndex, copyDefaultPool: false);
        target.CopyFrom(_poolClipboard, targetStageIndex);
        _validatedSourceIndex = -1;
        ShowNotification(new GUIContent($"Pasted {_poolClipboardLabel} into {GetStagePoolLabel(targetStageIndex)}."));
    }

    // Purpose: Returns the concise label used consistently by the stage table UI.
    private static string GetStagePoolLabel(int stageIndex)
    {
        return stageIndex <= 0 ? "Default Table" : $"Stage {stageIndex} Table";
    }

    // Purpose: Draws the source fields authoring UI.
    private void DrawSourceFields(LootSourceDefinition source)
    {
        SourceSelectionUi.Synchronize(source, _selectedStageIndex.ToString());
        EditorGUI.BeginChangeCheck();
        string displayName = EditorGUILayout.TextField("Display Name (SO Fallback)", source.displayName);

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.EnumPopup("Source Kind", source.sourceKind);

        if (source.sourceKind == LootSourceKind.EnemyShooter)
        {
            EnemyShooterConfigSO enemy = (EnemyShooterConfigSO)EditorGUILayout.ObjectField(
                "Enemy Shooter Config",
                source.enemyShooterConfig,
                typeof(EnemyShooterConfigSO),
                false);

            if (EditorGUI.EndChangeCheck())
            {
                RecordConfig("Edit Enemy Loot Source");
                source.displayName = displayName;
                source.enemyShooterConfig = enemy;
                if (enemy != null && string.IsNullOrWhiteSpace(source.sourceId))
                    source.sourceId = enemy.Id;
            }
        }
        else
        {
            GameObject prefab = (GameObject)EditorGUILayout.ObjectField(
                "Loot Object Prefab (reference)",
                source.lootObjectPrefab,
                typeof(GameObject),
                false);
            string sourceId = EditorGUILayout.TextField("Source Id", source.sourceId);
            int lootSlotCount = EditorGUILayout.IntField(
                new GUIContent(
                    "Loot Slot Count",
                    "Loot Hub capacity authored by this source. World prefabs do not own this value."),
                source.LootSlotCount);

            if (EditorGUI.EndChangeCheck())
            {
                RecordConfig("Edit Object Loot Source");
                source.displayName = displayName;
                source.lootObjectPrefab = prefab;
                source.sourceId = sourceId;
                source.lootSlotCount = Mathf.Max(1, lootSlotCount);
            }
        }

    }

    // Purpose: Draws the currently selected default or stage-specific pool authoring UI.
    private void DrawSelectedPool(LootStagePool pool, bool isExactStageOverride)
    {
        EditorGUILayout.Space(5f);
        int itemCount = CountPoolItems(pool);
        _showCommonPool = EditorGUILayout.Foldout(
            _showCommonPool,
            $"Loot Table — {GetStagePoolLabel(_selectedStageIndex)}  ({itemCount} item entries)",
            true,
            EditorStyles.foldoutHeader);
        if (!_showCommonPool)
            return;

        if (pool == null)
            return;

        pool.EnsureDefaultTiers();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(
                _selectedStageIndex == 0
                    ? "This default table is used by any stage without its own override. Open only the rarity tier you want to edit."
                    : isExactStageOverride
                        ? "This exact table replaces the default table only in the selected stage."
                        : "This stage currently inherits the default table.",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.HorizontalScope())
            {
                int minRolls = EditorGUILayout.IntField("Min Rolls", pool.minRollCount);
                int maxRolls = EditorGUILayout.IntField("Max Rolls", pool.maxRollCount);
                if (EditorGUI.EndChangeCheck())
                {
                    RecordConfig("Edit Loot Stage Pool");
                    pool.minRollCount = Mathf.Max(1, minRolls);
                    pool.maxRollCount = Mathf.Max(pool.minRollCount, maxRolls);
                }
            }

            DrawEquipmentStageHint(_selectedStageIndex);
            var preview = LootProgressionConfigSO.GetStageEquipmentDistribution(pool, _selectedStageIndex);
            for (int tierIndex = 0; tierIndex < pool.rarityTiers.Count; tierIndex++)
                DrawTier(pool.rarityTiers[tierIndex], preview);
        }
    }

    // Purpose: Draws the tier authoring UI.
    private void DrawTier(LootRarityTier tier, StageEquipmentLootDistribution preview)
    {
        if (tier == null)
            return;

        tier.items ??= new List<LootItemEntry>();
        int validItemCount = 0;
        for (int i = 0; i < tier.items.Count; i++)
        {
            if (tier.items[i]?.item != null)
                validItemCount++;
        }

        bool defaultExpanded = validItemCount > 0;
        bool expanded = _tierExpanded.TryGetValue(tier.rarity, out bool savedExpanded)
            ? savedExpanded
            : defaultExpanded;
        expanded = EditorGUILayout.Foldout(
            expanded,
            $"{tier.rarity}   Base {tier.chancePercent:0.#} / Actual {preview.RarityPercent(tier.rarity):0.##}%   •   {validItemCount} item(s)",
            true,
            EditorStyles.foldoutHeader);
        _tierExpanded[tier.rarity] = expanded;

        if (!expanded)
            return;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                float chance = EditorGUILayout.Slider("Tier Chance", tier.chancePercent, 0f, 100f);
                if (EditorGUI.EndChangeCheck())
                {
                    RecordConfig("Edit Loot Tier Chance");
                    tier.chancePercent = chance;
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Item", EditorStyles.miniBoldLabel, GUILayout.MinWidth(160f));
                GUILayout.Label("Min", EditorStyles.miniBoldLabel, GUILayout.Width(42f));
                GUILayout.Label("Max", EditorStyles.miniBoldLabel, GUILayout.Width(42f));
                GUILayout.Label("Share", EditorStyles.miniBoldLabel, GUILayout.Width(126f));
                GUILayout.Label(new GUIContent("Actual", "Final chance per drop, including the stage LV rule. Excludes the separate global valuable roll."), EditorStyles.miniBoldLabel, GUILayout.Width(52f));
            }

            for (int itemIndex = 0; itemIndex < tier.items.Count; itemIndex++)
            {
                LootItemEntry entry = tier.items[itemIndex];
                if (entry == null)
                    continue;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    ItemSO item = (ItemSO)EditorGUILayout.ObjectField(entry.item, typeof(ItemSO), false, GUILayout.MinWidth(160f));
                    int minAmount = EditorGUILayout.IntField(entry.minAmount, GUILayout.Width(42f));
                    int maxAmount = EditorGUILayout.IntField(entry.maxAmount, GUILayout.Width(42f));
                    bool manual = EditorGUILayout.ToggleLeft("Manual", entry.useManualShare, GUILayout.Width(70f));
                    float manualPercent = entry.manualSharePercent;
                    if (manual)
                        manualPercent = EditorGUILayout.FloatField(manualPercent, GUILayout.Width(56f));

                    if (EditorGUI.EndChangeCheck())
                    {
                        RecordConfig("Edit Loot Item Entry");
                        entry.item = item;
                        entry.minAmount = Mathf.Max(1, minAmount);
                        entry.maxAmount = Mathf.Max(entry.minAmount, maxAmount);
                        entry.useManualShare = manual;
                        entry.manualSharePercent = Mathf.Clamp(manualPercent, 0f, 100f);
                    }

                    float resolved = preview.ItemPercent(entry);
                    GUILayout.Label($"{resolved:0.##}%", EditorStyles.miniLabel, GUILayout.Width(52f));

                    if (GUILayout.Button("X", GUILayout.Width(22f)))
                    {
                        RecordConfig("Remove Loot Item Entry");
                        tier.items.RemoveAt(itemIndex);
                        return;
                    }
                }
            }

            if (GUILayout.Button($"+ Add Item to {tier.rarity}"))
            {
                RecordConfig("Add Loot Item Entry");
                tier.items.Add(new LootItemEntry());
            }

            float manualTotal = 0f;
            for (int i = 0; i < tier.items.Count; i++)
            {
                if (tier.items[i] != null && tier.items[i].useManualShare)
                    manualTotal += Mathf.Max(0f, tier.items[i].manualSharePercent);
            }

            if (manualTotal > 100f)
            {
                EditorGUILayout.HelpBox(
                    $"Manual total is {manualTotal:0.##}%. Runtime normalizes manual entries to 100%, and automatic entries receive 0%.",
                    MessageType.Warning);
            }
        }
    }

    // Purpose: Adds the requested source to this component.
    private void AddSource(LootSourceKind kind)
    {
        if (kind == LootSourceKind.LootableObject)
        {
            ShowNotification(new GUIContent("World containers use direct LootObjectSO -> LootTableSO assets, not Loot Progression Config rows."));
            return;
        }

        if (kind == LootSourceKind.LootableObject &&
            Selection.activeObject is GameObject selectedObject &&
            !PrefabUtility.IsPartOfPrefabAsset(selectedObject))
        {
            ShowNotification(new GUIContent("Loot Object sources must use a prefab selected from Project, not a scene object."));
            return;
        }

        Undo.RecordObject(_config, "Add Loot Source");

        if (kind == LootSourceKind.EnemyShooter && Selection.activeObject is EnemyShooterConfigSO enemy)
        {
            LootSourceDefinition existing = _config.FindEnemySource(enemy);
            if (existing != null)
            {
                _selectedSourceIndex = IndexOfSource(existing);
                ShowNotification(new GUIContent("That Enemy Shooter already has a loot source. Selected it instead."));
                return;
            }

            LootSourceDefinition source = _config.CreateSource(kind);
            source.enemyShooterConfig = enemy;
            source.displayName = enemy.DisplayName;
            source.sourceId = string.IsNullOrWhiteSpace(enemy.Id) ? enemy.name : enemy.Id;
            EditorUtility.SetDirty(_config);
            _selectedSourceIndex = _config.Sources.Count - 1;
            return;
        }

        if (kind == LootSourceKind.LootableObject && Selection.activeObject is GameObject prefab)
        {
            LootableObject lootable = prefab.GetComponentInChildren<LootableObject>(true);
            if (lootable == null || lootable.LootObject == null)
            {
                ShowNotification(new GUIContent("Assign LootObjectSO and LootTableSO directly on this prefab first."));
                return;
            }

            Selection.activeObject = lootable.LootObject;
            ShowNotification(new GUIContent("World loot is authored on the selected LootObjectSO, not in Loot Progression Config."));
            return;
        }

        _config.CreateSource(kind);
        EditorUtility.SetDirty(_config);
        _selectedSourceIndex = _config.Sources.Count - 1;
    }

    // Purpose: Adds the requested rare cache source to this component.
    private void AddRareCacheSource()
    {
        if (_config == null)
            return;

        const string rareCacheSourceId = "rare_cache";
        LootSourceDefinition existing = _config.FindLootObjectSource(rareCacheSourceId);
        if (existing != null)
        {
            _selectedSourceIndex = IndexOfSource(existing);
            ShowNotification(new GUIContent("Selected existing Rare Cache source."));
            return;
        }

        Undo.RecordObject(_config, "Add Rare Cache Loot Source");
        LootSourceDefinition source = _config.CreateSource(LootSourceKind.LootableObject);
        source.displayName = "Rare Cache";
        source.sourceId = rareCacheSourceId;

        if (Selection.activeObject is GameObject prefab &&
            PrefabUtility.IsPartOfPrefabAsset(prefab))
        {
            source.lootObjectPrefab = prefab;
        }

        EditorUtility.SetDirty(_config);
        _selectedSourceIndex = _config.Sources.Count - 1;
        _validatedSourceIndex = -1;
        ShowNotification(
            new GUIContent(
                source.lootObjectPrefab != null
                    ? "Created Rare Cache source and bound the selected prefab."
                    : "Created Rare Cache source. Bind its LootableObject prefab next."));
    }

    // Purpose: Adds the requested selected source to this component.
    private void AddSelectedSource()
    {
        if (Selection.activeObject is EnemyShooterConfigSO)
        {
            AddSource(LootSourceKind.EnemyShooter);
            return;
        }

        if (Selection.activeObject is GameObject selectedObject &&
            PrefabUtility.IsPartOfPrefabAsset(selectedObject))
        {
            AddSource(LootSourceKind.LootableObject);
            return;
        }

        if (Selection.activeObject is GameObject)
        {
            ShowNotification(new GUIContent("Select a loot-object prefab from Project, not a scene object."));
            return;
        }

        ShowNotification(new GUIContent("Select an EnemyShooterConfigSO or a loot-object prefab first."));
    }

    // Purpose: Adds the requested quick item to this component.
    private void AddQuickItem(LootStagePool pool)
    {
        if (pool == null)
            return;

        pool.EnsureDefaultTiers();
        LootRarityTier tier = pool.FindTier(_quickAddRarity);
        if (tier == null)
            return;

        tier.items ??= new List<LootItemEntry>();
        if (ContainsItem(tier.items, _quickAddItem))
        {
            ShowNotification(new GUIContent("This item is already in the selected rarity tier."));
            return;
        }

        RecordConfig("Quick Add Loot Item");
        tier.items.Add(
            new LootItemEntry
            {
                item = _quickAddItem,
                minAmount = Mathf.Max(1, _quickAddMinAmount),
                maxAmount = Mathf.Max(_quickAddMinAmount, _quickAddMaxAmount)
            });
        _quickAddItem = null;
        ShowNotification(new GUIContent($"Added item to {GetStagePoolLabel(_selectedStageIndex)}."));
    }

    // Purpose: Performs the duplicate selected source workflow for this component.
    private void DuplicateSelectedSource(LootSourceDefinition source)
    {
        if (_config == null || source == null)
            return;

        Undo.RecordObject(_config, "Duplicate Loot Source");
        LootSourceDefinition duplicate = _config.CreateSource(source.sourceKind);
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), duplicate);

        duplicate.displayName = string.IsNullOrWhiteSpace(source.displayName)
            ? "Loot Source Copy"
            : $"{source.displayName} Copy";
        duplicate.enemyShooterConfig = null;
        duplicate.lootObjectPrefab = null;
        duplicate.sourceId = MakeUniqueSourceId(
            string.IsNullOrWhiteSpace(source.sourceId)
                ? "loot_source_copy"
                : $"{source.sourceId}_copy",
            duplicate);
        duplicate.EnsureDefaultPool();

        EditorUtility.SetDirty(_config);
        _selectedSourceIndex = _config.Sources.Count - 1;
        _validatedSourceIndex = -1;
        ShowNotification(new GUIContent("Duplicated source without its runtime target. Bind a new enemy config or prefab."));
    }

    // Purpose: Validates selected source and reports invalid authored data.
    private void ValidateSelectedSource(LootSourceDefinition source)
    {
        _sourceValidationIssues.Clear();
        _validatedSourceIndex = _selectedSourceIndex;

        if (source == null)
        {
            _sourceValidationIssues.Add("The selected source is missing.");
            return;
        }

        if (string.IsNullOrWhiteSpace(source.displayName))
            _sourceValidationIssues.Add("Display Name is empty.");

        if (source.sourceKind == LootSourceKind.EnemyShooter && source.enemyShooterConfig == null)
            _sourceValidationIssues.Add("Enemy Shooter Config is missing.");

        if (source.sourceKind == LootSourceKind.LootableObject)
        {
            if (source.lootObjectPrefab == null)
                _sourceValidationIssues.Add("Loot Object Prefab is missing.");
            if (string.IsNullOrWhiteSpace(source.sourceId))
                _sourceValidationIssues.Add("Source Id is empty.");
        }

        ValidatePool(source.CommonPool, "Default Table");
        for (int stageIndex = 1; stageIndex <= 5; stageIndex++)
        {
            LootStagePool stagePool = source.FindExactPool(stageIndex);
            if (stagePool != null)
                ValidatePool(stagePool, GetStagePoolLabel(stageIndex));
        }

        ShowNotification(new GUIContent(
            _sourceValidationIssues.Count == 0
                ? "Selected Loot Source validation passed."
                : $"Found {_sourceValidationIssues.Count} Loot Source issue(s)."));
    }

    // Purpose: Checks one authored table without treating intentionally absent stage overrides as an error.
    private void ValidatePool(LootStagePool pool, string tableLabel)
    {
        if (pool == null)
        {
            _sourceValidationIssues.Add($"{tableLabel} is missing.");
            return;
        }

        int validItemCount = 0;
        if (pool.rarityTiers == null)
        {
            _sourceValidationIssues.Add($"{tableLabel}: rarity tier list is missing.");
            return;
        }

        for (int tierIndex = 0; tierIndex < pool.rarityTiers.Count; tierIndex++)
        {
            LootRarityTier tier = pool.rarityTiers[tierIndex];
            if (tier == null)
            {
                _sourceValidationIssues.Add($"{tableLabel}: tier {tierIndex + 1} is missing.");
                continue;
            }

            if (tier.items == null)
            {
                _sourceValidationIssues.Add($"{tableLabel} / {tier.rarity}: item list is missing.");
                continue;
            }

            float manualTotal = 0f;
            for (int itemIndex = 0; itemIndex < tier.items.Count; itemIndex++)
            {
                LootItemEntry entry = tier.items[itemIndex];
                if (entry == null || entry.item == null)
                {
                    _sourceValidationIssues.Add($"{tableLabel} / {tier.rarity}: item {itemIndex + 1} is missing.");
                    continue;
                }

                validItemCount++;
                if (entry.maxAmount < entry.minAmount)
                    _sourceValidationIssues.Add($"{tableLabel} / {tier.rarity}: {entry.item.name} has Max Amount below Min Amount.");
                if (entry.useManualShare)
                    manualTotal += Mathf.Max(0f, entry.manualSharePercent);
            }

            if (manualTotal > 100f)
                _sourceValidationIssues.Add($"{tableLabel} / {tier.rarity}: Manual shares total {manualTotal:0.##}%.");
        }

        if (validItemCount == 0)
            _sourceValidationIssues.Add($"{tableLabel} has no valid ItemSO entries.");
    }

    // Purpose: Performs the make unique source id workflow for this component.
    private string MakeUniqueSourceId(string proposedId, LootSourceDefinition self)
    {
        string baseId = MakeObjectId(proposedId);
        string candidate = baseId;
        int suffix = 2;

        while (HasOtherSourceWithId(candidate, self))
            candidate = $"{baseId}_{suffix++}";

        return candidate;
    }

    // Purpose: Checks whether this component has other source with id.
    private bool HasOtherSourceWithId(string sourceId, LootSourceDefinition self)
    {
        if (_config == null)
            return false;

        for (int i = 0; i < _config.Sources.Count; i++)
        {
            LootSourceDefinition source = _config.Sources[i];
            if (source != null && !ReferenceEquals(source, self) &&
                string.Equals(source.sourceId, sourceId, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    // Purpose: Performs the index of source workflow for this component.
    private int IndexOfSource(LootSourceDefinition source)
    {
        if (_config == null || source == null)
            return -1;

        for (int i = 0; i < _config.Sources.Count; i++)
        {
            if (ReferenceEquals(_config.Sources[i], source))
                return i;
        }

        return -1;
    }

    // Purpose: Creates the config owned by this workflow.
    private void CreateConfig()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Loot Progression Config",
            "LootProgressionConfig",
            "asset",
            "Choose a location for the global loot config.");
        if (string.IsNullOrWhiteSpace(path))
            return;

        LootProgressionConfigSO config = CreateInstance<LootProgressionConfigSO>();
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        _config = config;
        Selection.activeObject = config;
        _selectedSourceIndex = -1;
    }

    // Purpose: Finds the requested first config.
    private void FindFirstConfig()
    {
        string[] guids = AssetDatabase.FindAssets("t:LootProgressionConfigSO");
        if (guids.Length == 0)
        {
            ShowNotification(new GUIContent("No LootProgressionConfigSO found. Create one first."));
            return;
        }

        _config = AssetDatabase.LoadAssetAtPath<LootProgressionConfigSO>(
            AssetDatabase.GUIDToAssetPath(guids[0]));
        _selectedSourceIndex = -1;
        Selection.activeObject = _config;
        ShowNotification(new GUIContent(
            guids.Length == 1
                ? "Loaded the shared loot config."
                : $"Found {guids.Length} configs. Loaded the first; choose another with the field if needed."));
    }

    // Purpose: Performs the record config workflow for this component.
    private void RecordConfig(string action)
    {
        Undo.RecordObject(_config, action);
        EditorUtility.SetDirty(_config);
    }

    // Purpose: Performs the contains item workflow for this component.
    private static bool ContainsItem(List<LootItemEntry> entries, ItemSO item)
    {
        if (entries == null)
            return false;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].item == item)
                return true;
        }

        return false;
    }

    // Purpose: Performs the make object id workflow for this component.
    private static string MakeObjectId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "loot_object";

        return value.Trim().ToLowerInvariant().Replace(' ', '_');
    }
}

// The regular Inspector exposes the same occupied-slot contract as Loot Maker.
[CustomEditor(typeof(LootObjectSO))]
public sealed class LootObjectSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var box = (LootObjectSO)target;
        int min = box.MinGeneratedSlots, max = box.MaxGeneratedSlots;
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "_rollSettings", "_slotGenerationVersion",
            "_minGeneratedSlots", "_maxGeneratedSlots", "_rarityWeights", "_lootSlotCount");
        if (serializedObject.ApplyModifiedProperties() && !box.HasSlotGenerationSettings)
        {
            Undo.RecordObject(box, "Keep Box Filled Slots");
            box.SetGeneratedSlotRange(min, max);
            EditorUtility.SetDirty(box);
        }
        LootProgressionEditorWindow.DrawBoxGenerationFields(box);
    }
}
