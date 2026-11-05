/*
 * [적 전투 컨트롤러: 공통 데이터와 Unity 생명주기]
 * 이 파일부터 읽으세요. 프리팹에 붙는 MonoBehaviour는 이 클래스 하나입니다.
 * EnemyShooterCombatController.*.cs는 partial로 합쳐지는 같은 클래스의 기능별 파일입니다.
 *
 * 전체 호출 흐름
 *   HFSM의 Action/Condition -> 컨트롤러의 공개 함수/프로퍼티
 *   -> 기능별 Domain(실제 처리) -> 사격/장비/NavMesh 등 캐릭터 컴포넌트
 * 이 컨트롤러가 매 프레임 모든 행동을 돌리는 것은 아닙니다. 행동 Tick은 HFSM에서 호출합니다.
 *
 * 수정할 기능에 따른 파일 안내 (파일명 앞의 EnemyShooterCombatController. 생략)
 *   Perception      : 감지, 타겟 획득/상실, 조준, 피격에 따른 경계
 *   PatrolChase     : 순찰과 추적의 시작/갱신
 *   Investigation   : 소리/마지막 목격 위치 조사, 도착 대기, 다음 행동 목표 선택
 *   RangedAttack    : 사격, 버스트, 발사 간격, 공격 중 이동
 *   EquipmentAttacks: 무기 슬롯 선택, 근접 공격, 소지 투척물 사용
 *   Decisions       : 버스트 마무리 후 공격 변경, 투척의 전술적 이유/최근 목격 위치
 *   Tactics         : 전술 선택, 엄폐 자리 예약, 숨기/피킹, 측면 이동
 *   Evasion         : 대시, 날아오는 투척물/탄환 및 위험 장판 회피
 *   Navigation      : 경로 요청, 도착 판정, 이동 정지, 막힌 추적 복구
 *   Hearing         : 소리 이벤트 수신, 우선순위/중복/거리 필터
 *   Status          : 행동을 막는 상태이상 판정과 정지/복구
 *   Queries         : HFSM과 외부 코드가 읽는 조건/상태/유효 설정
 *   Utilities       : 평면 벡터 계산, 소리 키 등 공통 정적 보조 함수
 *   Diagnostics     : Debug Monitor 스냅샷과 실행 중 테스트용 설정
 *
 * 주의: 공유 필드는 기존 프리팹 직렬화와 호출 호환성을 위해 여기에 남아 있습니다.
 * Domain은 별도 MonoBehaviour가 아니며 _owner를 통해 이 필드들을 사용합니다.
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// HFSM 상태 에셋 자체가 아니라, 현재 인지 정보로 선택한 행동 목표입니다.
public enum EnemyShooterGoal
{
    Patrol,
    Investigate,
    ChaseTarget,
    AttackTarget
}

// 조사에 쓰는 단서 종류: 들은 소리 또는 마지막으로 목격한 타겟 위치.
public enum EnemyShooterInvestigationType
{
    None,
    Sound,
    LastKnownTarget
}

// Attack 상태 내부의 사격 단계. HFSM 상태 전환과는 별도로 버스트/휴식을 추적합니다.
public enum EnemyShooterAttackPhase
{
    None,
    FiringBurst,
    BurstCooldown
}

// 엄폐 전술의 내부 진행 단계: 엄폐 이동 -> 숨기 -> 피킹 이동 -> 노출 -> 복귀.
public enum EnemyShooterCoverTacticPhase
{
    None,
    MovingToCover,
    Hidden,
    MovingToPeek,
    Peeking,
    ReturningToCover
}

[DisallowMultipleComponent]
/*
 * 필드 이름/타입을 바꾸면 프리팹 저장 데이터에 영향을 줍니다.
 * 행동 수정은 해당 기능 파일에서 하고, 공통 참조 확보/이벤트 구독은 이 파일에서 관리합니다.
 * partial 파일을 별도 컴포넌트처럼 프리팹에 추가하지 마세요.
 */
