using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Listen on")]
    [SerializeField] private AudioCueEventChannelSO _audioChannel;

    private class Playing
    {
        public GameObject root;
        public List<AudioSource> sources = new();
        public bool looping;
        public AudioConfigurationSO config;
    }

    private readonly Dictionary<AudioCueKey, Playing> _playing = new();

   private void OnEnable()
    {       
        if (_audioChannel == null) return;
        _audioChannel.OnAudioCuePlayRequested   += HandlePlay;
        _audioChannel.OnAudioCueStopRequested   += HandleStop;
        _audioChannel.OnAudioCueFinishRequested += HandleFinish;
    }

    private void OnDisable()
    {
        if (_audioChannel == null) return;
        _audioChannel.OnAudioCuePlayRequested   -= HandlePlay;
        _audioChannel.OnAudioCueStopRequested   -= HandleStop;
        _audioChannel.OnAudioCueFinishRequested -= HandleFinish;
    }


    private AudioCueKey HandlePlay(AudioCueSO cue, AudioConfigurationSO config, Vector3 position)
    {
        if (cue == null || config == null) return AudioCueKey.Invalid;

        var clips = cue.GetClips();
        if (clips == null || clips.Length == 0) return AudioCueKey.Invalid;

        var key = AudioCueKey.Create();

        var root = new GameObject($"AudioCue_{key.id}_{cue.name}");
        root.transform.SetParent(transform, false);
        root.transform.position = position;

        var playing = new Playing { root = root, looping = cue.looping, config = config };

        float longest = 0f;

        foreach (var clip in clips)
        {
            if (clip == null) continue;

            var src = root.AddComponent<AudioSource>();
            ApplyConfig(src, config);

            src.clip = clip;
            src.loop = cue.looping;
            src.Play();

            playing.sources.Add(src);

            if (!cue.looping)
                longest = Mathf.Max(longest, clip.length);
        }

        _playing[key] = playing;

        if (!cue.looping)
            StartCoroutine(AutoCleanup(key, longest / Mathf.Max(0.01f, playing.sources.Count > 0 ? playing.sources[0].pitch : 1f)));

        return key;
    }

    private void ApplyConfig(AudioSource src, AudioConfigurationSO config)
    {
        src.outputAudioMixerGroup = config.outputMixerGroup;
        src.volume = config.volume;

        float pitch = config.pitch;
        if (config.randomPitch)
            pitch = Random.Range(config.pitchMin, config.pitchMax);
        src.pitch = pitch;

        src.spatialBlend = config.spatialBlend;
        src.minDistance = config.minDistance;
        src.maxDistance = config.maxDistance;
        src.playOnAwake = false;
    }

    private bool HandleStop(AudioCueKey key)
    {
        if (!_playing.TryGetValue(key, out var p)) return false;
        StopAndDestroy(key, p);
        return true;
    }

    private bool HandleFinish(AudioCueKey key)
    {
        if (!_playing.TryGetValue(key, out var p)) return false;

        // 최소 구현: loop면 loop 해제 후 정리, non-loop는 stop과 동일
        foreach (var s in p.sources)
        {
            if (s == null) continue;
            s.loop = false;
        }

        // fade는 나중에(지금은 stopFadeTime 0이면 즉시 정리)
        if (p.looping)
        {
            if (p.config != null && p.config.stopFadeTime > 0f)
                StartCoroutine(FadeOutThenDestroy(key, p, p.config.stopFadeTime));
            else
                StopAndDestroy(key, p);
        }
        else
        {
            StopAndDestroy(key, p);
        }

        return true;
    }

    private IEnumerator FadeOutThenDestroy(AudioCueKey key, Playing p, float t)
    {
        float start = 1f;
        if (p.sources.Count > 0 && p.sources[0] != null) start = p.sources[0].volume;

        float time = 0f;
        while (time < t)
        {
            time += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(time / t);
            foreach (var s in p.sources)
                if (s != null) s.volume = start * k;
            yield return null;
        }

        StopAndDestroy(key, p);
    }

    private IEnumerator AutoCleanup(AudioCueKey key, float delay)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, delay));
        if (_playing.TryGetValue(key, out var p))
            StopAndDestroy(key, p);
    }

    private void StopAndDestroy(AudioCueKey key, Playing p)
    {
        _playing.Remove(key);

        foreach (var s in p.sources)
            if (s != null) s.Stop();

        if (p.root != null)
            Destroy(p.root);
    }
}