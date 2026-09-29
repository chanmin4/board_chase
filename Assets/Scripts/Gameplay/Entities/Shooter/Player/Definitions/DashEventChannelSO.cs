/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player > Definitions
 * Primary responsibility: Implements the focused DashEventType responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

public enum DashEventType
{
    Started,
    Finished,
    Canceled
}

[Serializable]
public struct DashEvent
{
    public DashEventType eventType;
    public GameObject sender;
    public Transform origin;
    public Vector3 direction;
    public float speed;
    public float duration;

    public DashEvent(
        DashEventType eventType,
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
    fileName = "DashEventChannel",
    menuName = "Events/Dash Event Channel")]
public class DashEventChannelSO : ScriptableObject
{
    public event Action<DashEvent> OnEventRaised;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(DashEvent dashEvent)
    {
        OnEventRaised?.Invoke(dashEvent);
    }
}
