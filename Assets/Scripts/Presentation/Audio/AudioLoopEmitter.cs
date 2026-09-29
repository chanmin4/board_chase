/*
 * Role hierarchy: Presentation > Audio
 * Primary responsibility: Plays one authored world-space loop through the shared audio service.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// Scene-only component for persistent environmental loops such as generators,
/// shop machinery and ambience anchors. One-shots must use AudioCue requests
/// from gameplay code instead of adding this component to every object.
/// </summary>
[DisallowMultipleComponent]
public class AudioLoopEmitter : MonoBehaviour
{
    [Header("Shared Audio Output")]
    [SerializeField] private AudioCueEventChannelSO _sfxEventChannel;

    [Header("Loop")]
    [SerializeField] private AudioCueSO _loopCue;
    [SerializeField] private AudioConfigurationSO _playbackOverride;
    [SerializeField] private bool _playOnEnable = true;
    [SerializeField] private bool _stopOnDisable = true;

    private AudioCueKey _activeLoopKey = AudioCueKey.Invalid;

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_playOnEnable)
            StartLoop();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_stopOnDisable)
            StopLoop();
    }

    // Purpose: Begins the loop workflow.
    public void StartLoop()
    {
        if (_activeLoopKey.IsValid ||
            _loopCue == null ||
            !_loopCue.looping ||
            _sfxEventChannel == null)
        {
            return;
        }

        _activeLoopKey = _sfxEventChannel.RaisePlayEvent(
            _loopCue,
            _playbackOverride,
            transform.position);
    }

    // Purpose: Stops the active loop behavior.
    public void StopLoop()
    {
        if (!_activeLoopKey.IsValid)
            return;

        _sfxEventChannel?.RaiseStopEvent(_activeLoopKey);
        _activeLoopKey = AudioCueKey.Invalid;
    }
}
