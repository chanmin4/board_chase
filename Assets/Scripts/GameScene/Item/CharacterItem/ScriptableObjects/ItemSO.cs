using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Item", menuName = "Item")]
public class ItemSO : SerializableScriptableObject
{
    [Header("Display")]
    [Tooltip("The localized name of the item.")]
    [SerializeField] private LocalizedString _name = default;

    [Tooltip("A preview image used by UI, shop, treasure, inventory, etc.")]
    [SerializeField] private Sprite _previewImage = default;

    [Tooltip("The localized description of the item.")]
    [SerializeField] private LocalizedString _description = default;

    [Header("Loot")]
    [Tooltip("Rarity used by treasure rooms, shop rooms, and shop UI.")]
    [SerializeField] private PlayerShopItemRarity _rarity = PlayerShopItemRarity.Normal;

    [Tooltip("Base purchase price. Shops sell at this price and buy back using their sell price rate.")]
    [SerializeField, Min(0)] private int _purchasePrice = 10;

    [Tooltip("World pickup prefab spawned into the scene when this item appears as loot.")]
    [FormerlySerializedAs("_prefab")]
    [SerializeField] private GameObject _worldItemPrefab = default;

    [Header("Inventory")]
    [Tooltip("1 means non-stackable. Higher than 1 means this item can stack up to that amount.")]
    [SerializeField, Min(1)] private int _maxStack = 1;

    public LocalizedString Name => _name;
    public Sprite PreviewImage => _previewImage;
    public LocalizedString Description => _description;
    public PlayerShopItemRarity Rarity => _rarity;
    public int PurchasePrice => Mathf.Max(0, _purchasePrice);
    public GameObject WorldItemPrefab => _worldItemPrefab;
    public int MaxStack => Mathf.Max(1, _maxStack);

    public virtual bool IsLocalized { get; }
    public virtual LocalizedSprite LocalizePreviewImage { get; }
}
