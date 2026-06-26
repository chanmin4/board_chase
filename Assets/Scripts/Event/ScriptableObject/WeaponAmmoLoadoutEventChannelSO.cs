using System;
using UnityEngine;

/// <summary>
/// HUD data for one ammo slot.
/// Contains bullet icon, ammo count, selection state, and bullet ammo type.
/// </summary>
[Serializable]
public struct WeaponAmmoSlotSnapshot
{
    public int slotIndex;
    public BulletSO bullet;
    public BulletAmmoType bulletType;
    public Sprite bulletIcon;
    public string displayName;
    public int currentAmmo;
    public int reserveAmmo;
    public bool infiniteReserve;
    public bool isSelected;
    public bool isRequiredDefault;
    public bool isEmpty;
    public bool canSell;
    public int totalAmmo;
    public int sellPricePerAmmo;
    public WeaponAmmoSlotSnapshot(
        int slotIndex,
        BulletSO bullet,
        int currentAmmo,
        int reserveAmmo,
        bool infiniteReserve,
        bool isSelected,
        bool isRequiredDefault,
        int sellPricePerAmmo)
    {
        this.slotIndex = slotIndex;
        this.bullet = bullet;
        bulletType = bullet != null ? bullet.AmmoType : BulletAmmoType.Special;
        bulletIcon = bullet != null ? bullet.PreviewImage : null;
        displayName = bullet != null ? bullet.DisplayName : string.Empty;
        this.currentAmmo = currentAmmo;
        this.reserveAmmo = reserveAmmo;
        this.infiniteReserve = infiniteReserve;
        this.isSelected = isSelected;
        this.isRequiredDefault = isRequiredDefault;
        isEmpty = bullet == null;
        totalAmmo = Mathf.Max(0, currentAmmo) + Mathf.Max(0, reserveAmmo);
        this.sellPricePerAmmo = Mathf.Max(0, sellPricePerAmmo);
        canSell = !isEmpty && !isRequiredDefault && !infiniteReserve && totalAmmo > 0 && this.sellPricePerAmmo > 0;
    }
}

/// <summary>
/// Full runtime ammo loadout snapshot for the HUD.
/// </summary>
[Serializable]
public struct WeaponAmmoLoadoutSnapshot
{
    public WeaponAmmoSlotSnapshot[] slots;
    public int selectedSlotIndex;

    public WeaponAmmoLoadoutSnapshot(
        WeaponAmmoSlotSnapshot[] slots,
        int selectedSlotIndex)
    {
        this.slots = slots;
        this.selectedSlotIndex = selectedSlotIndex;
    }
}

[CreateAssetMenu(
    fileName = "WeaponAmmoLoadoutEventChannel",
    menuName = "Events/Weapon/Weapon Ammo Loadout Event Channel")]
public class WeaponAmmoLoadoutEventChannelSO : ScriptableObject
{
    public event Action<WeaponAmmoLoadoutSnapshot> OnEventRaised;

    public WeaponAmmoLoadoutSnapshot Current { get; private set; }

    public void RaiseEvent(WeaponAmmoLoadoutSnapshot snapshot)
    {
        Current = snapshot;
        OnEventRaised?.Invoke(snapshot);
    }
}