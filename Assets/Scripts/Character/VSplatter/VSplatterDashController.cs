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
    [Header("Invulnerability")]
    [SerializeField] private InvulnerabilityController _invulnerabilityController;
    [SerializeField] private InvulnerabilityConfigSO _dashInvulnerabilityConfig;
        
    [Header("Debug")]
    [ReadOnly] [SerializeField] private bool _isDashing;
    [ReadOnly] [SerializeField] private float _remainingTime;
    [ReadOnly] [SerializeField] private Vector3 _dashDirection;

    private float _elapsed;
    private bool _suppressUntilInputRelease;

    public bool IsDashing => _isDashing;
    public Vector3 DashDirection => _dashDirection;
    public float DashSpeed => _config != null ? _config.DashSpeed : 14f;
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

    private void OnDisable()
    {
        CancelDash();
    }

    private void Update()
    {
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
        _character.DashInput = false;
        _suppressUntilInputRelease = true;
        _invulnerabilityController?.Begin(_dashInvulnerabilityConfig);
        RaiseDashEvent(VSplatterDashEventType.Started);
        DashStarted?.Invoke();
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

    private float ResolveDashDuration()
    {
        return _config != null ? _config.DashDuration : 0.18f;
    }
}