public partial class EnemyShooterCombatController : MonoBehaviour
{
    // ── 위험 회피 검사 상수 (Evasion) ─────────────────────────────────────
    // 시간 간격은 초, 안전 여유/탐색 거리는 월드 단위입니다.
    private const float IncomingThrowableSafetyPadding = 2f;
    private const float IncomingThrowableCheckInterval = 0.05f;
    private const float IncomingThrowableMinimumEscapeDistance = 3f;
    private const int IncomingThrowableEscapeSampleCount = 16;
    private const float IncomingBulletCheckInterval = 0.02f;
    private const float IncomingBulletPredictionSeconds = 0.45f;
    private const float FailedThrowableRetrySeconds = 1f;
    // ── 캐릭터 기능 참조 ─────────────────────────────────────────────────
    // ResolveRefs가 미지정 참조만 보충합니다. Config와 실시간 능력치는 사격/스탯 컴포넌트에서 읽습니다.
    [Header("Refs")]
    [SerializeField]
    private EnemyShooterShoot _shoot;
    [SerializeField]
    private EnemyShooterStatsRuntime _statsRuntime;
    [SerializeField]
    private EntityVisionController _vision;
    [SerializeField]
    private NavMeshAgent _agent;
    [SerializeField]
    private Damageable _damageable;
    [SerializeField]
    private ShooterEffectController _effects;
    [SerializeField]
    private EnemyShooterDashController _dashController;
    [SerializeField]
    private ShooterEquipmentController _equipmentController;
    [SerializeField]
    private EnemyLootInventoryRuntime _lootInventory;
    [SerializeField]
    private ShooterMeleeAttack _meleeAttack;
    [SerializeField]
    private ShooterThrowableAttack _throwableAttack;
    [SerializeField]
    private EntityStatusEffectRuntime _statusEffectRuntime;
    // ── 선택적 외부 이벤트 ───────────────────────────────────────────────
    // OnEnable에서 구독, OnDisable에서 해제합니다. 미지정 이벤트 채널은 사용하지 않습니다.
    [Header("Optional Events")]
    [Tooltip("Optional global hit event. If assigned, this shooter alerts when this object receives a hit event.")]
    [SerializeField]
    private HitReceivedEventChannelSO _hitReceivedEvent;
    [SerializeField]
    private SoundStimulusEventChannelSO _soundStimulusEvent;
    // ── 현재 인지/타겟 기억 (Perception) ──────────────────────────────────
    // 아래 ReadOnly 값은 진행 상태를 보여주는 값이지, 전투 밸런스 설정값이 아닙니다.
    [Header("Runtime Don't Touch")]
    [ReadOnly]
    [SerializeField]
    private Transform _target;
    [ReadOnly]
    [SerializeField]
    private bool _isAlerted;
    [ReadOnly]
    [SerializeField]
    private bool _canSeeTarget;
    [ReadOnly]
    [SerializeField]
    private bool _hasLastKnownPosition;
    [ReadOnly]
    [SerializeField]
    private Vector3 _lastKnownPosition;
    [ReadOnly]
    [SerializeField]
    private float _lastSeenTime = -1f;
    [ReadOnly]
    [SerializeField]
    private float _targetLockUntilTime = -1f;
    [ReadOnly]
    [SerializeField]
    private bool _attackActive;
    // ── 현재 조사 단서 (Investigation/Hearing) ────────────────────────────
    [ReadOnly]
    [SerializeField]
    private bool _hasSoundInvestigation;
    [ReadOnly]
    [SerializeField]
    private Vector3 _soundInvestigationPosition;
    [ReadOnly]
    [SerializeField]
    private float _soundInvestigationMoveTime;
    [ReadOnly]
    [SerializeField]
    private SoundStimulusType _soundInvestigationStimulusType = SoundStimulusType.Unknown;
    [ReadOnly]
    [SerializeField]
    private int _soundInvestigationPriority;
    [ReadOnly]
    [SerializeField]
    private int _soundInvestigationEmitterId;
    // ── 행동 목표와 내부 진행 단계 ────────────────────────────────────────
    [ReadOnly]
    [SerializeField]
    private EnemyShooterGoal _currentGoal = EnemyShooterGoal.Patrol;
    [ReadOnly]
    [SerializeField]
    private EnemyShooterInvestigationType _currentInvestigationType = EnemyShooterInvestigationType.None;
    [ReadOnly]
    [SerializeField]
    private EnemyShooterAttackPhase _attackPhase = EnemyShooterAttackPhase.None;
    [ReadOnly]
    [SerializeField]
    private bool _patrolDestinationReached;
    [ReadOnly]
    [SerializeField]
    private bool _investigationArrived;
    // ── 다음에 처리할 소리와 마지막 소리 판정 결과 ────────────────────────
    [ReadOnly]
    [SerializeField]
    private bool _hasQueuedSoundInvestigation;
    [ReadOnly]
    [SerializeField]
    private SoundStimulusType _queuedSoundInvestigationStimulusType = SoundStimulusType.Unknown;
    [ReadOnly]
    [SerializeField]
    private int _queuedSoundInvestigationPriority;
    [ReadOnly]
    [SerializeField]
    private string _lastSoundStimulusResult = "None";
    [ReadOnly]
    [SerializeField]
    private float _lastSoundStimulusDistance = -1f;
    [ReadOnly]
    [SerializeField]
    private float _lastSoundStimulusRadius;
    // ── 긴급 투척물 회피 (Evasion) ────────────────────────────────────────
    [ReadOnly]
    [SerializeField]
    private bool _isEvadingIncomingThrowable;
    [ReadOnly]
    [SerializeField]
    private int _incomingThrowableId;
    [ReadOnly]
    [SerializeField]
    private Vector3 _incomingThrowableDangerCenter;
    [ReadOnly]
    [SerializeField]
    private float _incomingThrowableDangerRadius;
    [ReadOnly]
    [SerializeField]
    private Vector3 _incomingThrowableEscapeDestination;
    // ── 엄폐 자리와 피킹 진행 상태 (Tactics) ──────────────────────────────
    [ReadOnly]
    [SerializeField]
    private EnemyShooterCoverPoint _reservedCoverPoint;
    [ReadOnly]
    [SerializeField]
    private EnemyShooterCoverPoint _runtimeAutoCoverPoint;
    [ReadOnly]
    [SerializeField]
    private EnemyShooterCoverTacticPhase _coverTacticPhase;
    [ReadOnly]
    [SerializeField]
    private Vector3 _coverPeekDestination;
    // ── 로그 출력 옵션 ────────────────────────────────────────────────────
    [Header("Debug")]
    [SerializeField]
    private bool _debugMovement;
    [SerializeField, Min(0.1f)]
    private float _debugMovementInterval = 1f;
    [Tooltip("Logs sound-stimulus distance, priority, emitter ID, and exact accept/reject reason. This can be verbose during automatic fire.")]
    [SerializeField]
    private bool _debugSoundStimuli;
    // ── 타겟 캐시 / 순찰 기준점 / 인지 반응 시간 ───────────────────────────
    // *Until/*Time 값 대부분은 남은 시간이 아니라 Time.time 기준의 절대 시각입니다.
    private Damageable _targetDamageable;
    private Rigidbody _targetRigidbody;
    private Transform _targetAimAnchor;
    private Vector3 _homePosition;
    private Vector3 _patrolDestination;
    private bool _hasPatrolDestination;
    private float _nextPatrolRefreshTime;
    private float _patrolIdleUntil;
    private float _nextTargetSearchTime;
    private float _reactionStartedTime = -1f;
    private float _activeReactionDelaySeconds;
    private bool _hasSeenTargetDuringCurrentEngagement;
    // ── 사격 타이머와 현재 버스트 (RangedAttack) ───────────────────────────
    private float _nextFireTime;
    [ReadOnly]
    [SerializeField]
    private int _burstShotsRemaining;
    [ReadOnly]
    [SerializeField]
    private float _nextBurstShotTime;
    [ReadOnly]
    [SerializeField]
    private bool _hasFiredCurrentBurst;
    private float _nextCombatMoveRefreshTime;
    // ── 조사 기억·도착 대기·예약된 소리 (Investigation) ─────────────────────
    private float _lastKnownSetTime = -1f;
    private float _soundInvestigationSetTime = -1f;
    private Vector3 _soundInvestigationDirection;
    private float _investigationFinishTime;
    private float _ignoreInvestigationUntilTime = -1f;
    private Vector3 _queuedSoundInvestigationPosition;
    private float _queuedSoundInvestigationMoveTime;
    private float _queuedSoundInvestigationSetTime = -1f;
    private int _queuedSoundInvestigationEmitterId;
    private Vector3 _queuedSoundInvestigationDirection;
    private float _nextInvestigationRetargetTime = -1f;
    private int _attackStrafeSign = 1;
    // ── 현재 전술 목적지, 엄폐 대기 시간, 조준 방향 ────────────────────────
    [ReadOnly]
    [SerializeField]
    private EnemyShooterTacticalIntent _tacticalIntent;
    [ReadOnly]
    [SerializeField]
    private Vector3 _tacticalDestination;
    private Transform _tacticalIntentTarget;
    private float _nextTacticalIntentRefreshTime;
    private float _coverPhaseEndsAt;
    private Vector3 _aimDirection;
    private float _lastHealth;
    private float _nextHarmfulAreaCheckTime;
    private float _nextIncomingThrowableCheckTime;
    private float _nextIncomingBulletCheckTime;
    private int _lastIncomingBulletThreatId;
    private float _nextMovementDebugTime;
    // ── 근접/투척의 진행 시간과 무기 교체 제한 (EquipmentAttacks) ───────────
    private float _nextMeleeAllowedTime;
    private float _nextCombatWeaponSwitchTime;
    private float _meleeActionFinishTime;
    private bool _meleeActionActive;
    private float _nextThrowableAllowedTime;
    private float _throwableActionFinishTime;
    private bool _throwableActionActive;
    // ── 경로 요청 캐시 (Navigation) ────────────────────────────────────────
    // 같은 목적지로 매번 경로를 재계산하지 않도록 마지막 요청과 실제 도달 가능 지점을 보관합니다.
    private NavMeshPath _navigationPath;
    private bool _hasNavigationRequest;
    private Vector3 _lastNavigationRequest;
    private Vector3 _lastReachableDestination;
    private float _lastNavigationStoppingDistance;
    private float _nextNavigationRetryTime;
    // ── 소리 중복 방지 / 총구 검사 캐시 / 상태이상 전이 감지 ────────────────
    private readonly Dictionary<long, float> _soundStimulusDuplicateUntil = new();
    private readonly List<long> _expiredSoundStimulusKeys = new();
    private float _nextSoundStimulusCleanupTime;
    private int _muzzleClearanceFrame = -1;
    private bool _muzzleClearanceResult;
    private bool _wasStatusActionBlocked;
    // ── Debug Monitor 전용 런타임 덮어쓰기 (Diagnostics/Queries) ────────────
    // 현재 생성된 적 한 개에만 적용합니다. 직렬화하지 않으며 공유 Config SO를 수정하지 않습니다.
    private bool _debugCombatOverridesEnabled;
    private bool _debugUseTacticalIntent;
    private bool _debugUseCoverTactics;
    private bool _debugFireWhileMovingToCover;
    private bool _debugUseDuelToken;
    private bool _debugUseFlankingTactics;
    private bool _debugUseDash;
    private bool _debugDashOnDamaged;
    private bool _debugDashWhenHealthLow;
    private bool _debugUseThrowableAttack;
    private bool _debugUsePreferredRangeBeforeStationaryAttack;
    private float _debugCoverSearchRadius;
    private float _debugCoverHideSeconds;
    private float _debugCoverPeekSeconds;
    private float _debugCoverApproachSpeedMultiplier;
    private float _debugTacticalFlankDistance;
    private float _debugDashChanceOnDamaged;
    private float _debugDashHealthThreshold;
    private float _debugDashCooldownSeconds;
    private float _debugDashDurationSeconds;
    private float _debugDashSpeed;
    private float _debugStationaryAttackKeepRangePadding;
    // 상태이상 시스템 버전별 행동 차단 프로퍼티 이름을 허용하기 위한 호환용 조회 정보입니다.
    // 실제 판정과 기존 stun ID 대체 규칙은 Status 파일에 있습니다.
    private static readonly System.Reflection.PropertyInfo RuntimeBlocksActionsProperty = typeof(EntityStatusEffectRuntime).GetProperty("BlocksActions") ?? typeof(EntityStatusEffectRuntime).GetProperty("BlocksAllActions") ?? typeof(EntityStatusEffectRuntime).GetProperty("IsActionBlocked");
    private static readonly System.Reflection.PropertyInfo DefinitionBlocksActionsProperty = typeof(StatusEffectDefinitionSO).GetProperty("BlocksActions") ?? typeof(StatusEffectDefinitionSO).GetProperty("BlocksAllActions") ?? typeof(StatusEffectDefinitionSO).GetProperty("IsActionBlocked");
    // ── Unity 생명주기: 참조 준비 / 구독 / 해제 ────────────────────────────
    /// <summary>Inspector에서 컴포넌트를 추가/Reset할 때 같은 캐릭터의 기능 참조를 찾습니다.</summary>
    private void Reset()
    {
        ResolveRefs();
    }

