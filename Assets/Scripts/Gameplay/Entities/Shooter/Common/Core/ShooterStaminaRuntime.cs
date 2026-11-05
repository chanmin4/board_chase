using UnityEngine;

[System.Serializable]
public sealed class ShooterLocomotionSettings
{
    public bool enabled = true;
    // Compatibility/temporary-debug values only. Authored maximum belongs to the character config.
    [HideInInspector, Min(1f)] public float maximumStamina = 200f;
    [HideInInspector, Min(1f)] public float enemyPoolMultiplier = 2f;
    [Min(0f)] public float walkSpeedMultiplier = 1f;
    [Min(0f)] public float sprintSpeedMultiplier = 1.5f;
    [Min(0f)] public float sprintCostPerSecond = 20f;
    [Min(0f)] public float dashCost = 50f;
    [Min(0f)] public float recoveryPerSecond = 15f;
    [Tooltip("마지막 스태미나 소모 후 회복을 시작하기까지의 시간(초). 달리기로 소모 중이면 계속 갱신됩니다.")]
    [Min(0f)] public float recoveryDelay = 2f;
    [Tooltip("달리기/대시 입력 시작 시의 최소 비율. 이미 승인된 달리기는 키를 놓거나 스태미나가 0이 될 때까지 유지합니다.")]
    [Range(0.01f, 0.9f)] public float exhaustionThreshold = 0.1f;
    [HideInInspector] public bool enemyCanSprint = true;
    [HideInInspector, Min(0f)] public float enemySprintStartDistance = 12f;
    [HideInInspector, Min(0f)] public float enemySprintStopDistance = 8f;
}

[System.Serializable]
public sealed class ShooterAimFeedbackSettings
{
    [HideInInspector, Min(0f)] public float shotKickDistance = 0.35f; // Legacy weapon feedback.
    [Min(0f)] public float hitKickDistance = 0.65f;
    [HideInInspector, Min(0f)] public float maximumKickDistance = 2f;
    [HideInInspector, Min(0f)] public float recoveryPerSecond = 2.5f;
    [Min(0f)] public float maximumHitKickDistance = 2f;
    [Min(0f)] public float hitKickRecoveryPerSecond = 2.5f;
    [Min(0f)] public float enemyHitInaccuracySeconds = 0.35f;
    [Min(0f)] public float enemyHitExtraSpreadDegrees = 8f;
}

/// <summary>Shared player/enemy resource. Pool size changes, spending rules do not.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(-80)]
public sealed class ShooterStaminaRuntime : MonoBehaviour
{
    private static readonly ShooterLocomotionSettings Defaults = new();
    private ShooterStatsRuntime _stats;
    private Damageable _health;
    private ShooterDashController _dash;
    private PlayerAimController _playerAim;
    private float _hitInaccuracyUntil;
    public float HitExtraSpreadDegrees => Time.time < _hitInaccuracyUntil && _stats != null && _stats.StatConfig is ShooterStatConfigSO cfg
        ? Mathf.Max(0f, cfg.AimFeedback.enemyHitExtraSpreadDegrees) : 0f;
    private float _current, _recoverAt;
    private bool _initialized, _sprintRequested, _sprintAuthorized;
    private bool _sprintMovementAllowed = true;
    private ShooterLocomotionSettings _temporarySettings;
    public void ApplyTemporarySettings(ShooterLocomotionSettings settings)
    {
        if (settings != null)
            foreach (float value in new[] { settings.maximumStamina, settings.enemyPoolMultiplier, settings.walkSpeedMultiplier,
                settings.sprintSpeedMultiplier, settings.sprintCostPerSecond, settings.dashCost, settings.recoveryPerSecond,
                settings.recoveryDelay, settings.exhaustionThreshold, settings.enemySprintStartDistance, settings.enemySprintStopDistance })
                if (float.IsNaN(value) || float.IsInfinity(value))
                { Debug.LogWarning("[Stamina] 임시 설정에 유한한 수치만 입력하세요.", this); return; }
        _temporarySettings = settings != null ? JsonUtility.FromJson<ShooterLocomotionSettings>(JsonUtility.ToJson(settings)) : null;
        if (_temporarySettings != null)
        {
            _temporarySettings.maximumStamina = Mathf.Max(1f, _temporarySettings.maximumStamina);
            _temporarySettings.enemyPoolMultiplier = Mathf.Max(1f, _temporarySettings.enemyPoolMultiplier);
            _temporarySettings.sprintCostPerSecond = Mathf.Max(0f, _temporarySettings.sprintCostPerSecond);
            _temporarySettings.dashCost = Mathf.Max(0f, _temporarySettings.dashCost);
            _temporarySettings.recoveryPerSecond = Mathf.Max(0f, _temporarySettings.recoveryPerSecond);
            _temporarySettings.recoveryDelay = Mathf.Max(0f, _temporarySettings.recoveryDelay);
            _temporarySettings.walkSpeedMultiplier = Mathf.Max(0f, _temporarySettings.walkSpeedMultiplier);
            _temporarySettings.sprintSpeedMultiplier = Mathf.Max(0f, _temporarySettings.sprintSpeedMultiplier);
            _temporarySettings.exhaustionThreshold = Mathf.Clamp(_temporarySettings.exhaustionThreshold, .01f, .9f);
            _temporarySettings.enemySprintStartDistance = Mathf.Max(0f, _temporarySettings.enemySprintStartDistance);
            _temporarySettings.enemySprintStopDistance = Mathf.Clamp(_temporarySettings.enemySprintStopDistance, 0f, _temporarySettings.enemySprintStartDistance);
        }
        StopSprint();
    }
    public ShooterLocomotionSettings Settings => _temporarySettings ?? AuthoredSettings;
    private ShooterLocomotionSettings AuthoredSettings =>
        _stats != null && _stats.StatConfig is ShooterStatConfigSO config ? config.Locomotion : Defaults;
    public bool UsesStamina => Settings.enabled;
    public float Maximum => _temporarySettings == null && _stats != null && _stats.StatConfig is ShooterStatConfigSO config
        ? _stats.AdditionalStat(PlayerStatId.MaximumStamina,config.MaximumStamina)
        : Mathf.Max(1f, Settings.maximumStamina *
            (_stats is EnemyShooterStatsRuntime ? Mathf.Max(1f, Settings.enemyPoolMultiplier) : 1f));
    public bool EnemyCanSprint => _temporarySettings?.enemyCanSprint ??
        ((_stats?.StatConfig as ShooterStatConfigSO)?.EnemyCanSprint ?? true);
    public float EnemySprintStartDistance => _temporarySettings?.enemySprintStartDistance ??
        ((_stats?.StatConfig as ShooterStatConfigSO)?.EnemySprintStartDistance ?? 12f);
    public float EnemySprintStopDistance => _temporarySettings?.enemySprintStopDistance ??
        ((_stats?.StatConfig as ShooterStatConfigSO)?.EnemySprintStopDistance ?? 8f);
    public float Current => _current;
    public float Normalized => Mathf.Clamp01(_current / Maximum);
    public bool IsExhausted => UsesStamina && !CanSpend;
    public bool IsSprinting { get; private set; }
    // Start gate only. Dash may consume the remaining pool even when it is below its nominal cost.
    public bool CanSpend => !UsesStamina || !_initialized ||
        (_current > 0f && _current >= Maximum * Mathf.Clamp(Settings.exhaustionThreshold, .01f, .9f));
    public float SpeedMultiplier => !UsesStamina ? 1f :
        Mathf.Max(0f, IsSprinting ? Settings.sprintSpeedMultiplier : Settings.walkSpeedMultiplier);

