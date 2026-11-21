/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player > Inventory
 * Primary responsibility: Manages PlayerInventoryItemStack item storage, equipment, transfer state, and related rules.
 * Execution boundary: Runtime (Assembly-CSharp)
 *
 * Ownership and refactor contract:
 * - _items owns Pocket, permanent Inventory, and backpack bonus stacks in that order.
 * - EntityEquipmentRuntime owns equipped weapon/armor instances and durability.
 * - PlayerQuickSlotRuntime slots 0-1 mirror equipment; only usable slots own
 *   independent item amounts and must be included in ownership totals once.
 * - PlayerBulletLoadoutRuntime owns the active weapon virtual-magazine data.
 *   Its old AmmoSlot UI facade is isolated in WIP/Legacy; new authoring must
 *   use the virtual-magazine and inventory-reserve model directly.
 *
 * Remove the legacy ammo adapter only after all prefab/UI call sites of
 * Try*AmmoSlot, PlayerBulletLoadoutRuntime, and WeaponAmmoSlotSnapshot are
 * migrated, and the ammo swap / reload transaction regression test passes.
 */

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public sealed class PlayerInventoryItemStack
{
    [SerializeField] private ItemSO _item;
    [SerializeField, Min(1)] private int _amount = 1;
    [SerializeField] private float _armorDurability = -1f;
    [SerializeField] private float _armorMaxDurability = -1f;
    [SerializeField] private float _weaponCurrentDurability = -1f;
    [SerializeField] private float _weaponMaxDurability = -1f;
    [SerializeField] private float _gasMaskFilterDurability = -1f;
    [SerializeField] private float _gasMaskMaxFilterDurability = -1f;

    public ItemSO Item => _item;
    public int Amount => Mathf.Max(0, _amount);
    public float ArmorDurability => _armorDurability;
    public bool HasArmorDurability => _armorDurability >= 0f;
    public float ArmorMaxDurability => _armorMaxDurability;
    public bool HasArmorCondition => _item is ArmorItemSO && _armorMaxDurability >= 0f;
    public float WeaponCurrentDurability => _weaponCurrentDurability;
    public float WeaponMaxDurability => _weaponMaxDurability;
    public bool HasWeaponDurability =>
        _item is RangedWeaponSO && _weaponCurrentDurability >= 0f && _weaponMaxDurability > 0f;
    public float GasMaskFilterDurability => _gasMaskFilterDurability;
    public bool HasGasMaskFilterDurability => _item is GasMaskItemSO && _gasMaskFilterDurability >= 0f;
    public float GasMaskMaxFilterDurability => _gasMaskMaxFilterDurability;
    public bool HasGasMaskCondition => _item is GasMaskItemSO && _gasMaskMaxFilterDurability >= 0f;
    public bool IsEmpty => _item == null || _amount <= 0;

    // Purpose: Updates the set state.
    public void Set(ItemSO item, int amount)
    {
        Set(item, amount, -1f);
    }

    // Purpose: Updates the set state.
    public void Set(ItemSO item, int amount, float armorDurability)
    {
        Set(item, amount, armorDurability, -1f, -1f);
    }

    // Purpose: Updates the set state.
    public void Set(
        ItemSO item,
        int amount,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability)
    {
        Set(
            item,
            amount,
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            -1f);
    }

    // Purpose: Stores condition values for all condition-bearing item kinds.
    public void Set(
        ItemSO item,
        int amount,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        float gasMaskFilterDurability)
    {
        Set(
            item,
            amount,
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            gasMaskFilterDurability,
            -1f,
            -1f);
    }

    // Purpose: Stores current and maximum condition values for every repairable item kind.
    public void Set(
        ItemSO item,
        int amount,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        float gasMaskFilterDurability,
        float armorMaxDurability,
        float gasMaskMaxFilterDurability)
    {
        _item = item;
        _amount = Mathf.Max(1, amount);

        if (item is ArmorItemSO armor)
        {
            _armorMaxDurability = Mathf.Clamp(
                armorMaxDurability >= 0f ? armorMaxDurability : armor.MaxDurability,
                0f,
                armor.MaxDurability);
            _armorDurability = Mathf.Clamp(
                armorDurability >= 0f ? armorDurability : _armorMaxDurability,
                0f,
                _armorMaxDurability);
        }
        else
        {
            _armorDurability = -1f;
            _armorMaxDurability = -1f;
        }

        if (item is RangedWeaponSO rangedWeapon)
        {
            float resolvedMax = weaponMaxDurability > 0f
                ? weaponMaxDurability
                : rangedWeapon.MaxDurability;
            _weaponMaxDurability = Mathf.Max(0f, resolvedMax);
            _weaponCurrentDurability = Mathf.Clamp(
                weaponCurrentDurability >= 0f ? weaponCurrentDurability : _weaponMaxDurability,
                0f,
                _weaponMaxDurability);
        }
        else if (item is ConsumableItemSO medical && medical.UsesDurability)
        {
            // Reuse the existing current/max instance-condition transport. Legacy field names
            // are retained so warehouse, world drops, loot and run saves do not lose condition.
            _weaponMaxDurability = weaponMaxDurability > 0f ? Mathf.Min(weaponMaxDurability, medical.MaxDurability) : medical.MaxDurability;
            _weaponCurrentDurability = Mathf.Clamp(weaponCurrentDurability >= 0f ? weaponCurrentDurability :
                _weaponMaxDurability, 0f, _weaponMaxDurability);
        }
        else
        {
            _weaponCurrentDurability = -1f;
            _weaponMaxDurability = -1f;
        }

        if (item is GasMaskItemSO gasMask)
        {
            _gasMaskMaxFilterDurability = Mathf.Clamp(
                gasMaskMaxFilterDurability >= 0f
                    ? gasMaskMaxFilterDurability
                    : gasMask.MaxFilterDurability,
                0f,
                gasMask.MaxFilterDurability);
            _gasMaskFilterDurability = Mathf.Clamp(
                gasMaskFilterDurability >= 0f
                    ? gasMaskFilterDurability
                    : _gasMaskMaxFilterDurability,
                0f,
                _gasMaskMaxFilterDurability);
        }
        else
        {
            _gasMaskFilterDurability = -1f;
            _gasMaskMaxFilterDurability = -1f;
        }
    }

    // Purpose: Clears the current clear state.
    public void Clear()
    {
        _item = null;
        _amount = 0;
        _armorDurability = -1f;
        _armorMaxDurability = -1f;
        _weaponCurrentDurability = -1f;
        _weaponMaxDurability = -1f;
        _gasMaskFilterDurability = -1f;
        _gasMaskMaxFilterDurability = -1f;
    }

    // Purpose: Adds the requested add to this component.
    public void Add(int amount)
    {
        _amount = Mathf.Max(1, _amount + Mathf.Max(1, amount));
    }

    // Purpose: Removes the requested remove from this component.
    public bool Remove(int amount)
    {
        if (amount <= 0 || IsEmpty)
            return false;

        _amount -= amount;
        // Old saves may contain a full stack of bandages. Condition belongs to its leading
        // unit; removing/transferring that unit leaves full unused units, never duplicates it.
        if (_amount > 0 && _item is ConsumableItemSO medical && medical.UsesDurability)
            _weaponCurrentDurability = _weaponMaxDurability = medical.MaxDurability;
        return _amount <= 0;
    }

    public bool TrySpendConsumableDurability(ConsumableItemSO medical)
    {
        if (IsEmpty || _item != medical || !medical.UsesDurability) return false;
        float maximum = _weaponMaxDurability > 0f ? _weaponMaxDurability : medical.MaxDurability;
        float current = _weaponCurrentDurability >= 0f ? _weaponCurrentDurability : maximum;
        if (current <= 0f) return false;
        current = Mathf.Max(0f, current - medical.DurabilityCostPerUse);
        if (current <= 0f)
        { if (Remove(1)) Clear(); }
        else { _weaponCurrentDurability = current; _weaponMaxDurability = maximum; }
        return true;
    }
}

public readonly struct PlayerInventoryRemovedStack
{
    public readonly ItemSO item;
    public readonly int amount;
    public readonly float armorDurability;
    public readonly float armorMaxDurability;
    public readonly float weaponCurrentDurability;
    public readonly float weaponMaxDurability;
    public readonly float gasMaskFilterDurability;
    public readonly float gasMaskMaxFilterDurability;

    public bool HasItem => item != null && amount > 0;

    public PlayerInventoryRemovedStack(
        ItemSO item,
        int amount,
        float armorDurability)
        : this(item, amount, armorDurability, -1f, -1f)
    {
    }

    public PlayerInventoryRemovedStack(
        ItemSO item,
        int amount,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability)
        : this(
            item,
            amount,
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            -1f)
    {
    }

    public PlayerInventoryRemovedStack(
        ItemSO item,
        int amount,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        float gasMaskFilterDurability)
        : this(
            item,
            amount,
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            gasMaskFilterDurability,
            -1f,
            -1f)
    {
    }

    public PlayerInventoryRemovedStack(
        ItemSO item,
        int amount,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        float gasMaskFilterDurability,
        float armorMaxDurability,
        float gasMaskMaxFilterDurability)
    {
        this.item = item;
        this.amount = Mathf.Max(0, amount);
        this.armorDurability = armorDurability;
        this.armorMaxDurability = armorMaxDurability;
        this.weaponCurrentDurability = weaponCurrentDurability;
        this.weaponMaxDurability = weaponMaxDurability;
        this.gasMaskFilterDurability = gasMaskFilterDurability;
        this.gasMaskMaxFilterDurability = gasMaskMaxFilterDurability;
    }
}

public enum PlayerInventoryEquipmentSlotKind
{
    Weapon = 0,
    Armor = 1,
    Backpack = 2,
    Head = 3
}

[DisallowMultipleComponent]
public partial class PlayerInventoryRuntime : InventoryRuntimeBase
{
    [Header("Virtual Magazine Runtime")]
    [Tooltip("Current virtual-magazine runtime. Legacy AmmoSlot UI compatibility methods are isolated under Assets/Scripts/WIP/Legacy; do not add new ammo equipment slots.")]
    [SerializeField] private PlayerBulletLoadoutRuntime _bulletLoadout;

    [Header("Refs")]
    [FormerlySerializedAs("_weaponHolder")]
    [SerializeField] private ShooterEquipmentController _equipmentController;
    [SerializeField] private EntityEquipmentRuntime _equipmentRuntime;
    [SerializeField] private ShooterStatsRuntime _statsRuntime;
    [SerializeField] private PlayerQuickSlotRuntime _quickSlotRuntime;

    [Header("Runtime Ready")]
    [SerializeField] private PlayerInventoryRuntimeReadyEventChannelSO _inventoryRuntimeReadyChannel;

    [Header("Runtime")]
    [SerializeField] private List<PlayerInventoryItemStack> _items = new();

    [Header("Diagnostics")]
    [Tooltip("Logs external loot transfers, including the matching inventory stacks considered before and after the transfer. Turn off after verifying loot stacking.")]
    [SerializeField] private bool _logExternalLootStacking = true;

    private bool _startingLoadoutInventoryApplied;

    public event Action OnChanged;
    public event Action<ItemSO> ItemPickedUp;
    public event Action<ItemSO, string> PickupRejected;

    public IReadOnlyList<PlayerInventoryItemStack> Items => _items;
    public int Capacity => ResolveBackpackInventoryCapacity();
    public int UsedSlots => CountUsedSlots();
    public int FreeSlots => Mathf.Max(0, Capacity - UsedSlots);
    public WeaponSO EquippedWeapon =>
        _equipmentRuntime != null && _equipmentRuntime.CurrentWeapon != null
            ? _equipmentRuntime.CurrentWeapon
            : _equipmentController != null ? _equipmentController.CurrentWeapon : null;
    public int EquippedWeaponAmount =>
        _equipmentRuntime != null
            ? _equipmentRuntime.CurrentWeaponAmount
            : _equipmentController != null ? _equipmentController.CurrentWeaponAmount : (EquippedWeapon != null ? 1 : 0);
    public ArmorItemSO EquippedArmor => _equipmentRuntime != null ? _equipmentRuntime.CurrentArmor : null;
    public float EquippedArmorDurability => _equipmentRuntime != null ? _equipmentRuntime.CurrentArmorDurability : 0f;
    public float EquippedArmorMaxDurability => _equipmentRuntime != null ? _equipmentRuntime.MaxArmorDurability : 0f;
    // Purpose: Returns the current equipped weapon current durability.
    public float GetEquippedWeaponCurrentDurability(int slotIndex) =>
        _equipmentRuntime != null ? _equipmentRuntime.GetWeaponCurrentDurability(slotIndex) : 0f;
    // Purpose: Returns the current equipped weapon max durability.
    public float GetEquippedWeaponMaxDurability(int slotIndex) =>
        _equipmentRuntime != null ? _equipmentRuntime.GetWeaponMaxDurability(slotIndex) : 0f;

    // Purpose: Returns the current inventory item amount.
    public int GetInventoryItemAmount(ItemSO item)
    {
        if (item == null || _items == null)
            return 0;

        int total = 0;

        for (int i = 0; i < _items.Count; i++)
        {
            PlayerInventoryItemStack stack = _items[i];

            if (stack != null && !stack.IsEmpty && IsSameItemIdentity(stack.Item, item))
                total += stack.Amount;
        }

        return total;
    }

