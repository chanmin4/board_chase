using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Mutarus_Pattern_Config",
    menuName = "ScriptableObject/GameScene/Enemy/Definition_Config/NamedEnemy/Mutarus/Pattern Config")]
public class MutarusPatternConfigSO : NamedPatternConfigSO
{
    [Serializable]
    public class QTEPatternBombSettings
    {
        [Header("Periodic Arc Bomb")]
        [SerializeField] private bool _usePeriodicArcBomb = true;
        [SerializeField] private EnemyArcBombProjectile _arcBombPrefab;
        [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

        [Tooltip("If true, the first burst starts as soon as the QTE runtime becomes ready.")]
        [SerializeField] private bool _fireBombOnEnter;

        [Tooltip("Delay before the first burst when Fire Bomb On Enter is false.")]
        [SerializeField, Min(0f)] private float _firstBombDelay = 2f;

        [Tooltip("Long delay after each burst finishes. Pattern keeps repeating bursts while active.")]
        [SerializeField, Min(0.01f)] private float _bombInterval = 5f;

        [Header("Periodic Arc Bomb Burst")]
        [SerializeField, Min(1)] private int _bombsPerBurst = 1;
        [SerializeField, Min(0f)] private float _bombShotInterval = 0.25f;

        [Header("Arc Bomb Trajectory")]
        [SerializeField, Min(0.01f)] private float _bombTravelTime = 1.2f;
        [SerializeField, Min(0f)] private float _bombArcHeight = 4f;
        [SerializeField] private float _bombSpawnYOffset = 0.5f;
        [SerializeField] private float _bombTargetYOffset;
        [SerializeField, Min(0f)] private float _fallbackTargetDistance = 6f;
        [SerializeField, Min(0f)] private float _targetRandomRadius;

        [Header("Arc Bomb Facing")]
        [Tooltip("If true, Mutarus rotates toward the bomb target before firing pattern bombs.")]
        [SerializeField] private bool _faceTargetOnBomb = true;

        [Tooltip("If true, Mutarus immediately snaps toward the bomb target when a pattern bomb fires.")]
        [SerializeField] private bool _snapFacingOnBomb = true;

        [Tooltip("If true, Mutarus keeps rotating toward the player while a pattern bomb burst is active.")]
        [SerializeField] private bool _faceTargetWhileBombing = true;

        [Tooltip("Rotation speed used when Snap Facing On Bomb is false or while tracking during a bomb burst.")]
        [SerializeField, Min(1f)] private float _bombFacingRotationSpeedDegPerSec = 1440f;

        [Header("Arc Bomb Flight")]
        [SerializeField] private bool _disableProjectileCollidersDuringFlight = true;

        [Header("Arc Bomb Impact Damage")]
        [SerializeField] private ArcBombRadiusSource _damageRadiusSource = ArcBombRadiusSource.PaintRadiusWorld;
        [SerializeField, Min(0f)] private float _damageRadius = 1.5f;
        [SerializeField, Min(0f)] private float _impactHealthDamage = 10f;
        [SerializeField, Min(0f)] private float _impactInfectionDamage = 5f;
        [SerializeField] private LayerMask _damageTargetMask;
        [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide;

        [Header("Arc Bomb Impact Telegraph")]
        [SerializeField] private bool _showImpactTelegraph = true;
        [SerializeField] private ArcBombRadiusSource _telegraphRadiusSource = ArcBombRadiusSource.PaintRadiusWorld;
        [SerializeField, Min(0f)] private float _telegraphRadiusCustom = 1.5f;
        [SerializeField] private AreaAttackTelegraphStyle _impactTelegraphStyle = default;

        [Header("Arc Bomb Paint")]
        [SerializeField] private PaintChannel _paintChannel = PaintChannel.PoisonPuddle;
        [SerializeField, Min(0f)] private float _paintRadiusWorld = 1.5f;
        [SerializeField] private int _paintPriority;
        [SerializeField] private PoisonPuddleDamageConfigSO _poisonPuddleDamageConfig;

        [Header("Pattern Failed Bomb")]
        [SerializeField] private bool _fireBombOnPatternFailed = true;
        [SerializeField] private EnemyArcBombProjectile _failureArcBombPrefabOverride;
        [SerializeField] private bool _failureBombTargetSectorCenter = true;
        [SerializeField, Min(0.01f)] private float _failureBombTravelTime = 1.5f;
        [SerializeField, Min(0f)] private float _failureBombArcHeight = 6f;
        [SerializeField] private float _failureBombSpawnYOffset = 0.5f;
        [SerializeField] private float _failureBombTargetYOffset;

        [Header("Pattern Failed Bomb Impact Damage")]
        [SerializeField] private ArcBombRadiusSource _failureDamageRadiusSource = ArcBombRadiusSource.PaintRadiusWorld;
        [SerializeField, Min(0f)] private float _failureDamageRadius = 5f;
        [SerializeField, Min(0f)] private float _failureImpactHealthDamage = 25f;
        [SerializeField, Min(0f)] private float _failureImpactInfectionDamage = 20f;

        [Header("Pattern Failed Bomb Impact Telegraph")]
        [SerializeField] private ArcBombRadiusSource _failureTelegraphRadiusSource = ArcBombRadiusSource.PaintRadiusWorld;
        [SerializeField, Min(0f)] private float _failureTelegraphRadiusCustom = 5f;

        [Header("Pattern Failed Bomb Paint")]
        [SerializeField] private PaintChannel _failurePaintChannel = PaintChannel.PoisonPuddle;
        [SerializeField, Min(0f)] private float _failurePaintRadiusWorld = 5f;
        [SerializeField] private int _failurePaintPriority = 10;
        [SerializeField] private PoisonPuddleDamageConfigSO _failurePoisonPuddleDamageConfig;

        [Header("Periodic Bomb Animator")]
        [SerializeField] private bool _triggerAnimatorOnBomb = true;
        [SerializeField] private string _bombAnimatorTrigger = "PatternBomb";

        [Header("Failure Bomb Animator")]
        [SerializeField] private bool _triggerAnimatorOnFailureBomb = true;
        [SerializeField] private string _failureBombAnimatorTrigger = "PatternFailedBomb";

        public bool UsePeriodicArcBomb => _usePeriodicArcBomb;
        public EnemyArcBombProjectile ArcBombPrefab => _arcBombPrefab;
        public MaskRenderManagerEventChannelSO MaskRenderManagerReadyChannel => _maskRenderManagerReadyChannel;
        public bool FireBombOnEnter => _fireBombOnEnter;
        public float FirstBombDelay => Mathf.Max(0f, _firstBombDelay);
        public float BombInterval => Mathf.Max(0.01f, _bombInterval);
        public int BombsPerBurst => Mathf.Max(1, _bombsPerBurst);
        public float BombShotInterval => Mathf.Max(0f, _bombShotInterval);
        public float BombTravelTime => Mathf.Max(0.01f, _bombTravelTime);
        public float BombArcHeight => Mathf.Max(0f, _bombArcHeight);
        public float BombSpawnYOffset => _bombSpawnYOffset;
        public float BombTargetYOffset => _bombTargetYOffset;
        public float FallbackTargetDistance => Mathf.Max(0f, _fallbackTargetDistance);
        public float TargetRandomRadius => Mathf.Max(0f, _targetRandomRadius);
        public bool FaceTargetOnBomb => _faceTargetOnBomb;
        public bool SnapFacingOnBomb => _snapFacingOnBomb;
        public bool FaceTargetWhileBombing => _faceTargetWhileBombing;
        public float BombFacingRotationSpeedDegPerSec => Mathf.Max(1f, _bombFacingRotationSpeedDegPerSec);
        public bool DisableProjectileCollidersDuringFlight => _disableProjectileCollidersDuringFlight;

        public float DamageRadius => ResolveRadius(_damageRadiusSource, Mathf.Max(0f, _damageRadius), Mathf.Max(0f, _damageRadius), PaintRadiusWorld);
        public float ImpactHealthDamage => DifficultyRuntime.ApplyEnemyDamage(_impactHealthDamage);
        public float ImpactInfectionDamage => DifficultyRuntime.ApplyPlayerInfectionGain(_impactInfectionDamage);
        public LayerMask DamageTargetMask => _damageTargetMask;
        public QueryTriggerInteraction TriggerInteraction => _triggerInteraction;
        public bool ShowImpactTelegraph => _showImpactTelegraph;
        public float ImpactTelegraphRadius => ResolveRadius(_telegraphRadiusSource, Mathf.Max(0f, _telegraphRadiusCustom), DamageRadius, PaintRadiusWorld);
        public AreaAttackTelegraphStyle ImpactTelegraphStyle =>
            _impactTelegraphStyle.segments > 0 ? _impactTelegraphStyle : AreaAttackTelegraphStyle.Default;
        public PaintChannel PaintChannel => _paintChannel;
        public float PaintRadiusWorld => DifficultyRuntime.ApplyEnemyVirusPaintRadius(_paintRadiusWorld);
        public int PaintPriority => _paintPriority;
        public PoisonPuddleDamageConfigSO PoisonPuddleDamageConfig => _poisonPuddleDamageConfig;

        public bool FireBombOnPatternFailed => _fireBombOnPatternFailed;
        public EnemyArcBombProjectile FailureArcBombPrefab =>
            _failureArcBombPrefabOverride != null ? _failureArcBombPrefabOverride : _arcBombPrefab;
        public bool FailureBombTargetSectorCenter => _failureBombTargetSectorCenter;
        public float FailureBombTravelTime => Mathf.Max(0.01f, _failureBombTravelTime);
        public float FailureBombArcHeight => Mathf.Max(0f, _failureBombArcHeight);
        public float FailureBombSpawnYOffset => _failureBombSpawnYOffset;
        public float FailureBombTargetYOffset => _failureBombTargetYOffset;
        public float FailurePaintRadiusWorld => DifficultyRuntime.ApplyEnemyVirusPaintRadius(_failurePaintRadiusWorld);
        public float FailureDamageRadius => ResolveRadius(_failureDamageRadiusSource, Mathf.Max(0f, _failureDamageRadius), Mathf.Max(0f, _failureDamageRadius), FailurePaintRadiusWorld);
        public float FailureImpactHealthDamage => DifficultyRuntime.ApplyEnemyDamage(_failureImpactHealthDamage);
        public float FailureImpactInfectionDamage => DifficultyRuntime.ApplyPlayerInfectionGain(_failureImpactInfectionDamage);
        public float FailureImpactTelegraphRadius => ResolveRadius(_failureTelegraphRadiusSource, Mathf.Max(0f, _failureTelegraphRadiusCustom), FailureDamageRadius, FailurePaintRadiusWorld);
        public PaintChannel FailurePaintChannel => _failurePaintChannel;
        public int FailurePaintPriority => _failurePaintPriority;
        public PoisonPuddleDamageConfigSO FailurePoisonPuddleDamageConfig =>
            _failurePoisonPuddleDamageConfig != null ? _failurePoisonPuddleDamageConfig : _poisonPuddleDamageConfig;
        public bool TriggerAnimatorOnBomb => _triggerAnimatorOnBomb;
        public string BombAnimatorTrigger => _bombAnimatorTrigger;
        public bool TriggerAnimatorOnFailureBomb => _triggerAnimatorOnFailureBomb;
        public string FailureBombAnimatorTrigger => _failureBombAnimatorTrigger;
    }

    [Header("QTE Pattern Bomb")]
    [SerializeField] private QTEPatternBombSettings _qtePatternBomb = new QTEPatternBombSettings();

    [Header("Pattern Facing")]
    [Tooltip("If true, Mutarus keeps facing the player while the QTE pattern action is active.")]
    [SerializeField] private bool _facePlayerWhilePatternActive = true;

    [Tooltip("If true, Mutarus immediately snaps toward the player when the QTE pattern starts.")]
    [SerializeField] private bool _snapFacingOnPatternEnter = true;

    [Tooltip("Rotation speed while tracking the player during the QTE pattern.")]
    [SerializeField, Min(1f)] private float _patternFacingRotationSpeedDegPerSec = 1440f;

    private QTEPatternBombSettings QTEPatternBomb => _qtePatternBomb ?? (_qtePatternBomb = new QTEPatternBombSettings());

    public bool FacePlayerWhilePatternActive => _facePlayerWhilePatternActive;
    public bool SnapFacingOnPatternEnter => _snapFacingOnPatternEnter;
    public float PatternFacingRotationSpeedDegPerSec => Mathf.Max(1f, _patternFacingRotationSpeedDegPerSec);

    public bool UsePeriodicArcBomb => QTEPatternBomb.UsePeriodicArcBomb;
    public EnemyArcBombProjectile ArcBombPrefab => QTEPatternBomb.ArcBombPrefab;
    public MaskRenderManagerEventChannelSO MaskRenderManagerReadyChannel => QTEPatternBomb.MaskRenderManagerReadyChannel;
    public bool FireBombOnEnter => QTEPatternBomb.FireBombOnEnter;
    public float FirstBombDelay => QTEPatternBomb.FirstBombDelay;
    public float BombInterval => QTEPatternBomb.BombInterval;
    public int BombsPerBurst => QTEPatternBomb.BombsPerBurst;
    public float BombShotInterval => QTEPatternBomb.BombShotInterval;
    public float BombTravelTime => QTEPatternBomb.BombTravelTime;
    public float BombArcHeight => QTEPatternBomb.BombArcHeight;
    public float BombSpawnYOffset => QTEPatternBomb.BombSpawnYOffset;
    public float BombTargetYOffset => QTEPatternBomb.BombTargetYOffset;
    public float FallbackTargetDistance => QTEPatternBomb.FallbackTargetDistance;
    public float TargetRandomRadius => QTEPatternBomb.TargetRandomRadius;
    public bool FaceTargetOnBomb => QTEPatternBomb.FaceTargetOnBomb;
    public bool SnapFacingOnBomb => QTEPatternBomb.SnapFacingOnBomb;
    public bool FaceTargetWhileBombing => QTEPatternBomb.FaceTargetWhileBombing;
    public float BombFacingRotationSpeedDegPerSec => QTEPatternBomb.BombFacingRotationSpeedDegPerSec;
    public bool DisableProjectileCollidersDuringFlight => QTEPatternBomb.DisableProjectileCollidersDuringFlight;
    public float DamageRadius => QTEPatternBomb.DamageRadius;
    public float ImpactHealthDamage => QTEPatternBomb.ImpactHealthDamage;
    public float ImpactInfectionDamage => QTEPatternBomb.ImpactInfectionDamage;
    public LayerMask DamageTargetMask => QTEPatternBomb.DamageTargetMask;
    public QueryTriggerInteraction TriggerInteraction => QTEPatternBomb.TriggerInteraction;
    public bool ShowImpactTelegraph => QTEPatternBomb.ShowImpactTelegraph;
    public float ImpactTelegraphRadius => QTEPatternBomb.ImpactTelegraphRadius;
    public AreaAttackTelegraphStyle ImpactTelegraphStyle => QTEPatternBomb.ImpactTelegraphStyle;
    public PaintChannel PaintChannel => QTEPatternBomb.PaintChannel;
    public float PaintRadiusWorld => QTEPatternBomb.PaintRadiusWorld;
    public int PaintPriority => QTEPatternBomb.PaintPriority;
    public PoisonPuddleDamageConfigSO PoisonPuddleDamageConfig => QTEPatternBomb.PoisonPuddleDamageConfig;
    public bool FireBombOnPatternFailed => QTEPatternBomb.FireBombOnPatternFailed;
    public EnemyArcBombProjectile FailureArcBombPrefab => QTEPatternBomb.FailureArcBombPrefab;
    public bool FailureBombTargetSectorCenter => QTEPatternBomb.FailureBombTargetSectorCenter;
    public float FailureBombTravelTime => QTEPatternBomb.FailureBombTravelTime;
    public float FailureBombArcHeight => QTEPatternBomb.FailureBombArcHeight;
    public float FailureBombSpawnYOffset => QTEPatternBomb.FailureBombSpawnYOffset;
    public float FailureBombTargetYOffset => QTEPatternBomb.FailureBombTargetYOffset;
    public float FailureDamageRadius => QTEPatternBomb.FailureDamageRadius;
    public float FailureImpactHealthDamage => QTEPatternBomb.FailureImpactHealthDamage;
    public float FailureImpactInfectionDamage => QTEPatternBomb.FailureImpactInfectionDamage;
    public float FailureImpactTelegraphRadius => QTEPatternBomb.FailureImpactTelegraphRadius;
    public PaintChannel FailurePaintChannel => QTEPatternBomb.FailurePaintChannel;
    public float FailurePaintRadiusWorld => QTEPatternBomb.FailurePaintRadiusWorld;
    public int FailurePaintPriority => QTEPatternBomb.FailurePaintPriority;
    public PoisonPuddleDamageConfigSO FailurePoisonPuddleDamageConfig => QTEPatternBomb.FailurePoisonPuddleDamageConfig;
    public bool TriggerAnimatorOnBomb => QTEPatternBomb.TriggerAnimatorOnBomb;
    public string BombAnimatorTrigger => QTEPatternBomb.BombAnimatorTrigger;
    public bool TriggerAnimatorOnFailureBomb => QTEPatternBomb.TriggerAnimatorOnFailureBomb;
    public string FailureBombAnimatorTrigger => QTEPatternBomb.FailureBombAnimatorTrigger;

    private static float ResolveRadius(
        ArcBombRadiusSource source,
        float customRadius,
        float damageRadius,
        float paintRadiusWorld)
    {
        switch (source)
        {
            case ArcBombRadiusSource.DamageRadius:
                return Mathf.Max(0f, damageRadius);

            case ArcBombRadiusSource.Custom:
                return Mathf.Max(0f, customRadius);

            case ArcBombRadiusSource.PaintRadiusWorld:
            default:
                return Mathf.Max(0f, paintRadiusWorld);
        }
    }
}
