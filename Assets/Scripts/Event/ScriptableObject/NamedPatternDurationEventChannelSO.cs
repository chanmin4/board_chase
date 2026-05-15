using System;
using UnityEngine;

public readonly struct NamedPatternDurationSnapshot
{
    public readonly bool visible;
    public readonly string patternName;
    public readonly float remainingSeconds;
    public readonly float durationSeconds;
    public readonly float remaining01;
    public readonly bool objectiveVisible;
    public readonly int objectiveRemaining;
    public readonly int objectiveRequired;
    public readonly int objectiveCompleted;
    public NamedPatternDurationSnapshot(
        bool visible,
        string patternName,
        float remainingSeconds,
        float durationSeconds,
        bool objectiveVisible = false,
        int objectiveRemaining = 0,
        int objectiveRequired = 0,
        int objectiveCompleted = 0
        )
    {
        this.visible = visible;
        this.patternName = patternName;
        this.remainingSeconds = Mathf.Max(0f, remainingSeconds);
        this.durationSeconds = Mathf.Max(0.01f, durationSeconds);

        remaining01 = visible
            ? Mathf.Clamp01(this.remainingSeconds / this.durationSeconds)
            : 0f;
        this.objectiveVisible = objectiveVisible;
        this.objectiveRemaining = Mathf.Max(0, objectiveRemaining);
        this.objectiveRequired = Mathf.Max(0, objectiveRequired);
        this.objectiveCompleted = Mathf.Max(0, objectiveCompleted);
    }

    public static NamedPatternDurationSnapshot Hidden =>
        new NamedPatternDurationSnapshot(false, string.Empty, 0f, 1f);
}

[CreateAssetMenu(
    fileName = "NamedPatternDurationEventChannel",
    menuName = "Events/Named Enemy/Named Pattern Duration Event Channel")]
public class NamedPatternDurationEventChannelSO : ScriptableObject
{
    public event Action<NamedPatternDurationSnapshot> OnEventRaised;

    public void RaiseEvent(NamedPatternDurationSnapshot snapshot)
    {
        OnEventRaised?.Invoke(snapshot);
    }
}
