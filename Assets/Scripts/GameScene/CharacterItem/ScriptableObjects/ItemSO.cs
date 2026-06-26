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

   	[Tooltip("World pickup prefab spawned into the scene when this item appears as loot.")]
    [FormerlySerializedAs("_prefab")]
    [SerializeField] private GameObject _worldItemPrefab = default;

    public LocalizedString Name => _name;
    public Sprite PreviewImage => _previewImage;
    public LocalizedString Description => _description;
    public PlayerShopItemRarity Rarity => _rarity;
    public GameObject WorldItemPrefab => _worldItemPrefab;

    public virtual bool IsLocalized { get; }
    public virtual LocalizedSprite LocalizePreviewImage { get; }
}