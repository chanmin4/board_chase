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
            case PassiveEffectType.InkRadiusMulAdd:
                if (trail) trail.radiusMul += def.amount;
                break;

            case PassiveEffectType.InkRadiusAddWorldAdd:
                if (trail) trail.radiusAddWorld += def.amount;
                break;

            case PassiveEffectType.LaunchCooldownMul:
                if (launcher) launcher.cooldownSeconds *= def.mul; // 예: 0.9 → 10% 단축
                break;
        }

        OnChanged?.Invoke();
    }

    public IReadOnlyList<PassiveUpgradeDef> GetAcquired() => acquired;
    public int GetStacks(string id) => stacks.TryGetValue(id, out var s) ? s : 0;
}
