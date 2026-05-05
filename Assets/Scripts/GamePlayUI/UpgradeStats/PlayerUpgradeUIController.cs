using UnityEngine;

public class PlayerUpgradeUIController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private UpgradeCatalogSO _catalog;

    [Header("Listening")]
    [SerializeField] private PlayerUpgradeStateReadyEventChannelSO _upgradeStateReadyChannel;
    [SerializeField] private PlayerUpgradePanelReadyEventChannelSO _panelReadyChannel;

    [Header("Options")]
    [SerializeField] private bool _pauseGameWhileOpen = true;

    private PlayerUpgradeState _upgradeState;
    private PlayerUpgradePanelUI _panel;
    private bool _isOpen;
    private float _previousTimeScale = 1f;

    private void OnEnable()
    {
        if (_inputReader != null)
            _inputReader.UpgradeStatsEvent += Toggle;

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
    }

    private void OnDisable()
    {
        if (_inputReader != null)
            _inputReader.UpgradeStatsEvent -= Toggle;

        if (_upgradeStateReadyChannel != null)
            _upgradeStateReadyChannel.OnEventRaised -= HandleUpgradeStateReady;

        if (_panelReadyChannel != null)
            _panelReadyChannel.OnEventRaised -= HandlePanelReady;

        UnbindUpgradeState();
        UnbindPanel();

        if (_isOpen)
            ResumeTime();
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
        if (_isOpen)
            return;

        if (_panel == null)
            return;

        _isOpen = true;

        if (_pauseGameWhileOpen)
            PauseTime();

        Refresh();
        _panel.SetVisible(true);
    }

    public void Close()
    {
        if (!_isOpen)
            return;

        _isOpen = false;

        if (_pauseGameWhileOpen)
            ResumeTime();

        if (_panel != null)
            _panel.SetVisible(false);
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
        if (_panel == panel)
            return;

        UnbindPanel();

        _panel = panel;

        if (_panel != null)
        {
            _panel.SetCloseRequested(Close);
            _panel.SetVisible(_isOpen);

            if (_isOpen)
                Refresh();
        }
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
        {
            _panel.SetCloseRequested(null);
            _panel.SetVisible(false);
        }

        _panel = null;
    }

    private void PauseTime()
    {
        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    private void ResumeTime()
    {
        Time.timeScale = _previousTimeScale;
    }
}
