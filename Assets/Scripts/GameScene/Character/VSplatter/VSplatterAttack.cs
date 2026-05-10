using System;
using UnityEngine;

[DisallowMultipleComponent]
public class VSplatterAttack : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private VSplatterRange _range;
    [SerializeField] private VSplatterWeaponHolder _weaponHolder;
    [SerializeField] private PlayerStatsRuntime _statsRuntime;
    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool debugDraw = false;
    [SerializeField] private float debugDrawDuration = 0.15f;

    [Header("AutoRef Don't Touch")]
    [SerializeField] private Camera _aimCamera;

    public event Action Fired;

    private WeaponSO CurrentWeapon => _weaponHolder != null ? _weaponHolder.CurrentWeapon : null;
    private Transform GameplayFireOrigin => _weaponHolder != null ? _weaponHolder.GameplayFireOrigin : transform;
    private Transform VisualFireOrigin => _weaponHolder != null ? _weaponHolder.VisualFireOrigin : null;
    private Transform ProjectilesRoot => _weaponHolder != null ? _weaponHolder.ProjectilesRoot : null;
    private float CurrentMaxRange =>
    _statsRuntime != null
        ? Mathf.Max(0.1f, _statsRuntime.Weapon.maxRange)
        : CurrentWeapon != null ? CurrentWeapon.MaxRange : 0f;

    private float CurrentDamage =>
        _statsRuntime != null
            ? Mathf.Max(0f, _statsRuntime.Weapon.attackDamage)
            : CurrentWeapon != null ? CurrentWeapon.Damage : 0f;
    private void Reset()
    {
        if (_range == null)
            _range = GetComponent<VSplatterRange>();

        if (_weaponHolder == null)
            _weaponHolder = GetComponent<VSplatterWeaponHolder>();

        if (_aimCamera == null)
            _aimCamera = Camera.main;
        if (_statsRuntime == null)
            _statsRuntime = GetComponent<PlayerStatsRuntime>();
    }

    private void Awake()
    {
        if (_range == null)
            _range = GetComponent<VSplatterRange>();

        if (_weaponHolder == null)
            _weaponHolder = GetComponent<VSplatterWeaponHolder>();

        if (_aimCamera == null)
            _aimCamera = Camera.main;
        if (_statsRuntime == null)
            _statsRuntime = GetComponent<PlayerStatsRuntime>();
    }

    public bool TryFireOnce()
    {
        float maxRange = CurrentMaxRange;
        float damage = CurrentDamage;       
        if (_range == null || !_range.HasValidWeapon() || CurrentWeapon == null)
            return false;

        AttackBulletSO bulletConfig = CurrentWeapon.AttackBullet;
        if (bulletConfig == null || bulletConfig.BulletPrefab == null)
            return false;

        bool gotAimPoint = VSplatterAimUtility.TryGetAimPoint(
            _aimCamera,
            CurrentWeapon.AimHitMask,
            CurrentWeapon.AllowFallbackPlane,
            CurrentWeapon.FallbackPlaneY,
            transform,
            out Vector3 aimPoint,
            out _);

        if (!gotAimPoint)
            return false;

        Ray aimRay = _aimCamera.ScreenPointToRay(Input.mousePosition);

        Transform fireOrigin = VisualFireOrigin != null
            ? VisualFireOrigin
            : GameplayFireOrigin != null ? GameplayFireOrigin : transform;

        Vector3 visualStart = fireOrigin.position;

        if (!VSplatterAimUtility.TryGetPointOnYPlane(
                aimRay,
                visualStart.y,
                out Vector3 visualAimPoint))
        {
            return false;
        }

        Vector3 visualDirection = visualAimPoint - visualStart;
        visualDirection.y = 0f;

        if (visualDirection.sqrMagnitude < 0.0001f)
            return false;

        visualDirection.Normalize();

        Vector3 gameplayDirection = visualDirection;

        Vector3 rangeOrigin = _range.RangeOrigin != null
            ? _range.RangeOrigin.position
            : transform.position;

        Vector3 gameplayStart = visualStart + gameplayDirection * bulletConfig.SpawnOffset;
        Vector3 visualSpawn = visualStart + visualDirection * bulletConfig.SpawnOffset;

        if (!VSplatterAimUtility.TryGetFlatCircleExitDistance(
                gameplayStart,
                gameplayDirection,
                rangeOrigin,
                maxRange,
                out float maxDistance))
        {
            return false;
        }

        Quaternion bulletRotation = Quaternion.LookRotation(visualDirection, Vector3.up);

        AttackBullet bullet = Instantiate(
            bulletConfig.BulletPrefab,
            visualSpawn,
            bulletRotation,
            ProjectilesRoot).GetComponent<AttackBullet>();

        bullet.Init(
            gameplayStart,
            gameplayDirection,
            visualDirection,
            maxDistance,
            bulletConfig.Speed,
            bulletConfig.CastRadius,
            bulletConfig.MaxLifetime,
            damage,
            bulletConfig.DamageTargetMask,
            bulletConfig.ImpactMask,
            bulletConfig.TriggerInteraction,
            gameObject);

        if (debugDraw)
            Debug.DrawLine(visualSpawn, visualSpawn + visualDirection * maxDistance, Color.yellow, debugDrawDuration);

        if (debugLogs)
            Debug.Log("[VSplatterAttack] attack bullet fired.");

        Fired?.Invoke();
        return true;
    }


}
