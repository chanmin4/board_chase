using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// 디스크에 선택된 패시브를 누적/적용 관리.
/// - CleanTrailAbility_Disk, DiskLauncher 등 참조해서 실제 수치 갱신
[DisallowMultipleComponent]
public class DiskPassiveBank : MonoBehaviour
{
    [Header("Refs")]
    public CleanTrailAbility_Disk trail;
    public DiskLauncher launcher;
    public PerfectBounce perfectbounce;
    public SurvivalGauge survivalgauge;
    public SurvivalDirector survivaldirector;

    [Header("Events")]
    public UnityEvent OnChanged;  // UI 갱신용

    // 내부 상태
    readonly Dictionary<string, int> stacks = new();
    readonly List<PassiveUpgradeDef> acquired = new();

    void Awake()
    {
        if (!trail) trail = GetComponent<CleanTrailAbility_Disk>();
        if (!launcher) launcher = GetComponent<DiskLauncher>();
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
            case PassiveEffectType.LaunchCooldown_Minus:
                if (launcher) launcher.cooldownSeconds-= def.amount; 
                break;
            case PassiveEffectType.BounceInkGet_Plus:
                if (perfectbounce) perfectbounce.inkGainOnSuccess += def.amount;
                break;
            case PassiveEffectType.BounceInkLoss_Minus:
                if (perfectbounce) perfectbounce.inkLossOnFail-=def.amount;
                break;
            case PassiveEffectType.BounceSpeed_Plus:
                if (perfectbounce) perfectbounce.speedAddOnSuccess+=def.amount;
                break;
            case PassiveEffectType.InkRadiusConsume_Minus:
                if (survivalgauge) survivalgauge.baseCostPerMeter-=def.amount;
                break;
            case PassiveEffectType.InkRadiusConsumeEnemyInk_Minus:
                if (survivalgauge) survivalgauge.contamExtraMul -= def.amount;
                break; 
            case PassiveEffectType.StunRecoverSpeedUp_Minus:
                if (survivalgauge) survivalgauge.recoverDuration-= def.amount; 
                break;
            case PassiveEffectType.InkZonebonusHitInkRecover_Plus:
                if (survivalgauge) survivalgauge.zonebonusarc += def.amount;
                break;
            case PassiveEffectType.PerFectBounceRange_Plus:
                if (perfectbounce) perfectbounce.PerfectBounceDeg += def.amount;
                break;
            case PassiveEffectType.ZoneBonusArc_Plus:
                if (survivaldirector) survivaldirector.bonusArcDeg += def.amount;
                break;

        }

        OnChanged?.Invoke();
    }

    public IReadOnlyList<PassiveUpgradeDef> GetAcquired() => acquired;
    public int GetStacks(string id) => stacks.TryGetValue(id, out var s) ? s : 0;
}
