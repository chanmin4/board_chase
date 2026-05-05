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
            transform,
            out Vector3 aimPoint,
            out _);

        if (!gotAimPoint)
            return false;

        Vector3 paintTarget = _range.ClampToRange(aimPoint);

        Transform fireOrigin = VisualFireOrigin != null
            ? VisualFireOrigin
            : GameplayFireOrigin != null ? GameplayFireOrigin : transform;

        Vector3 visualStart = fireOrigin.position;
        Vector3 visualDirection = paintTarget - visualStart;

        if (visualDirection.sqrMagnitude < 0.0001f)
            return false;

        visualDirection.Normalize();

        Vector3 gameplayDirection = paintTarget - visualStart;
        gameplayDirection.y = 0f;

        if (gameplayDirection.sqrMagnitude < 0.0001f)
            return false;

        gameplayDirection.Normalize();

        Vector3 gameplayStart = visualStart + gameplayDirection * bulletConfig.SpawnOffset;
        Vector3 visualSpawn = visualStart + visualDirection * bulletConfig.SpawnOffset;

        Quaternion bulletRotation = Quaternion.LookRotation(visualDirection, Vector3.up);

        PaintBullet bullet = Instantiate(
            bulletConfig.BulletPrefab,
            visualSpawn,
            bulletRotation,
            ProjectilesRoot).GetComponent<PaintBullet>();

        bullet.Init(
            gameplayStart,
            gameplayDirection,
            visualSpawn,
            paintTarget,
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
            Debug.DrawLine(visualSpawn, aimPoint, Color.cyan, debugDrawDuration);

        if (debugLogs)
            Debug.Log("[VSplatterPaint] paint bullet fired.");

        Fired?.Invoke();
        return true;
    }

}
