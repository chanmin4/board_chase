/*
 * Role hierarchy: Core > Events > ScriptableObject > PanelHub
 * Primary responsibility: Defines the WeaponAmmoShopPurchaseRequestEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponAmmoShopPurchaseRequestEventChannel",
    menuName = "Events/Weapon/Weapon Ammo Shop Purchase Request Event Channel")]
public class WeaponAmmoShopPurchaseRequestEventChannelSO : ScriptableObject
{
    public event Action<WeaponAmmoShopPurchaseRequest> OnEventRaised;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(WeaponAmmoShopPurchaseRequest request)
    {
        OnEventRaised?.Invoke(request);
    }
}
