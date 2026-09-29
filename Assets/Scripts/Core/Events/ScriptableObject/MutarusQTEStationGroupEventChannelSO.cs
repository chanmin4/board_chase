/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the MutarusQTEStationGroupEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Publishes the event notification.
    public void RaiseEvent(MutarusQTEStationGroup group)
    {
        _current = group;
        OnEventRaised?.Invoke(group);
    }

    // Purpose: Clears the current clear state.
    public void Clear(MutarusQTEStationGroup group)
    {
        if (_current != group)
            return;

        _current = null;
        OnEventRaised?.Invoke(null);
    }
}
