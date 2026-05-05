using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUpgradePanelUI : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("CanvasGroup on the upgrade panel root. If missing, this script will add one automatically.")]
    [SerializeField] private CanvasGroup _canvasGroup;

    [Tooltip("Text field that displays the player's unspent upgrade points.")]
    [SerializeField] private TextMeshProUGUI _pointText;

    [Tooltip("Button used to close the upgrade panel.")]
    [SerializeField] private Button _closeButton;


    [Header("Track Columns")]
    [Tooltip("Column UI used for the Removal upgrade track.")]
    [SerializeField] private PlayerUpgradeTrackColumnUI _removalColumn;

    [Tooltip("Column UI used for the Occupation upgrade track.")]
    [SerializeField] private PlayerUpgradeTrackColumnUI _occupationColumn;

    [Tooltip("Column UI used for the Control and Survival upgrade track.")]
    [SerializeField] private PlayerUpgradeTrackColumnUI _controlColumn;
    [Header("Broadcasting")]
    [Tooltip("Broadcasts this panel instance when the additive UI scene is loaded.")]
    [SerializeField] private PlayerUpgradePanelReadyEventChannelSO _panelReadyChannel;

    [Header("Tooltip")]
    [Tooltip("Speech bubble prefab used for upgrade node hover details. It is created lazily on first hover.")]
    [SerializeField] private PlayerUpgradeTooltipUI _tooltipPrefab;

    [Tooltip("Parent where the tooltip prefab will be instantiated. Leave empty to use this panel transform.")]
    [SerializeField] private Transform _tooltipRoot;
    [Header("Options")]
    [Tooltip("Hides this panel on scene start using CanvasGroup, while keeping the GameObject active for event broadcasting.")]
    [SerializeField] private bool _hideOnAwake = true;

    private PlayerUpgradeTooltipUI _tooltip;
    private Action _closeRequested;

    private void Awake()
    {
        EnsureCanvasGroup();

        if (_hideOnAwake)
            SetVisible(false);
    }

    private void OnEnable()
    {
        if (_closeButton != null)
            _closeButton.onClick.AddListener(OnCloseClicked);

        if (_panelReadyChannel != null)
            _panelReadyChannel.RaiseEvent(this);
    }

    private void OnDisable()
    {
        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(OnCloseClicked);

        _tooltip?.Hide();

        if (_panelReadyChannel != null)
            _panelReadyChannel.Clear(this);
    }

    public void Bind(
        PlayerUpgradeUISnapshot snapshot,
        Action<PlayerUpgradeTrack, int> nodeClicked)
    {
        if (_pointText != null)
            _pointText.text = $"보유 포인트 : {snapshot.unspentPoints}pt";

        _removalColumn?.Bind(snapshot.removal, nodeClicked, GetOrCreateTooltip);
        _occupationColumn?.Bind(snapshot.occupation, nodeClicked, GetOrCreateTooltip);
        _controlColumn?.Bind(snapshot.control, nodeClicked, GetOrCreateTooltip);
    }

    public void SetCloseRequested(Action closeRequested)
    {
        _closeRequested = closeRequested;
    }

    public void SetVisible(bool visible)
    {
        EnsureCanvasGroup();

        if (!visible)
            _tooltip?.Hide();

        gameObject.SetActive(true);
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    private PlayerUpgradeTooltipUI GetOrCreateTooltip()
    {
        if (_tooltip != null)
            return _tooltip;

        if (_tooltipPrefab == null)
            return null;

        Transform root = _tooltipRoot != null ? _tooltipRoot : transform;
        _tooltip = Instantiate(_tooltipPrefab, root);
        _tooltip.Hide();

        return _tooltip;
    }

    private void EnsureCanvasGroup()
    {
        if (_canvasGroup != null)
            return;

        _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnCloseClicked()
    {
        _closeRequested?.Invoke();
    }
}
