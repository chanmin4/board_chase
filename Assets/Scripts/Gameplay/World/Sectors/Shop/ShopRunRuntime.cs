/*
 * Role hierarchy: Gameplay > World > Sectors > Shop
 * Primary responsibility: Owns the non-persistent Home-shop run identity and reset signal.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShopRunRuntime : MonoBehaviour
{
    private static ShopRunRuntime _instance;
    private int _runId;
    private readonly Dictionary<string, PlayerRunShopStateSaveData> _pendingShopStates =
        new(StringComparer.Ordinal);
    private RuntimeContentCatalogSO _restoreCatalog;

    public static ShopRunRuntime Instance => _instance;
    public int RunId => _runId;
    public event Action<int> OnRunReset;

    // Purpose: Ensures runtime is ready before the workflow continues.
    public static ShopRunRuntime EnsureRuntime()
    {
        if (_instance != null)
            return _instance;

        ShopRunRuntime existing = FindFirstObjectByType<ShopRunRuntime>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        return new GameObject("ShopRunRuntime").AddComponent<ShopRunRuntime>();
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

    // Purpose: Performs the reset for run loss workflow for this component.
    public void ResetForRunLoss()
    {
        unchecked
        {
            _runId++;
        }

        _pendingShopStates.Clear();
        _restoreCatalog = null;
        OnRunReset?.Invoke(_runId);
    }

    /// <summary>
    /// Captures all live Home-shop stations into the run-bound profile. Shop
    /// stock resets on death, not on a normal application restart.
    /// </summary>
    public void CaptureRunState(PlayerRunStateSaveData target)
    {
        if (target == null)
            return;

        target.shopRunId = Mathf.Max(0, _runId);
        target.shopStates.Clear();

        SectorShop[] shops = FindObjectsByType<SectorShop>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int index = 0; index < shops.Length; index++)
        {
            PlayerRunShopStateSaveData state = new();
            if (shops[index] != null && shops[index].CaptureRunState(state))
                target.shopStates.Add(state);
        }
    }

    /// <summary>
    /// Makes saved shop state available to current and future Home-shop
    /// stations. A malformed shop snapshot falls back to a deterministic reroll
    /// and never blocks restoration of player possessions.
    /// </summary>
    public void RestoreRunState(PlayerRunStateSaveData source, RuntimeContentCatalogSO catalog)
    {
        _pendingShopStates.Clear();
        _restoreCatalog = catalog;

        if (source == null)
            return;

        _runId = Mathf.Max(0, source.shopRunId);
        for (int index = 0; source.shopStates != null && index < source.shopStates.Count; index++)
        {
            PlayerRunShopStateSaveData state = source.shopStates[index];
            if (state == null || string.IsNullOrWhiteSpace(state.shopStateId))
                continue;

            state.EnsureDefaults();
            _pendingShopStates[state.shopStateId] = state;
        }

        SectorShop[] shops = FindObjectsByType<SectorShop>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int index = 0; index < shops.Length; index++)
            TryApplyPendingState(shops[index]);
    }

    /// <summary>
    /// Gives a newly configured station its matching saved stock state.
    /// </summary>
    public void TryApplyPendingState(SectorShop shop)
    {
        if (shop == null || _restoreCatalog == null ||
            !shop.TryGetRunStateId(out string stateId) ||
            !_pendingShopStates.TryGetValue(stateId, out PlayerRunShopStateSaveData state))
        {
            return;
        }

        _pendingShopStates.Remove(stateId);
        if (!shop.TryRestoreRunState(state, _restoreCatalog, out string message))
        {
            Debug.LogError(
                $"[ShopRunRuntime] Saved shop state was rejected; the station will reroll normally. " +
                $"shop={stateId}, details={message}",
                shop);
        }
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}
