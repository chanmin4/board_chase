using UnityEngine;

[CreateAssetMenu(menuName = "Events/AudioCue Event Channel")]
public class AudioCueEventChannelSO : DescriptionBaseSO
{
    public AudioCuePlayAction   OnAudioCuePlayRequested;
    public AudioCueStopAction   OnAudioCueStopRequested;
    public AudioCueFinishAction OnAudioCueFinishRequested;

    public AudioCueKey RaisePlayEvent(AudioCueSO audioCue, AudioConfigurationSO config, Vector3 positionInSpace = default)
    {
        AudioCueKey key = AudioCueKey.Invalid;

        if (OnAudioCuePlayRequested != null)
            key = OnAudioCuePlayRequested.Invoke(audioCue, config, positionInSpace);
        else
            Debug.LogWarning($"AudioCue play requested for {audioCue?.name}, but nobody picked it up.");

        return key;
    }

    public bool RaiseStopEvent(AudioCueKey key)
    {
        if (OnAudioCueStopRequested != null) return OnAudioCueStopRequested.Invoke(key);
        Debug.LogWarning("AudioCue stop requested, but nobody picked it up.");
        return false;
    }

    public bool RaiseFinishEvent(AudioCueKey key)
    {
        if (OnAudioCueFinishRequested != null) return OnAudioCueFinishRequested.Invoke(key);
        Debug.LogWarning("AudioCue finish requested, but nobody picked it up.");
        return false;
    }
}

public delegate AudioCueKey AudioCuePlayAction(AudioCueSO cue, AudioConfigurationSO config, Vector3 positionInSpace);
public delegate bool AudioCueStopAction(AudioCueKey key);
public delegate bool AudioCueFinishAction(AudioCueKey key);