using UnityEngine;

public static class GameplayAttackInputBlocker
{
    private static Object _source;

    public static bool IsBlocked { get; private set; }

    public static void SetBlocked(bool blocked, Object source)
    {
        IsBlocked = blocked;
        _source = blocked ? source : null;
    }

    public static void Clear(Object source)
    {
        if (_source != null && source != null && _source != source)
            return;

        IsBlocked = false;
        _source = null;
    }
}
