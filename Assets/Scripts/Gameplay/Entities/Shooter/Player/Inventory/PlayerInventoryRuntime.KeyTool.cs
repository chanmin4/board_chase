/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player > Inventory
 * Primary responsibility: Owns the nested inventory data inside a carried
 * Key Tool item. The Key Tool itself remains an ordinary backpack item.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PlayerKeyToolSlotState
{
    [SerializeField] private KeyItemSO _key;
    [SerializeField, Min(0)] private int _remainingUses;

    public KeyItemSO Key => _key;
    public int RemainingUses => Mathf.Clamp(_remainingUses, 0, MaxUses);
    public int MaxUses => _key != null ? _key.MaxUses : 0;
    public bool HasKey => _key != null && RemainingUses > 0;

    // Purpose: Initializes one physical durable key slot.
    internal void Set(KeyItemSO key, int remainingUses = -1)
    {
        _key = key;
        _remainingUses = key == null
            ? 0
            : Mathf.Clamp(
                remainingUses >= 0 ? remainingUses : key.MaxUses,
                0,
                key.MaxUses);
    }

    // Purpose: Consumes one successful matching unlock use from this physical key.
    internal bool TryConsumeUse()
    {
        if (!HasKey)
            return false;

        _remainingUses--;
        return true;
    }
}

/// <summary>
/// Read-only state for a visible Key Tool grid position. Slots are physical
/// keys, so two identical keys remain separate durable objects.
/// </summary>
public readonly struct PlayerKeyToolSlotSnapshot
{
    public PlayerKeyToolSlotSnapshot(int slotIndex, KeyItemSO key, int remainingUses)
    {
        SlotIndex = Mathf.Max(0, slotIndex);
        Key = key;
        RemainingUses = key != null
            ? Mathf.Clamp(remainingUses, 0, key.MaxUses)
            : 0;
    }

    public int SlotIndex { get; }
    public KeyItemSO Key { get; }
    public int RemainingUses { get; }
    public int MaxUses => Key != null ? Key.MaxUses : 0;
    public bool HasKey => Key != null && RemainingUses > 0;
}

/// <summary>
/// Aggregated tier data used only by the locked-loot selector. The physical
/// Key Tool screen uses <see cref="PlayerKeyToolSlotSnapshot"/> instead.
/// </summary>
public readonly struct PlayerKeyToolTierSnapshot
{
    public PlayerKeyToolTierSnapshot(
        LootKeyTier tier,
        int keyCopies,
        int activeKeyDurability,
        int usesPerKey,
        int maxKeyCopies,
        int totalRemainingUses)
    {
        Tier = tier;
        KeyCopies = Mathf.Max(0, keyCopies);
        ActiveKeyDurability = KeyCopies > 0
            ? Mathf.Clamp(activeKeyDurability, 0, Mathf.Max(1, usesPerKey))
            : 0;
        UsesPerKey = Mathf.Max(1, usesPerKey);
        MaxKeyCopies = Mathf.Max(1, maxKeyCopies);
        TotalRemainingUses = Mathf.Max(0, totalRemainingUses);
    }

    public LootKeyTier Tier { get; }
    public int KeyCopies { get; }
    public int ActiveKeyDurability { get; }
    public int UsesPerKey { get; }
    public int MaxKeyCopies { get; }
    public int TotalRemainingUses { get; }
    public bool HasUsableKey => TotalRemainingUses > 0;
}

public partial class PlayerInventoryRuntime
{
    [Header("Key Tool Container")]
    [Tooltip("Runtime contents of the carried Key Tool. Do not author this list manually; it is populated by key pickup and the container UI.")]
    [SerializeField] private List<PlayerKeyToolSlotState> _keyToolSlots = new();

    public event Action KeyToolChanged;

    /// <summary>The Key Tool item currently occupying an ordinary player inventory slot.</summary>
    public KeyToolItemSO KeyTool => FindCarriedKeyTool();
    public bool HasKeyTool => KeyTool != null;
    public int KeyToolSlotCapacity => KeyTool != null ? KeyTool.SlotCapacity : 0;
    public int KeyToolColumnCount => KeyTool != null ? KeyTool.ColumnCount : 4;

    // Purpose: Returns one physical key slot for the nested Key Tool UI.
    public PlayerKeyToolSlotSnapshot GetKeyToolSlotSnapshot(int slotIndex)
    {
        NormalizeKeyToolStorage();

        if (slotIndex < 0 || slotIndex >= KeyToolSlotCapacity ||
            _keyToolSlots == null || slotIndex >= _keyToolSlots.Count)
        {
            return new PlayerKeyToolSlotSnapshot(slotIndex, null, 0);
        }

        PlayerKeyToolSlotState state = _keyToolSlots[slotIndex];
        return state == null
            ? new PlayerKeyToolSlotSnapshot(slotIndex, null, 0)
            : new PlayerKeyToolSlotSnapshot(slotIndex, state.Key, state.RemainingUses);
    }

