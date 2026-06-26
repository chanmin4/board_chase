using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "StageProgressionRules",
    menuName = "Game/Stage/Stage Progression Rules")]
public class StageProgressionRulesSO : ScriptableObject
{
    [Serializable]
    public class WeightedGoalEncounterPreset
    {
        [Tooltip("Goal-room encounter preset. Currently used by BigMonsterWave goal rooms.")]
        public StageBattleEncounterPresetSO preset;

        [Tooltip("Weighted chance when selecting this goal encounter preset.")]
        [Min(0)] public int weight = 1;
    }

    [Serializable]
    public class NamedOrBossEnemyEntry
    {
        [Tooltip("Named/Boss enemy config spawned for this stage goal.")]
        public EnemyStatConfigSO archetype;

        [Tooltip("Weighted chance when selecting the Named/Boss enemy config.")]
        [Min(0)] public int weight = 1;
    }

    [Serializable]
    public class NamedOrBossGoalOptions
    {
        [Tooltip("If disabled, this stage goal will not start the Named/Boss reservation cycle.")]
        public bool cycleEnabled = true;

        [Tooltip("Candidate Named/Boss enemies for this stage goal.")]
        public List<NamedOrBossEnemyEntry> enemyEntries = new();

        [Tooltip("If true, the cycle starts when managers are ready or a stage is applied.")]
        public bool startCycleOnReady = true;

        [Tooltip("If true, the goal sector is reserved immediately instead of waiting for First Reservation Delay.")]
        public bool reserveFirstSectorImmediately = true;

        [Tooltip("Delay before the first Named/Boss reservation when immediate reservation is disabled.")]
        [Min(0f)] public float firstReservationDelay = 0f;

        [Tooltip("How long the selected Named/Boss sector stays reserved before it is presented.")]
        [Min(0f)] public float reservationDuration = 30f;

        [Tooltip("Cooldown before the next Named/Boss cycle after the enemy is killed and reward is confirmed.")]
        [Min(0f)] public float respawnCooldownAfterKill = 120f;

        [Tooltip("Retry delay when no valid sector candidate exists.")]
        [Min(0f)] public float retryDelayWhenNoCandidate = 5f;

        [Tooltip("How often Named/Boss timer snapshots are published to UI.")]
        [Min(0.01f)] public float timerPublishInterval = 0.1f;
    }

    [Serializable]
    public class BigMonsterWaveGoalOptions
    {
        [Tooltip("Candidate encounter presets for BigMonsterWave goal rooms.")]
        public List<WeightedGoalEncounterPreset> encounters = new();
    }

    [Serializable]
    public class StageProgressRule
    {
        [Tooltip("Stage index. 0 can be used as the StartSector-only intro stage.")]
        public int stageIndex;

        [Tooltip("HUD/result display name. If empty, code fallback is used.")]
        public string displayName;

        [Header("Stage Map")]
        [Tooltip("Generated room grid size for this stage. 2 = 2x2, 3 = 3x3. 0 can be used for StartSector-only stage.")]
        [Min(0)] public int roomGridSize = 1;

        [Tooltip("Goal battle type placed at the stage goal coordinate. Only Named, Boss, and BigMonsterWave are exposed here.")]
        public StageGoalRoomType goalRoomType = StageGoalRoomType.Named;

        [Tooltip("Settings used only when Goal Room Type is Named.")]
        public NamedOrBossGoalOptions namedGoal = new();

        [Tooltip("Settings used only when Goal Room Type is Boss.")]
        public NamedOrBossGoalOptions bossGoal = new();

        [Tooltip("Settings used only when Goal Room Type is BigMonsterWave.")]
        public BigMonsterWaveGoalOptions bigMonsterWaveGoal = new();

        [Tooltip("Debug log interval for goal-room timer logs.")]
        [Min(0.1f)] public float goalDebugLogInterval = 1f;

        public StageRoomType GoalStageRoomType => goalRoomType.ToStageRoomType();

        public NamedOrBossGoalOptions CurrentNamedOrBossGoalOptions
        {
            get
            {
                return goalRoomType switch
                {
                    StageGoalRoomType.Named => namedGoal,
                    StageGoalRoomType.Boss => bossGoal,
                    _ => null
                };
            }
        }

        [Tooltip("If true, this stage uses only StartSector and does not generate room grid.")]
        [FormerlySerializedAs("useStartSectorAsRequirement")]
        public bool useStartSectorOnly = false;

        [Header("Stage Completion")]
        [Tooltip("If true, completing StartSector advances to the next stage.")]
        public bool advanceStageOnStartSectorComplete = false;

        [Tooltip("If true, completing the goal Named/Boss battle advances to the next stage.")]
        [FormerlySerializedAs("advanceStageOnNamedBattleComplete")]
        public bool advanceStageOnBossBattleComplete = true;

        [Tooltip("If true, this stage ends the run instead of advancing to another stage.")]
        public bool isFinalStage = false;

        [Header("Stage Rest")]
        [Tooltip("Break time before the next stage.")]
        [Min(0f)] public float restSecondsBeforeNextStage = 5f;

        [Header("Reward")]
        [Tooltip("Infection Control recovered when this stage is completed.")]
        [Min(0f)] public float infectionControlRecoverOnComplete = 0f;
    }

    [SerializeField] private List<StageProgressRule> _rules = new();

    public bool TryGetRule(int stageIndex, out StageProgressRule rule)
    {
        for (int i = 0; i < _rules.Count; i++)
        {
            if (_rules[i] != null && _rules[i].stageIndex == stageIndex)
            {
                rule = _rules[i];
                return true;
            }
        }

        rule = null;
        return false;
    }

