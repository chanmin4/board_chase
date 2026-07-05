using UnityEngine;

[DisallowMultipleComponent]
public class EnemyShooterStatsRuntime : ShooterStatsRuntime
{
    [Header("Config")]
    [SerializeField] private EnemyShooterConfigSO _config;

    [Header("Refs")]
    [SerializeField] private Damageable _damageable;
    [SerializeField] private EntityWeaponHolder _weaponHolder;
    [SerializeField] private EntityEquipmentRuntime _equipmentRuntime;

    private BulletSO _runtimeBullet;

    public EnemyShooterConfigSO Config => ResolveConfig();
    public override EntityStatConfigSO StatConfig => Config;
    public override WeaponSO CurrentWeapon =>
        _weaponHolder != null ? _weaponHolder.CurrentWeapon : null;
    public override BulletSO CurrentBullet => _runtimeBullet != null ? _runtimeBullet : Config != null ? Config.Bullet : null;
    public override PaintChannel PaintChannel => Config != null ? Config.PaintChannel : PaintChannel.Virus;
    public override float AttackDamage => ResolveWithRuntimeModifiers(PlayerStatId.AttackDamage, Config != null ? Config.Damage : 0f, 0f);
    public override float MaxRange => ResolveWithRuntimeModifiers(PlayerStatId.MaxRange, Config != null ? Config.MaxRange : 0f, 0.1f);
    public override float ShotsPerSecond => ResolveWithRuntimeModifiers(PlayerStatId.ShotsPerSecond, Config != null ? Config.ShotsPerSecond : 0f, 0.01f);
    public override float ReloadDurationSeconds => ResolveWithRuntimeModifiers(PlayerStatId.ReloadDurationSeconds, Config != null ? Config.ReloadDurationSeconds : 0f, 0f);
    public override int MagazineSize => Mathf.Max(1, Mathf.RoundToInt(ResolveWithRuntimeModifiers(PlayerStatId.MagazineSize, Config != null ? Config.MagazineSize : 1f, 1f)));
    public override float PaintRadius => ResolveWithRuntimeModifiers(PlayerStatId.PaintRadius, Config != null ? Config.PaintRadius : 0f, 0f);
    public override int PaintPriority => Config != null ? Config.PaintPriority : 0;
    public override float PaintMarkDamage => ResolveWithRuntimeModifiers(PlayerStatId.PaintMarkDamage, Config != null ? Config.PaintMarkDamage : 0f, 0f);
    public override float InfectionDamage => ResolveWithRuntimeModifiers(PlayerStatId.InfectionDamage, Config != null ? Config.InfectionDamage : 0f, 0f);
    public override int PenetrationClassBonus => Mathf.Max(
        0,
        Mathf.RoundToInt(ResolveWithRuntimeModifiers(
            PlayerStatId.PenetrationClass,
            Config != null ? Config.PenetrationClassBonus : 0,
            0f)));
    public override float MoveSpeed => ResolveWithRuntimeModifiers(PlayerStatId.MoveSpeed, Config != null ? Config.MoveSpeed : 0f, 0f);
    public override float MaxHealth => ResolveWithRuntimeModifiers(PlayerStatId.MaxHealth, Config != null ? Config.MaxHealth : 0f, 1f);
    public override int ArmorClass => Mathf.Max(
        0,
        Mathf.RoundToInt(ResolveWithRuntimeModifiers(
            PlayerStatId.ArmorClass,
            Config != null ? Config.BaseArmorClass : 0,
            0f)));
    public override float ArmorHealthDurabilityLossMultiplier => ResolveWithRuntimeModifiers(
        PlayerStatId.ArmorHealthDurabilityLossMultiplier,
        Config != null && Config.Armor != null ? Config.Armor.HealthDurabilityLossMultiplier : 1f,
        0f);

    public override float ArmorInfectionDurabilityLossMultiplier => ResolveWithRuntimeModifiers(
        PlayerStatId.ArmorInfectionDurabilityLossMultiplier,
        Config != null && Config.Armor != null ? Config.Armor.InfectionDurabilityLossMultiplier : 0.5f,
        0f);

    public override float VisionRange => ResolveWithRuntimeModifiers(PlayerStatId.VisionRange, Config != null ? Config.Vision.VisionRange : 0f, 0f);
    public override float GunshotSoundRadius =>
        CurrentWeapon != null
            ? CurrentWeapon.ResolveGunshotSoundRadius(Config != null ? Config.GunshotSoundRadius : 0f)
            : Config != null ? Config.GunshotSoundRadius : 0f;
    public override float SoundInvestigateDelaySeconds =>
        Config != null ? Config.SoundInvestigateDelaySeconds : 0f;
    public override float FootstepSoundRadius =>
        Config != null ? Config.FootstepSoundRadius : 0f;
    public override float FootstepSoundInterval =>
        Config != null ? Config.FootstepSoundInterval : 0.35f;
    public override float RecoilForwardDistancePerShot =>
        Config != null ? Config.RecoilForwardDistancePerShot : 0f;
    public override float RecoilSideDistancePerShot =>
        Config != null ? Config.RecoilSideDistancePerShot : 0f;
    public override float MaxRecoilDistance =>
        Config != null ? Config.MaxRecoilDistance : 0f;
    public override float RecoilDistanceRecoveryPerSecond =>
        Config != null ? Config.RecoilDistanceRecoveryPerSecond : 0f;
    public override float HipFireSpreadRadius =>
        Config != null ? Config.HipFireSpreadRadius : 0f;
    public override float AimSpreadRadius =>
        Config != null ? Config.AimSpreadRadius : 0f;

