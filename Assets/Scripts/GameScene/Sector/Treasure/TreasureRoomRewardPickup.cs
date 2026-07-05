using System;
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
    [Tooltip("Optional passive item pickup event. Passive rewards raise this after they are added to PlayerInventoryRuntime.")]
    [SerializeField] private ItemEventChannelSO _passiveItemPickedUpChannel;

    private bool _pickedUp;

    public event Action<TreasureRoomRewardPickup> PickedUp;

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

        PlayerInventoryRuntime inventory =
            player.GetComponentInParent<PlayerInventoryRuntime>();

        if (inventory == null)
        {
            LogPickupFailure("PlayerInventoryRuntime is missing on player hierarchy.");
            return false;
        }

        bool picked = inventory.TryPickupTreasureReward(
            _rewardKind,
            _passiveItem,
            _bullet,
            _armorItem,
            _bulletBundleAmount,
            _bulletSellPriceRate,
            out string message);

        if (!picked)
            LogPickupFailure(message);

        if (picked)
        {
            _pickedUp = true;
            PickedUp?.Invoke(this);

            if (_rewardKind == TreasureRewardKind.Passive && _passiveItem != null)
                _passiveItemPickedUpChannel?.RaiseEvent(_passiveItem);

            if (_destroyOnPickup)
                Destroy(gameObject);
        }

        return picked;
    }

    private void LogPickupFailure(string message)
    {
        if (!_logPickupFailures)
            return;

        Debug.LogWarning($"[TreasureRoomRewardPickup] {message}", this);
    }
}
