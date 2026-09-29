/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the InfectionControlManagerReadyEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

// Assets/Scripts/Event/ScriptableObject/InfectionControlManagerReadyEventChannelSO.cs
using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "InfectionControlManagerReadyEventChannel",
    menuName = "Events/Infection Control Manager Ready Event Channel")]
public class InfectionControlManagerReadyEventChannelSO : ScriptableObject
{
    private InfectionControlManager _current;

    public event Action<InfectionControlManager> OnEventRaised;

    public InfectionControlManager Current => _current;
    public bool HasCurrent => _current != null;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(InfectionControlManager manager)
    {
        _current = manager;
        OnEventRaised?.Invoke(manager);
    }

    // Purpose: Clears the current clear state.
    public void Clear(InfectionControlManager manager)
    {
        if (_current == manager)
            _current = null;
    }
}
