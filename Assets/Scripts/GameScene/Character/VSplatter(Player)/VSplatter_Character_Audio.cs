using UnityEngine;

[DisallowMultipleComponent]
public class VSplatter_Character_Audio : CharacterAudio
{
    [Header("Need Ref - Player Components")]
    [SerializeField] private VSplatter_Character _character;
    [SerializeField] private VSplatterAttack _attack;
    [SerializeField] private VSplatterPaint _paint;
    [SerializeField] private VSplatterDashController _dashController;
    [SerializeField] private Damageable _damageable;
    [SerializeField] private CharacterController _characterController;

    [Header("Need Ref - Audio Cues")]
    [SerializeField] private AudioCueSO _attackCue;
    [SerializeField] private AudioCueSO _paintCue;
    [SerializeField] private AudioCueSO _specialCue;
    [SerializeField] private AudioCueSO _dashCue;
    [SerializeField] private AudioCueSO _footstepsCue;
    [SerializeField] private AudioCueSO _getHitCue;
    [SerializeField] private AudioCueSO _dieCue;
    [SerializeField] private AudioCueSO _shockwaveChargeCue;
    [SerializeField] private AudioCueSO _shockwaveReleaseCue;

    [Header("Footstep")]
    [SerializeField] private bool _playFootstepsWhileMoving = true;
    [SerializeField, Min(0.01f)] private float _footstepInterval = 0.35f;
    [SerializeField, Min(0f)] private float _movementThreshold = 0.12f;
    [SerializeField] private bool _requireGroundedForFootsteps = true;

    [Header("Don't Touch Ref Auto")]
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

        if (_attack == null)
            _attack = GetComponent<VSplatterAttack>();

        if (_paint == null)
            _paint = GetComponent<VSplatterPaint>();

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

        if (_attack != null)
            _attack.Fired += PlayAttack;

        if (_paint != null)
            _paint.Fired += PlayPaint;

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

        if (_attack != null)
            _attack.Fired -= PlayAttack;

        if (_paint != null)
            _paint.Fired -= PlayPaint;

        if (_dashController != null)
            _dashController.DashStarted -= PlayDash;

        if (_damageable != null)
        {
            _damageable.OnHealthChanged -= HandleHealthChanged;
            _damageable.OnDie -= PlayDie;
        }

        _subscribed = false;
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
		Debug.Log("[VSplatterAudio] PlayAttack", this);
		PlayAudio(_attackCue, _audioConfig, transform.position);
	}
	public void PlayPaint()
	{
		Debug.Log("[VSplatterAudio] PlayPaint", this);
		PlayAudio(_paintCue, _audioConfig, transform.position);
	}

    public void PlaySpecial()
    {
        PlayAudio(_specialCue, _audioConfig, transform.position);
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