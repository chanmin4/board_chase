/*
 * Role hierarchy: Gameplay > World > Sectors
 * Primary responsibility: Defines the SectorRuntimeEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Publishes the event notification.
    public void RaiseEvent(SectorRuntime sector)
    {
        _current = sector;
        OnEventRaised?.Invoke(sector);
    }

    // Purpose: Clears the current clear state.
    public void Clear(SectorRuntime sector)
    {
        if (_current == sector)
            _current = null;
    }

    // Purpose: Clears the current clear state.
    public void Clear()
    {
        _current = null;
    }
}
