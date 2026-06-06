using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerCurrencyRuntimeReadyEventChannel",
    menuName = "Events/Player/Player Currency Runtime Ready Event Channel")]
public class PlayerCurrencyRuntimeReadyEventChannelSO : ScriptableObject
{
    public event Action<PlayerCurrencyRuntime> OnEventRaised;

    public PlayerCurrencyRuntime Current { get; private set; }
    public bool HasCurrent => Current != null;

    public void RaiseEvent(PlayerCurrencyRuntime runtime)
    {
        Current = runtime;
        OnEventRaised?.Invoke(runtime);
    }

    public void Clear(PlayerCurrencyRuntime runtime)
    {
        if (Current == runtime)
            Current = null;
    }
}