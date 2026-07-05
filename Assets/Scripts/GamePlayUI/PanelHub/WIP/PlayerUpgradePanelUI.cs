using System;
using TMPro;
using UnityEngine;

public class PlayerUpgradePanelUI : PanelUI
{
    [Header("Refs")]
    [Tooltip("PlayerPanelHub that owns upgrade/shop/stats panel visibility. If empty, this script searches parent.")]
    [SerializeField] private PlayerPanelHub _panelHub;

    [Tooltip("Text field that displays the player's unspent upgrade points.")]
    [SerializeField] private TextMeshProUGUI _pointText;

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
    [SerializeField] private PlayerTooltipUI _tooltipPrefab;

    [Tooltip("Parent where the tooltip prefab will be instantiated. Leave empty to use this panel transform.")]
    [SerializeField] private Transform _tooltipRoot;

    private PlayerTooltipUI _tooltip;
    private Action _closeRequested;

    protected override void Awake()
    {
        base.Awake();
        EnsureRefs();
    }

    private void OnEnable()
    {
        if (_panelReadyChannel != null)
            _panelReadyChannel.RaiseEvent(this);
    }

    private void OnDisable()
    {
        _tooltip?.Hide();

        if (_panelReadyChannel != null)
            _panelReadyChannel.Clear(this);
    }

    public void Bind(
        PlayerUpgradeUISnapshot snapshot,
        Action<PlayerUpgradeTrack, int> nodeClicked)
    {
        if (_pointText != null)
            _pointText.text = $"Points: {snapshot.unspentPoints}";

        _removalColumn?.Bind(snapshot.removal, nodeClicked, GetOrCreateTooltip);
        _occupationColumn?.Bind(snapshot.occupation, nodeClicked, GetOrCreateTooltip);
        _controlColumn?.Bind(snapshot.control, nodeClicked, GetOrCreateTooltip);

        _panelHub?.Bind(snapshot);
    }

    public void SetCloseRequested(Action closeRequested)
    {
        _closeRequested = closeRequested;
    }

    public void OpenInventory()
    {
        EnsureRefs();
        _panelHub?.OpenInventory();
    }

    public void SetVisible(bool visible)
    {
        EnsureRefs();

        if (!visible)
            _tooltip?.Hide();

        if (_panelHub != null)
        {
            SetPanelVisible(true);
            //_panelHub.SetVisible(visible);
            return;
        }

        SetPanelVisible(visible);
    }

    private PlayerTooltipUI GetOrCreateTooltip()
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

    private void EnsureRefs()
    {
        if (_panelHub == null)
            _panelHub = GetComponentInParent<PlayerPanelHub>();

        EnsurePanelGroup();
    }

    private void OnCloseClicked()
    {
        _closeRequested?.Invoke();
    }
}
