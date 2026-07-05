using System;
using UnityEngine;

[Serializable]
public struct WeaponAmmoSellPopupRequest
{
    public WeaponAmmoSlotSnapshot slot;

    public WeaponAmmoSellPopupRequest(WeaponAmmoSlotSnapshot slot)
    {
        this.slot = slot;
    }
}

[Serializable]
public struct WeaponAmmoSellConfirmRequest
{
    public int slotIndex;
    public int amount;

    public WeaponAmmoSellConfirmRequest(int slotIndex, int amount)
    {
        this.slotIndex = slotIndex;
        this.amount = amount;
    }
}

[Serializable]
public struct WeaponAmmoSellResult
{
    public bool success;
    public int slotIndex;
    public int soldAmount;
    public int currencyGained;
    public string message;

    public WeaponAmmoSellResult(
        bool success,
        int slotIndex,
        int soldAmount,
        int currencyGained,
        string message)
    {
        this.success = success;
        this.slotIndex = slotIndex;
        this.soldAmount = soldAmount;
        this.currencyGained = currencyGained;
        this.message = message;
    }
}