using System;
using UnityEngine;

[DisallowMultipleComponent]
public class VSplatterPaint : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private VSplatterRange _range;
    [SerializeField] private VSplatterWeaponHolder _weaponHolder;

    [Header("Listening To")]
[SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;



    [Header("Options")]
    [SerializeField] private MaskRenderManager.PaintChannel _paintChannel = MaskRenderManager.PaintChannel.Vaccine;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool debugDraw = false;
    [SerializeField] private float debugDrawDuration = 0.15f;

    [Header("AutoRef Don't Touch")]
    [SerializeField] private Camera _aimCamera;
    [NonSerialized] private MaskRenderManager _maskRenderManager;

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
    private void OnEnable()
    {
        if (_maskRenderManagerReadyChannel != null)
        {
            _maskRenderManagerReadyChannel.OnEventRaised += OnMaskRenderManagerChanged;

            if (_maskRenderManagerReadyChannel.Current != null)
                OnMaskRenderManagerChanged(_maskRenderManagerReadyChannel.Current);
        }
    }

    private void OnDisable()
    {
        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.OnEventRaised -= OnMaskRenderManagerChanged;

        _maskRenderManager = null;
    }

    private void OnMaskRenderManagerChanged(MaskRenderManager manager)
    {
        _maskRenderManager = manager;
    }
    public bool TryFireOnce()
    {
        if (_range == null || !_range.HasValidWeapon() || _maskRenderManager == null || CurrentWeapon == null)
            return false;

        PaintBulletSO bulletConfig = CurrentWeapon.PaintBullet;
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
                Debug.Log("[VSplatterPaint] out of range");

            return false;
        }

        Transform fireOrigin = GameplayFireOrigin != null ? GameplayFireOrigin : transform;
        Vector3 start = fireOrigin.position;

        Vector3 flightTarget = aimPoint;
        flightTarget.y = start.y;

        Vector3 dir = flightTarget - start;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return false;

        dir.Normalize();

        start += dir * bulletConfig.SpawnOffset;

        float maxDistance = Vector3.Distance(
            new Vector3(start.x, 0f, start.z),
            new Vector3(flightTarget.x, 0f, flightTarget.z));

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
            bulletConfig.ImpactMask,
            bulletConfig.TriggerInteraction,
            _maskRenderManager,
            _paintChannel,
            CurrentWeapon.PaintRadiusWorld,
            CurrentWeapon.PaintPriority,
            this);

        if (debugDraw)
            Debug.DrawLine(start, flightTarget, Color.cyan, debugDrawDuration);

        if (debugLogs)
            Debug.Log("[VSplatterPaint] paint bullet fired.");

        Fired?.Invoke();
        return true;
    }

}