    // Purpose: Returns the current total owned item amount.
    public int GetTotalOwnedItemAmount(ItemSO item)
    {
        if (item is PassiveItemSO passive && passive.IsRolledInstance) item = passive.SourceDefinition;
        if (item == null)
            return 0;

        ResolveRefs();
        int total = GetInventoryItemAmount(item);

        if (_equipmentRuntime != null)
        {
            for (int i = 0; i < _equipmentRuntime.WeaponSlotCount; i++)
            {
                if (IsSameItemIdentity(_equipmentRuntime.GetWeapon(i), item))
                    total += Mathf.Max(0, _equipmentRuntime.GetWeaponAmount(i));
            }

            if (IsSameItemIdentity(_equipmentRuntime.CurrentArmor, item))
                total += 1;
        }

        // Only 3-5 physically own usable stacks. Slots 1-2 mirror weapon
        // equipment and must not be counted twice.
        if (_quickSlotRuntime != null)
        {
            for (int i = 2; i < PlayerQuickSlotRuntime.SlotCount; i++)
            {
                if (_quickSlotRuntime.TryGetSnapshot(i, out PlayerQuickSlotSnapshot snapshot) &&
                    snapshot.kind == PlayerQuickSlotBindingKind.Usable &&
                    IsSameItemIdentity(snapshot.item, item))
                {
                    total += Mathf.Max(0, snapshot.amount);
                }
            }
        }

        // Retain compatibility with removable legacy ammo slots while they
        // still exist in old prefabs. Infinite/default ammo is not inventory.
        if (item is BulletSO bullet && _bulletLoadout != null)
        {
            const int legacyAmmoSlotCount = 3;

            for (int i = 0; i < legacyAmmoSlotCount; i++)
            {
                if (!_bulletLoadout.TryGetSlotSnapshot(i, out WeaponAmmoSlotSnapshot snapshot) ||
                    !IsSameItemIdentity(snapshot.bullet, bullet) ||
                    snapshot.infiniteReserve ||
                    snapshot.isRequiredDefault)
                {
                    continue;
                }

                total += Mathf.Max(0, snapshot.totalAmmo);
            }
        }

        return Mathf.Max(0, total);
    }

    // Purpose: Attempts to find inventory item slot and reports whether the request succeeded.
    public bool TryFindInventoryItemSlot(ItemSO item, out int slotIndex)
    {
        slotIndex = -1;

        if (item == null || _items == null)
            return false;

        for (int i = 0; i < _items.Count; i++)
        {
            PlayerInventoryItemStack stack = _items[i];

            if (stack == null || stack.IsEmpty || !IsSameItemIdentity(stack.Item, item))
                continue;

            slotIndex = i;
            return true;
        }

        return false;
    }

    // Purpose: Attempts to consume inventory item and reports whether the request succeeded.
    // Medical doses are batched without callbacks until both inventory and quick slots are updated.
    public bool TryConsumeMedicalItemAmount(ConsumableItemSO item, int amount)
    {
        NormalizeStorage();
        if (item == null || amount <= 0 || GetInventoryItemAmount(item) < amount) return false;
        int remaining = amount;
        foreach (var stack in _items)
        {
            if (remaining <= 0) break;
            if (stack == null || stack.IsEmpty || !IsSameItemIdentity(stack.Item, item)) continue;
            int taken = Mathf.Min(remaining, stack.Amount);
            if (stack.Remove(taken)) stack.Clear();
            remaining -= taken;
        }
        return remaining == 0;
    }
    public void PublishMedicalItemChange() => NotifyChanged();

    public bool TryConsumeInventoryItem(ItemSO item, int amount, out string message)
    {
        if (item == null || amount <= 0)
        {
            message = "Invalid inventory item consumption request.";
            return false;
        }

        TraceBulletInventoryTransaction(
            "TryConsumeInventoryItem begin",
            item as BulletSO,
            $"requested={amount}");

        if (GetInventoryItemAmount(item) < amount)
        {
            message = $"Not enough {InventoryItemDisplayUtility.ResolveItemName(item)}.";
            TraceBulletInventoryTransaction(
                "TryConsumeInventoryItem rejected",
                item as BulletSO,
                $"requested={amount}, message={message}");
            return false;
        }

        int remaining = amount;

        while (remaining > 0 && TryFindInventoryItemSlot(item, out int slotIndex))
        {
            PlayerInventoryItemStack stack = _items[slotIndex];
            int removeAmount = Mathf.Min(remaining, stack.Amount);

            if (!TryRemoveInventoryItem(slotIndex, removeAmount, out _, out message))
                return false;

            remaining -= removeAmount;
        }

        message = $"{InventoryItemDisplayUtility.ResolveItemName(item)} consumed.";
        TraceBulletInventoryTransaction(
            "TryConsumeInventoryItem completed",
            item as BulletSO,
            $"requested={amount}, remainingToConsume={remaining}, message={message}");
        return remaining == 0;
    }

    /// <summary>
    /// Validates every physical payment before any stack is changed. Duplicate
    /// ItemSO rows are aggregated, so a barter cannot pass by counting the
    /// same stack twice for two separate requirements.
    /// </summary>
    public bool TryValidateInventoryItemRequirements(
        IReadOnlyList<ShopTradeRequirement> requirements,
        out string message)
    {
        if (!TryAggregateItemRequirements(requirements, out List<ShopTradeRequirement> aggregated, out message))
            return false;

        for (int index = 0; index < aggregated.Count; index++)
        {
            ShopTradeRequirement requirement = aggregated[index];
            int owned = GetInventoryItemAmount(requirement.Item);

            if (owned < requirement.Amount)
            {
                message = $"Not enough {InventoryItemDisplayUtility.ResolveItemName(requirement.Item)} " +
                          $"({owned}/{requirement.Amount}).";
                return false;
            }
        }

        message = string.Empty;
        return true;
    }

    /// <summary>
    /// Removes a fully prevalidated barter payment. If an unexpected low-level
    /// removal error occurs, already removed rows are restored before failure
    /// is reported so the player does not lose a partial payment.
    /// </summary>
    public bool TryConsumeInventoryItemRequirements(
        IReadOnlyList<ShopTradeRequirement> requirements,
        out string message)
        => TryConsumeInventoryItemRequirements(requirements, out _, out message);

    public bool TryConsumeInventoryItemRequirements(
        IReadOnlyList<ShopTradeRequirement> requirements,
        out List<ShopTradeRequirement> receipt,
        out string message)
    {
        receipt = new List<ShopTradeRequirement>();
        if (!TryAggregateItemRequirements(requirements, out List<ShopTradeRequirement> aggregated, out message) ||
            !TryValidateInventoryItemRequirements(aggregated, out message))
        {
            return false;
        }

        var planned = new List<ShopTradeRequirement>();
        var equipped = GetComponent<PlayerPassiveInventoryRuntime>()?.EquippedSlots;
        foreach (var requirement in aggregated)
        {
            if (requirement.Item is not PassiveItemSO) { planned.Add(requirement); continue; }
            int remaining = requirement.Amount;
            foreach (var stack in _items)
            {
                if (remaining == 0) break;
                if (stack == null || stack.IsEmpty || !IsSameItemIdentity(stack.Item, requirement.Item)) continue;
                if (stack.Item is PassiveItemSO { IsBound: true }) continue;
                bool isEquipped = false;
                if (equipped != null) foreach (var item in equipped) if (item == stack.Item) { isEquipped = true; break; }
                if (isEquipped) continue;
                int take = Mathf.Min(remaining, stack.Amount);
                planned.Add(new ShopTradeRequirement(stack.Item, take));
                remaining -= take;
            }
            if (remaining > 0) return Fail("장착 중인 패시브는 자동 제출/교환하지 않습니다. 해제하거나 여분을 준비하세요.", out message);
        }
        List<ShopTradeRequirement> consumed = receipt;

        for (int index = 0; index < planned.Count; index++)
        {
            ShopTradeRequirement requirement = planned[index];
            if (TryConsumeInventoryItem(requirement.Item, requirement.Amount, out message))
            {
                consumed.Add(requirement);
                continue;
            }

            bool restored = TryRestoreInventoryItemRequirements(consumed, out string restoreMessage);
            if (!restored)
            {
                Debug.LogError(
                    $"[PlayerInventoryRuntime] Shop payment rollback failed. " +
                    $"removeError={message}, rollbackError={restoreMessage}",
                    this);
            }

            return false;
        }

        message = "Payment items consumed.";
        return true;
    }

    /// <summary>
    /// Restores an already consumed physical payment after a later product
    /// grant fails. It is public only for the shop's compensating transaction.
    /// </summary>
    public bool TryRestoreInventoryItemRequirements(
        IReadOnlyList<ShopTradeRequirement> requirements,
        out string message)
    {
        for (int index = 0; requirements != null && index < requirements.Count; index++)
        {
            ShopTradeRequirement requirement = requirements[index];
            if (requirement == null || !requirement.IsValid)
                continue;

            if (requirement.Item is PassiveItemSO { IsRolledInstance: true })
            {
                if (!TryStoreItem(requirement.Item, requirement.Amount, out message)) return false;
                NotifyChanged();
                continue;
            }
            if (TryPickup(requirement.Item, requirement.Amount, out message))
                continue;

            return false;
        }

        message = "Payment items restored.";
        return true;
    }

    /// <summary>
    /// Copies and aggregates requirement rows using the runtime's stable item
    /// identity check rather than relying on a single stack reference.
    /// </summary>
    private static bool TryAggregateItemRequirements(
        IReadOnlyList<ShopTradeRequirement> requirements,
        out List<ShopTradeRequirement> aggregated,
        out string message)
    {
        aggregated = new List<ShopTradeRequirement>();

        if (requirements == null || requirements.Count == 0)
        {
            message = "This trade has no physical payment requirement.";
            return false;
        }

        for (int index = 0; index < requirements.Count; index++)
        {
            ShopTradeRequirement requirement = requirements[index];
            if (requirement == null || !requirement.IsValid)
            {
                message = "This trade contains an invalid payment item.";
                return false;
            }

            bool merged = false;
            for (int aggregateIndex = 0; aggregateIndex < aggregated.Count; aggregateIndex++)
            {
                ShopTradeRequirement existing = aggregated[aggregateIndex];
                if (!IsSameItemIdentity(existing.Item, requirement.Item))
                    continue;

                aggregated[aggregateIndex] = new ShopTradeRequirement(
                    existing.Item,
                    existing.Amount + requirement.Amount);
                merged = true;
                break;
            }

            if (!merged)
                aggregated.Add(new ShopTradeRequirement(requirement.Item, requirement.Amount));
        }

        message = string.Empty;
        return true;
    }

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
        NormalizeStorage();
        NormalizeKeyToolStorage();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveRefs();
        NormalizeStorage();
        NormalizeKeyToolStorage();

        if (_equipmentRuntime != null)
            _equipmentRuntime.OnEquipmentChanged += HandleEquipmentChanged;

