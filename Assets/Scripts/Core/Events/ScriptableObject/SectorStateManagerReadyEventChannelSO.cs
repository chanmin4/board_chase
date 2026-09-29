/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the SectorStateManagerReadyEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Publishes the event notification.
    public void RaiseEvent(SectorStateManager manager)
    {
        _current = manager;
        OnEventRaised?.Invoke(manager);
    }

    // Purpose: Clears the current clear state.
    public void Clear(SectorStateManager manager)
    {
        if (_current != manager)
            return;

        _current = null;
    }
}
