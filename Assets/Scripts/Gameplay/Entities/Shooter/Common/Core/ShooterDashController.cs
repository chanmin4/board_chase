/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Coordinates the ShooterDashController subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-60)]
public class ShooterDashController : MonoBehaviour
{
    [Header("Dash")]
    [SerializeField] private DashConfigSO _config;

    [Header("Refs")]
    [SerializeField] private ShooterStatsRuntime _statsRuntime;
    [SerializeField] private InvulnerabilityController _invulnerabilityController;
    [SerializeField] private InvulnerabilityConfigSO _dashInvulnerabilityConfig;
    [SerializeField] private ShooterEffectController _effects;

    [Header("Events")]
    [SerializeField] private DashEventChannelSO _dashEventChannel;

    [Header("Cooldown Broadcasting")]
    [SerializeField] private CooldownSnapshotEventChannelSO _cooldownChangedChannel;
    [SerializeField] private VoidEventChannelSO _requestCooldownSnapshotChannel;

    [Header("Visual Lean")]
    [SerializeField] private Transform _leanTarget;

    [Header("Runtime")]
    [ReadOnly] [SerializeField] private bool _isDashing;
    [ReadOnly] [SerializeField] private float _remainingTime;
    [ReadOnly] [SerializeField] private Vector3 _dashDirection;
    [ReadOnly] [SerializeField] private float _cooldownRemaining;

    private float _elapsed;
    private float _nextDashReadyTime;
    private float _lastPublishedRemaining = -1f;
    private bool _lastPublishedDashing;
    private Quaternion _baseLeanLocalRotation;
    private bool _hasBaseLeanLocalRotation;

    public DashConfigSO Config => _config;
    public bool IsDashing => _isDashing;
    public bool IsDashReady => !_isDashing && CooldownRemaining <= 0.0001f;
    public Vector3 DashDirection => _dashDirection;
    public float DashSpeed => ResolveDashSpeed();
    public float CooldownRemaining => Mathf.Max(0f, _nextDashReadyTime - Time.time);
    public float CooldownDuration => ResolveDashCooldown();
    public float Cooldown01 =>
        CooldownDuration > 0f
            ? Mathf.Clamp01(CooldownRemaining / CooldownDuration)
            : 0f;
    public virtual bool RotateTowardDashDirection => _config == null || _config.RotateTowardDashDirection;

    public event Action DashStarted;
    public event Action DashFinished;
    public event Action DashCanceled;
    public event Action<DashEvent> DashEventRaised;

