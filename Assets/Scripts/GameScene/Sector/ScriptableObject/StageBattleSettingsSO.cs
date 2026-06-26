using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "StageBattleSettings",
    menuName = "Game/Battle/Stage Battle Settings")]
public class StageBattleSettingsSO : ScriptableObject
{
    [Serializable]
    public class WeightedEncounterPreset
    {
        [Tooltip("Reusable normal battle encounter preset used by this stage.")]
        public StageBattleEncounterPresetSO preset;

        [Tooltip("Weighted chance when selecting an encounter preset for a NormalBattle room.")]
        [Min(0)] public int weight = 1;
    }

    [Serializable]
    public class WeightedPlayerStageObjectRoomPreset
    {
        [Tooltip("Reusable player-side object room preset.")]
        public PlayerStageObjectRoomPresetSO preset;

        [Tooltip("Weighted chance when picking player-side object presets for a room.")]
        [Min(0)] public int weight = 1;
    }

    [Serializable]
    public class WeightedEnemyStageObjectRoomPreset
    {
        [Tooltip("Reusable enemy-side object room preset.")]
        public EnemyStageObjectRoomPresetSO preset;

        [Tooltip("Weighted chance when picking enemy-side object presets for a room.")]
        [Min(0)] public int weight = 1;
    }

    [Serializable]
    public class StageSpawnRule
    {
        [Tooltip("Stage index this battle setting applies to.")]
        public int stageIndex;

        [Tooltip("Inspector-only label for this stage battle setting.")]
        public string displayName;

        [Header("Normal Battle Encounters")]
        [FormerlySerializedAs("normalBattleEncounterPresets")]
        [Tooltip("NormalBattle rooms pick one encounter preset from this list. Goal-room encounters live in StageProgressionRulesSO.")]
        public List<WeightedEncounterPreset> normalBattleEncounters = new();

        [Header("Normal Battle Timer")]
        [Tooltip("NormalBattle and BigMonsterWave room timer duration. The timer applies pressure but does not clear/fail the room by itself.")]
        [Min(0f)] public float normalBattleTimerSeconds = 30f;

        [Tooltip("Legacy non-generated sector timer option. If true, timer resets when its requirement is lost.")]
        public bool resetTimerWhenRequirementLost = false;

        [Header("Player Stage Objects")]
        [FormerlySerializedAs("playerStageObjectPresetPickCount")]
        [Tooltip("How many actual player-side stage objects are spawned in one battle room. Each spawn rolls one weighted preset from the candidate list, then one object from that preset.")]
        [Min(0)] public int playerStageObjectSpawnCount = 2;

        [Tooltip("Candidate player-side object room presets for this stage.")]
        public List<WeightedPlayerStageObjectRoomPreset> playerStageObjectPresets = new();

        [Header("Enemy Stage Objects")]
        [FormerlySerializedAs("enemyStageObjectPresetPickCount")]
        [Tooltip("How many actual enemy-side stage objects are spawned in one battle room. Each spawn rolls one weighted preset from the candidate list, then one object from that preset.")]
        [Min(0)] public int enemyStageObjectSpawnCount = 2;

        [Tooltip("Candidate enemy-side object room presets for this stage.")]
        public List<WeightedEnemyStageObjectRoomPreset> enemyStageObjectPresets = new();

        [Header("Debug")]
        [Min(0.1f)]
        public float debugLogInterval = 1f;
    }

    [Header("Stage Battle Rules")]
    [FormerlySerializedAs("_rules")]
    [SerializeField] private List<StageSpawnRule> _rules = new();

    public bool TryGetRule(int stageIndex, out StageSpawnRule rule)
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

    public bool TryPickBattleEncounterPreset(
        int stageIndex,
        int stageSeed,
        Vector2Int sectorCoord,
        StageRoomType roomType,
        out StageBattleEncounterPresetSO preset)
    {
        preset = null;

        if (roomType != StageRoomType.NormalBattle)
            return false;

        if (!TryGetRule(stageIndex, out StageSpawnRule rule) ||
            rule.normalBattleEncounters == null)
        {
            return false;
        }

        int seed = BuildSectorSeed(stageSeed, sectorCoord, 101);
        return TryPickWeightedEncounterPreset(rule.normalBattleEncounters, seed, out preset);
    }

