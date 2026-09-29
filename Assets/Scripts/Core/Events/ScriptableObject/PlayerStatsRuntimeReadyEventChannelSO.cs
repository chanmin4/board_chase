/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the PlayerStatsRuntimeReadyEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerStatsRuntimeReadyEventChannel",
    menuName = "Events/Player/Player Stats Runtime Ready Event Channel")]
public class PlayerStatsRuntimeReadyEventChannelSO : ScriptableObject
{
    public event Action<PlayerStatsRuntime> OnEventRaised;

    public PlayerStatsRuntime Current { get; private set; }
    public bool HasCurrent => Current != null;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(PlayerStatsRuntime runtime)
    {
        Current = runtime;
        OnEventRaised?.Invoke(runtime);
    }

    // Purpose: Clears the current clear state.
    public void Clear(PlayerStatsRuntime runtime)
    {
        if (Current == runtime)
            Current = null;
    }
}
