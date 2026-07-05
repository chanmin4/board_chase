using UnityEngine;

public enum PlayerPanelHubMode
{
    Inventory = 0,
    Shop = 1,
    EnemyLoot = 2
}

[DisallowMultipleComponent]
public class PlayerPanelHub : PanelUI
{
    [Header("Always Visible While Hub Open")]
    [SerializeField] private PlayerInventoryUI _playerInventoryPanel;
    [SerializeField] private CanvasGroup _playerInventoryPanelGroup;
    [SerializeField] private PlayerStatsSummaryPanelUI _statsPanel;
    [SerializeField] private CanvasGroup _statsPanelGroup;

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

    public PlayerPanelHubMode CurrentMode { get; private set; } = PlayerPanelHubMode.Inventory;
    public PlayerInventoryUI PlayerInventoryPanel => _playerInventoryPanel;

    protected override void Reset()
    {
        base.Reset();
        ResolveRefs();
    }

    protected override void Awake()
    {
        base.Awake();
        ResolveRefs();

        SetMode(PlayerPanelHubMode.Inventory);
    }

    private void OnEnable()
    {
        ResolveRefs();

        if (_shopOpenRequestChannel != null)
            _shopOpenRequestChannel.OnEventRaised += HandleShopOpenRequested;

        if (_enemyLootOpenRequestChannel != null)
            _enemyLootOpenRequestChannel.OnEventRaised += HandleEnemyLootOpenRequested;

        if (_inputReader != null)
            _inputReader.PlayerMenuEvent += HandlePlayerMenuInput;
    }

    private void OnDisable()
    {
        if (_inputReader != null)
            _inputReader.PlayerMenuEvent -= HandlePlayerMenuInput;

        if (_shopOpenRequestChannel != null)
            _shopOpenRequestChannel.OnEventRaised -= HandleShopOpenRequested;

        if (_enemyLootOpenRequestChannel != null)
            _enemyLootOpenRequestChannel.OnEventRaised -= HandleEnemyLootOpenRequested;
    }

    public override void OnOverlayShown()
    {
        SetHubVisible(true);
        ApplyMode();
    }

    public override void OnOverlayHidden()
    {
        SetHubVisible(false);
        SetMode(PlayerPanelHubMode.Inventory);

        if (_playerInventoryPanel != null)
            _playerInventoryPanel.SetInteractionMode(PlayerInventoryInteractionMode.Normal);
    }

    public void OpenInventory()
    {
        SetHubVisible(true);
        SetMode(PlayerPanelHubMode.Inventory);
    }

    public void OpenShop()
    {
        SetHubVisible(true);
        SetMode(PlayerPanelHubMode.Shop);
    }

    public void OpenEnemyLoot()
    {
        SetHubVisible(true);
        SetMode(PlayerPanelHubMode.EnemyLoot);
    }

    public void Bind(PlayerUpgradeUISnapshot snapshot)
    {
        if (_statsPanel != null)
            _statsPanel.Bind(snapshot);
    }

    private void HandleShopOpenRequested(SectorShopOpenRequest request)
    {
        OpenShop();
    }

    private void HandleEnemyLootOpenRequested(EnemyLootOpenRequest request)
    {
        OpenEnemyLoot();
    }

    private void HandlePlayerMenuInput()
    {
        if (_overlayRequestChannel != null)
            _overlayRequestChannel.Toggle(UIOverlayId.PlayerPanelHub);
    }

    private void SetMode(PlayerPanelHubMode mode)
    {
        CurrentMode = mode;
        ApplyMode();
    }

    private void ApplyMode()
    {
        bool shopVisible = CurrentMode == PlayerPanelHubMode.Shop;
        bool enemyLootVisible = CurrentMode == PlayerPanelHubMode.EnemyLoot;

        SetCanvasGroup(_playerInventoryPanelGroup, true, true);
        SetCanvasGroup(_statsPanelGroup, true, true);
        SetCanvasGroup(_shopPanelGroup, shopVisible, shopVisible);
        SetCanvasGroup(_enemyInventoryPanelGroup, enemyLootVisible, enemyLootVisible);

        if (_playerInventoryPanel != null)
        {
            _playerInventoryPanel.SetInteractionMode(
                shopVisible
                    ? PlayerInventoryInteractionMode.SellSelect
                    : PlayerInventoryInteractionMode.Normal);
        }

        if (_shopPanel != null)
            _shopPanel.SetSellSelectionSource(shopVisible ? _playerInventoryPanel : null);
    }

    private void SetHubVisible(bool visible)
    {
        ResolveRefs();

        SetPanelVisible(visible);

        if (!visible)
        {
            SetCanvasGroup(_shopPanelGroup, false, false);
            SetCanvasGroup(_enemyInventoryPanelGroup, false, false);
            return;
        }

        SetCanvasGroup(_playerInventoryPanelGroup, true, true);
        SetCanvasGroup(_statsPanelGroup, true, true);
    }

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

        if (_shopPanel == null)
            _shopPanel = GetComponentInChildren<PlayerShopPanelUI>(true);

        if (_shopPanelGroup == null && _shopPanel != null)
            _shopPanelGroup = GetOrAddCanvasGroup(_shopPanel.gameObject);

        if (_enemyInventoryPanel == null)
            _enemyInventoryPanel = GetComponentInChildren<EnemyInventoryUI>(true);

        if (_enemyInventoryPanelGroup == null && _enemyInventoryPanel != null)
            _enemyInventoryPanelGroup = GetOrAddCanvasGroup(_enemyInventoryPanel.gameObject);

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

    private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        if (target == null)
            return null;

        CanvasGroup group = target.GetComponent<CanvasGroup>();

        if (group == null)
            group = target.AddComponent<CanvasGroup>();

        return group;
    }

    private static void SetCanvasGroup(CanvasGroup group, bool visible, bool interactive)
    {
        if (group == null)
            return;

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible && interactive;
        group.blocksRaycasts = visible && interactive;
    }
}
