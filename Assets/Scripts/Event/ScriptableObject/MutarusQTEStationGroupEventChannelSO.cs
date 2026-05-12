using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MutarusQTEStationGroupEventChannel",
    menuName = "Events/Named Enemy/Mutarus QTE Station Group Event Channel")]
public class MutarusQTEStationGroupEventChannelSO : ScriptableObject
{
    public event Action<MutarusQTEStationGroup> OnEventRaised;

    [NonSerialized] private MutarusQTEStationGroup _current;

    public MutarusQTEStationGroup Current => _current;

    public void RaiseEvent(MutarusQTEStationGroup group)
    {
        _current = group;
        OnEventRaised?.Invoke(group);
    }

    public void Clear(MutarusQTEStationGroup group)
    {
        if (_current != group)
            return;

        _current = null;
        OnEventRaised?.Invoke(null);
    }
}
