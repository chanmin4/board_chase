/*
 * Role hierarchy: Gameplay > Items > World
 * Primary responsibility: Manages WorldItemPickupSpawner stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene-level entry point for all floor item drops. One shared ItemPickup prefab
/// is configured here, so ItemSO never owns interaction/collider/pickup prefabs.
/// </summary>
[DisallowMultipleComponent]
public sealed class WorldItemPickupSpawner : MonoBehaviour
{
    private static WorldItemPickupSpawner _instance;

    [Header("Shared Pickup")]
    [Tooltip("The one generic ItemPickup prefab. It must contain WorldItemPickup, a trigger collider, InteractionPromptAnchor, and a VisualRoot child.")]
    [SerializeField] private WorldItemPickup _itemPickupPrefab;

    [Header("Debug")]
    [SerializeField] private bool _logMissingConfiguration = true;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogError(
                "[WorldItemPickupSpawner] More than one spawner is active. Keep exactly one scene-level spawner.",
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

    // Purpose: Attempts to spawn and reports whether the request succeeded.
    public static bool TrySpawn(
        ItemSO item,
        int amount,
        int bulletBundleAmount,
        float bulletSellPriceRate,
        float armorDurability,
        Vector3 position,
        Quaternion rotation,
        out WorldItemPickup pickup)
    {
        return TrySpawn(
            item,
            amount,
            bulletBundleAmount,
            bulletSellPriceRate,
            armorDurability,
            -1f,
            -1f,
            position,
            rotation,
            out pickup);
    }

    // Purpose: Attempts to spawn and reports whether the request succeeded.
    public static bool TrySpawn(
        ItemSO item,
        int amount,
        int bulletBundleAmount,
        float bulletSellPriceRate,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        Vector3 position,
        Quaternion rotation,
        out WorldItemPickup pickup)
    {
        return TrySpawn(
            item,
            amount,
            bulletBundleAmount,
            bulletSellPriceRate,
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            -1f,
            position,
            rotation,
            out pickup);
    }

    // Purpose: Spawns an item while preserving armor, weapon, or gas-mask condition data.
    public static bool TrySpawn(
        ItemSO item,
        int amount,
        int bulletBundleAmount,
        float bulletSellPriceRate,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        float gasMaskFilterDurability,
        Vector3 position,
        Quaternion rotation,
        out WorldItemPickup pickup)
    {
        return TrySpawn(
            item,
            amount,
            bulletBundleAmount,
            bulletSellPriceRate,
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            gasMaskFilterDurability,
            -1f,
            -1f,
            position,
            rotation,
            out pickup);
    }

    // Purpose: Spawns an item while retaining all repairable-instance condition values.
    public static bool TrySpawn(
        ItemSO item,
        int amount,
        int bulletBundleAmount,
        float bulletSellPriceRate,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        float gasMaskFilterDurability,
        float armorMaxDurability,
        float gasMaskMaxFilterDurability,
        Vector3 position,
        Quaternion rotation,
        out WorldItemPickup pickup)
    {
        pickup = null;

        WorldItemPickupSpawner spawner = ResolveActiveSpawner();

        if (spawner == null)
        {
            Debug.LogError(
                "[WorldItemPickupSpawner] No active spawner. Add it to the scene SpawnSystem and assign ItemPickup.prefab.");
            return false;
        }

        return spawner.TrySpawnInternal(
            item,
            amount,
            bulletBundleAmount,
            bulletSellPriceRate,
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            gasMaskFilterDurability,
            armorMaxDurability,
            gasMaskMaxFilterDurability,
            position,
            rotation,
            out pickup);
    }

    /// <summary>
    /// Spawns one worn backpack together with the exact cargo that was in its
    /// bag-only slots. The shared pickup wrapper still owns interaction and
    /// visual spawning; this payload only preserves inventory state.
    /// </summary>
    public static bool TrySpawnBackpack(
        BackpackItemSO backpack,
        IReadOnlyList<PlayerInventoryItemStack> cargo,
        Vector3 position,
        Quaternion rotation,
        out WorldItemPickup pickup)
    {
        pickup = null;

        if (backpack == null)
        {
            Debug.LogError("[WorldItemPickupSpawner] Cannot spawn a null backpack.");
            return false;
        }

        if (!TrySpawn(
                backpack,
                1,
                1,
                0f,
                -1f,
                -1f,
                -1f,
                position,
                rotation,
                out pickup) || pickup == null)
        {
            return false;
        }

        BackpackWorldPickupPayload payload =
            pickup.GetComponent<BackpackWorldPickupPayload>() ??
            pickup.gameObject.AddComponent<BackpackWorldPickupPayload>();
        payload.Initialize(backpack, cargo);
        return true;
    }

    // The gameplay location is loaded additively. If Unity recompiles scripts
    // while it is already open, the static reference may be cleared even
    // though the configured SpawnSystem component still exists in the scene.
    // Recover that one active scene service before rejecting a valid drop.
    // Purpose: Resolves the active spawner needed by this component.
    private static WorldItemPickupSpawner ResolveActiveSpawner()
    {
        if (_instance != null && _instance.isActiveAndEnabled)
            return _instance;

        _instance = FindFirstObjectByType<WorldItemPickupSpawner>(FindObjectsInactive.Exclude);
        return _instance != null && _instance.isActiveAndEnabled
            ? _instance
            : null;
    }

    // Purpose: Attempts to spawn internal and reports whether the request succeeded.
    private bool TrySpawnInternal(
        ItemSO item,
        int amount,
        int bulletBundleAmount,
        float bulletSellPriceRate,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        float gasMaskFilterDurability,
        float armorMaxDurability,
        float gasMaskMaxFilterDurability,
        Vector3 position,
        Quaternion rotation,
        out WorldItemPickup pickup)
    {
        pickup = null;

        if (item == null)
        {
            LogMissingConfiguration("Cannot spawn a null item.");
            return false;
        }

        if (_itemPickupPrefab == null)
        {
            LogMissingConfiguration("ItemPickup prefab is missing.");
            return false;
        }

        Transform fieldItemRoot = ResolveFieldItemRoot(position);
        pickup = Instantiate(_itemPickupPrefab, position, rotation, fieldItemRoot);
        pickup.Initialize(
            item,
            Mathf.Max(1, amount),
            Mathf.Max(1, bulletBundleAmount),
            Mathf.Clamp01(bulletSellPriceRate),
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            gasMaskFilterDurability,
            armorMaxDurability,
            gasMaskMaxFilterDurability);

        return true;
    }

    // A floor drop belongs to the field where it was created, not to a global
    // SpawnSystem transform. This lets each SectorRuntime (including HomeSector)
    // own and clean up only its own WorldItemContainer children.
    // Purpose: Resolves the field item root needed by this component.
    private Transform ResolveFieldItemRoot(Vector3 position)
    {
        SectorRuntime[] sectors = FindObjectsByType<SectorRuntime>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        SectorRuntime closestSector = null;
        float closestDistanceSqr = float.PositiveInfinity;

        for (int i = 0; i < sectors.Length; i++)
        {
            SectorRuntime sector = sectors[i];

            if (sector == null)
                continue;

            Bounds bounds = sector.GetWorldBounds();

            if (bounds.Contains(position))
                return sector.WorldItemContainer;

            float distanceSqr = bounds.SqrDistance(position);

            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestSector = sector;
            }
        }

        // A spawn just outside its field bounds should still be grouped with
        // the nearest field instead of returning to the global scene root.
        return closestSector != null
            ? closestSector.WorldItemContainer
            : null;
    }

    // Purpose: Performs the log missing configuration workflow for this component.
    private void LogMissingConfiguration(string message)
    {
        if (_logMissingConfiguration)
            Debug.LogError($"[WorldItemPickupSpawner] {message}", this);
    }
}
