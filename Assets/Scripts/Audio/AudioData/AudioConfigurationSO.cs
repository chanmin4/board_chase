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
    [Range(0f, 1f)] public float spatialBlend = 0f;
    [Min(0f)] public float minDistance = 1f;
    [Min(0f)] public float maxDistance = 50f;

    [Header("Fade")]
    [Range(0f, 3f)] public float stopFadeTime = 0.25f;

    public float Volume => volume;
    public float StopFadeTime => stopFadeTime;

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
        source.playOnAwake = false;
        source.dopplerLevel = 0f;
    }
}
