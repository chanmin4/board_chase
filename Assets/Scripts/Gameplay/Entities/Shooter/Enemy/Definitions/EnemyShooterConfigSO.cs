/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > Definitions
 * Primary responsibility: Implements the focused EnemyShooterRole responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Serialization;

public enum EnemyShooterRole
{
    // Ordinary combat enemy. Raider, Scraper, Agent, Butcher, and Punisher all use this role.
    StandardEnemy = 0,
    NamedEnemy = 1,
    Boss = 2,
    BossGuard = 3
}

public enum EnemyShooterAttackMovementMode
{
    StopWhileAttacking = 0,
    RepositionBetweenBursts = 1,
    MoveWhileFiring = 2
}

// Zero preserves all existing prefabs/assets without a migration.
public enum ShooterSpawnLookMode { ExistingPrefab = 0, Fixed = 1, RandomPool = 2 }

/// <summary>몹 정의는 하나만 유지한다. 스테이지에 따른 장비 차이는 Config 복제가 아닌 이 목록에 작성한다.</summary>
[System.Serializable]
public sealed class EnemyStageLoadout
{
    [Range(1, 5)] public int stage = 1;
    public ShooterLoadoutTableSO loadout;
}

/// <summary>
/// Authoring-only label for the shared Enemy Shooter FSM.
///
/// Every shooter prefab variant uses the same Mode/Alive transition tables.
/// This label documents the intended capability combination for a config; it
/// never overrides the detailed settings below at runtime. Keep a new enemy on
/// the shared FSM unless it truly needs a different state topology.
/// </summary>
public enum EnemyShooterFsmArchetype
{
    BasicRanged = 0,
    TacticalRanged = 1,
    MeleeOnly = 2,
    HybridPursuer = 3,
    HeavyRanged = 4,
    Custom = 5
}

[CreateAssetMenu(
    fileName = "EnemyShooterConfig",
    menuName = "Game/Enemy/Shooter Config")]
public class EnemyShooterConfigSO : ShooterStatConfigSO
{
    [Header("세력 · 역할과 독립")]
    [SerializeField] private ShooterFaction _faction = ShooterFaction.Scraper;
    public ShooterFaction Faction => _faction;

    [Header("귀속 패시브 · 플레이어와 동일한 효과 규칙")]
    [SerializeField, Range(1, 8)] private int _passiveSlotCount = 2;
    [SerializeField] private PlayerShopItemRarity _passiveMinimumRarity;
    [SerializeField] private PlayerShopItemRarity _passiveMaximumRarity;
    public int PassiveSlotCount => Mathf.Clamp(_passiveSlotCount, 1, 8);
    public PlayerShopItemRarity PassiveMinimumRarity => _passiveMinimumRarity;
    public PlayerShopItemRarity PassiveMaximumRarity => _passiveMaximumRarity;
    [Header("Enemy")]
    [Tooltip("Common enemy identity, rewards, UI layout, and drop reward settings.")]
    [SerializeField] private EnemyCommonSettings _enemy = new();

    [Tooltip("Gameplay role used by encounter validation, boss-room completion, UI, and debugging. Standard Enemy covers Raider, Scraper, Agent, Butcher, and Punisher. This does not automatically change combat behavior.")]
    [SerializeField] private EnemyShooterRole _enemyRole = EnemyShooterRole.StandardEnemy;

    [Header("시체 방탄 드롭 · 절대 내구도 (임시 밸런스)")]
    [SerializeField] private bool _limitCorpseArmorDurability = true;
    [SerializeField] private bool _overrideCommonArmorDrop;
    [SerializeField, HideInInspector] private bool _overrideCommonArmorDropInitialized;
    [SerializeField, HideInInspector] private Vector2 _normalArmorDropDurability = new Vector2(350f, 500f);
    [SerializeField, HideInInspector] private Vector2 _bossArmorDropDurability = new Vector2(500f, 650f);
    public bool LimitCorpseArmorDurability => _limitCorpseArmorDurability;
    public Vector2 ArmorDropDurabilityRange => !_overrideCommonArmorDrop && CommonRules != null
        ? (_enemyRole == EnemyShooterRole.Boss ? CommonRules.BossArmorDropDurability : CommonRules.NormalArmorDropDurability)
        : (_enemyRole == EnemyShooterRole.Boss ? _bossArmorDropDurability : _normalArmorDropDurability);
    public float ResolveDroppedArmorDurability(ArmorItemSO armor, float current, System.Random random)
    {
        if (armor == null) return -1f;
        current = Mathf.Clamp(current, 0f, armor.MaxDurability);
        if (!_limitCorpseArmorDurability) return current;
        Vector2 range = ArmorDropDurabilityRange;
        float minimum = Mathf.Clamp(Mathf.Min(range.x, range.y), 0f, armor.MaxDurability);
        float maximum = Mathf.Clamp(Mathf.Max(range.x, range.y), minimum, armor.MaxDurability);
        return Mathf.Min(current, Mathf.Lerp(minimum, maximum, (float)random.NextDouble()));
    }

