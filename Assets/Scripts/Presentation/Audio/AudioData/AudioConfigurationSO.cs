/*
 * Role hierarchy: Presentation > Audio > AudioData
 * Primary responsibility: Defines the AudioConfigurationSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "AudioConfig", menuName = "Audio/Audio Configuration")]
public class AudioConfigurationSO : DescriptionBaseSO
{
    [Header("Output")]
    public AudioMixerGroup outputMixerGroup;

    [Header("Volume / Pitch")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(-3f, 3f)] public float pitch = 1f;
    public bool randomPitch = false;
    [Range(0.5f, 2f)] public float pitchMin = 0.95f;
    [Range(0.5f, 2f)] public float pitchMax = 1.05f;

    [Header("Spatial")]
    [Tooltip("0 plays as listener-centered 2D audio. 1 plays from the emitted world position as fully 3D audio.")]
    [Range(0f, 1f)] public float spatialBlend = 0f;

    [Tooltip("World distance where 3D attenuation begins.")]
    [Min(0f)] public float minDistance = 1f;

    [Tooltip("World distance where the configured 3D attenuation floor is reached.")]
    [Min(0f)] public float maxDistance = 50f;

    [Tooltip("Speaker-space width of a 3D source. Keep positional one-shots such as footsteps and gunshots at 0 so they read as a precise point.")]
    [Range(0f, 360f)] public float spread = 0f;

    [Tooltip("For 3D audio, use a controlled linear rolloff that never drops below Minimum Volume At Max Distance. Disable this for Unity's logarithmic rolloff.")]
    public bool useDistanceAttenuationFloor = false;

    [Tooltip("Minimum volume multiplier reached at Max Distance. 0.25 means the sound reaches 25% volume at the configured far distance.")]
    [Range(0f, 1f)] public float minimumVolumeAtMaxDistance = 0.7f;

    [Header("Top-down Direction Assist")]
    [Tooltip("Adds a small screen-left/right pan cue and darkens sounds behind the screen-facing listener. This helps distinguish top and bottom without an external HRTF plugin.")]
    public bool useScreenDirectionAssist = false;

    [Tooltip("Additional stereo pan applied from the listener's screen-right axis. Keep this subtle because Unity's 3D panner is still active.")]
    [Range(0f, 0.5f)] public float horizontalPanAssist = 0.18f;

    [Tooltip("Low-pass cutoff used for a sound directly behind the screen-facing listener. Front and side sounds remain unfiltered.")]
    [Range(1000f, 22000f)] public float rearLowPassCutoffHz = 10000f;

    [Header("Fade")]
    [Range(0f, 3f)] public float stopFadeTime = 0.25f;

    [System.NonSerialized] private AnimationCurve _cachedDistanceRolloffCurve;
    [System.NonSerialized] private float _cachedMinimumVolumeAtMaxDistance = -1f;

    public float Volume => volume;
    public float StopFadeTime => stopFadeTime;

    // Purpose: Applies the requested to change.
    public void ApplyTo(AudioSource source)
    {
        if (source == null)
            return;

        source.outputAudioMixerGroup = outputMixerGroup;
        source.volume = volume;
        source.pitch = randomPitch ? Random.Range(pitchMin, pitchMax) : pitch;
        source.spatialBlend = spatialBlend;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.spread = spread;
        source.panStereo = 0f;
        source.playOnAwake = false;
        source.dopplerLevel = 0f;

        if (spatialBlend > 0f && useDistanceAttenuationFloor)
        {
            source.rolloffMode = AudioRolloffMode.Custom;
            source.SetCustomCurve(
                AudioSourceCurveType.CustomRolloff,
                ResolveDistanceRolloffCurve());
        }
        else
        {
            // SoundEmitters are pooled. Explicitly restore the default so a 2D
            // or ordinary 3D cue never inherits a previous custom rolloff.
            source.rolloffMode = AudioRolloffMode.Logarithmic;
        }
    }

    // Purpose: Applies the requested top down direction assist change.
    public void ApplyTopDownDirectionAssist(
        AudioSource source,
        AudioLowPassFilter lowPassFilter,
        Vector3 sourcePosition,
        Transform listener)
    {
        if (source == null || lowPassFilter == null)
            return;

        if (spatialBlend <= 0f || !useScreenDirectionAssist || listener == null)
        {
            source.panStereo = 0f;
            lowPassFilter.enabled = false;
            return;
        }

        Vector3 direction = sourcePosition - listener.position;
        direction.y = 0f;

        Vector3 listenerForward = listener.forward;
        listenerForward.y = 0f;

        Vector3 listenerRight = listener.right;
        listenerRight.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f ||
            listenerForward.sqrMagnitude <= 0.0001f ||
            listenerRight.sqrMagnitude <= 0.0001f)
        {
            source.panStereo = 0f;
            lowPassFilter.enabled = false;
            return;
        }

        direction.Normalize();
        listenerForward.Normalize();
        listenerRight.Normalize();

        source.panStereo = Mathf.Clamp(
            Vector3.Dot(listenerRight, direction) * horizontalPanAssist,
            -0.5f,
            0.5f);

        float rearAmount = Mathf.Clamp01(-Vector3.Dot(listenerForward, direction));
        lowPassFilter.cutoffFrequency = Mathf.Lerp(
            22000f,
            Mathf.Clamp(rearLowPassCutoffHz, 1000f, 22000f),
            rearAmount);
        lowPassFilter.lowpassResonanceQ = 1f;
        lowPassFilter.enabled = rearAmount > 0.001f;
    }

    // Purpose: Resolves the distance rolloff curve needed by this component.
    private AnimationCurve ResolveDistanceRolloffCurve()
    {
        float floor = Mathf.Clamp01(minimumVolumeAtMaxDistance);

        if (_cachedDistanceRolloffCurve == null ||
            !Mathf.Approximately(_cachedMinimumVolumeAtMaxDistance, floor))
        {
            _cachedMinimumVolumeAtMaxDistance = floor;
            _cachedDistanceRolloffCurve = AnimationCurve.Linear(0f, 1f, 1f, floor);
        }

        return _cachedDistanceRolloffCurve;
    }
}
