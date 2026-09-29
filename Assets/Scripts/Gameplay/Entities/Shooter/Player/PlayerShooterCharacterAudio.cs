/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player
 * Primary responsibility: Handles PlayerShooterCharacterAudio audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

// PlayerShooterCharacterAudio.cs
using UnityEngine;

public class PlayerShooterCharacterAudio : EntityShooterCharacterAudio
{
    [Header("Player Refs")]
    [SerializeField] private PlayerCharacter _character;
    [SerializeField] private PlayerAimController _aimAction;
    [SerializeField] private ShooterDashController _dashController;
    [SerializeField] private CharacterController _characterController;

    [Header("Player Events")]
    [SerializeField] private ObjectAudioEventChannelSO _objectAudioEventChannel;
    [SerializeField] private ShootHitConfirmedEventChannelSO _shootHitConfirmedEvent;
    [SerializeField] private PlayerInventoryRuntimeReadyEventChannelSO _inventoryRuntimeReadyChannel;

    [Header("Player Audio Cues")]
    [SerializeField] private AudioCueSO _dashCue;
    [SerializeField] private AudioCueSO _footstepsCue;
    [SerializeField] private AudioCueSO _playerGetHitCue;
    [SerializeField] private AudioCueSO _hitConfirmFallbackCue;
    [SerializeField] private AudioCueSO _aimCue;
    [SerializeField] private AudioCueSO _pickupCue;

    [Header("Player World Action Cues")]
    [SerializeField] private AudioCueSO _genericInteractCue;
    [SerializeField] private AudioCueSO _portalInteractCue;
    [SerializeField] private AudioCueSO _corpseInteractCue;
    [SerializeField] private AudioCueSO _treasurePickupCue;

    [Header("Key Tool Cues")]
    [SerializeField] private AudioCueSO _keyInsertCue;
    [SerializeField] private AudioCueSO _keySuccessCue;
    [SerializeField] private AudioCueSO _keyRejectedCue;

    [Header("Footsteps")]
    [SerializeField] private float _footstepInterval = 0.25f;
    [SerializeField] private float _minMoveSpeedForFootsteps = 0.1f;

    private float _nextFootstepTime;
    private float _lastHealth;
    private PlayerInventoryRuntime _inventoryRuntime;

    private PlayerShooterAudioProfileSO PlayerAudioProfile =>
        AudioProfile as PlayerShooterAudioProfileSO;

    // Purpose: Resolves the refs needed by this component.
    protected override void ResolveRefs()
    {
        base.ResolveRefs();

        if (_character == null)
            _character = GetComponent<PlayerCharacter>();

        if (_aimAction == null)
            _aimAction = EntityComponentResolver.Resolve<PlayerAimController>(this);

        if (_dashController == null)
            _dashController = EntityComponentResolver.Resolve<ShooterDashController>(this);

        if (_characterController == null)
            _characterController = GetComponent<CharacterController>();

        if (_inventoryRuntime == null)
            _inventoryRuntime = GetComponent<PlayerInventoryRuntime>() ?? GetComponentInParent<PlayerInventoryRuntime>();
    }

    // Purpose: Subscribes to additional events notifications.
    protected override void SubscribeAdditionalEvents()
    {
        if (_aimAction != null)
        {
            _aimAction.OnReloadStarted += PlayPlayerReload;
            _aimAction.AimStarted += PlayAim;
        }

        if (_dashController != null)
            _dashController.DashStarted += PlayDash;

        if (_objectAudioEventChannel != null)
            _objectAudioEventChannel.OnEventRaised += HandleObjectAudioRequested;

        if (_shootHitConfirmedEvent != null)
            _shootHitConfirmedEvent.OnEventRaised += PlayHitConfirm;

        if (_inventoryRuntime != null)
            _inventoryRuntime.ItemPickedUp += PlayPickup;

        if (_inventoryRuntimeReadyChannel != null)
        {
            _inventoryRuntimeReadyChannel.OnEventRaised += HandleInventoryRuntimeReady;

            if (_inventoryRuntimeReadyChannel.HasCurrent)
                HandleInventoryRuntimeReady(_inventoryRuntimeReadyChannel.Current);
        }

        if (Damageable != null)
        {
            _lastHealth = Damageable.CurrentHealth;
            Damageable.OnHealthChanged += HandlePlayerHealthChanged;
        }
    }

