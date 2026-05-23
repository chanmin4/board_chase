public sealed class PlayerShopOffer
{
    public PlayerShopCatalogSO.BulletShopEntry Entry { get; }
    public int InitialStock { get; }
    public int RemainingStock { get; private set; }

    public BulletSO Bullet => Entry != null ? Entry.Bullet : null;
    public PlayerShopItemRarity Rarity => Entry != null ? Entry.Rarity : PlayerShopItemRarity.Normal;
    public int Price => Entry != null ? Entry.Price : 0;
    public int BundleAmount => Entry != null ? Entry.BundleAmount : 0;
    public string Description => Entry != null ? Entry.Description : string.Empty;
    public bool IsValid => Entry != null && Entry.IsValid;
    public bool IsSoldOut => RemainingStock <= 0;

    public PlayerShopOffer(PlayerShopCatalogSO.BulletShopEntry entry)
    {
        Entry = entry;
        InitialStock = entry != null ? entry.Stock : 0;
        RemainingStock = InitialStock;
    }

    public bool TryConsumeStock()
    {
        if (!IsValid || IsSoldOut)
            return false;

        RemainingStock--;
        return true;
    }
}