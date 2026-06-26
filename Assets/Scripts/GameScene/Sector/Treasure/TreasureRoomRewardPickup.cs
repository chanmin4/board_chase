using UnityEngine;

[DisallowMultipleComponent]
public class TreasureRoomRewardPickup : MonoBehaviour
{
    [Header("Reward")]
    [SerializeField] private TreasureRewardKind _rewardKind;
    [SerializeField] private PassiveItemSO _passiveItem;
    [SerializeField] private BulletSO _bullet;
    [SerializeField] private ArmorItemSO _armorItem;
    [SerializeField, Min(1)] private int _bulletBundleAmount = 20;
    [SerializeField, Range(0f, 1f)] private float _bulletSellPriceRate = 0f;

    [Header("Pickup")]
    [SerializeField] private bool _destroyOnPickup = true;
    [SerializeField] private bool _logPickupFailures = true;

    [Header("Broadcasting")]
    [Tooltip("Optional passive item pickup event. Passive rewards raise this after they are added to PlayerPassiveInventoryRuntime.")]
    [SerializeField] private ItemEventChannelSO _passiveItemPickedUpChannel;

    private bool _pickedUp;

    public TreasureRewardKind RewardKind => _rewardKind;
    public PassiveItemSO PassiveItem => _passiveItem;
    public BulletSO Bullet => _bullet;
    public ArmorItemSO ArmorItem => _armorItem;
    public bool CanInteract => !_pickedUp && HasReward;

    private bool HasReward
    {
        get
        {
            return _rewardKind switch
            {
                TreasureRewardKind.Passive => _passiveItem != null,
                TreasureRewardKind.Bullet => _bullet != null,
                TreasureRewardKind.Armor => _armorItem != null,
                _ => false
            };
        }
    }

    public void Initialize(TreasureRoomReward reward)
    {
        _rewardKind = reward.Kind;
        _passiveItem = reward.PassiveItem;
        _bullet = reward.Bullet;
        _armorItem = reward.ArmorItem;
        _bulletBundleAmount = Mathf.Max(1, reward.BulletBundleAmount);
        _bulletSellPriceRate = Mathf.Clamp01(reward.BulletSellPriceRate);
        _pickedUp = false;
    }

    public bool TryPickup(Component picker)
    {
        if (!CanInteract)
            return false;

        if (picker == null)
            return false;

        VSplatter_Character player = picker.GetComponentInParent<VSplatter_Character>();

        if (player == null)
            return false;

        bool picked;

        switch (_rewardKind)
        {
            case TreasureRewardKind.Passive:
                picked = TryPickupPassive(player);
                break;

            case TreasureRewardKind.Bullet:
                picked = TryPickupBullet(player);
                break;

            case TreasureRewardKind.Armor:
                picked = TryPickupArmor(player);
                break;

            default:
                picked = false;
                break;
        }

        if (picked)
        {
            _pickedUp = true;

            if (_destroyOnPickup)
                Destroy(gameObject);
        }

        return picked;
    }

    private bool TryPickupPassive(VSplatter_Character player)
    {
        if (_passiveItem == null)
        {
            LogPickupFailure("Passive item is missing.");
            return false;
        }

        PlayerPassiveInventoryRuntime inventory =
            player.GetComponentInParent<PlayerPassiveInventoryRuntime>();

        if (inventory == null)
        {
            LogPickupFailure("PlayerPassiveInventoryRuntime is missing on player hierarchy.");
            return false;
        }

        if (!inventory.TryAdd(_passiveItem))
        {
            LogPickupFailure($"Passive item already owned or rejected. item={_passiveItem.name}");
            return false;
        }

        _passiveItemPickedUpChannel?.RaiseEvent(_passiveItem);
        return true;
    }

    private bool TryPickupBullet(VSplatter_Character player)
    {
        if (_bullet == null)
        {
            LogPickupFailure("Bullet is missing.");
            return false;
        }

        PlayerBulletLoadoutRuntime loadout =
            player.GetComponentInParent<PlayerBulletLoadoutRuntime>();

        if (loadout == null)
        {
            LogPickupFailure("PlayerBulletLoadoutRuntime is missing on player hierarchy.");
            return false;
        }

        bool granted = loadout.TryAcquireTreasureBullet(
            _bullet,
            _bulletBundleAmount,
            _bulletSellPriceRate,
            out _,
            out string message);

        if (!granted)
            LogPickupFailure(message);

        return granted;
    }

    private bool TryPickupArmor(VSplatter_Character player)
    {
        if (_armorItem == null)
        {
            LogPickupFailure("Armor item is missing.");
            return false;
        }

        EntityEquipmentRuntime equipment =
            player.GetComponentInParent<EntityEquipmentRuntime>();

        if (equipment == null)
        {
            LogPickupFailure("EntityEquipmentRuntime is missing on player hierarchy.");
            return false;
        }

        equipment.EquipArmor(_armorItem);
        return true;
    }

    private void LogPickupFailure(string message)
    {
        if (!_logPickupFailures)
            return;

        Debug.LogWarning($"[TreasureRoomRewardPickup] {message}", this);
    }
}
