using System;
using UnityEngine;

[Serializable]
public readonly struct NamedSectorTimerSnapshot
{
    public readonly NamedSectorPhase phase;
    public readonly SectorRuntime sector;
    public readonly float remainingSeconds;
    public readonly float durationSeconds;
    public readonly float normalized;

    public NamedSectorTimerSnapshot(
        NamedSectorPhase phase,
        SectorRuntime sector,
        float remainingSeconds,
        float durationSeconds)
    {
        this.phase = phase;
        this.sector = sector;
        this.remainingSeconds = Mathf.Max(0f, remainingSeconds);
        this.durationSeconds = Mathf.Max(0f, durationSeconds);

        normalized = this.durationSeconds > 0f
            ? 1f - Mathf.Clamp01(this.remainingSeconds / this.durationSeconds)
            : 0f;
    }
}

[CreateAssetMenu(
    fileName = "NamedSectorTimerSnapshotEventChannel",
    menuName = "Events/Named Sector Timer Snapshot Event Channel")]
public class NamedSectorTimerSnapshotEventChannelSO : ScriptableObject
{
    public event Action<NamedSectorTimerSnapshot> OnEventRaised;

    public void RaiseEvent(NamedSectorTimerSnapshot snapshot)
    {
        OnEventRaised?.Invoke(snapshot);
    }
}
