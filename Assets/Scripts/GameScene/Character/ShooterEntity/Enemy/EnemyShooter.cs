using UnityEngine;

[DisallowMultipleComponent]
public class EnemyShooter : Enemy
{
    [Header("Shooter Refs")]
    [SerializeField] private EnemyShooterStatsRuntime _statsRuntime;
    [SerializeField] private EnemyShooterShoot _shoot;
    [SerializeField] private EnemyShooterCombatController _combat;
    [SerializeField] private EntityWeaponHolder _weaponHolder;
    [SerializeField] private EntityEquipmentRuntime _equipmentRuntime;
    [SerializeField] private EnemyLootInventoryRuntime _lootInventory;
    [SerializeField] private EnemyShooterEffectController _effects;

    public EnemyShooterConfigSO Config =>
        _statsRuntime != null ? _statsRuntime.Config :
        _shoot != null ? _shoot.Config :
        null;

    private void Reset()
    {
        ResolveRefs();
    }

    private void Awake()
    {
        ResolveRefs();
    }

    public void SetEnemyShooterConfig(EnemyShooterConfigSO config)
    {
        ResolveRefs();

        if (_statsRuntime != null)
            _statsRuntime.SetConfig(config);

        EnemyShooterLoadout loadout = ResolveSpawnLoadout(config);
        WeaponSO weapon = loadout.Weapon;
        BulletSO bullet = loadout.Bullet != null
            ? loadout.Bullet
            : config != null ? config.Bullet : null;
        ArmorItemSO armor = loadout.Armor;

        if (_weaponHolder != null && weapon != null)
            _weaponHolder.Equip(weapon);

        if (_equipmentRuntime != null)
            _equipmentRuntime.EquipArmor(armor);

        if (_lootInventory != null)
            _lootInventory.Initialize(loadout);

        if (_statsRuntime != null)
            _statsRuntime.SetRuntimeBullet(bullet);

        if (_shoot != null)
        {
            _shoot.SetConfig(config);
            _shoot.SetRuntimeBullet(bullet);
        }

        _effects?.PlaySpawnParticles();
    }

    public override void OnAlertTriggerChange(bool entered, GameObject who)
    {
        base.OnAlertTriggerChange(entered, who);

        if (!entered)
            return;

        ResolveRefs();
        _combat?.AlertToPlayer();
    }

    public override void OnAttackTriggerChange(bool entered, GameObject who)
    {
        base.OnAttackTriggerChange(entered, who);
    }

    public override void NotifyDamagedBy(GameObject attacker)
    {
        base.NotifyDamagedBy(attacker);

        ResolveRefs();
        _combat?.TryAlertToPlayerFromDamage(attacker);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        ResolveRefs();
        _combat?.ClearCombat();
    }

    private void ResolveRefs()
    {
        if (_statsRuntime == null)
            _statsRuntime = GetComponent<EnemyShooterStatsRuntime>() ??
                            GetComponentInChildren<EnemyShooterStatsRuntime>(true) ??
                            GetComponentInParent<EnemyShooterStatsRuntime>();

        if (_shoot == null)
            _shoot = GetComponent<EnemyShooterShoot>() ??
                     GetComponentInChildren<EnemyShooterShoot>(true) ??
                     GetComponentInParent<EnemyShooterShoot>();

        if (_combat == null)
            _combat = GetComponent<EnemyShooterCombatController>() ??
                      GetComponentInChildren<EnemyShooterCombatController>(true) ??
                      GetComponentInParent<EnemyShooterCombatController>();

        if (_weaponHolder == null)
            _weaponHolder = GetComponent<EntityWeaponHolder>() ??
                            GetComponentInChildren<EntityWeaponHolder>(true) ??
                            GetComponentInParent<EntityWeaponHolder>();

        if (_equipmentRuntime == null)
            _equipmentRuntime = GetComponent<EntityEquipmentRuntime>() ??
                                GetComponentInChildren<EntityEquipmentRuntime>(true) ??
                                GetComponentInParent<EntityEquipmentRuntime>();

        if (_lootInventory == null)
            _lootInventory = GetComponent<EnemyLootInventoryRuntime>() ??
                             GetComponentInChildren<EnemyLootInventoryRuntime>(true) ??
                             GetComponentInParent<EnemyLootInventoryRuntime>();

        if (_effects == null)
            _effects = GetComponent<EnemyShooterEffectController>() ??
                       GetComponentInChildren<EnemyShooterEffectController>(true) ??
                       GetComponentInParent<EnemyShooterEffectController>();
    }

    private static EnemyShooterLoadout ResolveSpawnLoadout(EnemyShooterConfigSO config)
    {
        if (config is RaiderShooterConfigSO raiderConfig)
            return raiderConfig.PickLoadout();

        return default;
    }
}
