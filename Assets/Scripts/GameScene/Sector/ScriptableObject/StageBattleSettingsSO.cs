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

    [Header("Debug")]
    [SerializeField] private bool _debugEncounterPick = true;

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

        if (_debugEncounterPick)
        {
            Debug.Log(
                $"[StageBattleSettingsSO] TryPickBattleEncounterPreset begin. " +
                $"asset={name}, stage={stageIndex}, stageSeed={stageSeed}, coord={sectorCoord}, roomType={roomType}",
                this);
        }

        if (roomType != StageRoomType.NormalBattle)
        {
            LogEncounterPickFail(
                stageIndex,
                sectorCoord,
                roomType,
                "room type is not NormalBattle");
            return false;
        }

        bool ruleFound = TryGetRule(stageIndex, out StageSpawnRule rule);
        if (!ruleFound)
        {
            LogEncounterPickFail(
                stageIndex,
                sectorCoord,
                roomType,
                "stage rule not found");
            return false;
        }

        if (rule.normalBattleEncounters == null)
        {
            LogEncounterPickFail(
                stageIndex,
                sectorCoord,
                roomType,
                "normalBattleEncounters is null");
            return false;
        }

        int seed = BuildSectorSeed(stageSeed, sectorCoord, 101);

        if (_debugEncounterPick)
        {
            Debug.Log(
                $"[StageBattleSettingsSO] Rule found. " +
                $"stage={stageIndex}, ruleName={rule.displayName}, encounterCount={rule.normalBattleEncounters.Count}, pickSeed={seed}",
                this);
        }

        bool picked = TryPickWeightedEncounterPreset(
            rule.normalBattleEncounters,
            seed,
            $"stage={stageIndex}, coord={sectorCoord}, roomType={roomType}",
            out preset);

        if (_debugEncounterPick)
        {
            Debug.Log(
                $"[StageBattleSettingsSO] TryPickBattleEncounterPreset result. " +
                $"stage={stageIndex}, coord={sectorCoord}, picked={picked}, preset={(preset != null ? preset.name : "null")}",
                preset != null ? preset : this);
        }

        return picked;
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

    private bool TryPickWeightedEncounterPreset(
        List<WeightedEncounterPreset> candidates,
        int seed,
        string context,
        out StageBattleEncounterPresetSO pickedPreset)
    {
        pickedPreset = null;

        if (candidates == null)
        {
            Debug.LogWarning(
                $"[StageBattleSettingsSO] Encounter pick failed. {context}, candidates=null",
                this);
            return false;
        }

        int totalWeight = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            WeightedEncounterPreset candidate = candidates[i];
            bool valid = IsEncounterCandidateValid(candidate);
            int weight = candidate != null ? Mathf.Max(0, candidate.weight) : 0;

            if (_debugEncounterPick)
                LogEncounterCandidate(i, candidate, valid, context);

            if (!valid)
                continue;

            totalWeight += weight;
        }

        if (_debugEncounterPick)
        {
            Debug.Log(
                $"[StageBattleSettingsSO] Encounter candidate scan finished. " +
                $"{context}, candidateCount={candidates.Count}, totalValidWeight={totalWeight}",
                this);
        }

        if (totalWeight <= 0)
        {
            Debug.LogWarning(
                $"[StageBattleSettingsSO] Encounter pick failed. {context}, totalValidWeight <= 0",
                this);
            return false;
        }

        int roll = new System.Random(seed).Next(0, totalWeight);

        if (_debugEncounterPick)
        {
            Debug.Log(
                $"[StageBattleSettingsSO] Encounter roll. {context}, seed={seed}, roll={roll}, totalWeight={totalWeight}",
                this);
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            WeightedEncounterPreset candidate = candidates[i];

            if (!IsEncounterCandidateValid(candidate))
                continue;

            int weight = Mathf.Max(0, candidate.weight);

            if (_debugEncounterPick)
            {
                Debug.Log(
                    $"[StageBattleSettingsSO] Encounter roll check. " +
                    $"{context}, index={i}, preset={candidate.preset.name}, weight={weight}, rollBefore={roll}",
                    candidate.preset);
            }

            if (roll < weight)
            {
                pickedPreset = candidate.preset;

                if (_debugEncounterPick)
                {
                    Debug.Log(
                        $"[StageBattleSettingsSO] Encounter picked. {context}, index={i}, preset={pickedPreset.name}",
                        pickedPreset);
                }

                return true;
            }

            roll -= weight;
        }

        Debug.LogWarning(
            $"[StageBattleSettingsSO] Encounter pick failed unexpectedly after roll loop. {context}",
            this);
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

    private void LogEncounterPickFail(
        int stageIndex,
        Vector2Int sectorCoord,
        StageRoomType roomType,
        string reason)
    {
        if (!_debugEncounterPick)
            return;

        Debug.LogWarning(
            $"[StageBattleSettingsSO] TryPickBattleEncounterPreset failed. " +
            $"asset={name}, stage={stageIndex}, coord={sectorCoord}, roomType={roomType}, reason={reason}",
            this);
    }

    private void LogEncounterCandidate(
        int index,
        WeightedEncounterPreset candidate,
        bool valid,
        string context)
    {
        string invalidReason = GetEncounterCandidateInvalidReason(candidate);

        Debug.Log(
            $"[StageBattleSettingsSO] Encounter candidate. " +
            $"{context}, index={index}, valid={valid}, reason={invalidReason}, " +
            $"candidateNull={candidate == null}, " +
            $"weight={(candidate != null ? candidate.weight : -1)}, " +
            $"preset={(candidate != null && candidate.preset != null ? candidate.preset.name : "null")}, " +
            $"presetValid={(candidate != null && candidate.preset != null && candidate.preset.IsValid)}",
            candidate != null && candidate.preset != null ? candidate.preset : this);

        if (candidate != null && candidate.preset != null)
            LogEncounterPresetDetails(candidate.preset, context, index);
    }

    private static string GetEncounterCandidateInvalidReason(WeightedEncounterPreset candidate)
    {
        if (candidate == null)
            return "candidate is null";

        if (candidate.weight <= 0)
            return "weight <= 0";

        if (candidate.preset == null)
            return "preset is null";

        if (!candidate.preset.IsValid)
            return "preset.IsValid == false";

        return "valid";
    }

    private void LogEncounterPresetDetails(
        StageBattleEncounterPresetSO preset,
        string context,
        int candidateIndex)
    {
        if (!_debugEncounterPick || preset == null)
            return;

        int waveCount = preset.Waves != null ? preset.WaveCount : -1;

        Debug.Log(
            $"[StageBattleSettingsSO] Encounter preset details. " +
            $"{context}, candidateIndex={candidateIndex}, preset={preset.name}, waveCount={waveCount}, isValid={preset.IsValid}",
            preset);

        if (preset.Waves == null)
            return;

        for (int waveIndex = 0; waveIndex < preset.Waves.Count; waveIndex++)
        {
            StageBattleEncounterPresetSO.EncounterWave wave = preset.Waves[waveIndex];
            int candidateCount =
                wave != null && wave.enemyWavePresetCandidates != null
                    ? wave.enemyWavePresetCandidates.Count
                    : -1;

            Debug.Log(
                $"[StageBattleSettingsSO] Encounter wave details. " +
                $"{context}, preset={preset.name}, waveIndex={waveIndex}, waveNull={wave == null}, candidateCount={candidateCount}",
                preset);

            if (wave == null || wave.enemyWavePresetCandidates == null)
                continue;

            for (int enemyWaveCandidateIndex = 0;
                 enemyWaveCandidateIndex < wave.enemyWavePresetCandidates.Count;
                 enemyWaveCandidateIndex++)
            {
                StageBattleEncounterPresetSO.EnemyWavePresetCandidate waveCandidate =
                    wave.enemyWavePresetCandidates[enemyWaveCandidateIndex];

                StageEnemyWavePresetSO wavePreset =
                    waveCandidate != null ? waveCandidate.preset : null;

                Debug.Log(
                    $"[StageBattleSettingsSO] Enemy wave candidate details. " +
                    $"{context}, preset={preset.name}, waveIndex={waveIndex}, candidateIndex={enemyWaveCandidateIndex}, " +
                    $"candidateNull={waveCandidate == null}, " +
                    $"weight={(waveCandidate != null ? waveCandidate.weight : -1)}, " +
                    $"wavePreset={(wavePreset != null ? wavePreset.name : "null")}, " +
                    $"wavePresetValid={(wavePreset != null && wavePreset.IsValid)}, " +
                    $"totalSpawn={(wavePreset != null ? wavePreset.TotalSpawnCount : -1)}",
                    wavePreset != null ? wavePreset : preset);

                if (wavePreset != null)
                    LogEnemyWavePresetDetails(wavePreset, context, preset.name, waveIndex, enemyWaveCandidateIndex);
            }
        }
    }

    private void LogEnemyWavePresetDetails(
        StageEnemyWavePresetSO wavePreset,
        string context,
        string encounterPresetName,
        int encounterWaveIndex,
        int enemyWaveCandidateIndex)
    {
        if (!_debugEncounterPick || wavePreset == null)
            return;

        int enemyCount = wavePreset.Enemies != null ? wavePreset.Enemies.Count : -1;

        Debug.Log(
            $"[StageBattleSettingsSO] Enemy wave preset details. " +
            $"{context}, encounter={encounterPresetName}, encounterWaveIndex={encounterWaveIndex}, " +
            $"enemyWaveCandidateIndex={enemyWaveCandidateIndex}, wavePreset={wavePreset.name}, " +
            $"enemyCount={enemyCount}, totalSpawn={wavePreset.TotalSpawnCount}, isValid={wavePreset.IsValid}",
            wavePreset);

        if (wavePreset.Enemies == null)
            return;

        for (int enemyIndex = 0; enemyIndex < wavePreset.Enemies.Count; enemyIndex++)
        {
            StageEnemyWavePresetSO.EnemySpawn spawn = wavePreset.Enemies[enemyIndex];
            EnemyStatConfigSO archetype = spawn != null ? spawn.archetype : null;

            Debug.Log(
                $"[StageBattleSettingsSO] Enemy spawn details. " +
                $"{context}, wavePreset={wavePreset.name}, enemyIndex={enemyIndex}, " +
                $"spawnNull={spawn == null}, " +
                $"count={(spawn != null ? spawn.count : -1)}, " +
                $"archetype={(archetype != null ? archetype.name : "null")}, " +
                $"archetypeValid={(archetype != null && archetype.IsValid)}, " +
                $"enemyPrefab={(archetype != null && archetype.EnemyPrefab != null ? archetype.EnemyPrefab.name : "null")}",
                archetype != null ? archetype : wavePreset);
        }
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