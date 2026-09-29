/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the WorldCameraEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WorldCameraEventChannel",
    menuName = "Events/Camera/World Camera Event Channel")]
public class WorldCameraEventChannelSO : ScriptableObject
{
    public event Action<Camera> OnEventRaised;

    [NonSerialized] private Camera _current;

    public Camera Current => _current;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(Camera camera)
    {
        _current = camera;
        OnEventRaised?.Invoke(camera);
    }

    // Purpose: Clears the current clear state.
    public void Clear(Camera camera)
    {
        if (_current != camera)
            return;

        _current = null;
        OnEventRaised?.Invoke(null);
    }
}
