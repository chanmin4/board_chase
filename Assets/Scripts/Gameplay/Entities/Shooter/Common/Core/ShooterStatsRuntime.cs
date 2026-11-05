/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Coordinates the ShooterStatsRuntime subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public abstract class ShooterStatsRuntime : MonoBehaviour
{
    private EntityStatusEffectRuntime _statusEffects;
    // Runtime-only cache for weapon condition calculations. Keep its name
    // distinct from the serialized equipment refs owned by derived runtimes.
    private EntityEquipmentRuntime _weaponConditionEquipmentRuntime;
    public abstract EntityStatConfigSO StatConfig { get; }
    public ShooterFaction Faction => StatConfig is EnemyShooterConfigSO enemy ? enemy.Faction : ShooterFaction.Player;
    public virtual float GasInfectionResistance => 0f;
    public ShooterInventorySettings InventorySettings => (StatConfig as ShooterStatConfigSO)?.Inventory;
    public ShooterDashSettings DashSettings => (StatConfig as ShooterStatConfigSO)?.Dash;

    public virtual WeaponSO CurrentWeapon => null;
    public virtual BulletSO CurrentBullet => null;
    public RangedWeaponSO CurrentFirearm => CurrentWeapon as RangedWeaponSO;

    public virtual float AttackDamage => 0f;
    public virtual float MaxRange => CurrentFirearm != null ? CurrentFirearm.MaxRange : 0f;
    public virtual float ShotsPerSecond => CurrentFirearm != null ? CurrentFirearm.ShotsPerSecond : 0f;
    public virtual float ReloadDurationSeconds => CurrentFirearm != null ? CurrentFirearm.ReloadDurationSeconds : 0f;
    public virtual int MagazineSize => CurrentWeapon is RangedWeaponSO firearm ? firearm.MagazineSize : 1;

    public virtual float PaintMarkDamage => 0f;
    public virtual float InfectionDamage => 0f;
    public virtual float HealthDamageCoefficient => 1f; // Bullet damage and weapon DamageMultiplier own the baseline.
    public virtual float InfectionDamageCoefficient =>
        StatConfig is ShooterStatConfigSO shooterConfig ? shooterConfig.InfectionDamageCoefficient : 1f;
    public virtual float MarkAmountCoefficient =>
        StatConfig is ShooterStatConfigSO shooterConfig ? shooterConfig.MarkAmountCoefficient : 1f;
    public virtual float PenetrationClassBonus => CurrentFirearm != null ? CurrentFirearm.PenetrationClassBonus : 0f;

    public virtual float MoveSpeed => 0f;
    public virtual float StaminaRecoveryMultiplier => 1f;
    public EntityStatusEffectRuntime StatusEffects
    {
        get
        {
            if (_statusEffects == null)
                _statusEffects = EntityComponentResolver.Resolve<EntityStatusEffectRuntime>(this);

            return _statusEffects;
        }
    }
    public virtual float DashCooldownSeconds => StatConfig is ShooterStatConfigSO shooter ? shooter.DashCooldownSeconds : 0f;
    public virtual float AdditionalStat(PlayerStatId stat,float baseline=0)=>ResolveStatWithWeaponAndBulletModifiers(stat,baseline,0,CurrentBullet);
    public virtual float DashDistanceMultiplier => StatConfig is ShooterStatConfigSO shooter ? shooter.Dash.distanceMultiplier : 1.3f;
    public virtual float MaxHealth => 0f;
    /// <summary>
    /// Final base armor class before equipped armor and temporary deltas.
    /// This is fractional so permanent progression bonuses can add half
    /// classes while authored armor and bullet penetration stay integer-based.
    /// </summary>
    public virtual float ArmorClass => StatConfig != null ? StatConfig.BaseArmorClass : 0f;
    public virtual float DodgeChance => 0f;
    public virtual float VisionRange => StatConfig != null ? StatConfig.Vision.VisionRange : 0f;
    public virtual float ArmorHealthDurabilityLossMultiplier =>
        StatConfig != null && StatConfig.Armor != null
            ? StatConfig.Armor.HealthDurabilityLossMultiplier
            : 1f;

    public virtual float ArmorInfectionDurabilityLossMultiplier =>
        StatConfig != null && StatConfig.Armor != null
            ? StatConfig.Armor.InfectionDurabilityLossMultiplier
            : 0.5f;

    public virtual float GunshotSoundRadius =>
        CurrentWeapon is RangedWeaponSO rangedWeapon
            ? rangedWeapon.GunshotSoundRadius
            : 0f;
    public virtual float SoundInvestigateDelaySeconds => 0f;
    public virtual float FootstepSoundRadius => 0f;
    public virtual float FootstepSoundInterval => 0.35f;
    public virtual int InventoryCapacity => InventorySettings?.CapacityWithoutBackpack ?? 0;

    public virtual float AimSpeed => CurrentFirearm != null ? CurrentFirearm.AimSpeed : 3f;
    public virtual float AimingMoveSpeedMultiplier => CurrentFirearm != null ? CurrentFirearm.AimingMoveSpeedMultiplier : 1f;
    public virtual float ShotKickDistance => CurrentFirearm != null ? CurrentFirearm.ShotKickDistance : 0f;
    public virtual float MaximumShotKickDistance => CurrentFirearm != null ? CurrentFirearm.MaximumShotKickDistance : 0f;
    public virtual float ShotKickRecoveryPerSecond => CurrentFirearm != null ? CurrentFirearm.ShotKickRecoveryPerSecond : 0f;
    public virtual float AimRangeMultiplier => 1f;
    public virtual float AimMoveSpeedMultiplier => 1f;
    public virtual float HipFireSpreadAngleDeg => CurrentFirearm != null ? CurrentFirearm.HipFireSpreadAngleDeg : 0f;
    public virtual float AimSpreadAngleDeg => CurrentFirearm != null ? CurrentFirearm.AimSpreadAngleDeg : 0f;
    public virtual float RecoilAngleDeg => 0f;
    public virtual float RecoilRecoverySpeedDegPerSecond => 0f;
    public virtual float RecoilForwardDistancePerShot => 0f;
    public virtual float RecoilSideDistancePerShot => 0f;
    public virtual float MaxRecoilDistance => 0f;
    public virtual float RecoilDistanceRecoveryPerSecond => 0f;
    public virtual float HipFireSpreadRadius => 0f;
    public virtual float AimSpreadRadius => 0f;

    // Purpose: Resolves the attack damage needed by this component.
    public virtual float ResolveAttackDamage(BulletSO bullet)
    {
        float baseDamage = bullet != null ? bullet.HealthDamage : 0f;
        return ApplyWeaponConditionToDamage(ApplyWeaponArchetypeToDamage(
            ResolveStatWithWeaponAndBulletModifiers(
                PlayerStatId.AttackDamage,
                baseDamage * HealthDamageCoefficient,
                0f,
                bullet),
            bullet));
    }

    // Purpose: Applies the requested weapon condition to damage change.
    protected float ApplyWeaponConditionToDamage(float damage)
    {
        return Mathf.Max(0f, damage * ResolveWeaponConditionPerformanceMultiplier());
    }

    // Purpose: Applies the requested weapon archetype to damage change.
    protected float ApplyWeaponArchetypeToDamage(float damage, BulletSO bullet)
    {
        float multiplier = CurrentWeapon is RangedWeaponSO rangedWeapon
            ? rangedWeapon.ResolveDamageMultiplierPerProjectile(bullet)
            : 1f;

        return Mathf.Max(0f, damage * multiplier);
    }

    // Purpose: Applies the requested weapon archetype to shots per second change.
    protected float ApplyWeaponArchetypeToShotsPerSecond(float shotsPerSecond)
    {
        return Mathf.Max(0.01f, shotsPerSecond); // Compatibility helper: rate is already absolute on the weapon.
    }

    // Purpose: Applies the requested weapon condition to spread change.
    protected float ApplyWeaponConditionToSpread(float spread)
    {
        float performance = ResolveWeaponConditionPerformanceMultiplier();
        return performance > 0f
            ? Mathf.Max(0f, spread) / performance
            : Mathf.Max(0f, spread);
    }

    // Purpose: Resolves the weapon condition performance multiplier needed by this component.
    protected float ResolveWeaponConditionPerformanceMultiplier()
    {
        if (_weaponConditionEquipmentRuntime == null)
        {
            _weaponConditionEquipmentRuntime =
                EntityComponentResolver.Resolve<EntityEquipmentRuntime>(this);
        }

        return _weaponConditionEquipmentRuntime != null &&
               CurrentWeapon is RangedWeaponSO
            ? _weaponConditionEquipmentRuntime.GetWeaponPerformanceMultiplier(
                _weaponConditionEquipmentRuntime.ActiveWeaponSlotIndex)
            : 1f;
    }

    // Purpose: Resolves the paint mark damage needed by this component.
    public virtual float ResolvePaintMarkDamage(BulletSO bullet) => 0f; // WIP basic-bullet compatibility only.

    // Purpose: Resolves the infection damage needed by this component.
    public virtual float ResolveInfectionDamage(BulletSO bullet) => 0f; // WIP basic-bullet compatibility only.

    // Purpose: Resolves the shots per second needed by this component.
    public virtual float ResolveShotsPerSecond(BulletSO bullet)
    {
        return ShotsPerSecond;
    }

    // Purpose: Resolves the magazine size needed by this component.
    public virtual int ResolveMagazineSize(BulletSO bullet)
    {
        return Mathf.Max(1, MagazineSize);
    }

    // Purpose: Resolves the penetration class needed by this component.
    public virtual float ResolvePenetrationClass(BulletSO bullet)
    {
        float bulletPenetration = bullet != null ? bullet.PenetrationClass : 0;
        return Mathf.Max(0, bulletPenetration + PenetrationClassBonus);
    }

    // Purpose: Resolves the armor health durability loss multiplier needed by this component.
    public virtual float ResolveArmorHealthDurabilityLossMultiplier(BulletSO bullet)
    {
        return ResolveStatWithWeaponAndBulletModifiers(
            PlayerStatId.ArmorHealthDurabilityLossMultiplier,
            ArmorHealthDurabilityLossMultiplier,
            0f,
            bullet);
    }

    // Purpose: Resolves the armor infection durability loss multiplier needed by this component.
    public virtual float ResolveArmorInfectionDurabilityLossMultiplier(BulletSO bullet)
    {
        return ResolveStatWithWeaponAndBulletModifiers(
            PlayerStatId.ArmorInfectionDurabilityLossMultiplier,
            ArmorInfectionDurabilityLossMultiplier,
            0f,
            bullet);
    }

    // Purpose: Resolves the stat with weapon and bullet modifiers needed by this component.
    protected float ResolveStatWithWeaponAndBulletModifiers(
        PlayerStatId stat,
        float baseValue,
        float minValue,
        BulletSO bullet)
    {
        StatAccumulator accumulator = default;

        ApplyMatchingModifiers(ref accumulator, stat, CurrentWeapon != null ? CurrentWeapon.StatModifiers : null);
        ApplyMatchingModifiers(ref accumulator, stat, bullet != null ? bullet.StatModifiers : null);

        return Mathf.Max(minValue, accumulator.Resolve(baseValue));
    }

    // Purpose: Applies the requested status move speed change.
    protected float ApplyStatusMoveSpeed(float baseMoveSpeed)
    {
        float multiplier = StatusEffects != null
            ? StatusEffects.GetMultiplier(StatusEffectModifierType.MoveSpeedMultiplier)
            : 1f;
        return Mathf.Max(0f, baseMoveSpeed * multiplier);
    }

    // Purpose: Applies the requested matching modifiers change.
    protected static void ApplyMatchingModifiers(
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

    protected struct StatAccumulator
    {
        private float _flatAdd;
        private float _percentAdd;
        private bool _hasPercentMultiply;
        private float _percentMultiply;
        private bool _hasOverride;
        private float _overrideValue;

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
