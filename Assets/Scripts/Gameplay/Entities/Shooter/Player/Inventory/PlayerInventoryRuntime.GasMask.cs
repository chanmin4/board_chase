/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player > Inventory
 * Primary responsibility: Owns the player's dedicated head-equipment slot
 * and its runtime gas-mask filter condition. Visual sockets are deliberately
 * not instantiated here; visual authoring belongs to ShooterCustomize.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

public partial class PlayerInventoryRuntime
{
    [Header("Head Equipment / Gas Mask")]
    [Tooltip("Dedicated head-equipment item. It is separate from body armor and is used by room-gas mitigation only.")]
    [SerializeField] private GasMaskItemSO _equippedGasMask;

    [Tooltip("Runtime filter durability. This is consumed only by Biochemical Gas and is saved with the player inventory component.")]
    [ReadOnly] [SerializeField] private float _equippedGasMaskFilterDurability = -1f;

    [Tooltip("Runtime maximum filter durability. Repairs can permanently lower this value without editing the shared GasMaskItemSO.")]
    [ReadOnly] [SerializeField] private float _equippedGasMaskMaxFilterDurability = -1f;

    public event Action<GasMaskItemSO> GasMaskChanged;

    public GasMaskItemSO EquippedGasMask => _equippedGasMask;
    public float EquippedGasMaskFilterDurability => _equippedGasMask != null
        ? Mathf.Clamp(_equippedGasMaskFilterDurability, 0f, EquippedGasMaskMaxFilterDurability)
        : 0f;
    public float EquippedGasMaskMaxFilterDurability => _equippedGasMask != null
        ? Mathf.Clamp(
            _equippedGasMaskMaxFilterDurability >= 0f
                ? _equippedGasMaskMaxFilterDurability
                : _equippedGasMask.MaxFilterDurability,
            0f,
            _equippedGasMask.MaxFilterDurability)
        : 0f;
    public bool HasUsableGasMask => _equippedGasMask != null && EquippedGasMaskFilterDurability > 0f;
    public int GasDefenseLevel => HasUsableGasMask ? _equippedGasMask.GasDefenseLevel : 0;
    public float InfectionCapacityBonus => _equippedGasMask != null ? _equippedGasMask.InfectionCapacityBonus : 0f;

    // Gas defense subtracts pressure levels, never the suppression-failure pressure.
    public float ResolveGasPressureExposure(float pressure, float seconds, float rate)
    {
        if (pressure <= 0f || seconds <= 0f || rate <= 0f) return 0f;
        if (!HasUsableGasMask) return pressure * seconds * rate;
        float wearPerSecond = pressure * rate * _equippedGasMask.FilterDurabilityLossPerRawInfection;
        float protectedSeconds = Mathf.Min(seconds, EquippedGasMaskFilterDurability / wearPerSecond);
        float exposure = Mathf.Max(0f, pressure - GasDefenseLevel) * protectedSeconds * rate +
            pressure * (seconds - protectedSeconds) * rate;
        // Defense reduces new gas exposure only. It never changes accumulated status capacity/stacks.
        _equippedGasMaskFilterDurability = Mathf.Max(0f,
            EquippedGasMaskFilterDurability - protectedSeconds * wearPerSecond);
        NotifyChanged();
        return exposure;
    }

    // Purpose: Equips a gas mask from a normal inventory slot and preserves the old mask as an inventory stack.
    private bool TryEquipGasMaskFromInventory(int index, GasMaskItemSO gasMask, out string message)
    {
        if (gasMask == null)
            return Fail("Gas mask is missing.", out message);

        if (!IsValidIndex(index) || _items[index] == null || _items[index].Item != gasMask)
            return Fail("The gas mask is no longer in that inventory slot.", out message);

        GasMaskItemSO previous = _equippedGasMask;
        float previousDurability = EquippedGasMaskFilterDurability;
        float previousMaxDurability = EquippedGasMaskMaxFilterDurability;
        float incomingDurability = _items[index].GasMaskFilterDurability;
        float incomingMaxDurability = _items[index].GasMaskMaxFilterDurability;

        _items[index].Clear();
        _equippedGasMask = gasMask;
        _equippedGasMaskMaxFilterDurability = Mathf.Clamp(
            incomingMaxDurability >= 0f ? incomingMaxDurability : gasMask.MaxFilterDurability,
            0f,
            gasMask.MaxFilterDurability);
        _equippedGasMaskFilterDurability = Mathf.Clamp(
            incomingDurability >= 0f ? incomingDurability : _equippedGasMaskMaxFilterDurability,
            0f,
            _equippedGasMaskMaxFilterDurability);

        if (previous != null)
        {
            // The source slot is now empty, so swapping head equipment can
            // never fail due to a full inventory.
            _items[index].Set(
                previous,
                1,
                -1f,
                -1f,
                -1f,
                previousDurability,
                -1f,
                previousMaxDurability);
        }

        GasMaskChanged?.Invoke(_equippedGasMask);
        message = previous == null
            ? $"{gasMask.DisplayName} equipped."
            : $"{gasMask.DisplayName} equipped. {previous.DisplayName} returned to inventory.";
        NotifyChanged();
        return true;
    }