    // Purpose: Copies every visible nested Key Tool slot without exposing mutable runtime state.
    public void GetKeyToolSlotSnapshots(List<PlayerKeyToolSlotSnapshot> target)
    {
        if (target == null)
            return;

        target.Clear();
        int capacity = KeyToolSlotCapacity;
        for (int index = 0; index < capacity; index++)
            target.Add(GetKeyToolSlotSnapshot(index));
    }

    // Purpose: Returns aggregated tier state for the locked-loot key selector.
    public PlayerKeyToolTierSnapshot GetKeyToolTierSnapshot(LootKeyTier tier)
    {
        NormalizeKeyToolStorage();

        int keyCopies = 0;
        int activeDurability = 0;
        int activeMaxUses = 3;
        int totalUses = 0;

        if (_keyToolSlots != null)
        {
            for (int index = 0; index < _keyToolSlots.Count; index++)
            {
                PlayerKeyToolSlotState state = _keyToolSlots[index];
                if (state == null || !state.HasKey || state.Key.Tier != tier)
                    continue;

                keyCopies++;
                totalUses += state.RemainingUses;

                if (activeDurability <= 0)
                {
                    activeDurability = state.RemainingUses;
                    activeMaxUses = state.MaxUses;
                }
            }
        }

        return new PlayerKeyToolTierSnapshot(
            tier,
            keyCopies,
            activeDurability,
            activeMaxUses,
            KeyToolSlotCapacity,
            totalUses);
    }

    // Purpose: Copies tier summaries for lock UI and debugging without exposing mutable storage.
    public void GetKeyToolTierSnapshots(List<PlayerKeyToolTierSnapshot> target)
    {
        if (target == null)
            return;

        target.Clear();
        foreach (LootKeyTier tier in Enum.GetValues(typeof(LootKeyTier)))
            target.Add(GetKeyToolTierSnapshot(tier));
    }

    // Purpose: Checks whether the carried Key Tool has one usable key of the requested tier.
    public bool HasUsableKey(LootKeyTier tier)
    {
        return HasKeyTool && GetKeyToolTierSnapshot(tier).HasUsableKey;
    }

    /// <summary>
    /// Receives a physical key. Before the player owns a Key Tool, the key
    /// stays in the normal inventory. Once the tool is carried, it enters its
    /// internal grid and no longer costs a backpack slot.
    /// </summary>
    public bool TryAddKey(KeyItemSO key, int amount, out string message)
    {
        if (key == null)
            return Fail("Key item is missing.", out message);

        int requested = Mathf.Max(1, amount);

        if (!HasKeyTool)
        {
            bool stored = TryStoreItem(key, requested, out message);
            if (stored)
                message = $"{key.DisplayName} stored in inventory. Buy a Key Tool to free the slot.";

            return stored;
        }

        NormalizeKeyToolStorage();
        int freeSlots = Mathf.Max(0, KeyToolSlotCapacity - _keyToolSlots.Count);
        if (freeSlots < requested)
            return Fail($"{KeyTool.DisplayName} is full ({KeyToolSlotCapacity} key slots).", out message);

        for (int index = 0; index < requested; index++)
            _keyToolSlots.Add(CreateKeyToolSlot(key));

        message = requested == 1
            ? $"{key.DisplayName} stored in {KeyTool.DisplayName}."
            : $"{key.DisplayName} x{requested} stored in {KeyTool.DisplayName}.";
        NotifyKeyToolChanged();
        return true;
    }

    // Purpose: Places a previously carried normal-inventory key into the open Key Tool.
    public bool TryMoveInventoryKeyToKeyTool(int inventoryIndex, out string message)
    {
        NormalizeStorage();
        NormalizeKeyToolStorage();

        if (!HasKeyTool)
            return Fail("Buy a Key Tool before storing keys inside it.", out message);

        if (!IsValidIndex(inventoryIndex) || _items[inventoryIndex] == null || _items[inventoryIndex].IsEmpty ||
            _items[inventoryIndex].Item is not KeyItemSO key)
        {
            return Fail("The selected inventory slot does not contain a key.", out message);
        }

        if (_keyToolSlots.Count >= KeyToolSlotCapacity)
            return Fail($"{KeyTool.DisplayName} is full ({KeyToolSlotCapacity} key slots).", out message);

        _keyToolSlots.Add(CreateKeyToolSlot(key));
        if (_items[inventoryIndex].Remove(1))
            _items[inventoryIndex].Clear();

        message = $"{key.DisplayName} moved into {KeyTool.DisplayName}.";
        NotifyKeyToolChanged();
        return true;
    }

    // Purpose: Moves one Key Tool key back into normal inventory on the container slot double-click.
    public bool TryWithdrawKeyToolSlot(int slotIndex, out string message)
    {
        NormalizeKeyToolStorage();

        if (!HasKeyTool || slotIndex < 0 || slotIndex >= _keyToolSlots.Count)
            return Fail("Key Tool slot is empty.", out message);

        PlayerKeyToolSlotState state = _keyToolSlots[slotIndex];
        if (state == null || !state.HasKey)
            return Fail("Key Tool slot is empty.", out message);

        KeyItemSO key = state.Key;
        if (!TryStoreItem(key, 1, out message))
            return false;

        _keyToolSlots.RemoveAt(slotIndex);
        message = $"{key.DisplayName} moved to inventory. It restores to full durability outside the Key Tool.";
        NotifyKeyToolChanged();
        return true;
    }

