/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player > Passive
 * Primary responsibility: Owns the player's equipped passive slots separately
 * from backpack ownership, and exposes only equipped passives to stat runtime.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class PlayerPassiveInventoryRuntime : MonoBehaviour
{
    public const int MinimumUnlockedSlotCount = 2;
    public const int MaximumSlotCount = 5;

    private readonly List<PassiveItemSO> _equippedItems = new(MaximumSlotCount);

    [Header("Passive Slots")]
    [Tooltip("The player starts with two passive slots. Progression can unlock up to five.")]
    [SerializeField, Range(MinimumUnlockedSlotCount, MaximumSlotCount)]
    private int _unlockedSlotCount = MinimumUnlockedSlotCount;

    [Tooltip("Every equip, replacement, or removal starts this shared cooldown to prevent passive swapping exploits.")]
    [SerializeField, Min(0f)] private float _changeCooldownSeconds = 10f;

    [SerializeField] private List<PassiveItemSO> _equippedSlots = new(MaximumSlotCount);
    [SerializeField, ReadOnly] private float _nextChangeAllowedAt;

    [Header("Rules")]
    [Tooltip("Debug override. If true, locally stored passives can be duplicated regardless of the PassiveItemSO pickup rule.")]
    [FormerlySerializedAs("_allowDuplicates")]
    [SerializeField] private bool _forceAllowDuplicates;

    [Header("Inventory Source")]
    [Tooltip("When enabled, a passive must remain in PlayerInventoryRuntime to stay equipped. Backpack ownership alone does not apply its effects.")]
    [SerializeField] private PlayerInventoryRuntime _inventoryRuntime;
    [SerializeField] private bool _useInventoryRuntime = true;

    [Header("Legacy Local Ownership")]
    [Tooltip("Fallback ownership only for scenes that do not use PlayerInventoryRuntime. Effects still require an equipped slot.")]
    [SerializeField] private List<PassiveItemSO> _items = new();

    public event Action OnChanged;

    /// <summary>
    /// The stat system consumes this list. Backpack ownership is deliberately
    /// excluded so an unrelated inventory update cannot trigger a passive
    /// stat rebuild.
    /// </summary>
    public IReadOnlyList<PassiveItemSO> Items => _equippedItems;
    public IReadOnlyList<PassiveItemSO> EquippedSlots => _equippedSlots;
    public int UnlockedSlotCount => Mathf.Clamp(
        _unlockedSlotCount,
        MinimumUnlockedSlotCount,
        MaximumSlotCount);
    public int MaxSlotCount => MaximumSlotCount;
    public float ChangeCooldownSeconds => Mathf.Max(0f, _changeCooldownSeconds);
    public float ChangeCooldownRemaining => Mathf.Max(0f, _nextChangeAllowedAt - Time.time);
    public bool IsChangeOnCooldown => ChangeCooldownRemaining > 0f;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        ResolveRefs();
        EnsureSlotStorage();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
        EnsureSlotStorage();
        RebuildEquippedItems();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveRefs();
        EnsureSlotStorage();

        if (_inventoryRuntime != null)
            _inventoryRuntime.OnChanged += HandleInventoryChanged;

        RebuildEquippedItems();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_inventoryRuntime != null)
            _inventoryRuntime.OnChanged -= HandleInventoryChanged;
    }

    // Purpose: Checks whether slot unlocked is currently true.
    public bool IsSlotUnlocked(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < UnlockedSlotCount;
    }

    // Purpose: Attempts to get equipped passive and reports whether the request succeeded.
    public bool TryGetEquippedPassive(int slotIndex, out PassiveItemSO item)
    {
        EnsureSlotStorage();
        item = null;

        if (slotIndex < 0 || slotIndex >= MaximumSlotCount)
            return false;

        item = _equippedSlots[slotIndex];
        return item != null;
    }

    /// <summary>
    /// Copies the current passive slot layout. Ownership remains in the normal
    /// inventory and is restored there first by PlayerRunStatePersistence.
    /// </summary>
    public void CaptureRunState(PlayerRunStateSaveData target)
    {
        if (target == null)
            return;

        EnsureSlotStorage();
        target.unlockedPassiveSlotCount = UnlockedSlotCount;
        target.equippedPassiveGuids.Clear();

        for (int index = 0; index < MaximumSlotCount; index++)
        {
            PassiveItemSO passive = _equippedSlots[index];
            target.equippedPassiveGuids.Add(passive != null ? passive.Guid : string.Empty);
        }
    }

    /// <summary>
    /// Restores slot assignments only when the referenced passives are still
    /// present in the restored inventory. This avoids applying a stat effect
    /// for an item whose save reference was removed by content changes.
    /// </summary>
    public bool TryRestoreRunState(
        PlayerRunStateSaveData source,
        RuntimeContentCatalogSO catalog,
        out string message)
    {
        message = string.Empty;

        if (source == null || !source.hasSnapshot)
        {
            message = "No resumable player run exists.";
            return false;
        }

        if (catalog == null)
        {
            message = "Runtime content catalog is missing.";
            return false;
        }

        for (int index = 0; index < source.equippedPassiveGuids.Count; index++)
        {
            string guid = source.equippedPassiveGuids[index];
            if (string.IsNullOrWhiteSpace(guid))
                continue;

            if (!catalog.TryResolveItem(guid, out ItemSO item) || item is not PassiveItemSO)
            {
                message = $"Saved passive cannot be resolved. guid={guid}";
                return false;
            }
        }

        EnsureSlotStorage();
        _unlockedSlotCount = Mathf.Clamp(
            source.unlockedPassiveSlotCount,
            MinimumUnlockedSlotCount,
            MaximumSlotCount);

        for (int index = 0; index < MaximumSlotCount; index++)
        {
            PassiveItemSO restored = null;
            if (index < _unlockedSlotCount && index < source.equippedPassiveGuids.Count)
            {
                string guid = source.equippedPassiveGuids[index];
                if (!string.IsNullOrWhiteSpace(guid))
                {
                    catalog.TryResolveItem(guid, out ItemSO item);
                    restored = item as PassiveItemSO;
                }
            }

            _equippedSlots[index] = restored;
        }

        PruneSlotsWithoutOwnedCopies();
        RebuildEquippedItems();
        OnChanged?.Invoke();
        message = "Restored passive slot assignments.";
        return true;
    }

    // Purpose: Attempts to equip from inventory and reports whether the request succeeded.
    public bool TryEquipFromInventory(
        int inventoryIndex,
        int targetSlotIndex,
        out string message)
    {
        ResolveRefs();

        if (_inventoryRuntime == null ||
            inventoryIndex < 0 ||
            inventoryIndex >= _inventoryRuntime.Items.Count)
        {
            return Fail("Invalid inventory slot.", out message);
        }

        PlayerInventoryItemStack stack = _inventoryRuntime.Items[inventoryIndex];

        if (stack == null ||
            stack.IsEmpty ||
            stack.Item is not PassiveItemSO passive)
        {
            return Fail("Only passive items can be equipped in passive slots.", out message);
        }

        return TryEquip(passive, targetSlotIndex, out message);
    }

    // Purpose: Attempts to equip first available and reports whether the request succeeded.
    public bool TryEquipFirstAvailable(PassiveItemSO item, out string message)
    {
        EnsureSlotStorage();

        if (item == null)
            return Fail("Passive item is missing.", out message);

        for (int i = 0; i < UnlockedSlotCount; i++)
        {
            if (_equippedSlots[i] == null)
                return TryEquip(item, i, out message);
        }

        return Fail("All passive slots are occupied. Replace a passive slot directly.", out message);
    }

    // Purpose: Attempts to equip and reports whether the request succeeded.
    public bool TryEquip(PassiveItemSO item, int targetSlotIndex, out string message)
    {
        EnsureSlotStorage();

        if (item == null)
            return Fail("Passive item is missing.", out message);

        if (!IsSlotUnlocked(targetSlotIndex))
            return Fail("This passive slot is locked.", out message);

        PassiveItemSO current = _equippedSlots[targetSlotIndex];

        if (current == item)
        {
            message = $"{ResolveItemName(item)} is already equipped in this slot.";
            return true;
        }

        if (IsChangeOnCooldown)
            return Fail(GetCooldownMessage(), out message);

        if (!HasAvailableOwnedCopy(item, current))
        {
            return Fail(
                $"An unassigned copy of {ResolveItemName(item)} is required.",
                out message);
        }

        _equippedSlots[targetSlotIndex] = item;
        StartChangeCooldown();
        RebuildEquippedItems();
        OnChanged?.Invoke();

        message = current == null
            ? $"{ResolveItemName(item)} equipped in passive slot {targetSlotIndex + 1}."
            : $"{ResolveItemName(current)} replaced with {ResolveItemName(item)} in passive slot {targetSlotIndex + 1}.";
        return true;
    }

    // Purpose: Attempts to unequip and reports whether the request succeeded.
    public bool TryUnequip(int slotIndex, out string message)
    {
        EnsureSlotStorage();

        if (!IsSlotUnlocked(slotIndex))
            return Fail("This passive slot is locked.", out message);

        PassiveItemSO current = _equippedSlots[slotIndex];

        if (current == null)
            return Fail("This passive slot is already empty.", out message);

        if (IsChangeOnCooldown)
            return Fail(GetCooldownMessage(), out message);

        _equippedSlots[slotIndex] = null;
        StartChangeCooldown();
        RebuildEquippedItems();
        OnChanged?.Invoke();

        message = $"{ResolveItemName(current)} removed from passive slot {slotIndex + 1}.";
        return true;
    }

    /// <summary>
    /// Progression entry point. This intentionally only unlocks one slot per
    /// call, keeping the 2 -> 5 growth rule explicit for future reward code.
    /// </summary>
    public bool TryUnlockNextSlot(out string message)
    {
        EnsureSlotStorage();

        if (UnlockedSlotCount >= MaximumSlotCount)
            return Fail("All passive slots are already unlocked.", out message);

        _unlockedSlotCount++;
        OnChanged?.Invoke();
        message = $"Passive slot {_unlockedSlotCount} unlocked.";
        return true;
    }

    // Purpose: Updates the unlocked slot count state.
    public void SetUnlockedSlotCount(int value)
    {
        EnsureSlotStorage();
        int clamped = Mathf.Clamp(value, MinimumUnlockedSlotCount, MaximumSlotCount);

        if (_unlockedSlotCount == clamped)
            return;

        _unlockedSlotCount = clamped;
        bool changed = false;

        for (int i = _unlockedSlotCount; i < MaximumSlotCount; i++)
        {
            if (_equippedSlots[i] == null)
                continue;

            _equippedSlots[i] = null;
            changed = true;
        }

        if (!changed)
        {
            OnChanged?.Invoke();
            return;
        }

        RebuildEquippedItems();
        OnChanged?.Invoke();
    }

    // Purpose: Attempts to add and reports whether the request succeeded.
    public bool TryAdd(PassiveItemSO item)
    {
        if (item == null)
            return false;

        ResolveRefs();

        if (_useInventoryRuntime && _inventoryRuntime != null)
            return _inventoryRuntime.TryPickupPassiveItem(item, out _);

        if (!_forceAllowDuplicates &&
            !item.AllowDuplicatePickup &&
            _items.Contains(item))
        {
            return false;
        }

        _items.Add(item);
        return true;
    }

    // Purpose: Clears the current clear state.
    public void Clear()
    {
        EnsureSlotStorage();

        if (_useInventoryRuntime && _inventoryRuntime != null)
        {
            _inventoryRuntime.ClearPassiveItems();
            return;
        }

        bool changed = false;

        for (int i = 0; i < _equippedSlots.Count; i++)
        {
            if (_equippedSlots[i] == null)
                continue;

            _equippedSlots[i] = null;
            changed = true;
        }

        _items.Clear();

        if (!changed)
            return;

        RebuildEquippedItems();
        OnChanged?.Invoke();
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_inventoryRuntime == null)
            _inventoryRuntime = GetComponent<PlayerInventoryRuntime>();
    }

    // Purpose: Handles the incoming inventory changed request or event.
    private void HandleInventoryChanged()
    {
        // Most inventory changes (bullets, consumables, etc.) must not touch
        // passive effects. Only remove an equipped passive when its owned copy
        // is actually gone from the backpack.
        if (!PruneSlotsWithoutOwnedCopies())
            return;

        RebuildEquippedItems();
        OnChanged?.Invoke();
    }

    // Purpose: Checks whether this component has available owned copy.
    private bool HasAvailableOwnedCopy(PassiveItemSO item, PassiveItemSO replacing)
    {
        int assignedCopies = CountAssignedCopies(item);

        if (replacing == item)
            assignedCopies = Mathf.Max(0, assignedCopies - 1);

        return GetOwnedCount(item) > assignedCopies;
    }

    // Purpose: Returns the current owned count.
    private int GetOwnedCount(PassiveItemSO item)
    {
        if (item == null)
            return 0;

        if (_useInventoryRuntime && _inventoryRuntime != null)
            return _inventoryRuntime.GetInventoryItemAmount(item);

        int count = 0;

        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] == item)
                count++;
        }

        return count;
    }

    // Purpose: Counts the available assigned copies.
    private int CountAssignedCopies(PassiveItemSO item)
    {
        int count = 0;

        for (int i = 0; i < UnlockedSlotCount; i++)
        {
            if (_equippedSlots[i] == item)
                count++;
        }

        return count;
    }

    // Purpose: Performs the prune slots without owned copies workflow for this component.
    private bool PruneSlotsWithoutOwnedCopies()
    {
        EnsureSlotStorage();
        Dictionary<PassiveItemSO, int> remainingOwned = new();
        bool changed = false;

        for (int i = 0; i < UnlockedSlotCount; i++)
        {
            PassiveItemSO item = _equippedSlots[i];

            if (item == null)
                continue;

            if (!remainingOwned.TryGetValue(item, out int remaining))
            {
                remaining = GetOwnedCount(item);
                remainingOwned.Add(item, remaining);
            }

            if (remaining > 0)
            {
                remainingOwned[item] = remaining - 1;
                continue;
            }

            _equippedSlots[i] = null;
            changed = true;
        }

        return changed;
    }

    // Purpose: Ensures slot storage is ready before the workflow continues.
    private void EnsureSlotStorage()
    {
        _unlockedSlotCount = Mathf.Clamp(
            _unlockedSlotCount,
            MinimumUnlockedSlotCount,
            MaximumSlotCount);

        _equippedSlots ??= new List<PassiveItemSO>(MaximumSlotCount);

        while (_equippedSlots.Count < MaximumSlotCount)
            _equippedSlots.Add(null);

        if (_equippedSlots.Count > MaximumSlotCount)
            _equippedSlots.RemoveRange(
                MaximumSlotCount,
                _equippedSlots.Count - MaximumSlotCount);
    }

    // Purpose: Repairs or rebuilds equipped items from the current source data.
    private void RebuildEquippedItems()
    {
        EnsureSlotStorage();
        _equippedItems.Clear();

        for (int i = 0; i < UnlockedSlotCount; i++)
        {
            PassiveItemSO item = _equippedSlots[i];

            if (item != null)
                _equippedItems.Add(item);
        }
    }

    // Purpose: Begins the change cooldown workflow.
    private void StartChangeCooldown()
    {
        _nextChangeAllowedAt = Time.time + ChangeCooldownSeconds;
    }

    // Purpose: Returns the current cooldown message.
    private string GetCooldownMessage()
    {
        return $"Passive changes are available in {ChangeCooldownRemaining:0.0}s.";
    }

    // Purpose: Resolves the item name needed by this component.
    private static string ResolveItemName(ItemSO item)
    {
        return InventoryItemDisplayUtility.ResolveItemName(item);
    }

    // Purpose: Marks the active fail workflow as failed.
    private static bool Fail(string text, out string message)
    {
        message = text;
        return false;
    }
}
