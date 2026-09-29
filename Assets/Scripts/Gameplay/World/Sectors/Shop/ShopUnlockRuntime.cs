/*
 * Role hierarchy: Gameplay > World > Sectors > Shop
 * Primary responsibility: Persists Home-shop progression unlock nodes independently of run stock.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ShopUnlockSaveData
{
    public List<string> unlockedNodeIds = new List<string>();
}

[DisallowMultipleComponent]
public sealed class ShopUnlockRuntime : MonoBehaviour
{
    private const string LegacySaveKeyPrefix = "HomeShop.Unlocks.";

    private static ShopUnlockRuntime _instance;
    private readonly HashSet<string> _unlockedNodeIds = new HashSet<string>(StringComparer.Ordinal);
    private ShopConfigSO _config;
    private SaveSystem _saveSystem;

    public static ShopUnlockRuntime Instance => _instance;
    public event Action OnUnlockStateChanged;

    // Purpose: Ensures runtime is ready before the workflow continues.
    public static ShopUnlockRuntime EnsureRuntime()
    {
        if (_instance != null)
            return _instance;

        ShopUnlockRuntime existing = FindFirstObjectByType<ShopUnlockRuntime>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        return new GameObject("ShopUnlockRuntime").AddComponent<ShopUnlockRuntime>();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Purpose: Configures this component''s configure.
    public void Configure(ShopConfigSO config, SaveSystem saveSystem = null)
    {
        SaveSystem resolvedSaveSystem = saveSystem != null ? saveSystem : SaveSystem.Active;
        bool persistenceChanged = _saveSystem != resolvedSaveSystem;
        _saveSystem = resolvedSaveSystem;

        if (_config == config && !persistenceChanged)
        {
            return;
        }

        _config = config;
        Load();
    }

    // Purpose: Checks whether product available is currently true.
    public bool IsProductAvailable(ShopConfigSO config, ShopConfigSO.ProductDefinition product)
    {
        if (config == null || product == null)
            return false;

        Configure(config);

        if (product.AvailableByDefault)
            return true;

        IReadOnlyList<ShopConfigSO.UnlockNode> nodes = config.UnlockNodes;

        for (int i = 0; i < nodes.Count; i++)
        {
            ShopConfigSO.UnlockNode node = nodes[i];

            if (node == null || !_unlockedNodeIds.Contains(node.NodeId))
                continue;

            IReadOnlyList<string> productIds = node.UnlockedProductIds;
            for (int productIndex = 0; productIndex < productIds.Count; productIndex++)
            {
                if (productIds[productIndex] == product.ProductId)
                    return true;
            }
        }

        return false;
    }

    // Purpose: Checks whether node unlocked is currently true.
    public bool IsNodeUnlocked(string nodeId)
    {
        return !string.IsNullOrWhiteSpace(nodeId) && _unlockedNodeIds.Contains(nodeId);
    }

    /// <summary>
    /// Explicitly grants one authored Home-shop unlock node. Quest rewards own
    /// this transition; prerequisite node links only enforce authored order.
    /// </summary>
    public bool TryUnlockNode(
        ShopConfigSO config,
        string nodeId,
        SaveSystem saveSystem = null)
    {
        if (config == null || string.IsNullOrWhiteSpace(nodeId))
            return false;

        Configure(config, saveSystem);

        if (!config.TryGetNode(nodeId, out ShopConfigSO.UnlockNode node))
        {
            Debug.LogError(
                $"[ShopUnlockRuntime] Quest unlock rejected because node does not exist. " +
                $"config={config.ConfigId}, node={nodeId}",
                this);
            return false;
        }

        if (_unlockedNodeIds.Contains(nodeId))
            return true;

        IReadOnlyList<string> prerequisites = node.PrerequisiteNodeIds;
        for (int i = 0; prerequisites != null && i < prerequisites.Count; i++)
        {
            string prerequisiteId = prerequisites[i];
            if (string.IsNullOrWhiteSpace(prerequisiteId) || !_unlockedNodeIds.Contains(prerequisiteId))
            {
                Debug.LogError(
                    $"[ShopUnlockRuntime] Quest unlock rejected because prerequisite '{prerequisiteId}' is not unlocked. " +
                    $"config={config.ConfigId}, node={nodeId}",
                    this);
                return false;
            }
        }

        if (_saveSystem == null || _saveSystem.saveData == null)
        {
            Debug.LogError(
                "[ShopUnlockRuntime] Quest unlock rejected because SaveSystem is missing.",
                this);
            return false;
        }

        _unlockedNodeIds.Add(nodeId);
        Save();
        OnUnlockStateChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Compatibility entry point for old UI callers. Home-shop unlocks are
    /// intentionally explicit QuestReward transactions; no stage, timer, or
    /// quest-polling path can grant products automatically.
    /// </summary>
    public void RefreshAutomaticUnlocks()
    {
        // Intentionally empty. See the XML summary above.
    }

    // Purpose: Loads load into the active runtime state.
    private void Load()
    {
        _unlockedNodeIds.Clear();

        if (_config == null)
            return;

        ShopUnlockProgressSaveData profileData = FindProfileData();
        if (profileData != null)
        {
            AddUnlockedNodeIds(profileData.unlockedNodeIds);
            return;
        }

        // The old shop stored a per-config JSON value in PlayerPrefs. Import it
        // only when this ES3 profile has not yet recorded this configuration.
        string json = PlayerPrefs.GetString(BuildLegacySaveKey(), string.Empty);

        if (string.IsNullOrWhiteSpace(json))
            return;

        ShopUnlockSaveData data = JsonUtility.FromJson<ShopUnlockSaveData>(json);

        if (data?.unlockedNodeIds == null)
            return;

        AddUnlockedNodeIds(data.unlockedNodeIds);

        if (_unlockedNodeIds.Count > 0)
            Save();
    }

    // Purpose: Saves the current save state.
    private void Save()
    {
        if (_config == null || _saveSystem == null)
        {
            Debug.LogWarning(
                "[ShopUnlockRuntime] SaveSystem is missing. Shop unlock changes remain session-only.",
                this);
            return;
        }

        ShopUnlockProgressSaveData profileData = FindProfileData();
        if (profileData == null)
        {
            profileData = new ShopUnlockProgressSaveData
            {
                configId = _config.ConfigId
            };
            _saveSystem.saveData.ShopUnlockProgress.Add(profileData);
        }

        profileData.unlockedNodeIds = new List<string>(_unlockedNodeIds);
        profileData.EnsureDefaults();
        _saveSystem.SaveDataToDisk();
    }

    // Purpose: Finds the requested profile data.
    private ShopUnlockProgressSaveData FindProfileData()
    {
        if (_config == null || _saveSystem == null || _saveSystem.saveData == null)
            return null;

        string configId = _config.ConfigId;
        List<ShopUnlockProgressSaveData> records = _saveSystem.saveData.ShopUnlockProgress;

        for (int i = 0; i < records.Count; i++)
        {
            ShopUnlockProgressSaveData record = records[i];
            if (record != null && string.Equals(record.configId, configId, StringComparison.Ordinal))
                return record;
        }

        return null;
    }

    // Purpose: Adds the requested unlocked node ids to this component.
    private void AddUnlockedNodeIds(IReadOnlyList<string> nodeIds)
    {
        if (nodeIds == null)
            return;

        for (int i = 0; i < nodeIds.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(nodeIds[i]))
                _unlockedNodeIds.Add(nodeIds[i]);
        }
    }

    // Purpose: Builds the legacy save key from the current authored data.
    private string BuildLegacySaveKey()
    {
        return LegacySaveKeyPrefix + (_config != null ? _config.ConfigId : "default");
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}
