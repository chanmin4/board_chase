using UnityEngine;

[DisallowMultipleComponent]
public class ObjectAudio : MonoBehaviour
{
    [Header("Events")]
    [Tooltip("Optional. If assigned, this ObjectAudio plays only requests whose target is this object, its parent, or one of its children.")]
    [SerializeField] private ObjectAudioEventChannelSO _objectAudioEventChannel;

    [Tooltip("Optional. If assigned, this object plays On Hit when this object's Damageable raises the shared hit event.")]
    [SerializeField] private HitReceivedEventChannelSO _hitReceivedEventChannel;

    [Header("Audio Output")]
    [SerializeField] private AudioCueEventChannelSO _sfxEventChannel;
    [SerializeField] private AudioConfigurationSO _audioConfig;
    [SerializeField] private GameStateSO _gameState;

    [Header("Object Cues")]
    [SerializeField] private AudioCueSO _pickupCue;
    [SerializeField] private AudioCueSO _interactCue;
    [SerializeField] private AudioCueSO _onHitCue;

    private void OnEnable()
    {
        if (_objectAudioEventChannel != null)
            _objectAudioEventChannel.OnEventRaised += HandleObjectAudioRequested;

        if (_hitReceivedEventChannel != null)
            _hitReceivedEventChannel.OnEventRaised += HandleHitReceived;
    }

    private void OnDisable()
    {
        if (_objectAudioEventChannel != null)
            _objectAudioEventChannel.OnEventRaised -= HandleObjectAudioRequested;

        if (_hitReceivedEventChannel != null)
            _hitReceivedEventChannel.OnEventRaised -= HandleHitReceived;
    }

    public void PlayPickup()
    {
        Play(ObjectAudioCueType.Pickup, transform.position);
    }

    public void PlayInteract()
    {
        Play(ObjectAudioCueType.Interact, transform.position);
    }

    public void PlayOnHit()
    {
        Play(ObjectAudioCueType.OnHit, transform.position);
    }

    public void Play(ObjectAudioCueType cueType)
    {
        Play(cueType, transform.position);
    }

    public void Play(ObjectAudioCueType cueType, Vector3 position)
    {
        PlayAudio(ResolveCue(cueType), position);
    }

    private void HandleObjectAudioRequested(ObjectAudioRequest request)
    {
        if (!IsTargetForThisObject(request.Target))
            return;

        Play(
            request.CueType,
            request.UsePosition ? request.Position : transform.position);
    }

    private void HandleHitReceived(GameObject hitTarget)
    {
        if (!IsTargetForThisObject(hitTarget))
            return;

        PlayOnHit();
    }

    private bool IsTargetForThisObject(GameObject target)
    {
        if (target == null)
            return false;

        Transform targetTransform = target.transform;

        return targetTransform == transform ||
               targetTransform.IsChildOf(transform) ||
               transform.IsChildOf(targetTransform);
    }

    private AudioCueSO ResolveCue(ObjectAudioCueType cueType)
    {
        return cueType switch
        {
            ObjectAudioCueType.Pickup => _pickupCue,
            ObjectAudioCueType.Interact => _interactCue,
            ObjectAudioCueType.OnHit => _onHitCue,
            _ => null
        };
    }

    private void PlayAudio(AudioCueSO cue, Vector3 position)
    {
        if (!CanPlay(cue))
            return;

        _sfxEventChannel.RaisePlayEvent(cue, _audioConfig, position);
    }

    private bool CanPlay(AudioCueSO cue)
    {
        if (!isActiveAndEnabled)
            return false;

        if (cue == null || _audioConfig == null || _sfxEventChannel == null)
            return false;

        if (_gameState != null && _gameState.CurrentGameState == GameState.Cutscene)
            return false;

        return true;
    }
}