    // Purpose: Equips an externally supplied gas mask and returns any old head equipment to inventory first.
    private bool TryEquipExternalGasMask(
        GasMaskItemSO gasMask,
        float filterDurability,
        float maxFilterDurability,
        out string message)
    {
        if (gasMask == null)
            return Fail("Gas mask is missing.", out message);

        GasMaskItemSO previous = _equippedGasMask;
        float previousDurability = EquippedGasMaskFilterDurability;
        float previousMaxDurability = EquippedGasMaskMaxFilterDurability;
        if (previous != null &&
            !TryStoreItem(
                previous,
                1,
                -1f,
                -1f,
                -1f,
                previousDurability,
                -1f,
                previousMaxDurability,
                out message))
            return false;

        _equippedGasMask = gasMask;
        _equippedGasMaskMaxFilterDurability = Mathf.Clamp(
            maxFilterDurability >= 0f
                ? maxFilterDurability
                : gasMask.MaxFilterDurability,
            0f,
            gasMask.MaxFilterDurability);
        _equippedGasMaskFilterDurability = Mathf.Clamp(
            filterDurability >= 0f
                ? filterDurability
                : _equippedGasMaskMaxFilterDurability,
            0f,
            _equippedGasMaskMaxFilterDurability);
        GasMaskChanged?.Invoke(_equippedGasMask);
        message = $"{gasMask.DisplayName} equipped.";
        NotifyChanged();
        return true;
    }

    // Purpose: Unequips the head gas mask into an inventory slot.
    private bool TryUnequipGasMaskToInventorySlot(int targetIndex, out string message)
    {
        if (_equippedGasMask == null)
            return Fail("No gas mask equipped.", out message);

        GasMaskItemSO gasMask = _equippedGasMask;
        float filterDurability = EquippedGasMaskFilterDurability;
        float maxFilterDurability = EquippedGasMaskMaxFilterDurability;
        if (!TryPlaceItemIntoInventorySlot(
                gasMask,
                1,
                targetIndex,
                -1f,
                -1f,
                -1f,
                filterDurability,
                -1f,
                maxFilterDurability,
                out message))
            return false;

        ClearEquippedGasMask();
        message = $"{gasMask.DisplayName} unequipped.";
        NotifyChanged();
        return true;
    }

    // Purpose: Removes head equipment for discard, shop, or world-drop workflows.
    private bool TryRemoveEquippedGasMask(
        out PlayerInventoryRemovedStack removed,
        out string message)
    {
        removed = default;

        if (_equippedGasMask == null)
            return Fail("No gas mask equipped.", out message);

        GasMaskItemSO gasMask = _equippedGasMask;
        float filterDurability = EquippedGasMaskFilterDurability;
        float maxFilterDurability = EquippedGasMaskMaxFilterDurability;
        ClearEquippedGasMask();
        removed = new PlayerInventoryRemovedStack(
            gasMask,
            1,
            -1f,
            -1f,
            -1f,
            filterDurability,
            -1f,
            maxFilterDurability);
        message = $"{gasMask.DisplayName} discarded.";
        NotifyChanged();
        return true;
    }

    // Purpose: Filters personal room-gas Infection through the equipped mask and consumes its filter durability.
    public float ResolveBiochemicalGasExposure(float rawInfection, out float filterDurabilityConsumed)
    {
        filterDurabilityConsumed = 0f;
        float raw = Mathf.Max(0f, rawInfection);

        if (raw <= 0f || !HasUsableGasMask)
            return raw;

        float availableExposure = EquippedGasMaskFilterDurability /
                                  _equippedGasMask.FilterDurabilityLossPerRawInfection;
        float filteredExposure = Mathf.Min(raw, availableExposure);
        filterDurabilityConsumed = filteredExposure *
                                  _equippedGasMask.FilterDurabilityLossPerRawInfection;
        _equippedGasMaskFilterDurability = Mathf.Max(
            0f,
            EquippedGasMaskFilterDurability - filterDurabilityConsumed);

        // Exposure after the filter breaks is unmitigated. This makes a
        // depleted mask visibly remain in the head slot while no longer
        // creating a hidden protection state.
        float remainingRawExposure = raw - filteredExposure;
        float appliedInfection =
            filteredExposure * _equippedGasMask.GasInfectionMultiplier +
            remainingRawExposure;

        if (filterDurabilityConsumed > 0f)
        {
            if (_equippedGasMaskFilterDurability <= 0f)
                Debug.Log($"[PlayerInventoryRuntime] Gas-mask filter depleted. mask={_equippedGasMask.name}", this);

            NotifyChanged();
        }

        return Mathf.Max(0f, appliedInfection);
    }

