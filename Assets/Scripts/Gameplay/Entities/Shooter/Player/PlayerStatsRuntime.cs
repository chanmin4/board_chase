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
    [Tooltip("Test balance: persistent bleeding roll after a damaging enemy basic bullet. Separate from temporary injuries and painkiller suppression.")]
    [SerializeField, Range(0f, 1f)] private float _enemyBulletBleedingChance = 0.1f;
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

    [SerializeField, HideInInspector] private DashConfigSO _dashConfig; // Serialized WIP compatibility only.


    [Header("Broadcasting")]
    [SerializeField] private PlayerStatsChangedEventChannelSO _statsChangedChannel;
    [SerializeField] private PlayerStatsRuntimeReadyEventChannelSO _statsRuntimeReadyChannel;
    [SerializeField] private PlayerRuntimeReadyEventChannelSO _playerRuntimeReadyChannel;

    [Header("Runtime Snapshot")]
    [SerializeField] private PlayerStatsSnapshot _current;

    private readonly Dictionary<PlayerStatId, StatAccumulator> _accumulators = new();
    private StatAccumulator _passiveAttackDamage;
    private readonly Dictionary<PlayerStatId, StatAccumulator> _withoutPassiveAccumulators = new();
    private readonly Dictionary<PlayerStatId, StatAccumulator> _weaponAccumulators = new();
    private bool _healthAppliedOnce;
    private ShooterMeleeAttack _fixedKnifeAttack;

    public PlayerStatsSnapshot Current => _current;
    public PlayerWeaponStats Weapon => _current.weapon;
    public PlayerPaintStats Paint => _current.paint;
    public PlayerMovementStats Movement => _current.movement;
    public PlayerSurvivalStats Survival => _current.survival;
    public PlayerVisionStats Vision => _current.vision;
    public PlayerAimStats Aim => _current.aim;
    public PlayerSoundStats Sound => _current.sound;
    public PlayerFeatureFlags Features => _current.features;

    public readonly struct WeaponStatBreakdown
    {
        public readonly float weaponValue, passiveDelta, otherDelta, finalValue;

        public WeaponStatBreakdown(float weaponValue, float passiveDelta, float otherDelta, float finalValue)
        {
            this.weaponValue = weaponValue;
            this.passiveDelta = passiveDelta;
            this.otherDelta = otherDelta;
            this.finalValue = finalValue;
        }
    }

    // A shared weapon SO is not an equipped-item identity: verify both owner and active slot.
    public bool IsActiveWeaponSlot(PlayerInventoryRuntime owner, int slotIndex, WeaponSO weapon)
    {
        return owner != null && owner == _inventoryRuntime && _equipmentController != null &&
               slotIndex == _equipmentController.ActiveWeaponSlotIndex &&
               owner.GetEquippedWeapon(slotIndex) == weapon && CurrentWeapon == weapon;
    }

    /// <summary>
    /// Uses the same accumulators and effective getters as combat. Passive is its marginal
    /// contribution with other persistent sources held fixed; the remainder includes quests,
    /// armor, ammunition, condition and status, never mislabeled as a passive bonus.
    /// </summary>
    public bool TryGetWeaponStatBreakdown(PlayerStatId stat, out WeaponStatBreakdown breakdown)
    {
        breakdown = default;
        if (CurrentWeapon is not RangedWeaponSO weapon) return false;

        float baseline, minimum = 0f, effective;
        switch (stat)
        {
            case PlayerStatId.MaxRange: baseline = weapon.MaxRange; minimum = 0.1f; effective = MaxRange; break;
            case PlayerStatId.ShotsPerSecond: baseline = weapon.ShotsPerSecond; minimum = 0.01f; effective = ResolveShotsPerSecond(CurrentBullet); break;
            case PlayerStatId.ReloadDurationSeconds: baseline = weapon.ReloadDurationSeconds; minimum = 0.01f; effective = ReloadDurationSeconds; break;
            case PlayerStatId.AimSpeed: baseline = weapon.AimSpeed; minimum = 0.01f; effective = AimSpeed; break;
            case PlayerStatId.HipFireSpreadAngleDeg: baseline = weapon.HipFireSpreadAngleDeg; effective = HipFireSpreadAngleDeg; break;
            case PlayerStatId.AimSpreadAngleDeg: baseline = weapon.AimSpreadAngleDeg; effective = AimSpreadAngleDeg; break;
            case PlayerStatId.ShotKickDistance: baseline = weapon.ShotKickDistance; effective = ShotKickDistance; break;
            case PlayerStatId.MaximumShotKickDistance: baseline = weapon.MaximumShotKickDistance; effective = MaximumShotKickDistance; break;
            case PlayerStatId.ShotKickRecoveryPerSecond: baseline = weapon.ShotKickRecoveryPerSecond; effective = ShotKickRecoveryPerSecond; break;
            case PlayerStatId.PenetrationClass: baseline = weapon.PenetrationClassBonus; effective = PenetrationClassBonus; break;
            case PlayerStatId.GunshotSoundRadius: baseline = weapon.GunshotSoundRadius; effective = GunshotSoundRadius; break;
            default: return false;
        }

        float weaponValue = ResolveFrom(_weaponAccumulators, stat, baseline, minimum);
        float passiveDelta = Resolve(stat, baseline, minimum) -
                             ResolveFrom(_withoutPassiveAccumulators, stat, baseline, minimum);
        breakdown = new WeaponStatBreakdown(weaponValue, passiveDelta,
            effective - weaponValue - passiveDelta, effective);
        return true;
    }

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
    public float InfectionDefense=>Resolve(PlayerStatId.InfectionDefense,0,0);
    public override float AdditionalStat(PlayerStatId stat,float baseline=0)=>Resolve(stat,baseline,0);
    public override float GasInfectionResistance => Mathf.Clamp(Resolve(PlayerStatId.GasInfectionResistance, 0f, 0f), 0f, 0.9f);
    public float MeleeDamage => ResolveCombatKnifeDamage();

    private float ResolveCombatKnifeDamage()
    {
        if (_fixedKnifeAttack == null)
            _fixedKnifeAttack = EntityComponentResolver.Resolve<ShooterMeleeAttack>(this);
        return _fixedKnifeAttack != null && _fixedKnifeAttack.PlayerCombatKnife != null
            ? _fixedKnifeAttack.PlayerCombatKnife.Damage : 0f;
    }
    public override float MaxRange => _current.weapon.maxRange;
    public override float ShotsPerSecond => _current.weapon.shotsPerSecond;
    public override float ReloadDurationSeconds => _current.weapon.reloadDurationSeconds;
    public override int MagazineSize => base.MagazineSize;
    public override float PaintMarkDamage => _current.weapon.paintMarkDamage;
    public override float InfectionDamage => _current.weapon.infectionDamage;
    public override float PenetrationClassBonus => _current.weapon.penetrationClass;
    public override float MoveSpeed =>
        ApplyStatusMoveSpeed(_current.movement.moveSpeed) *
        (_inventoryRuntime != null
            ? _inventoryRuntime.BackpackMoveSpeedMultiplier * _inventoryRuntime.CarryLoad.MoveSpeedMultiplier
            : 1f);

    private CharacterController _carryMotionController;
    private PlayerCharacter _carryPlayer;

    public override float StaminaRecoveryMultiplier
    {
        get
        {
            if (_inventoryRuntime == null) return 1f;
            PlayerCarryLoad load = _inventoryRuntime.CarryLoad;
            if (!load.RequiresStandingForRecovery) return load.RecoveryMultiplier(false);

            if (_carryMotionController == null)
                _carryMotionController = EntityComponentResolver.Resolve<CharacterController>(this);
            if (_carryPlayer == null)
                _carryPlayer = EntityComponentResolver.Resolve<PlayerCharacter>(this);

            Vector3 velocity = _carryMotionController != null ? _carryMotionController.velocity : Vector3.zero;
            Vector3 input = _carryPlayer != null && !GameplayActionInputBlocker.IsMovementBlocked
                ? _carryPlayer.movementInput : Vector3.zero;
            bool moving = velocity.x * velocity.x + velocity.z * velocity.z > .0025f ||
                input.x * input.x + input.z * input.z > .0025f;
            return load.RecoveryMultiplier(moving);
        }
    }

    public override float DashCooldownSeconds => _current.movement.dashCooldownSeconds;
    public override float DashDistanceMultiplier => _current.movement.dashDistanceMultiplier;
    public override float MaxHealth => _current.survival.maxHealth;
    public override float ArmorClass => _current.survival.armorClass;
    public override float ArmorHealthDurabilityLossMultiplier => _current.survival.armorHealthDurabilityLossMultiplier;
    public override float ArmorInfectionDurabilityLossMultiplier => _current.survival.armorInfectionDurabilityLossMultiplier;
    public override float DodgeChance => _current.survival.dodgeChance;
    public override float VisionRange => _current.vision.visionRange;
    public override float AimSpeed => _current.aim.aimSpeed;
    public override float AimingMoveSpeedMultiplier => _current.aim.aimingMoveSpeedMultiplier;
    public override float ShotKickDistance => _current.aim.shotKickDistance;
    public override float MaximumShotKickDistance => _current.aim.maximumShotKickDistance;
    public override float ShotKickRecoveryPerSecond => _current.aim.shotKickRecoveryPerSecond;
    public override float HipFireSpreadAngleDeg => ApplyWeaponConditionToSpread(ApplyStatusSpread(_current.aim.hipFireSpreadAngleDeg));
    public override float AimSpreadAngleDeg => ApplyWeaponConditionToSpread(ApplyStatusSpread(_current.aim.aimSpreadAngleDeg));

    public override float GunshotSoundRadius => _current.sound.gunshotSoundRadius;

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
        _passiveAttackDamage = default;
        _withoutPassiveAccumulators.Clear();
        _weaponAccumulators.Clear();
        _current = default; // Never retain obsolete fields from a serialized debug snapshot.

        PlayerFeatureFlags flags = default;

        ApplyQuestRewardEffects();
        // Experience/level rewards are retired; permanent growth comes from quest receipts.
        ApplyPassiveItemEffects(ref flags);
        ApplyEquipmentEffects();

        WeaponSO weapon = CurrentWeapon;
        PlayerStatsConfigSO config = _playerStatsConfig;

        float baseRange = base.MaxRange;
        float baseShotsPerSecond = base.ShotsPerSecond;
        float baseReloadDuration = base.ReloadDurationSeconds;

        // 현재 근접 피해 표시는 고정 나이프 SO에 맞춥니다. 이전 주먹/총 휘두르기 스탯은 WIP입니다.
        _current.weapon.meleeDamage = ResolveCombatKnifeDamage();
        ApplyModifiers(weapon != null ? weapon.StatModifiers : null, weaponSource: true);

        BulletSO selectedBullet = CurrentBullet;
        _current.weapon.attackDamage = ResolveAttackDamage(selectedBullet);
        _current.weapon.maxRange = Resolve(PlayerStatId.MaxRange, baseRange, 0.1f);
        _current.weapon.shotsPerSecond = Resolve(PlayerStatId.ShotsPerSecond, baseShotsPerSecond, 0.01f);
        _current.weapon.reloadDurationSeconds = Resolve(PlayerStatId.ReloadDurationSeconds, baseReloadDuration, 0.01f);
        _current.weapon.penetrationClass = Resolve(
            PlayerStatId.PenetrationClass, base.PenetrationClassBonus, float.NegativeInfinity);
        _current.weapon.magazineSize = MagazineSize;

        float baseMoveSpeed = config != null
            ? config.MoveSpeed
            : _horizontalMoveAction != null ? _horizontalMoveAction.speed : 8f;

        _current.movement.moveSpeed = Resolve(PlayerStatId.MoveSpeed, baseMoveSpeed, 0f);

        float baseVisionRange = config != null ? config.VisionRange : 12f;
        _current.vision.visionRange = Resolve(PlayerStatId.VisionRange, baseVisionRange, 0.1f);

        _current.aim.aimSpeed = Resolve(
            PlayerStatId.AimSpeed,
            base.AimSpeed,
            0.01f);
        _current.aim.aimingMoveSpeedMultiplier = Mathf.Clamp01(Resolve(
            PlayerStatId.AimingMoveSpeedMultiplier, base.AimingMoveSpeedMultiplier, 0f));
        _current.aim.shotKickDistance = Resolve(PlayerStatId.ShotKickDistance, base.ShotKickDistance, 0f);
        _current.aim.maximumShotKickDistance = Resolve(PlayerStatId.MaximumShotKickDistance, base.MaximumShotKickDistance, 0f);
        _current.aim.shotKickRecoveryPerSecond = Resolve(PlayerStatId.ShotKickRecoveryPerSecond, base.ShotKickRecoveryPerSecond, 0f);

        _current.aim.aimRangeMultiplier = 1f; // Compatibility snapshots only.
        _current.aim.aimMoveSpeedMultiplier = 1f;

        _current.aim.hipFireSpreadAngleDeg = Resolve(
            PlayerStatId.HipFireSpreadAngleDeg,
            base.HipFireSpreadAngleDeg,
            0f);

        _current.aim.aimSpreadAngleDeg = Resolve(
            PlayerStatId.AimSpreadAngleDeg,
            base.AimSpreadAngleDeg,
            0f);

        _current.sound.gunshotSoundRadius = Resolve(
            PlayerStatId.GunshotSoundRadius,
            base.GunshotSoundRadius,
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
            : 3f;

        _current.movement.dashCooldownSeconds = Resolve(
            PlayerStatId.DashCooldownSeconds,
            baseDashCooldown,
            0f);

        _current.movement.dashDistanceMultiplier = Resolve(
            PlayerStatId.DashDistance,
            config != null ? config.Dash.distanceMultiplier : 1.3f,
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
        if (!ShooterStatPolicy.IsCharacterStat(stat)) return 0f;
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

            if (item.StatModifiers != null)
                foreach (var modifier in item.StatModifiers)
                    if (ShooterStatPolicy.IsPassiveStat(modifier.stat)) ApplyModifier(PassiveModuleRulesSO.Active != null ? PassiveModuleRulesSO.Active.ApplyStrength(item, modifier) : modifier, passiveSource: true);
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
    private void ApplyModifiers(PlayerStatModifier[] modifiers, bool weaponSource = false)
    {
        if (modifiers == null)
            return;

        for (int i = 0; i < modifiers.Length; i++)
            ApplyModifier(modifiers[i], weaponSource: weaponSource);
    }

    // Purpose: Applies the requested modifier change.
    private void ApplyModifier(PlayerStatModifier modifier, bool passiveSource = false, bool weaponSource = false)
    {
        if (!ShooterStatPolicy.IsCharacterStat(modifier.stat)) return;
        Accumulate(_accumulators, modifier);
        if (passiveSource && modifier.stat == PlayerStatId.AttackDamage) _passiveAttackDamage.Apply(modifier);
        if (!passiveSource) Accumulate(_withoutPassiveAccumulators, modifier);
        if (weaponSource) Accumulate(_weaponAccumulators, modifier);
    }

    private static void Accumulate(Dictionary<PlayerStatId, StatAccumulator> target, PlayerStatModifier modifier)
    {
        target.TryGetValue(modifier.stat, out StatAccumulator accumulator);
        accumulator.Apply(modifier);
        target[modifier.stat] = accumulator;
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
        return ResolveFrom(_accumulators, stat, baseValue, minValue);
    }

    private static float ResolveFrom(Dictionary<PlayerStatId, StatAccumulator> source,
        PlayerStatId stat, float baseValue, float minValue)
    {
        if (!source.TryGetValue(stat, out StatAccumulator accumulator))
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
        if (bullet == null) return 0f;
        float baseDamage = (_playerStatsConfig?.BaseRangedAttackDamage ?? 30f) + CombatBalanceRules.AmmoDamageAdjustment(bullet.Tier);
        float playerAndWeapon = ResolveFrom(_withoutPassiveAccumulators, PlayerStatId.AttackDamage, baseDamage, 0f);
        float withAmmo = ResolveWithExtraModifiers(PlayerStatId.AttackDamage, playerAndWeapon, 0f, bullet.StatModifiers);
        float shot = ApplyWeaponConditionToDamage(ApplyWeaponArchetypeToDamage(withAmmo, bullet));
        return Mathf.Max(0f, _passiveAttackDamage.Resolve(shot));
    }

    // Purpose: Resolves the paint mark damage needed by this component.
    public override float ResolvePaintMarkDamage(BulletSO bullet) => 0f; // WIP basic-bullet compatibility only.

    // Purpose: Resolves the infection damage needed by this component.
    public override float ResolveInfectionDamage(BulletSO bullet) => 0f; // WIP basic-bullet compatibility only.

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
        return MagazineSize;
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

        // Bleeding is a separate treatment-only condition; painkillers do not suppress it.
        if (effects != null && Random.value < Mathf.Clamp01(_enemyBulletBleedingChance * SectorRoomTraitRuntime.CurrentPlayerInjuryChanceMultiplier))
        {
            var bleeding = Resources.Load<StatusEffectDefinitionSO>("Bleeding");
            if (bleeding != null) effects.Apply(bleeding);
        }

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

        return removedInjuries + StatusEffects.TreatBleeding(item.BleedingStacksRemoved);
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
