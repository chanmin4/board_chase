using UnityEngine;

/// <summary>
/// Player bullet loadout runtime.
/// Owns ammo slots, active bullet per ammo type, current magazine ammo,
/// reserve ammo, shop purchase placement, selling, reload, and HUD snapshots.
/// 
/// Runtime refs are received by event channels.
/// No GetComponent lookup for PlayerStatsRuntime.
/// </summary>
[DisallowMultipleComponent]
public class PlayerBulletLoadoutRuntime : MonoBehaviour
{
    private const int SlotCount = 5;
    private const int DefaultAttackSlot = 0;
    private const int DefaultPaintSlot = 2;
    private const int MissingStatsEmergencyMagazineSize = 1;

    private sealed class AmmoSlotState
    {
        public BulletSO bullet;
        public int currentAmmo;
        public int reserveAmmo;
        public bool infiniteReserve;
        public bool requiredDefault;
        public int sellPricePerAmmo;

        public int TotalAmmo => Mathf.Max(0, currentAmmo) + Mathf.Max(0, reserveAmmo);

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
    [SerializeField] private AttackBulletSO _defaultAttackBullet;
    [SerializeField] private PaintBulletSO _defaultPaintBullet;

    [Header("Need Ref - Runtime Ready")]
    [SerializeField] private PlayerStatsRuntimeReadyEventChannelSO _statsRuntimeReadyChannel;
    [SerializeField] private PlayerBulletLoadoutRuntimeReadyEventChannelSO _loadoutRuntimeReadyChannel;

    [Header("Need Ref - Event Channels")]
    [SerializeField] private VoidEventChannelSO _requestWeaponAmmoLoadoutSnapshotChannel;
    [SerializeField] private WeaponAmmoSlotSwapRequestEventChannelSO _slotSwapRequestChannel;
    [SerializeField] private IntEventChannelSO _slotSelectRequestChannel;
    [SerializeField] private WeaponAmmoShopPurchaseRequestEventChannelSO _shopPurchaseRequestChannel;
    [SerializeField] private WeaponAmmoShopPurchaseResultEventChannelSO _shopPurchaseResultChannel;
    [SerializeField] private WeaponAmmoSellConfirmRequestEventChannelSO _sellConfirmRequestChannel;
    [SerializeField] private WeaponAmmoSellResultEventChannelSO _sellResultChannel;
    [SerializeField] private WeaponAmmoLoadoutEventChannelSO _weaponAmmoLoadoutEventChannel;

    [Header("Don't Touch Ref Auto")]
    [SerializeField] private PlayerStatsRuntime _statsRuntime;
    [SerializeField] private int _activeAttackSlotIndex = DefaultAttackSlot;
    [SerializeField] private int _activePaintSlotIndex = DefaultPaintSlot;
    [SerializeField] private int _activeSpecialSlotIndex = -1;
    [SerializeField] private int _lastSelectedSlotIndex = DefaultAttackSlot;

    private AmmoSlotState[] _slots;
    private bool _warnedMissingStatsRuntime;

    private int GetMagazineSize(BulletSO bullet)
    {
        if (_statsRuntime != null)
            return Mathf.Max(1, _statsRuntime.ResolveMagazineSize(bullet));

        if (!_warnedMissingStatsRuntime)
        {
            _warnedMissingStatsRuntime = true;
            Debug.LogWarning("[PlayerBulletLoadoutRuntime] PlayerStatsRuntime is not ready. Using emergency magazine size.", this);
        }

        return MissingStatsEmergencyMagazineSize;
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

        if (_statsRuntimeReadyChannel != null)
        {
            _statsRuntimeReadyChannel.OnEventRaised += HandleStatsRuntimeReady;

            if (_statsRuntimeReadyChannel.HasCurrent)
                HandleStatsRuntimeReady(_statsRuntimeReadyChannel.Current);
        }
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

        if (_statsRuntimeReadyChannel != null)
            _statsRuntimeReadyChannel.OnEventRaised -= HandleStatsRuntimeReady;

        if (_loadoutRuntimeReadyChannel != null)
            _loadoutRuntimeReadyChannel.Clear(this);
    }

