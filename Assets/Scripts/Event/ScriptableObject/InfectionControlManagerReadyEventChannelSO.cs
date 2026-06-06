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

    public void RaiseEvent(InfectionControlManager manager)
    {
        _current = manager;
        OnEventRaised?.Invoke(manager);
    }

    public void Clear(InfectionControlManager manager)
    {
        if (_current == manager)
            _current = null;
    }
}