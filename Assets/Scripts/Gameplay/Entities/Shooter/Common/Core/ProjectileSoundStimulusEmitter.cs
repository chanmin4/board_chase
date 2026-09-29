/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Handles ProjectileSoundStimulusEmitter combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// Emits mechanical AI-hearing events for a single projectile. It is not an
/// AudioSource and does not control player-facing sound playback.
/// </summary>
public sealed class ProjectileSoundStimulusEmitter
{
    private SoundStimulusEventChannelSO _channel;
    private GameObject _source;
    private int _emitterId;
    private float _flybyRadius;
    private float _flybyEmitDistance;
    private float _flybyIntensity;
    private float _impactRadius;
    private float _impactIntensity;
    private float _investigateDelaySeconds;
    private float _distanceUntilNextFlyby;

    // Purpose: Configures this component''s configure.
    public void Configure(
        SoundStimulusEventChannelSO channel,
        GameObject source,
        GameObject emitter,
        float flybyRadius,
        float flybyEmitDistance,
        float flybyIntensity,
        float impactRadius,
        float impactIntensity,
        float investigateDelaySeconds)
    {
        _channel = channel;
        _source = source;
        _emitterId = emitter != null ? emitter.GetInstanceID() : 0;
        _flybyRadius = Mathf.Max(0f, flybyRadius);
        _flybyEmitDistance = Mathf.Max(0.1f, flybyEmitDistance);
        _flybyIntensity = Mathf.Max(0f, flybyIntensity);
        _impactRadius = Mathf.Max(0f, impactRadius);
        _impactIntensity = Mathf.Max(0f, impactIntensity);
        _investigateDelaySeconds = Mathf.Max(0f, investigateDelaySeconds);
        _distanceUntilNextFlyby = _flybyEmitDistance;
    }

    // Purpose: Performs the emit travel segment workflow for this component.
    public void EmitTravelSegment(Vector3 start, Vector3 end)
    {
        if (_channel == null || _flybyRadius <= 0f || _flybyIntensity <= 0f)
            return;

        Vector3 segment = end - start;
        float segmentDistance = segment.magnitude;

        if (segmentDistance <= 0.0001f)
            return;

        Vector3 direction = segment / segmentDistance;
        float consumedDistance = 0f;
        float remainingDistance = segmentDistance;

        while (remainingDistance >= _distanceUntilNextFlyby)
        {
            consumedDistance += _distanceUntilNextFlyby;
            Vector3 position = start + direction * consumedDistance;

            _channel.RaiseEvent(new SoundStimulus(
                _source,
                position,
                _flybyRadius,
                _investigateDelaySeconds,
                SoundStimulusType.ProjectileFlyby,
                _flybyIntensity,
                _emitterId,
                direction));

            remainingDistance -= _distanceUntilNextFlyby;
            _distanceUntilNextFlyby = _flybyEmitDistance;
        }

        _distanceUntilNextFlyby -= remainingDistance;
    }

    // Purpose: Performs the emit impact workflow for this component.
    public void EmitImpact(Vector3 position, Vector3 direction)
    {
        if (_channel == null || _impactRadius <= 0f || _impactIntensity <= 0f)
            return;

        _channel.RaiseEvent(new SoundStimulus(
            _source,
            position,
            _impactRadius,
            _investigateDelaySeconds,
            SoundStimulusType.ProjectileImpact,
            _impactIntensity,
            _emitterId,
            direction));
    }
}
