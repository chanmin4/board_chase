using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerStatsRuntime : MonoBehaviour
{
    [Header("Upgrade Runtime")]
    [SerializeField] private UpgradeCatalogSO _catalog;
    [SerializeField] private PlayerUpgradeState _upgradeState;

    [Header("Base Weapon")]
    [SerializeField] private VSplatterWeaponHolder _weaponHolder;
    [SerializeField] private WeaponSO _fallbackWeapon;

    [Header("Base Movement")]
    [SerializeField] private HorizontalMoveActionSO _horizontalMoveAction;

    [Header("Base Survival")]
    [SerializeField] private HealthConfigSO _healthConfig;

    [Header("Base Dash / Shockwave")]
    [SerializeField] private VSplatterDashConfigSO _dashConfig;
    [SerializeField] private VSplatterShockwaveConfigSO _shockwaveConfig;

    [Header("Base Occupation")]
    [SerializeField, Range(0f, 1f)] private float _baseOccupationWinThreshold = 0.5f;

    [Header("Broadcasting")]
    [SerializeField] private PlayerStatsChangedEventChannelSO _statsChangedChannel;

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
        if (_upgradeState != null)
            _upgradeState.OnChanged += RebuildAndPublish;

        if (_weaponHolder != null)
            _weaponHolder.OnWeaponChanged += OnWeaponChanged;

        RebuildAndPublish();
    }

    private void OnDisable()
    {
        if (_upgradeState != null)
            _upgradeState.OnChanged -= RebuildAndPublish;

        if (_weaponHolder != null)
            _weaponHolder.OnWeaponChanged -= OnWeaponChanged;
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

        WeaponSO weapon = CurrentWeapon;

        float baseDamage = weapon != null ? weapon.Damage : 0f;
        float baseShotsPerSecond = weapon != null ? weapon.ShotsPerSecond : 1f;
        float baseReloadDuration = weapon != null ? weapon.ReloadDuration : 1f;
        float basePaintRadius = weapon != null ? weapon.PaintRadiusWorld : 1f;
        int baseMagazineSize = weapon != null ? weapon.MagazineSize : 1;

        float reloadSpeed = Resolve(PlayerStatId.ReloadSpeedMultiplier, 1f, 0.01f);

        _current.weapon.attackDamage = Resolve(PlayerStatId.AttackDamage, baseDamage, 0f);
        _current.weapon.namedBossDamageMultiplier = Resolve(PlayerStatId.NamedBossDamageMultiplier, 1f, 0f);
        _current.weapon.shotsPerSecond = Resolve(PlayerStatId.ShotsPerSecond, baseShotsPerSecond, 0.01f);
        _current.weapon.reloadSpeedMultiplier = reloadSpeed;
        _current.weapon.reloadDurationSeconds = Mathf.Max(0.01f, baseReloadDuration / reloadSpeed);
        _current.weapon.magazineSize = Mathf.Max(1, Mathf.RoundToInt(Resolve(PlayerStatId.MagazineSize, baseMagazineSize, 1f)));

        _current.paint.paintRadius = Resolve(PlayerStatId.PaintRadius, basePaintRadius, 0.01f);
        _current.paint.occupationWinThreshold = Mathf.Clamp01(
            Resolve(PlayerStatId.OccupationWinThreshold, _baseOccupationWinThreshold, 0f));

        float baseMoveSpeed = _horizontalMoveAction != null ? _horizontalMoveAction.speed : 8f;
        _current.movement.moveSpeed = Resolve(PlayerStatId.MoveSpeed, baseMoveSpeed, 0f);
        _current.movement.dashDistanceMultiplier = Resolve(PlayerStatId.DashDistanceMultiplier, 1f, 0f);

        float baseMaxHealth = _healthConfig != null ? _healthConfig.InitialHealth : 100f;
        _current.survival.maxHealth = Resolve(PlayerStatId.MaxHealth, baseMaxHealth, 1f);

        float baseShockwaveCooldown = _shockwaveConfig != null ? _shockwaveConfig.CooldownSeconds : 0f;
        _current.shockwave.cooldownSeconds = Resolve(PlayerStatId.ShockwaveCooldownSeconds, baseShockwaveCooldown, 0f);

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
