/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player
 * Primary responsibility: Coordinates the PlayerStatsRuntime subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class PlayerStatsRuntime : ShooterStatsRuntime
{
    [Header("Player Stats Config")]
    [FormerlySerializedAs("_baseStatsConfig")]
    [SerializeField] private PlayerStatsConfigSO _playerStatsConfig;

    [Header("Persistent Quest Rewards")]
    [Tooltip("Completed quest stat rewards are persistent. The old meta-upgrade currency system is intentionally WIP and does not modify player stats.")]
    [SerializeField] private SaveSystem _saveSystem;

    [Header("Passive Items")]
    [SerializeField] private PlayerPassiveInventoryRuntime _passiveInventory;

    [Header("Equipment")]
    [SerializeField] private EntityEquipmentRuntime _equipmentRuntime;
    [SerializeField] private PlayerBulletLoadoutRuntime _bulletLoadout;
    [SerializeField] private PlayerInventoryRuntime _inventoryRuntime;

    [Header("Health Binding")]
    [SerializeField] private Damageable _damageable;
    [SerializeField] private bool _healToFullOnFirstHealthApply = true;

    [Header("Enemy Bullet Injuries")]
    [Tooltip("Each configured injury is rolled independently after an enemy basic bullet damages this player. Painkiller effects temporarily suppress these rolls.")]
    [SerializeField, Range(0f, 1f)] private float _enemyBulletInjuryChance = 0.02f;
    [SerializeField] private StatusEffectDefinitionSO _visionBlurInjury;
    [SerializeField] private StatusEffectDefinitionSO _brokenLegInjury;
    [SerializeField] private StatusEffectDefinitionSO _brokenArmInjury;
    [SerializeField] private StatusEffectDefinitionSO _deafnessInjury;
    [SerializeField] private StatusEffectDefinitionSO _woundInjury;
    [SerializeField] private StatusEffectDefinitionSO _criticalInjury;

    [Header("Base Weapon")]
    [FormerlySerializedAs("_weaponHolder")]
    [SerializeField] private ShooterEquipmentController _equipmentController;
    [SerializeField] private WeaponSO _fallbackWeapon;

    [Header("Fallback Movement")]
    [SerializeField] private HorizontalMoveActionSO _horizontalMoveAction;

    [Header("Fallback Dash ")]
    [SerializeField] private DashConfigSO _dashConfig;


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

    /// <summary>
    /// Returns the total flat-add contribution granted by completed quests for
    /// one stat. This intentionally excludes weapons, ammunition, equipment,
    /// passives, and temporary status effects so the Stats panel can present
    /// permanent quest progression separately from the current loadout.
    /// </summary>
    public float GetPersistentQuestFlatAdd(PlayerStatId stat)
    {
        return GetPersistentQuestModifierTotal(stat, StatModifierType.FlatAdd);
    }

    /// <summary>
    /// Returns the total PercentAdd contribution granted by completed quests
    /// for one stat. Values use the authored percentage convention: 5 means
    /// +5%, while a reload-duration reward of -5 means +5% reload speed.
    /// </summary>
    public float GetPersistentQuestPercentAdd(PlayerStatId stat)
    {
        return GetPersistentQuestModifierTotal(stat, StatModifierType.PercentAdd);
    }

    public override EntityStatConfigSO StatConfig => _playerStatsConfig;

    public override WeaponSO CurrentWeapon =>
        _equipmentController != null && _equipmentController.CurrentWeapon != null
            ? _equipmentController.CurrentWeapon
            : _fallbackWeapon;

    public override BulletSO CurrentBullet
    {
        get
        {
            if (_bulletLoadout != null &&
                _bulletLoadout.TryGetSelectedBullet(out BulletSO bullet))
            {
                return bullet;
            }

            return null;
        }
    }

    public override float AttackDamage => _current.weapon.attackDamage;
    public float MeleeDamage => Mathf.Max(0f, _current.weapon.meleeDamage);
    public override float MaxRange => _current.weapon.maxRange;
    public override float ShotsPerSecond => _current.weapon.shotsPerSecond;
    public override float ReloadDurationSeconds => _current.weapon.reloadDurationSeconds;
    public override int MagazineSize => _current.weapon.magazineSize;
    public override float PaintMarkDamage => _current.weapon.paintMarkDamage;
    public override float InfectionDamage => _current.weapon.infectionDamage;
    public override int PenetrationClassBonus => _current.weapon.penetrationClass;
    public override float MoveSpeed =>
        ApplyStatusMoveSpeed(_current.movement.moveSpeed) *
        (_inventoryRuntime != null ? _inventoryRuntime.BackpackMoveSpeedMultiplier : 1f);
    public override float DashCooldownSeconds => _current.movement.dashCooldownSeconds;
    public override float DashDistanceMultiplier => _current.movement.dashDistanceMultiplier;
    public override float MaxHealth => _current.survival.maxHealth;
    public override float ArmorClass => _current.survival.armorClass;
    public override float ArmorHealthDurabilityLossMultiplier => _current.survival.armorHealthDurabilityLossMultiplier;
    public override float ArmorInfectionDurabilityLossMultiplier => _current.survival.armorInfectionDurabilityLossMultiplier;
    public override float DodgeChance => _current.survival.dodgeChance;
    public override float VisionRange => _current.vision.visionRange;
    public override float AimSpeed => _current.aim.aimSpeed;
    public override float AimRangeMultiplier => _current.aim.aimRangeMultiplier;
    public override float AimMoveSpeedMultiplier => _current.aim.aimMoveSpeedMultiplier;
    public override float HipFireSpreadAngleDeg => ApplyWeaponConditionToSpread(ApplyStatusSpread(_current.aim.hipFireSpreadAngleDeg));
    public override float AimSpreadAngleDeg => ApplyWeaponConditionToSpread(ApplyStatusSpread(_current.aim.aimSpreadAngleDeg));
    public override float RecoilAngleDeg => _current.aim.recoilAngleDeg;
    public override float RecoilRecoverySpeedDegPerSecond => _current.aim.recoilRecoverySpeedDegPerSecond;
    public override float RecoilForwardDistancePerShot => _current.aim.recoilForwardDistancePerShot;
    public override float RecoilSideDistancePerShot => _current.aim.recoilSideDistancePerShot;
    public override float MaxRecoilDistance => _current.aim.maxRecoilDistance;
    public override float RecoilDistanceRecoveryPerSecond => _current.aim.recoilDistanceRecoveryPerSecond;
    public override float HipFireSpreadRadius => ApplyWeaponConditionToSpread(ApplyStatusSpread(_current.aim.hipFireSpreadRadius));
    public override float AimSpreadRadius => ApplyWeaponConditionToSpread(ApplyStatusSpread(_current.aim.aimSpreadRadius));

    public override float GunshotSoundRadius =>
        CurrentWeapon is RangedWeaponSO rangedWeapon
            ? rangedWeapon.ResolveGunshotSoundRadius(_current.sound.gunshotSoundRadius)
            : _current.sound.gunshotSoundRadius;

    public override float SoundInvestigateDelaySeconds => _current.sound.soundInvestigateDelaySeconds;
    public override float FootstepSoundRadius => _current.sound.footstepSoundRadius;
    public override float FootstepSoundInterval => _current.sound.footstepSoundInterval;
    public override int InventoryCapacity => _playerStatsConfig != null ? _playerStatsConfig.InventoryCapacity : 0;

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

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveRefs();

        if (_equipmentController != null)
            _equipmentController.OnWeaponChanged += OnWeaponChanged;

        if (_passiveInventory != null)
            _passiveInventory.OnChanged += RebuildAndPublish;

        if (_equipmentRuntime != null)
            _equipmentRuntime.OnEquipmentChanged += RebuildAndPublish;

        if (_bulletLoadout != null)
            _bulletLoadout.SelectedBulletChanged += OnSelectedBulletChanged;

        if (_inventoryRuntime != null)
            _inventoryRuntime.BackpackChanged += OnBackpackChanged;

        RebuildAndPublish();

        _statsRuntimeReadyChannel?.RaiseEvent(this);
        _playerRuntimeReadyChannel?.RaiseEvent(transform);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_equipmentController != null)
            _equipmentController.OnWeaponChanged -= OnWeaponChanged;

        if (_passiveInventory != null)
            _passiveInventory.OnChanged -= RebuildAndPublish;

        if (_equipmentRuntime != null)
            _equipmentRuntime.OnEquipmentChanged -= RebuildAndPublish;

        if (_bulletLoadout != null)
            _bulletLoadout.SelectedBulletChanged -= OnSelectedBulletChanged;

        if (_inventoryRuntime != null)
            _inventoryRuntime.BackpackChanged -= OnBackpackChanged;

        _statsRuntimeReadyChannel?.Clear(this);
        _playerRuntimeReadyChannel?.Clear(transform);
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_equipmentController == null)
            _equipmentController = EntityComponentResolver.Resolve<ShooterEquipmentController>(this);

        if (_passiveInventory == null)
            _passiveInventory = GetComponent<PlayerPassiveInventoryRuntime>();

        if (_equipmentRuntime == null)
            _equipmentRuntime = EntityComponentResolver.Resolve<EntityEquipmentRuntime>(this);

        if (_bulletLoadout == null)
            _bulletLoadout = EntityComponentResolver.Resolve<PlayerBulletLoadoutRuntime>(this);

        if (_inventoryRuntime == null)
            _inventoryRuntime = EntityComponentResolver.Resolve<PlayerInventoryRuntime>(this);

        if (_damageable == null)
            _damageable = EntityComponentResolver.Resolve<Damageable>(this);
    }

    // Purpose: Rebuilds movement-derived stats after the player changes equipped backpacks.
    private void OnBackpackChanged(BackpackItemSO _)
    {
        RebuildAndPublish();
    }

    // Purpose: Repairs or rebuilds and publish from the current source data.
    public void RebuildAndPublish()
    {
        Rebuild();
        ApplyHealthStats();
        _statsChangedChannel?.RaiseEvent(_current);
    }

    // Purpose: Applies the requested health stats change.
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

    // Purpose: Repairs or rebuilds rebuild from the current source data.
    private void Rebuild()
    {
        _accumulators.Clear();

        PlayerFeatureFlags flags = default;

        ApplyQuestRewardEffects();
        ApplyPassiveItemEffects(ref flags);
        ApplyEquipmentEffects();

        WeaponSO weapon = CurrentWeapon;
        PlayerStatsConfigSO config = _playerStatsConfig;

        float baseRange = config != null ? config.MaxRange : 12f;
        float baseShotsPerSecond = config != null ? config.ShotsPerSecond : 2f;
        float baseReloadDuration = config != null ? config.ReloadDurationSeconds : 1.2f;
        int baseMagazineSize = config != null ? config.MagazineSize : 6;

        // Resolve the player-owned fallback before gun/bullet modifiers are applied.
        _current.weapon.meleeDamage = Resolve(PlayerStatId.MeleeDamage, config != null ? config.MeleeDamage : 5f, 0f);
        ApplyModifiers(weapon != null ? weapon.StatModifiers : null);

        BulletSO selectedBullet = CurrentBullet;
        _current.weapon.attackDamage = ResolveAttackDamage(selectedBullet);
        _current.weapon.maxRange = Resolve(PlayerStatId.MaxRange, baseRange, 0.1f);
        _current.weapon.shotsPerSecond = ApplyWeaponArchetypeToShotsPerSecond(
            Resolve(PlayerStatId.ShotsPerSecond, baseShotsPerSecond, 0.01f));
        _current.weapon.reloadDurationSeconds = Resolve(PlayerStatId.ReloadDurationSeconds, baseReloadDuration, 0.01f);
        _current.weapon.paintMarkDamage = ResolvePaintMarkDamage(selectedBullet);
        _current.weapon.infectionDamage = ResolveInfectionDamage(selectedBullet);
        _current.weapon.penetrationClass = Mathf.Max(
            0,
            Mathf.RoundToInt(Resolve(
                PlayerStatId.PenetrationClass,
                config != null ? config.PenetrationClassBonus : 0,
                0f)));
        _current.weapon.magazineSize = Mathf.Max(
            1,
            Mathf.RoundToInt(Resolve(PlayerStatId.MagazineSize, baseMagazineSize, 1f)));

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
            0f,
            Resolve(
                PlayerStatId.ArmorClass,
                config != null ? config.BaseArmorClass : 0f,
                0f));

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

    // Purpose: Applies the persistent direct-stat rewards granted by completed quests.
    private void ApplyQuestRewardEffects()
    {
        if (_saveSystem == null || _saveSystem.saveData == null)
            return;

        _saveSystem.saveData.EnsureRuntimeDefaults();
        IReadOnlyList<QuestStatRewardSaveData> rewards = _saveSystem.saveData.QuestStatRewards;

        for (int i = 0; i < rewards.Count; i++)
        {
            QuestStatRewardSaveData reward = rewards[i];
            if (reward == null)
                continue;

            ApplyModifier(new PlayerStatModifier
            {
                stat = reward.stat,
                type = reward.modifierType,
                value = reward.value
            });
        }
    }

    // Purpose: Sums one persistent quest modifier type without mixing in runtime gear or status effects.
    private float GetPersistentQuestModifierTotal(PlayerStatId stat, StatModifierType modifierType)
    {
        if (_saveSystem == null || _saveSystem.saveData == null)
            return 0f;

        _saveSystem.saveData.EnsureRuntimeDefaults();
        IReadOnlyList<QuestStatRewardSaveData> rewards = _saveSystem.saveData.QuestStatRewards;
        float total = 0f;

        for (int i = 0; i < rewards.Count; i++)
        {
            QuestStatRewardSaveData reward = rewards[i];
            if (reward == null || reward.stat != stat || reward.modifierType != modifierType)
                continue;

            total += reward.value;
        }

        return total;
    }

    // Purpose: Applies the requested passive item effects change.
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

    // Purpose: Applies the requested equipment effects change.
    private void ApplyEquipmentEffects()
    {
        if (_equipmentRuntime == null || !_equipmentRuntime.HasUsableArmor || _equipmentRuntime.CurrentArmor == null)
            return;

        ApplyModifiers(_equipmentRuntime.CurrentArmor.StatModifiers);
    }

    // Purpose: Applies the requested modifiers change.
    private void ApplyModifiers(PlayerStatModifier[] modifiers)
    {
        if (modifiers == null)
            return;

        for (int i = 0; i < modifiers.Length; i++)
            ApplyModifier(modifiers[i]);
    }

    // Purpose: Applies the requested modifier change.
    private void ApplyModifier(PlayerStatModifier modifier)
    {
        _accumulators.TryGetValue(modifier.stat, out StatAccumulator accumulator);
        accumulator.Apply(modifier);
        _accumulators[modifier.stat] = accumulator;
    }

    // Purpose: Applies the requested feature flags change.
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

    // Purpose: Resolves the resolve needed by this component.
    private float Resolve(PlayerStatId stat, float baseValue, float minValue)
    {
        if (!_accumulators.TryGetValue(stat, out StatAccumulator accumulator))
            return Mathf.Max(minValue, baseValue);

        return Mathf.Max(minValue, accumulator.Resolve(baseValue));
    }

    // Purpose: Responds to the weapon changed callback.
    private void OnWeaponChanged(WeaponSO weapon)
    {
        RebuildAndPublish();
    }

    // Purpose: Responds to the selected bullet changed callback.
    private void OnSelectedBulletChanged(BulletSO bullet)
    {
        RebuildAndPublish();
    }

    // Purpose: Resolves the attack damage needed by this component.
    public override float ResolveAttackDamage(BulletSO bullet)
    {
        float baseDamage = bullet != null
            ? bullet.HealthDamage * (_playerStatsConfig != null ? _playerStatsConfig.HealthDamageCoefficient : 1f)
            : 0f;

        return ApplyWeaponConditionToDamage(ApplyWeaponArchetypeToDamage(
            ResolveWithExtraModifiers(
                PlayerStatId.AttackDamage,
                Resolve(PlayerStatId.AttackDamage, baseDamage, 0f),
                0f,
                bullet != null ? bullet.StatModifiers : null),
            bullet));
    }

    // Purpose: Resolves the paint mark damage needed by this component.
    public override float ResolvePaintMarkDamage(BulletSO bullet)
    {
        if (!ProjectileCombatFeatureFlags.EnableLegacyBasicBulletMark)
            return 0f;

        float baseAmount = bullet != null
            ? bullet.MarkAmount * (_playerStatsConfig != null ? _playerStatsConfig.MarkAmountCoefficient : 1f)
            : 0f;

        return ResolveWithExtraModifiers(
            PlayerStatId.PaintMarkDamage,
            Resolve(PlayerStatId.PaintMarkDamage, baseAmount, 0f),
            0f,
            bullet != null ? bullet.StatModifiers : null);
    }

    // Purpose: Resolves the infection damage needed by this component.
    public override float ResolveInfectionDamage(BulletSO bullet)
    {
        if (!ProjectileCombatFeatureFlags.EnableLegacyBasicBulletInfectionDamage)
            return 0f;

        float baseDamage = bullet != null
            ? bullet.InfectionDamage * (_playerStatsConfig != null ? _playerStatsConfig.InfectionDamageCoefficient : 1f)
            : 0f;

        return ApplyWeaponConditionToDamage(ApplyWeaponArchetypeToDamage(
            ResolveWithExtraModifiers(
                PlayerStatId.InfectionDamage,
                Resolve(PlayerStatId.InfectionDamage, baseDamage, 0f),
                0f,
                bullet != null ? bullet.StatModifiers : null),
            bullet));
    }

    // Purpose: Resolves the shots per second needed by this component.
    public override float ResolveShotsPerSecond(BulletSO bullet)
    {
        return ResolveWithExtraModifiers(
            PlayerStatId.ShotsPerSecond,
            _current.weapon.shotsPerSecond,
            0.01f,
            bullet != null ? bullet.StatModifiers : null);
    }

    // Purpose: Resolves the magazine size needed by this component.
    public override int ResolveMagazineSize(BulletSO bullet)
    {
        float weaponMagazineSize = CurrentWeapon is RangedWeaponSO rangedWeapon
            ? rangedWeapon.MagazineSize
            : _current.weapon.magazineSize;

        float resolved = ResolveWithExtraModifiers(
            PlayerStatId.MagazineSize,
            weaponMagazineSize,
            1f,
            bullet != null ? bullet.StatModifiers : null);

        return Mathf.Max(1, Mathf.RoundToInt(resolved));
    }

    // Purpose: Resolves the armor health durability loss multiplier needed by this component.
    public override float ResolveArmorHealthDurabilityLossMultiplier(BulletSO bullet)
    {
        return ResolveWithExtraModifiers(
            PlayerStatId.ArmorHealthDurabilityLossMultiplier,
            _current.survival.armorHealthDurabilityLossMultiplier,
            0f,
            bullet != null ? bullet.StatModifiers : null);
    }

    // Purpose: Resolves the armor infection durability loss multiplier needed by this component.
    public override float ResolveArmorInfectionDurabilityLossMultiplier(BulletSO bullet)
    {
        return ResolveWithExtraModifiers(
            PlayerStatId.ArmorInfectionDurabilityLossMultiplier,
            _current.survival.armorInfectionDurabilityLossMultiplier,
            0f,
            bullet != null ? bullet.StatModifiers : null);
    }

    // Purpose: Resolves the with extra modifiers needed by this component.
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

    /// <summary>
    /// Rolls every configured injury separately after a successful enemy
    /// projectile hit. A painkiller status suppresses new rolls only.
    /// </summary>
    public void TryApplyEnemyBulletInjuries()
    {
        EntityStatusEffectRuntime effects = StatusEffects;

        if (effects == null || effects.SuppressesInjuryApplications)
            return;

        TryApplyEnemyBulletInjury(effects, _visionBlurInjury);
        TryApplyEnemyBulletInjury(effects, _brokenLegInjury);
        TryApplyEnemyBulletInjury(effects, _brokenArmInjury);
        TryApplyEnemyBulletInjury(effects, _deafnessInjury);
        TryApplyEnemyBulletInjury(effects, _woundInjury);
        TryApplyEnemyBulletInjury(effects, _criticalInjury);
    }

    /// <summary>
    /// Applies the optional consumable status effect and, for configured
    /// medical items, clears all active injury effects.
    /// </summary>
    public int ApplyConsumableStatusEffect(ConsumableItemSO item)
    {
        if (item == null || StatusEffects == null)
            return 0;

        int removedInjuries = item.CuresInjuries
            ? StatusEffects.RemoveInjuries()
            : 0;

        if (item.AppliedStatusEffect != null)
            StatusEffects.Apply(item.AppliedStatusEffect);

        return removedInjuries;
    }

    // Purpose: Attempts to apply enemy bullet injury and reports whether the request succeeded.
    private void TryApplyEnemyBulletInjury(
        EntityStatusEffectRuntime effects,
        StatusEffectDefinitionSO injury)
    {
        float traitAdjustedChance = Mathf.Clamp01(
            _enemyBulletInjuryChance *
            SectorRoomTraitRuntime.CurrentPlayerInjuryChanceMultiplier);

        if (injury != null && Random.value <= traitAdjustedChance)
            effects.Apply(injury);
    }

    // Purpose: Applies the requested status spread change.
    private float ApplyStatusSpread(float spread)
    {
        float multiplier = StatusEffects != null
            ? StatusEffects.GetMultiplier(StatusEffectModifierType.SpreadMultiplier)
            : 1f;

        return Mathf.Max(0f, spread * multiplier);
    }

    private new struct StatAccumulator
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
