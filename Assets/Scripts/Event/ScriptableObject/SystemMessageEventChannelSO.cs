using System;
using UnityEngine;

[Serializable]
public struct SystemMessageRequest
{
    public string message;
    public float duration;

    public SystemMessageRequest(string message, float duration)
    {
        this.message = message;
        this.duration = duration;
    }
}

[CreateAssetMenu(
    fileName = "SystemMessageEventChannel",
    menuName = "Events/UI/System Message Event Channel")]
public class SystemMessageEventChannelSO : ScriptableObject
{
    public event Action<SystemMessageRequest> OnEventRaised;

    public void RaiseEvent(string message, float duration = 2f)
    {
        OnEventRaised?.Invoke(new SystemMessageRequest(message, duration));
    }
}