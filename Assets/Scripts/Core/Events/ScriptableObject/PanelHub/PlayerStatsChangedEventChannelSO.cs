/*
 * Role hierarchy: Core > Events > ScriptableObject > PanelHub
 * Primary responsibility: Defines the PlayerStatsChangedEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Publishes the event notification.
    public void RaiseEvent(PlayerStatsSnapshot snapshot)
    {
        Current = snapshot;
        HasCurrent = true;
        OnEventRaised?.Invoke(snapshot);
    }

    // Purpose: Clears the current clear state.
    public void Clear()
    {
        Current = default;
        HasCurrent = false;
        OnEventRaised?.Invoke(Current);
    }
}
