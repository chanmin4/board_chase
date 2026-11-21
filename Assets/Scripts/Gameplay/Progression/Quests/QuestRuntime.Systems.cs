using System;
using UnityEngine;

public sealed partial class QuestRuntime
{
    public static bool HasVisitedStages(QuestDefinitionSO quest)
    {
        if (quest == null) return false;
        foreach (int stage in quest.RequiredVisitedStages)
            if (SaveSystem.Active?.Profile.visitedStageIndices?.Contains(stage) != true) return false;
        return true;
    }

    public static bool GrantCraftUnlock(string recipeId)
    {
        var profile = SaveSystem.Active?.Profile;
        if (profile == null || string.IsNullOrWhiteSpace(recipeId)) return false;
        var ids = profile.unlockedCraftRecipeIds ??= new();
        if (!ids.Contains(recipeId)) ids.Add(recipeId);
        return true; // Persisted with the enclosing quest reward transaction.
    }

    private bool GrantSharedStat(QuestDefinitionSO quest, int index, QuestRewardDefinition reward)
    {
        string group = quest.SharedGrowthRewardGroup;
        var profile = SaveSystem.Active?.Profile;
        if (profile == null) return false;
        var owners = profile.sharedQuestRewardGroups ??= new();
        if (!string.IsNullOrWhiteSpace(group))
        {
            string prefix = group + "|";
            string owner = owners.Find(x => x != null && x.StartsWith(prefix, StringComparison.Ordinal));
            if (owner != null && owner != prefix + quest.QuestId) return true;
            string receipt=prefix+quest.QuestId;
            if(owner==null)owners.Add(receipt); // Live stat callbacks may save the same profile.
            if (!TryGrantPersistentStatReward(quest, index, reward))
            {if(owner==null)owners.Remove(receipt);return false;}
            return true;
        }
        return TryGrantPersistentStatReward(quest, index, reward);
    }

    /// <summary>Stable authored action ID. Emits progress only; never auto-completes/claims a quest.</summary>
    public static void ReportAction(string actionId, int amount = 1,
        QuestObjectiveType type = QuestObjectiveType.Action, ItemSO item = null)
    {
#if UNITY_EDITOR
        if (CombatLabRuntime.IsIsolatedSession) return;
#endif
        var runtime = _instance;
        if (runtime == null || amount <= 0) return;
        bool changed = false;
        foreach (var state in runtime._states)
        {
            if (state == null || !state.IsAccepted || state.IsCompleted || state.Definition == null) continue;
            var objectives = state.Definition.Objectives;
            for (int i = 0; i < objectives.Length; i++)
            {
                var goal = objectives[i];
                if (goal == null || goal.ObjectiveType != type ||
                    (!string.IsNullOrEmpty(goal.ActionId) && goal.ActionId != actionId) ||
                    (goal.TargetItem != null && goal.TargetItem != item)) continue;
                int current = state.ObjectiveProgress[i];
                changed |= state.TrySetProgress(i, (int)Math.Min(goal.RequiredCount, (long)current + amount));
            }
        }
        if (changed) runtime.NotifyStateChanged();
    }
}