    private void Reset()
    {
        ResolveRefs();
    }

    private void Awake()
    {
        ResolveRefs();
    }

    public void SetConfig(EnemyShooterConfigSO config)
    {
        _config = config;
    }

    public void SetRuntimeBullet(BulletSO bullet)
    {
        _runtimeBullet = bullet;
    }

    public override float ResolveAttackDamage(BulletSO bullet)
    {
        return ResolveWithExtraModifiers(PlayerStatId.AttackDamage, AttackDamage, 0f, bullet != null ? bullet.StatModifiers : null);
    }

    public override float ResolvePaintRadius(BulletSO bullet)
    {
        return ResolveWithExtraModifiers(PlayerStatId.PaintRadius, PaintRadius, 0f, bullet != null ? bullet.StatModifiers : null);
    }

    public override float ResolvePaintMarkDamage(BulletSO bullet)
    {
        return ResolveWithExtraModifiers(PlayerStatId.PaintMarkDamage, PaintMarkDamage, 0f, bullet != null ? bullet.StatModifiers : null);
    }

    public override float ResolveInfectionDamage(BulletSO bullet)
    {
        return ResolveWithExtraModifiers(PlayerStatId.InfectionDamage, InfectionDamage, 0f, bullet != null ? bullet.StatModifiers : null);
    }

    public override float ResolveShotsPerSecond(BulletSO bullet)
    {
        return ResolveWithExtraModifiers(PlayerStatId.ShotsPerSecond, ShotsPerSecond, 0.01f, bullet != null ? bullet.StatModifiers : null);
    }

    public override int ResolveMagazineSize(BulletSO bullet)
    {
        float resolved = ResolveWithExtraModifiers(PlayerStatId.MagazineSize, MagazineSize, 1f, bullet != null ? bullet.StatModifiers : null);
        return Mathf.Max(1, Mathf.RoundToInt(resolved));
    }

    public override float ResolveArmorHealthDurabilityLossMultiplier(BulletSO bullet)
    {
        return ResolveWithExtraModifiers(
            PlayerStatId.ArmorHealthDurabilityLossMultiplier,
            ArmorHealthDurabilityLossMultiplier,
            0f,
            bullet != null ? bullet.StatModifiers : null);
    }

    public override float ResolveArmorInfectionDurabilityLossMultiplier(BulletSO bullet)
    {
        return ResolveWithExtraModifiers(
            PlayerStatId.ArmorInfectionDurabilityLossMultiplier,
            ArmorInfectionDurabilityLossMultiplier,
            0f,
            bullet != null ? bullet.StatModifiers : null);
    }

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

    private void ResolveRefs()
    {
        if (_damageable == null)
            _damageable = GetComponent<Damageable>() ?? GetComponentInParent<Damageable>();

        if (_weaponHolder == null)
            _weaponHolder = GetComponent<EntityWeaponHolder>() ??
                            GetComponentInChildren<EntityWeaponHolder>(true) ??
                            GetComponentInParent<EntityWeaponHolder>();

        if (_equipmentRuntime == null)
            _equipmentRuntime = GetComponent<EntityEquipmentRuntime>() ??
                                GetComponentInChildren<EntityEquipmentRuntime>(true) ??
                                GetComponentInParent<EntityEquipmentRuntime>();
    }

    private float ResolveWithRuntimeModifiers(PlayerStatId stat, float baseValue, float minValue)
    {
        StatAccumulator accumulator = default;
        ApplyMatchingModifiers(ref accumulator, stat, CurrentWeapon != null ? CurrentWeapon.StatModifiers : null);
        ApplyMatchingModifiers(ref accumulator, stat, _equipmentRuntime != null && _equipmentRuntime.HasUsableArmor && _equipmentRuntime.CurrentArmor != null
            ? _equipmentRuntime.CurrentArmor.StatModifiers
            : null);

        return Mathf.Max(minValue, accumulator.Resolve(baseValue));
    }

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

    private static void ApplyMatchingModifiers(
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

    private struct StatAccumulator
    {
        private float _flatAdd;
        private float _percentAdd;
        private bool _hasOverride;
        private float _overrideValue;
        private bool _hasPercentMultiply;
        private float _percentMultiply;
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
