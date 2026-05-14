using UnityEngine;

[CreateAssetMenu(
    fileName = "NamedPatternConfig",
    menuName = "Named Enemy/Pattern Config")]
public class NamedPatternConfigSO : ScriptableObject
{
    [Header("Start Delay")]
    [Tooltip("Seconds after named combat starts before the first pattern becomes ready.")]
    [SerializeField, Min(0f)] private float _firstPatternDelay = 30f;

    [Header("Repeat")]
    [Tooltip("If true, pattern timer restarts after PatternResolve.")]
    [SerializeField] private bool _repeatPattern = true;

    [Tooltip("Seconds after PatternResolve before the next pattern becomes ready.")]
    [SerializeField, Min(0f)] private float _repeatPatternDelay = 45f;

    [Header("Active Duration")]
    [Tooltip("Time limit while PatternActive state is running.")]
    [SerializeField, Min(0.01f)] private float _patternActiveDuration = 20f;

    [Header("Damage Multiplier During Pattern Active")]
    [SerializeField] private bool _applyActiveDamageMultiplier = true;
    [SerializeField, Min(0f)] private float _activeDamageTakenMultiplier = 0.5f;

    [Header("Damage Multiplier After Pattern Success")]
    [SerializeField] private bool _applySuccessDamageMultiplier = true;
    [SerializeField, Min(0f)] private float _successDamageTakenMultiplier = 2f;
    [SerializeField, Min(0f)] private float _successMultiplierDuration = 8f;

    [Header("Damage Multiplier After Pattern Failure")]
    [SerializeField] private bool _applyFailureDamageMultiplier = true;
    [SerializeField, Min(0f)] private float _failureDamageTakenMultiplier = 1f;
    [SerializeField, Min(0f)] private float _failureMultiplierDuration = 3f;

    public float FirstPatternDelay => _firstPatternDelay;
    public bool RepeatPattern => _repeatPattern;
    public float RepeatPatternDelay => _repeatPatternDelay;
    public float PatternActiveDuration => _patternActiveDuration;

    public bool ApplyActiveDamageMultiplier => _applyActiveDamageMultiplier;
    public float ActiveDamageTakenMultiplier => _activeDamageTakenMultiplier;

    public bool ApplySuccessDamageMultiplier => _applySuccessDamageMultiplier;
    public float SuccessDamageTakenMultiplier => _successDamageTakenMultiplier;
    public float SuccessMultiplierDuration => _successMultiplierDuration;

    public bool ApplyFailureDamageMultiplier => _applyFailureDamageMultiplier;
    public float FailureDamageTakenMultiplier => _failureDamageTakenMultiplier;
    public float FailureMultiplierDuration => _failureMultiplierDuration;
}
