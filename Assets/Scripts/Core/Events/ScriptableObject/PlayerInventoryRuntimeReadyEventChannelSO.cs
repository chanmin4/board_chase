/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the PlayerInventoryRuntimeReadyEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerInventoryRuntimeReadyEventChannel",
    menuName = "Events/Character/Player Inventory Runtime Ready Event Channel")]
public class PlayerInventoryRuntimeReadyEventChannelSO : ScriptableObject
{
    public event Action<PlayerInventoryRuntime> OnEventRaised;

    public PlayerInventoryRuntime Current { get; private set; }
    public bool HasCurrent => Current != null;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(PlayerInventoryRuntime runtime)
    {
        Current = runtime;
        OnEventRaised?.Invoke(runtime);
    }

    // Purpose: Clears the current clear state.
    public void Clear(PlayerInventoryRuntime runtime)
    {
        if (Current == runtime)
            Current = null;
    }
}