    [Header("Shared Shooter FSM")]
    [Tooltip("Authoring label only. All EnemyShooter prefab variants use Enemy_ShooterBase_Mode and Enemy_ShooterBase_Alive. Configure weapons and the capability toggles below to create the actual behavior; do not duplicate FSM tables for ordinary variants.")]
    [SerializeField] private EnemyShooterFsmArchetype _fsmArchetype = EnemyShooterFsmArchetype.BasicRanged;

    [Header("Shared Target Detection")]
    [Tooltip("Off by default. When off, this Shooter uses the common 70m / 60-degree / structure-blocked target-detection shape so Scraper, Raider, Agent, guards, and bosses never silently fall back to the generic 12m vision defaults. Enable only for a deliberately exceptional enemy; the inherited Vision fields then become its runtime source.")]
    [SerializeField] private bool _useIndividualTargetDetection;
    [SerializeField, HideInInspector] private bool _useIndividualTargetDetectionInitialized;

    [Header("Prefab")]
    [Tooltip("Enemy shooter prefab spawned by sector enemy spawners.")]
    [SerializeField] private EnemyShooter _enemyShooterPrefab;

    [Header("Spawn Appearance (visual only)")]
    [SerializeField] private ShooterSpawnLookMode _spawnLookMode;
    [SerializeField] private ShooterCustomizeSO _fixedSpawnLook;
    [SerializeField] private ShooterCustomizePoolSO _spawnLookPool;

    [Header("Stage Equipment (same enemy, optional)")]
    [Tooltip("Only equipment changes by stage. Stats, AI and spawn appearances always use this one config. Unlisted stages use the base Loadout Table.")]
    [SerializeField] private EnemyStageLoadout[] _stageLoadouts = System.Array.Empty<EnemyStageLoadout>();

    public ShooterLoadoutTableSO ResolveLoadoutTable(int stage)
    {
        if (stage >= 1 && stage <= 5 && _stageLoadouts != null)
            foreach (var entry in _stageLoadouts)
                if (entry != null && entry.stage == stage && entry.loadout != null) return entry.loadout;
        return LoadoutTable;
    }

    public ShooterSpawnLookMode SpawnLookMode => _spawnLookMode;
    public ShooterCustomizeSO FixedSpawnLook => _fixedSpawnLook;
    public ShooterCustomizePoolSO SpawnLookPool => _spawnLookPool;
    public bool AllowsRandomSpawnLook => EnemyRole != EnemyShooterRole.Boss && EnemyRole != EnemyShooterRole.NamedEnemy;

    /// <summary>Null means retain the prefab appearance; never changes stats, equipment or colliders.</summary>
    public ShooterCustomizeSO PickSpawnLook()
    {
        if (_spawnLookMode == ShooterSpawnLookMode.ExistingPrefab) return null;
        if (_spawnLookMode == ShooterSpawnLookMode.Fixed || !AllowsRandomSpawnLook) return _fixedSpawnLook;
        return _spawnLookPool != null ? _spawnLookPool.GetRandom() : null;
    }

    [Header("Awareness")]
    [Tooltip("If true, this shooter becomes alerted when damaged by the player.")]
    [SerializeField] private bool _alertOnDamaged = true;

    [Tooltip("How often this shooter scans for a visible player while idle or patrolling.")]
    [SerializeField, Min(0.05f)] private float _targetSearchInterval = 0.35f;

    [Tooltip("How long this shooter remembers the last seen player position after losing sight. Twenty seconds is the intended baseline for a tactical hold-angle standoff.")]
    [SerializeField, Min(0f)] private float _targetMemorySeconds = 20f;
    [Tooltip("경계 중 / 추적 재발견 시 반응 대기 배율. 이 적 SO의 값만 사용합니다.")]
    [SerializeField, Min(0f)] private float _alertReactionMultiplier = .5f;

    [Tooltip("Minimum seconds this shooter keeps a player target after acquiring it. During this lock, occlusion can keep chase alive, but shooting still requires current vision.")]
    [SerializeField, Min(0f)] private float _targetLockMinSeconds = 5f;

    [Tooltip("If true, this shooter chases the last known player position after losing sight.")]
    [SerializeField] private bool _chaseLastKnownPosition = true;

    [Tooltip("How long a last known player position remains valid for investigation after the target is no longer active.")]
    [SerializeField, Min(0f)] private float _lastKnownInvestigationSeconds = 6f;

    [Tooltip("How long a heard sound remains valid after its investigate delay has elapsed.")]
    [SerializeField, Min(0f)] private float _soundInvestigationMemorySeconds = 5f;

    [Tooltip("Short cooldown after finishing an investigation before this shooter can enter investigation again from low-priority clues such as footsteps. Increase this if the enemy flips Patrol <-> Investigate too fast.")]
    [SerializeField, Min(0f)] private float _investigationReenterCooldown = 0.5f;

    [Tooltip("If true, this shooter ignores sound stimuli whose source is another enemy. This prevents enemies from endlessly investigating each other's footsteps.")]
    [SerializeField] private bool _ignoreEnemySoundStimuli = true;

    [Tooltip("Seconds the same enemy ignores a repeated stimulus type from the same projectile. Prevents one fast projectile from continuously retargeting investigation.")]
    [SerializeField, Min(0f)] private float _soundStimulusDuplicateCooldown = 5f;

