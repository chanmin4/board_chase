/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the AudioCueEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(menuName = "Events/AudioCue Event Channel")]
public class AudioCueEventChannelSO : DescriptionBaseSO
{
    public AudioCuePlayAction   OnAudioCuePlayRequested;
    public AudioCueStopAction   OnAudioCueStopRequested;
    public AudioCueFinishAction OnAudioCueFinishRequested;

    // Purpose: Publishes the play event notification.
    public AudioCueKey RaisePlayEvent(AudioCueSO audioCue, AudioConfigurationSO config, Vector3 positionInSpace = default)
    {
        AudioCueKey key = AudioCueKey.Invalid;

        if (OnAudioCuePlayRequested != null)
            key = OnAudioCuePlayRequested.Invoke(audioCue, config, positionInSpace);
        else
            Debug.LogWarning($"AudioCue play requested for {audioCue?.name}, but nobody picked it up.");

        return key;
    }

    // Purpose: Publishes the stop event notification.
    public bool RaiseStopEvent(AudioCueKey key)
    {
        if (OnAudioCueStopRequested != null) return OnAudioCueStopRequested.Invoke(key);
        Debug.LogWarning("AudioCue stop requested, but nobody picked it up.");
        return false;
    }

    // Purpose: Publishes the finish event notification.
    public bool RaiseFinishEvent(AudioCueKey key)
    {
        if (OnAudioCueFinishRequested != null) return OnAudioCueFinishRequested.Invoke(key);
        Debug.LogWarning("AudioCue finish requested, but nobody picked it up.");
        return false;
    }
}

// Purpose: Performs the audio cue play action workflow for this component.
public delegate AudioCueKey AudioCuePlayAction(AudioCueSO cue, AudioConfigurationSO config, Vector3 positionInSpace);
// Purpose: Performs the audio cue stop action workflow for this component.
public delegate bool AudioCueStopAction(AudioCueKey key);
// Purpose: Performs the audio cue finish action workflow for this component.
public delegate bool AudioCueFinishAction(AudioCueKey key);
