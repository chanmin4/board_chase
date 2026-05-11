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

    public void RaiseEvent(MutarusQTEPatternController controller)
    {
        _current = controller;
        OnEventRaised?.Invoke(controller);
    }

    public void Clear(MutarusQTEPatternController controller)
    {
        if (_current != controller)
            return;

        _current = null;
        OnEventRaised?.Invoke(null);
    }
}
