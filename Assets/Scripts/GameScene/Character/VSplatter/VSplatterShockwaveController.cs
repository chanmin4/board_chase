using System;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public class VSplatterShockwaveController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VSplatter_Character _character;
    [SerializeField] private Transform _origin;
    [SerializeField] private VSplatterShockwaveConfigSO _config;
    [SerializeField] private VSplatterShockwaveEventChannelSO _shockwaveEventChannel;

    [Header("Debug")]
    [ReadOnly] [SerializeField] private bool _isCharging;
    [ReadOnly] [SerializeField] private bool _releaseTriggeredThisFrame;
    [ReadOnly] [SerializeField] private bool _suppressUntilInputRelease;
    [ReadOnly] [SerializeField] private float _chargeStartTime;

    public bool IsCharging => _isCharging;
    public bool WasReleasedThisFrame => _releaseTriggeredThisFrame;
    public float ChargeDuration => _isCharging ? Mathf.Max(0f, Time.time - _chargeStartTime) : 0f;
    public float ChargeNormalized => ResolveMaxChargeTime() <= 0f ? 1f : Mathf.Clamp01(ChargeDuration / ResolveMaxChargeTime());
    public Transform Origin => _origin != null ? _origin : transform;
    public VSplatterShockwaveEventChannelSO ShockwaveEventChannel => _shockwaveEventChannel;

    public event Action ChargeStarted;
    public event Action ChargeReleased;
    public event Action ChargeCanceled;
    public event Action<VSplatterShockwaveEvent> ShockwaveEventRaised;

    private void Awake()
    {
        if (_character == null)
            TryGetComponent(out _character);

        if (_origin == null)
            _origin = transform;

        if (GetComponent<VSplatterShockwaveEffectResolver>() == null)
            gameObject.AddComponent<VSplatterShockwaveEffectResolver>();
    }

    private void OnDisable()
    {
        ResetState();
    }

    private void Update()
    {
        _releaseTriggeredThisFrame = false;

        if (_character == null)
            return;

        bool wantsCharge = _character.ShockwaveInput;

        if (_suppressUntilInputRelease)
        {
            if (!wantsCharge)
                _suppressUntilInputRelease = false;

            return;
        }

        if (wantsCharge)
        {
            if (!_isCharging)
            {
                _isCharging = true;
                _chargeStartTime = Time.time;
                RaiseShockwaveEvent(BuildEvent(VSplatterShockwaveEventType.ChargeStarted, 0f));
                ChargeStarted?.Invoke();
            }

            return;
        }

        if (_isCharging)
            ReleaseCharge();
    }

    public bool ConsumeReleaseThisFrame()
    {
        bool wasReleased = _releaseTriggeredThisFrame;
        _releaseTriggeredThisFrame = false;
        return wasReleased;
    }

    public void CancelCharge(bool suppressUntilInputRelease = true)
    {
        bool wasCharging = _isCharging;
        float chargeDuration = wasCharging ? Mathf.Max(0f, Time.time - _chargeStartTime) : 0f;
        float normalizedCharge = ResolveMaxChargeTime() <= 0f ? 1f : Mathf.Clamp01(chargeDuration / ResolveMaxChargeTime());

        _isCharging = false;
        _releaseTriggeredThisFrame = false;
        _chargeStartTime = 0f;

        if (suppressUntilInputRelease)
            _suppressUntilInputRelease = true;

        if (wasCharging)
        {
            RaiseShockwaveEvent(BuildEvent(VSplatterShockwaveEventType.Canceled, normalizedCharge, chargeDuration));
            ChargeCanceled?.Invoke();
        }
    }

    private void ReleaseCharge()
    {
        float chargeDuration = ChargeDuration;
        float normalizedCharge = ChargeNormalized;
        _isCharging = false;
        _releaseTriggeredThisFrame = true;
        RaiseShockwaveEvent(BuildEvent(VSplatterShockwaveEventType.Released, normalizedCharge, chargeDuration));
        ChargeReleased?.Invoke();
    }

    private void ResetState()
    {
        _isCharging = false;
        _releaseTriggeredThisFrame = false;
        _suppressUntilInputRelease = false;
        _chargeStartTime = 0f;
    }

    private VSplatterShockwaveEvent BuildEvent(VSplatterShockwaveEventType eventType, float normalizedCharge)
    {
        return BuildEvent(eventType, normalizedCharge, ChargeDuration);
    }

    private VSplatterShockwaveEvent BuildEvent(
        VSplatterShockwaveEventType eventType,
        float normalizedCharge,
        float chargeDuration)
    {
        Vector3 center = Origin.position;
        float radius = Mathf.Lerp(ResolveBaseRadius(), ResolveMaxRadius(), normalizedCharge);
        float damage = Mathf.Lerp(ResolveBaseDamage(), ResolveMaxDamage(), normalizedCharge);
        float knockbackDistance = Mathf.Lerp(ResolveBaseKnockbackDistance(), ResolveMaxKnockbackDistance(), normalizedCharge);

        return new VSplatterShockwaveEvent(
            eventType,
            gameObject,
            Origin,
            center,
            chargeDuration,
            normalizedCharge,
            radius,
            damage,
            knockbackDistance,
            ResolveKnockbackDuration(),
            ResolveHitMask(),
            ResolveTriggerInteraction(),
            ResolveApplyDamage(),
            ResolveApplyKnockback());
    }

    private void RaiseShockwaveEvent(VSplatterShockwaveEvent shockwaveEvent)
    {
        _shockwaveEventChannel?.RaiseEvent(shockwaveEvent);
        ShockwaveEventRaised?.Invoke(shockwaveEvent);
    }

    private float ResolveMaxChargeTime() => _config != null ? _config.MaxChargeTime : 1f;
    private float ResolveBaseRadius() => _config != null ? _config.BaseRadius : 2f;
    private float ResolveMaxRadius() => _config != null ? _config.MaxRadius : 5f;
    private float ResolveBaseDamage() => _config != null ? _config.BaseDamage : 5f;
    private float ResolveMaxDamage() => _config != null ? _config.MaxDamage : 15f;
    private float ResolveBaseKnockbackDistance() => _config != null ? _config.BaseKnockbackDistance : 2f;
    private float ResolveMaxKnockbackDistance() => _config != null ? _config.MaxKnockbackDistance : 5f;
    private float ResolveKnockbackDuration() => _config != null ? _config.KnockbackDuration : 0.2f;
    private bool ResolveApplyDamage() => _config == null || _config.ApplyDamage;
    private bool ResolveApplyKnockback() => _config == null || _config.ApplyKnockback;
    private LayerMask ResolveHitMask() => _config != null ? _config.HitMask : ~0;
    private QueryTriggerInteraction ResolveTriggerInteraction() => _config != null ? _config.TriggerInteraction : QueryTriggerInteraction.Collide;
}
