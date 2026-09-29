/*
 * Role hierarchy: WIP > Legacy > Gameplay > Player > AmmoSlotCompatibility
 * Primary responsibility: Retains old AmmoSlot UI, drag/drop, split, discard,
 * sell, and external-grant entry points while prefabs are migrated.
 * Execution boundary: Runtime (Assembly-CSharp)
 *
 * WIP exit criteria:
 * - No prefab/UI calls a Try*AmmoSlot method or consumes WeaponAmmoSlotSnapshot.
 * - Ammo selection uses PlayerBulletLoadoutRuntime's virtual-magazine API only.
 * - The ammo transaction regression test covers select, reload, split, sell,
 *   discard, and world-drop without this compatibility facade.
 *
 * Do not add new gameplay features here. This file intentionally keeps the
 * old AmmoSlot path separate so its remaining callers are easy to locate.
 */

using UnityEngine;

public partial class PlayerInventoryRuntime
{
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

        TraceBulletInventoryTransaction(
            "TryEquipInventoryBulletToAmmoSlot begin",
            bullet,
            $"inventoryIndex={inventoryIndex}, legacyAmmoSlot={ammoSlotIndex}, amount={amount}, destination=legacy-slot");

        if (!_bulletLoadout.TryGetSlotSnapshot(
                ammoSlotIndex,
                out WeaponAmmoSlotSnapshot targetSnapshot))
        {
            return Fail("Invalid ammo slot.", out message);
        }

        if (targetSnapshot.bullet == bullet &&
            !targetSnapshot.isRequiredDefault &&
            !targetSnapshot.infiniteReserve)
        {
            if (!_bulletLoadout.TryAddBulletToSlot(
                    ammoSlotIndex,
                    bullet,
                    amount,
                    out message))
            {
                return false;
            }

            _items[inventoryIndex].Clear();
            NotifyChanged();
            TraceBulletInventoryTransaction(
                "TryEquipInventoryBulletToAmmoSlot moved inventory stack into existing legacy slot",
                bullet,
                $"inventoryIndex={inventoryIndex}, legacyAmmoSlot={ammoSlotIndex}, moved={amount}");
            return true;
        }

        if (!_bulletLoadout.TryEquipBulletToSlotAndReturnPrevious(
                bullet,
                amount,
                0f,
                ammoSlotIndex,
                out BulletSO previousBullet,
                out int previousAmount,
                out message))
        {
            return false;
        }

        if (previousBullet != null && previousAmount > 0)
            _items[inventoryIndex].Set(previousBullet, previousAmount);
        else
            _items[inventoryIndex].Clear();

        NotifyChanged();
        TraceBulletInventoryTransaction(
            "TryEquipInventoryBulletToAmmoSlot completed",
            bullet,
            $"inventoryIndex={inventoryIndex}, legacyAmmoSlot={ammoSlotIndex}, moved={amount}, previous={ResolveItemName(previousBullet)}, previousAmount={previousAmount}");
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

    public bool TrySplitAmmoSlotToInventory(
        int ammoSlotIndex,
        int splitAmount,
        out int inventorySlotIndex,
        out string message)
    {
        ResolveRefs();
        NormalizeStorage();
        inventorySlotIndex = -1;

        if (_bulletLoadout == null)
            return Fail("Bullet loadout is missing.", out message);

        if (!_bulletLoadout.TryGetSlotSnapshot(
                ammoSlotIndex,
                out WeaponAmmoSlotSnapshot snapshot))
        {
            return Fail("Invalid ammo slot.", out message);
        }

        if (snapshot.bullet == null || snapshot.totalAmmo <= 1)
            return Fail("This ammo stack cannot be split.", out message);

        if (snapshot.isRequiredDefault || snapshot.infiniteReserve)
            return Fail("Default ammo slot cannot be split.", out message);

        if (!TryFindEmptySlotIndex(out inventorySlotIndex))
            return Fail("Inventory is full. Ammo split cancelled.", out message);

        int amount = Mathf.Clamp(splitAmount, 1, snapshot.totalAmmo - 1);

        if (!_bulletLoadout.TryRemoveAmmoFromSlot(
                ammoSlotIndex,
                amount,
                out BulletSO removedBullet,
                out int removedAmount,
                out message))
        {
            inventorySlotIndex = -1;
            return false;
        }

        _items[inventorySlotIndex].Set(removedBullet, removedAmount);
        message = $"{ResolveItemName(removedBullet)} split into inventory.";
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

        if (!CanAcquireOwnedAmount(bullet, resolvedAmount, out message))
            return false;

        if (!_bulletLoadout.TryGetSlotSnapshot(
                ammoSlotIndex,
                out WeaponAmmoSlotSnapshot targetSnapshot))
        {
            return Fail("Invalid ammo slot.", out message);
        }

        if (targetSnapshot.bullet == bullet &&
            !targetSnapshot.isRequiredDefault &&
            !targetSnapshot.infiniteReserve)
        {
            if (!_bulletLoadout.TryAddBulletToSlot(
                    ammoSlotIndex,
                    bullet,
                    resolvedAmount,
                    out message))
            {
                return false;
            }

            NotifyChanged();
            return true;
        }

        if (targetSnapshot.bullet != null &&
            targetSnapshot.totalAmmo > 0 &&
            CalculateStoreCapacity(targetSnapshot.bullet) < targetSnapshot.totalAmmo)
        {
            return Fail("Inventory is full. Ammo swap cancelled.", out message);
        }

        if (!_bulletLoadout.TryEquipBulletToSlotAndReturnPrevious(
                bullet,
                resolvedAmount,
                0f,
                ammoSlotIndex,
                out BulletSO previousBullet,
                out int previousAmount,
                out message))
        {
            return false;
        }

        if (previousBullet != null && previousAmount > 0)
        {
            if (!TryStoreItem(previousBullet, previousAmount, out string storeMessage))
                return Fail($"Ammo swap failed. {storeMessage}", out message);
        }

        NotifyChanged();
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
}
