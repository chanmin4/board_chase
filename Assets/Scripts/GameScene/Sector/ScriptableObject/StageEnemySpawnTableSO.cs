using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "StageEnemySpawnTable",
    menuName = "Game/Enemy/Stage Enemy Spawn Table")]
public class StageEnemySpawnTableSO : ScriptableObject
{
    [Serializable]
    public class SpawnEntry
    {
        public EnemyArchetypeSO archetype;
        // 이 stage에서 등장 가능한 일반몹 종류

        [Min(0)]
        public int weight = 1;
        // 선택 가중치. 값이 클수록 더 자주 뽑힘
    }

    [Serializable]
    public class StageSpawnRule
    {
        public int stageIndex;
        // 이 규칙이 적용될 stage 번호

        public string displayName;
        // 인스펙터 표시용 이름

        [Min(0)]
        public int sectorMinAlive = 0;
        // 이 stage에서 sector가 최소 유지하려는 적 수

        [Min(0)]
        public int sectorMaxAlive = 0;
        // 이 stage에서 sector가 최대로 유지할 적 수

        [Min(0.01f)]
        public float spawnIntervalSeconds = 5f;
        // maxAlive 미만일 때 다음 적을 뽑는 기본 간격

        public List<SpawnEntry> spawnEntries = new();
        // 이 stage에서 등장 가능한 적 목록과 weight
    }

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

        if (!TryGetRule(stageIndex, out StageSpawnRule rule) || rule.spawnEntries == null)
            return false;

        int totalWeight = 0;

        for (int i = 0; i < rule.spawnEntries.Count; i++)
        {
            SpawnEntry entry = rule.spawnEntries[i];
            if (!IsEntryValid(entry))
                continue;

            totalWeight += Mathf.Max(0, entry.weight);
        }

        if (totalWeight <= 0)
            return false;

        int roll = UnityEngine.Random.Range(0, totalWeight);

        for (int i = 0; i < rule.spawnEntries.Count; i++)
        {
            SpawnEntry entry = rule.spawnEntries[i];
            if (!IsEntryValid(entry))
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

    private static bool IsEntryValid(SpawnEntry entry)
    {
        return entry != null &&
               entry.archetype != null &&
               entry.archetype.IsValid &&
               entry.weight > 0;
    }
}
