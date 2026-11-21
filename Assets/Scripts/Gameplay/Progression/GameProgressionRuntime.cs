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
    public static int Level => 1; // WIP compatibility only; XP no longer gates gameplay.
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
        message = string.Empty;
        return item != null;
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
        // Retired XP entry point. Existing callers and serialized data remain compatible.
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
        if (sector == null || !InRaid) return;
        var profile = SaveSystem.Active?.Profile;
        int stage = RaidRuntime.Active.Definition.StageIndex;
        if (profile != null && !(profile.visitedStageIndices ??= new()).Contains(stage))
        {
            profile.visitedStageIndices.Add(stage);
            SaveSystem.Active.SaveDataToDisk();
            Changed?.Invoke();
        }
    }
    public static void Kill(GameObject attacker, Damageable target)
    {
        // Retired XP entry point. Existing callers and serialized data remain compatible.
    }
    public static void GrantQuest(string questId, int amount = -1)
    {
        // Retired XP entry point. Existing callers and serialized data remain compatible.
    }
    // The profile retains all completed run ids, not only the most recent one.
    public static void Settle(bool survived)
    {
        // Retired XP entry point. Existing callers and serialized data remain compatible.
    }
    private static void Notify()
    {
        if (_playerStats == null) _playerStats = UnityEngine.Object.FindFirstObjectByType<PlayerStatsRuntime>();
        _playerStats?.RebuildAndPublish();
        Changed?.Invoke();
    }
    public static int ApplyExperienceBonus(int amount)
    {
        return 0; // Experience bonuses are retired.
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
