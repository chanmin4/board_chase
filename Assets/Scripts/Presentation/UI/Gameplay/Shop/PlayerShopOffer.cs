/*
 * Role hierarchy: Presentation > UI > Gameplay > Shop
 * Primary responsibility: Handles PlayerShopOffer interaction, pickup, trade, loot, or storage flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine.Localization;

public sealed class PlayerShopOffer
{
    public ItemSO Item { get; }
    public TreasureRewardKind Kind { get; }
    public PlayerShopItemRarity Rarity { get; }
    public int Price { get; private set; }
    public int BundleAmount { get; }
    public string Description { get; }
    public int InitialStock { get; }
    public int RemainingStock { get; private set; }
    public bool IsUnlimitedStock { get; }
    public bool CanReroll { get; }
    private readonly List<ShopTradeRequirement> _tradeRequirements;

    public BulletSO Bullet => Item as BulletSO;
    public WeaponSO Weapon => Item as WeaponSO;
    public PassiveItemSO PassiveItem => Item as PassiveItemSO;
    public ArmorItemSO ArmorItem => Item as ArmorItemSO;
    public LocalizedString DescriptionReference => Item != null ? Item.Description : null;
    public bool IsBullet => Kind == TreasureRewardKind.Bullet && Bullet != null;
    public bool IsWeapon => Weapon != null;
    public bool IsPassive => Kind == TreasureRewardKind.Passive && PassiveItem != null;
    public bool IsArmor => Kind == TreasureRewardKind.Armor && ArmorItem != null;
    public bool IsValid => Item != null;
    // Unlimited offers deliberately ignore the serialized stock value. This keeps
    // service-style products purchasable even when their authored stock is zero.
    public bool IsSoldOut => !IsUnlimitedStock && RemainingStock <= 0;
    public string StockDisplay => IsUnlimitedStock ? "∞" : RemainingStock.ToString();
    public UnityEngine.Sprite Icon => Bullet != null ? Bullet.PreviewImage : Item != null ? Item.PreviewImage : null;
    public string DisplayName => ResolveDisplayName();
    public IReadOnlyList<ShopTradeRequirement> TradeRequirements => _tradeRequirements;
    public bool UsesPhysicalPayment => _tradeRequirements.Count > 0;

    /// <summary>
    /// Keeps malformed authored payment entries visible to the transaction
    /// layer so it can fail safely rather than reverting to a numerical price.
    /// </summary>
    public bool HasValidPhysicalPayment
    {
        get
        {
            if (!UsesPhysicalPayment)
                return false;

            for (int index = 0; index < _tradeRequirements.Count; index++)
            {
                if (_tradeRequirements[index] == null || !_tradeRequirements[index].IsValid)
                    return false;
            }

            return true;
        }
    }

    public PlayerShopOffer(
        ItemSO item,
        TreasureRewardKind kind,
        PlayerShopItemRarity rarity,
        int price,
        int stock,
        int bundleAmount,
        bool unlimitedStock = false,
        bool allowReroll = true,
        IReadOnlyList<ShopTradeRequirement> tradeRequirements = null)
    {
        Item = item;
        Kind = kind;
        Rarity = rarity;
        Price = UnityEngine.Mathf.Max(0, price);
        InitialStock = UnityEngine.Mathf.Max(0, stock);
        RemainingStock = InitialStock;
        IsUnlimitedStock = unlimitedStock;
        CanReroll = allowReroll;
        BundleAmount = UnityEngine.Mathf.Max(1, bundleAmount);
        Description = ResolveItemDescription(item);
        _tradeRequirements = CopyTradeRequirements(tradeRequirements);
    }

    // Purpose: Attempts to consume stock and reports whether the request succeeded.
    public bool TryConsumeStock()
    {
        if (!IsValid || (!IsUnlimitedStock && IsSoldOut))
            return false;

        if (IsUnlimitedStock)
            return true;

        RemainingStock--;
        return true;
    }

    /// <summary>
    /// Reverses one successful finite-stock reservation when a later purchase
    /// step fails. It is intentionally unavailable for unlimited service offers.
    /// </summary>
    public void RestoreConsumedStock()
    {
        if (!IsUnlimitedStock)
            RemainingStock = UnityEngine.Mathf.Clamp(RemainingStock + 1, 0, InitialStock);
    }

    // Purpose: Updates the price state.
    public void SetPrice(int price)
    {
        Price = UnityEngine.Mathf.Max(0, price);
    }

    // Purpose: Builds a concise trade summary for shop cards and tooltips.
    public string BuildTradeSummary()
    {
        if (!UsesPhysicalPayment)
            return "ITEM PAYMENT REQUIRED";

        List<string> parts = new List<string>();
        for (int index = 0; index < _tradeRequirements.Count; index++)
        {
            ShopTradeRequirement requirement = _tradeRequirements[index];
            if (requirement == null || !requirement.IsValid)
                continue;

            parts.Add($"{InventoryItemDisplayUtility.ResolveItemName(requirement.Item)} x{requirement.Amount}");
        }

        return parts.Count > 0 ? string.Join(" + ", parts) : "INVALID PAYMENT";
    }

    /// <summary>
    /// Applies saved finite stock after an offer has been reconstructed from
    /// its stable ItemSO identity. Unlimited offers keep this value only for
    /// diagnostics because their purchase rule deliberately ignores stock.
    /// </summary>
    public void RestoreRemainingStock(int remainingStock)
    {
        RemainingStock = UnityEngine.Mathf.Clamp(remainingStock, 0, InitialStock);
    }

    // Purpose: Resolves the display name needed by this component.
    private string ResolveDisplayName()
    {
        if (Item == null)
            return "-";

        return InventoryItemDisplayUtility.ResolveItemName(Item);
    }

    // Purpose: Resolves the item description needed by this component.
    private static string ResolveItemDescription(ItemSO item)
    {
        return string.Empty;
    }

    private static List<ShopTradeRequirement> CopyTradeRequirements(
        IReadOnlyList<ShopTradeRequirement> source)
    {
        List<ShopTradeRequirement> copied = new List<ShopTradeRequirement>();

        for (int index = 0; source != null && index < source.Count; index++)
        {
            ShopTradeRequirement requirement = source[index];
            copied.Add(requirement == null
                ? null
                : new ShopTradeRequirement(requirement.Item, requirement.Amount));
        }

        return copied;
    }
}
