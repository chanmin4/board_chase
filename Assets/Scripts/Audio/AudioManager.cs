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

    private const string MasterVolumeParam = "MasterVolume";
    private const string MusicVolumeParam = "MusicVolume";
    private const string SFXVolumeParam = "SFXVolume";

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

        ApplyAllVolumes();
    }

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

    private void OnValidate()
    {
        if (Application.isPlaying)
            ApplyAllVolumes();
    }

    private void ApplyAllVolumes()
    {
        SetGroupVolume(MasterVolumeParam, _masterVolume);
        SetGroupVolume(MusicVolumeParam, _musicVolume);
        SetGroupVolume(SFXVolumeParam, _sfxVolume);
    }

    private void ChangeMasterVolume(float newVolume)
    {
        _masterVolume = newVolume;
        SetGroupVolume(MasterVolumeParam, _masterVolume);
    }

    private void ChangeMusicVolume(float newVolume)
    {
        _musicVolume = newVolume;
        SetGroupVolume(MusicVolumeParam, _musicVolume);
    }

    private void ChangeSFXVolume(float newVolume)
    {
        _sfxVolume = newVolume;
        SetGroupVolume(SFXVolumeParam, _sfxVolume);
    }

    public void SetGroupVolume(string parameterName, float normalizedVolume)
    {
        if (audioMixer == null)
            return;

//        bool volumeSet = audioMixer.SetFloat(parameterName, NormalizedToMixerValue(normalizedVolume));
        //if (!volumeSet)
         //   Debug.LogWarning($"AudioMixer parameter not found: {parameterName}");
    }

    public float GetGroupVolume(string parameterName)
    {
        if (audioMixer == null)
            return 0f;

        if (audioMixer.GetFloat(parameterName, out float rawVolume))
            return MixerValueToNormalized(rawVolume);

        Debug.LogWarning($"AudioMixer parameter not found: {parameterName}");
        return 0f;
    }

    private float MixerValueToNormalized(float mixerValue)
    {
        return 1f + (mixerValue / 80f);
    }

    private float NormalizedToMixerValue(float normalizedValue)
    {
        normalizedValue = Mathf.Clamp01(normalizedValue);
        return (normalizedValue - 1f) * 80f;
    }

    private AudioCueKey PlayMusicTrack(AudioCueSO audioCue, AudioConfigurationSO audioConfiguration, Vector3 positionInSpace)
    {
        if (_pool == null || audioCue == null)
            return AudioCueKey.Invalid;

        AudioClip[] clips = audioCue.GetClips();
        if (clips == null || clips.Length == 0 || clips[0] == null)
            return AudioCueKey.Invalid;

        AudioClip musicClip = clips[0];

        float fadeDuration = audioConfiguration != null ? audioConfiguration.stopFadeTime : 0.25f;
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
        _musicSoundEmitter.FadeMusicIn(musicClip, audioConfiguration, fadeDuration, startTime);

        return AudioCueKey.Invalid;
    }

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

    public void TimelineInterruptsMusic()
    {
        StopMusic(AudioCueKey.Invalid);
    }

    public AudioCueKey PlayAudioCue(AudioCueSO audioCue, AudioConfigurationSO settings, Vector3 position = default)
    {
        if (_pool == null || audioCue == null)
            return AudioCueKey.Invalid;

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
            emitter.PlayAudioClip(clip, settings, audioCue.looping, position);

            emitters.Add(emitter);
            _emitterToKey[emitter] = key;
        }

        if (emitters.Count == 0)
            return AudioCueKey.Invalid;

        _activeEmittersByKey[key] = emitters;
        return key;
    }

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
