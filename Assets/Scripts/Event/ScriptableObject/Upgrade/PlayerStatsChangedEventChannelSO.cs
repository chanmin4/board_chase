using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerStatsChangedEventChannel",
    menuName = "Events/Player/Player Stats Changed Event Channel")]
public class PlayerStatsChangedEventChannelSO : ScriptableObject
{
    public event Action<PlayerStatsSnapshot> OnEventRaised;

    public void RaiseEvent(PlayerStatsSnapshot snapshot)
    {
        OnEventRaised?.Invoke(snapshot);
    }
}