    // Purpose: Removes the additional events notification subscription.
    protected override void UnsubscribeAdditionalEvents()
    {
        if (_aimAction != null)
        {
            _aimAction.OnReloadStarted -= PlayPlayerReload;
            _aimAction.AimStarted -= PlayAim;
        }

        if (_dashController != null)
            _dashController.DashStarted -= PlayDash;

        if (_objectAudioEventChannel != null)
            _objectAudioEventChannel.OnEventRaised -= HandleObjectAudioRequested;

        if (_shootHitConfirmedEvent != null)
            _shootHitConfirmedEvent.OnEventRaised -= PlayHitConfirm;

        if (_inventoryRuntimeReadyChannel != null)
            _inventoryRuntimeReadyChannel.OnEventRaised -= HandleInventoryRuntimeReady;

        if (_inventoryRuntime != null)
            _inventoryRuntime.ItemPickedUp -= PlayPickup;

        if (Damageable != null)
            Damageable.OnHealthChanged -= HandlePlayerHealthChanged;
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        TickFootsteps();
    }

    // Purpose: Plays the requested dash action or effect.
    public void PlayDash()
    {
        _nextFootstepTime = Time.time + Mathf.Max(0.05f, _footstepInterval);
        AudioCueSO cue = AudioProfile != null && AudioProfile.DashCue != null
            ? AudioProfile.DashCue
            : _dashCue;
        PlayAudio(cue, _audioConfig, transform.position);
    }

    // Purpose: Plays the requested hit confirm action or effect.
    public void PlayHitConfirm()
    {
        RangedWeaponSO weapon = CurrentWeapon as RangedWeaponSO;

        AudioCueSO cue = weapon != null && weapon.HitConfirmAudioCue != null
            ? weapon.HitConfirmAudioCue
            : PlayerAudioProfile != null && PlayerAudioProfile.HitConfirmFallbackCue != null
                ? PlayerAudioProfile.HitConfirmFallbackCue
                : _hitConfirmFallbackCue;

        PlayAudio(cue, _audioConfig, transform.position);
    }

    // Purpose: Plays the requested aim action or effect.
    public void PlayAim()
    {
        AudioCueSO cue = PlayerAudioProfile != null && PlayerAudioProfile.AimCue != null
            ? PlayerAudioProfile.AimCue
            : _aimCue;
        PlayAudio(cue, _audioConfig, transform.position);
    }

    // Purpose: Plays the requested pickup action or effect.
    public void PlayPickup(ItemSO item)
    {
        if (item == null)
            return;

        AudioCueSO cue = PlayerAudioProfile != null && PlayerAudioProfile.PickupCue != null
            ? PlayerAudioProfile.PickupCue
            : _pickupCue;
        PlayAudio(cue, _audioConfig, transform.position);
    }

    // Purpose: Plays the requested generic interact action or effect.
    public void PlayGenericInteract()
    {
        AudioCueSO cue = PlayerAudioProfile != null && PlayerAudioProfile.GenericInteractCue != null
            ? PlayerAudioProfile.GenericInteractCue
            : _genericInteractCue;
        PlayAudio(cue, _audioConfig, transform.position);
    }

    // Purpose: Plays the requested portal interact action or effect.
    public void PlayPortalInteract()
    {
        AudioCueSO cue = PlayerAudioProfile != null && PlayerAudioProfile.PortalInteractCue != null
            ? PlayerAudioProfile.PortalInteractCue
            : _portalInteractCue;
        PlayAudio(cue, _audioConfig, transform.position);
    }

    // Purpose: Plays the requested corpse interact action or effect.
    public void PlayCorpseInteract()
    {
        AudioCueSO cue = PlayerAudioProfile != null && PlayerAudioProfile.CorpseInteractCue != null
            ? PlayerAudioProfile.CorpseInteractCue
            : _corpseInteractCue;
        PlayAudio(cue, _audioConfig, transform.position);
    }

