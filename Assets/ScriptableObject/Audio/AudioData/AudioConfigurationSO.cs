using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "AudioConfig", menuName = "Audio/Audio Configuration")]
public class AudioConfigurationSO : DescriptionBaseSO
{
    [Header("Output")]
    public AudioMixerGroup outputMixerGroup;

    [Header("Volume/Pitch")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(-3f, 3f)] public float pitch = 1f;
    public bool randomPitch = false;
    [Range(0.5f, 2f)] public float pitchMin = 0.95f;
    [Range(0.5f, 2f)] public float pitchMax = 1.05f;

    [Header("3D")]
    [Range(0f, 1f)] public float spatialBlend = 0f; // 0=2D, 1=3D
    public float minDistance = 1f;
    public float maxDistance = 50f;

    [Header("Fade (optional)")]
    [Range(0f, 2f)] public float stopFadeTime = 0f;
}