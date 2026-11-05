using System;
using System.Collections.Generic;
using UnityEngine;

public partial class EnemyShooterStatsRuntime
{
    [System.NonSerialized] private List<PassiveItemSO> _passiveModules;
    public IReadOnlyList<PassiveItemSO> PassiveModules
    {
        get { EnsurePassiveModules(); return _passiveModules; }
    }

    public void ResetPassiveModules() => _passiveModules = null;

    private void EnsurePassiveModules()
    {
        if (_passiveModules != null) return;
        _passiveModules = new List<PassiveItemSO>();
        var rules = PassiveModuleRulesSO.Active;
        var config = Config;
        if (!Application.isPlaying || rules == null || config == null || EntityComponentResolver.Resolve<HomeQuestNpc>(this) != null) return;
        var order = new[] { PlayerShopItemRarity.Normal, PlayerShopItemRarity.Rare, PlayerShopItemRarity.Unique, PlayerShopItemRarity.Legendary, PlayerShopItemRarity.Mythic };
        int min = Array.IndexOf(order, config.PassiveMinimumRarity);
        int max = Array.IndexOf(order, config.PassiveMaximumRarity);
        if (min > max) (min, max) = (max, min);
        bool boss = config.EnemyRole == EnemyShooterRole.Boss;
        var random = new System.Random();
        void Add(PassiveItemSO source)
        {
            if (source == null) return;
            var tiers = new List<PlayerShopItemRarity>();
            for (int t = Mathf.Max(0, min); t <= max; t++)
                if (!source.RandomStatsEnabled || source.FindRoll(order[t])?.stats.Count > 0) tiers.Add(order[t]);
            if (tiers.Count == 0) return;
            var item = PassiveInstanceRuntime.Create(source, random, tiers[random.Next(tiers.Count)], boss) as PassiveItemSO;
            if (item != null && item.IsRolledInstance) { PassiveInstanceRuntime.Bind(item); _passiveModules.Add(item); }
        }
        var candidates = new List<PassiveItemSO>(rules.enemyPool ?? Array.Empty<PassiveItemSO>());
        while (_passiveModules.Count < config.PassiveSlotCount && candidates.Count > 0)
        {
            int index = random.Next(candidates.Count);
            var candidate = candidates[index]; candidates.RemoveAt(index);
            if (candidate == null || _passiveModules.Exists(x => candidate.SharesEffectWith(x))) continue;
            if (_passiveModules.FindAll(x => x.Category == candidate.Category).Count >= 4) continue;
            Add(candidate);
        }
    }
}
