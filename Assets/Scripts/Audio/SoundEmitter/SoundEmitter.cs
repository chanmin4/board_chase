using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class SoundEmitter : MonoBehaviour
{
    private AudioSource _audioSource;
    private Coroutine _finishRoutine;
    private Coroutine _fadeRoutine;

    public event UnityAction<SoundEmitter> OnSoundFinishedPlaying;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
    }

    public void PlayAudioCue(AudioCueSO cue, AudioConfigurationSO settings, bool overrideLoop, Vector3 position = default)
    {
        if (cue == null)
            return;

        AudioClip[] clips = cue.GetClips();
        if (clips == null || clips.Length == 0 || clips[0] == null)
            return;

        PlayAudioClip(clips[0], settings, overrideLoop, position);
    }

    public void PlayAudioCue(AudioCueSO cue, AudioConfigurationSO settings, SoundEmitterPoolSO pool, Vector3 position = default)
    {
        if (cue == null)
            return;

        AudioClip[] clips = cue.GetClips();
        if (clips == null || clips.Length == 0 || clips[0] == null)
            return;

        PlayAudioClip(clips[0], settings, cue.looping, position);
    }

    public void PlayAudioClip(AudioClip clip, AudioConfigurationSO settings, bool hasToLoop, Vector3 position = default)
    {
        if (clip == null)
            return;

        StopInternalRoutines();

        _audioSource.Stop();
        _audioSource.clip = clip;

        if (settings != null)
            settings.ApplyTo(_audioSource);
        else
        {
            _audioSource.outputAudioMixerGroup = null;
            _audioSource.volume = 1f;
            _audioSource.pitch = 1f;
            _audioSource.spatialBlend = 0f;
        }

        transform.position = position;
        _audioSource.loop = hasToLoop;
        _audioSource.time = 0f;
        _audioSource.Play();

        if (!hasToLoop)
            _finishRoutine = StartCoroutine(FinishedPlaying(clip.length));
    }

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

    public AudioClip GetClip() => _audioSource.clip;

    public void Resume() => _audioSource.UnPause();

    public void Pause() => _audioSource.Pause();

    public void Stop()
    {
        StopInternalRoutines();
        _audioSource.Stop();
        _audioSource.clip = null;
    }

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

    public bool IsPlaying() => _audioSource.isPlaying;
    public bool IsLooping() => _audioSource.loop;

    private IEnumerator FinishedPlaying(float clipLength)
    {
        yield return new WaitForSeconds(clipLength);
        NotifyBeingDone();
    }

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

    private void NotifyBeingDone()
    {
        StopInternalRoutines();
        OnSoundFinishedPlaying?.Invoke(this);
    }

    private void StopInternalRoutines()
    {
        StopFinishRoutineOnly();

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }
    }

    private void StopFinishRoutineOnly()
    {
        if (_finishRoutine != null)
        {
            StopCoroutine(_finishRoutine);
            _finishRoutine = null;
        }
    }
}
