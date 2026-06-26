using UnityEngine;

[DisallowMultipleComponent]
public class PlayerShooterShoot : EntityShootCore
{
    [Header("Refs")]
    [SerializeField] private PlayerAimAction _aimAction;
    [SerializeField] private VSplatterRange _range;
    [SerializeField] private PlayerStatsRuntime _statsRuntime;
    [SerializeField] private PlayerBulletLoadoutRuntime _bulletLoadout;
    [SerializeField] private EntityWeaponHolder _weaponHolder;

    [Header("Events")]
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;
    [SerializeField] private ShootHitConfirmedEventChannelSO _shootHitConfirmedEvent;
    [SerializeField] private SoundStimulusEventChannelSO _soundStimulusEvent;

    [Header("Paint")]
    [SerializeField] private PaintChannel _paintChannel = PaintChannel.Vaccine;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = false;
    [SerializeField] private bool _debugDraw = false;
    [SerializeField, Min(0f)] private float _debugDrawDuration = 0.15f;

    private const ShooterFaction ShooterSide = ShooterFaction.Player;
    private const PaintMarkFaction HitMarkSide = PaintMarkFaction.Vaccine;

    private MaskRenderManager _maskRenderManager;

    private Transform VisualFireOrigin =>
        _weaponHolder != null && _weaponHolder.VisualFireOrigin != null
            ? _weaponHolder.VisualFireOrigin
            : null;

    private Transform RangeOrigin =>
        _range != null && _range.RangeOrigin != null
            ? _range.RangeOrigin
            : transform;

    private Transform ProjectilesRoot =>
        _weaponHolder != null ? _weaponHolder.ProjectilesRoot : null;

    private float MaxRange =>
        _range != null
            ? Mathf.Max(0.1f, _range.MaxRange)
            : _statsRuntime != null ? Mathf.Max(0.1f, _statsRuntime.MaxRange) : 0.1f;

    private int PaintPriority =>
        _statsRuntime != null ? _statsRuntime.PaintPriority : 0;

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

    public bool TryFireOnce(BulletAmmoType ammoType)
    {
        ResolveRefs();

        if (_aimAction == null || _bulletLoadout == null)
            return false;

        if (!_aimAction.TryGetAimPoint(out Vector3 aimWorldPoint))
            return false;

        return ammoType switch
        {
            BulletAmmoType.AttackAndPaint => TryFirePrimary(aimWorldPoint),
            BulletAmmoType.Attack => TryFirePrimary(aimWorldPoint),
            BulletAmmoType.Paint => TryFirePaint(aimWorldPoint),
            _ => false
        };
    }

    private bool TryFirePrimary(Vector3 aimWorldPoint)
    {
        if (!_bulletLoadout.TryGetActivePrimaryBullet(out BulletSO bullet))
            return false;

        if (!ValidatePrimaryBullet(bullet))
            return false;

        bool needsPaint = bullet.AmmoType == BulletAmmoType.AttackAndPaint;

        if (needsPaint && !EnsureMaskRenderManager())
            return false;

        EntityShootStats stats = BuildPrimaryStats(bullet, needsPaint);
        Transform visualFireOrigin = VisualFireOrigin;
        Transform rangeOrigin = RangeOrigin;

        if (visualFireOrigin == null || rangeOrigin == null)
        {
            Debug.LogError(
                "[PlayerShooterShoot] Fire origin or range origin is missing. Assign WeaponSO WeaponViewPrefab with WeaponView Fire Origin.",
                this);
            return false;
        }

        aimWorldPoint = ApplyAimError(visualFireOrigin.position, aimWorldPoint);

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

        if (!_bulletLoadout.TryConsumePrimaryAmmo(1, out bullet))
            return false;

        bool fired = TryFirePrimary(
            bullet,
            gameplayStart,
            gameplayDirection,
            visualDirection,
            maxDistance,
            visualSpawn,
            ProjectilesRoot,
            needsPaint ? _maskRenderManager : null,
            stats,
            gameObject,
            _shootHitConfirmedEvent,
            _debugLogs,
            _debugDraw,
            _debugDrawDuration);

        if (fired)
        {
            _aimAction?.ApplyShotRecoil();
            PublishGunshotSound(visualFireOrigin.position);
        }

        return fired;
    }