    // Purpose: Consumes one durability use only after a lock completes its unlock transaction.
    public bool TryConsumeKeyUse(LootKeyTier tier, out string message)
    {
        NormalizeKeyToolStorage();

        if (!HasKeyTool)
            return Fail("A Key Tool is required to use stored keys.", out message);

        for (int index = 0; index < _keyToolSlots.Count; index++)
        {
            PlayerKeyToolSlotState state = _keyToolSlots[index];
            if (state == null || !state.HasKey || state.Key.Tier != tier)
                continue;

            KeyItemSO key = state.Key;
            state.TryConsumeUse();

            if (!state.HasKey)
            {
                _keyToolSlots.RemoveAt(index);
                message = $"{key.DisplayName} was depleted and removed from {KeyTool.DisplayName}.";
            }
            else
            {
                message = $"{key.DisplayName} used. Durability: {state.RemainingUses}/{state.MaxUses}.";
            }

            NotifyKeyToolChanged();
            return true;
        }

        return Fail($"No usable {tier} Key is stored in {KeyTool.DisplayName}.", out message);
    }

    // Purpose: Stores the purchased Key Tool in an ordinary inventory slot, then imports carried keys if room exists.
    private bool TryPickupKeyTool(KeyToolItemSO keyTool, out string message)
    {
        if (keyTool == null)
            return Fail("Key Tool item is missing.", out message);

        if (HasKeyTool)
            return Fail("A Key Tool is already carried.", out message);

        if (!TryStoreItem(keyTool, 1, out message))
            return false;

        int moved = AbsorbCarriedInventoryKeys();
        message = moved > 0
            ? $"{keyTool.DisplayName} purchased. {moved} key(s) moved inside."
            : $"{keyTool.DisplayName} purchased. Open it from inventory to store keys.";
        NotifyKeyToolChanged();
        return true;
    }

    // Purpose: Moves full-durability keys carried before Key Tool purchase into the newly acquired container.
    private int AbsorbCarriedInventoryKeys()
    {
        if (!HasKeyTool)
            return 0;

        NormalizeStorage();
        NormalizeKeyToolStorage();
        int moved = 0;

        for (int index = 0; index < Capacity && _keyToolSlots.Count < KeyToolSlotCapacity; index++)
        {
            PlayerInventoryItemStack stack = _items[index];
            if (stack == null || stack.IsEmpty || stack.Item is not KeyItemSO key)
                continue;

            _keyToolSlots.Add(CreateKeyToolSlot(key));
            if (stack.Remove(1))
                stack.Clear();

            moved++;
            index--;
        }

        return moved;
    }

    // Purpose: Normalizes only invalid/depleted serialized nested keys without creating ownership outside a carried Key Tool.
    private void NormalizeKeyToolStorage()
    {
        _keyToolSlots ??= new List<PlayerKeyToolSlotState>();

        for (int index = _keyToolSlots.Count - 1; index >= 0; index--)
        {
            PlayerKeyToolSlotState state = _keyToolSlots[index];
            if (state == null || !state.HasKey)
                _keyToolSlots.RemoveAt(index);
        }

        if (!HasKeyTool && _keyToolSlots.Count > 0)
            _keyToolSlots.Clear();
    }

    // Purpose: Clears nested run-bound keys when the player loses the run or no longer owns the Key Tool.
    private void ClearKeyToolStorage()
    {
        if (_keyToolSlots == null || _keyToolSlots.Count <= 0)
            return;

        _keyToolSlots.Clear();
        KeyToolChanged?.Invoke();
    }

    // Purpose: Counts only physical keys inside the current carried Key Tool.
    private int GetStoredKeyCount()
    {
        NormalizeKeyToolStorage();
        return _keyToolSlots != null ? _keyToolSlots.Count : 0;
    }

    // Purpose: Finds the physical Key Tool item among normal Pocket/Inventory slots.
    private KeyToolItemSO FindCarriedKeyTool()
    {
        if (_items == null)
            return null;

        int slotLimit = Mathf.Min(Capacity, _items.Count);
        for (int index = 0; index < slotLimit; index++)
        {
            PlayerInventoryItemStack stack = _items[index];
            if (stack != null && !stack.IsEmpty && stack.Item is KeyToolItemSO keyTool)
                return keyTool;
        }

        return null;
    }

    // Purpose: Creates one full-durability physical key entry inside the Key Tool.
    private static PlayerKeyToolSlotState CreateKeyToolSlot(KeyItemSO key)
    {
        PlayerKeyToolSlotState state = new();
        state.Set(key);
        return state;
    }

    // Purpose: Notifies inventory views, lock UI, and future run-save observers after Key Tool state changes.
    private void NotifyKeyToolChanged()
    {
        KeyToolChanged?.Invoke();
        NotifyChanged();
    }
}
