/*
 * Role hierarchy: Presentation > UI > Gameplay > Overlay
 * Primary responsibility: Collects GameplayActionInputBlocker input and forwards it as gameplay action requests.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public static class GameplayActionInputBlocker
{
    private static Object _source;

    public static bool IsCombatInputBlocked { get; private set; }
    public static bool IsAttackBlocked => IsCombatInputBlocked;
    public static bool IsAimBlocked => IsCombatInputBlocked || UIEscapeRouter.HasOpenPopup;
    public static bool IsMovementBlocked => IsCombatInputBlocked;

    // Purpose: Updates the blocked state.
    public static void SetBlocked(bool blocked, Object source)
    {
        IsCombatInputBlocked = blocked;
        _source = blocked ? source : null;
    }

    // Purpose: Clears the current clear state.
    public static void Clear(Object source)
    {
        if (_source != null && source != null && _source != source)
            return;

        IsCombatInputBlocked = false;
        _source = null;
    }
}

public static class GameplayAttackInputBlocker
{
    public static bool IsBlocked => GameplayActionInputBlocker.IsAttackBlocked;

    // Purpose: Updates the blocked state.
    public static void SetBlocked(bool blocked, Object source)
    {
        GameplayActionInputBlocker.SetBlocked(blocked, source);
    }

    // Purpose: Clears the current clear state.
    public static void Clear(Object source)
    {
        GameplayActionInputBlocker.Clear(source);
    }
}
