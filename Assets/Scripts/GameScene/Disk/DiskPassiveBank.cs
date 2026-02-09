
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class DiskPassiveBank : MonoBehaviour
{
    [Header("Refs")]
    public CleanTrailAbility_Disk trail;
    public PlayerDisk playerdisk;
    //public PerfectBounce perfectbounce;
    public SurvivalGauge survivalgauge;
    public SurvivalDirector survivaldirector;

    [Header("Events")]
    public UnityEvent OnChanged;

    readonly Dictionary<string, int> stacks = new();
    readonly List<PassiveUpgradeDef> acquired = new();

    void Awake()
    {
        if (!trail)    trail    = GetComponent<CleanTrailAbility_Disk>();
        if (!playerdisk) playerdisk = GetComponent<PlayerDisk>();
    }

    public void Apply(PassiveUpgradeDef def)
    {
        if (!def) return;

        stacks.TryGetValue(def.id, out int cur);
        if (def.maxStacks > 0 && cur >= def.maxStacks) return;

        stacks[def.id] = cur + 1;
        acquired.Add(def);

        switch (def.effect)
        {
            case PassiveEffectType.InkRadiusAddWorld_Plus:
                if (trail) trail.radiusAddWorld += def.amount;
                break;
            case PassiveEffectType.InkRadiusConsume_Minus:
                if (survivalgauge) survivalgauge.baseCostPerMeter -= def.amount;
                break;
            case PassiveEffectType.InkRadiusConsumeEnemyInk_Minus:
                if (survivalgauge) survivalgauge.contamExtraMul -= def.amount;
                break;
            case PassiveEffectType.StunRecoverSpeedUp_Minus:
                if (survivalgauge) survivalgauge.recoverDuration -= def.amount;
                break;
        }

        OnChanged?.Invoke(); // 패시브 변동 알림
    }

    public IReadOnlyList<PassiveUpgradeDef> GetAcquired() => acquired;
    public int GetStacks(string id) => stacks.TryGetValue(id, out var s) ? s : 0;
}