    private void HandleStatsRuntimeReady(PlayerStatsRuntime statsRuntime)
    {
        if (statsRuntime == null)
            return;

        _statsRuntime = statsRuntime;
        _warnedMissingStatsRuntime = false;

        if (_slots == null)
            BuildRuntimeSlots();
        else
            ClampAllMagazineAmmo();

        PublishSnapshot();

        if (_loadoutRuntimeReadyChannel != null)
            _loadoutRuntimeReadyChannel.RaiseEvent(this);
    }

    public bool SelectSlot(int slotIndex)
    {
        if (!IsRuntimeReady() || !IsValidSlot(slotIndex))
            return false;

        AmmoSlotState slot = _slots[slotIndex];

        if (slot.bullet == null)
            return false;

        SetActiveSlot(slot.bullet.AmmoType, slotIndex);
        _lastSelectedSlotIndex = slotIndex;

        PublishSnapshot();
        return true;
    }

    public bool HasLoadedAmmo(BulletAmmoType ammoType)
    {
        if (!IsRuntimeReady())
            return false;

        int slotIndex = GetActiveSlotIndex(ammoType);

        if (!IsValidSlot(slotIndex))
            return false;

        AmmoSlotState slot = _slots[slotIndex];

        return slot.bullet != null &&
               slot.bullet.AmmoType == ammoType &&
               slot.currentAmmo > 0;
    }
    public bool TryGetActiveAttackBullet(out AttackBulletSO bullet)
    {
        bullet = GetActiveBullet(BulletAmmoType.Attack) as AttackBulletSO;
        return bullet != null;
    }

    public bool TryGetActivePaintBullet(out PaintBulletSO bullet)
    {
        bullet = GetActiveBullet(BulletAmmoType.Paint) as PaintBulletSO;
        return bullet != null;
    }

    public bool TryConsumeAttackAmmo(int amount, out AttackBulletSO bullet)
    {
        bullet = null;

        if (!TryConsumeAmmo(BulletAmmoType.Attack, amount, out BulletSO consumedBullet))
            return false;

        bullet = consumedBullet as AttackBulletSO;
        return bullet != null;
    }

    public bool TryConsumePaintAmmo(int amount, out PaintBulletSO bullet)
    {
        bullet = null;

        if (!TryConsumeAmmo(BulletAmmoType.Paint, amount, out BulletSO consumedBullet))
            return false;

        bullet = consumedBullet as PaintBulletSO;
        return bullet != null;
    }

    private bool TryConsumeAmmo(BulletAmmoType ammoType, int amount, out BulletSO bullet)
    {
        bullet = null;

        if (!IsRuntimeReady() || amount <= 0)
            return false;

        int slotIndex = GetActiveSlotIndex(ammoType);

        if (!IsValidSlot(slotIndex))
            return false;

        AmmoSlotState slot = _slots[slotIndex];

        if (slot.bullet == null || slot.bullet.AmmoType != ammoType)
            return false;

        if (slot.currentAmmo < amount)
            return false;

        slot.currentAmmo -= amount;
        bullet = slot.bullet;

        if (!slot.infiniteReserve && slot.currentAmmo <= 0 && slot.reserveAmmo <= 0)
            slot.Clear();

        EnsureActiveSlotsValid();
        PublishSnapshot();

        return bullet != null;
    }

    public bool TrySwapSlots(int fromSlotIndex, int toSlotIndex)
    {
        if (!IsRuntimeReady())
            return false;

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

        SwapActiveIndex(ref _activeAttackSlotIndex, fromSlotIndex, toSlotIndex);
        SwapActiveIndex(ref _activePaintSlotIndex, fromSlotIndex, toSlotIndex);
        SwapActiveIndex(ref _activeSpecialSlotIndex, fromSlotIndex, toSlotIndex);
        SwapActiveIndex(ref _lastSelectedSlotIndex, fromSlotIndex, toSlotIndex);

        EnsureActiveSlotsValid();
        PublishSnapshot();

        return true;
    }

    private bool CanReloadSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return false;

        AmmoSlotState slot = _slots[slotIndex];

        if (slot.bullet == null)
            return false;

        int magazineSize = GetMagazineSize(slot.bullet);

        if (slot.currentAmmo >= magazineSize)
            return false;

        return slot.infiniteReserve || slot.reserveAmmo > 0;
    }

