using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponAmmoShopPurchaseRequestEventChannel",
    menuName = "Events/Weapon/Weapon Ammo Shop Purchase Request Event Channel")]
public class WeaponAmmoShopPurchaseRequestEventChannelSO : ScriptableObject
{
    public event Action<WeaponAmmoShopPurchaseRequest> OnEventRaised;

    public void RaiseEvent(WeaponAmmoShopPurchaseRequest request)
    {
        OnEventRaised?.Invoke(request);
    }
}