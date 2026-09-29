/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the PlayerInfectionEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerInfectionEventChannel",
    menuName = "Events/Player/Player Infection Event Channel")]
public class PlayerInfectionEventChannelSO : ScriptableObject
{
    public event Action<PlayerShooterInfection> OnEventRaised;

    [NonSerialized] private PlayerShooterInfection _current;

    public PlayerShooterInfection Current => _current;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(PlayerShooterInfection playerInfection)
    {
        _current = playerInfection;
        OnEventRaised?.Invoke(playerInfection);
    }

    // Purpose: Clears the current clear state.
    public void Clear(PlayerShooterInfection playerInfection)
    {
        if (_current != playerInfection)
            return;

        _current = null;
        OnEventRaised?.Invoke(null);
    }
}