    private bool ReloadSlot(int slotIndex)
    {
        if (!CanReloadSlot(slotIndex))
            return false;

        AmmoSlotState slot = _slots[slotIndex];
        int magazineSize = GetMagazineSize(slot.bullet);
        int needed = Mathf.Max(0, magazineSize - slot.currentAmmo);

        if (needed <= 0)
            return false;

        if (slot.infiniteReserve)
        {
            slot.currentAmmo = magazineSize;
            return true;
        }

        int loaded = Mathf.Min(needed, slot.reserveAmmo);

        if (loaded <= 0)
            return false;

        slot.currentAmmo += loaded;
        slot.reserveAmmo -= loaded;

        return true;
    }

    private BulletSO GetActiveBullet(BulletAmmoType ammoType)
    {
        if (!IsRuntimeReady())
            return null;

        int slotIndex = GetActiveSlotIndex(ammoType);

        if (!IsValidSlot(slotIndex))
            return null;

        AmmoSlotState slot = _slots[slotIndex];

        if (slot.bullet == null || slot.bullet.AmmoType != ammoType)
            return null;

        return slot.bullet;
    }

    private void BuildRuntimeSlots()
    {
        _slots = new AmmoSlotState[SlotCount];

        for (int i = 0; i < SlotCount; i++)
            _slots[i] = new AmmoSlotState();

        PlaceRequiredDefault(DefaultAttackSlot, _defaultAttackBullet);
        PlaceRequiredDefault(DefaultPaintSlot, _defaultPaintBullet);

        _activeAttackSlotIndex = DefaultAttackSlot;
        _activePaintSlotIndex = DefaultPaintSlot;
        _activeSpecialSlotIndex = FindFirstSlotOfType(BulletAmmoType.Special);
        _lastSelectedSlotIndex = DefaultAttackSlot;
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
        int magazineSize = GetMagazineSize(bullet);

        slot.bullet = bullet;
        slot.currentAmmo = magazineSize;
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
        if (!IsRuntimeReady())
        {
            RaisePurchaseResult(request, false, -1, "Ammo runtime is not ready.");
            return;
        }

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
        int magazineSize = GetMagazineSize(request.bullet);

        AmmoSlotState slot = _slots[slotIndex];

        slot.bullet = request.bullet;
        slot.currentAmmo = Mathf.Min(magazineSize, bundleAmount);
        slot.reserveAmmo = Mathf.Max(0, bundleAmount - slot.currentAmmo);
        slot.infiniteReserve = false;
        slot.requiredDefault = false;
        slot.sellPricePerAmmo = Mathf.Max(
            1,
            Mathf.FloorToInt(request.totalPrice * request.sellPriceRate / bundleAmount));

        SetActiveSlot(request.bullet.AmmoType, slotIndex);
        _lastSelectedSlotIndex = slotIndex;

        PublishSnapshot();

        RaisePurchaseResult(
            request,
            true,
            slotIndex,
            $"{request.bullet.DisplayName} equipped to slot {slotIndex + 1}.");
    }

    private void HandleSellConfirmRequested(WeaponAmmoSellConfirmRequest request)
    {
        if (!IsRuntimeReady())
        {
            RaiseSellResult(false, request.slotIndex, 0, 0, "Ammo runtime is not ready.");
            return;
        }

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

        int totalAmmo = slot.TotalAmmo;

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

        EnsureActiveSlotsValid();
        PublishSnapshot();

        RaiseSellResult(
            true,
            request.slotIndex,
            sellAmount,
            gained,
            $"Sold ammo x{sellAmount}.");
    }