    // Purpose: Predicts the current per-second room-gas Infection after mask mitigation without consuming filter durability.
    public float PreviewBiochemicalGasExposure(float rawInfectionPerSecond)
    {
        float raw = Mathf.Max(0f, rawInfectionPerSecond);
        return HasUsableGasMask
            ? raw * _equippedGasMask.GasInfectionMultiplier
            : raw;
    }

    // Purpose: Repairs an inventory gas mask to full while reducing only that item's maximum filter durability.
    public bool TryRepairInventoryGasMaskToFull(
        int index,
        out float repairedAmount,
        out string message)
    {
        repairedAmount = 0f;
        if (!EquipmentRepairPolicy.Enabled) return Fail(EquipmentRepairPolicy.DisabledMessage, out message);
        NormalizeStorage();

        if (!IsValidIndex(index))
            return Fail("Invalid inventory slot.", out message);

        PlayerInventoryItemStack stack = _items[index];
        if (stack.Item is not GasMaskItemSO gasMask)
            return Fail("This item is not a gas mask.", out message);

        float oldMax = stack.HasGasMaskCondition
            ? stack.GasMaskMaxFilterDurability
            : gasMask.MaxFilterDurability;
        float oldCurrent = stack.HasGasMaskFilterDurability
            ? Mathf.Clamp(stack.GasMaskFilterDurability, 0f, oldMax)
            : oldMax;

        if (oldCurrent >= oldMax - 0.001f)
            return Fail("Gas mask filter is already fully repaired.", out message);

        float missing = Mathf.Max(0f, oldMax - oldCurrent);
        float newMax = Mathf.Max(
            1f,
            oldMax - (missing * gasMask.RepairPermanentLossRatio));
        newMax = Mathf.Min(newMax, gasMask.MaxFilterDurability);
        stack.Set(gasMask, 1, -1f, -1f, -1f, newMax, -1f, newMax);
        repairedAmount = Mathf.Max(0f, newMax - oldCurrent);
        message = $"{gasMask.DisplayName} filter repaired to {newMax:0.#}/{newMax:0.#}.";
        NotifyChanged();
        return true;
    }

    // Purpose: Repairs the equipped gas mask to full while reducing its runtime maximum filter durability.
    public bool TryRepairEquippedGasMaskToFull(
        out float repairedAmount,
        out string message)
    {
        repairedAmount = 0f;
        if (!EquipmentRepairPolicy.Enabled) return Fail(EquipmentRepairPolicy.DisabledMessage, out message);

        if (_equippedGasMask == null)
            return Fail("No gas mask equipped.", out message);

        float oldMax = EquippedGasMaskMaxFilterDurability;
        float oldCurrent = EquippedGasMaskFilterDurability;
        if (oldCurrent >= oldMax - 0.001f)
            return Fail("Gas mask filter is already fully repaired.", out message);

        float missing = Mathf.Max(0f, oldMax - oldCurrent);
        _equippedGasMaskMaxFilterDurability = Mathf.Max(
            1f,
            oldMax - (missing * _equippedGasMask.RepairPermanentLossRatio));
        _equippedGasMaskMaxFilterDurability = Mathf.Min(
            _equippedGasMaskMaxFilterDurability,
            _equippedGasMask.MaxFilterDurability);
        _equippedGasMaskFilterDurability = _equippedGasMaskMaxFilterDurability;
        repairedAmount = Mathf.Max(0f, _equippedGasMaskMaxFilterDurability - oldCurrent);
        GasMaskChanged?.Invoke(_equippedGasMask);
        message = $"{_equippedGasMask.DisplayName} filter repaired to {_equippedGasMaskMaxFilterDurability:0.#}/{_equippedGasMaskMaxFilterDurability:0.#}.";
        NotifyChanged();
        return true;
    }

    // Purpose: Clears the head-equipment runtime state and notifies its visual/UI observers.
    private void ClearEquippedGasMask()
    {
        _equippedGasMask = null;
        _equippedGasMaskFilterDurability = -1f;
        _equippedGasMaskMaxFilterDurability = -1f;
        GasMaskChanged?.Invoke(null);
    }
}