    public bool TryGetNormalBattleTimerSettings(
        int stageIndex,
        out float timerSeconds,
        out bool resetTimerWhenRequirementLost)
    {
        timerSeconds = 30f;
        resetTimerWhenRequirementLost = false;

        if (!TryGetRule(stageIndex, out StageSpawnRule rule))
            return false;

        timerSeconds = Mathf.Max(0f, rule.normalBattleTimerSeconds);
        resetTimerWhenRequirementLost = rule.resetTimerWhenRequirementLost;
        return true;
    }

    public int GetPlayerStageObjectSpawnCount(int stageIndex)
    {
        return TryGetRule(stageIndex, out StageSpawnRule rule)
            ? Mathf.Max(0, rule.playerStageObjectSpawnCount)
            : 0;
    }

    public int GetEnemyStageObjectSpawnCount(int stageIndex)
    {
        return TryGetRule(stageIndex, out StageSpawnRule rule)
            ? Mathf.Max(0, rule.enemyStageObjectSpawnCount)
            : 0;
    }

    public bool TryPickPlayerStageObjectConfig(
        int stageIndex,
        int stageSeed,
        Vector2Int sectorCoord,
        int spawnIndex,
        out PlayerStageObjectConfigSO objectConfig)
    {
        objectConfig = null;

        if (!TryGetRule(stageIndex, out StageSpawnRule rule) ||
            rule.playerStageObjectPresets == null ||
            rule.playerStageObjectSpawnCount <= 0)
        {
            return false;
        }

        int safeSpawnIndex = Mathf.Max(0, spawnIndex);
        int presetSeed = BuildSectorSeed(stageSeed, sectorCoord, 307 + safeSpawnIndex * 37);

        if (!TryPickWeighted(
            rule.playerStageObjectPresets,
            presetSeed,
            IsPlayerStageObjectCandidateValid,
            candidate => Mathf.Max(0, candidate.weight),
            candidate => candidate.preset,
            out PlayerStageObjectRoomPresetSO preset))
        {
            return false;
        }

        return TryPickPlayerStageObjectFromPreset(
            preset,
            BuildSectorSeed(stageSeed, sectorCoord, 1307 + safeSpawnIndex * 37),
            out objectConfig);
    }

    public bool TryPickEnemyStageObjectConfig(
        int stageIndex,
        int stageSeed,
        Vector2Int sectorCoord,
        int spawnIndex,
        out EnemyStageObjectConfigSO objectConfig)
    {
        objectConfig = null;

        if (!TryGetRule(stageIndex, out StageSpawnRule rule) ||
            rule.enemyStageObjectPresets == null ||
            rule.enemyStageObjectSpawnCount <= 0)
        {
            return false;
        }

        int safeSpawnIndex = Mathf.Max(0, spawnIndex);
        int presetSeed = BuildSectorSeed(stageSeed, sectorCoord, 409 + safeSpawnIndex * 41);

        if (!TryPickWeighted(
            rule.enemyStageObjectPresets,
            presetSeed,
            IsEnemyStageObjectCandidateValid,
            candidate => Mathf.Max(0, candidate.weight),
            candidate => candidate.preset,
            out EnemyStageObjectRoomPresetSO preset))
        {
            return false;
        }

        return TryPickEnemyStageObjectFromPreset(
            preset,
            BuildSectorSeed(stageSeed, sectorCoord, 1409 + safeSpawnIndex * 41),
            out objectConfig);
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
        List<WeightedEncounterPreset> candidates,
        int seed,
        out StageBattleEncounterPresetSO pickedPreset)
    {
        pickedPreset = null;

        if (candidates == null)
            return false;

        int totalWeight = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            WeightedEncounterPreset candidate = candidates[i];

            if (!IsEncounterCandidateValid(candidate))
                continue;

            totalWeight += Mathf.Max(0, candidate.weight);
        }

