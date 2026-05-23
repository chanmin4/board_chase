using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponAmmoShopPurchaseResultEventChannel",
    menuName = "Events/Weapon/Weapon Ammo Shop Purchase Result Event Channel")]
public class WeaponAmmoShopPurchaseResultEventChannelSO : ScriptableObject
{
    public event Action<WeaponAmmoShopPurchaseResult> OnEventRaised;

    public void RaiseEvent(WeaponAmmoShopPurchaseResult result)
    {
        OnEventRaised?.Invoke(result);
    }
}