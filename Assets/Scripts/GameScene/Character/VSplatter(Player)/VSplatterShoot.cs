using System;
using UnityEngine;

[DisallowMultipleComponent]
public class VSplatterShoot : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private VSplatterRange _range;
    [SerializeField] private VSplatterWeaponHolder _weaponHolder;
    [SerializeField] private PlayerStatsRuntime _statsRuntime;
    [SerializeField] private PlayerBulletLoadoutRuntime _bulletLoadout;

    [Header("Ready Events")]
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Header("Paint")]
    [SerializeField]
    private PaintChannel _paintChannel =
        PaintChannel.Vaccine;

    [Header("Hit Feedback")]
    [SerializeField] private ShootHitConfirmedEventChannelSO _shootHitConfirmedEvent;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = false;
    [SerializeField] private bool _debugDraw = false;
    [SerializeField, Min(0f)] private float _debugDrawDuration = 0.15f;

    [Header("Auto Refs")]
    [SerializeField] private Camera _aimCamera;

    [NonSerialized] private MaskRenderManager _maskRenderManager;

    public event Action<BulletAmmoType> Fired;

    private WeaponSO CurrentWeapon =>
        _weaponHolder != null ? _weaponHolder.CurrentWeapon : null;

    private Transform GameplayFireOrigin =>
        _weaponHolder != null && _weaponHolder.GameplayFireOrigin != null
            ? _weaponHolder.GameplayFireOrigin
            : transform;

    private Transform VisualFireOrigin =>
        _weaponHolder != null && _weaponHolder.VisualFireOrigin != null
            ? _weaponHolder.VisualFireOrigin
            : GameplayFireOrigin;

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
            _maskRenderManagerReadyChannel.OnEventRaised += HandleMaskRenderManagerReady;

            if (_maskRenderManagerReadyChannel.Current != null)
                HandleMaskRenderManagerReady(_maskRenderManagerReadyChannel.Current);
        }
    }

    private void OnDisable()
    {
        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.OnEventRaised -= HandleMaskRenderManagerReady;

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
        if (manager == null)
            return;

        _maskRenderManager = manager;
    }

    public bool TryFireOnce()
    {
        ResolveRefs();

        if (!CanFireCommon())
            return false;

        if (!_bulletLoadout.TryGetSelectedAmmoType(out BulletAmmoType ammoType))
            return false;

        return TryFireOnce(ammoType);
    }

    public bool TryFireOnce(BulletAmmoType ammoType)
    {
        ResolveRefs();

        if (!CanFireCommon())
            return false;

        switch (ammoType)
        {
            case BulletAmmoType.AttackAndPaint:
            case BulletAmmoType.Attack:
                return TryFirePrimary();

            case BulletAmmoType.Paint:
                return TryFirePaint();

            case BulletAmmoType.Special:
                if (_debugLogs)
                    Debug.Log("[VSplatterShoot] Special fire is not handled in VSplatterShoot.", this);
                return false;
        }

        return false;
    }

    private bool CanFireCommon()
    {
        if (_range == null || !_range.HasValidWeapon())
            return false;

        if (CurrentWeapon == null)
            return false;

        if (_bulletLoadout == null)
            return false;

        return true;
    }

    private bool TryFirePrimary()
    {
        if (!_bulletLoadout.TryGetActivePrimaryBullet(out AttackBulletSO bulletConfig))
            return false;

        if (bulletConfig == null || bulletConfig.BulletPrefab == null)
            return false;

        bool isAttackAndPaint = bulletConfig.AmmoType == BulletAmmoType.AttackAndPaint;

        if (isAttackAndPaint)
        {
            if (!EnsureMaskRenderManager())
                return false;

            if (!bulletConfig.BulletPrefab.TryGetComponent<Attack_PaintBullet>(out _))
            {
                Debug.LogError(
                    $"[VSplatterShoot] AttackAndPaint bullet prefab requires Attack_PaintBullet: {bulletConfig.BulletPrefab.name}",
                    bulletConfig.BulletPrefab);
                return false;
            }
        }
        else
        {
            bool hasAttackBullet =
                bulletConfig.BulletPrefab.TryGetComponent<AttackBullet>(out _);

            bool hasAttackPaintBullet =
                bulletConfig.BulletPrefab.TryGetComponent<Attack_PaintBullet>(out _);

            if (!hasAttackBullet || hasAttackPaintBullet)
            {
                Debug.LogError(
                    $"[VSplatterShoot] Attack bullet prefab requires AttackBullet only: {bulletConfig.BulletPrefab.name}",
                    bulletConfig.BulletPrefab);
                return false;
            }
        }

        if (!TryResolvePrimaryShot(
                out Vector3 gameplayStart,
                out Vector3 gameplayDirection,
                out Vector3 visualDirection,
                out float maxDistance,
                out Vector3 visualSpawn))
        {
            return false;
        }

        if (!_bulletLoadout.TryConsumePrimaryAmmo(1, out bulletConfig))
            return false;

        if (bulletConfig == null || bulletConfig.BulletPrefab == null)
            return false;

        float damage = _statsRuntime != null
            ? _statsRuntime.ResolveAttackDamage(bulletConfig)
            : 0f;

        float paintRadius =
            isAttackAndPaint && _statsRuntime != null
                ? _statsRuntime.ResolvePaintRadius(bulletConfig)
                : 0f;

        Quaternion rotation = Quaternion.LookRotation(visualDirection, Vector3.up);

        GameObject bulletObject = Instantiate(
            bulletConfig.BulletPrefab,
            visualSpawn,
            rotation,
            ProjectilesRoot);

        if (isAttackAndPaint)
        {
            Attack_PaintBullet bullet = bulletObject.GetComponent<Attack_PaintBullet>();

            if (bullet == null)
            {
                Debug.LogError(
                    $"[VSplatterShoot] Attack_PaintBullet missing after instantiate: {bulletConfig.BulletPrefab.name}",
                    bulletObject);
                Destroy(bulletObject);
                return false;
            }

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
                CurrentPaintPriority,
                gameObject,
                _shootHitConfirmedEvent);
        }
        else
        {
            AttackBullet bullet = bulletObject.GetComponent<AttackBullet>();

            if (bullet == null)
            {
                Debug.LogError(
                    $"[VSplatterShoot] AttackBullet missing after instantiate: {bulletConfig.BulletPrefab.name}",
                    bulletObject);
                Destroy(bulletObject);
                return false;
            }

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
                gameObject,
                _shootHitConfirmedEvent);
        }

        if (_debugDraw)
        {
            Debug.DrawLine(
                visualSpawn,
                visualSpawn + visualDirection * maxDistance,
                Color.yellow,
                _debugDrawDuration);
        }

        if (_debugLogs)
        {
            Debug.Log(
                $"[VSplatterShoot] Fired primary. bullet={bulletConfig.name}, ammoType={bulletConfig.AmmoType}",
                this);
        }

        Fired?.Invoke(bulletConfig.AmmoType);
        return true;
    }

    private bool TryFirePaint()
    {
        if (!EnsureMaskRenderManager())
            return false;

        if (!_bulletLoadout.TryGetActivePaintBullet(out PaintBulletSO bulletConfig))
            return false;

        if (bulletConfig == null || bulletConfig.BulletPrefab == null)
            return false;

        if (!bulletConfig.BulletPrefab.TryGetComponent<PaintBullet>(out _))
        {
            Debug.LogError(
                $"[VSplatterShoot] Paint bullet prefab requires PaintBullet: {bulletConfig.BulletPrefab.name}",
                bulletConfig.BulletPrefab);
            return false;
        }

        if (!TryResolvePaintShot(
                out Vector3 gameplayStart,
                out Vector3 gameplayDirection,
                out Vector3 visualSpawn,
                out Vector3 visualTarget,
                out Vector3 paintTarget))
        {
            return false;
        }

        if (!_bulletLoadout.TryConsumePaintAmmo(1, out bulletConfig))
            return false;

        if (bulletConfig == null || bulletConfig.BulletPrefab == null)
            return false;

        float paintRadius = _statsRuntime != null
            ? _statsRuntime.ResolvePaintRadius(bulletConfig)
            : 1f;

        Quaternion rotation = Quaternion.LookRotation(gameplayDirection, Vector3.up);

        GameObject bulletObject = Instantiate(
            bulletConfig.BulletPrefab,
            visualSpawn,
            rotation,
            ProjectilesRoot);

        PaintBullet bullet = bulletObject.GetComponent<PaintBullet>();

        if (bullet == null)
        {
            Debug.LogError(
                $"[VSplatterShoot] PaintBullet missing after instantiate: {bulletConfig.BulletPrefab.name}",
                bulletObject);
            Destroy(bulletObject);
            return false;
        }

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
            CurrentPaintPriority,
            this);

        if (_debugDraw)
            Debug.DrawLine(visualSpawn, paintTarget, Color.cyan, _debugDrawDuration);

        if (_debugLogs)
        {
            Debug.Log(
                $"[VSplatterShoot] Fired paint. bullet={bulletConfig.name}, ammoType={bulletConfig.AmmoType}",
                this);
        }

        Fired?.Invoke(bulletConfig.AmmoType);
        return true;
    }

    private bool EnsureMaskRenderManager()
    {
        if (_maskRenderManager == null)
            _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();

        return _maskRenderManager != null;
    }

    private bool TryResolvePrimaryShot(
        out Vector3 gameplayStart,
        out Vector3 gameplayDirection,
        out Vector3 visualDirection,
        out float maxDistance,
        out Vector3 visualSpawn)
    {
        gameplayStart = default;
        gameplayDirection = default;
        visualDirection = default;
        maxDistance = 0f;
        visualSpawn = default;

        if (_aimCamera == null)
            _aimCamera = Camera.main;

        if (_aimCamera == null)
            return false;

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

        Vector3 visualStart = VisualFireOrigin.position;

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

        gameplayDirection = targetPoint - visualStart;
        gameplayDirection.y = 0f;

        if (gameplayDirection.sqrMagnitude < 0.0001f)
            return false;

        gameplayDirection.Normalize();

        visualDirection = visualAimPoint - visualStart;
        visualDirection.y = 0f;

        if (visualDirection.sqrMagnitude < 0.0001f)
            visualDirection = gameplayDirection;

        visualDirection.Normalize();

        float spawnOffset = ResolvePrimarySpawnOffset();

        gameplayStart = visualStart + gameplayDirection * spawnOffset;
        visualSpawn = visualStart + visualDirection * spawnOffset;

        maxDistance = Vector3.Distance(
            new Vector3(gameplayStart.x, 0f, gameplayStart.z),
            new Vector3(targetPoint.x, 0f, targetPoint.z));

        return maxDistance > 0.001f;
    }

    private bool TryResolvePaintShot(
        out Vector3 gameplayStart,
        out Vector3 gameplayDirection,
        out Vector3 visualSpawn,
        out Vector3 visualTarget,
        out Vector3 paintTarget)
    {
        gameplayStart = default;
        gameplayDirection = default;
        visualSpawn = default;
        visualTarget = default;
        paintTarget = default;

        if (_aimCamera == null)
            _aimCamera = Camera.main;

        if (_aimCamera == null)
            return false;

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

        Vector3 rangeOrigin = _range.RangeOrigin != null
            ? _range.RangeOrigin.position
            : transform.position;

        paintTarget = VSplatterAimUtility.ClampFlatPointToRange(
            rangeOrigin,
            aimPoint,
            CurrentMaxRange);

        Ray aimRay = _aimCamera.ScreenPointToRay(Input.mousePosition);

        Vector3 visualStart = VisualFireOrigin.position;

        gameplayDirection = paintTarget - visualStart;
        gameplayDirection.y = 0f;

        if (gameplayDirection.sqrMagnitude < 0.0001f)
            return false;

        gameplayDirection.Normalize();

        float spawnOffset = ResolvePaintSpawnOffset();
        gameplayStart = visualStart + gameplayDirection * spawnOffset;

        if (!VSplatterAimUtility.TryGetPointOnYPlane(
                aimRay,
                visualStart.y,
                out Vector3 visualAimPoint))
        {
            visualAimPoint = paintTarget;
            visualAimPoint.y = visualStart.y;
        }

        Vector3 visualDirection = visualAimPoint - visualStart;
        visualDirection.y = 0f;

        if (visualDirection.sqrMagnitude < 0.0001f)
            visualDirection = gameplayDirection;

        visualDirection.Normalize();

        visualSpawn = visualStart + visualDirection * spawnOffset;

        float maxVisualDistance = Vector3.Distance(
            new Vector3(gameplayStart.x, 0f, gameplayStart.z),
            new Vector3(paintTarget.x, 0f, paintTarget.z));

        visualTarget = visualSpawn + visualDirection * maxVisualDistance;
        return true;
    }

    private float ResolvePrimarySpawnOffset()
    {
        if (_bulletLoadout != null &&
            _bulletLoadout.TryGetActivePrimaryBullet(out AttackBulletSO bullet) &&
            bullet != null)
        {
            return bullet.SpawnOffset;
        }

        return 0.12f;
    }

    private float ResolvePaintSpawnOffset()
    {
        if (_bulletLoadout != null &&
            _bulletLoadout.TryGetActivePaintBullet(out PaintBulletSO bullet) &&
            bullet != null)
        {
            return bullet.SpawnOffset;
        }

        return 0.12f;
    }
}