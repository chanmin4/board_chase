using System;
using System.Collections.Generic;

/// <summary>
/// 공통 LV 규칙: Stage S의 방탄복/일반 탄약은 S..S+3을 60:30:9:1로 선택한다.
/// 기존 확률이 정한 '방탄복/각 탄종이 나올 비중'은 유지하고, 그 안의 LV 분포만 교체한다.
/// 희귀도 비율을 다시 곱하지 않는다. 같은 LV 내 여러 후보의 상대 비중은 보존한다.
/// 테이블에 없는 아이템을 추가하지 않으며, 빠진/비활성 LV는 제외 후 재정규화한다.
/// </summary>
public sealed class StageEquipmentLootDistribution
{
    public sealed class Row
    {
        public LootItemEntry Entry;
        public PlayerShopItemRarity Rarity;
        public double Weight;
        public string Family;
        public int Level;
    }
    public readonly List<Row> Rows = new();
    public double TotalWeight { get; private set; }
    public bool HasCandidates => TotalWeight > 0;
    public bool HasLeveledEquipment { get; private set; }
    public static bool AppliesToStage(int stage) => stage >= 1 && stage <= 5;
    public static int LevelWeight(int stage, int level) => !AppliesToStage(stage) ? 0 : (level - stage) switch
    { 0 => 60, 1 => 30, 2 => 9, 3 => 1, _ => 0 };
    public const string RuleLabel = "방탄복·일반 탄약 LV: Stage S의 S / S+1 / S+2 / S+3 = 60 / 30 / 9 / 1";

    public static bool TryGetLevel(ItemSO item, out string family, out int level)
    {
        family = null; level = 0;
        if (item is ArmorItemSO armor) { family = "Armor"; level = armor.ArmorClass; }
        // Rocket / Universal ammunition has no authored LV1..8 ladder and stays on its old table.
        else if (item is BulletSO bullet && bullet.InfectionExposureOnHit <= 0f &&
            bullet.Family >= BulletFamily.Light && bullet.Family <= BulletFamily.ShotgunShell)
        { family = "Bullet:" + bullet.Family + ":" + bullet.AmmoType; level = (int)bullet.Tier; }
        return family != null && level >= 1 && level <= 8;
    }

    public void AddTier(LootRarityTier tier, LootCategorySelection selection, double tierWeight)
    {
        if (tier == null || selection == null || !selection.HasCandidates || !Positive(tierWeight)) return;
        foreach (var group in selection.Groups)
        {
            if (!group.Eligible) continue;
            for (int i = 0; i < group.Entries.Count; i++)
            {
                double weight = tierWeight * group.CategoryWeight / selection.TotalWeight * group.ItemWeights[i] / group.ItemTotal;
                var entry = group.Entries[i];
                if (entry?.item == null || !Positive(weight)) continue;
                bool leveled = TryGetLevel(entry.item, out var family, out int level);
                HasLeveledEquipment |= leveled;
                Rows.Add(new Row { Entry = entry, Rarity = tier.rarity, Weight = weight, Family = leveled ? family : null, Level = level });
            }
        }
    }

    /// <summary>Call once after AddTier. No serialized asset or source entry is modified.</summary>
    public void ApplyStage(int stage)
    {
        if (AppliesToStage(stage))
        {
            var families = new Dictionary<string, List<Row>>();
            foreach (var row in Rows)
            {
                if (row.Family == null) continue;
                if (!families.TryGetValue(row.Family, out var family)) families[row.Family] = family = new List<Row>();
                family.Add(row);
            }
            foreach (var family in families.Values)
            {
                double familyMass = 0;
                var levelMass = new Dictionary<int, double>();
                foreach (var row in family)
                {
                    familyMass += row.Weight;
                    if (!levelMass.ContainsKey(row.Level)) levelMass[row.Level] = 0;
                    levelMass[row.Level] += row.Weight;
                }
                double levelTotal = 0;
                foreach (var pair in levelMass) if (pair.Value > 0) levelTotal += LevelWeight(stage, pair.Key);
                foreach (var row in family)
                    row.Weight = levelTotal > 0 ? familyMass * LevelWeight(stage, row.Level) / levelTotal * row.Weight / levelMass[row.Level] : 0;
            }
        }
        TotalWeight = 0;
        foreach (var row in Rows) if (Positive(row.Weight)) TotalWeight += row.Weight;
    }

    public Row Pick(System.Random random) => PickUnit(random.NextDouble());
    public Row PickUnit(double sample)
    {
        if (!HasCandidates || double.IsNaN(sample)) return null;
        double pick = Math.Max(0, Math.Min(1, sample)) * TotalWeight;
        Row last = null;
        foreach (var row in Rows)
        {
            if (!Positive(row.Weight)) continue;
            last = row;
            if (pick < row.Weight) return row;
            pick -= row.Weight;
        }
        return last;
    }

    public float ItemPercent(LootItemEntry entry)
    {
        double mass = 0; foreach (var row in Rows) if (ReferenceEquals(row.Entry, entry)) mass += row.Weight;
        return TotalWeight > 0 ? (float)(mass / TotalWeight * 100) : 0;
    }
    public float RarityPercent(PlayerShopItemRarity rarity)
    {
        double mass = 0; foreach (var row in Rows) if (row.Rarity == rarity) mass += row.Weight;
        return TotalWeight > 0 ? (float)(mass / TotalWeight * 100) : 0;
    }
    private static bool Positive(double value) => value > 0 && !double.IsNaN(value) && !double.IsInfinity(value);
}
