/*
 * Role hierarchy: Presentation > Audio > AudioData
 * Primary responsibility: Defines the AudioCueSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioCue", menuName = "Audio/Audio Cue")]
public class AudioCueSO : DescriptionBaseSO
{
    [Header("Authoring Metadata")]
    [Tooltip("Stable internal identifier for the Audio Cue Library. Generated automatically for new cues.")]
    [SerializeField, HideInInspector] private string _cueId;

    [Tooltip("Content ownership category used by the Audio Cue Library for filtering and validation.")]
    [SerializeField] private AudioCueCategory _category = AudioCueCategory.Uncategorized;

    [Tooltip("Fine-grained authoring category only. Empty lets the library browser suggest a group from the cue name; playback is unchanged.")]
    [SerializeField] private string _subcategory;

    [Tooltip("Optional handoff note for audio/design. This is authoring data only and has no runtime behaviour.")]
    [SerializeField, TextArea(2, 5)] private string _authoringNotes;

    [Header("Default Playback")]
    [Tooltip("Used whenever the caller does not explicitly override this cue's playback configuration. Existing callers that provide their own configuration remain unchanged.")]
    [SerializeField] private AudioConfigurationSO _defaultPlaybackConfiguration;

    [Header("Clips")]
    public bool looping = false;

    [Tooltip("Additional gain applied before spatial attenuation. Use this to balance source clips with unusually low or high recorded levels.")]
    [SerializeField, Range(-24f, 24f)] private float _gainDb;

    [SerializeField] private AudioClipsGroup[] _audioClipGroups = default;

    public string CueId => _cueId;
    public AudioCueCategory Category => _category;
    public string Subcategory => _subcategory;
    public string AuthoringNotes => _authoringNotes;
    public AudioConfigurationSO DefaultPlaybackConfiguration => _defaultPlaybackConfiguration;
    public float GainDb => _gainDb;

    public int ClipCount
    {
        get
        {
            if (_audioClipGroups == null)
                return 0;

            int count = 0;

            for (int i = 0; i < _audioClipGroups.Length; i++)
            {
                AudioClipsGroup group = _audioClipGroups[i];

                if (group?.audioClips == null)
                    continue;

                for (int j = 0; j < group.audioClips.Length; j++)
                {
                    if (group.audioClips[j] != null)
                        count++;
                }
            }

            return count;
        }
    }

    public bool HasPlayableClip => ClipCount > 0;

    // Purpose: Normalizes Inspector-authored data after it changes in the Editor.
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(_cueId))
            _cueId = System.Guid.NewGuid().ToString("N");
    }

    // Purpose: Returns the current clips.
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

/// <summary>
/// Authoring ownership only. Runtime routing remains defined by the cue's
/// playback configuration and the global AudioManager.
/// </summary>
public enum AudioCueCategory
{
    Uncategorized,
    Ui,
    Music,
    Player,
    Weapon,
    Enemy,
    Creature,
    Interactable,
    World,
    Ambience,
    Voice
}

[Serializable]
public class AudioClipsGroup
{
    public SequenceMode sequenceMode = SequenceMode.RandomNoImmediateRepeat;
    public AudioClip[] audioClips;

    private int _nextClipToPlay = -1;
    private int _lastClipPlayed = -1;

    // Purpose: Returns the current next clip.
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