    [Header("Combat Distance")]
    [Tooltip("If true, Attack Keep Range always uses current MaxRange from stats/loadout. If false, Attack Keep Range uses the manual value below.")]
    [SerializeField] private bool _useMaxRangeAsAttackKeepRange = true;

    [Tooltip("Manual range used to keep attacking after attack has started. Only used when Use Max Range As Attack Keep Range is false.")]
    [SerializeField, Min(0f)] private float _attackKeepRange = 0f;

    [Tooltip("Only affects Stop While Attacking. When enabled, this shooter must chase into Attack Preferred Distance before starting a direct ranged attack, even if its weapon Max Range is larger. Leave disabled for long-range or stationary sentries that should fire at maximum range.")]
    [SerializeField] private bool _usePreferredRangeBeforeStationaryAttack;

    [Tooltip("Extra distance allowed after a stationary attack has started while Use Preferred Range Before Stationary Attack is enabled. This prevents rapid Attack <-> Chase switching near the preferred-distance boundary.")]
    [SerializeField, Min(0f)] private float _stationaryAttackKeepRangePadding = 2f;

    [Tooltip("NavMeshAgent stopping distance while chasing the active target. This is not chase cancel distance. Small values like 1~3 keep the shooter close before it switches to attack.")]
    [SerializeField, Min(0f)] private float _chaseStoppingDistance = 0f;

    [Tooltip("Maximum planar distance before this shooter gives up an active target chase. 0 disables distance-based chase loss. This is not NavMesh stopping distance.")]
    [SerializeField, Min(0f)] private float _chaseLoseRange = 0f;

    [Header("Combat Timing")]
    [Tooltip("Delay before firing when the player is directly seen for the first time in a new engagement.")]
    [FormerlySerializedAs("_reactionTime")]
    [SerializeField, Min(0f)] private float _playerDiscoveryReactionTime = 0.3f;

    [Tooltip("Delay before firing after direct sight of the same player is regained while the shooter is still pursuing their last-known position.")]
    [SerializeField, Min(0f)] private float _pursuitReacquireReactionTime = 0.2f;

    [Tooltip("Number of shots fired in one attack burst.")]
    [SerializeField, Min(1)] private int _burstCount = 1;

    [Tooltip("Delay between shots inside one burst.")]
    [SerializeField, Min(0f)] private float _burstInterval = 0.12f;

    [Tooltip("When enabled, an enemy that already fired at least one shot keeps tracking the player and fires every remaining shot when sight is lost or its muzzle path becomes blocked. Structures still absorb those projectiles. The enemy then finishes its Burst Cooldown before chasing/repositioning; no new burst starts without direct sight and a clear muzzle path.")]
    [SerializeField] private bool _finishStartedBurstAfterSightLost;

    [Header("Combat Movement")]
    [Tooltip("NavMeshAgent acceleration used for both starting and stopping movement. Higher values make shooters stop cleanly before firing instead of sliding. Values around 60~100 suit move speeds near 20.")]
    [SerializeField, Min(0.1f)] private float _navMeshAcceleration = 80f;

    [Tooltip("Controls NavMesh movement during ranged attack. Normal enemies should normally use Stop While Attacking. Harmful-area escape and defensive dash remain explicit exceptions.")]
    [SerializeField] private EnemyShooterAttackMovementMode _attackMovementMode =
        EnemyShooterAttackMovementMode.StopWhileAttacking;

    [Tooltip("How often the combat movement destination refreshes while attacking.")]
    [SerializeField, Min(0.02f)] private float _combatMoveRefreshInterval = 0.1f;

    [Tooltip("Cooldown after one burst ends. Repositioning modes may move during this time; stationary mode waits in place. 0 means use the fire-rate interval from Shots Per Second.")]
    [FormerlySerializedAs("_burstIntervalMoveSeconds")]
    [SerializeField, Min(0f)] private float _burstCooldownSeconds = 0f;

    [Tooltip("Preferred distance from the target while repositioning between bursts. 0 disables distance correction and only strafes. When Use Preferred Range Before Stationary Attack is enabled, this also becomes the direct attack start distance for Stop While Attacking.")]
    [SerializeField, Min(0f)] private float _attackPreferredDistance = 10f;

    [Tooltip("Short NavMesh strafe/reposition distance used between attack bursts.")]
    [SerializeField, Min(0f)] private float _attackStrafeDistance = 3f;

    [Header("Combat Stuck Recovery / 전투 이동 끼임 복구")]
    [Tooltip("실제 몸의 위치가 바뀌지 않는 전투 이동을 감시합니다. 워프나 충돌 무시는 하지 않습니다.")]
    [SerializeField] private bool _useCombatStuckRecovery = true;

    [Tooltip("이 시간 동안 실제 위치 변화가 최소 이동량보다 작으면 공격 조건을 재평가하고 짧은 우회 이동을 시도합니다.")]
    [SerializeField, Min(0.25f)] private float _combatStuckSeconds = 1.25f;

    [Tooltip("NavMesh 속도가 아니라 실제 몸이 이 거리 이상 이동해야 진행한 것으로 판정합니다.")]
    [SerializeField, Min(0.01f)] private float _combatStuckMinimumProgress = 0.2f;

