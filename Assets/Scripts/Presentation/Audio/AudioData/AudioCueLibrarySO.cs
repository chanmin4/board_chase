/*
 * Role hierarchy: Presentation > Audio > AudioData
 * Primary responsibility: Defines the project-owned Audio Cue Library catalogue.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single authoring catalogue for the project's AudioCueSO assets.
/// It intentionally does not perform runtime lookup or playback: gameplay data
/// references cues directly, while the future editor tool owns catalogue edits,
/// search, preview and validation.
/// </summary>
[CreateAssetMenu(fileName = "AudioCueLibrary", menuName = "Audio/Audio Cue Library")]
public class AudioCueLibrarySO : DescriptionBaseSO
{
    [Header("Cue Catalogue")]
    [Tooltip("Project-approved cues. The Audio Cue Library editor will maintain this list; do not use it as a runtime registry.")]
    [SerializeField] private List<AudioCueSO> _cues = new();

    [Header("Shared Playback Presets")]
    [Tooltip("Optional commonly used AudioConfigurationSO presets for audio authoring. Cue references remain the source of playback defaults.")]
    [SerializeField] private List<AudioConfigurationSO> _sharedPlaybackPresets = new();

    public IReadOnlyList<AudioCueSO> Cues => _cues;
    public IReadOnlyList<AudioConfigurationSO> SharedPlaybackPresets => _sharedPlaybackPresets;

    // Purpose: Performs the contains workflow for this component.
    public bool Contains(AudioCueSO cue)
    {
        return cue != null && _cues != null && _cues.Contains(cue);
    }

    // Purpose: Attempts to find by id and reports whether the request succeeded.
    public bool TryFindById(string cueId, out AudioCueSO cue)
    {
        cue = null;

        if (string.IsNullOrWhiteSpace(cueId) || _cues == null)
            return false;

        for (int i = 0; i < _cues.Count; i++)
        {
            AudioCueSO candidate = _cues[i];

            if (candidate != null && candidate.CueId == cueId)
            {
                cue = candidate;
                return true;
            }
        }

        return false;
    }

    // Purpose: Normalizes Inspector-authored data after it changes in the Editor.
    private void OnValidate()
    {
        if (_cues != null)
            RemoveNullAndDuplicateEntries(_cues);

        if (_sharedPlaybackPresets != null)
            RemoveNullAndDuplicateEntries(_sharedPlaybackPresets);
    }

    private static void RemoveNullAndDuplicateEntries<T>(List<T> entries) where T : Object
    {
        HashSet<T> seen = new();

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            T entry = entries[i];

            if (entry == null || !seen.Add(entry))
                entries.RemoveAt(i);
        }
    }
}
