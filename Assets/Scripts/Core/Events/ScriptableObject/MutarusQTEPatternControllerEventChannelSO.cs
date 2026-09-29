/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the MutarusQTEPatternControllerEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MutarusQTEPatternControllerEventChannel",
    menuName = "Events/Named Enemy/Mutarus QTE Pattern Controller Event Channel")]
public class MutarusQTEPatternControllerEventChannelSO : ScriptableObject
{
    public event Action<MutarusQTEPatternController> OnEventRaised;

    [NonSerialized] private MutarusQTEPatternController _current;

    public MutarusQTEPatternController Current => _current;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(MutarusQTEPatternController controller)
    {
        _current = controller;
        OnEventRaised?.Invoke(controller);
    }

    // Purpose: Clears the current clear state.
    public void Clear(MutarusQTEPatternController controller)
    {
        if (_current != controller)
            return;

        _current = null;
        OnEventRaised?.Invoke(null);
    }
}
