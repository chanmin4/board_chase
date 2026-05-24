using System;
using UnityEngine;

[DisallowMultipleComponent]
public class VSplatterAimAction : MonoBehaviour
{
    public enum FireKind
    {
        Attack,
        Paint
    }

    [Header("Refs")]
    [SerializeField] private VSplatterRange _range;
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private VSplatterAttack _attack;
    [SerializeField] private VSplatterPaint _paint;
    [SerializeField] private PlayerStatsRuntime _statsRuntime;
    [SerializeField] private PlayerBulletLoadoutRuntime _bulletLoadout;
    [SerializeField] private PlayerBulletLoadoutRuntimeReadyEventChannelSO _bulletLoadoutReadyChannel;
    [SerializeField] private VSplatter_Character _character;
    [Header("Auto Refs Don't Touch")]
    [SerializeField] private Camera _aimCamera;
    

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = false;
    private FireKind _lastActiveFireKind = FireKind.Attack;
    private Vector3 _aimWorldPoint;
    private bool _hasAimPoint;
    private bool _isAimWithinRange;

    private float _nextAttackFireTime;
    private float _nextPaintFireTime;

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

    private bool IsReloadModifierHeld =>
        _inputReader != null && _inputReader.ReloadInputHeld;

    private float CurrentAttackShotsPerSecond =>
        _statsRuntime != null ? Mathf.Max(0.01f, _statsRuntime.Weapon.attackShotsPerSecond) : 0.01f;

    private float CurrentPaintShotsPerSecond =>
        _statsRuntime != null ? Mathf.Max(0.01f, _statsRuntime.Weapon.paintShotsPerSecond) : 0.01f;

    private float CurrentReloadDuration =>
        _statsRuntime != null ? Mathf.Max(0.01f, _statsRuntime.Weapon.reloadDurationSeconds) : 0.01f;

    public bool CanFireNow => CanFireNowFor(FireKind.Attack);

    public bool CanFireNowFor(FireKind kind)
    {
        _lastActiveFireKind = kind;
        if (CurrentWeapon == null)
            return false;

        if (_bulletLoadout == null)
            return false;

        BulletAmmoType ammoType = ToAmmoType(kind);

        if (IsReloadModifierHeld)
        {
            RequestReload(ammoType);
            return false;
        }

        if (_isReloading)
            return false;

        if (!_bulletLoadout.HasLoadedAmmo(ammoType))
        {
            RequestReload(ammoType);
            return false;
        }

        if (!_hasAimPoint)
            return false;

        return Time.time >= GetNextFireTime(kind);
    }

    public bool IsOnFireCooldown =>
        Time.time < _nextAttackFireTime || Time.time < _nextPaintFireTime;

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

    public float Cooldown01 => GetCooldown01(_lastActiveFireKind);

    public float ActiveProgress01
    {
        get
        {
            if (_isReloading)
                return Reload01;

            FireKind kind = _lastActiveFireKind;

            if (Time.time < GetNextFireTime(kind))
                return GetCooldown01(kind);

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

        if (_statsRuntime == null)
            _statsRuntime = GetComponent<PlayerStatsRuntime>();

        if (_character == null)
            _character = GetComponent<VSplatter_Character>();
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

        if (_statsRuntime == null)
            _statsRuntime = GetComponent<PlayerStatsRuntime>();

        if (_character == null)
            _character = GetComponent<VSplatter_Character>();

        EnsureAimCamera();
    }

    private void OnEnable()
    {
        if (_attack != null)
            _attack.Fired += OnAttackShotExecuted;

        if (_paint != null)
            _paint.Fired += OnPaintShotExecuted;

        if (_inputReader != null)
            _inputReader.SpecialShotEvent += OnMiddleClickRequested;

        if (_bulletLoadoutReadyChannel != null)
        {
            _bulletLoadoutReadyChannel.OnEventRaised += HandleBulletLoadoutReady;

            if (_bulletLoadoutReadyChannel.HasCurrent)
                HandleBulletLoadoutReady(_bulletLoadoutReadyChannel.Current);
        }
    }

    private void OnDisable()
    {
        if (_attack != null)
            _attack.Fired -= OnAttackShotExecuted;

        if (_paint != null)
            _paint.Fired -= OnPaintShotExecuted;

        if (_inputReader != null)
            _inputReader.SpecialShotEvent -= OnMiddleClickRequested;

        if (_bulletLoadoutReadyChannel != null)
            _bulletLoadoutReadyChannel.OnEventRaised -= HandleBulletLoadoutReady;
    }

    private void Update()
    {
        EnsureAimCamera();
        UpdateAimPoint();
        UpdateReloadState();
    }

    private void OnMiddleClickRequested()
    {
        if (!IsReloadModifierHeld)
            return;

        RequestReload(BulletAmmoType.Special);
    }

    private void OnAttackShotExecuted()
    {
        OnShotExecuted(FireKind.Attack);
    }

    private void OnPaintShotExecuted()
    {
        OnShotExecuted(FireKind.Paint);
    }

    private void OnShotExecuted(FireKind kind)
    {
        _lastActiveFireKind = kind;
        if (CurrentWeapon == null)
            return;

        float shotsPerSecond = GetShotsPerSecond(kind);
        SetNextFireTime(kind, Time.time + (1f / shotsPerSecond));

        OnShotConsumed?.Invoke();

        if (_debugLogs)
            Debug.Log($"[VSplatterAimAction] {kind} shot cooldown started.");
    }

    public bool RequestReloadFor(FireKind kind)
    {
        return RequestReload(ToAmmoType(kind));
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

    private float GetNextFireTime(FireKind kind)
    {
        return kind == FireKind.Paint
            ? _nextPaintFireTime
            : _nextAttackFireTime;
    }

    private float GetShotsPerSecond(FireKind kind)
    {
        return kind == FireKind.Paint
            ? CurrentPaintShotsPerSecond
            : CurrentAttackShotsPerSecond;
    }

    private void SetNextFireTime(FireKind kind, float value)
    {
        if (kind == FireKind.Paint)
            _nextPaintFireTime = value;
        else
            _nextAttackFireTime = value;
    }

    private FireKind GetActiveFireKind()
    {
        if (_character != null && _character.paintInput)
            return FireKind.Paint;

        return FireKind.Attack;
    }

    private float GetCooldown01(FireKind kind)
    {
        float duration = 1f / GetShotsPerSecond(kind);
        float nextFireTime = GetNextFireTime(kind);

        if (Time.time >= nextFireTime)
            return 1f;

        float start = nextFireTime - duration;
        return Mathf.Clamp01((Time.time - start) / duration);
    }

    private static BulletAmmoType ToAmmoType(FireKind kind)
    {
        return kind == FireKind.Paint
            ? BulletAmmoType.Paint
            : BulletAmmoType.Attack;
    }

    private void HandleBulletLoadoutReady(PlayerBulletLoadoutRuntime bulletLoadout)
    {
        if (bulletLoadout == null)
            return;

        _bulletLoadout = bulletLoadout;
    }
}