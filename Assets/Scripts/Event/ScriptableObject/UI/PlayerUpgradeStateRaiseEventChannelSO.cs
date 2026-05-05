using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerUpgradeStateReadyEventChannel",
    menuName = "Events/Upgrade/Player Upgrade State ReadyEvent Channel")]
public class PlayerUpgradeStateReadyEventChannelSO : ScriptableObject
{
    public event Action<PlayerUpgradeState> OnEventRaised;

    public PlayerUpgradeState Current { get; private set; }

    public void RaiseEvent(PlayerUpgradeState state)
    {
        Current = state;
        OnEventRaised?.Invoke(state);
    }

    public void Clear(PlayerUpgradeState state)
    {
        if (Current != state)
            return;

        Current = null;
        OnEventRaised?.Invoke(null);
    }
}
