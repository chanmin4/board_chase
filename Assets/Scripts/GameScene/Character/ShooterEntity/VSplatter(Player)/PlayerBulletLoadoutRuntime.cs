using UnityEngine;
using UnityEngine.Serialization;

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
    private const int SlotCount = 3;
    private const int DefaultPrimarySlot = 0;
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
    [FormerlySerializedAs("_defaultAttackBullet")]
    [FormerlySerializedAs("_defaultPrimaryBullet")]
    [SerializeField] private AttackAndPaintBulletSO _defaultAttackAndPaintBullet;

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
    [FormerlySerializedAs("_activeAttackSlotIndex")]
    [SerializeField] private int _activePrimarySlotIndex = DefaultPrimarySlot;
    [SerializeField] private int _activePaintSlotIndex = -1;
    [SerializeField] private int _activeSpecialSlotIndex = -1;
    [SerializeField] private int _lastSelectedSlotIndex = DefaultPrimarySlot;

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

    public bool TryGetSelectedBullet(out BulletSO bullet)
    {
        bullet = null;

        if (!IsRuntimeReady() || !IsValidSlot(_lastSelectedSlotIndex))
            return false;

        bullet = _slots[_lastSelectedSlotIndex].bullet;
        return bullet != null;
    }

    public bool TryGetSelectedAmmoType(out BulletAmmoType ammoType)
    {
        ammoType = default;

        if (!TryGetSelectedBullet(out BulletSO bullet))
            return false;

        ammoType = bullet.AmmoType;
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
               IsCompatibleAmmoType(slot.bullet.AmmoType, ammoType) &&
               slot.currentAmmo > 0;
    }

    public bool TryGetActiveBullet(BulletAmmoType ammoType, out BulletSO bullet)
    {
        bullet = GetActiveBullet(ammoType);
        return bullet != null;
    }

    public bool TryGetActivePrimaryBullet(out BulletSO bullet)
    {
        bullet = GetActiveBullet(BulletAmmoType.AttackAndPaint);
        return bullet != null && bullet.IsPrimary;
    }

    public bool TryGetActivePaintBullet(out PaintBulletSO bullet)
    {
        bullet = GetActiveBullet(BulletAmmoType.Paint) as PaintBulletSO;
        return bullet != null;
    }

    public bool TryConsumePrimaryAmmo(int amount, out BulletSO bullet)
    {
        bullet = null;

        if (!TryConsumeAmmo(BulletAmmoType.AttackAndPaint, amount, out BulletSO consumedBullet))
            return false;

        bullet = consumedBullet;
        return bullet != null && bullet.IsPrimary;
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

        if (slot.bullet == null ||
            !IsCompatibleAmmoType(slot.bullet.AmmoType, ammoType))
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

        SwapActiveIndex(ref _activePrimarySlotIndex, fromSlotIndex, toSlotIndex);
        SwapActiveIndex(ref _activePaintSlotIndex, fromSlotIndex, toSlotIndex);
        SwapActiveIndex(ref _activeSpecialSlotIndex, fromSlotIndex, toSlotIndex);
        SwapActiveIndex(ref _lastSelectedSlotIndex, fromSlotIndex, toSlotIndex);

        EnsureActiveSlotsValid();
        PublishSnapshot();

        return true;
    }

    public bool TryGetSlotSnapshot(int slotIndex, out WeaponAmmoSlotSnapshot snapshot)
    {
        snapshot = default;

        if (!IsRuntimeReady() || !IsValidSlot(slotIndex))
            return false;

        AmmoSlotState slot = _slots[slotIndex];

        snapshot = new WeaponAmmoSlotSnapshot(
            slotIndex,
            slot.bullet,
            slot.currentAmmo,
            slot.reserveAmmo,
            slot.infiniteReserve,
            IsSlotActive(slotIndex),
            slot.requiredDefault,
            slot.sellPricePerAmmo);

        return true;
    }

    public bool TryClearRemovableSlot(int slotIndex, out string message)
    {
        message = string.Empty;

        if (!IsRuntimeReady())
        {
            message = "Ammo runtime is not ready.";
            return false;
        }

        if (!IsValidSlot(slotIndex))
        {
            message = "Invalid ammo slot.";
            return false;
        }

        AmmoSlotState slot = _slots[slotIndex];

        if (slot.bullet == null)
        {
            message = "Ammo slot is empty.";
            return false;
        }

        if (slot.requiredDefault || slot.infiniteReserve)
        {
            message = "Default ammo slot cannot be removed.";
            return false;
        }

        slot.Clear();
        EnsureActiveSlotsValid();
        PublishSnapshot();

        message = "Ammo slot cleared.";
        return true;
    }

    public bool TryGrantBullet(
        BulletSO bullet,
        int bundleAmount,
        float sellPriceRate,
        out int slotIndex,
        out string message,
        int totalPrice = 0)
    {
        slotIndex = -1;
        message = string.Empty;

        if (!IsRuntimeReady())
        {
            message = "Ammo runtime is not ready.";
            return false;
        }

        if (bullet == null)
        {
            message = "Bullet is missing.";
            return false;
        }

        if (!TryFindEmptyCompatibleSlot(bullet, out slotIndex))
        {
            message = "Please empty a compatible ammo slot first.";
            return false;
        }

        int resolvedBundleAmount = Mathf.Max(1, bundleAmount);
        EquipBulletToSlot(
            slotIndex,
            bullet,
            resolvedBundleAmount,
            sellPriceRate,
            totalPrice,
            false,
            false);

        SetActiveSlot(bullet.AmmoType, slotIndex);
        _lastSelectedSlotIndex = slotIndex;

        PublishSnapshot();

        message = $"{bullet.DisplayName} equipped to slot {slotIndex + 1}.";
        return true;
    }

    public bool TryAcquireTreasureBullet(
        BulletSO bullet,
        int bundleAmount,
        float sellPriceRate,
        out int slotIndex,
        out string message)
    {
        slotIndex = -1;
        message = string.Empty;

        if (!IsRuntimeReady())
        {
            message = "Ammo runtime is not ready.";
            return false;
        }

        if (bullet == null)
        {
            message = "Bullet is missing.";
            return false;
        }

        int resolvedBundleAmount = Mathf.Max(1, bundleAmount);

        if (TryFindNonInfiniteSameBulletSlot(bullet, out slotIndex))
        {
            AddAmmoToSlot(slotIndex, resolvedBundleAmount);
            PublishSnapshot();

            message = $"{bullet.DisplayName} ammo added to slot {slotIndex + 1}.";
            return true;
        }

        if (!TryFindEmptyTreasureCompatibleSlot(bullet, out slotIndex) &&
            !TryFindTreasureReplaceSlot(bullet, out slotIndex))
        {
            message = "No compatible replaceable ammo slot.";
            return false;
        }

        EquipBulletToSlot(
            slotIndex,
            bullet,
            resolvedBundleAmount,
            sellPriceRate,
            0,
            false,
            false);

        SetActiveSlot(bullet.AmmoType, slotIndex);
        _lastSelectedSlotIndex = slotIndex;
        PublishSnapshot();

        message = $"{bullet.DisplayName} acquired to slot {slotIndex + 1}.";
        return true;
    }

    public bool TryAcquireBulletToAvailableSlot(
        BulletSO bullet,
        int bundleAmount,
        float sellPriceRate,
        out int slotIndex,
        out string message)
    {
        slotIndex = -1;
        message = string.Empty;

        if (!IsRuntimeReady())
        {
            message = "Ammo runtime is not ready.";
            return false;
        }

        if (bullet == null)
        {
            message = "Bullet is missing.";
            return false;
        }

        int resolvedBundleAmount = Mathf.Max(1, bundleAmount);

        if (TryFindNonInfiniteSameBulletSlot(bullet, out slotIndex))
        {
            AddAmmoToSlot(slotIndex, resolvedBundleAmount);
            PublishSnapshot();

            message = $"{bullet.DisplayName} ammo added to slot {slotIndex + 1}.";
            return true;
        }

        if (!TryFindEmptyTreasureCompatibleSlot(bullet, out slotIndex))
        {
            message = "No empty compatible ammo slot.";
            return false;
        }

        EquipBulletToSlot(
            slotIndex,
            bullet,
            resolvedBundleAmount,
            sellPriceRate,
            0,
            false,
            false);

        SetActiveSlot(bullet.AmmoType, slotIndex);
        _lastSelectedSlotIndex = slotIndex;
        PublishSnapshot();

        message = $"{bullet.DisplayName} equipped to slot {slotIndex + 1}.";
        return true;
    }

    public bool TryEquipBulletToSlot(
        BulletSO bullet,
        int bundleAmount,
        float sellPriceRate,
        int slotIndex,
        out string message)
    {
        message = string.Empty;

        if (!IsRuntimeReady())
        {
            message = "Ammo runtime is not ready.";
            return false;
        }

        if (bullet == null)
        {
            message = "Bullet is missing.";
            return false;
        }

        if (!IsValidSlot(slotIndex))
        {
            message = "Invalid ammo slot.";
            return false;
        }

        AmmoSlotState slot = _slots[slotIndex];

        if (slot.requiredDefault || slot.infiniteReserve)
        {
            message = "Default ammo slot cannot be replaced.";
            return false;
        }

        if (!CanEquipBulletInSlot(slotIndex, bullet))
        {
            message = "This bullet cannot be equipped to that slot.";
            return false;
        }

        EquipBulletToSlot(
            slotIndex,
            bullet,
            Mathf.Max(1, bundleAmount),
            sellPriceRate,
            0,
            false,
            false);

        SetActiveSlot(bullet.AmmoType, slotIndex);
        _lastSelectedSlotIndex = slotIndex;
        PublishSnapshot();

        message = $"{bullet.DisplayName} equipped to slot {slotIndex + 1}.";
        return true;
    }

    public bool TryEquipBulletToSlotAndReturnPrevious(
        BulletSO bullet,
        int bundleAmount,
        float sellPriceRate,
        int slotIndex,
        out BulletSO previousBullet,
        out int previousAmount,
        out string message,
        bool allowRequiredDefaultReplacement = false)
    {
        previousBullet = null;
        previousAmount = 0;
        message = string.Empty;

        if (!IsRuntimeReady())
        {
            message = "Ammo runtime is not ready.";
            return false;
        }

        if (bullet == null)
        {
            message = "Bullet is missing.";
            return false;
        }

        if (!IsValidSlot(slotIndex))
        {
            message = "Invalid ammo slot.";
            return false;
        }

        AmmoSlotState slot = _slots[slotIndex];

        if ((slot.requiredDefault || slot.infiniteReserve) &&
            !allowRequiredDefaultReplacement)
        {
            message = "Default ammo slot cannot be replaced.";
            return false;
        }

        if (!CanEquipBulletInSlot(slotIndex, bullet))
        {
            message = "This bullet cannot be equipped to that slot.";
            return false;
        }

        bool returnPrevious =
            slot.bullet != null &&
            !slot.requiredDefault &&
            !slot.infiniteReserve;

        previousBullet = returnPrevious ? slot.bullet : null;
        previousAmount = returnPrevious
            ? Mathf.Max(0, slot.currentAmmo) + Mathf.Max(0, slot.reserveAmmo)
            : 0;

        EquipBulletToSlot(
            slotIndex,
            bullet,
            Mathf.Max(1, bundleAmount),
            sellPriceRate,
            0,
            false,
            false);

        SetActiveSlot(bullet.AmmoType, slotIndex);
        _lastSelectedSlotIndex = slotIndex;
        PublishSnapshot();

        message = $"{bullet.DisplayName} equipped to slot {slotIndex + 1}.";
        return true;
    }

    private void EquipBulletToSlot(
        int slotIndex,
        BulletSO bullet,
        int bundleAmount,
        float sellPriceRate,
        int totalPrice,
        bool requiredDefault,
        bool infiniteReserve)
    {
        if (!IsValidSlot(slotIndex) || bullet == null)
            return;

        int resolvedBundleAmount = Mathf.Max(1, bundleAmount);
        int magazineSize = GetMagazineSize(bullet);

        AmmoSlotState slot = _slots[slotIndex];

        slot.bullet = bullet;
        slot.currentAmmo = Mathf.Min(magazineSize, resolvedBundleAmount);
        slot.reserveAmmo = infiniteReserve
            ? 0
            : Mathf.Max(0, resolvedBundleAmount - slot.currentAmmo);
        slot.infiniteReserve = infiniteReserve;
        slot.requiredDefault = requiredDefault;
        slot.sellPricePerAmmo = totalPrice > 0 && !infiniteReserve
            ? Mathf.Max(
                1,
                Mathf.FloorToInt(totalPrice * Mathf.Clamp01(sellPriceRate) / resolvedBundleAmount))
            : 0;
    }

    private void AddAmmoToSlot(int slotIndex, int amount)
    {
        if (!IsValidSlot(slotIndex) || amount <= 0)
            return;

        AmmoSlotState slot = _slots[slotIndex];

        if (slot.bullet == null || slot.infiniteReserve)
            return;

        int magazineSize = GetMagazineSize(slot.bullet);
        int currentAmmoSpace = Mathf.Max(0, magazineSize - slot.currentAmmo);
        int loadedAmount = Mathf.Min(currentAmmoSpace, amount);

        slot.currentAmmo += loadedAmount;
        slot.reserveAmmo += Mathf.Max(0, amount - loadedAmount);
    }

    private bool TryFindNonInfiniteSameBulletSlot(BulletSO bullet, out int slotIndex)
    {
        slotIndex = -1;

        if (bullet == null)
            return false;

        for (int i = 0; i < SlotCount; i++)
        {
            AmmoSlotState slot = _slots[i];

            if (slot.bullet != bullet || slot.infiniteReserve)
                continue;

            slotIndex = i;
            return true;
        }

        return false;
    }

    private bool TryFindEmptyTreasureCompatibleSlot(BulletSO bullet, out int slotIndex)
    {
        slotIndex = -1;

        for (int i = 0; i < SlotCount; i++)
        {
            if (i == DefaultPrimarySlot)
                continue;

            if (_slots[i].bullet != null)
                continue;

            if (!CanEquipBulletInSlot(i, bullet))
                continue;

            slotIndex = i;
            return true;
        }

        return false;
    }

    private bool TryFindTreasureReplaceSlot(BulletSO bullet, out int slotIndex)
    {
        slotIndex = -1;

        if (CanReplaceSlotWithTreasureBullet(_lastSelectedSlotIndex, bullet))
        {
            slotIndex = _lastSelectedSlotIndex;
            return true;
        }

        int activeSlotIndex = GetActiveSlotIndex(bullet.AmmoType);

        if (CanReplaceSlotWithTreasureBullet(activeSlotIndex, bullet))
        {
            slotIndex = activeSlotIndex;
            return true;
        }

        for (int i = 0; i < SlotCount; i++)
        {
            if (!CanReplaceSlotWithTreasureBullet(i, bullet))
                continue;

            slotIndex = i;
            return true;
        }

        return false;
    }

    private bool CanReplaceSlotWithTreasureBullet(int slotIndex, BulletSO bullet)
    {
        if (!IsValidSlot(slotIndex) || bullet == null)
            return false;

        if (slotIndex == DefaultPrimarySlot)
            return false;

        AmmoSlotState slot = _slots[slotIndex];

        if (slot.requiredDefault || slot.infiniteReserve)
            return false;

        return CanEquipBulletInSlot(slotIndex, bullet);
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

        if (slot.bullet == null ||
            !IsCompatibleAmmoType(slot.bullet.AmmoType, ammoType))
            return null;

        return slot.bullet;
    }

    private void BuildRuntimeSlots()
    {
        _slots = new AmmoSlotState[SlotCount];

        for (int i = 0; i < SlotCount; i++)
            _slots[i] = new AmmoSlotState();

        PlaceRequiredDefault(DefaultPrimarySlot, _defaultAttackAndPaintBullet);

        _activePrimarySlotIndex = DefaultPrimarySlot;
        _activePaintSlotIndex = FindFirstSlotOfType(BulletAmmoType.Paint);
        _activeSpecialSlotIndex = FindFirstSlotOfType(BulletAmmoType.Special);
        _lastSelectedSlotIndex = DefaultPrimarySlot;
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
        bool granted = TryGrantBullet(
            request.bullet,
            request.bundleAmount,
            request.sellPriceRate,
            out int slotIndex,
            out string message,
            request.totalPrice);

        RaisePurchaseResult(request, granted, slotIndex, message);
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

        if (slotIndex == DefaultPrimarySlot)
            return bullet.IsPrimary;

        return true;
    }

    private int GetActiveSlotIndex(BulletAmmoType ammoType)
    {
        return ammoType switch
        {
            BulletAmmoType.AttackAndPaint => _activePrimarySlotIndex,
            BulletAmmoType.Attack => _activePrimarySlotIndex,
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
            !IsCompatibleAmmoType(_slots[slotIndex].bullet.AmmoType, ammoType))
            return;

        switch (ammoType)
        {
            case BulletAmmoType.AttackAndPaint:
            case BulletAmmoType.Attack:
                _activePrimarySlotIndex = slotIndex;
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
        if (!IsActiveSlotValid(BulletAmmoType.AttackAndPaint, _activePrimarySlotIndex))
            _activePrimarySlotIndex = FindFirstSlotOfType(BulletAmmoType.AttackAndPaint);

        if (!IsActiveSlotValid(BulletAmmoType.Paint, _activePaintSlotIndex))
            _activePaintSlotIndex = FindFirstSlotOfType(BulletAmmoType.Paint);

        if (!IsActiveSlotValid(BulletAmmoType.Special, _activeSpecialSlotIndex))
            _activeSpecialSlotIndex = FindFirstSlotOfType(BulletAmmoType.Special);

        if (!IsValidSlot(_lastSelectedSlotIndex) || _slots[_lastSelectedSlotIndex].bullet == null)
            _lastSelectedSlotIndex = _activePrimarySlotIndex;
    }

    private bool IsActiveSlotValid(BulletAmmoType ammoType, int slotIndex)
    {
        return IsValidSlot(slotIndex) &&
               _slots[slotIndex].bullet != null &&
               IsCompatibleAmmoType(_slots[slotIndex].bullet.AmmoType, ammoType);
    }

    private int FindFirstSlotOfType(BulletAmmoType ammoType)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (_slots[i].bullet != null &&
                IsCompatibleAmmoType(_slots[i].bullet.AmmoType, ammoType))
                return i;
        }

        return -1;
    }

    private bool IsSlotActive(int slotIndex)
    {
        return slotIndex == _activePrimarySlotIndex ||
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

    private static bool IsCompatibleAmmoType(
        BulletAmmoType bulletType,
        BulletAmmoType requestedType)
    {
        if (bulletType == requestedType)
            return true;

        bool bulletIsPrimary =
            bulletType == BulletAmmoType.AttackAndPaint ||
            bulletType == BulletAmmoType.Attack;

        bool requestedIsPrimary =
            requestedType == BulletAmmoType.AttackAndPaint ||
            requestedType == BulletAmmoType.Attack;

        return bulletIsPrimary && requestedIsPrimary;
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
