/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Handles SoundStimulusType audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public enum SoundStimulusType
{
    Unknown = 0,
    Gunshot = 1,
    Footstep = 2,
    Impact = 3,
    ProjectileFlyby = 4,
    ProjectileImpact = 5,
    Explosion = 6
}

public readonly struct SoundStimulus
{
    public readonly GameObject source;
    public readonly Vector3 position;
    public readonly float radius;
    public readonly float investigateDelaySeconds;
    public readonly SoundStimulusType type;
    public readonly float intensity;
    public readonly int emitterId;
    public readonly Vector3 direction;

    public SoundStimulus(
        GameObject source,
        Vector3 position,
        float radius,
        float investigateDelaySeconds)
        : this(
            source,
            position,
            radius,
            investigateDelaySeconds,
            SoundStimulusType.Gunshot,
            1f,
            0,
            Vector3.zero)
    {
    }

    public SoundStimulus(
        GameObject source,
        Vector3 position,
        float radius,
        float investigateDelaySeconds,
        SoundStimulusType type,
        float intensity = 1f)
        : this(
            source,
            position,
            radius,
            investigateDelaySeconds,
            type,
            intensity,
            0,
            Vector3.zero)
    {
    }

    public SoundStimulus(
        GameObject source,
        Vector3 position,
        float radius,
        float investigateDelaySeconds,
        SoundStimulusType type,
        float intensity,
        int emitterId,
        Vector3 direction)
    {
        this.source = source;
        this.position = position;
        this.radius = Mathf.Max(0f, radius);
        this.investigateDelaySeconds = Mathf.Max(0f, investigateDelaySeconds);
        this.type = type;
        this.intensity = Mathf.Max(0f, intensity);
        this.emitterId = emitterId;
        this.direction = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : Vector3.zero;
    }

    public bool IsValid => radius > 0f && intensity > 0f;
    public bool HasEmitter => emitterId != 0;
    public bool HasDirection => direction.sqrMagnitude > 0.0001f;
}
