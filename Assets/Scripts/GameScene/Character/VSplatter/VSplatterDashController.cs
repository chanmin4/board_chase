using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(VSplatter_Character))]
[DefaultExecutionOrder(-60)]
public class VSplatterDashController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VSplatter_Character _character;
    [SerializeField] private VSplatterDashConfigSO _config;
    [SerializeField] private VSplatterDashEventChannelSO _dashEventChannel;

    [Header("Broadcasting")]
    [SerializeField] private CooldownSnapshotEventChannelSO _cooldownChangedChannel;

    [Header("Listening To")]
    [SerializeField] private VoidEventChannelSO _requestCooldownSnapshotChannel;

    [Header("Invulnerability")]
    [SerializeField] private InvulnerabilityController _invulnerabilityController;
    [SerializeField] private InvulnerabilityConfigSO _dashInvulnerabilityConfig;

    [Header("Debug")]
    [ReadOnly] [SerializeField] private bool _isDashing;
    [ReadOnly] [SerializeField] private float _remainingTime;
    [ReadOnly] [SerializeField] private Vector3 _dashDirection;
    [ReadOnly] [SerializeField] private float _cooldownRemaining;

    private float _elapsed;
    private bool _suppressUntilInputRelease;
    private float _nextDashReadyTime;
    private float _lastPublishedRemaining = -1f;
    private bool _lastPublishedDashing;

    public bool IsDashing => _isDashing;
    public bool IsDashReady => !_isDashing && CooldownRemaining <= 0.0001f;
    public Vector3 DashDirection => _dashDirection;
    public float DashSpeed => _config != null ? _config.DashSpeed : 14f;
    public float CooldownRemaining => Mathf.Max(0f, _nextDashReadyTime - Time.time);
    public float CooldownDuration => _config != null ? _config.CooldownSeconds : 0f;
    public float Cooldown01 =>
        CooldownDuration > 0f
            ? Mathf.Clamp01(CooldownRemaining / CooldownDuration)
            : 0f;

    public bool RotateTowardDashDirection => _config == null || _config.RotateTowardDashDirection;
    public VSplatterDashEventChannelSO DashEventChannel => _dashEventChannel;

    public event Action DashStarted;
    public event Action DashFinished;
    public event Action DashCanceled;
    public event Action<VSplatterDashEvent> DashEventRaised;

    private void Awake()
    {
        if (_character == null)
            TryGetComponent(out _character);
        if (_invulnerabilityController == null)
            TryGetComponent(out _invulnerabilityController);
    }

    private void OnEnable()
    {
        if (_requestCooldownSnapshotChannel != null)
            _requestCooldownSnapshotChannel.OnEventRaised += PublishCooldownSnapshot;

        PublishCooldownSnapshot();
    }

    private void OnDisable()
    {
        if (_requestCooldownSnapshotChannel != null)
            _requestCooldownSnapshotChannel.OnEventRaised -= PublishCooldownSnapshot;

        CancelDash();
    }

    private void Update()
    {
        UpdateCooldownRuntime();
        PublishCooldownSnapshotIfNeeded();

        if (_character == null)
            return;

        if (_suppressUntilInputRelease)
        {
            if (!_character.DashInput)
                _suppressUntilInputRelease = false;
        }
        else if (!_isDashing && _character.DashInput)
        {
            TryStartDash();
        }

        if (!_isDashing)
            return;

        _elapsed += Time.deltaTime;
        _remainingTime = Mathf.Max(0f, ResolveDashDuration() - _elapsed);

        if (_elapsed >= ResolveDashDuration())
            FinishDash();
    }

    public bool TryStartDash()
    {
        if (_isDashing || _character == null)
            return false;

        if (CooldownRemaining > 0.0001f)
            return false;

        Vector3 requestedDirection = _character.movementInput;
        requestedDirection.y = 0f;

        if (requestedDirection.sqrMagnitude <= 0.0001f)
        {
            if (_config != null && !_config.UseForwardWhenNoMovementInput)
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
        _character.DashInput = false;
        _suppressUntilInputRelease = true;

        _invulnerabilityController?.Begin(_dashInvulnerabilityConfig);

        RaiseDashEvent(VSplatterDashEventType.Started);
        DashStarted?.Invoke();
        PublishCooldownSnapshot();
        return true;
    }

    public void CancelDash()
    {
        bool wasDashing = _isDashing;
        Vector3 lastDirection = _dashDirection;

        _isDashing = false;
        _elapsed = 0f;
        _remainingTime = 0f;
        _dashDirection = Vector3.zero;

        _invulnerabilityController?.End(_dashInvulnerabilityConfig);

        if (wasDashing)
        {
            RaiseDashEvent(VSplatterDashEventType.Canceled, lastDirection);
            DashCanceled?.Invoke();
        }

        PublishCooldownSnapshot();
    }

    public Vector3 BuildDashMovementVector(float currentY)
    {
        if (!_isDashing)
            return new Vector3(0f, currentY, 0f);

        return new Vector3(
            _dashDirection.x * DashSpeed,
            currentY,
            _dashDirection.z * DashSpeed);
    }

    private void FinishDash()
    {
        _isDashing = false;
        _elapsed = 0f;
        _remainingTime = 0f;

        _invulnerabilityController?.End(_dashInvulnerabilityConfig);

        RaiseDashEvent(VSplatterDashEventType.Finished, _dashDirection);
        DashFinished?.Invoke();
        PublishCooldownSnapshot();
    }

    private void RaiseDashEvent(VSplatterDashEventType eventType)
    {
        RaiseDashEvent(eventType, _dashDirection);
    }

    private void RaiseDashEvent(VSplatterDashEventType eventType, Vector3 direction)
    {
        VSplatterDashEvent dashEvent = new VSplatterDashEvent(
            eventType,
            gameObject,
            transform,
            direction,
            DashSpeed,
            ResolveDashDuration());

        _dashEventChannel?.RaiseEvent(dashEvent);
        DashEventRaised?.Invoke(dashEvent);
    }

    private void UpdateCooldownRuntime()
    {
        _cooldownRemaining = CooldownRemaining;
    }

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

    private void PublishCooldownSnapshotIfNeeded()
    {
        float remaining = CooldownRemaining;

        if (Mathf.Approximately(_lastPublishedRemaining, remaining) &&
            _lastPublishedDashing == _isDashing)
            return;

        _lastPublishedRemaining = remaining;
        _lastPublishedDashing = _isDashing;
        PublishCooldownSnapshot();
    }

    private float ResolveDashDuration()
    {
        return _config != null ? _config.DashDuration : 0.18f;
    }

    private float ResolveDashCooldown()
    {
        return _config != null ? _config.CooldownSeconds : 0f;
    }
}
