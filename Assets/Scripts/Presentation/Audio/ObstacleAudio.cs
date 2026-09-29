/*
 * Role hierarchy: Presentation > Audio
 * Primary responsibility: Handles ObstacleAudio audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

// ObstacleAudio.cs
using UnityEngine;

[DisallowMultipleComponent]
public class ObstacleAudio : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private ObjectAudioEventChannelSO _objectAudioEventChannel;
    [SerializeField] private HitReceivedEventChannelSO _hitReceivedEventChannel;

    [Header("Audio Output")]
    [SerializeField] private AudioCueEventChannelSO _sfxEventChannel;
    [SerializeField] private AudioConfigurationSO _audioConfig;
    [SerializeField] private GameStateSO _gameState;

    [Header("Obstacle Cues")]
    [Tooltip("Optional authored world-object handoff. Its Hit Cue overrides the legacy cue below.")]
    [SerializeField] private InteractableAudioProfileSO _audioProfile;

    [Tooltip("Used only when Audio Profile has no Hit Cue.")]
    [SerializeField] private AudioCueSO _onHitCue;

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_objectAudioEventChannel != null)
            _objectAudioEventChannel.OnEventRaised += HandleObjectAudioRequested;

        if (_hitReceivedEventChannel != null)
            _hitReceivedEventChannel.OnEventRaised += HandleHitReceived;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_objectAudioEventChannel != null)
            _objectAudioEventChannel.OnEventRaised -= HandleObjectAudioRequested;

        if (_hitReceivedEventChannel != null)
            _hitReceivedEventChannel.OnEventRaised -= HandleHitReceived;
    }

    // Purpose: Plays the requested on hit action or effect.
    public void PlayOnHit()
    {
        PlayAudio(ResolveHitCue(), transform.position);
    }

    // Purpose: Plays the requested on hit at action or effect.
    public void PlayOnHitAt(Vector3 position)
    {
        PlayAudio(ResolveHitCue(), position);
    }

    // Purpose: Handles the incoming object audio requested request or event.
    private void HandleObjectAudioRequested(ObjectAudioRequest request)
    {
        if (request.CueType != ObjectAudioCueType.OnHit)
            return;

        if (!IsTargetForThisObject(request.Target))
            return;

        Vector3 position = request.UsePosition
            ? request.Position
            : transform.position;

        PlayAudio(ResolveHitCue(), position);
    }

    // Purpose: Handles the incoming hit received request or event.
    private void HandleHitReceived(GameObject hitTarget)
    {
        if (!IsTargetForThisObject(hitTarget))
            return;

        PlayAudio(
            ResolveHitCue(),
            hitTarget != null ? hitTarget.transform.position : transform.position);
    }

    // Purpose: Resolves the hit cue needed by this component.
    private AudioCueSO ResolveHitCue()
    {
        return _audioProfile != null && _audioProfile.HitCue != null
            ? _audioProfile.HitCue
            : _onHitCue;
    }

    // Purpose: Checks whether target for this object is currently true.
    private bool IsTargetForThisObject(GameObject target)
    {
        if (target == null)
            return false;

        Transform targetTransform = target.transform;

        return targetTransform == transform ||
               targetTransform.IsChildOf(transform) ||
               transform.IsChildOf(targetTransform);
    }

    // Purpose: Plays the requested audio action or effect.
    private void PlayAudio(AudioCueSO cue, Vector3 position)
    {
        if (cue == null ||
            (_audioConfig == null && cue.DefaultPlaybackConfiguration == null) ||
            _sfxEventChannel == null)
            return;

        if (_gameState != null && _gameState.CurrentGameState == GameState.Cutscene)
            return;

        _sfxEventChannel.RaisePlayEvent(cue, _audioConfig, position);
    }
}
