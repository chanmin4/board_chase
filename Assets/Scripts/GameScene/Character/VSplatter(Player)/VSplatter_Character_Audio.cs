using UnityEngine;

[DisallowMultipleComponent]
public class VSplatter_Character_Audio : CharacterAudio
{
    [Header("Refs")]
    [SerializeField] private VSplatter_Character _character;
    [SerializeField] private VSplatterShoot _shoot;
    [SerializeField] private VSplatterDashController _dashController;
    [SerializeField] private Damageable _damageable;
    [SerializeField] private CharacterController _characterController;

    [Header("Audio Cues")]
    [SerializeField] private AudioCueSO _shootCue;
    [SerializeField] private AudioCueSO _attackCue;
    [SerializeField] private AudioCueSO _paintCue;
    [SerializeField] private AudioCueSO _specialCue;
    [SerializeField] private AudioCueSO _dashCue;
    [SerializeField] private AudioCueSO _footstepsCue;
    [SerializeField] private AudioCueSO _getHitCue;
    [SerializeField] private AudioCueSO _dieCue;
    [SerializeField] private AudioCueSO _shockwaveChargeCue;
    [SerializeField] private AudioCueSO _shockwaveReleaseCue;

    [Header("Footsteps")]
    [SerializeField] private bool _playFootstepsWhileMoving = true;
    [SerializeField, Min(0.01f)] private float _footstepInterval = 0.35f;
    [SerializeField, Min(0f)] private float _movementThreshold = 0.12f;
    [SerializeField] private bool _requireGroundedForFootsteps = true;

    [Header("Debug")]
    [SerializeField] private float _debugLastHealth;
    [SerializeField] private float _debugFootstepTimer;

    private bool _subscribed;

    private void Reset()
    {
        ResolveRefs();
    }

    private void Awake()
    {
        ResolveRefs();

        if (_damageable != null)
            _debugLastHealth = _damageable.CurrentHealth;
    }

    private void OnEnable()
    {
        ResolveRefs();
        Subscribe();

        if (_damageable != null)
            _debugLastHealth = _damageable.CurrentHealth;
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        TickFootsteps();
    }

    private void ResolveRefs()
    {
        if (_character == null)
            _character = GetComponent<VSplatter_Character>();

        if (_shoot == null)
            _shoot = GetComponent<VSplatterShoot>();

        if (_dashController == null)
            _dashController = GetComponent<VSplatterDashController>();

        if (_damageable == null)
            _damageable = GetComponent<Damageable>();

        if (_characterController == null)
            _characterController = GetComponent<CharacterController>();
    }

    private void Subscribe()
    {
        if (_subscribed)
            return;

        if (_shoot != null)
            _shoot.Fired += HandleShotFired;

        if (_dashController != null)
            _dashController.DashStarted += PlayDash;

        if (_damageable != null)
        {
            _damageable.OnHealthChanged += HandleHealthChanged;
            _damageable.OnDie += PlayDie;
        }

        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
            return;

        if (_shoot != null)
            _shoot.Fired -= HandleShotFired;

        if (_dashController != null)
            _dashController.DashStarted -= PlayDash;

        if (_damageable != null)
        {
            _damageable.OnHealthChanged -= HandleHealthChanged;
            _damageable.OnDie -= PlayDie;
        }

        _subscribed = false;
    }

    private void HandleShotFired(BulletAmmoType ammoType)
    {
        switch (ammoType)
        {
            case BulletAmmoType.AttackAndPaint:
            case BulletAmmoType.Attack:
                PlayAttack();
                break;

            case BulletAmmoType.Paint:
                PlayPaint();
                break;

            case BulletAmmoType.Special:
                PlaySpecial();
                break;

            default:
                PlayShoot();
                break;
        }
    }

    private void HandleHealthChanged(Damageable damageable)
    {
        if (damageable == null)
            return;

        float currentHealth = damageable.CurrentHealth;
        bool healthDecreased = currentHealth < _debugLastHealth;

        _debugLastHealth = currentHealth;

        if (!healthDecreased)
            return;

        if (damageable.IsDead)
            return;

        PlayGetHit();
    }

    private void TickFootsteps()
    {
        if (!_playFootstepsWhileMoving)
            return;

        if (_character == null)
            return;

        if (_footstepsCue == null)
            return;

        if (_dashController != null && _dashController.IsDashing)
            return;

        if (_requireGroundedForFootsteps &&
            _characterController != null &&
            !_characterController.isGrounded)
        {
            _debugFootstepTimer = 0f;
            return;
        }

        float movementAmount = _character.movementInput.magnitude;

        if (movementAmount < _movementThreshold)
        {
            _debugFootstepTimer = 0f;
            return;
        }

        _debugFootstepTimer += Time.deltaTime;

        float interval = Mathf.Max(0.01f, _footstepInterval);

        if (_debugFootstepTimer < interval)
            return;

        _debugFootstepTimer = 0f;
        PlayFootstep();
    }

    public void PlayAttack()
    {
        AudioCueSO cue = _shootCue != null ? _shootCue : _attackCue;
        PlayAudio(cue, _audioConfig, transform.position);
    }

    public void PlayPaint()
    {
        AudioCueSO cue = _shootCue != null ? _shootCue : _paintCue;
        PlayAudio(cue, _audioConfig, transform.position);
    }

    public void PlayShoot()
    {
        AudioCueSO cue = _shootCue != null ? _shootCue : _attackCue;
        PlayAudio(cue, _audioConfig, transform.position);
    }

    public void PlaySpecial()
    {
        AudioCueSO cue = _specialCue != null ? _specialCue : _shootCue;
        PlayAudio(cue, _audioConfig, transform.position);
    }

    public void PlayDash()
    {
        PlayAudio(_dashCue, _audioConfig, transform.position);
    }

    public void PlayFootstep()
    {
        Vector3 position = _character != null && _character.Feet != null
            ? _character.Feet.position
            : transform.position;

        PlayAudio(_footstepsCue, _audioConfig, position);
    }

    public void PlayGetHit()
    {
        PlayAudio(_getHitCue, _audioConfig, transform.position);
    }

    public void PlayDie()
    {
        PlayAudio(_dieCue, _audioConfig, transform.position);
    }

    public void PlayShockwaveCharge()
    {
        PlayAudio(_shockwaveChargeCue, _audioConfig, transform.position);
    }

    public void PlayShockwaveRelease()
    {
        PlayAudio(_shockwaveReleaseCue, _audioConfig, transform.position);
    }
}