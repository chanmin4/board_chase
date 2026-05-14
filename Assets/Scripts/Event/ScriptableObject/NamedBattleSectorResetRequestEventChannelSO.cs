using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NamedBattleSectorResetRequestEventChannel",
    menuName = "Events/Named Enemy/Named Battle Sector Reset Request Event Channel")]
public class NamedBattleSectorResetRequestEventChannelSO : ScriptableObject
{
    public event Action OnEventRaised;

    public void RaiseEvent()
    {
        OnEventRaised?.Invoke();
    }
}
