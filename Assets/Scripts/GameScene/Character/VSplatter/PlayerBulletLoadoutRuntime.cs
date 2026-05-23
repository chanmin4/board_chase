using UnityEngine;

/// <summary>
/// Player bullet loadout runtime.
/// Owns 5 ammo slots, slot selection, shop purchase placement, selling, and HUD snapshots.
///
/// Slot rules:
/// Slot 1,2 = Attack
/// Slot 3,4 = Paint
/// Slot 5   = Special
///
/// Default bullets:
/// Slot 1 = default attack bullet
/// Slot 3 = default paint bullet
/// </summary>
[DisallowMultipleComponent]
public class PlayerBulletLoadoutRuntime : MonoBehaviour
{
    private const int SlotCount = 5;
    private const int DefaultAttackSlot = 0;
    private const int DefaultPaintSlot = 2;

    private sealed class AmmoSlotState
    {
        public BulletSO bullet;
        public int currentAmmo;
        public int reserveAmmo;
        public bool infiniteReserve;
        public bool requiredDefault;
        public int sellPricePerAmmo;

        public void Clear()
        {
            bullet = null;
            currentAmmo = 0;
            reserveAmmo = 0;
            infiniteReserve = false;
            requiredDefault = false;
            sellPricePerAmmo = 0;
        }
    }

    [Header("Need Ref - Default Bullets")]
    [Tooltip("Required. Automatically placed in slot 1. Must be AttackBulletSO.")]
    [SerializeField] private AttackBulletSO _defaultAttackBullet;

    [Tooltip("Required. Automatically placed in slot 3. Must be PaintBulletSO.")]
    [SerializeField] private PaintBulletSO _defaultPaintBullet;

    [Header("Need Ref - Event Channels")]
    [Tooltip("HUD requests the current ammo loadout snapshot through this channel.")]
    [SerializeField] private VoidEventChannelSO _requestWeaponAmmoLoadoutSnapshotChannel;

    [Tooltip("HUD drag/drop requests slot swaps through this channel.")]
    [SerializeField] private WeaponAmmoSlotSwapRequestEventChannelSO _slotSwapRequestChannel;

    [Tooltip("Slot buttons or input may request slot selection through this channel. Value is zero-based slot index.")]
    [SerializeField] private IntEventChannelSO _slotSelectRequestChannel;

    [Tooltip("Shop sends purchase requests here. This runtime places the purchased bullet into a valid empty slot.")]
    [SerializeField] private WeaponAmmoShopPurchaseRequestEventChannelSO _shopPurchaseRequestChannel;

    [Tooltip("This runtime broadcasts purchase success/fail results here.")]
    [SerializeField] private WeaponAmmoShopPurchaseResultEventChannelSO _shopPurchaseResultChannel;

    [Tooltip("Sell popup sends confirmed sell requests here.")]
    [SerializeField] private WeaponAmmoSellConfirmRequestEventChannelSO _sellConfirmRequestChannel;

    [Tooltip("This runtime broadcasts sell success/fail results here.")]
    [SerializeField] private WeaponAmmoSellResultEventChannelSO _sellResultChannel;

    [Tooltip("This runtime broadcasts the full ammo loadout snapshot to the HUD.")]
    [SerializeField] private WeaponAmmoLoadoutEventChannelSO _weaponAmmoLoadoutEventChannel;

    [Header("Don't Touch Ref Auto")]
    [Tooltip("Runtime debug value only. Do not edit in Inspector.")]
    [SerializeField] private int _selectedSlotIndex;

    private AmmoSlotState[] _slots;

    public int SelectedSlotIndex => _selectedSlotIndex;

    public BulletSO SelectedBullet =>
        IsValidSlot(_selectedSlotIndex) ? _slots[_selectedSlotIndex].bullet : null;

    private void Awake()
    {
        BuildRuntimeSlots();
    }

    private void OnEnable()
    {
        if (_requestWeaponAmmoLoadoutSnapshotChannel != null)
            _requestWeaponAmmoLoadoutSnapshotChannel.OnEventRaised += PublishSnapshot;

        if (_slotSwapRequestChannel != null)
            _slotSwapRequestChannel.OnEventRaised += HandleSlotSwapRequested;

        if (_slotSelectRequestChannel != null)
            _slotSelectRequestChannel.OnEventRaised += HandleSlotSelectRequested;

        if (_shopPurchaseRequestChannel != null)
            _shopPurchaseRequestChannel.OnEventRaised += HandleShopPurchaseRequested;

        if (_sellConfirmRequestChannel != null)
            _sellConfirmRequestChannel.OnEventRaised += HandleSellConfirmRequested;

        PublishSnapshot();
    }

