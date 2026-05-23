using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerStatsChangedEventChannel",
    menuName = "Events/Player/Player Stats Changed Event Channel")]
public class PlayerStatsChangedEventChannelSO : ScriptableObject
{
    public event Action<PlayerStatsSnapshot> OnEventRaised;

    public bool HasCurrent { get; private set; }
    public PlayerStatsSnapshot Current { get; private set; }

    public void RaiseEvent(PlayerStatsSnapshot snapshot)
    {
        Current = snapshot;
        HasCurrent = true;
        OnEventRaised?.Invoke(snapshot);
    }

    public void Clear()
    {
        Current = default;
        HasCurrent = false;
        OnEventRaised?.Invoke(Current);
    }
}