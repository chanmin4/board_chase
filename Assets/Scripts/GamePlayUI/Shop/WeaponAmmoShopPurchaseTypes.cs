using System;
using UnityEngine;

[Serializable]
public struct WeaponAmmoShopPurchaseRequest
{
    public int requestId;
    public BulletSO bullet;
    public int bundleAmount;
    public int totalPrice;
    public float sellPriceRate;

    public WeaponAmmoShopPurchaseRequest(
        int requestId,
        BulletSO bullet,
        int bundleAmount,
        int totalPrice,
        float sellPriceRate)
    {
        this.requestId = requestId;
        this.bullet = bullet;
        this.bundleAmount = bundleAmount;
        this.totalPrice = totalPrice;
        this.sellPriceRate = sellPriceRate;
    }
}

[Serializable]
public struct WeaponAmmoShopPurchaseResult
{
    public int requestId;
    public bool success;
    public int slotIndex;
    public BulletSO bullet;
    public string message;

    public WeaponAmmoShopPurchaseResult(
        int requestId,
        bool success,
        int slotIndex,
        BulletSO bullet,
        string message)
    {
        this.requestId = requestId;
        this.success = success;
        this.slotIndex = slotIndex;
        this.bullet = bullet;
        this.message = message;
    }
}