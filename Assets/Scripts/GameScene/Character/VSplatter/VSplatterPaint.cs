using System;
using UnityEngine;

[DisallowMultipleComponent]
public class VSplatterPaint : MonoBehaviour
{
    [Header("Need Ref")]
    [SerializeField] private VSplatterRange _range;
    [SerializeField] private VSplatterWeaponHolder _weaponHolder;
    [SerializeField] private PlayerStatsRuntime _statsRuntime;
    [SerializeField] private PlayerBulletLoadoutRuntime _bulletLoadout;

    [Header("Listening To")]
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Header("Options")]
    [SerializeField] private MaskRenderManager.PaintChannel _paintChannel = MaskRenderManager.PaintChannel.Vaccine;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool debugDraw = false;
    [SerializeField] private float debugDrawDuration = 0.15f;

    [Header("Don't Touch Ref Auto")]
    [SerializeField] private Camera _aimCamera;
    [NonSerialized] private MaskRenderManager _maskRenderManager;

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

    private int CurrentPaintPriority =>
        _statsRuntime != null
            ? _statsRuntime.Paint.paintPriority
            : 0;

    private void Reset()
    {
        ResolveRefs();
    }

    private void Awake()
    {
        ResolveRefs();
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

    private void OnMaskRenderManagerChanged(MaskRenderManager manager)
    {
        _maskRenderManager = manager;
    }

    public bool TryFireOnce()
    {
        if (_range == null || !_range.HasValidWeapon() || _maskRenderManager == null || CurrentWeapon == null)
            return false;

        if (_bulletLoadout == null)
            return false;

        if (!_bulletLoadout.TryGetActivePaintBullet(out PaintBulletSO bulletConfig))
            return false;

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

        Vector3 rangeOrigin = _range.RangeOrigin != null
            ? _range.RangeOrigin.position
            : transform.position;

        Vector3 paintTarget = VSplatterAimUtility.ClampFlatPointToRange(
            rangeOrigin,
            aimPoint,
            CurrentMaxRange);

        Ray aimRay = _aimCamera.ScreenPointToRay(Input.mousePosition);

        Transform fireOrigin = VisualFireOrigin != null
            ? VisualFireOrigin
            : GameplayFireOrigin != null ? GameplayFireOrigin : transform;

        Vector3 visualStart = fireOrigin.position;

        Vector3 gameplayDirection = paintTarget - visualStart;
        gameplayDirection.y = 0f;

        if (gameplayDirection.sqrMagnitude < 0.0001f)
            return false;

        gameplayDirection.Normalize();

        Vector3 gameplayStart = visualStart + gameplayDirection * bulletConfig.SpawnOffset;

        Vector3 visualAimPoint;
        if (!VSplatterAimUtility.TryGetPointOnYPlane(
                aimRay,
                visualStart.y,
                out visualAimPoint))
        {
            visualAimPoint = paintTarget;
            visualAimPoint.y = visualStart.y;
        }

        Vector3 visualDirection = visualAimPoint - visualStart;
        visualDirection.y = 0f;

        if (visualDirection.sqrMagnitude < 0.0001f)
            visualDirection = gameplayDirection;

        visualDirection.Normalize();

        Vector3 visualSpawn = visualStart + visualDirection * bulletConfig.SpawnOffset;

        float maxVisualDistance = Vector3.Distance(
            new Vector3(gameplayStart.x, 0f, gameplayStart.z),
            new Vector3(paintTarget.x, 0f, paintTarget.z));

        Vector3 visualTarget = visualSpawn + visualDirection * maxVisualDistance;

        if (!_bulletLoadout.TryConsumePaintAmmo(1, out bulletConfig))
            return false;

        if (bulletConfig == null || bulletConfig.BulletPrefab == null)
            return false;

        float paintRadius = _statsRuntime != null
            ? _statsRuntime.ResolvePaintRadius(bulletConfig)
            : 1f;

        int paintPriority = CurrentPaintPriority;

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
            visualTarget,
            paintTarget,
            bulletConfig.Speed,
            bulletConfig.CastRadius,
            bulletConfig.MaxLifetime,
            bulletConfig.ImpactMask,
            bulletConfig.TriggerInteraction,
            _maskRenderManager,
            _paintChannel,
            paintRadius,
            paintPriority,
            this);

        if (debugDraw)
            Debug.DrawLine(visualSpawn, aimPoint, Color.cyan, debugDrawDuration);

        if (debugLogs)
            Debug.Log("[VSplatterPaint] paint bullet fired.");

        Fired?.Invoke();
        return true;
    }
}