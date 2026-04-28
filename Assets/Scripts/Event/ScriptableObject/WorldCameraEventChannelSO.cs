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

    public void RaiseEvent(Camera camera)
    {
        _current = camera;
        OnEventRaised?.Invoke(camera);
    }

    public void Clear(Camera camera)
    {
        if (_current != camera)
            return;

        _current = null;
        OnEventRaised?.Invoke(null);
    }
}
