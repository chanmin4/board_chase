using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponAmmoSellPopupRequestEventChannel",
    menuName = "Events/Weapon/Weapon Ammo Sell Popup Request Event Channel")]
public class WeaponAmmoSellPopupRequestEventChannelSO : ScriptableObject
{
    public event Action<WeaponAmmoSellPopupRequest> OnEventRaised;

    public void RaiseEvent(WeaponAmmoSellPopupRequest request)
    {
        OnEventRaised?.Invoke(request);
    }
}