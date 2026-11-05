using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class WeightedSectorWorldItem
{
    public ItemSO item;
    [Min(0)] public int weight = 1;
    [Min(1)] public int minAmount = 1;
    [Min(1)] public int maxAmount = 1;
    [Tooltip("장비/의료품/필터 내구도를 정할 때만 켭니다. 끄면 새 제품 상태입니다.")]
    public bool overrideCondition;
    [Range(0, 100)] public float minConditionPercent = 100;
    [Range(0, 100)] public float maxConditionPercent = 100;
    public bool IsValid => item != null && item.WorldVisualPrefab != null && weight > 0;

    public int RollAmount(System.Random random)
    {
        int limit = item != null ? item.MaxStack : 1;
        // 내구도를 가진 장비는 하나의 월드 개체에 하나의 상태만 저장합니다.
        if (item is ArmorItemSO || item is GasMaskItemSO || item is RangedWeaponSO ||
            item is ConsumableItemSO medical && medical.UsesDurability) limit = 1;
        int min = Mathf.Clamp(minAmount, 1, limit), max = Mathf.Clamp(maxAmount, min, limit);
        return min + (int)(random.NextDouble() * ((long)max - min + 1));
    }

    public float RollCondition(System.Random random)
    {
        if (!overrideCondition) return 1f;
        float min = Mathf.Clamp01(minConditionPercent / 100f);
        float max = Mathf.Clamp(maxConditionPercent / 100f, min, 1f);
        return Mathf.Lerp(min, max, (float)random.NextDouble());
    }
}

[CreateAssetMenu(fileName = "WorldItemSpawnTable", menuName = "Game/Sector/World Item Spawn Table")]
public sealed class SectorWorldItemSpawnTableSO : ScriptableObject
{
    [SerializeField] private List<WeightedSectorWorldItem> _candidates = new();
    public IReadOnlyList<WeightedSectorWorldItem> Candidates => _candidates;
    public bool HasCandidates
    {
        get { if (_candidates != null) foreach (var row in _candidates) if (CanSpawnInField(row)) return true; return false; }
    }
    private static bool CanSpawnInField(WeightedSectorWorldItem row) =>
        row != null && row.IsValid && row.item is not PassiveItemSO;

    public WeightedSectorWorldItem Pick(System.Random random)
    {
        double total = 0;
        if (_candidates != null) foreach (var row in _candidates) if (CanSpawnInField(row)) total += row.weight;
        if (total <= 0 || random == null) return null;
        double roll = random.NextDouble() * total;
        foreach (var row in _candidates)
        {
            if (!CanSpawnInField(row)) continue;
            roll -= row.weight;
            if (roll < 0) return row;
        }
        return null;
    }
}
