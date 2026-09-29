/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > Common
 * Primary responsibility: Handles EnemyShooterCharacterAudio audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.AI;

public class EnemyShooterCharacterAudio : EntityShooterCharacterAudio
{
    [Header("Enemy Shooter")]
    [SerializeField] private EnemyShooterShoot _enemyShoot;
    [SerializeField] private EnemyShooterCombatController _combatController;
    [SerializeField] private ShooterDashController _dashController;
    [SerializeField] private ShooterStatsRuntime _statsRuntime;
    [SerializeField] private NavMeshAgent _navMeshAgent;

    [Header("Enemy Audio Cues")]
    [Tooltip("Played at the enemy world position when a dash starts.")]
    [SerializeField] private AudioCueSO _dashCue;

    [Tooltip("Played repeatedly while the enemy NavMeshAgent is moving.")]
    [SerializeField] private AudioCueSO _footstepsCue;

    [Tooltip("Optional long-range 3D configuration used only for enemy footsteps.")]
    [SerializeField] private AudioConfigurationSO _footstepAudioConfig;

    [Tooltip("Played at the enemy world position when its health decreases.")]
    [SerializeField] private AudioCueSO _enemyGetHitCue;

    [Tooltip("Temporary low-pitch cue played once when this shooter enters Alert. Replace this with authored enemy voice/audio later.")]
    [SerializeField] private AudioCueSO _alertCue;

    [Tooltip("Temporary high-pitch cue played once when this shooter enters a confirmed combat goal. ShooterAudioProfileSO.SpecialCue overrides this for authored elite/boss reactions.")]
    [SerializeField] private AudioCueSO _playerDiscoveryCue;

    [Header("Footsteps")]
    [Tooltip("Minimum planar NavMeshAgent speed required before footsteps play.")]
    [SerializeField, Min(0f)] private float _minMoveSpeedForFootsteps = 0.1f;

    [Header("Options")]
    [SerializeField] private bool _playEnemyReloadAudio = false;

    private float _nextFootstepTime;
    private float _engagementCueNotBeforeTime;
    private float _lastHealth;
    private bool _wasAlerted;
    private bool _wasEngaged;

    // Purpose: Resolves the refs needed by this component.
    protected override void ResolveRefs()
    {
        base.ResolveRefs();

        if (_enemyShoot == null)
            _enemyShoot = EntityComponentResolver.Resolve<EnemyShooterShoot>(this);

        if (_combatController == null)
            _combatController = EntityComponentResolver.Resolve<EnemyShooterCombatController>(this);

        if (_dashController == null)
            _dashController = EntityComponentResolver.Resolve<ShooterDashController>(this);

        if (_statsRuntime == null)
            _statsRuntime = EntityComponentResolver.Resolve<ShooterStatsRuntime>(this);

        if (_navMeshAgent == null)
            _navMeshAgent = EntityComponentResolver.Resolve<NavMeshAgent>(this);
    }

    // Purpose: Subscribes to additional events notifications.
    protected override void SubscribeAdditionalEvents()
    {
        if (_playEnemyReloadAudio && _enemyShoot != null)
            _enemyShoot.ReloadStarted += PlayEnemyReload;

        if (_dashController != null)
            _dashController.DashStarted += PlayDash;

        if (Damageable != null)
        {
            _lastHealth = Damageable.CurrentHealth;
            Damageable.OnHealthChanged += HandleHealthChanged;
        }

        _wasAlerted = _combatController != null && _combatController.IsAlerted;
        _wasEngaged = _combatController != null && _combatController.IsEngaged;
        _engagementCueNotBeforeTime = 0f;
    }

    // Purpose: Removes the additional events notification subscription.
    protected override void UnsubscribeAdditionalEvents()
    {
        if (_enemyShoot != null)
            _enemyShoot.ReloadStarted -= PlayEnemyReload;

        if (_dashController != null)
            _dashController.DashStarted -= PlayDash;

        if (Damageable != null)
            Damageable.OnHealthChanged -= HandleHealthChanged;
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        TickFootsteps();
        TickAlertCue();
    }

    // Purpose: Plays one low cue for Alert and one high cue for Combat entry.
    // A short gap keeps direct visual discovery from layering both temporary beeps in the same frame.
    private void TickAlertCue()
    {
        bool isAlerted = _combatController != null && _combatController.IsAlerted;
        bool isEngaged = _combatController != null && _combatController.IsEngaged;

        if (isAlerted && !_wasAlerted)
        {
            PlayAudio(_alertCue, null, transform.position);
            _engagementCueNotBeforeTime = Time.time + 0.12f;
        }

        if (isEngaged && !_wasEngaged && Time.time >= _engagementCueNotBeforeTime)
        {
            AudioCueSO cue = AudioProfile != null && AudioProfile.SpecialCue != null
                ? AudioProfile.SpecialCue
                : _playerDiscoveryCue;
            PlayAudio(cue, null, transform.position);
        }

        _wasAlerted = isAlerted;
        _wasEngaged = isEngaged && Time.time >= _engagementCueNotBeforeTime;
    }

    // Purpose: Plays the requested dash action or effect.
    public void PlayDash()
    {
        _nextFootstepTime = Time.time + ResolveFootstepInterval();
        AudioCueSO cue = AudioProfile != null && AudioProfile.DashCue != null
            ? AudioProfile.DashCue
            : _dashCue;
        PlayAudio(cue, _audioConfig, transform.position);
    }

    // Purpose: Handles the incoming health changed request or event.
    private void HandleHealthChanged(Damageable damageable)
    {
        if (damageable == null)
            return;

        float currentHealth = damageable.CurrentHealth;

        if (currentHealth < _lastHealth)
        {
            AudioCueSO cue = AudioProfile != null && AudioProfile.HurtCue != null
                ? AudioProfile.HurtCue
                : _enemyGetHitCue;
            PlayAudio(cue, _audioConfig, transform.position);
        }

        _lastHealth = currentHealth;
    }

    // Purpose: Processes one update step for footsteps.
    private void TickFootsteps()
    {
        AudioCueSO footstepCue = AudioProfile != null && AudioProfile.FootstepCue != null
            ? AudioProfile.FootstepCue
            : _footstepsCue;

        if (footstepCue == null ||
            _navMeshAgent == null ||
            !_navMeshAgent.isActiveAndEnabled ||
            (Damageable != null && Damageable.IsDead))
        {
            return;
        }

        if (_dashController != null && _dashController.IsDashing)
            return;

        if (Time.time < _nextFootstepTime)
            return;

        Vector3 velocity = _navMeshAgent.velocity;
        velocity.y = 0f;

        if (velocity.magnitude < _minMoveSpeedForFootsteps)
            return;

        _nextFootstepTime = Time.time + ResolveFootstepInterval();
        PlayAudio(
            footstepCue,
            _footstepAudioConfig != null ? _footstepAudioConfig : _audioConfig,
            transform.position);
    }

    // Purpose: Resolves the footstep interval needed by this component.
    private float ResolveFootstepInterval()
    {
        return _statsRuntime != null
            ? Mathf.Max(0.05f, _statsRuntime.FootstepSoundInterval)
            : 0.35f;
    }
}
