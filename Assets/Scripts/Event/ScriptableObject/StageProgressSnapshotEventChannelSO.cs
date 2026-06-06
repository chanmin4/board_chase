using System;
using UnityEngine;

[Serializable]
public struct StageProgressSnapshot
{
    public int stageIndex;
    public string displayName;

    public float remainingSeconds;
    public float durationSeconds;
    public float progress01;

    public int requiredPlayerOwnedCount;
    public int currentPlayerOwnedCount;

    public bool requirementMet;
    public bool isCompleted;
    public bool hasNextStage;
    public bool isStartSector;

    public bool isResting;
    public float restRemainingSeconds;
    public float restDurationSeconds;
    public float restProgress01;
}

[CreateAssetMenu(
    fileName = "StageProgressSnapshotChanged",
    menuName = "Events/Stage Progress Snapshot Changed")]
public class StageProgressSnapshotEventChannelSO : ScriptableObject
{
    public event Action<StageProgressSnapshot> OnEventRaised;

    private bool _hasCurrent;
    private StageProgressSnapshot _current;

    public bool HasCurrent => _hasCurrent;
    public StageProgressSnapshot Current => _current;

    public void RaiseEvent(StageProgressSnapshot snapshot)
    {
        _current = snapshot;
        _hasCurrent = true;
        OnEventRaised?.Invoke(snapshot);
    }

    public void Clear()
    {
        _hasCurrent = false;
        _current = default;
    }
}