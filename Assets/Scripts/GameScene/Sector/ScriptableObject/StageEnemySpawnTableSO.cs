using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "StageEnemySpawnTable",
    menuName = "Game/Enemy/Stage Enemy Spawn Table")]
public class StageEnemySpawnTableSO : ScriptableObject
{
    [Serializable]
    public class NormalEnemySpawnEntry
    {
        [Tooltip("Normal enemy archetype that can spawn in this stage.")]
        public EnemyArchetypeSO archetype;

        [Min(0)]
        [Tooltip("Weighted random value. 0 means this entry will not be picked.")]
        public int weight = 1;
    }

    [Serializable]
    public class NamedEnemySpawnEntry
    {
        [Tooltip("Named enemy archetype that can spawn in this stage.")]
        public EnemyArchetypeSO archetype;

        [Min(0)]
        [Tooltip("Weighted random value. 0 means this entry will not be picked.")]
        public int weight = 1;
    }

    [Serializable]
    public class StageSpawnRule
    {
        [Tooltip("Stage index this rule applies to.")]
        public int stageIndex;

        [Tooltip("Inspector display name only.")]
        public string displayName;

        [Header("Normal Enemy Spawn")]
        [Min(0)]
        [Tooltip("Minimum number of normal enemies this sector tries to keep alive.")]
        public int sectorMinAlive = 0;

        [Min(0)]
        [Tooltip("Maximum number of normal enemies this sector can keep alive.")]
        public int sectorMaxAlive = 0;

        [Min(0.01f)]
        [Tooltip("Normal enemy spawn interval when alive count is below max.")]
        public float spawnIntervalSeconds = 5f;

        [Tooltip("Normal enemies available in this stage.")]
        public List<NormalEnemySpawnEntry> normalenemyspawnEntries = new();

        [Header("Named Enemy Spawn")]
        [Tooltip("Named enemies available in this stage. Normal spawn interval/alive limits do not apply.")]
        public List<NamedEnemySpawnEntry> namedEnemyEntries = new();
    }
    [Header("Named Enemy Global")]
    [SerializeField, Min(0)]
    [Tooltip("Named encounter can start only at this stage or later.")]
    private int _minimumNamedStageToStart = 1;

    public int MinimumNamedStageToStart => _minimumNamedStageToStart;
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

    public bool TryPickArchetype(int stageIndex, out EnemyArchetypeSO archetype)
    {
        archetype = null;

        if (!TryGetRule(stageIndex, out StageSpawnRule rule) || rule.normalenemyspawnEntries == null)
            return false;

        return TryPickWeightedNormal(rule.normalenemyspawnEntries, out archetype);
    }

    public bool TryPickNamedArchetype(int stageIndex, out EnemyArchetypeSO archetype)
    {
        archetype = null;

        if (!TryGetRule(stageIndex, out StageSpawnRule rule) || rule.namedEnemyEntries == null)
            return false;

        return TryPickWeightedNamed(rule.namedEnemyEntries, out archetype);
    }
    public bool CanStartNamedCycle(int stageIndex)
    {
        if (stageIndex < _minimumNamedStageToStart)
            return false;

        return HasValidNamedEntry(stageIndex);
    }

    public bool HasValidNamedEntry(int stageIndex)
    {
        if (!TryGetRule(stageIndex, out StageSpawnRule rule) || rule.namedEnemyEntries == null)
            return false;

        for (int i = 0; i < rule.namedEnemyEntries.Count; i++)
        {
            if (IsNamedEntryValid(rule.namedEnemyEntries[i]))
                return true;
        }

        return false;
    }
    private static bool TryPickWeightedNormal(
        List<NormalEnemySpawnEntry> entries,
        out EnemyArchetypeSO archetype)
    {
        archetype = null;

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
                archetype = entry.archetype;
                return true;
            }

            roll -= weight;
        }

        return false;
    }

    private static bool TryPickWeightedNamed(
        List<NamedEnemySpawnEntry> entries,
        out EnemyArchetypeSO archetype)
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
