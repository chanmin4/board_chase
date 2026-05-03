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
    private Transform VisualFireOrigin => _weaponHolder != null ? _weaponHolder.VisualFireOrigin : null;
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
            transform,
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

        Vector3 rangeOrigin = _range.RangeOrigin != null
            ? _range.RangeOrigin.position
            : transform.position;

        Vector3 rangeDirection = aimPoint - rangeOrigin;
        rangeDirection.y = 0f;

        if (rangeDirection.sqrMagnitude < 0.0001f)
            return false;

        rangeDirection.Normalize();

        Vector3 rangeEndPoint = rangeOrigin + rangeDirection * CurrentWeapon.MaxRange;
        rangeEndPoint.y = aimPoint.y;

        Transform fireOrigin = VisualFireOrigin != null
            ? VisualFireOrigin
            : GameplayFireOrigin != null ? GameplayFireOrigin : transform;

        Vector3 visualStart = fireOrigin.position;

        Vector3 visualDirection = rangeEndPoint - visualStart;
        if (visualDirection.sqrMagnitude < 0.0001f)
            return false;

        visualDirection.Normalize();

        Vector3 gameplayDirection = rangeEndPoint - visualStart;
        gameplayDirection.y = 0f;

        if (gameplayDirection.sqrMagnitude < 0.0001f)
            return false;

        gameplayDirection.Normalize();

        Vector3 gameplayStart = visualStart + gameplayDirection * bulletConfig.SpawnOffset;
        Vector3 visualSpawn = visualStart + visualDirection * bulletConfig.SpawnOffset;

        float maxDistance = Vector3.Distance(
            new Vector3(gameplayStart.x, 0f, gameplayStart.z),
            new Vector3(rangeEndPoint.x, 0f, rangeEndPoint.z));

        if (maxDistance <= 0.01f)
            return false;

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
            CurrentWeapon.Damage,
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
