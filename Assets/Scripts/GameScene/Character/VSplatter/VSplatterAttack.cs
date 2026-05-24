using System;
using UnityEngine;

[DisallowMultipleComponent]
public class VSplatterAttack : MonoBehaviour
{
    [Header("Need Ref")]
    [SerializeField] private VSplatterRange _range;
    [SerializeField] private VSplatterWeaponHolder _weaponHolder;
    [SerializeField] private PlayerStatsRuntime _statsRuntime;
    [SerializeField] private PlayerBulletLoadoutRuntime _bulletLoadout;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool debugDraw = false;
    [SerializeField] private float debugDrawDuration = 0.15f;

    [Header("Don't Touch Ref Auto")]
    [SerializeField] private Camera _aimCamera;

    public event Action Fired;

    private WeaponSO CurrentWeapon => _weaponHolder != null ? _weaponHolder.CurrentWeapon : null;

    private Transform GameplayFireOrigin =>
        _weaponHolder != null ? _weaponHolder.GameplayFireOrigin : transform;

    private Transform VisualFireOrigin =>
        _weaponHolder != null ? _weaponHolder.VisualFireOrigin : null;

    private Transform ProjectilesRoot =>
        _weaponHolder != null ? _weaponHolder.ProjectilesRoot : null;

    private float CurrentMaxRange =>
        _statsRuntime != null
            ? Mathf.Max(0.1f, _statsRuntime.Weapon.maxRange)
            : 12f;

    private void Reset()
    {
        ResolveRefs();
    }

    private void Awake()
    {
        ResolveRefs();
    }

    private void ResolveRefs()
    {
        if (_range == null)
            _range = GetComponent<VSplatterRange>();

        if (_weaponHolder == null)
            _weaponHolder = GetComponent<VSplatterWeaponHolder>();

        if (_statsRuntime == null)
            _statsRuntime = GetComponent<PlayerStatsRuntime>();

        if (_bulletLoadout == null)
            _bulletLoadout = GetComponent<PlayerBulletLoadoutRuntime>();

        if (_aimCamera == null)
            _aimCamera = Camera.main;
    }

    public bool TryFireOnce()
    {
        if (_range == null || !_range.HasValidWeapon() || CurrentWeapon == null)
            return false;

        if (_bulletLoadout == null)
            return false;

        if (!_bulletLoadout.TryGetActiveAttackBullet(out AttackBulletSO bulletConfig))
            return false;

        if (bulletConfig == null || bulletConfig.BulletPrefab == null)
            return false;

        bool gotAimPoint = VSplatterAimUtility.TryGetAimPoint(
            _aimCamera,
            CurrentWeapon.AimHitMask,
            CurrentWeapon.AllowFallbackPlane,
            CurrentWeapon.FallbackPlaneY,
            transform,
            out _,
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
                CurrentMaxRange,
                out float maxDistance))
        {
            return false;
        }

        if (!_bulletLoadout.TryConsumeAttackAmmo(1, out bulletConfig))
            return false;

        if (bulletConfig == null || bulletConfig.BulletPrefab == null)
            return false;

        float damage = _statsRuntime != null
            ? _statsRuntime.ResolveAttackDamage(bulletConfig)
            : 0f;

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
            Debug.DrawLine(
                visualSpawn,
                visualSpawn + visualDirection * maxDistance,
                Color.yellow,
                debugDrawDuration);

        if (debugLogs)
            Debug.Log("[VSplatterAttack] attack bullet fired.");

        Fired?.Invoke();
        return true;
    }
}