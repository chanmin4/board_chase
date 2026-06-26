using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyShooterShoot : EntityShootCore
{
    [Header("Config")]
    [SerializeField] private EnemyShooterConfigSO _config;

    [Header("Stats Runtime")]
    [SerializeField] private EnemyShooterStatsRuntime _statsRuntime;

    [Header("Weapon Holder")]
    [SerializeField] private EntityWeaponHolder _weaponHolder;

    [Header("Events")]
    [SerializeField] private SoundStimulusEventChannelSO _soundStimulusEvent;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = false;
    [SerializeField] private bool _debugDraw = false;
    [SerializeField, Min(0f)] private float _debugDrawDuration = 0.15f;

    private const ShooterFaction ShooterSide = ShooterFaction.Enemy;
    private const PaintMarkFaction HitMarkSide = PaintMarkFaction.Virus;

    private Damageable _damageable;
    private MaskRenderManager _maskRenderManager;
    private BulletSO _runtimeBullet;
    private BulletSO _loadedBullet;
    private int _magazineAmmo;
    private bool _isReloading;
    private float _reloadEndTime;

    public event Action ReloadStarted;
    public event Action ReloadFinished;

    public bool IsReloading => _isReloading;
    public int MagazineAmmo => _magazineAmmo;

    public EnemyShooterConfigSO Config => ResolveConfig();

    public BulletSO CurrentBullet =>
        _runtimeBullet != null
            ? _runtimeBullet
            : ResolveConfig() != null ? ResolveConfig().Bullet : null;

    private Transform VisualFireOrigin =>
        _weaponHolder != null && _weaponHolder.VisualFireOrigin != null
            ? _weaponHolder.VisualFireOrigin
            : null;

    private Transform RangeOrigin => transform;

    private Transform ProjectilesRoot =>
        _weaponHolder != null ? _weaponHolder.ProjectilesRoot : null;

    private void Reset()
    {
        ResolveRefs();
    }

    private void Awake()
    {
        ResolveRefs();
    }

    public void SetConfig(EnemyShooterConfigSO config)
    {
        _config = config;

        if (_statsRuntime != null)
            _statsRuntime.SetConfig(config);
    }

    public void SetRuntimeBullet(BulletSO bullet)
    {
        bool wasReloading = _isReloading;

        _runtimeBullet = bullet;
        _loadedBullet = null;
        _magazineAmmo = 0;
        _isReloading = false;
        _reloadEndTime = 0f;

        if (wasReloading)
            ReloadFinished?.Invoke();
    }

    public bool TryFireAt(Vector3 aimWorldPoint)
    {
        EnemyShooterConfigSO config = ResolveConfig();
        BulletSO bullet = CurrentBullet;

        if (config == null || bullet == null)
            return false;

        TickReload();

        if (_isReloading)
            return false;

        EnsureMagazineReady(bullet);

        if (_magazineAmmo <= 0)
        {
            BeginReload();
            return false;
        }

        EntityShootStats stats = BuildStats(config);
        bool fired = false;

        switch (bullet.AmmoType)
        {
            case BulletAmmoType.AttackAndPaint:
            case BulletAmmoType.Attack:
                fired = TryFirePrimaryAt(bullet, aimWorldPoint, stats);
                break;

            case BulletAmmoType.Paint:
                fired = TryFirePaintAt(bullet as PaintBulletSO, aimWorldPoint, stats);
                break;
        }

        if (!fired)
            return false;

        _magazineAmmo = Mathf.Max(0, _magazineAmmo - 1);
        PublishGunshotSound();

        if (_magazineAmmo <= 0)
            BeginReload();

        return true;
    }

    private bool TryFirePrimaryAt(BulletSO bullet, Vector3 aimWorldPoint, EntityShootStats stats)
    {
        if (bullet == null)
            return false;

        if (bullet.AmmoType == BulletAmmoType.AttackAndPaint)
            _maskRenderManager = ResolveMaskRenderManager(_maskRenderManager);

        Transform visualFireOrigin = VisualFireOrigin;
        Transform rangeOrigin = RangeOrigin;

        if (visualFireOrigin == null || rangeOrigin == null)
        {
            Debug.LogError("[EnemyShooterShoot] Fire origin or range origin is missing.", this);
            return false;
        }

        Vector3 visualAimPoint = aimWorldPoint;
        visualAimPoint.y = visualFireOrigin.position.y;

        if (!TryResolveShotFromAimPoints(
                bullet,
                aimWorldPoint,
                visualAimPoint,
                stats.maxRange,
                visualFireOrigin,
                rangeOrigin,
                out Vector3 gameplayStart,
                out Vector3 gameplayDirection,
                out Vector3 visualDirection,
                out float maxDistance,
                out Vector3 visualSpawn))
        {
            return false;
        }

        return TryFirePrimary(
            bullet,
            gameplayStart,
            gameplayDirection,
            visualDirection,
            maxDistance,
            visualSpawn,
            ProjectilesRoot,
            _maskRenderManager,
            stats,
            gameObject,
            null,
            _debugLogs,
            _debugDraw,
            _debugDrawDuration);
    }

    private bool TryFirePaintAt(PaintBulletSO bullet, Vector3 aimWorldPoint, EntityShootStats stats)
    {
        _maskRenderManager = ResolveMaskRenderManager(_maskRenderManager);

        if (bullet == null || _maskRenderManager == null)
            return false;

        Transform visualFireOrigin = VisualFireOrigin;
        Transform rangeOrigin = RangeOrigin;

        if (visualFireOrigin == null || rangeOrigin == null)
        {
            Debug.LogError("[EnemyShooterShoot] Fire origin or range origin is missing.", this);
            return false;
        }

        Vector3 visualAimPoint = aimWorldPoint;
        visualAimPoint.y = visualFireOrigin.position.y;

        if (!TryResolvePaintShotFromAimPoints(
                bullet,
                aimWorldPoint,
                visualAimPoint,
                stats.maxRange,
                visualFireOrigin,
                rangeOrigin,
                out Vector3 gameplayStart,
                out Vector3 gameplayDirection,
                out Vector3 visualSpawn,
                out Vector3 visualTarget,
                out Vector3 paintTarget))
        {
            return false;
        }

        return TryFirePaint(
            bullet,
            gameplayStart,
            gameplayDirection,
            visualSpawn,
            visualTarget,
            paintTarget,
            ProjectilesRoot,
            _maskRenderManager,
            stats,
            gameObject,
            _debugLogs,
            _debugDraw,
            _debugDrawDuration);
    }

    private EntityShootStats BuildStats(EnemyShooterConfigSO config)
    {
        BulletSO bullet = CurrentBullet;

        return new EntityShootStats(
            _statsRuntime != null ? _statsRuntime.MaxRange : config.MaxRange,
            _statsRuntime != null ? _statsRuntime.ResolveAttackDamage(bullet) : config.Damage,
            _statsRuntime != null ? _statsRuntime.ResolvePaintRadius(bullet) : config.PaintRadius,
            _statsRuntime != null ? _statsRuntime.PaintPriority : config.PaintPriority,
            _statsRuntime != null ? _statsRuntime.PaintChannel : config.PaintChannel,
            bullet != null ? bullet.ResolveDamageTargetMask(ShooterSide) : default,
            HitMarkSide,
            bullet != null ? bullet.PaintMarkAmountOnHit : 0f);
    }

    private EnemyShooterConfigSO ResolveConfig()
    {
        if (_config != null)
            return _config;

        if (_statsRuntime == null)
            _statsRuntime = GetComponent<EnemyShooterStatsRuntime>() ?? GetComponentInParent<EnemyShooterStatsRuntime>();

        if (_statsRuntime != null && _statsRuntime.Config != null)
            return _statsRuntime.Config;

        if (_damageable == null)
            _damageable = GetComponent<Damageable>() ?? GetComponentInParent<Damageable>();

        _config = _damageable != null
            ? _damageable.StatConfig as EnemyShooterConfigSO
            : null;

        return _config;
    }

    private void ResolveRefs()
    {
        if (_damageable == null)
            _damageable = GetComponent<Damageable>() ?? GetComponentInParent<Damageable>();

        if (_statsRuntime == null)
            _statsRuntime = GetComponent<EnemyShooterStatsRuntime>() ?? GetComponentInParent<EnemyShooterStatsRuntime>();

        if (_weaponHolder == null)
            _weaponHolder = GetComponent<EntityWeaponHolder>() ??
                            GetComponentInChildren<EntityWeaponHolder>() ??
                            GetComponentInParent<EntityWeaponHolder>();

        ResolveConfig();
    }

    private void EnsureMagazineReady(BulletSO bullet)
    {
        if (_loadedBullet == bullet && _magazineAmmo > 0)
            return;

        _loadedBullet = bullet;
        _magazineAmmo = ResolveMagazineSize(bullet);
        _isReloading = false;
        _reloadEndTime = 0f;
    }

    private void BeginReload()
    {
        if (_isReloading)
            return;

        float duration = ResolveReloadDuration();

        if (duration <= 0f)
        {
            _magazineAmmo = ResolveMagazineSize(_loadedBullet);
            _isReloading = false;
            _reloadEndTime = 0f;
            return;
        }

        _isReloading = true;
        _reloadEndTime = Time.time + duration;
        ReloadStarted?.Invoke();
    }

    private void TickReload()
    {
        if (!_isReloading || Time.time < _reloadEndTime)
            return;

        _magazineAmmo = ResolveMagazineSize(_loadedBullet);
        _isReloading = false;
        _reloadEndTime = 0f;
        ReloadFinished?.Invoke();
    }

    private int ResolveMagazineSize(BulletSO bullet)
    {
        if (_statsRuntime != null)
            return _statsRuntime.ResolveMagazineSize(bullet);

        EnemyShooterConfigSO config = ResolveConfig();
        return config != null ? config.MagazineSize : 1;
    }

    private float ResolveReloadDuration()
    {
        if (_statsRuntime != null)
            return Mathf.Max(0f, _statsRuntime.ReloadDurationSeconds);

        EnemyShooterConfigSO config = ResolveConfig();
        return config != null ? config.ReloadDurationSeconds : 0f;
    }

    private void PublishGunshotSound()
    {
        if (_soundStimulusEvent == null || _statsRuntime == null)
            return;

        Transform fireOrigin = VisualFireOrigin;
        Vector3 position = fireOrigin != null ? fireOrigin.position : transform.position;

        _soundStimulusEvent.RaiseEvent(new SoundStimulus(
            gameObject,
            position,
            _statsRuntime.GunshotSoundRadius,
            _statsRuntime.SoundInvestigateDelaySeconds,
            SoundStimulusType.Gunshot));
    }
}