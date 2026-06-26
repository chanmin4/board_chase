using System;
using UnityEngine;

public enum VSplatterDashEventType
{
    Started,
    Finished,
    Canceled
}

[Serializable]
public struct VSplatterDashEvent
{
    public VSplatterDashEventType eventType;
    public GameObject sender;
    public Transform origin;
    public Vector3 direction;
    public float speed;
    public float duration;

    public VSplatterDashEvent(
        VSplatterDashEventType eventType,
        GameObject sender,
        Transform origin,
        Vector3 direction,
        float speed,
        float duration)
    {
        this.eventType = eventType;
        this.sender = sender;
        this.origin = origin;
        this.direction = direction;
        this.speed = speed;
        this.duration = duration;
    }
}

[CreateAssetMenu(
    fileName = "VSplatterDashEventChannel",
    menuName = "Events/Player/VSplatter Dash Event Channel")]
public class VSplatterDashEventChannelSO : ScriptableObject
{
    public event Action<VSplatterDashEvent> OnEventRaised;

    public void RaiseEvent(VSplatterDashEvent dashEvent)
    {
        OnEventRaised?.Invoke(dashEvent);
    }
}
