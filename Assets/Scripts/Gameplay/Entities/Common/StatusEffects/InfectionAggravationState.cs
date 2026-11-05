using System;
using UnityEngine;

// Keep this saved type and RunData.infectionExposure key compatible with existing runs.
[Serializable]
public sealed class InfectionExposureSaveData
{
    public float gauge;
    public int stacks;
    public float nextTick = 5;
    public float recoveryDelayRemaining;
}

/// <summary>Pure accumulation arithmetic shared by the status runtime and regression checks.</summary>
public static class InfectionAggravationMath
{
    public static bool FinitePositive(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);
    public static void Add(InfectionExposureSaveData state, float amount, float capacity, int maximumStacks, float recoveryDelay)
        => Accumulate(state, amount, capacity, maximumStacks, recoveryDelay);

    public static int Accumulate(InfectionExposureSaveData state, float amount, float capacity, int maximumStacks, float recoveryDelay)
    {
        if (state == null || amount < 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return 0;
        capacity = Mathf.Max(1, capacity); maximumStacks = Mathf.Max(1, maximumStacks);
        double total = (double)Mathf.Max(0, state.gauge) + amount;
        long added = (long)Math.Min(int.MaxValue, Math.Floor(total / capacity));
        state.stacks = (int)Math.Min(maximumStacks, (long)Mathf.Max(0, state.stacks) + added);
        // Display stack cap never stops further circles or maximum-health erosion.
        state.gauge = (float)(total % capacity);
        state.recoveryDelayRemaining = Mathf.Max(0, recoveryDelay);
        return (int)added;
    }

    public static void Reduce(InfectionExposureSaveData state, float amount, float capacity)
    {
        if (state == null || !FinitePositive(amount)) return;
        capacity = Mathf.Max(1, capacity);
        double total = Math.Max(0, (double)Mathf.Max(0, state.stacks) * capacity + Mathf.Max(0, state.gauge) - amount);
        state.stacks = (int)Math.Min(int.MaxValue, Math.Floor(total / capacity));
        state.gauge = (float)(total % capacity);
    }

    public static float Tick(InfectionExposureSaveData state, float delta, float capacity, float interval, float damagePerStackPerTick,
        float recoveryPerSecond)
    {
        if (state == null || !FinitePositive(delta)) return 0;
        // nextTick and the old signature survive for save/caller compatibility, never for periodic damage.
        float recoverySeconds = Mathf.Max(0, delta - Mathf.Max(0, state.recoveryDelayRemaining));
        state.recoveryDelayRemaining = Mathf.Max(0, state.recoveryDelayRemaining - delta);
        state.gauge = Mathf.Max(0f, state.gauge - Mathf.Max(0, recoveryPerSecond) * recoverySeconds);
        return 0;
    }
}
