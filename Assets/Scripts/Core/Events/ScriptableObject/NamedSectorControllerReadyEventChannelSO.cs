/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the NamedSectorControllerReadyEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

// Assets/Scripts/Event/ScriptableObject/NamedSectorControllerReadyEventChannelSO.cs
using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NamedSectorControllerReadyEventChannel",
    menuName = "Events/Named Sector Controller Ready Event Channel")]
public class NamedSectorControllerReadyEventChannelSO : ScriptableObject
{
    private NamedSectorController _current;

    public event Action<NamedSectorController> OnEventRaised;

    public NamedSectorController Current => _current;
    public bool HasCurrent => _current != null;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(NamedSectorController controller)
    {
        _current = controller;
        OnEventRaised?.Invoke(controller);
    }

    // Purpose: Clears the current clear state.
    public void Clear(NamedSectorController controller)
    {
        if (_current == controller)
            _current = null;
    }
}
