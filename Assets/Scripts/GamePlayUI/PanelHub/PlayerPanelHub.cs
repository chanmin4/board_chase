using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerPanelHub : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("CanvasGroup on PlayerPanelHub. This hides/shows upgrade, shop, stats, and toggle buttons together.")]
    [SerializeField] private CanvasGroup _hubGroup;

    [Header("Panels")]
    [SerializeField] private PlayerUpgradePanelUI _upgradePanel;

    [Tooltip("CanvasGroup on ShopPanel.")]
    [SerializeField] private CanvasGroup _shopPanelGroup;

    [SerializeField] private PlayerShopPanelUI _shopPanel;

    [Tooltip("CanvasGroup on StatsPanel.")]
    [SerializeField] private CanvasGroup _statsPanelGroup;

    [SerializeField] private PlayerStatsSummaryPanelUI _statsPanel;

    [Header("Shop Toggle")]
    [SerializeField] private Button _shopToggleButton;
    [SerializeField] private Image _shopToggleImage;
    [SerializeField] private Sprite _shopCollapsedSprite;
    [SerializeField] private Sprite _shopExpandedSprite;

    [Header("Stats Toggle")]
    [SerializeField] private Button _statsToggleButton;
    [SerializeField] private Image _statsToggleImage;
    [SerializeField] private Sprite _statsCollapsedSprite;
    [SerializeField] private Sprite _statsExpandedSprite;

    [Header("Default")]
    [SerializeField] private bool _hideOnAwake = true;
    [SerializeField] private bool _resetFoldStateOnAwake = true;
    [SerializeField] private bool _defaultShopExpanded;
    [SerializeField] private bool _defaultStatsExpanded;

    [Header("Raycast Safety")]
    [Tooltip("Keeps toggle buttons above PlayerUpgradePanel so panel images cannot block toggle clicks.")]
    [SerializeField] private bool _forceToggleButtonsToFront = true;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs;

    public bool IsShopExpanded { get; private set; }
    public bool IsStatsExpanded { get; private set; }

    private void Reset()
    {
        _hubGroup = GetComponent<CanvasGroup>();
        _upgradePanel = GetComponentInChildren<PlayerUpgradePanelUI>(true);
        _shopPanel = GetComponentInChildren<PlayerShopPanelUI>(true);
        _statsPanel = GetComponentInChildren<PlayerStatsSummaryPanelUI>(true);

        if (_shopPanel != null)
            _shopPanelGroup = _shopPanel.GetComponent<CanvasGroup>();

        if (_statsPanel != null)
            _statsPanelGroup = _statsPanel.GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        EnsureRefs();
        BringToggleButtonsToFront();

        if (_resetFoldStateOnAwake)
            ResetFoldState();
        else
            ApplyFoldState();

        if (_hideOnAwake)
            SetVisible(false);
    }

    private void OnEnable()
    {
        EnsureRefs();
        BringToggleButtonsToFront();

        if (_shopToggleButton != null)
            _shopToggleButton.onClick.AddListener(ToggleShop);

        if (_statsToggleButton != null)
            _statsToggleButton.onClick.AddListener(ToggleStats);

        ApplyFoldState();
    }

    private void OnDisable()
    {
        if (_shopToggleButton != null)
            _shopToggleButton.onClick.RemoveListener(ToggleShop);

        if (_statsToggleButton != null)
            _statsToggleButton.onClick.RemoveListener(ToggleStats);
    }

    public void SetVisible(bool visible)
    {
        EnsureRefs();
        BringToggleButtonsToFront();

        SetCanvasGroup(_hubGroup, visible, visible);

        if (visible)
            ApplyFoldState();
    }

    public void Bind(PlayerUpgradeUISnapshot snapshot)
    {
        _statsPanel?.Bind(snapshot);
        _shopPanel?.BindPlaceholder();
    }

    public void ResetFoldState()
    {
        IsShopExpanded = _defaultShopExpanded;
        IsStatsExpanded = _defaultStatsExpanded;
        ApplyFoldState();
    }

    public void ToggleShop()
    {
        SetShopExpanded(!IsShopExpanded);

        if (_debugLogs)
            Debug.Log($"[PlayerPanelHub] Toggle Shop. expanded={IsShopExpanded}", this);
    }

    public void ToggleStats()
    {
        SetStatsExpanded(!IsStatsExpanded);

        if (_debugLogs)
            Debug.Log($"[PlayerPanelHub] Toggle Stats. expanded={IsStatsExpanded}", this);
    }

    public void SetShopExpanded(bool expanded)
    {
        IsShopExpanded = expanded;
        ApplyShopFoldState();
    }

    public void SetStatsExpanded(bool expanded)
    {
        IsStatsExpanded = expanded;
        ApplyStatsFoldState();
    }

    private void ApplyFoldState()
    {
        ApplyShopFoldState();
        ApplyStatsFoldState();
        BringToggleButtonsToFront();
    }

    private void ApplyShopFoldState()
    {
        SetCanvasGroup(_shopPanelGroup, IsShopExpanded, IsShopExpanded);
        SetToggleSprite(_shopToggleImage, IsShopExpanded, _shopCollapsedSprite, _shopExpandedSprite);
    }

    private void ApplyStatsFoldState()
    {
        SetCanvasGroup(_statsPanelGroup, IsStatsExpanded, IsStatsExpanded);
        SetToggleSprite(_statsToggleImage, IsStatsExpanded, _statsCollapsedSprite, _statsExpandedSprite);
    }

    private void BringToggleButtonsToFront()
    {
        if (!_forceToggleButtonsToFront)
            return;

        if (_shopToggleButton != null)
            _shopToggleButton.transform.SetAsLastSibling();

        if (_statsToggleButton != null)
            _statsToggleButton.transform.SetAsLastSibling();
    }

    private void EnsureRefs()
    {
        if (_hubGroup == null)
            _hubGroup = GetComponent<CanvasGroup>();

        if (_hubGroup == null)
            _hubGroup = gameObject.AddComponent<CanvasGroup>();

        if (_upgradePanel == null)
            _upgradePanel = GetComponentInChildren<PlayerUpgradePanelUI>(true);

        if (_shopPanel == null)
            _shopPanel = GetComponentInChildren<PlayerShopPanelUI>(true);

        if (_statsPanel == null)
            _statsPanel = GetComponentInChildren<PlayerStatsSummaryPanelUI>(true);

        if (_shopPanelGroup == null && _shopPanel != null)
            _shopPanelGroup = GetOrAddCanvasGroup(_shopPanel.gameObject);

        if (_statsPanelGroup == null && _statsPanel != null)
            _statsPanelGroup = GetOrAddCanvasGroup(_statsPanel.gameObject);
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

    private static void SetToggleSprite(
        Image image,
        bool expanded,
        Sprite collapsedSprite,
        Sprite expandedSprite)
    {
        if (image == null)
            return;

        Sprite sprite = expanded ? expandedSprite : collapsedSprite;

        if (sprite != null)
            image.sprite = sprite;
    }
}