    [Tooltip("끼임 복구 시 옆/뒤로 이동할 거리입니다. 몸 반경에 비해 너무 작으면 자동으로 늘립니다.")]
    [SerializeField, Min(0.25f)] private float _combatStuckStepDistance = 2f;

    [Tooltip("우회 목적지를 유지하는 시간입니다. 도착하거나 직접 공격 조건이 열리면 일찍 종료합니다.")]
    [SerializeField, Min(0.25f)] private float _combatStuckStepSeconds = 1.25f;

    [Header("Tactical Intent")]
    [Tooltip("Enables optional cover, hold-angle, and flank movement. Keep this disabled for simple low-difficulty shooters; use stats, weapons, and these flags instead of separate enemy behavior classes.")]
    [SerializeField] private bool _useTacticalIntent;

    [Tooltip("When enabled, the shooter repositions when its muzzle is blocked by a structure. This only changes movement and never grants or removes permission to fire.")]
    [SerializeField] private bool _useCoverTactics;

    [Tooltip("Allows this shooter to claim one target's duel token. The holder keeps pressure on the last-known angle instead of immediately pushing after sight loss.")]
    [SerializeField] private bool _useDuelToken;

    [Tooltip("Allows a non-holder shooter to widen around a claimed last-known player position instead of entering the same firing lane.")]
    [SerializeField] private bool _useFlankingTactics;

    [Header("Cover Peek")]
    [Tooltip("Maximum planar distance used when this shooter searches authored EnemyShooterCoverPoint objects. A cover point must block the anchor-to-target shot and expose at least one valid peek point.")]
    [SerializeField, Min(0.5f)] private float _coverSearchRadius = 16f;

    [Tooltip("Seconds spent tucked behind the selected cover anchor before each peek attempt.")]
    [SerializeField, Min(0f)] private float _coverHideSeconds = 0.55f;

    [Tooltip("Maximum seconds spent at a peek point before the shooter returns behind cover. Firing still follows the regular burst and reaction rules.")]
    [SerializeField, Min(0.05f)] private float _coverPeekSeconds = 0.8f;

    [Tooltip("Planar arrival distance used for both cover anchor and peek points.")]
    [SerializeField, Min(0.05f)] private float _coverArriveDistance = 0.35f;

    [Tooltip("Seconds between tactical movement decisions. Higher values keep a chosen flank lane stable instead of jittering.")]
    [SerializeField, Min(0.05f)] private float _tacticalIntentRefreshInterval = 0.4f;

    [Tooltip("Planar distance from the last-known player position used when taking a flank lane.")]
    [SerializeField, Min(0.1f)] private float _tacticalFlankDistance = 6f;

    [Tooltip("Angle away from the direct lane used when selecting a left or right flank destination.")]
    [SerializeField, Range(10f, 85f)] private float _tacticalFlankAngleDegrees = 45f;

    [Header("Patrol")]
    [Tooltip("Random patrol radius around the spawn/home position. Set 0 to effectively disable random patrol movement.")]
    [SerializeField, Min(0f)] private float _patrolRadius = 12f;

    [Tooltip("순찰 목적지 도착 후 대기 시간. 전역 보정 없이 이 값을 그대로 사용합니다.")]
    [SerializeField, Min(.5f)] private float _patrolIdleSeconds = 2.25f;

    [Tooltip("Minimum interval before a patrol destination can be refreshed. Patrol still waits at the destination using Patrol Idle Seconds.")]
    [SerializeField, Min(0.05f)] private float _patrolDestinationRefreshInterval = 1.25f;

    [Tooltip("Distance from patrol destination considered arrived.")]
    [SerializeField, Min(0f)] private float _patrolArriveDistance = 0.5f;

    [Tooltip("Max distance used when sampling patrol/chase positions onto the NavMesh.")]
    [SerializeField, Min(0f)] private float _navMeshSampleDistance = 3f;

    [Header("Investigate")]
    [Tooltip("Distance from sound/last-known destination considered arrived.")]
    [SerializeField, Min(0f)] private float _investigateArriveDistance = 0.75f;

    [Tooltip("How long this shooter waits/searches at an investigation destination before returning to patrol.")]
    [SerializeField, Min(0f)] private float _investigateSearchSeconds = 1.25f;

    [Tooltip("Minimum seconds this shooter commits to a newly accepted investigation point before it can finish. Prevents footstep clues from instantly resolving into Patrol.")]
    [SerializeField, Min(0f)] private float _investigateMinimumCommitSeconds = 0.5f;

    [Tooltip("While already investigating, new footstep clues can retarget movement only after this cooldown. Gunshots bypass this because they are high priority.")]
    [SerializeField, Min(0f)] private float _investigateRetargetCooldown = 0.35f;

    [Tooltip("When a new sound is heard while waiting/searching at an investigation point, the shooter queues it and moves there after the current wait finishes instead of flickering Patrol <-> Investigate.")]
    [SerializeField] private bool _queueSoundDuringInvestigationWait = true;

    [Header("AI Aim")]
    [Tooltip("How fast the shooter aim direction rotates toward the current target direction.")]
    [SerializeField, Min(0f)] private float _aimTrackingSpeedDegPerSecond = 720f;

    [HideInInspector, SerializeField] private float _aimErrorAngleDeg = 0f;

