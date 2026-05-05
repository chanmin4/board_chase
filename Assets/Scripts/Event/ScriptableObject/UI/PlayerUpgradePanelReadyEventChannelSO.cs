using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerUpgradePanelReadyEventChannel",
    menuName = "Events/UI/Player Upgrade Panel Ready Event Channel")]
public class PlayerUpgradePanelReadyEventChannelSO : ScriptableObject
{
    public event Action<PlayerUpgradePanelUI> OnEventRaised;

    public PlayerUpgradePanelUI Current { get; private set; }

    public void RaiseEvent(PlayerUpgradePanelUI panel)
    {
        Current = panel;
        OnEventRaised?.Invoke(panel);
    }

    public void Clear(PlayerUpgradePanelUI panel)
    {
        if (Current != panel)
            return;

        Current = null;
        OnEventRaised?.Invoke(null);
    }
}
