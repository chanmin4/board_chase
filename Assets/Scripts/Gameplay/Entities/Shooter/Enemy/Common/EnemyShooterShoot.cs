/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > Common
 * Primary responsibility: Implements the focused EnemyShooterShoot responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class EnemyShooterShoot : EntityShootCore
{
    [Header("Config")]
    [SerializeField] private EnemyShooterConfigSO _config;

    [Header("Stats Runtime")]
    [SerializeField] private EnemyShooterStatsRuntime _statsRuntime;

    [Header("Equipment")]
    [FormerlySerializedAs("_weaponHolder")]
    [SerializeField] private ShooterEquipmentController _equipmentController;

    [Header("Events")]
    [SerializeField] private SoundStimulusEventChannelSO _soundStimulusEvent;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = false;
    [SerializeField] private bool _debugDraw = false;
    [SerializeField, Min(0f)] private float _debugDrawDuration = 0.15f;

    private ShooterFaction ShooterSide => Config != null ? Config.Faction : ShooterFaction.Scraper;
    private const PaintMarkFaction HitMarkSide = PaintMarkFaction.Virus;

    private Damageable _damageable;
    private Transform _entityRoot;
    private MaskRenderManager _maskRenderManager;
    private BulletSO _runtimeBullet;
    private BulletSO _loadedBullet;
    private int _magazineAmmo;
    private bool _isReloading;
    private float _reloadEndTime;
    private float _nextCompatibilityErrorTime;
    private readonly RaycastHit[] _ballisticPathHits = new RaycastHit[32];

    public event Action ReloadStarted;
    public event Action ReloadFinished;

    public bool IsReloading => _isReloading;
    public int MagazineAmmo => _magazineAmmo;
    public SoundStimulusEventChannelSO SoundStimulusEventChannel => _soundStimulusEvent;
    public RangedWeaponSO CurrentRangedWeapon =>
        _equipmentController != null
            ? _equipmentController.CurrentWeapon as RangedWeaponSO
            : null;

    public EnemyShooterConfigSO Config => ResolveConfig();

    public BulletSO CurrentBullet => _runtimeBullet;

    private Transform VisualFireOrigin =>
        _equipmentController != null && _equipmentController.VisualFireOrigin != null
            ? _equipmentController.VisualFireOrigin
            : null;

    private Transform RangeOrigin => transform;

    private Transform ProjectilesRoot =>
        _equipmentController != null ? _equipmentController.ProjectilesRoot : null;

    private GameObject ProjectileSource
    {
        get
        {
            if (_entityRoot == null)
                _entityRoot = EntityComponentResolver.ResolveEntityRoot(this);

            return _entityRoot != null ? _entityRoot.gameObject : gameObject;
        }
    }

    // Purpose: Attempts to get aim laser ray and reports whether the request succeeded.
    public bool TryGetAimLaserRay(
        Vector3 aimWorldPoint,
        out Vector3 origin,
        out Vector3 direction,
        out float maxDistance)
    {
        ResolveRefs();

        origin = default;
        direction = default;
        maxDistance = 0f;

        RangedWeaponSO weapon = CurrentRangedWeapon;
        Transform fireOrigin = VisualFireOrigin;
        EnemyShooterConfigSO config = ResolveConfig();

        if (weapon == null || !weapon.ShowEnemyAimLaser || fireOrigin == null || config == null)
            return false;

        origin = fireOrigin.position;
        Vector3 aimDirection = aimWorldPoint - origin;

        if (aimDirection.sqrMagnitude < 0.0001f)
            return false;

        direction = aimDirection.normalized;
        maxDistance = Mathf.Max(
            0.1f,
            _statsRuntime != null ? _statsRuntime.MaxRange : weapon.MaxRange);
        return true;
    }

    // Purpose: Attempts to get aim origin and reports whether the request succeeded.
    public bool TryGetAimOrigin(out Vector3 origin)
    {
        ResolveRefs();
        Transform fireOrigin = VisualFireOrigin;
        origin = fireOrigin != null ? fireOrigin.position : default;
        return fireOrigin != null;
    }

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
        EnsureFootstepSoundEmitter();
    }

    // Purpose: Updates the config state.
    public void SetConfig(EnemyShooterConfigSO config)
    {
        _config = config;

        if (_statsRuntime != null)
            _statsRuntime.SetConfig(config);
    }

    // Purpose: Updates the runtime bullet state.
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

    // Purpose: Checks whether this component has clear ballistic path to.
    public bool HasClearBallisticPathTo(
        Vector3 aimWorldPoint,
        Transform allowedTarget)
    {
        ResolveRefs();

        Vector3 rootPosition = _entityRoot != null
            ? _entityRoot.position
            : transform.position;

        return HasClearBallisticPathFromRootPosition(
            rootPosition,
            aimWorldPoint,
            allowedTarget);
    }

    // Purpose: Checks whether this component has clear ballistic path from root position.
    public bool HasClearBallisticPathFromRootPosition(
        Vector3 shooterRootWorldPosition,
        Vector3 aimWorldPoint,
        Transform allowedTarget)
    {
        ResolveRefs();

        BulletSO bullet = CurrentBullet;
        Transform fireOrigin = VisualFireOrigin;

        if (bullet == null || fireOrigin == null)
            return false;

        Transform sourceRoot = _entityRoot != null ? _entityRoot : transform;
        Transform targetRoot = allowedTarget != null
            ? EntityComponentResolver.ResolveEntityRoot(allowedTarget)
            : null;

        Vector3 rootDelta = shooterRootWorldPosition - sourceRoot.position;
        Vector3 firePosition = fireOrigin.position + rootDelta;
        Vector3 planarAimPoint = aimWorldPoint;
        planarAimPoint.y = firePosition.y;

        Vector3 direction = planarAimPoint - firePosition;
        float targetDistance = direction.magnitude;

        if (targetDistance <= 0.001f)
            return true;

        direction /= targetDistance;

        float spawnOffset = Mathf.Min(
            bullet.SpawnOffset,
            Mathf.Max(0f, targetDistance - 0.001f));
        Vector3 castStart = firePosition + direction * spawnOffset;
        float castDistance = targetDistance - spawnOffset;

        if (castDistance <= 0.001f || bullet.ProjectileCollisionMask.value == 0)
            return true;

        int hitCount = Physics.SphereCastNonAlloc(
            castStart,
            bullet.CastRadius,
            direction,
            _ballisticPathHits,
            castDistance,
            bullet.ProjectileCollisionMask,
            bullet.ProjectileTriggerInteraction);

        Collider closestCollider = null;
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _ballisticPathHits[i];
            Collider collider = hit.collider;

            if (collider == null || IsColliderBelowRoot(collider, sourceRoot))
                continue;

            if (hit.distance >= closestDistance)
                continue;

            closestCollider = collider;
            closestDistance = hit.distance;
        }

        return closestCollider == null ||
               IsColliderBelowRoot(closestCollider, targetRoot);
    }

    // Purpose: Attempts to fire at and reports whether the request succeeded.
    public bool TryFireAt(Vector3 aimWorldPoint)
    {
        var stamina = EntityComponentResolver.Resolve<ShooterStaminaRuntime>(this);
        if (stamina != null && stamina.IsSprinting) return false;
        EnemyShooterConfigSO config = ResolveConfig();
        BulletSO bullet = CurrentBullet;

        if (config == null || bullet == null)
            return false;

        RangedWeaponSO rangedWeapon = CurrentRangedWeapon;
        if (rangedWeapon == null || !rangedWeapon.IsCompatibleWith(bullet))
        {
            if (Time.unscaledTime >= _nextCompatibilityErrorTime)
            {
                _nextCompatibilityErrorTime = Time.unscaledTime + 2f;
                Debug.LogError(
                    rangedWeapon == null
                        ? $"[EnemyShooterShoot] A ranged shot requires an equipped RangedWeaponSO. bullet={bullet.name}"
                        : $"[EnemyShooterShoot] Incompatible weapon and bullet. weapon={rangedWeapon.name}, weaponType={rangedWeapon.WeaponType}, required={rangedWeapon.RequiredBulletFamily}, bullet={bullet.name}, family={bullet.Family}",
                    this);
            }

            return false;
        }

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
            case BulletAmmoType.Basic:
                fired = TryFirePrimaryAt(bullet, aimWorldPoint, stats);
                break;

            case BulletAmmoType.Special:
                fired = TryFireSpecialAt(bullet as SpecialBulletSO, aimWorldPoint, stats);
                break;
        }

        if (!fired)
            return false;

        _equipmentController?.PlayMuzzleParticle();
        // Enemy firing consumes ammunition, not weapon condition, and never rolls a malfunction.
        float preserveChance=Mathf.Clamp01((_statsRuntime?.AdditionalStat(PlayerStatId.AmmoPreservationChance)??0f)/100f);
        if(preserveChance<=0f||UnityEngine.Random.value>=preserveChance)
            _magazineAmmo = Mathf.Max(0, _magazineAmmo - 1);
        PublishGunshotSound();

        if (_magazineAmmo <= 0)
            BeginReload();

        return true;
    }

    // Purpose: Attempts to fire primary at and reports whether the request succeeded.
    private bool TryFirePrimaryAt(BulletSO bullet, Vector3 aimWorldPoint, EntityShootStats stats)
    {
        if (bullet == null)
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
            CurrentRangedWeapon,
            gameplayStart,
            gameplayDirection,
            visualDirection,
            maxDistance,
            visualSpawn,
            ProjectilesRoot,
            stats,
            ProjectileSource,
            _soundStimulusEvent,
            null,
            _debugLogs,
            _debugDraw,
            _debugDrawDuration);
    }

    // Purpose: Attempts to fire special at and reports whether the request succeeded.
    private bool TryFireSpecialAt(SpecialBulletSO bullet, Vector3 aimWorldPoint, EntityShootStats stats)
    {
        _maskRenderManager = ResolveMaskRenderManager(_maskRenderManager);

        if (bullet == null)
            return false;

        if (bullet.RequiresMaskRenderManager && _maskRenderManager == null)
        {
            Debug.LogError(
                $"[EnemyShooterShoot] Special bullet requires MaskRenderManager: {bullet.name}",
                bullet);
            return false;
        }

        Transform visualFireOrigin = VisualFireOrigin;
        Transform rangeOrigin = RangeOrigin;

        if (visualFireOrigin == null || rangeOrigin == null)
        {
            Debug.LogError("[EnemyShooterShoot] Fire origin or range origin is missing.", this);
            return false;
        }

        Vector3 visualAimPoint = aimWorldPoint;
        visualAimPoint.y = visualFireOrigin.position.y;

        if (!TryResolveSpecialShotFromAimPoints(
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
                out Vector3 impactTarget))
        {
            return false;
        }

        return TryFireSpecial(
            bullet,
            gameplayStart,
            gameplayDirection,
            visualSpawn,
            visualTarget,
            impactTarget,
            ProjectilesRoot,
            _maskRenderManager,
            ProjectileSource,
            _soundStimulusEvent,
            _debugLogs,
            _debugDraw,
            _debugDrawDuration);
    }

    // Purpose: Builds the stats from the current authored data.
    private EntityShootStats BuildStats(EnemyShooterConfigSO config)
    {
        BulletSO bullet = CurrentBullet;

        return new EntityShootStats(
            _statsRuntime != null ? _statsRuntime.MaxRange : CurrentRangedWeapon != null ? CurrentRangedWeapon.MaxRange : 0f,
            _statsRuntime != null ? _statsRuntime.ResolveAttackDamage(bullet) : bullet != null && CurrentRangedWeapon != null
                ? bullet.HealthDamage * CurrentRangedWeapon.ResolveDamageMultiplierPerProjectile(bullet) : 0f,
            bullet != null ? bullet.ResolveDamageTargetMask(ShooterSide) : default,
            HitMarkSide,
            0f,
            0f,
            _statsRuntime != null ? _statsRuntime.ResolvePenetrationClass(bullet)
                : (bullet != null ? bullet.PenetrationClass : 0f) + (CurrentRangedWeapon != null ? CurrentRangedWeapon.PenetrationClassBonus : 0f),
            _statsRuntime != null ? _statsRuntime.ResolveArmorHealthDurabilityLossMultiplier(bullet) : 1f,
            _statsRuntime != null ? _statsRuntime.ResolveArmorInfectionDurabilityLossMultiplier(bullet) : 0.5f);
    }

    // Purpose: Resolves the config needed by this component.
    private EnemyShooterConfigSO ResolveConfig()
    {
        if (_config != null)
            return _config;

        if (_statsRuntime == null)
            _statsRuntime = EntityComponentResolver.Resolve<EnemyShooterStatsRuntime>(this);

        if (_statsRuntime != null && _statsRuntime.Config != null)
            return _statsRuntime.Config;

        if (_damageable == null)
            _damageable = EntityComponentResolver.Resolve<Damageable>(this);

        _config = _damageable != null
            ? _damageable.StatConfig as EnemyShooterConfigSO
            : null;

        return _config;
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_entityRoot == null)
            _entityRoot = EntityComponentResolver.ResolveEntityRoot(this);

        if (_damageable == null)
            _damageable = EntityComponentResolver.Resolve<Damageable>(this);

        if (_statsRuntime == null)
            _statsRuntime = EntityComponentResolver.Resolve<EnemyShooterStatsRuntime>(this);

        if (_equipmentController == null)
            _equipmentController = EntityComponentResolver.Resolve<ShooterEquipmentController>(this);

        ResolveConfig();
    }

    // Purpose: Ensures footstep sound emitter is ready before the workflow continues.
    private void EnsureFootstepSoundEmitter()
    {
        if (_soundStimulusEvent == null || _statsRuntime == null)
            return;

        EntityFootstepSoundEmitter emitter =
            EntityComponentResolver.Resolve<EntityFootstepSoundEmitter>(this);

        if (emitter == null)
            emitter = _statsRuntime.gameObject.AddComponent<EntityFootstepSoundEmitter>();

        emitter.Configure(_statsRuntime, _soundStimulusEvent, _statsRuntime.transform);
    }

    // Purpose: Ensures magazine ready is ready before the workflow continues.
    private void EnsureMagazineReady(BulletSO bullet)
    {
        if (_loadedBullet == bullet && _magazineAmmo > 0)
            return;

        _loadedBullet = bullet;
        _magazineAmmo = ResolveMagazineSize(bullet);
        _isReloading = false;
        _reloadEndTime = 0f;
    }

    // Purpose: Begins the reload workflow.
    private void BeginReload()
    {
        var stamina = EntityComponentResolver.Resolve<ShooterStaminaRuntime>(this);
        if (stamina != null && stamina.IsSprinting) return;
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

    // Purpose: Processes one update step for reload.
    private void TickReload()
    {
        if (!_isReloading || Time.time < _reloadEndTime)
            return;

        _magazineAmmo = ResolveMagazineSize(_loadedBullet);
        _isReloading = false;
        _reloadEndTime = 0f;
        ReloadFinished?.Invoke();
    }

    // Purpose: Resolves the magazine size needed by this component.
    private int ResolveMagazineSize(BulletSO bullet)
    {
        if (_statsRuntime != null)
            return _statsRuntime.ResolveMagazineSize(bullet);

        return CurrentRangedWeapon != null ? CurrentRangedWeapon.MagazineSize : 1;
    }

    // Purpose: Resolves the reload duration needed by this component.
    private float ResolveReloadDuration()
    {
        if (_statsRuntime != null)
            return Mathf.Max(0f, _statsRuntime.ReloadDurationSeconds);

        return CurrentRangedWeapon != null ? CurrentRangedWeapon.ReloadDurationSeconds : 0f;
    }

    // Purpose: Publishes the gunshot sound notification.
    private void PublishGunshotSound()
    {
        if (_soundStimulusEvent == null || _statsRuntime == null)
            return;

        Transform fireOrigin = VisualFireOrigin;
        Vector3 position = fireOrigin != null ? fireOrigin.position : transform.position;
        // StatsRuntime already includes the equipped weapon's sound modifier.
        float radius = _statsRuntime.GunshotSoundRadius;

        _soundStimulusEvent.RaiseEvent(new SoundStimulus(
            gameObject,
            position,
            radius,
            _statsRuntime.SoundInvestigateDelaySeconds,
            SoundStimulusType.Gunshot));
    }

    // Purpose: Checks whether collider below root is currently true.
    private static bool IsColliderBelowRoot(Collider collider, Transform root)
    {
        if (collider == null || root == null)
            return false;

        Transform colliderTransform = collider.transform;
        return colliderTransform == root || colliderTransform.IsChildOf(root);
    }
}
