using UnityEngine;

public class PlayerShooterCharacterAudio : EntityShooterCharacterAudio
{
    [Header("Player Refs")]
    [SerializeField] private VSplatter_Character _character;
    [SerializeField] private PlayerAimAction _aimAction;
    [SerializeField] private VSplatterDashController _dashController;
    [SerializeField] private CharacterController _characterController;

    [Header("Player Events")]
    [SerializeField] private ShootHitConfirmedEventChannelSO _shootHitConfirmedEvent;
    [SerializeField] private PlayerInventoryRuntimeReadyEventChannelSO _inventoryRuntimeReadyChannel;
    [SerializeField] private PlayerCurrencyChangedEventChannelSO _currencyChangedChannel;

    [Header("Player Audio Cues")]
    [SerializeField] private AudioCueSO _dashCue;
    [SerializeField] private AudioCueSO _footstepsCue;
    [SerializeField] private AudioCueSO _playerGetHitCue;
    [SerializeField] private AudioCueSO _hitConfirmFallbackCue;
    [SerializeField] private AudioCueSO _aimCue;
    [SerializeField] private AudioCueSO _pickupCue;
    [SerializeField] private AudioCueSO _currencyPickupCue;

    [Header("Footsteps")]
    [SerializeField] private float _footstepInterval = 0.25f;
    [SerializeField] private float _minMoveSpeedForFootsteps = 0.1f;

    private float _nextFootstepTime;
    private float _lastHealth;
    private PlayerInventoryRuntime _inventoryRuntime;
    private PlayerCurrencySnapshot _lastCurrencySnapshot;
    private bool _hasLastCurrencySnapshot;

    protected override void ResolveRefs()
    {
        base.ResolveRefs();

        if (_character == null)
            _character = GetComponent<VSplatter_Character>();

        if (_aimAction == null)
            _aimAction = GetComponent<PlayerAimAction>();

        if (_dashController == null)
            _dashController = GetComponent<VSplatterDashController>();

        if (_characterController == null)
            _characterController = GetComponent<CharacterController>();

        if (_inventoryRuntime == null)
            _inventoryRuntime = GetComponent<PlayerInventoryRuntime>() ?? GetComponentInParent<PlayerInventoryRuntime>();
    }

    protected override void SubscribeAdditionalEvents()
    {
        if (_aimAction != null)
        {
            _aimAction.OnReloadStarted += PlayPlayerReload;
            _aimAction.AimStarted += PlayAim;
        }

        if (_dashController != null)
            _dashController.DashStarted += PlayDash;

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

        if (_currencyChangedChannel != null)
        {
            _currencyChangedChannel.OnEventRaised += HandleCurrencyChanged;

            if (_currencyChangedChannel.HasCurrent)
                HandleCurrencyChanged(_currencyChangedChannel.Current);
        }

        if (Damageable != null)
        {
            _lastHealth = Damageable.CurrentHealth;
            Damageable.OnHealthChanged += HandlePlayerHealthChanged;
        }
    }

    protected override void UnsubscribeAdditionalEvents()
    {
        if (_aimAction != null)
        {
            _aimAction.OnReloadStarted -= PlayPlayerReload;
            _aimAction.AimStarted -= PlayAim;
        }

        if (_dashController != null)
            _dashController.DashStarted -= PlayDash;

        if (_shootHitConfirmedEvent != null)
            _shootHitConfirmedEvent.OnEventRaised -= PlayHitConfirm;

        if (_inventoryRuntimeReadyChannel != null)
            _inventoryRuntimeReadyChannel.OnEventRaised -= HandleInventoryRuntimeReady;

        if (_inventoryRuntime != null)
            _inventoryRuntime.ItemPickedUp -= PlayPickup;

        if (_currencyChangedChannel != null)
            _currencyChangedChannel.OnEventRaised -= HandleCurrencyChanged;

        if (Damageable != null)
            Damageable.OnHealthChanged -= HandlePlayerHealthChanged;
    }

    private void Update()
    {
        TickFootsteps();
    }

    public void PlayDash()
    {
        _nextFootstepTime = Time.time + Mathf.Max(0.05f, _footstepInterval);
        PlayAudio(_dashCue, _audioConfig, transform.position);
    }

    public void PlayHitConfirm()
    {
        AudioCueSO cue = CurrentWeapon != null && CurrentWeapon.HitConfirmAudioCue != null
            ? CurrentWeapon.HitConfirmAudioCue
            : _hitConfirmFallbackCue;

        PlayAudio(cue, _audioConfig, transform.position);
    }

    public void PlayAim()
    {
        PlayAudio(_aimCue, _audioConfig, transform.position);
    }

    public void PlayPickup(ItemSO item)
    {
        if (item == null)
            return;

        PlayAudio(_pickupCue, _audioConfig, transform.position);
    }

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

    private void HandleCurrencyChanged(PlayerCurrencySnapshot snapshot)
    {
        if (_hasLastCurrencySnapshot)
        {
            bool gained =
                snapshot.runCurrency > _lastCurrencySnapshot.runCurrency ||
                snapshot.roguelikeCurrency > _lastCurrencySnapshot.roguelikeCurrency;

            if (gained)
                PlayAudio(_currencyPickupCue, _audioConfig, transform.position);
        }

        _lastCurrencySnapshot = snapshot;
        _hasLastCurrencySnapshot = true;
    }

    private void HandlePlayerHealthChanged(Damageable damageable)
    {
        if (damageable == null)
            return;

        float current = damageable.CurrentHealth;

        if (current < _lastHealth)
            PlayAudio(_playerGetHitCue, _audioConfig, transform.position);

        _lastHealth = current;
    }

    private void TickFootsteps()
    {
        if (_footstepsCue == null || _characterController == null)
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
        PlayAudio(_footstepsCue, _audioConfig, transform.position);
    }
}
