using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SectorStateManagerReadyEventChannel",
    menuName = "Events/Sector State Manager Ready Event Channel")]
public class SectorStateManagerReadyEventChannelSO : ScriptableObject
{
    private SectorStateManager _current;

    public event Action<SectorStateManager> OnEventRaised;

    public SectorStateManager Current => _current;
    public bool HasCurrent => _current != null;

    public void RaiseEvent(SectorStateManager manager)
    {
        _current = manager;
        OnEventRaised?.Invoke(manager);
    }

    public void Clear(SectorStateManager manager)
    {
        if (_current != manager)
            return;

        _current = null;
    }
}
