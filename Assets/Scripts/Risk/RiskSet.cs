using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct TypeCap
{
    public RiskType type;
    [Min(0)] public int maxActive; // 이 타입에서 동시에 켤 수 있는 최대 개수
}

[CreateAssetMenu(menuName = "Risk/Set", fileName = "RISKSET_Default")]
public class RiskSet : ScriptableObject
{
    [Header("Info")]
    public string setId = "default_contract";
    public string title = "도전 조정(기본)";
    [TextArea] public string desc = "위험 옵션을 선택해 난이도와 보상을 조정합니다.";

    [Header("Baseline (이번 모드의 기본값)")]
    [Min(0)] public float baseSuccessSeconds = 300f; // 5분
    [Min(0)] public float baseDragCooldown = 2.0f;   // 드래그 기본 쿨다운
    public ZoneLayout baseLayout = new ZoneLayout { large = 2, medium = 2, small = 1 };

    [Header("Scoring")]
    [Tooltip("총 위험도 1점당 목표 생존시간 보정(고정 5분이면 0)")]
    public float secondsPerPoint = 0f;

    [Header("Available Risks")]
    public List<RiskDef> available = new List<RiskDef>();

    [Header("Selection Rules")]
    [Tooltip("명시되지 않은 타입은 기본 cap 적용")]
    public int defaultTypeCap = 1; // ← 타입당 기본 최대 1개
    public TypeCap[] typeCaps = new TypeCap[] {
        // 필요할 때만 예외적으로 cap을 바꿔 넣으세요.
        // new TypeCap { type = RiskType.MissileCountUp, maxActive = 1 },
    };

    /// <summary>선택 가능한지 검증(배타/타입 한도)</summary>
    public bool CanToggle(List<RiskDef> selected, RiskDef def, bool turnOn, out string reason)
    {
        reason = null;
        if (!def) { reason = "정의 없음"; return false; }
        if (!turnOn) return true; // 끄는 건 항상 허용

        // 1) excludes 충돌
        if (def.excludes != null && selected != null)
        {
            foreach (var ex in def.excludes)
            {
                if (string.IsNullOrEmpty(ex)) continue;
                if (selected.Exists(r => r && r.riskId == ex))
                {
                    reason = $"충돌: {ex}";
                    return false;
                }
            }
        }

        // 2) 타입별 최대 개수 검사
        int cap = GetTypeCap(def.type);
        if (cap >= 0 && selected != null)
        {
            int cur = 0;
            foreach (var r in selected)
                if (r && r.type == def.type) cur++;

            if (cur >= cap)
            {
                reason = $"{def.type} 타입은 최대 {cap}개";
                return false;
            }
        }

        return true;
    }

    int GetTypeCap(RiskType t)
    {
        for (int i = 0; i < typeCaps.Length; i++)
            if (typeCaps[i].type.Equals(t)) return typeCaps[i].maxActive;

        return Mathf.Max(0, defaultTypeCap); // 기본 cap(보통 1개)
    }

    /// <summary>선택된 위험들의 총점</summary>
    public int SumPoints(List<RiskDef> selected)
    {
        int p = 0;
        if (selected == null) return p;
        foreach (var r in selected) if (r) p += Mathf.Max(0, r.points);
        return p;
    }

    /// <summary>목표 생존 시간(선택 점수 반영)</summary>
    public float ComputeTargetSeconds(List<RiskDef> selected)
    {
        return baseSuccessSeconds + SumPoints(selected) * secondsPerPoint;
    }
}