        if (_inventoryRuntimeReadyChannel != null)
            _inventoryRuntimeReadyChannel.RaiseEvent(this);
    }

    // Purpose: Completes initialization after scene dependencies are ready.
    private void Start()
    {
        ApplyStartingLoadoutInventoryIfNeeded();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_equipmentRuntime != null)
            _equipmentRuntime.OnEquipmentChanged -= HandleEquipmentChanged;

        if (_inventoryRuntimeReadyChannel != null)
            _inventoryRuntimeReadyChannel.Clear(this);
    }

    // Purpose: Handles the incoming equipment changed request or event.
    private void HandleEquipmentChanged()
    {
        NotifyChanged();
    }

    // Purpose: Applies the requested starting loadout inventory if needed change.
    private void ApplyStartingLoadoutInventoryIfNeeded()
    {
        if (_startingLoadoutInventoryApplied)
            return;

        ResolveRefs();

        if (_equipmentRuntime == null ||
            !_equipmentRuntime.TryGetLastStartingLoadoutSnapshot(out ShooterLoadoutSnapshot snapshot))
        {
            return;
        }

        if (!TryPrepareStartingLoadoutBackpack(snapshot, out string backpackMessage))
        {
            Debug.LogError("[PlayerInventoryRuntime] Failed to prepare starting backpack. " +
                $"reason={backpackMessage}", this);
            return;
        }

        _startingLoadoutInventoryApplied = true;

        if (snapshot.inventoryStacks == null)
            return;

        for (int i = 0; i < snapshot.inventoryStacks.Length; i++)
        {
            ShooterLoadoutItemStack stack = snapshot.inventoryStacks[i];

            if (!stack.HasItem)
                continue;

            // The preparation pass already equipped the starter bag, regardless
            // of this row's position. Never duplicate it inside its own cargo.
            if (stack.item is BackpackItemSO)
                continue;

            if (TryStoreItem(
                    stack.item,
                    stack.amount,
                    stack.armorDurability,
                    out string message))
            {
                continue;
            }

            Debug.LogError(
                $"[PlayerInventoryRuntime] Failed to apply starting inventory item. " +
                $"item={stack.item.name}, amount={stack.amount}, reason={message}",
                this);
        }
    }

    // Purpose: Attempts to pickup and reports whether the request succeeded.
    public bool TryPickup(ItemSO item, out string message)
    {
        return TryPickup(item, 1, out message);
    }

    // Purpose: Attempts to pickup and reports whether the request succeeded.
    public bool TryPickup(ItemSO item, int amount, out string message)
    {
        return TryPickup(
            item,
            amount,
            Mathf.Max(1, amount),
            0f,
            out message);
    }

    // Purpose: Attempts to pickup and reports whether the request succeeded.
    public bool TryPickup(
        ItemSO item,
        int amount,
        int bulletBundleAmount,
        float bulletSellPriceRate,
        out string message)
    {
        ResolveRefs();

        if (item == null)
        {
            message = "Item is missing.";
            Reject(null, message);
            return false;
        }

        int requestedAmount = item is BulletSO
            ? Mathf.Max(1, bulletBundleAmount)
            : Mathf.Max(1, amount);

        TraceBulletInventoryTransaction(
            "TryPickup begin",
            item as BulletSO,
            $"amount={amount}, bulletBundleAmount={bulletBundleAmount}, requested={requestedAmount}");

        if (!CanAcquireOwnedAmount(item, requestedAmount, out message))
        {
            Reject(item, message);
            TraceBulletInventoryTransaction(
                "TryPickup rejected by ownership limit",
                item as BulletSO,
                $"requested={requestedAmount}, message={message}");
            return false;
        }

        bool picked;

        if (item is PassiveItemSO passive)
            picked = TryPickupPassiveItem(passive, Mathf.Max(1, amount), out message);
        else if (item is KeyItemSO key)
            picked = TryAddKey(key, Mathf.Max(1, amount), out message);
        else if (item is KeyToolItemSO keyTool)
            picked = TryPickupKeyTool(keyTool, out message);
        else if (item is BulletSO bullet)
            picked = TryPickupBullet(bullet, bulletBundleAmount, bulletSellPriceRate, out message);
        else if (item is WeaponSO weapon)
            picked = TryPickupWeapon(weapon, Mathf.Max(1, amount), out message);
        else if (item is ArmorItemSO armor)
            picked = TryPickupArmor(armor, out message);
        else if (item is UsableItemSO usable)
            picked = TryStoreExternalItemToInventorySlot(
                usable,
                Mathf.Max(1, amount),
                -1f,
                -1,
                out message);
        else
            picked = TryStoreItem(item, Mathf.Max(1, amount), out message);

        if (!picked)
            Reject(item, message);

        TraceBulletInventoryTransaction(
            picked ? "TryPickup completed" : "TryPickup failed",
            item as BulletSO,
            $"requested={requestedAmount}, message={message}");

        return picked;
    }

    // Purpose: Attempts to pickup passive item and reports whether the request succeeded.
    public bool TryPickupPassiveItem(PassiveItemSO item, out string message)
    {
        return TryPickupPassiveItem(item, 1, out message);
    }

    // Purpose: Attempts to pickup passive item and reports whether the request succeeded.
    public bool TryPickupPassiveItem(PassiveItemSO item, int amount, out string message)
    {
        if (item == null)
            return Fail("Passive item is missing.", out message);

        if (!CanAcquireOwnedAmount(item, Mathf.Max(1, amount), out message))
            return false;

        if (!item.AllowDuplicatePickup && ContainsPassive(item.SourceDefinition))
            return Fail("Passive item already owned.", out message);

        if (item.IsRolledInstance && amount != 1)
            return Fail("개체형 패시브는 한 번에 한 개만 이동할 수 있습니다.", out message);
        if (PassiveInstanceRuntime.NeedsRoll(item))
        {
            NormalizeStorage();
            int count = Mathf.Max(1, amount);
            if (CalculateStoreCapacity(item) < count) return Fail("Inventory is full.", out message);
            // Prepare every instance before changing inventory. Invalid authoring consumes nothing.
            var instances = new List<PassiveItemSO>(count);
            for (int i = 0; i < count; i++)
            {
                var rolled = PassiveInstanceRuntime.Create(item) as PassiveItemSO;
                if (rolled == null) return Fail("Item Maker의 패시브 랜덤 범위 설정이 필요합니다.", out message);
                instances.Add(rolled);
            }
            foreach (var instance in instances)
                if (!TryStoreItem(instance, 1, out message)) return false;
            message = $"{ResolveItemName(item)} x{count} picked up.";
            NotifyPickedUp(instances[0]);
            return true;
        }

        if (!TryStoreItem(item, Mathf.Max(1, amount), out message))
            return false;

        message = $"{ResolveItemName(item)} picked up.";
        NotifyPickedUp(item);
        return true;
    }

    // Purpose: Attempts to pickup armor loot and reports whether the request succeeded.
    public bool TryPickupArmorLoot(ArmorItemSO armor, float durability, out string message)
    {
        return TryPickupArmorLoot(armor, durability, -1f, out message);
    }

    // Purpose: Picks up armor loot while preserving its instance maximum durability.
    public bool TryPickupArmorLoot(
        ArmorItemSO armor,
        float durability,
        float maxDurability,
        out string message)
    {
        if (!CanAcquireOwnedAmount(armor, 1, out message))
            return false;

        return TryPickupArmor(armor, durability, maxDurability, out message);
    }

    // Purpose: Picks up a gas mask while retaining filter condition from a world pickup.
    public bool TryPickupGasMaskLoot(
        GasMaskItemSO gasMask,
        float filterDurability,
        out string message)
    {
        return TryPickupGasMaskLoot(gasMask, filterDurability, -1f, out message);
    }

    // Purpose: Picks up a gas mask while preserving its instance maximum filter durability.
    public bool TryPickupGasMaskLoot(
        GasMaskItemSO gasMask,
        float filterDurability,
        float maxFilterDurability,
        out string message)
    {
        if (gasMask == null)
            return Fail("Gas mask is missing.", out message);

        if (!CanAcquireOwnedAmount(gasMask, 1, out message))
            return false;

        if (!TryStoreItem(
                gasMask,
                1,
                -1f,
                -1f,
                -1f,
                filterDurability,
                -1f,
                maxFilterDurability,
                out message))
        {
            Reject(gasMask, message);
            return false;
        }

        message = $"{ResolveItemName(gasMask)} picked up.";
        NotifyPickedUp(gasMask);
        return true;
    }

    // Purpose: Attempts to pickup weapon loot and reports whether the request succeeded.
    public bool TryPickupWeaponLoot(
        WeaponSO weapon,
        int amount,
        float currentDurability,
        float maxDurability,
        out string message)
    {
        ResolveRefs();

        if (!CanAcquireOwnedAmount(weapon, Mathf.Max(1, amount), out message))
            return false;

        bool picked = TryPickupWeapon(
            weapon,
            Mathf.Max(1, amount),
            currentDurability,
            maxDurability,
            out message);

        if (!picked)
            Reject(weapon, message);

        return picked;
    }

    // Purpose: Attempts to equip inventory item and reports whether the request succeeded.
    public bool TryEquipInventoryItem(int index, out string message)
    {
        ResolveRefs();

        if (!IsValidIndex(index))
            return Fail("Invalid inventory slot.", out message);

        ItemSO item = _items[index].Item;
        if (!GameProgressionRuntime.CanUse(item,out message)) return false;

        if (item is WeaponSO weapon)
            return TryEquipWeaponFromInventory(index, weapon, out message);

        if (item is ArmorItemSO armor)
            return TryEquipArmorFromInventory(
                index,
                armor,
                _items[index].ArmorDurability,
                _items[index].ArmorMaxDurability,
                out message);

        if (item is BackpackItemSO backpack)
            return TryEquipBackpackFromInventory(index, backpack, out message);

        if (item is GasMaskItemSO gasMask)
            return TryEquipGasMaskFromInventory(index, gasMask, out message);

        if (item is BulletSO bullet)
            return TryEquipBulletFromInventory(index, bullet, out message);

        return Fail("This item cannot be equipped.", out message);
    }

    // Purpose: Attempts to quick equip inventory item and reports whether the request succeeded.
    public bool TryQuickEquipInventoryItem(int index, out string message)
    {
        ResolveRefs();

        if (!IsValidIndex(index))
            return Fail("Invalid inventory slot.", out message);

        ItemSO item = _items[index].Item;
        if (!GameProgressionRuntime.CanUse(item,out message)) return false;

        if (item is WeaponSO weapon)
            return TryEquipWeaponFromInventory(index, weapon, out message);

        if (item is ArmorItemSO armor)
            return TryEquipArmorFromInventory(
                index,
                armor,
                _items[index].ArmorDurability,
                _items[index].ArmorMaxDurability,
                out message);

        if (item is BackpackItemSO backpack)
            return TryEquipBackpackFromInventory(index, backpack, out message);

        if (item is GasMaskItemSO gasMask)
            return TryEquipGasMaskFromInventory(index, gasMask, out message);

        if (item is BulletSO bullet)
            return TryEquipBulletFromInventoryToFirstAmmoSlot(index, bullet, out message);

        return Fail("This item cannot be equipped.", out message);
    }

    // Purpose: Attempts to equip inventory item to slot and reports whether the request succeeded.
    public bool TryEquipInventoryItemToSlot(
        int index,
        PlayerInventoryEquipmentSlotKind slotKind,
        out string message)
    {
        int equipmentSlotIndex = slotKind == PlayerInventoryEquipmentSlotKind.Weapon
            ? ResolveAutomaticWeaponSlotIndex()
            : 0;

        return TryEquipInventoryItemToSlot(
            index,
            slotKind,
            equipmentSlotIndex,
            out message);
    }

    // Purpose: Attempts to equip inventory item to slot and reports whether the request succeeded.
    public bool TryEquipInventoryItemToSlot(
        int index,
        PlayerInventoryEquipmentSlotKind slotKind,
        int equipmentSlotIndex,
        out string message)
    {
        ResolveRefs();

        if (!IsValidIndex(index))
            return Fail("Invalid inventory slot.", out message);

        if(!GameProgressionRuntime.CanUse(_items[index].Item,out message))return false;

        switch (slotKind)
        {
            case PlayerInventoryEquipmentSlotKind.Weapon:
                if (_items[index].Item is WeaponSO weapon)
                    return TryEquipWeaponFromInventory(index, weapon, equipmentSlotIndex, out message);

                return Fail("This item is not a weapon.", out message);

            case PlayerInventoryEquipmentSlotKind.Armor:
                if (_items[index].Item is ArmorItemSO armor)
                    return TryEquipArmorFromInventory(
                        index,
                        armor,
                        _items[index].ArmorDurability,
                        _items[index].ArmorMaxDurability,
                        out message);

                return Fail("This item is not armor.", out message);

            case PlayerInventoryEquipmentSlotKind.Backpack:
                if (_items[index].Item is BackpackItemSO backpack)
                    return TryEquipBackpackFromInventory(index, backpack, out message);

                return Fail("This item is not a backpack.", out message);

            case PlayerInventoryEquipmentSlotKind.Head:
                if (_items[index].Item is GasMaskItemSO gasMask)
                    return TryEquipGasMaskFromInventory(index, gasMask, out message);

                return Fail("This item is not a gas mask.", out message);

            default:
                return Fail("Unsupported equipment slot.", out message);
        }
    }

    // Purpose: Attempts to swap weapon slots and reports whether the request succeeded.
    public bool TrySwapWeaponSlots(
        int fromWeaponSlotIndex,
        int toWeaponSlotIndex,
        out string message)
    {
        ResolveRefs();

        if (_equipmentRuntime == null)
            return Fail("Equipment runtime is missing.", out message);

        if (!_equipmentRuntime.TrySwapWeaponSlots(
                fromWeaponSlotIndex,
                toWeaponSlotIndex))
        {
            return Fail("Cannot swap weapon slots.", out message);
        }

        message = fromWeaponSlotIndex == toWeaponSlotIndex
            ? "Weapon slot unchanged."
            : "Weapon slots swapped.";

        NotifyChanged();
        return true;
    }

    // Purpose: Attempts to store external item to inventory slot and reports whether the request succeeded.
    public bool TryStoreExternalItemToInventorySlot(
        ItemSO item,
        int amount,
        float armorDurability,
        int targetIndex,
        out string message)
    {
        return TryStoreExternalItemToInventorySlot(
            item,
            amount,
            armorDurability,
            -1f,
            -1f,
            targetIndex,
            out message);
    }

    // Purpose: Attempts to store external item to inventory slot and reports whether the request succeeded.
    public bool TryStoreExternalItemToInventorySlot(
        ItemSO item,
        int amount,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        int targetIndex,
        out string message)
    {
        return TryStoreExternalItemToInventorySlot(
            item,
            amount,
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            -1f,
            -1f,
            -1f,
            targetIndex,
            out message);
    }

    // Purpose: Stores an external stack while retaining current and reduced maximum condition values.
    public bool TryStoreExternalItemToInventorySlot(
        ItemSO item,
        int amount,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        float gasMaskFilterDurability,
        float armorMaxDurability,
        float gasMaskMaxFilterDurability,
        int targetIndex,
        out string message)
    {
        ResolveRefs();
        LogExternalLootStacking("begin", item, amount, targetIndex, string.Empty);

        if (!CanAcquireOwnedAmount(item, Mathf.Max(1, amount), out message))
        {
            LogExternalLootStacking("rejected-owned-limit", item, amount, targetIndex, message);
            return false;
        }

        if (targetIndex < 0 && item is BulletSO bullet && _bulletLoadout != null &&
            _bulletLoadout.TryAddBulletToMatchingSlot(
                bullet,
                Mathf.Max(1, amount),
                out _,
                out message))
        {
            NotifyChanged();
            LogExternalLootStacking("stored-ammo-loadout", item, amount, targetIndex, message);
            return true;
        }

        if (targetIndex < 0 && item is WeaponSO weapon && CanStack(weapon) &&
            TryAddWeaponToMatchingEquipmentStack(
                weapon,
                Mathf.Max(1, amount),
                out message))
        {
            NotifyChanged();
            LogExternalLootStacking("stacked-equipped-weapon", item, amount, targetIndex, message);
            return true;
        }

        int requestedAmount = Mathf.Max(1, amount);

        if (targetIndex < 0 &&
            item is UsableItemSO usable &&
            _quickSlotRuntime != null)
        {
            int quickSlotCapacity = _quickSlotRuntime.GetMatchingUsableStackSpace(usable);

            if (quickSlotCapacity > 0)
            {
                int backpackCapacity = CalculateStoreCapacity(item);
                if (quickSlotCapacity + backpackCapacity < requestedAmount)
                {
                    message = "Matching quick slot and inventory stacks are full.";
                    LogExternalLootStacking("rejected-usable-capacity", item, amount, targetIndex, message);
                    return false;
                }

                if (_quickSlotRuntime.TryAddToMatchingUsableStacks(
                        usable,
                        requestedAmount,
                        out int quickSlotAdded,
                        out string quickSlotMessage) &&
                    quickSlotAdded > 0)
                {
                    int remainingAmount = requestedAmount - quickSlotAdded;
                    if (remainingAmount <= 0)
                    {
                        NotifyChanged();
                        message = quickSlotMessage;
                        LogExternalLootStacking("stacked-matching-quick-slot", item, amount, targetIndex, message);
                        return true;
                    }

                    bool storedRemainder = TryPlaceItemIntoInventorySlot(
                        item,
                        remainingAmount,
                        -1,
                        armorDurability,
                        weaponCurrentDurability,
                        weaponMaxDurability,
                        gasMaskFilterDurability,
                        armorMaxDurability,
                        gasMaskMaxFilterDurability,
                        out message);

                    if (storedRemainder)
                    {
                        message = $"{quickSlotMessage} Remaining {remainingAmount} stored in inventory.";
                        LogExternalLootStacking("stacked-quick-slot-and-inventory", item, amount, targetIndex, message);
                        return true;
                    }

                    LogExternalLootStacking("failed-after-quick-slot-stack", item, amount, targetIndex, message);
                    return false;
                }
            }
        }

        bool stored = TryPlaceItemIntoInventorySlot(
            item,
            requestedAmount,
            targetIndex,
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            gasMaskFilterDurability,
            armorMaxDurability,
            gasMaskMaxFilterDurability,
            out message);
        LogExternalLootStacking(stored ? "stored-inventory" : "rejected-inventory", item, amount, targetIndex, message);
        return stored;
    }

    // Purpose: Attempts to equip external item to slot and reports whether the request succeeded.
    public bool TryEquipExternalItemToSlot(
        ItemSO item,
        int amount,
        float armorDurability,
        PlayerInventoryEquipmentSlotKind slotKind,
        out string message)
    {
        return TryEquipExternalItemToSlot(item, amount, armorDurability, -1f, -1f, slotKind, 0, out message);
    }

    // Purpose: Attempts to equip external item to slot and reports whether the request succeeded.
    public bool TryEquipExternalItemToSlot(
        ItemSO item,
        int amount,
        float armorDurability,
        PlayerInventoryEquipmentSlotKind slotKind,
        int equipmentSlotIndex,
        out string message)
    {
        return TryEquipExternalItemToSlot(
            item,
            amount,
            armorDurability,
            -1f,
            -1f,
            slotKind,
            equipmentSlotIndex,
            out message);
    }

    // Purpose: Attempts to equip external item to slot and reports whether the request succeeded.
    public bool TryEquipExternalItemToSlot(
        ItemSO item,
        int amount,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        PlayerInventoryEquipmentSlotKind slotKind,
        int equipmentSlotIndex,
        out string message)
    {
        return TryEquipExternalItemToSlot(
            item,
            amount,
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            -1f,
            -1f,
            -1f,
            slotKind,
            equipmentSlotIndex,
            out message);
    }

    // Purpose: Equips an external stack while retaining current and reduced maximum condition values.
    public bool TryEquipExternalItemToSlot(
        ItemSO item,
        int amount,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        float gasMaskFilterDurability,
        float armorMaxDurability,
        float gasMaskMaxFilterDurability,
        PlayerInventoryEquipmentSlotKind slotKind,
        int equipmentSlotIndex,
        out string message)
    {
        ResolveRefs();

        if(!GameProgressionRuntime.CanUse(item,out message))return false;

        if (!CanAcquireOwnedAmount(item, Mathf.Max(1, amount), out message))
            return false;

        switch (slotKind)
        {
            case PlayerInventoryEquipmentSlotKind.Weapon:
                if (item is WeaponSO weapon)
                    return TryEquipExternalWeapon(
                        weapon,
                        Mathf.Max(1, amount),
                        weaponCurrentDurability,
                        weaponMaxDurability,
                        equipmentSlotIndex,
                        out message);

                return Fail("This item is not a weapon.", out message);

            case PlayerInventoryEquipmentSlotKind.Armor:
                if (item is ArmorItemSO armor)
                    return TryEquipExternalArmor(
                        armor,
                        armorDurability,
                        armorMaxDurability,
                        out message);

                return Fail("This item is not armor.", out message);

            case PlayerInventoryEquipmentSlotKind.Head:
                if (item is GasMaskItemSO gasMask)
                    return TryEquipExternalGasMask(
                        gasMask,
                        gasMaskFilterDurability,
                        gasMaskMaxFilterDurability,
                        out message);

                return Fail("This item is not a gas mask.", out message);

            default:
                return Fail("Unsupported equipment slot.", out message);
        }
    }

    // Purpose: Attempts to move inventory item and reports whether the request succeeded.
    public bool TryMoveInventoryItem(int fromIndex, int toIndex, out string message)
    {
        NormalizeStorage();

        if (!IsValidIndex(fromIndex))
            return Fail("Invalid source slot.", out message);

        if (!IsSlotInCapacity(toIndex))
            return Fail("Invalid target slot.", out message);

        if (fromIndex == toIndex)
            return Fail("Same inventory slot.", out message);

        PlayerInventoryItemStack from = _items[fromIndex];
        PlayerInventoryItemStack to = _items[toIndex];

        if (to == null)
        {
            to = new PlayerInventoryItemStack();
            to.Clear();
            _items[toIndex] = to;
        }

        if (to.IsEmpty)
        {
            ItemSO movedItem = from.Item;
            to.Set(
                from.Item,
                from.Amount,
                from.ArmorDurability,
                from.WeaponCurrentDurability,
                from.WeaponMaxDurability,
                from.GasMaskFilterDurability,
                from.ArmorMaxDurability,
                from.GasMaskMaxFilterDurability);
            from.Clear();

            message = $"{ResolveItemName(movedItem)} moved.";
            NotifyChanged();
            return true;
        }

        if (CanStack(from.Item) && IsSameItemIdentity(to.Item, from.Item))
        {
            ItemSO stackedItem = from.Item;
            int space = Mathf.Max(0, ResolveMaxStack(stackedItem) - to.Amount);

            if (space <= 0)
            {
                message = "Target stack is full.";
                return false;
            }

            int movedAmount = Mathf.Min(space, from.Amount);
            to.Add(movedAmount);

            if (from.Remove(movedAmount))
                from.Clear();

            message = $"{ResolveItemName(stackedItem)} stacked.";
            NotifyChanged();
            return true;
        }

        (_items[fromIndex], _items[toIndex]) = (_items[toIndex], _items[fromIndex]);
        message = "Inventory slots swapped.";
        NotifyChanged();
        return true;
    }

    /// <summary>
    /// Exchanges one backpack slot with an item stack owned by another
    /// storage surface, such as a usable quick slot. This keeps the transfer
    /// atomic so a drag operation cannot duplicate either stack.
    /// </summary>
    public bool TryExchangeInventorySlotWithExternalStack(
        int inventoryIndex,
        PlayerInventoryRemovedStack incoming,
        out PlayerInventoryRemovedStack outgoing,
        out string message)
    {
        NormalizeStorage();
        outgoing = default;

        if (!IsSlotInCapacity(inventoryIndex))
            return Fail("Invalid inventory slot.", out message);

        PlayerInventoryItemStack inventoryStack = _items[inventoryIndex];

        if (inventoryStack == null)
        {
            inventoryStack = new PlayerInventoryItemStack();
            inventoryStack.Clear();
            _items[inventoryIndex] = inventoryStack;
        }

        if (!inventoryStack.IsEmpty)
        {
            outgoing = new PlayerInventoryRemovedStack(
                inventoryStack.Item,
                inventoryStack.Amount,
                inventoryStack.ArmorDurability,
                inventoryStack.WeaponCurrentDurability,
                inventoryStack.WeaponMaxDurability,
                inventoryStack.GasMaskFilterDurability,
                inventoryStack.ArmorMaxDurability,
                inventoryStack.GasMaskMaxFilterDurability);
        }

        if (incoming.HasItem)
        {
            inventoryStack.Set(
                incoming.item,
                incoming.amount,
                incoming.armorDurability,
                incoming.weaponCurrentDurability,
                incoming.weaponMaxDurability,
                incoming.gasMaskFilterDurability,
                incoming.armorMaxDurability,
                incoming.gasMaskMaxFilterDurability);
        }
        else
        {
            inventoryStack.Clear();
        }

        message = outgoing.HasItem
            ? "Inventory and quick slot items swapped."
            : "Item moved to inventory.";
        NotifyChanged();
        return true;
    }

    // Purpose: Attempts to find first empty inventory slot and reports whether the request succeeded.
    public bool TryFindFirstEmptyInventorySlot(out int index)
    {
        return TryFindEmptySlotIndex(out index);
    }

    // Purpose: Attempts to discard inventory item and reports whether the request succeeded.
    public bool TryDiscardInventoryItem(int index, int amount, out string message)
    {
        return TryRemoveInventoryItem(
            index,
            amount,
            out _,
            out message);
    }

    // Purpose: Attempts to remove inventory item and reports whether the request succeeded.
    public bool TryRemoveInventoryItem(
        int index,
        int amount,
        out PlayerInventoryRemovedStack removed,
        out string message)
    {
        NormalizeStorage();
        removed = default;

        if (!IsValidIndex(index))
            return Fail("Invalid inventory slot.", out message);

        PlayerInventoryItemStack stack = _items[index];
        ItemSO item = stack.Item;
        int removeAmount = Mathf.Clamp(amount <= 0 ? stack.Amount : amount, 1, stack.Amount);

        if (IsRaidLockedEquippedModule(item))
            return Fail("장착 중인 모듈은 HOME에서 해제한 뒤 옮기거나 버릴 수 있습니다.", out message);

        if (item is KeyToolItemSO && GetStoredKeyCount() > 0)
            return Fail("Remove the keys from the Key Tool before discarding it.", out message);

        float armorDurability = stack.ArmorDurability;
        float armorMaxDurability = stack.ArmorMaxDurability;
        float weaponCurrentDurability = stack.WeaponCurrentDurability;
        float weaponMaxDurability = stack.WeaponMaxDurability;
        float gasMaskFilterDurability = stack.GasMaskFilterDurability;
        float gasMaskMaxFilterDurability = stack.GasMaskMaxFilterDurability;

        TraceBulletInventoryTransaction(
            "TryRemoveInventoryItem begin",
            item as BulletSO,
            $"index={index}, removeAmount={removeAmount}");

        if (stack.Remove(removeAmount))
            stack.Clear();

        removed = new PlayerInventoryRemovedStack(
            item,
            removeAmount,
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            gasMaskFilterDurability,
            armorMaxDurability,
            gasMaskMaxFilterDurability);
        message = $"{ResolveItemName(item)} discarded.";
        NotifyChanged();
        TraceBulletInventoryTransaction(
            "TryRemoveInventoryItem completed",
            item as BulletSO,
            $"index={index}, removed={removeAmount}, destination=caller-owned");
        return true;
    }

    // Purpose: Attempts to split inventory item and reports whether the request succeeded.
    public bool TrySplitInventoryItem(
        int index,
        int splitAmount,
        out int newSlotIndex,
        out string message)
    {
        NormalizeStorage();
        newSlotIndex = -1;

        if (!IsValidIndex(index))
            return Fail("Invalid inventory slot.", out message);

        PlayerInventoryItemStack source = _items[index];

        if (source.Amount <= 1)
            return Fail("This stack cannot be split.", out message);

        if (!CanStack(source.Item))
            return Fail("This item cannot be split.", out message);

        // The split dialog permits its right-edge value to equal the source
        // stack size. In that case this is a safe full-stack move to an empty
        // inventory slot, not an item loss.
        int amount = Mathf.Clamp(splitAmount, 1, source.Amount);
        bool movesEntireStack = amount == source.Amount;

        if (!TryFindEmptySlotIndex(out newSlotIndex))
            return Fail("Inventory is full.", out message);

        PlayerInventoryItemStack target = _items[newSlotIndex];

        if (target == null)
        {
            target = new PlayerInventoryItemStack();
            _items[newSlotIndex] = target;
        }

        target.Set(
            source.Item,
            amount,
            source.ArmorDurability,
            source.WeaponCurrentDurability,
            source.WeaponMaxDurability,
            source.GasMaskFilterDurability,
            source.ArmorMaxDurability,
            source.GasMaskMaxFilterDurability);

        if (source.Remove(amount))
            source.Clear();

        message = movesEntireStack
            ? $"{ResolveItemName(target.Item)} moved to an empty slot."
            : $"{ResolveItemName(target.Item)} split.";
        NotifyChanged();
        return true;
    }

    // Purpose: Attempts to unequip to inventory and reports whether the request succeeded.
    public bool TryUnequipToInventory(
        PlayerInventoryEquipmentSlotKind slotKind,
        out string message)
    {
        return TryUnequipToInventorySlot(slotKind, -1, out message);
    }

    // Purpose: Attempts to unequip to inventory slot and reports whether the request succeeded.
    public bool TryUnequipToInventorySlot(
        PlayerInventoryEquipmentSlotKind slotKind,
        int targetIndex,
        out string message)
    {
        return TryUnequipToInventorySlot(slotKind, 0, targetIndex, out message);
    }

    // Purpose: Attempts to unequip to inventory slot and reports whether the request succeeded.
    public bool TryUnequipToInventorySlot(
        PlayerInventoryEquipmentSlotKind slotKind,
        int equipmentSlotIndex,
        int targetIndex,
        out string message)
    {
        ResolveRefs();
        NormalizeStorage();

        switch (slotKind)
        {
            case PlayerInventoryEquipmentSlotKind.Weapon:
                return TryUnequipWeaponToInventorySlot(equipmentSlotIndex, targetIndex, out message);

            case PlayerInventoryEquipmentSlotKind.Armor:
                return TryUnequipArmorToInventorySlot(targetIndex, out message);

            case PlayerInventoryEquipmentSlotKind.Head:
                return TryUnequipGasMaskToInventorySlot(targetIndex, out message);

            default:
                return Fail("Unsupported equipment slot.", out message);
        }
    }

    // Purpose: Attempts to discard equipped and reports whether the request succeeded.
    public bool TryDiscardEquipped(
        PlayerInventoryEquipmentSlotKind slotKind,
        out string message)
    {
        return TryRemoveEquipped(
            slotKind,
            0,
            out _,
            out message);
    }

    // Purpose: Attempts to remove equipped and reports whether the request succeeded.
    public bool TryRemoveEquipped(
        PlayerInventoryEquipmentSlotKind slotKind,
        out PlayerInventoryRemovedStack removed,
        out string message)
    {
        return TryRemoveEquipped(slotKind, 0, out removed, out message);
    }

    // Purpose: Attempts to remove equipped and reports whether the request succeeded.
    public bool TryRemoveEquipped(
        PlayerInventoryEquipmentSlotKind slotKind,
        int equipmentSlotIndex,
        out PlayerInventoryRemovedStack removed,
        out string message)
    {
        ResolveRefs();
        removed = default;

        switch (slotKind)
        {
            case PlayerInventoryEquipmentSlotKind.Weapon:
                WeaponSO weapon = GetEquippedWeapon(equipmentSlotIndex);
                if (_equipmentRuntime != null && _equipmentRuntime.IsWeaponJammed(equipmentSlotIndex))
                    return Fail("고장 난 총기는 T키 기능처치 후 옮길 수 있습니다.", out message);
                int amount = GetEquippedWeaponAmount(equipmentSlotIndex);
                float weaponCurrentDurability = GetEquippedWeaponCurrentDurability(equipmentSlotIndex);
                float weaponMaxDurability = GetEquippedWeaponMaxDurability(equipmentSlotIndex);

                if (weapon == null)
                    return Fail("No weapon equipped.", out message);

                ClearEquippedWeapon(equipmentSlotIndex);
                removed = new PlayerInventoryRemovedStack(
                    weapon,
                    Mathf.Max(1, amount),
                    -1f,
                    weaponCurrentDurability,
                    weaponMaxDurability);
                message = $"{weapon.DisplayName} discarded.";
                NotifyChanged();
                return true;

            case PlayerInventoryEquipmentSlotKind.Armor:
                if (EquippedArmor == null)
                    return Fail("No armor equipped.", out message);

                ArmorItemSO armor = EquippedArmor;
                float armorDurability = EquippedArmorDurability;
                float armorMaxDurability = _equipmentRuntime != null
                    ? _equipmentRuntime.MaxArmorDurability
                    : armor.MaxDurability;
                _equipmentRuntime.ClearArmor();
                removed = new PlayerInventoryRemovedStack(
                    armor,
                    1,
                    armorDurability,
                    -1f,
                    -1f,
                    -1f,
                    armorMaxDurability,
                    -1f);
                message = $"{armor.DisplayName} discarded.";
                NotifyChanged();
                return true;

            case PlayerInventoryEquipmentSlotKind.Head:
                return TryRemoveEquippedGasMask(out removed, out message);

            default:
                return Fail("Unsupported equipment slot.", out message);
        }
    }

    // Purpose: Attempts to repair inventory armor to full and reports whether the request succeeded.
    public bool TryRepairInventoryArmorToFull(
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

        if (stack.Item is not ArmorItemSO armor)
            return Fail("This item is not armor.", out message);

        float maxDurability = stack.HasArmorCondition
            ? Mathf.Max(0f, stack.ArmorMaxDurability)
            : Mathf.Max(0f, armor.MaxDurability);
        float currentDurability = stack.HasArmorDurability
            ? Mathf.Clamp(stack.ArmorDurability, 0f, maxDurability)
            : maxDurability;

        repairedAmount = Mathf.Max(0f, maxDurability - currentDurability);

        if (repairedAmount <= 0f)
            return Fail("Armor is already fully repaired.", out message);

        float repairedMaxDurability = Mathf.Max(
            1f,
            maxDurability - (repairedAmount * armor.RepairPermanentLossRatio));
        repairedMaxDurability = Mathf.Min(repairedMaxDurability, armor.MaxDurability);
        stack.Set(
            armor,
            stack.Amount,
            repairedMaxDurability,
            -1f,
            -1f,
            -1f,
            repairedMaxDurability,
            -1f);
        repairedAmount = Mathf.Max(0f, repairedMaxDurability - currentDurability);
        message = $"{ResolveItemName(armor)} repaired to {repairedMaxDurability:0.#}/{repairedMaxDurability:0.#}.";
        NotifyChanged();
        return true;
    }

    // Purpose: Attempts to repair equipped armor to full and reports whether the request succeeded.
    public bool TryRepairEquippedArmorToFull(
        out float repairedAmount,
        out string message)
    {
        repairedAmount = 0f;
        if (!EquipmentRepairPolicy.Enabled) return Fail(EquipmentRepairPolicy.DisabledMessage, out message);
        ResolveRefs();

        if (_equipmentRuntime == null || _equipmentRuntime.CurrentArmor == null)
            return Fail("No armor equipped.", out message);

        float maxDurability = Mathf.Max(0f, _equipmentRuntime.MaxArmorDurability);
        float currentDurability = Mathf.Clamp(
            _equipmentRuntime.CurrentArmorDurability,
            0f,
            maxDurability);

        repairedAmount = Mathf.Max(0f, maxDurability - currentDurability);

        if (repairedAmount <= 0f)
            return Fail("Armor is already fully repaired.", out message);

        _equipmentRuntime.RepairArmorToFull();
        float repairedMaxDurability = _equipmentRuntime.MaxArmorDurability;
        repairedAmount = Mathf.Max(0f, repairedMaxDurability - currentDurability);
        message = $"{ResolveItemName(_equipmentRuntime.CurrentArmor)} repaired to {repairedMaxDurability:0.#}/{repairedMaxDurability:0.#}.";
        NotifyChanged();
        return true;
    }

    // Purpose: Attempts to repair inventory weapon to full and reports whether the request succeeded.
    public bool TryRepairInventoryWeaponToFull(
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

        if (stack.Item is not RangedWeaponSO weapon)
            return Fail("This item is not a firearm.", out message);

        float oldMax = stack.HasWeaponDurability
            ? stack.WeaponMaxDurability
            : weapon.MaxDurability;
        float oldCurrent = stack.HasWeaponDurability
            ? stack.WeaponCurrentDurability
            : oldMax;

        if (oldCurrent >= oldMax - 0.001f)
            return Fail("Weapon is already fully repaired.", out message);

        float newMax = Mathf.Max(
            1f,
            oldMax - ((oldMax - oldCurrent) * weapon.RepairPermanentLossRatio));
        repairedAmount = Mathf.Max(0f, newMax - oldCurrent);
        stack.Set(weapon, stack.Amount, -1f, newMax, newMax);
        message = $"{ResolveItemName(weapon)} repaired to {newMax:0.#}/{newMax:0.#}.";
        NotifyChanged();
        return true;
    }

    // Purpose: Attempts to repair equipped weapon to full and reports whether the request succeeded.
    public bool TryRepairEquippedWeaponToFull(
        int slotIndex,
        out float repairedAmount,
        out string message)
    {
        repairedAmount = 0f;
        if (!EquipmentRepairPolicy.Enabled) return Fail(EquipmentRepairPolicy.DisabledMessage, out message);
        ResolveRefs();

        if (_equipmentRuntime == null || _equipmentRuntime.GetWeapon(slotIndex) is not RangedWeaponSO weapon)
            return Fail("No firearm equipped.", out message);

        float oldCurrent = _equipmentRuntime.GetWeaponCurrentDurability(slotIndex);
        float oldMax = _equipmentRuntime.GetWeaponMaxDurability(slotIndex);

        if (oldCurrent >= oldMax - 0.001f)
            return Fail("Weapon is already fully repaired.", out message);

        if (!_equipmentRuntime.RepairWeaponToFull(slotIndex))
            return Fail("Weapon repair failed.", out message);

        float newMax = _equipmentRuntime.GetWeaponMaxDurability(slotIndex);
        repairedAmount = Mathf.Max(0f, newMax - oldCurrent);
        message = $"{ResolveItemName(weapon)} repaired to {newMax:0.#}/{newMax:0.#}.";
        NotifyChanged();
        return true;
    }

    // Purpose: Returns the current passive items.
    public void GetPassiveItems(List<PassiveItemSO> results)
    {
        if (results == null)
            return;

        results.Clear();

        for (int i = 0; i < _items.Count; i++)
        {
            PlayerInventoryItemStack stack = _items[i];

            if (stack == null || stack.Item is not PassiveItemSO passive)
                continue;

            int amount = Mathf.Max(1, stack.Amount);

            for (int j = 0; j < amount; j++)
                results.Add(passive);
        }
    }

    // Purpose: Performs the contains passive workflow for this component.
    public bool ContainsPassive(PassiveItemSO item)
    {
        if (item == null)
            return false;

        for (int i = 0; i < _items.Count; i++)
        {
            PlayerInventoryItemStack stack = _items[i];

            if (stack != null && IsSameItemIdentity(stack.Item, item) && stack.Amount > 0)
                return true;
        }

        return false;
    }

    // Purpose: Clears the current passive items state.
    public void ClearPassiveItems()
    {
        bool changed = false;

        for (int i = _items.Count - 1; i >= 0; i--)
        {
            if (_items[i] != null && _items[i].Item is PassiveItemSO)
            {
                _items[i].Clear();
                changed = true;
            }
        }

        if (changed)
            NotifyChanged();
    }

    /// <summary>
    /// Clears run-bound possessions after player death. Persistent inheritance
    /// warehouse contents are stored by InheritanceWarehouseRuntime and are
    /// intentionally outside this runtime.
    /// </summary>
    public void ClearForRunLoss()
    {
        SaveSystem.Active?.RunData.recyclingSlots?.Clear();
        ResolveRefs();

        for (int i = 0; i < _items.Count; i++)
            _items[i]?.Clear();

        ClearKeyToolStorage();
        ClearEquippedBackpackForRunLoss();
        ClearEquippedGasMask();

        // Do not reapply the current instance's configured starter inventory.
        _startingLoadoutInventoryApplied = true;

        _bulletLoadout?.ClearForRunLoss();

        if (_equipmentRuntime != null)
        {
            for (int i = 0; i < _equipmentRuntime.WeaponSlotCount; i++)
                _equipmentRuntime.ClearWeapon(i);

            _equipmentRuntime.ClearArmor();
        }

        _quickSlotRuntime?.ClearAllBindingsForRunLoss();
        NotifyChanged();
    }

    // Purpose: Attempts to pickup bullet and reports whether the request succeeded.
    private bool TryPickupBullet(
        BulletSO bullet,
        int bulletBundleAmount,
        float bulletSellPriceRate,
        out string message)
    {
        if (bullet == null)
            return Fail("Bullet is missing.", out message);

        int amount = Mathf.Max(1, bulletBundleAmount);

        if (!TryStoreItem(bullet, amount, out message))
            return false;

        message = $"{bullet.DisplayName} stored in inventory.";
        NotifyPickedUp(bullet);
        return true;
    }

    // Purpose: Attempts to pickup weapon and reports whether the request succeeded.
    private bool TryPickupWeapon(WeaponSO weapon, int amount, out string message)
    {
        return TryPickupWeapon(weapon, amount, -1f, -1f, out message);
    }

    // Purpose: Attempts to pickup weapon and reports whether the request succeeded.
    private bool TryPickupWeapon(
        WeaponSO weapon,
        int amount,
        float currentDurability,
        float maxDurability,
        out string message)
    {
        if (weapon == null)
            return Fail("Weapon is missing.", out message);

        int resolvedAmount = Mathf.Max(1, amount);

        if (CanStack(weapon) &&
            TryAddWeaponToMatchingEquipmentStack(weapon, resolvedAmount, out message))
        {
            NotifyPickedUp(weapon);
            NotifyChanged();
            return true;
        }

        if (CanStack(weapon) &&
            TryFindStackWithSpace(weapon, out _) &&
            TryStoreItem(weapon, resolvedAmount, -1f, currentDurability, maxDurability, out message))
        {
            message = $"{weapon.DisplayName} stacked in inventory.";
            NotifyPickedUp(weapon);
            return true;
        }

        int equipmentSlotIndex = ResolveAutomaticWeaponSlotIndex();

        if (weapon is not MeleeWeaponSO && GetEquippedWeapon(equipmentSlotIndex) == null &&
            (_equipmentRuntime != null || _equipmentController != null))
        {
            EquipRuntimeWeapon(
                equipmentSlotIndex,
                weapon,
                resolvedAmount,
                currentDurability,
                maxDurability);
            message = $"{weapon.DisplayName} equipped.";
            NotifyPickedUp(weapon);
            NotifyChanged();
            return true;
        }

        if (!TryStoreItem(
                weapon,
                resolvedAmount,
                -1f,
                currentDurability,
                maxDurability,
                out message))
            return false;

        message = $"{weapon.DisplayName} stored in inventory.";
        NotifyPickedUp(weapon);
        return true;
    }

    // Purpose: Attempts to pickup armor and reports whether the request succeeded.
    private bool TryPickupArmor(ArmorItemSO armor, out string message)
    {
        return TryPickupArmor(
            armor,
            armor != null ? armor.MaxDurability : 0f,
            armor != null ? armor.MaxDurability : 0f,
            out message);
    }

    // Purpose: Attempts to pickup armor and reports whether the request succeeded.
    private bool TryPickupArmor(ArmorItemSO armor, float durability, out string message)
    {
        return TryPickupArmor(armor, durability, -1f, out message);
    }

    // Purpose: Picks up armor while preserving its instance maximum durability.
    private bool TryPickupArmor(
        ArmorItemSO armor,
        float durability,
        float maxDurability,
        out string message)
    {
        if (armor == null)
            return Fail("Armor is missing.", out message);

        if (_equipmentRuntime != null && _equipmentRuntime.CurrentArmor == null)
        {
            _equipmentRuntime.EquipArmor(armor, durability, maxDurability);
            message = $"{armor.DisplayName} equipped.";
            NotifyPickedUp(armor);
            NotifyChanged();
            return true;
        }

        if (!TryStoreItem(
                armor,
                1,
                durability,
                -1f,
                -1f,
                -1f,
                maxDurability,
                -1f,
                out message))
            return false;

        message = $"{armor.DisplayName} stored in inventory.";
        NotifyPickedUp(armor);
        return true;
    }

    // Purpose: Attempts to equip weapon from inventory and reports whether the request succeeded.
    private bool TryEquipWeaponFromInventory(int index, WeaponSO weapon, out string message)
    {
        return TryEquipWeaponFromInventory(
            index,
            weapon,
            ResolveAutomaticWeaponSlotIndex(),
            out message);
    }

    // Purpose: Attempts to equip weapon from inventory and reports whether the request succeeded.
    private bool TryEquipWeaponFromInventory(
        int index,
        WeaponSO weapon,
        int equipmentSlotIndex,
        out string message)
    {
        if (_equipmentRuntime != null && _equipmentRuntime.IsWeaponJammed(equipmentSlotIndex))
            return Fail("기존 총기를 T키로 기능처치한 후 교체하세요. 다른 무기 슬롯 선택은 가능합니다.", out message);
        if (weapon is MeleeWeaponSO)
            return Fail("근접 무기 교체는 WIP입니다. Combat Knife는 V키로 사용합니다.", out message);
        if (_equipmentRuntime == null && _equipmentController == null)
            return Fail("Shooter equipment runtime is missing.", out message);

        int amount = Mathf.Max(1, _items[index].Amount);
        float currentDurability = _items[index].WeaponCurrentDurability;
        float maxDurability = _items[index].WeaponMaxDurability;
        WeaponSO previous = GetEquippedWeapon(equipmentSlotIndex);
        int previousAmount = GetEquippedWeaponAmount(equipmentSlotIndex);
        float previousCurrentDurability = GetEquippedWeaponCurrentDurability(equipmentSlotIndex);
        float previousMaxDurability = GetEquippedWeaponMaxDurability(equipmentSlotIndex);

        EquipRuntimeWeapon(
            equipmentSlotIndex,
            weapon,
            amount,
            currentDurability,
            maxDurability);

        if (previous != null)
            ReplaceOrRemoveInventorySlot(
                index,
                previous,
                previousAmount,
                -1f,
                previousCurrentDurability,
                previousMaxDurability);
        else
            _items[index].Clear();

        message = $"{weapon.DisplayName} equipped.";
        NotifyChanged();
        return true;
    }

    // Purpose: Attempts to equip external weapon and reports whether the request succeeded.
    private bool TryEquipExternalWeapon(WeaponSO weapon, out string message)
    {
        return TryEquipExternalWeapon(
            weapon,
            1,
            ResolveAutomaticWeaponSlotIndex(),
            out message);
    }

    // Purpose: Attempts to equip external weapon and reports whether the request succeeded.
    private bool TryEquipExternalWeapon(
        WeaponSO weapon,
        int amount,
        int equipmentSlotIndex,
        out string message)
    {
        return TryEquipExternalWeapon(weapon, amount, -1f, -1f, equipmentSlotIndex, out message);
    }

    // Purpose: Attempts to equip external weapon and reports whether the request succeeded.
    private bool TryEquipExternalWeapon(
        WeaponSO weapon,
        int amount,
        float currentDurability,
        float maxDurability,
        int equipmentSlotIndex,
        out string message)
    {
        if (_equipmentRuntime != null && _equipmentRuntime.IsWeaponJammed(equipmentSlotIndex))
            return Fail("고장 난 총기를 T키로 기능처치한 후 교체하세요.", out message);
        if (weapon is MeleeWeaponSO)
            return Fail("근접 무기 교체는 WIP입니다. Combat Knife는 V키로 사용합니다.", out message);
        if (_equipmentRuntime == null && _equipmentController == null)
            return Fail("Shooter equipment runtime is missing.", out message);

        if (weapon == null)
            return Fail("Weapon is missing.", out message);

        WeaponSO previous = GetEquippedWeapon(equipmentSlotIndex);
        int previousAmount = GetEquippedWeaponAmount(equipmentSlotIndex);
        float previousCurrentDurability = GetEquippedWeaponCurrentDurability(equipmentSlotIndex);
        float previousMaxDurability = GetEquippedWeaponMaxDurability(equipmentSlotIndex);

        if (previous != null &&
            !TryStoreItem(
                previous,
                Mathf.Max(1, previousAmount),
                -1f,
                previousCurrentDurability,
                previousMaxDurability,
                out message))
            return false;

        EquipRuntimeWeapon(
            equipmentSlotIndex,
            weapon,
            amount,
            currentDurability,
            maxDurability);
        message = $"{weapon.DisplayName} equipped.";
        NotifyChanged();
        return true;
    }

    // Purpose: Attempts to equip armor from inventory and reports whether the request succeeded.
    private bool TryEquipArmorFromInventory(
        int index,
        ArmorItemSO armor,
        float durability,
        float maxDurability,
        out string message)
    {
        if (_equipmentRuntime == null)
            return Fail("Equipment runtime is missing.", out message);

        ArmorItemSO previous = _equipmentRuntime.CurrentArmor;
        float previousDurability = _equipmentRuntime.CurrentArmorDurability;
        float previousMaxDurability = _equipmentRuntime.MaxArmorDurability;
        _equipmentRuntime.EquipArmor(
            armor,
            durability >= 0f ? durability : armor.MaxDurability,
            maxDurability >= 0f ? maxDurability : armor.MaxDurability);
        ReplaceOrRemoveInventorySlot(index, previous, previousDurability, previousMaxDurability);

        message = $"{armor.DisplayName} equipped.";
        NotifyChanged();
        return true;
    }

    // Purpose: Attempts to equip external armor and reports whether the request succeeded.
    private bool TryEquipExternalArmor(
        ArmorItemSO armor,
        float durability,
        float maxDurability,
        out string message)
    {
        if (_equipmentRuntime == null)
            return Fail("Equipment runtime is missing.", out message);

        if (armor == null)
            return Fail("Armor is missing.", out message);

        ArmorItemSO previous = _equipmentRuntime.CurrentArmor;
        float previousDurability = _equipmentRuntime.CurrentArmorDurability;
        float previousMaxDurability = _equipmentRuntime.MaxArmorDurability;

        if (previous != null &&
            !TryStoreItem(
                previous,
                1,
                previousDurability,
                -1f,
                -1f,
                -1f,
                previousMaxDurability,
                -1f,
                out message))
            return false;

        _equipmentRuntime.EquipArmor(
            armor,
            durability >= 0f ? durability : armor.MaxDurability,
            maxDurability >= 0f ? maxDurability : armor.MaxDurability);

        message = $"{armor.DisplayName} equipped.";
        NotifyChanged();
        return true;
    }

    // Purpose: Attempts to equip bullet from inventory and reports whether the request succeeded.
    private bool TryEquipBulletFromInventory(int index, BulletSO bullet, out string message)
    {
        if (_bulletLoadout == null)
            return Fail("Bullet loadout is missing.", out message);

        int amount = Mathf.Max(1, _items[index].Amount);

        if (!_bulletLoadout.TryAcquireBulletToAvailableSlot(
                bullet,
                amount,
                0f,
                out _,
                out message))
        {
            return false;
        }

        _items[index].Clear();
        NotifyChanged();
        return true;
    }

    // Purpose: Attempts to equip bullet from inventory to first ammo slot and reports whether the request succeeded.
    private bool TryEquipBulletFromInventoryToFirstAmmoSlot(int index, BulletSO bullet, out string message)
    {
        if (_bulletLoadout == null)
            return Fail("Bullet loadout is missing.", out message);

        int amount = Mathf.Max(1, _items[index].Amount);

        if (_bulletLoadout.TryAcquireBulletToAvailableSlot(
                bullet,
                amount,
                0f,
                out _,
                out message))
        {
            _items[index].Clear();
            NotifyChanged();
            return true;
        }

        const int firstAmmoSlotIndex = 0;

        if (!_bulletLoadout.TryEquipBulletToSlotAndReturnPrevious(
                bullet,
                amount,
                0f,
                firstAmmoSlotIndex,
                out BulletSO previousBullet,
                out int previousAmount,
                out message,
                true))
        {
            return false;
        }

        if (previousBullet != null && previousAmount > 0)
            _items[index].Set(previousBullet, previousAmount);
        else
            _items[index].Clear();

        NotifyChanged();
        return true;
    }

    // Purpose: Attempts to store item and reports whether the request succeeded.
    private bool TryStoreItem(ItemSO item, int amount, out string message)
    {
        return TryStoreItem(item, amount, -1f, out message);
    }

    /// <summary>
    /// Restores a stack that was removed for a world drop when spawning the
    /// pickup failed. Item condition data is preserved exactly like a normal
    /// inventory transfer.
    /// </summary>
    public bool TryRestoreRemovedStack(
        PlayerInventoryRemovedStack removed,
        out string message)
    {
        if (!removed.HasItem)
            return Fail("Removed item stack is empty.", out message);

        return TryStoreItem(
            removed.item,
            removed.amount,
            removed.armorDurability,
            removed.weaponCurrentDurability,
            removed.weaponMaxDurability,
            removed.gasMaskFilterDurability,
            removed.armorMaxDurability,
            removed.gasMaskMaxFilterDurability,
            out message);
    }

    // Purpose: Attempts to store item and reports whether the request succeeded.
    private bool TryStoreItem(ItemSO item, int amount, float armorDurability, out string message)
    {
        return TryStoreItem(item, amount, armorDurability, -1f, -1f, out message);
    }

    // Purpose: Attempts to store item and reports whether the request succeeded.
    private bool TryStoreItem(
        ItemSO item,
        int amount,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        out string message)
    {
        return TryStoreItem(
            item,
            amount,
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            -1f,
            out message);
    }

    // Purpose: Stores an item while preserving armor, weapon, or gas-mask condition data.
    private bool TryStoreItem(
        ItemSO item,
        int amount,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        float gasMaskFilterDurability,
        out string message)
    {
        return TryStoreItem(
            item,
            amount,
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            gasMaskFilterDurability,
            -1f,
            -1f,
            out message);
    }

    // Purpose: Stores an item while retaining both current and reduced maximum condition.
    private bool TryStoreItem(
        ItemSO item,
        int amount,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        float gasMaskFilterDurability,
        float armorMaxDurability,
        float gasMaskMaxFilterDurability,
        out string message)
    {
        NormalizeStorage();

        if (item == null)
            return Fail("Item is missing.", out message);

        int remainingAmount = Mathf.Max(1, amount);
        int maxStack = ResolveMaxStack(item);
        bool stacked = false;

        if (CalculateStoreCapacity(item) < remainingAmount)
            return Fail("Inventory is full.", out message);

        if (CanStack(item))
        {
            while (remainingAmount > 0 &&
                   TryFindStackWithSpace(item, out PlayerInventoryItemStack existingStack))
            {
                int space = Mathf.Max(0, maxStack - existingStack.Amount);
                int addAmount = Mathf.Min(space, remainingAmount);

                if (addAmount <= 0)
                    break;

                existingStack.Add(addAmount);
                remainingAmount -= addAmount;
                stacked = true;
            }
        }

        while (remainingAmount > 0)
        {
            if (!TryFindEmptySlotIndex(out int emptyIndex))
                return Fail("Inventory is full.", out message);

            int stackAmount = Mathf.Min(maxStack, remainingAmount);
            PlayerInventoryItemStack stack = new();
            stack.Set(
                item,
                stackAmount,
                armorDurability,
                weaponCurrentDurability,
                weaponMaxDurability,
                gasMaskFilterDurability,
                armorMaxDurability,
                gasMaskMaxFilterDurability);
            _items[emptyIndex] = stack;
            remainingAmount -= stackAmount;
            // Legacy stacked medical saves have one partly-used leading unit.
            // When unfolded into non-stackable instances, only the first inherits that condition.
            if (item is ConsumableItemSO medical && medical.UsesDurability)
                weaponCurrentDurability = weaponMaxDurability = medical.MaxDurability;
        }

        message = stacked
            ? $"{ResolveItemName(item)} stacked."
            : $"{ResolveItemName(item)} stored.";
        NotifyChanged();
        return true;
    }

    // Purpose: Performs the replace or remove inventory slot workflow for this component.
    private void ReplaceOrRemoveInventorySlot(int index, ItemSO replacement)
    {
        ReplaceOrRemoveInventorySlot(index, replacement, -1f);
    }

    // Purpose: Performs the replace or remove inventory slot workflow for this component.
    private void ReplaceOrRemoveInventorySlot(int index, ItemSO replacement, float armorDurability)
    {
        ReplaceOrRemoveInventorySlot(index, replacement, armorDurability, -1f);
    }

    // Purpose: Replaces a slot with armor while retaining its current and maximum condition.
    private void ReplaceOrRemoveInventorySlot(
        int index,
        ItemSO replacement,
        float armorDurability,
        float armorMaxDurability)
    {
        ReplaceOrRemoveInventorySlot(index, replacement, 1, armorDurability, -1f, -1f, armorMaxDurability);
    }

    // Purpose: Performs the replace or remove inventory slot workflow for this component.
    private void ReplaceOrRemoveInventorySlot(
        int index,
        ItemSO replacement,
        int amount,
        float armorDurability)
    {
        ReplaceOrRemoveInventorySlot(index, replacement, amount, armorDurability, -1f, -1f, -1f);
    }

    // Purpose: Performs the replace or remove inventory slot workflow for this component.
    private void ReplaceOrRemoveInventorySlot(
        int index,
        ItemSO replacement,
        int amount,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability)
    {
        ReplaceOrRemoveInventorySlot(
            index,
            replacement,
            amount,
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            -1f);
    }

    // Purpose: Replaces a slot while preserving optional armor maximum condition.
    private void ReplaceOrRemoveInventorySlot(
        int index,
        ItemSO replacement,
        int amount,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        float armorMaxDurability)
    {
        if (!IsValidIndex(index))
            return;

        if (replacement == null)
            _items[index].Clear();
        else
            _items[index].Set(
                replacement,
                Mathf.Max(1, amount),
                armorDurability,
                weaponCurrentDurability,
                weaponMaxDurability,
                -1f,
                armorMaxDurability,
                -1f);

        NormalizeStorage();
    }

    // Purpose: Attempts to unequip weapon to inventory slot and reports whether the request succeeded.
    private bool TryUnequipWeaponToInventorySlot(int targetIndex, out string message)
    {
        return TryUnequipWeaponToInventorySlot(0, targetIndex, out message);
    }

    // Purpose: Attempts to unequip weapon to inventory slot and reports whether the request succeeded.
    private bool TryUnequipWeaponToInventorySlot(
        int equipmentSlotIndex,
        int targetIndex,
        out string message)
    {
        if (_equipmentRuntime != null && _equipmentRuntime.IsWeaponJammed(equipmentSlotIndex))
            return Fail("고장 난 총기는 T키 기능처치 후 옮길 수 있습니다.", out message);
        WeaponSO weapon = GetEquippedWeapon(equipmentSlotIndex);
        int amount = GetEquippedWeaponAmount(equipmentSlotIndex);
        float currentDurability = GetEquippedWeaponCurrentDurability(equipmentSlotIndex);
        float maxDurability = GetEquippedWeaponMaxDurability(equipmentSlotIndex);

        if ((_equipmentRuntime == null && _equipmentController == null) || weapon == null)
            return Fail("No weapon equipped.", out message);

        if (!TryPlaceItemIntoInventorySlot(
                weapon,
                Mathf.Max(1, amount),
                targetIndex,
                -1f,
                currentDurability,
                maxDurability,
                out message))
            return false;

        ClearEquippedWeapon(equipmentSlotIndex);
        message = $"{weapon.DisplayName} unequipped.";
        NotifyChanged();
        return true;
    }

    // Purpose: Attempts to unequip armor to inventory slot and reports whether the request succeeded.
    private bool TryUnequipArmorToInventorySlot(int targetIndex, out string message)
    {
        if (_equipmentRuntime == null || EquippedArmor == null)
            return Fail("No armor equipped.", out message);

        ArmorItemSO armor = EquippedArmor;

        float durability = _equipmentRuntime.CurrentArmorDurability;
        float maxDurability = _equipmentRuntime.MaxArmorDurability;

        if (!TryPlaceItemIntoInventorySlot(
                armor,
                1,
                targetIndex,
                durability,
                -1f,
                -1f,
                -1f,
                maxDurability,
                -1f,
                out message))
            return false;

        _equipmentRuntime.ClearArmor();
        message = $"{armor.DisplayName} unequipped.";
        NotifyChanged();
        return true;
    }

    // Purpose: Attempts to place item into inventory slot and reports whether the request succeeded.
    private bool TryPlaceItemIntoInventorySlot(
        ItemSO item,
        int amount,
        int targetIndex,
        out string message)
    {
        return TryPlaceItemIntoInventorySlot(item, amount, targetIndex, -1f, out message);
    }

    // Purpose: Attempts to place item into inventory slot and reports whether the request succeeded.
    private bool TryPlaceItemIntoInventorySlot(
        ItemSO item,
        int amount,
        int targetIndex,
        float armorDurability,
        out string message)
    {
        return TryPlaceItemIntoInventorySlot(
            item,
            amount,
            targetIndex,
            armorDurability,
            -1f,
            -1f,
            out message);
    }

    // Purpose: Attempts to place item into inventory slot and reports whether the request succeeded.
    private bool TryPlaceItemIntoInventorySlot(
        ItemSO item,
        int amount,
        int targetIndex,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        out string message)
    {
        return TryPlaceItemIntoInventorySlot(
            item,
            amount,
            targetIndex,
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            -1f,
            out message);
    }

    // Purpose: Places an item while preserving armor, weapon, or gas-mask condition data.
    private bool TryPlaceItemIntoInventorySlot(
        ItemSO item,
        int amount,
        int targetIndex,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        float gasMaskFilterDurability,
        out string message)
    {
        return TryPlaceItemIntoInventorySlot(
            item,
            amount,
            targetIndex,
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            gasMaskFilterDurability,
            -1f,
            -1f,
            out message);
    }

    // Purpose: Places an item while preserving current and reduced maximum condition values.
    private bool TryPlaceItemIntoInventorySlot(
        ItemSO item,
        int amount,
        int targetIndex,
        float armorDurability,
        float weaponCurrentDurability,
        float weaponMaxDurability,
        float gasMaskFilterDurability,
        float armorMaxDurability,
        float gasMaskMaxFilterDurability,
        out string message)
    {
        NormalizeStorage();

        if (item == null)
            return Fail("Item is missing.", out message);

        int resolvedAmount = Mathf.Max(1, amount);
        int maxStack = ResolveMaxStack(item);

        if (targetIndex >= 0)
        {
            if (!IsSlotInCapacity(targetIndex))
                return Fail("Invalid target slot.", out message);

            if (resolvedAmount > maxStack)
                return Fail("Target slot cannot hold the full stack.", out message);

            PlayerInventoryItemStack target = _items[targetIndex];

            if (target == null)
            {
                target = new PlayerInventoryItemStack();
                target.Clear();
                _items[targetIndex] = target;
            }

            if (!target.IsEmpty)
            {
                if (CanStack(item) &&
                    IsSameItemIdentity(target.Item, item) &&
                    target.Amount + resolvedAmount <= maxStack)
                {
                    target.Add(resolvedAmount);
                    message = $"{ResolveItemName(item)} stacked.";
                    NotifyChanged();
                    return true;
                }

                return Fail("Target inventory slot is occupied.", out message);
            }

            target.Set(
                item,
                resolvedAmount,
                armorDurability,
                weaponCurrentDurability,
                weaponMaxDurability,
                gasMaskFilterDurability,
                armorMaxDurability,
                gasMaskMaxFilterDurability);
            message = $"{ResolveItemName(item)} moved.";
            NotifyChanged();
            return true;
        }

        return TryStoreItem(
            item,
            resolvedAmount,
            armorDurability,
            weaponCurrentDurability,
            weaponMaxDurability,
            gasMaskFilterDurability,
            armorMaxDurability,
            gasMaskMaxFilterDurability,
            out message);
    }

    // Purpose: Attempts to find stack and reports whether the request succeeded.
    private bool TryFindStack(ItemSO item, out PlayerInventoryItemStack stack)
    {
        stack = null;

        for (int i = 0; i < _items.Count; i++)
        {
            PlayerInventoryItemStack candidate = _items[i];

            if (candidate == null || candidate.IsEmpty)
                continue;

            if (IsSameItemIdentity(candidate.Item, item))
            {
                stack = candidate;
                return true;
            }
        }

        return false;
    }

    // Purpose: Attempts to find stack with space and reports whether the request succeeded.
    private bool TryFindStackWithSpace(ItemSO item, out PlayerInventoryItemStack stack)
    {
        stack = null;

        if (!CanStack(item))
            return false;

        int maxStack = ResolveMaxStack(item);

        for (int i = 0; i < Capacity && i < _items.Count; i++)
        {
            PlayerInventoryItemStack candidate = _items[i];

            if (candidate == null || candidate.IsEmpty)
                continue;

            if (IsSameItemIdentity(candidate.Item, item) && candidate.Amount < maxStack)
            {
                stack = candidate;
                return true;
            }
        }

        return false;
    }

    // Purpose: Checks whether stack is currently permitted.
    private static bool CanStack(ItemSO item)
    {
        return ResolveMaxStack(item) > 1;
    }

    // Purpose: Checks whether same item identity is currently true.
    private static bool IsSameItemIdentity(ItemSO first, ItemSO second)
    {
        if (second is PassiveItemSO requested && !requested.IsRolledInstance && first is PassiveItemSO)
            return PassiveInstanceRuntime.MatchesDefinition(first, second);
        if (ReferenceEquals(first, second))
            return true;

        if (first == null || second == null)
            return false;

        string firstGuid = first.Guid;
        string secondGuid = second.Guid;
        return !string.IsNullOrWhiteSpace(firstGuid) &&
               !string.IsNullOrWhiteSpace(secondGuid) &&
               string.Equals(firstGuid, secondGuid, StringComparison.Ordinal);
    }

    // Purpose: Performs the log external loot stacking workflow for this component.
    private void LogExternalLootStacking(
        string phase,
        ItemSO item,
        int amount,
        int targetIndex,
        string message)
    {
        if (!_logExternalLootStacking)
            return;

        StringBuilder candidates = new StringBuilder();
        bool hasCandidate = false;

        if (item != null && _items != null)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                PlayerInventoryItemStack stack = _items[i];
                if (stack == null || stack.IsEmpty || stack.Item == null)
                    continue;

                bool sameIdentity = IsSameItemIdentity(stack.Item, item);
                bool sameDisplayName = string.Equals(stack.Item.name, item.name, StringComparison.Ordinal);
                if (!sameIdentity && !sameDisplayName)
                    continue;

                if (hasCandidate)
                    candidates.Append(" | ");

                candidates.Append($"slot={i}, item={stack.Item.name}, amount={stack.Amount}, " +
                                  $"sameRef={ReferenceEquals(stack.Item, item)}, sameIdentity={sameIdentity}, " +
                                  $"guid={stack.Item.Guid}, instance={stack.Item.GetInstanceID()}");
                hasCandidate = true;
            }
        }

        if (item is UsableItemSO usable && _quickSlotRuntime != null)
        {
            for (int i = 2; i < PlayerQuickSlotRuntime.SlotCount; i++)
            {
                if (!_quickSlotRuntime.TryGetSnapshot(i, out PlayerQuickSlotSnapshot snapshot) ||
                    snapshot.kind != PlayerQuickSlotBindingKind.Usable ||
                    snapshot.item is not UsableItemSO quickSlotItem)
                {
                    continue;
                }

                bool sameIdentity = IsSameItemIdentity(quickSlotItem, usable);
                bool sameDisplayName = string.Equals(quickSlotItem.name, usable.name, StringComparison.Ordinal);
                if (!sameIdentity && !sameDisplayName)
                    continue;

                if (hasCandidate)
                    candidates.Append(" | ");

                candidates.Append($"quickSlot={i}, item={quickSlotItem.name}, amount={snapshot.amount}, " +
                                  $"sameRef={ReferenceEquals(quickSlotItem, usable)}, sameIdentity={sameIdentity}, " +
                                  $"guid={quickSlotItem.Guid}, instance={quickSlotItem.GetInstanceID()}");
                hasCandidate = true;
            }
        }

        Debug.Log(
            $"[InventoryLootStack] phase={phase}, item={item?.name ?? "<null>"}, guid={item?.Guid ?? "<none>"}, " +
            $"instance={item?.GetInstanceID() ?? 0}, requested={Mathf.Max(1, amount)}, maxStack={ResolveMaxStack(item)}, " +
            $"targetIndex={targetIndex}, message={message}, candidates={(hasCandidate ? candidates.ToString() : "<none>")}",
            this);
    }


    // Purpose: Resolves the max stack needed by this component.
    private static int ResolveMaxStack(ItemSO item)
    {
        return item is ConsumableItemSO medical && medical.UsesDurability ? 1 :
            item != null ? Mathf.Max(1, item.MaxStack) : 1;
    }

    // Purpose: Calculates store capacity from the current inputs.
    private int CalculateStoreCapacity(ItemSO item)
    {
        if (item == null)
            return 0;

        int maxStack = ResolveMaxStack(item);
        int capacity = 0;
        bool canStack = CanStack(item);

        NormalizeStorage();

        // Storage may retain empty slots after dropping a bag. They are not
        // usable capacity; counting them can partially store a failed pickup.
        for (int i = 0; i < Capacity && i < _items.Count; i++)
        {
            PlayerInventoryItemStack candidate = _items[i];

            if (candidate == null || candidate.IsEmpty)
            {
                capacity += maxStack;
                continue;
            }

            if (canStack && IsSameItemIdentity(candidate.Item, item))
                capacity += Mathf.Max(0, maxStack - candidate.Amount);
        }

        return capacity;
    }

    // Purpose: Checks whether valid index is currently true.
    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < _items.Count && _items[index] != null && !_items[index].IsEmpty;
    }

    // Purpose: Checks whether slot in capacity is currently true.
    private bool IsSlotInCapacity(int index)
    {
        return index >= 0 && index < Capacity;
    }

    // Purpose: Counts the available used slots.
    private int CountUsedSlots()
    {
        int count = 0;

        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] != null && !_items[i].IsEmpty)
                count++;
        }

        return count;
    }

    // Purpose: Normalizes storage to a safe authored value.
    private void NormalizeStorage()
    {
        NormalizeInventoryStorage(_items, Capacity);
    }

    // Purpose: Attempts to find empty slot index and reports whether the request succeeded.
    private bool TryFindEmptySlotIndex(out int index)
    {
        NormalizeStorage();

        for (int i = 0; i < Capacity; i++)
        {
            if (_items[i] == null || _items[i].IsEmpty)
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_bulletLoadout == null)
            _bulletLoadout = GetComponent<PlayerBulletLoadoutRuntime>();

        if (_equipmentController == null)
            _equipmentController = EntityComponentResolver.Resolve<ShooterEquipmentController>(this);

        if (_equipmentRuntime == null)
            _equipmentRuntime = EntityComponentResolver.Resolve<EntityEquipmentRuntime>(this);

        if (_statsRuntime == null)
            _statsRuntime = EntityComponentResolver.Resolve<ShooterStatsRuntime>(this);

        if (_quickSlotRuntime == null)
            _quickSlotRuntime = EntityComponentResolver.Resolve<PlayerQuickSlotRuntime>(this);
    }

    // Purpose: Checks whether acquire owned amount is currently permitted.
    private bool CanAcquireOwnedAmount(
        ItemSO item,
        int amount,
        out string message)
    {
        if (item == null)
            return Fail("Item is missing.", out message);

        int maximum = item.MaxOwnedAmount;

        if (maximum <= 0)
        {
            message = null;
            return true;
        }

        int current = GetTotalOwnedItemAmount(item);
        int requested = Mathf.Max(1, amount);

        if (current + requested <= maximum)
        {
            message = null;
            return true;
        }

        message =
            $"{InventoryItemDisplayUtility.ResolveItemName(item)} ownership limit reached " +
            $"({current}/{maximum}).";
        return false;
    }

    // Purpose: Equips runtime weapon for the current owner.
    private void EquipRuntimeWeapon(WeaponSO weapon)
    {
        EquipRuntimeWeapon(ResolveAutomaticWeaponSlotIndex(), weapon, 1);
    }

    // Purpose: Resolves the automatic weapon slot index needed by this component.
    private int ResolveAutomaticWeaponSlotIndex()
    {
        ResolveRefs();

        if (_equipmentRuntime == null)
            return 0;

        int slotCount = _equipmentRuntime.WeaponSlotCount;

        for (int i = 0; i < slotCount; i++)
        {
            if (_equipmentRuntime.GetWeapon(i) == null)
                return i;
        }

        return _equipmentRuntime.ActiveWeaponSlotIndex;
    }

    // Purpose: Attempts to add weapon to matching equipment stack and reports whether the request succeeded.
    private bool TryAddWeaponToMatchingEquipmentStack(
        WeaponSO weapon,
        int amount,
        out string message)
    {
        message = string.Empty;

        if (_equipmentRuntime == null || weapon == null || !CanStack(weapon))
            return false;

        int maxStack = ResolveMaxStack(weapon);
        int equipmentCapacity = 0;

        for (int i = 0; i < _equipmentRuntime.WeaponSlotCount; i++)
        {
            if (_equipmentRuntime.GetWeapon(i) != weapon)
                continue;

            equipmentCapacity += Mathf.Max(
                0,
                maxStack - _equipmentRuntime.GetWeaponAmount(i));
        }

        if (equipmentCapacity <= 0)
            return false;

        int remaining = Mathf.Max(1, amount);
        int inventoryOverflow = Mathf.Max(0, remaining - equipmentCapacity);

        if (inventoryOverflow > 0)
        {
            if (CalculateStoreCapacity(weapon) < inventoryOverflow ||
                !TryStoreItem(weapon, inventoryOverflow, out message))
            {
                return false;
            }

            remaining -= inventoryOverflow;
        }

        for (int i = 0; i < _equipmentRuntime.WeaponSlotCount && remaining > 0; i++)
        {
            if (_equipmentRuntime.GetWeapon(i) != weapon)
                continue;

            int currentAmount = _equipmentRuntime.GetWeaponAmount(i);
            int addAmount = Mathf.Min(
                remaining,
                Mathf.Max(0, maxStack - currentAmount));

            if (addAmount <= 0)
                continue;

            _equipmentRuntime.EquipWeapon(i, weapon, currentAmount + addAmount);
            remaining -= addAmount;
        }

        message = $"{weapon.DisplayName} stacked in equipment.";
        return remaining <= 0;
    }

    // Purpose: Equips runtime weapon for the current owner.
    private void EquipRuntimeWeapon(int equipmentSlotIndex, WeaponSO weapon, int amount)
    {
        EquipRuntimeWeapon(equipmentSlotIndex, weapon, amount, -1f, -1f);
    }

    // Purpose: Equips runtime weapon for the current owner.
    private void EquipRuntimeWeapon(
        int equipmentSlotIndex,
        WeaponSO weapon,
        int amount,
        float currentDurability,
        float maxDurability)
    {
        if (_equipmentRuntime != null)
        {
            _equipmentRuntime.EquipWeapon(
                equipmentSlotIndex,
                weapon,
                amount,
                currentDurability,
                maxDurability);
            _equipmentRuntime.SetActiveWeaponSlot(equipmentSlotIndex);
            return;
        }

        _equipmentController?.Equip(weapon);
    }

    // Purpose: Clears the current equipped weapon state.
    private void ClearEquippedWeapon()
    {
        ClearEquippedWeapon(0);
    }

    // Purpose: Clears the current equipped weapon state.
    private void ClearEquippedWeapon(int equipmentSlotIndex)
    {
        if (_equipmentRuntime != null)
        {
            _equipmentRuntime.ClearWeapon(equipmentSlotIndex);
            return;
        }

        _equipmentController?.ClearWeapon();
    }

    // Purpose: Returns the current equipped weapon.
    public WeaponSO GetEquippedWeapon(int equipmentSlotIndex)
    {
        ResolveRefs();

        if (_equipmentRuntime != null)
            return _equipmentRuntime.GetWeapon(equipmentSlotIndex);

        return _equipmentController != null ? _equipmentController.GetWeapon(equipmentSlotIndex) : null;
    }

    // Purpose: Returns the current equipped weapon amount.
    public int GetEquippedWeaponAmount(int equipmentSlotIndex)
    {
        ResolveRefs();

        if (_equipmentRuntime != null)
            return _equipmentRuntime.GetWeaponAmount(equipmentSlotIndex);

        return _equipmentController != null ? _equipmentController.GetWeaponAmount(equipmentSlotIndex) : 0;
    }

    // Purpose: Attempts to consume equipped weapon and reports whether the request succeeded.
    public bool TryConsumeEquippedWeapon(
        int equipmentSlotIndex,
        int amount,
        out WeaponSO consumedWeapon,
        out string message)
    {
        ResolveRefs();
        consumedWeapon = null;

        if (_equipmentRuntime != null)
        {
            if (!_equipmentRuntime.TryConsumeWeaponStack(
                    equipmentSlotIndex,
                    amount,
                    out consumedWeapon,
                    out _))
            {
                return Fail("No equipped weapon stack to consume.", out message);
            }

            message = $"{ResolveItemName(consumedWeapon)} consumed.";
            NotifyChanged();
            return true;
        }

        if (_equipmentController != null &&
            _equipmentController.TryConsumeWeaponStack(
                equipmentSlotIndex,
                amount,
                out consumedWeapon,
                out _))
        {
            message = $"{ResolveItemName(consumedWeapon)} consumed.";
            NotifyChanged();
            return true;
        }

        return Fail("No equipped weapon stack to consume.", out message);
    }

    // Purpose: Publishes the picked up notification.
    private void NotifyPickedUp(ItemSO item)
    {
        ItemPickedUp?.Invoke(item);
    }

    // Purpose: Publishes the changed notification.
    private void NotifyChanged()
    {
        _carryWeightFrame = -1;
        // A crafting transaction publishes only after inventory and its next recipe are durable.
        if (_craftingTransaction) return;
        NormalizeStorage();
        OnChanged?.Invoke();
    }

    // Purpose: Performs the reject workflow for this component.
    private void Reject(ItemSO item, string message)
    {
        PickupRejected?.Invoke(item, message);
    }

    // Purpose: Performs the trace bullet inventory transaction workflow for this component.
    private void TraceBulletInventoryTransaction(
        string action,
        BulletSO bullet,
        string details)
    {
        if (bullet == null ||
            _bulletLoadout == null ||
            !_bulletLoadout.IsAmmoTransactionTraceEnabled)
        {
            return;
        }

        _bulletLoadout.TraceAmmoTransaction(
            "Inventory",
            action,
            bullet,
            null,
            details);
    }

    // Purpose: Marks the active fail workflow as failed.
    private static bool Fail(string text, out string message)
    {
        message = text;
        return false;
    }

    // Purpose: Resolves the item name needed by this component.
    private static string ResolveItemName(ItemSO item)
    {
        if (item == null)
            return "Item";

        if (item is BulletSO bullet)
            return bullet.DisplayName;

        if (item is WeaponSO weapon)
            return weapon.DisplayName;

        if (item is ArmorItemSO armor)
            return armor.DisplayName;

        return item.name;
    }
}
