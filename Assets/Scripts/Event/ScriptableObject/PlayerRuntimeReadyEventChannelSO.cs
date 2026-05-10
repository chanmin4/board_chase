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

    public void RaiseEvent(Transform playerRoot)
    {
        _current = playerRoot;
        OnEventRaised?.Invoke(playerRoot);
    }

    public void Clear(Transform playerRoot)
    {
        if (_current != playerRoot)
            return;

        _current = null;
    }
}