    private void OnDisable()
    {
        if (_requestWeaponAmmoLoadoutSnapshotChannel != null)
            _requestWeaponAmmoLoadoutSnapshotChannel.OnEventRaised -= PublishSnapshot;

        if (_slotSwapRequestChannel != null)
            _slotSwapRequestChannel.OnEventRaised -= HandleSlotSwapRequested;

        if (_slotSelectRequestChannel != null)
            _slotSelectRequestChannel.OnEventRaised -= HandleSlotSelectRequested;

        if (_shopPurchaseRequestChannel != null)
            _shopPurchaseRequestChannel.OnEventRaised -= HandleShopPurchaseRequested;

        if (_sellConfirmRequestChannel != null)
            _sellConfirmRequestChannel.OnEventRaised -= HandleSellConfirmRequested;
    }

    public bool SelectSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return false;

        if (_slots[slotIndex].bullet == null)
            return false;

        _selectedSlotIndex = slotIndex;
        PublishSnapshot();

        return true;
    }

    public bool TrySwapSlots(int fromSlotIndex, int toSlotIndex)
    {
        if (!IsValidSlot(fromSlotIndex) || !IsValidSlot(toSlotIndex))
            return false;

        if (fromSlotIndex == toSlotIndex)
            return false;

        AmmoSlotState from = _slots[fromSlotIndex];
        AmmoSlotState to = _slots[toSlotIndex];

        if (from.bullet == null)
            return false;

        if (from.requiredDefault || to.requiredDefault)
            return false;

        if (!CanMoveSlotTo(from, toSlotIndex))
            return false;

        if (!CanMoveSlotTo(to, fromSlotIndex))
            return false;

        (_slots[fromSlotIndex], _slots[toSlotIndex]) =
            (_slots[toSlotIndex], _slots[fromSlotIndex]);

        if (_selectedSlotIndex == fromSlotIndex)
            _selectedSlotIndex = toSlotIndex;
        else if (_selectedSlotIndex == toSlotIndex)
            _selectedSlotIndex = fromSlotIndex;

        PublishSnapshot();
        return true;
    }

    public bool TryConsumeSelectedAmmo(int amount = 1)
    {
        if (!IsValidSlot(_selectedSlotIndex))
            return false;

        AmmoSlotState slot = _slots[_selectedSlotIndex];

        if (slot.bullet == null || amount <= 0)
            return false;

        if (slot.infiniteReserve)
            return true;

        if (slot.currentAmmo < amount)
            return false;

        slot.currentAmmo -= amount;
        PublishSnapshot();

        return true;
    }

    public void ReloadSelectedSlot()
    {
        // Magazine size is no longer owned by this runtime.
        // Later, connect this to player stats or bullet ammo config.
        PublishSnapshot();
    }

    private void BuildRuntimeSlots()
    {
        _slots = new AmmoSlotState[SlotCount];

        for (int i = 0; i < SlotCount; i++)
            _slots[i] = new AmmoSlotState();

        PlaceRequiredDefault(DefaultAttackSlot, _defaultAttackBullet);
        PlaceRequiredDefault(DefaultPaintSlot, _defaultPaintBullet);

        _selectedSlotIndex = FindFirstNonEmptySlot();
    }

    private void PlaceRequiredDefault(int slotIndex, BulletSO bullet)
    {
        if (!IsValidSlot(slotIndex) || bullet == null)
            return;

        if (!CanEquipBulletInSlot(slotIndex, bullet))
        {
            Debug.LogWarning(
                $"[PlayerBulletLoadoutRuntime] Default bullet type mismatch. slot={slotIndex + 1}, bullet={bullet.name}, type={bullet.AmmoType}",
                this);
            return;
        }

        AmmoSlotState slot = _slots[slotIndex];

        slot.bullet = bullet;
        slot.currentAmmo = 0;
        slot.reserveAmmo = 0;
        slot.infiniteReserve = true;
        slot.requiredDefault = true;
        slot.sellPricePerAmmo = 0;
    }

    private void HandleSlotSelectRequested(int slotIndex)
    {
        SelectSlot(slotIndex);
    }

    private void HandleSlotSwapRequested(WeaponAmmoSlotSwapRequest request)
    {
        TrySwapSlots(request.fromSlotIndex, request.toSlotIndex);
    }

    private void HandleShopPurchaseRequested(WeaponAmmoShopPurchaseRequest request)
    {
        if (request.bullet == null)
        {
            RaisePurchaseResult(request, false, -1, "Bullet is missing.");
            return;
        }

        if (!TryFindEmptyCompatibleSlot(request.bullet, out int slotIndex))
        {
            RaisePurchaseResult(request, false, -1, "Please empty a compatible ammo slot first.");
            return;
        }

        int bundleAmount = Mathf.Max(1, request.bundleAmount);

        AmmoSlotState slot = _slots[slotIndex];

        slot.bullet = request.bullet;
        slot.currentAmmo = bundleAmount;
        slot.reserveAmmo = 0;
        slot.infiniteReserve = false;
        slot.requiredDefault = false;
        slot.sellPricePerAmmo = Mathf.Max(
            1,
            Mathf.FloorToInt(request.totalPrice * request.sellPriceRate / bundleAmount));

        PublishSnapshot();

        RaisePurchaseResult(
            request,
            true,
            slotIndex,
            $"{request.bullet.DisplayName} equipped to slot {slotIndex + 1}.");
    }

    private void HandleSellConfirmRequested(WeaponAmmoSellConfirmRequest request)
    {
        if (!IsValidSlot(request.slotIndex))
        {
            RaiseSellResult(false, request.slotIndex, 0, 0, "Invalid ammo slot.");
            return;
        }

        AmmoSlotState slot = _slots[request.slotIndex];

        if (slot.bullet == null)
        {
            RaiseSellResult(false, request.slotIndex, 0, 0, "This slot is empty.");
            return;
        }

        if (slot.requiredDefault || slot.infiniteReserve)
        {
            RaiseSellResult(false, request.slotIndex, 0, 0, "Default ammo cannot be sold.");
            return;
        }

        int totalAmmo = slot.currentAmmo + slot.reserveAmmo;

        if (totalAmmo <= 0)
        {
            RaiseSellResult(false, request.slotIndex, 0, 0, "No ammo to sell.");
            return;
        }

        int sellAmount = Mathf.Clamp(request.amount, 1, totalAmmo);
        int remainingToSell = sellAmount;

        int reserveSold = Mathf.Min(slot.reserveAmmo, remainingToSell);
        slot.reserveAmmo -= reserveSold;
        remainingToSell -= reserveSold;

        int currentSold = Mathf.Min(slot.currentAmmo, remainingToSell);
        slot.currentAmmo -= currentSold;

        int gained = sellAmount * Mathf.Max(0, slot.sellPricePerAmmo);

        if (slot.currentAmmo <= 0 && slot.reserveAmmo <= 0)
            slot.Clear();

        if (!IsValidSlot(_selectedSlotIndex) || _slots[_selectedSlotIndex].bullet == null)
            _selectedSlotIndex = FindFirstNonEmptySlot();

        PublishSnapshot();

        RaiseSellResult(
            true,
            request.slotIndex,
            sellAmount,
            gained,
            $"Sold ammo x{sellAmount}.");
    }

    private bool TryFindEmptyCompatibleSlot(BulletSO bullet, out int slotIndex)
    {
        slotIndex = -1;

        for (int i = 0; i < SlotCount; i++)
        {
            if (_slots[i].bullet != null)
                continue;

            if (!CanEquipBulletInSlot(i, bullet))
                continue;

            slotIndex = i;
            return true;
        }

        return false;
    }

    private bool CanMoveSlotTo(AmmoSlotState state, int slotIndex)
    {
        if (state == null || state.bullet == null)
            return true;

        return CanEquipBulletInSlot(slotIndex, state.bullet);
    }

    private bool CanEquipBulletInSlot(int slotIndex, BulletSO bullet)
    {
        if (!IsValidSlot(slotIndex) || bullet == null)
            return false;

        return bullet.AmmoType == GetAllowedAmmoType(slotIndex);
    }

    private BulletAmmoType GetAllowedAmmoType(int slotIndex)
    {
        return slotIndex switch
        {
            0 => BulletAmmoType.Attack,
            1 => BulletAmmoType.Attack,
            2 => BulletAmmoType.Paint,
            3 => BulletAmmoType.Paint,
            4 => BulletAmmoType.Special,
            _ => BulletAmmoType.Special
        };
    }

    private int FindFirstNonEmptySlot()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (_slots[i].bullet != null)
                return i;
        }

        return 0;
    }

    private void PublishSnapshot()
    {
        if (_weaponAmmoLoadoutEventChannel == null || _slots == null)
            return;

        WeaponAmmoSlotSnapshot[] snapshots = new WeaponAmmoSlotSnapshot[SlotCount];

        for (int i = 0; i < SlotCount; i++)
        {
            AmmoSlotState slot = _slots[i];

            snapshots[i] = new WeaponAmmoSlotSnapshot(
                i,
                slot.bullet,
                slot.currentAmmo,
                slot.reserveAmmo,
                slot.infiniteReserve,
                i == _selectedSlotIndex,
                slot.requiredDefault,
                slot.sellPricePerAmmo);
        }

        _weaponAmmoLoadoutEventChannel.RaiseEvent(
            new WeaponAmmoLoadoutSnapshot(snapshots, _selectedSlotIndex));
    }

    private void RaisePurchaseResult(
        WeaponAmmoShopPurchaseRequest request,
        bool success,
        int slotIndex,
        string message)
    {
        if (_shopPurchaseResultChannel == null)
            return;

        _shopPurchaseResultChannel.RaiseEvent(
            new WeaponAmmoShopPurchaseResult(
                request.requestId,
                success,
                slotIndex,
                request.bullet,
                message));
    }

    private void RaiseSellResult(
        bool success,
        int slotIndex,
        int soldAmount,
        int currencyGained,
        string message)
    {
        if (_sellResultChannel == null)
            return;

        _sellResultChannel.RaiseEvent(
            new WeaponAmmoSellResult(
                success,
                slotIndex,
                soldAmount,
                currencyGained,
                message));
    }

    private bool IsValidSlot(int slotIndex)
    {
        return _slots != null && slotIndex >= 0 && slotIndex < SlotCount;
    }
}