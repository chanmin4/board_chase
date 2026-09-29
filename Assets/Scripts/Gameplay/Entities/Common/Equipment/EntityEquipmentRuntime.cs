/*
 * Role hierarchy: Gameplay > Entities > Common > Equipment
 * Primary responsibility: Manages EntityEquipmentRuntime item storage, equipment, transfer state, and related rules.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EntityEquipmentRuntime : MonoBehaviour
{
    private const int DefaultWeaponSlotCount = 2;

    [Header("Starting Loadout")]
    [Tooltip("Initial equipment applied when this runtime wakes up. Character-specific defaults should live here, not on visual controllers.")]
    [SerializeField] private ShooterLoadoutSO _startingLoadout;

    [Header("Weapon")]
    [Tooltip("Number of equipment weapon slots. Slot 0 is the primary weapon slot. Slot 1 is intended for throwable/grenade or a secondary weapon.")]
    [SerializeField, Min(1)] private int _weaponSlotCount = DefaultWeaponSlotCount;
    [SerializeField] private WeaponSO[] _weaponSlots = new WeaponSO[DefaultWeaponSlotCount];
    [SerializeField] private int[] _weaponSlotAmounts = new int[DefaultWeaponSlotCount];
    [Tooltip("Runtime current durability for each ranged-weapon slot. This value moves with the item through inventory and loot transfers.")]
    [ReadOnly] [SerializeField] private float[] _weaponCurrentDurabilities = new float[DefaultWeaponSlotCount];
    [Tooltip("Runtime maximum durability for each ranged-weapon slot. Repairs may permanently lower this value.")]
    [ReadOnly] [SerializeField] private float[] _weaponMaxDurabilities = new float[DefaultWeaponSlotCount];
    [SerializeField, Min(0)] private int _activeWeaponSlotIndex;

    [Header("Armor")]
    [SerializeField] private Transform _armorVisualRoot;

    private ArmorItemSO _currentArmor;
    private GameObject _armorVisualInstance;
    [ReadOnly] [SerializeField] private float _currentArmorDurability;
    [Tooltip("Runtime armor maximum durability. Repairs can permanently lower it without modifying the shared ArmorItemSO.")]
    [ReadOnly] [SerializeField] private float _maxArmorDurability;
    private ShooterLoadoutSnapshot _lastStartingLoadoutSnapshot;
    private bool _hasLastStartingLoadoutSnapshot;

    public event Action OnEquipmentChanged;
    public event Action<WeaponSO> OnWeaponChanged;
    public event Action<int, WeaponSO, int> OnWeaponSlotChanged;
    public event Action<ArmorItemSO> OnArmorChanged;

    public int WeaponSlotCount => Mathf.Max(1, _weaponSlotCount);
    public int ActiveWeaponSlotIndex => Mathf.Clamp(_activeWeaponSlotIndex, 0, WeaponSlotCount - 1);
    public WeaponSO CurrentWeapon => GetWeapon(ActiveWeaponSlotIndex);
    public int CurrentWeaponAmount => GetWeaponAmount(ActiveWeaponSlotIndex);
    public float CurrentWeaponDurability => GetWeaponCurrentDurability(ActiveWeaponSlotIndex);
    public float MaxWeaponDurability => GetWeaponMaxDurability(ActiveWeaponSlotIndex);
    public float CurrentWeaponCondition01 => GetWeaponCondition01(ActiveWeaponSlotIndex);
    public ArmorItemSO CurrentArmor => _currentArmor;
    public bool HasLastStartingLoadoutSnapshot => _hasLastStartingLoadoutSnapshot;

    public Transform ArmorVisualRoot => _armorVisualRoot;
    public float ArmorClass => HasUsableArmor ? _currentArmor.ArmorClass : 0f;
    public float CurrentArmorDurability => _currentArmorDurability;
    public float MaxArmorDurability => _currentArmor != null ? Mathf.Max(0f, _maxArmorDurability) : 0f;
    public bool HasUsableArmor =>
        _currentArmor != null &&
        (MaxArmorDurability <= 0f || _currentArmorDurability > 0f);

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        EnsureWeaponSlots();
        ApplyStartingLoadoutIfNeeded();
    }

    // Purpose: Applies the requested starting loadout change.
    public void ApplyStartingLoadout()
    {
        ApplyStartingLoadout(force: true);
    }

    // Purpose: Applies the requested starting loadout if needed change.
    private void ApplyStartingLoadoutIfNeeded()
    {
        ApplyStartingLoadout(force: false);
    }

    // Purpose: Applies the requested starting loadout change.
    private void ApplyStartingLoadout(bool force)
    {
        if (_startingLoadout == null)
            return;

        ShooterLoadoutSnapshot snapshot = _startingLoadout.Roll();
        _lastStartingLoadoutSnapshot = snapshot;
        _hasLastStartingLoadoutSnapshot = true;

        if (snapshot.weaponStacks != null)
        {
            for (int i = 0; i < snapshot.weaponStacks.Length; i++)
            {
                ShooterLoadoutWeaponStack stack = snapshot.weaponStacks[i];

                if (!stack.HasWeapon)
                    continue;

                if (force || GetWeapon(stack.slotIndex) == null)
                    EquipStartingWeapon(stack.slotIndex, stack.weapon, stack.amount);
            }
        }
        else if (snapshot.weapon != null && (force || CurrentWeapon == null))
        {
            EquipStartingWeapon(0, snapshot.weapon, 1);
        }

        if (snapshot.armor != null && (force || _currentArmor == null))
            EquipArmor(
                snapshot.armor,
                snapshot.armorDurability);
    }

    // Purpose: Attempts to get last starting loadout snapshot and reports whether the request succeeded.
    public bool TryGetLastStartingLoadoutSnapshot(out ShooterLoadoutSnapshot snapshot)
    {
        snapshot = _lastStartingLoadoutSnapshot;
        return _hasLastStartingLoadoutSnapshot;
    }

    // Purpose: Equips weapon for the current owner.
    public void EquipWeapon(WeaponSO weapon)
    {
        EquipWeapon(0, weapon, 1);
    }

    // Purpose: Equips weapon for the current owner.
    public void EquipWeapon(int slotIndex, WeaponSO weapon)
    {
        EquipWeapon(slotIndex, weapon, 1);
    }

    // Purpose: Equips weapon for the current owner.
    public void EquipWeapon(int slotIndex, WeaponSO weapon, int amount)
    {
        EnsureWeaponSlots();
        int resolvedSlotIndex = ResolveWeaponSlotIndex(slotIndex);
        float maxDurability = ResolveFactoryMaxDurability(weapon);
        float currentDurability = _weaponSlots[resolvedSlotIndex] == weapon &&
                                  weapon is RangedWeaponSO
            ? Mathf.Clamp(_weaponCurrentDurabilities[resolvedSlotIndex], 0f, Mathf.Max(1f, _weaponMaxDurabilities[resolvedSlotIndex]))
            : maxDurability;
        float runtimeMaxDurability = _weaponSlots[resolvedSlotIndex] == weapon &&
                                     weapon is RangedWeaponSO &&
                                     _weaponMaxDurabilities[resolvedSlotIndex] > 0f
            ? _weaponMaxDurabilities[resolvedSlotIndex]
            : maxDurability;

        EquipWeapon(slotIndex, weapon, amount, currentDurability, runtimeMaxDurability);
    }

    // Purpose: Equips weapon for the current owner.
    public void EquipWeapon(
        int slotIndex,
        WeaponSO weapon,
        int amount,
        float currentDurability,
        float maxDurability)
    {
        EnsureWeaponSlots();

        int resolvedSlotIndex = ResolveWeaponSlotIndex(slotIndex);
        int resolvedAmount = weapon != null ? Mathf.Max(1, amount) : 0;
        ResolveDurabilityValues(
            weapon,
            currentDurability,
            maxDurability,
            out float resolvedCurrentDurability,
            out float resolvedMaxDurability);

        if (_weaponSlots[resolvedSlotIndex] == weapon &&
            _weaponSlotAmounts[resolvedSlotIndex] == resolvedAmount &&
            Mathf.Approximately(_weaponCurrentDurabilities[resolvedSlotIndex], resolvedCurrentDurability) &&
            Mathf.Approximately(_weaponMaxDurabilities[resolvedSlotIndex], resolvedMaxDurability))
        {
            return;
        }

        _weaponSlots[resolvedSlotIndex] = weapon;
        _weaponSlotAmounts[resolvedSlotIndex] = resolvedAmount;
        _weaponCurrentDurabilities[resolvedSlotIndex] = resolvedCurrentDurability;
        _weaponMaxDurabilities[resolvedSlotIndex] = resolvedMaxDurability;

        OnWeaponSlotChanged?.Invoke(
            resolvedSlotIndex,
            _weaponSlots[resolvedSlotIndex],
            _weaponSlotAmounts[resolvedSlotIndex]);

        if (resolvedSlotIndex == ActiveWeaponSlotIndex)
            OnWeaponChanged?.Invoke(CurrentWeapon);

        OnEquipmentChanged?.Invoke();
    }

    // Purpose: Equips enemy spawn weapon for the current owner.
    public void EquipEnemySpawnWeapon(
        int slotIndex,
        WeaponSO weapon,
        int amount,
        EnemyShooterRole enemyRole)
    {
        ResolveEnemySpawnDurability(
            weapon,
            enemyRole,
            out float currentDurability,
            out float maxDurability);
        EquipWeapon(
            slotIndex,
            weapon,
            amount,
            currentDurability,
            maxDurability);
    }

    // Purpose: Clears the current weapon state.
    public void ClearWeapon()
    {
        ClearWeapon(ActiveWeaponSlotIndex);
    }

    // Purpose: Clears the current weapon state.
    public void ClearWeapon(int slotIndex)
    {
        EnsureWeaponSlots();

        int resolvedSlotIndex = ResolveWeaponSlotIndex(slotIndex);

        if (_weaponSlots[resolvedSlotIndex] == null &&
            _weaponSlotAmounts[resolvedSlotIndex] <= 0)
        {
            return;
        }

        _weaponSlots[resolvedSlotIndex] = null;
        _weaponSlotAmounts[resolvedSlotIndex] = 0;
        _weaponCurrentDurabilities[resolvedSlotIndex] = 0f;
        _weaponMaxDurabilities[resolvedSlotIndex] = 0f;

        OnWeaponSlotChanged?.Invoke(resolvedSlotIndex, null, 0);

        if (resolvedSlotIndex == ActiveWeaponSlotIndex)
            OnWeaponChanged?.Invoke(CurrentWeapon);

        OnEquipmentChanged?.Invoke();
    }

    // Purpose: Updates the active weapon slot state.
    public void SetActiveWeaponSlot(int slotIndex)
    {
        EnsureWeaponSlots();

        int resolvedSlotIndex = ResolveWeaponSlotIndex(slotIndex);

        if (_activeWeaponSlotIndex == resolvedSlotIndex)
            return;

        _activeWeaponSlotIndex = resolvedSlotIndex;
        OnWeaponChanged?.Invoke(CurrentWeapon);
        OnEquipmentChanged?.Invoke();
    }

    // Purpose: Returns the current weapon.
    public WeaponSO GetWeapon(int slotIndex)
    {
        EnsureWeaponSlots();
        return _weaponSlots[ResolveWeaponSlotIndex(slotIndex)];
    }

    // Purpose: Returns the current weapon amount.
    public int GetWeaponAmount(int slotIndex)
    {
        EnsureWeaponSlots();

        int resolvedSlotIndex = ResolveWeaponSlotIndex(slotIndex);
        return _weaponSlots[resolvedSlotIndex] != null
            ? Mathf.Max(1, _weaponSlotAmounts[resolvedSlotIndex])
            : 0;
    }

    // Purpose: Returns the current weapon current durability.
    public float GetWeaponCurrentDurability(int slotIndex)
    {
        EnsureWeaponSlots();
        int resolvedSlotIndex = ResolveWeaponSlotIndex(slotIndex);
        return _weaponSlots[resolvedSlotIndex] is RangedWeaponSO
            ? Mathf.Clamp(
                _weaponCurrentDurabilities[resolvedSlotIndex],
                0f,
                Mathf.Max(0f, _weaponMaxDurabilities[resolvedSlotIndex]))
            : 0f;
    }

    // Purpose: Returns the current weapon max durability.
    public float GetWeaponMaxDurability(int slotIndex)
    {
        EnsureWeaponSlots();
        int resolvedSlotIndex = ResolveWeaponSlotIndex(slotIndex);
        return _weaponSlots[resolvedSlotIndex] is RangedWeaponSO
            ? Mathf.Max(0f, _weaponMaxDurabilities[resolvedSlotIndex])
            : 0f;
    }

    // Purpose: Returns the current weapon condition01.
    public float GetWeaponCondition01(int slotIndex)
    {
        float maximum = GetWeaponMaxDurability(slotIndex);
        return maximum > 0f
            ? Mathf.Clamp01(GetWeaponCurrentDurability(slotIndex) / maximum)
            : 1f;
    }

    // Purpose: Returns the current weapon performance multiplier.
    public float GetWeaponPerformanceMultiplier(int slotIndex)
    {
        // 100% durability keeps authored performance. 50% durability produces
        // 75% damage/accuracy, and 0% reaches the 50% floor but cannot fire.
        return Mathf.Lerp(0.5f, 1f, GetWeaponCondition01(slotIndex));
    }

    public bool CanFireCurrentWeapon =>
        CurrentWeapon is not RangedWeaponSO || CurrentWeaponDurability > 0f;

    // Purpose: Applies the requested current weapon fire wear change.
    public bool ApplyCurrentWeaponFireWear()
    {
        EnsureWeaponSlots();
        int slotIndex = ActiveWeaponSlotIndex;

        if (_weaponSlots[slotIndex] is not RangedWeaponSO rangedWeapon ||
            _weaponCurrentDurabilities[slotIndex] <= 0f)
        {
            return false;
        }

        float previous = _weaponCurrentDurabilities[slotIndex];
        _weaponCurrentDurabilities[slotIndex] = Mathf.Max(
            0f,
            previous - rangedWeapon.DurabilityLossPerFire);

        if (!Mathf.Approximately(previous, _weaponCurrentDurabilities[slotIndex]))
            PublishWeaponSlotChanged(slotIndex);

        return true;
    }

    // Purpose: Repairs or rebuilds weapon to full from the current source data.
    public bool RepairWeaponToFull(int slotIndex)
    {
        EnsureWeaponSlots();
        int resolvedSlotIndex = ResolveWeaponSlotIndex(slotIndex);

        if (_weaponSlots[resolvedSlotIndex] is not RangedWeaponSO rangedWeapon)
            return false;

        float current = GetWeaponCurrentDurability(resolvedSlotIndex);
        float maximum = GetWeaponMaxDurability(resolvedSlotIndex);

        if (maximum <= 0f || current >= maximum)
            return false;

        float missing = maximum - current;
        float repairedMaximum = Mathf.Max(
            1f,
            maximum - missing * rangedWeapon.RepairPermanentLossRatio);
        _weaponMaxDurabilities[resolvedSlotIndex] = repairedMaximum;
        _weaponCurrentDurabilities[resolvedSlotIndex] = repairedMaximum;
        PublishWeaponSlotChanged(resolvedSlotIndex);
        return true;
    }

    // Purpose: Attempts to swap weapon slots and reports whether the request succeeded.
    public bool TrySwapWeaponSlots(int fromSlotIndex, int toSlotIndex)
    {
        EnsureWeaponSlots();

        if (fromSlotIndex < 0 || fromSlotIndex >= WeaponSlotCount ||
            toSlotIndex < 0 || toSlotIndex >= WeaponSlotCount)
        {
            return false;
        }

        if (fromSlotIndex == toSlotIndex)
            return true;

        WeaponSO fromWeapon = _weaponSlots[fromSlotIndex];
        int fromAmount = _weaponSlotAmounts[fromSlotIndex];
        float fromCurrentDurability = _weaponCurrentDurabilities[fromSlotIndex];
        float fromMaxDurability = _weaponMaxDurabilities[fromSlotIndex];

        _weaponSlots[fromSlotIndex] = _weaponSlots[toSlotIndex];
        _weaponSlotAmounts[fromSlotIndex] = _weaponSlotAmounts[toSlotIndex];
        _weaponCurrentDurabilities[fromSlotIndex] = _weaponCurrentDurabilities[toSlotIndex];
        _weaponMaxDurabilities[fromSlotIndex] = _weaponMaxDurabilities[toSlotIndex];
        _weaponSlots[toSlotIndex] = fromWeapon;
        _weaponSlotAmounts[toSlotIndex] = fromAmount;
        _weaponCurrentDurabilities[toSlotIndex] = fromCurrentDurability;
        _weaponMaxDurabilities[toSlotIndex] = fromMaxDurability;

        OnWeaponSlotChanged?.Invoke(
            fromSlotIndex,
            _weaponSlots[fromSlotIndex],
            GetWeaponAmount(fromSlotIndex));

        OnWeaponSlotChanged?.Invoke(
            toSlotIndex,
            _weaponSlots[toSlotIndex],
            GetWeaponAmount(toSlotIndex));

        if (fromSlotIndex == ActiveWeaponSlotIndex ||
            toSlotIndex == ActiveWeaponSlotIndex)
        {
            OnWeaponChanged?.Invoke(CurrentWeapon);
        }

        OnEquipmentChanged?.Invoke();
        return true;
    }

    // Purpose: Attempts to consume weapon stack and reports whether the request succeeded.
    public bool TryConsumeWeaponStack(
        int slotIndex,
        int amount,
        out WeaponSO consumedWeapon,
        out int remainingAmount)
    {
        EnsureWeaponSlots();

        consumedWeapon = null;
        remainingAmount = 0;

        int resolvedSlotIndex = ResolveWeaponSlotIndex(slotIndex);
        WeaponSO weapon = _weaponSlots[resolvedSlotIndex];

        if (weapon == null)
            return false;

        int consumeAmount = Mathf.Max(1, amount);
        int currentAmount = Mathf.Max(1, _weaponSlotAmounts[resolvedSlotIndex]);

        if (currentAmount < consumeAmount)
            return false;

        consumedWeapon = weapon;
        currentAmount -= consumeAmount;
        remainingAmount = currentAmount;

        if (currentAmount <= 0)
            ClearWeapon(resolvedSlotIndex);
        else
            EquipWeapon(resolvedSlotIndex, weapon, currentAmount);

        return true;
    }

    // Purpose: Equips armor for the current owner.
    public void EquipArmor(ArmorItemSO armor)
    {
        EquipArmor(armor, armor != null ? armor.MaxDurability : 0f, armor != null ? armor.MaxDurability : 0f);
    }

    // Purpose: Equips armor for the current owner.
    public void EquipArmor(ArmorItemSO armor, float durability)
    {
        EquipArmor(armor, durability, armor != null ? armor.MaxDurability : 0f);
    }

    // Purpose: Equips armor while preserving the instance-specific current and maximum condition values.
    public void EquipArmor(ArmorItemSO armor, float durability, float maxDurability)
    {
        bool sameArmor = _currentArmor == armor && _armorVisualInstance != null;
        _currentArmor = armor;
        _maxArmorDurability = armor != null
            ? Mathf.Clamp(maxDurability > 0f ? maxDurability : armor.MaxDurability, 0f, armor.MaxDurability)
            : 0f;
        _currentArmorDurability = armor != null
            ? Mathf.Clamp(durability >= 0f ? durability : _maxArmorDurability, 0f, _maxArmorDurability)
            : 0f;

        if (!sameArmor)
            RebuildArmorVisual();

        OnArmorChanged?.Invoke(_currentArmor);
        OnEquipmentChanged?.Invoke();
    }

    // Purpose: Clears the current armor state.
    public void ClearArmor()
    {
        if (_currentArmor == null && _armorVisualInstance == null)
            return;

        _currentArmor = null;
        _currentArmorDurability = 0f;
        _maxArmorDurability = 0f;
        RebuildArmorVisual();
        OnArmorChanged?.Invoke(null);
        OnEquipmentChanged?.Invoke();
    }

    // Purpose: Resolves the effective armor class needed by this component.
    public float ResolveEffectiveArmorClass(float baseArmorClass, float armorClassDelta)
    {
        return Mathf.Max(0f, Mathf.Max(0f, baseArmorClass) + ArmorClass + armorClassDelta);
    }

    // Purpose: Resolves the health damage multiplier needed by this component.
    public float ResolveHealthDamageMultiplier(
        int penetrationClass,
        float baseArmorClass,
        float armorClassDelta)
    {
        if (!HasUsableArmor)
        {
            float effectiveArmorClass = Mathf.Max(
                0f,
                Mathf.Max(0f, baseArmorClass) + armorClassDelta);

            return ArmorPenetrationDamageCurve.RollMultiplier(
                effectiveArmorClass,
                penetrationClass);
        }

        return _currentArmor.ResolveHealthDamageMultiplier(
            penetrationClass,
            baseArmorClass,
            armorClassDelta);
    }



    // Purpose: Resolves the mark damage multiplier needed by this component.
    public float ResolveMarkDamageMultiplier(
        int penetrationClass,
        float baseArmorClass,
        float armorClassDelta)
    {
        if (!HasUsableArmor)
            return 1f;

        return _currentArmor.ResolveMarkDamageMultiplier(
            penetrationClass,
            baseArmorClass,
            armorClassDelta);
    }
    // Purpose: Applies the requested armor absorbed damage change.
    public void ApplyArmorAbsorbedDamage(float absorbedDamage)
    {
        ApplyArmorAbsorbedDamage(absorbedDamage, 0f, 1f, 0f);
    }

    // Purpose: Applies the requested armor absorbed damage change.
    public void ApplyArmorAbsorbedDamage(
        float healthAbsorbedDamage,
        float infectionAbsorbedDamage,
        float healthDurabilityLossMultiplier,
        float infectionDurabilityLossMultiplier)
    {
        if (!HasUsableArmor)
            return;

        if (MaxArmorDurability <= 0f)
            return;

        float durabilityDamage =
            Mathf.Max(0f, healthAbsorbedDamage) * Mathf.Max(0f, healthDurabilityLossMultiplier) +
            Mathf.Max(0f, infectionAbsorbedDamage) * Mathf.Max(0f, infectionDurabilityLossMultiplier);

        if (durabilityDamage <= 0f)
            return;

        float previous = _currentArmorDurability;
        _currentArmorDurability = Mathf.Max(0f, _currentArmorDurability - durabilityDamage);

        if (!Mathf.Approximately(previous, _currentArmorDurability))
        {
            OnArmorChanged?.Invoke(_currentArmor);
            OnEquipmentChanged?.Invoke();
        }
    }

    // Purpose: Repairs or rebuilds armor from the current source data.
    public void RepairArmor(float amount)
    {
        if (_currentArmor == null || MaxArmorDurability <= 0f)
            return;

        float previous = _currentArmorDurability;
        _currentArmorDurability = Mathf.Clamp(
            _currentArmorDurability + Mathf.Max(0f, amount),
            0f,
            MaxArmorDurability);

        if (!Mathf.Approximately(previous, _currentArmorDurability))
        {
            OnArmorChanged?.Invoke(_currentArmor);
            OnEquipmentChanged?.Invoke();
        }
    }

    // Purpose: Repairs armor to its reduced instance maximum while applying permanent maximum-condition loss.
    public void RepairArmorToFull()
    {
        if (_currentArmor == null || MaxArmorDurability <= 0f)
            return;

        float previousCurrent = _currentArmorDurability;
        float previousMax = MaxArmorDurability;
        float missing = Mathf.Max(0f, previousMax - previousCurrent);
        _maxArmorDurability = Mathf.Max(
            1f,
            previousMax - (missing * _currentArmor.RepairPermanentLossRatio));
        _maxArmorDurability = Mathf.Min(_maxArmorDurability, _currentArmor.MaxDurability);
        _currentArmorDurability = _maxArmorDurability;

        if (!Mathf.Approximately(previousCurrent, _currentArmorDurability) ||
            !Mathf.Approximately(previousMax, _maxArmorDurability))
        {
            OnArmorChanged?.Invoke(_currentArmor);
            OnEquipmentChanged?.Invoke();
        }
    }

    // Purpose: Repairs or rebuilds armor visual from the current source data.
    private void RebuildArmorVisual()
    {
        if (_armorVisualInstance != null)
        {
            Destroy(_armorVisualInstance);
            _armorVisualInstance = null;
        }

        if (UsesShooterEquipmentControllerVisual())
            return;

        if (_currentArmor == null ||
            _currentArmor.EquippedVisualPrefab == null ||
            _armorVisualRoot == null)
        {
            return;
        }

        _armorVisualInstance = Instantiate(_currentArmor.EquippedVisualPrefab, _armorVisualRoot);
        _currentArmor.ApplyEquippedVisualTransform(_armorVisualInstance.transform);
    }

    // Purpose: Ensures weapon slots is ready before the workflow continues.
    private void EnsureWeaponSlots()
    {
        _weaponSlotCount = Mathf.Max(1, _weaponSlotCount);

        if (_weaponSlots == null || _weaponSlots.Length != _weaponSlotCount)
            Array.Resize(ref _weaponSlots, _weaponSlotCount);

        if (_weaponSlotAmounts == null || _weaponSlotAmounts.Length != _weaponSlotCount)
            Array.Resize(ref _weaponSlotAmounts, _weaponSlotCount);

        if (_weaponCurrentDurabilities == null || _weaponCurrentDurabilities.Length != _weaponSlotCount)
            Array.Resize(ref _weaponCurrentDurabilities, _weaponSlotCount);

        if (_weaponMaxDurabilities == null || _weaponMaxDurabilities.Length != _weaponSlotCount)
            Array.Resize(ref _weaponMaxDurabilities, _weaponSlotCount);

        for (int i = 0; i < _weaponSlotAmounts.Length; i++)
        {
            if (_weaponSlots[i] == null)
            {
                _weaponSlotAmounts[i] = 0;
                _weaponCurrentDurabilities[i] = 0f;
                _weaponMaxDurabilities[i] = 0f;
            }
            else if (_weaponSlotAmounts[i] <= 0)
                _weaponSlotAmounts[i] = 1;

            if (_weaponSlots[i] is RangedWeaponSO rangedWeapon)
            {
                bool hadNoDurabilityState = _weaponMaxDurabilities[i] <= 0f;

                if (hadNoDurabilityState)
                {
                    _weaponMaxDurabilities[i] = rangedWeapon.MaxDurability;
                    _weaponCurrentDurabilities[i] = _weaponMaxDurabilities[i];
                }

                _weaponCurrentDurabilities[i] = Mathf.Clamp(
                    _weaponCurrentDurabilities[i],
                    0f,
                    _weaponMaxDurabilities[i]);
            }
        }

        _activeWeaponSlotIndex = ResolveWeaponSlotIndex(_activeWeaponSlotIndex);
    }

    // Purpose: Equips starting weapon for the current owner.
    private void EquipStartingWeapon(int slotIndex, WeaponSO weapon, int amount)
    {
        ResolveStartingDurability(weapon, out float currentDurability, out float maxDurability);
        EquipWeapon(slotIndex, weapon, amount, currentDurability, maxDurability);
    }

    // Purpose: Resolves the starting durability needed by this component.
    private void ResolveStartingDurability(
        WeaponSO weapon,
        out float currentDurability,
        out float maxDurability)
    {
        maxDurability = ResolveFactoryMaxDurability(weapon);
        currentDurability = maxDurability;

        if (weapon is not RangedWeaponSO)
            return;

        Damageable damageable = EntityComponentResolver.Resolve<Damageable>(this);
        EnemyShooterConfigSO enemyConfig = damageable != null
            ? damageable.StatConfig as EnemyShooterConfigSO
            : null;

        if (enemyConfig == null)
            return;

        ResolveEnemySpawnDurability(
            weapon,
            enemyConfig.EnemyRole,
            out currentDurability,
            out maxDurability);
    }

    // Purpose: Resolves the enemy spawn durability needed by this component.
    private static void ResolveEnemySpawnDurability(
        WeaponSO weapon,
        EnemyShooterRole enemyRole,
        out float currentDurability,
        out float maxDurability)
    {
        maxDurability = ResolveFactoryMaxDurability(weapon);
        currentDurability = maxDurability;

        if (weapon is not RangedWeaponSO)
            return;

        if (enemyRole == EnemyShooterRole.Boss ||
            enemyRole == EnemyShooterRole.BossGuard)
        {
            maxDurability = 100f;
            currentDurability = 100f;
            return;
        }

        maxDurability = UnityEngine.Random.Range(70f, 95f);
        currentDurability = maxDurability * UnityEngine.Random.Range(0.7f, 1f);
    }

    // Purpose: Resolves the factory max durability needed by this component.
    private static float ResolveFactoryMaxDurability(WeaponSO weapon)
    {
        return weapon is RangedWeaponSO rangedWeapon
            ? rangedWeapon.MaxDurability
            : 0f;
    }

    // Purpose: Resolves the durability values needed by this component.
    private static void ResolveDurabilityValues(
        WeaponSO weapon,
        float currentDurability,
        float maxDurability,
        out float resolvedCurrentDurability,
        out float resolvedMaxDurability)
    {
        if (weapon is not RangedWeaponSO rangedWeapon)
        {
            resolvedCurrentDurability = 0f;
            resolvedMaxDurability = 0f;
            return;
        }

        resolvedMaxDurability = maxDurability > 0f
            ? maxDurability
            : rangedWeapon.MaxDurability;
        resolvedCurrentDurability = Mathf.Clamp(
            currentDurability >= 0f ? currentDurability : resolvedMaxDurability,
            0f,
            resolvedMaxDurability);
    }

    // Purpose: Publishes the weapon slot changed notification.
    private void PublishWeaponSlotChanged(int slotIndex)
    {
        OnWeaponSlotChanged?.Invoke(
            slotIndex,
            _weaponSlots[slotIndex],
            GetWeaponAmount(slotIndex));

        if (slotIndex == ActiveWeaponSlotIndex)
            OnWeaponChanged?.Invoke(CurrentWeapon);

        OnEquipmentChanged?.Invoke();
    }

    // Purpose: Resolves the weapon slot index needed by this component.
    private int ResolveWeaponSlotIndex(int slotIndex)
    {
        return Mathf.Clamp(slotIndex, 0, Mathf.Max(1, _weaponSlotCount) - 1);
    }

    // Purpose: Performs the uses shooter equipment controller visual workflow for this component.
    private bool UsesShooterEquipmentControllerVisual()
    {
        return EntityComponentResolver.Resolve<ShooterEquipmentController>(this) != null;
    }

    // Purpose: Resolves the base armor multiplier needed by this component.
    private static float ResolveBaseArmorMultiplier(
        int penetrationClass,
        int baseArmorClass,
        int armorClassDelta,
        float sameOrHigherMultiplier,
        float perClassGapMultiplier)
    {
        int effectiveArmorClass = Mathf.Max(0, Mathf.Max(0, baseArmorClass) + armorClassDelta);
        int classGap = Mathf.Max(0, effectiveArmorClass - Mathf.Max(0, penetrationClass));

        return Mathf.Max(
            0f,
            Mathf.Max(0f, sameOrHigherMultiplier) *
            Mathf.Pow(Mathf.Clamp01(perClassGapMultiplier), classGap));
    }
}
