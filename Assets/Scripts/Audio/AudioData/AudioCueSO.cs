using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioCue", menuName = "Audio/Audio Cue")]
public class AudioCueSO : DescriptionBaseSO
{
    public bool looping = false;
    [SerializeField] private AudioClipsGroup[] _audioClipGroups = default;

    public AudioClip[] GetClips()
    {
        if (_audioClipGroups == null || _audioClipGroups.Length == 0)
            return Array.Empty<AudioClip>();

        int n = _audioClipGroups.Length;
        AudioClip[] clips = new AudioClip[n];
        for (int i = 0; i < n; i++)
            clips[i] = _audioClipGroups[i] != null ? _audioClipGroups[i].GetNextClip() : null;

        return clips;
    }
}

[Serializable]
public class AudioClipsGroup
{
    public SequenceMode sequenceMode = SequenceMode.RandomNoImmediateRepeat;
    public AudioClip[] audioClips;

    private int _nextClipToPlay = -1;
    private int _lastClipPlayed = -1;

    public AudioClip GetNextClip()
    {
        if (audioClips == null || audioClips.Length == 0)
            return null;

        if (audioClips.Length == 1)
            return audioClips[0];

        if (_nextClipToPlay == -1)
        {
            _nextClipToPlay = sequenceMode == SequenceMode.Sequential
                ? 0
                : UnityEngine.Random.Range(0, audioClips.Length);
        }
        else
        {
            switch (sequenceMode)
            {
                case SequenceMode.Random:
                    _nextClipToPlay = UnityEngine.Random.Range(0, audioClips.Length);
                    break;
                case SequenceMode.RandomNoImmediateRepeat:
                    do
                    {
                        _nextClipToPlay = UnityEngine.Random.Range(0, audioClips.Length);
                    }
                    while (audioClips.Length > 1 && _nextClipToPlay == _lastClipPlayed);
                    break;
                case SequenceMode.Sequential:
                    _nextClipToPlay = (int)Mathf.Repeat(_nextClipToPlay + 1, audioClips.Length);
                    break;
            }
        }

        _lastClipPlayed = _nextClipToPlay;
        return audioClips[_nextClipToPlay];
    }

    public enum SequenceMode
    {
        Random,
        RandomNoImmediateRepeat,
        Sequential
    }
}