    // Purpose: Initializes Inspector defaults for this component.
    protected virtual void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    protected virtual void Awake()
    {
        ResolveRefs();
        CacheLeanBaseRotation();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    protected virtual void OnEnable()
    {
        ResolveRefs();
        CacheLeanBaseRotation();

        if (_requestCooldownSnapshotChannel != null)
            _requestCooldownSnapshotChannel.OnEventRaised += PublishCooldownSnapshot;

        PublishCooldownSnapshot();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    protected virtual void OnDisable()
    {
        if (_requestCooldownSnapshotChannel != null)
            _requestCooldownSnapshotChannel.OnEventRaised -= PublishCooldownSnapshot;

        CancelDash();
        ResetLeanTarget();
    }

    // Purpose: Processes this component's per-frame runtime work.
    protected virtual void Update()
    {
        TickDashRuntime();
        UpdateCooldownRuntime();
        PublishCooldownSnapshotIfNeeded();
        UpdateVisualLean();
    }

    // Purpose: Attempts to start dash and reports whether the request succeeded.
    public bool TryStartDash(Vector3 requestedDirection)
    {
        if (_isDashing)
            return false;

        if (!CanStartDash())
            return false;

        if (CooldownRemaining > 0.0001f)
            return false;

        requestedDirection.y = 0f;

        if (requestedDirection.sqrMagnitude <= 0.0001f)
        {
            if (!UseForwardWhenNoMovementInput())
                return false;

            requestedDirection = transform.forward;
            requestedDirection.y = 0f;
        }

        if (requestedDirection.sqrMagnitude <= 0.0001f)
            return false;

        _dashDirection = requestedDirection.normalized;
        _isDashing = true;
        _elapsed = 0f;
        _remainingTime = ResolveDashDuration();
        _nextDashReadyTime = Time.time + ResolveDashCooldown();

        _invulnerabilityController?.Begin(_dashInvulnerabilityConfig);
        _effects?.SetFootTrailActive(false);
        _effects?.PlayDashParticles();

        RaiseDashEvent(DashEventType.Started);
        DashStarted?.Invoke();
        PublishCooldownSnapshot();
        return true;
    }

    // Purpose: Checks whether cancel dash is currently permitted.
    public void CancelDash()
    {
        bool wasDashing = _isDashing;
        Vector3 lastDirection = _dashDirection;

        _isDashing = false;
        _elapsed = 0f;
        _remainingTime = 0f;
        _dashDirection = Vector3.zero;

        _invulnerabilityController?.End(_dashInvulnerabilityConfig);
        _effects?.StopDashParticles();

        if (wasDashing)
        {
            RaiseDashEvent(DashEventType.Canceled, lastDirection);
            DashCanceled?.Invoke();
        }

        PublishCooldownSnapshot();
    }

    // Purpose: Builds the dash movement vector from the current authored data.
    public Vector3 BuildDashMovementVector(float currentY)
    {
        if (!_isDashing)
            return new Vector3(0f, currentY, 0f);

        return new Vector3(
            _dashDirection.x * DashSpeed,
            currentY,
            _dashDirection.z * DashSpeed);
    }

    // Purpose: Checks whether start dash is currently permitted.
    protected virtual bool CanStartDash()
    {
        return _statsRuntime == null ||
               _statsRuntime.StatusEffects == null ||
               !_statsRuntime.StatusEffects.BlocksActions;
    }

    // Purpose: Performs the use forward when no movement input workflow for this component.
    protected virtual bool UseForwardWhenNoMovementInput()
    {
        return _config == null || _config.UseForwardWhenNoMovementInput;
    }

    // Purpose: Resolves the dash duration needed by this component.
    protected virtual float ResolveDashDuration()
    {
        return _config != null ? _config.DashDuration : 0.18f;
    }

    // Purpose: Resolves the dash cooldown needed by this component.
    protected virtual float ResolveDashCooldown()
    {
        if (_statsRuntime != null)
            return Mathf.Max(0f, _statsRuntime.DashCooldownSeconds);

        return _config != null ? _config.CooldownSeconds : 0f;
    }

    // Purpose: Resolves the dash speed needed by this component.
    protected virtual float ResolveDashSpeed()
    {
        float fixedSpeed = _config != null ? _config.FixedDashSpeed : 14f;

        if (_statsRuntime == null)
            return fixedSpeed;

        if (_config != null && !_config.UseMoveSpeedBasedDash)
            return fixedSpeed;

        float moveSpeed = Mathf.Max(0.01f, _statsRuntime.MoveSpeed);
        float dashMultiplier = _config != null ? Mathf.Max(0f, _config.DashSpeedMultiplier) : 2.5f;
        float distanceMultiplier = Mathf.Max(0f, _statsRuntime.DashDistanceMultiplier);

        return moveSpeed * dashMultiplier * distanceMultiplier;
    }

    // Purpose: Resolves the refs needed by this component.
    protected virtual void ResolveRefs()
    {
        if (_statsRuntime == null)
            _statsRuntime = EntityComponentResolver.Resolve<ShooterStatsRuntime>(this);

        if (_invulnerabilityController == null)
            _invulnerabilityController = EntityComponentResolver.Resolve<InvulnerabilityController>(this);

        if (_effects == null)
            _effects = EntityComponentResolver.Resolve<ShooterEffectController>(this);

        // Player prefabs explicitly assign the model pivot used for the dash
        // lean. Shooter enemies share the same dash presentation but their
        // base prefab deliberately leaves this field unassigned so visual
        // variants can replace the body. In that case the authored Visual
        // root is the stable, visual-only pivot; gameplay colliders remain on
        // the entity root and are never rotated by the dash presentation.
        if (_leanTarget == null)
            _leanTarget = ResolveVisualLeanTarget();
    }

    // Purpose: Resolves the visual lean target needed by this component.
    private Transform ResolveVisualLeanTarget()
    {
        Transform entityRoot = EntityComponentResolver.ResolveEntityRoot(this);
        if (entityRoot == null)
            return null;

        Transform directVisualRoot = entityRoot.Find("Visual");
        if (directVisualRoot != null)
            return directVisualRoot;

        Transform[] descendants = entityRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < descendants.Length; i++)
        {
            Transform candidate = descendants[i];
            if (candidate != null && candidate.name == "Visual")
                return candidate;
        }

        return null;
    }

    // Purpose: Processes one update step for dash runtime.
    private void TickDashRuntime()
    {
        if (!_isDashing)
            return;

        _elapsed += Time.deltaTime;
        _remainingTime = Mathf.Max(0f, ResolveDashDuration() - _elapsed);

        if (_elapsed >= ResolveDashDuration())
            FinishDash();
    }

    // Purpose: Finishes the active dash workflow.
    private void FinishDash()
    {
        _isDashing = false;
        _elapsed = 0f;
        _remainingTime = 0f;

        _invulnerabilityController?.End(_dashInvulnerabilityConfig);
        _effects?.StopDashParticles();

        RaiseDashEvent(DashEventType.Finished, _dashDirection);
        DashFinished?.Invoke();
        PublishCooldownSnapshot();
    }

    // Purpose: Publishes the dash event notification.
    private void RaiseDashEvent(DashEventType eventType)
    {
        RaiseDashEvent(eventType, _dashDirection);
    }

    // Purpose: Publishes the dash event notification.
    private void RaiseDashEvent(DashEventType eventType, Vector3 direction)
    {
        DashEvent dashEvent = new DashEvent(
            eventType,
            gameObject,
            transform,
            direction,
            DashSpeed,
            ResolveDashDuration());

        _dashEventChannel?.RaiseEvent(dashEvent);
        DashEventRaised?.Invoke(dashEvent);
    }

    // Purpose: Updates the cooldown runtime from the current runtime state.
    private void UpdateCooldownRuntime()
    {
        _cooldownRemaining = CooldownRemaining;
    }

    // Purpose: Publishes the cooldown snapshot notification.
    private void PublishCooldownSnapshot()
    {
        if (_cooldownChangedChannel == null)
            return;

        _cooldownChangedChannel.RaiseEvent(new CooldownSnapshot(
            IsDashReady,
            _isDashing,
            CooldownRemaining,
            ResolveDashCooldown()));
    }

    // Purpose: Publishes the cooldown snapshot if needed notification.
    private void PublishCooldownSnapshotIfNeeded()
    {
        float remaining = CooldownRemaining;

        if (Mathf.Approximately(_lastPublishedRemaining, remaining) &&
            _lastPublishedDashing == _isDashing)
        {
            return;
        }

        _lastPublishedRemaining = remaining;
        _lastPublishedDashing = _isDashing;
        PublishCooldownSnapshot();
    }

    // Purpose: Caches lean base rotation for later reuse.
    private void CacheLeanBaseRotation()
    {
        if (_leanTarget == null || _hasBaseLeanLocalRotation)
            return;

        _baseLeanLocalRotation = _leanTarget.localRotation;
        _hasBaseLeanLocalRotation = true;
    }

    // Purpose: Updates the visual lean from the current runtime state.
    private void UpdateVisualLean()
    {
        if (_leanTarget == null || !_hasBaseLeanLocalRotation)
            return;

        bool useLean = _config == null || _config.UseDashVisualLean;
        bool isDashing = useLean && _isDashing;

        Vector3 leanEuler = _config != null
            ? _config.DashVisualLeanEuler
            : new Vector3(18f, 0f, 0f);

        float speed = isDashing
            ? _config != null ? _config.DashVisualLeanInSpeed : 18f
            : _config != null ? _config.DashVisualLeanOutSpeed : 12f;

        Quaternion targetRotation = isDashing
            ? _baseLeanLocalRotation * Quaternion.Euler(leanEuler)
            : _baseLeanLocalRotation;

        if (speed <= 0.001f)
        {
            _leanTarget.localRotation = targetRotation;
            return;
        }

        float t = 1f - Mathf.Exp(-speed * Time.deltaTime);
        _leanTarget.localRotation = Quaternion.Slerp(
            _leanTarget.localRotation,
            targetRotation,
            t);
    }

    // Purpose: Performs the reset lean target workflow for this component.
    private void ResetLeanTarget()
    {
        if (_leanTarget != null && _hasBaseLeanLocalRotation)
            _leanTarget.localRotation = _baseLeanLocalRotation;
    }
}
