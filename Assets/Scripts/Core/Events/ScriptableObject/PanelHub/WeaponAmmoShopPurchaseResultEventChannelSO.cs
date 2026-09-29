/*
 * Role hierarchy: Core > Events > ScriptableObject > PanelHub
 * Primary responsibility: Defines the WeaponAmmoShopPurchaseResultEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponAmmoShopPurchaseResultEventChannel",
    menuName = "Events/Weapon/Weapon Ammo Shop Purchase Result Event Channel")]
public class WeaponAmmoShopPurchaseResultEventChannelSO : ScriptableObject
{
    public event Action<WeaponAmmoShopPurchaseResult> OnEventRaised;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(WeaponAmmoShopPurchaseResult result)
    {
        OnEventRaised?.Invoke(result);
    }
}
