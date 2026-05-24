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

    public void RaiseEvent(PlayerStatsRuntime runtime)
    {
        Current = runtime;
        OnEventRaised?.Invoke(runtime);
    }

    public void Clear(PlayerStatsRuntime runtime)
    {
        if (Current == runtime)
            Current = null;
    }
}