        if (totalWeight <= 0)
            return false;

        int roll = new System.Random(seed).Next(0, totalWeight);

        for (int i = 0; i < candidates.Count; i++)
        {
            WeightedEncounterPreset candidate = candidates[i];

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

    private static bool TryPickWeighted<TCandidate, TResult>(
        IReadOnlyList<TCandidate> source,
        int seed,
        Func<TCandidate, bool> isValid,
        Func<TCandidate, int> getWeight,
        Func<TCandidate, TResult> getResult,
        out TResult result)
    {
        result = default;

        if (source == null)
            return false;

        int totalWeight = 0;
        for (int i = 0; i < source.Count; i++)
        {
            TCandidate candidate = source[i];

            if (isValid(candidate))
                totalWeight += Mathf.Max(0, getWeight(candidate));
        }

        if (totalWeight <= 0)
            return false;

        int roll = new System.Random(seed).Next(0, totalWeight);

        for (int i = 0; i < source.Count; i++)
        {
            TCandidate candidate = source[i];

            if (!isValid(candidate))
                continue;

            int weight = Mathf.Max(0, getWeight(candidate));

            if (roll < weight)
            {
                result = getResult(candidate);
                return true;
            }

            roll -= weight;
        }

        return false;
    }

    private static bool TryPickPlayerStageObjectFromPreset(
        PlayerStageObjectRoomPresetSO preset,
        int seed,
        out PlayerStageObjectConfigSO objectConfig)
    {
        objectConfig = null;

        if (preset == null || preset.Objects == null)
            return false;

        List<PlayerStageObjectConfigSO> validObjects = new();

        for (int i = 0; i < preset.Objects.Count; i++)
        {
            PlayerStageObjectRoomPresetSO.ObjectSpawn spawn = preset.Objects[i];

            if (spawn != null &&
                spawn.objectConfig != null &&
                spawn.objectConfig.IsValid)
            {
                validObjects.Add(spawn.objectConfig);
            }
        }

        if (validObjects.Count <= 0)
            return false;

        objectConfig = validObjects[new System.Random(seed).Next(0, validObjects.Count)];
        return true;
    }

    private static bool TryPickEnemyStageObjectFromPreset(
        EnemyStageObjectRoomPresetSO preset,
        int seed,
        out EnemyStageObjectConfigSO objectConfig)
    {
        objectConfig = null;

        if (preset == null || preset.Objects == null)
            return false;

        List<EnemyStageObjectConfigSO> validObjects = new();

        for (int i = 0; i < preset.Objects.Count; i++)
        {
            EnemyStageObjectRoomPresetSO.ObjectSpawn spawn = preset.Objects[i];

            if (spawn != null &&
                spawn.objectConfig != null &&
                spawn.objectConfig.IsValid)
            {
                validObjects.Add(spawn.objectConfig);
            }
        }

        if (validObjects.Count <= 0)
            return false;

        objectConfig = validObjects[new System.Random(seed).Next(0, validObjects.Count)];
        return true;
    }

    private static bool IsEncounterCandidateValid(WeightedEncounterPreset candidate)
    {
        return candidate != null &&
               candidate.weight > 0 &&
               candidate.preset != null &&
               candidate.preset.IsValid;
    }

    private static bool IsPlayerStageObjectCandidateValid(
        WeightedPlayerStageObjectRoomPreset candidate)
    {
        return candidate != null &&
               candidate.weight > 0 &&
               candidate.preset != null &&
               candidate.preset.IsValid;
    }

    private static bool IsEnemyStageObjectCandidateValid(
        WeightedEnemyStageObjectRoomPreset candidate)
    {
        return candidate != null &&
               candidate.weight > 0 &&
               candidate.preset != null &&
               candidate.preset.IsValid;
    }
}
