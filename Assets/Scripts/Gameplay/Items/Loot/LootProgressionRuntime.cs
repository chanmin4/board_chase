/*
 * Role hierarchy: Gameplay > Items > Loot
 * Primary responsibility: Provides one scene-level entry point for the authored loot progression config.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LootProgressionRuntime : MonoBehaviour
{
    private static LootProgressionRuntime _instance;

    [SerializeField] private LootProgressionConfigSO _config;
    [Tooltip("Optional runtime owner of the active generated stage. Leave empty to resolve the single SectorStateManager automatically.")]
    [SerializeField] private SectorStateManager _sectorStateManager;
    [SerializeField] private bool _logMissingConfig = true;

    public static LootProgressionRuntime Instance => _instance;
    public LootProgressionConfigSO Config => _config;

    /// <summary>
    /// Returns the generated stage containing the player. Home and any other
    /// external sector deliberately resolve to 0 so their containers use the
    /// default loot table instead of the last run's stage override.
    /// </summary>
    public int CurrentLootStageIndex
    {
        get
        {
            ResolveSectorStateManager();
            if (_sectorStateManager == null || _sectorStateManager.CurrentSector == null)
                return 0;

            IReadOnlyList<SectorStateManager.RuntimeSectorStateEntry> states =
                _sectorStateManager.RuntimeSectorStates;
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].sector == _sectorStateManager.CurrentSector)
                    return Mathf.Max(0, _sectorStateManager.CurrentStage);
            }

            return 0;
        }
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogError(
                "[LootProgressionRuntime] More than one runtime exists. Keep one scene-level runtime.",
                this);
            enabled = false;
            return;
        }

        _instance = this;
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    // Purpose: Attempts to get config and reports whether the request succeeded.
    public bool TryGetConfig(out LootProgressionConfigSO config)
    {
        config = _config;

        if (config != null)
            return true;

        if (_logMissingConfig)
        {
            Debug.LogWarning(
                "[LootProgressionRuntime] Loot Progression Config is missing. No authored loot will roll.",
                this);
        }

        return false;
    }

    // Purpose: Resolves the one persistent sector-state service only when an Inspector reference is absent.
    private void ResolveSectorStateManager()
    {
        if (_sectorStateManager == null)
            _sectorStateManager = FindFirstObjectByType<SectorStateManager>();
    }
}
