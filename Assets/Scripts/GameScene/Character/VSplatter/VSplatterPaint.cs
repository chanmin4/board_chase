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

        Ray aimRay = _aimCamera.ScreenPointToRay(Input.mousePosition);

        Vector3 paintTarget = _range.ClampToRange(aimPoint);

        Transform fireOrigin = VisualFireOrigin != null
            ? VisualFireOrigin
            : GameplayFireOrigin != null ? GameplayFireOrigin : transform;

        Vector3 visualStart = fireOrigin.position;

        // 실제 판정/페인트는 paintTarget 방향으로 간다.
        Vector3 gameplayDirection = paintTarget - visualStart;
        gameplayDirection.y = 0f;

        if (gameplayDirection.sqrMagnitude < 0.0001f)
            return false;

        gameplayDirection.Normalize();

        Vector3 gameplayStart = visualStart + gameplayDirection * bulletConfig.SpawnOffset;

        // 비주얼 방향은 크로스헤어 중앙 Ray 기준으로 잡는다.
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

        // 중요: visualTarget을 raw aim point로 쓰면 멀리 날아감.
        // gameplay 탄착 거리만큼만 비주얼도 날아가게 잘라준다.
        float maxVisualDistance = Vector3.Distance(
            new Vector3(gameplayStart.x, 0f, gameplayStart.z),
            new Vector3(paintTarget.x, 0f, paintTarget.z));

        Vector3 visualTarget = visualSpawn + visualDirection * maxVisualDistance;

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
