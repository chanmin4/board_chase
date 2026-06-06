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

    [Header("Paint")]
    [SerializeField]
    private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [SerializeField]
    private MaskRenderManager.PaintChannel _paintChannel =
        MaskRenderManager.PaintChannel.Vaccine;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;
    [SerializeField] private bool debugDraw;
    [SerializeField] private float debugDrawDuration = 0.15f;

    [Header("Don't Touch Ref Auto")]
    [SerializeField] private Camera _aimCamera;

    [NonSerialized] private MaskRenderManager _maskRenderManager;

    public event Action Fired;

    private WeaponSO CurrentWeapon =>
        _weaponHolder != null ? _weaponHolder.CurrentWeapon : null;

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

    private void OnEnable()
    {
        if (_maskRenderManagerReadyChannel == null)
            return;

        _maskRenderManagerReadyChannel.OnEventRaised +=
            HandleMaskRenderManagerReady;

        if (_maskRenderManagerReadyChannel.Current != null)
        {
            HandleMaskRenderManagerReady(
                _maskRenderManagerReadyChannel.Current);
        }
    }

    private void OnDisable()
    {
        if (_maskRenderManagerReadyChannel != null)
        {
            _maskRenderManagerReadyChannel.OnEventRaised -=
                HandleMaskRenderManagerReady;
        }

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

        if (_maskRenderManager == null)
            _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();
    }

    private void HandleMaskRenderManagerReady(MaskRenderManager manager)
    {
        _maskRenderManager = manager;
    }

    public bool TryFireOnce()
    {
        if (_range == null ||
            !_range.HasValidWeapon() ||
            CurrentWeapon == null ||
            _bulletLoadout == null)
        {
            return false;
        }

        if (!_bulletLoadout.TryGetActivePrimaryBullet(
                out AttackBulletSO bulletConfig))
        {
            return false;
        }

        if (bulletConfig == null || bulletConfig.BulletPrefab == null)
            return false;

        bool isAttackAndPaint =
            bulletConfig.AmmoType == BulletAmmoType.AttackAndPaint;

        if (isAttackAndPaint)
        {
            if (_maskRenderManager == null)
                _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();

            if (_maskRenderManager == null)
                return false;

            if (!bulletConfig.BulletPrefab.TryGetComponent<Attack_PaintBullet>(
                    out _))
            {
                Debug.LogError(
                    $"[VSplatterAttack] Attack_PaintBullet component is missing: {bulletConfig.BulletPrefab.name}",
                    bulletConfig.BulletPrefab);

                return false;
            }
        }
        else
        {
            bool hasAttackBullet =
                bulletConfig.BulletPrefab.TryGetComponent<AttackBullet>(out _);

            bool hasAttackPaintBullet =
                bulletConfig.BulletPrefab.TryGetComponent<Attack_PaintBullet>(
                    out _);

            if (!hasAttackBullet || hasAttackPaintBullet)
            {
                Debug.LogError(
                    $"[VSplatterAttack] Pure Attack prefab requires AttackBullet: {bulletConfig.BulletPrefab.name}",
                    bulletConfig.BulletPrefab);

                return false;
            }
        }

        if (!VSplatterAimUtility.TryGetAimPoint(
                _aimCamera,
                CurrentWeapon.AimHitMask,
                CurrentWeapon.AllowFallbackPlane,
                CurrentWeapon.FallbackPlaneY,
                transform,
                out Vector3 aimPoint,
                out _))
        {
            return false;
        }

        Ray aimRay = _aimCamera.ScreenPointToRay(Input.mousePosition);

        Transform fireOrigin = VisualFireOrigin != null
            ? VisualFireOrigin
            : GameplayFireOrigin != null
                ? GameplayFireOrigin
                : transform;

        Vector3 visualStart = fireOrigin.position;

        if (!VSplatterAimUtility.TryGetPointOnYPlane(
                aimRay,
                visualStart.y,
                out Vector3 visualAimPoint))
        {
            return false;
        }

        Vector3 rangeOrigin = _range.RangeOrigin != null
            ? _range.RangeOrigin.position
            : transform.position;

        Vector3 targetPoint = VSplatterAimUtility.ClampFlatPointToRange(
            rangeOrigin,
            aimPoint,
            CurrentMaxRange);

        Vector3 gameplayDirection = targetPoint - visualStart;
        gameplayDirection.y = 0f;

        if (gameplayDirection.sqrMagnitude < 0.0001f)
            return false;

        gameplayDirection.Normalize();

        Vector3 visualDirection = visualAimPoint - visualStart;
        visualDirection.y = 0f;

        if (visualDirection.sqrMagnitude < 0.0001f)
            visualDirection = gameplayDirection;

        visualDirection.Normalize();

        Vector3 gameplayStart =
            visualStart + gameplayDirection * bulletConfig.SpawnOffset;

        Vector3 visualSpawn =
            visualStart + visualDirection * bulletConfig.SpawnOffset;

        float maxDistance = Vector3.Distance(
            new Vector3(gameplayStart.x, 0f, gameplayStart.z),
            new Vector3(targetPoint.x, 0f, targetPoint.z));

        if (maxDistance <= 0.001f)
            return false;

        if (!_bulletLoadout.TryConsumePrimaryAmmo(1, out bulletConfig))
            return false;

        float damage = _statsRuntime != null
            ? _statsRuntime.ResolveAttackDamage(bulletConfig)
            : 0f;

        float paintRadius =
            isAttackAndPaint && _statsRuntime != null
                ? _statsRuntime.ResolvePaintRadius(bulletConfig)
                : 0f;

        Quaternion bulletRotation =
            Quaternion.LookRotation(visualDirection, Vector3.up);

        GameObject bulletObject = Instantiate(
            bulletConfig.BulletPrefab,
            visualSpawn,
            bulletRotation,
            ProjectilesRoot);

        if (isAttackAndPaint)
        {
            Attack_PaintBullet bullet =
                bulletObject.GetComponent<Attack_PaintBullet>();

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
                _maskRenderManager,
                _paintChannel,
                paintRadius,
                _statsRuntime != null
                    ? _statsRuntime.Paint.paintPriority
                    : 0,
                gameObject);
        }
        else
        {
            AttackBullet bullet = bulletObject.GetComponent<AttackBullet>();

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
        }

        if (debugDraw)
        {
            Debug.DrawLine(
                visualSpawn,
                visualSpawn + visualDirection * maxDistance,
                Color.yellow,
                debugDrawDuration);
        }

        if (debugLogs)
        {
            Debug.Log(
                $"[VSplatterAttack] Fired. type={bulletConfig.AmmoType}");
        }

        Fired?.Invoke();
        return true;
    }
}