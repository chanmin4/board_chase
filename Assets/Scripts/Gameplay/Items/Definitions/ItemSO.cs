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
    [Header("사용 레벨 · 0이면 Game System Tool의 공통 규칙")]
    [SerializeField, Min(0)] private int _usageLevelOverride;
    public int UsageLevelOverride => Mathf.Max(0,_usageLevelOverride);
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
    public const int AmmoStackLimit = 60;
    [Tooltip("탄약의 칸당 수량. 최대 60발입니다. 화폐는 500,000개, 그 외는 1개로 고정됩니다.")]
    [SerializeField, Range(1, AmmoStackLimit)] private int _maxStack = AmmoStackLimit;

    [Tooltip("개당 무게(kg). -1은 종류별 임시 기본값, 0 이상은 직접 지정. 모듈에는 적용되지 않습니다.")]
    [SerializeField, Min(-1f)] private float _weightKg = -1f;

    public float WeightKg
    {
        get
        {
            if (this is PassiveItemSO) return 0f;
            if (_weightKg >= 0f && !float.IsInfinity(_weightKg)) return _weightKg;
            // Editable starting values, not per-item balance decisions.
            return this switch
            {
                BulletSO bullet => bullet.Family switch
                {
                    BulletFamily.Light => .012f, BulletFamily.Medium => .017f,
                    BulletFamily.Heavy => .026f, BulletFamily.Sniper => .032f,
                    BulletFamily.ShotgunShell => .045f, BulletFamily.Rocket => 1f,
                    _ => .02f
                },
                RangedWeaponSO firearm => firearm.WeaponType switch
                {
                    RangedWeaponType.Pistol => .9f, RangedWeaponType.Revolver => 1.2f,
                    RangedWeaponType.SubmachineGun => 2.5f, RangedWeaponType.LightMachineGun => 7f,
                    RangedWeaponType.SniperRifle => 5.5f, RangedWeaponType.Shotgun => 3.5f,
                    RangedWeaponType.RocketLauncher => 7.5f, _ => 3.4f
                },
                ArmorItemSO => 5f,
                BackpackItemSO => 1.2f,
                GasMaskItemSO => .65f,
                ThrowableUsableItemSO => .4f,
                KeyItemSO => .04f,
                KeyToolItemSO => .2f,
                CurrencyItemSO => .005f,
                WeaponSO => .6f,
                _ => .25f
            };
        }
    }

    public float WeightForAmount(int amount) => WeightKg * Mathf.Max(0, amount);

    [Tooltip("Maximum total amount the player may own across inventory, equipped slots, and quick slots. 0 means unlimited. This is an ownership cap, not a per-slot stack size.")]
    [SerializeField, Min(0)] private int _maxOwnedAmount;

    public LocalizedString Name => _name;
    public Sprite PreviewImage => _previewImage;
    public LocalizedString Description => _description;
    public virtual PlayerShopItemRarity Rarity => _rarity;
    public int PurchasePrice => Mathf.Max(0, _purchasePrice);
    public GameObject WorldVisualPrefab => _worldVisualPrefab;
    public virtual int MaxStack => this is BulletSO ? Mathf.Clamp(_maxStack, 1, AmmoStackLimit) : 1;
    public virtual int MaxOwnedAmount => Mathf.Max(0, _maxOwnedAmount);
    public virtual ItemKind Kind => ItemKind.Misc;

    public virtual bool IsLocalized { get; }
    public virtual LocalizedSprite LocalizePreviewImage { get; }
}
