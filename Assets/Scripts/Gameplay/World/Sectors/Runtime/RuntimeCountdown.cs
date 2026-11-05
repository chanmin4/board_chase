using UnityEngine;

/// <summary>Scaled countdown extracted from the retired named-reservation timer; no spawn/phase side effects.</summary>
public static class RuntimeCountdown
{
    public static bool Tick(ref float remaining, float delta)
    {
        if (delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return false;
        if (float.IsNaN(remaining) || float.IsInfinity(remaining)) return false;
        remaining = Mathf.Max(0f, remaining - delta);
        return remaining <= 0;
    }
}
