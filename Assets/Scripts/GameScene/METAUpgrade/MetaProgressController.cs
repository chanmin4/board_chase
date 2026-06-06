using System;
using UnityEngine;

[DisallowMultipleComponent]
public class MetaProgressController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SaveSystem _saveSystem;
    [SerializeField] private MetaUpgradeCatalogSO _catalog;

    [Header("Events")]
    [SerializeField] private MetaProgressChangedEventChannelSO _metaProgressChangedChannel;
    [SerializeField] private VoidEventChannelSO _metaProgressRefreshRequestedChannel;
    // ui 쪽에서 현재메타진행도 요청
    [Header("Save")]
    [SerializeField] private bool _loadSaveOnEnable = true;
    [SerializeField] private bool _saveToDiskOnChange = true;

    public event Action OnChanged;

    public int RoguelikeCurrency =>
        _saveSystem != null && _saveSystem.saveData != null
            ? Mathf.Max(0, _saveSystem.saveData._roguelikeCurrency)
            : 0;

    public MetaUpgradeCatalogSO Catalog => _catalog;

    private void OnEnable()
    {
        if (_metaProgressRefreshRequestedChannel != null)
            _metaProgressRefreshRequestedChannel.OnEventRaised += PublishSnapshot;

        if (_loadSaveOnEnable)
            LoadSave();

        EnsureSaveData();
        PublishSnapshot();
    }

    private void OnDisable()
    {
        if (_metaProgressRefreshRequestedChannel != null)
            _metaProgressRefreshRequestedChannel.OnEventRaised -= PublishSnapshot;
    }

    public int GetLevel(MetaUpgradeId id)
    {
        if (!EnsureSaveData())
            return 0;

        return _saveSystem.saveData.MetaUpgrades.GetLevel(id);
    }

    public bool TryPurchase(MetaUpgradeId id)
    {
        if (!EnsureSaveData() || _catalog == null)
            return false;

        if (!_catalog.TryGetUpgrade(id, out MetaUpgradeDefinition upgrade))
            return false;

        MetaUpgradeSaveData metaUpgrades = _saveSystem.saveData.MetaUpgrades;
        int currentLevel = Mathf.Clamp(metaUpgrades.GetLevel(id), 0, upgrade.MaxLevel);

        if (currentLevel >= upgrade.MaxLevel)
            return false;

        int cost = upgrade.GetCostForNextLevel(currentLevel);

        if (_saveSystem.saveData._roguelikeCurrency < cost)
            return false;

        _saveSystem.saveData._roguelikeCurrency -= cost;
        metaUpgrades.SetLevel(id, currentLevel + 1);

        SaveIfNeeded();
        PublishChanged();
        return true;
    }

    public void AddRoguelikeCurrency(int amount)
    {
        if (amount <= 0 || !EnsureSaveData())
            return;

        _saveSystem.saveData._roguelikeCurrency += amount;

        SaveIfNeeded();
        PublishChanged();
    }

    public void SetRoguelikeCurrency(int amount)
    {
        if (!EnsureSaveData())
            return;

        _saveSystem.saveData._roguelikeCurrency = Mathf.Max(0, amount);

        SaveIfNeeded();
        PublishChanged();
    }

    public MetaProgressSnapshot CreateSnapshot()
    {
        if (!EnsureSaveData() || _catalog == null)
        {
            return new MetaProgressSnapshot
            {
                currency = RoguelikeCurrency,
                upgrades = new MetaUpgradeSnapshot[0]
            };
        }

        return _catalog.BuildSnapshot(
            _saveSystem.saveData.MetaUpgrades,
            _saveSystem.saveData._roguelikeCurrency);
    }

    public void PublishSnapshot()
    {
        if (_metaProgressChangedChannel == null)
            return;

        _metaProgressChangedChannel.RaiseEvent(CreateSnapshot());
    }

    private void PublishChanged()
    {
        OnChanged?.Invoke();
        PublishSnapshot();
    }

    private void LoadSave()
    {
        if (_saveSystem == null)
            return;

        _saveSystem.LoadSaveDataFromDisk();
    }

    private bool EnsureSaveData()
    {
        if (_saveSystem == null || _saveSystem.saveData == null)
            return false;

        _saveSystem.saveData.EnsureRuntimeDefaults();
        return true;
    }

    private void SaveIfNeeded()
    {
        if (!_saveToDiskOnChange || _saveSystem == null)
            return;

        _saveSystem.SaveDataToDisk();
    }
}
