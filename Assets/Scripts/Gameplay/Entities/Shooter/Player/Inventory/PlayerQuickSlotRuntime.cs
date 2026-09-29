/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player > Inventory
 * Primary responsibility: Collects PlayerQuickSlotBindingKind input and forwards it as gameplay action requests.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

public enum PlayerQuickSlotBindingKind
{
    None = 0,
    Weapon = 1,
    Ammo = 2,
    Usable = 3
}

[Serializable]
public sealed class PlayerQuickSlotBinding
{
    [SerializeField] private PlayerQuickSlotBindingKind _kind;
    [SerializeField, Min(0)] private int _sourceSlotIndex;
    [SerializeField] private UsableItemSO _usableItem;
    [SerializeField, Min(0)] private int _amount;

    public PlayerQuickSlotBindingKind Kind => _kind;
    public int SourceSlotIndex => Mathf.Max(0, _sourceSlotIndex);
    public UsableItemSO UsableItem => _usableItem;
    public int Amount => Mathf.Max(0, _amount);

    public PlayerQuickSlotBinding()
    {
    }

    public PlayerQuickSlotBinding(
        PlayerQuickSlotBindingKind kind,
        int sourceSlotIndex)
    {
        Set(kind, sourceSlotIndex, null, 0);
    }

    // Purpose: Updates the set state.
    public void Set(
        PlayerQuickSlotBindingKind kind,
        int sourceSlotIndex,
        UsableItemSO usableItem)
    {
        Set(kind, sourceSlotIndex, usableItem, 0);
    }

    // Purpose: Updates the set state.
    public void Set(
        PlayerQuickSlotBindingKind kind,
        int sourceSlotIndex,
        UsableItemSO usableItem,
        int amount)
    {
        _kind = kind;
        _sourceSlotIndex = Mathf.Max(0, sourceSlotIndex);
        _usableItem = kind == PlayerQuickSlotBindingKind.Usable
            ? usableItem
            : null;
        _amount = _usableItem != null
            ? Mathf.Max(0, amount)
            : 0;
    }

    // Purpose: Updates the usable state.
    public void SetUsable(UsableItemSO usableItem, int amount)
    {
        Set(
            PlayerQuickSlotBindingKind.Usable,
            0,
            usableItem,
            amount);
    }

    // Purpose: Removes the requested remove from this component.
    public bool Remove(int amount)
    {
        if (_kind != PlayerQuickSlotBindingKind.Usable ||
            _usableItem == null ||
            amount <= 0 ||
            _amount < amount)
        {
            return false;
        }

        _amount -= amount;
        return true;
    }

    // Purpose: Clears the current clear state.
    public void Clear()
    {
        _kind = PlayerQuickSlotBindingKind.None;
        _sourceSlotIndex = 0;
        _usableItem = null;
        _amount = 0;
    }
}

public readonly struct PlayerQuickSlotSnapshot
{
    public readonly int slotIndex;
    public readonly PlayerQuickSlotBindingKind kind;
    public readonly ItemSO item;
    public readonly int amount;
    public readonly bool isSelected;
    public readonly float currentDurability;
    public readonly float maxDurability;
    public readonly WeaponAmmoSlotSnapshot ammo;

    public bool IsEmpty => item == null;

    public PlayerQuickSlotSnapshot(
        int slotIndex,
        PlayerQuickSlotBindingKind kind,
        ItemSO item,
        int amount,
        bool isSelected,
        float currentDurability,
        float maxDurability,
        WeaponAmmoSlotSnapshot ammo)
    {
        this.slotIndex = slotIndex;
        this.kind = kind;
        this.item = item;
        this.amount = Mathf.Max(0, amount);
        this.isSelected = isSelected;
        this.currentDurability = Mathf.Max(0f, currentDurability);
        this.maxDurability = Mathf.Max(0f, maxDurability);
        this.ammo = ammo;
    }
}

/// <summary>
/// Slots 3-5 are inventory-backed usable quick slots.
/// Slots 1-2 are intentionally reserved for the two weapon equipment slots
/// and are not owned by this runtime.
/// </summary>
[DisallowMultipleComponent]
public class PlayerQuickSlotRuntime : MonoBehaviour
{
    public const int SlotCount = 5;

    [Header("Refs")]
    [SerializeField] private PlayerInventoryRuntime _inventoryRuntime;
    [SerializeField] private EntityEquipmentRuntime _equipmentRuntime;
    [SerializeField] private PlayerBulletLoadoutRuntime _bulletLoadout;
    [SerializeField] private ShooterEquipmentController _equipmentController;
    [SerializeField] private PlayerConsumableUseRuntime _consumableUseRuntime;
    [SerializeField] private SystemMessageEventChannelSO _systemMessageChannel;

    [Header("Bindings")]
    [Tooltip(
        "Only indices 2-4 are usable quick slots (keyboard 3-5). " +
        "Indices 0-1 are reserved for weapon equipment selection.")]
    [SerializeField] private PlayerQuickSlotBinding[] _bindings =
    {
        new PlayerQuickSlotBinding(),
        new PlayerQuickSlotBinding(),
        new PlayerQuickSlotBinding(),
        new PlayerQuickSlotBinding(),
        new PlayerQuickSlotBinding()
    };

    [Tooltip(
        "At startup, usable item stacks may move from the backpack into " +
        "empty quick slots 3-5.")]
    [SerializeField] private bool _autoBindUsablesToEmptyAmmoBindings = true;

    [Header("Runtime Don't Touch")]
    [SerializeField, ReadOnly] private int _activeQuickSlotIndex = -1;

    private GameObject _heldUsableView;
    private WeaponView _heldUsableWeaponView;
    private UsableItemSO _heldUsableItem;
    private GameObject _heldUsablePrefab;
    private bool _autoBindPassCompleted;
    private bool _isMutatingStorage;

