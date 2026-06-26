using UnityEngine;

public enum SoundStimulusType
{
    Unknown,
    Gunshot,
    Footstep,
    Impact
}

public readonly struct SoundStimulus
{
    public readonly GameObject source;
    public readonly Vector3 position;
    public readonly float radius;
    public readonly float investigateDelaySeconds;
    public readonly SoundStimulusType type;
    public readonly float intensity;

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
            1f)
    {
    }

    public SoundStimulus(
        GameObject source,
        Vector3 position,
        float radius,
        float investigateDelaySeconds,
        SoundStimulusType type,
        float intensity = 1f)
    {
        this.source = source;
        this.position = position;
        this.radius = Mathf.Max(0f, radius);
        this.investigateDelaySeconds = Mathf.Max(0f, investigateDelaySeconds);
        this.type = type;
        this.intensity = Mathf.Max(0f, intensity);
    }

    public bool IsValid => radius > 0f && intensity > 0f;
}
