using System;
using UnityEngine;

[DisallowMultipleComponent]
public class VSplatterAimAction : MonoBehaviour
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
    [SerializeField] private VSplatterShoot _shoot;
    [SerializeField] private PlayerStatsRuntime _statsRuntime;
    [SerializeField] private PlayerBulletLoadoutRuntime _bulletLoadout;
    [SerializeField] private PlayerBulletLoadoutRuntimeReadyEventChannelSO _bulletLoadoutReadyChannel;

    [Header("Auto Refs Don't Touch")]
    [SerializeField] private Camera _aimCamera;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = false;

    private BulletAmmoType _lastActiveAmmoType = BulletAmmoType.AttackAndPaint;
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

    public Vector3 AimWorldPoint => _aimWorldPoint;
    public bool HasAimPoint => _hasAimPoint;
    public bool IsAimWithinRange => _isAimWithinRange;
    public bool IsReloading => _isReloading;
    public int CurrentAmmo => 0;

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

    public bool CanFireNowFor(BulletAmmoType ammoType)
    {
        _lastActiveAmmoType = ammoType;

        if (CurrentWeapon == null)
            return false;

        if (_bulletLoadout == null)
            return false;

        if (_isReloading)
            return false;

        if (!_bulletLoadout.HasLoadedAmmo(ammoType))
        {
            RequestReload(ammoType);
            return false;
        }

        if (!_hasAimPoint)
            return false;

        return Time.time >= GetNextFireTime(ammoType);
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

    private void Reset()
    {
        if (_range == null)
            _range = GetComponent<VSplatterRange>();

        if (_inputReader == null)
            _inputReader = FindAnyObjectByType<InputReader>();

        if (_shoot == null)
            _shoot = GetComponent<VSplatterShoot>();

        if (_statsRuntime == null)
            _statsRuntime = GetComponent<PlayerStatsRuntime>();
    }

    private void Awake()
    {
        if (_range == null)
            _range = GetComponent<VSplatterRange>();

        if (_inputReader == null)
            _inputReader = FindAnyObjectByType<InputReader>();

        if (_shoot == null)
            _shoot = GetComponent<VSplatterShoot>();

        if (_statsRuntime == null)
            _statsRuntime = GetComponent<PlayerStatsRuntime>();

        EnsureAimCamera();
    }

    private void OnEnable()
    {
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

    private void OnDisable()
    {
        if (_shoot != null)
            _shoot.Fired -= OnShotExecuted;


        if (_inputReader != null)
            _inputReader.ReloadEvent -= OnReloadRequested;
        if (_bulletLoadoutReadyChannel != null)
            _bulletLoadoutReadyChannel.OnEventRaised -= HandleBulletLoadoutReady;
    }

    private void Update()
    {
        if (GamePause.IsPaused || Time.timeScale <= 0f)
        {
            _hasAimPoint = false;
            _isAimWithinRange = false;
            return;
        }

        EnsureAimCamera();
        UpdateAimPoint();
        UpdateReloadState();
    }


    private void OnShotExecuted(BulletAmmoType ammoType)
    {
        _lastActiveAmmoType = ammoType;

        if (CurrentWeapon == null)
            return;

        float shotsPerSecond = GetShotsPerSecond(ammoType);
        SetNextFireTime(ammoType, Time.time + (1f / shotsPerSecond));

        OnShotConsumed?.Invoke();

        if (_debugLogs)
            Debug.Log($"[VSplatterAimAction] {ammoType} shot cooldown started.");
    }

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
            Debug.Log($"[VSplatterAimAction] Reload started. ammoType={ammoType}");

        return true;
    }

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
            Debug.Log($"[VSplatterAimAction] Reload finished. ammoType={_reloadingAmmoType}");
    }

    public bool TryGetAimPoint(out Vector3 worldPoint)
    {
        worldPoint = default;

        if (_aimCamera == null || CurrentWeapon == null)
            return false;

        return VSplatterAimUtility.TryGetAimPoint(
            _aimCamera,
            CurrentWeapon.AimHitMask,
            CurrentWeapon.AllowFallbackPlane,
            CurrentWeapon.FallbackPlaneY,
            transform,
            out worldPoint,
            out _);
    }

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

    private void EnsureAimCamera()
    {
        if (_aimCamera != null)
            return;

        _aimCamera = Camera.main;
    }

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

    private float GetShotsPerSecond(BulletAmmoType ammoType)
    {
        float fallback = ResolveLane(ammoType) == ShootLane.Paint
            ? _statsRuntime != null ? _statsRuntime.Weapon.paintShotsPerSecond : 0.01f
            : _statsRuntime != null ? _statsRuntime.Weapon.attackShotsPerSecond : 0.01f;

        if (_statsRuntime == null || _bulletLoadout == null)
            return Mathf.Max(0.01f, fallback);

        if (!_bulletLoadout.TryGetActiveBullet(ammoType, out BulletSO bullet))
            return Mathf.Max(0.01f, fallback);

        return Mathf.Max(0.01f, _statsRuntime.ResolveShotsPerSecond(bullet));
    }

    private float GetCooldown01(BulletAmmoType ammoType)
    {
        float duration = 1f / GetShotsPerSecond(ammoType);
        float nextFireTime = GetNextFireTime(ammoType);

        if (Time.time >= nextFireTime)
            return 1f;

        float start = nextFireTime - duration;
        return Mathf.Clamp01((Time.time - start) / duration);
    }

    private static ShootLane ResolveLane(BulletAmmoType ammoType)
    {
        return ammoType switch
        {
            BulletAmmoType.AttackAndPaint => ShootLane.Primary,
            BulletAmmoType.Attack => ShootLane.Primary,
            BulletAmmoType.Paint => ShootLane.Paint,
            BulletAmmoType.Special => ShootLane.Special,
            _ => ShootLane.Primary
        };
    }

    private void HandleBulletLoadoutReady(PlayerBulletLoadoutRuntime bulletLoadout)
    {
        if (bulletLoadout == null)
            return;

        _bulletLoadout = bulletLoadout;
    }
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