    [Tooltip("How far ahead to lead a moving target. 0 means no target leading.")]
    [SerializeField, Min(0f)] private float _targetLeadSeconds = 0f;

    [Header("Harmful Area Avoidance")]
    [Tooltip("If true, this shooter leaves SpecialAreaRuntime zones that report this entity as a harmful target. Areas never become NavMesh obstacles.")]
    [FormerlySerializedAs("_avoidVaccinePaint")]
    [SerializeField] private bool _avoidHarmfulAreas = true;

    [Tooltip("If true, harmful-area avoidance may interrupt ranged attack. Patrol, investigate, and chase always use avoidance when enabled.")]
    [FormerlySerializedAs("_avoidVaccinePaintWhileAttacking")]
    [SerializeField] private bool _avoidHarmfulAreasWhileAttacking = true;

    [Tooltip("How often area avoidance evaluates the current position and nearby NavMesh candidates.")]
    [FormerlySerializedAs("_paintAvoidanceCheckInterval")]
    [SerializeField, Min(0.05f)] private float _harmfulAreaCheckInterval = 0.2f;

    [Tooltip("Radius used to sample an escape position. If every sample is still harmful, the lowest-threat candidate is selected instead of freezing.")]
    [FormerlySerializedAs("_paintAvoidanceSearchRadius")]
    [SerializeField, Min(0.1f)] private float _harmfulAreaSearchRadius = 4f;

    [Tooltip("Number of evenly distributed directions considered for escaping a harmful area.")]
    [FormerlySerializedAs("_paintAvoidanceSampleCount")]
    [SerializeField, Min(1)] private int _harmfulAreaSampleCount = 8;

    [Tooltip("Maximum NavMesh projection distance for each harmful-area escape candidate.")]
    [FormerlySerializedAs("_paintAvoidanceNavMeshSampleDistance")]
    [SerializeField, Min(0.1f)] private float _harmfulAreaNavMeshSampleDistance = 2f;

    [Header("Melee AI")]
    [Tooltip("If true, this shooter may use the equipped melee weapon when the target is close enough. Keep false for normal rifle enemies.")]
    [SerializeField] private bool _useMeleeAttack = false;

    [Header("Combat Weapon Slots")]
    [Tooltip("Enables automatic combat weapon switching. The enemy selects the melee slot before a melee swing and the ranged slot before firing. Leave disabled for ordinary one-weapon shooters.")]
    [SerializeField] private bool _autoSwitchCombatWeaponSlots;

    [Tooltip("Weapon slot used for ranged attacks when Automatic Combat Weapon Switching is enabled.")]
    [SerializeField, Min(0)] private int _rangedWeaponSlotIndex;

    [Tooltip("Weapon slot used for melee attacks when Automatic Combat Weapon Switching is enabled.")]
    [SerializeField, Min(0)] private int _meleeWeaponSlotIndex = 1;

    [Tooltip("Minimum time before this enemy may switch between its configured ranged and melee combat slots again. Set this only on hybrid enemies to prevent rapid range-boundary weapon swapping.")]
    [SerializeField, Min(0f)] private float _combatWeaponSwitchCooldownSeconds;

    [Tooltip("Extra distance added to the melee weapon range when deciding if the enemy may swing.")]
    [SerializeField, Min(0f)] private float _meleeRangePadding = 0.25f;

    [Tooltip("Cooldown between AI melee swings. The melee weapon cooldown still also applies.")]
    [SerializeField, Min(0f)] private float _meleeAttackCooldownSeconds = 1.2f;

    [Tooltip("Seconds the melee FSM state stays active after a swing attempt before returning to normal combat.")]
    [SerializeField, Min(0.01f)] private float _meleeActionSeconds = 0.35f;

    [Header("Throwable AI")]
    [Tooltip("If true, this shooter may throw the first ThrowableUsableItemSO carried in its inventory.")]
    [SerializeField] private bool _useThrowableAttack = false;

    [Tooltip("Long cooldown after throwing. Keep this high to avoid grenade spam.")]
    [SerializeField, Min(0f)] private float _throwableCooldownSeconds = 20f;

    [Tooltip("Minimum target distance for throwing. Prevents enemies from throwing at their own feet.")]
    [SerializeField, Min(0f)] private float _throwableMinRange = 8f;

    [Tooltip("Maximum target distance for throwing. 0 disables the upper limit.")]
    [SerializeField, Min(0f)] private float _throwableMaxRange = 35f;

    [Tooltip("최근 직접 목격한 위치를 투척 목표로 사용할 수 있는 시간(초). 보이지 않는 타겟의 현재 위치를 따라 던지지 않습니다.")]
    [SerializeField, Min(0f)] private float _throwableRecentlyKnownSeconds = 3f;

    [Tooltip("투척 판단 대기 시간(초). 직접 목격한 적의 시야/총구 경로가 이 시간 동안 연속해서 막혔을 때만 투척 후보가 됩니다. 사격각이 열리면 초기화됩니다. 최근 목격 유효 시간보다 짧게 설정하세요. 선제 투척은 하지 않습니다.")]
    [SerializeField, Min(0.1f)] private float _throwableBlockedFireSeconds = 1.5f;

