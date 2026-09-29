/*
 * Role hierarchy: Core > Events > ScriptableObject > PanelHub
 * Primary responsibility: Defines the WeaponAmmoSellResultEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponAmmoSellResultEventChannel",
    menuName = "Events/Weapon/Weapon Ammo Sell Result Event Channel")]
public class WeaponAmmoSellResultEventChannelSO : ScriptableObject
{
    public event Action<WeaponAmmoSellResult> OnEventRaised;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(WeaponAmmoSellResult result)
    {
        OnEventRaised?.Invoke(result);
    }
}
