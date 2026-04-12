using System;
using UnityEngine;

[DisallowMultipleComponent]
public class VSplatterAttack : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private VSplatterRange _range;
    [SerializeField] private Transform _fireOrigin;
    [SerializeField] private VSplatterWeaponHolder _weaponHolder;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool debugDraw = false;
    [SerializeField] private float debugDrawDuration = 0.15f;

    [Header("AutoRef Don't Touch")]
    [SerializeField] private Camera _aimCamera;

    public event Action Fired;

    private WeaponSO CurrentWeapon => _weaponHolder != null ? _weaponHolder.CurrentWeapon : null;

    private void Reset()
    {
        if (_range == null)
            _range = GetComponent<VSplatterRange>();

        if (_aimCamera == null)
            _aimCamera = Camera.main;

        if (_fireOrigin == null)
            _fireOrigin = transform;
    }

    private void Awake()
    {
        if (_range == null)
            _range = GetComponent<VSplatterRange>();

        if (_aimCamera == null)
            _aimCamera = Camera.main;

        if (_fireOrigin == null)
            _fireOrigin = transform;
    }

    public bool TryFireOnce()
    {
        if (_range == null || !_range.HasValidWeapon() || CurrentWeapon == null)
            return false;

        Transform fireOrigin = _fireOrigin != null ? _fireOrigin : transform;

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

        Vector3 start = fireOrigin.position;
        Vector3 dir = aimPoint - start;

        if (dir.sqrMagnitude < 0.0001f)
            return false;

        dir.Normalize();

        bool hitSomething = Physics.Raycast(
            start,
            dir,
            out RaycastHit hit,
            CurrentWeapon.MaxRange,
            CurrentWeapon.DamageHitMask,
            QueryTriggerInteraction.Ignore);

        if (debugDraw)
        {
            Vector3 end = hitSomething ? hit.point : (start + dir * CurrentWeapon.MaxRange);
            Debug.DrawLine(start, end, Color.yellow, debugDrawDuration);
        }

        if (!hitSomething)
        {
            if (debugLogs)
                Debug.Log("[VSplatterAttack] fired, but no valid damage target was hit.");

            Fired?.Invoke();
            return true;
        }

        // TODO: 데미지 처리 붙일 위치
        // TryApplyDamage(hit, CurrentWeapon.Damage);

        if (debugLogs)
            Debug.Log("[VSplatterAttack] hit = " + hit.collider.name);

        Fired?.Invoke();
        return true;
    }
}