    /// <summary>경로 버퍼와 참조를 준비하고, 최초 위치를 순찰 기준점으로 기억합니다.</summary>
    private void Awake()
    {
        _stamina = ShooterStaminaRuntime.EnsureOn(this);
        _navigationPath ??= new NavMeshPath();
        ResolveRefs();
        ConfigureAgent();
        _homePosition = transform.position;
        _lastHealth = _damageable != null ? _damageable.CurrentHealth : 0f;
    }

    /// <summary>활성화될 때 이동 설정을 맞추고 체력·피격·소리 이벤트를 구독합니다.</summary>
    private void OnEnable()
    {
        ResolveRefs();
        ConfigureAgent();
        Navigation.ResetRecovery();
        if (_damageable != null)
            _damageable.OnHealthChanged += OnHealthChanged;
        if (_hitReceivedEvent != null)
            _hitReceivedEvent.OnEventRaised += OnHitReceived;
        if (_soundStimulusEvent != null)
            _soundStimulusEvent.OnEventRaised += OnSoundStimulus;
        DebugMovement($"Enabled. pos={transform.position}, agentNull={_agent == null}, isOnNavMesh={(_agent != null && _agent.isOnNavMesh)}, targetNull={_target == null}");
    }

    /// <summary>구독을 해제하고 대시·회피·엄폐 예약·전술 토큰·이펙트를 정리합니다.</summary>
    private void OnDisable()
    {
        if (_damageable != null)
            _damageable.OnHealthChanged -= OnHealthChanged;
        if (_hitReceivedEvent != null)
            _hitReceivedEvent.OnEventRaised -= OnHitReceived;
        if (_soundStimulusEvent != null)
            _soundStimulusEvent.OnEventRaised -= OnSoundStimulus;
        _dashController?.CancelDash();
        ClearIncomingThrowableEvasion();
        ReleaseCoverTactic();
        EnemyShooterTacticalIntentSelector.ReleaseDuelHolder(this);
        ResetTacticalIntent();
        _wasStatusActionBlocked = false;
        ResetThrowableOpportunity();
        _navigationDomain?.ResetRecovery();
        _effects?.SetFootTrailActive(false);
        _effects?.StopDashParticles();
    }

