using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PassiveStatRollRange
{
    public PlayerStatId stat = PlayerStatId.MaxHealth;
    public float minimum = 1f;
    public float maximum = 5f;
    public StatModifierType type = StatModifierType.FlatAdd;
    [Min(0)] public float step;
    public float Roll(System.Random random,bool maximumRoll=false)
    {
        float low=Mathf.Min(minimum,maximum),high=Mathf.Max(minimum,maximum);
        if(maximumRoll)return high;
        if(step<=0)return Mathf.Lerp(low,high,(float)random.NextDouble());
        int steps=(int)System.Math.Min(int.MaxValue-1,System.Math.Max(0,System.Math.Floor(((double)high-low)/step+0.00001)));
        return Mathf.Min(high,low+random.Next(steps+1)*step);
    }
}

[Serializable]
public sealed class PassiveRarityRoll
{
    public PlayerShopItemRarity rarity;
    [Tooltip("각 행을 한 번씩 추첨. 절대 합산/% 보정과 간격을 행별로 지정합니다. 고정 패널티는 최소=최대로 작성하세요.")]
    public List<PassiveStatRollRange> stats = new();
}

/// <summary>Immutable per-item effects. The ID is carried by every existing item-GUID save path.</summary>
[Serializable]
public sealed class PassiveInstanceSaveData
{
    public string instanceId;
    public string definitionGuid;
    public PlayerShopItemRarity rarity;
    public PlayerStatModifier[] modifiers;
    public PlayerFeatureFlagModifier[] featureFlags;
    public bool boundToOwner;
    public bool disabledForRaid;
    public bool everEquipped;
    public int maximumDismantleYield;
}

