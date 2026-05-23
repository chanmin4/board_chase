using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponAmmoSellConfirmRequestEventChannel",
    menuName = "Events/Weapon/Weapon Ammo Sell Confirm Request Event Channel")]
public class WeaponAmmoSellConfirmRequestEventChannelSO : ScriptableObject
{
    public event Action<WeaponAmmoSellConfirmRequest> OnEventRaised;

    public void RaiseEvent(WeaponAmmoSellConfirmRequest request)
    {
        OnEventRaised?.Invoke(request);
    }
}