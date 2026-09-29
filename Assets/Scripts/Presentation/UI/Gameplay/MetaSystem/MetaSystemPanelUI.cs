/*
 * Role hierarchy: Presentation > UI > Gameplay > MetaSystem
 * Primary responsibility: Presents MetaSystemPanelUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_backButton != null)
            _backButton.onClick.AddListener(Close);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_backButton != null)
            _backButton.onClick.RemoveListener(Close);
    }

    // Purpose: Opens the meta system screen UI or workflow.
    public void OpenMetaSystemScreen()
    {
        if (_metaProgressController != null)
            _metaProgressController.PublishSnapshot();

        if (_statsPanel != null)
            _statsPanel.RefreshRows();
    }

    // Purpose: Closes the active close UI or workflow.
    public void Close()
    {
        Closed?.Invoke();
    }
}