public static class PassiveInstanceRuntime
{
    private const string Prefix = "passive-instance:";
    private static readonly Dictionary<string, PassiveItemSO> Live = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, PassiveInstanceSaveData> Records = new(StringComparer.Ordinal);
    private static bool CanPersist
    {
        get
        {
#if UNITY_EDITOR
            if (CombatLabRuntime.IsIsolatedSession) return false;
#endif
            return Application.isPlaying;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { Live.Clear(); Records.Clear(); }

    public static bool IsInstanceId(string id) => id != null && id.StartsWith(Prefix, StringComparison.Ordinal);
    public static bool NeedsRoll(ItemSO item) => item is PassiveItemSO p && !p.IsRolledInstance;

    public static bool MatchesDefinition(ItemSO owned, ItemSO requested)
    {
        if (owned == requested) return true;
        if (owned == null || requested == null) return false;
        // An instance request is exact. Only an authored template request accepts its rolled copies.
        if (requested is PassiveItemSO requestedPassive && !requestedPassive.IsRolledInstance && owned is PassiveItemSO passive)
            return !string.IsNullOrEmpty(requestedPassive.Guid) && passive.SourceDefinition.Guid == requestedPassive.Guid;
        return !string.IsNullOrEmpty(owned.Guid) && owned.Guid == requested.Guid;
    }

    public static bool CanCreate(ItemSO item,out string message,PlayerShopItemRarity? rarity=null)
    {
        message="";
        if(item==null){message="아이템 원본이 없습니다.";return false;}
        if(!NeedsRoll(item))return true;
        var source=(PassiveItemSO)item;
        var tier=source.FindRoll(rarity??source.Rarity);
        if(string.IsNullOrEmpty(source.Guid)||(source.RandomStatsEnabled&&(tier?.stats==null||tier.stats.Count==0)))
        {message=$"{source.name}: 해당 등급 패시브 수치 범위 또는 GUID가 없습니다.";return false;}
        if(source.RandomStatsEnabled)
            foreach(var range in tier.stats)
                if(range==null||!Finite(range.minimum)||!Finite(range.maximum)||!Finite(range.step)||range.step<0)
                {message=$"{source.name}: 패시브 수치/간격 설정이 잘못되었습니다.";return false;}
        return true;
    }
    public static ItemSO Create(ItemSO item, System.Random random = null, PlayerShopItemRarity? rarity = null, bool maximum = false)
    {
        // Loot Maker previews must not allocate persistent gameplay instances or touch a profile.
        if (!Application.isPlaying) return item;
        if (!NeedsRoll(item)) return item;
        if(!CanCreate(item,out string validation,rarity)){Debug.LogError("[Passive] "+validation,item);return null;}
        var source = (PassiveItemSO)item;
        var finalRarity = rarity ?? source.Rarity;
        var tier = source.FindRoll(finalRarity);
        if (string.IsNullOrEmpty(source.Guid) || (source.RandomStatsEnabled && (tier?.stats == null || tier.stats.Count == 0)))
        {
            Debug.LogError($"[Passive] {source.name}: {finalRarity} 랜덤 스탯 범위/원본 GUID가 없습니다. Item Maker에서 설정하세요.", source);
            return null;
        }
        random ??= new System.Random();
        var values = source.RandomStatsEnabled ? new PlayerStatModifier[tier.stats.Count] :
            (PlayerStatModifier[])(source.StatModifiers ?? Array.Empty<PlayerStatModifier>()).Clone();
        for (int i = 0; source.RandomStatsEnabled && i < values.Length; i++)
        {
            var range = tier.stats[i];
            if (range == null || !Finite(range.minimum) || !Finite(range.maximum))
                return null;
            float value = range.Roll(random,maximum);
            // Preserve old rolls/items, but unsupported rows contribute nothing. Do not delete the loot item.
            values[i] = new PlayerStatModifier { stat = range.stat, type = range.type,
                value = ShooterStatPolicy.IsPassiveStat(range.stat) ? value : 0f };
        }
        var record = new PassiveInstanceSaveData
        {
            instanceId = Prefix + System.Guid.NewGuid().ToString("N"), definitionGuid = source.Guid,
            rarity = finalRarity, modifiers = values,
            featureFlags = source.FeatureFlags != null ? (PlayerFeatureFlagModifier[])source.FeatureFlags.Clone() : Array.Empty<PlayerFeatureFlagModifier>()
        };
        var instance = UnityEngine.Object.Instantiate(source);
        instance.ConfigureInstance(source, record);
        Live.Add(record.instanceId, instance);
        Records[record.instanceId] = record;
        // Both partitions own their references: a run-only checkpoint must also contain the rolls.
        var saves = SaveSystem.Active;
        if (saves != null && CanPersist)
        {
            (saves.Profile.passiveInstances ??= new()).Add(record);
            (saves.RunData.passiveInstances ??= new()).Add(record);
        }
        return instance;
    }

    public static bool TryResolve(string id, RuntimeContentCatalogSO catalog, out ItemSO item)
    {
        item = null;
        if (!IsInstanceId(id) || catalog == null) return false;
        if (Live.TryGetValue(id, out var cached) && cached != null) { item = cached; return true; }
        var saves = SaveSystem.Active;
        var record = saves?.Profile.passiveInstances?.Find(x => x != null && x.instanceId == id) ??
            saves?.RunData.passiveInstances?.Find(x => x != null && x.instanceId == id);
        if (record == null || IsInstanceId(record.definitionGuid) ||
            !catalog.TryResolveItem(record.definitionGuid, out var definition)) return false;
        // The next ordinary save writes the canonical valuable GUID, not the old rolled instance ID.
        if (definition is ValuableItemSO { WasPassive: true })
        {
            item = definition;
            return true;
        }
        if (record.modifiers == null || !Enum.IsDefined(typeof(PlayerShopItemRarity), record.rarity) ||
            definition is not PassiveItemSO source) return false;
        foreach (var modifier in record.modifiers)
            if (!Enum.IsDefined(typeof(StatModifierType), modifier.type) || !Finite(modifier.value) || !Enum.IsDefined(typeof(PlayerStatId), modifier.stat)) return false;
        var restored = UnityEngine.Object.Instantiate(source);
        restored.ConfigureInstance(source, record);
        Live[id] = restored;
        Records[id] = record;
        // Promote run-only records before the run partition can be cleared or a warehouse is written.
        if (saves != null && CanPersist && !(saves.Profile.passiveInstances ??= new()).Exists(x => x != null && x.instanceId == id))
            saves.Profile.passiveInstances.Add(record);
        item = restored;
        return true;
    }

    public static void Append(List<LootRollResult> results, ItemSO item, int amount, PlayerShopItemRarity rarity, System.Random random)
    {
        if (!Application.isPlaying || !NeedsRoll(item)) { results.Add(new LootRollResult(item, amount, rarity)); return; }
        for (int i = 0; i < Mathf.Max(1, amount); i++)
        {
            var instance = Create(item, random, rarity);
            if (instance != null) results.Add(new LootRollResult(instance, 1, rarity));
        }
    }
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    public static bool Bind(PassiveItemSO item)
    {
        if (item == null || !item.IsRolledInstance || !Records.TryGetValue(item.Guid, out var record)) return false;
        record.boundToOwner = true;
        record.everEquipped = true;
        item.ConfigureInstance(item.SourceDefinition, record);
        CommitUpdatedRecord(record);
        return true;
    }

    public static bool Disable(PassiveItemSO item)
    {
        if (item == null || !item.IsRolledInstance || !Records.TryGetValue(item.Guid, out var record)) return false;
        record.disabledForRaid = true;
        record.boundToOwner = true;
        record.everEquipped = true;
        item.ConfigureInstance(item.SourceDefinition, record);
        CommitUpdatedRecord(record);
        return true;
    }

    public static bool LimitDismantleYield(PassiveItemSO item, int maximum)
    {
        if (item == null || !item.IsRolledInstance || !Records.TryGetValue(item.Guid, out var record)) return false;
        record.maximumDismantleYield = Mathf.Max(1, maximum);
        item.ConfigureInstance(item.SourceDefinition, record);
        CommitUpdatedRecord(record);
        return true;
    }

    // Preserve identity, rarity, binding and effect kinds. Only numeric values are rerolled.
    public static bool TryReroll(PassiveItemSO item, System.Random random = null)
    {
        if (item == null || item.DisabledForRaid || !item.AllowNumericReroll || !item.IsRolledInstance || !Records.TryGetValue(item.Guid, out var record)) return false;
        var ranges = item.SourceDefinition.FindRoll(item.Rarity)?.stats;
        if (ranges == null || ranges.Count == 0) return false;
        var values = (PlayerStatModifier[])record.modifiers.Clone();
        if (values.Length == 0) return false;
        for (int i = 0; i < values.Length; i++)
        {
            var range = ranges.Find(x => x != null && x.stat == values[i].stat);
            if (range == null || !Finite(range.minimum) || !Finite(range.maximum)) return false;
            random ??= new System.Random();
            if(range.type!=values[i].type)return false;
            values[i].value = range.Roll(random);
        }
        record.modifiers = values;
        item.ConfigureInstance(item.SourceDefinition, record);
        CommitUpdatedRecord(record);
        return true;
    }

    private static void CommitUpdatedRecord(PassiveInstanceSaveData record)
    {
        Records[record.instanceId] = record;
        var saves = SaveSystem.Active;
        if (saves == null || !CanPersist) return;
        // Profile and run deserialize into different record objects. Updating only one would
        // let the stale partition overwrite the new roll during snapshot synchronization.
        void Replace(List<PassiveInstanceSaveData> list)
        {
            int index = list.FindIndex(x => x != null && x.instanceId == record.instanceId);
            if (index < 0) list.Add(record); else list[index] = record;
        }
        Replace(saves.Profile.passiveInstances ??= new());
        Replace(saves.RunData.passiveInstances ??= new());
    }

    /// <summary>Persist reachable instances only. Keeps discarded rolls from growing the save forever.</summary>
    public static void SynchronizeSnapshots(Save profile, RunSaveData run)
    {
        if (profile == null || run == null) return;
        var profileRecords = profile.passiveInstances;
        var runRecords = run.passiveInstances;
        foreach (var row in profileRecords ?? new List<PassiveInstanceSaveData>())
            if (row != null && IsInstanceId(row.instanceId)) Records[row.instanceId] = row;
        foreach (var row in runRecords ?? new List<PassiveInstanceSaveData>())
            if (row != null && IsInstanceId(row.instanceId)) Records[row.instanceId] = row;
        string references;
        try
        {
            profile.passiveInstances = null;
            run.passiveInstances = null;
            references = JsonUtility.ToJson(profile) + JsonUtility.ToJson(run);
        }
        finally { profile.passiveInstances = profileRecords; run.passiveInstances = runRecords; }
        var kept = new List<PassiveInstanceSaveData>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(references, "passive-instance:[a-f0-9]{32}"))
            if (seen.Add(match.Value) && Records.TryGetValue(match.Value, out var row)) kept.Add(row);
        profile.passiveInstances = new List<PassiveInstanceSaveData>(kept);
        run.passiveInstances = new List<PassiveInstanceSaveData>(kept);
    }
}
