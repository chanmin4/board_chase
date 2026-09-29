/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player
 * Primary responsibility: Handles PlayerAimAction camera, vision, range, or aiming calculations and presentation.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

#if false
using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerAimAction : MonoBehaviour
{
    private enum ShootLane
    {
        Primary,
        Paint,
        Special
    }

    [Header("Refs")]
    [SerializeField] private VSplatterRange _range;
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private PlayerShooterShoot _shoot;
    [SerializeField] private PlayerStatsRuntime _statsRuntime;
    [SerializeField] private PlayerBulletLoadoutRuntime _bulletLoadout;
    [SerializeField] private PlayerBulletLoadoutRuntimeReadyEventChannelSO _bulletLoadoutReadyChannel;
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
    [ReadOnly][SerializeField] private float _recoilAngle;
    [ReadOnly][SerializeField] private float _recoilForwardOffset;
    [ReadOnly][SerializeField] private float _recoilSideOffset;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = false;

    private BulletAmmoType _lastActiveAmmoType = BulletAmmoType.Basic;
    private Vector3 _aimWorldPoint;
    private bool _hasAimPoint;
    private bool _isAimWithinRange;

    private float _nextPrimaryFireTime;
    private float _nextPaintFireTime;
    private float _nextSpecialFireTime;

    private bool _isReloading;
    private bool _hasReloadingAmmoType;
    private BulletAmmoType _reloadingAmmoType;
    private float _reloadStartTime;
    private float _reloadEndTime;

    public event Action OnReloadStarted;
    public event Action OnReloadFinished;
    public event Action OnShotConsumed;
    public event Action AimStarted;
    public event Action AimEnded;

    public Vector3 AimWorldPoint => _aimWorldPoint;
    public bool HasAimPoint => _hasAimPoint;
    public bool IsAimWithinRange => _isAimWithinRange;
    public bool IsReloading => _isReloading;
    public int CurrentAmmo => 0;

    public bool IsAiming => _isAiming;
    public float Aim01 => _aim01;
    public bool IsAimComplete => _aim01 >= 0.999f;

    public float RangeMultiplier =>
        Mathf.Lerp(
            1f,
            _statsRuntime != null ? _statsRuntime.AimRangeMultiplier : 1f,
            _aim01);

    public float MoveSpeedMultiplier =>
        Mathf.Lerp(
            1f,
            _statsRuntime != null ? _statsRuntime.AimMoveSpeedMultiplier : 1f,
            _aim01);

    public Camera AimCamera => _aimCamera;
    public WeaponSO CurrentWeapon => _range != null ? _range.CurrentWeapon : null;

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

            float duration = CurrentReloadDuration;
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
    private void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
        EnsureAimCamera();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveRefs();

        if (_shoot != null)
            _shoot.Fired += OnShotExecuted;

        if (_inputReader != null)
            _inputReader.ReloadEvent += OnReloadRequested;

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
        if (_shoot != null)
            _shoot.Fired -= OnShotExecuted;

        if (_inputReader != null)
            _inputReader.ReloadEvent -= OnReloadRequested;

        if (_bulletLoadoutReadyChannel != null)
            _bulletLoadoutReadyChannel.OnEventRaised -= HandleBulletLoadoutReady;
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

        if (_isAiming && !IsAimComplete)
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

        if (_isReloading)
            return false;

        if (!_bulletLoadout.CanReloadActiveAmmo(ammoType))
            return false;

        _isReloading = true;
        _hasReloadingAmmoType = true;
        _reloadingAmmoType = ammoType;

        _reloadStartTime = Time.time;
        _reloadEndTime = Time.time + CurrentReloadDuration;

        OnReloadStarted?.Invoke();

        if (_debugLogs)
            Debug.Log($"[PlayerAimAction] Reload started. ammoType={ammoType}", this);

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

    // Purpose: Applies the requested accuracy and recoil change.
    public Vector3 ApplyAccuracyAndRecoil(Vector3 origin, Vector3 target)
    {
        Vector3 direction = target - origin;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return target;

        Vector3 flatDirection = direction.normalized;
        Vector3 right = new Vector3(flatDirection.z, 0f, -flatDirection.x);

        float hipSpreadRadius = _statsRuntime != null ? _statsRuntime.HipFireSpreadRadius : 0f;
        float aimSpreadRadius = _statsRuntime != null ? _statsRuntime.AimSpreadRadius : 0f;
        float spreadRadius = Mathf.Lerp(hipSpreadRadius, aimSpreadRadius, _aim01);

        Vector2 spread = spreadRadius > 0f
            ? UnityEngine.Random.insideUnitCircle * spreadRadius
            : Vector2.zero;

        Vector3 aimPointOffset =
            flatDirection * (_recoilForwardOffset + spread.y) +
            right * (_recoilSideOffset + spread.x);

        float hipSpread = _statsRuntime != null ? _statsRuntime.HipFireSpreadAngleDeg : 0f;
        float aimSpread = _statsRuntime != null ? _statsRuntime.AimSpreadAngleDeg : 0f;
        float angularSpread = Mathf.Lerp(hipSpread, aimSpread, _aim01);
        float randomAngularSpread = angularSpread > 0f ? UnityEngine.Random.Range(-angularSpread, angularSpread) : 0f;

        float recoilAngle = _recoilAngle > 0f
            ? UnityEngine.Random.Range(-_recoilAngle, _recoilAngle)
            : 0f;

        Quaternion rotation = Quaternion.AngleAxis(randomAngularSpread + recoilAngle, Vector3.up);
        Vector3 adjustedDirection = rotation * flatDirection;

        return origin + adjustedDirection * direction.magnitude + aimPointOffset;
    }

    // Purpose: Applies the requested shot recoil change.
    public void ApplyShotRecoil()
    {
        float recoil = _statsRuntime != null ? _statsRuntime.RecoilAngleDeg : 0f;
        _recoilAngle = Mathf.Max(_recoilAngle, Mathf.Max(0f, recoil));

        float maxDistance = _statsRuntime != null ? _statsRuntime.MaxRecoilDistance : 0f;

        if (maxDistance <= 0f)
            return;

        float forward = _statsRuntime != null ? _statsRuntime.RecoilForwardDistancePerShot : 0f;
        float side = _statsRuntime != null ? _statsRuntime.RecoilSideDistancePerShot : 0f;

        _recoilForwardOffset = Mathf.Min(
            maxDistance,
            _recoilForwardOffset + Mathf.Max(0f, forward));

        _recoilSideOffset = Mathf.Clamp(
            _recoilSideOffset + UnityEngine.Random.Range(-side, side),
            -maxDistance,
            maxDistance);
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_range == null)
            _range = GetComponent<VSplatterRange>();

        if (_inputReader == null)
            _inputReader = FindAnyObjectByType<InputReader>();

        if (_shoot == null)
            _shoot = GetComponent<PlayerShooterShoot>();

        if (_statsRuntime == null)
            _statsRuntime = GetComponent<PlayerStatsRuntime>();

        if (_bulletLoadout == null)
            _bulletLoadout = GetComponent<PlayerBulletLoadoutRuntime>();
    }

    // Purpose: Updates the aim state from the current runtime state.
    private void UpdateAimState()
    {
        bool nextIsAiming =
            !GameplayActionInputBlocker.IsAimBlocked &&
            _pollRightMouseButton &&
            Mouse.current != null &&
            Mouse.current.rightButton.isPressed;

        SetAiming(nextIsAiming);

        float speed = _statsRuntime != null ? _statsRuntime.AimSpeed : 8f;
        float target = _isAiming ? 1f : 0f;

        _aim01 = Mathf.MoveTowards(
            _aim01,
            target,
            Mathf.Max(0.01f, speed) * Time.deltaTime);

        float recovery = _statsRuntime != null
            ? _statsRuntime.RecoilRecoverySpeedDegPerSecond
            : 20f;

        _recoilAngle = Mathf.MoveTowards(
            _recoilAngle,
            0f,
            Mathf.Max(0f, recovery) * Time.deltaTime);

        float distanceRecovery = _statsRuntime != null
            ? _statsRuntime.RecoilDistanceRecoveryPerSecond
            : 0f;

        float recoveryStep = Mathf.Max(0f, distanceRecovery) * Time.deltaTime;

        _recoilForwardOffset = Mathf.MoveTowards(_recoilForwardOffset, 0f, recoveryStep);
        _recoilSideOffset = Mathf.MoveTowards(_recoilSideOffset, 0f, recoveryStep);
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
            Debug.Log($"[PlayerAimAction] {ammoType} shot cooldown started.", this);
    }

    // Purpose: Updates the reload state from the current runtime state.
    private void UpdateReloadState()
    {
        if (!_isReloading)
            return;

        if (Time.time < _reloadEndTime)
            return;

        _isReloading = false;

        if (_bulletLoadout != null && _hasReloadingAmmoType)
            _bulletLoadout.ReloadActiveAmmo(_reloadingAmmoType);

        _hasReloadingAmmoType = false;

        OnReloadFinished?.Invoke();

        if (_debugLogs)
            Debug.Log($"[PlayerAimAction] Reload finished. ammoType={_reloadingAmmoType}", this);
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

        _isAimWithinRange = _range != null && _range.IsWithinRange(_aimWorldPoint);
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

        _bulletLoadout = bulletLoadout;
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
#endif
