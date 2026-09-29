/*
 * Role hierarchy: Core > Events > ScriptableObject > PanelHub
 * Primary responsibility: Defines the WeaponAmmoSellConfirmRequestEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponAmmoSellConfirmRequestEventChannel",
    menuName = "Events/Weapon/Weapon Ammo Sell Confirm Request Event Channel")]
public class WeaponAmmoSellConfirmRequestEventChannelSO : ScriptableObject
{
    public event Action<WeaponAmmoSellConfirmRequest> OnEventRaised;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(WeaponAmmoSellConfirmRequest request)
    {
        OnEventRaised?.Invoke(request);
    }
}