    // Purpose: Plays the requested treasure pickup action or effect.
    public void PlayTreasurePickup()
    {
        AudioCueSO cue = PlayerAudioProfile != null && PlayerAudioProfile.TreasurePickupCue != null
            ? PlayerAudioProfile.TreasurePickupCue
            : _treasurePickupCue;
        PlayAudio(cue, _audioConfig, transform.position);
    }

    // Purpose: Plays the temporary feedback for placing a Key Tool entry into a lock socket.
    public void PlayKeyInsert()
    {
        PlayAudio(_keyInsertCue, _audioConfig, transform.position);
    }

    // Purpose: Plays the confirmed locked-container unlock feedback.
    public void PlayKeySuccess()
    {
        PlayAudio(_keySuccessCue, _audioConfig, transform.position);
    }

    // Purpose: Plays the rejected locked-container key feedback.
    public void PlayKeyRejected()
    {
        PlayAudio(_keyRejectedCue, _audioConfig, transform.position);
    }

    // Purpose: Handles the incoming object audio requested request or event.
    private void HandleObjectAudioRequested(ObjectAudioRequest request)
    {
        AudioCueSO cue = ResolvePlayerWorldActionCue(request.CueType);

        if (cue == null)
            return;

        PlayAudio(cue, _audioConfig, transform.position);
    }

    // Purpose: Resolves the player world action cue needed by this component.
    private AudioCueSO ResolvePlayerWorldActionCue(ObjectAudioCueType cueType)
    {
        return cueType switch
        {
            ObjectAudioCueType.PlayerPortalInteract =>
                PlayerAudioProfile != null && PlayerAudioProfile.PortalInteractCue != null
                    ? PlayerAudioProfile.PortalInteractCue
                    : _portalInteractCue,
            ObjectAudioCueType.PlayerCorpseInteract =>
                PlayerAudioProfile != null && PlayerAudioProfile.CorpseInteractCue != null
                    ? PlayerAudioProfile.CorpseInteractCue
                    : _corpseInteractCue,
            ObjectAudioCueType.PlayerTreasurePickup =>
                PlayerAudioProfile != null && PlayerAudioProfile.TreasurePickupCue != null
                    ? PlayerAudioProfile.TreasurePickupCue
                    : _treasurePickupCue,
            _ => null
        };
    }

    // Purpose: Handles the incoming inventory runtime ready request or event.
    private void HandleInventoryRuntimeReady(PlayerInventoryRuntime inventoryRuntime)
    {
        if (_inventoryRuntime == inventoryRuntime)
            return;

        if (_inventoryRuntime != null)
            _inventoryRuntime.ItemPickedUp -= PlayPickup;

        _inventoryRuntime = inventoryRuntime;

        if (_inventoryRuntime != null)
            _inventoryRuntime.ItemPickedUp += PlayPickup;
    }

    // Purpose: Handles the incoming player health changed request or event.
    private void HandlePlayerHealthChanged(Damageable damageable)
    {
        if (damageable == null)
            return;

        float current = damageable.CurrentHealth;

        if (current < _lastHealth)
        {
            AudioCueSO cue = AudioProfile != null && AudioProfile.HurtCue != null
                ? AudioProfile.HurtCue
                : _playerGetHitCue;
            PlayAudio(cue, _audioConfig, transform.position);
        }

        _lastHealth = current;
    }

    // Purpose: Processes one update step for footsteps.
    private void TickFootsteps()
    {
        AudioCueSO footstepCue = AudioProfile != null && AudioProfile.FootstepCue != null
            ? AudioProfile.FootstepCue
            : _footstepsCue;

        if (footstepCue == null || _characterController == null)
            return;

        if (_dashController != null && _dashController.IsDashing)
            return;

        if (Time.time < _nextFootstepTime)
            return;

        Vector3 velocity = _characterController.velocity;
        velocity.y = 0f;

        if (velocity.magnitude < _minMoveSpeedForFootsteps)
            return;

        _nextFootstepTime = Time.time + Mathf.Max(0.01f, _footstepInterval);
        PlayAudio(footstepCue, _audioConfig, transform.position);
    }
}