    public bool HasNextRule(int stageIndex)
    {
        return TryGetRule(stageIndex + 1, out _);
    }

    public bool TryGetNamedOrBossOptions(
        int stageIndex,
        out NamedOrBossGoalOptions options)
    {
        options = null;

        if (!TryGetRule(stageIndex, out StageProgressRule rule))
            return false;

        options = rule.CurrentNamedOrBossGoalOptions;
        return options != null;
    }

    public bool TryPickGoalEncounterPreset(
        int stageIndex,
        int stageSeed,
        Vector2Int sectorCoord,
        StageRoomType roomType,
        out StageBattleEncounterPresetSO preset)
    {
        preset = null;

        if (!TryGetRule(stageIndex, out StageProgressRule rule))
            return false;

        if (rule.GoalStageRoomType != roomType ||
            roomType != StageRoomType.BigMonsterWave ||
            rule.bigMonsterWaveGoal == null)
        {
            return false;
        }

        int seed = BuildSectorSeed(stageSeed, sectorCoord, 701);
        return TryPickWeightedEncounterPreset(
            rule.bigMonsterWaveGoal.encounters,
            seed,
            out preset);
    }

    public bool TryPickNamedOrBossArchetype(
        int stageIndex,
        out EnemyStatConfigSO archetype)
    {
        archetype = null;

        if (!TryGetNamedOrBossOptions(stageIndex, out NamedOrBossGoalOptions options))
            return false;

        int seed = unchecked(stageIndex * 73856093) ^ Environment.TickCount;
        return TryPickWeightedNamedOrBoss(options.enemyEntries, seed, out archetype);
    }

    public bool CanStartNamedOrBossCycle(int stageIndex)
    {
        return TryGetNamedOrBossOptions(stageIndex, out NamedOrBossGoalOptions options) &&
               options.cycleEnabled &&
               HasValidNamedOrBossEntry(stageIndex);
    }

    public bool HasValidNamedOrBossEntry(int stageIndex)
    {
        return TryGetNamedOrBossOptions(stageIndex, out NamedOrBossGoalOptions options) &&
               HasValidNamedOrBossEntry(options);
    }

    public bool TryGetNamedOrBossCycleRule(
        int stageIndex,
        out StageProgressRule rule)
    {
        rule = null;

        if (!TryGetRule(stageIndex, out StageProgressRule foundRule))
            return false;

        if (foundRule.CurrentNamedOrBossGoalOptions == null)
            return false;

        rule = foundRule;
        return true;
    }

    public static int BuildSectorSeed(int stageSeed, Vector2Int sectorCoord, int salt)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + stageSeed;
            hash = hash * 31 + sectorCoord.x;
            hash = hash * 31 + sectorCoord.y;
            hash = hash * 31 + salt;
            return hash;
        }
    }

    private static bool TryPickWeightedEncounterPreset(
        List<WeightedGoalEncounterPreset> candidates,
        int seed,
        out StageBattleEncounterPresetSO pickedPreset)
    {
        pickedPreset = null;

        if (candidates == null)
            return false;

        int totalWeight = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            WeightedGoalEncounterPreset candidate = candidates[i];

            if (!IsEncounterCandidateValid(candidate))
                continue;

            totalWeight += Mathf.Max(0, candidate.weight);
        }

        if (totalWeight <= 0)
            return false;

        int roll = new System.Random(seed).Next(0, totalWeight);

        for (int i = 0; i < candidates.Count; i++)
        {
            WeightedGoalEncounterPreset candidate = candidates[i];

            if (!IsEncounterCandidateValid(candidate))
                continue;

            int weight = Mathf.Max(0, candidate.weight);

            if (roll < weight)
            {
                pickedPreset = candidate.preset;
                return true;
            }

            roll -= weight;
        }

        return false;
    }

    private static bool TryPickWeightedNamedOrBoss(
        List<NamedOrBossEnemyEntry> candidates,
        int seed,
        out EnemyStatConfigSO pickedArchetype)
    {
        pickedArchetype = null;

        if (candidates == null)
            return false;

        int totalWeight = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            NamedOrBossEnemyEntry candidate = candidates[i];

            if (!IsNamedOrBossEntryValid(candidate))
                continue;

            totalWeight += Mathf.Max(0, candidate.weight);
        }

        if (totalWeight <= 0)
            return false;

        int roll = new System.Random(seed).Next(0, totalWeight);

        for (int i = 0; i < candidates.Count; i++)
        {
            NamedOrBossEnemyEntry candidate = candidates[i];

            if (!IsNamedOrBossEntryValid(candidate))
                continue;

            int weight = Mathf.Max(0, candidate.weight);

            if (roll < weight)
            {
                pickedArchetype = candidate.archetype;
                return true;
            }

            roll -= weight;
        }

        return false;
    }

    private static bool HasValidNamedOrBossEntry(
        NamedOrBossGoalOptions options)
    {
        if (options == null || options.enemyEntries == null)
            return false;

        for (int i = 0; i < options.enemyEntries.Count; i++)
        {
            if (IsNamedOrBossEntryValid(options.enemyEntries[i]))
                return true;
        }

        return false;
    }

    private static bool IsEncounterCandidateValid(
        WeightedGoalEncounterPreset candidate)
    {
        return candidate != null &&
               candidate.weight > 0 &&
               candidate.preset != null &&
               candidate.preset.IsValid;
    }

    private static bool IsNamedOrBossEntryValid(
        NamedOrBossEnemyEntry candidate)
    {
        return candidate != null &&
               candidate.weight > 0 &&
               candidate.archetype != null &&
               candidate.archetype.EnemyPrefab != null;
    }
}
