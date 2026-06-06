using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "StageEnemySetting",
    menuName = "Game/Enemy/Stage Enemy Setting")]
public class StageEnemySettingSO : ScriptableObject
{
    [Serializable]
    public class NormalEnemySpawnEntry
    {
        public EnemyStatConfigSO archetype;
        public int weight = 1;
    }

    [Serializable]
    public class NamedEnemySpawnEntry
    {
        public EnemyStatConfigSO archetype;
        public int weight = 1;
    }

    [Serializable]
    public class StageSpawnRule
    {
        [Tooltip("Stage index this rule applies to. 0 = first stage.")]
        public int stageIndex;

        [Tooltip("Inspector display name only.")]
        public string displayName;

        [Header("Normal Enemy Spawn")]
        [Min(0)]
        [Tooltip("Minimum number of normal enemies each opened sector tries to keep alive.")]
        public int sectorMinAlive = 0;

        [Min(0)]
        [Tooltip("Maximum number of normal enemies each opened sector can keep alive.")]
        public int sectorMaxAlive = 0;

        [Min(1)]
        [Tooltip("How many normal enemies are spawned at once per spawn tick in this stage.")]
        public int sectorNumberOfEnemiesSpawnedSimultaneously = 1;

        [Min(0f)]
        [Tooltip("Delay before the first normal enemy spawn after the stage starts or sector opens.")]
        public float firstSpawnIntervalSeconds = 5f;

        [Min(0.01f)]
        [Tooltip("Normal enemy spawn interval after the first spawn.")]
        public float spawnIntervalSeconds = 5f;

        [Tooltip("Normal enemies available in this stage.")]
        [FormerlySerializedAs("normalenemyspawnEntries")]
        public List<NormalEnemySpawnEntry> normalEnemySpawnEntries = new();

        [Header("Named Enemy Spawn")]
        [Tooltip("If false, this stage never starts named sector cycle even if entries exist.")]
        public bool namedCycleEnabled = true;

        [Tooltip("Named enemies available in this stage.")]
        public List<NamedEnemySpawnEntry> namedEnemyEntries = new();

        [Header("Named Boot")]
        [Tooltip("Start named sector cycle when SectorStateManager is ready or when this stage is applied.")]
        public bool startNamedCycleOnReady = true;

        [Tooltip("If true, first named sector is reserved immediately. If false, First Reservation Delay is used.")]
        public bool reserveFirstSectorImmediately = true;

        [Min(0f)]
        [Tooltip("Only used when Reserve First Sector Immediately is false.")]
        public float firstReservationDelay = 0f;

        [Header("Named Cycle")]
        [Min(0f)]
        [Tooltip("How long a selected sector stays reserved before named becomes present.")]
        public float reservationDuration = 30f;

        [Min(0f)]
        [Tooltip("Delay after named is killed before the next random sector is reserved.")]
        public float respawnCooldownAfterKill = 120f;

        [Min(0f)]
        [Tooltip("Retry delay when no valid opened sector can be selected.")]
        public float retryDelayWhenNoCandidate = 5f;

        [Header("Named Publish")]
        [Min(0.01f)]
        [Tooltip("How often named timer snapshot is sent to UI.")]
        public float timerPublishInterval = 0.1f;

        [Header("Debug")]
        [Min(0.1f)]
        [Tooltip("How often named timer debug logs are printed when debug logging is enabled.")]
        public float debugLogInterval = 1f;

        public int SimultaneousSpawnCount =>
            Mathf.Max(1, sectorNumberOfEnemiesSpawnedSimultaneously);
    }

    [Header("Stage Spawn Rules")]
    [Tooltip("Normal enemy spawn and named enemy cycle settings per stage.")]
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

    public bool TryPickNormalEntry(int stageIndex, out NormalEnemySpawnEntry entry)
    {
        entry = null;

        if (!TryGetRule(stageIndex, out StageSpawnRule rule) ||
            rule.normalEnemySpawnEntries == null)
        {
            return false;
        }

        return TryPickWeightedNormalEntry(rule.normalEnemySpawnEntries, out entry);
    }

    public bool TryPickArchetype(int stageIndex, out EnemyStatConfigSO archetype)
    {
        archetype = null;

        if (!TryPickNormalEntry(stageIndex, out NormalEnemySpawnEntry entry))
            return false;

        archetype = entry.archetype;
        return archetype != null;
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

    private static bool TryPickWeightedNormalEntry(
        List<NormalEnemySpawnEntry> entries,
        out NormalEnemySpawnEntry pickedEntry)
    {
        pickedEntry = null;
        int totalWeight = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            NormalEnemySpawnEntry entry = entries[i];

            if (!IsNormalEntryValid(entry))
                continue;

            totalWeight += Mathf.Max(0, entry.weight);
        }

        if (totalWeight <= 0)
            return false;

        int roll = UnityEngine.Random.Range(0, totalWeight);

        for (int i = 0; i < entries.Count; i++)
        {
            NormalEnemySpawnEntry entry = entries[i];

            if (!IsNormalEntryValid(entry))
                continue;

            int weight = Mathf.Max(0, entry.weight);

            if (roll < weight)
            {
                pickedEntry = entry;
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

    private static bool IsNormalEntryValid(NormalEnemySpawnEntry entry)
    {
        return entry != null &&
               entry.archetype != null &&
               entry.archetype.IsValid &&
               entry.weight > 0;
    }

    private static bool IsNamedEntryValid(NamedEnemySpawnEntry entry)
    {
        return entry != null &&
               entry.archetype != null &&
               entry.archetype.IsValid &&
               entry.weight > 0;
    }
}