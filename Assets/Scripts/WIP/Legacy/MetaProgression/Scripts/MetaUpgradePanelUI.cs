// WIP legacy compatibility only. Active growth uses quest rewards and experience, not this system.
/*
 * Role hierarchy: Presentation > UI > Gameplay > MetaSystem
 * Primary responsibility: Presents MetaUpgradePanelUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("")]
[DisallowMultipleComponent]
public class MetaUpgradePanelUI : PanelUI
{
    [Header("Panel")]
    [SerializeField] private Button _backButton;

    [Header("Refs")]
    [SerializeField] private MetaProgressController _runtime;
    [SerializeField] private Transform _rowRoot;
    [SerializeField] private MetaUpgradeRowUI _rowPrefab;
    [SerializeField] private TextMeshProUGUI _currencyText;

    [Header("Events")]
    [SerializeField] private MetaProgressChangedEventChannelSO _progressChangedChannel;

    [Header("Format")]
    [SerializeField] private string _currencyFormat = "{0}";

    private readonly List<MetaUpgradeRowUI> _rows = new();

    public event Action Closed;

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        enabled=false;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_backButton != null)
            _backButton.onClick.RemoveListener(HandleBackClicked);

        if (_progressChangedChannel != null)
            _progressChangedChannel.OnEventRaised -= HandleProgressChanged;
    }

    // Purpose: Opens the panel UI or workflow.
    public void OpenPanel()
    {
        HidePanel(); // WIP entry cannot open a retired purchasing panel.
    }

    // Purpose: Closes the active panel UI or workflow.
    public void ClosePanel()
    {
        HidePanel();
    }

    // Purpose: Handles the incoming back clicked request or event.
    private void HandleBackClicked()
    {
        Closed?.Invoke();
    }

    // Purpose: Handles the incoming progress changed request or event.
    private void HandleProgressChanged(MetaProgressSnapshot snapshot)
    {
        if (_currencyText != null)
            _currencyText.text = string.Format(_currencyFormat, snapshot.currency);

        MetaUpgradeSnapshot[] upgrades = snapshot.upgrades;
        int count = upgrades != null ? upgrades.Length : 0;

        EnsureRowCount(count);

        for (int i = 0; i < _rows.Count; i++)
        {
            bool active = i < count;

            if (_rows[i] == null)
                continue;

            _rows[i].gameObject.SetActive(active);

            if (active)
                _rows[i].Bind(upgrades[i], _runtime);
        }
    }

    // Purpose: Ensures row count is ready before the workflow continues.
    private void EnsureRowCount(int count)
    {
        if (_rowRoot == null || _rowPrefab == null)
            return;

        while (_rows.Count < count)
        {
            MetaUpgradeRowUI row = Instantiate(_rowPrefab, _rowRoot);
            _rows.Add(row);
        }
    }

}
