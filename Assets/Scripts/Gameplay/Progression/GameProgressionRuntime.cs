using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public sealed class PlayerExperienceSaveData
{
    public long total;
    public string lastSettlementId;
    public int lastSettlementAmount;
    public List<string> completedQuestIds = new();
    public List<string> settledRunIds = new();
}
[Serializable] public sealed class RunExperienceSaveData
{
    public string id;
    public int pending;
    public bool settled;
    public List<string> discoveries = new();
}

/// <summary>No independent XP window or hidden duplicate level. Profile total is authoritative.</summary>
public static class GameProgressionRuntime
{
    public static event Action Changed;
    private static PlayerStatsRuntime _playerStats;
    private static ExperienceRulesSO Rules => ExperienceRulesSO.Active;
    public static int Level => Rules != null ? Rules.experience.Level(SaveSystem.Active?.Profile.experience?.total ?? 0) : 1;
    public static bool InRaid => RaidRuntime.Active != null && RaidRuntime.Active.State?.active == true && RaidRuntime.Active.IsPlayerInRaid;
    public static bool PassiveEffectsActive => RaidRuntime.Active?.State?.active == true || RunLifecycleCoordinator.Instance?.Phase == RunLifecyclePhase.RunActive;
    public static bool IsHomePreparation => !PassiveEffectsActive && (RunLifecycleCoordinator.Instance == null || RunLifecycleCoordinator.Instance.Phase == RunLifecyclePhase.Home);
    public static bool CanChangePassives(out string message)
    {
        bool allowed = IsHomePreparation;
        message = allowed ? string.Empty : "모듈은 HOME에서만 장착·해제·교체할 수 있습니다.";
        return allowed;
    }
    public static bool CanUse(ItemSO item, out string message)
    {
        if (item is PassiveItemSO) { message = string.Empty; return true; } // Level gates slots/draws, never passive items.
        int required = ItemUsageRulesSO.Active != null ? ItemUsageRulesSO.Active.usage.Required(item) : Mathf.Max(1,item != null ? item.UsageLevelOverride : 1);
        message = $"사용 레벨 부족: 필요 Lv.{required} / 현재 Lv.{Level}";
        return Level >= required;
    }
    public static void BeginRun()
    {
        var save = SaveSystem.Active;
        if (save == null || IsIsolated) return;
        var run = save.RunData.experience;
        if (run == null || string.IsNullOrEmpty(run.id) || run.settled)
        {
            save.RunData.experience = new RunExperienceSaveData { id = Guid.NewGuid().ToString("N") };
            (save.RunData.startingPassives ??= new()).committed = true;
        }
        Changed?.Invoke();
    }
    public static void Discover(string key, int amount)
    {
        if(amount<=0||!MarkDiscovery(key))return;
        var run=SaveSystem.Active.RunData.experience;
        run.pending = (int)Math.Min(int.MaxValue,(long)Math.Max(0,run.pending) + ApplyExperienceBonus(amount));
        Changed?.Invoke();
    }
    public static bool MarkDiscovery(string key)
    {
        if(!InRaid||IsIsolated||string.IsNullOrEmpty(key))return false;
        BeginRun();var run=SaveSystem.Active?.RunData.experience;
        if(run==null||run.settled)return false;
        run.discoveries??=new();if(run.discoveries.Contains(key))return false;
        run.discoveries.Add(key);return true;
    }
    public static void EnterSector(SectorRuntime sector)
    {
        if (sector == null || Rules == null || !InRaid) return;
        string stage = RaidRuntime.Active.Definition.StageIndex + ":" + RaidRuntime.Active.State.layout.stageSeed;
        Discover("sector:" + stage + ":" + sector.Coord, Rules.experience.sectorDiscovery);
    }
    public static void Kill(GameObject attacker, Damageable target)
    {
        if (attacker == null || target == null || Rules == null || EntityComponentResolver.Resolve<PlayerStatsRuntime>(attacker.transform) == null) return;
        var enemy = EntityComponentResolver.Resolve<EnemyShooterStatsRuntime>(target);
        int amount = enemy?.Config?.EnemyRole == EnemyShooterRole.Boss ? Rules.experience.bossKill : Rules.experience.normalKill;
        Discover("kill:" + Identity(target), amount);
    }
    public static void GrantQuest(string questId, int amount = -1)
    {
        var save = SaveSystem.Active;
        if (save == null || IsIsolated || string.IsNullOrWhiteSpace(questId) || Rules == null) return;
        var data = save.Profile.experience ??= new();
        data.completedQuestIds ??= new();
        if (data.completedQuestIds.Contains(questId)) return;
        data.completedQuestIds.Add(questId);
        data.total = AddTotal(data.total,ApplyExperienceBonus(Mathf.Max(0, amount < 0 ? Rules.experience.defaultQuest : amount)));
        Notify();
    }
    // The profile retains all completed run ids, not only the most recent one.
    public static void Settle(bool survived)
    {
        var save = SaveSystem.Active;
        if (save == null || IsIsolated || Rules == null) return;
        var run = save.RunData.experience;
        if (run == null || run.settled || string.IsNullOrEmpty(run.id)) return;
        var profile = save.Profile.experience ??= new();
        profile.settledRunIds??=new();
        if (profile.lastSettlementId != run.id && !profile.settledRunIds.Contains(run.id))
        {
            int amount = SettlementAmount(run.pending,survived?Rules.experience.extractionMultiplier:Rules.experience.deathMultiplier);
            profile.total = AddTotal(profile.total,amount);
            profile.lastSettlementId = run.id;
            profile.lastSettlementAmount = amount;
            Debug.Log($"[Experience] {(survived ? "탈출" : "사망")} 정산 +{amount} / Lv.{Rules.experience.Level(profile.total)}");
        }
        if(!profile.settledRunIds.Contains(run.id))profile.settledRunIds.Add(run.id);
        run.settled = true;
        Notify();
    }
    private static void Notify()
    {
        if (_playerStats == null) _playerStats = UnityEngine.Object.FindFirstObjectByType<PlayerStatsRuntime>();
        _playerStats?.RebuildAndPublish();
        Changed?.Invoke();
    }
    public static int ApplyExperienceBonus(int amount)
    {
        if (_playerStats == null) _playerStats = UnityEngine.Object.FindFirstObjectByType<PlayerStatsRuntime>();
        float bonus = PassiveEffectsActive ? Mathf.Max(0, _playerStats?.AdditionalStat(PlayerStatId.ExperienceGainBonus) ?? 0) : 0;
        if (float.IsNaN(bonus) || float.IsInfinity(bonus)) bonus = 0;
        return (int)Math.Min(int.MaxValue, Math.Floor(Math.Max(0, amount) * (1d + bonus)));
    }
    public static int SettlementAmount(int pending,float multiplier)=>float.IsNaN(multiplier)||float.IsInfinity(multiplier)?0:
        (int)Math.Min(int.MaxValue,Math.Floor((double)Math.Max(0,pending)*Math.Max(0,multiplier)));
    private static long AddTotal(long current,int amount)
    {
        current=Math.Max(0,current);
        return long.MaxValue-current<amount?long.MaxValue:current+amount;
    }
    private static bool IsIsolated
    {
        get {
#if UNITY_EDITOR
            return CombatLabRuntime.IsIsolatedSession;
#else
            return false;
#endif
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { Changed = null; _playerStats = null; }
    public static string Identity(Component component)
    {
        if (component == null) return string.Empty;
        var generated=component.GetComponentInParent<GameProgressionIdentity>();
        if(generated!=null&&!string.IsNullOrEmpty(generated.Key))return generated.Key;
        string path = "";
        for (var t = component.transform; t != null; t = t.parent)
        {
            path = t.name + ":" + t.GetSiblingIndex() + "/" + path;
            if (t.GetComponent<SectorRuntime>() != null) break;
        }
        return (RaidRuntime.Active?.Definition?.StageIndex ?? 0) + ":" + (RaidRuntime.Active?.State?.layout?.stageSeed ?? 0) + ":" + path;
    }
}
