using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MetaSystemPanelUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _backButton;

    [Header("Refs")]
    [SerializeField] private MetaProgressController _metaProgressController;
    [SerializeField] private MetaUpgradePanelUI _upgradePanel;
    [SerializeField] private MetaStatsPanelUI _statsPanel;

    public event Action Closed;

    private void OnEnable()
    {
        if (_backButton != null)
            _backButton.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        if (_backButton != null)
            _backButton.onClick.RemoveListener(Close);
    }

    public void OpenMetaSystemScreen()
    {
        if (_metaProgressController != null)
            _metaProgressController.PublishSnapshot();

        if (_statsPanel != null)
            _statsPanel.RefreshRows();
    }

    public void Close()
    {
        Closed?.Invoke();
    }
}
