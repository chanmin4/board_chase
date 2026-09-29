/*
 * Role hierarchy: WIP > Legacy > NumericCurrency
 * Primary responsibility: Retains serialized numeric-currency ready-event assets while the active economy uses physical item stacks.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerCurrencyRuntimeReadyEventChannel",
    menuName = "WIP/Legacy/Player Currency Runtime Ready Event Channel")]
public class PlayerCurrencyRuntimeReadyEventChannelSO : ScriptableObject
{
    public event Action<PlayerCurrencyRuntime> OnEventRaised;

    public PlayerCurrencyRuntime Current { get; private set; }
    public bool HasCurrent => Current != null;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(PlayerCurrencyRuntime runtime)
    {
        Current = runtime;
        OnEventRaised?.Invoke(runtime);
    }

    // Purpose: Clears the current clear state.
    public void Clear(PlayerCurrencyRuntime runtime)
    {
        if (Current == runtime)
            Current = null;
    }
}
