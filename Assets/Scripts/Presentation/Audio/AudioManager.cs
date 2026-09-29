/*
 * Role hierarchy: Presentation > Audio
 * Primary responsibility: Handles AudioManager audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;

    [Header("SoundEmitters pool")]
    [SerializeField] private SoundEmitterPoolSO _pool = default;
    [SerializeField] private int _initialSize = 10;

    [Header("Listening on channels")]
    [Tooltip("The SoundManager listens to this event, fired by objects in any scene, to play SFXs")]
    [SerializeField] private AudioCueEventChannelSO _SFXEventChannel = default;
    [Tooltip("The SoundManager listens to this event, fired by objects in any scene, to play Music")]
    [SerializeField] private AudioCueEventChannelSO _musicEventChannel = default;
    [Tooltip("The SoundManager listens to this event, fired by objects in any scene, to change SFXs volume")]
    [SerializeField] private FloatEventChannelSO _SFXVolumeEventChannel = default;
    [Tooltip("The SoundManager listens to this event, fired by objects in any scene, to change Music volume")]
    [SerializeField] private FloatEventChannelSO _musicVolumeEventChannel = default;
    [Tooltip("The SoundManager listens to this event, fired by objects in any scene, to change Master volume")]
    [SerializeField] private FloatEventChannelSO _masterVolumeEventChannel = default;

    [Header("Audio control")]
    [SerializeField] private AudioMixer audioMixer = default;
    [Range(0f, 1f)] [SerializeField] private float _masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float _musicVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 1f;

    private readonly Dictionary<AudioCueKey, List<SoundEmitter>> _activeEmittersByKey = new();
    private readonly Dictionary<SoundEmitter, AudioCueKey> _emitterToKey = new();

    private SoundEmitter _musicSoundEmitter;
    private bool _volumesReady;

    private const string MasterVolumeParam = "MasterVolume";
    private const string MusicVolumeParam = "MusicVolume";
    private const string SFXVolumeParam = "SFXVolume";

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (_pool != null)
        {
            _pool.Prewarm(_initialSize);
            _pool.SetParent(transform);
        }

    }

    // Mixer parameters are safe to set after Awake; settings events may arrive earlier.
    private void Start()
    {
        SaveSystem save = SaveSystem.Active;
        if (save != null && save.HasLoadedSaveData)
        {
            _masterVolume = save.DeviceSettings.masterVolume;
            _musicVolume = save.DeviceSettings.musicVolume;
            _sfxVolume = save.DeviceSettings.sfxVolume;
        }
        _volumesReady = true;
        ApplyAllVolumes();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_SFXEventChannel != null)
        {
            _SFXEventChannel.OnAudioCuePlayRequested += PlayAudioCue;
            _SFXEventChannel.OnAudioCueStopRequested += StopAudioCue;
            _SFXEventChannel.OnAudioCueFinishRequested += FinishAudioCue;
        }

        if (_musicEventChannel != null)
        {
            _musicEventChannel.OnAudioCuePlayRequested += PlayMusicTrack;
            _musicEventChannel.OnAudioCueStopRequested += StopMusic;
        }

        if (_masterVolumeEventChannel != null)
            _masterVolumeEventChannel.OnEventRaised += ChangeMasterVolume;

        if (_musicVolumeEventChannel != null)
            _musicVolumeEventChannel.OnEventRaised += ChangeMusicVolume;

        if (_SFXVolumeEventChannel != null)
            _SFXVolumeEventChannel.OnEventRaised += ChangeSFXVolume;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_SFXEventChannel != null)
        {
            _SFXEventChannel.OnAudioCuePlayRequested -= PlayAudioCue;
            _SFXEventChannel.OnAudioCueStopRequested -= StopAudioCue;
            _SFXEventChannel.OnAudioCueFinishRequested -= FinishAudioCue;
        }

        if (_musicEventChannel != null)
        {
            _musicEventChannel.OnAudioCuePlayRequested -= PlayMusicTrack;
            _musicEventChannel.OnAudioCueStopRequested -= StopMusic;
        }

        if (_masterVolumeEventChannel != null)
            _masterVolumeEventChannel.OnEventRaised -= ChangeMasterVolume;

        if (_musicVolumeEventChannel != null)
            _musicVolumeEventChannel.OnEventRaised -= ChangeMusicVolume;

        if (_SFXVolumeEventChannel != null)
            _SFXVolumeEventChannel.OnEventRaised -= ChangeSFXVolume;
    }

    // Purpose: Normalizes Inspector-authored data after it changes in the Editor.
    private void OnValidate()
    {
        if (Application.isPlaying)
            ApplyAllVolumes();
    }

    // Purpose: Applies the requested all volumes change.
    private void ApplyAllVolumes()
    {
        SetGroupVolume(MasterVolumeParam, _masterVolume);
        SetGroupVolume(MusicVolumeParam, _musicVolume);
        SetGroupVolume(SFXVolumeParam, _sfxVolume);
    }

    // Purpose: Performs the change master volume workflow for this component.
    private void ChangeMasterVolume(float newVolume)
    {
        _masterVolume = newVolume;
        SetGroupVolume(MasterVolumeParam, _masterVolume);
    }

    // Purpose: Performs the change music volume workflow for this component.
    private void ChangeMusicVolume(float newVolume)
    {
        _musicVolume = newVolume;
        SetGroupVolume(MusicVolumeParam, _musicVolume);
    }

    // Purpose: Performs the change sfx volume workflow for this component.
    private void ChangeSFXVolume(float newVolume)
    {
        _sfxVolume = newVolume;
        SetGroupVolume(SFXVolumeParam, _sfxVolume);
    }

    // Purpose: Updates the group volume state.
    public void SetGroupVolume(string parameterName, float normalizedVolume)
    {
        if (audioMixer == null || !_volumesReady)
            return;

        bool volumeSet = audioMixer.SetFloat(
            parameterName,
            NormalizedToMixerValue(normalizedVolume));

        if (!volumeSet)
        {
            Debug.LogWarning(
                $"[AudioManager] AudioMixer exposed parameter is missing. parameter={parameterName}",
                this);
        }
    }

    // Purpose: Returns the current group volume.
    public float GetGroupVolume(string parameterName)
    {
        if (audioMixer == null)
            return 0f;

        if (audioMixer.GetFloat(parameterName, out float rawVolume))
            return MixerValueToNormalized(rawVolume);

        Debug.LogWarning($"AudioMixer parameter not found: {parameterName}");
        return 0f;
    }

    // Purpose: Performs the mixer value to normalized workflow for this component.
    private float MixerValueToNormalized(float mixerValue)
    {
        return mixerValue <= -80f ? 0f : Mathf.Clamp01(Mathf.Pow(10f, mixerValue / 20f));
    }

    // Purpose: Normalizes normalized to mixer value to a safe authored value.
    private float NormalizedToMixerValue(float normalizedValue)
    {
        normalizedValue = Mathf.Clamp01(normalizedValue);
        return normalizedValue <= 0.0001f ? -80f : 20f * Mathf.Log10(normalizedValue);
    }

    // Purpose: Plays the requested music track action or effect.
    private AudioCueKey PlayMusicTrack(AudioCueSO audioCue, AudioConfigurationSO audioConfiguration, Vector3 positionInSpace)
    {
        if (_pool == null || audioCue == null)
            return AudioCueKey.Invalid;

        AudioConfigurationSO resolvedConfiguration = ResolvePlaybackConfiguration(audioCue, audioConfiguration);

        AudioClip[] clips = audioCue.GetClips();
        if (clips == null || clips.Length == 0 || clips[0] == null)
            return AudioCueKey.Invalid;

        AudioClip musicClip = clips[0];

        float fadeDuration = resolvedConfiguration != null ? resolvedConfiguration.stopFadeTime : 0.25f;
        float startTime = 0f;

        if (_musicSoundEmitter != null && _musicSoundEmitter.IsPlaying())
        {
            if (_musicSoundEmitter.GetClip() == musicClip)
                return AudioCueKey.Invalid;

            startTime = _musicSoundEmitter.FadeMusicOut(fadeDuration);
            _musicSoundEmitter.OnSoundFinishedPlaying -= StopMusicEmitter;
            _musicSoundEmitter.OnSoundFinishedPlaying += StopMusicEmitter;
        }

        _musicSoundEmitter = _pool.Request();
        _musicSoundEmitter.OnSoundFinishedPlaying -= StopMusicEmitter;
        _musicSoundEmitter.OnSoundFinishedPlaying += StopMusicEmitter;
        _musicSoundEmitter.transform.position = positionInSpace;
        _musicSoundEmitter.FadeMusicIn(musicClip, resolvedConfiguration, fadeDuration, startTime);

        return AudioCueKey.Invalid;
    }

    // Purpose: Stops the active music behavior.
    private bool StopMusic(AudioCueKey key)
    {
        if (_musicSoundEmitter == null)
            return false;

        if (_musicSoundEmitter.IsPlaying())
        {
            _musicSoundEmitter.OnSoundFinishedPlaying -= StopMusicEmitter;
            _musicSoundEmitter.Stop();
            StopMusicEmitter(_musicSoundEmitter);
            _musicSoundEmitter = null;
            return true;
        }

        return false;
    }

    // Purpose: Performs the timeline interrupts music workflow for this component.
    public void TimelineInterruptsMusic()
    {
        StopMusic(AudioCueKey.Invalid);
    }

    // Purpose: Plays the requested audio cue action or effect.
    public AudioCueKey PlayAudioCue(AudioCueSO audioCue, AudioConfigurationSO settings, Vector3 position = default)
    {
        if (_pool == null || audioCue == null)
            return AudioCueKey.Invalid;

        AudioConfigurationSO resolvedConfiguration = ResolvePlaybackConfiguration(audioCue, settings);

        AudioClip[] clipsToPlay = audioCue.GetClips();
        if (clipsToPlay == null || clipsToPlay.Length == 0)
            return AudioCueKey.Invalid;

        AudioCueKey key = AudioCueKey.Create();
        List<SoundEmitter> emitters = new();

        for (int i = 0; i < clipsToPlay.Length; i++)
        {
            AudioClip clip = clipsToPlay[i];
            if (clip == null)
                continue;

            SoundEmitter emitter = _pool.Request();
            if (emitter == null)
                continue;

            emitter.transform.position = position;
            emitter.OnSoundFinishedPlaying -= OnSoundEmitterFinishedPlaying;
            emitter.OnSoundFinishedPlaying += OnSoundEmitterFinishedPlaying;
            emitter.PlayAudioClip(
                clip,
                resolvedConfiguration,
                audioCue.looping,
                position,
                audioCue.GainDb);

            emitters.Add(emitter);
            _emitterToKey[emitter] = key;
        }

        if (emitters.Count == 0)
            return AudioCueKey.Invalid;

        _activeEmittersByKey[key] = emitters;
        return key;
    }

    // Purpose: Resolves the playback configuration needed by this component.
    private static AudioConfigurationSO ResolvePlaybackConfiguration(
        AudioCueSO audioCue,
        AudioConfigurationSO explicitConfiguration)
    {
        return explicitConfiguration != null
            ? explicitConfiguration
            : audioCue != null ? audioCue.DefaultPlaybackConfiguration : null;
    }

    // Purpose: Finishes the active audio cue workflow.
    public bool FinishAudioCue(AudioCueKey audioCueKey)
    {
        if (!_activeEmittersByKey.TryGetValue(audioCueKey, out List<SoundEmitter> emitters))
            return false;

        for (int i = 0; i < emitters.Count; i++)
        {
            if (emitters[i] != null)
                emitters[i].Finish();
        }

        return true;
    }

    // Purpose: Stops the active audio cue behavior.
    public bool StopAudioCue(AudioCueKey audioCueKey)
    {
        if (!_activeEmittersByKey.TryGetValue(audioCueKey, out List<SoundEmitter> emitters))
            return false;

        for (int i = emitters.Count - 1; i >= 0; i--)
        {
            if (emitters[i] != null)
                StopAndCleanEmitter(emitters[i]);
        }

        _activeEmittersByKey.Remove(audioCueKey);
        return true;
    }

    // Purpose: Responds to the sound emitter finished playing callback.
    private void OnSoundEmitterFinishedPlaying(SoundEmitter soundEmitter)
    {
        if (soundEmitter == null)
            return;

        if (_emitterToKey.TryGetValue(soundEmitter, out AudioCueKey key) &&
            _activeEmittersByKey.TryGetValue(key, out List<SoundEmitter> emitters))
        {
            emitters.Remove(soundEmitter);
            if (emitters.Count == 0)
                _activeEmittersByKey.Remove(key);
        }

        _emitterToKey.Remove(soundEmitter);
        StopAndCleanEmitter(soundEmitter, false);
    }

    // Purpose: Stops the active and clean emitter behavior.
    private void StopAndCleanEmitter(SoundEmitter soundEmitter, bool removeFromMaps = true)
    {
        if (soundEmitter == null)
            return;

        soundEmitter.OnSoundFinishedPlaying -= OnSoundEmitterFinishedPlaying;
        soundEmitter.Stop();

        if (removeFromMaps && _emitterToKey.TryGetValue(soundEmitter, out AudioCueKey key))
        {
            if (_activeEmittersByKey.TryGetValue(key, out List<SoundEmitter> emitters))
            {
                emitters.Remove(soundEmitter);
                if (emitters.Count == 0)
                    _activeEmittersByKey.Remove(key);
            }

            _emitterToKey.Remove(soundEmitter);
        }

        _pool?.Return(soundEmitter);
    }

    // Purpose: Stops the active music emitter behavior.
    private void StopMusicEmitter(SoundEmitter soundEmitter)
    {
        if (soundEmitter == null)
            return;

        soundEmitter.OnSoundFinishedPlaying -= StopMusicEmitter;
        soundEmitter.Stop();
        _pool?.Return(soundEmitter);

        if (_musicSoundEmitter == soundEmitter)
            _musicSoundEmitter = null;
    }
}