    public event Action OnChanged;

    public int ActiveQuickSlotIndex => _activeQuickSlotIndex;

    public UsableItemSO ActiveUsableItem =>
        ResolveActiveUsable(true);

    public ThrowableUsableItemSO ActiveThrowable =>
        ActiveUsableItem as ThrowableUsableItemSO;

    public Transform ActiveUsableFireOrigin =>
        _heldUsableWeaponView != null && _heldUsableWeaponView.HasFireOrigin
            ? _heldUsableWeaponView.FireOrigin
            : null;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        ResolveRefs();
        NormalizeBindings();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
        NormalizeBindings();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveRefs();
        NormalizeBindings();
        Subscribe();
        EnsureEquippedItemsHaveBindings();
        TryAutoBindUsables();
        RefreshSelectionVisual();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        Unsubscribe();
        ClearHeldUsableView();
        _equipmentController?.SetWeaponViewSuppressed(false);
    }

    // Purpose: Performs the activate workflow for this component.
    public bool Activate(
        int quickSlotIndex,
        out string message)
    {
        NormalizeBindings();

        if (!IsValidSlot(quickSlotIndex))
        {
            message = "Invalid quick slot.";
            return false;
        }

        if (quickSlotIndex < 2)
        {
            message = "Slots 1-2 are weapon slots.";
            return false;
        }

        if (_consumableUseRuntime != null &&
            _consumableUseRuntime.IsUsing &&
            quickSlotIndex != _activeQuickSlotIndex)
        {
            message = "An item is being used.";
            return false;
        }

        PlayerQuickSlotBinding binding = _bindings[quickSlotIndex];

        switch (binding.Kind)
        {
            case PlayerQuickSlotBindingKind.Usable:
                if (binding.UsableItem == null ||
                    binding.Amount <= 0)
                {
                    message = "This quick slot is empty.";
                    return false;
                }

                break;

            default:
                message = "This quick slot is empty.";
                return false;
        }

        _activeQuickSlotIndex = quickSlotIndex;
        RefreshSelectionVisual();
        OnChanged?.Invoke();

        message = null;
        return true;
    }

    /// <summary>
    /// Returns control to the selected equipment weapon. Weapon slot selection
    /// calls this so weapon slots 1-2 and usable slots 3-5 form one exclusive
    /// held-item selection.
    /// </summary>
    public bool DeactivateActiveUsable()
    {
        if (_activeQuickSlotIndex < 2)
            return false;

        _activeQuickSlotIndex = -1;
        RefreshSelectionVisual();
        OnChanged?.Invoke();
        return true;
    }

    // Purpose: Attempts to use active consumable and reports whether the request succeeded.
    public bool TryUseActiveConsumable(out string message)
    {
        ConsumableItemSO consumable =
            ActiveUsableItem as ConsumableItemSO;

        if (consumable == null)
        {
            message =
                "The selected quick slot is not a consumable.";
            return false;
        }

        if (_consumableUseRuntime == null)
        {
            message = "Consumable runtime is missing.";
            return false;
        }

        return _consumableUseRuntime.TryBeginUseFromQuickSlot(
            _activeQuickSlotIndex,
            consumable,
            out message);
    }

    // Purpose: Attempts to consume usable and reports whether the request succeeded.
    public bool TryConsumeUsable(
        UsableItemSO item,
        int amount,
        out string message)
    {
        NormalizeBindings();

        if (item == null || amount <= 0)
        {
            message = "Invalid usable item amount.";
            return false;
        }

        if (!IsValidSlot(_activeQuickSlotIndex))
        {
            message = "No usable quick slot is selected.";
            return false;
        }

        PlayerQuickSlotBinding binding =
            _bindings[_activeQuickSlotIndex];

        if (binding.Kind != PlayerQuickSlotBindingKind.Usable ||
            binding.UsableItem != item ||
            binding.Amount < amount ||
            !binding.Remove(amount))
        {
            message = "The selected quick slot does not contain enough items.";
            return false;
        }

        if (binding.Amount <= 0)
        {
            binding.Clear();
            _activeQuickSlotIndex = -1;
        }

        RefreshSelectionVisual();
        OnChanged?.Invoke();
        message = $"{item.UsableDisplayName} consumed.";
        return true;
    }

    // Purpose: Attempts to get snapshot and reports whether the request succeeded.
    public bool TryGetSnapshot(
        int quickSlotIndex,
        out PlayerQuickSlotSnapshot snapshot)
    {
        NormalizeBindings();
        snapshot = default;

        if (!IsValidSlot(quickSlotIndex))
            return false;

        PlayerQuickSlotBinding binding =
            _bindings[quickSlotIndex];

        ItemSO item = null;
        int amount = 0;
        float currentDurability = 0f;
        float maxDurability = 0f;
        WeaponAmmoSlotSnapshot ammo = default;

        switch (binding.Kind)
        {
            case PlayerQuickSlotBindingKind.Weapon:
                if (_equipmentRuntime != null)
                {
                    item = _equipmentRuntime.GetWeapon(
                        binding.SourceSlotIndex);

                    amount = _equipmentRuntime.GetWeaponAmount(
                        binding.SourceSlotIndex);

                    currentDurability =
                        _equipmentRuntime.GetWeaponCurrentDurability(
                            binding.SourceSlotIndex);

                    maxDurability =
                        _equipmentRuntime.GetWeaponMaxDurability(
                            binding.SourceSlotIndex);
                }

                break;

            case PlayerQuickSlotBindingKind.Ammo:
                if (_bulletLoadout != null &&
                    _bulletLoadout.TryGetSlotSnapshot(
                        binding.SourceSlotIndex,
                        out ammo))
                {
                    item = ammo.bullet;
                    amount =
                        Mathf.Max(0, ammo.currentAmmo) +
                        Mathf.Max(0, ammo.reserveAmmo);
                }

                break;

            case PlayerQuickSlotBindingKind.Usable:
                item = binding.UsableItem;
                amount = binding.Amount;
                break;
        }

        snapshot = new PlayerQuickSlotSnapshot(
            quickSlotIndex,
            binding.Kind,
            item,
            amount,
            quickSlotIndex == _activeQuickSlotIndex,
            currentDurability,
            maxDurability,
            ammo);

        return true;
    }

    /// <summary>
    /// Returns free space in existing usable quick-slot stacks for this logical
    /// item. Enemy/world loot uses this before creating a backpack stack.
    /// </summary>
    public int GetMatchingUsableStackSpace(UsableItemSO usableItem)
    {
        NormalizeBindings();

        if (usableItem == null)
            return 0;

        int available = 0;

        for (int i = 2; i < SlotCount; i++)
        {
            PlayerQuickSlotBinding binding = _bindings[i];
            if (binding == null ||
                binding.Kind != PlayerQuickSlotBindingKind.Usable ||
                !IsSameUsableIdentity(binding.UsableItem, usableItem))
            {
                continue;
            }

            available += Mathf.Max(0, Mathf.Max(1, binding.UsableItem.MaxStack) - binding.Amount);
        }

        return available;
    }

    /// <summary>
    /// Adds an externally acquired usable item to matching quick-slot stacks.
    /// The caller keeps any remainder in the backpack.
    /// </summary>
    public bool TryAddToMatchingUsableStacks(
        UsableItemSO usableItem,
        int amount,
        out int addedAmount,
        out string message)
    {
        NormalizeBindings();
        addedAmount = 0;

        if (usableItem == null || amount <= 0)
        {
            message = "Invalid usable stack transfer.";
            return false;
        }

        int remaining = Mathf.Max(1, amount);

        for (int i = 2; i < SlotCount && remaining > 0; i++)
        {
            PlayerQuickSlotBinding binding = _bindings[i];
            if (binding == null ||
                binding.Kind != PlayerQuickSlotBindingKind.Usable ||
                !IsSameUsableIdentity(binding.UsableItem, usableItem))
            {
                continue;
            }

            int available = Mathf.Max(0, Mathf.Max(1, binding.UsableItem.MaxStack) - binding.Amount);
            int added = Mathf.Min(available, remaining);
            if (added <= 0)
                continue;

            binding.SetUsable(binding.UsableItem, binding.Amount + added);
            remaining -= added;
            addedAmount += added;
        }

        if (addedAmount <= 0)
        {
            message = "No matching quick slot has stack space.";
            return false;
        }

        RefreshSelectionVisual();
        OnChanged?.Invoke();
        message = $"{ResolveItemName(usableItem)} added to matching quick slot stack.";
        return true;
    }

    // Purpose: Binds weapon to the active runtime or UI target.
    public bool BindWeapon(
        int quickSlotIndex,
        int equipmentWeaponSlotIndex,
        out string message)
    {
        if (_equipmentRuntime == null ||
            _equipmentRuntime.GetWeapon(
                equipmentWeaponSlotIndex) == null)
        {
            message = "Only an equipped weapon can be bound.";
            return false;
        }

        return SetBinding(
            quickSlotIndex,
            PlayerQuickSlotBindingKind.Weapon,
            equipmentWeaponSlotIndex,
            null,
            out message);
    }

    // Purpose: Binds ammo to the active runtime or UI target.
    public bool BindAmmo(
        int quickSlotIndex,
        int equipmentAmmoSlotIndex,
        out string message)
    {
        if (_bulletLoadout == null ||
            !_bulletLoadout.TryGetSlotSnapshot(
                equipmentAmmoSlotIndex,
                out WeaponAmmoSlotSnapshot ammo) ||
            ammo.isEmpty)
        {
            message =
                "Only equipped ammunition can be bound.";
            return false;
        }

        return SetBinding(
            quickSlotIndex,
            PlayerQuickSlotBindingKind.Ammo,
            equipmentAmmoSlotIndex,
            null,
            out message);
    }

    // Purpose: Binds usable to the active runtime or UI target.
    public bool BindUsable(
        int quickSlotIndex,
        UsableItemSO usableItem,
        out string message)
    {
        if (quickSlotIndex < 2)
        {
            message = "Usable items can only be bound to slots 3-5.";
            return false;
        }

        if (usableItem == null ||
            _inventoryRuntime == null ||
            !_inventoryRuntime.TryFindInventoryItemSlot(
                usableItem,
                out int inventoryIndex))
        {
            message =
                "Only a usable item in the inventory can be bound.";
            return false;
        }

        return TryAssignInventoryItem(
            inventoryIndex,
            quickSlotIndex,
            out message);
    }

    // Purpose: Attempts to assign inventory item and reports whether the request succeeded.
    public bool TryAssignInventoryItem(
        int inventoryIndex,
        int quickSlotIndex,
        out string message)
    {
        ResolveRefs();
        NormalizeBindings();

        if (!IsValidSlot(quickSlotIndex))
        {
            message = "Invalid quick slot.";
            return false;
        }

        if (quickSlotIndex < 2)
        {
            message = "Usable items can only be moved to slots 3-5.";
            return false;
        }

        if (_inventoryRuntime == null ||
            inventoryIndex < 0 ||
            inventoryIndex >= _inventoryRuntime.Items.Count)
        {
            message = "Invalid inventory slot.";
            return false;
        }

        PlayerInventoryItemStack stack =
            _inventoryRuntime.Items[inventoryIndex];

        if (stack == null || stack.IsEmpty)
        {
            message = "This inventory slot is empty.";
            return false;
        }

        if (stack.Item is UsableItemSO usable)
        {
            PlayerQuickSlotBinding target =
                _bindings[quickSlotIndex];

            if (target.Kind == PlayerQuickSlotBindingKind.Usable &&
                IsSameUsableIdentity(target.UsableItem, usable))
            {
                int maxStack = Mathf.Max(1, usable.MaxStack);
                int availableSpace = Mathf.Max(0, maxStack - target.Amount);

                if (availableSpace <= 0)
                {
                    message = "This quick slot stack is full.";
                    return false;
                }

                int transferAmount = Mathf.Min(
                    availableSpace,
                    stack.Amount);
                bool removed;
                PlayerInventoryRemovedStack transferred;
                _isMutatingStorage = true;

                try
                {
                    removed = _inventoryRuntime.TryRemoveInventoryItem(
                        inventoryIndex,
                        transferAmount,
                        out transferred,
                        out message);

                    if (removed)
                    {
                        target.SetUsable(
                            usable,
                            target.Amount + transferred.amount);
                    }
                }
                finally
                {
                    _isMutatingStorage = false;
                }

                if (!removed)
                    return false;

                _autoBindPassCompleted = true;
                RefreshSelectionVisual();
                OnChanged?.Invoke();
                message =
                    $"{ResolveItemName(usable)} stacked in quick slot " +
                    $"({target.Amount}/{maxStack}).";
                return true;
            }

            PlayerInventoryRemovedStack quickStack =
                CreateStoredStack(target);

            bool exchanged;
            PlayerInventoryRemovedStack movedFromInventory;
            _isMutatingStorage = true;

            try
            {
                exchanged =
                    _inventoryRuntime.TryExchangeInventorySlotWithExternalStack(
                        inventoryIndex,
                        quickStack,
                        out movedFromInventory,
                        out message);
            }
            finally
            {
                _isMutatingStorage = false;
            }

            if (!exchanged ||
                movedFromInventory.item is not UsableItemSO movedUsable)
            {
                return false;
            }

            target.SetUsable(
                movedUsable,
                movedFromInventory.amount);
            _autoBindPassCompleted = true;
            RefreshSelectionVisual();
            OnChanged?.Invoke();
            message = quickStack.HasItem
                ? "Inventory and quick slot items swapped."
                : $"{ResolveItemName(movedUsable)} moved to quick slot.";
            return true;
        }

        message = "Only usable items can be assigned to slots 3-5.";
        return false;
    }

    // Purpose: Attempts to unequip quick slot and reports whether the request succeeded.
    public bool TryUnequipQuickSlot(
        int quickSlotIndex,
        int targetInventoryIndex,
        out string message)
    {
        ResolveRefs();
        NormalizeBindings();

        if (!IsValidSlot(quickSlotIndex))
        {
            message = "Invalid quick slot.";
            return false;
        }

        PlayerQuickSlotBinding binding =
            _bindings[quickSlotIndex];

        switch (binding.Kind)
        {
            case PlayerQuickSlotBindingKind.Weapon:
                // _inventoryRuntime가 null이면 아래 메서드가 호출되지 않으므로
                // message를 이 분기에서 직접 할당해야 한다.
                if (_inventoryRuntime == null)
                {
                    message =
                        "Player inventory runtime is missing.";
                    return false;
                }

                if (!_inventoryRuntime.TryUnequipToInventorySlot(
                        PlayerInventoryEquipmentSlotKind.Weapon,
                        binding.SourceSlotIndex,
                        targetInventoryIndex,
                        out message))
                {
                    return false;
                }

                ClearBindingsForSource(
                    PlayerQuickSlotBindingKind.Weapon,
                    binding.SourceSlotIndex);

                return true;

            case PlayerQuickSlotBindingKind.Ammo:
                // Weapon 분기와 동일하게 null 경로에서도 message를
                // 반드시 할당한다.
                if (_inventoryRuntime == null)
                {
                    message =
                        "Player inventory runtime is missing.";
                    return false;
                }

                if (!_inventoryRuntime.TryUnequipAmmoSlotToInventorySlot(
                        binding.SourceSlotIndex,
                        targetInventoryIndex,
                        out message))
                {
                    return false;
                }

                ClearBindingsForSource(
                    PlayerQuickSlotBindingKind.Ammo,
                    binding.SourceSlotIndex);

                return true;

            case PlayerQuickSlotBindingKind.Usable:
                if (_inventoryRuntime == null)
                {
                    message = "Player inventory runtime is missing.";
                    return false;
                }

                int resolvedTargetIndex = targetInventoryIndex;

                if (resolvedTargetIndex < 0 &&
                    !_inventoryRuntime.TryFindFirstEmptyInventorySlot(
                        out resolvedTargetIndex))
                {
                    message = "Inventory is full.";
                    return false;
                }

                if (resolvedTargetIndex < 0 ||
                    resolvedTargetIndex >= _inventoryRuntime.Items.Count)
                {
                    message = "Invalid inventory slot.";
                    return false;
                }

                PlayerInventoryItemStack targetInventoryStack =
                    _inventoryRuntime.Items[resolvedTargetIndex];

                if (targetInventoryStack != null &&
                    !targetInventoryStack.IsEmpty &&
                    targetInventoryStack.Item is not UsableItemSO)
                {
                    message =
                        "A quick slot can only swap with an empty or usable inventory slot.";
                    return false;
                }

                bool moved;
                PlayerInventoryRemovedStack displaced;
                _isMutatingStorage = true;

                try
                {
                    moved =
                        _inventoryRuntime.TryExchangeInventorySlotWithExternalStack(
                            resolvedTargetIndex,
                            CreateStoredStack(binding),
                            out displaced,
                            out message);
                }
                finally
                {
                    _isMutatingStorage = false;
                }

                if (!moved)
                    return false;

                if (displaced.HasItem)
                {
                    if (displaced.item is not UsableItemSO displacedUsable)
                    {
                        message = "The displaced item is not usable.";
                        return false;
                    }

                    binding.SetUsable(
                        displacedUsable,
                        displaced.amount);
                }
                else
                {
                    binding.Clear();

                    if (_activeQuickSlotIndex == quickSlotIndex)
                        _activeQuickSlotIndex = -1;
                }

                RefreshSelectionVisual();
                OnChanged?.Invoke();
                message = displaced.HasItem
                    ? "Inventory and quick slot items swapped."
                    : "Quick slot item moved to inventory.";
                return true;

            default:
                message = "This quick slot is empty.";
                return false;
        }
    }

    // Purpose: Attempts to unequip quick slot and reports whether the request succeeded.
    public bool TryUnequipQuickSlot(
        int quickSlotIndex,
        out string message)
    {
        return TryUnequipQuickSlot(
            quickSlotIndex,
            -1,
            out message);
    }

    // Purpose: Performs the swap bindings workflow for this component.
    public bool SwapBindings(
        int firstSlotIndex,
        int secondSlotIndex,
        out string message)
    {
        NormalizeBindings();

        if (!IsValidSlot(firstSlotIndex) ||
            !IsValidSlot(secondSlotIndex))
        {
            message = "Invalid quick slot.";
            return false;
        }

        if (firstSlotIndex < 2 || secondSlotIndex < 2)
        {
            message = "Only usable quick slots 3-5 can be swapped.";
            return false;
        }

        if (firstSlotIndex == secondSlotIndex)
        {
            message = null;
            return true;
        }

        PlayerQuickSlotBinding temporary =
            _bindings[firstSlotIndex];

        _bindings[firstSlotIndex] =
            _bindings[secondSlotIndex];

        _bindings[secondSlotIndex] =
            temporary;

        if (_activeQuickSlotIndex == firstSlotIndex)
        {
            _activeQuickSlotIndex = secondSlotIndex;
        }
        else if (_activeQuickSlotIndex == secondSlotIndex)
        {
            _activeQuickSlotIndex = firstSlotIndex;
        }

        RefreshSelectionVisual();
        OnChanged?.Invoke();

        message = "Quick slots swapped.";
        return true;
    }

    // Purpose: Clears the current binding state.
    public void ClearBinding(int quickSlotIndex)
    {
        NormalizeBindings();

        if (!IsValidSlot(quickSlotIndex))
            return;

        _bindings[quickSlotIndex].Clear();

        if (_activeQuickSlotIndex == quickSlotIndex)
            _activeQuickSlotIndex = -1;

        RefreshSelectionVisual();
        OnChanged?.Invoke();
    }

    /// <summary>
    /// Clears every runtime quick-slot binding when a run ends. Quick slots are
    /// not persistent storage; the inheritance warehouse owns persistent items.
    /// </summary>
    public void ClearAllBindingsForRunLoss()
    {
        NormalizeBindings();

        bool changed = false;

        for (int i = 0; i < _bindings.Length; i++)
        {
            if (_bindings[i].Kind == PlayerQuickSlotBindingKind.None)
                continue;

            _bindings[i].Clear();
            changed = true;
        }

        _activeQuickSlotIndex = -1;
        ClearHeldUsableView();
        _equipmentController?.SetWeaponViewSuppressed(false);

        if (!changed)
            return;

        RefreshSelectionVisual();
        OnChanged?.Invoke();
    }

    // Purpose: Publishes the message notification.
    public void RaiseMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (_systemMessageChannel != null)
        {
            _systemMessageChannel.RaiseEvent(message);
        }
        else
        {
            Debug.Log(
                $"[PlayerQuickSlotRuntime] {message}",
                this);
        }
    }

    // Purpose: Updates the binding state.
    private bool SetBinding(
        int quickSlotIndex,
        PlayerQuickSlotBindingKind kind,
        int sourceSlotIndex,
        UsableItemSO usableItem,
        out string message)
    {
        NormalizeBindings();

        if (!IsValidSlot(quickSlotIndex))
        {
            message = "Invalid quick slot.";
            return false;
        }

        if (quickSlotIndex < 2 ||
            kind != PlayerQuickSlotBindingKind.Usable)
        {
            message = "Only usable items can be bound to slots 3-5.";
            return false;
        }

        SetBindingInternal(
            quickSlotIndex,
            kind,
            sourceSlotIndex,
            usableItem,
            false);

        message = "Quick slot bound.";
        return true;
    }

    // Purpose: Updates the binding internal state.
    private void SetBindingInternal(
        int quickSlotIndex,
        PlayerQuickSlotBindingKind kind,
        int sourceSlotIndex,
        UsableItemSO usableItem,
        bool select)
    {
        ClearDuplicateSourceBindings(
            kind,
            sourceSlotIndex,
            usableItem,
            quickSlotIndex);

        _bindings[quickSlotIndex].Set(
            kind,
            sourceSlotIndex,
            usableItem);

        _autoBindPassCompleted = true;

        EnsureEquippedItemsHaveBindings();

        if (select)
            _activeQuickSlotIndex = quickSlotIndex;

        RefreshSelectionVisual();
        OnChanged?.Invoke();
    }

    // Purpose: Handles the incoming owned data changed request or event.
    private void HandleOwnedDataChanged()
    {
        if (_isMutatingStorage)
            return;

        PruneUnavailableUsableBindings();
        TryAutoBindUsables();
        RefreshSelectionVisual();
        OnChanged?.Invoke();
    }

    // Purpose: Resolves the weapon target slot needed by this component.
    private int ResolveWeaponTargetSlot(
        int quickSlotIndex,
        WeaponSO weapon)
    {
        PlayerQuickSlotBinding targetBinding =
            _bindings[quickSlotIndex];

        int weaponSlotCount =
            Mathf.Max(
                1,
                _equipmentRuntime?.WeaponSlotCount ?? 1);

        if (targetBinding.Kind ==
                PlayerQuickSlotBindingKind.Weapon &&
            targetBinding.SourceSlotIndex < weaponSlotCount)
        {
            return targetBinding.SourceSlotIndex;
        }

        if (_equipmentRuntime == null)
            return 0;

        for (int i = 0;
             i < _equipmentRuntime.WeaponSlotCount;
             i++)
        {
            if (_equipmentRuntime.GetWeapon(i) == weapon)
                return i;
        }

        for (int i = 0;
             i < _equipmentRuntime.WeaponSlotCount;
             i++)
        {
            if (_equipmentRuntime.GetWeapon(i) == null)
                return i;
        }

        return _equipmentRuntime.ActiveWeaponSlotIndex;
    }

    // Purpose: Resolves the ammo target slot needed by this component.
    private int ResolveAmmoTargetSlot(
        int quickSlotIndex,
        BulletSO bullet)
    {
        const int ammoSlotCount = 3;

        PlayerQuickSlotBinding targetBinding =
            _bindings[quickSlotIndex];

        if (targetBinding.Kind ==
                PlayerQuickSlotBindingKind.Ammo &&
            targetBinding.SourceSlotIndex < ammoSlotCount)
        {
            return targetBinding.SourceSlotIndex;
        }

        if (_bulletLoadout == null)
            return 0;

        for (int i = 0; i < ammoSlotCount; i++)
        {
            if (_bulletLoadout.TryGetSlotSnapshot(
                    i,
                    out WeaponAmmoSlotSnapshot ammo) &&
                ammo.bullet == bullet)
            {
                return i;
            }
        }

        for (int i = 0; i < ammoSlotCount; i++)
        {
            if (_bulletLoadout.TryGetSlotSnapshot(
                    i,
                    out WeaponAmmoSlotSnapshot ammo) &&
                ammo.isEmpty)
            {
                return i;
            }
        }

        for (int i = 0; i < ammoSlotCount; i++)
        {
            if (_bulletLoadout.TryGetSlotSnapshot(
                    i,
                    out WeaponAmmoSlotSnapshot ammo) &&
                ammo.isSelected)
            {
                return i;
            }
        }

        return Mathf.Clamp(
            quickSlotIndex - 2,
            0,
            ammoSlotCount - 1);
    }

    // Purpose: Ensures equipped items have bindings is ready before the workflow continues.
    private void EnsureEquippedItemsHaveBindings()
    {
        NormalizeBindings();
    }

    // Purpose: Finds the requested available binding slot.
    private int FindAvailableBindingSlot(
        int preferredQuickSlot)
    {
        if (IsBindingDisplayEmpty(preferredQuickSlot))
            return preferredQuickSlot;

        for (int i = 0; i < _bindings.Length; i++)
        {
            if (IsBindingDisplayEmpty(i))
                return i;
        }

        return -1;
    }

    // Purpose: Checks whether binding display empty is currently true.
    private bool IsBindingDisplayEmpty(
        int quickSlotIndex)
    {
        if (!IsValidSlot(quickSlotIndex))
            return false;

        PlayerQuickSlotBinding binding =
            _bindings[quickSlotIndex];

        switch (binding.Kind)
        {
            case PlayerQuickSlotBindingKind.None:
                return true;

            case PlayerQuickSlotBindingKind.Weapon:
                return
                    _equipmentRuntime == null ||
                    _equipmentRuntime.GetWeapon(
                        binding.SourceSlotIndex) == null;

            case PlayerQuickSlotBindingKind.Ammo:
                return
                    _bulletLoadout == null ||
                    !_bulletLoadout.TryGetSlotSnapshot(
                        binding.SourceSlotIndex,
                        out WeaponAmmoSlotSnapshot ammo) ||
                    ammo.isEmpty;

            case PlayerQuickSlotBindingKind.Usable:
                return
                    binding.UsableItem == null ||
                    binding.Amount <= 0;

            default:
                return true;
        }
    }

    // Purpose: Checks whether this component has source binding.
    private bool HasSourceBinding(
        PlayerQuickSlotBindingKind kind,
        int sourceSlotIndex)
    {
        for (int i = 0; i < _bindings.Length; i++)
        {
            if (_bindings[i].Kind == kind &&
                _bindings[i].SourceSlotIndex ==
                sourceSlotIndex)
            {
                return true;
            }
        }

        return false;
    }

    // Purpose: Clears the current duplicate source bindings state.
    private void ClearDuplicateSourceBindings(
        PlayerQuickSlotBindingKind kind,
        int sourceSlotIndex,
        UsableItemSO usableItem,
        int exceptQuickSlotIndex)
    {
        for (int i = 0; i < _bindings.Length; i++)
        {
            if (i == exceptQuickSlotIndex)
                continue;

            PlayerQuickSlotBinding binding =
                _bindings[i];

            bool duplicate =
                kind == PlayerQuickSlotBindingKind.Usable
                    ? binding.Kind == kind &&
                      binding.UsableItem == usableItem
                    : binding.Kind == kind &&
                      binding.SourceSlotIndex ==
                      sourceSlotIndex;

            if (!duplicate)
                continue;

            binding.Clear();

            if (_activeQuickSlotIndex == i)
                _activeQuickSlotIndex = -1;
        }
    }

    // Purpose: Clears the current bindings for source state.
    private void ClearBindingsForSource(
        PlayerQuickSlotBindingKind kind,
        int sourceSlotIndex)
    {
        for (int i = 0; i < _bindings.Length; i++)
        {
            PlayerQuickSlotBinding binding =
                _bindings[i];

            if (binding.Kind != kind ||
                binding.SourceSlotIndex != sourceSlotIndex)
            {
                continue;
            }

            binding.Clear();

            if (_activeQuickSlotIndex == i)
                _activeQuickSlotIndex = -1;
        }

        RefreshSelectionVisual();
        OnChanged?.Invoke();
    }

    // Purpose: Performs the prune unavailable usable bindings workflow for this component.
    private void PruneUnavailableUsableBindings()
    {
        NormalizeBindings();

        for (int i = 0; i < _bindings.Length; i++)
        {
            PlayerQuickSlotBinding binding =
                _bindings[i];

            if (binding.Kind !=
                    PlayerQuickSlotBindingKind.Usable ||
                (binding.UsableItem != null &&
                 binding.Amount > 0))
            {
                continue;
            }

            binding.Clear();

            if (_activeQuickSlotIndex == i)
                _activeQuickSlotIndex = -1;
        }
    }

    // Purpose: Attempts to auto bind usables and reports whether the request succeeded.
    private void TryAutoBindUsables()
    {
        if (!_autoBindUsablesToEmptyAmmoBindings ||
            _autoBindPassCompleted ||
            _inventoryRuntime == null)
        {
            return;
        }

        for (int itemIndex = 0;
             itemIndex < _inventoryRuntime.Items.Count;
             itemIndex++)
        {
            PlayerInventoryItemStack stack =
                _inventoryRuntime.Items[itemIndex];

            if (stack == null ||
                stack.IsEmpty ||
                stack.Item is not UsableItemSO usable)
            {
                continue;
            }

            for (int slotIndex = 2;
                 slotIndex < _bindings.Length;
                 slotIndex++)
            {
                PlayerQuickSlotBinding binding =
                    _bindings[slotIndex];

                if (binding.Kind != PlayerQuickSlotBindingKind.None)
                {
                    continue;
                }

                if (!TryAssignInventoryItem(
                        itemIndex,
                        slotIndex,
                        out _))
                {
                    continue;
                }

                break;
            }
        }

        if (HasAnyStoredUsable())
            _autoBindPassCompleted = true;
    }

    // Purpose: Checks whether this component has usable binding.
    private bool HasUsableBinding(
        UsableItemSO usable)
    {
        for (int i = 0; i < _bindings.Length; i++)
        {
            if (_bindings[i].Kind ==
                    PlayerQuickSlotBindingKind.Usable &&
                _bindings[i].UsableItem == usable)
            {
                return true;
            }
        }

        return false;
    }

    // Purpose: Checks whether this component has any stored usable.
    private bool HasAnyStoredUsable()
    {
        for (int i = 2; i < _bindings.Length; i++)
        {
            if (_bindings[i].Kind == PlayerQuickSlotBindingKind.Usable &&
                _bindings[i].UsableItem != null &&
                _bindings[i].Amount > 0)
            {
                return true;
            }
        }

        return false;
    }

    // Purpose: Creates the stored stack owned by this workflow.
    private static PlayerInventoryRemovedStack CreateStoredStack(
        PlayerQuickSlotBinding binding)
    {
        return binding != null &&
               binding.Kind == PlayerQuickSlotBindingKind.Usable &&
               binding.UsableItem != null &&
               binding.Amount > 0
            ? new PlayerInventoryRemovedStack(
                binding.UsableItem,
                binding.Amount,
                -1f)
            : default;
    }

    // Purpose: Resolves the item name needed by this component.
    private static string ResolveItemName(ItemSO item)
    {
        if (item == null)
            return "Item";

        if (item is UsableItemSO usable)
            return usable.UsableDisplayName;

        if (item is BulletSO bullet)
            return bullet.DisplayName;

        if (item is WeaponSO weapon)
            return weapon.DisplayName;

        if (item is ArmorItemSO armor)
            return armor.DisplayName;

        return item.name;
    }

    // Purpose: Checks whether same usable identity is currently true.
    private static bool IsSameUsableIdentity(UsableItemSO first, UsableItemSO second)
    {
        if (ReferenceEquals(first, second))
            return true;

        if (first == null || second == null)
            return false;

        return !string.IsNullOrWhiteSpace(first.Guid) &&
               !string.IsNullOrWhiteSpace(second.Guid) &&
               string.Equals(first.Guid, second.Guid, StringComparison.Ordinal);
    }

    // Purpose: Resolves the active usable needed by this component.
    private UsableItemSO ResolveActiveUsable(
        bool requireOwnedStack)
    {
        if (!IsValidSlot(_activeQuickSlotIndex))
            return null;

        PlayerQuickSlotBinding binding =
            _bindings[_activeQuickSlotIndex];

        if (binding.Kind !=
                PlayerQuickSlotBindingKind.Usable ||
            binding.UsableItem == null)
        {
            return null;
        }

        if (requireOwnedStack &&
            binding.Amount <= 0)
        {
            return null;
        }

        return binding.UsableItem;
    }

    // Purpose: Refreshes the displayed or cached selection visual.
    private void RefreshSelectionVisual()
    {
        UsableItemSO usable =
            ResolveActiveUsable(true);

        bool showUsable = usable != null;

        _equipmentController?.SetWeaponViewSuppressed(
            showUsable);

        if (!showUsable)
        {
            ClearHeldUsableView();
            return;
        }

        if (_heldUsableView != null && _heldUsableItem == usable &&
            _heldUsablePrefab == usable.HandVisualPrefab &&
            _equipmentController != null &&
            _heldUsableView.transform.parent == _equipmentController.HandSocket)
        {
            return;
        }

        ClearHeldUsableView();

        if (usable.HandVisualPrefab == null ||
            _equipmentController == null ||
            _equipmentController.HandSocket == null)
        {
            return;
        }

        _heldUsableView = UsableHeldVisualUtility.Create(usable, _equipmentController.HandSocket);
        if (_heldUsableView == null)
            return;
        _heldUsableItem = usable;
        _heldUsablePrefab = usable.HandVisualPrefab;
        _heldUsableWeaponView = _heldUsableView.GetComponentInChildren<WeaponView>(true);
        _equipmentController.SetHeldItemVisual(_heldUsableView);

        ThrowableProjectile projectile =
            _heldUsableView.GetComponent<ThrowableProjectile>() ??
            _heldUsableView.GetComponentInChildren<ThrowableProjectile>(
                true);

        projectile?.PrepareForEquipped();
    }

    // Purpose: Clears the current held usable view state.
    private void ClearHeldUsableView()
    {
        if (_heldUsableView != null)
        {
            _heldUsableView.SetActive(false);
            Destroy(_heldUsableView);
        }

        _heldUsableView = null;
        _heldUsableWeaponView = null;
        _heldUsableItem = null;
        _heldUsablePrefab = null;
        _equipmentController?.SetHeldItemVisual(null);
    }

    // Purpose: Subscribes to subscribe notifications.
    private void Subscribe()
    {
        if (_inventoryRuntime != null)
        {
            _inventoryRuntime.OnChanged +=
                HandleOwnedDataChanged;
        }

        if (_equipmentRuntime != null)
        {
            _equipmentRuntime.OnEquipmentChanged +=
                HandleOwnedDataChanged;
            _equipmentRuntime.OnWeaponChanged +=
                HandleActiveWeaponChanged;
        }

        if (_bulletLoadout != null)
        {
            _bulletLoadout.SelectedBulletChanged +=
                HandleSelectedBulletChanged;
        }
    }

    // Purpose: Removes the unsubscribe notification subscription.
    private void Unsubscribe()
    {
        if (_inventoryRuntime != null)
        {
            _inventoryRuntime.OnChanged -=
                HandleOwnedDataChanged;
        }

        if (_equipmentRuntime != null)
        {
            _equipmentRuntime.OnEquipmentChanged -=
                HandleOwnedDataChanged;
            _equipmentRuntime.OnWeaponChanged -=
                HandleActiveWeaponChanged;
        }

        if (_bulletLoadout != null)
        {
            _bulletLoadout.SelectedBulletChanged -=
                HandleSelectedBulletChanged;
        }
    }

    // Purpose: Handles the incoming selected bullet changed request or event.
    private void HandleSelectedBulletChanged(BulletSO _)
    {
        HandleOwnedDataChanged();
    }

    // Purpose: Handles the incoming active weapon changed request or event.
    private void HandleActiveWeaponChanged(WeaponSO _)
    {
        // Inventory equipment changes can activate a weapon without passing
        // through SlotHUD. Keep slots 1-5 mutually exclusive on that path too.
        DeactivateActiveUsable();
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_inventoryRuntime == null)
        {
            _inventoryRuntime =
                EntityComponentResolver.Resolve<
                    PlayerInventoryRuntime>(this);
        }

        if (_equipmentRuntime == null)
        {
            _equipmentRuntime =
                EntityComponentResolver.Resolve<
                    EntityEquipmentRuntime>(this);
        }

        if (_bulletLoadout == null)
        {
            _bulletLoadout =
                EntityComponentResolver.Resolve<
                    PlayerBulletLoadoutRuntime>(this);
        }

        if (_equipmentController == null)
        {
            _equipmentController =
                EntityComponentResolver.Resolve<
                    ShooterEquipmentController>(this);
        }

        if (_consumableUseRuntime == null)
        {
            _consumableUseRuntime =
                EntityComponentResolver.Resolve<
                    PlayerConsumableUseRuntime>(this);
        }
    }

    // Purpose: Normalizes bindings to a safe authored value.
    private void NormalizeBindings()
    {
        if (_bindings == null ||
            _bindings.Length != SlotCount)
        {
            PlayerQuickSlotBinding[] normalized =
                new PlayerQuickSlotBinding[SlotCount];

            for (int i = 0; i < normalized.Length; i++)
            {
                normalized[i] =
                    _bindings != null &&
                    i < _bindings.Length &&
                    _bindings[i] != null
                        ? _bindings[i]
                        : CreateDefaultBinding(i);
            }

            _bindings = normalized;
        }

        for (int i = 0; i < _bindings.Length; i++)
        {
            if (_bindings[i] == null)
                _bindings[i] = CreateDefaultBinding(i);

            if (i < 2 ||
                (_bindings[i].Kind != PlayerQuickSlotBindingKind.None &&
                 _bindings[i].Kind != PlayerQuickSlotBindingKind.Usable) ||
                (_bindings[i].Kind == PlayerQuickSlotBindingKind.Usable &&
                 (_bindings[i].UsableItem == null ||
                  _bindings[i].Amount <= 0)))
            {
                _bindings[i].Clear();

                if (_activeQuickSlotIndex == i)
                    _activeQuickSlotIndex = -1;
            }
        }
    }

    // Purpose: Creates the default binding owned by this workflow.
    private static PlayerQuickSlotBinding CreateDefaultBinding(
        int slotIndex)
    {
        return new PlayerQuickSlotBinding();
    }

    // Purpose: Checks whether valid slot is currently true.
    private static bool IsValidSlot(int slotIndex)
    {
        return slotIndex >= 0 &&
               slotIndex < SlotCount;
    }
}