    // 기존 작성 데이터/외부 코드 호환용. 새 전술 정책은 두 값과 무관하게 열린 사격각에서 항상 투척을 금지합니다.
    [SerializeField, HideInInspector] private bool _throwablePreferWhenNoLineOfSight = true;
    [SerializeField, HideInInspector] private bool _throwableBlockWhenCanAttack = true;

    [Tooltip("Seconds the throwable FSM state stays active after the throw. This creates a short weapon-switch/recovery delay.")]
    [SerializeField, Min(0.01f)] private float _throwableActionSeconds = 0.8f;

    [Tooltip("Extra safety distance added to explosion radius when checking if the enemy is too close to the planned throw point.")]
    [SerializeField, Min(0f)] private float _throwableSelfSafetyPadding = 1f;

    [Tooltip("투척 시 아군 안전 검사 레이어. 비어 있으면 Enemy 레이어를 자동 사용합니다. 검사 레이어를 찾을 수 없으면 안전을 보장할 수 없어 투척을 막습니다.")]
    [SerializeField] private LayerMask _throwableAllySafetyMask = 0;

    [Tooltip("Extra radius added to the grenade explosion radius when checking nearby allies. Use a small value to avoid edge-case friendly hits.")]
    [SerializeField, Min(0f)] private float _throwableAllySafetyPadding = 1f;

    [Header("Defensive Dash")]
    [Tooltip("If true, this enemy shooter can use defensive dash behavior.")]
    [SerializeField] private bool _useDash = false;

    [Tooltip("If true, this shooter may dash when a hostile basic bullet is predicted to hit it shortly. It does not dash after damage has already been received.")]
    [FormerlySerializedAs("_dashOnDamaged")]
    [SerializeField] private bool _dashOnIncomingProjectile = true;

    [Tooltip("Chance to dash for each hostile basic bullet predicted to hit this shooter. 1 means always dash if cooldown allows.")]
    [FormerlySerializedAs("_dashChanceOnDamaged")]
    [SerializeField, Range(0f, 1f)] private float _dashChanceOnIncomingProjectile = 1f;

    [Tooltip("If true, this shooter may dash when current health ratio is below the threshold.")]
    [SerializeField] private bool _dashWhenHealthBelowThreshold = true;

    [Tooltip("Health ratio threshold for low-health defensive dash.")]
    [SerializeField, Range(0f, 1f)] private float _dashHealthNormalizedThreshold = 0.35f;

    [Tooltip("Cooldown between defensive dashes.")]
    [SerializeField, HideInInspector, Min(0f)] private float _dashCooldownSeconds = 4f;

    [Tooltip("How long one defensive dash lasts.")]
    [SerializeField, HideInInspector, Min(0.01f)] private float _dashDurationSeconds = 0.18f;

    [Tooltip("Movement speed used during defensive dash.")]
    [SerializeField, HideInInspector, Min(0f)] private float _dashSpeed = 14f;

    [Tooltip("If true, dash direction is away from the current target. If false, the dash controller can choose a fallback direction.")]
    [SerializeField] private bool _dashAwayFromTarget = true;

    public EnemyShooter EnemyShooterPrefab => _enemyShooterPrefab;
    public Enemy EnemyPrefab => _enemyShooterPrefab;
    public bool IsValid => EnemyPrefab != null;
    public EnemyShooterRole EnemyRole => _enemyRole;
    public EnemyShooterFsmArchetype FsmArchetype => _fsmArchetype;
    public bool UsesSharedTargetDetection => !_useIndividualTargetDetection;
    public override EntityVisionSettings Vision =>
        _useIndividualTargetDetection
            ? base.Vision
            : CommonRules != null ? CommonRules.EnemyVision : SharedTargetDetectionVision;

    private static readonly EntityVisionSettings SharedTargetDetectionVision =
        EntityVisionSettings.CreateSharedEnemyShooterDetection();

    /// <summary>
    /// Returns the short authoring rule for the selected shared-FSM archetype.
    /// This is intentionally informational: detailed config fields and loadout
    /// data remain the sole runtime source of truth.
    /// </summary>
    public string FsmArchetypeGuidance => _fsmArchetype switch
    {
        EnemyShooterFsmArchetype.BasicRanged =>
            "Ranged loadout only. Keep melee, throwable, dash, and tactical intent disabled.",
        EnemyShooterFsmArchetype.TacticalRanged =>
            "Ranged loadout with selected cover, duel-token, flank, dash, or throwable capabilities enabled.",
        EnemyShooterFsmArchetype.MeleeOnly =>
            "Melee loadout only. Enable melee and leave automatic ranged/melee slot switching disabled.",
        EnemyShooterFsmArchetype.HybridPursuer =>
            "Ranged and melee loadout. Enable melee plus automatic combat-weapon switching and set a switch cooldown.",
        EnemyShooterFsmArchetype.HeavyRanged =>
            "Ranged boss/heavy loadout. Use burst, throwable, dash, and tactical toggles instead of a new base FSM.",
        _ =>
            "Custom capability combination. It still uses the shared shooter Mode and Alive transition tables."
    };

    public string Id => _enemy.Id;
    public string DisplayName => _enemy.DisplayName;
    public DroppableRewardConfigSO DroppableRewardConfig => _enemy.DroppableRewardConfig;
    public EnemyScreenSpaceWidgetLayout HealthBarUI => _enemy.HealthBarUI;
    public EnemyScreenSpaceWidgetLayout InfectionCastBarUI => _enemy.InfectionCastBarUI;

