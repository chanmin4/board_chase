/*
 * Role hierarchy: Presentation > Audio > SoundEmitter
 * Primary responsibility: Handles SoundEmitter audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioGainFilter))]
[RequireComponent(typeof(AudioLowPassFilter))]
public class SoundEmitter : MonoBehaviour
{
    private AudioSource _audioSource;
    private AudioGainFilter _gainFilter;
    private AudioLowPassFilter _lowPassFilter;
    private Coroutine _finishRoutine;
    private Coroutine _fadeRoutine;

    private static AudioListener _cachedListener;

    public event UnityAction<SoundEmitter> OnSoundFinishedPlaying;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _gainFilter = GetComponent<AudioGainFilter>();
        _lowPassFilter = GetComponent<AudioLowPassFilter>();

        // RequireComponent is not retroactive for already-authored prefabs.
        if (_lowPassFilter == null)
            _lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();

        _lowPassFilter.enabled = false;
        _audioSource.playOnAwake = false;
    }

    // Purpose: Plays the requested audio cue action or effect.
    public void PlayAudioCue(AudioCueSO cue, AudioConfigurationSO settings, bool overrideLoop, Vector3 position = default)
    {
        if (cue == null)
            return;

        AudioClip[] clips = cue.GetClips();
        if (clips == null || clips.Length == 0 || clips[0] == null)
            return;

        PlayAudioClip(clips[0], settings, overrideLoop, position, cue.GainDb);
    }

    // Purpose: Plays the requested audio cue action or effect.
    public void PlayAudioCue(AudioCueSO cue, AudioConfigurationSO settings, SoundEmitterPoolSO pool, Vector3 position = default)
    {
        if (cue == null)
            return;

        AudioClip[] clips = cue.GetClips();
        if (clips == null || clips.Length == 0 || clips[0] == null)
            return;

        PlayAudioClip(clips[0], settings, cue.looping, position, cue.GainDb);
    }

    // Purpose: Plays the requested audio clip action or effect.
    public void PlayAudioClip(
        AudioClip clip,
        AudioConfigurationSO settings,
        bool hasToLoop,
        Vector3 position = default,
        float gainDb = 0f)
    {
        if (clip == null)
            return;

        StopInternalRoutines();

        _audioSource.Stop();
        _audioSource.clip = clip;

        transform.position = position;

        if (settings != null)
        {
            settings.ApplyTo(_audioSource);
            settings.ApplyTopDownDirectionAssist(
                _audioSource,
                _lowPassFilter,
                position,
                ResolveListenerTransform());
        }
        else
        {
            _audioSource.outputAudioMixerGroup = null;
            _audioSource.volume = 1f;
            _audioSource.pitch = 1f;
            _audioSource.spatialBlend = 0f;
            _audioSource.panStereo = 0f;
            _audioSource.spread = 0f;

            if (_lowPassFilter != null)
                _lowPassFilter.enabled = false;
        }

        if (_gainFilter != null)
            _gainFilter.SetGainDb(gainDb);

        _audioSource.loop = hasToLoop;
        _audioSource.time = 0f;
        _audioSource.Play();

        if (!hasToLoop)
            _finishRoutine = StartCoroutine(FinishedPlaying(clip.length));
    }

    // Purpose: Performs the fade music in workflow for this component.
    public void FadeMusicIn(AudioClip musicClip, AudioConfigurationSO settings, float duration, float startTime = 0f)
    {
        PlayAudioClip(musicClip, settings, true);

        float targetVolume = settings != null ? settings.Volume : 1f;
        _audioSource.volume = 0f;

        if (_audioSource.clip != null && startTime > 0f && startTime <= _audioSource.clip.length)
            _audioSource.time = startTime;

        if (duration <= 0f)
        {
            _audioSource.volume = targetVolume;
            return;
        }

        _fadeRoutine = StartCoroutine(FadeVolumeRoutine(0f, targetVolume, duration, null));
    }

    // Purpose: Performs the fade music out workflow for this component.
    public float FadeMusicOut(float duration)
    {
        float currentTime = _audioSource.time;

        if (!_audioSource.isPlaying)
            return currentTime;

        if (duration <= 0f)
        {
            NotifyBeingDone();
            return currentTime;
        }

        StopInternalRoutines();
        _fadeRoutine = StartCoroutine(FadeVolumeRoutine(_audioSource.volume, 0f, duration, NotifyBeingDone));
        return currentTime;
    }

    // Purpose: Returns the current clip.
    public AudioClip GetClip() => _audioSource.clip;

    // Purpose: Resumes the paused resume workflow.
    public void Resume() => _audioSource.UnPause();

    // Purpose: Pauses the active pause workflow.
    public void Pause() => _audioSource.Pause();

    // Purpose: Stops the active stop behavior.
    public void Stop()
    {
        StopInternalRoutines();
        _audioSource.Stop();
        _audioSource.clip = null;
    }

    // Purpose: Finishes the active finish workflow.
    public void Finish()
    {
        if (_audioSource.clip == null)
            return;

        if (_audioSource.loop)
        {
            _audioSource.loop = false;
            float timeRemaining = Mathf.Max(0f, _audioSource.clip.length - _audioSource.time);
            StopFinishRoutineOnly();
            _finishRoutine = StartCoroutine(FinishedPlaying(timeRemaining));
        }
    }

    // Purpose: Checks whether playing is currently true.
    public bool IsPlaying() => _audioSource.isPlaying;
    // Purpose: Checks whether looping is currently true.
    public bool IsLooping() => _audioSource.loop;

    // Purpose: Finishes the active finished playing workflow.
    private IEnumerator FinishedPlaying(float clipLength)
    {
        yield return new WaitForSeconds(clipLength);
        NotifyBeingDone();
    }

    // Purpose: Performs the fade volume routine workflow for this component.
    private IEnumerator FadeVolumeRoutine(float from, float to, float duration, System.Action onComplete)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            _audioSource.volume = Mathf.Lerp(from, to, k);
            yield return null;
        }

        _audioSource.volume = to;
        onComplete?.Invoke();
    }

    // Purpose: Publishes the being done notification.
    private void NotifyBeingDone()
    {
        StopInternalRoutines();
        OnSoundFinishedPlaying?.Invoke(this);
    }

    // Purpose: Stops the active internal routines behavior.
    private void StopInternalRoutines()
    {
        StopFinishRoutineOnly();

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }
    }

    // Purpose: Stops the active finish routine only behavior.
    private void StopFinishRoutineOnly()
    {
        if (_finishRoutine != null)
        {
            StopCoroutine(_finishRoutine);
            _finishRoutine = null;
        }
    }

    // Purpose: Resolves the listener transform needed by this component.
    private static Transform ResolveListenerTransform()
    {
        if (_cachedListener == null || !_cachedListener.isActiveAndEnabled)
            _cachedListener = Object.FindFirstObjectByType<AudioListener>();

        return _cachedListener != null ? _cachedListener.transform : null;
    }
}
