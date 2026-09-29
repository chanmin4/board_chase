/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub
 * Primary responsibility: Implements the focused PlayerPanelHubMode responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum PlayerPanelHubMode
{
    Inventory = 0,
    Shop = 1,
    EnemyLoot = 2,
    CharacterStats = 3,
    FullMap = 4,
    Missions = 5
}

[DisallowMultipleComponent]
public class PlayerPanelHub : PanelUI
{
    private struct RectTransformLayout
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector3 localScale;
    }

    private sealed class PrimaryTabVisual
    {
        public PlayerPanelHubMode mode;
        public Button button;
        public Image background;
        public TextMeshProUGUI label;
    }

    [Header("Primary Pages")]
    [SerializeField] private PlayerInventoryUI _playerInventoryPanel;
    [SerializeField] private CanvasGroup _playerInventoryPanelGroup;
    [SerializeField] private PlayerStatsSummaryPanelUI _statsPanel;
    [SerializeField] private CanvasGroup _statsPanelGroup;
    [SerializeField] private FullMapUI _fullMapPanel;
    [SerializeField] private QuestMissionPanelUI _missionsPanel;
    [SerializeField] private CanvasGroup _missionsPanelGroup;

    [Header("Right Context Panels")]
    [SerializeField] private PlayerShopPanelUI _shopPanel;
    [SerializeField] private CanvasGroup _shopPanelGroup;
    [SerializeField] private EnemyInventoryUI _enemyInventoryPanel;
    [SerializeField] private CanvasGroup _enemyInventoryPanelGroup;

    [Header("Events")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private UIOverlayRequestEventChannelSO _overlayRequestChannel;
    [SerializeField] private SectorShopOpenRequestEventChannelSO _shopOpenRequestChannel;
    [SerializeField] private EnemyLootOpenRequestEventChannelSO _enemyLootOpenRequestChannel;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = true;

    private readonly List<PrimaryTabVisual> _primaryTabVisuals = new();
    private RectTransform _primaryTabBarRoot;
    private RectTransform _shopHubRoot;
    private RectTransform _lootHubRoot;
    private Transform _inventoryHomeParent;
    private int _inventoryHomeSiblingIndex;
    private RectTransformLayout _inventoryHomeLayout;
    private bool _hasInventoryHomeLayout;
    private PlayerItemDetailPanelUI _itemDetailPanel;
    private PlayerPanelHubMode? _activeContextMode;
    private bool _loggedMissingFullMap;

    private static readonly Color TabBarColor = new(0.018f, 0.047f, 0.071f, 1f);
    private static readonly Color TabNormalColor = new(0.055f, 0.13f, 0.17f, 1f);
    private static readonly Color TabSelectedColor = new(0.02f, 0.56f, 0.68f, 1f);
    private static readonly Color TabTextColor = new(0.72f, 0.9f, 0.94f, 1f);
    private static readonly Color TabSelectedTextColor = Color.white;
    // Shop uses the available HUD area as a two-column workspace. Loot instead
    // uses two equal inventory columns with the shared item description between
    // them, so moving items never changes the player's available slot footprint.
    private const float ContextInventoryScale = 0.76f;
    private const float ContextLeftPanelCenterX = -430f;
    private const float ContextShopPanelCenterX = 440f;
    private static readonly Vector2 ContextShopPlayerPanelSize = new(1020f, 1120f);
    private static readonly Vector2 ContextShopPanelSize = new(860f, 850f);
    // Loot uses a dedicated three-column transfer workspace. The two inventories
    // deliberately have identical bounds, while the shared item description owns
    // the narrow middle column: Player 4 : Description 2 : Loot 4.
    private const float LootTransferPlayerPanelCenterX = -556f;
    private const float LootTransferLootPanelCenterX = 556f;
    private static readonly Vector2 LootTransferSidePanelSize = new(720f, 840f);

    public PlayerPanelHubMode CurrentMode { get; private set; } = PlayerPanelHubMode.Inventory;
    public PlayerInventoryUI PlayerInventoryPanel => _playerInventoryPanel;

    // Purpose: Initializes Inspector defaults for this component.
    protected override void Reset()
    {
        base.Reset();
        ResolveRefs();
    }

    // Purpose: Keeps the authoring canvas in the same 4:2:4 Loot workspace
    // used at runtime, without reparenting the Player panel out of its normal
    // PlayerPanelHub home.
    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        ResolveRefs();

        ConfigureLootTransferPlayerPanelRect(
            _playerInventoryPanel != null ? _playerInventoryPanel.transform as RectTransform : null);
        ConfigureContextRightPanelRect(
            _enemyInventoryPanel != null ? _enemyInventoryPanel.transform as RectTransform : null,
            LootTransferSidePanelSize,
            LootTransferLootPanelCenterX);

        _playerInventoryPanel?.ApplyCompactPanelLayout();
        _enemyInventoryPanel?.ApplyContextPresentation();
    }

    // Purpose: Caches the component references required before runtime initialization.
    protected override void Awake()
    {
        base.Awake();
        ResolveRefs();
        CaptureInventoryHome();
        EnsureContextHubs();
        EnsurePrimaryTabBar();

        SetMode(PlayerPanelHubMode.Inventory);
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveRefs();

        if (_shopOpenRequestChannel != null)
            _shopOpenRequestChannel.OnEventRaised += HandleShopOpenRequested;

        if (_enemyLootOpenRequestChannel != null)
            _enemyLootOpenRequestChannel.OnEventRaised += HandleEnemyLootOpenRequested;

        if (_inputReader != null)
        {
            _inputReader.PlayerMenuEvent += HandlePlayerMenuInput;
            _inputReader.MapEvent += HandleMapInput;
        }
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_inputReader != null)
        {
            _inputReader.PlayerMenuEvent -= HandlePlayerMenuInput;
            _inputReader.MapEvent -= HandleMapInput;
        }

        if (_shopOpenRequestChannel != null)
            _shopOpenRequestChannel.OnEventRaised -= HandleShopOpenRequested;

        if (_enemyLootOpenRequestChannel != null)
            _enemyLootOpenRequestChannel.OnEventRaised -= HandleEnemyLootOpenRequested;
    }

    // Purpose: Responds to the overlay shown callback.
    public override void OnOverlayShown()
    {
        if (_activeContextMode.HasValue)
        {
            CloseActiveContextHub();
            RestorePlayerInventoryHome();
            SetCanvasGroup(_shopPanelGroup, false, false);
            SetCanvasGroup(_enemyInventoryPanelGroup, false, false);
            _activeContextMode = null;
        }

        SetItemDetailContextPresentation(false);
        SetHubVisible(true);
        ApplyMode();
    }

    // Purpose: Responds to the overlay hidden callback.
    public override void OnOverlayHidden()
    {
        InventoryDragContext.Clear();
        ItemDetailSelectionBus.Clear();
        SetItemDetailContextPresentation(false);
        CloseChildContextMenus();
        SetHubVisible(false);
        SetMode(PlayerPanelHubMode.Inventory);

        if (_playerInventoryPanel != null && _activeContextMode == null)
            _playerInventoryPanel.SetInteractionMode(PlayerInventoryInteractionMode.Normal);
    }

    // Purpose: Opens the inventory UI or workflow.
    public void OpenInventory()
    {
        SetHubVisible(true);
        SetMode(PlayerPanelHubMode.Inventory);
    }

    // Purpose: Opens the character stats UI or workflow.
    public void OpenCharacterStats()
    {
        SetHubVisible(true);
        SetMode(PlayerPanelHubMode.CharacterStats);
    }

    // Purpose: Opens the full map UI or workflow.
    public void OpenFullMap()
    {
        SetHubVisible(true);
        SetMode(PlayerPanelHubMode.FullMap);
    }

    // Purpose: Opens the missions UI or workflow.
    public void OpenMissions(HomeQuestNpc questGiver = null)
    {
        ResolveRefs();

        QuestRuntime runtime = QuestRuntime.EnsureRuntime();
        if (questGiver != null)
            runtime.SelectQuestGiver(questGiver);

        _missionsPanel?.SetQuestGiver(questGiver);
        _missionsPanel?.Refresh();
        SetHubVisible(true);
        SetMode(PlayerPanelHubMode.Missions);
    }

    // Purpose: Opens the shop UI or workflow.
    public void OpenShop()
    {
        OpenContextHub(PlayerPanelHubMode.Shop);
    }

    // Purpose: Opens the enemy loot UI or workflow.
    public void OpenEnemyLoot()
    {
        OpenContextHub(PlayerPanelHubMode.EnemyLoot);
    }

    // Purpose: Handles the incoming shop open requested request or event.
    private void HandleShopOpenRequested(SectorShopOpenRequest request)
    {
        OpenShop();
    }

    // Purpose: Handles the incoming enemy loot open requested request or event.
    private void HandleEnemyLootOpenRequested(EnemyLootOpenRequest request)
    {
        OpenEnemyLoot();
    }

    // Purpose: Handles the incoming player menu input request or event.
    private void HandlePlayerMenuInput()
    {
        if (_debugLogs)
        {
            Debug.Log(
                $"[PlayerPanelHub] PlayerMenu input received. overlayChannelNull={_overlayRequestChannel == null}, mode={CurrentMode}",
                this);
        }

        HandlePrimaryPageInput(PlayerPanelHubMode.Inventory);
    }

    // Purpose: Handles the incoming map input request or event.
    private void HandleMapInput()
    {
        if (_debugLogs)
        {
            Debug.Log(
                $"[PlayerPanelHub] Map input received. overlayChannelNull={_overlayRequestChannel == null}, mode={CurrentMode}",
                this);
        }

        HandlePrimaryPageInput(PlayerPanelHubMode.FullMap);
    }

    // Purpose: Handles the incoming primary page input request or event.
    private void HandlePrimaryPageInput(PlayerPanelHubMode requestedMode)
    {
        if (_overlayRequestChannel == null)
        {
            Debug.LogWarning("[PlayerPanelHub] Cannot open PlayerPanelHub. Overlay request channel is null.", this);
            return;
        }

        if (_activeContextMode.HasValue)
        {
            CloseActiveContextHub();

            // Inventory is already visible inside Shop/Loot. Pressing the inventory key
            // closes that contextual hub instead of opening another inventory window.
            if (requestedMode == PlayerPanelHubMode.Inventory)
                return;

            SetMode(requestedMode);
            _overlayRequestChannel.Open(UIOverlayId.PlayerPanelHub);
            return;
        }

        if (!IsPrimaryHubOpen())
        {
            SetMode(requestedMode);
            _overlayRequestChannel.Open(UIOverlayId.PlayerPanelHub);
            return;
        }

        if (CurrentMode == requestedMode)
        {
            _overlayRequestChannel.Close(UIOverlayId.PlayerPanelHub);
            return;
        }

        SetMode(requestedMode);
    }

    // Purpose: Updates the mode state.
    private void SetMode(PlayerPanelHubMode mode)
    {
        if (CurrentMode != mode)
            CloseChildContextMenus();

        CurrentMode = mode;
        ApplyMode();
    }

    // Purpose: Applies the requested mode change.
    private void ApplyMode()
    {
        bool inventoryVisible = CurrentMode == PlayerPanelHubMode.Inventory;
        bool statsVisible = CurrentMode == PlayerPanelHubMode.CharacterStats;
        bool fullMapVisible = CurrentMode == PlayerPanelHubMode.FullMap;
        bool missionsVisible = CurrentMode == PlayerPanelHubMode.Missions;
        if (!missionsVisible) _missionsPanel?.EndConversation();

        // Primary pages and contextual Shop/Loot hubs are mutually exclusive.
        // Explicitly hide context children here as well so an overlay close/open
        // in the same input frame cannot leave two inventory layouts visible.
        SetCanvasGroup(_shopPanelGroup, false, false);
        SetCanvasGroup(_enemyInventoryPanelGroup, false, false);
        SetCanvasGroup(_playerInventoryPanelGroup, inventoryVisible, inventoryVisible);
        SetCanvasGroup(_statsPanelGroup, statsVisible, statsVisible);
        SetCanvasGroup(_missionsPanelGroup, missionsVisible, missionsVisible);

        if (_fullMapPanel != null)
        {
            _fullMapPanel.SetOpen(fullMapVisible);
            _loggedMissingFullMap = false;
        }
        else if (fullMapVisible && !_loggedMissingFullMap)
        {
            _loggedMissingFullMap = true;
            Debug.LogError("[PlayerPanelHub] FullMap page is missing below PlayerPanelHub.", this);
        }

        if (_playerInventoryPanel != null)
            _playerInventoryPanel.SetInteractionMode(PlayerInventoryInteractionMode.Normal);

        if (_shopPanel != null)
            _shopPanel.SetSellSelectionSource(null);

        RefreshPrimaryTabVisuals();
    }

    // Purpose: Closes the active child context menus UI or workflow.
    private void CloseChildContextMenus()
    {
        if (_playerInventoryPanel != null)
            _playerInventoryPanel.CloseContextMenu();

        if (_enemyInventoryPanel != null)
            _enemyInventoryPanel.CloseContextMenu();
    }

    // Purpose: Updates the hub visible state.
    private void SetHubVisible(bool visible)
    {
        ResolveRefs();

        if (!visible) _missionsPanel?.EndConversation();

        SetPanelVisible(visible);

        if (!visible)
        {
            if (_fullMapPanel != null)
                _fullMapPanel.SetOpen(false);

            return;
        }

        ApplyMode();
    }

    // Purpose: Performs the capture inventory home workflow for this component.
    private void CaptureInventoryHome()
    {
        if (_playerInventoryPanel == null || _inventoryHomeParent != null)
            return;

        _inventoryHomeParent = _playerInventoryPanel.transform.parent;
        _inventoryHomeSiblingIndex = _playerInventoryPanel.transform.GetSiblingIndex();

        if (_playerInventoryPanel.transform is RectTransform rect)
        {
            _inventoryHomeLayout = CaptureLayout(rect);
            _hasInventoryHomeLayout = true;
        }
    }

    // Purpose: Ensures context hubs is ready before the workflow continues.
    private void EnsureContextHubs()
    {
        Transform center = transform.parent;
        if (center == null)
            return;

        _shopHubRoot = EnsureContextHubRoot(
            center,
            "ShopHub",
            UIOverlayId.ShopHub,
            PlayerPanelHubMode.Shop,
            _shopHubRoot);
        _lootHubRoot = EnsureContextHubRoot(
            center,
            "LootHub",
            UIOverlayId.LootHub,
            PlayerPanelHubMode.EnemyLoot,
            _lootHubRoot);

        MovePanelToHub(_shopPanel != null ? _shopPanel.transform : null, _shopHubRoot);
        MovePanelToHub(_enemyInventoryPanel != null ? _enemyInventoryPanel.transform : null, _lootHubRoot);
        ApplyContextPanelLayout();
        SetCanvasGroup(_shopPanelGroup, false, false);
        SetCanvasGroup(_enemyInventoryPanelGroup, false, false);
    }

    // Purpose: Ensures context hub root is ready before the workflow continues.
    private RectTransform EnsureContextHubRoot(
        Transform center,
        string hubName,
        UIOverlayId overlayId,
        PlayerPanelHubMode mode,
        RectTransform cached)
    {
        if (cached != null)
        {
            ConfigureContextHubRect(cached);
            return cached;
        }

        Transform existing = center.Find(hubName);
        GameObject hubObject;

        if (existing != null)
        {
            hubObject = existing.gameObject;
        }
        else
        {
            hubObject = new GameObject(hubName, typeof(RectTransform));
            hubObject.layer = gameObject.layer;
            hubObject.transform.SetParent(center, false);
        }

        RectTransform rect = hubObject.GetComponent<RectTransform>();
        ConfigureContextHubRect(rect);

        CanvasGroup group = hubObject.GetComponent<CanvasGroup>();
        if (group == null)
            group = hubObject.AddComponent<CanvasGroup>();

        UIOverlayPanel overlay = hubObject.GetComponent<UIOverlayPanel>();
        if (overlay == null)
            overlay = hubObject.AddComponent<UIOverlayPanel>();

        overlay.ConfigureRuntime(overlayId, group, true, true);
        ResolveOverlayManager()?.RegisterPanel(overlay);

        ContextualHubOverlayLifecycle lifecycle =
            hubObject.GetComponent<ContextualHubOverlayLifecycle>();
        if (lifecycle == null)
            lifecycle = hubObject.AddComponent<ContextualHubOverlayLifecycle>();

        lifecycle.Configure(this, mode);
        return rect;
    }

    // Purpose: Configures this component''s context hub rect.
    private void ConfigureContextHubRect(RectTransform rect)
    {
        if (rect == null)
            return;

        RectTransform layoutReference = transform as RectTransform;
        Vector2 referenceSize = layoutReference != null
            ? layoutReference.rect.size
            : new Vector2(1920f, 800f);

        if (referenceSize.x < 1f || referenceSize.y < 1f)
            referenceSize = new Vector2(1920f, 800f);

        // Center is an old 750x750 anchor container. Stretching to it compresses
        // both 650-800px panels into the middle. Match the real PlayerPanelHub
        // layout canvas instead so left/right anchors represent screen sides.
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = referenceSize;
        rect.localScale = Vector3.one;
    }

    // Purpose: Resolves the overlay manager needed by this component.
    private UIOverlayManager ResolveOverlayManager()
    {
        UIOverlayManager overlayManager = GetComponentInParent<UIOverlayManager>();
        if (overlayManager == null)
        {
            overlayManager =
                FindFirstObjectByType<UIOverlayManager>(FindObjectsInactive.Include);
        }

        return overlayManager;
    }

    // Purpose: Moves panel to hub toward its requested destination.
    private static void MovePanelToHub(Transform panel, RectTransform hub)
    {
        if (panel == null || hub == null || panel.parent == hub)
            return;

        panel.SetParent(hub, false);
    }

    // Purpose: Opens the context hub UI or workflow.
    private void OpenContextHub(PlayerPanelHubMode mode)
    {
        ResolveRefs();
        CaptureInventoryHome();
        EnsureContextHubs();

        UIOverlayId targetId = mode == PlayerPanelHubMode.Shop
            ? UIOverlayId.ShopHub
            : UIOverlayId.LootHub;
        UIOverlayId otherId = mode == PlayerPanelHubMode.Shop
            ? UIOverlayId.LootHub
            : UIOverlayId.ShopHub;
        RectTransform targetRoot = mode == PlayerPanelHubMode.Shop
            ? _shopHubRoot
            : _lootHubRoot;

        _overlayRequestChannel?.Close(otherId);
        _overlayRequestChannel?.Close(UIOverlayId.PlayerPanelHub);

        _activeContextMode = mode;
        CurrentMode = mode;

        if (_playerInventoryPanel != null && targetRoot != null)
        {
            _playerInventoryPanel.transform.SetParent(targetRoot, false);
            _playerInventoryPanel.transform.SetAsFirstSibling();
        }

        ApplyContextPanelLayout();
        SetItemDetailContextPresentation(true);

        SetCanvasGroup(_playerInventoryPanelGroup, true, true);
        SetCanvasGroup(_shopPanelGroup, mode == PlayerPanelHubMode.Shop, mode == PlayerPanelHubMode.Shop);
        SetCanvasGroup(
            _enemyInventoryPanelGroup,
            mode == PlayerPanelHubMode.EnemyLoot,
            mode == PlayerPanelHubMode.EnemyLoot);

        if (_playerInventoryPanel != null)
        {
            _playerInventoryPanel.SetInteractionMode(
                mode == PlayerPanelHubMode.Shop
                    ? PlayerInventoryInteractionMode.SellSelect
                    : PlayerInventoryInteractionMode.Normal);
        }

        if (_shopPanel != null)
        {
            _shopPanel.SetSellSelectionSource(
                mode == PlayerPanelHubMode.Shop ? _playerInventoryPanel : null);
        }

        _overlayRequestChannel?.Open(targetId);
    }

    /// <summary>
    /// Context hubs cover the whole screen, so their children must own explicit
    /// left/right anchors. Keeping prefab-local center anchors caused the player
    /// inventory and the Shop/Loot panel to occupy the same screen position after
    /// they were reparented at runtime.
    /// </summary>
    private void ApplyContextPanelLayout()
    {
        if (!_activeContextMode.HasValue)
            return;

        if (_activeContextMode == PlayerPanelHubMode.Shop)
        {
            ConfigureContextPlayerPanelRect(
                _playerInventoryPanel != null ? _playerInventoryPanel.transform as RectTransform : null);
            ConfigureContextRightPanelRect(
                _shopPanel != null ? _shopPanel.transform as RectTransform : null,
                ContextShopPanelSize,
                ContextShopPanelCenterX);
            _shopPanel?.ApplyContextPresentation();
        }
        else if (_activeContextMode == PlayerPanelHubMode.EnemyLoot)
        {
            ConfigureLootTransferPlayerPanelRect(
                _playerInventoryPanel != null ? _playerInventoryPanel.transform as RectTransform : null);
            ConfigureContextRightPanelRect(
                _enemyInventoryPanel != null ? _enemyInventoryPanel.transform as RectTransform : null,
                LootTransferSidePanelSize,
                LootTransferLootPanelCenterX);
            _playerInventoryPanel?.ApplyCompactPanelLayout();
            _enemyInventoryPanel?.ApplyContextPresentation();
        }
    }

    // Purpose: Places the authored player panel in the left half of Shop/Loot.
    private static void ConfigureContextPlayerPanelRect(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(ContextLeftPanelCenterX, 0f);
        rect.sizeDelta = ContextShopPlayerPanelSize;
        rect.localScale = Vector3.one * ContextInventoryScale;
    }

    // Purpose: Places the player inventory in the equal-width left Loot transfer column.
    private static void ConfigureLootTransferPlayerPanelRect(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(LootTransferPlayerPanelCenterX, 0f);
        rect.sizeDelta = LootTransferSidePanelSize;
        rect.localScale = Vector3.one;
    }

    // Purpose: Places the active Shop or Loot surface in its assigned right column.
    private static void ConfigureContextRightPanelRect(RectTransform rect, Vector2 size, float centerX)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(centerX, 0f);
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    // Purpose: Closes the active active context hub UI or workflow.
    private void CloseActiveContextHub()
    {
        if (!_activeContextMode.HasValue)
            return;

        UIOverlayId id = _activeContextMode == PlayerPanelHubMode.Shop
            ? UIOverlayId.ShopHub
            : UIOverlayId.LootHub;
        _overlayRequestChannel?.Close(id);
    }

    // Purpose: Handles the incoming context hub shown request or event.
    public void HandleContextHubShown(PlayerPanelHubMode mode)
    {
        if (_activeContextMode != mode)
            OpenContextHub(mode);
    }

    // Purpose: Handles the incoming context hub hidden request or event.
    public void HandleContextHubHidden(PlayerPanelHubMode mode)
    {
        if (_activeContextMode != mode)
            return;

        InventoryDragContext.Clear();
        ItemDetailSelectionBus.Clear();
        CloseChildContextMenus();
        SetCanvasGroup(_shopPanelGroup, false, false);
        SetCanvasGroup(_enemyInventoryPanelGroup, false, false);

        RestorePlayerInventoryHome();
        SetItemDetailContextPresentation(false);

        if (_shopPanel != null)
            _shopPanel.SetSellSelectionSource(null);

        _activeContextMode = null;
        CurrentMode = PlayerPanelHubMode.Inventory;
    }

    // Purpose: Restores the saved player inventory home state.
    private void RestorePlayerInventoryHome()
    {
        if (_playerInventoryPanel == null)
            return;

        _playerInventoryPanel.SetInteractionMode(PlayerInventoryInteractionMode.Normal);
        if (_inventoryHomeParent != null && _playerInventoryPanel.transform.parent != _inventoryHomeParent)
        {
            _playerInventoryPanel.transform.SetParent(_inventoryHomeParent, false);
            _playerInventoryPanel.transform.SetSiblingIndex(
                Mathf.Clamp(
                    _inventoryHomeSiblingIndex,
                    0,
                    Mathf.Max(0, _inventoryHomeParent.childCount - 1)));
        }

        if (_hasInventoryHomeLayout && _playerInventoryPanel.transform is RectTransform rect)
            RestoreLayout(rect, _inventoryHomeLayout);
    }

    // Purpose: Captures a RectTransform before it is temporarily moved into a context hub.
    private static RectTransformLayout CaptureLayout(RectTransform rect)
    {
        return new RectTransformLayout
        {
            anchorMin = rect.anchorMin,
            anchorMax = rect.anchorMax,
            pivot = rect.pivot,
            anchoredPosition = rect.anchoredPosition,
            sizeDelta = rect.sizeDelta,
            localScale = rect.localScale
        };
    }

    // Purpose: Restores the authored standalone inventory layout after context close.
    private static void RestoreLayout(RectTransform rect, RectTransformLayout layout)
    {
        rect.anchorMin = layout.anchorMin;
        rect.anchorMax = layout.anchorMax;
        rect.pivot = layout.pivot;
        rect.anchoredPosition = layout.anchoredPosition;
        rect.sizeDelta = layout.sizeDelta;
        rect.localScale = layout.localScale;
    }

    // Purpose: Switches the shared item detail panel between standalone and context-overlay layouts.
    private void SetItemDetailContextPresentation(bool isContextOverlay)
    {
        if (_itemDetailPanel == null)
            _itemDetailPanel = FindFirstObjectByType<PlayerItemDetailPanelUI>(FindObjectsInactive.Include);

        bool useLootTransferLayout = isContextOverlay &&
                                     _activeContextMode == PlayerPanelHubMode.EnemyLoot;
        _itemDetailPanel?.SetContextOverlayPresentation(isContextOverlay, useLootTransferLayout);
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_playerInventoryPanel == null)
            _playerInventoryPanel = GetComponentInChildren<PlayerInventoryUI>(true);

        if (_playerInventoryPanelGroup == null && _playerInventoryPanel != null)
            _playerInventoryPanelGroup = GetOrAddCanvasGroup(_playerInventoryPanel.gameObject);

        if (_statsPanel == null)
            _statsPanel = GetComponentInChildren<PlayerStatsSummaryPanelUI>(true);

        if (_statsPanelGroup == null && _statsPanel != null)
            _statsPanelGroup = GetOrAddCanvasGroup(_statsPanel.gameObject);

        if (_fullMapPanel == null)
            _fullMapPanel = GetComponentInChildren<FullMapUI>(true);

        if (_missionsPanel == null)
            _missionsPanel = GetComponentInChildren<QuestMissionPanelUI>(true);

        if (_missionsPanel == null)
            _missionsPanel = QuestMissionPanelUI.CreateRuntime(transform, gameObject.layer);

        if (_missionsPanelGroup == null && _missionsPanel != null)
            _missionsPanelGroup = GetOrAddCanvasGroup(_missionsPanel.gameObject);

        if (_shopPanel == null)
            _shopPanel = GetComponentInChildren<PlayerShopPanelUI>(true);

        if (_shopPanelGroup == null && _shopPanel != null)
            _shopPanelGroup = GetOrAddCanvasGroup(_shopPanel.gameObject);

        if (_enemyInventoryPanel == null)
            _enemyInventoryPanel = GetComponentInChildren<EnemyInventoryUI>(true);

        if (_enemyInventoryPanelGroup == null && _enemyInventoryPanel != null)
            _enemyInventoryPanelGroup = GetOrAddCanvasGroup(_enemyInventoryPanel.gameObject);

        if (_itemDetailPanel == null)
            _itemDetailPanel = FindFirstObjectByType<PlayerItemDetailPanelUI>(FindObjectsInactive.Include);

        if (_inputReader == null || _overlayRequestChannel == null)
        {
            UIOverlayManager overlayManager = GetComponentInParent<UIOverlayManager>();

            if (overlayManager == null)
                overlayManager = FindFirstObjectByType<UIOverlayManager>(FindObjectsInactive.Include);

            if (_inputReader == null && overlayManager != null)
                _inputReader = overlayManager.InputReader;

            if (_overlayRequestChannel == null && overlayManager != null)
                _overlayRequestChannel = overlayManager.RequestChannel;
        }

        if (_inputReader == null)
            _inputReader = FindFirstObjectByType<InputReader>(FindObjectsInactive.Include);
    }

    // Purpose: Checks whether hub visible is currently true.
    private bool IsHubVisible()
    {
        return PanelGroup != null &&
               PanelGroup.alpha > 0.001f &&
               PanelGroup.blocksRaycasts;
    }

    // Purpose: Checks whether primary hub open is currently true.
    private bool IsPrimaryHubOpen()
    {
        UIOverlayManager overlayManager = ResolveOverlayManager();

        // The overlay stack is authoritative. A page switch can change a child
        // CanvasGroup before the hub root updates, so checking only PanelGroup
        // makes the next press re-open an already open Inventory/Map page.
        return overlayManager != null
            ? overlayManager.TopOverlayId == UIOverlayId.PlayerPanelHub
            : IsHubVisible();
    }

    // Purpose: Ensures primary tab bar is ready before the workflow continues.
    private void EnsurePrimaryTabBar()
    {
        if (_primaryTabBarRoot != null)
            return;

        TextMeshProUGUI existingLabel = GetComponentInChildren<TextMeshProUGUI>(true);
        TMP_FontAsset font = existingLabel != null ? existingLabel.font : TMP_Settings.defaultFontAsset;

        GameObject barObject = new(
            "PrimaryTabBar",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(HorizontalLayoutGroup));

        barObject.layer = gameObject.layer;
        barObject.transform.SetParent(transform, false);

        _primaryTabBarRoot = (RectTransform)barObject.transform;
        _primaryTabBarRoot.anchorMin = new Vector2(0.5f, 1f);
        _primaryTabBarRoot.anchorMax = new Vector2(0.5f, 1f);
        _primaryTabBarRoot.pivot = new Vector2(0.5f, 0.5f);
        _primaryTabBarRoot.anchoredPosition = new Vector2(0f, 18f);
        _primaryTabBarRoot.sizeDelta = new Vector2(820f, 50f);

        Image barBackground = barObject.GetComponent<Image>();
        barBackground.color = TabBarColor;
        barBackground.raycastTarget = true;

        HorizontalLayoutGroup layout = barObject.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(7, 7, 5, 5);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        /* Legacy localized labels kept here only for source history. The actual labels are defined below. */
        /*

        CreatePrimaryTab(PlayerPanelHubMode.Inventory, "인벤토리", font);
        CreatePrimaryTab(PlayerPanelHubMode.CharacterStats, "캐릭터 스탯", font);
        CreatePrimaryTab(PlayerPanelHubMode.FullMap, "전체 지도", font);

        */
        CreatePrimaryTab(PlayerPanelHubMode.Inventory, "INVENTORY", font);
        CreatePrimaryTab(PlayerPanelHubMode.CharacterStats, "STATS", font);
        CreatePrimaryTab(PlayerPanelHubMode.FullMap, "MAP", font);
        CreatePrimaryTab(PlayerPanelHubMode.Missions, "MISSIONS", font);

        _primaryTabBarRoot.SetAsLastSibling();
        RefreshPrimaryTabVisuals();
    }

    // Purpose: Creates the primary tab owned by this workflow.
    private void CreatePrimaryTab(PlayerPanelHubMode mode, string labelText, TMP_FontAsset font)
    {
        labelText = mode switch
        {
            PlayerPanelHubMode.Inventory => UIRuntimeLocalization.Get("UI_Inventory", "Inventory_Title", "Inventory"),
            PlayerPanelHubMode.CharacterStats => UIRuntimeLocalization.Get("UI_Inventory", "Inventory_Stats", "Stats"),
            PlayerPanelHubMode.FullMap => UIRuntimeLocalization.Get("UI_Inventory", "Inventory_Map", "Map"),
            PlayerPanelHubMode.Missions => UIRuntimeLocalization.Get("UI_Inventory", "Inventory_Missions", "Missions"),
            _ => labelText
        };

        GameObject tabObject = new(
            $"Tab_{mode}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement));

        tabObject.layer = gameObject.layer;
        tabObject.transform.SetParent(_primaryTabBarRoot, false);

        Image background = tabObject.GetComponent<Image>();
        background.color = TabNormalColor;

        Button button = tabObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;
        button.onClick.AddListener(() => SetMode(mode));

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;

        LayoutElement layoutElement = tabObject.GetComponent<LayoutElement>();
        layoutElement.minWidth = 180f;
        layoutElement.minHeight = 54f;

        GameObject labelObject = new(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));

        labelObject.layer = gameObject.layer;
        labelObject.transform.SetParent(tabObject.transform, false);

        RectTransform labelRect = (RectTransform)labelObject.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = labelText;
        label.fontSize = 26f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = TabTextColor;
        label.raycastTarget = false;
        label.enableWordWrapping = false;

        _primaryTabVisuals.Add(new PrimaryTabVisual
        {
            mode = mode,
            button = button,
            background = background,
            label = label
        });
    }

    // Purpose: Refreshes the displayed or cached primary tab visuals.
    private void RefreshPrimaryTabVisuals()
    {
        PlayerPanelHubMode selectedMode = CurrentMode == PlayerPanelHubMode.Shop ||
                                          CurrentMode == PlayerPanelHubMode.EnemyLoot
            ? PlayerPanelHubMode.Inventory
            : CurrentMode;

        for (int i = 0; i < _primaryTabVisuals.Count; i++)
        {
            PrimaryTabVisual visual = _primaryTabVisuals[i];
            bool selected = visual.mode == selectedMode;

            if (visual.background != null)
                visual.background.color = selected ? TabSelectedColor : TabNormalColor;

            if (visual.label != null)
                visual.label.color = selected ? TabSelectedTextColor : TabTextColor;

            if (visual.button != null)
                visual.button.interactable = !selected;
        }

        if (_primaryTabBarRoot != null)
            _primaryTabBarRoot.SetAsLastSibling();
    }

    // Purpose: Returns the current or add canvas group.
    private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        if (target == null)
            return null;

        CanvasGroup group = target.GetComponent<CanvasGroup>();

        if (group == null)
            group = target.AddComponent<CanvasGroup>();

        return group;
    }

    // Purpose: Updates the canvas group state.
    private static void SetCanvasGroup(CanvasGroup group, bool visible, bool interactive)
    {
        if (group == null)
            return;

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible && interactive;
        group.blocksRaycasts = visible && interactive;
    }
}