    private void ClampAllMagazineAmmo()
    {
        if (_slots == null)
            return;

        for (int i = 0; i < _slots.Length; i++)
        {
            AmmoSlotState slot = _slots[i];

            if (slot == null || slot.bullet == null)
                continue;

            slot.currentAmmo = Mathf.Clamp(slot.currentAmmo, 0, GetMagazineSize(slot.bullet));
        }
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

    private int GetActiveSlotIndex(BulletAmmoType ammoType)
    {
        return ammoType switch
        {
            BulletAmmoType.Attack => _activeAttackSlotIndex,
            BulletAmmoType.Paint => _activePaintSlotIndex,
            BulletAmmoType.Special => _activeSpecialSlotIndex,
            _ => -1
        };
    }

    private void SetActiveSlot(BulletAmmoType ammoType, int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return;

        if (_slots[slotIndex].bullet == null ||
            _slots[slotIndex].bullet.AmmoType != ammoType)
            return;

        switch (ammoType)
        {
            case BulletAmmoType.Attack:
                _activeAttackSlotIndex = slotIndex;
                break;

            case BulletAmmoType.Paint:
                _activePaintSlotIndex = slotIndex;
                break;

            case BulletAmmoType.Special:
                _activeSpecialSlotIndex = slotIndex;
                break;
        }
    }

    private void EnsureActiveSlotsValid()
    {
        if (!IsActiveSlotValid(BulletAmmoType.Attack, _activeAttackSlotIndex))
            _activeAttackSlotIndex = FindFirstSlotOfType(BulletAmmoType.Attack);

        if (!IsActiveSlotValid(BulletAmmoType.Paint, _activePaintSlotIndex))
            _activePaintSlotIndex = FindFirstSlotOfType(BulletAmmoType.Paint);

        if (!IsActiveSlotValid(BulletAmmoType.Special, _activeSpecialSlotIndex))
            _activeSpecialSlotIndex = FindFirstSlotOfType(BulletAmmoType.Special);

        if (!IsValidSlot(_lastSelectedSlotIndex) || _slots[_lastSelectedSlotIndex].bullet == null)
            _lastSelectedSlotIndex = _activeAttackSlotIndex;
    }

    private bool IsActiveSlotValid(BulletAmmoType ammoType, int slotIndex)
    {
        return IsValidSlot(slotIndex) &&
               _slots[slotIndex].bullet != null &&
               _slots[slotIndex].bullet.AmmoType == ammoType;
    }

    private int FindFirstSlotOfType(BulletAmmoType ammoType)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (_slots[i].bullet != null && _slots[i].bullet.AmmoType == ammoType)
                return i;
        }

        return -1;
    }

    private bool IsSlotActive(int slotIndex)
    {
        return slotIndex == _activeAttackSlotIndex ||
               slotIndex == _activePaintSlotIndex ||
               slotIndex == _activeSpecialSlotIndex;
    }

    private static void SwapActiveIndex(ref int activeIndex, int fromSlotIndex, int toSlotIndex)
    {
        if (activeIndex == fromSlotIndex)
            activeIndex = toSlotIndex;
        else if (activeIndex == toSlotIndex)
            activeIndex = fromSlotIndex;
    }

    private void PublishSnapshot()
    {
        if (_weaponAmmoLoadoutEventChannel == null || _slots == null)
            return;

        ClampAllMagazineAmmo();

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
                IsSlotActive(i),
                slot.requiredDefault,
                slot.sellPricePerAmmo);
        }

        _weaponAmmoLoadoutEventChannel.RaiseEvent(
            new WeaponAmmoLoadoutSnapshot(snapshots, _lastSelectedSlotIndex));
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

    private bool IsRuntimeReady()
    {
        return _slots != null && _statsRuntime != null;
    }

    private bool IsValidSlot(int slotIndex)
    {
        return _slots != null && slotIndex >= 0 && slotIndex < SlotCount;
    }

    public bool CanReloadActiveAmmo(BulletAmmoType ammoType)
    {
        if (!IsRuntimeReady())
            return false;

        return CanReloadSlot(GetActiveSlotIndex(ammoType));
    }

    public bool ReloadActiveAmmo(BulletAmmoType ammoType)
    {
        if (!IsRuntimeReady())
            return false;

        bool reloaded = ReloadSlot(GetActiveSlotIndex(ammoType));

        if (reloaded)
            PublishSnapshot();

        return reloaded;
    }

    public bool TryGetActiveSpecialBullet(out SpecialBulletSO bullet)
    {
        bullet = GetActiveBullet(BulletAmmoType.Special) as SpecialBulletSO;
        return bullet != null;
    }

    public bool TryConsumeSpecialAmmo(int amount, out SpecialBulletSO bullet)
    {
        bullet = null;

        if (!TryConsumeAmmo(BulletAmmoType.Special, amount, out BulletSO consumedBullet))
            return false;

        bullet = consumedBullet as SpecialBulletSO;
        return bullet != null;
    }
}