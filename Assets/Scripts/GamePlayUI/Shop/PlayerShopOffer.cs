public sealed class PlayerShopOffer
{
    public ItemSO Item { get; }
    public TreasureRewardKind Kind { get; }
    public PlayerShopItemRarity Rarity { get; }
    public int Price { get; }
    public int BundleAmount { get; }
    public string Description { get; }
    public int InitialStock { get; }
    public int RemainingStock { get; private set; }

    public BulletSO Bullet => Item as BulletSO;
    public WeaponSO Weapon => Item as WeaponSO;
    public PassiveItemSO PassiveItem => Item as PassiveItemSO;
    public ArmorItemSO ArmorItem => Item as ArmorItemSO;
    public bool IsBullet => Kind == TreasureRewardKind.Bullet && Bullet != null;
    public bool IsWeapon => Weapon != null;
    public bool IsPassive => Kind == TreasureRewardKind.Passive && PassiveItem != null;
    public bool IsArmor => Kind == TreasureRewardKind.Armor && ArmorItem != null;
    public bool IsValid => Item != null;
    public bool IsSoldOut => RemainingStock <= 0;
    public UnityEngine.Sprite Icon => Bullet != null ? Bullet.PreviewImage : Item != null ? Item.PreviewImage : null;
    public string DisplayName => ResolveDisplayName();

    public PlayerShopOffer(
        ItemSO item,
        TreasureRewardKind kind,
        PlayerShopItemRarity rarity,
        int price,
        int stock,
        int bundleAmount)
    {
        Item = item;
        Kind = kind;
        Rarity = rarity;
        Price = UnityEngine.Mathf.Max(0, price);
        InitialStock = UnityEngine.Mathf.Max(0, stock);
        RemainingStock = InitialStock;
        BundleAmount = UnityEngine.Mathf.Max(1, bundleAmount);
        Description = ResolveItemDescription(item);
    }

    public bool TryConsumeStock()
    {
        if (!IsValid || IsSoldOut)
            return false;

        RemainingStock--;
        return true;
    }

    private string ResolveDisplayName()
    {
        if (Bullet != null)
            return Bullet.DisplayName;

        if (Weapon != null)
            return Weapon.DisplayName;

        if (ArmorItem != null)
            return ArmorItem.DisplayName;

        if (Item == null)
            return "-";

        if (Item.Name != null)
        {
            string localized = Item.Name.GetLocalizedString();

            if (!string.IsNullOrWhiteSpace(localized))
                return localized;
        }

        return Item.name;
    }

    private static string ResolveItemDescription(ItemSO item)
    {
        if (item == null || item.Description == null)
            return string.Empty;

        return item.Description.GetLocalizedString();
    }
}
