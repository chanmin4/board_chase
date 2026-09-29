/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the PlayerRuntimeReadyEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerRuntimeReadyEventChannel",
    menuName = "Events/Player Runtime Ready Event Channel")]
public class PlayerRuntimeReadyEventChannelSO : ScriptableObject
{
    private Transform _current;

    public event Action<Transform> OnEventRaised;

    public Transform Current => _current;
    public bool HasCurrent => _current != null;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(Transform playerRoot)
    {
        _current = playerRoot;
        OnEventRaised?.Invoke(playerRoot);
    }

    // Purpose: Clears the current clear state.
    public void Clear(Transform playerRoot)
    {
        if (_current != playerRoot)
            return;

        _current = null;
    }
}
