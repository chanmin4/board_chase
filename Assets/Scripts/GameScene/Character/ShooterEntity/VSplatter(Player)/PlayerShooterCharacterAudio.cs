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

    [Header("Player Audio Cues")]
    [SerializeField] private AudioCueSO _dashCue;
    [SerializeField] private AudioCueSO _footstepsCue;
    [SerializeField] private AudioCueSO _playerGetHitCue;
    [SerializeField] private AudioCueSO _hitConfirmFallbackCue;

    [Header("Footsteps")]
    [SerializeField] private float _footstepInterval = 0.25f;
    [SerializeField] private float _minMoveSpeedForFootsteps = 0.1f;

    private float _nextFootstepTime;
    private float _lastHealth;

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
    }

    protected override void SubscribeAdditionalEvents()
    {
        if (_aimAction != null)
            _aimAction.OnReloadStarted += PlayPlayerReload;

        if (_dashController != null)
            _dashController.DashStarted += PlayDash;

        if (_shootHitConfirmedEvent != null)
            _shootHitConfirmedEvent.OnEventRaised += PlayHitConfirm;

        if (Damageable != null)
        {
            _lastHealth = Damageable.CurrentHealth;
            Damageable.OnHealthChanged += HandlePlayerHealthChanged;
        }
    }

    protected override void UnsubscribeAdditionalEvents()
    {
        if (_aimAction != null)
            _aimAction.OnReloadStarted -= PlayPlayerReload;

        if (_dashController != null)
            _dashController.DashStarted -= PlayDash;

        if (_shootHitConfirmedEvent != null)
            _shootHitConfirmedEvent.OnEventRaised -= PlayHitConfirm;

        if (Damageable != null)
            Damageable.OnHealthChanged -= HandlePlayerHealthChanged;
    }

    private void Update()
    {
        TickFootsteps();
    }

    public void PlayDash()
    {
        PlayAudio(_dashCue, _audioConfig, transform.position);
    }

    public void PlayHitConfirm()
    {
        AudioCueSO cue = CurrentWeapon != null && CurrentWeapon.HitConfirmAudioCue != null
            ? CurrentWeapon.HitConfirmAudioCue
            : _hitConfirmFallbackCue;

        PlayAudio(cue, _audioConfig, transform.position);
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