    public bool AlertOnDamaged => _alertOnDamaged;
    public float TargetSearchInterval => Mathf.Max(0.05f, _targetSearchInterval);
    public float TargetMemorySeconds => Mathf.Max(0f, _targetMemorySeconds);
    public float AlertReactionMultiplier => Mathf.Max(0f, _alertReactionMultiplier);
    public float TargetLockMinSeconds => Mathf.Max(0f, _targetLockMinSeconds);
    public bool ChaseLastKnownPosition => _chaseLastKnownPosition;
    public float LastKnownInvestigationSeconds => Mathf.Max(0f, _lastKnownInvestigationSeconds);
    public float SoundInvestigationMemorySeconds => Mathf.Max(0f, _soundInvestigationMemorySeconds);
    public float InvestigationReenterCooldown => Mathf.Max(0f, _investigationReenterCooldown);
    public bool IgnoreEnemySoundStimuli => _ignoreEnemySoundStimuli;
    public float SoundStimulusDuplicateCooldown => Mathf.Max(0f, _soundStimulusDuplicateCooldown);

    public float AttackStartRange => MaxRange;
    public bool UseWeaponRangeAsAttackKeepRange => _useMaxRangeAsAttackKeepRange;
    public float AttackKeepRange => _useMaxRangeAsAttackKeepRange ? MaxRange : Mathf.Max(0f, _attackKeepRange);
    public bool UsePreferredRangeBeforeStationaryAttack => _usePreferredRangeBeforeStationaryAttack;
    public float StationaryAttackKeepRangePadding => Mathf.Max(0f, _stationaryAttackKeepRangePadding);

    public float ChaseStoppingDistance =>
        _chaseStoppingDistance > 0f
            ? _chaseStoppingDistance
            : 8f;

    public float ChaseLoseRange => Mathf.Max(0f, _chaseLoseRange);

    public float PlayerDiscoveryReactionTime => Mathf.Max(0f, _playerDiscoveryReactionTime);
    public float PursuitReacquireReactionTime => Mathf.Max(0f, _pursuitReacquireReactionTime);
    public int BurstCount => Mathf.Max(1, _burstCount);
    public float BurstInterval => Mathf.Max(0f, _burstInterval);
    public bool FinishStartedBurstAfterSightLost => _finishStartedBurstAfterSightLost;

    public EnemyShooterAttackMovementMode AttackMovementMode => _attackMovementMode;
    public bool MoveWhileAttacking =>
        _attackMovementMode == EnemyShooterAttackMovementMode.MoveWhileFiring;
    public bool MoveDuringBurstInterval =>
        _attackMovementMode == EnemyShooterAttackMovementMode.RepositionBetweenBursts ||
        _attackMovementMode == EnemyShooterAttackMovementMode.MoveWhileFiring;
    public float NavMeshAcceleration => Mathf.Max(0.1f, _navMeshAcceleration);
    public float CombatMoveRefreshInterval => Mathf.Max(0.02f, _combatMoveRefreshInterval);
    public float BurstCooldownSeconds => Mathf.Max(0f, _burstCooldownSeconds);
    public float AttackPreferredDistance => Mathf.Max(0f, _attackPreferredDistance);
    public float AttackStrafeDistance => Mathf.Max(0f, _attackStrafeDistance);
    public bool UseCombatStuckRecovery => _useCombatStuckRecovery;
    public float CombatStuckSeconds => Mathf.Max(0.25f, _combatStuckSeconds);
    public float CombatStuckMinimumProgress => Mathf.Max(0.01f, _combatStuckMinimumProgress);
    public float CombatStuckStepDistance => Mathf.Max(0.25f, _combatStuckStepDistance);
    public float CombatStuckStepSeconds => Mathf.Max(0.25f, _combatStuckStepSeconds);
    public bool UseTacticalIntent => _useTacticalIntent;
    public bool UseCoverTactics => _useTacticalIntent && _useCoverTactics;
    public bool UseDuelToken => _useTacticalIntent && _useDuelToken;
    public bool UseFlankingTactics => _useTacticalIntent && _useFlankingTactics;
    public float CoverSearchRadius => Mathf.Max(0.5f, _coverSearchRadius);
    public float CoverHideSeconds => Mathf.Max(0f, _coverHideSeconds);
    public float CoverPeekSeconds => Mathf.Max(0.05f, _coverPeekSeconds);
    public float CoverArriveDistance => Mathf.Max(0.05f, _coverArriveDistance);
    public float TacticalIntentRefreshInterval => Mathf.Max(0.05f, _tacticalIntentRefreshInterval);
    public float TacticalFlankDistance => Mathf.Max(0.1f, _tacticalFlankDistance);
    public float TacticalFlankAngleDegrees => Mathf.Clamp(_tacticalFlankAngleDegrees, 10f, 85f);

