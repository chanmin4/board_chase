using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NamedBattleSignalEventChannel",
    menuName = "Events/Named Battle Signal Event Channel")]
public class NamedBattleSignalEventChannelSO : ScriptableObject
{
    public event System.Action<SectorRuntime> OnEventRaised;

    public void RaiseEvent(SectorRuntime sector)
    {
        OnEventRaised?.Invoke(sector);
    }
}
