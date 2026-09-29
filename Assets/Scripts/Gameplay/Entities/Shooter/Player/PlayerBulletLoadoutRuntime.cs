/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player
 * Primary responsibility: Implements the focused CompatibleAmmoDisplayEntry responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public readonly struct CompatibleAmmoDisplayEntry
{
    public CompatibleAmmoDisplayEntry(
        BulletSO bullet,
        int reserveAmount,
        int firstInventorySlotIndex,
        bool isSelected)
    {
        Bullet = bullet;
        ReserveAmount = Mathf.Max(0, reserveAmount);
        FirstInventorySlotIndex = firstInventorySlotIndex;
        IsSelected = isSelected;
    }

    public BulletSO Bullet { get; }
    public int ReserveAmount { get; }
    public int FirstInventorySlotIndex { get; }
    public bool IsSelected { get; }
}

/// <summary>
/// Player bullet loadout runtime.
/// Keeps one loaded magazine per weapon slot while all reserve ammunition stays
/// in PlayerInventoryRuntime. The active ranged weapon automatically selects a
/// compatible inventory bullet, and the player can cycle compatible bullets.
/// 
/// Runtime refs are received by event channels.
/// No GetComponent lookup for PlayerStatsRuntime.
/// </summary>
[DisallowMultipleComponent]
public class PlayerBulletLoadoutRuntime : MonoBehaviour
{
    public event Action<BulletSO> SelectedBulletChanged;
    public event Action<BulletSO> AmmoSelectionReloadRequested;
    private const int SlotCount = 3;
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

        // Purpose: Clears the current clear state.
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

    private sealed class WeaponMagazineState
    {
        public BulletSO bullet;
        public int currentAmmo;

        // Purpose: Clears the current clear state.
        public void Clear()
        {
            bullet = null;
            currentAmmo = 0;
        }
    }

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
    [SerializeField] private PlayerInventoryRuntime _inventoryRuntime;
    [SerializeField] private EntityEquipmentRuntime _equipmentRuntime;
    [SerializeField] private int _activePrimarySlotIndex = -1;
    [SerializeField] private int _activeSpecialSlotIndex = -1;
    [SerializeField] private int _lastSelectedSlotIndex = -1;

    private AmmoSlotState[] _slots;
    private bool _warnedMissingStatsRuntime;
    private bool _startingLoadoutAmmoApplied;
    private BulletSO _lastNotifiedSelectedBullet;
    private WeaponMagazineState[] _weaponMagazines;
    private PlayerInventoryRuntime _subscribedInventoryRuntime;
    private EntityEquipmentRuntime _subscribedEquipmentRuntime;
    private bool _isMutatingInventory;

    [Header("Debug - Ammo Transaction Trace")]
    [Tooltip("Temporary diagnosis trace. Logs ammo returns, reloads, legacy-slot mutations, and world-drop fallbacks.")]
    [SerializeField] private bool _debugAmmoTransactionTrace = true;
    [SerializeField] private bool _debugAmmoTransactionTraceCallers = true;
    private int _ammoTraceSequence;

    public bool IsAmmoTransactionTraceEnabled => _debugAmmoTransactionTrace;

    // Purpose: Returns the current magazine size.
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

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveInventoryRefs();
        BindInventoryEvents();

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

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        UnbindInventoryEvents();

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

    // Purpose: Handles the incoming stats runtime ready request or event.
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

        ResolveInventoryRefs();
        BindInventoryEvents();
        EnsureWeaponMagazineStates();
        ApplyStartingLoadoutAmmoIfNeeded();
        EnsureCompatibleSelectionForActiveWeapon();
        PublishSnapshot();

