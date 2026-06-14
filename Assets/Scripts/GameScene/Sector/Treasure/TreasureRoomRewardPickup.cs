using UnityEngine;

[DisallowMultipleComponent]
public class TreasureRoomRewardPickup : MonoBehaviour
{
    [Header("Reward")]
    [SerializeField] private TreasureRewardKind _rewardKind;
    [SerializeField] private PassiveItemSO _passiveItem;
    [SerializeField] private BulletSO _bullet;
    [SerializeField, Min(1)] private int _bulletBundleAmount = 20;
    [SerializeField, Range(0f, 1f)] private float _bulletSellPriceRate = 0f;

    [Header("Pickup")]
    [SerializeField] private bool _destroyOnPickup = true;
    [SerializeField] private bool _logPickupFailures = true;

    [Header("Broadcasting")]
    [Tooltip("Optional legacy item pickup event. Passive rewards raise this after they are added to PlayerPassiveInventoryRuntime.")]
    [SerializeField] private ItemEventChannelSO _passiveItemPickedUpChannel;

    public void Initialize(TreasureRoomReward reward)
    {
        _rewardKind = reward.Kind;
        _passiveItem = reward.PassiveItem;
        _bullet = reward.Bullet;
        _bulletBundleAmount = Mathf.Max(1, reward.BulletBundleAmount);
        _bulletSellPriceRate = Mathf.Clamp01(reward.BulletSellPriceRate);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryPickup(other);
    }

    public bool TryPickup(Component picker)
    {
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

            default:
                picked = false;
                break;
        }

        if (picked && _destroyOnPickup)
            Destroy(gameObject);

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

        bool granted = loadout.TryGrantBullet(
            _bullet,
            _bulletBundleAmount,
            _bulletSellPriceRate,
            out _,
            out string message);

        if (!granted)
            LogPickupFailure(message);

        return granted;
    }

    private void LogPickupFailure(string message)
    {
        if (!_logPickupFailures)
            return;

        Debug.LogWarning($"[TreasureRoomRewardPickup] {message}", this);
    }
}
