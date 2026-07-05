using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PlayerInventoryItemStack
{
    [SerializeField] private ItemSO _item;
    [SerializeField, Min(1)] private int _amount = 1;
    [SerializeField] private float _armorDurability = -1f;

    public ItemSO Item => _item;
    public int Amount => Mathf.Max(0, _amount);
    public float ArmorDurability => _armorDurability;
    public bool HasArmorDurability => _armorDurability >= 0f;
    public bool IsEmpty => _item == null || _amount <= 0;

    public void Set(ItemSO item, int amount)
    {
        Set(item, amount, -1f);
    }

    public void Set(ItemSO item, int amount, float armorDurability)
    {
        _item = item;
        _amount = Mathf.Max(1, amount);
        _armorDurability = item is ArmorItemSO ? armorDurability : -1f;
    }

    public void Clear()
    {
        _item = null;
        _amount = 0;
        _armorDurability = -1f;
    }

    public void Add(int amount)
    {
        _amount = Mathf.Max(1, _amount + Mathf.Max(1, amount));
    }

    public bool Remove(int amount)
    {
        if (amount <= 0 || IsEmpty)
            return false;

        _amount -= amount;
        return _amount <= 0;
    }
}

public readonly struct PlayerInventoryRemovedStack
{
    public readonly ItemSO item;
    public readonly int amount;
    public readonly float armorDurability;

    public bool HasItem => item != null && amount > 0;

    public PlayerInventoryRemovedStack(
        ItemSO item,
        int amount,
        float armorDurability)
    {
        this.item = item;
        this.amount = Mathf.Max(0, amount);
        this.armorDurability = armorDurability;
    }
}

public enum PlayerInventoryEquipmentSlotKind
{
    Weapon = 0,
    Armor = 1
}

[DisallowMultipleComponent]
public class PlayerInventoryRuntime : MonoBehaviour
{
    [Header("Capacity")]
    [Tooltip("General backpack slot count. Equipped weapon, equipped armor, and ammo loadout slots are separate.")]
    [SerializeField, Min(0)] private int _inventoryCapacity = 12;

    [Header("Refs")]
    [SerializeField] private PlayerBulletLoadoutRuntime _bulletLoadout;
    [SerializeField] private EntityWeaponHolder _weaponHolder;
    [SerializeField] private EntityEquipmentRuntime _equipmentRuntime;

    [Header("Runtime Ready")]
    [SerializeField] private PlayerInventoryRuntimeReadyEventChannelSO _inventoryRuntimeReadyChannel;

    [Header("Pickup Defaults")]
    [SerializeField, Min(1)] private int _defaultBulletBundleAmount = 20;
    [SerializeField, Range(0f, 1f)] private float _defaultBulletSellPriceRate = 0f;

    [Header("Runtime")]
    [SerializeField] private List<PlayerInventoryItemStack> _items = new();

    public event Action OnChanged;
    public event Action<ItemSO> ItemPickedUp;
    public event Action<ItemSO, string> PickupRejected;

    public IReadOnlyList<PlayerInventoryItemStack> Items => _items;
    public int Capacity => Mathf.Max(0, _inventoryCapacity);
    public int UsedSlots => CountUsedSlots();
    public int FreeSlots => Mathf.Max(0, Capacity - UsedSlots);
    public WeaponSO EquippedWeapon => _weaponHolder != null ? _weaponHolder.CurrentWeapon : null;
    public ArmorItemSO EquippedArmor => _equipmentRuntime != null ? _equipmentRuntime.CurrentArmor : null;
    public float EquippedArmorDurability => _equipmentRuntime != null ? _equipmentRuntime.CurrentArmorDurability : 0f;
    public float EquippedArmorMaxDurability => _equipmentRuntime != null ? _equipmentRuntime.MaxArmorDurability : 0f;

    private void Reset()
    {
        ResolveRefs();
    }

    private void Awake()
    {
        ResolveRefs();
        NormalizeStorage();
    }

    private void OnEnable()
    {
        ResolveRefs();
        NormalizeStorage();

        if (_inventoryRuntimeReadyChannel != null)
            _inventoryRuntimeReadyChannel.RaiseEvent(this);
    }

    private void OnDisable()
    {
        if (_inventoryRuntimeReadyChannel != null)
            _inventoryRuntimeReadyChannel.Clear(this);
    }

