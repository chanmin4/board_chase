using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class PlayerStatsRuntime : MonoBehaviour
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

    [Header("Base Weapon")]
    [SerializeField] private VSplatterWeaponHolder _weaponHolder;
    [SerializeField] private WeaponSO _fallbackWeapon;

    [Header("Fallback Movement")]
    [SerializeField] private HorizontalMoveActionSO _horizontalMoveAction;

    [Header("Fallback Dash / Shockwave")]
    [SerializeField] private VSplatterDashConfigSO _dashConfig;
    [SerializeField] private VSplatterShockwaveConfigSO _shockwaveConfig;

    [Header("Fallback Occupation")]
    [SerializeField, Range(0f, 1f)] private float _baseOccupationWinThreshold = 0.5f;

    [Header("Broadcasting")]
    [SerializeField] private PlayerStatsChangedEventChannelSO _statsChangedChannel;
    [SerializeField] private PlayerStatsRuntimeReadyEventChannelSO _statsRuntimeReadyChannel;

    [Header("Runtime Snapshot")]
    [SerializeField] private PlayerStatsSnapshot _current;

    private readonly Dictionary<PlayerStatId, StatAccumulator> _accumulators = new();

    public PlayerStatsSnapshot Current => _current;
    public PlayerWeaponStats Weapon => _current.weapon;
    public PlayerPaintStats Paint => _current.paint;
    public PlayerMovementStats Movement => _current.movement;
    public PlayerSurvivalStats Survival => _current.survival;
    public PlayerShockwaveStats Shockwave => _current.shockwave;
    public PlayerFeatureFlags Features => _current.features;

    private WeaponSO CurrentWeapon =>
        _weaponHolder != null && _weaponHolder.CurrentWeapon != null
            ? _weaponHolder.CurrentWeapon
            : _fallbackWeapon;

    private void Reset()
    {
        if (_upgradeState == null)
            _upgradeState = GetComponent<PlayerUpgradeState>();

        if (_weaponHolder == null)
            _weaponHolder = GetComponent<VSplatterWeaponHolder>();
    }

    private void OnEnable()
    {
        if (_loadMetaSaveOnEnable && _saveSystem != null)
            _saveSystem.LoadSaveDataFromDisk();

        if (_upgradeState != null)
            _upgradeState.OnChanged += RebuildAndPublish;

        if (_weaponHolder != null)
            _weaponHolder.OnWeaponChanged += OnWeaponChanged;

        RebuildAndPublish();

        if (_statsRuntimeReadyChannel != null)
            _statsRuntimeReadyChannel.RaiseEvent(this);
    }

    private void OnDisable()
    {
        if (_upgradeState != null)
            _upgradeState.OnChanged -= RebuildAndPublish;

        if (_weaponHolder != null)
            _weaponHolder.OnWeaponChanged -= OnWeaponChanged;

        if (_statsRuntimeReadyChannel != null)
            _statsRuntimeReadyChannel.Clear(this);
    }

    public void RebuildAndPublish()
    {
        Rebuild();
        _statsChangedChannel?.RaiseEvent(_current);
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

        WeaponSO weapon = CurrentWeapon;
        PlayerStatsConfigSO config = _playerStatsConfig;

        float baseDamage = config != null ? config.AttackDamage : 10f;
        float baseRange = config != null ? config.MaxRange : 12f;
        float baseAttackSps = config != null ? config.AttackShotsPerSecond : 2f;
        float basePaintSps = config != null ? config.PaintShotsPerSecond : 1f;
        float baseReloadDuration = config != null ? config.ReloadDurationSeconds : 1.2f;
        float basePaintRadius = config != null ? config.PaintRadius : 1.25f;
        int baseMagazineSize = config != null ? config.MagazineSize : 6;
        int basePaintPriority = config != null ? config.PaintPriority : 0;

        ApplyModifiers(weapon != null ? weapon.StatModifiers : null);

        float reloadSpeed = Resolve(PlayerStatId.ReloadSpeedMultiplier, 1f, 0.01f);

        _current.weapon.attackDamage = Resolve(PlayerStatId.AttackDamage, baseDamage, 0f);
        _current.weapon.namedBossDamageMultiplier = Resolve(PlayerStatId.NamedBossDamageMultiplier, 1f, 0f);
        _current.weapon.maxRange = Resolve(PlayerStatId.MaxRange, baseRange, 0.1f);
        _current.weapon.attackShotsPerSecond = Resolve(PlayerStatId.AttackShotsPerSecond, baseAttackSps, 0.01f);
        _current.weapon.paintShotsPerSecond = Resolve(PlayerStatId.PaintShotsPerSecond, basePaintSps, 0.01f);
        float reloadDuration = Resolve(PlayerStatId.ReloadDurationSeconds, baseReloadDuration, 0.01f);

        _current.weapon.reloadSpeedMultiplier = reloadSpeed;
        _current.weapon.reloadDurationSeconds = Mathf.Max(0.01f, reloadDuration / reloadSpeed);
        _current.weapon.magazineSize = Mathf.Max(1, Mathf.RoundToInt(Resolve(PlayerStatId.MagazineSize, baseMagazineSize, 1f)));

        _current.paint.paintRadius = Resolve(PlayerStatId.PaintRadius, basePaintRadius, 0.01f);
        _current.paint.paintPriority = basePaintPriority;

        float baseOccupationWinThreshold = config != null
            ? config.OccupationWinThreshold
            : _baseOccupationWinThreshold;

        _current.paint.occupationWinThreshold = Mathf.Clamp01(
            Resolve(PlayerStatId.OccupationWinThreshold, baseOccupationWinThreshold, 0f));

        float baseMoveSpeed = config != null
            ? config.MoveSpeed
            : _horizontalMoveAction != null ? _horizontalMoveAction.speed : 8f;

        _current.movement.moveSpeed = Resolve(PlayerStatId.MoveSpeed, baseMoveSpeed, 0f);

        float baseMaxHealth = config != null ? config.MaxHealth : 100f;
        _current.survival.maxHealth = Resolve(PlayerStatId.MaxHealth, baseMaxHealth, 1f);

        float baseShockwaveCooldown = config != null
            ? config.ShockwaveCooldownSeconds
            : _shockwaveConfig != null ? _shockwaveConfig.CooldownSeconds : 0f;

        _current.shockwave.cooldownSeconds = Resolve(
            PlayerStatId.ShockwaveCooldownSeconds,
            baseShockwaveCooldown,
            0f);

        float baseDashCooldown = config != null
            ? config.DashCooldownSeconds
            : _dashConfig != null ? _dashConfig.CooldownSeconds : 0f;

        _current.movement.dashCooldownSeconds = Resolve(
            PlayerStatId.DashCooldownSeconds,
            baseDashCooldown,
            0f);

        _current.movement.dashDistanceMultiplier = Resolve(
            PlayerStatId.DashDistanceMultiplier,
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
        if (_metaUpgradeCatalog == null || _saveSystem == null || _saveSystem.saveData == null)
            return;

        _saveSystem.saveData.EnsureRuntimeDefaults();
        _metaUpgradeCatalog.ApplyMetaModifiers(_saveSystem.saveData.MetaUpgrades, ApplyModifier);
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

    private void ApplyFeatureFlags(PlayerFeatureFlagModifier[] modifiers, ref PlayerFeatureFlags flags)
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

                case PlayerFeatureFlagId.ShockwavePaintsVaccine:
                    flags.shockwavePaintsVaccine = modifier.enabled;
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

    public float ResolveAttackDamage(AttackBulletSO bullet)
    {
        return ResolveWithExtraModifiers(
            PlayerStatId.AttackDamage,
            _current.weapon.attackDamage,
            0f,
            bullet != null ? bullet.StatModifiers : null);
    }

    public float ResolvePaintRadius(BulletSO bullet)
    {
        return ResolveWithExtraModifiers(
            PlayerStatId.PaintRadius,
            _current.paint.paintRadius,
            0.01f,
            bullet != null ? bullet.StatModifiers : null);
    }

    public float ResolveShotsPerSecond(BulletSO bullet)
    {
        bool usesPaintFireRate =
            bullet != null &&
            bullet.AmmoType == BulletAmmoType.Paint;

        PlayerStatId stat = usesPaintFireRate
            ? PlayerStatId.PaintShotsPerSecond
            : PlayerStatId.AttackShotsPerSecond;

        float baseValue = usesPaintFireRate
            ? _current.weapon.paintShotsPerSecond
            : _current.weapon.attackShotsPerSecond;

        return ResolveWithExtraModifiers(
            stat,
            baseValue,
            0.01f,
            bullet != null ? bullet.StatModifiers : null);
    }

    public int ResolveMagazineSize(BulletSO bullet)
    {
        float resolved = ResolveWithExtraModifiers(
            PlayerStatId.MagazineSize,
            _current.weapon.magazineSize,
            1f,
            bullet != null ? bullet.StatModifiers : null);

        return Mathf.Max(1, Mathf.RoundToInt(resolved));
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

            return (baseValue + _flatAdd) * (1f + _percentAdd);
        }
    }
}
