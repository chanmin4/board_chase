/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Implements the focused WeaponAmmoSnapshot responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[Serializable]
public struct WeaponAmmoSnapshot
{
    public WeaponSO weapon;
    public Sprite weaponIcon;
    public int currentAmmo;
    public int maxAmmo;
    public bool isReloading;
    public float reload01;

    public WeaponAmmoSnapshot(
        WeaponSO weapon,
        int currentAmmo,
        int maxAmmo,
        bool isReloading,
        float reload01)
    {
        this.weapon = weapon;
        this.weaponIcon = weapon != null ? weapon.PreviewImage : null;
        this.currentAmmo = currentAmmo;
        this.maxAmmo = maxAmmo;
        this.isReloading = isReloading;
        this.reload01 = reload01;
    }
}

[CreateAssetMenu(
    fileName = "WeaponAmmoEventChannel",
    menuName = "Events/Weapon/Weapon Ammo Event Channel")]
public class WeaponAmmoEventChannelSO : ScriptableObject
{
    public event Action<WeaponAmmoSnapshot> OnEventRaised;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(WeaponAmmoSnapshot snapshot)
    {
        OnEventRaised?.Invoke(snapshot);
    }
}
