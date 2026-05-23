using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponAmmoSellResultEventChannel",
    menuName = "Events/Weapon/Weapon Ammo Sell Result Event Channel")]
public class WeaponAmmoSellResultEventChannelSO : ScriptableObject
{
    public event Action<WeaponAmmoSellResult> OnEventRaised;

    public void RaiseEvent(WeaponAmmoSellResult result)
    {
        OnEventRaised?.Invoke(result);
    }
}