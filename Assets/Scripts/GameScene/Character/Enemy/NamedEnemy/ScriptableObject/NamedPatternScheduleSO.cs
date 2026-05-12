using UnityEngine;

[CreateAssetMenu(
    fileName = "NamedPatternSchedule",
    menuName = "Named Enemy/Pattern Schedule")]
public class NamedPatternScheduleSO : ScriptableObject
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

    public float PatternActiveDuration => _patternActiveDuration;
    public float FirstPatternDelay => _firstPatternDelay;
    public bool RepeatPattern => _repeatPattern;
    public float RepeatPatternDelay => _repeatPatternDelay;
}
