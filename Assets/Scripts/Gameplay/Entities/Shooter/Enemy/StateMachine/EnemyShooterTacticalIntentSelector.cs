/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > StateMachine
 * Primary responsibility: Coordinates a per-target duel holder so tactical EnemyShooters
 * divide hold-angle and flank movement without restricting fire permission.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

public enum EnemyShooterTacticalIntent
{
    Direct = 0,
    HoldLastKnownPosition = 1,
    FlankLeft = 2,
    FlankRight = 3
}

/// <summary>
/// Keeps a single shooter assigned to pressure a target's last-known position. Other
/// tactical shooters can then widen the angle instead of stacking in the same lane.
/// This is movement coordination only; it never limits attack, burst, or damage logic.
/// </summary>
public static class EnemyShooterTacticalIntentSelector
{
    private static readonly Dictionary<int, EnemyShooterCombatController> DuelHolders = new();

    // Purpose: Attempts to claim duel holder and reports whether the request succeeded.
    public static bool TryClaimDuelHolder(
        EnemyShooterCombatController requester,
        Transform target)
    {
        if (requester == null || target == null)
            return false;

        int targetId = target.GetInstanceID();

        if (DuelHolders.TryGetValue(targetId, out EnemyShooterCombatController holder))
        {
            if (IsValidHolder(holder, target))
                return holder == requester;

            DuelHolders.Remove(targetId);
        }

        DuelHolders[targetId] = requester;
        return true;
    }

    // Purpose: Checks whether this component has duel holder.
    public static bool HasDuelHolder(Transform target)
    {
        if (target == null)
            return false;

        int targetId = target.GetInstanceID();

        if (!DuelHolders.TryGetValue(targetId, out EnemyShooterCombatController holder))
            return false;

        if (IsValidHolder(holder, target))
            return true;

        DuelHolders.Remove(targetId);
        return false;
    }

    // Purpose: Checks whether duel holder is currently true.
    public static bool IsDuelHolder(
        EnemyShooterCombatController requester,
        Transform target)
    {
        if (requester == null || target == null)
            return false;

        int targetId = target.GetInstanceID();
        if (!DuelHolders.TryGetValue(targetId, out EnemyShooterCombatController holder))
            return false;

        if (!IsValidHolder(holder, target))
        {
            DuelHolders.Remove(targetId);
            return false;
        }

        return holder == requester;
    }

    // Purpose: Releases the owned duel holder resource.
    public static void ReleaseDuelHolder(EnemyShooterCombatController requester)
    {
        if (requester == null)
            return;

        int targetIdToRelease = 0;
        bool found = false;

        foreach (KeyValuePair<int, EnemyShooterCombatController> pair in DuelHolders)
        {
            if (pair.Value != requester)
                continue;

            targetIdToRelease = pair.Key;
            found = true;
            break;
        }

        if (found)
            DuelHolders.Remove(targetIdToRelease);
    }

    // Purpose: Checks whether valid holder is currently true.
    private static bool IsValidHolder(
        EnemyShooterCombatController holder,
        Transform target)
    {
        return holder != null &&
               holder.isActiveAndEnabled &&
               holder.HasTarget &&
               holder.Target == target;
    }
}
