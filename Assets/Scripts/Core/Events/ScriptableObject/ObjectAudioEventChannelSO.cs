/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Handles ObjectAudioCueType audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

public enum ObjectAudioCueType
{
    OnHit = 1,

    PlayerPortalInteract = 10,
    PlayerCorpseInteract = 11,
    PlayerTreasurePickup = 12
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

    // Purpose: Publishes the event notification.
    public void RaiseEvent(GameObject target, ObjectAudioCueType cueType)
    {
        RaiseEvent(new ObjectAudioRequest(target, cueType, default, false));
    }

    // Purpose: Publishes the event notification.
    public void RaiseEvent(GameObject target, ObjectAudioCueType cueType, Vector3 position)
    {
        RaiseEvent(new ObjectAudioRequest(target, cueType, position, true));
    }

    // Purpose: Publishes the event notification.
    public void RaiseEvent(ObjectAudioRequest request)
    {
        OnEventRaised?.Invoke(request);
    }
}
