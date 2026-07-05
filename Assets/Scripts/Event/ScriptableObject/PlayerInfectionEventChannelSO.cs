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

    public void RaiseEvent(PlayerShooterInfection playerInfection)
    {
        _current = playerInfection;
        OnEventRaised?.Invoke(playerInfection);
    }

    public void Clear(PlayerShooterInfection playerInfection)
    {
        if (_current != playerInfection)
            return;

        _current = null;
        OnEventRaised?.Invoke(null);
    }
}
