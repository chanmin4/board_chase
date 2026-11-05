/*
 * Role hierarchy: Gameplay > Items > Loot
 * Primary responsibility: Rolls authored enemy loot into the existing corpse LootHub when the enemy dies.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyLootDropOnDeath : MonoBehaviour
{
    [Header("Optional Override")]
    [Tooltip("Normally resolved from the scene LootProgressionRuntime.")]
    [SerializeField] private LootProgressionConfigSO _configOverride;
    [SerializeField] private EnemyShooterConfigSO _enemyConfigOverride;

    [Header("Fallback World Drop")]
    [Tooltip("If the corpse LootHub is full, the rolled item is spawned here instead. Leave empty to use the enemy position.")]
    [SerializeField] private Transform _worldDropOrigin;
    [SerializeField] private bool _logDropResults;

    private readonly List<LootRollResult> _rollResults = new();
    private Damageable _damageable;
    private EnemyLootInventoryRuntime _lootInventory;
    private EnemyShooter _enemyShooter;
    private EnemyShooterConfigSO _configuredEnemy;
    private bool _subscribed;
    private bool _rolled;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
        Subscribe();
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        Unsubscribe();
    }

    // Purpose: Configures this component''s configure.
    public void Configure(EnemyShooterConfigSO enemyConfig)
    {
        _configuredEnemy = enemyConfig;
        ResolveRefs();
        Subscribe();
        RefreshLegacyDropSuppression();
    }

    // Purpose: Handles the incoming death request or event.
    private void HandleDeath()
    {
        if (_rolled)
            return;

        _rolled = true;
        ResolveRefs();

        LootProgressionRuntime lootRuntime = LootProgressionRuntime.Instance;
        LootProgressionConfigSO config = _configOverride;
        if (config == null && lootRuntime != null)
            config = lootRuntime.Config;

        EnemyShooterConfigSO enemyConfig = _enemyConfigOverride ??
                                           _configuredEnemy ??
                                           (_enemyShooter != null ? _enemyShooter.Config : null);

        if (config == null || enemyConfig == null)
            return;

        int seed = ResolveRollSeed(enemyConfig);
        var conditionRandom = new System.Random(unchecked(seed ^ 0x3A5417));

        if (!config.TryRollEnemy(
                enemyConfig,
                new System.Random(seed),
                _rollResults,
                lootRuntime != null ? lootRuntime.CurrentLootStageIndex : 0))
        {
            return;
        }

        for (int i = 0; i < _rollResults.Count; i++)
        {
            LootRollResult result = _rollResults[i];
            if (result.Item == null)
                continue;
            float armorCondition = result.Item is ArmorItemSO rolledArmor
                ? enemyConfig.ResolveDroppedArmorDurability(rolledArmor, rolledArmor.MaxDurability, conditionRandom) : -1f;

            string message = "Corpse LootHub is missing.";
            bool stored = _lootInventory != null &&
                          _lootInventory.TryAddItemStack(
                              result.Item,
                              result.Amount,
                              out message, armorCondition);

            if (stored)
            {
                if (_logDropResults)
                {
                    Debug.Log(
                        $"[EnemyLootDropOnDeath] Added corpse loot. enemy={name}, item={result.Item.name}, amount={result.Amount}, rarity={result.Rarity}",
                        this);
                }

                continue;
            }

            Vector3 position = _worldDropOrigin != null
                ? _worldDropOrigin.position
                : transform.position;
            bool spawned = WorldItemPickupSpawner.TrySpawn(
                result.Item,
                result.Amount,
                result.Item is BulletSO ? result.Amount : 1,
                0f,
                armorCondition,
                position,
                Quaternion.identity,
                out _);

            if (!spawned)
            {
                Debug.LogError(
                    $"[EnemyLootDropOnDeath] Loot could not enter corpse inventory or spawn in the world. " +
                    $"enemy={name}, item={result.Item.name}, amount={result.Amount}, reason={message}",
                    this);
            }
            else if (_logDropResults)
            {
                Debug.Log(
                    $"[EnemyLootDropOnDeath] Corpse was full; spawned world loot. enemy={name}, item={result.Item.name}, amount={result.Amount}",
                    this);
            }
        }
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_damageable == null)
            _damageable = EntityComponentResolver.Resolve<Damageable>(this);

        if (_lootInventory == null)
            _lootInventory = EntityComponentResolver.Resolve<EnemyLootInventoryRuntime>(this);

        if (_enemyShooter == null)
            _enemyShooter = GetComponent<EnemyShooter>() ?? GetComponentInParent<EnemyShooter>();
    }

    // Purpose: Subscribes to subscribe notifications.
    private void Subscribe()
    {
        if (_subscribed || _damageable == null)
            return;

        _damageable.OnDie += HandleDeath;
        _subscribed = true;
    }

    // Purpose: Refreshes the displayed or cached legacy drop suppression.
    private void RefreshLegacyDropSuppression()
    {
        if (_lootInventory == null || _configuredEnemy == null)
            return;

        LootProgressionConfigSO config = _configOverride;
        if (config == null && LootProgressionRuntime.Instance != null)
            config = LootProgressionRuntime.Instance.Config;

        // Keep old DroppableRewardConfigSO behavior untouched until this enemy
        // is explicitly registered in the new authoring table.
        if (config != null && config.FindEnemySource(_configuredEnemy) != null)
            _lootInventory.SetSuppressRandomDropReward(true);
    }

    // Purpose: Removes the unsubscribe notification subscription.
    private void Unsubscribe()
    {
        if (!_subscribed || _damageable == null)
            return;

        _damageable.OnDie -= HandleDeath;
        _subscribed = false;
    }

    // Purpose: Resolves the roll seed needed by this component.
    private int ResolveRollSeed(EnemyShooterConfigSO enemyConfig)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + transform.position.GetHashCode();
            hash = hash * 31 + (enemyConfig != null ? enemyConfig.GetInstanceID() : 0);
            hash = hash * 31 + GetInstanceID();
            return hash;
        }
    }
}
