/*
 * Role hierarchy: Presentation > Audio
 * Primary responsibility: Handles AudioCue audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections;
using UnityEngine;

public class AudioCue : MonoBehaviour
{
    [Header("Sound definition")]
    [SerializeField] private AudioCueSO _audioCue = default;
    [SerializeField] private bool _playOnStart = false;

    [Header("Configuration")]
    [SerializeField] private AudioCueEventChannelSO _audioCueEventChannel = default;
    [SerializeField] private AudioConfigurationSO _audioConfiguration = default;

    private AudioCueKey controlKey = AudioCueKey.Invalid;

    // Purpose: Completes initialization after scene dependencies are ready.
    private void Start()
    {
        if (_playOnStart)
            StartCoroutine(PlayDelayed());
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        _playOnStart = false;
        StopAudioCue();
    }

    // Purpose: Plays the requested delayed action or effect.
    private IEnumerator PlayDelayed()
    {
        yield return new WaitForSeconds(1f);
        if (_playOnStart)
            PlayAudioCue();
    }

    // Purpose: Plays the requested audio cue action or effect.
    public void PlayAudioCue()
    {
        controlKey = _audioCueEventChannel.RaisePlayEvent(_audioCue, _audioConfiguration, transform.position);
    }

    // Purpose: Stops the active audio cue behavior.
    public void StopAudioCue()
    {
        if (controlKey != AudioCueKey.Invalid)
        {
            if (!_audioCueEventChannel.RaiseStopEvent(controlKey))
                controlKey = AudioCueKey.Invalid;
        }
    }

    // Purpose: Finishes the active audio cue workflow.
    public void FinishAudioCue()
    {
        if (controlKey != AudioCueKey.Invalid)
        {
            if (!_audioCueEventChannel.RaiseFinishEvent(controlKey))
                controlKey = AudioCueKey.Invalid;
        }
    }
}
