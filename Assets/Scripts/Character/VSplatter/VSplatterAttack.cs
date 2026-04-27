using System;
using UnityEngine;

[DisallowMultipleComponent]
public class VSplatterAttack : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private VSplatterRange _range;
    [SerializeField] private VSplatterWeaponHolder _weaponHolder;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool debugDraw = false;
    [SerializeField] private float debugDrawDuration = 0.15f;

    [Header("AutoRef Don't Touch")]
    [SerializeField] private Camera _aimCamera;

    public event Action Fired;

    private WeaponSO CurrentWeapon => _weaponHolder != null ? _weaponHolder.CurrentWeapon : null;
    private Transform GameplayFireOrigin => _weaponHolder != null ? _weaponHolder.GameplayFireOrigin : transform;
    private Transform ProjectilesRoot => _weaponHolder != null ? _weaponHolder.ProjectilesRoot : null;

    private void Reset()
    {
        if (_range == null)
            _range = GetComponent<VSplatterRange>();

        if (_weaponHolder == null)
            _weaponHolder = GetComponent<VSplatterWeaponHolder>();

        if (_aimCamera == null)
            _aimCamera = Camera.main;
    }

    private void Awake()
    {
        if (_range == null)
            _range = GetComponent<VSplatterRange>();

        if (_weaponHolder == null)
            _weaponHolder = GetComponent<VSplatterWeaponHolder>();

        if (_aimCamera == null)
            _aimCamera = Camera.main;
    }

    public bool TryFireOnce()
    {
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
            out Vector3 aimPoint,
            out _);

        if (!gotAimPoint)
            return false;

        if (!_range.IsWithinRange(aimPoint))
        {
            if (debugLogs)
                Debug.Log("[VSplatterAttack] out of range");

            return false;
        }

        Transform fireOrigin = GameplayFireOrigin != null ? GameplayFireOrigin : transform;

        Vector3 dir = fireOrigin.forward;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return false;

        dir.Normalize();

        Vector3 start = fireOrigin.position + dir * bulletConfig.SpawnOffset;

        float maxDistance = CurrentWeapon.MaxRange;
        Quaternion bulletRotation = Quaternion.LookRotation(dir, Vector3.up);

        AttackBullet bullet = Instantiate(
            bulletConfig.BulletPrefab,
            start,
            bulletRotation,
            ProjectilesRoot).GetComponent<AttackBullet>();

        bullet.Init(
            dir,
            maxDistance,
            bulletConfig.Speed,
            bulletConfig.CastRadius,
            bulletConfig.MaxLifetime,
            CurrentWeapon.Damage,
            bulletConfig.DamageTargetMask,
            bulletConfig.ImpactMask,
            bulletConfig.TriggerInteraction,
            bulletConfig.UseFlatDamageHit,
            bulletConfig.FlatHitHalfHeight,
            gameObject);

        if (debugDraw)
            Debug.DrawLine(start, start + dir * maxDistance, Color.yellow, debugDrawDuration);

        if (debugLogs)
            Debug.Log("[VSplatterAttack] attack bullet fired.");

        Fired?.Invoke();
        return true;
    }

}
