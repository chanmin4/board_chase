/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > Common
 * Primary responsibility: Coordinates the EnemyShooterStatsRuntime subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public partial class EnemyShooterStatsRuntime : ShooterStatsRuntime
{
    [Header("Config")]
    [SerializeField] private EnemyShooterConfigSO _config;

    [Header("Refs")]
    [SerializeField] private Damageable _damageable;
    [FormerlySerializedAs("_weaponHolder")]
    [SerializeField] private ShooterEquipmentController _equipmentController;
    [SerializeField] private EntityEquipmentRuntime _equipmentRuntime;

    private BulletSO _runtimeBullet;
    private SectorEnemyTraitModifier _sectorTraitModifier;
    private EnemyLootInventoryRuntime _inventory;
    private bool _inventoryResolved;

    public EnemyShooterConfigSO Config => ResolveConfig();
    public override EntityStatConfigSO StatConfig => Config;
    public override WeaponSO CurrentWeapon =>
        _equipmentController != null ? _equipmentController.CurrentWeapon : null;
    public override BulletSO CurrentBullet => _runtimeBullet;
    public override float AttackDamage => ResolveAttackDamage(CurrentBullet);
    public override float DodgeChance => Mathf.Clamp01(ResolveWithRuntimeModifiers(PlayerStatId.DodgeChance,0f,0f));
    public override float GasInfectionResistance => Mathf.Clamp(ResolveWithRuntimeModifiers(PlayerStatId.GasInfectionResistance, 0f, 0f), 0f, 0.9f);
    public override float MaxRange => ResolveWithRuntimeModifiers(PlayerStatId.MaxRange, base.MaxRange, 0.1f);
    public override float ShotsPerSecond => ResolveWithRuntimeModifiers(PlayerStatId.ShotsPerSecond, base.ShotsPerSecond, 0.01f);
    public override float ReloadDurationSeconds => ResolveWithRuntimeModifiers(PlayerStatId.ReloadDurationSeconds, base.ReloadDurationSeconds, 0.01f);
    public override int MagazineSize => base.MagazineSize;
    public override float PaintMarkDamage => ResolvePaintMarkDamage(CurrentBullet);
    public override float InfectionDamage => ResolveInfectionDamage(CurrentBullet);
    public override float PenetrationClassBonus => Mathf.Max(
        0,
        ResolveWithRuntimeModifiers(
            PlayerStatId.PenetrationClass,
            base.PenetrationClassBonus,
            0f));
    public override float MoveSpeed => ApplyStatusMoveSpeed(
        ResolveWithRuntimeModifiers(PlayerStatId.MoveSpeed, Config != null ? Config.MoveSpeed : 0f, 0f)) *
        ResolveSectorTraitMoveSpeedMultiplier() * ResolveBackpackMoveSpeedMultiplier();
    public override float MaxHealth => ResolveWithRuntimeModifiers(PlayerStatId.MaxHealth, Config != null ? Config.MaxHealth : 0f, 1f) *
        ResolveSectorTraitMaxHealthMultiplier();
    public override float ArmorClass => Mathf.Max(
        0f,
        ResolveWithRuntimeModifiers(
            PlayerStatId.ArmorClass,
            Config != null ? Config.BaseArmorClass : 0f,
            0f) + ResolveSectorTraitArmorClassBonus());
    public override float ArmorHealthDurabilityLossMultiplier => ResolveWithRuntimeModifiers(
        PlayerStatId.ArmorHealthDurabilityLossMultiplier,
        Config != null && Config.Armor != null ? Config.Armor.HealthDurabilityLossMultiplier : 1f,
        0f);

    public override float ArmorInfectionDurabilityLossMultiplier => ResolveWithRuntimeModifiers(
        PlayerStatId.ArmorInfectionDurabilityLossMultiplier,
        Config != null && Config.Armor != null ? Config.Armor.InfectionDurabilityLossMultiplier : 0.5f,
        0f);

    public override float VisionRange => ResolveWithRuntimeModifiers(PlayerStatId.VisionRange, Config != null ? Config.Vision.VisionRange : 0f, 0f);
    public override float GunshotSoundRadius => ResolveWithRuntimeModifiers(PlayerStatId.GunshotSoundRadius, base.GunshotSoundRadius, 0f);
    public override float SoundInvestigateDelaySeconds =>
        Config != null ? Config.SoundInvestigateDelaySeconds : 0f;
    public override float FootstepSoundRadius =>
        Config != null ? Config.FootstepSoundRadius : 0f;
    public override float FootstepSoundInterval =>
        Config != null ? Config.FootstepSoundInterval : 0.35f;
    public override int InventoryCapacity =>
        Config != null ? Config.InventoryCapacity : 0;
    public override float AimSpeed => ResolveWithRuntimeModifiers(PlayerStatId.AimSpeed, base.AimSpeed, 0.01f);
    public override float AimingMoveSpeedMultiplier => Mathf.Clamp01(ResolveWithRuntimeModifiers(
        PlayerStatId.AimingMoveSpeedMultiplier, base.AimingMoveSpeedMultiplier, 0f));
    public override float ShotKickDistance => ResolveWithRuntimeModifiers(PlayerStatId.ShotKickDistance, base.ShotKickDistance, 0f);
    public override float MaximumShotKickDistance => ResolveWithRuntimeModifiers(PlayerStatId.MaximumShotKickDistance, base.MaximumShotKickDistance, 0f);
    public override float ShotKickRecoveryPerSecond => ResolveWithRuntimeModifiers(PlayerStatId.ShotKickRecoveryPerSecond, base.ShotKickRecoveryPerSecond, 0f);
    public override float HipFireSpreadAngleDeg => ApplyWeaponConditionToSpread(ResolveWithRuntimeModifiers(
        PlayerStatId.HipFireSpreadAngleDeg,
        base.HipFireSpreadAngleDeg,
        0f));
    public override float AimSpreadAngleDeg => ApplyWeaponConditionToSpread(ResolveWithRuntimeModifiers(
        PlayerStatId.AimSpreadAngleDeg,
        base.AimSpreadAngleDeg,
        0f));
    public override float DashCooldownSeconds => ResolveWithRuntimeModifiers(
        PlayerStatId.DashCooldownSeconds, base.DashCooldownSeconds, 0f);
    public override float DashDistanceMultiplier => ResolveWithRuntimeModifiers(PlayerStatId.DashDistance, base.DashDistanceMultiplier, 0f);

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
    }

    // Purpose: Updates the config state.
    public void SetConfig(EnemyShooterConfigSO config)
    {
        if (_config != config) ResetPassiveModules();
        _config = config;
    }

    // Purpose: Updates the runtime bullet state.
    public void SetRuntimeBullet(BulletSO bullet)
    {
        _runtimeBullet = bullet;
    }

    // Purpose: Resolves the attack damage needed by this component.
    public override float ResolveAttackDamage(BulletSO bullet)
    {
        float baseDamage = bullet != null ? bullet.HealthDamage : 0f;
        float runtimeValue = ResolveWithRuntimeModifiers(PlayerStatId.AttackDamage, baseDamage, 0f);
        return ApplyWeaponConditionToDamage(ApplyWeaponArchetypeToDamage(
            ResolveWithExtraModifiers(PlayerStatId.AttackDamage, runtimeValue, 0f, bullet != null ? bullet.StatModifiers : null),
            bullet)) * ResolveSectorTraitAttackDamageMultiplier();
    }

    // Purpose: Resolves the paint mark damage needed by this component.
    public override float ResolvePaintMarkDamage(BulletSO bullet) => 0f; // WIP basic-bullet compatibility only.

    // Purpose: Resolves the infection damage needed by this component.
    public override float ResolveInfectionDamage(BulletSO bullet) => 0f; // WIP basic-bullet compatibility only.

    // Purpose: Resolves the shots per second needed by this component.
    public override float ResolveShotsPerSecond(BulletSO bullet)
    {
        return ResolveWithExtraModifiers(PlayerStatId.ShotsPerSecond, ShotsPerSecond, 0.01f, bullet != null ? bullet.StatModifiers : null);
    }

    // Purpose: Resolves the magazine size needed by this component.
    public override int ResolveMagazineSize(BulletSO bullet)
    {
        return MagazineSize;
    }

    // Purpose: Resolves the armor health durability loss multiplier needed by this component.
    public override float ResolveArmorHealthDurabilityLossMultiplier(BulletSO bullet)
    {
        return ResolveWithExtraModifiers(
            PlayerStatId.ArmorHealthDurabilityLossMultiplier,
            ArmorHealthDurabilityLossMultiplier,
            0f,
            bullet != null ? bullet.StatModifiers : null);
    }

    // Purpose: Resolves the armor infection durability loss multiplier needed by this component.
    public override float ResolveArmorInfectionDurabilityLossMultiplier(BulletSO bullet)
    {
        return ResolveWithExtraModifiers(
            PlayerStatId.ArmorInfectionDurabilityLossMultiplier,
            ArmorInfectionDurabilityLossMultiplier,
            0f,
            bullet != null ? bullet.StatModifiers : null);
    }

    // Purpose: Resolves the config needed by this component.
    private EnemyShooterConfigSO ResolveConfig()
    {
        if (_config != null)
            return _config;

        ResolveRefs();

        _config = _damageable != null
            ? _damageable.StatConfig as EnemyShooterConfigSO
            : null;

        return _config;
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_damageable == null)
            _damageable = EntityComponentResolver.Resolve<Damageable>(this);

        if (_equipmentController == null)
            _equipmentController = EntityComponentResolver.Resolve<ShooterEquipmentController>(this);

        if (_equipmentRuntime == null)
            _equipmentRuntime = EntityComponentResolver.Resolve<EntityEquipmentRuntime>(this);

        if (_sectorTraitModifier == null)
            _sectorTraitModifier = EntityComponentResolver.Resolve<SectorEnemyTraitModifier>(this);
    }

    // Purpose: Resolves the sector trait modifier needed by this component.
    private SectorEnemyTraitModifier ResolveSectorTraitModifier()
    {
        if (_sectorTraitModifier == null)
            _sectorTraitModifier = EntityComponentResolver.Resolve<SectorEnemyTraitModifier>(this);

        return _sectorTraitModifier;
    }

    // Purpose: Resolves the sector trait attack damage multiplier needed by this component.
    private float ResolveSectorTraitAttackDamageMultiplier()
    {
        SectorEnemyTraitModifier modifier = ResolveSectorTraitModifier();
        return modifier != null ? modifier.AttackDamageMultiplier : 1f;
    }

    // Purpose: Resolves the sector trait max health multiplier needed by this component.
    private float ResolveSectorTraitMaxHealthMultiplier()
    {
        SectorEnemyTraitModifier modifier = ResolveSectorTraitModifier();
        return modifier != null ? modifier.MaxHealthMultiplier : 1f;
    }

    // Purpose: Resolves the sector trait move speed multiplier needed by this component.
    private float ResolveSectorTraitMoveSpeedMultiplier()
    {
        SectorEnemyTraitModifier modifier = ResolveSectorTraitModifier();
        return modifier != null ? modifier.MoveSpeedMultiplier : 1f;
    }

    private float ResolveBackpackMoveSpeedMultiplier()
    {
        if (!_inventoryResolved)
        {
            _inventory = EntityComponentResolver.Resolve<EnemyLootInventoryRuntime>(this);
            _inventoryResolved = true;
        }
        return _inventory != null ? _inventory.BackpackMoveSpeedMultiplier : 1f;
    }

    // Purpose: Resolves the sector trait armor class bonus needed by this component.
    private int ResolveSectorTraitArmorClassBonus()
    {
        SectorEnemyTraitModifier modifier = ResolveSectorTraitModifier();
        return modifier != null ? modifier.ArmorClassBonus : 0;
    }

    // Purpose: Resolves the with runtime modifiers needed by this component.
    public override float AdditionalStat(PlayerStatId stat,float baseline=0)=>ResolveWithRuntimeModifiers(stat,baseline,0);
    private float ResolveWithRuntimeModifiers(PlayerStatId stat, float baseValue, float minValue)
    {
        if (!ShooterStatPolicy.IsCharacterStat(stat)) return Mathf.Max(minValue, baseValue);
        StatAccumulator accumulator = default;
        ApplyMatchingModifiers(ref accumulator, stat, CurrentWeapon != null ? CurrentWeapon.StatModifiers : null);
        ApplyMatchingModifiers(ref accumulator, stat, _equipmentRuntime != null && _equipmentRuntime.HasUsableArmor && _equipmentRuntime.CurrentArmor != null
            ? _equipmentRuntime.CurrentArmor.StatModifiers
            : null);
        foreach (var passive in PassiveModules)
            if (passive != null && ShooterStatPolicy.IsPassiveStat(stat))
                foreach (var modifier in passive.StatModifiers ?? System.Array.Empty<PlayerStatModifier>())
                    if (modifier.stat == stat)
                        accumulator.Apply(PassiveModuleRulesSO.Active != null ? PassiveModuleRulesSO.Active.ApplyStrength(passive, modifier) : modifier);

        return Mathf.Max(minValue, accumulator.Resolve(baseValue));
    }

    // Purpose: Resolves the with extra modifiers needed by this component.
    private static float ResolveWithExtraModifiers(
        PlayerStatId stat,
        float baseValue,
        float minValue,
        PlayerStatModifier[] modifiers)
    {
        StatAccumulator accumulator = default;

        if (modifiers != null)
        {
            for (int i = 0; i < modifiers.Length; i++)
            {
                if (modifiers[i].stat == stat)
                    accumulator.Apply(modifiers[i]);
            }
        }

        return Mathf.Max(minValue, accumulator.Resolve(baseValue));
    }

    // Purpose: Applies the requested matching modifiers change.
    private static void ApplyMatchingModifiers(
        ref StatAccumulator accumulator,
        PlayerStatId stat,
        PlayerStatModifier[] modifiers)
    {
        if (modifiers == null || !ShooterStatPolicy.IsCharacterStat(stat))
            return;

        for (int i = 0; i < modifiers.Length; i++)
        {
            if (modifiers[i].stat == stat)
                accumulator.Apply(modifiers[i]);
        }
    }

    private new struct StatAccumulator
    {
        private float _flatAdd;
        private float _percentAdd;
        private bool _hasOverride;
        private float _overrideValue;
        private bool _hasPercentMultiply;
        private float _percentMultiply;
        // Purpose: Applies the requested apply change.
        public void Apply(PlayerStatModifier modifier)
        {
            switch (modifier.type)
            {
                case StatModifierType.FlatAdd:
                    _flatAdd += modifier.value;
                    break;

                case StatModifierType.PercentAdd:
                    _percentAdd += modifier.value;
                    break;
                case StatModifierType.PercentMultiply:
                    if (!_hasPercentMultiply)
                    {
                        _hasPercentMultiply = true;
                        _percentMultiply = 1f;
                    }

                    _percentMultiply *= modifier.value;
                    break;
                case StatModifierType.Override:
                    _hasOverride = true;
                    _overrideValue = modifier.value;
                    break;
            }
        }

        // Purpose: Resolves the resolve needed by this component.
        public float Resolve(float baseValue)
        {
            if (_hasOverride)
                return _overrideValue;

            float value = (baseValue + _flatAdd) * (1f + _percentAdd * 0.01f);

            if (_hasPercentMultiply)
                value *= _percentMultiply;

            return value;
        }
    }
}
