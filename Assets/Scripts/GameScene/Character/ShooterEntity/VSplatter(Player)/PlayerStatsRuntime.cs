using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class PlayerStatsRuntime : ShooterStatsRuntime
{
    [Header("Player Stats Config")]
    [FormerlySerializedAs("_baseStatsConfig")]
    [SerializeField] private PlayerStatsConfigSO _playerStatsConfig;

    [Header("Upgrade Runtime")]
    [SerializeField] private UpgradeCatalogSO _catalog;
    [SerializeField] private PlayerUpgradeState _upgradeState;

    [Header("Meta Progress")]
    [SerializeField] private MetaUpgradeCatalogSO _metaUpgradeCatalog;
    [SerializeField] private SaveSystem _saveSystem;
    [SerializeField] private bool _loadMetaSaveOnEnable = true;

    [Header("Passive Items")]
    [SerializeField] private PlayerPassiveInventoryRuntime _passiveInventory;

    [Header("Equipment")]
    [SerializeField] private EntityEquipmentRuntime _equipmentRuntime;

    [Header("Health Binding")]
    [SerializeField] private Damageable _damageable;
    [SerializeField] private bool _healToFullOnFirstHealthApply = true;

    [Header("Base Weapon")]
    [SerializeField] private EntityWeaponHolder _weaponHolder;
    [SerializeField] private WeaponSO _fallbackWeapon;

    [Header("Fallback Movement")]
    [SerializeField] private HorizontalMoveActionSO _horizontalMoveAction;

    [Header("Fallback Dash ")]
    [SerializeField] private VSplatterDashConfigSO _dashConfig;


    [Header("Fallback Occupation")]
    [SerializeField, Range(0f, 1f)] private float _baseOccupationWinThreshold = 0.5f;

    [Header("Broadcasting")]
    [SerializeField] private PlayerStatsChangedEventChannelSO _statsChangedChannel;
    [SerializeField] private PlayerStatsRuntimeReadyEventChannelSO _statsRuntimeReadyChannel;
    [SerializeField] private PlayerRuntimeReadyEventChannelSO _playerRuntimeReadyChannel;

    [Header("Runtime Snapshot")]
    [SerializeField] private PlayerStatsSnapshot _current;

    private readonly Dictionary<PlayerStatId, StatAccumulator> _accumulators = new();
    private bool _healthAppliedOnce;

    public PlayerStatsSnapshot Current => _current;
    public PlayerWeaponStats Weapon => _current.weapon;
    public PlayerPaintStats Paint => _current.paint;
    public PlayerMovementStats Movement => _current.movement;
    public PlayerSurvivalStats Survival => _current.survival;
    public PlayerVisionStats Vision => _current.vision;
    public PlayerAimStats Aim => _current.aim;
    public PlayerSoundStats Sound => _current.sound;
    public PlayerFeatureFlags Features => _current.features;

    public override EntityStatConfigSO StatConfig => _playerStatsConfig;

    public override WeaponSO CurrentWeapon =>
        _weaponHolder != null && _weaponHolder.CurrentWeapon != null
            ? _weaponHolder.CurrentWeapon
            : _fallbackWeapon;

    public override float AttackDamage => _current.weapon.attackDamage;
    public override float MaxRange => _current.weapon.maxRange;
    public override float ShotsPerSecond => _current.weapon.shotsPerSecond;
    public override float ReloadDurationSeconds => _current.weapon.reloadDurationSeconds;
    public override int MagazineSize => _current.weapon.magazineSize;
    public override float PaintMarkDamage => _current.weapon.paintMarkDamage;
    public override float InfectionDamage => _current.weapon.infectionDamage;
    public override int PenetrationClassBonus => _current.weapon.penetrationClass;
    public override float PaintRadius => _current.paint.paintRadius;
    public override int PaintPriority => _current.paint.paintPriority;
    public override float MoveSpeed => _current.movement.moveSpeed;
    public override float MaxHealth => _current.survival.maxHealth;
    public override int ArmorClass => _current.survival.armorClass;
    public override float ArmorHealthDurabilityLossMultiplier => _current.survival.armorHealthDurabilityLossMultiplier;
    public override float ArmorInfectionDurabilityLossMultiplier => _current.survival.armorInfectionDurabilityLossMultiplier;
    public override float DodgeChance => _current.survival.dodgeChance;
    public override float VisionRange => _current.vision.visionRange;
    public override float AimSpeed => _current.aim.aimSpeed;
    public override float AimRangeMultiplier => _current.aim.aimRangeMultiplier;
    public override float AimMoveSpeedMultiplier => _current.aim.aimMoveSpeedMultiplier;
    public override float HipFireSpreadAngleDeg => _current.aim.hipFireSpreadAngleDeg;
    public override float AimSpreadAngleDeg => _current.aim.aimSpreadAngleDeg;
    public override float RecoilAngleDeg => _current.aim.recoilAngleDeg;
    public override float RecoilRecoverySpeedDegPerSecond => _current.aim.recoilRecoverySpeedDegPerSecond;
    public override float RecoilForwardDistancePerShot => _current.aim.recoilForwardDistancePerShot;
    public override float RecoilSideDistancePerShot => _current.aim.recoilSideDistancePerShot;
    public override float MaxRecoilDistance => _current.aim.maxRecoilDistance;
    public override float RecoilDistanceRecoveryPerSecond => _current.aim.recoilDistanceRecoveryPerSecond;
    public override float HipFireSpreadRadius => _current.aim.hipFireSpreadRadius;
    public override float AimSpreadRadius => _current.aim.aimSpreadRadius;

    public override float GunshotSoundRadius =>
        CurrentWeapon != null
            ? CurrentWeapon.ResolveGunshotSoundRadius(_current.sound.gunshotSoundRadius)
            : _current.sound.gunshotSoundRadius;

    public override float SoundInvestigateDelaySeconds => _current.sound.soundInvestigateDelaySeconds;
    public override float FootstepSoundRadius => _current.sound.footstepSoundRadius;
    public override float FootstepSoundInterval => _current.sound.footstepSoundInterval;

    private void Reset()
    {
        ResolveRefs();
    }

    private void Awake()
    {
        ResolveRefs();
    }

    private void OnEnable()
    {
        ResolveRefs();

        if (_loadMetaSaveOnEnable && _saveSystem != null)
            _saveSystem.LoadSaveDataFromDisk();

        if (_upgradeState != null)
            _upgradeState.OnChanged += RebuildAndPublish;

        if (_weaponHolder != null)
            _weaponHolder.OnWeaponChanged += OnWeaponChanged;

        if (_passiveInventory != null)
            _passiveInventory.OnChanged += RebuildAndPublish;

        if (_equipmentRuntime != null)
            _equipmentRuntime.OnEquipmentChanged += RebuildAndPublish;

        RebuildAndPublish();

        _statsRuntimeReadyChannel?.RaiseEvent(this);
        _playerRuntimeReadyChannel?.RaiseEvent(transform);
    }

    private void OnDisable()
    {
        if (_upgradeState != null)
            _upgradeState.OnChanged -= RebuildAndPublish;

        if (_weaponHolder != null)
            _weaponHolder.OnWeaponChanged -= OnWeaponChanged;

        if (_passiveInventory != null)
            _passiveInventory.OnChanged -= RebuildAndPublish;

        if (_equipmentRuntime != null)
            _equipmentRuntime.OnEquipmentChanged -= RebuildAndPublish;

        _statsRuntimeReadyChannel?.Clear(this);
        _playerRuntimeReadyChannel?.Clear(transform);
    }

    private void ResolveRefs()
    {
        if (_upgradeState == null)
            _upgradeState = GetComponent<PlayerUpgradeState>();

        if (_weaponHolder == null)
            _weaponHolder = GetComponent<EntityWeaponHolder>();

        if (_passiveInventory == null)
            _passiveInventory = GetComponent<PlayerPassiveInventoryRuntime>();

        if (_equipmentRuntime == null)
            _equipmentRuntime = GetComponent<EntityEquipmentRuntime>();

        if (_damageable == null)
            _damageable = GetComponent<Damageable>();
    }

    public void RebuildAndPublish()
    {
        Rebuild();
        ApplyHealthStats();
        _statsChangedChannel?.RaiseEvent(_current);
    }

    private void ApplyHealthStats()
    {
        if (_damageable == null)
            return;

        if (_current.survival.maxHealth <= 0f)
            return;

        bool healToFull =
            !_healthAppliedOnce &&
            _healToFullOnFirstHealthApply;

        _damageable.ApplyMaxHealthFromStats(
            _current.survival.maxHealth,
            healToFull);

        _healthAppliedOnce = true;
    }

    private void Rebuild()
    {
        _accumulators.Clear();

        PlayerFeatureFlags flags = default;

        ApplyTrackEffects(PlayerUpgradeTrack.Removal, ref flags);
        ApplyTrackEffects(PlayerUpgradeTrack.Occupation, ref flags);
        ApplyTrackEffects(PlayerUpgradeTrack.Control, ref flags);
        ApplyBossUpgradeEffects();
        ApplyMetaUpgradeEffects();
        ApplyPassiveItemEffects(ref flags);
        ApplyEquipmentEffects();

        WeaponSO weapon = CurrentWeapon;
        PlayerStatsConfigSO config = _playerStatsConfig;

        float baseDamage = config != null ? config.AttackDamage : 10f;
        float baseRange = config != null ? config.MaxRange : 12f;
        float baseShotsPerSecond = config != null ? config.ShotsPerSecond : 2f;
        float baseReloadDuration = config != null ? config.ReloadDurationSeconds : 1.2f;
        float basePaintRadius = config != null ? config.PaintRadius : 1.25f;
        float basePaintMarkDamage = config != null ? config.PaintMarkDamage : 0f;
        float baseInfectionDamage = config != null ? config.InfectionDamage : 0f;
        int baseMagazineSize = config != null ? config.MagazineSize : 6;
        int basePaintPriority = config != null ? config.PaintPriority : 0;

        ApplyModifiers(weapon != null ? weapon.StatModifiers : null);

        _current.weapon.attackDamage = Resolve(PlayerStatId.AttackDamage, baseDamage, 0f);
        _current.weapon.maxRange = Resolve(PlayerStatId.MaxRange, baseRange, 0.1f);
        _current.weapon.shotsPerSecond = Resolve(PlayerStatId.ShotsPerSecond, baseShotsPerSecond, 0.01f);
        _current.weapon.reloadDurationSeconds = Resolve(PlayerStatId.ReloadDurationSeconds, baseReloadDuration, 0.01f);
        _current.weapon.paintMarkDamage = Resolve(PlayerStatId.PaintMarkDamage, basePaintMarkDamage, 0f);
        _current.weapon.infectionDamage = Resolve(PlayerStatId.InfectionDamage, baseInfectionDamage, 0f);
        _current.weapon.penetrationClass = Mathf.Max(
            0,
            Mathf.RoundToInt(Resolve(
                PlayerStatId.PenetrationClass,
                config != null ? config.PenetrationClassBonus : 0,
                0f)));
        _current.weapon.magazineSize = Mathf.Max(
            1,
            Mathf.RoundToInt(Resolve(PlayerStatId.MagazineSize, baseMagazineSize, 1f)));

        _current.paint.paintRadius = Resolve(PlayerStatId.PaintRadius, basePaintRadius, 0.01f);
        _current.paint.paintPriority = basePaintPriority;

        float baseMoveSpeed = config != null
            ? config.MoveSpeed
            : _horizontalMoveAction != null ? _horizontalMoveAction.speed : 8f;

        _current.movement.moveSpeed = Resolve(PlayerStatId.MoveSpeed, baseMoveSpeed, 0f);

        float baseVisionRange = config != null ? config.VisionRange : 12f;
        _current.vision.visionRange = Resolve(PlayerStatId.VisionRange, baseVisionRange, 0.1f);

        _current.aim.aimSpeed = Resolve(
            PlayerStatId.AimSpeed,
            config != null ? config.AimSpeed : 8f,
            0.01f);

        _current.aim.aimRangeMultiplier = Resolve(
            PlayerStatId.AimRangeMultiplier,
            config != null ? config.AimRangeMultiplier : 1f,
            0.1f);

        _current.aim.aimMoveSpeedMultiplier = Mathf.Clamp(
            Resolve(
                PlayerStatId.AimMoveSpeedMultiplier,
                config != null ? config.AimMoveSpeedMultiplier : 1f,
                0.05f),
            0.05f,
            1f);

        _current.aim.hipFireSpreadAngleDeg = Resolve(
            PlayerStatId.HipFireSpreadAngleDeg,
            config != null ? config.HipFireSpreadAngleDeg : 0f,
            0f);

        _current.aim.aimSpreadAngleDeg = Resolve(
            PlayerStatId.AimSpreadAngleDeg,
            config != null ? config.AimSpreadAngleDeg : 0f,
            0f);

        _current.aim.recoilAngleDeg = Resolve(
            PlayerStatId.RecoilAngleDeg,
            config != null ? config.RecoilAngleDeg : 0f,
            0f);

        _current.aim.recoilRecoverySpeedDegPerSecond = Resolve(
            PlayerStatId.RecoilRecoverySpeedDegPerSecond,
            config != null ? config.RecoilRecoverySpeedDegPerSecond : 20f,
            0f);

        _current.aim.recoilForwardDistancePerShot = Resolve(
            PlayerStatId.RecoilForwardDistancePerShot,
            config != null ? config.RecoilForwardDistancePerShot : 0f,
            0f);

        _current.aim.recoilSideDistancePerShot = Resolve(
            PlayerStatId.RecoilSideDistancePerShot,
            config != null ? config.RecoilSideDistancePerShot : 0f,
            0f);

        _current.aim.maxRecoilDistance = Resolve(
            PlayerStatId.MaxRecoilDistance,
            config != null ? config.MaxRecoilDistance : 0f,
            0f);

        _current.aim.recoilDistanceRecoveryPerSecond = Resolve(
            PlayerStatId.RecoilDistanceRecoveryPerSecond,
            config != null ? config.RecoilDistanceRecoveryPerSecond : 0f,
            0f);

        _current.aim.hipFireSpreadRadius = Resolve(
            PlayerStatId.HipFireSpreadRadius,
            config != null ? config.HipFireSpreadRadius : 0f,
            0f);

        _current.aim.aimSpreadRadius = Resolve(
            PlayerStatId.AimSpreadRadius,
            config != null ? config.AimSpreadRadius : 0f,
            0f);

        _current.sound.gunshotSoundRadius = Resolve(
            PlayerStatId.GunshotSoundRadius,
            config != null ? config.GunshotSoundRadius : 0f,
            0f);

        _current.sound.soundInvestigateDelaySeconds = Resolve(
            PlayerStatId.SoundInvestigateDelaySeconds,
            config != null ? config.SoundInvestigateDelaySeconds : 0f,
            0f);

        _current.sound.footstepSoundRadius = Resolve(
            PlayerStatId.FootstepSoundRadius,
            config != null ? config.FootstepSoundRadius : 0f,
            0f);

        _current.sound.footstepSoundInterval = Resolve(
            PlayerStatId.FootstepSoundInterval,
            config != null ? config.FootstepSoundInterval : 0.35f,
            0.05f);

        float baseMaxHealth = config != null ? config.MaxHealth : 100f;
        _current.survival.maxHealth = Resolve(PlayerStatId.MaxHealth, baseMaxHealth, 1f);
        _current.survival.armorClass = Mathf.Max(
            0,
            Mathf.RoundToInt(Resolve(
                PlayerStatId.ArmorClass,
                config != null ? config.BaseArmorClass : 0,
                0f)));

        _current.survival.armorHealthDurabilityLossMultiplier = Resolve(
            PlayerStatId.ArmorHealthDurabilityLossMultiplier,
            config != null && config.Armor != null ? config.Armor.HealthDurabilityLossMultiplier : 1f,
            0f);

        _current.survival.armorInfectionDurabilityLossMultiplier = Resolve(
            PlayerStatId.ArmorInfectionDurabilityLossMultiplier,
            config != null && config.Armor != null ? config.Armor.InfectionDurabilityLossMultiplier : 0.5f,
            0f);

        _current.survival.dodgeChance = Mathf.Clamp01(
            Resolve(
                PlayerStatId.DodgeChance,
                config != null ? config.DodgeChance : 0f,
                0f));
        float baseDashCooldown = config != null
            ? config.DashCooldownSeconds
            : _dashConfig != null ? _dashConfig.CooldownSeconds : 0f;

        _current.movement.dashCooldownSeconds = Resolve(
            PlayerStatId.DashCooldownSeconds,
            baseDashCooldown,
            0f);

        _current.movement.dashDistanceMultiplier = Resolve(
            PlayerStatId.DashDistance,
            1f,
            0f);

        _current.features = flags;
    }

    private void ApplyTrackEffects(PlayerUpgradeTrack track, ref PlayerFeatureFlags flags)
    {
        if (_catalog == null || _upgradeState == null)
            return;

        int level = _upgradeState.GetTrackLevel(track);

        for (int i = 1; i <= level; i++)
        {
            if (!_catalog.TryGetUpgrade(track, i, out PlayerUpgradeDefinition upgrade))
                continue;

            ApplyModifiers(upgrade.statModifiers);
            ApplyFeatureFlags(upgrade.featureFlags, ref flags);
        }
    }

    private void ApplyBossUpgradeEffects()
    {
        if (_catalog == null || _upgradeState == null)
            return;

        IReadOnlyList<BossUpgradePickRecord> picks = _upgradeState.BossUpgradePicks;

        for (int i = 0; i < picks.Count; i++)
        {
            BossUpgradePickRecord pick = picks[i];

            if (!_catalog.TryGetBossUpgrade(pick.bossUpgradeId, out BossUpgradeDefinition upgrade))
                continue;

            ApplyModifiers(pick.infected ? upgrade.infectedModifiers : upgrade.normalModifiers);
        }
    }

    private void ApplyMetaUpgradeEffects()
    {
        if (_metaUpgradeCatalog == null ||
            _saveSystem == null ||
            _saveSystem.saveData == null)
        {
            return;
        }

        _saveSystem.saveData.EnsureRuntimeDefaults();
        _metaUpgradeCatalog.ApplyMetaModifiers(
            _saveSystem.saveData.MetaUpgrades,
            ApplyModifier);
    }

    private void ApplyPassiveItemEffects(ref PlayerFeatureFlags flags)
    {
        if (_passiveInventory == null)
            return;

        IReadOnlyList<PassiveItemSO> items = _passiveInventory.Items;

        for (int i = 0; i < items.Count; i++)
        {
            PassiveItemSO item = items[i];

            if (item == null)
                continue;

            ApplyModifiers(item.StatModifiers);
            ApplyFeatureFlags(item.FeatureFlags, ref flags);
        }
    }

    private void ApplyEquipmentEffects()
    {
        if (_equipmentRuntime == null || !_equipmentRuntime.HasUsableArmor || _equipmentRuntime.CurrentArmor == null)
            return;

        ApplyModifiers(_equipmentRuntime.CurrentArmor.StatModifiers);
    }

    private void ApplyModifiers(PlayerStatModifier[] modifiers)
    {
        if (modifiers == null)
            return;

        for (int i = 0; i < modifiers.Length; i++)
            ApplyModifier(modifiers[i]);
    }

    private void ApplyModifier(PlayerStatModifier modifier)
    {
        _accumulators.TryGetValue(modifier.stat, out StatAccumulator accumulator);
        accumulator.Apply(modifier);
        _accumulators[modifier.stat] = accumulator;
    }

    private void ApplyFeatureFlags(
        PlayerFeatureFlagModifier[] modifiers,
        ref PlayerFeatureFlags flags)
    {
        if (modifiers == null)
            return;

        for (int i = 0; i < modifiers.Length; i++)
        {
            PlayerFeatureFlagModifier modifier = modifiers[i];

            switch (modifier.flag)
            {
                case PlayerFeatureFlagId.LeaveVaccineOnEnemyKill:
                    flags.leaveVaccineOnEnemyKill = modifier.enabled;
                    break;

                case PlayerFeatureFlagId.PaintBulletLeavesTrail:
                    flags.paintBulletLeavesTrail = modifier.enabled;
                    break;
            }
        }
    }

    private float Resolve(PlayerStatId stat, float baseValue, float minValue)
    {
        if (!_accumulators.TryGetValue(stat, out StatAccumulator accumulator))
            return Mathf.Max(minValue, baseValue);

        return Mathf.Max(minValue, accumulator.Resolve(baseValue));
    }

    private void OnWeaponChanged(WeaponSO weapon)
    {
        RebuildAndPublish();
    }

    public override float ResolveAttackDamage(BulletSO bullet)
    {
        return ResolveWithExtraModifiers(
            PlayerStatId.AttackDamage,
            _current.weapon.attackDamage,
            0f,
            bullet != null ? bullet.StatModifiers : null);
    }

    public override float ResolvePaintRadius(BulletSO bullet)
    {
        return ResolveWithExtraModifiers(
            PlayerStatId.PaintRadius,
            _current.paint.paintRadius,
            0.01f,
            bullet != null ? bullet.StatModifiers : null);
    }

    public override float ResolvePaintMarkDamage(BulletSO bullet)
    {
        return ResolveWithExtraModifiers(
            PlayerStatId.PaintMarkDamage,
            _current.weapon.paintMarkDamage,
            0f,
            bullet != null ? bullet.StatModifiers : null);
    }

    public override float ResolveInfectionDamage(BulletSO bullet)
    {
        return ResolveWithExtraModifiers(
            PlayerStatId.InfectionDamage,
            _current.weapon.infectionDamage,
            0f,
            bullet != null ? bullet.StatModifiers : null);
    }

    public override float ResolveShotsPerSecond(BulletSO bullet)
    {
        return ResolveWithExtraModifiers(
            PlayerStatId.ShotsPerSecond,
            _current.weapon.shotsPerSecond,
            0.01f,
            bullet != null ? bullet.StatModifiers : null);
    }

    public override int ResolveMagazineSize(BulletSO bullet)
    {
        float resolved = ResolveWithExtraModifiers(
            PlayerStatId.MagazineSize,
            _current.weapon.magazineSize,
            1f,
            bullet != null ? bullet.StatModifiers : null);

        return Mathf.Max(1, Mathf.RoundToInt(resolved));
    }

    public override float ResolveArmorHealthDurabilityLossMultiplier(BulletSO bullet)
    {
        return ResolveWithExtraModifiers(
            PlayerStatId.ArmorHealthDurabilityLossMultiplier,
            _current.survival.armorHealthDurabilityLossMultiplier,
            0f,
            bullet != null ? bullet.StatModifiers : null);
    }

    public override float ResolveArmorInfectionDurabilityLossMultiplier(BulletSO bullet)
    {
        return ResolveWithExtraModifiers(
            PlayerStatId.ArmorInfectionDurabilityLossMultiplier,
            _current.survival.armorInfectionDurabilityLossMultiplier,
            0f,
            bullet != null ? bullet.StatModifiers : null);
    }

    private float ResolveWithExtraModifiers(
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

    private struct StatAccumulator
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
