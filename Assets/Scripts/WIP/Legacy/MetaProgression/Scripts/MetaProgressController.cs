// WIP legacy compatibility only. Active growth uses quest rewards and experience, not this system.
/*
 * Role hierarchy: Gameplay > Progression > Meta
 * Primary responsibility: Coordinates the MetaProgressController subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[AddComponentMenu("")]
[DisallowMultipleComponent]
public class MetaProgressController : MonoBehaviour
{
    [Header("WIP Legacy System")]
    [Tooltip("Retired numerical meta progression. Quest rewards are the active persistent progression path; this component intentionally cannot be re-enabled from the Inspector.")]
    [SerializeField, HideInInspector] private bool _enableWipMetaProgression;

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

    public int RoguelikeCurrency => 0;

    public MetaUpgradeCatalogSO Catalog => _catalog;

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        _enableWipMetaProgression = false;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
    }

    // Purpose: Returns the current level.
    public int GetLevel(MetaUpgradeId id)
    {
        return 0;
    }

    // Purpose: Attempts to purchase and reports whether the request succeeded.
    public bool TryPurchase(MetaUpgradeId id)
    {
        return false;
    }

    // Purpose: Adds the requested roguelike currency to this component.
    public void AddRoguelikeCurrency(int amount)
    {
    }

    // Purpose: Updates the roguelike currency state.
    public void SetRoguelikeCurrency(int amount)
    {
    }

    // Purpose: Creates the snapshot owned by this workflow.
    public MetaProgressSnapshot CreateSnapshot()
    {
        return new MetaProgressSnapshot
        {
            currency = 0,
            upgrades = new MetaUpgradeSnapshot[0]
        };
    }

    // Purpose: Publishes the snapshot notification.
    public void PublishSnapshot()
    {
        if (_metaProgressChangedChannel == null)
            return;

        _metaProgressChangedChannel.RaiseEvent(CreateSnapshot());
    }

    // Purpose: Publishes the changed notification.
    private void PublishChanged()
    {
        OnChanged?.Invoke();
        PublishSnapshot();
    }

    // Purpose: Loads save into the active runtime state.
    private void LoadSave()
    {
        if (_saveSystem == null)
            return;

        _saveSystem.LoadSaveDataFromDisk();
    }

    // Purpose: Ensures save data is ready before the workflow continues.
    private bool EnsureSaveData()
    {
        if (_saveSystem == null || _saveSystem.saveData == null)
            return false;

        _saveSystem.saveData.EnsureRuntimeDefaults();
        return true;
    }

    // Purpose: Saves the current if needed state.
    private void SaveIfNeeded()
    {
        if (!_saveToDiskOnChange || _saveSystem == null)
            return;

        _saveSystem.SaveDataToDisk();
    }
}