    public bool TryPickup(ItemSO item, out string message)
    {
        return TryPickup(
            item,
            1,
            _defaultBulletBundleAmount,
            _defaultBulletSellPriceRate,
            out message);
    }

    public bool TryPickup(ItemSO item, int amount, out string message)
    {
        return TryPickup(
            item,
            amount,
            _defaultBulletBundleAmount,
            _defaultBulletSellPriceRate,
            out message);
    }

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

        bool picked;

        if (item is PassiveItemSO passive)
            picked = TryPickupPassiveItem(passive, Mathf.Max(1, amount), out message);
        else if (item is BulletSO bullet)
            picked = TryPickupBullet(bullet, bulletBundleAmount, bulletSellPriceRate, out message);
        else if (item is WeaponSO weapon)
            picked = TryPickupWeapon(weapon, out message);
        else if (item is ArmorItemSO armor)
            picked = TryPickupArmor(armor, out message);
        else
            picked = TryStoreItem(item, Mathf.Max(1, amount), out message);

        if (!picked)
            Reject(item, message);

        return picked;
    }

    public bool TryPickupTreasureReward(
        TreasureRewardKind kind,
        PassiveItemSO passive,
        BulletSO bullet,
        ArmorItemSO armor,
        int bulletBundleAmount,
        float bulletSellPriceRate,
        out string message)
    {
        return kind switch
        {
            TreasureRewardKind.Passive => TryPickup(passive, 1, bulletBundleAmount, bulletSellPriceRate, out message),
            TreasureRewardKind.Bullet => TryPickup(bullet, 1, bulletBundleAmount, bulletSellPriceRate, out message),
            TreasureRewardKind.Armor => TryPickup(armor, 1, bulletBundleAmount, bulletSellPriceRate, out message),
            _ => Fail("Unsupported reward kind.", out message)
        };
    }

    public bool TryPickupPassiveItem(PassiveItemSO item, out string message)
    {
        return TryPickupPassiveItem(item, 1, out message);
    }

    public bool TryPickupPassiveItem(PassiveItemSO item, int amount, out string message)
    {
        if (item == null)
            return Fail("Passive item is missing.", out message);

        if (!item.AllowDuplicatePickup && ContainsPassive(item))
            return Fail("Passive item already owned.", out message);

        if (!TryStoreItem(item, Mathf.Max(1, amount), out message))
            return false;

        message = $"{ResolveItemName(item)} picked up.";
        NotifyPickedUp(item);
        return true;
    }

    public bool TryPickupArmorLoot(ArmorItemSO armor, float durability, out string message)
    {
        return TryPickupArmor(armor, durability, out message);
    }

    public bool TryEquipInventoryItem(int index, out string message)
    {
        ResolveRefs();

        if (!IsValidIndex(index))
            return Fail("Invalid inventory slot.", out message);

        ItemSO item = _items[index].Item;

        if (item is WeaponSO weapon)
            return TryEquipWeaponFromInventory(index, weapon, out message);

        if (item is ArmorItemSO armor)
            return TryEquipArmorFromInventory(index, armor, _items[index].ArmorDurability, out message);

        if (item is BulletSO bullet)
            return TryEquipBulletFromInventory(index, bullet, out message);

        return Fail("This item cannot be equipped.", out message);
    }

    public bool TryQuickEquipInventoryItem(int index, out string message)
    {
        ResolveRefs();

        if (!IsValidIndex(index))
            return Fail("Invalid inventory slot.", out message);

        ItemSO item = _items[index].Item;

        if (item is WeaponSO weapon)
            return TryEquipWeaponFromInventory(index, weapon, out message);

        if (item is ArmorItemSO armor)
            return TryEquipArmorFromInventory(index, armor, _items[index].ArmorDurability, out message);

        if (item is BulletSO bullet)
            return TryEquipBulletFromInventoryToFirstAmmoSlot(index, bullet, out message);

        return Fail("This item cannot be equipped.", out message);
    }

    public bool TryEquipInventoryItemToSlot(
        int index,
        PlayerInventoryEquipmentSlotKind slotKind,
        out string message)
    {
        ResolveRefs();

        if (!IsValidIndex(index))
            return Fail("Invalid inventory slot.", out message);

        switch (slotKind)
        {
            case PlayerInventoryEquipmentSlotKind.Weapon:
                if (_items[index].Item is WeaponSO weapon)
                    return TryEquipWeaponFromInventory(index, weapon, out message);

                return Fail("This item is not a weapon.", out message);

            case PlayerInventoryEquipmentSlotKind.Armor:
                if (_items[index].Item is ArmorItemSO armor)
                    return TryEquipArmorFromInventory(index, armor, _items[index].ArmorDurability, out message);

                return Fail("This item is not armor.", out message);

            default:
                return Fail("Unsupported equipment slot.", out message);
        }
    }

    public bool TryEquipInventoryBulletToAmmoSlot(
        int inventoryIndex,
        int ammoSlotIndex,
        out string message)
    {
        ResolveRefs();
        NormalizeStorage();

        if (!IsValidIndex(inventoryIndex))
            return Fail("Invalid inventory slot.", out message);

        if (_items[inventoryIndex].Item is not BulletSO bullet)
            return Fail("This item is not a bullet.", out message);

        if (_bulletLoadout == null)
            return Fail("Bullet loadout is missing.", out message);

        int amount = Mathf.Max(1, _items[inventoryIndex].Amount);

        if (!_bulletLoadout.TryEquipBulletToSlot(
                bullet,
                amount,
                _defaultBulletSellPriceRate,
                ammoSlotIndex,
                out message))
        {
            return false;
        }

        _items[inventoryIndex].Clear();
        NotifyChanged();
        return true;
    }

    public bool TryGetAmmoSlotSnapshot(int ammoSlotIndex, out WeaponAmmoSlotSnapshot snapshot)
    {
        ResolveRefs();

        if (_bulletLoadout == null)
        {
            snapshot = default;
            return false;
        }

        return _bulletLoadout.TryGetSlotSnapshot(ammoSlotIndex, out snapshot);
    }

    public bool TryUnequipAmmoSlotToInventorySlot(
        int ammoSlotIndex,
        int targetInventoryIndex,
        out string message)
    {
        ResolveRefs();
        NormalizeStorage();

        if (_bulletLoadout == null)
            return Fail("Bullet loadout is missing.", out message);

        if (!_bulletLoadout.TryGetSlotSnapshot(
                ammoSlotIndex,
                out WeaponAmmoSlotSnapshot snapshot))
        {
            return Fail("Invalid ammo slot.", out message);
        }

        if (snapshot.isEmpty || snapshot.bullet == null)
            return Fail("Ammo slot is empty.", out message);

        if (snapshot.isRequiredDefault || snapshot.infiniteReserve)
            return Fail("Default ammo slot cannot be removed.", out message);

        int amount = Mathf.Max(0, snapshot.totalAmmo);

        if (amount <= 0)
            return Fail("Ammo slot has no ammo.", out message);

        if (!TryPlaceItemIntoInventorySlot(
                snapshot.bullet,
                amount,
                targetInventoryIndex,
                out message))
        {
            return false;
        }

        if (!_bulletLoadout.TryClearRemovableSlot(ammoSlotIndex, out string clearMessage))
        {
            message = clearMessage;
            return false;
        }

        message = $"{ResolveItemName(snapshot.bullet)} unequipped.";
        NotifyChanged();
        return true;
    }

    public bool TrySwapAmmoSlots(
        int fromAmmoSlotIndex,
        int toAmmoSlotIndex,
        out string message)
    {
        ResolveRefs();

        if (_bulletLoadout == null)
            return Fail("Bullet loadout is missing.", out message);

        if (!_bulletLoadout.TrySwapSlots(fromAmmoSlotIndex, toAmmoSlotIndex))
            return Fail("Cannot swap ammo slots.", out message);

        message = "Ammo slots swapped.";
        NotifyChanged();
        return true;
    }

    public bool TryDiscardAmmoSlot(int ammoSlotIndex, out string message)
    {
        return TryRemoveAmmoSlot(
            ammoSlotIndex,
            out _,
            out message);
    }

    public bool TryRemoveAmmoSlot(
        int ammoSlotIndex,
        out PlayerInventoryRemovedStack removed,
        out string message)
    {
        ResolveRefs();
        removed = default;

        if (_bulletLoadout == null)
            return Fail("Bullet loadout is missing.", out message);

        if (!_bulletLoadout.TryGetSlotSnapshot(
                ammoSlotIndex,
                out WeaponAmmoSlotSnapshot snapshot))
        {
            return Fail("Invalid ammo slot.", out message);
        }

        if (snapshot.isEmpty || snapshot.bullet == null)
            return Fail("Ammo slot is empty.", out message);

        if (snapshot.isRequiredDefault || snapshot.infiniteReserve)
            return Fail("Default ammo slot cannot be removed.", out message);

        int amount = Mathf.Max(0, snapshot.totalAmmo);

        if (amount <= 0)
            return Fail("Ammo slot has no ammo.", out message);

        if (!_bulletLoadout.TryClearRemovableSlot(ammoSlotIndex, out message))
            return false;

        removed = new PlayerInventoryRemovedStack(snapshot.bullet, amount, -1f);
        message = $"{ResolveItemName(snapshot.bullet)} discarded.";
        NotifyChanged();
        return true;
    }

    public bool TryStoreExternalItemToInventorySlot(
        ItemSO item,
        int amount,
        float armorDurability,
        int targetIndex,
        out string message)
    {
        return TryPlaceItemIntoInventorySlot(
            item,
            Mathf.Max(1, amount),
            targetIndex,
            armorDurability,
            out message);
    }

    public bool TryEquipExternalItemToSlot(
        ItemSO item,
        int amount,
        float armorDurability,
        PlayerInventoryEquipmentSlotKind slotKind,
        out string message)
    {
        ResolveRefs();

        switch (slotKind)
        {
            case PlayerInventoryEquipmentSlotKind.Weapon:
                if (item is WeaponSO weapon)
                    return TryEquipExternalWeapon(weapon, out message);

                return Fail("This item is not a weapon.", out message);

            case PlayerInventoryEquipmentSlotKind.Armor:
                if (item is ArmorItemSO armor)
                    return TryEquipExternalArmor(armor, armorDurability, out message);

                return Fail("This item is not armor.", out message);

            default:
                return Fail("Unsupported equipment slot.", out message);
        }
    }

    public bool TryEquipExternalBulletToAmmoSlot(
        BulletSO bullet,
        int amount,
        int ammoSlotIndex,
        out string message)
    {
        ResolveRefs();

        if (bullet == null)
            return Fail("Bullet is missing.", out message);

        if (_bulletLoadout == null)
            return Fail("Bullet loadout is missing.", out message);

        int resolvedAmount = Mathf.Max(1, amount);

        if (!_bulletLoadout.TryEquipBulletToSlotAndReturnPrevious(
                bullet,
                resolvedAmount,
                _defaultBulletSellPriceRate,
                ammoSlotIndex,
                out BulletSO previousBullet,
                out int previousAmount,
                out message,
                true))
        {
            return false;
        }

        if (previousBullet != null && previousAmount > 0)
        {
            if (!TryStoreItem(previousBullet, previousAmount, out string storeMessage))
                message = $"{message} {storeMessage}";
        }

        NotifyChanged();
        return true;
    }

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
            to.Set(from.Item, from.Amount, from.ArmorDurability);
            from.Clear();

            message = $"{ResolveItemName(movedItem)} moved.";
            NotifyChanged();
            return true;
        }

        if (CanStack(from.Item) && to.Item == from.Item)
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

    public bool TryDiscardInventoryItem(int index, int amount, out string message)
    {
        return TryRemoveInventoryItem(
            index,
            amount,
            out _,
            out message);
    }

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
        float armorDurability = stack.ArmorDurability;

        if (stack.Remove(removeAmount))
            stack.Clear();

        removed = new PlayerInventoryRemovedStack(item, removeAmount, armorDurability);
        message = $"{ResolveItemName(item)} discarded.";
        NotifyChanged();
        return true;
    }

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

        int amount = Mathf.Clamp(splitAmount, 1, source.Amount - 1);

        if (!TryFindEmptySlotIndex(out newSlotIndex))
            return Fail("Inventory is full.", out message);

        PlayerInventoryItemStack target = _items[newSlotIndex];

        if (target == null)
        {
            target = new PlayerInventoryItemStack();
            _items[newSlotIndex] = target;
        }

        target.Set(source.Item, amount, source.ArmorDurability);

        if (source.Remove(amount))
            source.Clear();

        message = $"{ResolveItemName(target.Item)} split.";
        NotifyChanged();
        return true;
    }

    public bool TrySellInventoryItem(
        int index,
        int amount,
        int unitPrice,
        out int currencyGained,
        out string message)
    {
        currencyGained = 0;
        NormalizeStorage();

        if (!IsValidIndex(index))
            return Fail("Invalid inventory slot.", out message);

        PlayerInventoryItemStack stack = _items[index];
        ItemSO item = stack.Item;
        int sellAmount = Mathf.Clamp(amount <= 0 ? stack.Amount : amount, 1, stack.Amount);

        if (stack.Remove(sellAmount))
            stack.Clear();

        currencyGained = Mathf.Max(0, unitPrice) * sellAmount;
        message = $"Sold {ResolveItemName(item)} x{sellAmount}.";
        NotifyChanged();
        return true;
    }

    public bool TryUnequipToInventory(
        PlayerInventoryEquipmentSlotKind slotKind,
        out string message)
    {
        return TryUnequipToInventorySlot(slotKind, -1, out message);
    }

    public bool TryUnequipToInventorySlot(
        PlayerInventoryEquipmentSlotKind slotKind,
        int targetIndex,
        out string message)
    {
        ResolveRefs();
        NormalizeStorage();

        switch (slotKind)
        {
            case PlayerInventoryEquipmentSlotKind.Weapon:
                return TryUnequipWeaponToInventorySlot(targetIndex, out message);

            case PlayerInventoryEquipmentSlotKind.Armor:
                return TryUnequipArmorToInventorySlot(targetIndex, out message);

            default:
                return Fail("Unsupported equipment slot.", out message);
        }
    }

    public bool TryDiscardEquipped(
        PlayerInventoryEquipmentSlotKind slotKind,
        out string message)
    {
        return TryRemoveEquipped(
            slotKind,
            out _,
            out message);
    }

    public bool TryRemoveEquipped(
        PlayerInventoryEquipmentSlotKind slotKind,
        out PlayerInventoryRemovedStack removed,
        out string message)
    {
        ResolveRefs();
        removed = default;

        switch (slotKind)
        {
            case PlayerInventoryEquipmentSlotKind.Weapon:
                if (EquippedWeapon == null)
                    return Fail("No weapon equipped.", out message);

                WeaponSO weapon = EquippedWeapon;
                _weaponHolder.ClearWeapon();
                removed = new PlayerInventoryRemovedStack(weapon, 1, -1f);
                message = $"{weapon.DisplayName} discarded.";
                NotifyChanged();
                return true;

            case PlayerInventoryEquipmentSlotKind.Armor:
                if (EquippedArmor == null)
                    return Fail("No armor equipped.", out message);

                ArmorItemSO armor = EquippedArmor;
                float armorDurability = EquippedArmorDurability;
                _equipmentRuntime.ClearArmor();
                removed = new PlayerInventoryRemovedStack(armor, 1, armorDurability);
                message = $"{armor.DisplayName} discarded.";
                NotifyChanged();
                return true;

            default:
                return Fail("Unsupported equipment slot.", out message);
        }
    }

    public bool TrySellEquipped(
        PlayerInventoryEquipmentSlotKind slotKind,
        int unitPrice,
        out int currencyGained,
        out string message)
    {
        currencyGained = 0;

        if (!TryDiscardEquipped(slotKind, out message))
            return false;

        currencyGained = Mathf.Max(0, unitPrice);
        message = message.Replace("discarded", "sold");
        return true;
    }

    public bool TrySellAmmoSlot(
        int ammoSlotIndex,
        int unitPrice,
        out int currencyGained,
        out string message)
    {
        currencyGained = 0;
        ResolveRefs();

        if (_bulletLoadout == null)
            return Fail("Bullet loadout is missing.", out message);

        if (!_bulletLoadout.TryGetSlotSnapshot(
                ammoSlotIndex,
                out WeaponAmmoSlotSnapshot snapshot))
        {
            return Fail("Invalid ammo slot.", out message);
        }

        if (snapshot.isEmpty || snapshot.bullet == null)
            return Fail("Ammo slot is empty.", out message);

        if (snapshot.isRequiredDefault || snapshot.infiniteReserve)
            return Fail("Default ammo slot cannot be sold.", out message);

        int sellAmount = Mathf.Max(0, snapshot.totalAmmo);

        if (sellAmount <= 0)
            return Fail("Ammo slot has no ammo.", out message);

        if (!_bulletLoadout.TryClearRemovableSlot(ammoSlotIndex, out message))
            return false;

        currencyGained = Mathf.Max(0, unitPrice) * sellAmount;
        message = $"Sold {ResolveItemName(snapshot.bullet)} x{sellAmount}.";
        NotifyChanged();
        return true;
    }

    public bool TryRepairInventoryArmorToFull(
        int index,
        out float repairedAmount,
        out string message)
    {
        repairedAmount = 0f;
        NormalizeStorage();

        if (!IsValidIndex(index))
            return Fail("Invalid inventory slot.", out message);

        PlayerInventoryItemStack stack = _items[index];

        if (stack.Item is not ArmorItemSO armor)
            return Fail("This item is not armor.", out message);

        float maxDurability = Mathf.Max(0f, armor.MaxDurability);
        float currentDurability = stack.HasArmorDurability
            ? Mathf.Clamp(stack.ArmorDurability, 0f, maxDurability)
            : maxDurability;

        repairedAmount = Mathf.Max(0f, maxDurability - currentDurability);

        if (repairedAmount <= 0f)
            return Fail("Armor is already fully repaired.", out message);

        stack.Set(armor, stack.Amount, maxDurability);
        message = $"{ResolveItemName(armor)} repaired.";
        NotifyChanged();
        return true;
    }

    public bool TryRepairEquippedArmorToFull(
        out float repairedAmount,
        out string message)
    {
        repairedAmount = 0f;
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
        message = $"{ResolveItemName(_equipmentRuntime.CurrentArmor)} repaired.";
        NotifyChanged();
        return true;
    }

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

    public bool ContainsPassive(PassiveItemSO item)
    {
        if (item == null)
            return false;

        for (int i = 0; i < _items.Count; i++)
        {
            PlayerInventoryItemStack stack = _items[i];

            if (stack != null && stack.Item == item && stack.Amount > 0)
                return true;
        }

        return false;
    }

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

    private bool TryPickupBullet(
        BulletSO bullet,
        int bulletBundleAmount,
        float bulletSellPriceRate,
        out string message)
    {
        if (bullet == null)
            return Fail("Bullet is missing.", out message);

        int amount = Mathf.Max(1, bulletBundleAmount);

        if (_bulletLoadout != null &&
            _bulletLoadout.TryAcquireBulletToAvailableSlot(
                bullet,
                amount,
                bulletSellPriceRate,
                out _,
                out message))
        {
            NotifyPickedUp(bullet);
            return true;
        }

        if (!TryStoreItem(bullet, amount, out message))
            return false;

        message = $"{bullet.DisplayName} stored in inventory.";
        NotifyPickedUp(bullet);
        return true;
    }

    private bool TryPickupWeapon(WeaponSO weapon, out string message)
    {
        if (weapon == null)
            return Fail("Weapon is missing.", out message);

        if (_weaponHolder != null && _weaponHolder.CurrentWeapon == null)
        {
            _weaponHolder.Equip(weapon);
            message = $"{weapon.DisplayName} equipped.";
            NotifyPickedUp(weapon);
            NotifyChanged();
            return true;
        }

        if (!TryStoreItem(weapon, 1, out message))
            return false;

        message = $"{weapon.DisplayName} stored in inventory.";
        NotifyPickedUp(weapon);
        return true;
    }

    private bool TryPickupArmor(ArmorItemSO armor, out string message)
    {
        return TryPickupArmor(armor, armor != null ? armor.MaxDurability : 0f, out message);
    }

    private bool TryPickupArmor(ArmorItemSO armor, float durability, out string message)
    {
        if (armor == null)
            return Fail("Armor is missing.", out message);

        if (_equipmentRuntime != null && _equipmentRuntime.CurrentArmor == null)
        {
            _equipmentRuntime.EquipArmor(armor, durability);
            message = $"{armor.DisplayName} equipped.";
            NotifyPickedUp(armor);
            NotifyChanged();
            return true;
        }

        if (!TryStoreItem(armor, 1, durability, out message))
            return false;

        message = $"{armor.DisplayName} stored in inventory.";
        NotifyPickedUp(armor);
        return true;
    }

    private bool TryEquipWeaponFromInventory(int index, WeaponSO weapon, out string message)
    {
        if (_weaponHolder == null)
            return Fail("Weapon holder is missing.", out message);

        WeaponSO previous = _weaponHolder.CurrentWeapon;
        _weaponHolder.Equip(weapon);
        ReplaceOrRemoveInventorySlot(index, previous);

        message = $"{weapon.DisplayName} equipped.";
        NotifyChanged();
        return true;
    }

    private bool TryEquipExternalWeapon(WeaponSO weapon, out string message)
    {
        if (_weaponHolder == null)
            return Fail("Weapon holder is missing.", out message);

        if (weapon == null)
            return Fail("Weapon is missing.", out message);

        WeaponSO previous = _weaponHolder.CurrentWeapon;

        if (previous != null && !TryStoreItem(previous, 1, out message))
            return false;

        _weaponHolder.Equip(weapon);
        message = $"{weapon.DisplayName} equipped.";
        NotifyChanged();
        return true;
    }

    private bool TryEquipArmorFromInventory(int index, ArmorItemSO armor, float durability, out string message)
    {
        if (_equipmentRuntime == null)
            return Fail("Equipment runtime is missing.", out message);

        ArmorItemSO previous = _equipmentRuntime.CurrentArmor;
        float previousDurability = _equipmentRuntime.CurrentArmorDurability;
        _equipmentRuntime.EquipArmor(
            armor,
            durability >= 0f ? durability : armor.MaxDurability);
        ReplaceOrRemoveInventorySlot(index, previous, previousDurability);

        message = $"{armor.DisplayName} equipped.";
        NotifyChanged();
        return true;
    }

    private bool TryEquipExternalArmor(ArmorItemSO armor, float durability, out string message)
    {
        if (_equipmentRuntime == null)
            return Fail("Equipment runtime is missing.", out message);

        if (armor == null)
            return Fail("Armor is missing.", out message);

        ArmorItemSO previous = _equipmentRuntime.CurrentArmor;
        float previousDurability = _equipmentRuntime.CurrentArmorDurability;

        if (previous != null && !TryStoreItem(previous, 1, previousDurability, out message))
            return false;

        _equipmentRuntime.EquipArmor(
            armor,
            durability >= 0f ? durability : armor.MaxDurability);

        message = $"{armor.DisplayName} equipped.";
        NotifyChanged();
        return true;
    }

    private bool TryEquipBulletFromInventory(int index, BulletSO bullet, out string message)
    {
        if (_bulletLoadout == null)
            return Fail("Bullet loadout is missing.", out message);

        int amount = Mathf.Max(1, _items[index].Amount);

        if (!_bulletLoadout.TryAcquireBulletToAvailableSlot(
                bullet,
                amount,
                _defaultBulletSellPriceRate,
                out _,
                out message))
        {
            return false;
        }

        _items[index].Clear();
        NotifyChanged();
        return true;
    }

    private bool TryEquipBulletFromInventoryToFirstAmmoSlot(int index, BulletSO bullet, out string message)
    {
        if (_bulletLoadout == null)
            return Fail("Bullet loadout is missing.", out message);

        int amount = Mathf.Max(1, _items[index].Amount);

        if (_bulletLoadout.TryAcquireBulletToAvailableSlot(
                bullet,
                amount,
                _defaultBulletSellPriceRate,
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
                _defaultBulletSellPriceRate,
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

    private bool TryStoreItem(ItemSO item, int amount, out string message)
    {
        return TryStoreItem(item, amount, -1f, out message);
    }

    private bool TryStoreItem(ItemSO item, int amount, float armorDurability, out string message)
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
            stack.Set(item, stackAmount, armorDurability);
            _items[emptyIndex] = stack;
            remainingAmount -= stackAmount;
        }

        message = stacked
            ? $"{ResolveItemName(item)} stacked."
            : $"{ResolveItemName(item)} stored.";
        NotifyChanged();
        return true;
    }

    private void ReplaceOrRemoveInventorySlot(int index, ItemSO replacement)
    {
        ReplaceOrRemoveInventorySlot(index, replacement, -1f);
    }

    private void ReplaceOrRemoveInventorySlot(int index, ItemSO replacement, float armorDurability)
    {
        if (!IsValidIndex(index))
            return;

        if (replacement == null)
            _items[index].Clear();
        else
            _items[index].Set(replacement, 1, armorDurability);

        NormalizeStorage();
    }

    private bool TryUnequipWeaponToInventorySlot(int targetIndex, out string message)
    {
        if (_weaponHolder == null || EquippedWeapon == null)
            return Fail("No weapon equipped.", out message);

        WeaponSO weapon = EquippedWeapon;

        if (!TryPlaceItemIntoInventorySlot(weapon, 1, targetIndex, out message))
            return false;

        _weaponHolder.ClearWeapon();
        message = $"{weapon.DisplayName} unequipped.";
        NotifyChanged();
        return true;
    }

    private bool TryUnequipArmorToInventorySlot(int targetIndex, out string message)
    {
        if (_equipmentRuntime == null || EquippedArmor == null)
            return Fail("No armor equipped.", out message);

        ArmorItemSO armor = EquippedArmor;

        float durability = _equipmentRuntime.CurrentArmorDurability;

        if (!TryPlaceItemIntoInventorySlot(armor, 1, targetIndex, durability, out message))
            return false;

        _equipmentRuntime.ClearArmor();
        message = $"{armor.DisplayName} unequipped.";
        NotifyChanged();
        return true;
    }

    private bool TryPlaceItemIntoInventorySlot(
        ItemSO item,
        int amount,
        int targetIndex,
        out string message)
    {
        return TryPlaceItemIntoInventorySlot(item, amount, targetIndex, -1f, out message);
    }

    private bool TryPlaceItemIntoInventorySlot(
        ItemSO item,
        int amount,
        int targetIndex,
        float armorDurability,
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
                return Fail("Target inventory slot is occupied.", out message);

            target.Set(item, resolvedAmount, armorDurability);
            message = $"{ResolveItemName(item)} moved.";
            NotifyChanged();
            return true;
        }

        return TryStoreItem(item, resolvedAmount, armorDurability, out message);
    }

    private bool TryFindStack(ItemSO item, out PlayerInventoryItemStack stack)
    {
        stack = null;

        for (int i = 0; i < _items.Count; i++)
        {
            PlayerInventoryItemStack candidate = _items[i];

            if (candidate == null || candidate.IsEmpty)
                continue;

            if (candidate.Item == item)
            {
                stack = candidate;
                return true;
            }
        }

        return false;
    }

    private bool TryFindStackWithSpace(ItemSO item, out PlayerInventoryItemStack stack)
    {
        stack = null;

        if (!CanStack(item))
            return false;

        int maxStack = ResolveMaxStack(item);

        for (int i = 0; i < _items.Count; i++)
        {
            PlayerInventoryItemStack candidate = _items[i];

            if (candidate == null || candidate.IsEmpty)
                continue;

            if (candidate.Item == item && candidate.Amount < maxStack)
            {
                stack = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool CanStack(ItemSO item)
    {
        return ResolveMaxStack(item) > 1;
    }


    private static int ResolveMaxStack(ItemSO item)
    {
        return item != null ? Mathf.Max(1, item.MaxStack) : 1;
    }

    private int CalculateStoreCapacity(ItemSO item)
    {
        if (item == null)
            return 0;

        int maxStack = ResolveMaxStack(item);
        int capacity = 0;
        bool canStack = CanStack(item);

        NormalizeStorage();

        for (int i = 0; i < _items.Count; i++)
        {
            PlayerInventoryItemStack candidate = _items[i];

            if (candidate == null || candidate.IsEmpty)
            {
                capacity += maxStack;
                continue;
            }

            if (canStack && candidate.Item == item)
                capacity += Mathf.Max(0, maxStack - candidate.Amount);
        }

        return capacity;
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < _items.Count && _items[index] != null && !_items[index].IsEmpty;
    }

    private bool IsSlotInCapacity(int index)
    {
        return index >= 0 && index < Capacity;
    }

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

    private void NormalizeStorage()
    {
        int capacity = Capacity;

        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] == null)
            {
                PlayerInventoryItemStack empty = new();
                empty.Clear();
                _items[i] = empty;
            }
        }

        while (_items.Count < capacity)
        {
            PlayerInventoryItemStack empty = new();
            empty.Clear();
            _items.Add(empty);
        }
    }

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

    private void ResolveRefs()
    {
        if (_bulletLoadout == null)
            _bulletLoadout = GetComponent<PlayerBulletLoadoutRuntime>();

        if (_weaponHolder == null)
            _weaponHolder = GetComponent<EntityWeaponHolder>();

        if (_equipmentRuntime == null)
            _equipmentRuntime = GetComponent<EntityEquipmentRuntime>();
    }

    private void NotifyPickedUp(ItemSO item)
    {
        ItemPickedUp?.Invoke(item);
    }

    private void NotifyChanged()
    {
        NormalizeStorage();
        OnChanged?.Invoke();
    }

    private void Reject(ItemSO item, string message)
    {
        PickupRejected?.Invoke(item, message);
    }

    private static bool Fail(string text, out string message)
    {
        message = text;
        return false;
    }

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
