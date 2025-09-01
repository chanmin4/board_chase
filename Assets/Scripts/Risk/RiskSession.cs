using System.Collections.Generic;
using UnityEngine;

public static class RiskSession
{
    public static RiskSet Set;                     // 사용 중인 세트
    public static readonly List<RiskDef> Selected = new();  // 선택 결과

    public static void SetSelection(RiskSet set, IEnumerable<RiskDef> defs)
    {
        Set = set;
        Selected.Clear();
        if (defs != null) Selected.AddRange(defs);
    }

    public static void Clear()
    {
        Set = null;
        Selected.Clear();
    }
}
