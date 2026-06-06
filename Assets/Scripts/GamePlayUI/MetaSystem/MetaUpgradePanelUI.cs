using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MetaUpgradePanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private CanvasGroup _panelGroup;
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

    private void OnEnable()
    {
        if (_backButton != null)
            _backButton.onClick.AddListener(HandleBackClicked);

        if (_progressChangedChannel != null)
        {
            _progressChangedChannel.OnEventRaised += HandleProgressChanged;

            if (_progressChangedChannel.HasCurrent)
                HandleProgressChanged(_progressChangedChannel.Current);
        }

        if (_runtime != null)
            _runtime.PublishSnapshot();
    }

    private void OnDisable()
    {
        if (_backButton != null)
            _backButton.onClick.RemoveListener(HandleBackClicked);

        if (_progressChangedChannel != null)
            _progressChangedChannel.OnEventRaised -= HandleProgressChanged;
    }

    public void OpenPanel()
    {
        SetVisible(_panelGroup, true);

        if (_runtime != null)
            _runtime.PublishSnapshot();
    }

    public void ClosePanel()
    {
        SetVisible(_panelGroup, false);
    }

    private void HandleBackClicked()
    {
        Closed?.Invoke();
    }

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

    private static void SetVisible(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }
}