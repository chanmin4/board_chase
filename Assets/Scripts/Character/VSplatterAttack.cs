using System;
using UnityEditor.EditorTools;
using UnityEngine;

[DisallowMultipleComponent]
public class VSplatterAttack : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private VSplatterRange _range;
    
    [SerializeField] private VSplatterWeaponHolder _weaponHolder;
    [Tooltip("bullet parent object")]
    [SerializeField] private Transform _projectilesRoot;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool debugDraw = false;
    [SerializeField] private float debugDrawDuration = 0.15f;

    [Header("AutoRef Don't Touch")]
    [SerializeField] private Camera _aimCamera;

    public event Action Fired;

    private WeaponSO CurrentWeapon => _weaponHolder != null ? _weaponHolder.CurrentWeapon : null;
    private Transform FireOrigin => _weaponHolder != null ? _weaponHolder.FireOrigin : transform;
    private Vector3 FireDirection => _weaponHolder != null ? _weaponHolder.FireDirection : FireOrigin.forward;
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

        

        Transform fireOrigin = FireOrigin != null ? FireOrigin : transform;

        bool gotAimPoint = VSplatterAimUtility.TryGetAimPoint(
            _aimCamera,
            CurrentWeapon.AimHitMask,
            CurrentWeapon.AllowFallbackPlane,
            CurrentWeapon.FallbackPlaneY,
            out Vector3 aimPoint,
            out _);
        Debug.Log($"gotAimPoint: {gotAimPoint}, aimPoint: {aimPoint}");
        if (!gotAimPoint)
            return false;

        if (!_range.IsWithinRange(aimPoint))
        {
            if (debugLogs)
                Debug.Log("[VSplatterAttack] out of range");

            return false;
        }

        Vector3 start = fireOrigin.position;

        Vector3 dir = FireDirection;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return false;

        dir.Normalize();

        Vector3 rangeBoundary = _range.RangeOrigin.position;
        rangeBoundary.y = start.y;
        rangeBoundary += dir * CurrentWeapon.MaxRange;

        float maxDistance = Vector3.Distance(Flatten(start), Flatten(rangeBoundary));

        if (debugDraw)
            Debug.DrawLine(start, rangeBoundary, Color.yellow, debugDrawDuration);
        Quaternion bulletRotation = Quaternion.LookRotation(dir, Vector3.up);
        AttackBullet bullet = Instantiate(
        bulletConfig.BulletPrefab,
        start,
        bulletRotation,
        _projectilesRoot).GetComponent<AttackBullet>();

        bullet.Init(
            dir,
            maxDistance,
            bulletConfig.Speed,
            bulletConfig.CastRadius,
            bulletConfig.MaxLifetime,
            CurrentWeapon.Damage,
            CurrentWeapon.DamageHitMask,
            bulletConfig.BlockHitMask,
            bulletConfig.TriggerInteraction,
            gameObject);

        if (debugLogs)
            Debug.Log("[VSplatterAttack] attack bullet fired.");

        Fired?.Invoke();
        return true;
    }

    private static Vector3 Flatten(Vector3 value)
    {
        value.y = 0f;
        return value;
    }
}
