using System;
using UnityEngine;

[DisallowMultipleComponent]
public class VSplatterAimAction : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private VSplatterRange _range;
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private VSplatterAttack _attack;
    [SerializeField] private VSplatterPaint _paint;
    [Header("Broadcasting")]
    [SerializeField] private WeaponAmmoEventChannelSO _weaponAmmoEventChannel;

    [Header("Listening")]
    [SerializeField] private VoidEventChannelSO _requestWeaponAmmoSnapshotChannel;

    [Header("Auto Refs Don't Touch")]
    [SerializeField] private Camera _aimCamera;

    [Header("Options")]
    
    [SerializeField] private bool _autoReloadOnEmpty = true;
    [SerializeField] private bool _debugLogs = false;


    private Vector3 _aimWorldPoint;
    private bool _hasAimPoint;
    private bool _isAimWithinRange;

    private float _nextFireTime;
    private bool _isReloading;
    private float _reloadStartTime;
    private float _reloadEndTime;
    private int _currentAmmo;

    private WeaponSO _cachedWeapon;

    public event Action OnReloadStarted;
    public event Action OnReloadFinished;
    public event Action OnShotConsumed;

    public Vector3 AimWorldPoint => _aimWorldPoint;
    public bool HasAimPoint => _hasAimPoint;
    public bool IsAimWithinRange => _isAimWithinRange;
    public bool IsReloading => _isReloading;
    public bool IsOnFireCooldown => Time.time < _nextFireTime;
    public int CurrentAmmo => _currentAmmo;
    public Camera AimCamera => _aimCamera;
    public WeaponSO CurrentWeapon => _range != null ? _range.CurrentWeapon : null;

    private float CurrentShotsPerSecond =>
        CurrentWeapon != null ? Mathf.Max(0.01f, CurrentWeapon.ShotsPerSecond) : 0.01f;

    private int CurrentMagazineSize =>
        CurrentWeapon != null ? Mathf.Max(1, CurrentWeapon.MagazineSize) : 1;

    private float CurrentReloadDuration =>
        CurrentWeapon != null ? Mathf.Max(0.01f, CurrentWeapon.ReloadDuration) : 0.01f;

    public bool CanFireNow =>
        CurrentWeapon != null &&
        _hasAimPoint &&
        _isAimWithinRange &&
        !_isReloading &&
        _currentAmmo > 0 &&
        Time.time >= _nextFireTime;

    public float Cooldown01
    {
        get
        {
            float duration = 1f / CurrentShotsPerSecond;
            if (Time.time >= _nextFireTime)
                return 1f;

            float start = _nextFireTime - duration;
            return Mathf.Clamp01((Time.time - start) / duration);
        }
    }

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

    public float ActiveProgress01
    {
        get
        {
            if (_isReloading)
                return Reload01;

            if (Time.time < _nextFireTime)
                return Cooldown01;

            return 1f;
        }
    }

    private void Reset()
    {
        if (_range == null)
            _range = GetComponent<VSplatterRange>();

        if (_inputReader == null)
            _inputReader = FindAnyObjectByType<InputReader>();

        if (_attack == null)
            _attack = GetComponent<VSplatterAttack>();

        if (_paint == null)
            _paint = GetComponent<VSplatterPaint>();
    }

    private void Awake()
    {
        if (_range == null)
            _range = GetComponent<VSplatterRange>();

        if (_inputReader == null)
            _inputReader = FindAnyObjectByType<InputReader>();

        if (_attack == null)
            _attack = GetComponent<VSplatterAttack>();

        if (_paint == null)
            _paint = GetComponent<VSplatterPaint>();

        EnsureAimCamera();

        _cachedWeapon = CurrentWeapon;
        _currentAmmo = CurrentMagazineSize;
    }

    private void OnEnable()
    {
        if (_inputReader != null)
            _inputReader.ReloadEvent += OnReloadRequested;

        if (_attack != null)
            _attack.Fired += OnShotExecuted;

        if (_paint != null)
            _paint.Fired += OnShotExecuted;
        if (_requestWeaponAmmoSnapshotChannel != null)
        _requestWeaponAmmoSnapshotChannel.OnEventRaised += PublishAmmoSnapshot;

        PublishAmmoSnapshot();
    }

    private void OnDisable()
    {
        if (_inputReader != null)
            _inputReader.ReloadEvent -= OnReloadRequested;

        if (_attack != null)
            _attack.Fired -= OnShotExecuted;

        if (_paint != null)
            _paint.Fired -= OnShotExecuted;
        if (_requestWeaponAmmoSnapshotChannel != null)
            _requestWeaponAmmoSnapshotChannel.OnEventRaised -= PublishAmmoSnapshot;
    }

    private void Update()
    {
        EnsureAimCamera();
        SyncWeaponState();
        UpdateAimPoint();
        UpdateReloadState();
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

    public bool RequestReload()
    {
        if (CurrentWeapon == null)
            return false;

        if (_isReloading)
            return false;

        if (_currentAmmo >= CurrentMagazineSize)
            return false;

        _isReloading = true;
        _reloadStartTime = Time.time;
        _reloadEndTime = Time.time + CurrentReloadDuration;

        OnReloadStarted?.Invoke();
        PublishAmmoSnapshot();
        if (_debugLogs)
            Debug.Log("[VSplatterAimAction] Reload started.");

        return true;
    }

    public void ForceRefillAmmo()
    {
        _isReloading = false;
        _currentAmmo = CurrentMagazineSize;
        _reloadStartTime = 0f;
        _reloadEndTime = 0f;

        OnReloadFinished?.Invoke();
        PublishAmmoSnapshot();
    }

    private void OnReloadRequested()
    {
        RequestReload();
    }

    private void OnShotExecuted()
    {
        if (_isReloading || _currentAmmo <= 0 || CurrentWeapon == null)
            return;

        _currentAmmo = Mathf.Max(0, _currentAmmo - 1);
        _nextFireTime = Time.time + (1f / CurrentShotsPerSecond);

        OnShotConsumed?.Invoke();
        PublishAmmoSnapshot();

        if (_debugLogs)
            Debug.Log($"[VSplatterAimAction] Shot consumed. Ammo={_currentAmmo}/{CurrentMagazineSize}");

        if (_autoReloadOnEmpty && _currentAmmo <= 0)
            RequestReload();
    }

    private void SyncWeaponState()
    {
        if (_cachedWeapon != CurrentWeapon)
        {
            _cachedWeapon = CurrentWeapon;
            _currentAmmo = CurrentMagazineSize;
            _isReloading = false;
            _reloadStartTime = 0f;
            _reloadEndTime = 0f;
            PublishAmmoSnapshot();
        }

        _currentAmmo = Mathf.Clamp(_currentAmmo, 0, CurrentMagazineSize);
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

    private void UpdateReloadState()
    {
        if (!_isReloading)
            return;

        if (Time.time < _reloadEndTime)
            return;

        _isReloading = false;
        _currentAmmo = CurrentMagazineSize;

        OnReloadFinished?.Invoke();
        PublishAmmoSnapshot();

        if (_debugLogs)
            Debug.Log("[VSplatterAimAction] Reload finished.");
    }

    private void EnsureAimCamera()
    {
        if (_aimCamera != null)
            return;

        _aimCamera = Camera.main;
    }
    private void PublishAmmoSnapshot()
    {
        if (_weaponAmmoEventChannel == null)
            return;

        _weaponAmmoEventChannel.RaiseEvent(new WeaponAmmoSnapshot(
            CurrentWeapon,
            _currentAmmo,
            CurrentMagazineSize,
            _isReloading,
            Reload01));
    }
}
