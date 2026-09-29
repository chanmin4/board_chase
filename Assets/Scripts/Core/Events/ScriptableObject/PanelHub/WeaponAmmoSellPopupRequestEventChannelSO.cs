/*
 * Role hierarchy: Core > Events > ScriptableObject > PanelHub
 * Primary responsibility: Defines the WeaponAmmoSellPopupRequestEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponAmmoSellPopupRequestEventChannel",
    menuName = "Events/Weapon/Weapon Ammo Sell Popup Request Event Channel")]
public class WeaponAmmoSellPopupRequestEventChannelSO : ScriptableObject
{
    public event Action<WeaponAmmoSellPopupRequest> OnEventRaised;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(WeaponAmmoSellPopupRequest request)
    {
        OnEventRaised?.Invoke(request);
    }
}
