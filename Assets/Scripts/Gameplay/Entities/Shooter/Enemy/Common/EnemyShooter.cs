/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > Common
 * Primary responsibility: Implements the focused EnemyShooter responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class EnemyShooter : Enemy
{
    [Header("Shooter Refs")]
    [SerializeField] private EnemyShooterStatsRuntime _statsRuntime;
    [SerializeField] private EnemyShooterShoot _shoot;
    [SerializeField] private EnemyShooterCombatController _combat;
    [SerializeField] private ShooterEquipmentController _equipmentController;
    [SerializeField] private EntityEquipmentRuntime _equipmentRuntime;
    [SerializeField] private EnemyLootInventoryRuntime _lootInventory;
    [SerializeField] private EnemyShooterEffectController _effects;

    // The gameplay prefab keeps combat, colliders, and spawn particles outside
    // these roots. Only the body and equipped item renderers are gated while
    // the shared enemy spawn state has not completed.
    private readonly Dictionary<Renderer, bool> _spawnHiddenRendererStates = new();

    public EnemyShooterConfigSO Config =>
        _statsRuntime != null ? _statsRuntime.Config :
        _shoot != null ? _shoot.Config :
        null;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
        ApplySpawnVisualVisibility(IsSpawnReady);
        ShooterStuckRecovery.EnsureOn(this);
    }

    // Purpose: Updates the spawn ready state.
    public override void SetSpawnReady(bool isReady)
    {
        base.SetSpawnReady(isReady);
        ApplySpawnVisualVisibility(isReady);
    }

    // Purpose: Updates the enemy shooter config state.
    public void SetEnemyShooterConfig(EnemyShooterConfigSO config)
    {
        ResolveRefs();

        if (_statsRuntime != null)
            _statsRuntime.SetConfig(config);

        EnemyShooterLoadout loadout = ResolveSpawnLoadout(config);
        ArmorItemSO armor = loadout.Armor;

        EquipWeaponLoadout(
            loadout.WeaponStacks,
            config != null ? config.EnemyRole : EnemyShooterRole.StandardEnemy);

        int activeWeaponSlotIndex = _equipmentRuntime != null
            ? _equipmentRuntime.ActiveWeaponSlotIndex
            : 0;
        BulletSO bullet = loadout.GetBulletForWeaponSlot(activeWeaponSlotIndex);

        if ((_equipmentRuntime != null ? _equipmentRuntime.CurrentWeapon : null) is RangedWeaponSO rangedWeapon &&
            bullet == null)
        {
            Debug.LogError(
                $"[EnemyShooter] Rolled ranged weapon has no compatible loadout ammunition. enemy={name}, config={(config != null ? config.name : "None")}, weapon={rangedWeapon.name}, requiredFamily={rangedWeapon.RequiredBulletFamily}.",
                this);
        }

        if (_equipmentController != null)
            _equipmentController.EquipArmor(armor);
        else if (_equipmentRuntime != null)
            _equipmentRuntime.EquipArmor(armor);

        if (_lootInventory != null)
            _lootInventory.Initialize(loadout);

        // Spawned shooters all share the same authored loot progression lookup.
        // The component writes into the existing corpse LootHub only on death.
        EnemyLootDropOnDeath authoredLoot =
            GetComponent<EnemyLootDropOnDeath>() ??
            gameObject.AddComponent<EnemyLootDropOnDeath>();
        authoredLoot.Configure(config);

        if (_statsRuntime != null)
            _statsRuntime.SetRuntimeBullet(bullet);

        if (_shoot != null)
        {
            _shoot.SetConfig(config);
            _shoot.SetRuntimeBullet(bullet);
        }

        // Weapon and armor views are created above. Re-apply the gate so a
        // newly instantiated loadout never appears before spawn completion.
        ApplySpawnVisualVisibility(IsSpawnReady);

        _effects?.PlaySpawnParticles();
    }

    // Purpose: Equips weapon loadout for the current owner.
    private void EquipWeaponLoadout(
        ShooterLoadoutWeaponStack[] weaponStacks,
        EnemyShooterRole enemyRole)
    {
        int slotCount = _equipmentRuntime != null
            ? _equipmentRuntime.WeaponSlotCount
            : 2;

        for (int i = 0; i < slotCount; i++)
        {
            if (_equipmentController != null)
                _equipmentController.ClearWeapon(i);
            else
                _equipmentRuntime?.ClearWeapon(i);
        }

        if (weaponStacks == null)
            return;

        for (int i = 0; i < weaponStacks.Length; i++)
        {
            ShooterLoadoutWeaponStack stack = weaponStacks[i];

            if (!stack.HasWeapon || stack.slotIndex >= slotCount)
                continue;

            if (_equipmentController != null)
            {
                _equipmentController.EquipEnemySpawnWeapon(
                    stack.slotIndex,
                    stack.weapon,
                    stack.amount,
                    enemyRole);
            }
            else
            {
                _equipmentRuntime?.EquipEnemySpawnWeapon(
                    stack.slotIndex,
                    stack.weapon,
                    stack.amount,
                    enemyRole);
            }
        }

        _equipmentRuntime?.SetActiveWeaponSlot(0);
    }

    // Purpose: Responds to the alert trigger change callback.
    public override void OnAlertTriggerChange(bool entered, GameObject who)
    {
        base.OnAlertTriggerChange(entered, who);

        if (!entered)
            return;

        ResolveRefs();
        _combat?.AlertToPlayer();
    }

    // Purpose: Responds to the attack trigger change callback.
    public override void OnAttackTriggerChange(bool entered, GameObject who)
    {
        base.OnAttackTriggerChange(entered, who);
    }

    // Purpose: Publishes the damaged by notification.
    public override void NotifyDamagedBy(GameObject attacker)
    {
        base.NotifyDamagedBy(attacker);

        ResolveRefs();
        _combat?.TryAlertToPlayerFromDamage(attacker);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    protected override void OnDisable()
    {
        base.OnDisable();

        ResolveRefs();
        _combat?.ClearCombat();
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_statsRuntime == null)
            _statsRuntime = EntityComponentResolver.Resolve<EnemyShooterStatsRuntime>(this);

        if (_shoot == null)
            _shoot = EntityComponentResolver.Resolve<EnemyShooterShoot>(this);

        if (_combat == null)
            _combat = EntityComponentResolver.Resolve<EnemyShooterCombatController>(this);

        if (_equipmentController == null)
            _equipmentController = EntityComponentResolver.Resolve<ShooterEquipmentController>(this);

        if (_equipmentRuntime == null)
            _equipmentRuntime = EntityComponentResolver.Resolve<EntityEquipmentRuntime>(this);

        if (_lootInventory == null)
            _lootInventory = EntityComponentResolver.Resolve<EnemyLootInventoryRuntime>(this);

        if (_effects == null)
            _effects = EntityComponentResolver.Resolve<EnemyShooterEffectController>(this);
    }

    // Purpose: Applies the requested spawn visual visibility change.
    private void ApplySpawnVisualVisibility(bool visible)
    {
        if (visible)
        {
            foreach (KeyValuePair<Renderer, bool> entry in _spawnHiddenRendererStates)
            {
                if (entry.Key != null)
                    entry.Key.forceRenderingOff = entry.Value;
            }

            _spawnHiddenRendererStates.Clear();
            return;
        }

        Transform visualRoot = transform.Find("Visual");
        Transform equipmentRoot = transform.Find("Equipment");

        bool foundAuthoredVisualRoot = false;
        foundAuthoredVisualRoot |= HideRenderersUnder(visualRoot);
        foundAuthoredVisualRoot |= HideRenderersUnder(equipmentRoot);

        if (!foundAuthoredVisualRoot)
        {
            // Keep custom Shooter variants safe even if they have not yet
            // adopted the standard Visual / Equipment root names.
            HideRenderersUnder(transform);
        }
    }

    // Purpose: Hides the renderers under UI or visual state.
    private bool HideRenderersUnder(Transform root)
    {
        if (root == null)
            return false;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer)
                continue;

            if (!_spawnHiddenRendererStates.ContainsKey(renderer))
                _spawnHiddenRendererStates.Add(renderer, renderer.forceRenderingOff);

            renderer.forceRenderingOff = true;
        }

        return true;
    }

    // Purpose: Resolves the spawn loadout needed by this component.
    private static EnemyShooterLoadout ResolveSpawnLoadout(EnemyShooterConfigSO config)
    {
        return config != null
            ? config.PickEnemyLoadout()
            : default;
    }
}