    public float PatrolRadius => Mathf.Max(0f, _patrolRadius);
    public float PatrolIdleSeconds => Mathf.Max(.5f, _patrolIdleSeconds);
    public float PatrolDestinationRefreshInterval => Mathf.Max(0.05f, _patrolDestinationRefreshInterval);
    public float PatrolArriveDistance => Mathf.Max(0f, _patrolArriveDistance);
    public float NavMeshSampleDistance => Mathf.Max(0f, _navMeshSampleDistance);
    public float InvestigateArriveDistance => Mathf.Max(0f, _investigateArriveDistance);
    public float InvestigateSearchSeconds => Mathf.Max(0f, _investigateSearchSeconds);
    public float InvestigateMinimumCommitSeconds => Mathf.Max(0f, _investigateMinimumCommitSeconds);
    public float InvestigateRetargetCooldown => Mathf.Max(0f, _investigateRetargetCooldown);
    public bool QueueSoundDuringInvestigationWait => _queueSoundDuringInvestigationWait;

    public float AimTrackingSpeedDegPerSecond => Mathf.Max(0f, _aimTrackingSpeedDegPerSecond);
    public float AimErrorAngleDeg => Mathf.Max(0f, _aimErrorAngleDeg);
    public float TargetLeadSeconds => Mathf.Max(0f, _targetLeadSeconds);

    public bool AvoidHarmfulAreas => _avoidHarmfulAreas;
    public bool AvoidHarmfulAreasWhileAttacking => _avoidHarmfulAreasWhileAttacking;
    public float HarmfulAreaCheckInterval => Mathf.Max(0.05f, _harmfulAreaCheckInterval);
    public float HarmfulAreaSearchRadius => Mathf.Max(0.1f, _harmfulAreaSearchRadius);
    public int HarmfulAreaSampleCount => Mathf.Max(1, _harmfulAreaSampleCount);
    public float HarmfulAreaNavMeshSampleDistance => Mathf.Max(0.1f, _harmfulAreaNavMeshSampleDistance);

    public bool UseMeleeAttack => _useMeleeAttack;
    public bool AutoSwitchCombatWeaponSlots => _autoSwitchCombatWeaponSlots;
    public int RangedWeaponSlotIndex => Mathf.Max(0, _rangedWeaponSlotIndex);
    public int MeleeWeaponSlotIndex => Mathf.Max(0, _meleeWeaponSlotIndex);
    public float CombatWeaponSwitchCooldownSeconds => Mathf.Max(0f, _combatWeaponSwitchCooldownSeconds);
    public float MeleeRangePadding => Mathf.Max(0f, _meleeRangePadding);
    public float MeleeAttackCooldownSeconds => Mathf.Max(0f, _meleeAttackCooldownSeconds);
    public float MeleeActionSeconds => Mathf.Max(0.01f, _meleeActionSeconds);

    public bool UseThrowableAttack => _useThrowableAttack;
    public float ThrowableCooldownSeconds => Mathf.Max(0f, _throwableCooldownSeconds);
    public float ThrowableMinRange => Mathf.Max(0f, _throwableMinRange);
    public float ThrowableMaxRange => Mathf.Max(0f, _throwableMaxRange);
    public float ThrowableRecentlyKnownSeconds => Mathf.Max(0f, _throwableRecentlyKnownSeconds);
    public float ThrowableBlockedFireSeconds => Mathf.Max(0.1f, _throwableBlockedFireSeconds);
    public bool ThrowablePreferWhenNoLineOfSight => _throwablePreferWhenNoLineOfSight;
    public bool ThrowableBlockWhenCanAttack => _throwableBlockWhenCanAttack;
    public float ThrowableActionSeconds => Mathf.Max(0.01f, _throwableActionSeconds);
    public float ThrowableSelfSafetyPadding => Mathf.Max(0f, _throwableSelfSafetyPadding);
    public LayerMask ThrowableAllySafetyMask => _throwableAllySafetyMask;
    public float ThrowableAllySafetyPadding => Mathf.Max(0f, _throwableAllySafetyPadding);

    public bool UseDash => _useDash;
    public bool DashOnIncomingProjectile => _dashOnIncomingProjectile;
    public float DashChanceOnIncomingProjectile => Mathf.Clamp01(_dashChanceOnIncomingProjectile);
    public bool DashWhenHealthBelowThreshold => _dashWhenHealthBelowThreshold;
    public float DashHealthNormalizedThreshold => Mathf.Clamp01(_dashHealthNormalizedThreshold);
    public float DashDurationSeconds => Mathf.Max(0.01f, Dash.durationSeconds);
    public float DashSpeed => Dash.useMoveSpeedBasedDash
        ? Mathf.Max(0f, MoveSpeed * Dash.speedMultiplier) : Mathf.Max(0f, Dash.fixedSpeed);
    public bool DashAwayFromTarget => _dashAwayFromTarget;

    // Purpose: Performs the pick enemy loadout workflow for this component.
    public EnemyShooterLoadout PickEnemyLoadout() => PickEnemyLoadout(0);

    public EnemyShooterLoadout PickEnemyLoadout(int stage)
    {
        var table = ResolveLoadoutTable(stage);
        return table != null && table.TryRoll(out ShooterLoadoutSnapshot snapshot)
            ? snapshot.ToEnemyShooterLoadout()
            : new EnemyShooterLoadout(
                System.Array.Empty<ShooterLoadoutWeaponStack>(),
                System.Array.Empty<ShooterLoadoutAmmoStack>(),
                null);
    }
}
