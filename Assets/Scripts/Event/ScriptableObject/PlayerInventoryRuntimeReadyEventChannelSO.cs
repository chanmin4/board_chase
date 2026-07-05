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

    public void RaiseEvent(PlayerInventoryRuntime runtime)
    {
        Current = runtime;
        OnEventRaised?.Invoke(runtime);
    }

    public void Clear(PlayerInventoryRuntime runtime)
    {
        if (Current == runtime)
            Current = null;
    }
}
