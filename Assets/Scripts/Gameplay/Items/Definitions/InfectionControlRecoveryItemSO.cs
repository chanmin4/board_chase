/*
 * Role hierarchy: Gameplay > Items > Definitions
 * Primary responsibility: Defines the Home-shop service item that restores global Infection Control instantly.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "InfectionControlRecoveryItem",
    menuName = "Game/Shop/Infection Control Recovery Item")]
public sealed class InfectionControlRecoveryItemSO : ItemSO
{
    [Header("Home Shop Service")]
    [SerializeField] private string _serviceId = "infection_control_recovery";
    [SerializeField, Min(0.01f)] private float _controlRecoveryAmount = 1000f;
    [SerializeField, Min(0)] private int _priceIncreasePerPurchase = 5;

    public string ServiceId => string.IsNullOrWhiteSpace(_serviceId)
        ? "infection_control_recovery"
        : _serviceId;
    public float ControlRecoveryAmount => Mathf.Max(0.01f, _controlRecoveryAmount);
    public int PriceIncreasePerPurchase => Mathf.Max(0, _priceIncreasePerPurchase);

    // Purpose: Resolves the purchase price needed by this component.
    public int ResolvePurchasePrice(int previousPurchaseCount)
    {
        long price = (long)PurchasePrice +
                     (long)PriceIncreasePerPurchase * Mathf.Max(0, previousPurchaseCount);
        return price >= int.MaxValue ? int.MaxValue : (int)price;
    }
}