        if (_loadoutRuntimeReadyChannel != null)
            _loadoutRuntimeReadyChannel.RaiseEvent(this);
    }

    // Purpose: Applies the requested starting loadout ammo if needed change.
    private void ApplyStartingLoadoutAmmoIfNeeded()
    {
        if (_startingLoadoutAmmoApplied)
            return;

        if (!IsRuntimeReady())
            return;

        ResolveInventoryRefs();

        if (_inventoryRuntime == null)
            return;

        ShooterLoadoutSnapshot snapshot = default;
        bool hasSnapshot =
            _equipmentRuntime != null &&
            _equipmentRuntime.TryGetLastStartingLoadoutSnapshot(out snapshot);

        if (!hasSnapshot &&
            _statsRuntime != null &&
            _statsRuntime.StatConfig is PlayerStatsConfigSO playerConfig)
        {
            hasSnapshot = playerConfig.TryRollPlayerLoadout(out snapshot);
        }

        if (!hasSnapshot || snapshot.ammoStacks == null)
        {
            _startingLoadoutAmmoApplied = true;
            return;
        }

        bool changed = false;
        _isMutatingInventory = true;

        try
        {
            for (int i = 0; i < snapshot.ammoStacks.Length; i++)
            {
                ShooterLoadoutAmmoStack stack = snapshot.ammoStacks[i];

                if (!stack.HasBullet)
                    continue;

                if (!_inventoryRuntime.TryPickup(
                        stack.bullet,
                        Mathf.Max(1, stack.amount),
                        out string message))
                {
                    Debug.LogError(
                        $"[PlayerBulletLoadoutRuntime] Failed to store starting ammo. " +
                        $"bullet={stack.bullet.name}, amount={stack.amount}, reason={message}",
                        this);
                    continue;
                }

                changed = true;
            }
        }
        finally
        {
            _isMutatingInventory = false;
        }

        if (changed)
            EnsureCompatibleSelectionForActiveWeapon();

        _startingLoadoutAmmoApplied = true;
    }

    // Purpose: Selects slot for the active workflow.
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

    // Purpose: Attempts to get selected bullet and reports whether the request succeeded.
    public bool TryGetSelectedBullet(out BulletSO bullet)
    {
        bullet = null;

        if (!IsRuntimeReady())
            return false;

        EnsureWeaponMagazineStates();
        EnsureCompatibleSelectionForActiveWeapon();

        WeaponMagazineState magazine = GetActiveWeaponMagazine();
        bullet = magazine != null ? magazine.bullet : null;
        return bullet != null;
    }

    /// <summary>
    /// Provides the active weapon's virtual magazine state for presentation.
    /// This is read-only: inventory and reload transactions remain owned by
    /// the existing loadout methods.
    /// </summary>
    public bool TryGetActiveWeaponMagazineState(out BulletSO bullet, out int currentAmmo)
    {
        bullet = null;
        currentAmmo = 0;

        if (!IsRuntimeReady())
            return false;

        EnsureWeaponMagazineStates();
        EnsureCompatibleSelectionForActiveWeapon();

        WeaponMagazineState magazine = GetActiveWeaponMagazine();

        if (magazine == null || magazine.bullet == null)
            return false;

        bullet = magazine.bullet;
        currentAmmo = Mathf.Max(0, magazine.currentAmmo);
        return true;
    }

    // Keep automatic reload ahead of gun bashing while reserve rounds are available.
    public bool HasNoFireableAmmunition
    {
        get
        {
            if (!IsRuntimeReady()) return false;
            TryGetActiveWeaponMagazineState(out BulletSO bullet, out int loaded);
            return loaded <= 0 && (bullet == null || !CanReloadActiveAmmo(bullet));
        }
    }

    // Purpose: Attempts to get selected ammo type and reports whether the request succeeded.
    public bool TryGetSelectedAmmoType(out BulletAmmoType ammoType)
    {
        ammoType = default;

        if (!TryGetSelectedBullet(out BulletSO bullet))
            return false;

        ammoType = bullet.AmmoType;
        return true;
    }

    // Purpose: Checks whether this component has loaded ammo.
    public bool HasLoadedAmmo(BulletAmmoType ammoType)
    {
        if (!IsRuntimeReady())
            return false;

        WeaponMagazineState magazine = GetActiveWeaponMagazine();
        return magazine != null &&
               magazine.bullet != null &&
               IsCompatibleAmmoType(magazine.bullet.AmmoType, ammoType) &&
               magazine.currentAmmo > 0;
    }

    // Purpose: Attempts to get active bullet and reports whether the request succeeded.
    public bool TryGetActiveBullet(BulletAmmoType ammoType, out BulletSO bullet)
    {
        bullet = GetActiveBullet(ammoType);
        return bullet != null;
    }

    // Purpose: Attempts to get active primary bullet and reports whether the request succeeded.
    public bool TryGetActivePrimaryBullet(out BulletSO bullet)
    {
        bullet = GetActiveBullet(BulletAmmoType.Basic);
        return bullet != null && bullet.IsPrimary;
    }

    // Purpose: Attempts to consume primary ammo and reports whether the request succeeded.
    public bool TryConsumePrimaryAmmo(int amount, out BulletSO bullet)
    {
        bullet = null;

        if (!TryConsumeAmmo(BulletAmmoType.Basic, amount, out BulletSO consumedBullet))
            return false;

        bullet = consumedBullet;
        return bullet != null && bullet.IsPrimary;
    }

    // Purpose: Attempts to consume ammo and reports whether the request succeeded.
    private bool TryConsumeAmmo(BulletAmmoType ammoType, int amount, out BulletSO bullet)
    {
        bullet = null;

        if (!IsRuntimeReady() || amount <= 0)
            return false;

        WeaponMagazineState magazine = GetActiveWeaponMagazine();

        if (magazine == null ||
            magazine.bullet == null ||
            !IsCompatibleAmmoType(magazine.bullet.AmmoType, ammoType))
            return false;

        if (magazine.currentAmmo < amount)
            return false;

        // Ammo Scavenger doubles only shots fired while the player remains in
        // that room. A final single round is still allowed to fire rather than
        // becoming unusable merely because the doubled cost cannot be paid.
        int requestedConsumption = amount *
            SectorRoomTraitRuntime.CurrentPlayerAmmoConsumptionMultiplier;
        int actualConsumption = Mathf.Min(magazine.currentAmmo, requestedConsumption);

        magazine.currentAmmo -= actualConsumption;
        bullet = magazine.bullet;

        PublishSnapshot();

        return bullet != null;
    }

    // Purpose: Attempts to swap slots and reports whether the request succeeded.
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
        SwapActiveIndex(ref _activeSpecialSlotIndex, fromSlotIndex, toSlotIndex);
        SwapActiveIndex(ref _lastSelectedSlotIndex, fromSlotIndex, toSlotIndex);

        EnsureActiveSlotsValid();
        PublishSnapshot();

        return true;
    }

    // Purpose: Attempts to get slot snapshot and reports whether the request succeeded.
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
            IsSlotSelected(slotIndex),
            slot.requiredDefault,
            slot.sellPricePerAmmo);

        return true;
    }

    // Purpose: Attempts to add bullet to matching slot and reports whether the request succeeded.
    public bool TryAddBulletToMatchingSlot(
        BulletSO bullet,
        int amount,
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

        if (!TryFindNonInfiniteSameBulletSlot(bullet, out slotIndex))
        {
            message = "No matching equipped ammo stack.";
            return false;
        }

        AddAmmoToSlot(slotIndex, Mathf.Max(1, amount));
        PublishSnapshot();
        message = $"{bullet.DisplayName} ammo added to slot {slotIndex + 1}.";
        TraceAmmoTransaction(
            "LegacySlot",
            "TryAddBulletToMatchingSlot completed",
            bullet,
            null,
            $"slot={slotIndex}, amount={Mathf.Max(1, amount)}, destination=legacy-slot");
        return true;
    }

    // Purpose: Attempts to add bullet to slot and reports whether the request succeeded.
    public bool TryAddBulletToSlot(
        int slotIndex,
        BulletSO bullet,
        int amount,
        out string message)
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

        if (bullet == null || slot.bullet != bullet)
        {
            message = "Ammo type does not match the equipped stack.";
            return false;
        }

        if (slot.requiredDefault || slot.infiniteReserve)
        {
            message = "Default ammo slot cannot receive a finite stack.";
            return false;
        }

        AddAmmoToSlot(slotIndex, Mathf.Max(1, amount));
        PublishSnapshot();
        message = $"{bullet.DisplayName} ammo stacked in slot {slotIndex + 1}.";
        TraceAmmoTransaction(
            "LegacySlot",
            "TryAddBulletToSlot completed",
            bullet,
            null,
            $"slot={slotIndex}, amount={Mathf.Max(1, amount)}, destination=legacy-slot");
        return true;
    }

    // Purpose: Attempts to remove ammo from slot and reports whether the request succeeded.
    public bool TryRemoveAmmoFromSlot(
        int slotIndex,
        int amount,
        out BulletSO removedBullet,
        out int removedAmount,
        out string message)
    {
        removedBullet = null;
        removedAmount = 0;
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

        if (slot.bullet == null || slot.TotalAmmo <= 0)
        {
            message = "Ammo slot is empty.";
            return false;
        }

        if (slot.requiredDefault || slot.infiniteReserve)
        {
            message = "Default ammo slot cannot be split.";
            return false;
        }

        int resolvedAmount = Mathf.Clamp(amount, 1, slot.TotalAmmo);
        removedBullet = slot.bullet;
        removedAmount = resolvedAmount;

        // Reserve is detached first so the loaded magazine remains ready when possible.
        int reserveRemoved = Mathf.Min(slot.reserveAmmo, resolvedAmount);
        slot.reserveAmmo -= reserveRemoved;

        int remaining = resolvedAmount - reserveRemoved;
        if (remaining > 0)
            slot.currentAmmo = Mathf.Max(0, slot.currentAmmo - remaining);

        if (slot.TotalAmmo <= 0)
            slot.Clear();

        EnsureActiveSlotsValid();
        PublishSnapshot();
        message = $"{removedBullet.DisplayName} ammo removed from slot {slotIndex + 1}.";
        TraceAmmoTransaction(
            "LegacySlot",
            "TryRemoveAmmoFromSlot completed",
            removedBullet,
            null,
            $"slot={slotIndex}, removed={removedAmount}, destination=caller-owned");
        return true;
    }

    // Purpose: Attempts to clear removable slot and reports whether the request succeeded.
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

        TraceAmmoTransaction(
            "LegacySlot",
            "TryClearRemovableSlot about to clear",
            slot.bullet,
            null,
            $"slot={slotIndex}, deletedCurrent={slot.currentAmmo}, deletedReserve={slot.reserveAmmo}, destination=NONE");
        slot.Clear();
        EnsureActiveSlotsValid();
        PublishSnapshot();

        message = "Ammo slot cleared.";
        return true;
    }

    /// <summary>
    /// Removes all runtime ammunition and loaded magazines after player death.
    /// Starting ammo is deliberately not re-granted to this run instance.
    /// </summary>
    public void ClearForRunLoss()
    {
        if (_slots == null)
            BuildRuntimeSlots();

        for (int i = 0; i < _slots.Length; i++)
            _slots[i].Clear();

        EnsureWeaponMagazineStates();

        for (int i = 0; i < _weaponMagazines.Length; i++)
            _weaponMagazines[i].Clear();

        _activePrimarySlotIndex = -1;
        _activeSpecialSlotIndex = -1;
        _lastSelectedSlotIndex = -1;
        _startingLoadoutAmmoApplied = true;

        PublishSnapshot();
    }

    /// <summary>
    /// Captures virtual magazines independently from the inventory reserve.
    /// A magazine retains its round type at zero rounds, so an empty current
    /// magazine is deliberately saved as a meaningful selection state.
    /// </summary>
    public void CaptureRunState(PlayerRunStateSaveData target)
    {
        if (target == null)
            return;

        EnsureWeaponMagazineStates();
        target.weaponMagazines.Clear();

        if (_weaponMagazines == null)
            return;

        for (int slotIndex = 0; slotIndex < _weaponMagazines.Length; slotIndex++)
        {
            WeaponMagazineState magazine = _weaponMagazines[slotIndex];
            if (magazine == null || magazine.bullet == null || string.IsNullOrWhiteSpace(magazine.bullet.Guid))
                continue;

            target.weaponMagazines.Add(new PlayerRunMagazineSaveData
            {
                weaponSlotIndex = slotIndex,
                bulletGuid = magazine.bullet.Guid,
                currentAmmo = magazine.currentAmmo
            });
        }
    }

    /// <summary>
    /// Restores virtual magazines only after item inventory/equipment has been
    /// restored. No inventory reserve is consumed here because the saved
    /// magazine rounds already belong to the weapon's virtual magazine.
    /// </summary>
    public bool TryRestoreRunState(
        PlayerRunStateSaveData source,
        RuntimeContentCatalogSO catalog,
        out string message)
    {
        message = string.Empty;

        if (source == null || !source.hasSnapshot)
        {
            message = "No resumable player run exists.";
            return false;
        }

        if (catalog == null)
        {
            message = "Runtime content catalog is missing.";
            return false;
        }

        EnsureWeaponMagazineStates();

        for (int index = 0; index < source.weaponMagazines.Count; index++)
        {
            PlayerRunMagazineSaveData entry = source.weaponMagazines[index];
            if (entry == null || entry.weaponSlotIndex < 0 ||
                entry.weaponSlotIndex >= _weaponMagazines.Length ||
                !catalog.TryResolveItem(entry.bulletGuid, out ItemSO item) ||
                item is not BulletSO)
            {
                message = $"Saved magazine cannot be resolved. slot={entry?.weaponSlotIndex}, bullet={entry?.bulletGuid}";
                return false;
            }
        }

        for (int slotIndex = 0; slotIndex < _weaponMagazines.Length; slotIndex++)
            _weaponMagazines[slotIndex].Clear();

        for (int index = 0; index < source.weaponMagazines.Count; index++)
        {
            PlayerRunMagazineSaveData entry = source.weaponMagazines[index];
            catalog.TryResolveItem(entry.bulletGuid, out ItemSO item);
            BulletSO bullet = item as BulletSO;
            WeaponMagazineState magazine = _weaponMagazines[entry.weaponSlotIndex];
            magazine.bullet = bullet;
            magazine.currentAmmo = Mathf.Clamp(entry.currentAmmo, 0, GetMagazineSize(bullet));
        }

        EnsureCompatibleSelectionForActiveWeapon();
        PublishSnapshot();
        message = $"Restored {source.weaponMagazines.Count} virtual magazine(s).";
        return true;
    }

    // Purpose: Attempts to grant bullet and reports whether the request succeeded.
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

    // Purpose: Attempts to acquire treasure bullet and reports whether the request succeeded.
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

    // Purpose: Attempts to acquire bullet to available slot and reports whether the request succeeded.
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

    // Purpose: Attempts to equip bullet to slot and reports whether the request succeeded.
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

    // Purpose: Attempts to equip bullet to slot and return previous and reports whether the request succeeded.
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

    // Purpose: Equips bullet to slot for the current owner.
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

    // Purpose: Adds the requested ammo to slot to this component.
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

    // Purpose: Attempts to find non infinite same bullet slot and reports whether the request succeeded.
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

    // Purpose: Attempts to find empty treasure compatible slot and reports whether the request succeeded.
    private bool TryFindEmptyTreasureCompatibleSlot(BulletSO bullet, out int slotIndex)
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

    // Purpose: Attempts to find treasure replace slot and reports whether the request succeeded.
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

    // Purpose: Checks whether replace slot with treasure bullet is currently permitted.
    private bool CanReplaceSlotWithTreasureBullet(int slotIndex, BulletSO bullet)
    {
        if (!IsValidSlot(slotIndex) || bullet == null)
            return false;

        AmmoSlotState slot = _slots[slotIndex];

        if (slot.requiredDefault || slot.infiniteReserve)
            return false;

        return CanEquipBulletInSlot(slotIndex, bullet);
    }

    // Purpose: Checks whether reload slot is currently permitted.
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

    // Purpose: Performs the reload slot workflow for this component.
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

    // Purpose: Returns the current active bullet.
    private BulletSO GetActiveBullet(BulletAmmoType ammoType)
    {
        if (!IsRuntimeReady())
            return null;

        EnsureWeaponMagazineStates();
        EnsureCompatibleSelectionForActiveWeapon();

        WeaponMagazineState magazine = GetActiveWeaponMagazine();

        if (magazine == null ||
            magazine.bullet == null ||
            !IsCompatibleAmmoType(magazine.bullet.AmmoType, ammoType))
            return null;

        return magazine.bullet;
    }

    // Purpose: Performs the cycle compatible inventory ammo workflow for this component.
    public bool CycleCompatibleInventoryAmmo(out string message)
    {
        message = string.Empty;
        ResolveInventoryRefs();
        EnsureWeaponMagazineStates();

        if (_inventoryRuntime == null ||
            _equipmentRuntime == null ||
            _equipmentRuntime.CurrentWeapon is not RangedWeaponSO rangedWeapon)
        {
            message = "Equip a ranged weapon before changing ammunition.";
            return false;
        }

        WeaponMagazineState magazine = GetActiveWeaponMagazine();
        if (magazine == null)
        {
            message = "Active weapon magazine is unavailable.";
            return false;
        }

        List<BulletSO> candidates = new();
        CollectCompatibleInventoryBullets(rangedWeapon, candidates);

        if (candidates.Count == 0)
        {
            message = $"No compatible ammunition for {rangedWeapon.DisplayName}.";
            return false;
        }

        int currentIndex = magazine.bullet != null
            ? candidates.IndexOf(magazine.bullet)
            : -1;
        int nextIndex = currentIndex >= 0
            ? (currentIndex + 1) % candidates.Count
            : 0;
        BulletSO nextBullet = candidates[nextIndex];

        if (nextBullet == magazine.bullet)
        {
            if (CanReloadActiveAmmo(nextBullet))
            {
                AmmoSelectionReloadRequested?.Invoke(nextBullet);
                message = $"{nextBullet.DisplayName} selected. Reloading.";
                return true;
            }

            message = $"{nextBullet.DisplayName} is the only compatible ammunition.";
            return true;
        }

        if (!TrySwitchLoadedAmmo(magazine, nextBullet, out message))
            return false;

        PublishSnapshot();
        AmmoSelectionReloadRequested?.Invoke(nextBullet);
        message = BuildAmmoSelectionMessage(nextBullet, message);
        return true;
    }

    /// <summary>
    /// Confirms a specific compatible ammunition type chosen by the HUD.
    /// The magazine is emptied back into inventory before changing type, then
    /// the normal reload action owns filling the newly selected magazine.
    /// </summary>
    public bool TrySelectCompatibleInventoryAmmo(
        BulletSO selectedBullet,
        out string message)
    {
        message = string.Empty;
        ResolveInventoryRefs();
        EnsureWeaponMagazineStates();

        if (selectedBullet == null)
        {
            message = "The selected ammunition is missing.";
            return false;
        }

        if (_inventoryRuntime == null ||
            _equipmentRuntime == null ||
            _equipmentRuntime.CurrentWeapon is not RangedWeaponSO rangedWeapon)
        {
            message = "Equip a ranged weapon before changing ammunition.";
            return false;
        }

        if (!rangedWeapon.IsCompatibleWith(selectedBullet))
        {
            message =
                $"{selectedBullet.DisplayName} is not compatible with " +
                $"{rangedWeapon.DisplayName}.";
            return false;
        }

        WeaponMagazineState magazine = GetActiveWeaponMagazine();

        if (magazine == null)
        {
            message = "Active weapon magazine is unavailable.";
            return false;
        }

        // The magazine remembers its selected round type even when it is empty.
        // Selecting that same zero-round type is a no-op confirmation, which
        // lets the HUD use it as the player's "keep current ammo" choice.
        bool isCurrentMagazineBullet = magazine.bullet == selectedBullet;
        bool existsInInventory =
            _inventoryRuntime.GetInventoryItemAmount(selectedBullet) > 0;

        if (!isCurrentMagazineBullet && !existsInInventory)
        {
            message = $"{selectedBullet.DisplayName} is no longer in inventory.";
            return false;
        }

        if (magazine.bullet == selectedBullet)
        {
            if (CanReloadActiveAmmo(selectedBullet))
            {
                AmmoSelectionReloadRequested?.Invoke(selectedBullet);
                message = $"{selectedBullet.DisplayName} selected. Reloading.";
            }
            else
            {
                message = $"{selectedBullet.DisplayName} is already selected.";
            }

            return true;
        }

        if (!TrySwitchLoadedAmmo(magazine, selectedBullet, out message))
            return false;

        PublishSnapshot();
        AmmoSelectionReloadRequested?.Invoke(selectedBullet);
        message = BuildAmmoSelectionMessage(selectedBullet, message);
        return true;
    }

    /// <summary>
    /// Builds the active weapon's compatible ammunition list in inventory-slot order.
    /// Duplicate stacks are combined while the first stack position decides display order.
    /// If the selected magazine ammo has no remaining inventory stack, it is
    /// appended once as a zero-reserve entry so it remains selectable.
    /// </summary>
    public void GetCompatibleInventoryAmmoEntries(
        List<CompatibleAmmoDisplayEntry> results)
    {
        if (results == null)
            throw new ArgumentNullException(nameof(results));

        results.Clear();
        ResolveInventoryRefs();
        EnsureWeaponMagazineStates();

        if (_inventoryRuntime == null ||
            _equipmentRuntime == null ||
            _equipmentRuntime.CurrentWeapon is not RangedWeaponSO rangedWeapon)
        {
            return;
        }

        WeaponMagazineState magazine = GetActiveWeaponMagazine();
        BulletSO selectedBullet = magazine != null ? magazine.bullet : null;
        IReadOnlyList<PlayerInventoryItemStack> items = _inventoryRuntime.Items;

        if (items != null)
        {
            for (int inventoryIndex = 0; inventoryIndex < items.Count; inventoryIndex++)
            {
                PlayerInventoryItemStack stack = items[inventoryIndex];

                if (stack == null ||
                    stack.IsEmpty ||
                    stack.Amount <= 0 ||
                    stack.Item is not BulletSO bullet ||
                    !rangedWeapon.IsCompatibleWith(bullet))
                {
                    continue;
                }

                int existingIndex = FindDisplayEntryIndex(results, bullet);

                if (existingIndex >= 0)
                {
                    CompatibleAmmoDisplayEntry existing = results[existingIndex];
                    results[existingIndex] = new CompatibleAmmoDisplayEntry(
                        existing.Bullet,
                        existing.ReserveAmount + stack.Amount,
                        existing.FirstInventorySlotIndex,
                        existing.IsSelected);
                    continue;
                }

                results.Add(new CompatibleAmmoDisplayEntry(
                    bullet,
                    stack.Amount,
                    inventoryIndex,
                    bullet == selectedBullet));
            }
        }

        // Do not duplicate an inventory row. The selected magazine's ammo is
        // only represented separately when every reserve stack is gone.
        if (selectedBullet != null &&
            FindDisplayEntryIndex(results, selectedBullet) < 0)
        {
            results.Add(new CompatibleAmmoDisplayEntry(
                selectedBullet,
                0,
                -1,
                true));
        }
    }

    // Purpose: Finds the requested display entry index.
    private static int FindDisplayEntryIndex(
        List<CompatibleAmmoDisplayEntry> entries,
        BulletSO bullet)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Bullet == bullet)
                return i;
        }

        return -1;
    }

    // Purpose: Resolves the inventory refs needed by this component.
    private void ResolveInventoryRefs()
    {
        if (_inventoryRuntime == null)
            _inventoryRuntime = EntityComponentResolver.Resolve<PlayerInventoryRuntime>(this);

        if (_equipmentRuntime == null)
            _equipmentRuntime = EntityComponentResolver.Resolve<EntityEquipmentRuntime>(this);
    }

    // Purpose: Binds inventory events to the active runtime or UI target.
    private void BindInventoryEvents()
    {
        ResolveInventoryRefs();

        if (_subscribedInventoryRuntime != _inventoryRuntime)
        {
            if (_subscribedInventoryRuntime != null)
                _subscribedInventoryRuntime.OnChanged -= HandleInventoryChanged;

            _subscribedInventoryRuntime = _inventoryRuntime;

            if (_subscribedInventoryRuntime != null)
                _subscribedInventoryRuntime.OnChanged += HandleInventoryChanged;
        }

        if (_subscribedEquipmentRuntime != _equipmentRuntime)
        {
            if (_subscribedEquipmentRuntime != null)
            {
                _subscribedEquipmentRuntime.OnWeaponChanged -= HandleWeaponChanged;
                _subscribedEquipmentRuntime.OnEquipmentChanged -= HandleEquipmentChanged;
            }

            _subscribedEquipmentRuntime = _equipmentRuntime;

            if (_subscribedEquipmentRuntime != null)
            {
                _subscribedEquipmentRuntime.OnWeaponChanged += HandleWeaponChanged;
                _subscribedEquipmentRuntime.OnEquipmentChanged += HandleEquipmentChanged;
            }
        }
    }

    // Purpose: Removes the inventory events binding.
    private void UnbindInventoryEvents()
    {
        if (_subscribedInventoryRuntime != null)
            _subscribedInventoryRuntime.OnChanged -= HandleInventoryChanged;

        if (_subscribedEquipmentRuntime != null)
        {
            _subscribedEquipmentRuntime.OnWeaponChanged -= HandleWeaponChanged;
            _subscribedEquipmentRuntime.OnEquipmentChanged -= HandleEquipmentChanged;
        }

        _subscribedInventoryRuntime = null;
        _subscribedEquipmentRuntime = null;
    }

    // Purpose: Handles the incoming inventory changed request or event.
    private void HandleInventoryChanged()
    {
        TraceAmmoTransaction(
            "Loadout",
            "PlayerInventoryRuntime.OnChanged",
            null,
            null,
            $"suppressed={_isMutatingInventory}");

        if (_isMutatingInventory)
            return;

        EnsureCompatibleSelectionForActiveWeapon();
        PublishSnapshot();
    }

    // Purpose: Handles the incoming weapon changed request or event.
    private void HandleWeaponChanged(WeaponSO _)
    {
        EnsureCompatibleSelectionForActiveWeapon();
        PublishSnapshot();
    }

    // Purpose: Handles the incoming equipment changed request or event.
    private void HandleEquipmentChanged()
    {
        EnsureCompatibleSelectionForActiveWeapon();
        PublishSnapshot();
    }

    // Purpose: Ensures weapon magazine states is ready before the workflow continues.
    private void EnsureWeaponMagazineStates()
    {
        ResolveInventoryRefs();
        int count = _equipmentRuntime != null
            ? Mathf.Max(1, _equipmentRuntime.WeaponSlotCount)
            : 2;

        if (_weaponMagazines != null && _weaponMagazines.Length == count)
            return;

        WeaponMagazineState[] previous = _weaponMagazines;
        _weaponMagazines = new WeaponMagazineState[count];

        for (int i = 0; i < count; i++)
        {
            _weaponMagazines[i] =
                previous != null && i < previous.Length && previous[i] != null
                    ? previous[i]
                    : new WeaponMagazineState();
        }
    }

    // Purpose: Returns the current active weapon magazine.
    private WeaponMagazineState GetActiveWeaponMagazine()
    {
        EnsureWeaponMagazineStates();

        if (_weaponMagazines == null || _weaponMagazines.Length == 0)
            return null;

        int index = _equipmentRuntime != null
            ? Mathf.Clamp(
                _equipmentRuntime.ActiveWeaponSlotIndex,
                0,
                _weaponMagazines.Length - 1)
            : 0;
        return _weaponMagazines[index];
    }

    // Purpose: Ensures compatible selection for active weapon is ready before the workflow continues.
    private bool EnsureCompatibleSelectionForActiveWeapon()
    {
        ResolveInventoryRefs();
        EnsureWeaponMagazineStates();

        WeaponMagazineState magazine = GetActiveWeaponMagazine();
        if (magazine == null)
            return false;

        if (_equipmentRuntime == null ||
            _equipmentRuntime.CurrentWeapon is not RangedWeaponSO rangedWeapon)
        {
            if (magazine.bullet == null)
                return false;

            if (!TryReturnLoadedAmmoToInventory(magazine, out _))
                return false;

            magazine.Clear();
            return true;
        }

        // An empty magazine still has a selected ammo type. Keep that type
        // while it remains compatible so the player can see and re-select it
        // in the ammo HUD even after its reserve reaches zero.
        bool selectedStillAvailable =
            magazine.bullet != null &&
            rangedWeapon.IsCompatibleWith(magazine.bullet);

        if (selectedStillAvailable)
            return false;

        BulletSO previous = magazine.bullet;

        if (previous != null &&
            !TryReturnLoadedAmmoToInventory(magazine, out _))
        {
            return false;
        }

        magazine.Clear();

        if (!TryFindFirstCompatibleInventoryBullet(rangedWeapon, out BulletSO bullet))
            return previous != null;

        magazine.bullet = bullet;
        return true;
    }

    // Purpose: Attempts to return loaded ammo to inventory and reports whether the request succeeded.
    private bool TryReturnLoadedAmmoToInventory(
        WeaponMagazineState magazine,
        out string message)
    {
        message = string.Empty;

        if (magazine == null ||
            magazine.bullet == null ||
            magazine.currentAmmo <= 0)
        {
            return true;
        }

        BulletSO returnedBullet = magazine.bullet;
        int returnedAmount = magazine.currentAmmo;

        TraceAmmoTransaction(
            "Loadout",
            "TryReturnLoadedAmmoToInventory begin",
            returnedBullet,
            null,
            $"magazineRounds={returnedAmount}");

        // The ownership-cap check includes loaded rounds. Remove these rounds
        // from the magazine before adding them back to inventory, then restore
        // them if the transfer cannot complete.
        magazine.currentAmmo = 0;

        TraceAmmoTransaction(
            "Loadout",
            "TryReturnLoadedAmmoToInventory magazine cleared before transfer",
            returnedBullet,
            null,
            $"transferAmount={returnedAmount}");

        if (!TryStoreAmmoOrDrop(returnedBullet, returnedAmount, out message))
        {
            magazine.currentAmmo = returnedAmount;
            TraceAmmoTransaction(
                "Loadout",
                "TryReturnLoadedAmmoToInventory failed and restored magazine",
                returnedBullet,
                null,
                $"restored={returnedAmount}, message={message}");
            return false;
        }

        TraceAmmoTransaction(
            "Loadout",
            "TryReturnLoadedAmmoToInventory completed",
            returnedBullet,
            null,
            $"moved={returnedAmount}, message={message}");
        return true;
    }

    /// <summary>
    /// Changes the selected magazine ammunition as a small transaction. The old
    /// loaded rounds must first be returned to inventory (or dropped beside the
    /// player) before the empty magazine can select the new ammunition type.
    /// Reloading then owns consuming the new rounds when its duration completes.
    /// </summary>
    private bool TrySwitchLoadedAmmo(
        WeaponMagazineState magazine,
        BulletSO nextBullet,
        out string message)
    {
        message = string.Empty;

        if (magazine == null || nextBullet == null)
        {
            message = "The selected ammunition or active magazine is missing.";
            return false;
        }

        BulletSO previousBullet = magazine.bullet;
        int previousAmmo = Mathf.Max(0, magazine.currentAmmo);
        bool wasMutatingInventory = _isMutatingInventory;

        TraceAmmoTransaction(
            "Loadout",
            "TrySwitchLoadedAmmo begin",
            previousBullet,
            nextBullet,
            $"previousMagazineRounds={previousAmmo}");

        // A return raises inventory events synchronously. Keep those callbacks
        // suppressed for the whole transaction so they cannot auto-select or
        // clear the half-transferred virtual magazine.
        _isMutatingInventory = true;

        try
        {
            // Do not select the new round until the currently loaded rounds have a
            // safe destination. Clear first so ownership limits do not count these
            // same rounds twice; restore on failure so no selected ammo is lost.
            magazine.currentAmmo = 0;

            TraceAmmoTransaction(
                "Loadout",
                "TrySwitchLoadedAmmo old magazine cleared before return",
                previousBullet,
                nextBullet,
                $"returnAmount={previousAmmo}");

            if (previousBullet != null &&
                previousAmmo > 0 &&
                !TryStoreAmmoOrDrop(previousBullet, previousAmmo, out message))
            {
                magazine.currentAmmo = previousAmmo;
                TraceAmmoTransaction(
                    "Loadout",
                    "TrySwitchLoadedAmmo failed and restored previous magazine",
                    previousBullet,
                    nextBullet,
                    $"restored={previousAmmo}, message={message}");
                return false;
            }

            TraceAmmoTransaction(
                "Loadout",
                "TrySwitchLoadedAmmo previous magazine returned",
                previousBullet,
                nextBullet,
                $"returned={previousAmmo}, message={message}");
            magazine.bullet = nextBullet;
            TraceAmmoTransaction(
                "Loadout",
                "TrySwitchLoadedAmmo selected next bullet",
                previousBullet,
                nextBullet,
                "newMagazineRounds=0, awaiting-reload");
            return true;
        }
        finally
        {
            _isMutatingInventory = wasMutatingInventory;
        }
    }

    // Purpose: Builds the ammo selection message from the current authored data.
    private static string BuildAmmoSelectionMessage(
        BulletSO selectedBullet,
        string returnMessage)
    {
        string selectionMessage =
            $"{selectedBullet.DisplayName} selected. Reloading.";

        return string.IsNullOrWhiteSpace(returnMessage)
            ? selectionMessage
            : $"{returnMessage} {selectionMessage}";
    }

    // Purpose: Attempts to store ammo or drop and reports whether the request succeeded.
    private bool TryStoreAmmoOrDrop(
        BulletSO returnedBullet,
        int returnedAmount,
        out string message)
    {
        message = string.Empty;

        if (returnedBullet == null || returnedAmount <= 0)
            return true;

        TraceAmmoTransaction(
            "Loadout",
            "TryStoreAmmoOrDrop begin",
            returnedBullet,
            null,
            $"amount={returnedAmount}, preferredDestination=inventory");

        if (_inventoryRuntime == null)
        {
            message = "Player inventory runtime is missing.";
            return false;
        }

        bool stored;

        bool wasMutatingInventory = _isMutatingInventory;
        _isMutatingInventory = true;

        try
        {
            stored = _inventoryRuntime.TryPickup(
                returnedBullet,
                returnedAmount,
                out message);
        }
        finally
        {
            _isMutatingInventory = wasMutatingInventory;
        }

        if (!stored)
        {
            TraceAmmoTransaction(
                "Loadout",
                "TryStoreAmmoOrDrop inventory rejected return",
                returnedBullet,
                null,
                $"amount={returnedAmount}, inventoryMessage={message}, fallback=world-drop");

            Transform actor = EntityComponentResolver.ResolveEntityRoot(this);
            actor = actor != null ? actor : transform;

            Vector3 forward = Vector3.ProjectOnPlane(actor.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            else
                forward.Normalize();

            Vector3 dropPosition =
                actor.position +
                forward * 0.8f +
                Vector3.up * 0.2f;

            if (!WorldItemPickupSpawner.TrySpawn(
                    returnedBullet,
                    returnedAmount,
                    returnedAmount,
                    0f,
                    0f,
                    dropPosition,
                    Quaternion.identity,
                    out WorldItemPickup worldDrop))
            {
                message = string.IsNullOrWhiteSpace(message)
                    ? "Inventory is full and the ejected magazine rounds could not be dropped."
                    : $"{message} Ejected magazine rounds could not be dropped.";
                TraceAmmoTransaction(
                    "Loadout",
                    "TryStoreAmmoOrDrop world-drop failed",
                    returnedBullet,
                    null,
                    $"amount={returnedAmount}, destination=NONE, message={message}");
                return false;
            }

            message = $"Inventory is full. Dropped {returnedAmount} {returnedBullet.DisplayName} from the magazine.";
            TraceAmmoTransaction(
                "Loadout",
                "TryStoreAmmoOrDrop world-drop created",
                returnedBullet,
                null,
                $"amount={returnedAmount}, destination=world-drop, pickup={(worldDrop != null ? worldDrop.name : "null")}");
        }
        else
        {
            TraceAmmoTransaction(
                "Loadout",
                "TryStoreAmmoOrDrop inventory transfer completed",
                returnedBullet,
                null,
                $"amount={returnedAmount}, destination=inventory, message={message}");
        }

        return true;
    }

    // Purpose: Attempts to find first compatible inventory bullet and reports whether the request succeeded.
    private bool TryFindFirstCompatibleInventoryBullet(
        RangedWeaponSO weapon,
        out BulletSO bullet)
    {
        bullet = null;

        if (weapon == null || _inventoryRuntime == null)
            return false;

        IReadOnlyList<PlayerInventoryItemStack> items = _inventoryRuntime.Items;

        if (items == null)
            return false;

        for (int i = 0; i < items.Count; i++)
        {
            PlayerInventoryItemStack stack = items[i];

            if (stack == null ||
                stack.IsEmpty ||
                stack.Amount <= 0 ||
                stack.Item is not BulletSO candidate ||
                !weapon.IsCompatibleWith(candidate))
            {
                continue;
            }

            bullet = candidate;
            return true;
        }

        return false;
    }

    // Purpose: Collects compatible inventory bullets for this workflow.
    private void CollectCompatibleInventoryBullets(
        RangedWeaponSO weapon,
        List<BulletSO> results)
    {
        results.Clear();

        if (weapon == null || _inventoryRuntime == null)
            return;

        IReadOnlyList<PlayerInventoryItemStack> items = _inventoryRuntime.Items;

        if (items == null)
            return;

        for (int i = 0; i < items.Count; i++)
        {
            PlayerInventoryItemStack stack = items[i];

            if (stack == null ||
                stack.IsEmpty ||
                stack.Amount <= 0 ||
                stack.Item is not BulletSO bullet ||
                !weapon.IsCompatibleWith(bullet) ||
                results.Contains(bullet))
            {
                continue;
            }

            results.Add(bullet);
        }
    }

    // Purpose: Builds the runtime slots from the current authored data.
    private void BuildRuntimeSlots()
    {
        _slots = new AmmoSlotState[SlotCount];

        for (int i = 0; i < SlotCount; i++)
            _slots[i] = new AmmoSlotState();

        _activePrimarySlotIndex = FindFirstSlotOfType(BulletAmmoType.Basic);
        _activeSpecialSlotIndex = FindFirstSlotOfType(BulletAmmoType.Special);
        _lastSelectedSlotIndex = FindFirstOccupiedSlot();
        EnsureWeaponMagazineStates();
    }

    // Purpose: Handles the incoming slot select requested request or event.
    private void HandleSlotSelectRequested(int slotIndex)
    {
        SelectSlot(slotIndex);
    }

    // Purpose: Handles the incoming slot swap requested request or event.
    private void HandleSlotSwapRequested(WeaponAmmoSlotSwapRequest request)
    {
        TrySwapSlots(request.fromSlotIndex, request.toSlotIndex);
    }

    // Purpose: Handles the incoming shop purchase requested request or event.
    private void HandleShopPurchaseRequested(WeaponAmmoShopPurchaseRequest request)
    {
        ResolveInventoryRefs();
        int slotIndex = -1;
        string message;
        bool granted;

        if (_inventoryRuntime != null)
        {
            granted = _inventoryRuntime.TryPickup(
                request.bullet,
                Mathf.Max(1, request.bundleAmount),
                out message);
        }
        else
        {
            granted = false;
            message = "Player inventory runtime is missing.";
        }

        RaisePurchaseResult(request, granted, slotIndex, message);
    }

    // Purpose: Handles the incoming sell confirm requested request or event.
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

    // Purpose: Performs the clamp all magazine ammo workflow for this component.
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

    // Purpose: Attempts to find empty compatible slot and reports whether the request succeeded.
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

    // Purpose: Checks whether move slot to is currently permitted.
    private bool CanMoveSlotTo(AmmoSlotState state, int slotIndex)
    {
        if (state == null || state.bullet == null)
            return true;

        return CanEquipBulletInSlot(slotIndex, state.bullet);
    }

    // Purpose: Checks whether equip bullet in slot is currently permitted.
    private bool CanEquipBulletInSlot(int slotIndex, BulletSO bullet)
    {
        // All three equipment slots are generic BulletSO slots. Special bullets
        // SpecialBulletSO variants inherit BulletSO and must be equipable in
        // the first slot as well; the bullet's AmmoType selects its firing lane.
        return IsValidSlot(slotIndex) && bullet != null;
    }

    // Purpose: Returns the current active slot index.
    private int GetActiveSlotIndex(BulletAmmoType ammoType)
    {
        return ammoType switch
        {
            BulletAmmoType.Basic => _activePrimarySlotIndex,
            BulletAmmoType.Special => _activeSpecialSlotIndex,
            _ => -1
        };
    }

    // Purpose: Updates the active slot state.
    private void SetActiveSlot(BulletAmmoType ammoType, int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return;

        if (_slots[slotIndex].bullet == null ||
            !IsCompatibleAmmoType(_slots[slotIndex].bullet.AmmoType, ammoType))
            return;

        switch (ammoType)
        {
            case BulletAmmoType.Basic:
                _activePrimarySlotIndex = slotIndex;
                break;

            case BulletAmmoType.Special:
                _activeSpecialSlotIndex = slotIndex;
                break;
        }
    }

    // Purpose: Ensures active slots valid is ready before the workflow continues.
    private void EnsureActiveSlotsValid()
    {
        if (!IsActiveSlotValid(BulletAmmoType.Basic, _activePrimarySlotIndex))
            _activePrimarySlotIndex = FindFirstSlotOfType(BulletAmmoType.Basic);

        if (!IsActiveSlotValid(BulletAmmoType.Special, _activeSpecialSlotIndex))
            _activeSpecialSlotIndex = FindFirstSlotOfType(BulletAmmoType.Special);

        if (!IsValidSlot(_lastSelectedSlotIndex) || _slots[_lastSelectedSlotIndex].bullet == null)
            _lastSelectedSlotIndex = FindFirstOccupiedSlot();
    }

    // Purpose: Checks whether active slot valid is currently true.
    private bool IsActiveSlotValid(BulletAmmoType ammoType, int slotIndex)
    {
        return IsValidSlot(slotIndex) &&
               _slots[slotIndex].bullet != null &&
               IsCompatibleAmmoType(_slots[slotIndex].bullet.AmmoType, ammoType);
    }

    // Purpose: Finds the requested first slot of type.
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

    // Purpose: Finds the requested first occupied slot.
    private int FindFirstOccupiedSlot()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (_slots[i].bullet != null)
                return i;
        }

        return -1;
    }

    // Purpose: Checks whether slot selected is currently true.
    private bool IsSlotSelected(int slotIndex)
    {
        return slotIndex == _lastSelectedSlotIndex;
    }

    // Purpose: Performs the swap active index workflow for this component.
    private static void SwapActiveIndex(ref int activeIndex, int fromSlotIndex, int toSlotIndex)
    {
        if (activeIndex == fromSlotIndex)
            activeIndex = toSlotIndex;
        else if (activeIndex == toSlotIndex)
            activeIndex = fromSlotIndex;
    }

    // Purpose: Publishes the snapshot notification.
    private void PublishSnapshot()
    {
        if (_slots == null)
            return;

        ClampAllMagazineAmmo();

        TryGetSelectedBullet(out BulletSO selectedBullet);

        if (_lastNotifiedSelectedBullet != selectedBullet)
        {
            _lastNotifiedSelectedBullet = selectedBullet;
            SelectedBulletChanged?.Invoke(selectedBullet);
        }

        if (_weaponAmmoLoadoutEventChannel == null)
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
                IsSlotSelected(i),
                slot.requiredDefault,
                slot.sellPricePerAmmo);
        }

        _weaponAmmoLoadoutEventChannel.RaiseEvent(
            new WeaponAmmoLoadoutSnapshot(snapshots, _lastSelectedSlotIndex));
    }

    // Purpose: Publishes the purchase result notification.
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

    // Purpose: Publishes the sell result notification.
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

    // Purpose: Checks whether runtime ready is currently true.
    private bool IsRuntimeReady()
    {
        return _slots != null && _statsRuntime != null;
    }

    // Purpose: Checks whether valid slot is currently true.
    private bool IsValidSlot(int slotIndex)
    {
        return _slots != null && slotIndex >= 0 && slotIndex < SlotCount;
    }

    // Purpose: Checks whether compatible ammo type is currently true.
    private static bool IsCompatibleAmmoType(
        BulletAmmoType bulletType,
        BulletAmmoType requestedType)
    {
        return bulletType == requestedType;
    }

    // Purpose: Checks whether reload active ammo is currently permitted.
    public bool CanReloadActiveAmmo(BulletAmmoType ammoType)
    {
        if (!IsRuntimeReady())
            return false;

        ResolveInventoryRefs();
        WeaponMagazineState magazine = GetActiveWeaponMagazine();

        if (magazine == null ||
            magazine.bullet == null ||
            !IsCompatibleAmmoType(magazine.bullet.AmmoType, ammoType) ||
            _inventoryRuntime == null)
        {
            return false;
        }

        return CanReloadActiveAmmo(magazine.bullet);
    }

    // Purpose: Checks whether reload active ammo is currently permitted.
    public bool CanReloadActiveAmmo(BulletSO expectedBullet)
    {
        if (!IsRuntimeReady() || expectedBullet == null)
            return false;

        ResolveInventoryRefs();
        WeaponMagazineState magazine = GetActiveWeaponMagazine();

        if (magazine == null ||
            magazine.bullet != expectedBullet ||
            _inventoryRuntime == null)
        {
            return false;
        }

        int magazineSize = GetMagazineSize(expectedBullet);
        return magazine.currentAmmo < magazineSize &&
               _inventoryRuntime.GetInventoryItemAmount(expectedBullet) > 0;
    }

    // Purpose: Performs the reload active ammo workflow for this component.
    public bool ReloadActiveAmmo(BulletAmmoType ammoType)
    {
        if (!IsRuntimeReady())
            return false;

        WeaponMagazineState magazine = GetActiveWeaponMagazine();
        if (magazine == null ||
            magazine.bullet == null ||
            !IsCompatibleAmmoType(magazine.bullet.AmmoType, ammoType))
        {
            return false;
        }

        return ReloadActiveAmmo(magazine.bullet);
    }

    // Purpose: Performs the reload active ammo workflow for this component.
    public bool ReloadActiveAmmo(BulletSO expectedBullet)
    {
        if (!CanReloadActiveAmmo(expectedBullet))
            return false;

        WeaponMagazineState magazine = GetActiveWeaponMagazine();
        int needed = Mathf.Max(
            0,
            GetMagazineSize(expectedBullet) - magazine.currentAmmo);
        int available = _inventoryRuntime.GetInventoryItemAmount(expectedBullet);
        int loadAmount = Mathf.Min(needed, available);

        if (loadAmount <= 0)
            return false;

        TraceAmmoTransaction(
            "Loadout",
            "ReloadActiveAmmo begin",
            expectedBullet,
            null,
            $"needed={needed}, available={available}, loadAmount={loadAmount}");

        _isMutatingInventory = true;
        bool consumed;

        try
        {
            consumed = _inventoryRuntime.TryConsumeInventoryItem(
                expectedBullet,
                loadAmount,
                out _);
        }
        finally
        {
            _isMutatingInventory = false;
        }

        if (!consumed)
        {
            TraceAmmoTransaction(
                "Loadout",
                "ReloadActiveAmmo inventory consumption failed",
                expectedBullet,
                null,
                $"requested={loadAmount}");
            return false;
        }

        TraceAmmoTransaction(
            "Loadout",
            "ReloadActiveAmmo inventory consumption completed",
            expectedBullet,
            null,
            $"consumed={loadAmount}, magazineNotIncrementedYet=true");

        magazine.currentAmmo += loadAmount;
        TraceAmmoTransaction(
            "Loadout",
            "ReloadActiveAmmo magazine increment completed",
            expectedBullet,
            null,
            $"loaded={loadAmount}");
        PublishSnapshot();
        return true;
    }

    // Purpose: Attempts to get active special bullet and reports whether the request succeeded.
    public bool TryGetActiveSpecialBullet(out SpecialBulletSO bullet)
    {
        bullet = GetActiveBullet(BulletAmmoType.Special) as SpecialBulletSO;
        return bullet != null;
    }

    // Purpose: Attempts to consume special ammo and reports whether the request succeeded.
    public bool TryConsumeSpecialAmmo(int amount, out SpecialBulletSO bullet)
    {
        bullet = null;

        if (!TryConsumeAmmo(BulletAmmoType.Special, amount, out BulletSO consumedBullet))
            return false;

        bullet = consumedBullet as SpecialBulletSO;
        return bullet != null;
    }

    // Purpose: Performs the trace ammo transaction workflow for this component.
    public void TraceAmmoTransaction(
        string source,
        string action,
        BulletSO primaryBullet = null,
        BulletSO secondaryBullet = null,
        string details = null)
    {
        if (!_debugAmmoTransactionTrace)
            return;

        int traceId = ++_ammoTraceSequence;
        string caller = ResolveAmmoTraceCaller();

        Debug.Log(
            $"[AmmoTrace #{traceId}][{source}] {action}. " +
            $"details={details ?? "none"}. caller={caller}. " +
            BuildAmmoTraceState(primaryBullet, secondaryBullet),
            this);
    }

    // Purpose: Builds the ammo trace state from the current authored data.
    private string BuildAmmoTraceState(BulletSO primaryBullet, BulletSO secondaryBullet)
    {
        StringBuilder builder = new();
        builder.Append("focus=");
        AppendAmmoTraceBulletState(builder, primaryBullet);

        if (secondaryBullet != null && secondaryBullet != primaryBullet)
        {
            builder.Append(" | ");
            AppendAmmoTraceBulletState(builder, secondaryBullet);
        }

        builder.Append(" | virtualMagazines=");

        if (_weaponMagazines == null || _weaponMagazines.Length == 0)
        {
            builder.Append("none");
        }
        else
        {
            for (int i = 0; i < _weaponMagazines.Length; i++)
            {
                WeaponMagazineState magazine = _weaponMagazines[i];

                if (i > 0)
                    builder.Append(", ");

                builder.Append($"slot{i}:{ResolveAmmoTraceBulletName(magazine != null ? magazine.bullet : null)}/");
                builder.Append(magazine != null ? Mathf.Max(0, magazine.currentAmmo) : 0);
            }
        }

        builder.Append(" | legacySlots=");

        if (_slots == null || _slots.Length == 0)
        {
            builder.Append("none");
        }
        else
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                AmmoSlotState slot = _slots[i];

                if (i > 0)
                    builder.Append(", ");

                builder.Append($"slot{i}:{ResolveAmmoTraceBulletName(slot != null ? slot.bullet : null)}/");
                builder.Append(slot != null ? Mathf.Max(0, slot.currentAmmo) : 0);
                builder.Append('+');
                builder.Append(slot != null ? Mathf.Max(0, slot.reserveAmmo) : 0);
            }
        }

        AppendAmmoTraceQuickSlots(builder, primaryBullet, secondaryBullet);
        return builder.ToString();
    }

    // Purpose: Performs the append ammo trace bullet state workflow for this component.
    private void AppendAmmoTraceBulletState(StringBuilder builder, BulletSO bullet)
    {
        if (bullet == null)
        {
            builder.Append("none");
            return;
        }

        int inventoryAmount = _inventoryRuntime != null
            ? _inventoryRuntime.GetInventoryItemAmount(bullet)
            : 0;
        int virtualAmount = 0;
        int legacyAmount = 0;

        if (_weaponMagazines != null)
        {
            for (int i = 0; i < _weaponMagazines.Length; i++)
            {
                WeaponMagazineState magazine = _weaponMagazines[i];

                if (magazine != null && magazine.bullet == bullet)
                    virtualAmount += Mathf.Max(0, magazine.currentAmmo);
            }
        }

        if (_slots != null)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                AmmoSlotState slot = _slots[i];

                if (slot != null && slot.bullet == bullet)
                    legacyAmount += slot.TotalAmmo;
            }
        }

        builder.Append(
            $"{ResolveAmmoTraceBulletName(bullet)}[inventory={inventoryAmount},virtual={virtualAmount},legacy={legacyAmount},total={inventoryAmount + virtualAmount + legacyAmount}]");
    }

    // Purpose: Performs the append ammo trace quick slots workflow for this component.
    private void AppendAmmoTraceQuickSlots(
        StringBuilder builder,
        BulletSO primaryBullet,
        BulletSO secondaryBullet)
    {
        PlayerQuickSlotRuntime quickSlots =
            EntityComponentResolver.Resolve<PlayerQuickSlotRuntime>(this);

        if (quickSlots == null)
            return;

        StringBuilder quickSlotDetails = new();

        for (int i = 0; i < PlayerQuickSlotRuntime.SlotCount; i++)
        {
            if (!quickSlots.TryGetSnapshot(i, out PlayerQuickSlotSnapshot snapshot) ||
                snapshot.IsEmpty)
            {
                continue;
            }

            bool referencesFocus = snapshot.item == primaryBullet ||
                                  snapshot.item == secondaryBullet ||
                                  snapshot.kind == PlayerQuickSlotBindingKind.Ammo;

            if (!referencesFocus)
                continue;

            if (quickSlotDetails.Length > 0)
                quickSlotDetails.Append(", ");

            quickSlotDetails.Append(
                $"quick{i}:{snapshot.kind}:{ResolveAmmoTraceBulletName(snapshot.item as BulletSO)}:{snapshot.amount}");
        }

        if (quickSlotDetails.Length > 0)
            builder.Append($" | quickSlots={quickSlotDetails}");
    }

    // Purpose: Resolves the ammo trace caller needed by this component.
    private string ResolveAmmoTraceCaller()
    {
        if (!_debugAmmoTransactionTraceCallers)
            return "disabled";

        System.Diagnostics.StackTrace trace = new(2, false);

        for (int i = 0; i < trace.FrameCount; i++)
        {
            System.Reflection.MethodBase method = trace.GetFrame(i)?.GetMethod();

            if (method == null || method.DeclaringType == typeof(PlayerBulletLoadoutRuntime))
                continue;

            return $"{method.DeclaringType?.Name}.{method.Name}";
        }

        return "unknown";
    }

    // Purpose: Resolves the ammo trace bullet name needed by this component.
    private static string ResolveAmmoTraceBulletName(BulletSO bullet)
    {
        if (bullet == null)
            return "none";

        return string.IsNullOrWhiteSpace(bullet.DisplayName)
            ? bullet.name
            : bullet.DisplayName;
    }
}
