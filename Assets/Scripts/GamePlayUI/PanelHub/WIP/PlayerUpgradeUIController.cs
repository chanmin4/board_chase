/*
using UnityEngine;

public class PlayerUpgradeUIController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private UpgradeCatalogSO _catalog;

    [Header("Listening")]
    [SerializeField] private PlayerUpgradeStateReadyEventChannelSO _upgradeStateReadyChannel;
    [SerializeField] private PlayerUpgradePanelReadyEventChannelSO _panelReadyChannel;
    [SerializeField] private SectorShopOpenRequestEventChannelSO _shopOpenRequestChannel;
    [SerializeField] private EnemyLootOpenRequestEventChannelSO _enemyLootOpenRequestChannel;
    [SerializeField] private UIOverlayRequestEventChannelSO _overlayRequestChannel;

    private PlayerUpgradeState _upgradeState;
    private PlayerUpgradePanelUI _panel;
    private bool _isOpen;

    private void OnEnable()
    {
        if (_inputReader != null)
            _inputReader.PlayerMenuEvent += Toggle;

        if (_upgradeStateReadyChannel != null)
        {
            _upgradeStateReadyChannel.OnEventRaised += HandleUpgradeStateReady;

            if (_upgradeStateReadyChannel.Current != null)
                HandleUpgradeStateReady(_upgradeStateReadyChannel.Current);
        }

        if (_panelReadyChannel != null)
        {
            _panelReadyChannel.OnEventRaised += HandlePanelReady;

            if (_panelReadyChannel.Current != null)
                HandlePanelReady(_panelReadyChannel.Current);
        }

        if (_shopOpenRequestChannel != null)
            _shopOpenRequestChannel.OnEventRaised += HandleExternalPanelOpenRequested;

        if (_enemyLootOpenRequestChannel != null)
            _enemyLootOpenRequestChannel.OnEventRaised += HandleExternalPanelOpenRequested;
    }

    private void OnDisable()
    {
        if (_inputReader != null)
            _inputReader.PlayerMenuEvent -= Toggle;

        if (_upgradeStateReadyChannel != null)
            _upgradeStateReadyChannel.OnEventRaised -= HandleUpgradeStateReady;

        if (_panelReadyChannel != null)
            _panelReadyChannel.OnEventRaised -= HandlePanelReady;

        if (_shopOpenRequestChannel != null)
            _shopOpenRequestChannel.OnEventRaised -= HandleExternalPanelOpenRequested;

        if (_enemyLootOpenRequestChannel != null)
            _enemyLootOpenRequestChannel.OnEventRaised -= HandleExternalPanelOpenRequested;

        UnbindUpgradeState();
        UnbindPanel();

    }

    public void Toggle()
    {
        if (_isOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        if (_isOpen || _panel == null)
            return;

        _isOpen = true;

        if (_inputReader != null)
            _inputReader.EnablePlayerMenuInput();

        Refresh();
        _panel.OpenInventory();
        _panel.SetVisible(true);
    }

    public void Close()
    {
        if (!_isOpen)
            return;

        _isOpen = false;

        if (_panel != null)
            _panel.SetVisible(false);

        if (_overlayRequestChannel != null)
            _overlayRequestChannel.Close(UIOverlayId.PlayerPanelHub);

        if (_inputReader != null)
            _inputReader.EnableGameplayInput();
    }

    public void Refresh()
    {
        if (_panel == null)
            return;

        PlayerUpgradeUISnapshot snapshot =
            PlayerUpgradeUIPresenter.Build(_catalog, _upgradeState);

        _panel.Bind(snapshot, OnNodeClicked);
    }

    private void OnNodeClicked(PlayerUpgradeTrack track, int level)
    {
        if (_upgradeState == null || _catalog == null)
            return;

        int expectedLevel = _upgradeState.GetTrackLevel(track) + 1;
        if (level != expectedLevel)
            return;

        if (_upgradeState.TryUpgradeTrack(track, _catalog))
            Refresh();
    }

    private void HandleUpgradeStateReady(PlayerUpgradeState state)
    {
        if (_upgradeState == state)
            return;

        UnbindUpgradeState();

        _upgradeState = state;

        if (_upgradeState != null)
            _upgradeState.OnChanged += Refresh;

        if (_isOpen)
            Refresh();
    }

    private void HandlePanelReady(PlayerUpgradePanelUI panel)
    {
        if (panel == null)
        {
            if (_panel != null)
                _panel.SetCloseRequested(null);

            _panel = null;
            return;
        }

        if (_panel == panel)
            return;

        UnbindPanel();

        _panel = panel;

        _panel.SetCloseRequested(Close);
        _panel.SetVisible(_isOpen);

        if (_isOpen)
            Refresh();
    }

    private void HandleExternalPanelOpenRequested(SectorShopOpenRequest request)
    {
        OpenFromExternalContext();
    }

    private void HandleExternalPanelOpenRequested(EnemyLootOpenRequest request)
    {
        OpenFromExternalContext();
    }

    private void OpenFromExternalContext()
    {
        if (_isOpen || _panel == null)
            return;

        _isOpen = true;

        if (_inputReader != null)
            _inputReader.EnablePlayerMenuInput();

        Refresh();
        _panel.SetVisible(true);
    }

    private void UnbindUpgradeState()
    {
        if (_upgradeState != null)
            _upgradeState.OnChanged -= Refresh;

        _upgradeState = null;
    }

    private void UnbindPanel()
    {
        if (_panel != null)
            _panel.SetCloseRequested(null);

        _panel = null;
    }
}
*/