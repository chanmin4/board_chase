using System;
using UnityEngine;

public enum ObjectAudioCueType
{
    Pickup = 0,
    Interact = 1,
    OnHit = 2
}

public readonly struct ObjectAudioRequest
{
    public readonly GameObject Target;
    public readonly ObjectAudioCueType CueType;
    public readonly Vector3 Position;
    public readonly bool UsePosition;

    public ObjectAudioRequest(
        GameObject target,
        ObjectAudioCueType cueType,
        Vector3 position,
        bool usePosition)
    {
        Target = target;
        CueType = cueType;
        Position = position;
        UsePosition = usePosition;
    }
}

[CreateAssetMenu(
    fileName = "ObjectAudioEventChannel",
    menuName = "Events/Audio/Object Audio Event Channel")]
public class ObjectAudioEventChannelSO : DescriptionBaseSO
{
    public event Action<ObjectAudioRequest> OnEventRaised;

    public void RaiseEvent(GameObject target, ObjectAudioCueType cueType)
    {
        RaiseEvent(new ObjectAudioRequest(target, cueType, default, false));
    }

    public void RaiseEvent(GameObject target, ObjectAudioCueType cueType, Vector3 position)
    {
        RaiseEvent(new ObjectAudioRequest(target, cueType, position, true));
    }

    public void RaiseEvent(ObjectAudioRequest request)
    {
        OnEventRaised?.Invoke(request);
    }
}
