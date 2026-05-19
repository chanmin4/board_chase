using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SectorRuntimeEventChannel",
    menuName = "Events/SectorRuntimeEventChannel")]
public class SectorRuntimeEventChannelSO : ScriptableObject
{
    public event Action<SectorRuntime> OnEventRaised;

    [SerializeField, ReadOnly] private SectorRuntime _current;

    public SectorRuntime Current => _current;
    public bool HasCurrent => _current != null;

    public void RaiseEvent(SectorRuntime sector)
    {
        _current = sector;
        OnEventRaised?.Invoke(sector);
    }

    public void Clear(SectorRuntime sector)
    {
        if (_current == sector)
            _current = null;
    }

    public void Clear()
    {
        _current = null;
    }
}