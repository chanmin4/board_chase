/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player
 * Primary responsibility: Handles PlayerAimController camera, vision, range, or aiming calculations and presentation.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerAimController : EntityRange
{
    private const float AimingMoveSpeedMultiplier = 0.6f;

    private enum ShootLane
    {
        Primary,
        Paint,
        Special
    }

    [Header("Refs")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private PlayerShooterShoot _shoot;
    [SerializeField] private PlayerStatsRuntime _statsRuntime;
    [SerializeField] private PlayerBulletLoadoutRuntime _bulletLoadout;
    [SerializeField] private PlayerConsumableUseRuntime _consumableUseRuntime;
    [SerializeField] private PlayerQuickSlotRuntime _quickSlots;
    [SerializeField] private EntityEquipmentRuntime _equipmentRuntime;
    [SerializeField] private PlayerBulletLoadoutRuntimeReadyEventChannelSO _bulletLoadoutReadyChannel;

    [Header("Reload Progress")]
    [Tooltip("Shared world action gauge used for both ordinary reloads and ammunition-change reloads.")]
    [SerializeField] private InteractionCastBarEventChannelSO _reloadCastBarChannel;

    [Header("Aim Raycast")]
    [SerializeField] private LayerMask _aimRaycastMask = ~0;
    [SerializeField] private bool _allowFallbackPlane = true;
    [SerializeField] private float _fallbackPlaneY = 0f;


    [Header("Aim State")]
    [SerializeField] private bool _pollRightMouseButton = true;

    [Header("Auto Refs Don't Touch")]
    [SerializeField] private Camera _aimCamera;

    [Header("Runtime")]
    [ReadOnly][SerializeField] private bool _isAiming;
    [ReadOnly][SerializeField, Range(0f, 1f)] private float _aim01;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = false;

    private BulletAmmoType _lastActiveAmmoType = BulletAmmoType.Basic;
    private Vector3 _aimWorldPoint;
    private bool _hasAimPoint;
    private bool _isAimWithinRange;
    private bool _blockAimUntilRightMouseReleased;

    private float _nextPrimaryFireTime;
    private float _nextPaintFireTime;
    private float _nextSpecialFireTime;

    private bool _isReloading;
    private bool _hasReloadingAmmoType;
    private BulletAmmoType _reloadingAmmoType;
    private BulletSO _reloadingBullet;
    private float _reloadStartTime;
    private float _reloadEndTime;
    private PlayerBulletLoadoutRuntime _subscribedBulletLoadout;
    private int _observedWeaponSlotIndex = -1;
    private WeaponSO _observedWeapon;
    private int _observedQuickSlotIndex = -1;
    private UsableItemSO _observedQuickSlotItem;

    public event Action OnReloadStarted;
    public event Action OnReloadFinished;
    public event Action OnShotConsumed;
    public event Action AimStarted;
    public event Action AimEnded;

    public Vector3 AimWorldPoint => _aimWorldPoint;
    public bool HasAimPoint => _hasAimPoint;
    // The cursor is no longer constrained by projectile travel distance.
    public bool IsAimWithinRange => _hasAimPoint;
    public bool IsReloading => _isReloading;
    public int CurrentAmmo => 0;

    public bool IsAiming => _isAiming;
    public float Aim01 => _aim01;
    public bool IsAimComplete => _aim01 >= 0.999f;

    public float RangeMultiplier => 1f;

    // Movement is slowed as soon as RMB is held and restored as soon as it is released.
    public float MoveSpeedMultiplier => _isAiming ? AimingMoveSpeedMultiplier : 1f;

    public Camera AimCamera => _aimCamera;
    public override WeaponSO CurrentWeapon => base.CurrentWeapon;

    public override float MaxRange =>
        _statsRuntime != null
            ? Mathf.Max(0.1f, _statsRuntime.MaxRange)
            : base.MaxRange;

    private float CurrentReloadDuration =>
        _statsRuntime != null
            ? Mathf.Max(0.01f, _statsRuntime.Weapon.reloadDurationSeconds)
            : 0.01f;

    public bool CanFireNow
    {
        get
        {
            if (_bulletLoadout == null ||
                !_bulletLoadout.TryGetSelectedAmmoType(out BulletAmmoType ammoType))
            {
                return false;
            }

            return CanFireNowFor(ammoType);
        }
    }

    public bool IsOnFireCooldown =>
        Time.time < _nextPrimaryFireTime ||
        Time.time < _nextPaintFireTime ||
        Time.time < _nextSpecialFireTime;

    public float Reload01
    {
        get
        {
            if (!_isReloading)
                return 1f;

            float duration = Mathf.Max(0.01f, _reloadEndTime - _reloadStartTime);
            return Mathf.Clamp01((Time.time - _reloadStartTime) / duration);
        }
    }

    public float Cooldown01 => GetCooldown01(_lastActiveAmmoType);

    public float ActiveProgress01
    {
        get
        {
            if (_isReloading)
                return Reload01;

            if (Time.time < GetNextFireTime(_lastActiveAmmoType))
                return GetCooldown01(_lastActiveAmmoType);

            return 1f;
        }
    }

    // Purpose: Initializes Inspector defaults for this component.
    protected override void Reset()
    {
        base.Reset();
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    protected override void Awake()
    {
        base.Awake();
        ResolveRefs();
        EnsureAimCamera();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveRefs();
        BindBulletLoadoutEvents(_bulletLoadout);

        if (_shoot != null)
            _shoot.Fired += OnShotExecuted;

        if (_inputReader != null)
            _inputReader.ReloadEvent += OnReloadRequested;

        if (_equipmentRuntime != null)
            _equipmentRuntime.OnEquipmentChanged += HandleEquipmentSelectionChanged;

        if (_quickSlots != null)
            _quickSlots.OnChanged += HandleQuickSlotSelectionChanged;

        CaptureHeldItemSelection();

        if (_bulletLoadoutReadyChannel != null)
        {
            _bulletLoadoutReadyChannel.OnEventRaised += HandleBulletLoadoutReady;

            if (_bulletLoadoutReadyChannel.HasCurrent)
                HandleBulletLoadoutReady(_bulletLoadoutReadyChannel.Current);
        }
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        bool wasReloading = _isReloading;

        if (_shoot != null)
            _shoot.Fired -= OnShotExecuted;

        if (_inputReader != null)
            _inputReader.ReloadEvent -= OnReloadRequested;

        if (_equipmentRuntime != null)
            _equipmentRuntime.OnEquipmentChanged -= HandleEquipmentSelectionChanged;

        if (_quickSlots != null)
            _quickSlots.OnChanged -= HandleQuickSlotSelectionChanged;

        if (_bulletLoadoutReadyChannel != null)
            _bulletLoadoutReadyChannel.OnEventRaised -= HandleBulletLoadoutReady;

        BindBulletLoadoutEvents(null);

        if (wasReloading)
            CancelReload("PlayerAimController disabled.");
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        if (GamePause.IsPaused || Time.timeScale <= 0f)
        {
            _hasAimPoint = false;
            _isAimWithinRange = false;
            SetAiming(false);
            return;
        }

        EnsureAimCamera();
        UpdateAimState();
        UpdateAimPoint();
        UpdateReloadState();
    }

    // Purpose: Checks whether fire now for is currently permitted.
    public bool CanFireNowFor(BulletAmmoType ammoType)
    {
        _lastActiveAmmoType = ammoType;

        if (GameplayActionInputBlocker.IsAttackBlocked)
            return false;

        if (CurrentWeapon == null)
            return false;

        if (_bulletLoadout == null)
            return false;

        if (_isReloading)
            return false;

        if (!_bulletLoadout.HasLoadedAmmo(ammoType))
        {
            if (_bulletLoadout.CanReloadActiveAmmo(ammoType))
            {
                RequestReload(ammoType);
                return false;
            }

            return Time.time >= GetNextFireTime(ammoType);
        }

        if (!_hasAimPoint)
            return false;

        return Time.time >= GetNextFireTime(ammoType);
    }

    // Purpose: Performs the block aim until right mouse released workflow for this component.
    public void BlockAimUntilRightMouseReleased()
    {
        _blockAimUntilRightMouseReleased = true;
        SetAiming(false);
    }

    // Purpose: Applies the requested dry fire cooldown change.
    public void ApplyDryFireCooldown(BulletAmmoType ammoType)
    {
        float cooldown = Mathf.Max(0.15f, 1f / GetShotsPerSecond(ammoType));
        SetNextFireTime(ammoType, Time.time + cooldown);
    }

    // Purpose: Requests reload from the owning service.
    public bool RequestReload(BulletAmmoType ammoType)
    {
        if (_bulletLoadout == null)
            return false;

        if (!_bulletLoadout.TryGetActiveBullet(ammoType, out BulletSO bullet) ||
            bullet == null)
        {
            return false;
        }

        return StartReload(bullet, false);
    }

    // Purpose: Begins the reload workflow.
    private bool StartReload(BulletSO bullet, bool restartIfActive)
    {
        if (_bulletLoadout == null || bullet == null)
            return false;

        if (_isReloading && !restartIfActive)
            return false;

        if (!_bulletLoadout.CanReloadActiveAmmo(bullet))
            return false;

        _isReloading = true;
        _hasReloadingAmmoType = true;
        _reloadingAmmoType = bullet.AmmoType;
        _reloadingBullet = bullet;

        _reloadStartTime = Time.time;
        _reloadEndTime = Time.time + CurrentReloadDuration;

        PublishReloadProgress();
        OnReloadStarted?.Invoke();
        _bulletLoadout.TraceAmmoTransaction(
            "AimController",
            "Reload started",
            bullet,
            null,
            $"restart={restartIfActive}, duration={CurrentReloadDuration:F3}");

        if (_debugLogs)
        {
            Debug.Log(
                $"[PlayerAimController] Reload started. bullet={bullet.DisplayName}, restart={restartIfActive}",
                this);
        }

        return true;
    }

    // Purpose: Attempts to get aim point and reports whether the request succeeded.
    public bool TryGetAimPoint(out Vector3 worldPoint)
    {
        worldPoint = default;

        if (_aimCamera == null ||
            CurrentWeapon == null ||
            !TryGetSelectedAimBullet(out BulletSO bullet))
        {
            return false;
        }

        return VSplatterAimUtility.TryGetAimPoint(
            _aimCamera,
            _aimRaycastMask,
            _allowFallbackPlane,
            _fallbackPlaneY,
            transform.root,
            out worldPoint,  out _);
    }

    // Purpose: Attempts to get raw aim point and reports whether the request succeeded.
    public bool TryGetRawAimPoint(out Vector3 worldPoint)
    {
        worldPoint = default;

        if (_aimCamera == null)
            return false;

        return VSplatterAimUtility.TryGetAimPoint(
            _aimCamera,
            _aimRaycastMask,
            _allowFallbackPlane,
            _fallbackPlaneY,
            transform.root,
            out worldPoint,
            out _);
    }

    // Purpose: Attempts to get flat aim direction and reports whether the request succeeded.
    public bool TryGetFlatAimDirection(Vector3 origin, out Vector3 direction)
    {
        direction = Vector3.zero;

        if (!_hasAimPoint)
            return false;

        direction = _aimWorldPoint - origin;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return false;

        direction.Normalize();
        return true;
    }

    // Purpose: Checks whether cancel reload is currently permitted.
    public bool CancelReload(string reason = null)
    {
        if (!_isReloading)
            return false;

        BulletSO canceledReloadBullet = _reloadingBullet;
        _isReloading = false;
        _hasReloadingAmmoType = false;
        _reloadingBullet = null;
        _reloadStartTime = 0f;
        _reloadEndTime = 0f;
        _reloadCastBarChannel?.Clear();
        OnReloadFinished?.Invoke();

        if (_bulletLoadout != null)
        {
            _bulletLoadout.TraceAmmoTransaction(
                "AimController",
                "Reload canceled",
                canceledReloadBullet,
                null,
                $"reason={reason ?? "Unspecified"}");
        }

        if (_debugLogs)
        {
            Debug.Log(
                $"[PlayerAimController] Reload canceled. reason={reason ?? "Unspecified"}",
                this);
        }

        return true;
    }

    // Purpose: Applies the requested accuracy and recoil change.
    public Vector3 ApplyAccuracyAndRecoil(Vector3 origin, Vector3 target)
    {
        Vector3 direction = target - origin;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return target;

        Vector3 flatDirection = direction.normalized;
        float hipSpread = _statsRuntime != null ? _statsRuntime.HipFireSpreadAngleDeg : 0f;
        float aimSpread = _statsRuntime != null ? _statsRuntime.AimSpreadAngleDeg : 0f;
        bool receivesAimBonus = _isAiming && IsAimComplete;
        float fullConeAngle = receivesAimBonus ? aimSpread : hipSpread;
        float halfConeAngle = Mathf.Max(0f, fullConeAngle) * 0.5f;
        float randomAngularSpread = halfConeAngle > 0f
            ? UnityEngine.Random.Range(-halfConeAngle, halfConeAngle)
            : 0f;

        Quaternion rotation = Quaternion.AngleAxis(randomAngularSpread, Vector3.up);
        Vector3 adjustedDirection = rotation * flatDirection;

        return origin + adjustedDirection * direction.magnitude;
    }

    // Kept as a call-site seam for muzzle/animation code. Accuracy no longer
    // accumulates recoil or bloom between automatic shots.
    // Purpose: Applies the requested shot recoil change.
    public void ApplyShotRecoil()
    {
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        ResolveEntityRangeRefs();

        if (_inputReader == null)
            _inputReader = FindAnyObjectByType<InputReader>();

        if (_shoot == null)
            _shoot = EntityComponentResolver.Resolve<PlayerShooterShoot>(this);

        if (_statsRuntime == null)
        {
            _statsRuntime = EntityComponentResolver.Resolve<PlayerStatsRuntime>(this);

            if (_statsRuntime == null && _entityStatsRuntime is PlayerStatsRuntime playerStatsRuntime)
                _statsRuntime = playerStatsRuntime;
        }

        if (_bulletLoadout == null)
            _bulletLoadout = EntityComponentResolver.Resolve<PlayerBulletLoadoutRuntime>(this);

        if (_consumableUseRuntime == null)
        {
            _consumableUseRuntime =
                EntityComponentResolver.Resolve<PlayerConsumableUseRuntime>(this);
        }

        if (_quickSlots == null)
            _quickSlots = EntityComponentResolver.Resolve<PlayerQuickSlotRuntime>(this);

        if (_equipmentRuntime == null)
            _equipmentRuntime = EntityComponentResolver.Resolve<EntityEquipmentRuntime>(this);
    }

    // Purpose: Performs the capture held item selection workflow for this component.
    private void CaptureHeldItemSelection()
    {
        _observedWeaponSlotIndex =
            _equipmentRuntime != null
                ? _equipmentRuntime.ActiveWeaponSlotIndex
                : -1;
        _observedWeapon =
            _equipmentRuntime != null
                ? _equipmentRuntime.CurrentWeapon
                : null;
        _observedQuickSlotIndex =
            _quickSlots != null
                ? _quickSlots.ActiveQuickSlotIndex
                : -1;
        _observedQuickSlotItem =
            _quickSlots != null
                ? _quickSlots.ActiveUsableItem
                : null;
    }

    // Purpose: Handles the incoming equipment selection changed request or event.
    private void HandleEquipmentSelectionChanged()
    {
        int nextSlot =
            _equipmentRuntime != null
                ? _equipmentRuntime.ActiveWeaponSlotIndex
                : -1;
        WeaponSO nextWeapon =
            _equipmentRuntime != null
                ? _equipmentRuntime.CurrentWeapon
                : null;
        bool heldWeaponChanged =
            nextSlot != _observedWeaponSlotIndex ||
            nextWeapon != _observedWeapon;

        _observedWeaponSlotIndex = nextSlot;
        _observedWeapon = nextWeapon;

        if (heldWeaponChanged)
            CancelReload("Equipped weapon changed.");
    }

    // Purpose: Handles the incoming quick slot selection changed request or event.
    private void HandleQuickSlotSelectionChanged()
    {
        int nextQuickSlot =
            _quickSlots != null
                ? _quickSlots.ActiveQuickSlotIndex
                : -1;
        UsableItemSO nextQuickSlotItem =
            _quickSlots != null
                ? _quickSlots.ActiveUsableItem
                : null;
        bool heldQuickSlotChanged =
            nextQuickSlot != _observedQuickSlotIndex ||
            nextQuickSlotItem != _observedQuickSlotItem;

        _observedQuickSlotIndex = nextQuickSlot;
        _observedQuickSlotItem = nextQuickSlotItem;

        if (heldQuickSlotChanged)
            CancelReload("Held quick-slot item changed.");
    }

    // Purpose: Updates the aim state from the current runtime state.
    private void UpdateAimState()
    {
        bool isRightMousePressed =
            Mouse.current != null &&
            Mouse.current.rightButton.isPressed;

        if (_blockAimUntilRightMouseReleased && !isRightMousePressed)
            _blockAimUntilRightMouseReleased = false;

        // Usable items reserve RMB for cancellation and never inherit firearm aim.
        bool supportsFirearmAim =
            (_quickSlots == null || _quickSlots.ActiveUsableItem == null) &&
            (_consumableUseRuntime == null || !_consumableUseRuntime.IsUsing) &&
            !_blockAimUntilRightMouseReleased;
        bool nextIsAiming =
            supportsFirearmAim &&
            !GameplayActionInputBlocker.IsAimBlocked &&
            _pollRightMouseButton &&
            isRightMousePressed;

        SetAiming(nextIsAiming);

        float speed = _statsRuntime != null ? _statsRuntime.AimSpeed : 8f;
        float target = _isAiming ? 1f : 0f;

        _aim01 = Mathf.MoveTowards(
            _aim01,
            target,
            Mathf.Max(0.01f, speed) * Time.deltaTime);

    }

    // Purpose: Updates the aiming state.
    private void SetAiming(bool isAiming)
    {
        if (_isAiming == isAiming)
            return;

        _isAiming = isAiming;

        if (_isAiming)
            AimStarted?.Invoke();
        else
            AimEnded?.Invoke();
    }

    // Purpose: Responds to the shot executed callback.
    private void OnShotExecuted(BulletAmmoType ammoType)
    {
        _lastActiveAmmoType = ammoType;

        if (CurrentWeapon == null)
            return;

        float shotsPerSecond = GetShotsPerSecond(ammoType);
        SetNextFireTime(ammoType, Time.time + (1f / shotsPerSecond));

        OnShotConsumed?.Invoke();

        if (_debugLogs)
            Debug.Log($"[PlayerAimController] {ammoType} shot cooldown started.", this);
    }

    // Purpose: Updates the reload state from the current runtime state.
    private void UpdateReloadState()
    {
        if (!_isReloading)
            return;

        if (Time.time < _reloadEndTime)
        {
            PublishReloadProgress();
            return;
        }

        _isReloading = false;

        BulletSO completedReloadBullet = _reloadingBullet;
        bool reloadApplied = false;

        if (_bulletLoadout != null &&
            _hasReloadingAmmoType &&
            _reloadingBullet != null)
        {
            reloadApplied = _bulletLoadout.ReloadActiveAmmo(_reloadingBullet);
            _bulletLoadout.TraceAmmoTransaction(
                "AimController",
                "Reload finished",
                completedReloadBullet,
                null,
                $"reloadApplied={reloadApplied}");
        }

        _hasReloadingAmmoType = false;
        _reloadingBullet = null;
        _reloadCastBarChannel?.Clear();

        OnReloadFinished?.Invoke();

        if (_debugLogs)
            Debug.Log($"[PlayerAimController] Reload finished. ammoType={_reloadingAmmoType}", this);
    }

    // Purpose: Publishes the reload progress notification.
    private void PublishReloadProgress()
    {
        if (_reloadCastBarChannel == null)
            return;

        Transform actor = EntityComponentResolver.ResolveEntityRoot(this);
        actor = actor != null ? actor : transform;

        _reloadCastBarChannel.RaiseEvent(new InteractionCastBarSnapshot(
            true,
            InteractionType.Reload,
            actor,
            actor,
            Reload01,
            Mathf.Max(0.01f, _reloadEndTime - _reloadStartTime)));
    }

    // Purpose: Attempts to get selected aim bullet and reports whether the request succeeded.
    private bool TryGetSelectedAimBullet(out BulletSO bullet)
    {
        bullet = null;

        if (_bulletLoadout == null)
            return false;

        if (!_bulletLoadout.TryGetSelectedAmmoType(out BulletAmmoType ammoType))
            return false;

        return _bulletLoadout.TryGetActiveBullet(ammoType, out bullet) && bullet != null;
    }

    // Purpose: Updates the aim point from the current runtime state.
    private void UpdateAimPoint()
    {
        _hasAimPoint = TryGetAimPoint(out _aimWorldPoint);

        if (!_hasAimPoint)
        {
            _isAimWithinRange = false;
            return;
        }

        _isAimWithinRange = true;
    }

    // Purpose: Ensures aim camera is ready before the workflow continues.
    private void EnsureAimCamera()
    {
        if (_aimCamera != null)
            return;

        _aimCamera = Camera.main;
    }

    // Purpose: Returns the current next fire time.
    private float GetNextFireTime(BulletAmmoType ammoType)
    {
        return ResolveLane(ammoType) switch
        {
            ShootLane.Primary => _nextPrimaryFireTime,
            ShootLane.Paint => _nextPaintFireTime,
            ShootLane.Special => _nextSpecialFireTime,
            _ => _nextPrimaryFireTime
        };
    }

    // Purpose: Updates the next fire time state.
    private void SetNextFireTime(BulletAmmoType ammoType, float value)
    {
        switch (ResolveLane(ammoType))
        {
            case ShootLane.Primary:
                _nextPrimaryFireTime = value;
                break;

            case ShootLane.Paint:
                _nextPaintFireTime = value;
                break;

            case ShootLane.Special:
                _nextSpecialFireTime = value;
                break;
        }
    }

    // Purpose: Returns the current shots per second.
    private float GetShotsPerSecond(BulletAmmoType ammoType)
    {
        float fallback = _statsRuntime != null
            ? _statsRuntime.Weapon.shotsPerSecond
            : 0.01f;

        if (_statsRuntime == null || _bulletLoadout == null)
            return Mathf.Max(0.01f, fallback);

        if (!_bulletLoadout.TryGetActiveBullet(ammoType, out BulletSO bullet))
            return Mathf.Max(0.01f, fallback);

        return Mathf.Max(0.01f, _statsRuntime.ResolveShotsPerSecond(bullet));
    }

    // Purpose: Returns the current cooldown01.
    private float GetCooldown01(BulletAmmoType ammoType)
    {
        float duration = 1f / GetShotsPerSecond(ammoType);
        float nextFireTime = GetNextFireTime(ammoType);

        if (Time.time >= nextFireTime)
            return 1f;

        float start = nextFireTime - duration;
        return Mathf.Clamp01((Time.time - start) / duration);
    }

    // Purpose: Resolves the lane needed by this component.
    private static ShootLane ResolveLane(BulletAmmoType ammoType)
    {
        return ammoType switch
        {
            BulletAmmoType.Basic => ShootLane.Primary,
            BulletAmmoType.Special => ShootLane.Special,
            _ => ShootLane.Primary
        };
    }

    // Purpose: Handles the incoming bullet loadout ready request or event.
    private void HandleBulletLoadoutReady(PlayerBulletLoadoutRuntime bulletLoadout)
    {
        if (bulletLoadout == null)
            return;

        BindBulletLoadoutEvents(bulletLoadout);
    }

    // Purpose: Binds bullet loadout events to the active runtime or UI target.
    private void BindBulletLoadoutEvents(PlayerBulletLoadoutRuntime bulletLoadout)
    {
        if (_subscribedBulletLoadout == bulletLoadout)
        {
            if (bulletLoadout != null)
                _bulletLoadout = bulletLoadout;

            return;
        }

        if (_subscribedBulletLoadout != null)
        {
            _subscribedBulletLoadout.AmmoSelectionReloadRequested -=
                HandleAmmoSelectionReloadRequested;
        }

        _subscribedBulletLoadout = bulletLoadout;

        if (bulletLoadout == null)
            return;

        _bulletLoadout = bulletLoadout;
        _subscribedBulletLoadout.AmmoSelectionReloadRequested +=
            HandleAmmoSelectionReloadRequested;
    }

    // Purpose: Handles the incoming ammo selection reload requested request or event.
    private void HandleAmmoSelectionReloadRequested(BulletSO selectedBullet)
    {
        if (selectedBullet == null)
            return;

        StartReload(selectedBullet, true);
    }

    // Purpose: Responds to the reload requested callback.
    private void OnReloadRequested()
    {
        if (_bulletLoadout == null)
            return;

        if (_isReloading)
            return;

        if (!_bulletLoadout.TryGetSelectedAmmoType(out BulletAmmoType ammoType))
            return;

        RequestReload(ammoType);
    }
}
