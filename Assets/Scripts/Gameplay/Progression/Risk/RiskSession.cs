/*
 * Role hierarchy: Gameplay > Progression > Risk
 * Primary responsibility: Implements the focused RiskSession responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

public static class RiskSession
{
    public static RiskSet Set;                     // 사용 중인 세트
    public static readonly List<RiskDef> Selected = new();  // 선택 결과

    // Purpose: Updates the selection state.
    public static void SetSelection(RiskSet set, IEnumerable<RiskDef> defs)
    {
        Set = set;
        Selected.Clear();
        if (defs != null) Selected.AddRange(defs);
    }

    // Purpose: Clears the current clear state.
    public static void Clear()
    {
        Set = null;
        Selected.Clear();
    }
}
