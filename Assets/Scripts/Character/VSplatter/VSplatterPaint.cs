using System;
using UnityEngine;

[DisallowMultipleComponent]
public class VSplatterPaint : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private VSplatterRange _range;
    [SerializeField] private VSplatterWeaponHolder _weaponHolder;
    [Tooltip("bullet parent object")]
  
    [NonSerialized] private MaskRenderManager _maskRenderManager;

    [Header("Options")]
    [SerializeField] private MaskRenderManager.PaintChannel _paintChannel = MaskRenderManager.PaintChannel.Vaccine;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool debugDraw = false;
    [SerializeField] private float debugDrawDuration = 0.15f;

    [Header("AutoRef Don't Touch")]
    [SerializeField] private Camera _aimCamera;

    public event Action Fired;

    private WeaponSO CurrentWeapon => _weaponHolder != null ? _weaponHolder.CurrentWeapon : null;
    private Transform FireOrigin => _weaponHolder != null ? _weaponHolder.GameplayFireOrigin : transform;
    private Vector3 FireDirection => _weaponHolder != null ? _weaponHolder.FireDirection : FireOrigin.forward;
    private Transform ProjectilesRoot => _weaponHolder != null ? _weaponHolder.ProjectilesRoot : null;
    private void Reset()
    {
        if (_range == null)
            _range = GetComponent<VSplatterRange>();

        if (_weaponHolder == null)
            _weaponHolder = GetComponent<VSplatterWeaponHolder>();

        if (_maskRenderManager == null)
            _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();

        if (_aimCamera == null)
            _aimCamera = Camera.main;

    }

    private void Awake()
    {
        if (_range == null)
            _range = GetComponent<VSplatterRange>();

        if (_weaponHolder == null)
            _weaponHolder = GetComponent<VSplatterWeaponHolder>();

        if (_maskRenderManager == null)
            _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();

        if (_aimCamera == null)
            _aimCamera = Camera.main;

    }

    public bool TryFireOnce()
    {
        if (_maskRenderManager == null)
            _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();

        if (_range == null || !_range.HasValidWeapon() || _maskRenderManager == null || CurrentWeapon == null)
            return false;
        Debug.Log($"[VSplatterPaint] TryFireOnce: CurrentWeapon={CurrentWeapon.DisplayName}");
        PaintBulletSO bulletConfig = CurrentWeapon.PaintBullet;
        Debug.Log($"[VSplatterPaint] TryFireOnce: bulletConfig={bulletConfig}");
        if (bulletConfig == null || bulletConfig.BulletPrefab == null)
            return false;
        
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
                Debug.Log("[VSplatterPaint] out of range");

            return false;
        }
        Transform fireOrigin = FireOrigin != null ? FireOrigin : transform;
        Vector3 start = fireOrigin.position;

        Vector3 flatAimPoint = aimPoint;
        flatAimPoint.y = start.y;

        Vector3 dir = flatAimPoint - start;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return false;

        dir.Normalize();
        Quaternion bulletRotation = Quaternion.LookRotation(dir, Vector3.up);
        PaintBullet bullet = Instantiate(
        bulletConfig.BulletPrefab,
        start,
        bulletRotation,
        ProjectilesRoot).GetComponent<PaintBullet>();

        bullet.Init(
        aimPoint,
        bulletConfig.Speed,
        bulletConfig.CastRadius,
        bulletConfig.MaxLifetime,
        bulletConfig.BlockHitMask,
        bulletConfig.TriggerInteraction,
        _maskRenderManager,
        _paintChannel,
        CurrentWeapon.PaintRadiusWorld,
        CurrentWeapon.PaintPriority,
        this);

        if (debugDraw)
            Debug.DrawLine(start, aimPoint, Color.cyan, debugDrawDuration);

        if (debugLogs)
            Debug.Log("[VSplatterPaint] paint bullet fired.");
        Debug.Log($"[VSplatterPaint] weaponHolder={_weaponHolder}, projectilesRoot={ProjectilesRoot}", this);
        Fired?.Invoke();
        return true;
    }
}