    private bool TryFirePaint(Vector3 aimWorldPoint)
    {
        if (!EnsureMaskRenderManager())
            return false;

        if (!_bulletLoadout.TryGetActivePaintBullet(out PaintBulletSO bullet))
            return false;

        if (!ValidatePaintBullet(bullet))
            return false;

        EntityShootStats stats = BuildPaintStats(bullet);
        Transform visualFireOrigin = VisualFireOrigin;
        Transform rangeOrigin = RangeOrigin;

        if (visualFireOrigin == null || rangeOrigin == null)
        {
            Debug.LogError(
                "[PlayerShooterShoot] Fire origin or range origin is missing. Assign WeaponSO WeaponViewPrefab with WeaponView Fire Origin.",
                this);
            return false;
        }

        aimWorldPoint = ApplyAimError(visualFireOrigin.position, aimWorldPoint);

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

        if (!_bulletLoadout.TryConsumePaintAmmo(1, out bullet))
            return false;

        bool fired = TryFirePaint(
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

        if (fired)
        {
            _aimAction?.ApplyShotRecoil();
            PublishGunshotSound(visualFireOrigin.position);
        }

        return fired;
    }

    private EntityShootStats BuildPrimaryStats(BulletSO bullet, bool needsPaint)
    {
        return new EntityShootStats(
            MaxRange,
            _statsRuntime != null ? _statsRuntime.ResolveAttackDamage(bullet) : 0f,
            needsPaint && _statsRuntime != null ? _statsRuntime.ResolvePaintRadius(bullet) : 0f,
            PaintPriority,
            _paintChannel,
            bullet != null ? bullet.ResolveDamageTargetMask(ShooterSide) : default,
            HitMarkSide,
            bullet != null ? bullet.PaintMarkAmountOnHit : 0f);
    }

    private EntityShootStats BuildPaintStats(PaintBulletSO bullet)
    {
        return new EntityShootStats(
            MaxRange,
            0f,
            _statsRuntime != null ? _statsRuntime.ResolvePaintRadius(bullet) : 0f,
            PaintPriority,
            _paintChannel);
    }

    private bool EnsureMaskRenderManager()
    {
        _maskRenderManager = ResolveMaskRenderManager(_maskRenderManager);
        return _maskRenderManager != null;
    }

    private void HandleMaskRenderManagerReady(MaskRenderManager manager)
    {
        if (manager != null)
            _maskRenderManager = manager;
    }

    private void ResolveRefs()
    {
        if (_aimAction == null)
            _aimAction = GetComponent<PlayerAimAction>();

        if (_range == null)
            _range = GetComponent<VSplatterRange>();

        if (_statsRuntime == null)
            _statsRuntime = GetComponent<PlayerStatsRuntime>();

        if (_bulletLoadout == null)
            _bulletLoadout = GetComponent<PlayerBulletLoadoutRuntime>();

        if (_weaponHolder == null)
            _weaponHolder = GetComponent<EntityWeaponHolder>();
    }

    private void PublishGunshotSound(Vector3 position)
    {
        if (_soundStimulusEvent == null || _statsRuntime == null)
            return;

        _soundStimulusEvent.RaiseEvent(new SoundStimulus(
            gameObject,
            position,
            _statsRuntime.GunshotSoundRadius,
            _statsRuntime.SoundInvestigateDelaySeconds,
            SoundStimulusType.Gunshot));
    }

    private Vector3 ApplyAimError(Vector3 origin, Vector3 aimWorldPoint)
    {
        return _aimAction != null
            ? _aimAction.ApplyAccuracyAndRecoil(origin, aimWorldPoint)
            : aimWorldPoint;
    }
}