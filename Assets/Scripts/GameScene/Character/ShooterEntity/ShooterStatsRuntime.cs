using UnityEngine;

public abstract class ShooterStatsRuntime : MonoBehaviour
{
    public abstract EntityStatConfigSO StatConfig { get; }

    public virtual WeaponSO CurrentWeapon => null;
    public virtual BulletSO CurrentBullet => null;
    public virtual PaintChannel PaintChannel => PaintChannel.Vaccine;

    public virtual float AttackDamage => 0f;
    public virtual float MaxRange => 0f;
    public virtual float ShotsPerSecond => 0f;
    public virtual float ReloadDurationSeconds => 0f;
    public virtual int MagazineSize => 0;

    public virtual float PaintRadius => 0f;
    public virtual int PaintPriority => 0;
    public virtual float PaintMarkDamage => 0f;
    public virtual float InfectionDamage => 0f;
    public virtual int PenetrationClassBonus => 0;

    public virtual float MoveSpeed => 0f;
    public virtual float MaxHealth => 0f;
    public virtual int ArmorClass => StatConfig != null ? StatConfig.BaseArmorClass : 0;
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
        CurrentWeapon != null ? CurrentWeapon.ResolveGunshotSoundRadius(0f) : 0f;
    public virtual float SoundInvestigateDelaySeconds => 0f;
    public virtual float FootstepSoundRadius => 0f;
    public virtual float FootstepSoundInterval => 0.35f;

    public virtual float AimSpeed => 8f;
    public virtual float AimRangeMultiplier => 1f;
    public virtual float AimMoveSpeedMultiplier => 1f;
    public virtual float HipFireSpreadAngleDeg => 0f;
    public virtual float AimSpreadAngleDeg => 0f;
    public virtual float RecoilAngleDeg => 0f;
    public virtual float RecoilRecoverySpeedDegPerSecond => 20f;
    public virtual float RecoilForwardDistancePerShot => 0f;
    public virtual float RecoilSideDistancePerShot => 0f;
    public virtual float MaxRecoilDistance => 0f;
    public virtual float RecoilDistanceRecoveryPerSecond => 0f;
    public virtual float HipFireSpreadRadius => 0f;
    public virtual float AimSpreadRadius => 0f;

    public virtual float ResolveAttackDamage(BulletSO bullet)
    {
        return Mathf.Max(0f, AttackDamage);
    }

    public virtual float ResolvePaintRadius(BulletSO bullet)
    {
        return Mathf.Max(0f, PaintRadius);
    }

    public virtual float ResolvePaintMarkDamage(BulletSO bullet)
    {
        return Mathf.Max(0f, PaintMarkDamage);
    }

    public virtual float ResolveInfectionDamage(BulletSO bullet)
    {
        return Mathf.Max(0f, InfectionDamage);
    }

    public virtual float ResolveShotsPerSecond(BulletSO bullet)
    {
        return Mathf.Max(0.01f, ShotsPerSecond);
    }

    public virtual int ResolveMagazineSize(BulletSO bullet)
    {
        return Mathf.Max(1, MagazineSize);
    }

    public virtual int ResolvePenetrationClass(BulletSO bullet)
    {
        int bulletPenetration = bullet != null ? bullet.PenetrationClass : 0;
        return Mathf.Max(0, bulletPenetration + PenetrationClassBonus);
    }

    public virtual float ResolveArmorHealthDurabilityLossMultiplier(BulletSO bullet)
    {
        return ResolveStatWithWeaponAndBulletModifiers(
            PlayerStatId.ArmorHealthDurabilityLossMultiplier,
            ArmorHealthDurabilityLossMultiplier,
            0f,
            bullet);
    }

    public virtual float ResolveArmorInfectionDurabilityLossMultiplier(BulletSO bullet)
    {
        return ResolveStatWithWeaponAndBulletModifiers(
            PlayerStatId.ArmorInfectionDurabilityLossMultiplier,
            ArmorInfectionDurabilityLossMultiplier,
            0f,
            bullet);
    }

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

    protected static void ApplyMatchingModifiers(
        ref StatAccumulator accumulator,
        PlayerStatId stat,
        PlayerStatModifier[] modifiers)
    {
        if (modifiers == null)
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
