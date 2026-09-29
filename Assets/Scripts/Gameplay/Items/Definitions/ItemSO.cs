/*
 * Role hierarchy: Gameplay > Items > Definitions
 * Primary responsibility: Defines the shared contract implemented by the ItemKind role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Serialization;

public enum ItemKind
{
    Misc = 0,
    Passive = 1,
    Weapon = 2,
    Armor = 3,
    Bullet = 4,
    Currency = 5,
    Usable = 6,
    Backpack = 7,
    GasMask = 8,
    Key = 9,
    KeyTool = 10,
    Valuable = 11
}

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

    [Tooltip("Purely visual prefab instantiated below the shared ItemPickup prefab's VisualRoot. It must not contain pickup, interaction, or gameplay logic. Existing item assets keep their current reference through this renamed field.")]
    [FormerlySerializedAs("_prefab")]
    [FormerlySerializedAs("_worldItemPrefab")]
    [SerializeField] private GameObject _worldVisualPrefab = default;

    [Header("Inventory")]
    [Tooltip("1 means non-stackable. Higher than 1 means this item can stack up to that amount.")]
    [SerializeField, Min(1)] private int _maxStack = 1;

    [Tooltip("Maximum total amount the player may own across inventory, equipped slots, and quick slots. 0 means unlimited. This is an ownership cap, not a per-slot stack size.")]
    [SerializeField, Min(0)] private int _maxOwnedAmount;

    public LocalizedString Name => _name;
    public Sprite PreviewImage => _previewImage;
    public LocalizedString Description => _description;
    public PlayerShopItemRarity Rarity => _rarity;
    public int PurchasePrice => Mathf.Max(0, _purchasePrice);
    public GameObject WorldVisualPrefab => _worldVisualPrefab;
    public virtual int MaxStack => Mathf.Max(1, _maxStack);
    public virtual int MaxOwnedAmount => Mathf.Max(0, _maxOwnedAmount);
    public virtual ItemKind Kind => ItemKind.Misc;

    public virtual bool IsLocalized { get; }
    public virtual LocalizedSprite LocalizePreviewImage { get; }
}
