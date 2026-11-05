using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class WeightedSectorLootObject
{
    public LootObjectSO lootObject;
    [Min(0)] public int weight = 1;
    [HideInInspector] public string persistenceKey;
    public bool IsValid => lootObject != null && lootObject.WorldPrefab != null && weight > 0 &&
        lootObject.WorldPrefab.GetComponentInChildren<LootableObject>(true) != null;
}

/// <summary>상자가 존재할지는 자리가 정하고, 이 테이블은 어떤 상자인지만 선택합니다.</summary>
[CreateAssetMenu(fileName = "LootObjectSpawnTable", menuName = "Game/Sector/Loot Object Spawn Table")]
public sealed class SectorLootObjectSpawnTableSO : ScriptableObject
{
    [SerializeField] private List<WeightedSectorLootObject> _candidates = new();
    public IReadOnlyList<WeightedSectorLootObject> Candidates => _candidates;
    public bool HasCandidates
    {
        get { if (_candidates != null) foreach (var row in _candidates) if (row != null && row.IsValid) return true; return false; }
    }

    public WeightedSectorLootObject Pick(System.Random random)
    {
        double total = 0;
        if (_candidates != null) foreach (var row in _candidates) if (row != null && row.IsValid) total += row.weight;
        if (total <= 0 || random == null) return null;
        double roll = random.NextDouble() * total;
        foreach (var row in _candidates)
        {
            if (row == null || !row.IsValid) continue;
            roll -= row.weight;
            if (roll < 0) return row;
        }
        return null;
    }
}
