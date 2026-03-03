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

    private void Start()
    {
        if (_playOnStart)
            StartCoroutine(PlayDelayed());
    }

    private void OnDisable()
    {
        _playOnStart = false;
        StopAudioCue();
    }

    private IEnumerator PlayDelayed()
    {
        yield return new WaitForSeconds(1f);
        if (_playOnStart)
            PlayAudioCue();
    }

    public void PlayAudioCue()
    {
        controlKey = _audioCueEventChannel.RaisePlayEvent(_audioCue, _audioConfiguration, transform.position);
    }

    public void StopAudioCue()
    {
        if (controlKey != AudioCueKey.Invalid)
        {
            if (!_audioCueEventChannel.RaiseStopEvent(controlKey))
                controlKey = AudioCueKey.Invalid;
        }
    }

    public void FinishAudioCue()
    {
        if (controlKey != AudioCueKey.Invalid)
        {
            if (!_audioCueEventChannel.RaiseFinishEvent(controlKey))
                controlKey = AudioCueKey.Invalid;
        }
    }
}