    /// <summary>HFSM 갱신 뒤 실제 위치 변화로 막힌 전투 이동을 감시하고 발자국 이펙트를 갱신합니다.</summary>
    private void LateUpdate()
    {
        TickStaminaLocomotion();
        Navigation.TickProgressWatch();
        if (_effects == null)
            return;
        if (IsActionBlockedByStatusEffect || IsDashing)
        {
            _effects.SetFootTrailActive(false);
            return;
        }

        bool moving = CanUseAgent() && !_agent.isStopped && _agent.velocity.sqrMagnitude > 0.01f;
        _effects.SetFootTrailActive(moving);
    }

    // ── 참조 연결: 이미 연결된 값은 유지하고 빈 참조만 찾기 ────────────────
    /// <summary>사격·스탯·시야·이동·장비 등 누락된 기능 참조를 캐릭터 계층에서 확보합니다.</summary>
    private void ResolveRefs()
    {
        if (_shoot == null)
            _shoot = ResolveComponentInOwnerHierarchy<EnemyShooterShoot>();
        if (_statsRuntime == null)
            _statsRuntime = ResolveComponentInOwnerHierarchy<EnemyShooterStatsRuntime>();
        if (_vision == null)
            _vision = ResolveComponentInOwnerHierarchy<EntityVisionController>();
        if (_agent == null)
            _agent = ResolveComponentInOwnerHierarchy<NavMeshAgent>();
        if (_damageable == null)
            _damageable = ResolveComponentInOwnerHierarchy<Damageable>();
        if (_effects == null)
            _effects = ResolveComponentInOwnerHierarchy<ShooterEffectController>();
        if (_dashController == null)
            _dashController = ResolveComponentInOwnerHierarchy<EnemyShooterDashController>();
        if (_equipmentController == null)
            _equipmentController = ResolveComponentInOwnerHierarchy<ShooterEquipmentController>();
        if (_lootInventory == null)
            _lootInventory = ResolveComponentInOwnerHierarchy<EnemyLootInventoryRuntime>();
        if (_meleeAttack == null)
            _meleeAttack = ResolveComponentInOwnerHierarchy<ShooterMeleeAttack>();
        if (_throwableAttack == null)
            _throwableAttack = ResolveComponentInOwnerHierarchy<ShooterThrowableAttack>();
        if (_statusEffectRuntime == null)
            _statusEffectRuntime = ResolveComponentInOwnerHierarchy<EntityStatusEffectRuntime>();
    }

    /// <summary>자신 -> 자식 -> 부모와 그 자식 순서로 기능을 찾습니다. 새 컴포넌트는 생성하지 않습니다.</summary>
    private T ResolveComponentInOwnerHierarchy<T>()
        where T : Component
    {
        T component = GetComponent<T>();
        if (component != null)
            return component;
        component = GetComponentInChildren<T>();
        if (component != null)
            return component;
        Transform current = transform.parent;
        while (current != null)
        {
            component = current.GetComponent<T>();
            if (component != null)
                return component;
            component = current.GetComponentInChildren<T>();
            if (component != null)
                return component;
            current = current.parent;
        }

        return null;
    }
}
