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
    public class NamedEnemySpawnEntry
    {
        public EnemyStatConfigSO archetype;

        [Min(0)]
        public int weight = 1;
    }

    [Serializable]
    public class EnemyPresetSpawn
    {
        public EnemyStatConfigSO archetype;

        [Min(1)]
        public int count = 1;
    }

    [Serializable]
    public class EnemyWavePreset
    {
        public string displayName;
        public List<EnemyPresetSpawn> enemies = new();

        public int TotalSpawnCount
        {
            get
            {
                int total = 0;

                if (enemies == null)
                    return 0;

                for (int i = 0; i < enemies.Count; i++)
                {
                    EnemyPresetSpawn spawn = enemies[i];

                    if (IsEnemyPresetSpawnValid(spawn))
                        total += Mathf.Max(1, spawn.count);
                }

                return total;
            }
        }
    }

    [Serializable]
    public class EnemyWavePresetCandidate
    {
        [Min(0)]
        [Tooltip("Index into this stage rule's Enemy Wave Preset Library.")]
        public int enemyWavePresetIndex;

        [Min(0)]
        public int weight = 1;
    }

    [Serializable]
    public class NormalBattleEnemyWave
    {
        [Min(0f)]
        public float delaySeconds;

        public List<EnemyWavePresetCandidate> enemyPresetCandidates = new();
    }

    [Serializable]
    public class NormalBattleEncounterPreset
    {
        public string displayName;

        [Min(0)]
        public int weight = 1;

        [Min(0)]
        [Tooltip("Maximum enemies alive at the same time. 0 means no cap.")]
        public int sectorMaxAlive = 0;

        public List<NormalBattleEnemyWave> waves = new();
    }

    [Serializable]
    public class SectorObjectPresetSpawn
    {
        public SectorObjectConfigSO objectConfig;

        [Min(1)]
        public int count = 1;
    }

    [Serializable]
    public class SectorObjectRoomPreset
    {
        public string displayName;

        [Min(0)]
        public int weight = 1;

        public List<SectorObjectPresetSpawn> objects = new();
    }

    [Serializable]
    public class StageSpawnRule
    {
        public int stageIndex;
        public string displayName;

        [Header("Normal Battle Enemy Wave Library")]
        public List<EnemyWavePreset> enemyWavePresetLibrary = new();

        [Header("Normal Battle Encounter Presets")]
        [FormerlySerializedAs("normalBattleEnemyPresets")]
        public List<NormalBattleEncounterPreset> normalBattleEncounterPresets = new();

        [Header("Normal Battle Sector Object Presets")]
        [FormerlySerializedAs("sectorObjectPresets")]
        public List<SectorObjectRoomPreset> sectorObjectRoomPresets = new();

        [Header("Named Enemy Spawn")]
        public bool namedCycleEnabled = true;
        public List<NamedEnemySpawnEntry> namedEnemyEntries = new();

        [Header("Named Boot")]
        public bool startNamedCycleOnReady = true;
        public bool reserveFirstSectorImmediately = true;

        [Min(0f)]
        public float firstReservationDelay = 0f;

        [Header("Named Cycle")]
        [Min(0f)]
        public float reservationDuration = 30f;

        [Min(0f)]
        public float respawnCooldownAfterKill = 120f;

        [Min(0f)]
        public float retryDelayWhenNoCandidate = 5f;

        [Header("Named Publish")]
        [Min(0.01f)]
        public float timerPublishInterval = 0.1f;

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

    public bool TryPickNormalBattleEncounterPreset(
        int stageIndex,
        int stageSeed,
        Vector2Int sectorCoord,
        out NormalBattleEncounterPreset preset)
    {
        preset = null;

        if (!TryGetRule(stageIndex, out StageSpawnRule rule) ||
            rule.normalBattleEncounterPresets == null)
        {
            return false;
        }

        int seed = BuildSectorSeed(stageSeed, sectorCoord, 101);
        return TryPickWeightedEncounterPreset(rule, seed, out preset);
    }

    public bool TryPickEnemyWavePreset(
        StageSpawnRule rule,
        NormalBattleEnemyWave wave,
        int seed,
        out EnemyWavePreset preset)
    {
        preset = null;

        if (rule == null ||
            wave == null ||
            wave.enemyPresetCandidates == null)
        {
            return false;
        }

        return TryPickWeightedEnemyWavePreset(
            rule,
            wave.enemyPresetCandidates,
            seed,
            out preset);
    }

    public bool TryPickSectorObjectRoomPreset(
        int stageIndex,
        int stageSeed,
        Vector2Int sectorCoord,
        out SectorObjectRoomPreset preset)
    {
        preset = null;

        if (!TryGetRule(stageIndex, out StageSpawnRule rule) ||
            rule.sectorObjectRoomPresets == null)
        {
            return false;
        }

        int seed = BuildSectorSeed(stageSeed, sectorCoord, 307);
        return TryPickWeightedSectorObjectRoomPreset(
            rule.sectorObjectRoomPresets,
            seed,
            out preset);
    }

    public bool TryPickNamedArchetype(int stageIndex, out EnemyStatConfigSO archetype)
    {
        archetype = null;

        if (!TryGetRule(stageIndex, out StageSpawnRule rule) ||
            rule.namedEnemyEntries == null)
        {
            return false;
        }

        return TryPickWeightedNamed(rule.namedEnemyEntries, out archetype);
    }

    public bool CanStartNamedCycle(int stageIndex)
    {
        if (!TryGetRule(stageIndex, out StageSpawnRule rule))
            return false;

        if (!rule.namedCycleEnabled || !rule.startNamedCycleOnReady)
            return false;

        return HasValidNamedEntry(stageIndex);
    }

    public bool HasValidNamedEntry(int stageIndex)
    {
        if (!TryGetRule(stageIndex, out StageSpawnRule rule) ||
            rule.namedEnemyEntries == null)
        {
            return false;
        }

        for (int i = 0; i < rule.namedEnemyEntries.Count; i++)
        {
            if (IsNamedEntryValid(rule.namedEnemyEntries[i]))
                return true;
        }

        return false;
    }

    public bool TryGetNamedCycleRule(int stageIndex, out StageSpawnRule rule)
    {
        if (!TryGetRule(stageIndex, out rule))
            return false;

        return rule.namedCycleEnabled && HasValidNamedEntry(stageIndex);
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
        StageSpawnRule rule,
        int seed,
        out NormalBattleEncounterPreset pickedPreset)
    {
        pickedPreset = null;
        int totalWeight = 0;

        for (int i = 0; i < rule.normalBattleEncounterPresets.Count; i++)
        {
            NormalBattleEncounterPreset preset = rule.normalBattleEncounterPresets[i];

            if (!IsNormalBattleEncounterPresetValid(rule, preset))
                continue;

            totalWeight += Mathf.Max(0, preset.weight);
        }

        if (totalWeight <= 0)
            return false;

        int roll = new System.Random(seed).Next(0, totalWeight);

        for (int i = 0; i < rule.normalBattleEncounterPresets.Count; i++)
        {
            NormalBattleEncounterPreset preset = rule.normalBattleEncounterPresets[i];

            if (!IsNormalBattleEncounterPresetValid(rule, preset))
                continue;

            int weight = Mathf.Max(0, preset.weight);

            if (roll < weight)
            {
                pickedPreset = preset;
                return true;
            }

            roll -= weight;
        }

        return false;
    }

    private static bool TryPickWeightedEnemyWavePreset(
        StageSpawnRule rule,
        List<EnemyWavePresetCandidate> candidates,
        int seed,
        out EnemyWavePreset pickedPreset)
    {
        pickedPreset = null;
        int totalWeight = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            EnemyWavePresetCandidate candidate = candidates[i];

            if (!IsEnemyWavePresetCandidateValid(rule, candidate))
                continue;

            totalWeight += Mathf.Max(0, candidate.weight);
        }

        if (totalWeight <= 0)
            return false;

        int roll = new System.Random(seed).Next(0, totalWeight);

        for (int i = 0; i < candidates.Count; i++)
        {
            EnemyWavePresetCandidate candidate = candidates[i];

            if (!IsEnemyWavePresetCandidateValid(rule, candidate))
                continue;

            int weight = Mathf.Max(0, candidate.weight);

            if (roll < weight)
                return TryResolveEnemyWavePreset(rule, candidate, out pickedPreset);

            roll -= weight;
        }

        return false;
    }

    private static bool TryPickWeightedSectorObjectRoomPreset(
        List<SectorObjectRoomPreset> presets,
        int seed,
        out SectorObjectRoomPreset pickedPreset)
    {
        pickedPreset = null;
        int totalWeight = 0;

        for (int i = 0; i < presets.Count; i++)
        {
            SectorObjectRoomPreset preset = presets[i];

            if (!IsSectorObjectRoomPresetValid(preset))
                continue;

            totalWeight += Mathf.Max(0, preset.weight);
        }

        if (totalWeight <= 0)
            return false;

        int roll = new System.Random(seed).Next(0, totalWeight);

        for (int i = 0; i < presets.Count; i++)
        {
            SectorObjectRoomPreset preset = presets[i];

            if (!IsSectorObjectRoomPresetValid(preset))
                continue;

            int weight = Mathf.Max(0, preset.weight);

            if (roll < weight)
            {
                pickedPreset = preset;
                return true;
            }

            roll -= weight;
        }

        return false;
    }

    private static bool TryPickWeightedNamed(
        List<NamedEnemySpawnEntry> entries,
        out EnemyStatConfigSO archetype)
    {
        archetype = null;
        int totalWeight = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            NamedEnemySpawnEntry entry = entries[i];

            if (!IsNamedEntryValid(entry))
                continue;

            totalWeight += Mathf.Max(0, entry.weight);
        }

        if (totalWeight <= 0)
            return false;

        int roll = UnityEngine.Random.Range(0, totalWeight);

        for (int i = 0; i < entries.Count; i++)
        {
            NamedEnemySpawnEntry entry = entries[i];

            if (!IsNamedEntryValid(entry))
                continue;

            int weight = Mathf.Max(0, entry.weight);

            if (roll < weight)
            {
                archetype = entry.archetype;
                return true;
            }

            roll -= weight;
        }

        return false;
    }

    private static bool TryResolveEnemyWavePreset(
        StageSpawnRule rule,
        EnemyWavePresetCandidate candidate,
        out EnemyWavePreset preset)
    {
        preset = null;

        if (rule == null ||
            candidate == null ||
            rule.enemyWavePresetLibrary == null)
        {
            return false;
        }

        int index = candidate.enemyWavePresetIndex;

        if (index < 0 || index >= rule.enemyWavePresetLibrary.Count)
            return false;

        preset = rule.enemyWavePresetLibrary[index];
        return IsEnemyWavePresetValid(preset);
    }

    private static bool IsNormalBattleEncounterPresetValid(
        StageSpawnRule rule,
        NormalBattleEncounterPreset preset)
    {
        if (rule == null ||
            preset == null ||
            preset.weight <= 0 ||
            preset.waves == null ||
            preset.waves.Count <= 0)
        {
            return false;
        }

        for (int i = 0; i < preset.waves.Count; i++)
        {
            if (!IsNormalBattleEnemyWaveValid(rule, preset.waves[i]))
                return false;
        }

        return true;
    }

    private static bool IsNormalBattleEnemyWaveValid(
        StageSpawnRule rule,
        NormalBattleEnemyWave wave)
    {
        if (wave == null || wave.enemyPresetCandidates == null)
            return false;

        for (int i = 0; i < wave.enemyPresetCandidates.Count; i++)
        {
            if (IsEnemyWavePresetCandidateValid(rule, wave.enemyPresetCandidates[i]))
                return true;
        }

        return false;
    }

    private static bool IsEnemyWavePresetCandidateValid(
        StageSpawnRule rule,
        EnemyWavePresetCandidate candidate)
    {
        return candidate != null &&
               candidate.weight > 0 &&
               TryResolveEnemyWavePreset(rule, candidate, out _);
    }

    private static bool IsEnemyWavePresetValid(EnemyWavePreset preset)
    {
        return preset != null && preset.TotalSpawnCount > 0;
    }

    private static bool IsEnemyPresetSpawnValid(EnemyPresetSpawn spawn)
    {
        return spawn != null &&
               spawn.archetype != null &&
               spawn.archetype.IsValid &&
               spawn.count > 0;
    }

    private static bool IsSectorObjectRoomPresetValid(SectorObjectRoomPreset preset)
    {
        if (preset == null ||
            preset.weight <= 0 ||
            preset.objects == null)
        {
            return false;
        }

        for (int i = 0; i < preset.objects.Count; i++)
        {
            if (IsSectorObjectPresetSpawnValid(preset.objects[i]))
                return true;
        }

        return false;
    }

    private static bool IsSectorObjectPresetSpawnValid(SectorObjectPresetSpawn spawn)
    {
        return spawn != null &&
               spawn.objectConfig != null &&
               spawn.objectConfig.IsValid &&
               spawn.count > 0;
    }

    private static bool IsNamedEntryValid(NamedEnemySpawnEntry entry)
    {
        return entry != null &&
               entry.archetype != null &&
               entry.archetype.IsValid &&
               entry.weight > 0;
    }
}