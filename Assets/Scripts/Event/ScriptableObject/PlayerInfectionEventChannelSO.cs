using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerInfectionEventChannel",
    menuName = "Events/Player/Player Infection Event Channel")]
public class PlayerInfectionEventChannelSO : ScriptableObject
{
    public event Action<PlayerInfection> OnEventRaised;

    [NonSerialized] private PlayerInfection _current;

    public PlayerInfection Current => _current;

    public void RaiseEvent(PlayerInfection playerInfection)
    {
        _current = playerInfection;
        OnEventRaised?.Invoke(playerInfection);
    }

    public void Clear(PlayerInfection playerInfection)
    {
        if (_current != playerInfection)
            return;

        _current = null;
        OnEventRaised?.Invoke(null);
    }
}