    public static ShooterStaminaRuntime EnsureOn(Component owner)
    {
        var stats = EntityComponentResolver.Resolve<ShooterStatsRuntime>(owner);
        GameObject root = stats != null ? stats.gameObject : owner.gameObject;
        var runtime = root.GetComponent<ShooterStaminaRuntime>();
        return runtime != null ? runtime : root.AddComponent<ShooterStaminaRuntime>();
    }

    private void Awake()
    {
        _stats = EntityComponentResolver.Resolve<ShooterStatsRuntime>(this);
        _health = EntityComponentResolver.Resolve<Damageable>(this);
        _dash = EntityComponentResolver.Resolve<ShooterDashController>(this);
        _playerAim = EntityComponentResolver.Resolve<PlayerAimController>(this);
    }
    private void OnEnable() { if (_health != null) _health.OnDamaged += OnDamaged; }
    private void OnDisable()
    {
        if (_health != null) _health.OnDamaged -= OnDamaged;
        StopSprint(); _hitInaccuracyUntil = 0f;
    }
    private void OnDamaged(Damageable target, float amount)
    {
        if (amount <= 0f || target.IsPeriodicStatusDamage) return;
        _playerAim?.ApplyHitKick();
        if (_stats != null && _stats.StatConfig is ShooterStatConfigSO cfg)
            _hitInaccuracyUntil = Time.time + Mathf.Max(0f, cfg.AimFeedback.enemyHitInaccuracySeconds);
    }
    public void SetSprintRequested(bool requested, bool movementAllowed = true)
    {
        if (requested && !_sprintRequested) _sprintAuthorized = CanSpend;
        _sprintRequested = requested;
        _sprintMovementAllowed = movementAllowed;
        if (!requested) _sprintAuthorized = false;
        if (!requested || !movementAllowed) IsSprinting = false;
    }
    public void StopSprint() { _sprintRequested = false; _sprintAuthorized = false; IsSprinting = false; }

    private void Update()
    {
        if (!_initialized) { _current = Maximum; _initialized = true; }
        _current = Mathf.Min(_current, Maximum);
        if (GamePause.IsPaused || Time.timeScale <= 0f) { StopSprint(); return; }
        bool blocked = (_health != null && _health.IsDead) ||
            (_stats != null && _stats.StatusEffects != null && _stats.StatusEffects.BlocksActions);
        bool dashing = _dash != null && _dash.IsDashing;
        if (_current <= 0f) _sprintAuthorized = false;
        IsSprinting = UsesStamina && _sprintRequested && _sprintAuthorized && _sprintMovementAllowed && !blocked && !dashing;
        if (IsSprinting)
        {
            _current = Mathf.Max(0f, _current - Mathf.Max(0f, Settings.sprintCostPerSecond) * Time.deltaTime);
            _recoverAt = Time.time + Mathf.Max(0f, Settings.recoveryDelay);
        }
        else if (!blocked && !dashing && Time.time >= _recoverAt)
        {
            float recovery = Mathf.Max(0f, Settings.recoveryPerSecond) *
                Mathf.Max(0f, _stats != null ? _stats.StaminaRecoveryMultiplier : 1f);
            _current = Mathf.Min(Maximum, _current + recovery * Time.deltaTime);
        }
        if (_current <= 0f) { _sprintAuthorized = false; IsSprinting = false; }
    }

    public bool TrySpendDash()
    {
        if (!UsesStamina) return true;
        if (!_initialized) { _current = Maximum; _initialized = true; }
        float cost = Mathf.Max(0f, Settings.dashCost);
        if (!CanSpend) return false;
        _current = Mathf.Max(0f, _current - cost);
        _recoverAt = Time.time + Mathf.Max(0f, Settings.recoveryDelay);
        IsSprinting = false;
        if (_current <= 0f) _sprintAuthorized = false;
        return true;
    }
}
